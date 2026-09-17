import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { PagedResult } from '../core/models/pagination.model';
import {
  CountryDto,
  PurchasedSiteDto,
  PurchasedSiteEventDto,
  RequestPublicationPayload,
  SiteCatalogFilter,
  SiteDto,
  SiteFormValue,
  TopicDto,
} from '../core/models/site.model';

// Thin HTTP wrapper, no state — state lives in stores/sites.store.ts. Ported from FOXLinks'
// services/site.service.ts.
@Injectable({ providedIn: 'root' })
export class SitesApiService {
  private readonly http = inject(HttpClient);

  getMySites(page: number, perPage = 10): Promise<PagedResult<SiteDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<SiteDto>>(ApiEndpoints.sites.mySites, { params }));
  }

  async getSite(id: number): Promise<SiteDto> {
    const response = await firstValueFrom(this.http.get<{ data: SiteDto }>(ApiEndpoints.sites.byId(id)));
    return response.data;
  }

  async createSite(payload: SiteFormValue): Promise<SiteDto> {
    const response = await firstValueFrom(this.http.post<{ data: SiteDto }>(ApiEndpoints.sites.mySites, payload));
    return response.data;
  }

  async updateSite(id: number, payload: SiteFormValue): Promise<SiteDto> {
    const response = await firstValueFrom(this.http.put<{ data: SiteDto }>(ApiEndpoints.sites.byId(id), payload));
    return response.data;
  }

  deactivateSite(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(ApiEndpoints.sites.byId(id)));
  }

  reactivateSite(id: number): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.sites.reactivate(id), {}));
  }

  uploadScreenshot(id: number, file: File): Promise<void> {
    const formData = new FormData();
    formData.append('file', file);
    return firstValueFrom(this.http.post<void>(ApiEndpoints.sites.screenshot(id), formData));
  }

  deleteScreenshot(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(ApiEndpoints.sites.screenshot(id)));
  }

  getTopics(): Promise<TopicDto[]> {
    return firstValueFrom(this.http.get<TopicDto[]>(ApiEndpoints.sites.topics));
  }

  getWebmasterSales(page: number, perPage = 10): Promise<PagedResult<PurchasedSiteDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<PurchasedSiteDto>>(ApiEndpoints.sites.webmasterSales, { params }));
  }

  async acceptOrder(id: number): Promise<PurchasedSiteDto> {
    const response = await firstValueFrom(this.http.post<{ data: PurchasedSiteDto }>(ApiEndpoints.sites.acceptOrder(id), {}));
    return response.data;
  }

  async declineOrder(id: number): Promise<PurchasedSiteDto> {
    const response = await firstValueFrom(this.http.post<{ data: PurchasedSiteDto }>(ApiEndpoints.sites.declineOrder(id), {}));
    return response.data;
  }

  async cancelOrder(id: number): Promise<PurchasedSiteDto> {
    const response = await firstValueFrom(this.http.post<{ data: PurchasedSiteDto }>(ApiEndpoints.sites.cancelOrder(id), {}));
    return response.data;
  }

  async openDispute(id: number, reason: string): Promise<PurchasedSiteDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: PurchasedSiteDto }>(ApiEndpoints.sites.openDispute(id), { reason }),
    );
    return response.data;
  }

  confirmPublished(purchasedSiteId: number): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.sites.confirmPublished(purchasedSiteId), {}));
  }

  getSitesByProject(projectId: number, page: number, perPage = 15): Promise<PagedResult<PurchasedSiteDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<PurchasedSiteDto>>(ApiEndpoints.sites.byProject(projectId), { params }));
  }

  getCatalog(page: number, perPage = 15, filter?: SiteCatalogFilter): Promise<PagedResult<SiteDto>> {
    let params = new HttpParams().set('page', page).set('perPage', perPage);
    if (filter) {
      for (const [key, value] of Object.entries(filter)) {
        if (value !== undefined && value !== null) {
          params = params.set(key, value);
        }
      }
    }
    return firstValueFrom(this.http.get<PagedResult<SiteDto>>(ApiEndpoints.sites.catalog, { params }));
  }

  async requestPublication(siteId: number, payload: RequestPublicationPayload): Promise<PurchasedSiteDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: PurchasedSiteDto }>(ApiEndpoints.sites.requestPublication(siteId), payload),
    );
    return response.data;
  }

  getCountries(): Promise<CountryDto[]> {
    return firstValueFrom(this.http.get<CountryDto[]>(ApiEndpoints.sites.countries));
  }

  async verifySite(id: number): Promise<boolean> {
    const response = await firstValueFrom(
      this.http.post<{ data: { verified: boolean } }>(ApiEndpoints.sites.verify(id), {}),
    );
    return response.data.verified;
  }

  getPurchasedSiteEvents(purchasedSiteId: number): Promise<PurchasedSiteEventDto[]> {
    return firstValueFrom(this.http.get<PurchasedSiteEventDto[]>(ApiEndpoints.sites.events(purchasedSiteId)));
  }

  exportWebmasterSales(): Promise<Blob> {
    return firstValueFrom(this.http.get(ApiEndpoints.sites.webmasterSalesExport, { responseType: 'blob' }));
  }

  exportProjectSites(projectId: number): Promise<Blob> {
    return firstValueFrom(this.http.get(ApiEndpoints.sites.byProjectExport(projectId), { responseType: 'blob' }));
  }
}
