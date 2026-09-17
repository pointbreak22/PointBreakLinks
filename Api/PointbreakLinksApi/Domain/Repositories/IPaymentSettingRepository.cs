using Domain.Entities;
using Domain.Enums;

namespace Domain.Repositories;

// Mirrors FOXLinks' PaymentSetting::firstOrCreate() dedup — the four flag columns together
// identify a reusable settings row, so requests with the same combination share one row
// instead of the table growing one row per order.
public interface IPaymentSettingRepository
{
    Task<PaymentSetting> GetOrCreateAsync(
        InsuranceType insuranceType,
        bool checkUniqueness,
        bool isUrgent,
        bool isExpertArticle,
        CancellationToken cancellationToken = default);
}
