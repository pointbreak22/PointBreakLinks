import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { PagedResult } from '../core/models/pagination.model';
import { ProjectDto, ProjectFormValue } from '../core/models/project.model';

// Ported from FOXLinks' services/projects.service.ts.
@Injectable({ providedIn: 'root' })
export class ProjectsApiService {
  private readonly http = inject(HttpClient);

  getMyProjects(page: number, perPage = 15): Promise<PagedResult<ProjectDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<ProjectDto>>(ApiEndpoints.projects.mine, { params }));
  }

  async getProject(id: number): Promise<ProjectDto> {
    const response = await firstValueFrom(this.http.get<{ data: ProjectDto }>(ApiEndpoints.projects.byId(id)));
    return response.data;
  }

  createProject(payload: ProjectFormValue): Promise<ProjectDto> {
    return firstValueFrom(this.http.post<ProjectDto>(ApiEndpoints.projects.mine, payload));
  }

  updateProject(id: number, payload: ProjectFormValue): Promise<ProjectDto> {
    return firstValueFrom(this.http.put<ProjectDto>(ApiEndpoints.projects.byId(id), payload));
  }

  deleteProject(id: number): Promise<void> {
    return firstValueFrom(this.http.delete<void>(ApiEndpoints.projects.byId(id)));
  }
}
