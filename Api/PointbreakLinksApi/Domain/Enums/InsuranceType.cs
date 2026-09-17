namespace Domain.Enums;

// Ported from payment_settings.insurance_type (Laravel stored this as a free string, validated
// in RequestPublicationRequest as in:none,loss_protection,full).
// An enum is safe here because, unlike Role/Status/Topic, this set is not admin-editable in the UI.
public enum InsuranceType
{
    None = 0,
    LossProtection = 1,
    Full = 2,
}
