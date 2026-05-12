using InnovatEpam.Portal.Domain.Common;
using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Exceptions;

namespace InnovatEpam.Portal.Domain.Ideas;

/// <summary>
/// Innovation proposal aggregate root (FR-009..014, FR-016..021). See data-model §4.
/// </summary>
public sealed class Idea : IAuditable
{
    public const int TitleMinLength = 5;
    public const int TitleMaxLength = 120;
    public const int DescriptionMinLength = 1;
    public const int DescriptionMaxLength = 4000;

    /// <summary>EF parameterless constructor.</summary>
    private Idea() { }

    private Idea(Guid id, string title, string description, Guid categoryId, Guid submitterId)
    {
        Id = id;
        Title = title;
        Description = description;
        CategoryId = categoryId;
        SubmitterId = submitterId;
        Status = IdeaStatus.Submitted;
    }

    public Guid Id { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public Guid CategoryId { get; private set; }
    public IdeaStatus Status { get; private set; } = IdeaStatus.Submitted;
    public Guid SubmitterId { get; private set; }
    public Guid? AttachmentId { get; private set; }
    public Guid? LastDecisionId { get; private set; }

    /// <summary>PostgreSQL <c>xmin</c> system column — concurrency token.</summary>
    public uint RowVersion { get; private set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// Creates a new idea in <see cref="IdeaStatus.Submitted"/>. Validates length
    /// rules from FR-009; the database CHECK constraints provide defence in depth.
    /// </summary>
    public static Idea Create(string title, string description, Guid categoryId, Guid submitterId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var trimmedTitle = title.Trim();
        var trimmedDescription = description.Trim();

        var errors = new Dictionary<string, string[]>();
        if (trimmedTitle.Length is < TitleMinLength or > TitleMaxLength)
        {
            errors[nameof(Title)] = [$"Title must be between {TitleMinLength} and {TitleMaxLength} characters."];
        }
        if (trimmedDescription.Length is < DescriptionMinLength or > DescriptionMaxLength)
        {
            errors[nameof(Description)] = [$"Description must be between {DescriptionMinLength} and {DescriptionMaxLength} characters."];
        }
        if (categoryId == Guid.Empty)
        {
            errors[nameof(CategoryId)] = ["Category is required."];
        }
        if (submitterId == Guid.Empty)
        {
            errors[nameof(SubmitterId)] = ["Submitter is required."];
        }
        if (errors.Count > 0)
        {
            throw new DomainValidationException("Invalid idea payload.", errors);
        }

        return new Idea(Guid.NewGuid(), trimmedTitle, trimmedDescription, categoryId, submitterId);
    }

    /// <summary>
    /// Transition this idea to <paramref name="next"/>. Validates the state
    /// machine from data-model §4. Throws <see cref="ConflictException"/> on
    /// illegal transitions (e.g. mutating a terminal status) — FR-020.
    /// </summary>
    public void TransitionTo(IdeaStatus next, DecisionAction action)
    {
        if (!IsLegalTransition(Status, next))
        {
            throw new ConflictException(
                $"Cannot transition idea from {Status} to {next} via {action}.");
        }
        Status = next;
    }

    /// <summary>Records the decision that drove the most recent transition.</summary>
    public void AssignLastDecision(Guid decisionId) => LastDecisionId = decisionId;

    /// <summary>Sets or replaces the (single) attachment pointer (FR-010).</summary>
    public void SetAttachment(Guid? attachmentId) => AttachmentId = attachmentId;

    private static bool IsLegalTransition(IdeaStatus from, IdeaStatus to) => (from, to) switch
    {
        (IdeaStatus.Submitted, IdeaStatus.UnderReview) => true,
        (IdeaStatus.Submitted, IdeaStatus.Accepted) => true,
        (IdeaStatus.Submitted, IdeaStatus.Rejected) => true,
        (IdeaStatus.UnderReview, IdeaStatus.Accepted) => true,
        (IdeaStatus.UnderReview, IdeaStatus.Rejected) => true,
        _ => false,
    };
}
