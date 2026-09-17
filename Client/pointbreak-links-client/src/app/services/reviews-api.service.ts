import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { PagedResult } from '../core/models/pagination.model';
import { SiteReviewDto } from '../core/models/site.model';

// Thin HTTP wrapper, no state — new module, not a port.
@Injectable({ providedIn: 'root' })
export class ReviewsApiService {
  private readonly http = inject(HttpClient);

  getSiteReviews(siteId: number, page = 1, perPage = 10): Promise<PagedResult<SiteReviewDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<SiteReviewDto>>(ApiEndpoints.sites.reviews(siteId), { params }));
  }

  async createReview(purchasedSiteId: number, rating: number, comment: string | null): Promise<SiteReviewDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: SiteReviewDto }>(ApiEndpoints.reviews.create(purchasedSiteId), { rating, comment }),
    );
    return response.data;
  }

  async replyToReview(reviewId: number, reply: string): Promise<SiteReviewDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: SiteReviewDto }>(ApiEndpoints.reviews.reply(reviewId), { reply }),
    );
    return response.data;
  }
}
