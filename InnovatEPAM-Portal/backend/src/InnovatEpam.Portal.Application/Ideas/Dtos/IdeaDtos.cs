using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Application.Ideas.Dtos;

/// <summary>Payload for <c>POST /api/v1/ideas</c>.</summary>
public sealed record CreateIdeaRequest(string Title, string Description, Guid CategoryId);

/// <summary>List-row projection (FR-013).</summary>
public sealed record IdeaListItem(
    Guid Id,
    string Title,
    Guid CategoryId,
    string CategoryName,
    IdeaStatus Status,
    Guid SubmitterId,
    string SubmitterDisplayName,
    bool HasAttachment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Detail projection for a single idea.</summary>
public sealed record IdeaDetail(
    Guid Id,
    string Title,
    string Description,
    Guid CategoryId,
    string CategoryName,
    IdeaStatus Status,
    Guid SubmitterId,
    string SubmitterDisplayName,
    AttachmentSummary? Attachment,
    string? LastDecisionComment,
    Guid? LastDecisionById,
    string? LastDecisionByDisplayName,
    DateTimeOffset? LastDecisionAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>Pagination wrapper for idea listings.</summary>
public sealed record PagedIdeas(IReadOnlyList<IdeaListItem> Items, int Total, int Page, int PageSize);

/// <summary>Lightweight attachment metadata (no bytes).</summary>
public sealed record AttachmentSummary(
    Guid Id,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateTimeOffset UploadedAt);
