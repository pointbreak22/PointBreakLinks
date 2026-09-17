import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { BulkModerationResult, ModerationAuditEntryDto, PendingSiteDto } from '../core/models/moderation.model';
import { PagedResult } from '../core/models/pagination.model';

// Thin HTTP wrapper, no state — state lives in stores/moderation.store.ts. New module, not a
// port (see PendingSiteDto's comment: the `moderator` role had no page anywhere in FOXLinks).
@Injectable({ providedIn: 'root' })
export class ModerationApiService {
  private readonly http = inject(HttpClient);

  getPendingSites(page: number, perPage = 15): Promise<PagedResult<PendingSiteDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<PendingSiteDto>>(ApiEndpoints.moderation.pendingSites, { params }));
  }

  approveSite(id: number): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.moderation.approveSite(id), {}));
  }

  rejectSite(id: number, reason?: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.moderation.rejectSite(id), { reason }));
  }

  bulkApprove(siteIds: number[]): Promise<BulkModerationResult> {
    return firstValueFrom(this.http.post<BulkModerationResult>(ApiEndpoints.moderation.bulkApprove, { siteIds }));
  }

  bulkReject(siteIds: number[], reason?: string): Promise<BulkModerationResult> {
    return firstValueFrom(this.http.post<BulkModerationResult>(ApiEndpoints.moderation.bulkReject, { siteIds, reason }));
  }

  getAuditLog(page: number, perPage = 15): Promise<PagedResult<ModerationAuditEntryDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<ModerationAuditEntryDto>>(ApiEndpoints.moderation.auditLog, { params }));
  }
}
