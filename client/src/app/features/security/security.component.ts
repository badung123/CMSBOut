import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-security',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './security.component.html',
  styleUrl: './security.component.scss'
})
export class SecurityComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  loading = true;
  twoFactorEnabled = false;
  setupMode = false;
  sharedKey = '';
  authenticatorUri = '';
  recoveryCodes: string[] = [];
  message = '';
  errorMessage = '';

  enableForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^\d{6}$/)]]
  });

  disableForm = this.fb.nonNullable.group({
    password: ['', Validators.required]
  });

  ngOnInit(): void {
    this.loadStatus();
  }

  loadStatus(): void {
    this.loading = true;
    this.auth.getTwoFactorStatus().subscribe({
      next: (res) => {
        this.twoFactorEnabled = res.enabled;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Không thể tải trạng thái bảo mật.';
      }
    });
  }

  startSetup(): void {
    this.message = '';
    this.errorMessage = '';
    this.recoveryCodes = [];
    this.setupMode = true;

    this.auth.setupTwoFactor().subscribe({
      next: (res) => {
        this.sharedKey = res.sharedKey;
        this.authenticatorUri = res.authenticatorUri;
      },
      error: (err) => {
        this.setupMode = false;
        this.errorMessage = err.error?.message ?? 'Không thể khởi tạo 2FA.';
      }
    });
  }

  enableTwoFactor(): void {
    if (this.enableForm.invalid) {
      this.enableForm.markAllAsTouched();
      return;
    }

    this.message = '';
    this.errorMessage = '';

    this.auth.enableTwoFactor({ code: this.enableForm.controls.code.value.trim() }).subscribe({
      next: (res) => {
        this.twoFactorEnabled = true;
        this.setupMode = false;
        this.recoveryCodes = res.recoveryCodes;
        this.message = 'Bật 2FA thành công. Hãy lưu các mã khôi phục bên dưới.';
        this.enableForm.reset({ code: '' });
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Mã xác thực không đúng.';
      }
    });
  }

  disableTwoFactor(): void {
    if (this.disableForm.invalid) {
      this.disableForm.markAllAsTouched();
      return;
    }

    this.message = '';
    this.errorMessage = '';

    this.auth.disableTwoFactor({ password: this.disableForm.controls.password.value }).subscribe({
      next: () => {
        this.twoFactorEnabled = false;
        this.setupMode = false;
        this.recoveryCodes = [];
        this.sharedKey = '';
        this.message = 'Đã tắt 2FA.';
        this.disableForm.reset({ password: '' });
      },
      error: (err) => {
        this.errorMessage = err.error?.message ?? 'Không thể tắt 2FA.';
      }
    });
  }

  cancelSetup(): void {
    this.setupMode = false;
    this.sharedKey = '';
    this.authenticatorUri = '';
    this.enableForm.reset({ code: '' });
    this.errorMessage = '';
  }

  copySharedKey(): void {
    void navigator.clipboard.writeText(this.sharedKey.replace(/\s/g, ''));
    this.message = 'Đã sao chép khóa bí mật.';
  }
}
