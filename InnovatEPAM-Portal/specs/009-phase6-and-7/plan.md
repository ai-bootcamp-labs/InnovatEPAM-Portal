# Implementation Plan: Phase 6 (Blind Review) + Phase 7 (Multi-Dimension Scoring)

**Branch**: `009-phase6-and-7` | **Date**: 2026-05-15 | **Spec**: [spec.md](./spec.md)

## Summary

Two additive vertical slices on top of the existing Phase 1 stack:

1. **Phase 6 — Blind Review.** Admin-only redaction at the API
   projection layer: when an idea's latest decision is not terminal
   (`Accept` / `Reject`), the submitter's identity is replaced with a
   deterministic per-idea alias. Persistence is unchanged; only the
   read-model mappers (`IdeaService` → `IdeaResponse`) and the admin
   frontend's idea card/detail components are touched. A new boolean
   `Decisions.WasBlind` column records the audit fact.

2. **Phase 7 — Multi-Dimension Scoring.** A new `IdeaScore` aggregate
   sibling to `Decision` with four integer dimensions (1..5), an
   optional comment, and a `(IdeaId, ReviewerId)` unique key. A new
   `ScoringService` performs the upsert, RBAC + status guards, and
   exposes aggregates via the existing idea read endpoints. The list
   endpoint gains an opt-in `sort=score:asc|desc` (with NULLS LAST
   semantics) computed in a single SQL pass via a pre-aggregated
   subquery / `LEFT JOIN LATERAL`.

Both slices are strictly additive: existing routes keep their shapes,
existing tests stay green, and no Phase 1 status-transition rules move.

## Technical Context

**Backend**: .NET 8, ASP.NET Core minimal-hosted Web API,
EF Core 8 on PostgreSQL, FluentValidation, ASP.NET Identity (roles
`Submitter` / `Admin`). One new EF migration adds `IdeaScores` table
+ `Decisions.WasBlind` column.

**Frontend**: React 18 + Vite, TanStack Query 5, react-hook-form 7 +
zod 3, Tailwind 3 + the Phase 8 primitives layer (`Button`, `Input`,
`Label`, `FieldError`, `Card`, `StatusBadge`). One new feature module
`frontend/src/features/scoring/` hosts the `ScorePanel`, hook
(`useUpsertScore`), and zod schema; admin queue + idea detail consume
it.

**Tests**:
- Backend: xUnit unit tests for domain rules + integration tests
  (`InnovatEpam.Portal.IntegrationTests`) hitting real Postgres via the
  existing testcontainer harness.
- Frontend: Vitest + Testing Library for `ScorePanel`, alias rendering,
  and ranking control; Playwright e2e smoke for the admin queue blind +
  ranking flow.

**Project Type**: Full-stack slice of the existing monorepo
(backend + frontend updated together; one PR).

**Constraints**:
- No N+1 queries on the list endpoint (NFR-002, FR-014).
- Existing API responses MUST stay backward-compatible for non-admin
  callers and for callers that do not opt into score sort (NFR-001).
- AA accessibility on the score input (NFR-003).
- Forward-only EF migration; safe to roll forward on a populated DB
  (NFR-005).

## Project Structure

```
backend/
  src/
    InnovatEpam.Portal.Domain/
      Decisions/Decision.cs                 (add WasBlind)
      Ideas/Idea.cs                         (no change; aggregate read-only here)
      Scoring/                              (NEW)
        IdeaScore.cs
        ScoreDimension.cs                   (enum)
    InnovatEpam.Portal.Application/
      Decisions/DecisionService.cs          (set WasBlind on Create)
      Ideas/IdeaService.cs                  (alias projection + score join)
      Ideas/Dtos/IdeaResponse.cs            (add submitterAlias + scores block)
      Scoring/                              (NEW)
        ScoringService.cs
        Dtos/SubmitScoreRequest.cs
        Dtos/IdeaScoreResponse.cs
        Validators/SubmitScoreRequestValidator.cs
      Auth/AliasService.cs                  (NEW — deterministic hash)
    InnovatEpam.Portal.Infrastructure/
      Persistence/Configurations/
        IdeaScoreConfiguration.cs           (NEW)
        DecisionConfiguration.cs            (add WasBlind)
      Persistence/Migrations/
        2026xxxx_AddBlindReviewAndScoring.cs (NEW)
    InnovatEpam.Portal.Api/
      Controllers/IdeasController.cs        (add sort=score:* support)
      Controllers/ScoresController.cs       (NEW)
  tests/
    InnovatEpam.Portal.UnitTests/
      Scoring/IdeaScoreTests.cs             (NEW)
      Auth/AliasServiceTests.cs             (NEW)
    InnovatEpam.Portal.IntegrationTests/
      Scoring/ScoringEndpointTests.cs       (NEW)
      Ideas/BlindReviewProjectionTests.cs   (NEW)
      Ideas/ScoreSortingTests.cs            (NEW)

frontend/
  src/
    features/
      scoring/                              (NEW)
        api.ts
        schema.ts
        ScorePanel.tsx
        ScoreAggregate.tsx
        useUpsertScore.ts
      ideas/
        api.ts                              (extend types: submitterAlias + scores)
        IdeasListPage.tsx                   (alias + sort control + overall)
        IdeaDetailPage.tsx                  (mount <ScorePanel /> + aggregate)
      admin/
        DecideControls.tsx                  (no change; reads WasBlind from decision)
    components/ui/
      ScoreInput.tsx                        (NEW — 1..5 radio group primitive)
  tests/
    unit/scoring/ScorePanel.test.tsx        (NEW)
    unit/ideas/IdeasListPage.alias.test.tsx (NEW)
    e2e/blind-review.spec.ts                (NEW)
    e2e/scoring.spec.ts                     (NEW)
```

## Phase Ordering

1. **Foundation (blocking).** Domain + EF migration + alias service
   + read-model DTO extensions. All later work depends on this.
2. **US1 / FR-001..005 — Blind Review backend.**
3. **US3 / FR-006..014 — Scoring backend (CRUD + aggregates).**
4. **US4 / FR-013, FR-014 — Score-sort on list endpoint.**
5. **Frontend US1 / FR-016 — Alias in admin queue + detail.**
6. **Frontend US3 / FR-017 — `<ScorePanel />` + aggregate.**
7. **Frontend US4 / FR-018 — Sort control.**
8. **Verification — e2e + perf smoke + docs.**

## Risks & Mitigations

| Risk | Impact | Mitigation |
|---|---|---|
| Submitter identity leaks via an existing field we forgot to redact (e.g. notification payload, attachment uploader metadata) | High — defeats the whole phase | A single integration test (`BlindReviewProjectionTests.NoPiiInPayload`) does a substring scan over the serialized response for seeded submitter name/email; runs on every PR. |
| Score sort triggers an N+1 against a 500-idea seed | Medium | Implement via `LEFT JOIN` on a `(IdeaId, AvgOverall, ReviewerCount)` projection materialized in the same query; assert via captured SQL in `ScoreSortingTests`. |
| Alias is reversible by a curious admin | Medium | Salt the hash with a server-only secret (config: `BlindReview:AliasSalt`) and truncate; never echo `SubmitterId` anywhere in the alias. |
| EF migration fails on populated DB | High | Migration is forward-only, additive (new table + new nullable bool column with default `false`); tested against the integration testcontainer pre-seeded with Phase 1 fixtures. |

## Out of Scope

See spec.md "Out of Scope (Deferred)" — D1..D5.
