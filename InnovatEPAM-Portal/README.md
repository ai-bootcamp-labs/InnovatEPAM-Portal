# InnovatEPAM Portal

[![CI](https://github.com/your-org/InnovatEPAM-Portal/actions/workflows/ci.yml/badge.svg)](https://github.com/your-org/InnovatEPAM-Portal/actions/workflows/ci.yml)

Phase 1 MVP — internal innovation idea catalog for EPAM employees.

## Repository layout

```text
backend/    .NET 8 Web API (4 projects + 2 test projects)
frontend/   React 18 + TypeScript + Vite SPA
docs/       Reference documentation (manual testing, etc.)
specs/      Spec Kit feature folders (specs/001-phase1-mvp/)
.specify/   Spec Kit configuration
```

## Quick start

See [specs/001-phase1-mvp/quickstart.md](specs/001-phase1-mvp/quickstart.md) for
local-dev setup instructions and the manual testing checklists (M1–M3, X1–X4).

The seeded admin account (configured via `Seed:AdminEmail` /
`Seed:AdminPassword` in `appsettings.Development.json`) is created on first
startup by `AdminUserSeeder` (T107). The default credentials are
`admin@innovatepam.test` / `Adm1n!Pass`.

## Health

| Endpoint                  | Purpose                                  | Auth |
|---------------------------|------------------------------------------|------|
| `GET /api/v1/health`       | Liveness / probe smoke (always returns ok) | none |
| `GET /api/v1/health/live`  | Liveness — process is up                 | none |
| `GET /api/v1/health/ready` | Readiness — Postgres reachable           | none |

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
