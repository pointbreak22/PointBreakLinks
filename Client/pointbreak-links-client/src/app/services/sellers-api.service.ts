import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { SellerProfileDto } from '../core/models/site.model';

// Thin HTTP wrapper, no state — new module, not a port.
@Injectable({ providedIn: 'root' })
export class SellersApiService {
  private readonly http = inject(HttpClient);

  getProfile(id: number): Promise<SellerProfileDto> {
    return firstValueFrom(this.http.get<SellerProfileDto>(ApiEndpoints.sellers.profile(id)));
  }
}
