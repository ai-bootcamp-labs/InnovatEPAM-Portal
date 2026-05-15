using FluentValidation;
using InnovatEpam.Portal.Application.Scoring.Dtos;
using InnovatEpam.Portal.Domain.Scoring;

namespace InnovatEpam.Portal.Application.Scoring.Validators;

/// <summary>FluentValidation rules for <see cref="SubmitScoreRequest"/> (Phase 7).</summary>
public sealed class SubmitScoreRequestValidator : AbstractValidator<SubmitScoreRequest>
{
    public SubmitScoreRequestValidator()
    {
        RuleFor(x => x.Impact).InclusiveBetween(IdeaScore.MinRating, IdeaScore.MaxRating);
        RuleFor(x => x.Feasibility).InclusiveBetween(IdeaScore.MinRating, IdeaScore.MaxRating);
        RuleFor(x => x.Innovation).InclusiveBetween(IdeaScore.MinRating, IdeaScore.MaxRating);
        RuleFor(x => x.Alignment).InclusiveBetween(IdeaScore.MinRating, IdeaScore.MaxRating);
        RuleFor(x => x.Comment)
            .MaximumLength(IdeaScore.CommentMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Comment));
    }
}
