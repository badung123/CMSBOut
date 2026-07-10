import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../constants';
import { Agent } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class AgentService {
  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<Agent[]>(`${API_BASE_URL}/agents`);
  }

  create(agentName: string) {
    return this.http.post<Agent>(`${API_BASE_URL}/agents`, { agentName });
  }

  update(id: number, agentName: string) {
    return this.http.put<Agent>(`${API_BASE_URL}/agents/${id}`, { agentName });
  }

  delete(id: number) {
    return this.http.delete<void>(`${API_BASE_URL}/agents/${id}`);
  }
}
