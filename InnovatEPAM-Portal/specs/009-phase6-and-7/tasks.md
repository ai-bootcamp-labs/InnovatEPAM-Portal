---
description: "Task list for Phase 6 (Blind Review) + Phase 7 (Multi-Dimension Scoring) — 009-phase6-and-7"
---

# Tasks: Phase 6 (Blind Review) + Phase 7 (Multi-Dimension Scoring)

**Input**: Design documents from `specs/009-phase6-and-7/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md)

**Tests**: Included. Backend uses xUnit + the existing testcontainer integration harness; frontend uses Vitest + Testing Library + Playwright.

**Organization**: Tasks are grouped by phase and user story so each story is independently shippable behind a flag if needed. Foundation (domain + migration + alias service + DTO extensions) is shared and MUST land before story phases begin.

## Format: `[ID] [P?] [Story] Description`

- **[P]** — Can run in parallel (different files, no cross-task dependencies)
- **[Story]** — `[US1]` blind review, `[US2]` alias semantics, `[US3]` scoring CRUD, `[US4]` ranking — maps to user stories from spec.md
- File paths are repository-relative; backend paths resolve under `backend/`, frontend paths under `frontend/`.

---

## Phase 1: Setup (No new runtime deps unless noted)

**Purpose**: Confirm baseline is green and capture pre-change behaviour so additive guarantees can be verified.

- [ ] T001 Verify backend baseline: from `backend/` run `dotnet restore`, `dotnet build -warnaserror`, `dotnet test`. All must pass. Record any pre-existing flaky tests in notes — they remain pre-existing after this feature.
- [ ] T002 [P] Verify frontend baseline: from `frontend/` run `npm ci`, `npm run lint -- --max-warnings=0`, `npm run typecheck`, `npm run test`, `npm run build`. All must pass.
- [ ] T003 [P] Capture a sample `GET /api/v1/ideas` and `GET /api/v1/ideas/{id}` response against the seeded Phase 1 fixture and save as a JSON snapshot under `specs/009-phase6-and-7/baseline/`. This is the backward-compat reference for NFR-001.
- [ ] T004 Add `BlindReview:AliasSalt` to `appsettings.Development.json` + `appsettings.json` (placeholder) + document in `backend/README.md` that production deployments MUST override via user-secrets / env var. Do NOT commit a production salt.

**Checkpoint**: Baseline green, snapshots stored, salt config wired (but unused).

---

## Phase 2: Foundational — Domain, persistence, alias service (Blocking)

**Purpose**: Stand up the shared data + infrastructure layer every story consumes. **⚠️ CRITICAL**: no story phase begins until this phase is complete.

### Domain model

- [ ] T005 Create `backend/src/InnovatEpam.Portal.Domain/Scoring/ScoreDimension.cs` defining the enum `{ Impact = 1, Feasibility = 2, Innovation = 3, Alignment = 4 }`. Persisted as `smallint` with CHECK constraint (configured in T009).
- [ ] T006 Create `backend/src/InnovatEpam.Portal.Domain/Scoring/IdeaScore.cs` as a sealed aggregate root. Fields: `Id` (Guid), `IdeaId`, `ReviewerId`, `Impact`, `Feasibility`, `Innovation`, `Alignment` (all `int` constrained `1..5`), `Comment` (string?, max 1000), `CreatedAt`, `UpdatedAt`. Expose `Create(...)` and `Update(...)` static/instance methods that validate range + comment length and throw `DomainValidationException` on violation. Implement `IAuditable`. Depends on T005.
- [ ] T007 Add `WasBlind` (`bool`, default `false`) to `backend/src/InnovatEpam.Portal.Domain/Decisions/Decision.cs`. Extend `Decision.Create(...)` signature with a `bool wasBlind` parameter and persist it. Update the existing xUnit unit tests under `backend/tests/InnovatEpam.Portal.UnitTests/Decisions/` so they pass `wasBlind: false` to keep semantics. Depends on nothing besides existing files.

### Persistence + EF migration

- [ ] T008 Create `backend/src/InnovatEpam.Portal.Infrastructure/Persistence/Configurations/IdeaScoreConfiguration.cs` mapping `IdeaScore` to table `IdeaScores`: PK `Id`, unique index `(IdeaId, ReviewerId)`, FK `IdeaId → Ideas.Id` (cascade), FK `ReviewerId → AspNetUsers.Id` (restrict), CHECK constraints on each dimension `BETWEEN 1 AND 5`, max length 1000 on `Comment`. Depends on T006.
- [ ] T009 Update `backend/src/InnovatEpam.Portal.Infrastructure/Persistence/Configurations/DecisionConfiguration.cs` to map `WasBlind` as `boolean NOT NULL DEFAULT false`. Depends on T007.
- [ ] T010 Register `DbSet<IdeaScore> IdeaScores` on `PortalDbContext`. Depends on T008.
- [ ] T011 Generate the EF migration `dotnet ef migrations add AddBlindReviewAndScoring --project InnovatEpam.Portal.Infrastructure --startup-project InnovatEpam.Portal.Api`. Inspect the generated SQL: (a) `IdeaScores` table is additive, (b) `Decisions.WasBlind` is nullable-with-default OR not-null-with-default-false so existing rows back-fill cleanly, (c) no rename / drop on existing tables. Depends on T010.
- [ ] T012 Apply migration against the integration testcontainer (`dotnet test --filter Category=Persistence`) to prove forward-only safety; document the rollback story in the migration's header comment ("forward-only; to revert, restore from snapshot + run inverse SQL").

### Alias service (shared by US1 + US3)

- [ ] T013 Create `backend/src/InnovatEpam.Portal.Application/Auth/AliasService.cs` exposing `IAliasService` with two methods: `string SubmitterAlias(Guid ideaId)` and `string ReviewerAlias(Guid ideaId, Guid reviewerId)`. Implementation: HMAC-SHA256 over the input using `BlindReview:AliasSalt` from `IConfiguration`, base32-truncated to 4 chars (submitter) / 4 chars (reviewer). Format: `"Submitter #{TOKEN}"` / `"Reviewer #{TOKEN}"`. Register as singleton in `Program.cs`. Depends on T004.
- [ ] T014 [P] Add `backend/tests/InnovatEpam.Portal.UnitTests/Auth/AliasServiceTests.cs` covering: (a) determinism (same input → same output), (b) two different idea IDs by the same submitter produce different aliases (FR-002), (c) format starts with `Submitter #` / `Reviewer #`, (d) salt change produces a different token (proves it depends on salt). Depends on T013.

### Read-model DTO extensions (non-breaking additions)

- [ ] T015 Extend `backend/src/InnovatEpam.Portal.Application/Ideas/Dtos/IdeaResponse.cs` with **optional / nullable** fields: `string? SubmitterAlias`, `IdeaScoreAggregateDto? Scores`. Existing `SubmitterName` / `SubmitterEmail` remain — they are populated for non-blind callers and set to `null` for blind admin views. Document in XML doc-comment: "Exactly one of `SubmitterAlias` or `SubmitterName` is non-null per response."
- [ ] T016 [P] Create `backend/src/InnovatEpam.Portal.Application/Scoring/Dtos/IdeaScoreAggregateDto.cs` (`Count`, `Overall`, `AverageByDimension` { `Impact`, `Feasibility`, `Innovation`, `Alignment` }, `Entries: IReadOnlyList<IdeaScoreEntryDto>`) and `IdeaScoreEntryDto` (`ReviewerAlias`, `Impact`, `Feasibility`, `Innovation`, `Alignment`, `Comment`, `CreatedAt`, `UpdatedAt`). Note: only `ReviewerAlias` is exposed during blind mode; never the reviewer's identity.
- [ ] T017 [P] Create `backend/src/InnovatEpam.Portal.Application/Scoring/Dtos/SubmitScoreRequest.cs` with the four 1..5 dimension fields + optional `Comment`. Add `backend/src/InnovatEpam.Portal.Application/Scoring/Validators/SubmitScoreRequestValidator.cs` (FluentValidation): each dimension `InclusiveBetween(1, 5)`, comment `MaximumLength(1000)`. Register validator in the existing FluentValidation DI assembly scan.

**Checkpoint**: domain + DB + DTOs + alias service in place; nothing observable from the API yet.

---

## Phase 3: US1 — Blind Review backend (FR-001..005)

**Goal**: Admin-facing read endpoints redact submitter identity for non-terminal ideas. Decisions record `WasBlind`.

- [ ] T018 [US1] In `backend/src/InnovatEpam.Portal.Application/Ideas/IdeaService.cs`, change the projection used by the admin paths so that when (caller is in role `Admin`) AND (idea's latest `Decision.Action ∉ { Accept, Reject }`): `SubmitterAlias = aliasService.SubmitterAlias(idea.Id)` and `SubmitterName = null`, `SubmitterEmail = null`. For all other callers/states: keep `SubmitterName` / `SubmitterEmail` as today and leave `SubmitterAlias = null`. The submitter viewing their own idea always sees their identity (FR-003). Depends on T013, T015.
- [ ] T019 [US1] [P] In `backend/src/InnovatEpam.Portal.Application/Decisions/DecisionService.cs`, before calling `Decision.Create(...)`, inspect the active redaction policy for the current call and pass `wasBlind = (callerIsAdmin && currentStatus != Accepted && currentStatus != Rejected)`. Persist via the existing `SaveChangesAsync`. Depends on T007.
- [ ] T020 [US1] [P] Audit `backend/src/InnovatEpam.Portal.Application/Notifications/` — verify the admin-facing notification payloads (if any) do not include `SubmitterName` / `SubmitterEmail` for pre-decision ideas. If they do, route through `AliasService.SubmitterAlias`. Submitter-targeted notifications remain unchanged (FR-005).
- [ ] T021 [US1] Integration test `backend/tests/InnovatEpam.Portal.IntegrationTests/Ideas/BlindReviewProjectionTests.cs`:
  - `Admin_GET_Ideas_PreDecision_ReturnsAliasNotName()` — seed one `Submitted` + one `UnderReview` idea; assert response contains `submitterAlias`, asserts response JSON string-search for the seeded submitter's name and email yields **zero matches**.
  - `Admin_GET_Ideas_PostDecision_ReturnsRealIdentity()` — seed one `Accepted` idea; assert real name/email present, alias is `null`.
  - `Submitter_GET_OwnIdea_AlwaysSeesIdentity()` — submitter calling their own pre-decision idea sees real name/email.
  - `Decision_Recorded_PersistsWasBlindTrue()` — admin records `Accept` on a `Submitted` idea; load the persisted `Decision` and assert `WasBlind == true`.
  - Depends on T018, T019.

**Checkpoint**: blind review is enforced and audited; suite green; no PII leakage in admin payloads.

---

## Phase 4: US3 — Scoring CRUD + aggregates (FR-006..012)

**Goal**: Admins can upsert scores; the idea detail payload exposes aggregates and reviewer-aliased entries.

- [ ] T022 [US3] Create `backend/src/InnovatEpam.Portal.Application/Scoring/ScoringService.cs` exposing `Task<UpsertScoreResult> UpsertAsync(Guid ideaId, Guid reviewerId, SubmitScoreRequest req, CancellationToken ct)` where `UpsertScoreResult = { bool Created, IdeaScoreAggregateDto Aggregate }`. Rules:
  - Load idea; `NotFound` → return 404 from controller.
  - If `reviewerId == idea.SubmitterId` → throw `SelfScoringForbiddenException` (FR-009).
  - If `idea.Status ∈ { Accepted, Rejected }` → throw `ScoringClosedException` (FR-008).
  - Lookup existing score by `(IdeaId, ReviewerId)`; if found, call `Update(...)` and return `Created = false`; else create a new `IdeaScore` and return `Created = true`.
  - After save, project the fresh aggregate (T024).
  Depends on T006, T017.
- [ ] T023 [US3] Add `SelfScoringForbiddenException` and `ScoringClosedException` under `backend/src/InnovatEpam.Portal.Domain/Exceptions/`. Wire them into the existing API problem-details mapper so they surface as `409` with stable error codes `SelfScoringForbidden` / `ScoringClosed`. Depends on existing ErrorHandling middleware.
- [ ] T024 [US3] [P] In `IdeaService.cs`, extend the **detail** projection (`GetByIdAsync`) to populate `Scores`:
  - `Count = scores.Count`.
  - `AverageByDimension.{Impact, Feasibility, Innovation, Alignment} = round(avg(dim), 2)` for each dimension (return `null` when `Count == 0`).
  - `Overall = round((avgImpact + avgFeasibility + avgInnovation + avgAlignment) / 4, 2)` when `Count > 0`, else `null` (FR-011).
  - `Entries` = each `IdeaScore` row projected to `IdeaScoreEntryDto` with `ReviewerAlias = aliasService.ReviewerAlias(idea.Id, score.ReviewerId)` regardless of caller role (FR-012 — reviewer identity is never exposed pre-decision; this phase keeps reviewer identity hidden across the board to keep the rules simple; future D-task can reveal reviewer identity post-decision).
  Depends on T013, T016.
- [ ] T025 [US3] Create `backend/src/InnovatEpam.Portal.Api/Controllers/ScoresController.cs` exposing `POST /api/v1/ideas/{ideaId:guid}/scores` (route may also live as a sub-route on `IdeasController` — pick the convention the codebase already uses for `decisions`). Authorize `Roles = Admin` (FR-008). Resolve `reviewerId` from the JWT subject. Map exceptions per T023. Return `201 Created` (insert) with `Location` header, `200 OK` (update). Body in both cases = `IdeaScoreAggregateDto`. Depends on T022, T024.
- [ ] T026 [US3] [P] Unit tests `backend/tests/InnovatEpam.Portal.UnitTests/Scoring/IdeaScoreTests.cs`:
  - `Create_AllDimensionsInRange_Succeeds()`.
  - `Create_DimensionOutOfRange_Throws()` (parameterised on each dimension and on values 0 and 6).
  - `Create_CommentTooLong_Throws()`.
  - `Update_Mutates_UpdatedAt_AndDimensions()`.
- [ ] T027 [US3] Integration tests `backend/tests/InnovatEpam.Portal.IntegrationTests/Scoring/ScoringEndpointTests.cs`:
  - `Admin_PostScore_FirstTime_Returns201_AndAggregateReflectsRow()`.
  - `Admin_PostScore_SecondTime_Returns200_AndUpdatesInPlace()` — assert exactly one row in `IdeaScores` for `(IdeaId, ReviewerId)`.
  - `Submitter_PostScore_Returns403()`.
  - `Admin_PostScore_OnAcceptedIdea_Returns409_ScoringClosed()`.
  - `Admin_PostScore_OnOwnIdea_Returns409_SelfScoringForbidden()`.
  - `Admin_GetIdea_AfterTwoScores_ReturnsExpectedAggregate()` — uses the US3 independent-test fixture (5/4/5/3 + 4/4/4/4) and asserts `count=2`, dimension averages `4.5/4/4.5/3.5`, `overall=4.125`.
  - `Admin_GetIdea_Entries_UseReviewerAliasNotReviewerName()` — JSON substring scan for the seeded reviewer's name/email yields zero matches.
  Depends on T025.

**Checkpoint**: scoring upsert + aggregates working end-to-end; RBAC + status + self-score guards enforced; reviewer identity hidden.

---

## Phase 5: US4 — Ranking on list endpoint (FR-013, FR-014)

**Goal**: `GET /api/v1/ideas?sort=score:asc|desc` returns ideas ordered by overall score with unscored ideas last, in a single SQL pass.

- [ ] T028 [US4] Extend the list query in `IdeaService.GetListAsync` to accept an opt-in `sort` argument parsed from the controller. When `sort == "score:desc"` (or `asc`), construct the EF Core query as a single statement joining a sub-query that projects `(IdeaId, AvgOverall = (avgImpact + avgFeasibility + avgInnovation + avgAlignment) / 4, ReviewerCount)`. Order: `AvgOverall DESC NULLS LAST, CreatedAt ASC` (or `AvgOverall ASC NULLS LAST, CreatedAt ASC`). Project `Overall` + `ReviewerCount` onto each row of `IdeaListItemResponse` regardless of sort (so the column shows even when sorting by default). Depends on T024.
- [ ] T029 [US4] [P] Update `IdeasController.Get(...)` to parse the new `sort` query parameter (allowed values: `score:asc`, `score:desc`; anything else → fall back to existing default; never throw). Update the OpenAPI annotation.
- [ ] T030 [US4] Integration test `backend/tests/InnovatEpam.Portal.IntegrationTests/Ideas/ScoreSortingTests.cs`:
  - `Sort_ScoreDesc_OrdersByOverallDescending_WithCreatedAtTieBreaker()` — seed three ideas with overalls `[3.1, 4.7, 4.7]` and creation order `[A, B, C]`; assert response order `[B, C, A]`.
  - `Sort_ScoreDesc_UnscoredIdeasAppearLast()` — assert NULLS LAST under both directions.
  - `Sort_Default_BehaviourIsUnchanged()` — assert response shape and order match the Phase 1 snapshot from T003 (NFR-001).
  - `Sort_NoN1Queries()` — capture EF Core SQL via the existing logging interceptor and assert exactly one SELECT against `Ideas` is emitted per request (FR-014).
  Depends on T028.

**Checkpoint**: ranking works in one DB round-trip and is backward-compatible.

---

## Phase 6: Frontend US1 — Alias rendering (FR-016)

**Goal**: Admin queue and detail page render the alias instead of the submitter's name while blind, and switch to the real name once decided.

- [ ] T031 [US1] Extend `frontend/src/features/ideas/api.ts` types: add `submitterAlias: string | null`, set `submitterName` / `submitterEmail` to `string | null`, add `scores: IdeaScoreAggregate | null` with full shape (mirrors T016). Update the existing query hooks' return types; no logic change yet.
- [ ] T032 [US1] [P] Create `frontend/src/features/ideas/SubmitterLabel.tsx` — a small presentational component that takes `{ submitterName?: string | null; submitterAlias?: string | null; isOwn?: boolean }` and renders, in order of priority: "You" (when `isOwn`), real name (when present), alias (when present), or a graceful fallback ("Anonymous submitter"). Add Vitest test under `frontend/tests/unit/ideas/SubmitterLabel.test.tsx`.
- [ ] T033 [US1] Wire `SubmitterLabel` into `IdeasListPage.tsx` (each row) and `IdeaDetailPage.tsx` (header). Resolve `isOwn` from the current auth user's id vs. `idea.submitterId` (already exposed today). Depends on T031, T032.
- [ ] T034 [US1] Unit test `frontend/tests/unit/ideas/IdeasListPage.alias.test.tsx`: mock the API to return one pre-decision idea (alias only) + one decided idea (real name). Assert the rendered DOM contains the alias for the first and the name for the second; assert the seeded submitter's email/name strings are absent from the first row's DOM (mirrors the backend a11y-style scan but in the UI).

**Checkpoint**: admin sees aliases pre-decision, identities post-decision; submitter sees themselves.

---

## Phase 7: Frontend US3 — `<ScorePanel />` + aggregate (FR-017)

**Goal**: Admins can submit scores via a keyboard-accessible 1–5 radio group per dimension; everyone with read access sees the aggregate panel and entry list (aliases only for reviewers).

- [ ] T035 [US3] Create `frontend/src/components/ui/ScoreInput.tsx` — a controlled radio-group primitive rendering five buttons labelled "1".."5" with `role="radiogroup"`, an `aria-labelledby` referencing its surrounding label, visible focus ring (re-use the Phase 8 `ring` token), keyboard arrow-key navigation between options. Add Vitest test covering keyboard nav and `aria-checked` per-option. **No new dependencies** — built on Phase 8 primitives.
- [ ] T036 [US3] [P] Create `frontend/src/features/scoring/schema.ts` exporting a zod schema mirroring `SubmitScoreRequest`: four required integers 1..5 + optional `comment` (≤ 1000). Export the inferred TS type.
- [ ] T037 [US3] [P] Create `frontend/src/features/scoring/api.ts` exporting `upsertScore(ideaId, body): Promise<IdeaScoreAggregate>` hitting `POST /api/v1/ideas/{id}/scores`. Map 409 `SelfScoringForbidden` / `ScoringClosed` into typed error subclasses so the form can surface them inline.
- [ ] T038 [US3] [P] Create `frontend/src/features/scoring/useUpsertScore.ts` — a TanStack Query mutation hook that invalidates the idea detail query on success and surfaces the typed errors to the form.
- [ ] T039 [US3] Create `frontend/src/features/scoring/ScoreAggregate.tsx` — presentational; renders `Overall`, per-dimension averages, and `Count`. Gracefully renders an empty state when `count === 0`. Depends on T031.
- [ ] T040 [US3] Create `frontend/src/features/scoring/ScorePanel.tsx` orchestrating: (a) four `<ScoreInput />`s + `<Textarea />` for comment + `<Button>Save</Button>`, wired via react-hook-form + the zod schema from T036; (b) shows `<ScoreAggregate />` and a read-only list of `entries` below the form. Visible only when the current user is in the `Admin` role AND `idea.status ∈ {Submitted, UnderReview}`; otherwise renders only the aggregate. Depends on T035..T039.
- [ ] T041 [US3] Mount `<ScorePanel />` on `IdeaDetailPage.tsx`. Depends on T040.
- [ ] T042 [US3] Vitest `frontend/tests/unit/scoring/ScorePanel.test.tsx`:
  - Renders four radio groups + a comment textarea.
  - Submitting `{5, 4, 5, 3, "..."}` calls the mutation with the expected payload.
  - Rejects `0` and `6` via the zod schema (no submit fired).
  - When `idea.status === 'Accepted'`, the form region is not rendered (only aggregate).
  - Mocked 409 `SelfScoringForbidden` surfaces a friendly inline error.

**Checkpoint**: end-to-end scoring works in the UI for admins; aggregate visible to all readers.

---

## Phase 8: Frontend US4 — Score-sort control (FR-018)

- [ ] T043 [US4] Add a sort toggle to `IdeasListPage.tsx` (admin view only): a `<Select>` with options `Default | Highest score | Lowest score` that flips a URL query param `sort=score:desc|asc`. Pass the param through `getIdeas(...)` to the API client. Show the existing `overall` + `reviewerCount` columns from T031 next to each row. Hide unscored ideas' overall as "—" while still rendering them at the bottom of a sorted list.
- [ ] T044 [US4] Playwright `frontend/tests/e2e/scoring.spec.ts`:
  - Sign in as admin, navigate to the admin queue, toggle "Highest score", assert the first row's overall is the maximum in the visible list, assert any "—" rows appear last.

---

## Phase 9: Verification — e2e, perf, docs

- [ ] T045 Playwright `frontend/tests/e2e/blind-review.spec.ts`:
  - Sign in as admin, open the queue; assert the row for a seeded pre-decision idea shows an alias and **does not** contain the seeded submitter's display name or email anywhere in the row's DOM.
  - Click into the idea; assert the detail header shows the alias.
  - Record an `Accept` decision; on the next reload assert the detail header now shows the real name.
- [ ] T046 [P] Perf smoke: re-run the existing list-endpoint benchmark (or add a minimal one under `backend/tests/InnovatEpam.Portal.IntegrationTests/Performance/`) at N=500 ideas with the default sort and with `sort=score:desc`. Assert P95 latency stays within +20 ms of the Phase 1 baseline (NFR-002). Record numbers in this task's notes.
- [ ] T047 [P] Re-run full backend `dotnet test` and full frontend `npm run lint && npm run typecheck && npm run test && npm run build` — all green with `--max-warnings=0` (NFR-004).
- [ ] T048 [P] Update `PROJECT_SUMMARY.md` and `InnovatEPAM-Portal/backend/README.md` with a brief "Phase 6 + 7" section: how to override the alias salt in production, how to opt into score sort, and the audit-trail guarantee around `Decision.WasBlind`. Do **not** generate a separate changelog file unless one already exists.
- [ ] T049 Manual smoke against `docs/manual-testing.md` — append a "Phase 6 + 7" checklist covering: admin sees alias, admin scores idea, admin sorts by score, submitter sees themselves, decision reveals identity.

**Final Checkpoint**: spec acceptance scenarios SC-001..SC-005 all pass; existing suites stay green; docs updated; no new runtime dependencies introduced; nothing committed automatically.

---

## Dependency Graph (high level)

```
Setup (T001..T004)
  └─ Foundation (T005..T017)
       ├─ US1 backend  (T018..T021)
       │     └─ Frontend US1 (T031..T034)
       ├─ US3 backend  (T022..T027)
       │     └─ Frontend US3 (T035..T042)
       └─ US4 backend  (T028..T030)   ← depends on US3 backend
             └─ Frontend US4 (T043..T044)
                   └─ Verification (T045..T049)
```

## Notes

- **No autocommit.** Per the user's instruction, no `git commit` is part of any task; reviewers stage and commit manually.
- **Backward compatibility.** Phase 6/7 only *adds* fields. Existing Phase 1 contract tests must keep passing without modification. T003's snapshot + T030's default-sort test are the safety nets.
- **Secret hygiene.** The alias salt is config-only. Never log it, never echo it in responses, never bake it into the migration. Production deploys override via env var / user-secrets (documented in T004 + T048).
