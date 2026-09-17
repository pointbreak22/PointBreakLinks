import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { NotificationDto, NotificationPreferenceDto } from '../core/models/notification.model';
import { PagedResult } from '../core/models/pagination.model';

@Injectable({ providedIn: 'root' })
export class NotificationsApiService {
  private readonly http = inject(HttpClient);

  getMyNotifications(page = 1, perPage = 20): Promise<PagedResult<NotificationDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<NotificationDto>>(ApiEndpoints.notifications.mine, { params }));
  }

  getUnreadCount(): Promise<number> {
    return firstValueFrom(this.http.get<number>(ApiEndpoints.notifications.unreadCount));
  }

  markAllRead(): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.notifications.markAllRead, {}));
  }

  getPreferences(): Promise<NotificationPreferenceDto> {
    return firstValueFrom(this.http.get<NotificationPreferenceDto>(ApiEndpoints.notifications.preferences));
  }

  updatePreferences(preferences: NotificationPreferenceDto): Promise<NotificationPreferenceDto> {
    return firstValueFrom(this.http.put<NotificationPreferenceDto>(ApiEndpoints.notifications.preferences, preferences));
  }
}
