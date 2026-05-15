using InnovatEpam.Portal.Application.Auth;
using InnovatEpam.Portal.Application.Persistence;
using InnovatEpam.Portal.Application.Scoring.Dtos;
using InnovatEpam.Portal.Domain.Enums;
using InnovatEpam.Portal.Domain.Exceptions;
using InnovatEpam.Portal.Domain.Scoring;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InnovatEpam.Portal.Application.Scoring;

/// <summary>
/// Phase 7 — multi-dimension scoring use-cases (FR-006..012).
/// One score per <c>(IdeaId, ReviewerId)</c>: a second POST upserts the existing row.
/// </summary>
public sealed class ScoringService
{
    private readonly IPortalDbContext _db;
    private readonly IAliasService _alias;
    private readonly ILogger<ScoringService> _logger;

    public ScoringService(IPortalDbContext db, IAliasService alias, ILogger<ScoringService> logger)
    {
        _db = db;
        _alias = alias;
        _logger = logger;
    }

    public async Task<UpsertScoreResult> UpsertAsync(
        Guid ideaId,
        Guid reviewerId,
        SubmitScoreRequest request,
        CancellationToken ct)
    {
        var idea = await _db.Ideas.AsNoTracking().FirstOrDefaultAsync(i => i.Id == ideaId, ct)
            ?? throw new NotFoundException("Idea not found.");

        if (idea.SubmitterId == reviewerId)
        {
            throw new SelfScoringForbiddenException();
        }

        if (idea.Status is IdeaStatus.Accepted or IdeaStatus.Rejected)
        {
            throw new ScoringClosedException();
        }

        var existing = await _db.IdeaScores
            .FirstOrDefaultAsync(s => s.IdeaId == ideaId && s.ReviewerId == reviewerId, ct);

        bool created;
        if (existing is null)
        {
            var score = IdeaScore.Create(ideaId, reviewerId,
                request.Impact, request.Feasibility, request.Innovation, request.Alignment, request.Comment);
            _db.IdeaScores.Add(score);
            created = true;
        }
        else
        {
            existing.Update(request.Impact, request.Feasibility, request.Innovation, request.Alignment, request.Comment);
            created = false;
        }

        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("score.upsert {IdeaId} {ReviewerId} created={Created}", ideaId, reviewerId, created);

        var aggregate = await BuildAggregateAsync(ideaId, ct);
        return new UpsertScoreResult(created, aggregate);
    }

    /// <summary>Builds the full aggregate snapshot for one idea. See FR-010..012.</summary>
    public async Task<IdeaScoreAggregateDto> BuildAggregateAsync(Guid ideaId, CancellationToken ct)
    {
        var rows = await _db.IdeaScores.AsNoTracking()
            .Where(s => s.IdeaId == ideaId)
            .OrderBy(s => s.CreatedAt)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            return new IdeaScoreAggregateDto(0, null, null, Array.Empty<IdeaScoreEntryDto>());
        }

        var avgImpact = Math.Round(rows.Average(r => (double)r.Impact), 2);
        var avgFeas = Math.Round(rows.Average(r => (double)r.Feasibility), 2);
        var avgInno = Math.Round(rows.Average(r => (double)r.Innovation), 2);
        var avgAlign = Math.Round(rows.Average(r => (double)r.Alignment), 2);
        var overall = Math.Round((avgImpact + avgFeas + avgInno + avgAlign) / 4d, 2);

        var entries = rows
            .Select(r => new IdeaScoreEntryDto(
                _alias.ReviewerAlias(r.IdeaId, r.ReviewerId),
                r.Impact, r.Feasibility, r.Innovation, r.Alignment,
                r.Comment, r.CreatedAt, r.UpdatedAt))
            .ToList();

        return new IdeaScoreAggregateDto(
            rows.Count,
            overall,
            new IdeaScoreAverageByDimension(avgImpact, avgFeas, avgInno, avgAlign),
            entries);
    }
}

/// <summary>Tuple-style result wrapper for the controller to choose between 200/201.</summary>
public sealed record UpsertScoreResult(bool Created, IdeaScoreAggregateDto Aggregate);
