using InnovatEpam.Portal.Application.Scoring.Dtos;
using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Application.Ideas.Dtos;

/// <summary>Payload for <c>POST /api/v1/ideas</c>.</summary>
public sealed record CreateIdeaRequest(string Title, string Description, Guid CategoryId);

/// <summary>
/// List-row projection (FR-013). Phase 6/7 added:
///  • <see cref="SubmitterName"/> may be <c>null</c> when blind-review mode hides identity;
///  • <see cref="SubmitterAlias"/> is non-null in that case;
///  • <see cref="Overall"/> / <see cref="ReviewerCount"/> expose Phase 7 aggregates
///    (FR-014, populated on every list response when scores exist).
/// </summary>
public sealed record IdeaListItem(
    Guid Id,
    string Title,
    Guid CategoryId,
    string CategoryName,
    IdeaStatus Status,
    Guid SubmitterId,
    string? SubmitterName,
    bool HasAttachment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? SubmitterAlias,
    double? Overall,
    int ReviewerCount);

/// <summary>
/// Detail projection for a single idea. Phase 6/7 added <see cref="SubmitterAlias"/>
/// (FR-001 — populated when admin views a pre-decision idea) and
/// <see cref="Scores"/> (FR-010..012 — full aggregate + reviewer-aliased entries).
/// Exactly one of <see cref="SubmitterAlias"/> or <see cref="SubmitterName"/> is non-null.
/// </summary>
public sealed record IdeaDetail(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    IdeaStatus Status,
    Guid SubmitterId,
    string? SubmitterName,
    AttachmentSummary? Attachment,
    string? LastDecisionComment,
    Guid? LastDecisionById,
    string? LastDecisionByName,
    DateTimeOffset? LastDecisionAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string? SubmitterAlias,
    IdeaScoreAggregateDto? Scores);

/// <summary>Pagination wrapper for idea listings.</summary>
public sealed record PagedIdeas(IReadOnlyList<IdeaListItem> Items, int Total, int Page, int PageSize);

/// <summary>Lightweight attachment metadata (no bytes).</summary>
public sealed record AttachmentSummary(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt);
