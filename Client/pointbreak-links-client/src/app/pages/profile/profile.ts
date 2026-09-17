import { isPlatformBrowser } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import {
  AbstractControl,
  ReactiveFormsModule,
  FormControl,
  FormGroup,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { Router } from '@angular/router';
import { toDataURL } from 'qrcode';
import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/http/api-error';
import { Header } from '../../shared/layout/header/header';
import { UserStore } from '../../stores/user.store';
import { TwoFactorApiService } from '../../services/two-factor-api.service';
import { LoginHistoryEntryDto, TrustedDeviceDto } from '../../core/models/two-factor.model';
import { NotificationsApiService } from '../../services/notifications-api.service';
import { NotificationPreferenceDto } from '../../core/models/notification.model';
import { ToastService } from '../../core/notifications/toast.service';

// Same rule enforced server-side by Application/Common/PasswordPolicy.cs.
const PASSWORD_PATTERN = /^(?=.*[A-Z])(?=.*\d).{8,}$/;

function passwordStrength(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null =>
    PASSWORD_PATTERN.test(control.value as string) ? null : { weakPassword: true };
}

function passwordsMatch(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get('newPassword')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, Header],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './profile.html',
})
export class Profile implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly twoFactorApi = inject(TwoFactorApiService);
  private readonly notificationsApi = inject(NotificationsApiService);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  protected readonly userStore = inject(UserStore);

  ngOnInit(): void {
    if (this.userStore.currentUser()?.twoFactorEnabled) {
      void this.refreshBackupCodesRemaining();
      void this.loadTrustedDevices();
    }
    void this.loadNotificationPreferences();
    void this.loadLoginHistory();
  }

  readonly form = new FormGroup(
    {
      currentPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      newPassword: new FormControl('', { nonNullable: true, validators: [Validators.required, passwordStrength()] }),
      confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    },
    { validators: passwordsMatch() },
  );

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly emailForm = new FormGroup({
    newEmail: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    currentPasswordForEmail: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });
  readonly submittingEmail = signal(false);
  readonly emailErrorMessage = signal<string | null>(null);
  readonly emailSuccessMessage = signal<string | null>(null);

  async submitEmailChange(): Promise<void> {
    this.emailSuccessMessage.set(null);
    if (this.emailForm.invalid) {
      this.emailForm.markAllAsTouched();
      return;
    }

    this.submittingEmail.set(true);
    this.emailErrorMessage.set(null);
    try {
      const { newEmail, currentPasswordForEmail } = this.emailForm.getRawValue();
      await this.authService.changeEmail(newEmail, currentPasswordForEmail);
      this.emailSuccessMessage.set('Письмо со ссылкой для подтверждения отправлено на новый адрес.');
      this.emailForm.reset();
    } catch (error) {
      this.emailErrorMessage.set(extractErrorMessage(error, 'Не удалось начать смену email.'));
    } finally {
      this.submittingEmail.set(false);
    }
  }

  async submit(): Promise<void> {
    this.successMessage.set(null);
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const { currentPassword, newPassword } = this.form.getRawValue();
      await this.authService.changePassword(currentPassword, newPassword);
      this.successMessage.set('Пароль успешно изменён.');
      this.form.reset();
    } catch (error) {
      this.errorMessage.set(extractErrorMessage(error, 'Не удалось изменить пароль.'));
    } finally {
      this.submitting.set(false);
    }
  }

  // --- Notification email preferences ---
  // Email opt-out only — the in-app inbox (header bell) always reflects what actually happened
  // regardless of these toggles, see NotificationPreference's comment on the API side.

  protected readonly notificationPreferences = signal<NotificationPreferenceDto | null>(null);
  protected readonly savingPreferences = signal(false);

  private async loadNotificationPreferences(): Promise<void> {
    try {
      this.notificationPreferences.set(await this.notificationsApi.getPreferences());
    } catch {
      // Non-critical — the toggles just won't render if this fails; no toast needed on load.
    }
  }

  async toggleNotificationPreference(key: keyof NotificationPreferenceDto): Promise<void> {
    const current = this.notificationPreferences();
    if (!current || this.savingPreferences()) return;

    const next = { ...current, [key]: !current[key] };
    this.notificationPreferences.set(next);
    this.savingPreferences.set(true);
    try {
      this.notificationPreferences.set(await this.notificationsApi.updatePreferences(next));
    } catch (error) {
      this.notificationPreferences.set(current);
      this.toastService.notify(extractErrorMessage(error, 'Не удалось сохранить настройки уведомлений'), 'error');
    } finally {
      this.savingPreferences.set(false);
    }
  }

  // --- Two-factor authentication ---

  protected readonly twoFactorQrCode = signal<string | null>(null);
  protected readonly twoFactorSecret = signal<string | null>(null);
  protected readonly twoFactorConfirmCode = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  protected readonly twoFactorDisablePassword = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  protected readonly twoFactorBusy = signal(false);
  protected readonly twoFactorError = signal<string | null>(null);
  protected readonly disablingTwoFactor = signal(false);

  // Backup codes are only ever visible right after they're minted (setup confirm or an explicit
  // regenerate) — the server never returns them again afterwards, only their count.
  protected readonly backupCodes = signal<string[] | null>(null);
  protected readonly backupCodesRemaining = signal<number | null>(null);
  protected readonly regeneratingBackupCodes = signal(false);
  protected readonly regenerateBackupCodesPassword = new FormControl('', { nonNullable: true, validators: [Validators.required] });

  async startTwoFactorSetup(): Promise<void> {
    this.twoFactorError.set(null);
    this.twoFactorBusy.set(true);
    try {
      const { secret, otpAuthUri } = await this.twoFactorApi.setup();
      this.twoFactorSecret.set(secret);
      if (this.isBrowser) {
        this.twoFactorQrCode.set(await toDataURL(otpAuthUri));
      }
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось начать настройку'), 'error');
    } finally {
      this.twoFactorBusy.set(false);
    }
  }

  cancelTwoFactorSetup(): void {
    this.twoFactorSecret.set(null);
    this.twoFactorQrCode.set(null);
    this.twoFactorConfirmCode.reset('');
    this.twoFactorError.set(null);
  }

  async confirmTwoFactorSetup(): Promise<void> {
    if (this.twoFactorConfirmCode.invalid) {
      this.twoFactorConfirmCode.markAsTouched();
      return;
    }

    this.twoFactorBusy.set(true);
    this.twoFactorError.set(null);
    try {
      const { backupCodes } = await this.twoFactorApi.confirm(this.twoFactorConfirmCode.value);
      this.userStore.setTwoFactorEnabled(true);
      this.cancelTwoFactorSetup();
      this.backupCodes.set(backupCodes);
      this.backupCodesRemaining.set(backupCodes.length);
      this.toastService.notify('Двухфакторная аутентификация включена', 'success');
    } catch (error) {
      this.twoFactorError.set(extractErrorMessage(error, 'Неверный код подтверждения.'));
    } finally {
      this.twoFactorBusy.set(false);
    }
  }

  // Dismisses the one-time "save these codes" panel shown after confirm/regenerate — the codes
  // themselves are gone from memory once this is called, matching the server never returning them again.
  acknowledgeBackupCodes(): void {
    this.backupCodes.set(null);
  }

  private async refreshBackupCodesRemaining(): Promise<void> {
    try {
      const { remaining } = await this.twoFactorApi.getBackupCodesStatus();
      this.backupCodesRemaining.set(remaining);
    } catch {
      // Non-critical display info — silently leave it unset rather than surfacing a toast.
    }
  }

  startRegenerateBackupCodes(): void {
    this.regeneratingBackupCodes.set(true);
  }

  cancelRegenerateBackupCodes(): void {
    this.regeneratingBackupCodes.set(false);
    this.regenerateBackupCodesPassword.reset('');
    this.twoFactorError.set(null);
  }

  async confirmRegenerateBackupCodes(): Promise<void> {
    if (this.regenerateBackupCodesPassword.invalid) {
      this.regenerateBackupCodesPassword.markAsTouched();
      return;
    }

    this.twoFactorBusy.set(true);
    this.twoFactorError.set(null);
    try {
      const { backupCodes } = await this.twoFactorApi.regenerateBackupCodes(this.regenerateBackupCodesPassword.value);
      this.backupCodes.set(backupCodes);
      this.backupCodesRemaining.set(backupCodes.length);
      this.cancelRegenerateBackupCodes();
      this.toastService.notify('Резервные коды обновлены', 'success');
    } catch (error) {
      this.twoFactorError.set(extractErrorMessage(error, 'Неверный пароль.'));
    } finally {
      this.twoFactorBusy.set(false);
    }
  }

  startDisableTwoFactor(): void {
    this.disablingTwoFactor.set(true);
  }

  cancelDisableTwoFactor(): void {
    this.disablingTwoFactor.set(false);
    this.twoFactorDisablePassword.reset('');
    this.twoFactorError.set(null);
  }

  async confirmDisableTwoFactor(): Promise<void> {
    if (this.twoFactorDisablePassword.invalid) {
      this.twoFactorDisablePassword.markAsTouched();
      return;
    }

    this.twoFactorBusy.set(true);
    this.twoFactorError.set(null);
    try {
      await this.twoFactorApi.disable(this.twoFactorDisablePassword.value);
      this.userStore.setTwoFactorEnabled(false);
      this.backupCodesRemaining.set(null);
      this.trustedDevices.set([]);
      this.cancelDisableTwoFactor();
      this.toastService.notify('Двухфакторная аутентификация отключена', 'success');
    } catch (error) {
      this.twoFactorError.set(extractErrorMessage(error, 'Неверный пароль.'));
    } finally {
      this.twoFactorBusy.set(false);
    }
  }

  // --- Trusted devices ("Запомнить это устройство" at 2FA login) ---

  protected readonly trustedDevices = signal<TrustedDeviceDto[]>([]);
  protected readonly revokingDeviceId = signal<number | null>(null);

  private async loadTrustedDevices(): Promise<void> {
    try {
      this.trustedDevices.set(await this.twoFactorApi.getTrustedDevices());
    } catch {
      // Non-critical display info — the list just won't render if this fails.
    }
  }

  async revokeTrustedDevice(id: number): Promise<void> {
    this.revokingDeviceId.set(id);
    try {
      await this.twoFactorApi.revokeTrustedDevice(id);
      this.trustedDevices.update((devices) => devices.filter((d) => d.id !== id));
      this.toastService.notify('Устройство больше не будет пропускать проверку 2FA', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось отозвать устройство'), 'error');
    } finally {
      this.revokingDeviceId.set(null);
    }
  }

  // --- Login history — purely informational, unlike TrustedDevices above nothing here gates
  // anything; it's just the last line of defense for a user noticing "that wasn't me."

  protected readonly loginHistory = signal<LoginHistoryEntryDto[]>([]);

  private async loadLoginHistory(): Promise<void> {
    try {
      this.loginHistory.set(await this.authService.getLoginHistory());
    } catch {
      // Non-critical display info — the list just won't render if this fails.
    }
  }

  // --- Account deletion ("Опасная зона") — self-service, password-confirmed soft delete.
  // See DeactivateAccountCommandHandler: blocks login, revokes 2FA/trusted devices/refresh
  // tokens server-side. This is the one destructive action on the page, so it's gated behind
  // an explicit "start" step rather than a single click.

  protected readonly deletingAccount = signal(false);
  protected readonly deleteAccountPassword = new FormControl('', { nonNullable: true, validators: [Validators.required] });
  protected readonly deleteAccountBusy = signal(false);
  protected readonly deleteAccountError = signal<string | null>(null);

  startDeleteAccount(): void {
    this.deletingAccount.set(true);
  }

  cancelDeleteAccount(): void {
    this.deletingAccount.set(false);
    this.deleteAccountPassword.reset('');
    this.deleteAccountError.set(null);
  }

  async confirmDeleteAccount(): Promise<void> {
    if (this.deleteAccountPassword.invalid) {
      this.deleteAccountPassword.markAsTouched();
      return;
    }

    this.deleteAccountBusy.set(true);
    this.deleteAccountError.set(null);
    try {
      await this.authService.deactivateAccount(this.deleteAccountPassword.value);
      this.toastService.notify('Аккаунт удалён', 'success');
      await this.router.navigateByUrl('/');
    } catch (error) {
      this.deleteAccountError.set(extractErrorMessage(error, 'Неверный пароль.'));
    } finally {
      this.deleteAccountBusy.set(false);
    }
  }
}
