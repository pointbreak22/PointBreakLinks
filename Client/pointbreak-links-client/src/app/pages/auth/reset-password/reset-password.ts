import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import {
  AbstractControl,
  ReactiveFormsModule,
  FormControl,
  FormGroup,
  ValidationErrors,
  ValidatorFn,
  Validators,
} from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { extractErrorMessage } from '../../../core/http/api-error';
import { Header } from '../../../shared/layout/header/header';

// Same rule enforced server-side by Application/Common/PasswordPolicy.cs and client-side by
// register.ts's passwordStrength() — kept in sync manually since there's no shared package
// between client and API in this repo.
const PASSWORD_PATTERN = /^(?=.*[A-Z])(?=.*\d).{8,}$/;

function passwordStrength(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null =>
    PASSWORD_PATTERN.test(control.value as string) ? null : { weakPassword: true };
}

function passwordsMatch(): ValidatorFn {
  return (group: AbstractControl): ValidationErrors | null => {
    const password = group.get('password')?.value;
    const confirmPassword = group.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  };
}

@Component({
  selector: 'app-reset-password',
  imports: [ReactiveFormsModule, RouterLink, Header],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './reset-password.html',
})
export class ResetPassword {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  private readonly token = this.route.snapshot.queryParamMap.get('token');

  readonly form = new FormGroup(
    {
      password: new FormControl('', { nonNullable: true, validators: [Validators.required, passwordStrength()] }),
      confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    },
    { validators: passwordsMatch() },
  );

  readonly submitting = signal(false);
  readonly submitted = signal(false);
  readonly errorMessage = signal<string | null>(this.token ? null : 'Ссылка для сброса пароля недействительна.');

  async submit(): Promise<void> {
    if (!this.token) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(null);
    try {
      const { password } = this.form.getRawValue();
      await this.authService.resetPassword(this.token, password);
      this.submitted.set(true);
      setTimeout(() => void this.router.navigateByUrl('/login'), 2000);
    } catch (error) {
      this.errorMessage.set(extractErrorMessage(error, 'Не удалось сбросить пароль.'));
    } finally {
      this.submitting.set(false);
    }
  }
}
