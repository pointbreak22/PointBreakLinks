import { ChangeDetectionStrategy, Component, OnInit, inject, input, output, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators } from '@angular/forms';
import { extractErrorMessage } from '../../../core/http/api-error';
import { ToastService } from '../../../core/notifications/toast.service';
import { ProjectsStore } from '../../../stores/projects.store';

// Ported from FOXLinks' modal-windows/project/create-edit-project-modal.vue. `type` isn't a
// form field — the source always sends the literal "folder" too, see
// Application/CQRS/Projects/Commands/CreateProject/CreateProjectCommandHandler.cs.
@Component({
  selector: 'app-create-edit-project-modal',
  imports: [ReactiveFormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './create-edit-project-modal.html',
})
export class CreateEditProjectModal implements OnInit {
  private readonly projectsStore = inject(ProjectsStore);
  private readonly toastService = inject(ToastService);

  readonly projectId = input<number | null>(null);
  readonly close = output<void>();

  protected readonly isEdit = signal(false);
  protected readonly submitting = signal(false);

  readonly form = new FormGroup({
    name: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    url: new FormControl('', { nonNullable: true }),
    taskForVm: new FormControl('', { nonNullable: true }),
    flLossInsurance: new FormControl(false, { nonNullable: true }),
    flLossAndIndexationInsurance: new FormControl(false, { nonNullable: true }),
  });

  async ngOnInit(): Promise<void> {
    const id = this.projectId();
    this.isEdit.set(id !== null);

    if (id === null) {
      this.projectsStore.clearCurrentProject();
      return;
    }

    try {
      await this.projectsStore.fetchProject(id);
      const project = this.projectsStore.currentProject();
      if (project) {
        this.form.reset({
          name: project.name,
          url: project.url ?? '',
          taskForVm: project.taskForVm,
          flLossInsurance: project.flLossInsurance,
          flLossAndIndexationInsurance: project.flLossAndIndexationInsurance,
        });
      }
    } catch {
      this.toastService.notify('Ошибка загрузки данных проекта', 'error');
    }
  }

  async submit(): Promise<void> {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    try {
      const { name, url, taskForVm, flLossInsurance, flLossAndIndexationInsurance } = this.form.getRawValue();
      // Same normalization as the source: a bare domain typed in gets an https:// prefix
      // rather than being rejected outright.
      const normalizedUrl = url && !url.includes('://') ? `https://${url}` : url || null;
      const payload = { name, url: normalizedUrl, taskForVm, flLossInsurance, flLossAndIndexationInsurance };

      const id = this.projectId();
      if (id !== null) {
        await this.projectsStore.updateProject(id, payload);
      } else {
        await this.projectsStore.createProject(payload);
      }
      this.close.emit();
    } catch (error) {
      this.toastService.notify(extractErrorMessage(error, 'Ошибка сохранения проекта'), 'error');
    } finally {
      this.submitting.set(false);
    }
  }
}
