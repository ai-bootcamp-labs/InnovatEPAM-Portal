namespace InnovatEpam.Portal.Domain.Enums;

/// <summary>
/// Lifecycle status of an <see cref="Ideas.Idea"/>.
/// </summary>
/// <remarks>
/// Persisted as <c>smallint</c> in PostgreSQL with a <c>CHECK</c> constraint
/// limiting values to the enumerated set. State transitions are enforced by
/// the domain layer (<c>Idea.TransitionTo</c>) and re-asserted at the database
/// level via the <c>trg_idea_terminal_status_immutable</c> trigger.
/// </remarks>
public enum IdeaStatus
{
    Submitted = 1,
    UnderReview = 2,
    Accepted = 3,
    Rejected = 4
}
