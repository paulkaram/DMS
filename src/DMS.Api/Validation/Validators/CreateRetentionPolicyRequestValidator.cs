using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CreateRetentionPolicyRequestValidator : AbstractValidator<CreateRetentionPolicyRequest>
{
    public CreateRetentionPolicyRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Policy name is required")
            .MaximumLength(255).WithMessage("Policy name must not exceed 255 characters");
        RuleFor(x => x.RetentionDays).GreaterThan(0).WithMessage("Retention days must be greater than 0");
        RuleFor(x => x.ExpirationAction).NotEmpty().WithMessage("Expiration action is required");
        RuleFor(x => x.NotificationDays)
            .GreaterThan(0).When(x => x.NotifyBeforeExpiration)
            .WithMessage("Notification days must be greater than 0 when notifications are enabled");
    }
}
