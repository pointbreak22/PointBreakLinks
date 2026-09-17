export interface NotificationDto {
  id: number;
  message: string;
  isRead: boolean;
  createdAt: string;
}

// Email opt-out only — the in-app inbox above is never gated by this, see
// Domain/Entities/NotificationPreference.cs's comment.
export interface NotificationPreferenceDto {
  emailOnOrderUpdates: boolean;
  emailOnDisputeUpdates: boolean;
}
