import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Header } from '../../shared/layout/header/header';
import { Sidebar } from '../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../shared/pagination/pagination';
import { FavoritesStore } from '../../stores/favorites.store';
import { SiteDto } from '../../core/models/site.model';
import { ApiEndpoints } from '../../core/http/api-endpoints';
import { BuyMiralinksModal } from '../optimizator/buy-miralinks-modal/buy-miralinks-modal';

// New page — not a port. FOXLinks' "Избранные площадки" links (both on the webmaster and
// optimizator sidebars) all pointed at /coming-soon.
@Component({
  selector: 'app-favorites',
  imports: [Header, Sidebar, Pagination, DecimalPipe, BuyMiralinksModal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './favorites.html',
})
export class Favorites implements OnInit {
  protected readonly favoritesStore = inject(FavoritesStore);
  protected readonly buyingSite = signal<SiteDto | null>(null);
  protected readonly screenshotUrl = (id: number) => ApiEndpoints.sites.screenshot(id);

  ngOnInit(): void {
    void this.favoritesStore.fetchFavorites();
    void this.favoritesStore.fetchFavoriteIds();
  }

  changePage(page: number): void {
    void this.favoritesStore.fetchFavorites(page);
  }

  async removeFavorite(siteId: number): Promise<void> {
    await this.favoritesStore.toggleFavorite(siteId);
  }

  openBuy(site: SiteDto): void {
    this.buyingSite.set(site);
  }

  closeBuy(): void {
    this.buyingSite.set(null);
  }

  onPurchased(): void {
    void this.favoritesStore.fetchFavorites(this.favoritesStore.meta().currentPage);
  }
}
