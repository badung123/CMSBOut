import { Component, ElementRef, HostListener, OnDestroy, QueryList, ViewChildren, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CloudflareAccountService } from '../../../core/services/cloudflare-account.service';
import { PurgeCacheResult } from '../../../core/models/api.models';

type ProgressState = 'waiting' | 'running' | 'success' | 'error' | 'stopped';

interface ProgressItem {
  domain: string;
  state: ProgressState;
  message: string;
}

@Component({
  selector: 'app-cache',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './cache.component.html',
  styleUrl: './cache.component.scss'
})
export class CacheComponent implements OnDestroy {
  private readonly cloudflareService = inject(CloudflareAccountService);
  private readonly fb = inject(FormBuilder);
  private destroyed = false;
  private cancelRequested = false;

  @ViewChildren('progressRow') progressRows?: QueryList<ElementRef<HTMLElement>>;

  running = false;
  popupOpen = false;
  errorMessage = '';
  progress: ProgressItem[] = [];

  form = this.fb.nonNullable.group({
    domainList: ['']
  });

  get doneCount(): number {
    return this.progress.filter(item => item.state !== 'waiting' && item.state !== 'running').length;
  }

  get successCount(): number {
    return this.progress.filter(item => item.state === 'success').length;
  }

  ngOnDestroy(): void {
    this.destroyed = true;
    this.cancelRequested = true;
  }

  @HostListener('document:keydown.escape')
  onEscape(): void {
    this.closePopup();
  }

  async start(): Promise<void> {
    if (this.running) return;

    const domains = parseDomains(this.form.controls.domainList.value);
    if (domains.length === 0) {
      this.errorMessage = 'Nhập ít nhất một domain.';
      return;
    }

    this.errorMessage = '';
    this.cancelRequested = false;
    this.progress = domains.map(domain => ({
      domain,
      state: 'waiting',
      message: 'Đang chờ'
    }));
    this.popupOpen = true;
    this.running = true;
    this.form.disable();

    try {
      for (let index = 0; index < this.progress.length; index++) {
        if (this.destroyed) return;

        const item = this.progress[index];
        if (this.cancelRequested) {
          item.state = 'stopped';
          item.message = 'Đã dừng.';
          continue;
        }

        item.state = 'running';
        item.message = 'Đang xóa cache...';
        this.scrollTo(index);

        try {
          const result = await firstValueFrom(this.cloudflareService.purgeEverything(item.domain));
          if (this.destroyed) return;
          this.applyResult(item, result);
        } catch (err: unknown) {
          if (this.destroyed) return;
          item.state = 'error';
          item.message = readError(err);
        }

        this.scrollTo(index);
      }
    } finally {
      if (!this.destroyed) {
        this.running = false;
        this.form.enable();
      }
    }
  }

  stop(): void {
    this.cancelRequested = true;
  }

  closePopup(): void {
    if (this.running) return;
    this.popupOpen = false;
  }

  private applyResult(item: ProgressItem, result: PurgeCacheResult): void {
    if (result.domain) item.domain = result.domain;
    item.message = result.message || (result.success ? 'Đã xóa cache.' : 'Không xóa được cache.');
    item.state = result.success ? 'success' : 'error';
  }

  private scrollTo(index: number): void {
    setTimeout(() => {
      this.progressRows?.get(index)?.nativeElement.scrollIntoView({ block: 'nearest' });
    });
  }
}

function parseDomains(text: string): string[] {
  const seen = new Set<string>();
  const domains: string[] = [];

  for (const part of text.split(/[\s,;]+/)) {
    const domain = normalizeDomain(part);
    if (!domain || seen.has(domain)) continue;
    seen.add(domain);
    domains.push(domain);
  }

  return domains;
}

function normalizeDomain(raw: string): string {
  let value = raw.trim().replace(/^['"]+|['"]+$/g, '');
  value = value.replace(/^[a-z][a-z0-9+.-]*:\/\//i, '');
  value = value.split(/[/?#]/)[0];
  value = value.replace(/^\*\./, '').replace(/\/\*$/, '');
  const colon = value.indexOf(':');
  if (colon >= 0) value = value.slice(0, colon);
  return value.replace(/\.$/, '').toLowerCase();
}

function readError(err: unknown): string {
  if (typeof err === 'object' && err && 'error' in err) {
    const body = (err as { error?: { message?: string } }).error;
    if (body?.message) return body.message;
  }

  return 'Không gọi được API.';
}
