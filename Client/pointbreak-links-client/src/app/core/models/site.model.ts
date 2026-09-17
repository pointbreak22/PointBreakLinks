// Shapes match Application/CQRS/Sites/DTOs/*.cs exactly (ASP.NET Core serializes to camelCase
// by default) — keep both sides in sync when the API DTOs change.

export interface CountryDto {
  id: number;
  code: string;
  name: string;
}

export interface StatusDto {
  name: string;
  description: string;
}

export interface TopicDto {
  id: number;
  name: string;
}

export interface SiteDto {
  id: number;
  url: string;
  iks: number;
  dr: number;
  traffic: number;
  country: CountryDto | null;
  countryId: number | null;
  price: number;
  description: string | null;
  topic: string;
  topicId: number;
  status: StatusDto;
  userIdSales: number;
  // Empty on responses that embed a Site without loading its Seller navigation (order/dispute
  // flows) — see SiteDto.FromEntity's own comment. Always populated on catalog/my-sites/get-by-id.
  sellerName: string;
  soldCount: number;
  updatedAt: string;
  isVerified: boolean;
  averageRating: number | null;
  reviewsCount: number;
  verificationToken: string;
  // Only meaningful to the owner (pause/resume) — the catalog never returns a false one.
  isActive: boolean;
  // The actual image is served by GET /sites/{id}/screenshot (SitesApiService.screenshotUrl) —
  // this just says whether it's worth requesting one.
  hasScreenshot: boolean;
}

// Shape matches Application/CQRS/Sellers/DTOs/SellerProfileDto.cs.
export interface SellerProfileDto {
  id: number;
  name: string;
  memberSince: string;
  averageRating: number | null;
  reviewsCount: number;
  activeSites: SiteDto[];
}

export type SiteSortBy = 'price' | 'iks' | 'dr' | 'traffic';

export interface SiteCatalogFilter {
  topicId?: number;
  countryId?: number;
  minPrice?: number;
  maxPrice?: number;
  minIks?: number;
  minDr?: number;
  sortBy?: SiteSortBy;
  sortDescending?: boolean;
}

export interface SiteReviewDto {
  id: number;
  buyerName: string;
  rating: number;
  comment: string | null;
  createdAt: string;
  sellerReply: string | null;
  sellerRepliedAt: string | null;
}

export interface SiteFormValue {
  url: string;
  topicId: number;
  description: string | null;
  price: number;
  iks: number;
  dr?: number;
  traffic?: number;
  countryId?: number | null;
}

export type InsuranceType = 'None' | 'LossProtection' | 'Full';

export interface PaymentSettingDto {
  insuranceType: InsuranceType;
  checkUniqueness: boolean;
  isUrgent: boolean;
  isExpertArticle: boolean;
}

export interface LinkDto {
  query: string;
  url: string;
}

export interface BuyerDto {
  id: number;
  name: string;
}

export interface LastMessageDto {
  text: string;
  createdAt: string;
  isUnread: boolean;
}

export interface RequestPublicationLink {
  text: string;
  url: string;
}

export interface RequestPublicationPayload {
  projectId: number;
  hasLinks: boolean;
  links: RequestPublicationLink[];
  taskDescription: string;
  priceFinal: number;
  paymentSettings: PaymentSettingDto;
}

export interface PurchasedSiteDto {
  id: number;
  projectId: number;
  site: SiteDto;
  priceFinal: number;
  status: StatusDto;
  taskDescription: string | null;
  links: LinkDto[];
  paymentSettings: PaymentSettingDto | null;
  buyer: BuyerDto;
  flHasLinks: boolean;
  isPublished: boolean;
  updatedAt: string;
  lastMessage: LastMessageDto | null;
  hasReview: boolean;
  isDisputed: boolean;
}

// "work" is the raw StatusId for every accepted order from the moment the seller accepts it
// until it's cancelled/rejected — it never changes on its own again (see StatusNames.cs's
// comment: OpenDisputeCommandHandler gates on StatusId === "work", so publishing/reviewing
// deliberately never advances the real status past it, or a completed order could no longer be
// disputed). That leaves a published-and-reviewed order showing the same "в работе" badge as one
// that's barely started — this derives a display-only label from isPublished/isDisputed/hasReview
// without touching the underlying status at all. Used by both my-sales.html (seller) and
// project-details.html (buyer) so the two order lists agree on what "done" looks like.
export interface OrderStatusLabel {
  text: string;
  cssClass: string;
}

export function getOrderStatusLabel(order: Pick<PurchasedSiteDto, 'status' | 'isPublished' | 'isDisputed' | 'hasReview'>): OrderStatusLabel {
  if (order.isDisputed) {
    return { text: 'Спор открыт', cssClass: 'disputed' };
  }
  if (order.status.name === 'work' && order.isPublished) {
    return order.hasReview ? { text: 'Завершён', cssClass: 'completed' } : { text: 'Опубликовано', cssClass: 'published' };
  }
  return { text: order.status.description, cssClass: order.status.name };
}

export interface PurchasedSiteEventDto {
  id: number;
  description: string;
  createdAt: string;
}

// Shape matches Application/CQRS/SavedSearches/DTOs/SavedSearchDto.cs.
export interface SavedSearchDto {
  id: number;
  topicId: number | null;
  topicName: string | null;
  countryId: number | null;
  countryName: string | null;
  minPrice: number | null;
  maxPrice: number | null;
  minIks: number | null;
  minDr: number | null;
  createdAt: string;
}
