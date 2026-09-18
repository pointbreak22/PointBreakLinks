import { TestBed } from '@angular/core/testing';
import { AdminStore } from './admin.store';
import { AdminApiService } from '../services/admin-api.service';
import { AdminUserDto, AdminWithdrawalRequestDto, DisputedOrderDto } from '../core/models/admin.model';
import { PagedResult } from '../core/models/pagination.model';

function pagedResult<T>(items: T[]): PagedResult<T> {
  return { items, currentPage: 1, lastPage: 1, perPage: 15, total: items.length };
}

function createStore(apiOverrides: Partial<AdminApiService> = {}): { store: AdminStore; api: AdminApiService } {
  const api = {
    getUsers: vi.fn().mockResolvedValue(pagedResult<AdminUserDto>([])),
    getDisputes: vi.fn().mockResolvedValue(pagedResult<DisputedOrderDto>([])),
    getWithdrawals: vi.fn().mockResolvedValue(pagedResult<AdminWithdrawalRequestDto>([])),
    resolveDispute: vi.fn().mockResolvedValue(undefined),
    approveWithdrawal: vi.fn().mockResolvedValue(undefined),
    rejectWithdrawal: vi.fn().mockResolvedValue(undefined),
    ...apiOverrides,
  } as unknown as AdminApiService;

  TestBed.configureTestingModule({ providers: [AdminStore, { provide: AdminApiService, useValue: api }] });
  return { store: TestBed.inject(AdminStore), api };
}

describe('AdminStore', () => {
  // A stale currentPage from a previous, differently-filtered result set could otherwise ask
  // the server for a page number that doesn't exist under the new filter (see the store's own
  // comment on setUsersSearch/setUsersRole).
  it('setUsersSearch resets to page 1 when refetching', async () => {
    const { store, api } = createStore();

    await store.setUsersSearch('Иван');

    expect(api.getUsers).toHaveBeenCalledWith(1, expect.anything(), 'Иван', expect.anything());
  });

  it('setUsersRole resets to page 1 when refetching', async () => {
    const { store, api } = createStore();

    await store.setUsersRole('moderator');

    expect(api.getUsers).toHaveBeenCalledWith(1, expect.anything(), expect.anything(), 'moderator');
  });

  // Disputes/withdrawals are removed from the local list once resolved rather than requiring a
  // full refetch — these tests guard that the filter targets the right id and doesn't touch
  // unrelated rows.
  it('resolveDispute removes only the resolved dispute from the local list', async () => {
    const { store } = createStore({
      getDisputes: vi.fn().mockResolvedValue(
        pagedResult<DisputedOrderDto>([
          { id: 1, siteUrl: 'a.ru', buyerName: 'A', sellerName: 'B', finalPrice: 100, reason: 'x', updatedAt: '' },
          { id: 2, siteUrl: 'b.ru', buyerName: 'C', sellerName: 'D', finalPrice: 200, reason: 'y', updatedAt: '' },
        ]),
      ),
    });
    await store.fetchDisputes();

    await store.resolveDispute(1, true);

    expect(store.disputes().map((d) => d.id)).toEqual([2]);
  });

  it('approveWithdrawal removes only the approved withdrawal from the local list', async () => {
    const { store } = createStore({
      getWithdrawals: vi.fn().mockResolvedValue(
        pagedResult<AdminWithdrawalRequestDto>([
          { id: 1, userName: 'A', userEmail: 'a@x.com', amount: 100, payoutDetails: '', requestedAt: '' },
          { id: 2, userName: 'B', userEmail: 'b@x.com', amount: 200, payoutDetails: '', requestedAt: '' },
        ]),
      ),
    });
    await store.fetchWithdrawals();

    await store.approveWithdrawal(2);

    expect(store.withdrawals().map((w) => w.id)).toEqual([1]);
  });
});
