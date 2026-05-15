# InnovatEPAM Portal — Project Summary

## Overview

InnovatEPAM Portal is an internal innovation idea catalog for EPAM employees.
It lets engineers submit new ideas, browse and filter existing ones, follow an
idea through a moderation workflow (Submitted → Under Review →
Accepted / Rejected), attach supporting files, and receive in-app
notifications when their submissions move forward. Administrators triage the
queue, leave decision comments, and audit the full history. The application
ships as a containerized .NET 8 Web API + a React 18 single-page app, runnable
end-to-end from a single `docker compose up --build` command.

## Phases Completed

- [x] **Phase 1** — MVP foundations: auth (register / login / JWT), idea
  submission, listing with filters & pagination, idea detail, file
  attachments, admin decision workflow, audit history, notifications,
  seeded admin account.
- [x] **Phase 2** — Foundational UI: HSL semantic Tailwind tokens, `cn()`
  helper, ten shared UI primitives barrel (`@/components/ui`), AppShell with
  focus ring, motion-safe transitions, and mobile nav.
- [x] **Phase 3** — User story 1: auth pages and idea submission migrated to
  the shared primitives with shared focus / invalid styling.
- [x] **Phase 4** — User story 2: idea detail, admin decide controls, decision
  dialog, and notifications panel refactored onto `Card` / `Button` /
  `StatusBadge`.
- [x] **Phase 5** — User story 3: list / detail / admin / notifications status
  cohesion via `StatusBadge`, `EmptyState`, `LoadingSkeleton`, and
  `Card[hoverable]`.
- [x] **Phase 6** — Verification: raw class-soup audit, all gates green
  (lint 0, tests 70/70, build green), CSS 4.60 KB gz (well inside the
  13.84 KB cap), zero new runtime deps, accessibility + reviewer checklist
  added to `quickstart.md`.
- [x] **Phase 7** — UI polish & docs: shared primitives section added to the
  root README; reviewer checklist consolidated; project documentation
  finalised.
- [x] **Phase 8** — Blind review (spec `009-phase6-and-7`): admin reviewers
  see deterministic per-idea aliases (`Submitter #ABCD`) instead of real
  names while an idea is in `Submitted` / `UnderReview`; identities are
  re-revealed on `Accepted` / `Rejected`, and submitters always see their
  own idea unredacted. Aliases derived via HMAC-SHA256 keyed by the
  `BlindReview:AliasSalt` config value (rotating the salt invalidates
  historical aliases by design).
- [x] **Phase 9** — 1-5 scoring system (spec `009-phase6-and-7`): admins
  score open ideas across four dimensions (impact, feasibility, innovation,
  alignment) with an optional comment; one score per (idea, reviewer),
  upserted via `POST /api/ideas/{id}/scores`. Aggregates (per-dimension and
  overall averages, reviewer count) are surfaced on the list and detail
  pages and sortable via `sort=score:desc|asc`. Self-scoring is forbidden
  (409 `SelfScoringForbidden`); scoring is closed once an idea is decided
  (409 `ScoringClosed`).

## Tech Stack

| Layer            | Technology                                                   |
|------------------|--------------------------------------------------------------|
| Backend          | **.NET 8** Web API (ASP.NET Core, EF Core, ASP.NET Identity, JWT, FluentValidation, Serilog, Swashbuckle) |
| Frontend         | **React 18 + Tailwind CSS** (TypeScript, Vite, React Query, React Hook Form, Zod, React Router) |
| Database         | **PostgreSQL 15**                                            |
| Containerization | **Docker** + Docker Compose (multi-stage `Dockerfile` for the API; named volumes for Postgres data + attachments) |
| Tooling          | Spec Kit, Vitest, Playwright, xUnit, ESLint, Prettier        |

## Key Architecture Decisions

- **SpecKit-driven AI development.** Every feature is born as a Spec Kit
  folder under `specs/NNN-<slug>/` containing `spec.md`, `plan.md`, and
  `tasks.md`. Each task carries explicit success criteria, functional / non-
  functional requirements, and verification gates. Tasks were implemented in
  close collaboration with GitHub Copilot in agent mode — the spec acts as the
  authoritative input to the AI, and the resulting code is reviewed against
  the spec's acceptance criteria before any phase closes. This made the AI a
  reproducible engineering partner rather than an ad-hoc autocomplete.
- **Containerized local-development workflow.** The full backend stack
  (Postgres + .NET API) is brought up by a single `docker compose up --build`.
  The API container builds via a multi-stage `Dockerfile` (csproj-only restore
  layer, Release publish, `aspnet:8.0` runtime on port 8080), waits for
  Postgres' healthcheck, applies pending EF Core migrations on boot, then
  seeds the admin account. The Vite dev server still runs on the host
  (`npm run dev`) for fast HMR. Host port 5222 → container 8080 keeps the
  frontend's default `VITE_API_BASE_URL` working with zero config.
- **Constitution-enforced minimal dependencies.** A four-pillar constitution
  (Clean Code, REST, UI/UX, Minimal Deps) is referenced in every PR. UI Phases
  2–7 added **zero** new runtime dependencies; only `clsx` and `tailwind-merge`
  (both already present) underpin the new primitives.
- **Accessibility first.** Status is communicated by icon **and** text (never
  colour-only), every interactive element shares the same `focus-visible`
  ring, transitions are gated on `motion-safe:`, and reviewers walk a
  documented checklist that includes Lighthouse ≥ 95, a 375 px mobile pass,
  and a reduced-motion pass before sign-off.

---

**Submitted by:** Ozan Ergüleç
**Date:** May 15, 2026
**Cohort:** EPAM AI Bootcamp A201
