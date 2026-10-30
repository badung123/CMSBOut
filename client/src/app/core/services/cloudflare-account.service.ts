import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { API_BASE_URL } from '../constants';
import { CloudflareAccount, PurgeCacheResult, Redirect301Result, SaveCloudflareAccountRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class CloudflareAccountService {
  constructor(private readonly http: HttpClient) {}

  getAll() {
    return this.http.get<CloudflareAccount[]>(`${API_BASE_URL}/cloudflareaccounts`);
  }

  create(request: SaveCloudflareAccountRequest) {
    return this.http.post<CloudflareAccount>(`${API_BASE_URL}/cloudflareaccounts`, request);
  }

  update(id: string, request: SaveCloudflareAccountRequest) {
    return this.http.put<CloudflareAccount>(`${API_BASE_URL}/cloudflareaccounts/${id}`, request);
  }

  delete(id: string) {
    return this.http.delete<void>(`${API_BASE_URL}/cloudflareaccounts/${id}`);
  }

  apply301(domain: string, destinationDomain: string) {
    return this.http.post<Redirect301Result>(`${API_BASE_URL}/cloudflareaccounts/redirect-301`, {
      domain,
      destinationDomain
    });
  }

  purgeEverything(domain: string) {
    return this.http.post<PurgeCacheResult>(`${API_BASE_URL}/cloudflareaccounts/purge-cache`, { domain });
  }
}
