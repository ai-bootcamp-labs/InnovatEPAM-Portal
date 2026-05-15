namespace InnovatEpam.Portal.Application.Scoring.Dtos;

/// <summary>Payload for <c>POST /api/v1/ideas/{id}/scores</c> (Phase 7).</summary>
public sealed record SubmitScoreRequest(
    int Impact,
    int Feasibility,
    int Innovation,
    int Alignment,
    string? Comment);
