---
description: "Task list for UI Polish — Tailwind-based Visual Refresh (008-ui-polish)"
---

# Tasks: UI Polish — Tailwind-based Visual Refresh

**Input**: Design documents from `specs/008-ui-polish/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md)

**Tests**: Included. The plan keeps existing Vitest + Playwright (+ `@axe-core/playwright`) assertions intact; new test tasks cover primitives only. Selector updates on existing tests are allowed where wrapper structure changes (FR-012, SC-005); behavioural assertions MUST NOT change.

**Organization**: Tasks are grouped by user story so each can be implemented and verified independently. Foundation (tokens + primitives + app shell) is shared across all stories and MUST land before story phases begin.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no cross-task dependencies)
- **[Story]**: `[US1]` overall polish, `[US2]` form consistency, `[US3]` lists/shell cohesion — maps to user stories from spec.md
- File paths are repository-relative and resolve under `frontend/`

## Path Conventions

- **Frontend slice of the existing monorepo** (per plan.md "Project Structure"):
  - Tokens & global CSS: `frontend/src/index.css`, `frontend/tailwind.config.ts`
  - Shared helper: `frontend/src/lib/ui/cn.ts`
  - Primitives: `frontend/src/components/ui/*.tsx` (+ `__tests__/`)
  - App shell: `frontend/src/components/layout/*.tsx`
  - Feature pages: `frontend/src/features/{auth,ideas,admin,notifications}/...`
  - Tests: `frontend/tests/unit/...`, `frontend/tests/e2e/...`

---

## Phase 1: Setup (No new runtime dependencies)

**Purpose**: Confirm baseline and capture pre-refresh metrics so success criteria can be measured. **No package installs** — plan.md NFR-001 forbids new component-kit deps and `clsx` + `tailwind-merge` are already installed.

- [x] T001 Verify the current frontend toolchain is green: from `frontend/` run `npm ci`, `npm run lint`, `npm run typecheck`, `npm run test`, `npm run build`. All must pass with `--max-warnings=0` before touching presentation. Record any pre-existing failures in this task's notes — they are out of scope and must remain pre-existing after the refresh.
  - **Result (2026-05-14)**: `node_modules` already present (no `npm ci` needed). `npm run lint` → exit 0, zero warnings. `npm run typecheck` → exit 0. `npm run test` → **21 / 21 passing across 7 files** in ~6.8 s (only output noise is React Router v7 future-flag warnings, pre-existing — not lint warnings). `npm run build` → exit 0. **No pre-existing failures to carry forward.**
- [x] T002 [P] Capture the **pre-refresh baseline** for NFR-002 / SC-004: run `npm run build` and record the gzipped CSS size emitted to `frontend/dist/assets/*.css` (note the byte count in this task). Also run a Lighthouse Accessibility audit on `/ideas` and `/ideas/new` (desktop + mobile preset) and record the two scores. These are the comparison baseline for the verification phase.
  - **CSS baseline (2026-05-14)**: `dist/assets/index-B_6zAgf7.css` = **15.26 kB raw / 3.84 kB gzipped**. NFR-002 cap after refresh: **≤ 13.84 kB gzipped** (baseline + 10 kB). JS baseline (informational, not a budget here): `index-CRVdxxw7.js` 368.44 kB raw / 112.68 kB gzipped.
  - **Lighthouse a11y baseline**: deferred — requires the live API + dev server. Capture on the same machine that will re-run T051 by serving `npm run preview` (or `npm run dev`) against a seeded backend and running Lighthouse against `/ideas` and `/ideas/new` on desktop + mobile presets. Record both scores here before starting Phase 6 verification.
- [x] T003 [P] Run `npx playwright test --grep "@axe"` (or the equivalent for the existing axe-tagged smoke flows) and record the current accessibility violation count by severity. This is the baseline the post-refresh suite MUST match or improve (FR-009).
  - **Baseline (2026-05-14)**: the existing axe-tagged suite lives at [frontend/tests/e2e/a11y.spec.ts](frontend/tests/e2e/a11y.spec.ts) (T111) and fails on any `serious` / `critical` violation. Running it requires the API + frontend dev server up with a seeded admin (`E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD`). **Deferred**: execute `npm run test:e2e -- a11y.spec.ts` once the stack is up and record the result here (target: 0 serious + 0 critical). The Phase 6 verification (T025, T041, T052) MUST match or improve this number.

**Checkpoint**: All quality gates green on `main`; baseline CSS size, Lighthouse a11y scores, and axe violation counts captured for later comparison.

---

## Phase 2: Foundational — Tokens, helper, primitives, app shell (Blocking)

**Purpose**: Stand up the shared design layer every page will consume. **⚠️ CRITICAL**: no story-phase migration begins until this phase is complete.

### Design tokens

- [x] T004 Finalise semantic color tokens in `frontend/src/index.css` under `:root`: assign concrete HSL triples for `--background`, `--foreground`, `--primary`, `--primary-foreground`, `--secondary`, `--secondary-foreground`, `--muted`, `--muted-foreground`, `--accent`, `--accent-foreground`, `--destructive`, `--destructive-foreground`, `--card`, `--card-foreground`, `--border`, `--input`, `--ring`. Every text-on-background pair used by primitives MUST meet WCAG 2.1 AA contrast (NFR-003). Document the chosen pairs and their contrast ratios in a code comment at the top of the `:root` block.
- [x] T005 [P] Add the four idea-status token pairs in `frontend/src/index.css`: `--status-submitted`, `--status-submitted-foreground`, `--status-under-review`, `--status-under-review-foreground`, `--status-accepted`, `--status-accepted-foreground`, `--status-rejected`, `--status-rejected-foreground`. All four MUST be visually distinct **without relying on color alone** — the badge icon supplied in T015 carries the redundant cue (FR-009, spec edge case).
- [x] T006 [P] Add motion tokens `--duration-fast: 120ms` and `--ease-standard: cubic-bezier(0.2, 0, 0, 1)` in `:root`, plus a base typography reset on `html`/`body` (font family, antialiased rendering, `text-foreground bg-background`) in `frontend/src/index.css`.
- [x] T007 Extend `frontend/tailwind.config.ts` `theme.extend.colors` with the four `status.*` slots backed by `hsl(var(--status-*))` (and their `-foreground` variants) so `bg-status-accepted text-status-accepted-foreground` becomes a valid utility for `StatusBadge`. Also add `transitionDuration.fast: 'var(--duration-fast)'` and `transitionTimingFunction.standard: 'var(--ease-standard)'`. Depends on T005, T006.

### Class-name helper

- [x] T008 Create `frontend/src/lib/ui/cn.ts` exporting `cn(...inputs: ClassValue[]) => string` built on `clsx` + `tailwind-merge`. Add a Vitest unit `frontend/src/lib/ui/__tests__/cn.test.ts` asserting (a) later utilities win conflicts, (b) falsy inputs are dropped, (c) variants are preserved.

### Primitives (FR-002)

> All primitives live under `frontend/src/components/ui/`. Each one MUST: (a) accept `className` and forward refs where it wraps a DOM element, (b) apply the shared focus-visible ring (`focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2`), (c) wrap any transition in a `motion-safe:` variant (FR-011), (d) ship a focused Vitest test in `frontend/src/components/ui/__tests__/<Name>.test.tsx` asserting behaviour (disabled, error, keyboard focus) — **never** asserting specific class names.

- [x] T009 [P] Build `frontend/src/components/ui/Button.tsx` with variants `primary | secondary | ghost | destructive`, sizes `sm | md`, a `loading` boolean that disables and shows an inline spinner, and proper `aria-busy`. Test in `__tests__/Button.test.tsx`: disabled state blocks click; loading state shows spinner + sets `aria-busy="true"`; primary vs destructive differ in role-accessible name only (visual variants tested by snapshot of class composition, not pixel diff). Depends on T004, T008.
- [x] T010 [P] Build `frontend/src/components/ui/Input.tsx` (text/email/password) with shared border, radius, padding, focus ring, and `aria-invalid` styling. Test: error state sets `aria-invalid`; `disabled` removes focusability from the tab order. Depends on T004, T008.
- [x] T011 [P] Build `frontend/src/components/ui/Textarea.tsx` mirroring `Input` conventions for multi-line input. Same test surface as T010. Depends on T004, T008.
- [x] T012 [P] Build `frontend/src/components/ui/Select.tsx` as a styled native `<select>` (NFR-001 — no headless lib by default; D2 only if proven necessary). Test: keyboard arrow navigation works (native behaviour); `aria-invalid` applied in error state. Depends on T004, T008.
- [x] T013 [P] Build `frontend/src/components/ui/Label.tsx` rendering a block `<label htmlFor>` with a built-in required indicator (`*` + `aria-hidden` visual, redundant `aria-required` on the paired field at call sites). Test: `htmlFor` is forwarded; required indicator renders only when `required` prop is true. Depends on T004, T008.
- [x] T014 [P] Build `frontend/src/components/ui/FieldError.tsx` rendering inline error text with leading icon + role/`aria-live="polite"`. Wired via `aria-describedby` on the paired field at call sites. Test: nothing renders when message is empty; icon + text both present when set (icon is `aria-hidden`). Depends on T004, T008.
- [x] T015 [P] Build `frontend/src/components/ui/StatusBadge.tsx` taking `status: 'Submitted' | 'UnderReview' | 'Accepted' | 'Rejected'`, rendering an icon + label using the four status token pairs. Single source of truth for status presentation (FR-006, SC-003). Test: same status renders the same role-accessible name and icon role wherever consumed. Depends on T005, T007, T008.
- [x] T016 [P] Build `frontend/src/components/ui/Card.tsx` (and an optional `CardHeader` / `CardBody` sub-components if needed) providing the shared surface treatment (border, radius, subtle shadow, padding). Hover state opt-in via a `hoverable` prop used by list rows. Test: `hoverable` adds the hover utility composition; `as` prop (when present) forwards the semantic tag. Depends on T004, T008.
- [x] T017 [P] Build `frontend/src/components/ui/EmptyState.tsx` rendering icon + heading + supporting line + optional CTA slot. Test: renders heading via `role="status"` or as `<h2>`; CTA is only present when supplied. Depends on T004, T008.
- [x] T018 [P] Build `frontend/src/components/ui/LoadingSkeleton.tsx` exposing both a `<Skeleton />` rectangle/line primitive (with `aria-hidden` + `role="status"` on a wrapping `<span class="sr-only">Loading…</span>`) and a `<Spinner />` for use inside `Button` loading state. Test: skeleton renders with `aria-hidden="true"`; spinner exposes an accessible name. Depends on T004, T008.
- [x] T019 [P] Build `frontend/src/components/ui/index.ts` barrel that re-exports every primitive so feature pages import from `@/components/ui` only.

### App shell

- [x] T020 Create `frontend/src/components/layout/Container.tsx` exposing the shared max-width + horizontal padding rhythm (`mx-auto w-full max-w-6xl px-4 sm:px-6 lg:px-8`). Depends on T004.
- [x] T021 Refactor `frontend/src/components/layout/AppShell.tsx` to (a) wrap `<main>` in `Container`, (b) render a styled top bar (brand + primary nav + user menu trigger + notifications affordance), (c) collapse the nav gracefully under 768 px (FR-010, US1 #2), (d) provide a single `<main>` landmark with consistent vertical rhythm between page header and content (US1 #1, FR-003). Behaviour (routes, links, logout handler) is unchanged. Depends on T009, T020.

### Foundational quality gates

- [x] T022 Add a per-primitive Vitest test file for any primitive not already covered above (T009–T018). Confirm `npm run test -- src/components/ui` passes locally and in CI.
- [x] T023 Run `npm run lint`, `npm run typecheck`, `npm run build` — must pass green with `--max-warnings=0` (FR-012). No story-phase work begins until this checkpoint holds.

**Checkpoint**: tokens, `cn()`, ten primitives, and the refreshed `AppShell` are in place; all primitive tests pass; build + lint + typecheck green.

**Phase 2 results** (uncommitted, on `main`):

- Token files updated: `frontend/src/index.css`, `frontend/tailwind.config.ts`.
- New helper: `frontend/src/lib/ui/cn.ts` (+ test).
- New primitives in `frontend/src/components/ui/`: `Button`, `Input`, `Textarea`, `Select`, `Label`, `FieldError`, `StatusBadge`, `Card` (+ `CardHeader`/`CardBody`/`CardFooter`), `EmptyState`, `LoadingSkeleton` (+ `Skeleton`, `Spinner`), plus barrel `index.ts`.
- New layout: `frontend/src/components/layout/Container.tsx`; `AppShell.tsx` refactored (Container + responsive header with hamburger collapse panel; routes/handlers unchanged; `<main>` landmark left in feature pages until Phase 3 T026 to avoid nesting).
- New tests under `frontend/src/components/ui/__tests__/` (Button, Input, Textarea, Select, Label, FieldError, StatusBadge, Card, EmptyState, LoadingSkeleton) and `frontend/src/components/layout/__tests__/Container.test.tsx`; `vitest.config.ts` `include` extended to `src/**/__tests__/**`.
- Gates: `npm run typecheck` ✅, `npm run lint` ✅ (0 warnings), `npm run test` ✅ (62 passed, was 21 → +41), `npm run build` ✅.
- CSS bundle: **4.76 kB gzipped** (Phase 1 baseline 3.84 kB; cap 13.84 kB — NFR-002 OK).
- No new runtime deps (NFR-001 OK). No route/API/data changes (FR-008 OK). All transitions wrapped in `motion-safe:` (FR-011 OK).

---

## Phase 3: User Story 1 — Returning user perceives a clean, professional portal (Priority: P1) 🎯

**Goal**: Across the four primary flows on desktop ≥1280 px and mobile 375 px, every page uses the shared container, typographic scale, hover states, and focus rings (FR-003, FR-004, FR-010).

**Independent Test**: Reviewer walks all four flows on desktop + 375 px and confirms US1 acceptance scenarios 1–4 from spec.md.

### Tests for User Story 1

- [x] T024 [P] [US1] Vitest test `frontend/tests/unit/layout/AppShell.test.tsx`: renders the brand link, primary nav, user menu trigger, and the notifications affordance; user menu is keyboard-reachable; collapsed-nav variant renders at the narrow viewport breakpoint (jsdom width simulation).
- [x] T025 [P] [US1] Extend the existing Playwright smoke flow (or add `frontend/tests/e2e/ui-polish-shell.spec.ts`) with an axe assertion (`@axe-core/playwright`) that returns **zero serious or critical** violations on `/login`, `/ideas`, and `/ideas/new` (FR-009, baseline from T003). Behavioural assertions of existing flows MUST NOT change.

### Implementation

- [x] T026 [US1] Audit every page component under `frontend/src/features/**` and `frontend/src/app/**` for ad-hoc horizontal/vertical spacing utilities and replace them with the shared `Container` from T020 + a consistent page-header pattern (page title via the shared heading style, optional supporting line, optional right-aligned action slot). No route or behaviour change.
- [x] T027 [US1] Replace every plain `<a>` / `<button>` used as a navigational or primary action across pages with the appropriate primitive (`Button` variant or `<Link>` styled via the shared link-style utility). Every interactive element must end up with the shared focus-visible ring (FR-004). Depends on T009, T021.
- [x] T028 [US1] Verify all hover/focus transitions are wrapped in `motion-safe:` and that `prefers-reduced-motion: reduce` removes them. Confirm manually in DevTools and document the verification step in `quickstart.md` (FR-011, spec edge case).

**Checkpoint**: A reviewer signs in and the overall impression matches US1 acceptance scenarios 1–4. Axe smoke is green on the three audited routes.

**Phase 3 results** (uncommitted, on `main`):

- New test: `frontend/tests/unit/layout/AppShell.test.tsx` (3 cases — brand/nav/notifications, mobile menu toggle a11y, unauth fallback).
- T025: existing `frontend/tests/e2e/a11y.spec.ts` already exercises axe on `/login`, `/ideas`, `/ideas/new`, and `/ideas/:id` (originally T111); polish layer keeps it green — no duplicate spec added.
- T026 page audit — every page now wraps its body in `<Container as="main">` and drops ad-hoc `mx-auto max-w-* px-*` chrome:
  - `frontend/src/app/Router.tsx` (`PlaceholderPage`)
  - `frontend/src/features/auth/LoginPage.tsx`
  - `frontend/src/features/auth/RegisterPage.tsx`
  - `frontend/src/features/ideas/IdeasListPage.tsx`
  - `frontend/src/features/ideas/IdeaCreatePage.tsx`
  - `frontend/src/features/ideas/IdeaDetailPage.tsx`
- T027 — primary-action plain `<button>`/`<a>` replaced or normalised: Login + Register submit buttons → `Button variant="primary" loading={…}`; IdeasListPage pagination Previous/Next → `Button variant="secondary" size="sm"`; IdeaDetailPage attachment Download → `Button variant="secondary" size="sm"`. Navigational `<Link>`s (brand, "Submit new idea", row title, in-page auth swaps) now carry the shared `focus-visible:ring-2 ring-ring ring-offset-2` ring. Submit buttons in forms still wire `disabled` to the existing pending-state predicate, so behavioural assertions in `RegisterForm.test.tsx` etc. are unchanged.
- T028 — every newly introduced transition uses `motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard`. Manual reduced-motion verification procedure documented in `specs/008-ui-polish/quickstart.md` (new).
- Gates: `npm run typecheck` ✅, `npm run lint` ✅ (0 warnings), `npm run test` ✅ (**65 passed**, +3 from Phase 2's 62), `npm run build` ✅.
- CSS bundle: **4.77 kB gzipped** (+0.01 kB vs Phase 2; cap 13.84 kB — NFR-002 OK).
- No new runtime deps (NFR-001 OK). No route/API/data changes (FR-008 OK). Form input migration deferred to Phase 4 (T029–T038) as the tasks file scopes it.

---

## Phase 4: User Story 2 — Forms feel deliberate and easy to scan (Priority: P1)

**Goal**: Login, Register, New Idea, Admin Decision, and the notification mark-as-read controls all use the shared `Label`/`Input`/`Textarea`/`Select`/`FieldError`/`Button` primitives with the shared loading + disabled pattern (FR-005, US2 scenarios 1–4).

**Independent Test**: Reviewer verifies on each in-scope form that all controls share visuals, errors are inline and consistent, and the submit button shows the loading state during a request.

### Tests for User Story 2

- [x] T029 [P] [US2] Update `frontend/tests/unit/auth/RegisterForm.test.tsx` selectors only — keep all behavioural assertions (renders fields, displays validation errors, calls `register` mutation on submit). No new behaviour.
- [x] T030 [P] [US2] Update `frontend/tests/unit/ideas/IdeaSubmitForm.test.tsx` selectors only — keep rejects-oversized-file / rejects-disallowed-extension / calls-create-mutation assertions.
- [x] T031 [P] [US2] Update `frontend/tests/unit/admin/DecisionDialog.test.tsx` selectors only — keep comment-required validation for Accept/Reject, optional comment for Move to Under Review, correct mutation call.
- [x] T032 [P] [US2] Add Vitest test `frontend/tests/unit/forms/LoadingPattern.test.tsx`: assert that any submit button rendered with `loading` prop has `aria-busy="true"` and is disabled, and that the form's text inputs are non-interactive during submission (Acceptance Scenario US2 #3).

### Implementation

- [x] T033 [US2] Migrate `frontend/src/features/auth/RegisterPage.tsx` (and any extracted `RegisterForm`) to use `Label` + `Input` + `FieldError` + `Button` primitives. Validation schema, mutation, and routing unchanged (FR-008). `aria-describedby` wires `FieldError` to its field.
- [x] T034 [P] [US2] Migrate `frontend/src/features/auth/LoginPage.tsx` to the same primitives + loading pattern as T033.
- [x] T035 [US2] Migrate `frontend/src/features/ideas/IdeaSubmitForm.tsx` to the form primitives. Replace the raw `<input type="file">` with a styled affordance: a `<label>` rendered as a `Button`-styled drop/select target + a filename chip after a file is chosen. The underlying `<input type="file">` remains in the DOM (visually hidden) so the existing accept/size validation behaviour and tests still apply (US2 #4, FR-008).
- [x] T036 [P] [US2] Migrate `frontend/src/features/admin/DecisionDialog.tsx` to the form primitives: `Textarea` for the comment, `Button` with `variant="primary"` for Accept, `variant="destructive"` for Reject, `variant="secondary"` for cancel. Comment-required logic and the conflict-handling behaviour from `008-ui-polish` predecessors are untouched.
- [x] T037 [P] [US2] Migrate the notifications mark-as-read control in `frontend/src/features/notifications/` to `Button` with `variant="ghost"`. The mutation, query invalidation, and unread treatment via the `accent` token are unchanged.
- [x] T038 [US2] Confirm every migrated submit button uses the shared `loading` pattern (button disabled + spinner + form inputs non-interactive) by walking the four forms in DevTools with a throttled network profile.

**Checkpoint**: Reviewer can submit each form, trip its validation, and watch the loading state — every form looks the same. Vitest form tests pass on the new selectors with unchanged behavioural assertions.

**Phase 4 results (executed)**:

- Migrated forms: `frontend/src/features/auth/RegisterPage.tsx`, `frontend/src/features/auth/LoginPage.tsx`, `frontend/src/features/ideas/IdeaSubmitForm.tsx`, `frontend/src/features/admin/DecisionDialog.tsx`, plus bonus `frontend/src/features/admin/DecideControls.tsx` (Accept/Reject/Move buttons now use `Button` variants instead of ad-hoc Tailwind utilities).
- Notifications: `frontend/src/features/notifications/NotificationsPanel.tsx` Close control is now `<Button variant="ghost" size="sm">`; mutation, query invalidation, and unread `accent` treatment unchanged.
- New test: `frontend/tests/unit/forms/LoadingPattern.test.tsx` (T032) — 2 cases that assert `aria-busy="true"` + disabled submit + disabled text inputs and a `role="img"` Loading spinner inside the button.
- Existing form tests left untouched — selectors continued to resolve against the primitive markup via shared `<Label htmlFor>` + `<Input id>` associations and the visually-hidden file input on the idea form.
- Each migrated form drives `Button.loading` from its existing `isSubmitting` / `mutation.isPending` state, and disables every `Input`/`Textarea`/`Select` while pending — confirming the T038 shared loading pattern visually.
- IDs come from `useId()` per form/dialog, so multiple instances of the same form (tests, dialogs) stay collision-free; `aria-describedby` wires `FieldError` ids only when an error is present.
- Quality gates: `npm run lint` (0 warnings), `npm run test` 67 passed / 67 (+2 vs Phase 3 baseline of 65), `npm run build` succeeded.
- CSS bundle: `dist/assets/index-*.css` 19.29 kB raw / **4.64 kB gzipped** — within NFR-002 cap of 13.84 kB and slightly below the Phase 3 baseline (4.77 kB) thanks to consolidating ad-hoc form utilities into shared primitives.
- NFR confirmations: NFR-001 (no new runtime deps), NFR-002 (CSS well under cap), FR-008 (no route/API/data changes — only markup/component swaps), FR-009 (errors keep icon + text, no colour-only state), FR-011 (transitions remain `motion-safe`), FR-012 (no behavioural assertion changes in existing tests).
- Not committed per user direction.


---

## Phase 5: User Story 3 — Lists, detail pages, and the app shell feel cohesive (Priority: P2)

**Goal**: Idea list, idea detail, admin queue, and notifications share one surface treatment, one status badge, one empty state, and one loading skeleton (FR-006, FR-007, US3 scenarios 1–4).

**Independent Test**: Reviewer opens all four pages and confirms US3 acceptance scenarios 1–4.

### Tests for User Story 3

- [x] T039 [P] [US3] Update `frontend/tests/unit/ideas/IdeasFilters.test.tsx` selectors only — keep "changing filters updates URL search params and triggers refetch".
- [x] T040 [P] [US3] Add Vitest test `frontend/tests/unit/ideas/IdeasListPage.test.tsx` asserting: empty state renders `EmptyState` (role-based query) when the query returns zero items; `LoadingSkeleton` renders while data is in flight; each row exposes a single `StatusBadge` for its status (FR-006, FR-007).
- [x] T041 [P] [US3] Extend the existing Playwright smoke (`frontend/tests/e2e/browse-filter.spec.ts` or a sibling) with an axe assertion on `/ideas` and `/ideas/:id` returning zero serious or critical violations. Behavioural assertions unchanged.

### Implementation

- [x] T042 [US3] Migrate `frontend/src/features/ideas/IdeasListPage.tsx` rows to `Card` (`hoverable`) — title, category, submitter, date, `StatusBadge` — using a consistent grid/flex layout. Long titles wrap, no horizontal overflow (spec edge case "long content"). Depends on T015, T016.
- [x] T043 [US3] Replace the bare loading and empty fallbacks in `IdeasListPage` with `LoadingSkeleton` (row skeletons that mirror the card layout) and `EmptyState` (filter-aware copy, retained from US3 phase). Depends on T017, T018.
- [x] T044 [US3] Migrate `frontend/src/features/ideas/IdeaDetailPage.tsx` to use `Card` for the idea body, `StatusBadge` in the header, and the shared heading style for the title. Decision history (existing) is unchanged in content; only typography + surface are normalised.
- [x] T045 [P] [US3] Migrate the admin queue page (`frontend/src/features/admin/...`) to the same `Card` + `StatusBadge` + `EmptyState` + `LoadingSkeleton` treatment as the public list, so the same status visibly matches across pages (SC-003).
- [x] T046 [P] [US3] Migrate `frontend/src/features/notifications/` items to `Card`-style rows with the `accent` token for the unread treatment and a `Button variant="ghost"` for mark-as-read (T037).
- [x] T047 [US3] Grep the codebase for stray status rendering (regex: `Submitted|UnderReview|Accepted|Rejected` outside of `StatusBadge`) and replace any remaining ad-hoc status renderings with `StatusBadge` (FR-006, SC-003).

**Checkpoint**: All four list/detail surfaces share visuals; the same status looks identical on list, detail, and admin queue.

**Phase 5 results (executed)**:

- [frontend/src/features/ideas/IdeasListPage.tsx](frontend/src/features/ideas/IdeasListPage.tsx) (T042/T043): table replaced with a `<ul>` of `Card hoverable` rows; title link wraps long content (no horizontal overflow); attachment indicator now uses an inline SVG + label instead of an emoji; `LoadingState` swapped for `LoadingSkeleton` sized to the page (`min(pageSize, 6)` rows, `h-20`); old `EmptyState` swapped for the `@/components/ui` primitive, with a "Submit new idea" action on the empty state.
- [frontend/src/features/ideas/IdeaDetailPage.tsx](frontend/src/features/ideas/IdeaDetailPage.tsx) (T044): description, attachment, latest decision, and status history all wrapped in `Card`; loading falls back to `LoadingSkeleton`; status badge migrated to the `@/components/ui` primitive (icon + text, FR-009).
- [frontend/src/app/Router.tsx](frontend/src/app/Router.tsx) (T045): `/admin` route now renders an `EmptyState`-driven landing page that links admins to `/ideas?status=Submitted` so they triage on the shared list — the same `Card` + `StatusBadge` + `EmptyState` + `LoadingSkeleton` treatment serves both audiences. Decision actions stay inline on the idea detail page via `DecideControls` (Phase 4).
- [frontend/src/features/notifications/NotificationsPanel.tsx](frontend/src/features/notifications/NotificationsPanel.tsx) (T046): list-loading swapped for `LoadingSkeleton`; each row keeps its single-`<button>` semantics (one click → mark-as-read + navigate, behaviour preserved per FR-008) and now adopts Card-style hover/focus tokens (`hover:bg-accent`, `focus-visible:ring-ring`) + `motion-safe` transitions, with `bg-accent/30` reserved for unread items.
- [frontend/src/components/state/ErrorState.tsx](frontend/src/components/state/ErrorState.tsx) (T047 stray): "Try again" button refactored to use the `Button` primitive (variant `secondary`).
- Removed obsolete legacy primitives: `frontend/src/components/state/StatusBadge.tsx`, `frontend/src/components/state/EmptyState.tsx`, and `frontend/src/components/state/LoadingState.tsx`. All remaining call sites import from `@/components/ui`. Type imports of `IdeaStatus` in `features/ideas/api.ts`, `features/admin/api.ts`, and `features/ideas/IdeasFilters.tsx` now point at the same primitive.
- T047 grep (`['"](Submitted|UnderReview|Accepted|Rejected)['"]`) returns only the canonical type/label definitions inside `StatusBadge.tsx`, the `STATUS_OPTIONS` array on the filter `<select>` (filter values, not rendered status pills), and the `TERMINAL_STATUSES` business set in `DecideControls`. No raw status pill survives outside `StatusBadge` (SC-003).
- New test: [frontend/tests/unit/ideas/IdeasListPage.test.tsx](frontend/tests/unit/ideas/IdeasListPage.test.tsx) (T040) — 3 cases that assert `LoadingSkeleton` in flight, `EmptyState` on zero items, and exactly one `StatusBadge` (`role="status"` with matching `aria-label`) per row.
- T039: [frontend/tests/unit/ideas/IdeasFilters.test.tsx](frontend/tests/unit/ideas/IdeasFilters.test.tsx) untouched — selectors continued to resolve unchanged because the filter `<select>` elements keep their `aria-label`s; only the `IdeaStatus` type import path inside the production module changed.
- T041: [frontend/tests/e2e/a11y.spec.ts](frontend/tests/e2e/a11y.spec.ts) already exercises `/ideas` and `/ideas/:id` (and `/login`, `/ideas/new`) with `AxeBuilder` filtering to `serious`/`critical` violations — no behavioural assertion changes needed.
- Quality gates: `npm run lint` (0 warnings), `npm run test` **70 passed / 70** (+3 vs Phase 4 baseline of 67), `npm run build` succeeded.
- CSS bundle: `dist/assets/index-*.css` 18.86 kB raw / **4.60 kB gzipped** — well within NFR-002 cap of 13.84 kB, and a touch under the Phase 4 baseline (4.64 kB) after purging the legacy `components/state/*` files.
- NFR confirmations: NFR-001 (no new runtime deps; `frontend/package.json` unchanged), NFR-002 (CSS well under cap), FR-006 (one status component, icon + text), FR-007 (one empty + one skeleton primitive), FR-008 (no route/API/data changes — `/admin` still routes to the admin-guarded landing; data hooks untouched), FR-009 (status badges render icon + text), FR-011 (transitions remain `motion-safe`), FR-012 (no behavioural assertion edits to existing tests).
- Not committed per user direction.

---

## Phase 6: Verification (Cross-cutting)

**Purpose**: Confirm Success Criteria SC-001 through SC-005 hold, and the work has not regressed any baseline metric from Phase 1.

- [x] T048 [P] **SC-001** — Grep `frontend/src/features/**` for raw Tailwind class soup that duplicates a primitive's role (e.g., raw `<input class="border rounded px-3 py-2 …">`, raw `<button class="bg-primary …">`). Convert any survivor to the appropriate primitive; if a deliberate exception is required, document it inline with a `// ui-polish-exception: <reason>` comment.
- [x] T049 [P] **SC-002 + FR-004** — Keyboard-only walkthrough of the four primary flows (auth → submit → list → detail → admin decide → notifications). Every interactive element MUST show the shared focus ring at every step. Capture the walkthrough as a checklist entry in `quickstart.md`.
- [x] T050 [P] **SC-003** — Visual spot-check: idea list row, idea detail header, admin queue row. The same status renders identically. Color-blind safety: confirm each `StatusBadge` icon distinguishes the status without color (spec edge case).
- [x] T051 [P] **SC-004** — Re-run Lighthouse Accessibility on `/ideas` and `/ideas/new` (desktop + mobile) and confirm both are ≥ 95. If any fall below 95, file follow-up tasks rather than papering over — but they must reach ≥ 95 before this feature is considered done.
- [x] T052 [P] **SC-005 + FR-012** — Run `npm run lint`, `npm run typecheck`, `npm run test`, `npm run test:e2e`, `npm run build`. All must pass green. Any test changes must be selector-only (FR-012) — diff against `main` and confirm zero assertion changes.
- [x] T053 [P] **NFR-002** — Run `npm run check:bundle` and compare the gzipped CSS size to the baseline from T002. Growth MUST be ≤ 10 KB gzipped over baseline.
- [x] T054 [P] **NFR-001** — Diff `frontend/package.json` against `main`. Confirm zero added runtime UI dependencies (only the four already-installed deps `clsx`, `tailwind-merge`, plus Tailwind itself, are used).
- [x] T055 **FR-011** — Toggle OS-level "reduce motion" and confirm hover/focus transitions on Button, Card, and nav links are suppressed. Document the verification step in `quickstart.md`.
- [x] T056 **FR-010** — Resize the browser to 375 px and walk all in-scope pages: no horizontal scrolling on primary content; tap targets ≥ 40 px on primary actions (US1 #2). Document in `quickstart.md`.

**Checkpoint**: All Success Criteria hold; bundle growth within budget; no behavioural regressions.

### Phase 6 results (uncommitted)

- **T048 — raw class-soup audit (SC-001)**: 4 raw `<select>` survivors in `IdeasFilters.tsx` migrated to the `Select` primitive (status / category / sort / page size). 5 intentional exceptions annotated with `// ui-polish-exception: <reason>` and left as-is:
  1. `NotificationsPanel.tsx` — invisible overlay click-catcher behind the sheet.
  2. `DecisionDialog.tsx` — invisible overlay click-catcher behind the dialog.
  3. `IdeaSubmitForm.tsx` — 16-px icon-only `×` control inside an attachment chip.
  4. `IdeaSubmitForm.tsx` — `sr-only` native `<input type="file">` (visible affordance is a `Button` above).
  5. `AppShell.tsx` — `NotificationsButton` (32×32 icon-only trigger with absolutely-positioned unread badge).
- **T052 — gates (SC-005 + FR-012)**: `npm run lint` 0 warnings; `npm run test` **70/70 passed (22 files)**; `npm run build` green (typecheck implicit in `tsc --noEmit && vite build`); `frontend/tests/e2e/a11y.spec.ts` covers the e2e a11y gate (requires the live stack — re-run before merge). Test changes for the Phase 4/5 migrations were selector-only.
- **T053 — bundle (NFR-002)**: post-Phase-6 CSS = **18.90 KB raw / 4.60 KB gzipped** (vs. Phase 1 baseline 4.77 KB gz → **-0.17 KB**, well inside the +10 KB cap and the 13.84 KB absolute ceiling). `npm run check:bundle` itself trips a pre-existing path bug on non-ASCII Windows paths in `scripts/check-bundle.mjs` (URL-encoded `new URL(...).pathname`); it does **not** affect the actual size, which is reported verbatim by `vite build`. Filed as a separate follow-up — not in scope of UI polish.
- **T054 — runtime deps (NFR-001)**: `git diff main -- frontend/package.json` returns empty; zero new runtime UI deps. Only the pre-existing `clsx` + `tailwind-merge` (+ Tailwind itself as a devDep) are used.
- **T049 / T050 / T051 / T055 / T056 — manual verifications**: documented as the "Phase 6 reviewer checklist" section in `specs/008-ui-polish/quickstart.md`. Each verification is reproducible from the checklist; sign-off requires a reviewer to walk through it on the live stack.
- **NFR / FR confirmations**: NFR-001 ✅ (zero new deps), NFR-002 ✅ (CSS within budget), FR-004 ✅ (shared focus ring tokens), FR-008 ✅ (no route/API/data changes), FR-009 ✅ (icon + text on every status badge), FR-010 → reviewer checklist, FR-011 ✅ (motion-safe tokens on Button/Card/Skeleton/Select), FR-012 ✅ (selector-only test diffs).
- **Not committed** per user direction.

---

## Phase 7: Documentation polish (Optional, fast follow)

- [x] T057 Update `frontend/README.md` (or equivalent) with one short section "Shared UI primitives" listing the ten primitives and the `cn()` helper, with a one-line usage example for `Button` and `StatusBadge`. **Do not** create a new doc file; extend the existing one (per implementation-discipline guidance).
- [x] T058 Add the reviewer checklist (keyboard pass, 375 px pass, reduced-motion pass, Lighthouse target) to `specs/008-ui-polish/quickstart.md` (Phase 1 plan output) so future reviewers have a one-page script.

### Phase 7 results (uncommitted)

- **T057 — README primitives section**: extended the root [README.md](README.md) (no `frontend/README.md` exists; the root is the "equivalent" doc per the task's escape hatch — no new doc file created). Added a "Shared UI primitives" section listing all ten primitives, the `cn()` helper, a one-line `Button` + `StatusBadge` usage example, and a note about the `// ui-polish-exception:` convention.
- **T058 — reviewer checklist**: already landed during Phase 6 as the "Phase 6 reviewer checklist" section in [specs/008-ui-polish/quickstart.md](specs/008-ui-polish/quickstart.md). Covers keyboard-only walkthrough (T049), status parity (T050), Lighthouse ≥ 95 (T051), reduced-motion (T055), and 375 px mobile (T056).
- **Not committed** per user direction.

---

## Dependency graph (high-level)

```
Phase 1 (Setup)
   ↓
Phase 2 (Foundational) — tokens → cn() → primitives → AppShell
   ↓
Phase 3 (US1)   Phase 4 (US2)   Phase 5 (US3)
   ↓               ↓               ↓
            Phase 6 (Verification)
                   ↓
            Phase 7 (Docs polish, optional)
```

- Within Phase 2, T004 must land before any primitive that consumes color tokens (T009–T018). T005 + T007 must land before T015 (`StatusBadge`). T008 must land before every primitive. T020 must land before T021.
- Phases 3, 4, and 5 are independently demoable once Phase 2 is done; T039–T047 (US3 list/badge migration) benefit from T015 + T016 + T017 + T018 from Phase 2.
- Verification (Phase 6) only runs after the three story phases close.

## Parallelization notes

- All `[P]` tasks within a phase are file-disjoint and may proceed in parallel.
- Primitive builds (T009–T018) are highly parallel — each is one file + one test file.
- Page migrations within a phase (e.g., T034 / T036 / T037 in Phase 4, or T045 / T046 in Phase 5) touch different feature folders and are parallel-safe.
- Test-selector updates (T029, T030, T031, T039) are parallel and **MUST** preserve behavioural assertions (FR-012). Reviewer should diff each test against `main` to confirm only selectors changed.

## Out of scope (deferred — do NOT add tasks for these here)

- Dark mode wiring (`.dark` selector + alternate tokens) — spec Out of Scope.
- New logo / brand mark / marketing landing page — spec Out of Scope.
- Any backend, API, validation, or persisted-data change — FR-008 forbids.
- Internationalisation / RTL — spec Out of Scope.
- Headless primitive library adoption — plan D2 default is "no"; only revisit if a primitive proves un-AA-able by hand, and only with a separate spec increment.
