import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Header } from '../../shared/layout/header/header';
import { Sidebar } from '../../shared/layout/sidebar/sidebar';
import { SidebarService } from '../../core/layout/sidebar.service';
import { ProjectDto } from '../../core/models/project.model';
import { ProjectsApiService } from '../../services/projects-api.service';
import { ToastService } from '../../core/notifications/toast.service';

// FOXLinks' position.vue renders a full SERP rank-tracking dashboard — visibility %, average
// position, a 6-bucket position-distribution row, an 18-row keyword table — entirely on
// hardcoded numbers (no `v-for`, literal `<td>` text; see PROJECT_MAP.md). There's no keyword,
// SERP-snapshot, or rank-history table anywhere in FOXLinks' backend or this app's domain, and
// no third-party rank-checking integration exists to source one — this isn't a porting gap,
// it's a feature FOXLinks' own UI describes but never actually built. Kept as a visual layout
// (per explicit request to build all four "coming soon" pages rather than skip the unbuildable
// ones) but every number here is an honest "—", not a copy of FOXLinks' invented figures — only
// the project selector is real, since Project rows genuinely exist.
@Component({
  selector: 'app-position',
  imports: [Header, Sidebar],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './position.html',
})
export class Position implements OnInit {
  private readonly projectsApi = inject(ProjectsApiService);
  private readonly toastService = inject(ToastService);
  protected readonly sidebarService = inject(SidebarService);

  protected readonly projects = signal<ProjectDto[]>([]);

  ngOnInit(): void {
    void this.loadProjects();
  }

  private async loadProjects(): Promise<void> {
    const response = await this.projectsApi.getMyProjects(1, 100);
    this.projects.set(response.items);
  }

  notifyUnavailable(): void {
    this.toastService.notify('Мониторинг позиций не подключён — нет интеграции с сервисом отслеживания позиций.', 'info');
  }
}
