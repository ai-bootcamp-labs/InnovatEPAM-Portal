using InnovatEpam.Portal.Domain.Common;
using InnovatEpam.Portal.Domain.Exceptions;

namespace InnovatEpam.Portal.Domain.Scoring;

/// <summary>
/// Multi-dimension score recorded by a single admin against a single idea
/// (Phase 7, FR-006). One row per <c>(IdeaId, ReviewerId)</c> — re-submitting
/// is an update, not an insert.
/// </summary>
public sealed class IdeaScore : IAuditable
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int CommentMaxLength = 1000;

    private IdeaScore() { }

    private IdeaScore(
        Guid id,
        Guid ideaId,
        Guid reviewerId,
        int impact,
        int feasibility,
        int innovation,
        int alignment,
        string? comment)
    {
        Id = id;
        IdeaId = ideaId;
        ReviewerId = reviewerId;
        Impact = impact;
        Feasibility = feasibility;
        Innovation = innovation;
        Alignment = alignment;
        Comment = comment;
    }

    public Guid Id { get; private set; }
    public Guid IdeaId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public int Impact { get; private set; }
    public int Feasibility { get; private set; }
    public int Innovation { get; private set; }
    public int Alignment { get; private set; }
    public string? Comment { get; private set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    public static IdeaScore Create(
        Guid ideaId,
        Guid reviewerId,
        int impact,
        int feasibility,
        int innovation,
        int alignment,
        string? comment)
    {
        Validate(impact, feasibility, innovation, alignment, comment, out var trimmedComment);
        return new IdeaScore(Guid.NewGuid(), ideaId, reviewerId,
            impact, feasibility, innovation, alignment, trimmedComment);
    }

    public void Update(int impact, int feasibility, int innovation, int alignment, string? comment)
    {
        Validate(impact, feasibility, innovation, alignment, comment, out var trimmedComment);
        Impact = impact;
        Feasibility = feasibility;
        Innovation = innovation;
        Alignment = alignment;
        Comment = trimmedComment;
    }

    private static void Validate(int impact, int feasibility, int innovation, int alignment, string? comment, out string? trimmed)
    {
        var errors = new Dictionary<string, string[]>();
        Check(nameof(Impact), impact);
        Check(nameof(Feasibility), feasibility);
        Check(nameof(Innovation), innovation);
        Check(nameof(Alignment), alignment);

        trimmed = string.IsNullOrWhiteSpace(comment) ? null : comment.Trim();
        if (trimmed is { Length: > CommentMaxLength })
        {
            errors[nameof(Comment)] = [$"Comment must be ≤ {CommentMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new DomainValidationException("Score validation failed.", errors);
        }

        void Check(string field, int value)
        {
            if (value is < MinRating or > MaxRating)
            {
                errors[field] = [$"{field} must be between {MinRating} and {MaxRating}."];
            }
        }
    }
}
