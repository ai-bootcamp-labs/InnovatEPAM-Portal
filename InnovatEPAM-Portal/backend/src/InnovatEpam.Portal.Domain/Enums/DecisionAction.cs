namespace InnovatEpam.Portal.Domain.Enums;

/// <summary>
/// The action recorded by an admin against an <see cref="Ideas.Idea"/>.
/// </summary>
/// <remarks>
/// Persisted as <c>smallint</c> with a <c>CHECK</c> constraint. Mapping to
/// resulting <see cref="IdeaStatus"/> is performed by the
/// <c>trg_decision_after_insert</c> trigger.
/// </remarks>
public enum DecisionAction
{
    MoveToUnderReview = 1,
    Accept = 2,
    Reject = 3
}
