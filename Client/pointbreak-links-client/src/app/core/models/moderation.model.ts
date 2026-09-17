// Shape matches Application/CQRS/Moderation/DTOs/PendingSiteDto.cs exactly.
export interface PendingSiteDto {
  id: number;
  url: string;
  topic: string;
  price: number;
  iks: number;
  dr: number;
  sellerName: string;
  createdAt: string;
}

// Shape matches ModerationController's anonymous bulk-approve/bulk-reject response.
export interface BulkModerationResult {
  approved?: number;
  rejected?: number;
  failed: number[];
}

// Shape matches Application/CQRS/Moderation/DTOs/ModerationAuditEntryDto.cs exactly.
export interface ModerationAuditEntryDto {
  siteUrl: string;
  moderatorName: string;
  action: 'approved' | 'rejected';
  reason: string | null;
  createdAt: string;
}
