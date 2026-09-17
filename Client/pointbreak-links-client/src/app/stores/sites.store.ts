import { Injectable, inject, signal } from '@angular/core';
import { SitesApiService } from '../services/sites-api.service';
import { PageMeta } from '../core/models/pagination.model';
import {
  PurchasedSiteDto,
  RequestPublicationPayload,
  SiteCatalogFilter,
  SiteDto,
  SiteFormValue,
} from '../core/models/site.model';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 10, total: 0 };

// Ported from FOXLinks' stores/sites.ts (Pinia) — seller side (my-platforms/my-sales) and
// buyer side (optimizator catalog + purchase).
@Injectable({ providedIn: 'root' })
export class SitesStore {
  private readonly api = inject(SitesApiService);

  private readonly _sites = signal<SiteDto[]>([]);
  private readonly _catalogSites = signal<SiteDto[]>([]);
  private readonly _purchasedSites = signal<PurchasedSiteDto[]>([]);
  private readonly _meta = signal<PageMeta>(emptyMeta);
  private readonly _currentSite = signal<SiteDto | null>(null);
  private readonly _loading = signal(false);

  readonly sites = this._sites.asReadonly();
  readonly catalogSites = this._catalogSites.asReadonly();
  readonly purchasedSites = this._purchasedSites.asReadonly();
  readonly meta = this._meta.asReadonly();
  readonly currentSite = this._currentSite.asReadonly();
  readonly loading = this._loading.asReadonly();

  async fetchMySites(page = 1): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getMySites(page);
      this._sites.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }

  async fetchSite(id: number): Promise<void> {
    this._loading.set(true);
    try {
      this._currentSite.set(await this.api.getSite(id));
    } finally {
      this._loading.set(false);
    }
  }

  clearCurrentSite(): void {
    this._currentSite.set(null);
  }

  async createSite(payload: SiteFormValue): Promise<void> {
    await this.api.createSite(payload);
    await this.fetchMySites();
  }

  async updateSite(id: number, payload: SiteFormValue): Promise<void> {
    await this.api.updateSite(id, payload);
    await this.fetchMySites();
  }

  async deactivateSite(id: number): Promise<void> {
    this._loading.set(true);
    try {
      await this.api.deactivateSite(id);
      await this.fetchMySites();
    } finally {
      this._loading.set(false);
    }
  }

  async reactivateSite(id: number): Promise<void> {
    this._loading.set(true);
    try {
      await this.api.reactivateSite(id);
      await this.fetchMySites();
    } finally {
      this._loading.set(false);
    }
  }

  async fetchSales(page = 1): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getWebmasterSales(page);
      this._purchasedSites.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }

  async acceptOrder(id: number): Promise<void> {
    this._loading.set(true);
    try {
      const updated = await this.api.acceptOrder(id);
      this._purchasedSites.update((orders) => orders.map((o) => (o.id === id ? updated : o)));
    } finally {
      this._loading.set(false);
    }
  }

  async declineOrder(id: number): Promise<void> {
    this._loading.set(true);
    try {
      const updated = await this.api.declineOrder(id);
      this._purchasedSites.update((orders) => orders.map((o) => (o.id === id ? updated : o)));
    } finally {
      this._loading.set(false);
    }
  }

  async cancelOrder(id: number): Promise<void> {
    this._loading.set(true);
    try {
      const updated = await this.api.cancelOrder(id);
      this._purchasedSites.update((orders) => orders.map((o) => (o.id === id ? updated : o)));
    } finally {
      this._loading.set(false);
    }
  }

  async openDispute(id: number, reason: string): Promise<void> {
    this._loading.set(true);
    try {
      const updated = await this.api.openDispute(id, reason);
      this._purchasedSites.update((orders) => orders.map((o) => (o.id === id ? updated : o)));
    } finally {
      this._loading.set(false);
    }
  }

  async confirmPublished(id: number): Promise<void> {
    this._loading.set(true);
    try {
      await this.api.confirmPublished(id);
      await this.fetchSales(this._meta().currentPage);
    } finally {
      this._loading.set(false);
    }
  }

  async fetchSitesByProject(projectId: number, page = 1): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getSitesByProject(projectId, page, this._meta().perPage);
      this._purchasedSites.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }

  async fetchCatalog(page = 1, filter?: SiteCatalogFilter): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getCatalog(page, this._meta().perPage, filter);
      this._catalogSites.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }

  requestPublication(siteId: number, payload: RequestPublicationPayload): Promise<PurchasedSiteDto> {
    return this.api.requestPublication(siteId, payload);
  }

  async verifySite(id: number): Promise<boolean> {
    const verified = await this.api.verifySite(id);
    await this.fetchMySites(this._meta().currentPage);
    return verified;
  }
}
