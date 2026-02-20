using DMS.BL.DTOs;
using FluentValidation;

namespace DMS.Api.Validation.Validators;

public class CreateDocumentDtoValidator : AbstractValidator<CreateDocumentDto>
{
    public CreateDocumentDtoValidator()
    {
        RuleFor(x => x.FolderId).NotEmpty().WithMessage("Folder ID is required");
        RuleFor(x => x.Name).NotEmpty().WithMessage("Document name is required")
            .MaximumLength(255).WithMessage("Document name must not exceed 255 characters");
    }
}
