import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Header } from '../../../shared/layout/header/header';
import { Sidebar } from '../../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../../shared/pagination/pagination';
import { AdminStore } from '../../../stores/admin.store';

// Backs the admin sidebar's "Проекты" link (previously a dead <li>, no link at all). Every
// project on the platform, not scoped to one owner — read-only, no admin action exists on a
// project (there's nothing analogous to ban/role-change for a project).
@Component({
  selector: 'app-admin-projects',
  imports: [Header, Sidebar, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './admin-projects.html',
})
export class AdminProjects implements OnInit {
  protected readonly adminStore = inject(AdminStore);

  ngOnInit(): void {
    void this.adminStore.fetchProjects();
  }

  changePage(page: number): void {
    void this.adminStore.fetchProjects(page);
  }
}
