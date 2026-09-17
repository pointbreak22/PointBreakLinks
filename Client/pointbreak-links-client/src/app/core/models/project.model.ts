// Shape matches Application/CQRS/Projects/DTOs/ProjectDto.cs (used for both list and details,
// same as FOXLinks' single ProjectResource).
export interface ProjectDto {
  id: number;
  name: string;
  type: string;
  url: string | null;
  flLossInsurance: boolean;
  flLossAndIndexationInsurance: boolean;
  taskForVm: string;
  totalNumberOfLinks: number;
  numberOfLinksPosted: number;
  numberOfFrozenPosted: number;
  spentMoney: number;
}

export interface ProjectFormValue {
  name: string;
  url: string | null;
  taskForVm: string;
  flLossInsurance: boolean;
  flLossAndIndexationInsurance: boolean;
}
