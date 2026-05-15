namespace InnovatEpam.Portal.Application.Scoring.Dtos;

/// <summary>Per-dimension averages computed across all reviewers (Phase 7, FR-010).</summary>
public sealed record IdeaScoreAverageByDimension(
    double Impact,
    double Feasibility,
    double Innovation,
    double Alignment);

/// <summary>A single reviewer's score row, with the reviewer aliased (FR-012).</summary>
public sealed record IdeaScoreEntryDto(
    string ReviewerAlias,
    int Impact,
    int Feasibility,
    int Innovation,
    int Alignment,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Aggregated score view of an idea exposed on the detail endpoint (Phase 7).
/// All numeric fields are <c>null</c> when <see cref="Count"/> is zero.
/// </summary>
public sealed record IdeaScoreAggregateDto(
    int Count,
    double? Overall,
    IdeaScoreAverageByDimension? AverageByDimension,
    IReadOnlyList<IdeaScoreEntryDto> Entries);
