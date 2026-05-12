using InnovatEpam.Portal.Domain.Enums;

namespace InnovatEpam.Portal.Application.Decisions.Dtos;

/// <summary>Payload for <c>POST /api/v1/ideas/{id}/decisions</c> (T084, FR-018, FR-019).</summary>
public sealed record CreateDecisionRequest(DecisionAction Action, string? Comment);

/// <summary>One row of an idea's status timeline returned by <c>GET /ideas/{id}/history</c>.</summary>
public sealed record StatusHistoryEntry(
    Guid Id,
    IdeaStatus? FromStatus,
    IdeaStatus ToStatus,
    Guid ActorId,
    string ActorDisplayName,
    string? Comment,
    Guid? DecisionId,
    DateTimeOffset OccurredAt);
