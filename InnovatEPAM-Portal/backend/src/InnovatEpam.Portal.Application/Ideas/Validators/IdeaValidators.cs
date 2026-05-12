using FluentValidation;
using InnovatEpam.Portal.Application.Ideas.Dtos;
using InnovatEpam.Portal.Domain.Ideas;

namespace InnovatEpam.Portal.Application.Ideas.Validators;

internal sealed class CreateIdeaRequestValidator : AbstractValidator<CreateIdeaRequest>
{
    public CreateIdeaRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MinimumLength(Idea.TitleMinLength)
            .MaximumLength(Idea.TitleMaxLength);
        RuleFor(x => x.Description)
            .NotEmpty()
            .MinimumLength(Idea.DescriptionMinLength)
            .MaximumLength(Idea.DescriptionMaxLength);
        RuleFor(x => x.CategoryId).NotEmpty();
    }
}
