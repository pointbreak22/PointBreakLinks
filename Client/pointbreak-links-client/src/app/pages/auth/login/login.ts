import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { extractErrorMessage } from '../../../core/http/api-error';
import { Header } from '../../../shared/layout/header/header';
import { UserStore } from '../../../stores/user.store';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink, Header],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.html',
})
export class Login {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly userStore = inject(UserStore);

  readonly form = new FormGroup({
    email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
    password: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
  });

  // A FormGroup (not a bare FormControl) even for this one field — a <form> with no
  // [formGroup] and no FormsModule import gets no NgForm/FormGroupDirective, so (ngSubmit)
  // never fires and the button falls back to the browser's native full-page submit instead.
  readonly twoFactorForm = new FormGroup({
    code: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    rememberDevice: new FormControl(false, { nonNullable: true }),
  });

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly twoFactorTicket = signal<string | null>(null);

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const { email, password } = this.form.getRawValue();
      const outcome = await this.authService.login(email, password);
      if (outcome.requiresTwoFactor) {
        this.twoFactorTicket.set(outcome.ticket);
        return;
      }
      await this.router.navigateByUrl(this.userStore.postLoginRoute());
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
      await this.router.navigateByUrl(this.userStore.postLoginRoute());
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
}
