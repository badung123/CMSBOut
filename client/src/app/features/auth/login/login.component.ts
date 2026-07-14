import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  loading = false;
  errorMessage = '';
  step: 'credentials' | 'twoFactor' = 'credentials';
  pendingToken = '';
  pendingUserName = '';

  form = this.fb.nonNullable.group({
    userName: ['', Validators.required],
    password: ['', Validators.required]
  });

  twoFactorForm = this.fb.nonNullable.group({
    code: ['', [Validators.required, Validators.pattern(/^[\dA-Za-z-]{6,20}$/)]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.auth.login(this.form.getRawValue()).subscribe({
      next: (response) => {
        this.loading = false;

        if (response.requiresTwoFactor) {
          if (!response.pendingToken) {
            this.errorMessage = 'Không thể khởi tạo bước xác thực 2FA. Vui lòng thử lại.';
            return;
          }

          this.pendingToken = response.pendingToken;
          this.pendingUserName = response.userName;
          this.step = 'twoFactor';
          this.twoFactorForm.reset({ code: '' });
          return;
        }

        if (!response.token) {
          this.errorMessage = 'Đăng nhập thất bại. Vui lòng thử lại.';
          return;
        }

        this.auth.completeLogin(response);
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Tên đăng nhập hoặc mật khẩu không đúng.';
      }
    });
  }

  submitTwoFactor(): void {
    if (this.twoFactorForm.invalid) {
      this.twoFactorForm.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.errorMessage = '';

    this.auth.verifyTwoFactor({
      pendingToken: this.pendingToken,
      code: this.twoFactorForm.controls.code.value.trim()
    }).subscribe({
      next: () => {
        this.loading = false;
        this.router.navigate(['/dashboard']);
      },
      error: () => {
        this.loading = false;
        this.errorMessage = 'Mã xác thực không đúng hoặc đã hết hạn.';
      }
    });
  }

  backToCredentials(): void {
    this.step = 'credentials';
    this.pendingToken = '';
    this.pendingUserName = '';
    this.errorMessage = '';
    this.twoFactorForm.reset({ code: '' });
  }
}
