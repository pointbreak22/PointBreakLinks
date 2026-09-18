import { getOrderStatusLabel } from './site.model';

// The dispute-eligibility constraint is the whole reason this function exists rather than just
// advancing PurchasedSite.StatusId on publish: OpenDisputeCommandHandler (backend) only allows
// opening a dispute while StatusId is still "work", so a completed/published order must keep
// reporting status.name === 'work' underneath — only the DISPLAYED label changes here.
describe('getOrderStatusLabel', () => {
  it('shows "Спор открыт" whenever isDisputed is true, regardless of the underlying status', () => {
    const label = getOrderStatusLabel({
      status: { name: 'work', description: 'в работе' },
      isPublished: true,
      isDisputed: true,
      hasReview: false,
    });

    expect(label).toEqual({ text: 'Спор открыт', cssClass: 'disputed' });
  });

  it('shows "Опубликовано" for a work-status order that is published but not yet reviewed', () => {
    const label = getOrderStatusLabel({
      status: { name: 'work', description: 'в работе' },
      isPublished: true,
      isDisputed: false,
      hasReview: false,
    });

    expect(label).toEqual({ text: 'Опубликовано', cssClass: 'published' });
  });

  it('shows "Завершён" once a published work-status order also has a review', () => {
    const label = getOrderStatusLabel({
      status: { name: 'work', description: 'в работе' },
      isPublished: true,
      isDisputed: false,
      hasReview: true,
    });

    expect(label).toEqual({ text: 'Завершён', cssClass: 'completed' });
  });

  it('falls back to the raw status description for a work-status order that is not yet published', () => {
    const label = getOrderStatusLabel({
      status: { name: 'work', description: 'в работе' },
      isPublished: false,
      isDisputed: false,
      hasReview: false,
    });

    expect(label).toEqual({ text: 'в работе', cssClass: 'work' });
  });

  it('falls back to the raw status for any non-work status (e.g. cancelled)', () => {
    const label = getOrderStatusLabel({
      status: { name: 'cancelled', description: 'Отменена' },
      isPublished: false,
      isDisputed: false,
      hasReview: false,
    });

    expect(label).toEqual({ text: 'Отменена', cssClass: 'cancelled' });
  });
});
