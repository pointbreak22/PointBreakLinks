// Shape matches Application/CQRS/Stats/DTOs/DynamicStatDto.cs.
export interface StatTrendDto {
  type: 'up' | 'down' | null;
  prefix: string | null;
  value: string | null;
  suffix: string | null;
  text: string | null;
}

export interface DynamicStatDto {
  id: number;
  pageKey: string;
  position: number;
  title: string;
  value: string;
  valueSuffix: string | null;
  trend: StatTrendDto;
}
