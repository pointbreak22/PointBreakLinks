export type BalanceTransactionType = 'TopUp' | 'Purchase' | 'Sale' | 'Refund' | 'DisputeChargeback' | 'Withdrawal';

export interface BalanceTransactionDto {
  id: number;
  type: BalanceTransactionType;
  amount: number;
  description: string;
  paymentMethod: string | null;
  createdAt: string;
}

// Shape matches Domain/Constants/PaymentMethodNames.cs.
export const PAYMENT_METHODS = [
  { code: 'visa_mastercard', label: 'Visa / Mastercard', icon: 'fa-credit-card' },
  { code: 'mir', label: 'МИР', icon: 'fa-credit-card' },
] as const;

// Shape matches Application/CQRS/Wallet/DTOs/WithdrawalRequestDto.cs. No real payout provider is
// integrated (same honest gap as TopUp, mirrored — see WithdrawalRequestCommandHandler's
// comment): an admin sends the money manually outside the app and marks the request Approved/
// Rejected here.
export type WithdrawalRequestStatus = 'Pending' | 'Approved' | 'Rejected';

export interface WithdrawalRequestDto {
  id: number;
  amount: number;
  payoutDetails: string;
  status: WithdrawalRequestStatus;
  requestedAt: string;
  processedAt: string | null;
  adminComment: string | null;
}

// Shape matches Application/CQRS/Wallet/DTOs/SavedPayoutMethodDto.cs.
export interface SavedPayoutMethodDto {
  id: number;
  label: string;
  details: string;
}
