import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { ApiEndpoints } from '../core/http/api-endpoints';
import { SupportMessageDto, SupportTicketSummaryDto } from '../core/models/support.model';
import { PagedResult } from '../core/models/pagination.model';

@Injectable({ providedIn: 'root' })
export class SupportApiService {
  private readonly http = inject(HttpClient);

  getMyThread(): Promise<SupportMessageDto[]> {
    return firstValueFrom(this.http.get<SupportMessageDto[]>(ApiEndpoints.support.myThread));
  }

  sendMyMessage(text: string): Promise<SupportMessageDto> {
    return firstValueFrom(this.http.post<SupportMessageDto>(ApiEndpoints.support.myThreadMessages, { text }));
  }

  getTickets(page = 1, perPage = 15): Promise<PagedResult<SupportTicketSummaryDto>> {
    const params = new HttpParams().set('page', page).set('perPage', perPage);
    return firstValueFrom(this.http.get<PagedResult<SupportTicketSummaryDto>>(ApiEndpoints.support.tickets, { params }));
  }

  getTicketMessages(ticketId: number): Promise<SupportMessageDto[]> {
    return firstValueFrom(this.http.get<SupportMessageDto[]>(ApiEndpoints.support.ticketMessages(ticketId)));
  }

  replyToTicket(ticketId: number, text: string): Promise<SupportMessageDto> {
    return firstValueFrom(this.http.post<SupportMessageDto>(ApiEndpoints.support.ticketMessages(ticketId), { text }));
  }
}
