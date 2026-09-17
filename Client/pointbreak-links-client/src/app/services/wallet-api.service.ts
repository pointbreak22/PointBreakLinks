import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { PagedResult } from '../core/models/pagination.model';
import { BalanceTransactionDto, SavedPayoutMethodDto, WithdrawalRequestDto } from '../core/models/wallet.model';

@Injectable({ providedIn: 'root' })
export class WalletApiService {
  private readonly http = inject(HttpClient);

  async getBalance(): Promise<number> {
    const response = await firstValueFrom(this.http.get<{ balance: number }>(ApiEndpoints.wallet.balance));
    return response.balance;
  }

  getTransactions(page = 1, perPage = 20): Promise<PagedResult<BalanceTransactionDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<BalanceTransactionDto>>(ApiEndpoints.wallet.transactions, { params }));
  }

  async topUp(amount: number, paymentMethod: string): Promise<number> {
    const response = await firstValueFrom(
      this.http.post<{ balance: number }>(ApiEndpoints.wallet.topUp, { amount, paymentMethod }),
    );
    return response.balance;
  }

  getWithdrawals(page = 1, perPage = 20): Promise<PagedResult<WithdrawalRequestDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<WithdrawalRequestDto>>(ApiEndpoints.wallet.withdrawals, { params }));
  }

  async requestWithdrawal(amount: number, payoutDetails: string): Promise<WithdrawalRequestDto> {
    const response = await firstValueFrom(
      this.http.post<{ data: WithdrawalRequestDto }>(ApiEndpoints.wallet.withdraw, { amount, payoutDetails }),
    );
    return response.data;
  }

  getPayoutMethods(): Promise<SavedPayoutMethodDto[]> {
    return firstValueFrom(this.http.get<SavedPayoutMethodDto[]>(ApiEndpoints.wallet.payoutMethods));
  }

  addPayoutMethod(label: string, details: string): Promise<SavedPayoutMethodDto> {
    return firstValueFrom(this.http.post<SavedPayoutMethodDto>(ApiEndpoints.wallet.payoutMethods, { label, details }));
  }

  deletePayoutMethod(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(ApiEndpoints.wallet.payoutMethodById(id)));
  }
}
