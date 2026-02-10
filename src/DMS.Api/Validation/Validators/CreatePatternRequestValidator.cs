using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CreatePatternRequestValidator : AbstractValidator<CreatePatternRequest>
{
    public CreatePatternRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Pattern name is required")
            .MaximumLength(255).WithMessage("Pattern name must not exceed 255 characters");
        RuleFor(x => x.Regex).NotEmpty().WithMessage("Regex pattern is required");
        RuleFor(x => x.PatternType).NotEmpty().WithMessage("Pattern type is required");
        RuleFor(x => x.Priority).GreaterThanOrEqualTo(0).WithMessage("Priority must be non-negative");
    }
}
