using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class ScanProcessRequestValidator : AbstractValidator<ScanProcessRequest>
{
    public ScanProcessRequestValidator()
    {
        RuleFor(x => x.TargetFolderId).NotEmpty().WithMessage("Target folder ID is required");
        RuleFor(x => x.DocumentName).NotEmpty().WithMessage("Document name is required")
            .MaximumLength(255).WithMessage("Document name must not exceed 255 characters");
        RuleFor(x => x.CompressionQuality).InclusiveBetween(1, 100).WithMessage("Compression quality must be between 1 and 100");
    }
}
