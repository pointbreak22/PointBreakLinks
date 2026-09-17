import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Header } from '../../shared/layout/header/header';
import { ApiEndpoints } from '../../core/http/api-endpoints';
import { SellersApiService } from '../../services/sellers-api.service';
import { SellerProfileDto, SiteDto } from '../../core/models/site.model';
import { BuyMiralinksModal } from '../optimizator/buy-miralinks-modal/buy-miralinks-modal';

// New page — SiteDto always carried a bare sellerId (userIdSales) with no name attached anywhere
// clickable, so a buyer had no way to see who they were dealing with beyond the catalog row.
@Component({
  selector: 'app-seller-profile',
  imports: [Header, DecimalPipe, BuyMiralinksModal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './seller-profile.html',
})
export class SellerProfile implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly sellersApi = inject(SellersApiService);

  protected readonly profile = signal<SellerProfileDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly buyingSite = signal<SiteDto | null>(null);
  protected readonly stars = [1, 2, 3, 4, 5];
  protected readonly screenshotUrl = (id: number) => ApiEndpoints.sites.screenshot(id);

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    void this.load(id);
  }

  private async load(id: number): Promise<void> {
    this.loading.set(true);
    try {
      this.profile.set(await this.sellersApi.getProfile(id));
    } catch {
      this.notFound.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  openBuy(site: SiteDto): void {
    this.buyingSite.set(site);
  }

  closeBuy(): void {
    this.buyingSite.set(null);
  }
}
