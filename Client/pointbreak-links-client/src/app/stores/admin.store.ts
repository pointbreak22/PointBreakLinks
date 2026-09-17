import { Injectable, inject, signal } from '@angular/core';
import { AdminApiService } from '../services/admin-api.service';
import {
  AdminDashboardDto,
  AdminProjectDto,
  AdminSystemLogDto,
  AdminTransactionDto,
  AdminUserDto,
  AdminWithdrawalRequestDto,
  DisputedOrderDto,
} from '../core/models/admin.model';
import { PageMeta } from '../core/models/pagination.model';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 15, total: 0 };

@Injectable({ providedIn: 'root' })
export class AdminStore {
  private readonly api = inject(AdminApiService);

  private readonly _users = signal<AdminUserDto[]>([]);
  private readonly _meta = signal<PageMeta>(emptyMeta);
  private readonly _loading = signal(false);
  private readonly _usersSearch = signal('');
  private readonly _usersRole = signal('');
  private readonly _dashboard = signal<AdminDashboardDto | null>(null);

  private readonly _projects = signal<AdminProjectDto[]>([]);
  private readonly _projectsMeta = signal<PageMeta>(emptyMeta);
  private readonly _projectsLoading = signal(false);

  private readonly _transactions = signal<AdminTransactionDto[]>([]);
  private readonly _transactionsMeta = signal<PageMeta>(emptyMeta);
  private readonly _transactionsLoading = signal(false);

  private readonly _systemLogs = signal<AdminSystemLogDto[]>([]);
  private readonly _systemLogsMeta = signal<PageMeta>(emptyMeta);
  private readonly _systemLogsLoading = signal(false);

  private readonly _disputes = signal<DisputedOrderDto[]>([]);
  private readonly _disputesMeta = signal<PageMeta>(emptyMeta);
  private readonly _disputesLoading = signal(false);

  private readonly _withdrawals = signal<AdminWithdrawalRequestDto[]>([]);
  private readonly _withdrawalsMeta = signal<PageMeta>(emptyMeta);
  private readonly _withdrawalsLoading = signal(false);

  readonly users = this._users.asReadonly();
  readonly meta = this._meta.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly dashboard = this._dashboard.asReadonly();
  readonly usersSearch = this._usersSearch.asReadonly();
  readonly usersRole = this._usersRole.asReadonly();

  readonly projects = this._projects.asReadonly();
  readonly projectsMeta = this._projectsMeta.asReadonly();
  readonly projectsLoading = this._projectsLoading.asReadonly();

  readonly transactions = this._transactions.asReadonly();
  readonly transactionsMeta = this._transactionsMeta.asReadonly();
  readonly transactionsLoading = this._transactionsLoading.asReadonly();

  readonly systemLogs = this._systemLogs.asReadonly();
  readonly systemLogsMeta = this._systemLogsMeta.asReadonly();
  readonly systemLogsLoading = this._systemLogsLoading.asReadonly();

  readonly disputes = this._disputes.asReadonly();
  readonly disputesMeta = this._disputesMeta.asReadonly();
  readonly disputesLoading = this._disputesLoading.asReadonly();

  readonly withdrawals = this._withdrawals.asReadonly();
  readonly withdrawalsMeta = this._withdrawalsMeta.asReadonly();
  readonly withdrawalsLoading = this._withdrawalsLoading.asReadonly();

  async fetchDashboard(): Promise<void> {
    this._loading.set(true);
    try {
      this._dashboard.set(await this.api.getDashboard());
    } finally {
      this._loading.set(false);
    }
  }

  async fetchUsers(page = 1): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getUsers(page, this._meta().perPage, this._usersSearch(), this._usersRole());
      this._users.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }

  // Resets to page 1 on every filter change — a stale currentPage from a previous, differently
  // filtered result set could otherwise ask the server for a page that no longer exists.
  setUsersSearch(search: string): void {
    this._usersSearch.set(search);
    void this.fetchUsers(1);
  }

  setUsersRole(role: string): void {
    this._usersRole.set(role);
    void this.fetchUsers(1);
  }

  async setBanned(id: number, isBanned: boolean): Promise<void> {
    const updated = await this.api.setBanned(id, isBanned);
    this._users.update((users) => users.map((u) => (u.id === id ? updated : u)));
  }

  async setRole(id: number, role: string): Promise<void> {
    const updated = await this.api.setRole(id, role);
    this._users.update((users) => users.map((u) => (u.id === id ? updated : u)));
  }

  async unlockUser(id: number): Promise<void> {
    const updated = await this.api.unlockUser(id);
    this._users.update((users) => users.map((u) => (u.id === id ? updated : u)));
  }

  async fetchProjects(page = 1): Promise<void> {
    this._projectsLoading.set(true);
    try {
      const response = await this.api.getProjects(page, this._projectsMeta().perPage);
      this._projects.set(response.items);
      this._projectsMeta.set(response);
    } finally {
      this._projectsLoading.set(false);
    }
  }

  async fetchTransactions(page = 1): Promise<void> {
    this._transactionsLoading.set(true);
    try {
      const response = await this.api.getTransactions(page, this._transactionsMeta().perPage);
      this._transactions.set(response.items);
      this._transactionsMeta.set(response);
    } finally {
      this._transactionsLoading.set(false);
    }
  }

  async fetchSystemLogs(page = 1): Promise<void> {
    this._systemLogsLoading.set(true);
    try {
      const response = await this.api.getSystemLogs(page, this._systemLogsMeta().perPage);
      this._systemLogs.set(response.items);
      this._systemLogsMeta.set(response);
    } finally {
      this._systemLogsLoading.set(false);
    }
  }

  async fetchDisputes(page = 1): Promise<void> {
    this._disputesLoading.set(true);
    try {
      const response = await this.api.getDisputes(page, this._disputesMeta().perPage);
      this._disputes.set(response.items);
      this._disputesMeta.set(response);
    } finally {
      this._disputesLoading.set(false);
    }
  }

  async resolveDispute(id: number, refundBuyer: boolean): Promise<void> {
    await this.api.resolveDispute(id, refundBuyer);
    this._disputes.update((items) => items.filter((d) => d.id !== id));
  }

  async fetchWithdrawals(page = 1): Promise<void> {
    this._withdrawalsLoading.set(true);
    try {
      const response = await this.api.getWithdrawals(page, this._withdrawalsMeta().perPage);
      this._withdrawals.set(response.items);
      this._withdrawalsMeta.set(response);
    } finally {
      this._withdrawalsLoading.set(false);
    }
  }

  async approveWithdrawal(id: number): Promise<void> {
    await this.api.approveWithdrawal(id);
    this._withdrawals.update((items) => items.filter((w) => w.id !== id));
  }

  async rejectWithdrawal(id: number, comment: string): Promise<void> {
    await this.api.rejectWithdrawal(id, comment);
    this._withdrawals.update((items) => items.filter((w) => w.id !== id));
  }
}
