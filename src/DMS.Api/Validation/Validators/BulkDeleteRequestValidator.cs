using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class BulkDeleteRequestValidator : AbstractValidator<BulkDeleteRequest>
{
    public BulkDeleteRequestValidator()
    {
        RuleFor(x => x.DocumentIds).NotEmpty().WithMessage("At least one document ID is required");
    }
}
