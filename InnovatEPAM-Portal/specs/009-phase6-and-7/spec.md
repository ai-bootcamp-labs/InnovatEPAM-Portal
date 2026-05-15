# Feature Specification: Phase 6 (Blind Review) + Phase 7 (Multi-Dimension Scoring)

**Feature Branch**: `009-phase6-and-7`
**Created**: 2026-05-15
**Status**: Draft
**Input**: User description: "Phase 6 = anonymous review (hide submitter identity until a decision is made). Phase 7 = multi-dimension 1–5 scoring per idea with aggregation and ranking. Update .NET backend (models/APIs) and React frontend."

## Overview

The InnovatEPAM Portal currently lets admins move ideas through the
`Submitted → UnderReview → Accepted | Rejected` lifecycle by recording a
single `Decision` (Phases 1–5). This feature adds two evaluation-quality
capabilities on top of that flow:

- **Phase 6 — Blind Review.** While an idea is in `Submitted` or
  `UnderReview`, the submitter's identity (name, email, avatar, any
  user-stamped metadata exposed via the API) is suppressed for admins
  and only re-exposed once a terminal `Decision` (Accept / Reject) has
  been recorded. Submitters always see their own ideas as themselves.
- **Phase 7 — Multi-Dimension Scoring.** Admins can record one or more
  **scores** against each idea across a fixed set of evaluation
  dimensions (Impact, Feasibility, Innovation, Alignment) using a 1–5
  integer scale. Per-idea aggregates (per-dimension average, overall
  weighted average, reviewer count) are computed and exposed, and the
  list endpoint supports sorting/ranking by overall score.

Both phases are **additive**: existing routes, validation rules, and
status-transition semantics from Phases 1–5 keep working unchanged.
Anonymous mode is a presentation-layer concern (the database still
stores `SubmitterId`); scoring is a new aggregate sibling to `Decision`,
not a replacement for it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Admin reviews ideas without seeing who submitted them (Priority: P1)

An admin opens the admin queue and sees ideas in `Submitted` /
`UnderReview` with submitter identity hidden (a stable, non-reversible
display label like "Submitter #A7C2" appears instead of name/email).
They can read the idea, attach scores (US3), and record a decision
without ever learning who wrote it. The moment a terminal decision is
saved, the submitter's real identity becomes visible on the **idea
detail** view for that admin (and only that admin role).

**Why this priority**: This is the headline value of Phase 6 — reducing
bias during evaluation. Without it the phase has no effect.

**Independent Test**: Seed two ideas (one `Submitted`, one `Accepted`).
Sign in as admin. (a) `GET /api/v1/ideas` and `GET /api/v1/ideas/{id}`
for the `Submitted` idea return **no** `submitterName`, `submitterEmail`,
or any field that resolves back to a specific user — only an opaque
`submitterAlias`. (b) The same endpoints for the `Accepted` idea return
real submitter details. (c) The admin UI list/detail screens render
the alias for the first idea and the real name for the second.

**Acceptance Scenarios**:

1. **Given** an admin viewing an idea in `Submitted` or `UnderReview`,
   **When** the admin loads the list or detail page, **Then** no
   personally identifying field of the submitter is present in the
   API response or the rendered DOM; an opaque alias and (optionally)
   submission timestamp are shown.
2. **Given** an admin viewing an idea whose latest decision is `Accept`
   or `Reject`, **When** the admin loads the detail page, **Then** the
   submitter's display name and email are visible.
3. **Given** a submitter viewing their **own** ideas, **When** they load
   the list or detail page, **Then** their own identity is visible to
   themselves regardless of status (blind review never hides a user
   from themselves).
4. **Given** an admin moves an idea from `Submitted` → `UnderReview`,
   **When** the admin re-loads the page, **Then** the submitter
   identity remains hidden (blind review covers both pre-terminal
   states).
5. **Given** the system records an Accept/Reject decision, **When**
   the audit/history payload is read, **Then** the historical fact
   that the admin was anonymized at decision time is preserved (e.g.
   `decision.wasBlind = true`) so the audit trail is honest.

---

### User Story 2 — Submitter sees a stable alias used during review (Priority: P2)

A submitter views their idea's public detail page while it is still in
`UnderReview`. The page shows the submitter's real name to themselves
("You") and the comment thread from admins, but any admin-visible
identity references in the page (e.g. "Reviewed by Submitter #A7C2"
context strings) use the alias and never leak the submitter's identity
to other submitters either.

**Why this priority**: Aliasing must be **deterministic** for a given
idea so admins can refer to it consistently across sessions, but it
must never reverse-engineer back to the user across ideas — the alias
is per-idea, not per-user.

**Acceptance Scenarios**:

1. **Given** an idea with `SubmitterId = U` and `Id = I`, **When** the
   alias is computed, **Then** repeated calls return the same string
   (deterministic per idea).
2. **Given** two ideas submitted by the same user, **When** their
   aliases are compared, **Then** they are **different** (per-idea, not
   per-user, so admins cannot cluster a single submitter across ideas).
3. **Given** any alias, **When** it is inspected, **Then** it does not
   contain any prefix or suffix derived from the user's email, name,
   or `SubmitterId` (no leakage by reversal).

---

### User Story 3 — Admin scores an idea across four dimensions (Priority: P1)

An admin opens an idea (`Submitted` or `UnderReview`) and submits a
score record consisting of integer ratings (1–5) for each of four
dimensions: **Impact**, **Feasibility**, **Innovation**, **Alignment**,
plus an optional comment (≤ 1000 chars). After saving, the idea's
detail view shows the admin's score row in the "Reviewer scores" list,
and the aggregate panel updates: per-dimension average, overall
weighted average (equal weights by default = simple mean of the four
dimension averages), and reviewer count.

**Why this priority**: This is the headline value of Phase 7. Without
it the phase has no effect.

**Independent Test**: As admin A score idea X with `{Impact: 5,
Feasibility: 4, Innovation: 5, Alignment: 3, Comment: "..."}`. As admin
B score the same idea X with `{4, 4, 4, 4}`. `GET /api/v1/ideas/{X}`
returns `scores.count = 2`, `scores.averageByDimension = { impact: 4.5,
feasibility: 4, innovation: 4.5, alignment: 3.5 }`, `scores.overall =
4.125`. The idea detail page renders these numbers and lists both
score rows.

**Acceptance Scenarios**:

1. **Given** an admin and an idea in a non-terminal status, **When**
   they POST a score with all four dimensions ∈ {1,2,3,4,5}, **Then**
   the score is stored and the idea aggregate reflects the new row.
2. **Given** an admin has already scored an idea, **When** they POST
   a new score for the same idea, **Then** the existing score is
   **updated** (one score per admin per idea — see edge cases).
3. **Given** an admin tries to score with a dimension value outside
   1..5 or missing a required dimension, **When** the POST is sent,
   **Then** the API returns `400 ValidationProblem` with a per-field
   message; nothing is persisted.
4. **Given** a non-admin user, **When** they attempt to score any
   idea, **Then** the API returns `403`.
5. **Given** an idea whose status is `Accepted` or `Rejected`, **When**
   an admin attempts to add or update a score, **Then** the API returns
   `409` with a "Scoring closed once decided" error code (scoring is
   only meaningful pre-decision).

---

### User Story 4 — Admin ranks the queue by overall score (Priority: P2)

An admin opens the admin queue and can sort the list by overall score
(descending by default) to prioritise the highest-rated ideas. The
list view shows each idea's overall score and reviewer count next to
its alias and status.

**Acceptance Scenarios**:

1. **Given** at least three ideas with overall scores 3.1, 4.7, 4.7
   and creation order A, B, C, **When** the admin requests
   `GET /api/v1/ideas?sort=score:desc`, **Then** results return in the
   order [highest, highest, lowest] with a documented tie-breaker
   (oldest `CreatedAt` first within a tie).
2. **Given** an idea has zero scores, **When** the list is sorted by
   score, **Then** unscored ideas appear **last** (treated as `null`,
   not as 0) regardless of sort direction.
3. **Given** sorting is not specified, **When** the list is requested,
   **Then** the existing default ordering from Phase 1 is preserved
   (no behavioural change for non-opt-in callers).

---

### Edge Cases

- **One score per admin per idea**: the unique constraint is
  `(IdeaId, ReviewerId)`. POSTing twice acts as an upsert; the API
  documents this explicitly and returns `200 OK` (update) vs `201
  Created` (insert).
- **Self-scoring**: an admin who is *also* the submitter (rare but
  possible during development/seeding) MUST NOT be able to score
  their own idea — returns `409 SelfScoringForbidden`.
- **Alias collision**: aliases are derived from a deterministic hash
  of `(IdeaId)` truncated to four hex chars; collisions inside a single
  admin's visible queue are acceptable but the underlying `IdeaId`
  remains the disambiguator.
- **Audit honesty**: a decision recorded while blind mode was active
  carries `wasBlind = true` so retrospective reports can distinguish
  blind-mode decisions from any future non-blind decisions.
- **Score retraction**: deleting a score is out of scope for this
  phase; admins can only insert or update. (Listed as a deferred D1.)
- **Reduced-motion / a11y**: the 1–5 score input MUST be reachable by
  keyboard with visible focus and announce its current value to
  screen readers (radio-group semantics, not a custom slider).
- **Bundle / perf**: aggregates are computed server-side; the list
  endpoint MUST NOT issue an N+1 query for scores (FR-019).

## Requirements *(mandatory)*

### Functional — Phase 6 (Blind Review)

- **FR-001**: The Ideas API (`GET /api/v1/ideas`, `GET /api/v1/ideas/{id}`)
  MUST return a `submitterAlias` string instead of `submitterName` /
  `submitterEmail` when **all** of: (a) the caller is in the `Admin`
  role, (b) the idea's latest `Decision.Action` is not `Accept` or
  `Reject`. In all other cases the existing identity fields are
  returned unchanged.
- **FR-002**: The `submitterAlias` MUST be deterministic per idea, MUST
  differ for two ideas authored by the same submitter, and MUST NOT be
  reversible to `SubmitterId` / email / name without server-side data
  (i.e. a salted hash of `IdeaId` truncated to a short display token).
- **FR-003**: Submitters viewing their **own** ideas MUST always see
  their real identity regardless of status (FR-001 is an admin-only
  redaction).
- **FR-004**: When a decision is recorded, the persisted `Decision`
  row MUST capture `WasBlind = true` (boolean column) reflecting that
  blind mode was active at decision time, so the audit log remains
  honest if blind mode is ever disabled later.
- **FR-005**: Notifications already produced by the existing decision
  pipeline MUST NOT leak the submitter's identity through their
  payload back to admin-facing UI; notifications targeted at the
  submitter themselves are unaffected.

### Functional — Phase 7 (Multi-Dimension Scoring)

- **FR-006**: A new `IdeaScore` aggregate sibling to `Decision` exists,
  keyed by `(IdeaId, ReviewerId)`, with integer columns `Impact`,
  `Feasibility`, `Innovation`, `Alignment` constrained to `1..5`
  (CHECK constraint at the DB layer + domain validation), an optional
  `Comment` (≤ 1000 chars), and `CreatedAt` / `UpdatedAt` audit columns.
- **FR-007**: `POST /api/v1/ideas/{id}/scores` MUST upsert the calling
  admin's score for the idea: insert returns `201 Created`, update
  returns `200 OK`, both return the updated aggregate snapshot.
- **FR-008**: Scoring MUST be limited to callers in the `Admin` role
  (`403` otherwise) and to ideas whose current status is `Submitted`
  or `UnderReview` (`409 ScoringClosed` for `Accepted` / `Rejected`).
- **FR-009**: An admin MUST NOT score an idea they themselves
  submitted (`409 SelfScoringForbidden`).
- **FR-010**: Per-dimension averages MUST be exposed on the idea
  detail payload as `scores.averageByDimension.{impact, feasibility,
  innovation, alignment}` (rounded to two decimals).
- **FR-011**: The overall score MUST be the simple mean of the four
  per-dimension averages (equal-weight default), rounded to two
  decimals, exposed as `scores.overall`.
- **FR-012**: The idea detail payload MUST include `scores.count`
  (number of distinct reviewers) and a list `scores.entries` of
  individual reviewer scores. **In blind mode**, each entry MUST use
  the reviewer's `Admin` role + a deterministic reviewer alias
  (`Reviewer #...`) instead of their real identity — even other admins
  do not see who scored what until the idea is decided.
- **FR-013**: `GET /api/v1/ideas?sort=score:asc|desc` MUST return
  results ordered by `scores.overall`, with unscored ideas placed
  last (NULLS LAST equivalent regardless of direction). Ties are
  broken by `CreatedAt` ascending.
- **FR-014**: The list endpoint MUST be able to return each idea's
  `overall` and `reviewerCount` without issuing per-row score queries
  (single SQL pass, e.g. a `LEFT JOIN LATERAL` or a pre-aggregated
  projection).
- **FR-015**: All existing Phase 1 fields, contracts, and validation
  rules MUST keep working without change for clients that do not opt
  into the new query / payload fields.

### Functional — Frontend

- **FR-016**: The admin queue list MUST render `submitterAlias` in
  place of submitter name while a row is in `Submitted` / `UnderReview`,
  switch to real identity once the row is `Accepted` / `Rejected`, and
  display `overall` + `reviewerCount` next to each row when present.
- **FR-017**: The idea detail page MUST render a `<ScorePanel>`
  component that (a) for admins on non-terminal ideas, exposes four
  1–5 radio-group inputs + comment + Save button wired to the upsert
  endpoint, (b) for everyone with read access, shows the aggregate
  panel (per-dimension average, overall, reviewer count) and the
  list of reviewer score rows (using reviewer aliases while blind).
- **FR-018**: The admin queue MUST offer a "Sort by score" control
  that toggles `?sort=score:desc` and shows unscored ideas last.

### Non-Functional

- **NFR-001**: API additions are **additive** — no existing endpoint
  changes its response shape in a breaking way for non-admin callers
  or for callers that do not request the new sort. Admin clients
  MUST handle both the new alias field and the legacy name fields
  during the rollout window.
- **NFR-002**: List endpoint response time MUST stay within +20 ms
  P95 of the Phase 1 baseline at N=500 ideas (verify via the existing
  perf smoke). No N+1 queries on the list path.
- **NFR-003**: Score input MUST meet WCAG 2.1 AA — keyboard reachable,
  visible focus ring (re-uses the Phase 8 `ring` token), screen-reader
  announces the current dimension and selected value.
- **NFR-004**: Lint + typecheck + unit + integration suites stay green
  with `--max-warnings=0`.
- **NFR-005**: Database migrations MUST be **forward-only** and safely
  re-runnable; the `IdeaScores` table and `Decisions.WasBlind` column
  ship in a single new migration.

### Out of Scope (Deferred)

- **D1**: Score deletion / retraction (admins can only insert or
  update).
- **D2**: Configurable per-dimension weights (equal-weight default
  is fixed for Phase 7).
- **D3**: Reviewer disagreement metrics (variance, stddev) — only the
  mean is exposed.
- **D4**: Permanent un-blinding (an admin "reveal" override). Identity
  reveal is strictly tied to a recorded terminal decision.
- **D5**: Multiple decisions per idea / score history — both `Decision`
  and `IdeaScore` keep their existing "one row per (idea, actor)"
  semantics.

## Success Criteria

- **SC-001**: All four acceptance scenarios under US1 pass against a
  seeded fixture (1 `Submitted` + 1 `Accepted` idea, 1 admin, 1
  submitter).
- **SC-002**: `GET /api/v1/ideas` returns the alias field for every
  pre-decision idea when the caller is admin; auditing the JSON for
  the string forms of any seeded submitter's name/email yields zero
  matches.
- **SC-003**: Two admins scoring the same idea produce the documented
  aggregate (US3 independent test) and exactly one row per `(IdeaId,
  ReviewerId)` exists in `IdeaScores`.
- **SC-004**: `?sort=score:desc` on a seeded queue of 10 ideas returns
  the expected ranking, with unscored ideas appearing last, in a
  single DB round-trip (verified via captured EF Core SQL log).
- **SC-005**: The Phase 1 + Phase 8 test suites continue to pass
  unchanged; new tests cover the alias, scoring upsert, RBAC, and
  ranking paths.
