import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { API_BASE_URL } from '../constants';
import {
  AgentOption,
  BankoutListItem,
  CreateBankoutRequest,
  PagedResponse
} from '../models/api.models';

export interface BankoutFilter {
  userName?: string;
  requestBankId?: string;
  status?: number | null;
  fromDate?: string;
  toDate?: string;
  page?: number;
  pageSize?: number;
}

@Injectable({ providedIn: 'root' })
export class BankoutService {
  constructor(private readonly http: HttpClient) {}

  create(request: CreateBankoutRequest) {
    return this.http.post<BankoutListItem>(`${API_BASE_URL}/bankout`, request);
  }

  getList(filter: BankoutFilter) {
    let params = new HttpParams()
      .set('page', (filter.page ?? 1).toString())
      .set('pageSize', (filter.pageSize ?? 10).toString());

    if (filter.userName) params = params.set('userName', filter.userName);
    if (filter.requestBankId) params = params.set('requestBankId', filter.requestBankId);
    if (filter.status != null) params = params.set('status', filter.status.toString());
    if (filter.fromDate) params = params.set('fromDate', filter.fromDate);
    if (filter.toDate) params = params.set('toDate', filter.toDate);

    return this.http.get<PagedResponse<BankoutListItem>>(`${API_BASE_URL}/bankout`, { params });
  }

  getAgentOptions() {
    return this.http.get<AgentOption[]>(`${API_BASE_URL}/bankout/agents`);
  }

  approve(id: string) {
    return this.http.post<BankoutListItem>(`${API_BASE_URL}/bankout/${id}/approve`, {});
  }

  cancel(id: string) {
    return this.http.post<BankoutListItem>(`${API_BASE_URL}/bankout/${id}/cancel`, {});
  }
}
