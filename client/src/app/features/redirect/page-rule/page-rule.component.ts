import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { CloudflareAccountService } from '../../../core/services/cloudflare-account.service';
import { CloudflareAccount, SaveCloudflareAccountRequest } from '../../../core/models/api.models';
import { formatDateTime } from '../../../shared/utils/text.utils';

@Component({
  selector: 'app-page-rule',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './page-rule.component.html',
  styleUrl: './page-rule.component.scss'
})
export class PageRuleComponent implements OnInit {
  private readonly cloudflareService = inject(CloudflareAccountService);
  private readonly fb = inject(FormBuilder);

  accounts: CloudflareAccount[] = [];
  loading = false;
  saving = false;
  errorMessage = '';
  editingId: string | null = null;

  formatDateTime = formatDateTime;

  form = this.fb.nonNullable.group({
    userCloudflare: ['', Validators.required],
    zoneId: ['', Validators.required],
    accountId: ['', Validators.required],
    apiToken: [''],
    listDomainRedirect: ['', Validators.required],
    status: [true]
  });

  ngOnInit(): void {
    this.loadAccounts();
  }

  loadAccounts(): void {
    this.loading = true;
    this.cloudflareService.getAll().subscribe({
      next: (accounts) => {
        this.accounts = accounts;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Không thể tải danh sách tài khoản Cloudflare.';
      }
    });
  }

  startEdit(account: CloudflareAccount): void {
    this.editingId = account.id;
    this.form.patchValue({
      userCloudflare: account.userCloudflare,
      zoneId: account.zoneId,
      accountId: account.accountId,
      apiToken: account.apiToken ?? '',
      listDomainRedirect: account.listDomainRedirect,
      status: account.status
    });
    this.errorMessage = '';
  }

  cancelEdit(): void {
    this.editingId = null;
    this.form.reset({
      userCloudflare: '',
      zoneId: '',
      accountId: '',
      apiToken: '',
      listDomainRedirect: '',
      status: true
    });
    this.errorMessage = '';
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.errorMessage = '';
    const request = this.toRequest();
    const request$ = this.editingId
      ? this.cloudflareService.update(this.editingId, request)
      : this.cloudflareService.create(request);

    request$.subscribe({
      next: () => {
        this.saving = false;
        this.cancelEdit();
        this.loadAccounts();
      },
      error: (err) => {
        this.saving = false;
        this.errorMessage = err.error?.message ?? 'Có lỗi xảy ra.';
      }
    });
  }

  toggleStatus(account: CloudflareAccount): void {
    const request: SaveCloudflareAccountRequest = {
      userCloudflare: account.userCloudflare,
      zoneId: account.zoneId,
      accountId: account.accountId,
      apiToken: account.apiToken ?? '',
      listDomainRedirect: account.listDomainRedirect,
      status: !account.status
    };

    this.cloudflareService.update(account.id, request).subscribe({
      next: () => this.loadAccounts(),
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Không thể đổi trạng thái.';
      }
    });
  }

  delete(account: CloudflareAccount): void {
    if (!confirm(`Xóa tài khoản Cloudflare "${account.userCloudflare}"?`)) return;

    this.cloudflareService.delete(account.id).subscribe({
      next: () => {
        if (this.editingId === account.id) {
          this.cancelEdit();
        }
        this.loadAccounts();
      },
      error: (err) => alert(err.error?.message ?? 'Không thể xóa tài khoản.')
    });
  }

  private toRequest(): SaveCloudflareAccountRequest {
    const value = this.form.getRawValue();
    return {
      userCloudflare: value.userCloudflare.trim(),
      zoneId: value.zoneId.trim(),
      accountId: value.accountId.trim(),
      apiToken: (value.apiToken ?? '').trim(),
      listDomainRedirect: value.listDomainRedirect.trim(),
      status: value.status
    };
  }
}
