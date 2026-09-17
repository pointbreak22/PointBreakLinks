import { ChangeDetectionStrategy, Component, PLATFORM_ID, inject, output, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';
import { extractErrorMessage } from '../../core/http/api-error';

const REMEMBER_EMAIL_KEY = 'rememberEmail';

// Login-anywhere overlay, opened from Header — matches FOXLinks' login-modal.vue, which is
// how that app handles auth everywhere except the dedicated /register page. The dedicated
// /login route (pages/auth/login) still exists too, for direct navigation/bookmarking.
@Component({
  selector: 'app-login-modal',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login-modal.html',
})
export class LoginModal {
  private readonly authService = inject(AuthService);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  readonly close = output<void>();
  readonly loginSuccess = output<void>();

  readonly form = new FormGroup({
    email: new FormControl(this.readRememberedEmail(), {
      nonNullable: true,
      validators: [Validators.required, Validators.email],
    }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    remember: new FormControl(false, { nonNullable: true }),
  });

  // A FormGroup (not a bare FormControl) even for this one field — a <form> with no
  // [formGroup] and no FormsModule import gets no NgForm/FormGroupDirective, so (ngSubmit)
  // never fires and the button falls back to the browser's native full-page submit instead.
  readonly twoFactorForm = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    rememberDevice: new FormControl(false, { nonNullable: true }),
  });

  readonly showPassword = signal(false);
  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly twoFactorTicket = signal<string | null>(null);

  togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const { email, password, remember } = this.form.getRawValue();
      const outcome = await this.authService.login(email, password);
      if (outcome.requiresTwoFactor) {
        this.twoFactorTicket.set(outcome.ticket);
        this.rememberEmail(remember ? email : null);
        return;
      }
      this.rememberEmail(remember ? email : null);
      this.loginSuccess.emit();
    } catch (error) {
      this.errorMessage.set(extractErrorMessage(error, 'Неверный email или пароль.'));
    } finally {
      this.submitting.set(false);
    }
  }

  async submitTwoFactor(): Promise<void> {
    const ticket = this.twoFactorTicket();
    if (!ticket || this.twoFactorForm.invalid) {
      this.twoFactorForm.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const { code, rememberDevice } = this.twoFactorForm.getRawValue();
      await this.authService.completeTwoFactorLogin(ticket, code, rememberDevice);
      this.loginSuccess.emit();
    } catch (error) {
      this.errorMessage.set(extractErrorMessage(error, 'Неверный код подтверждения.'));
    } finally {
      this.submitting.set(false);
    }
  }

  cancelTwoFactor(): void {
    this.twoFactorTicket.set(null);
    this.twoFactorForm.reset({ code: '', rememberDevice: false });
    this.errorMessage.set(null);
  }

  private readRememberedEmail(): string {
    if (!this.isBrowser) return '';
    try {
      return localStorage.getItem(REMEMBER_EMAIL_KEY) ?? '';
    } catch {
      return '';
    }
  }

  private rememberEmail(email: string | null): void {
    if (!this.isBrowser) return;
    try {
      if (email) {
        localStorage.setItem(REMEMBER_EMAIL_KEY, email);
      } else {
        localStorage.removeItem(REMEMBER_EMAIL_KEY);
      }
    } catch {
      // Private browsing / storage disabled — "remember me" just won't persist.
    }
  }
}
