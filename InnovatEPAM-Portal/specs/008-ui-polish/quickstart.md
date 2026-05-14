# 008-ui-polish — Quickstart

Quick verification steps reviewers run after a UI-polish change. Behavioural specs
live in `spec.md`; phased work plan lives in `tasks.md`. This file only documents
manual smoke checks that don't fit cleanly in an automated test.

## Motion-safe / reduced-motion (FR-011, T028)

All hover/focus colour transitions in the polish layer are wrapped in the
Tailwind `motion-safe:` variant, so users who set `prefers-reduced-motion: reduce`
get instant state changes with no animation.

Manual verification:

1. Run `npm run dev` in `frontend/` and open the app in Chromium-based DevTools.
2. Open **Rendering** (⋮ → More tools → Rendering).
3. Set **Emulate CSS media feature `prefers-reduced-motion`** to `reduce`.
4. Hover the brand link, the "Submit new idea" link, every primary `Button`, every
   `Card[hoverable]` row in `/ideas`, and the notification bell in `AppShell`.
   Confirm:
   - Colour/background state changes still apply (the styles are not gated on
     motion-safe).
   - No animated easing/duration is perceptible (the `transition-*` utilities
     are gated on `motion-safe:` and so are dropped).
5. Switch the emulation back to `no-preference` and confirm the easing returns
   (≈120 ms, `--ease-standard`).

If a new interactive element is added, it must follow the same pattern:

```tsx
className={cn(
  'motion-safe:transition-colors motion-safe:duration-fast motion-safe:ease-standard',
  // … other utilities
)}
```

## Axe smoke (FR-009, T025)

The existing Playwright suite at `frontend/tests/e2e/a11y.spec.ts` (originally
T111) already covers `/login`, `/ideas`, `/ideas/new`, and `/ideas/:id` with
`@axe-core/playwright`, failing on any **serious** or **critical** violation.
No new spec file is required for T025; the polish layer simply needs to keep
that suite green.

Run with the dev stack up:

```powershell
cd frontend
npm run test:e2e -- a11y.spec.ts
```

## Phase 6 reviewer checklist (T049, T050, T051, T055, T056)

A single-page script reviewers walk through before sign-off. Each line is a pass/fail
check; failures must be filed as follow-up tasks (do **not** paper over them).

### Keyboard-only walkthrough (T049, SC-002 + FR-004)

Drive the app with `Tab` / `Shift+Tab` / `Enter` only — no mouse. Confirm the
shared focus ring (`focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2`)
appears on every interactive element at every step.

1. `/login` → `Email` → `Password` → `Sign in` → `Create an account` link.
2. `/register` → `Display name` → `Email` → `Password` → `Create account` → `Sign in` link.
3. `/ideas` → `Submit new idea` link → each filter `<select>` → each idea row link → `Previous` / `Next`.
4. `/ideas/new` → `Title` → `Category` → `Description` → `Choose file` / `Replace file` /
   chip `×` (when present) → `Submit idea` → `Cancel` link.
5. `/ideas/:id` → `Move to Under Review` / `Accept` / `Reject` (admin) → dialog
   `Comment` → `Cancel` / `Record decision`.
6. AppShell → brand link → desktop nav → notifications bell → notifications panel
   `Close` → each notification row.

### Visual status parity (T050, SC-003 + colour-blind safety)

1. Open `/ideas` and confirm each row renders one `StatusBadge` (icon + text label).
2. Open an idea detail page; verify the header `StatusBadge` matches the list-row
   badge for the same status (identical icon, label, and colour token).
3. Repeat the comparison on the admin landing list — admins triage on the shared
   `/ideas?status=Submitted` view, so the badges are necessarily identical.
4. Enable a colour-vision-deficiency simulation in Chromium DevTools (Rendering
   panel → Emulate vision deficiencies → Achromatopsia). Each badge must remain
   distinguishable via its icon glyph (`Submitted` inbox, `UnderReview` clock,
   `Accepted` check, `Rejected` cross). If a badge becomes ambiguous, fix the
   `StatusBadge` icon — never restore colour-only signalling (FR-009).

### Lighthouse Accessibility ≥ 95 (T051, SC-004)

Run in Chrome DevTools → Lighthouse → Accessibility category only.

1. `/ideas` — desktop, then mobile (Moto G Power preset). Both must score **≥ 95**.
2. `/ideas/new` — desktop, then mobile. Both must score **≥ 95**.

If any run falls below 95, file a follow-up task in `tasks.md` describing the
failed audit; the feature is **not** done until every score is ≥ 95.

### Reduced-motion (T055, FR-011)

Already covered above — repeat the [Motion-safe / reduced-motion](#motion-safe--reduced-motion-fr-011-t028)
walkthrough and additionally confirm:

1. Toggle the OS-level reduced-motion preference (Windows: Settings → Accessibility →
   Visual effects → Animation effects → Off; macOS: System Settings → Accessibility →
   Display → Reduce motion).
2. Hover the brand link, primary `Button` instances, and `Card[hoverable]` idea
   rows. The colour change still applies; the easing/duration is dropped.
3. Skeleton placeholders no longer pulse (the `animate-pulse` utility is gated on
   `motion-safe:` in the `Skeleton` primitive).

### 375 px / mobile walkthrough (T056, FR-010)

1. Resize the browser to a viewport width of exactly 375 px (Chromium DevTools →
   Device toolbar → "iPhone SE" or a custom 375 × 667 device).
2. Walk each in-scope page (`/login`, `/register`, `/ideas`, `/ideas/new`,
   `/ideas/:id`, notifications panel). For each:
   - The page must show **no horizontal scrollbar** on its primary content.
   - Long idea titles wrap; they do not push the `StatusBadge` off-screen.
   - Tap targets on primary actions (`Submit idea`, `Sign in`, `Create account`,
     `Accept`/`Reject`, `Submit new idea`, notification bell) must measure
     **≥ 40 px on the shortest side**. Use DevTools' element ruler to verify.
3. Open the AppShell mobile menu (hamburger) and confirm the nav collapses
   correctly with no overlap.

If any check fails, file a follow-up task — do not relax the spec.

