import { Injectable, inject, signal } from '@angular/core';
import { NotificationsApiService } from '../services/notifications-api.service';
import { NotificationDto } from '../core/models/notification.model';

// Backs the header bell — unreadCount is refreshed on login and on every live SignalR push
// (NotificationsBootstrapService), independently of whether the dropdown is ever opened; the
// list itself is fetched lazily, only when the dropdown opens.
@Injectable({ providedIn: 'root' })
export class NotificationsStore {
  private readonly api = inject(NotificationsApiService);

  private readonly _notifications = signal<NotificationDto[]>([]);
  private readonly _unreadCount = signal(0);
  private readonly _loading = signal(false);

  readonly notifications = this._notifications.asReadonly();
  readonly unreadCount = this._unreadCount.asReadonly();
  readonly loading = this._loading.asReadonly();

  async refreshUnreadCount(): Promise<void> {
    this._unreadCount.set(await this.api.getUnreadCount());
  }

  async fetchNotifications(): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getMyNotifications();
      this._notifications.set(response.items);
    } finally {
      this._loading.set(false);
    }
  }

  async markAllRead(): Promise<void> {
    await this.api.markAllRead();
    this._notifications.update((items) => items.map((n) => ({ ...n, isRead: true })));
    this._unreadCount.set(0);
  }

  clear(): void {
    this._notifications.set([]);
    this._unreadCount.set(0);
  }
}
