export interface MonthlyPointDto {
  month: string;
  value: number;
}

export interface StatusBreakdownDto {
  status: string;
  description: string;
  count: number;
}

export interface ProjectSummaryDto {
  id: number;
  name: string;
  spent: number;
  ordersCount: number;
  linksPosted: number;
}

// Shape matches Application/CQRS/Analytics/DTOs/AnalyticsDto.cs exactly.
export interface AnalyticsDto {
  totalSpent: number;
  totalEarned: number;
  activeProjects: number;
  activeSites: number;
  spendByMonth: MonthlyPointDto[];
  earnedByMonth: MonthlyPointDto[];
  ordersByStatus: StatusBreakdownDto[];
  projects: ProjectSummaryDto[];
}
