using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CreateCabinetDtoValidator : AbstractValidator<CreateCabinetDto>
{
    public CreateCabinetDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Cabinet name is required")
            .MaximumLength(255).WithMessage("Cabinet name must not exceed 255 characters");
    }
}
