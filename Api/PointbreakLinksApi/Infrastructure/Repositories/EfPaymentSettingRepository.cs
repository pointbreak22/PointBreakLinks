using Domain.Entities;
using Domain.Enums;
using Domain.Repositories;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class EfPaymentSettingRepository(ApplicationDbContext db) : IPaymentSettingRepository
{
    public async Task<PaymentSetting> GetOrCreateAsync(
        InsuranceType insuranceType,
        bool checkUniqueness,
        bool isUrgent,
        bool isExpertArticle,
        CancellationToken cancellationToken = default)
    {
        // Only ever read back for its Id (assigned to PurchasedSite.PaymentSettingId) — never
        // mutated after the dedup lookup, so this can stay untracked.
        var existing = await db.PaymentSettings.AsNoTracking().FirstOrDefaultAsync(
            p => p.InsuranceType == insuranceType
                 && p.CheckUniqueness == checkUniqueness
                 && p.IsUrgent == isUrgent
                 && p.IsExpertArticle == isExpertArticle,
            cancellationToken);
        if (existing != null) return existing;

        var created = new PaymentSetting
        {
            InsuranceType = insuranceType,
            CheckUniqueness = checkUniqueness,
            IsUrgent = isUrgent,
            IsExpertArticle = isExpertArticle,
        };
        await db.PaymentSettings.AddAsync(created, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return created;
    }
}
