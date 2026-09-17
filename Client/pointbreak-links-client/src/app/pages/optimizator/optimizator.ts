import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Header } from '../../shared/layout/header/header';
import { Sidebar } from '../../shared/layout/sidebar/sidebar';
import { SmartGrid } from '../../shared/smart-grid/smart-grid';
import { Pagination } from '../../shared/pagination/pagination';
import { CountryDto, SavedSearchDto, SiteCatalogFilter, SiteDto, TopicDto } from '../../core/models/site.model';
import { ApiEndpoints } from '../../core/http/api-endpoints';
import { SitesApiService } from '../../services/sites-api.service';
import { SitesStore } from '../../stores/sites.store';
import { FavoritesStore } from '../../stores/favorites.store';
import { SavedSearchesStore } from '../../stores/saved-searches.store';
import { ToastService } from '../../core/notifications/toast.service';
import { extractErrorMessage } from '../../core/http/api-error';
import { BuyMiralinksModal } from './buy-miralinks-modal/buy-miralinks-modal';
import { SiteReviewsModal } from '../../shared/site-reviews-modal/site-reviews-modal';

// Ported from FOXLinks' pages/optimizator.vue. The mass-selection toolbar, the
// "Фильтрация"/"Расширенный поиск" panels and the "Новый заказ" button (commented out in the
// source) are all dead decorative UI — their handlers either show a fake spinner via
// setTimeout with no network call, or don't exist at all. Not ported; see handleSearchInput in
// the source for the one search feature that's real, reimplemented below as a computed filter
// instead of source's raw DOM textContent matching. The filter panel here (topic/country/price/
// ИКС/DR) and the favorite-star column are real, new features — the source's equivalents were
// exactly as decorative as the rest (see pages/_NEXT.md).
@Component({
  selector: 'app-optimizator',
  imports: [DecimalPipe, RouterLink, Header, Sidebar, SmartGrid, Pagination, BuyMiralinksModal, SiteReviewsModal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './optimizator.html',
})
export class Optimizator implements OnInit {
  protected readonly sitesStore = inject(SitesStore);
  protected readonly favoritesStore = inject(FavoritesStore);
  protected readonly savedSearchesStore = inject(SavedSearchesStore);
  private readonly sitesApi = inject(SitesApiService);
  private readonly toastService = inject(ToastService);

  protected readonly searchTerm = signal('');
  protected readonly screenshotUrl = (id: number) => ApiEndpoints.sites.screenshot(id);
  protected readonly buyingSite = signal<SiteDto | null>(null);
  protected readonly reviewsSite = signal<SiteDto | null>(null);
  protected readonly topics = signal<TopicDto[]>([]);
  protected readonly countries = signal<CountryDto[]>([]);
  protected readonly filtersOpen = signal(false);

  protected readonly filterTopicId = signal<number | null>(null);
  protected readonly filterCountryId = signal<number | null>(null);
  protected readonly filterMinPrice = signal<number | null>(null);
  protected readonly filterMaxPrice = signal<number | null>(null);
  protected readonly filterMinIks = signal<number | null>(null);
  protected readonly filterMinDr = signal<number | null>(null);
  // Combined sort dropdown value, e.g. "price_asc" — split into SiteCatalogFilter's
  // sortBy/sortDescending pair in currentFilter() rather than tracking two separate signals for
  // one <select>.
  protected readonly sortOption = signal('');

  protected readonly filteredSites = computed(() => {
    const term = this.searchTerm().trim().toLowerCase();
    if (!term) return this.sitesStore.catalogSites();

    return this.sitesStore.catalogSites().filter((site) =>
      [site.url, site.topic, site.description ?? ''].some((field) => field.toLowerCase().includes(term)),
    );
  });

  ngOnInit(): void {
    void this.sitesStore.fetchCatalog();
    void this.favoritesStore.fetchFavoriteIds();
    void this.savedSearchesStore.fetch();
    void this.sitesApi.getTopics().then((topics) => this.topics.set(topics));
    void this.sitesApi.getCountries().then((countries) => this.countries.set(countries));
  }

  changePage(page: number): void {
    void this.sitesStore.fetchCatalog(page, this.currentFilter());
  }

  private currentFilter(): SiteCatalogFilter {
    const [sortBy, sortDirection] = this.sortOption() ? this.sortOption().split('_') : [undefined, undefined];
    return {
      topicId: this.filterTopicId() ?? undefined,
      countryId: this.filterCountryId() ?? undefined,
      minPrice: this.filterMinPrice() ?? undefined,
      maxPrice: this.filterMaxPrice() ?? undefined,
      minIks: this.filterMinIks() ?? undefined,
      minDr: this.filterMinDr() ?? undefined,
      sortBy: sortBy as SiteCatalogFilter['sortBy'],
      sortDescending: sortDirection === 'desc',
    };
  }

  applyFilters(): void {
    void this.sitesStore.fetchCatalog(1, this.currentFilter());
  }

  onSortChange(value: string): void {
    this.sortOption.set(value);
    void this.sitesStore.fetchCatalog(1, this.currentFilter());
  }

  resetFilters(): void {
    this.filterTopicId.set(null);
    this.filterCountryId.set(null);
    this.filterMinPrice.set(null);
    this.filterMaxPrice.set(null);
    this.filterMinIks.set(null);
    this.filterMinDr.set(null);
    this.sortOption.set('');
    void this.sitesStore.fetchCatalog(1);
  }

  toggleFilters(): void {
    this.filtersOpen.update((v) => !v);
  }

  // Persists the currently-set filter fields as an alert — the buyer gets notified the moment
  // a newly approved listing satisfies them (ApproveSiteCommandHandler), without needing to
  // keep re-checking the catalog by hand.
  async saveSearch(): Promise<void> {
    try {
      await this.savedSearchesStore.create({
        topicId: this.filterTopicId(),
        countryId: this.filterCountryId(),
        minPrice: this.filterMinPrice(),
        maxPrice: this.filterMaxPrice(),
        minIks: this.filterMinIks(),
        minDr: this.filterMinDr(),
      });
      this.toastService.notify('Поиск сохранён — сообщим, когда появится подходящая площадка', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось сохранить поиск'), 'error');
    }
  }

  applySavedSearch(search: SavedSearchDto): void {
    this.filterTopicId.set(search.topicId);
    this.filterCountryId.set(search.countryId);
    this.filterMinPrice.set(search.minPrice);
    this.filterMaxPrice.set(search.maxPrice);
    this.filterMinIks.set(search.minIks);
    this.filterMinDr.set(search.minDr);
    this.filtersOpen.set(true);
    this.applyFilters();
  }

  async removeSavedSearch(id: number, event: Event): Promise<void> {
    event.stopPropagation();
    try {
      await this.savedSearchesStore.remove(id);
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось удалить поиск'), 'error');
    }
  }

  savedSearchLabel(search: SavedSearchDto): string {
    const parts: string[] = [];
    if (search.topicName) parts.push(search.topicName);
    if (search.countryName) parts.push(search.countryName);
    if (search.minPrice != null) parts.push(`от ${search.minPrice} ₽`);
    if (search.maxPrice != null) parts.push(`до ${search.maxPrice} ₽`);
    if (search.minIks != null) parts.push(`ИКС ≥ ${search.minIks}`);
    if (search.minDr != null) parts.push(`DR ≥ ${search.minDr}`);
    return parts.length > 0 ? parts.join(' · ') : 'Любая площадка';
  }

  onTopicFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.filterTopicId.set(value ? Number(value) : null);
  }

  onCountryFilterChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.filterCountryId.set(value ? Number(value) : null);
  }

  onMinPriceChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.filterMinPrice.set(value ? Number(value) : null);
  }

  onMaxPriceChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.filterMaxPrice.set(value ? Number(value) : null);
  }

  onMinIksChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.filterMinIks.set(value ? Number(value) : null);
  }

  onMinDrChange(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.filterMinDr.set(value ? Number(value) : null);
  }

  formatTraffic(value: number): string {
    return value >= 1000 ? (value / 1000).toFixed(1) + 'K' : value.toString();
  }

  onSearchInput(event: Event): void {
    this.searchTerm.set((event.target as HTMLInputElement).value);
  }

  isFavorite(siteId: number): boolean {
    return this.favoritesStore.favoriteIds().has(siteId);
  }

  async toggleFavorite(siteId: number): Promise<void> {
    try {
      await this.favoritesStore.toggleFavorite(siteId);
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось обновить избранное'), 'error');
    }
  }

  openBuy(site: SiteDto): void {
    this.buyingSite.set(site);
  }

  closeBuy(): void {
    this.buyingSite.set(null);
  }

  onPurchased(): void {
    void this.sitesStore.fetchCatalog(this.sitesStore.meta().currentPage, this.currentFilter());
  }

  openReviews(site: SiteDto): void {
    this.reviewsSite.set(site);
  }

  closeReviews(): void {
    this.reviewsSite.set(null);
  }
}
