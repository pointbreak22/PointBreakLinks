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
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { extractErrorMessage } from '../../../core/http/api-error';
import { ToastService } from '../../../core/notifications/toast.service';
import { Header } from '../../../shared/layout/header/header';

interface BenefitCard {
  icon: string;
  title: string;
  description: string;
}

// Requires 8+ chars, at least one uppercase letter and one digit — same rule FOXLinks'
// register.vue enforces client-side (the regex in its handleSubmit).
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

// Ported from FOXLinks' pages/register.vue.
@Component({
  selector: 'app-register',
  imports: [ReactiveFormsModule, RouterLink, Header],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './register.html',
})
export class Register {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toastService = inject(ToastService);

  protected readonly benefitCards: BenefitCard[] = [
    { icon: 'fa-headset', title: 'Режим «Беззаботный»', description: 'Передаем заботы сотрудникам системы.' },
    { icon: 'fa-shield-alt', title: 'Качественные площадки и гарантия', description: 'Более 21 000 площадок с ручной проверкой.' },
    { icon: 'fa-money-bill-wave', title: 'Разовая оплата', description: 'Платите только один раз, статьи размещаются на весь срок.' },
    { icon: 'fa-gift', title: 'Бонусы при пополнении', description: 'Дарим бонусы при пополнении картой или кошельком.' },
  ];

  readonly form = new FormGroup(
    {
      username: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      email: new FormControl('', { nonNullable: true, validators: [Validators.required, Validators.email] }),
      password: new FormControl('', { nonNullable: true, validators: [Validators.required, passwordStrength()] }),
      confirmPassword: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
      agreement: new FormControl(false, { nonNullable: true, validators: [Validators.requiredTrue] }),
      newsletter: new FormControl(true, { nonNullable: true }),
    },
    { validators: passwordsMatch() },
  );

  readonly submitting = signal(false);

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      if (this.form.errors?.['passwordMismatch']) {
        this.toastService.notify('Пароли не совпадают', 'error');
      }
      return;
    }

    this.submitting.set(true);
    try {
      const { username, email, password } = this.form.getRawValue();
      await this.authService.register(username, email, password);
      this.toastService.notify('Регистрация прошла успешно! Перенаправляем...', 'success');
      await this.router.navigateByUrl('/projects');
    } catch (error) {
      this.toastService.notify(
        extractErrorMessage(error, 'Не удалось зарегистрироваться. Возможно, email уже занят.'),
        'error',
      );
    } finally {
      this.submitting.set(false);
    }
  }
}
