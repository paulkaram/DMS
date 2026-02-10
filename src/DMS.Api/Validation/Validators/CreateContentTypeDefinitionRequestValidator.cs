using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CreateContentTypeDefinitionRequestValidator : AbstractValidator<CreateContentTypeDefinitionRequest>
{
    public CreateContentTypeDefinitionRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Content type name is required")
            .MaximumLength(255).WithMessage("Content type name must not exceed 255 characters");
    }
}
