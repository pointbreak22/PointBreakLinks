import { ChangeDetectionStrategy, Component, OnInit, PLATFORM_ID, inject, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { SidebarService } from '../../../core/layout/sidebar.service';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { SmartGrid } from '../../../shared/smart-grid/smart-grid';
import { Pagination } from '../../../shared/pagination/pagination';
import { ToastService } from '../../../core/notifications/toast.service';
import { ProjectsStore } from '../../../stores/projects.store';
import { CreateEditProjectModal } from '../create-edit-project-modal/create-edit-project-modal';

// Ported from FOXLinks' pages/project/project-list.vue. The source also had an "Добавить
// ссылку"/"Экспорт в *.xls"/bulk-selection toolbar and sortable column headers with no click
// handlers behind any of them — dead UI, not ported (see feedback in PROJECT_MAP.md's
// porting notes: don't replicate decoration that never did anything in the source either).
@Component({
  selector: 'app-project-list',
  imports: [Header, Sidebar, SmartGrid, Pagination, CreateEditProjectModal],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './project-list.html',
})
export class ProjectList implements OnInit {
  protected readonly sidebarService = inject(SidebarService);
  protected readonly projectsStore = inject(ProjectsStore);
  private readonly toastService = inject(ToastService);
  private readonly router = inject(Router);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));

  protected readonly modalOpen = signal(false);
  protected readonly editId = signal<number | null>(null);

  ngOnInit(): void {
    void this.projectsStore.fetchProjects(1);
  }

  openCreate(): void {
    this.editId.set(null);
    this.modalOpen.set(true);
  }

  openEdit(id: number): void {
    this.editId.set(id);
    this.modalOpen.set(true);
  }

  changePage(page: number): void {
    void this.projectsStore.fetchProjects(page);
  }

  onPerPageChange(event: Event): void {
    const perPage = Number((event.target as HTMLSelectElement).value);
    void this.projectsStore.fetchProjects(1, perPage);
  }

  async confirmDelete(id: number): Promise<void> {
    if (this.isBrowser && !window.confirm('Вы уверены, что хотите удалить проект?')) return;

    try {
      await this.projectsStore.deleteProject(id);
      this.toastService.notify('Проект удалён', 'success');
    } catch {
      this.toastService.notify('Не удалось удалить проект', 'error');
    }
  }

  goToProject(id: number, name: string): void {
    void this.router.navigate(['/projects', id], { queryParams: { name } });
  }
}
