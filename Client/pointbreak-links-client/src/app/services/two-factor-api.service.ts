import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { BackupCodesResultDto, BackupCodesStatusDto, TrustedDeviceDto, TwoFactorSetupDto } from '../core/models/two-factor.model';

@Injectable({ providedIn: 'root' })
export class TwoFactorApiService {
  private readonly http = inject(HttpClient);

  setup(): Promise<TwoFactorSetupDto> {
    return firstValueFrom(this.http.post<TwoFactorSetupDto>(ApiEndpoints.twoFactor.setup, {}));
  }

  // Backend mints and returns a set of one-time backup codes the moment 2FA is confirmed on —
  // see Identity.Application's Confirm2FaCommandHandler.
  confirm(code: string): Promise<BackupCodesResultDto> {
    return firstValueFrom(this.http.post<BackupCodesResultDto>(ApiEndpoints.twoFactor.confirm, { code }));
  }

  disable(password: string): Promise<void> {
    return firstValueFrom(this.http.post<void>(ApiEndpoints.twoFactor.disable, { password }));
  }

  getBackupCodesStatus(): Promise<BackupCodesStatusDto> {
    return firstValueFrom(this.http.get<BackupCodesStatusDto>(ApiEndpoints.twoFactor.backupCodesStatus));
  }

  regenerateBackupCodes(password: string): Promise<BackupCodesResultDto> {
    return firstValueFrom(this.http.post<BackupCodesResultDto>(ApiEndpoints.twoFactor.regenerateBackupCodes, { password }));
  }

  getTrustedDevices(): Promise<TrustedDeviceDto[]> {
    return firstValueFrom(this.http.get<TrustedDeviceDto[]>(ApiEndpoints.twoFactor.trustedDevices));
  }

  revokeTrustedDevice(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(ApiEndpoints.twoFactor.revokeTrustedDevice(id)));
  }
}
