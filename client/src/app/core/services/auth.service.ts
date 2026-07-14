import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { map, tap } from 'rxjs/operators';
import { API_BASE_URL } from '../constants';
import {
  DisableTwoFactorRequest,
  EnableTwoFactorRequest,
  EnableTwoFactorResponse,
  LoginRequest,
  LoginResponse,
  TwoFactorSetupResponse,
  TwoFactorStatusResponse,
  UserInfo,
  VerifyTwoFactorRequest
} from '../models/api.models';

const TOKEN_KEY = 'bankout_token';
const USER_KEY = 'bankout_user';

function normalizeLoginResponse(raw: LoginResponse & Record<string, unknown>): LoginResponse {
  return {
    token: (raw.token ?? raw['Token'] ?? null) as string | null,
    expiresAt: (raw.expiresAt ?? raw['ExpiresAt'] ?? null) as string | null,
    userName: (raw.userName ?? raw['UserName'] ?? '') as string,
    fullName: (raw.fullName ?? raw['FullName'] ?? '') as string,
    roles: (raw.roles ?? raw['Roles'] ?? []) as string[],
    requiresTwoFactor: Boolean(raw.requiresTwoFactor ?? raw['RequiresTwoFactor']),
    pendingToken: (raw.pendingToken ?? raw['PendingToken'] ?? null) as string | null
  };
}

function normalizeTwoFactorStatus(raw: TwoFactorStatusResponse & Record<string, unknown>): TwoFactorStatusResponse {
  return {
    enabled: Boolean(raw.enabled ?? raw['Enabled'])
  };
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly currentUser = signal<UserInfo | null>(this.loadUser());

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  get user() {
    return this.currentUser.asReadonly();
  }

  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  isAuthenticated(): boolean {
    const token = this.token;
    if (!token) return false;
    const user = this.currentUser();
    if (!user) return false;
    return true;
  }

  isAdmin(): boolean {
    return this.currentUser()?.roles.includes('ADMIN') ?? false;
  }

  login(request: LoginRequest) {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/auth/login`, request).pipe(
      map((response) => normalizeLoginResponse(response as LoginResponse & Record<string, unknown>))
    );
  }

  verifyTwoFactor(request: VerifyTwoFactorRequest) {
    return this.http.post<LoginResponse>(`${API_BASE_URL}/auth/verify-2fa`, request).pipe(
      map((response) => normalizeLoginResponse(response as LoginResponse & Record<string, unknown>)),
      tap((response) => this.persistSession(response))
    );
  }

  completeLogin(response: LoginResponse) {
    this.persistSession(response);
  }

  getTwoFactorStatus() {
    return this.http.get<TwoFactorStatusResponse>(`${API_BASE_URL}/auth/2fa/status`).pipe(
      map((response) => normalizeTwoFactorStatus(response as TwoFactorStatusResponse & Record<string, unknown>))
    );
  }

  setupTwoFactor() {
    return this.http.post<TwoFactorSetupResponse>(`${API_BASE_URL}/auth/2fa/setup`, {});
  }

  enableTwoFactor(request: EnableTwoFactorRequest) {
    return this.http.post<EnableTwoFactorResponse>(`${API_BASE_URL}/auth/2fa/enable`, request);
  }

  disableTwoFactor(request: DisableTwoFactorRequest) {
    return this.http.post<{ message: string }>(`${API_BASE_URL}/auth/2fa/disable`, request);
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this.currentUser.set(null);
    this.router.navigate(['/login']);
  }

  loadProfile() {
    return this.http.get<UserInfo>(`${API_BASE_URL}/auth/me`).pipe(
      tap((user) => {
        localStorage.setItem(USER_KEY, JSON.stringify(user));
        this.currentUser.set(user);
      })
    );
  }

  private persistSession(response: LoginResponse): void {
    if (!response.token) return;

    localStorage.setItem(TOKEN_KEY, response.token);
    const user: UserInfo = {
      userName: response.userName,
      fullName: response.fullName,
      email: '',
      roles: response.roles
    };
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this.currentUser.set(user);
  }

  private loadUser(): UserInfo | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as UserInfo;
    } catch {
      return null;
    }
  }
}
