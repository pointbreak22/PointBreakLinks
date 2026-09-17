import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { extractErrorMessage } from '../../../core/http/api-error';
import { Header } from '../../../shared/layout/header/header';

@Component({
  selector: 'app-forgot-password',
  imports: [ReactiveFormsModule, RouterLink, Header],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './forgot-password.html',
})
export class ForgotPassword {
  private readonly authService = inject(AuthService);

  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
  });

  readonly submitting = signal(false);
  // The API always responds the same way whether or not the email is registered (see
  // ForgotPasswordCommandHandler) — so this flips to a static confirmation message rather
  // than surfacing any per-attempt success/error state. A request can still fail outright
  // though (e.g. the endpoint's rate limit — see Program.cs's "auth-sensitive" policy), which
  // errorMessage below is for.
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(null);

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const { email } = this.form.getRawValue();
      await this.authService.forgotPassword(email);
      this.submitted.set(true);
    } catch (error) {
      this.errorMessage.set(extractErrorMessage(error, 'Не удалось отправить ссылку для сброса пароля.'));
    } finally {
      this.submitting.set(false);
    }
  }
}
