# InnovatEPAM Portal

[![CI](https://github.com/your-org/InnovatEPAM-Portal/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/InnovatEPAM-Portal/actions/workflows/ci.yml)

An internal innovation idea catalog for EPAM employees. Engineers submit
ideas, follow them through a moderation workflow
(`Submitted → Under Review → Accepted / Rejected`), attach supporting files,
and receive in-app notifications. Administrators triage the queue and record
decisions with full audit history.

The full backend stack runs from a single `docker compose up --build`.

## Repository layout

```text
backend/    .NET 8 Web API (4 projects + 2 test projects)
frontend/   React 18 + TypeScript + Vite SPA
docs/       Reference documentation (manual testing, etc.)
specs/      Spec Kit feature folders (specs/001-phase1-mvp/, …, specs/008-ui-polish/)
.specify/   Spec Kit configuration
```

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

## How to run locally

### 1. Start the backend stack (one command)

From the repository root:

```powershell
docker compose up --build
```

This builds the multi-stage API image, starts Postgres 15, waits for its
healthcheck, applies pending EF Core migrations, and seeds the admin account.

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
# backend
cd backend
dotnet test

# frontend
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

## Health

| Endpoint                  | Purpose                                  | Auth |
|---------------------------|------------------------------------------|------|
| `GET /api/v1/health`       | Liveness / probe smoke (always returns ok) | none |
| `GET /api/v1/health/live`  | Liveness — process is up                 | none |
| `GET /api/v1/health/ready` | Readiness — Postgres reachable           | none |

## Shared UI primitives

The frontend exposes a single barrel at `@/components/ui` (see
[frontend/src/components/ui/index.ts](frontend/src/components/ui/index.ts)).
Feature pages import from the barrel only — never reach into individual files.
The ten primitives plus the `cn()` class-merge helper cover every interactive
surface in the SPA; see [specs/008-ui-polish/](specs/008-ui-polish/) for the
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

## Governance

All work follows the project constitution at
[.specify/memory/constitution.md](.specify/memory/constitution.md). Pull
requests must include a one-line compliance statement against the four
principles (Clean Code, REST, UI/UX, Minimal Deps) using this template:

> **Constitution compliance:** Clean Code ✓ • REST ✓ • UI/UX ✓ • Minimal Deps ✓ —
> _add a sentence per principle if any caveat applies._

## Status

Phase 1 MVP — implementation complete (see
[specs/001-phase1-mvp/tasks.md](specs/001-phase1-mvp/tasks.md)).
Phase 7 UI polish — complete (see [specs/008-ui-polish/tasks.md](specs/008-ui-polish/tasks.md)).
For the cohort submission overview see [PROJECT_SUMMARY.md](PROJECT_SUMMARY.md).
