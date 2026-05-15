namespace InnovatEpam.Portal.Domain.Scoring;

/// <summary>
/// The fixed set of dimensions an admin rates an idea against (Phase 7).
/// </summary>
/// <remarks>
/// Backing values are stable integers; the persisted shape is one column
/// per dimension on <c>idea_scores</c> (see <c>IdeaScoreConfiguration</c>).
/// </remarks>
public enum ScoreDimension
{
    Impact = 1,
    Feasibility = 2,
    Innovation = 3,
    Alignment = 4
}
