export interface PageMeta {
  currentPage: number;
  lastPage: number;
  perPage: number;
  total: number;
}

// Shape matches Application/Common/PagedResult.cs exactly.
export interface PagedResult<T> extends PageMeta {
  items: T[];
}
