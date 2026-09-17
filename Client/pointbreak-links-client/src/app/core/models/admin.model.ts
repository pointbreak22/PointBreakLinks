// Shape matches Application/CQRS/Admin/DTOs/AdminUserDto.cs exactly.

export interface AdminRoleDto {
  name: string;
  displayName: string;
}

export interface AdminUserDto {
  id: number;
  name: string;
  email: string;
  roles: AdminRoleDto[];
  projectsCount: number;
  isBanned: boolean;
  createdAt: string;
  // Set only while a LoginCommandHandler brute-force lockout is still in effect.
  lockedUntil: string | null;
}

export interface AdminRoleCountDto {
  name: string;
  displayName: string;
  count: number;
}

export interface AdminRecentUserDto {
  id: number;
  name: string;
  email: string;
  createdAt: string;
}

export interface AdminRecentOrderDto {
  id: number;
  siteUrl: string;
  buyerName: string;
  finalPrice: number;
  statusDescription: string;
  createdAt: string;
}

export interface AdminProjectDto {
  id: number;
  name: string;
  ownerName: string;
  type: string;
  totalLinks: number;
  linksPosted: number;
  spentMoney: number;
  createdAt: string;
}

export interface AdminTransactionDto {
  id: number;
  userName: string;
  type: string;
  amount: number;
  description: string;
  createdAt: string;
}

// Pending-only admin queue row — see IWithdrawalRequestRepository.GetPendingAsync.
export interface AdminWithdrawalRequestDto {
  id: number;
  userName: string;
  userEmail: string;
  amount: number;
  payoutDetails: string;
  requestedAt: string;
}

export interface DisputedOrderDto {
  id: number;
  siteUrl: string;
  buyerName: string;
  sellerName: string;
  finalPrice: number;
  reason: string;
  updatedAt: string;
}

export interface AdminSystemLogDto {
  id: number;
  purchasedSiteId: number;
  siteUrl: string;
  description: string;
  createdAt: string;
}

export interface AdminDashboardDto {
  totalUsers: number;
  totalProjects: number;
  totalActiveSites: number;
  totalOrders: number;
  totalRevenue: number;
  pendingModerationCount: number;
  usersByRole: AdminRoleCountDto[];
  recentUsers: AdminRecentUserDto[];
  recentOrders: AdminRecentOrderDto[];
}
