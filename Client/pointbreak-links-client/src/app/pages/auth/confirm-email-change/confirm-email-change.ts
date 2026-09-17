import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { extractErrorMessage } from '../../../core/http/api-error';
import { Header } from '../../../shared/layout/header/header';

// Reached from the link in ChangeEmailCommandHandler's confirmation email — confirms
// automatically on load rather than waiting for a submit click, since there's nothing left for
// the visitor to fill in (the token in the URL is the whole payload).
@Component({
  selector: 'app-confirm-email-change',
  imports: [RouterLink, Header],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './confirm-email-change.html',
})
export class ConfirmEmailChange implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  protected readonly confirming = signal(true);
  protected readonly errorMessage = signal<string | null>(null);

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.errorMessage.set('Ссылка для подтверждения email недействительна.');
      this.confirming.set(false);
      return;
    }

    void this.confirm(token);
  }

  private async confirm(token: string): Promise<void> {
    try {
      await this.authService.confirmEmailChange(token);
    } catch (error) {
      this.errorMessage.set(extractErrorMessage(error, 'Не удалось подтвердить смену email.'));
    } finally {
      this.confirming.set(false);
    }
  }
}
