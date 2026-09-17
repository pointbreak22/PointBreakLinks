import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import {
  AdminDashboardDto,
  AdminProjectDto,
  AdminSystemLogDto,
  AdminTransactionDto,
  AdminUserDto,
  AdminWithdrawalRequestDto,
  DisputedOrderDto,
} from '../core/models/admin.model';
import { PurchasedSiteDto } from '../core/models/site.model';
import { PagedResult } from '../core/models/pagination.model';

// Thin HTTP wrapper, no state — state lives in stores/admin.store.ts. New module, not a port
// (see AdminUserDto's comment for why FOXLinks' admin-users.vue isn't the source of truth here).
@Injectable({ providedIn: 'root' })
export class AdminApiService {
  private readonly http = inject(HttpClient);

  getDashboard(): Promise<AdminDashboardDto> {
    return firstValueFrom(this.http.get<AdminDashboardDto>(ApiEndpoints.admin.dashboard));
  }

  getUsers(page: number, perPage = 15, search?: string, role?: string): Promise<PagedResult<AdminUserDto>> {
    let params = new HttpParams().set('page', page).set('perPage', perPage);
    if (search) params = params.set('search', search);
    if (role) params = params.set('role', role);
    return firstValueFrom(this.http.get<PagedResult<AdminUserDto>>(ApiEndpoints.admin.users, { params }));
  }

  async setBanned(id: number, isBanned: boolean): Promise<AdminUserDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: AdminUserDto }>(ApiEndpoints.admin.banUser(id), { isBanned }),
    );
    return response.data;
  }

  async setRole(id: number, role: string): Promise<AdminUserDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: AdminUserDto }>(ApiEndpoints.admin.setUserRole(id), { role }),
    );
    return response.data;
  }

  async unlockUser(id: number): Promise<AdminUserDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: AdminUserDto }>(ApiEndpoints.admin.unlockUser(id), {}),
    );
    return response.data;
  }

  getProjects(page: number, perPage = 15): Promise<PagedResult<AdminProjectDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<AdminProjectDto>>(ApiEndpoints.admin.projects, { params }));
  }

  getTransactions(page: number, perPage = 15): Promise<PagedResult<AdminTransactionDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<AdminTransactionDto>>(ApiEndpoints.admin.transactions, { params }));
  }

  getSystemLogs(page: number, perPage = 20): Promise<PagedResult<AdminSystemLogDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<AdminSystemLogDto>>(ApiEndpoints.admin.systemLogs, { params }));
  }

  getDisputes(page: number, perPage = 15): Promise<PagedResult<DisputedOrderDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<DisputedOrderDto>>(ApiEndpoints.admin.disputes, { params }));
  }

  async resolveDispute(id: number, refundBuyer: boolean): Promise<PurchasedSiteDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: PurchasedSiteDto }>(ApiEndpoints.admin.resolveDispute(id), { refundBuyer }),
    );
    return response.data;
  }

  getWithdrawals(page: number, perPage = 15): Promise<PagedResult<AdminWithdrawalRequestDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<AdminWithdrawalRequestDto>>(ApiEndpoints.admin.withdrawals, { params }));
  }

  approveWithdrawal(id: number): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.admin.approveWithdrawal(id), {}));
  }

  rejectWithdrawal(id: number, comment: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.admin.rejectWithdrawal(id), { comment }));
  }

  exportOrders(): Promise<Blob> {
    return firstValueFrom(this.http.get(ApiEndpoints.admin.reports.orders, { responseType: 'blob' }));
  }

  exportTransactions(): Promise<Blob> {
    return firstValueFrom(this.http.get(ApiEndpoints.admin.reports.transactions, { responseType: 'blob' }));
  }

  exportUsers(): Promise<Blob> {
    return firstValueFrom(this.http.get(ApiEndpoints.admin.reports.users, { responseType: 'blob' }));
  }

  exportWithdrawals(): Promise<Blob> {
    return firstValueFrom(this.http.get(ApiEndpoints.admin.reports.withdrawals, { responseType: 'blob' }));
  }
}
