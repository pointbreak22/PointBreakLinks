import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { extractErrorMessage } from '../../../core/http/api-error';
import { ToastService } from '../../../core/notifications/toast.service';
import { TopicDto } from '../../../core/models/site.model';
import { ApiEndpoints } from '../../../core/http/api-endpoints';
import { SitesApiService } from '../../../services/sites-api.service';
import { SitesStore } from '../../../stores/sites.store';

// Ported from FOXLinks' modal-windows/webmaster/add-edit-site-modal.vue. Only the fields the
// source form actually exposes (url/topic/description/price/iks) — dr/traffic/country aren't
// editable here either, matching the source exactly.
@Component({
  selector: 'app-add-edit-site-modal',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './add-edit-site-modal.html',
})
export class AddEditSiteModal implements OnInit {
  private readonly sitesApi = inject(SitesApiService);
  private readonly sitesStore = inject(SitesStore);
  private readonly toastService = inject(ToastService);

  readonly siteId = input<number | null>(null);
  readonly close = output<void>();
  readonly saved = output<void>();

  protected readonly isEdit = signal(false);
  protected readonly loading = signal(false);
  protected readonly topics = signal<TopicDto[]>([]);

  // Screenshot management only applies once a site exists (upload needs an id to attach to) —
  // never shown on the create form. screenshotVersion busts the <img> cache after a re-upload,
  // since the URL is otherwise identical (same site id) and Angular wouldn't know to refetch it.
  protected readonly hasScreenshot = signal(false);
  protected readonly screenshotVersion = signal(0);
  protected readonly uploadingScreenshot = signal(false);

  protected screenshotUrl(): string | null {
    const id = this.siteId();
    return id === null ? null : `${ApiEndpoints.sites.screenshot(id)}?v=${this.screenshotVersion()}`;
  }

  readonly form = new FormGroup({
    url: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    topicId: new FormControl(0, { nonNullable: true, validators: [Validators.required] }),
    description: new FormControl('', { nonNullable: true }),
    price: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
    iks: new FormControl(0, { nonNullable: true, validators: [Validators.required, Validators.min(0)] }),
  });

  async ngOnInit(): Promise<void> {
    this.topics.set(await this.sitesApi.getTopics());
    await this.loadForEditOrReset();
  }

  private async loadForEditOrReset(): Promise<void> {
    const id = this.siteId();
    this.isEdit.set(id !== null);

    if (id === null) {
      this.form.reset({
        url: '',
        topicId: this.topics()[0]?.id ?? 0,
        description: '',
        price: 0,
        iks: 0,
      });
      return;
    }

    this.loading.set(true);
    try {
      const site = await this.sitesApi.getSite(id);
      this.form.reset({
        url: site.url,
        topicId: site.topicId,
        description: site.description ?? '',
        price: site.price,
        iks: site.iks,
      });
      this.hasScreenshot.set(site.hasScreenshot);
      this.screenshotVersion.set(Date.now());
    } finally {
      this.loading.set(false);
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    try {
      const id = this.siteId();
      if (id !== null) {
        await this.sitesStore.updateSite(id, this.form.getRawValue());
      } else {
        await this.sitesStore.createSite(this.form.getRawValue());
      }
      this.saved.emit();
      this.close.emit();
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Ошибка сохранения площадки'), 'error');
    } finally {
      this.loading.set(false);
    }
  }

  async onScreenshotSelected(event: Event): Promise<void> {
    const id = this.siteId();
    const file = (event.target as HTMLInputElement).files?.[0];
    if (id === null || !file) return;

    this.uploadingScreenshot.set(true);
    try {
      await this.sitesApi.uploadScreenshot(id, file);
      this.hasScreenshot.set(true);
      this.screenshotVersion.set(Date.now());
      this.toastService.notify('Скриншот загружен', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось загрузить скриншот'), 'error');
    } finally {
      this.uploadingScreenshot.set(false);
      (event.target as HTMLInputElement).value = '';
    }
  }

  async removeScreenshot(): Promise<void> {
    const id = this.siteId();
    if (id === null) return;

    try {
      await this.sitesApi.deleteScreenshot(id);
      this.hasScreenshot.set(false);
      this.toastService.notify('Скриншот удалён', 'success');
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Не удалось удалить скриншот'), 'error');
    }
  }
}
