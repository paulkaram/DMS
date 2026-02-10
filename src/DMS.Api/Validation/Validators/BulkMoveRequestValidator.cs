using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class BulkMoveRequestValidator : AbstractValidator<BulkMoveRequest>
{
    public BulkMoveRequestValidator()
    {
        RuleFor(x => x.DocumentIds).NotEmpty().WithMessage("At least one document ID is required");
        RuleFor(x => x.TargetFolderId).NotEmpty().WithMessage("Target folder ID is required");
    }
}
