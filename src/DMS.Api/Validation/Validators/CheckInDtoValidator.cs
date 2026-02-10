using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CheckInDtoValidator : AbstractValidator<CheckInDto>
{
    public CheckInDtoValidator()
    {
        RuleFor(x => x.CheckInType).IsInEnum().WithMessage("Invalid check-in type");
        RuleFor(x => x.Comment).MaximumLength(1000).WithMessage("Comment must not exceed 1000 characters");
        RuleFor(x => x.ChangeDescription).MaximumLength(2000).WithMessage("Change description must not exceed 2000 characters");
    }
}
