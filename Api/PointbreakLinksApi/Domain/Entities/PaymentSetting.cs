using Domain.Common;
using Domain.Enums;

namespace Domain.Entities;

public class PaymentSetting : BaseEntity
{
    public InsuranceType InsuranceType { get; set; } = InsuranceType.None;
    public bool CheckUniqueness { get; set; }
    public bool IsUrgent { get; set; }
    public bool IsExpertArticle { get; set; }
}
