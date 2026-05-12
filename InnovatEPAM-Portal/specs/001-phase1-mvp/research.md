# Phase 0 — Research: Phase 1 MVP

**Feature**: Phase 1 MVP — InnovatEPAM Portal
**Date**: 2026-05-12
**Status**: Complete — no `NEEDS CLARIFICATION` items remain.

The constitution and the user-supplied technology constraints (.NET 8 Web API +
EF Core + PostgreSQL backend, React + Tailwind + shadcn/ui frontend, **date-fns**
for date formatting) collapse most "what tech?" questions. This file records the
remaining technology decisions inside that envelope.

---

## R1 — API authentication: ASP.NET Core Identity + JWT bearer

- **Decision**: Use ASP.NET Core Identity for the user store and password
  hashing (PBKDF2). Issue short-lived access tokens (60 min) signed with HMAC
  SHA-256 via `Microsoft.AspNetCore.Authentication.JwtBearer`. No refresh
  tokens in Phase 1 — re-login on expiry.
- **Rationale**: Identity ships with the framework (no extra dependency, FR-006
  / FR-007 satisfied for free). JWT bearer fits a SPA + REST split cleanly and
  avoids cookie/CSRF complexity for cross-origin calls. Short-lived tokens
  mitigate the lack of refresh.
- **Alternatives considered**:
  - **Cookie auth**: simpler for same-origin, but requires CSRF protection and
    couples session state to the API host; a SPA + reverse-proxy deploy is
    more flexible with bearer tokens.
  - **Identity Server / OpenIddict**: overkill for Phase 1 (single client, no
    federation); reserved for Phase 2 SSO.

## R2 — Persistence: EF Core 8 with Npgsql, code-first migrations

- **Decision**: Single `PortalDbContext` derived from `IdentityDbContext<AppUser, AppRole, Guid>`.
  Code-first migrations checked into source control. Use Npgsql snake_case
  naming convention (via `EFCore.NamingConventions`) so the schema reads
  natively in PostgreSQL tooling.
- **Rationale**: One context = one transaction boundary, simpler unit-of-work.
  `EFCore.NamingConventions` is a 7 KB single-purpose package — the only
  exception to the "framework primitives only" preference, justified because
  manual `[Table]/[Column]` attributes on every property would be far more
  duplication and a constitution risk (Principle I).
- **Alternatives considered**:
  - **Dapper**: raw control but loses migrations, change tracking, and
    Identity integration; not warranted at this scale.
  - **Default PascalCase tables**: uglier in `psql`, requires quoting in raw
    SQL — net cost outweighs the saved dependency.

## R3 — File attachments: filesystem behind `IAttachmentStorage`

- **Decision**: Phase 1 stores attachments on the local filesystem at a
  configurable root (e.g. `/var/innovatepam/attachments/{yyyy}/{mm}/{guid}`).
  All access goes through an `IAttachmentStorage` abstraction with
  `SaveAsync`, `OpenReadAsync`, `DeleteAsync`. The DB stores metadata only.
- **Rationale**: Keeps the DB lean; avoids a cloud dependency in Phase 1; the
  abstraction lets us swap to Azure Blob Storage later with no API or domain
  changes. Aligns with Principle IV (Minimal Dependencies).
- **Alternatives considered**:
  - **`bytea` in PostgreSQL**: simple operationally but bloats backups and
    forces large-object handling; rejected at the 10 MB / 1 000 ideas scale.
  - **Azure Blob Storage in Phase 1**: introduces an SDK + secrets management
    burden before it is needed.

## R4 — Upload validation: dual extension + content-type check

- **Decision**: Validate uploads with three gates: (1) `Content-Length` ≤ 10 MB
  enforced both at Kestrel (`MaxRequestBodySize`) and in middleware, (2)
  extension allow-list (`.pdf`, `.png`, `.jpg`, `.jpeg`, `.docx`, `.pptx`,
  `.xlsx`), (3) magic-byte sniffing against the declared content type using a
  small in-house lookup (≤ 200 LoC) — no third-party scanner in Phase 1.
- **Rationale**: FR-015 requires content-aware validation. Magic-byte sniffing
  catches the common "rename .exe → .pdf" attack without adding a dependency.
- **Alternatives considered**:
  - **MIME-detection library** (e.g., `MimeDetective`): adds a dependency for
    7 file types we already know; rejected per Principle IV.
  - **AV scan via ClamAV**: deferred to Phase 2 when attachments leave the
    application boundary.

## R5 — Error responses: RFC 7807 ProblemDetails

- **Decision**: Use ASP.NET Core's built-in `ProblemDetails` and
  `ValidationProblemDetails`. Centralise via
  `app.UseExceptionHandler("/error")` plus a `IExceptionHandler` for known
  domain errors (NotFound, Conflict, Forbidden). Type URIs are stable and
  documented in OpenAPI.
- **Rationale**: Constitution Principle II mandates RFC 7807; built-in support
  means zero added dependencies.

## R6 — API versioning: URL prefix `/api/v1/`

- **Decision**: All controllers route under `/api/v1/...`. Use route
  attributes only (no `Microsoft.AspNetCore.Mvc.Versioning` package) — for one
  version, a constant prefix is the simplest correct thing.
- **Rationale**: Constitution Principle II requires URL versioning. Adding the
  versioning package now would be premature.

## R7 — Logging: Serilog with JSON sink, request-scoped enrichment

- **Decision**: Serilog with `Serilog.AspNetCore`, JSON console sink in
  containers, file rolling sink in dev. Enrich with `RequestId`, `UserId` (when
  authenticated), and `Endpoint`. Forbid logging of password fields, JWTs, and
  attachment payloads via a custom destructuring policy.
- **Rationale**: Constitution requires structured logging and forbids PII.
  Serilog is the de-facto .NET choice and already permitted by Principle IV.

## R8 — Frontend data fetching: TanStack Query v5

- **Decision**: TanStack Query for server state, with mutations invalidating
  the relevant `ideas` query keys. No global state library (no Redux/Zustand).
  Auth state lives in a small React context backed by `localStorage` for the
  JWT.
- **Rationale**: TanStack Query handles caching, retries, loading/error
  states, and request dedup out of the box — directly serves Principle III's
  four-state requirement (loading/empty/error/success).
- **Alternatives considered**:
  - **Plain `fetch` + `useEffect`**: re-invents caching and suffers race
    conditions; rejected.
  - **Redux Toolkit Query**: heavier, ties us to Redux; rejected.

## R9 — Frontend forms: react-hook-form + zod

- **Decision**: `react-hook-form` for form state and `zod` for schemas. Reuse
  the same zod schema to type the API request body and the form values.
- **Rationale**: Smallest credible combination for accessible, validated forms
  with shadcn/ui's `Form` primitives, which expect `react-hook-form`. Zod
  schemas double as TypeScript types via `z.infer`, removing duplication
  (Principle I).

## R10 — Date formatting: date-fns v3 (user-mandated)

- **Decision**: Use **date-fns v3** for all date formatting and relative-time
  display on the frontend. Centralise formatters in `frontend/src/lib/date.ts`:
  - `formatIdeaDate(date) → "12 May 2026"` (`format(d, 'd LLL yyyy')`)
  - `formatIdeaDateTime(date) → "12 May 2026, 14:30"` (`format(d, 'd LLL yyyy, HH:mm')`)
  - `formatRelative(date) → "3 hours ago"` (`formatDistanceToNow(d, { addSuffix: true })`)
- **Rationale**: User explicitly required date-fns. v3 is tree-shakeable and
  ESM-first → bundle impact under the 20 KB budget when only the three helpers
  above are imported by name. No `Intl.DateTimeFormat` wrapping needed for
  Phase 1 (English only, per spec assumption).
- **Alternatives considered**:
  - **`Intl.DateTimeFormat` only**: zero deps, but no relative-time helper
    (Phase 1 wants "3 hours ago" on idea cards) → would re-implement; rejected.
  - **Day.js / Luxon**: not user-requested; rejected.

## R11 — Component library: shadcn/ui via CLI, no copy-paste sprawl

- **Decision**: Use the shadcn/ui CLI to add primitives to
  `frontend/src/components/ui/`. Forms use `Form`, `Input`, `Textarea`,
  `Select`, `Button`; layout uses `Card`, `Sheet`, `Dialog`, `Table`. No
  bespoke CSS files (Principle III).
- **Rationale**: Constitution mandates shadcn/ui. CLI keeps additions
  reproducible and pinned by component version.

## R12 — Backend testing: Testcontainers for PostgreSQL

- **Decision**: Integration tests spin up a throwaway PostgreSQL 15 container
  per fixture using `Testcontainers.PostgreSql` and `WebApplicationFactory`.
  Migrations are applied at fixture start; each test uses a savepoint or a
  fresh schema.
- **Rationale**: Tests run against the real database engine — catches
  PostgreSQL-specific bugs (case sensitivity, JSONB, citext) that an in-memory
  provider would mask. Dev workflow gate #6 requires migrations to be tested
  against a fresh DB.
- **Alternatives considered**:
  - **`Microsoft.EntityFrameworkCore.InMemory`**: well-documented as a poor
    fidelity provider; rejected.
  - **Shared dev database**: flaky in CI; rejected.

## R13 — Authorization model: two policies

- **Decision**: Two policies registered in `Program.cs`:
  - `RequireAuthenticatedUser` (default)
  - `RequireAdmin` (requires `Role == "Admin"`)
  Decorate Admin-only endpoints with `[Authorize(Policy = "Admin")]`.
- **Rationale**: FR-005 has only two roles. Policies (not raw `[Authorize(Roles=)]`)
  centralise the rule for the audit/SC-005 test ("0 % bypass").

## R14 — Notifications: in-portal only, polled

- **Decision**: A `notifications` table holds per-user notifications. The SPA
  polls `/api/v1/notifications/unread-count` every 30 s while focused. No
  WebSockets / SignalR in Phase 1.
- **Rationale**: FR-022 requires in-portal notification at minimum. Polling is
  trivial, keeps the dep surface small, and the 30 s cadence is well under any
  user-perceived freshness threshold for an evaluation workflow.

## R15 — Dev/test data seeding

- **Decision**: An `Infrastructure.Seed` static method seeds the closed
  category list (Process, Product, Technology, People, Other) and one Admin
  user from configuration (`Seed:AdminEmail`, `Seed:AdminPassword`). Runs
  only when the DB is empty.
- **Rationale**: Spec assumption: Admin promoted out-of-band. Config-driven
  seed satisfies that without exposing a self-service admin route.
