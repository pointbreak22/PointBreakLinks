import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { Header } from '../../shared/layout/header/header';
import { Sidebar } from '../../shared/layout/sidebar/sidebar';
import { Pagination } from '../../shared/pagination/pagination';
import { ModerationLogStore } from '../../stores/moderation-log.store';

// New page — surfaces ModerationAuditEntry rows recorded by Approve/RejectSiteCommandHandler
// (see their comments) so a moderation decision has a "who and when" behind it, not just the
// site's current StatusId.
@Component({
  selector: 'app-moderation-log',
  imports: [Header, Sidebar, Pagination],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './moderation-log.html',
})
export class ModerationLog implements OnInit {
  protected readonly logStore = inject(ModerationLogStore);

  ngOnInit(): void {
    void this.logStore.fetchAuditLog();
  }

  changePage(page: number): void {
    void this.logStore.fetchAuditLog(page);
  }
}
