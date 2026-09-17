import { Injectable, inject, signal } from '@angular/core';
import { SupportApiService } from '../services/support-api.service';
import { SupportMessageDto, SupportTicketSummaryDto } from '../core/models/support.model';
import { PageMeta } from '../core/models/pagination.model';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 15, total: 0 };

// Two independent halves, one store: the user's own persistent thread with support (mirrors
// wallet/notifications — fetched lazily when the /support page opens), and the staff ticket
// list + active-ticket thread (mirrors MessagesStore's conversations/thread shape almost
// exactly, since a support ticket is structurally the same "list of threads, one open at a
// time" as the messages inbox).
@Injectable({ providedIn: 'root' })
export class SupportStore {
  private readonly api = inject(SupportApiService);

  private readonly _myThread = signal<SupportMessageDto[]>([]);
  private readonly _myThreadLoading = signal(false);
  private readonly _mySending = signal(false);

  private readonly _tickets = signal<SupportTicketSummaryDto[]>([]);
  private readonly _ticketsMeta = signal<PageMeta>(emptyMeta);
  private readonly _ticketsLoading = signal(false);
  private readonly _activeTicket = signal<SupportTicketSummaryDto | null>(null);
  private readonly _ticketThread = signal<SupportMessageDto[]>([]);
  private readonly _ticketThreadLoading = signal(false);
  private readonly _replySending = signal(false);

  readonly myThread = this._myThread.asReadonly();
  readonly myThreadLoading = this._myThreadLoading.asReadonly();
  readonly mySending = this._mySending.asReadonly();

  readonly tickets = this._tickets.asReadonly();
  readonly ticketsMeta = this._ticketsMeta.asReadonly();
  readonly ticketsLoading = this._ticketsLoading.asReadonly();
  readonly activeTicket = this._activeTicket.asReadonly();
  readonly ticketThread = this._ticketThread.asReadonly();
  readonly ticketThreadLoading = this._ticketThreadLoading.asReadonly();
  readonly replySending = this._replySending.asReadonly();

  async fetchMyThread(): Promise<void> {
    this._myThreadLoading.set(true);
    try {
      this._myThread.set(await this.api.getMyThread());
    } finally {
      this._myThreadLoading.set(false);
    }
  }

  async sendMyMessage(text: string): Promise<void> {
    if (this._mySending()) return;
    this._mySending.set(true);
    try {
      const message = await this.api.sendMyMessage(text);
      this._myThread.update((current) => [...current, message]);
    } finally {
      this._mySending.set(false);
    }
  }

  // Called by NotificationsBootstrapService's shared hub handler for 'NewSupportMessage' — only
  // ever a staff reply (see INotificationPusher.NotifyNewSupportMessageAsync's comment), so it
  // always belongs on the end of the user's own thread if that page happens to be open.
  handleIncomingReply(message: SupportMessageDto): void {
    this._myThread.update((current) => (current.some((m) => m.id === message.id) ? current : [...current, message]));
  }

  async fetchTickets(page = 1): Promise<void> {
    this._ticketsLoading.set(true);
    try {
      const response = await this.api.getTickets(page, this._ticketsMeta().perPage);
      this._tickets.set(response.items);
      this._ticketsMeta.set(response);
    } finally {
      this._ticketsLoading.set(false);
    }
  }

  async openTicket(ticket: SupportTicketSummaryDto): Promise<void> {
    this._activeTicket.set(ticket);
    this._ticketThreadLoading.set(true);
    try {
      this._ticketThread.set(await this.api.getTicketMessages(ticket.id));
      this._tickets.update((items) => items.map((t) => (t.id === ticket.id ? { ...t, unreadCount: 0 } : t)));
    } finally {
      this._ticketThreadLoading.set(false);
    }
  }

  closeTicket(): void {
    this._activeTicket.set(null);
    this._ticketThread.set([]);
  }

  async sendReply(text: string): Promise<void> {
    const ticket = this._activeTicket();
    if (!ticket || this._replySending()) return;

    this._replySending.set(true);
    try {
      const message = await this.api.replyToTicket(ticket.id, text);
      this._ticketThread.update((current) => [...current, message]);
      this._tickets.update((items) =>
        items.map((t) => (t.id === ticket.id ? { ...t, lastMessageText: message.text, lastMessageAt: message.createdAt } : t)),
      );
    } finally {
      this._replySending.set(false);
    }
  }
}
