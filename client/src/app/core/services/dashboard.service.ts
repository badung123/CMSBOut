import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../constants';
import { DashboardData } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  constructor(private readonly http: HttpClient) {}

  getDashboard() {
    return this.http.get<DashboardData>(`${API_BASE_URL}/dashboard`);
  }

  addBalance(amount: number) {
    return this.http.post<{ balance: number }>(`${API_BASE_URL}/dashboard/balance/add`, { amount });
  }

  subtractBalance(amount: number) {
    return this.http.post<{ balance: number }>(`${API_BASE_URL}/dashboard/balance/subtract`, { amount });
  }
}
