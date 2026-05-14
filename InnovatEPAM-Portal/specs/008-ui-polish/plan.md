# Implementation Plan: UI Polish — Tailwind-based Visual Refresh

**Branch**: `008-ui-polish` | **Date**: 2026-05-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `specs/008-ui-polish/spec.md`

## Summary

Deliver a presentation-only refresh of the InnovatEPAM Portal frontend on top
of the existing React 18 + Vite + Tailwind CSS 3 stack. The work introduces a
small set of **semantic design tokens** (already partially scaffolded via the
HSL CSS variables in `tailwind.config.ts`) and a **shared UI primitives layer**
under `frontend/src/components/ui/` (Button, Input, Textarea, Select, Label,
FieldError, StatusBadge, Card, EmptyState, LoadingSkeleton). All in-scope
pages — Login, Register, Idea List, Idea Detail, New Idea, Admin Decision,
Notifications, and the AppShell — are migrated to consume these primitives.

The refresh is **strictly visual**: no routes, API contracts, validation
rules, data shapes, or persisted state change. Existing behavioural tests
(Vitest, Playwright) keep their assertions; only DOM-selector updates tied
to renamed class names or restructured wrapper elements are permitted.

## Technical Context

**Language/Version**: TypeScript 5.4 (strict) on Node.js 20 LTS; no backend changes.
**Primary Dependencies** (all already installed — no new runtime deps required):
- React 18.3, Vite 5.4, Tailwind CSS 3.4, PostCSS 8 + Autoprefixer 10
- `clsx` 2.1 + `tailwind-merge` 2.5 — used to build a single `cn()` class-name helper
- React Router 6, TanStack Query 5, react-hook-form 7 + zod 3 (already wired into the in-scope forms)
- Vitest 2 + Testing Library, Playwright 1.48 + `@axe-core/playwright` (already wired for a11y assertions)

**New dependencies**: **none required**. NFR-001 forbids component-kit
replacements; everything is built from Tailwind utilities + hand-rolled
primitives. The single optional addition under evaluation is a *headless*
accessibility primitive library for menu/dialog if and only if a primitive
proves non-trivial to make AA-accessible by hand (see Open Decision D2 below);
default is **no new dependency**.

**Storage**: N/A (frontend-only).
**Testing**: Vitest + Testing Library for primitives and forms; Playwright
end-to-end smoke flows kept green; `@axe-core/playwright` used to backstop
accessibility against regressions; Lighthouse a11y manually verified for SC-004.
**Target Platform**: Modern desktop + mobile browsers (last-two-versions
Chrome/Edge/Firefox/Safari); responsive 375 → 1920 px.
**Project Type**: Frontend slice of the existing web-app monorepo.
**Performance Goals**: No regression vs current bundle. Tailwind purge keeps
CSS lean; CSS bundle growth target ≤ 10 KB gzipped (NFR-002).
**Constraints**: HTTPS-only, AA contrast (NFR-003), `prefers-reduced-motion`
honoured (FR-011), no horizontal scroll ≥ 375 px (FR-010), ESLint + TS
strict + `--max-warnings=0` stay green (FR-012).
**Scale/Scope**: ~10 routes, ~4 forms, ~10 new primitive components, ~1
shared tokens layer in `index.css` + `tailwind.config.ts`.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

Source: [.specify/memory/constitution.md](../../.specify/memory/constitution.md) v1.0.0

| Principle / Section | Gate | Status |
|---|---|---|
| I. Clean Code & Maintainability | ESLint + Prettier + TS strict stay green with `--max-warnings=0`; per-page Tailwind "class soup" is replaced by named primitives; primitives carry JSDoc + prop types. | PASS |
| II. RESTful API Design | No API or contract change in this feature. | N/A |
| III. Simple & Responsive UI/UX | Feature *is* this principle: mobile-first verified at 375/768/1024/1280/1920; loading/empty/error/success states for every list and form via shared primitives; WCAG 2.1 AA contrast; navigation depth unchanged. | PASS |
| IV. Minimal Dependencies | Zero new runtime UI deps planned (NFR-001). `clsx` + `tailwind-merge` already present. Any optional headless primitive must be justified in `research.md` and stay within the bundle budget. | PASS |
| Tech Stack & Constraints | Stack unchanged: React 18 + TS strict + Tailwind 3 + Vite 5. Single frontend workspace untouched. | PASS |
| Dev Workflow & Quality Gates | CI continues to run build / test / lint / typecheck / a11y smoke; bundle check (`check:bundle`) gates CSS+JS growth. | PASS |

**Result**: No violations. Complexity Tracking section left empty.

## Architectural Approach

### 1. Semantic design tokens (one layer, in CSS variables)

The existing `tailwind.config.ts` already declares semantic color slots
(`background`, `foreground`, `primary`, `secondary`, `muted`, `accent`,
`destructive`, `card`, `border`, `input`, `ring`) backed by `hsl(var(--…))`.
This feature **finalises** that layer rather than introducing a new one:

- Define final HSL values for all tokens in `src/index.css` under `:root`,
  chosen to meet AA contrast for the pairs actually used (body on
  background, primary-foreground on primary, destructive-foreground on
  destructive, muted-foreground on background).
- Add token slots for the four idea statuses
  (`--status-submitted`, `--status-under-review`, `--status-accepted`,
  `--status-rejected`) plus their `-foreground` companions. These power
  `StatusBadge` and ensure every status renders identically everywhere
  (FR-006, SC-003).
- Add typography rhythm via Tailwind utility usage only — no new plugin —
  with a small **named heading set** baked into primitives (e.g.
  `<h1>` style for page titles, `<h2>` for section titles) so pages do
  not freestyle font sizes.
- Add motion tokens (`--duration-fast: 120ms`, `--ease-standard`) used by
  primitives; wrap all transitions in a `motion-safe:` variant so
  `prefers-reduced-motion: reduce` automatically suppresses them
  (FR-011).

Dark mode is intentionally **not** wired in this feature, but because
every color is semantic-token-driven, adding a `.dark` variant in a
future feature is a single CSS block, not a refactor (spec Edge Cases /
Out of Scope).

### 2. Shared primitives layer

A new folder `frontend/src/components/ui/` hosts each primitive as a
small, focused React component:

| Primitive          | Responsibility                                                                 |
|--------------------|---------------------------------------------------------------------------------|
| `Button`           | Variants: `primary` / `secondary` / `ghost` / `destructive`; sizes `sm`/`md`; `loading` state that disables and shows a spinner; forwards refs. |
| `Input`            | Text/email/password input with shared border, radius, padding, focus ring; `aria-invalid` styling when in error.                                |
| `Textarea`         | Same conventions as `Input`, multi-line.                                                                                                        |
| `Select`           | Native `<select>` styled to match `Input` (no headless library needed — native `<select>` is the simplest accessible choice for our short option lists). |
| `Label`            | Block label paired with form controls by `htmlFor`; required-indicator built in.                                                                |
| `FieldError`       | Inline error text using the shared error token; rendered via `aria-describedby`. Color **plus** a leading icon so meaning is not color-only.    |
| `StatusBadge`      | One component, one color treatment per idea status; uses status tokens; renders icon + label so contrast and meaning do not depend on color alone. |
| `Card`             | Surface wrapper (border, radius, subtle shadow, padding) used by list rows, detail blocks, form containers.                                     |
| `EmptyState`       | Icon + heading + supporting line + optional CTA slot for empty lists.                                                                           |
| `LoadingSkeleton`  | Rectangle / line skeleton primitive (and a `Spinner` for buttons) consumed by every list/detail page while data is in flight.                   |

Class composition uses **one** helper, `cn()`, defined in `src/lib/ui/cn.ts`
that wraps `clsx` + `tailwind-merge`. All primitives accept `className` to
let the caller override at the edges without losing default styles.

Each primitive ships with a focused Vitest unit test covering its
contract (disabled state, error state, keyboard focusability) — small,
fast, and behaviour-level so they do not lock in pixel decisions.

### 3. App-shell rhythm

`AppShell` (already exists under `frontend/src/components/layout/`) is
refactored — not rewritten — to:

- Wrap content in a shared `Container` (Tailwind `container mx-auto px-4
  sm:px-6 lg:px-8 max-w-6xl`) so every page gets the same horizontal
  rhythm (FR-003).
- Render a styled top bar with brand, primary nav, user menu, and the
  notifications affordance; nav collapses gracefully under 768 px
  (Acceptance Scenario US1 #2).
- Provide a single `<main>` landmark and consistent vertical rhythm
  between page header and content (US1 #1, FR-003).

### 4. Page migrations (no behaviour change)

Each in-scope page swaps its ad-hoc markup for the primitives:

- **auth/Login & auth/Register** → `Card` + `Label`/`Input`/`FieldError`
  + `Button` (`primary`, `loading`). Submit handler, validation schema,
  and routing are untouched (FR-008).
- **ideas/IdeaList** → `Card` rows with shared hover state, `StatusBadge`
  per row, `EmptyState` when empty, `LoadingSkeleton` rows while fetching.
- **ideas/IdeaDetail** → `Card` for the idea body, `StatusBadge` in the
  header, decision history rendered with shared typography.
- **ideas/NewIdea** → form primitives + a styled file input affordance
  built around the existing `<input type="file">` (label-as-button +
  filename chip), preserving the existing accept/size validation
  behaviour (Acceptance Scenario US2 #4).
- **admin/DecisionForm** → form primitives + `Button` variants
  (`primary` to confirm, `destructive` for reject, `secondary` for
  cancel); mandatory-comment behaviour unchanged.
- **notifications/Panel** → `Card` items + consistent unread treatment
  using `accent` token; mark-as-read action uses `Button` (`ghost`).

### 5. Accessibility & motion

- Focus ring: a single utility `focus-visible:ring-2
  focus-visible:ring-ring focus-visible:ring-offset-2` applied via
  primitive defaults so every interactive control passes
  Acceptance Scenario US1 #4.
- Every primitive that conveys state (`FieldError`, `StatusBadge`)
  encodes that state via **icon + text**, not color alone (FR-009,
  spec edge case "color alone").
- All transitions live under `motion-safe:` (FR-011).
- Existing `@axe-core/playwright` checks in the Playwright smoke
  suite keep us honest on a11y regressions.

### 6. Test strategy (preserve behaviour, update selectors only)

- **Vitest unit tests**: add per-primitive tests in
  `frontend/src/components/ui/__tests__/`. Existing feature tests stay,
  but `getByRole` / `getByLabelText` selectors are preferred over
  brittle class-name selectors; any test that relied on a specific
  class or wrapper structure is updated to the new structure with the
  **same assertions** (FR-012, SC-005).
- **Playwright E2E**: existing smoke flows run unchanged behaviourally;
  the axe integration must continue to pass with zero **serious** or
  **critical** violations.
- **Manual**: a one-page reviewer checklist mirroring Success Criteria
  (keyboard walkthrough, 375 px viewport, Lighthouse a11y) lives in
  `quickstart.md` (Phase 1 output).

### 7. Bundle & performance discipline

- No new runtime deps by default (NFR-001).
- `scripts/check-bundle.mjs` (already in `package.json`) gates total
  bundle growth; the plan budgets ≤ 10 KB gzipped CSS over current
  baseline (NFR-002). Tailwind's content-based purge keeps unused
  classes out.
- Vite's `rollup-plugin-visualizer` is consulted once at the end of
  Phase 2 implementation to confirm no surprise growth.

## Project Structure

### Documentation (this feature)

```text
specs/008-ui-polish/
├── plan.md              # This file (/speckit.plan command output)
├── spec.md              # /speckit.specify output
├── research.md          # Phase 0 output — tokens, color choices, headless-lib decision
├── data-model.md        # Phase 1 output — N/A (no entities); will state "no data changes" and inventory primitives instead
├── quickstart.md        # Phase 1 output — reviewer + manual a11y checklist
├── contracts/           # N/A — no API contracts in this feature
└── checklists/
    └── requirements.md  # /speckit.specify output
```

### Source Code (changes only — everything else untouched)

```text
frontend/
├── tailwind.config.ts                    # (edit) finalise semantic tokens, add status tokens, motion duration
├── src/
│   ├── index.css                         # (edit) define :root CSS variables for tokens; base typography reset
│   ├── lib/
│   │   └── ui/
│   │       └── cn.ts                     # (new) clsx + tailwind-merge helper
│   ├── components/
│   │   ├── layout/
│   │   │   ├── AppShell.tsx              # (edit) consume Container + shared nav styles
│   │   │   └── Container.tsx             # (new) shared max-width / padding wrapper
│   │   └── ui/                           # (new folder — all primitives below)
│   │       ├── Button.tsx
│   │       ├── Input.tsx
│   │       ├── Textarea.tsx
│   │       ├── Select.tsx
│   │       ├── Label.tsx
│   │       ├── FieldError.tsx
│   │       ├── StatusBadge.tsx
│   │       ├── Card.tsx
│   │       ├── EmptyState.tsx
│   │       ├── LoadingSkeleton.tsx
│   │       └── __tests__/                # (new) Vitest tests per primitive
│   └── features/                         # (edit) each in-scope page migrated to primitives
│       ├── auth/                         # Login, Register
│       ├── ideas/                        # List, Detail, NewIdea
│       ├── admin/                        # DecisionForm + queue
│       └── notifications/                # Panel + items
└── tests/
    ├── unit/                             # (edit selectors only where wrapper changed)
    └── e2e/                              # (edit selectors only; assertions unchanged)
```

**Structure Decision**: Continue with the existing single-frontend-workspace
layout (no monorepo restructure). Primitives are colocated under
`src/components/ui/` following the same convention already used for
`layout/` and `state/`. A dedicated `src/lib/ui/` is intentionally avoided
to prevent splitting "UI building blocks" across two folders; only the
class-name helper lives in `lib/ui/` because it is a pure utility, not a
component (resolves spec Open Question on primitive location).

## Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Refactor inadvertently changes behaviour (e.g. swallowing a click handler when restructuring a form). | Low | High | Keep Vitest + Playwright behavioural assertions intact; only selectors may change (FR-012, SC-005). Migrate one page at a time. |
| Status badge color choices fail AA contrast or are not distinguishable to color-blind users. | Medium | Medium | Status tokens validated against AA; badges always include an icon + label so meaning is not color-only (FR-009, spec edge case). |
| CSS bundle growth exceeds 10 KB gzipped. | Low | Low | Tailwind purge + `check:bundle` gate in CI (NFR-002). |
| Hover/focus transitions feel busy for reduced-motion users. | Medium | Low | Wrap all transitions in `motion-safe:` (FR-011); test with OS-level reduce-motion enabled. |
| Native `<select>` styling is inconsistent across browsers. | Medium | Low | Use a minimal styled native select first; revisit only if Acceptance Scenario US2 #1 visibly fails — at which point a *headless* primitive (zero-runtime UI lib) is justified. |
| Tests are over-coupled to class names and require widespread rewrites. | Medium | Medium | Prefer `getByRole` / `getByLabelText` selectors in primitives' tests; update legacy tests opportunistically as their page migrates. |

## Phase Output Plan

- **Phase 0 — research.md**: nail down (a) final token HSL values + contrast
  evidence, (b) StatusBadge color + icon mapping, (c) headless-primitive
  go/no-go decision (D2), (d) baseline bundle measurement.
- **Phase 1 — data-model.md** *(content adjusted for a non-data feature)*:
  inventory of primitives, their props, their states, and the pages that
  consume each. Plus an explicit "no entity changes" statement that
  references the existing `001-phase1-mvp/data-model.md` as the
  source of truth.
- **Phase 1 — quickstart.md**: reviewer walkthrough script
  (login → submit → list → detail → admin decide → notifications)
  including the keyboard-only pass and the 375 px responsive pass that
  back Success Criteria SC-001 through SC-004.
- **Phase 2 — tasks.md** (via `/speckit.tasks`): ordered task list
  (tokens → primitives → AppShell → page migrations → test-selector
  updates → bundle/a11y verification), each task small enough to land
  as one PR.

## Open Decisions (deferred to research.md)

- **D1 — final color palette**: exact HSL values for primary, accent,
  destructive, and the four status tokens. Constrained by NFR-003 (AA
  contrast) and SC-003 (statuses identifiable at a glance).
- **D2 — headless primitive library**: default **no**. Revisit only
  if (a) the user menu in `AppShell` or (b) the admin decision
  confirmation needs a true modal/menu primitive that is impractical
  to make AA-accessible from scratch. Any addition must respect
  NFR-001 (no component kit) and the bundle budget.

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

*Not applicable — no constitutional violations.*
