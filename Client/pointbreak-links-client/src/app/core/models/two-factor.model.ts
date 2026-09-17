// Shape matches Identity.Application/CQRS/Auth/DTOs/TwoFactorSetupDto.cs.
export interface TwoFactorSetupDto {
  secret: string;
  otpAuthUri: string;
}

// Shape matches TwoFactorController's anonymous Confirm/RegenerateBackupCodes responses —
// backupCodes is the only time these plaintext codes are ever sent to the client.
export interface BackupCodesResultDto {
  backupCodes: string[];
}

export interface BackupCodesStatusDto {
  remaining: number;
}

// Shape matches Identity.Application/CQRS/Auth/DTOs/TrustedDeviceDto.cs.
export interface TrustedDeviceDto {
  id: number;
  label: string | null;
  createdAt: string;
  expiresAt: string;
}

// Shape matches Identity.Application/CQRS/Auth/DTOs/LoginHistoryEntryDto.cs. Purely
// informational — unlike TrustedDeviceDto, nothing here gates anything.
export interface LoginHistoryEntryDto {
  ipAddress: string | null;
  userAgent: string | null;
  createdAt: string;
}
