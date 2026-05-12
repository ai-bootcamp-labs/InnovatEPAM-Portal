using FluentValidation;
using InnovatEpam.Portal.Application.Decisions.Dtos;
using InnovatEpam.Portal.Domain.Decisions;
using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Application.Decisions.Validators;

/// <summary>Enforces FR-019 — comment is required for Accept/Reject (T084).</summary>
internal sealed class CreateDecisionRequestValidator : AbstractValidator<CreateDecisionRequest>
{
    public CreateDecisionRequestValidator()
    {
        RuleFor(x => x.Action).IsInEnum();

        When(x => x.Action is DecisionAction.Accept or DecisionAction.Reject, () =>
        {
            RuleFor(x => x.Comment)
                .NotEmpty().WithMessage("Comment is required for Accept/Reject.")
                .MaximumLength(Decision.CommentMaxLength);
        });

        When(x => x.Action == DecisionAction.MoveToUnderReview, () =>
        {
            RuleFor(x => x.Comment).MaximumLength(Decision.CommentMaxLength);
        });
    }
}
