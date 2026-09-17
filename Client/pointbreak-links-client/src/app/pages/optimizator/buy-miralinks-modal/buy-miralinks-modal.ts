import { DecimalPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { extractErrorMessage } from '../../../core/http/api-error';
import { InsuranceType, SiteDto } from '../../../core/models/site.model';
import { ToastService } from '../../../core/notifications/toast.service';
import { ProjectsStore } from '../../../stores/projects.store';
import { SitesStore } from '../../../stores/sites.store';
import { UserStore } from '../../../stores/user.store';
import { WalletApiService } from '../../../services/wallet-api.service';

type Tab = 'links' | 'settings' | 'summary';

interface LinkRow {
  text: string;
  url: string;
}

const INSURANCE_LABELS: Record<InsuranceType, string> = {
  None: 'Без страхования',
  LossProtection: 'Страхование от пропажи',
  Full: 'Полное страхование',
};

// Ported from FOXLinks' modal-windows/optimizator/buy-miralinks-modal.vue — the real purchase
// flow (unlike create-order-modal.vue/details-panel.vue, which are unwired dead code and were
// not ported at all). The source's "Итоги" tab hardcoded fake values (link count always "2
// шт.", project/insurance always "Не выбрано") and a fake balance section with no backing
// data — fixed here to compute the summary from the same reactive state the other two tabs use.
// The balance section itself is real now (Application/CQRS/Wallet): submitting fails with a
// clear "Недостаточно средств..." error (surfaced by extractErrorMessage, no special-casing
// needed here) if the buyer can't afford it, and a successful order actually debits their
// balance server-side — refreshed into UserStore right after so the header updates immediately.
@Component({
  selector: 'app-buy-miralinks-modal',
  imports: [DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './buy-miralinks-modal.html',
})
export class BuyMiralinksModal implements OnInit {
  private readonly sitesStore = inject(SitesStore);
  protected readonly projectsStore = inject(ProjectsStore);
  private readonly toastService = inject(ToastService);
  private readonly userStore = inject(UserStore);
  private readonly walletApi = inject(WalletApiService);

  readonly site = input.required<SiteDto>();
  readonly close = output<void>();
  readonly purchased = output<void>();

  protected readonly activeTab = signal<Tab>('links');
  protected readonly selectedProjectId = signal<number | null>(null);
  protected readonly isNoLinks = signal(false);
  protected readonly links = signal<LinkRow[]>([{ text: '', url: '' }]);
  protected readonly taskDescription = signal('');
  protected readonly insuranceType = signal<InsuranceType>('None');
  protected readonly checkUniqueness = signal(true);
  protected readonly isUrgent = signal(false);
  protected readonly isExpertArticle = signal(false);
  protected readonly submitting = signal(false);

  protected readonly validLinks = computed(() => this.links().filter((l) => l.text.trim() && l.url.trim()));

  protected readonly basePrice = computed(() => this.site().price);

  protected readonly totalPrice = computed(() => {
    let price = this.basePrice();
    if (price === 0) return 0;

    if (this.isUrgent()) price += this.basePrice() * 0.2;
    if (this.isExpertArticle()) price += this.basePrice() * 0.5;

    return Math.round(price * 1.1);
  });

  protected readonly selectedProjectName = computed(
    () => this.projectsStore.projects().find((p) => p.id === this.selectedProjectId())?.name ?? 'Не выбран',
  );

  protected readonly insuranceLabel = computed(() => INSURANCE_LABELS[this.insuranceType()]);

  protected readonly balance = computed(() => this.userStore.currentUser()?.balance ?? 0);
  protected readonly hasEnoughBalance = computed(() => this.balance() >= this.totalPrice());

  protected readonly linksSummary = computed(() =>
    this.isNoLinks() ? 'без ссылок' : `${this.validLinks().length} шт.`,
  );

  ngOnInit(): void {
    if (this.projectsStore.projects().length === 0) {
      void this.projectsStore.fetchProjects(1, 100);
    }
  }

  setTab(tab: Tab): void {
    this.activeTab.set(tab);
  }

  private updateLink(index: number, field: keyof LinkRow, value: string): void {
    this.links.update((rows) => rows.map((row, i) => (i === index ? { ...row, [field]: value } : row)));
  }

  addLink(): void {
    this.links.update((rows) => [...rows, { text: '', url: '' }]);
  }

  removeLink(index: number): void {
    if (this.links().length > 1) {
      this.links.update((rows) => rows.filter((_, i) => i !== index));
    }
  }

  onProjectChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    this.selectedProjectId.set(value ? Number(value) : null);
  }

  onNoLinksChange(event: Event): void {
    this.isNoLinks.set((event.target as HTMLInputElement).checked);
  }

  onLinkTextInput(index: number, event: Event): void {
    this.updateLink(index, 'text', (event.target as HTMLInputElement).value);
  }

  onLinkUrlInput(index: number, event: Event): void {
    this.updateLink(index, 'url', (event.target as HTMLInputElement).value);
  }

  onTaskDescriptionInput(event: Event): void {
    this.taskDescription.set((event.target as HTMLTextAreaElement).value);
  }

  onInsuranceChange(value: InsuranceType): void {
    this.insuranceType.set(value);
  }

  onCheckUniquenessChange(event: Event): void {
    this.checkUniqueness.set((event.target as HTMLInputElement).checked);
  }

  onUrgentChange(event: Event): void {
    this.isUrgent.set((event.target as HTMLInputElement).checked);
  }

  onExpertArticleChange(event: Event): void {
    this.isExpertArticle.set((event.target as HTMLInputElement).checked);
  }

  async handleOrder(): Promise<void> {
    const projectId = this.selectedProjectId();
    if (!projectId) {
      this.toastService.notify('Выберите проект для размещения', 'error');
      return;
    }

    if (!this.isNoLinks() && this.validLinks().length === 0) {
      this.toastService.notify('Добавьте хотя бы одну ссылку или выберите "без ссылок"', 'error');
      return;
    }

    this.submitting.set(true);
    try {
      await this.sitesStore.requestPublication(this.site().id, {
        projectId,
        hasLinks: !this.isNoLinks(),
        links: this.isNoLinks() ? [] : this.validLinks(),
        taskDescription: this.taskDescription(),
        priceFinal: this.totalPrice(),
        paymentSettings: {
          insuranceType: this.insuranceType(),
          checkUniqueness: this.checkUniqueness(),
          isUrgent: this.isUrgent(),
          isExpertArticle: this.isExpertArticle(),
        },
      });

      this.toastService.notify('Заказ успешно отправлен в работу!', 'success');
      this.userStore.setBalance(await this.walletApi.getBalance());
      this.purchased.emit();
      this.close.emit();
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Ошибка отправки заявки'), 'error');
    } finally {
      this.submitting.set(false);
    }
  }
}
