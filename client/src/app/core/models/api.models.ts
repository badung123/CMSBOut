export interface LoginRequest {
  userName: string;
  password: string;
}

export interface LoginResponse {
  token: string | null;
  expiresAt: string | null;
  userName: string;
  fullName: string;
  roles: string[];
  requiresTwoFactor?: boolean;
  pendingToken?: string | null;
}

export interface VerifyTwoFactorRequest {
  pendingToken: string;
  code: string;
}

export interface UserInfo {
  userName: string;
  fullName: string;
  email: string;
  roles: string[];
  twoFactorEnabled?: boolean;
}

export interface TwoFactorStatusResponse {
  enabled: boolean;
}

export interface TwoFactorSetupResponse {
  sharedKey: string;
  authenticatorUri: string;
}

export interface EnableTwoFactorRequest {
  code: string;
}

export interface EnableTwoFactorResponse {
  recoveryCodes: string[];
}

export interface DisableTwoFactorRequest {
  password: string;
}

export interface DashboardData {
  balance: number;
  totalTransactions: number;
}

export interface CreateBankoutRequest {
  requestBankId?: string | null;
  bankAccountName: string;
  bankAccountNumber: string;
  amount: number;
  bankNo: string;
  agentId: number;
}

export interface BankoutListItem {
  id: string;
  bankAccountName: string;
  bankAccountNumber: string;
  amount: number;
  bankNo: string;
  bankName: string;
  shortBankName: string;
  agentName: string;
  requestBankId: string | null;
  createdDate: string;
  bankDate: string | null;
  log: string | null;
  status: number;
}

export interface PartnerBankItem {
  bankNo: string;
  bankName: string;
  shortBankName: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AgentOption {
  id: number;
  agentName: string;
}

export interface Agent {
  id: number;
  agentName: string;
  createdDate: string;
}
