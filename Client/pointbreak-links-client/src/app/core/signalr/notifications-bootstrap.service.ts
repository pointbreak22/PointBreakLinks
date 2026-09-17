import { Injectable, effect, inject } from '@angular/core';
import { NotificationHubService } from './notification-hub.service';
import { ToastService } from '../notifications/toast.service';
import { NotificationsStore } from '../../stores/notifications.store';
import { MessagesStore } from '../../stores/messages.store';
import { SupportStore } from '../../stores/support.store';
import { UserStore } from '../../stores/user.store';
import { MessageDto } from '../../core/models/message.model';
import { SupportMessageDto } from '../../core/models/support.model';

interface OrderReceivedPayload {
  siteUrl: string;
}

interface OrderAcceptedPayload {
  siteUrl: string;
}

type NewMessagePayload = MessageDto & { senderName: string };

interface SiteModeratedPayload {
  siteUrl: string;
  approved: boolean;
}

// Connects NotificationHubService the moment a session exists and surfaces its pushes as
// toasts — new order, order accepted, new chat message, site approved/rejected (see
// WebAPI/Services/SignalRNotificationPusher.cs for the matching server-side sends). Injected
// once from app.config.ts's initializer purely to instantiate it; the effect below then drives
// connect/disconnect for the rest of the app's lifetime as the session comes and goes.
@Injectable({ providedIn: 'root' })
export class NotificationsBootstrapService {
  private readonly hub = inject(NotificationHubService);
  private readonly toastService = inject(ToastService);
  private readonly notificationsStore = inject(NotificationsStore);
  private readonly messagesStore = inject(MessagesStore);
  private readonly supportStore = inject(SupportStore);
  private readonly userStore = inject(UserStore);

  constructor() {
    effect(() => {
      if (this.userStore.isAuthenticated()) {
        void this.connectAndListen();
      } else {
        void this.hub.disconnect();
        this.notificationsStore.clear();
        this.messagesStore.clear();
      }
    });
  }

  private async connectAndListen(): Promise<void> {
    await this.hub.connect();
    void this.notificationsStore.refreshUnreadCount();
    void this.messagesStore.refreshUnreadCount();

    // Each push here has a matching row persisted server-side (see
    // WebAPI/Controllers/NotificationsController.cs's handlers) — re-reading the count rather
    // than incrementing locally keeps it correct even if this tab missed an earlier push.
    this.hub.on<OrderReceivedPayload>('OrderReceived', (p) => {
      this.toastService.notify(`Новый заказ на площадку ${p.siteUrl}`, 'success');
      void this.notificationsStore.refreshUnreadCount();
    });
    this.hub.on<OrderAcceptedPayload>('OrderAccepted', (p) => {
      this.toastService.notify(`Ваш заказ на ${p.siteUrl} принят в работу`, 'success');
      void this.notificationsStore.refreshUnreadCount();
    });
    this.hub.on<NewMessagePayload>('NewMessage', (p) => {
      this.toastService.notify(`Новое сообщение от ${p.senderName}`, 'info');
      void this.notificationsStore.refreshUnreadCount();
      this.messagesStore.handleIncoming(p);
    });
    this.hub.on<SiteModeratedPayload>('SiteModerated', (p) => {
      this.toastService.notify(
        p.approved ? `Площадка ${p.siteUrl} одобрена` : `Площадка ${p.siteUrl} отклонена`,
        p.approved ? 'success' : 'error',
      );
      void this.notificationsStore.refreshUnreadCount();
    });
    this.hub.on<SupportMessageDto>('NewSupportMessage', (p) => {
      this.toastService.notify('Новый ответ от службы поддержки', 'info');
      void this.notificationsStore.refreshUnreadCount();
      this.supportStore.handleIncomingReply(p);
    });
  }
}
