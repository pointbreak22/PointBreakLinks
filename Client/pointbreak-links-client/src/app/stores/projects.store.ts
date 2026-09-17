import { Injectable, inject, signal } from '@angular/core';
import { ProjectsApiService } from '../services/projects-api.service';
import { PageMeta } from '../core/models/pagination.model';
import { ProjectDto, ProjectFormValue } from '../core/models/project.model';

const emptyMeta: PageMeta = { currentPage: 1, lastPage: 1, perPage: 15, total: 0 };

// Ported from FOXLinks' stores/projects.ts (Pinia).
@Injectable({ providedIn: 'root' })
export class ProjectsStore {
  private readonly api = inject(ProjectsApiService);

  private readonly _projects = signal<ProjectDto[]>([]);
  private readonly _meta = signal<PageMeta>(emptyMeta);
  private readonly _currentProject = signal<ProjectDto | null>(null);
  private readonly _loading = signal(false);

  readonly projects = this._projects.asReadonly();
  readonly meta = this._meta.asReadonly();
  readonly currentProject = this._currentProject.asReadonly();
  readonly loading = this._loading.asReadonly();

  async fetchProjects(page = 1, perPage?: number): Promise<void> {
    this._loading.set(true);
    try {
      const response = await this.api.getMyProjects(page, perPage ?? this._meta().perPage);
      this._projects.set(response.items);
      this._meta.set(response);
    } finally {
      this._loading.set(false);
    }
  }

  async fetchProject(id: number): Promise<void> {
    this._currentProject.set(await this.api.getProject(id));
  }

  clearCurrentProject(): void {
    this._currentProject.set(null);
  }

  async createProject(payload: ProjectFormValue): Promise<void> {
    await this.api.createProject(payload);
    await this.fetchProjects(this._meta().currentPage);
  }

  async updateProject(id: number, payload: ProjectFormValue): Promise<void> {
    await this.api.updateProject(id, payload);
    await this.fetchProjects(this._meta().currentPage);
  }

  async deleteProject(id: number): Promise<void> {
    this._loading.set(true);
    try {
      await this.api.deleteProject(id);
      await this.fetchProjects(this._meta().currentPage);
    } finally {
      this._loading.set(false);
    }
  }
}
