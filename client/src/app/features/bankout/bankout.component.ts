import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { BankoutService } from '../../core/services/bankout.service';
import { BankoutListItem, PagedResponse, PartnerBankItem } from '../../core/models/api.models';
import { AgentOption } from '../../core/models/api.models';
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
  banks: PartnerBankItem[] = [];
  listData: PagedResponse<BankoutListItem> | null = null;
  loading = false;
  saving = false;
  approving = false;
  formError = '';
  formSuccess = '';
  approveTarget: BankoutListItem | null = null;

  readonly statusOptions = [
    { value: null, label: 'Tất cả' },
    { value: StatusActionEnum.WaitAccept, label: StatusLabels[StatusActionEnum.WaitAccept] },
    { value: StatusActionEnum.WaitBank, label: StatusLabels[StatusActionEnum.WaitBank] },
    { value: StatusActionEnum.Success, label: StatusLabels[StatusActionEnum.Success] },
    { value: StatusActionEnum.ErrorRequestBank, label: StatusLabels[StatusActionEnum.ErrorRequestBank] },
    { value: StatusActionEnum.ErrorBank, label: StatusLabels[StatusActionEnum.ErrorBank] }
  ];

  readonly LAYMA_AGENT_NAME = 'LAYMA';
  readonly StatusActionEnum = StatusActionEnum;
  formatCurrency = formatCurrency;
  formatDateTime = formatDateTime;

  form = this.fb.nonNullable.group({
    requestBankId: [''],
    bankAccountName: ['', Validators.required],
    bankAccountNumber: ['', Validators.required],
    amount: [100000, [Validators.required, Validators.min(100000), Validators.max(300000000)]],
    bankNo: ['', Validators.required],
    agentId: [0, [Validators.required, Validators.min(1)]]
  });

  filterForm = this.fb.nonNullable.group({
    requestBankId: [''],
    status: [null as number | null],
    fromDate: [''],
    toDate: [''],
    page: [1],
    pageSize: [10]
  });

  ngOnInit(): void {
    this.loadAgents();
    this.loadBanks();
    this.loadList();
    this.form.controls.agentId.valueChanges.subscribe(() => this.updateRequestBankIdValidation());
  }

  get isLaymaAgent(): boolean {
    const agent = this.getSelectedAgent();
    return agent?.agentName?.trim().toUpperCase() === this.LAYMA_AGENT_NAME;
  }

  getSelectedAgent(): AgentOption | undefined {
    const agentId = Number(this.form.controls.agentId.value);
    return this.agents.find(a => a.id === agentId);
  }

  updateRequestBankIdValidation(): void {
    const control = this.form.controls.requestBankId;
    if (this.isLaymaAgent) {
      control.setValidators(Validators.required);
    } else {
      control.clearValidators();
      control.setValue('');
    }
    control.updateValueAndValidity();
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
          this.updateRequestBankIdValidation();
        }
      }
    });
  }

  loadBanks(): void {
    this.bankoutService.getBanks().subscribe({
      next: (banks) => {
        this.banks = banks;
        if (banks.length > 0 && !this.form.controls.bankNo.value) {
          this.form.patchValue({ bankNo: banks[0].bankNo });
        }
      },
      error: () => {
        this.formError = 'Không thể tải danh sách ngân hàng.';
      }
    });
  }

  formatBankDisplay(item: BankoutListItem): string {
    if (item.bankName) {
      return `${item.shortBankName} - ${item.bankName}`;
    }
    const bank = this.banks.find(b => b.bankNo === item.bankNo);
    return bank ? `${bank.shortBankName} - ${bank.bankName}` : item.bankNo;
  }

  loadList(): void {
    this.loading = true;
    const filter = this.filterForm.getRawValue();

    this.bankoutService.getList({
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
    this.updateRequestBankIdValidation();
    this.formSuccess = '';

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.formError = this.getFormValidationError();
      return;
    }

    this.saving = true;
    this.formError = '';

    const raw = this.form.getRawValue();
    this.bankoutService.create({
      requestBankId: this.isLaymaAgent ? (raw.requestBankId || null) : null,
      bankAccountName: toUppercaseNoAccent(raw.bankAccountName),
      bankAccountNumber: raw.bankAccountNumber,
      amount: raw.amount,
      bankNo: raw.bankNo,
      agentId: raw.agentId
    }).subscribe({
      next: () => {
        this.saving = false;
        this.formSuccess = 'Lưu yêu cầu bank-out thành công.';
        this.form.reset({
          requestBankId: '',
          bankAccountName: '',
          bankAccountNumber: '',
          amount: 100000,
          bankNo: this.banks[0]?.bankNo ?? '',
          agentId: this.agents[0]?.id ?? 0
        });
        this.updateRequestBankIdValidation();
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

  openApproveConfirm(item: BankoutListItem): void {
    this.approveTarget = item;
  }

  closeApproveConfirm(): void {
    this.approveTarget = null;
    this.approving = false;
  }

  confirmApprove(): void {
    if (!this.approveTarget) return;

    this.approving = true;
    this.bankoutService.approve(this.approveTarget.id).subscribe({
      next: () => {
        this.closeApproveConfirm();
        this.loadList();
      },
      error: (err) => {
        this.approving = false;
        alert(err.error?.message ?? 'Không thể duyệt.');
      }
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

  private getFormValidationError(): string {
    const controls = this.form.controls;
    if (this.isLaymaAgent && controls.requestBankId.invalid) return 'Vui lòng nhập Request Bank ID.';
    if (controls.bankAccountName.invalid) return 'Vui lòng nhập Tên Tài khoản.';
    if (controls.bankAccountNumber.invalid) return 'Vui lòng nhập Số Tài Khoản.';
    if (controls.amount.invalid) return 'Số tiền phải từ 10,000 đến 10,000,000.';
    if (controls.bankNo.invalid) return 'Vui lòng chọn Ngân hàng.';
    if (controls.agentId.invalid) return 'Vui lòng chọn Đối tác.';
    return 'Vui lòng kiểm tra lại thông tin nhập.';
  }
}
