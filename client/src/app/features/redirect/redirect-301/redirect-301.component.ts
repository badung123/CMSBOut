import { Component, ElementRef, HostListener, OnDestroy, QueryList, ViewChildren, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';
import { CloudflareAccountService } from '../../../core/services/cloudflare-account.service';
import { Redirect301Result } from '../../../core/models/api.models';

type ProgressState = 'waiting' | 'running' | 'success' | 'warning' | 'error' | 'stopped';

interface ProgressItem {
  domain: string;
  state: ProgressState;
  message: string;
}

@Component({
  selector: 'app-redirect-301',
  standalone: true,
  imports: [ReactiveFormsModule],
  templateUrl: './redirect-301.component.html',
  styleUrl: './redirect-301.component.scss'
})
export class Redirect301Component implements OnDestroy {
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
    domainList: [''],
    destinationDomain: ['']
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

    const domainList = this.form.controls.domainList.value;
    const destinationRaw = this.form.controls.destinationDomain.value.trim();
    const domains = parseDomains(domainList);
    const destination = normalizeDomain(destinationRaw);

    if (domains.length === 0) {
      this.errorMessage = 'Nhập ít nhất một domain.';
      return;
    }

    if (!destinationRaw) {
      this.errorMessage = 'Nhập domain cuối.';
      return;
    }

    if (destinationRaw.split(/[\s,;]+/).filter(Boolean).length > 1) {
      this.errorMessage = 'Domain cuối chỉ nhập một domain.';
      return;
    }

    if (!destination.includes('.')) {
      this.errorMessage = 'Domain cuối không hợp lệ.';
      return;
    }

    this.errorMessage = '';
    this.cancelRequested = false;
    this.progress = domains.map(domain => ({
      domain,
      state: domain === destination ? 'warning' : 'waiting',
      message: domain === destination ? 'Trùng domain cuối, bỏ qua.' : 'Đang chờ'
    }));
    this.popupOpen = true;
    this.running = true;
    this.form.disable();

    try {
      for (let index = 0; index < this.progress.length; index++) {
        if (this.destroyed) return;

        const item = this.progress[index];
        if (item.state === 'warning') continue;

        if (this.cancelRequested) {
          item.state = 'stopped';
          item.message = 'Đã dừng.';
          continue;
        }

        item.state = 'running';
        item.message = 'Đang 301...';
        this.scrollTo(index);

        try {
          const result = await firstValueFrom(this.cloudflareService.apply301(item.domain, destination));
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

  private applyResult(item: ProgressItem, result: Redirect301Result): void {
    if (result.domain) item.domain = result.domain;
    item.message = result.message || (result.success ? 'Đã 301.' : 'Không 301 được.');
    if (!result.success) {
      item.state = result.action === 'skipped' ? 'warning' : 'error';
      return;
    }

    item.state = result.action === 'disabled' ? 'warning' : 'success';
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
