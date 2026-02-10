using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CreateFolderDtoValidator : AbstractValidator<CreateFolderDto>
{
    public CreateFolderDtoValidator()
    {
        RuleFor(x => x.CabinetId).NotEmpty().WithMessage("Cabinet ID is required");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Folder name is required")
            .MaximumLength(255).WithMessage("Folder name must not exceed 255 characters");
    }
}
