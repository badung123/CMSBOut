import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BankoutService } from '../../core/services/bankout.service';
import { AgentOption, BankoutListItem, PagedResponse } from '../../core/models/api.models';
import { StatusActionEnum, StatusClassMap, StatusLabels } from '../../core/models/enums';
import { formatCurrency, formatDateTime, toUppercaseNoAccent } from '../../shared/utils/text.utils';

@Component({
  selector: 'app-bankout',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './bankout.component.html',
  styleUrl: './bankout.component.scss'
})
export class BankoutComponent implements OnInit {
  private readonly bankoutService = inject(BankoutService);
  private readonly fb = inject(FormBuilder);

  agents: AgentOption[] = [];
  listData: PagedResponse<BankoutListItem> | null = null;
  loading = false;
  saving = false;
  formError = '';
  formSuccess = '';

  readonly statusOptions = [
    { value: null, label: 'Tất cả' },
    { value: StatusActionEnum.WaitAccept, label: StatusLabels[StatusActionEnum.WaitAccept] },
    { value: StatusActionEnum.WaitBank, label: StatusLabels[StatusActionEnum.WaitBank] },
    { value: StatusActionEnum.Success, label: StatusLabels[StatusActionEnum.Success] },
    { value: StatusActionEnum.Error, label: StatusLabels[StatusActionEnum.Error] }
  ];

  readonly statusLabels = StatusLabels;
  readonly statusClassMap = StatusClassMap;
  readonly StatusActionEnum = StatusActionEnum;
  formatCurrency = formatCurrency;
  formatDateTime = formatDateTime;

  form = this.fb.nonNullable.group({
    requestBankId: ['', Validators.required],
    userName: ['', Validators.required],
    bankAccountName: ['', Validators.required],
    bankAccountNumber: ['', Validators.required],
    amount: [10000, [Validators.required, Validators.min(10000), Validators.max(10000000)]],
    bank: ['', Validators.required],
    agentId: [0, [Validators.required, Validators.min(1)]]
  });

  filterForm = this.fb.nonNullable.group({
    userName: [''],
    requestBankId: [''],
    status: [null as number | null],
    fromDate: [''],
    toDate: [''],
    page: [1],
    pageSize: [10]
  });

  ngOnInit(): void {
    this.loadAgents();
    this.loadList();
  }

  get previewAccountName(): string {
    return toUppercaseNoAccent(this.form.controls.bankAccountName.value);
  }

  loadAgents(): void {
    this.bankoutService.getAgentOptions().subscribe({
      next: (agents) => {
        this.agents = agents;
        if (agents.length > 0) {
          this.form.patchValue({ agentId: agents[0].id });
        }
      }
    });
  }

  loadList(): void {
    this.loading = true;
    const filter = this.filterForm.getRawValue();

    this.bankoutService.getList({
      userName: filter.userName || undefined,
      requestBankId: filter.requestBankId || undefined,
      status: filter.status,
      fromDate: filter.fromDate || undefined,
      toDate: filter.toDate || undefined,
      page: filter.page,
      pageSize: filter.pageSize
    }).subscribe({
      next: (data) => {
        this.listData = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving = true;
    this.formError = '';
    this.formSuccess = '';

    const raw = this.form.getRawValue();
    this.bankoutService.create({
      ...raw,
      bankAccountName: toUppercaseNoAccent(raw.bankAccountName)
    }).subscribe({
      next: () => {
        this.saving = false;
        this.formSuccess = 'Lưu yêu cầu bank-out thành công.';
        this.form.reset({
          requestBankId: '',
          userName: '',
          bankAccountName: '',
          bankAccountNumber: '',
          amount: 10000,
          bank: '',
          agentId: this.agents[0]?.id ?? 0
        });
        this.loadList();
      },
      error: (err) => {
        this.saving = false;
        this.formError = err.error?.message ?? 'Không thể lưu yêu cầu.';
      }
    });
  }

  search(): void {
    this.filterForm.patchValue({ page: 1 });
    this.loadList();
  }

  resetFilter(): void {
    this.filterForm.reset({
      userName: '',
      requestBankId: '',
      status: null,
      fromDate: '',
      toDate: '',
      page: 1,
      pageSize: 10
    });
    this.loadList();
  }

  changePage(page: number): void {
    if (!this.listData || page < 1 || page > this.listData.totalPages) return;
    this.filterForm.patchValue({ page });
    this.loadList();
  }

  approve(item: BankoutListItem): void {
    if (!confirm('Bạn có chắc muốn duyệt giao dịch này?')) return;

    this.bankoutService.approve(item.id).subscribe({
      next: () => this.loadList(),
      error: (err) => alert(err.error?.message ?? 'Không thể duyệt.')
    });
  }

  cancel(item: BankoutListItem): void {
    if (!confirm('Bạn có chắc muốn hủy giao dịch này?')) return;

    this.bankoutService.cancel(item.id).subscribe({
      next: () => this.loadList(),
      error: (err) => alert(err.error?.message ?? 'Không thể hủy.')
    });
  }

  getStatusLabel(status: number): string {
    return StatusLabels[status as StatusActionEnum] ?? 'Unknown';
  }

  getStatusClass(status: number): string {
    return StatusClassMap[status as StatusActionEnum] ?? '';
  }

  canAction(item: BankoutListItem): boolean {
    return item.status === StatusActionEnum.WaitAccept;
  }
}
