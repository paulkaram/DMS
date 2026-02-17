using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CreateLinkShareRequestValidator : AbstractValidator<CreateLinkShareRequest>
{
    public CreateLinkShareRequestValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty().WithMessage("Document ID is required");
        RuleFor(x => x.PermissionLevel).InclusiveBetween(1, 2).WithMessage("Permission level must be 1 (Read) or 2 (Write)");
        RuleFor(x => x.ExpiresAt).GreaterThan(DateTime.Now).When(x => x.ExpiresAt.HasValue).WithMessage("Expiry date must be in the future");
    }
}
