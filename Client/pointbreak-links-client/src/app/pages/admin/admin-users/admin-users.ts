import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { debounceTime, distinctUntilChanged } from 'rxjs';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../../shared/pagination/pagination';
import { extractErrorMessage } from '../../../core/http/api-error';
import { ToastService } from '../../../core/notifications/toast.service';
import { AdminStore } from '../../../stores/admin.store';
import { UserStore } from '../../../stores/user.store';

// New page, not a port — FOXLinks' admin-users.vue is a local mock array with no backing API
// (see AdminUserDto's comment on the API side). Kept to what's real: list users, change role,
// ban/unban. Dropped: fake balance column, "on moderation" user status, bulk-actions toolbar,
// filters, and the add-user modal (creating accounts with fabricated data isn't a real admin
// action worth replicating).
const ROLE_OPTIONS: { name: string; displayName: string }[] = [
  { name: 'admin', displayName: 'Администратор' },
  { name: 'moderator', displayName: 'Модератор' },
  { name: 'webmaster', displayName: 'Вебмастер' },
  { name: 'universal', displayName: 'Универсальный' },
  { name: 'optimizer', displayName: 'Оптимизатор' },
];

@Component({
  selector: 'app-admin-users',
  imports: [Header, Sidebar, Pagination, ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-users.html',
})
export class AdminUsers implements OnInit {
  protected readonly adminStore = inject(AdminStore);
  protected readonly userStore = inject(UserStore);
  private readonly toastService = inject(ToastService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly roleOptions = ROLE_OPTIONS;
  protected readonly searchControl = new FormControl('', { nonNullable: true });

  ngOnInit(): void {
    void this.adminStore.fetchUsers();

    // Debounced so typing doesn't fire a request per keystroke — role filter (a <select>) fires
    // immediately instead, since a discrete choice doesn't need the same debounce.
    this.searchControl.valueChanges
      .pipe(debounceTime(300), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe((value) => this.adminStore.setUsersSearch(value.trim()));
  }

  onRoleFilterChange(event: Event): void {
    this.adminStore.setUsersRole((event.target as HTMLSelectElement).value);
  }

  changePage(page: number): void {
    void this.adminStore.fetchUsers(page);
  }

  async onRoleChange(userId: number, event: Event): Promise<void> {
    const role = (event.target as HTMLSelectElement).value;
    try {
      await this.adminStore.setRole(userId, role);
      this.toastService.notify('Роль обновлена', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось обновить роль'), 'error');
    }
  }

  async toggleBanned(userId: number, isBanned: boolean): Promise<void> {
    try {
      await this.adminStore.setBanned(userId, !isBanned);
      this.toastService.notify(isBanned ? 'Пользователь разблокирован' : 'Пользователь заблокирован', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось изменить статус пользователя'), 'error');
    }
  }

  // Clears a LoginCommandHandler brute-force lockout early — separate from ban/unban, which is
  // an admin decision rather than an automatic security measure the user might want lifted
  // sooner (e.g. they got in touch with support after locking themselves out).
  async unlock(userId: number): Promise<void> {
    try {
      await this.adminStore.unlockUser(userId);
      this.toastService.notify('Блокировка снята', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось снять блокировку'), 'error');
    }
  }
}
