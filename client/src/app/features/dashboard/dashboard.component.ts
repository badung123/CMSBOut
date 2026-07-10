import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { DashboardService } from '../../core/services/dashboard.service';
import { AuthService } from '../../core/services/auth.service';
import { DashboardData } from '../../core/models/api.models';
import { formatCurrency } from '../../shared/utils/text.utils';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly dashboardService = inject(DashboardService);
  private readonly fb = inject(FormBuilder);
  readonly auth = inject(AuthService);

  data: DashboardData | null = null;
  loading = true;
  balanceMessage = '';
  balanceError = '';

  balanceForm = this.fb.nonNullable.group({
    amount: [0, [Validators.required, Validators.min(1)]]
  });

  formatCurrency = formatCurrency;

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;
    this.dashboardService.getDashboard().subscribe({
      next: (data) => {
        this.data = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  addBalance(): void {
    this.processBalanceChange('add');
  }

  subtractBalance(): void {
    this.processBalanceChange('subtract');
  }

  private processBalanceChange(type: 'add' | 'subtract'): void {
    if (this.balanceForm.invalid) {
      this.balanceForm.markAllAsTouched();
      return;
    }

    const amount = this.balanceForm.controls.amount.value;
    const request$ = type === 'add'
      ? this.dashboardService.addBalance(amount)
      : this.dashboardService.subtractBalance(amount);

    this.balanceMessage = '';
    this.balanceError = '';

    request$.subscribe({
      next: (res) => {
        if (this.data) {
          this.data = { ...this.data, balance: res.balance };
        }
        this.balanceMessage = type === 'add' ? 'Cộng tiền thành công.' : 'Trừ tiền thành công.';
        this.balanceForm.reset({ amount: 0 });
      },
      error: (err) => {
        this.balanceError = err.error?.message ?? 'Có lỗi xảy ra.';
      }
    });
  }
}
