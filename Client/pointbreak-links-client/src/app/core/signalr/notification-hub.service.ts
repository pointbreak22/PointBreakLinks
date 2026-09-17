import { Injectable, inject, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../../environments/environment';
import { UserStore } from '../../stores/user.store';

// Thin wrapper around the single SignalR connection, connected/disconnected for the app's whole
// session by NotificationsBootstrapService. Consumers: that bootstrap service (toasts + the
// header notification bell) and shared/message-chat-modal (live-appends an incoming message when
// its own order is open) — both call `.on()` on the same underlying connection, which SignalR
// supports (multiple handlers per event name), so this stays a single shared connection rather
// than one per consumer.
@Injectable({ providedIn: 'root' })
export class NotificationHubService {
  private readonly userStore = inject(UserStore);
  private connection: HubConnection | null = null;

  readonly connectionState = signal<HubConnectionState>(HubConnectionState.Disconnected);

  async connect(): Promise<void> {
    if (this.connection?.state === HubConnectionState.Connected) return;

    // Access token as a query param, not an Authorization header — the browser's
    // WebSocket/SSE transports can't set custom headers. JwtBearerEvents.OnMessageReceived
    // in Program.cs reads it back off the query string for requests under /hubs.
    this.connection = new HubConnectionBuilder()
      .withUrl(environment.notificationsHubUrl, {
        accessTokenFactory: () => this.userStore.accessToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();

    this.connection.onreconnecting(() => this.connectionState.set(HubConnectionState.Reconnecting));
    this.connection.onreconnected(() => this.connectionState.set(HubConnectionState.Connected));
    this.connection.onclose(() => this.connectionState.set(HubConnectionState.Disconnected));

    await this.connection.start();
    this.connectionState.set(HubConnectionState.Connected);
  }

  async disconnect(): Promise<void> {
    await this.connection?.stop();
    this.connection = null;
    this.connectionState.set(HubConnectionState.Disconnected);
  }

  on<T>(methodName: string, callback: (payload: T) => void): void {
    this.connection?.on(methodName, callback);
  }

  // SignalR's `.on()` stacks handlers rather than replacing them — a component that
  // subscribes on init (e.g. MessageChatModal, opened/closed repeatedly against this same
  // long-lived connection) must unsubscribe its exact callback on destroy, or a message ends up
  // appended once per past time the modal was opened.
  off<T>(methodName: string, callback: (payload: T) => void): void {
    this.connection?.off(methodName, callback);
  }
}
