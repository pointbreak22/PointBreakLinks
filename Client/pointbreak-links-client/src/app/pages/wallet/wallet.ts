import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Header } from '../../shared/layout/header/header';
import { Pagination } from '../../shared/pagination/pagination';
import { PageMeta } from '../../core/models/pagination.model';
import { BalanceTransactionDto, PAYMENT_METHODS, SavedPayoutMethodDto, WithdrawalRequestDto } from '../../core/models/wallet.model';
import { WalletApiService } from '../../services/wallet-api.service';
import { ToastService } from '../../core/notifications/toast.service';
import { UserStore } from '../../stores/user.store';
import { extractErrorMessage } from '../../core/http/api-error';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 20, total: 0 };

// New page, not a port — FOXLinks' balance-topup-modal.vue is orphaned (never imported
// anywhere in the source, see PROJECT_MAP.md), so there's no reference flow. Real ledger
// (BalanceTransaction) backing a real balance that RequestPublicationCommandHandler actually
// enforces — the one honest gap is the top-up itself: no payment provider is integrated, so
// it credits instantly instead of collecting real money (see
// TopUpBalanceCommandHandler's comment).
@Component({
  selector: 'app-wallet',
  imports: [Header, DecimalPipe, FormsModule, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './wallet.html',
})
export class Wallet implements OnInit {
  private readonly walletApi = inject(WalletApiService);
  private readonly toastService = inject(ToastService);
  protected readonly userStore = inject(UserStore);

  protected readonly transactions = signal<BalanceTransactionDto[]>([]);
  protected readonly meta = signal<PageMeta>(emptyMeta);
  protected readonly loading = signal(false);
  protected readonly topUpAmount = signal<number | null>(1000);
  protected readonly submitting = signal(false);
  protected readonly paymentMethods = PAYMENT_METHODS;
  protected readonly selectedMethod = signal<string>(PAYMENT_METHODS[0].code);

  protected readonly withdrawals = signal<WithdrawalRequestDto[]>([]);
  protected readonly withdrawalsMeta = signal<PageMeta>(emptyMeta);
  protected readonly withdrawalsLoading = signal(false);
  protected readonly withdrawAmount = signal<number | null>(null);
  protected readonly payoutDetails = signal('');
  protected readonly submittingWithdrawal = signal(false);

  protected readonly payoutMethods = signal<SavedPayoutMethodDto[]>([]);
  protected readonly addingPayoutMethod = signal(false);
  protected readonly newMethodLabel = signal('');
  protected readonly savingPayoutMethod = signal(false);

  ngOnInit(): void {
    void this.loadTransactions();
    void this.loadWithdrawals();
    void this.loadPayoutMethods();
  }

  async loadTransactions(page = 1): Promise<void> {
    this.loading.set(true);
    try {
      const response = await this.walletApi.getTransactions(page);
      this.transactions.set(response.items);
      this.meta.set(response);
    } finally {
      this.loading.set(false);
    }
  }

  async topUp(): Promise<void> {
    const amount = this.topUpAmount();
    if (!amount || amount <= 0 || this.submitting()) return;

    this.submitting.set(true);
    try {
      const balance = await this.walletApi.topUp(amount, this.selectedMethod());
      this.userStore.setBalance(balance);
      this.toastService.notify(`Баланс пополнен на ${amount} ₽`, 'success');
      await this.loadTransactions(this.meta().currentPage);
    } catch {
      this.toastService.notify('Не удалось пополнить баланс', 'error');
    } finally {
      this.submitting.set(false);
    }
  }

  async loadWithdrawals(page = 1): Promise<void> {
    this.withdrawalsLoading.set(true);
    try {
      const response = await this.walletApi.getWithdrawals(page);
      this.withdrawals.set(response.items);
      this.withdrawalsMeta.set(response);
    } finally {
      this.withdrawalsLoading.set(false);
    }
  }

  async requestWithdrawal(): Promise<void> {
    const amount = this.withdrawAmount();
    const details = this.payoutDetails().trim();
    if (!amount || amount <= 0 || !details || this.submittingWithdrawal()) return;

    this.submittingWithdrawal.set(true);
    try {
      await this.walletApi.requestWithdrawal(amount, details);
      const balance = await this.walletApi.getBalance();
      this.userStore.setBalance(balance);
      this.toastService.notify('Заявка на вывод средств создана', 'success');
      this.withdrawAmount.set(null);
      this.payoutDetails.set('');
      await this.loadWithdrawals(this.withdrawalsMeta().currentPage);
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось создать заявку на вывод'), 'error');
    } finally {
      this.submittingWithdrawal.set(false);
    }
  }

  // --- Saved payout methods — a personal address book so reused requisites don't need retyping.
  // Selecting one just fills payoutDetails; there's no link back to the withdrawal request itself
  // (see SavedPayoutMethod's own comment on why that's intentional).

  private async loadPayoutMethods(): Promise<void> {
    try {
      this.payoutMethods.set(await this.walletApi.getPayoutMethods());
    } catch {
      // Non-critical display info — the picker just won't render if this fails.
    }
  }

  useSavedPayoutMethod(method: SavedPayoutMethodDto): void {
    this.payoutDetails.set(method.details);
  }

  startSavePayoutMethod(): void {
    this.addingPayoutMethod.set(true);
  }

  cancelSavePayoutMethod(): void {
    this.addingPayoutMethod.set(false);
    this.newMethodLabel.set('');
  }

  async confirmSavePayoutMethod(): Promise<void> {
    const label = this.newMethodLabel().trim();
    const details = this.payoutDetails().trim();
    if (!label || !details || this.savingPayoutMethod()) return;

    this.savingPayoutMethod.set(true);
    try {
      const method = await this.walletApi.addPayoutMethod(label, details);
      this.payoutMethods.update((methods) => [method, ...methods]);
      this.cancelSavePayoutMethod();
      this.toastService.notify('Способ вывода сохранён', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось сохранить способ вывода'), 'error');
    } finally {
      this.savingPayoutMethod.set(false);
    }
  }

  async deletePayoutMethod(id: number): Promise<void> {
    try {
      await this.walletApi.deletePayoutMethod(id);
      this.payoutMethods.update((methods) => methods.filter((m) => m.id !== id));
    } catch {
      this.toastService.notify('Не удалось удалить способ вывода', 'error');
    }
  }
}
