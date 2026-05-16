# InnovatEPAM Portal



An internal innovation idea catalog for EPAM employees. Engineers submit
ideas, follow them through a moderation workflow
(`Submitted → Under Review → Accepted / Rejected`), attach supporting files,
and receive in-app notifications. Administrators triage the queue **blind**
(submitter identity is hidden until a decision is recorded), score ideas
across four evaluation dimensions, and record decisions with full audit
history.

The full backend stack runs from a single `docker compose up --build`.

---

## What was built

This repository is the cohort submission for **EPAM AI Bootcamp A201**. It
was developed Spec-Kit-first: every feature ships as a `specs/NNN-<slug>/`
folder with a `spec.md`, `plan.md`, and a numbered `tasks.md`, and every
task closes against explicit acceptance criteria. The four constitution
pillars (Clean Code, REST, UI/UX, Minimal Deps) gate every PR.

The rubric's eight feature phases are all complete; on top of them, a
ninth **UI Polish** phase (`specs/008-ui-polish`) was added as a
presentational refresh of the SPA. Documentation is the final phase.

### Setup & Spec Kit

- Initialised the monorepo (`backend/`, `frontend/`, `docs/`, `specs/`).
- Installed Spec Kit and authored the project constitution
  (`.specify/memory/constitution.md`) with the four pillars **Clean
  Code · REST · UI/UX · Minimal Deps** that gate every PR.
- Stood up the local stack as a single `docker compose up --build` —
  multi-stage `Dockerfile` for the API, Postgres 15 with a healthcheck,
  EF Core migrations applied on container start, admin seeded from
  config.
- Wired CI (`.github/workflows/ci.yml`) running `dotnet test` and the
  full frontend gate (`npm run lint && npm run typecheck && npm run
  test && npm run build`).

### Phase 1 — Core Portal (`specs/001-phase1-mvp`)

The end-to-end product slice that turns the portal from idea to MVP.

- **Authentication & identity** — ASP.NET Identity with local email +
  password, JWT bearer tokens, `Submitter` and `Admin` roles, hashed +
  salted password storage, password-strength rules at registration, and
  out-of-band admin seeding (`Seed:AdminEmail` / `Seed:AdminPassword`).
- **Idea catalog** — Paginated listing (page size 20, newest-first) with
  status and category filters; detail view shows full description,
  category, submitter, attachment download link, decision comment (when
  present), and the full status-history audit trail.
- **Notifications** — In-portal notifications fire when an idea changes
  status; the notifications panel lists unread / read entries and
  supports mark-as-read.
- **Cross-cutting** — Auth required on every route except register /
  login / static assets, Serilog structured logging of security-relevant
  events (no passwords, no attachment contents), and a health-check trio
  (`/health`, `/health/live`, `/health/ready` — readiness pings
  Postgres).

### Phase 2 — Smart Submission Forms (`specs/001-phase1-mvp`)

Field-level validation across the submission flow.

- **Server-side rules** — FluentValidation guards on every request:
  title required and 5 – 120 chars, description required and ≤ 4 000
  chars, category required and constrained to the fixed seed list,
  registration password strength (≥ 8 chars including letters and
  digits).
- **Client-side mirror** — React Hook Form + Zod schemas mirror the
  server rules so submitters get inline, per-field error messages
  before the request is sent.
- **Helpful failure modes** — Validation errors are returned as RFC 7807
  Problem Details mapped to field-level messages in the UI;
  registration with an existing email surfaces a generic
  "could not create account" message so no account-existence info
  leaks. The form preserves user-entered state when the server rejects
  the submission so nothing is retyped.

### Phase 3 — Multi-Media Support (`specs/001-phase1-mvp`)

Attachment handling for the New Idea form.

- **Allow-list with content-type validation** — Zero or one attachment
  per idea. Permitted types: PDF, PNG, JPG / JPEG, DOCX, PPTX, XLSX.
  Maximum size 10 MB. Uploads are validated against the actual content
  type, not the file extension, so a renamed `.exe` is refused.
- **Streamed storage** — Files are streamed to disk under
  `Attachments:RootPath` (mounted as a named Docker volume), keyed by
  a server-generated `Attachment` id; original filename, content type,
  size, and upload timestamp are persisted alongside.
- **Safe download** — `GET /api/v1/attachments/{id}` requires
  authentication and returns the binary with the original filename;
  missing-file responses surface as a friendly "attachment not
  available" message instead of a generic 500.

### Phase 4 — Draft Management (`specs/001-phase1-mvp`)

Keeps in-progress work intact across rejection / refresh cycles.

- **Form-state preservation** — React Hook Form retains every typed
  field after a failed submit (validation error, oversized file,
  disallowed type, network failure), so submitters never lose work
  while iterating.
- **Session resume** — JWT lifetime is configurable
  (`Jwt:AccessTokenLifetimeMinutes`, default 60 min). On token
  expiry the SPA preserves the unsubmitted form state and re-routes
  the user back to the New Idea form after re-authentication.
- **Single source of truth** — Once published, an idea is immutable
  for Phase 1 (editing post-submission is explicitly out of scope per
  spec assumptions); the persisted idea, its attachment, and its audit
  history are the durable record.

### Phase 5 — Multi-Stage Review (`specs/001-phase1-mvp`)

The admin moderation workflow with full audit history.

- **Status machine** — `Submitted → Under Review → Accepted / Rejected`.
  Admins move ideas through these transitions; `Accepted` and
  `Rejected` are **terminal** and further changes are refused at the
  API (`409 Conflict`).
- **Decisions** — Accept / Reject require a non-empty admin comment
  (1 – 2 000 chars). Each decision is persisted with actor, action,
  timestamp, and comment.
- **Audit trail** — Every status change writes a `StatusHistoryEntry`
  with prior status, new status, actor, timestamp, and optional
  comment, all surfaced on the idea detail page so submitters can see
  who decided and why.
- **RBAC enforcement** — Submitter-role accounts that attempt admin
  endpoints receive `403 Forbidden`; the SPA also hides admin-only
  controls client-side as a UX cue.

### Phase 6 — Blind Review (`specs/009-phase6-and-7`)

Reduces evaluator bias by hiding submitter identity from admins until a
decision is recorded.

- **Server-side redaction.** `GET /api/v1/ideas` and
  `GET /api/v1/ideas/{id}` return a deterministic `submitterAlias`
  (e.g. `Submitter #A7C2`) instead of `submitterName` / `submitterEmail`
  whenever the caller is in the `Admin` role **and** the idea's latest
  decision is not `Accept` / `Reject`. Real identity is re-revealed on
  terminal statuses.
- **Per-idea, non-reversible aliases.** Aliases are derived via
  HMAC-SHA256 keyed by `BlindReview:AliasSalt`, truncated to four hex
  chars. The alias is per-idea, not per-user — two ideas from the same
  submitter produce different aliases, so admins cannot cluster a
  submitter across the queue. Rotating the salt invalidates historical
  aliases by design.
- **Submitters always see themselves.** Blind redaction is admin-only;
  a submitter viewing their own idea sees their real identity regardless
  of status.
- **Honest audit.** Every `Decision` row carries a `WasBlind = true`
  flag captured at decision time, so retrospective reports can
  distinguish blind-mode decisions from any future non-blind ones.

### Phase 7 — Scoring System (`specs/009-phase6-and-7`)

A 1 – 5 multi-dimension evaluation rubric layered on top of (not instead
of) the Decision workflow.

- **`IdeaScore` aggregate.** New table keyed by `(IdeaId, ReviewerId)`
  with integer columns `Impact`, `Feasibility`, `Innovation`, `Alignment`
  constrained to `1..5` (DB CHECK constraint + domain validation), an
  optional `Comment` (≤ 1 000 chars), and `CreatedAt` / `UpdatedAt`.
- **Upsert API.** `POST /api/v1/ideas/{id}/scores` inserts (`201`) or
  updates (`200`) the calling admin's score and returns the refreshed
  aggregate. Admin-only (`403` otherwise). Closed once the idea is
  decided (`409 ScoringClosed`). Self-scoring is forbidden
  (`409 SelfScoringForbidden`).
- **Aggregates exposed.** `scores.averageByDimension.{impact,
  feasibility, innovation, alignment}`, `scores.overall` (simple mean
  of the four dimension averages, rounded to two decimals),
  `scores.count`, and a `scores.entries` list. While blind mode is
  active, individual entries surface reviewer aliases — admins do not
  see who scored what until the idea is decided.
- **Ranking.** `GET /api/v1/ideas?sort=score:desc|asc` orders by
  overall score in a single SQL pass (no N+1), with unscored ideas
  placed **last** regardless of direction and `CreatedAt` ascending as
  the tie-breaker.
- **Frontend.** The idea detail page renders a `<ScorePanel>` with four
  accessible radio-groups (1 – 5), an optional comment, and a Save
  button wired to the upsert. The admin queue exposes a *Sort by score*
  control and displays `overall` + `reviewerCount` next to each row.

### Phase 8 — UI Polish (additional, `specs/008-ui-polish`)

A purely presentational refresh of the React SPA, layered on after
the seven feature phases. No routes, no API contracts, no data shapes
changed. Internally tracked as five sub-phases in `008-ui-polish/tasks.md`:

- **Foundations.** Introduced HSL semantic Tailwind tokens (color,
  spacing, radius, typography, shadow, motion) and the four
  idea-status token pairs in `frontend/src/index.css`; extended
  `tailwind.config.ts` with `status.*` colours and motion utilities;
  added the `cn()` (`clsx` + `tailwind-merge`) helper; built the
  ten-primitive barrel under `frontend/src/components/ui/`
  (`Button` / `Input` / `Textarea` / `Select` / `Label` / `FieldError`
  / `StatusBadge` / `Card` / `EmptyState` / `LoadingSkeleton`);
  shipped a shared `Container` and refactored `AppShell` (top bar,
  primary nav, user menu, notifications affordance, mobile hamburger
  collapse) with a shared focus-visible ring and `motion-safe:`-gated
  transitions.
- **Overall page polish (US1).** Audited every page under
  `features/**` and `app/**`, removed ad-hoc horizontal / vertical
  spacing, and wrapped each page body in the shared
  `Container as="main"` with a consistent page-header pattern.
  Replaced plain `<a>` / `<button>` navigational and primary actions
  with `Button` variants and ring-styled `<Link>`s so every
  interactive element shares the same focus-visible ring (FR-004).
  Axe smoke on `/login`, `/ideas`, `/ideas/new`, `/ideas/:id` returns
  zero serious / critical violations.
- **Forms feel deliberate (US2).** Migrated `Login`, `Register`,
  `IdeaSubmitForm`, and the admin `DecisionDialog` onto `Label` +
  `Input` / `Textarea` / `Select` + `FieldError` + `Button`, with
  `aria-describedby` wiring each field to its inline error. The raw
  `<input type="file">` on the New-Idea form is kept (visually
  hidden, `sr-only`) so the existing accept / size validation still
  runs, while a `<label>` rendered as a styled drop / select target +
  filename chip provides the visible affordance. Every submit button
  drives the shared loading pattern (`aria-busy="true"`, inline
  `Spinner`, disabled inputs while in flight).
- **Lists, detail, queue, app shell cohesion (US3).** Brought the
  idea list, idea detail, admin queue, and notifications panel onto
  one visual language: shared `StatusBadge` everywhere status is
  rendered (icon + text, never colour-only — FR-006, SC-003),
  `EmptyState` for every empty list, `LoadingSkeleton` + `Spinner`
  for in-flight data instead of bare "Loading…" strings, and `Card`
  with the `hoverable` variant for navigable rows.
- **Verification gate.** ESLint `--max-warnings=0`, 70 / 70 unit
  tests, production build green, CSS bundle 4.60 KB gzipped (well
  inside the 13.84 KB cap = baseline 3.84 KB + 10 KB NFR-002 budget),
  **zero new runtime dependencies** introduced by the refresh.

### Documentation

Finalised this README, the shared-UI primitives reference, the manual
testing checklist under `docs/manual-testing.md`, and the reviewer
checklist in each spec's `quickstart.md`.

---

## Tech stack

| Layer            | Technology                                                   |
|------------------|--------------------------------------------------------------|
| Backend          | **.NET 8** Web API — ASP.NET Core, EF Core (PostgreSQL), ASP.NET Identity, JWT bearer, FluentValidation, Serilog, Swashbuckle |
| Frontend         | **React 18 + Tailwind CSS** — TypeScript, Vite, React Query, React Hook Form, Zod, React Router |
| Database         | **PostgreSQL 15**                                            |
| Containerization | **Docker** + Docker Compose — multi-stage `Dockerfile` for the API; named volumes for Postgres data + attachments |
| Tooling          | Spec Kit, xUnit, Vitest, Playwright, ESLint, Prettier        |

---

## Repository layout

```text
backend/    .NET 8 Web API — 4 projects (Api, Application, Domain,
            Infrastructure) + 2 test projects (UnitTests,
            IntegrationTests).
frontend/   React 18 + TypeScript + Vite SPA. Shared UI primitives at
            src/components/ui/. Vitest unit tests + Playwright E2E.
docs/       Reference documentation (manual-testing.md, etc.).
specs/      Spec Kit feature folders:
              001-phase1-mvp/   — Phases 1–5 (Core Portal, Smart Forms,
                                  Multi-Media, Draft Management,
                                  Multi-Stage Review)
              009-phase6-and-7/ — Phases 6–7 (Blind Review + Scoring)
              008-ui-polish/    — Additional Phase 8 (Tailwind UI refresh)
.specify/   Spec Kit configuration + constitution.
```

---

## Prerequisites

| Tool         | Version       | Required for                             |
|--------------|---------------|------------------------------------------|
| **Docker**   | Desktop 4.30+ (Compose v2) | Running the backend + database stack     |
| **.NET 8 SDK** | 8.0.x       | Local backend development outside Docker, EF Core migrations, running unit tests |
| **Node.js**  | 20 LTS or newer | Frontend dev server (`npm run dev`), tests, build |

> Docker alone is enough to run the API + Postgres. The .NET SDK and Node.js
> are only required if you want to develop the backend or frontend outside the
> containers (recommended for fast iteration on the SPA — HMR runs on the
> host).

---

## How to run locally

### 1. Start the backend stack (one command)

From the repository root:

```powershell
docker compose up --build
```

This builds the multi-stage API image, starts Postgres 15, waits for its
healthcheck, applies pending EF Core migrations (including the
`IdeaScores` table and `Decisions.WasBlind` column added in Phase 6 / 7),
and seeds the admin account.

When the containers are healthy the API is reachable at:

- API base — `http://localhost:5222/api/v1`
- Swagger  — `http://localhost:5222/swagger`
- Health   — `http://localhost:5222/api/v1/health`

The seeded admin credentials (configured under `Seed:*` in
`appsettings.Development.json`) default to:

- **Email:** `admin@innovatepam.test`
- **Password:** `Adm1n!Pass`

To stop the stack:

```powershell
docker compose down            # keep data
docker compose down -v         # also wipe Postgres + attachments volumes
```

### 2. Start the frontend dev server

In a second terminal:

```powershell
cd frontend
npm install        # first time only
npm run dev
```

The Vite dev server runs on `http://localhost:5173` and is pre-configured to
talk to `http://localhost:5222/api/v1`. CORS for that origin is allow-listed
in the `api` service environment.

### 3. Run the tests

```powershell
# backend (xUnit — unit + integration)
cd backend
dotnet test

# frontend (lint, unit via Vitest, production build)
cd ../frontend
npm run lint
npm run test
npm run build
```

### Optional: run the backend on the host (without Docker)

If you'd rather run the API via `dotnet run`, start only Postgres from
compose:

```powershell
docker compose up postgres
cd backend/src/InnovatEpam.Portal.Api
dotnet run
```

The default `appsettings.Development.json` already points at
`Host=localhost;Port=5433` to match the host-published Postgres port.

---

## API surface (v1)

All endpoints are prefixed with `/api/v1` and require a JWT bearer token
except `auth/*` and `health/*`.

| Method | Route                                  | Roles            | Purpose                                                              |
|--------|----------------------------------------|------------------|----------------------------------------------------------------------|
| POST   | `/auth/register`                        | anonymous        | Self-serve registration (assigns `Submitter`).                       |
| POST   | `/auth/login`                           | anonymous        | Issue a JWT.                                                         |
| POST   | `/auth/logout`                          | authenticated    | Invalidate the current session.                                      |
| GET    | `/ideas`                                | authenticated    | Paginated list. Supports `?status=`, `?category=`, `?sort=score:desc\|asc`. Returns `submitterAlias` for admins on non-terminal ideas. |
| GET    | `/ideas/{id}`                           | authenticated    | Idea detail with attachment link, audit history, and `scores` aggregate. |
| POST   | `/ideas`                                | Submitter, Admin | Create an idea (multipart for attachment).                           |
| POST   | `/ideas/{id}/decisions`                 | Admin            | Move to `UnderReview` / `Accept` / `Reject`. Stamps `WasBlind` from current blind-mode state. |
| POST   | `/ideas/{id}/scores`                    | Admin            | Upsert the calling admin's 1–5 score across four dimensions.         |
| GET    | `/attachments/{id}`                     | authenticated    | Download the attachment (404 if missing).                            |
| GET    | `/notifications`                        | authenticated    | List the caller's notifications.                                     |
| POST   | `/notifications/{id}/read`              | authenticated    | Mark a notification as read.                                         |
| GET    | `/health`, `/health/live`, `/health/ready` | anonymous     | Liveness / readiness probes (readiness pings Postgres).             |

---

## Health

| Endpoint                  | Purpose                                  | Auth |
|---------------------------|------------------------------------------|------|
| `GET /api/v1/health`       | Liveness / probe smoke (always returns ok) | none |
| `GET /api/v1/health/live`  | Liveness — process is up                 | none |
| `GET /api/v1/health/ready` | Readiness — Postgres reachable           | none |

---

## Shared UI primitives

The frontend exposes a single barrel at `@/components/ui` (see
[frontend/src/components/ui/index.ts](InnovatEPAM-Portal/frontend/src/components/ui/index.ts)).
Feature pages import from the barrel only — never reach into individual files.
The ten primitives plus the `cn()` class-merge helper cover every interactive
surface in the SPA; see [specs/008-ui-polish/](InnovatEPAM-Portal/specs/008-ui-polish/) for the
full spec and reviewer checklist.

| Primitive          | Purpose                                                    |
|--------------------|------------------------------------------------------------|
| `Button`           | All clickable actions (variants: `primary`, `secondary`, `ghost`, `destructive`, `link`). |
| `Input`            | Single-line text inputs with shared focus ring + `aria-invalid` styling. |
| `Textarea`         | Multi-line inputs with the same focus/invalid tokens.       |
| `Select`           | Native `<select>` with inline caret + shared tokens.        |
| `Label`            | Form labels paired with `Input` / `Textarea` / `Select`.    |
| `FieldError`       | Inline error message bound to a field via `aria-describedby`. |
| `StatusBadge`      | Idea-status pill (icon + text, never colour-only).          |
| `Card`             | Surface container; `hoverable` variant for clickable rows.  |
| `EmptyState`       | Zero-state panels for empty lists / queues.                 |
| `LoadingSkeleton`  | Skeleton + `Spinner` helpers, motion-safe gated.            |

The `cn()` helper (`clsx` + `tailwind-merge`) is the only sanctioned way to
compose Tailwind classes:

```tsx
import { Button, StatusBadge } from '@/components/ui';

<Button variant="primary" onClick={onSubmit}>Submit idea</Button>
<StatusBadge status="UnderReview" />
```

When a raw `<button>` / `<input>` is genuinely required (invisible overlays,
icon-only triggers, `sr-only` file inputs), annotate it with a
`// ui-polish-exception: <reason>` comment so the next audit knows it is
intentional.

---

## Configuration reference

Key configuration keys (defaults live in `appsettings.Development.json`):

| Key                          | Purpose                                                       |
|------------------------------|---------------------------------------------------------------|
| `ConnectionStrings:Postgres` | EF Core connection string.                                    |
| `Jwt:Issuer` / `Audience` / `SigningKey` / `AccessTokenLifetimeMinutes` | JWT bearer issuance + validation. |
| `Attachments:RootPath`       | Filesystem root for uploaded attachments (mounted as a volume in Docker). |
| `Cors:AllowedOrigins`        | Allow-listed SPA origins.                                     |
| `Seed:AdminEmail` / `AdminPassword` | One-time seeded admin account.                         |
| `BlindReview:AliasSalt`      | HMAC key for per-idea submitter / reviewer aliases. **Rotate to invalidate all historical aliases by design.** |

---

## Governance

All work follows the project constitution at
[.specify/memory/constitution.md](InnovatEPAM-Portal/.specify/memory/constitution.md). Pull
requests must include a one-line compliance statement against the four
principles (Clean Code, REST, UI/UX, Minimal Deps) using this template:

> **Constitution compliance:** Clean Code ✓ • REST ✓ • UI/UX ✓ • Minimal Deps ✓ —
> _add a sentence per principle if any caveat applies._

---

## Status

All rubric phases are complete, plus an additional UI Polish phase:

| Phase                          | Source spec                                                                                         | Status      |
|--------------------------------|------------------------------------------------------------------------------------------------------|-------------|
| Setup & Spec Kit               | `.specify/`, `docker-compose.yml`, CI                                                                | ✅ Complete |
| Phase 1 — Core Portal          | [specs/001-phase1-mvp/tasks.md](InnovatEPAM-Portal/specs/001-phase1-mvp/tasks.md)                    | ✅ Complete |
| Phase 2 — Smart Submission Forms | [specs/001-phase1-mvp/tasks.md](InnovatEPAM-Portal/specs/001-phase1-mvp/tasks.md)                   | ✅ Complete |
| Phase 3 — Multi-Media Support  | [specs/001-phase1-mvp/tasks.md](InnovatEPAM-Portal/specs/001-phase1-mvp/tasks.md)                    | ✅ Complete |
| Phase 4 — Draft Management     | [specs/001-phase1-mvp/tasks.md](InnovatEPAM-Portal/specs/001-phase1-mvp/tasks.md)                    | ✅ Complete |
| Phase 5 — Multi-Stage Review   | [specs/001-phase1-mvp/tasks.md](InnovatEPAM-Portal/specs/001-phase1-mvp/tasks.md)                    | ✅ Complete |
| Phase 6 — Blind Review         | [specs/009-phase6-and-7/tasks.md](InnovatEPAM-Portal/specs/009-phase6-and-7/tasks.md)                | ✅ Complete |
| Phase 7 — Scoring System       | [specs/009-phase6-and-7/tasks.md](InnovatEPAM-Portal/specs/009-phase6-and-7/tasks.md)                | ✅ Complete |
| Phase 8 — UI Polish (added)    | [specs/008-ui-polish/tasks.md](InnovatEPAM-Portal/specs/008-ui-polish/tasks.md)                      | ✅ Complete |
| Documentation                  | this README + `PROJECT_SUMMARY.md` + `docs/manual-testing.md`                                        | ✅ Complete |

## Time Breakdown

| Phase                          | Actual   |
|--------------------------------|----------|
| Setup & Spec Kit               | [time]   |
| Phase 1: Core Portal           | [time]   |
| Phase 2: Smart Submission Forms| [time]   |
| Phase 3: Multi-Media Support   | [time]   |
| Phase 4: Draft Management      | [time]   |
| Phase 5: Multi-Stage Review    | [time]   |
| Phase 6: Blind Review          | [time]   |
| Phase 7: Scoring System        | [time]   |
| Phase 8: UI Polish (additional)| [time]   |
| Documentation                  | [time]   |

For the cohort submission overview see [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md).

---

**Submitted by:** Ozan Ergüleç
**Cohort:** EPAM AI Bootcamp A201
