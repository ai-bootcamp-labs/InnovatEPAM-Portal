# Feature Specification: UI Polish — Tailwind-based Visual Refresh

**Feature Branch**: `008-ui-polish`  
**Created**: 2026-05-14  
**Status**: Draft  
**Input**: User description: "Polish the UI of the frontend application using Tailwind CSS. The design should be modern, clean, and professional, but not overly complex or exaggerated. Focus on better spacing, consistent typography, subtle hover effects, and clean form inputs."

## Overview

The InnovatEPAM Portal frontend is functionally complete for Phase 1 (auth,
idea submission, listing, admin review, notifications), but its visual layer
is still utilitarian: ad-hoc spacing, inconsistent typography, default form
inputs, and minimal interactive feedback. This feature delivers a focused
visual refresh built on Tailwind CSS that makes the portal feel modern,
calm, and professional without introducing a heavy design system or
animation framework.

The work is **purely presentational**. It MUST NOT change routes, API
contracts, data shapes, validation rules, or business behaviour. Existing
unit, integration, and E2E tests covering behaviour MUST continue to pass
unchanged (modulo selector updates strictly tied to renamed class names or
restructured wrapper elements).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A returning user perceives a clean, professional portal (Priority: P1)

A staff member who used the portal before the refresh signs in and immediately
notices that the application feels more polished: text hierarchy is clearer,
pages breathe, buttons and links give subtle feedback on hover, and forms
look intentional rather than browser-default. Nothing they previously knew
how to do has moved or been renamed.

**Why this priority**: This is the headline outcome of the feature. If the
overall impression is not "this looks more professional now", the work has
failed regardless of how many individual tokens were introduced.

**Independent Test**: A reviewer walks through the four main flows
(login/register, submit idea, browse idea list + detail, admin decision)
on desktop and on a 375px-wide viewport and confirms (a) every page uses
the shared typographic scale and spacing rhythm, (b) every interactive
element has a visible hover and focus state, (c) no page shows raw
browser-default inputs or unstyled buttons.

**Acceptance Scenarios**:

1. **Given** any page in the application, **When** a user views it on a
   standard desktop viewport (≥1280px), **Then** the main content has
   consistent horizontal padding/maximum width, a visible page title using
   the shared heading style, and consistent vertical spacing between
   sections.
2. **Given** any page in the application, **When** a user views it on a
   narrow viewport (375px), **Then** content remains readable without
   horizontal scrolling, the top navigation collapses gracefully, and
   tap targets for primary actions are at least 40px in their smaller
   dimension.
3. **Given** any interactive control (button, link, nav item, table row
   action), **When** the user hovers it with a pointer device, **Then**
   it shows a subtle visual change (background, border, or text color)
   that is clearly distinguishable from the default state but does not
   shift layout.
4. **Given** any interactive control, **When** the user focuses it via
   keyboard (Tab), **Then** a clearly visible focus ring appears that
   meets WCAG 2.1 AA contrast against the surrounding background.

---

### User Story 2 - Forms feel deliberate and easy to scan (Priority: P1)

The portal's primary forms — Login, Register, New Idea, Admin Decision,
and the notification mark-as-read controls — are restyled so that labels,
inputs, helper text, and validation errors share one consistent visual
pattern. Required fields, disabled fields, and error states are visually
distinct without relying on color alone.

**Why this priority**: Forms are where users spend the most focused time
in the portal. Inconsistent input styling is the single most visible
"unfinished" signal in the current UI.

**Independent Test**: For each in-scope form, a reviewer confirms (a) every
field has a styled label above its input, (b) inputs share one height,
border, radius, and focus style, (c) submitting an invalid form produces
inline error text in a consistent style, (d) the primary submit button
uses the shared primary-button style and shows a loading state while the
request is in flight, (e) disabled fields and buttons are visually
distinct and not focusable in a confusing way.

**Acceptance Scenarios**:

1. **Given** a user opens any in-scope form, **When** the page renders,
   **Then** every text input, textarea, and select uses the same border,
   border-radius, padding, font size, and focus ring style.
2. **Given** a required field is empty on submit, **When** the form is
   submitted, **Then** the field shows an inline error message in the
   shared error style and the field's border indicates the error state
   via both color and an icon or accent (not color alone).
3. **Given** a form is mid-submission, **When** the request is in flight,
   **Then** the primary submit button shows a loading indicator and is
   disabled, and the form's inputs are non-interactive.
4. **Given** a file attachment input on the New Idea form, **When** the
   user views or interacts with it, **Then** it presents a styled
   drop/select affordance rather than the raw `<input type="file">`
   browser default, while preserving the existing accept/size validation
   behaviour.

---

### User Story 3 - Lists, detail pages, and the app shell feel cohesive (Priority: P2)

The idea list, idea detail page, admin queue, notifications panel, and the
overall app shell (top bar + main content frame) share one visual language:
the same card/surface treatment, the same status-badge style, the same
empty-state pattern, and the same loading skeleton style.

**Why this priority**: Once forms and global tokens land (P1), the
remaining felt-inconsistency lives in lists and the app shell. Without
this, the polish work would look incomplete on the most-visited pages,
but it cannot be done well before the shared tokens exist.

**Independent Test**: A reviewer opens the idea list, an idea detail page,
the admin queue, and the notifications panel and confirms (a) cards/rows
use one shared surface style, (b) every status (Submitted / Under Review /
Accepted / Rejected) renders as a badge in a shared component with a
distinct, accessible color treatment, (c) empty lists show a consistent
empty-state block (icon + short message), (d) loading states use a
consistent skeleton or spinner pattern rather than a bare "Loading…"
string.

**Acceptance Scenarios**:

1. **Given** the idea list page, **When** it renders with results, **Then**
   each row/card uses the shared surface style, displays the idea's title,
   category, submitter, submission date, and status badge in a consistent
   layout, and the row has a subtle hover state indicating it is
   navigable.
2. **Given** any page that can show a list, **When** the list is empty,
   **Then** an empty-state block is shown using the shared empty-state
   component (short heading + supporting line + optional call-to-action),
   not a bare "No results" string.
3. **Given** any page that is loading its primary data, **When** the data
   is in flight, **Then** a skeleton or spinner from the shared loading
   component is shown rather than a bare text fallback.
4. **Given** an idea has a status, **When** the status is rendered
   anywhere in the app, **Then** it uses the shared `StatusBadge`
   treatment for that status, so the same status looks identical on the
   list, the detail page, and the admin queue.

---

### Edge Cases

- **Long content**: very long idea titles, descriptions, and admin
  comments must wrap cleanly inside cards and table cells without
  overflowing the page or breaking the layout grid.
- **Long lists**: the idea list and admin queue must remain visually
  consistent when populated with many rows (no alternating styling, no
  layout shift between pages of results).
- **Reduced motion**: hover and focus transitions must be subtle enough
  that they are acceptable to users with `prefers-reduced-motion: reduce`;
  any non-trivial animation MUST be gated on that media query.
- **Keyboard-only users**: every interactive element reachable by mouse
  must also be reachable and operable by keyboard with a visible focus
  state.
- **Dark mode**: out of scope for this feature; the refreshed light theme
  MUST NOT preclude a future dark theme (i.e., colors should be applied
  via semantic tokens, not hardcoded in arbitrary components).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The frontend MUST define a shared set of Tailwind-based
  design tokens (color, spacing, radius, typography scale, shadow,
  motion) consumed by all in-scope pages and components.
- **FR-002**: The frontend MUST expose a small set of shared UI
  primitives (at minimum: `Button`, `Input`, `Textarea`, `Select`,
  `Label`, `FieldError`, `StatusBadge`, `Card`, `EmptyState`,
  `LoadingSkeleton`) that all in-scope pages use instead of ad-hoc
  styling.
- **FR-003**: The app shell (top navigation + main content frame)
  MUST use a consistent max-width container, horizontal padding, and
  vertical rhythm on all routes.
- **FR-004**: Every interactive control MUST have a visible hover
  state on pointer devices and a visible focus state for keyboard
  users that meets WCAG 2.1 AA contrast.
- **FR-005**: Every in-scope form (Login, Register, New Idea, Admin
  Decision) MUST use the shared input/label/error primitives and a
  shared loading-and-disabled pattern for its submit button.
- **FR-006**: Idea status MUST be rendered via the shared
  `StatusBadge` component everywhere it appears, with one
  color/accent treatment per status that is distinguishable without
  relying on color alone.
- **FR-007**: All in-scope pages MUST render a shared empty state
  when their primary list is empty and a shared loading state while
  primary data is in flight.
- **FR-008**: The refresh MUST NOT change any route, API contract,
  request/response shape, validation rule, or persisted data shape.
- **FR-009**: The refresh MUST preserve all existing accessibility
  affordances (form labels, ARIA attributes, semantic landmarks) and
  MUST NOT regress keyboard navigation.
- **FR-010**: The frontend MUST remain responsive on viewport widths
  from 375px through 1920px without horizontal scrolling on primary
  content.
- **FR-011**: Any non-trivial transition or animation MUST be
  disabled or reduced under `prefers-reduced-motion: reduce`.
- **FR-012**: ESLint, TypeScript, and the existing frontend test suite
  MUST continue to pass after the refresh; selector-only test updates
  are permitted where wrapper structure changed, behaviour-level
  assertions MUST remain.

### Non-Functional Requirements

- **NFR-001**: No new runtime UI dependency heavier than what Tailwind
  already provides MAY be introduced (no Material UI, Chakra, Ant
  Design, or component-kit replacements). Headless utility libraries
  (e.g., a headless dialog/menu primitive) are acceptable if needed
  for accessibility.
- **NFR-002**: The production CSS bundle increase from this feature
  SHOULD be minimal (target: not more than ~10 KB gzipped over
  current baseline) since Tailwind purges unused classes.
- **NFR-003**: Color choices MUST achieve WCAG 2.1 AA contrast for
  body text and interactive elements against their background.

### Out of Scope

- A full dark theme.
- A new logo, brand mark, or marketing-style landing page.
- New product features, new routes, or new data fields.
- Replacing the routing, state-management, or data-fetching layer.
- Backend, API, or database changes of any kind.
- Internationalization / RTL layout work.

### Key Entities *(reference only — no schema change)*

- **Idea** (existing): title, description, category, attachment,
  status, submitter, timestamps.
- **Decision** (existing): status transition + comment + admin +
  timestamp.
- **Notification** (existing): recipient, kind, payload, readAt.

No entity is added, removed, or changed by this feature.

## Success Criteria *(mandatory)*

- **SC-001**: All four primary flows (auth, submit idea, browse +
  view idea, admin decision) render using only the shared UI
  primitives and design tokens; no in-scope page contains ad-hoc
  per-page Tailwind class soup that duplicates a primitive's role.
- **SC-002**: A keyboard-only walkthrough of all four primary flows
  completes successfully with a visible focus indicator on every
  interactive element at every step.
- **SC-003**: A reviewer can identify the status of any idea in the
  list, on the detail page, and in the admin queue at a glance from
  the badge alone (without reading the surrounding text).
- **SC-004**: Lighthouse Accessibility score for the idea list and
  the new idea form is ≥ 95 on desktop and mobile presets.
- **SC-005**: All pre-refresh behavioural tests (unit, integration,
  E2E) continue to pass; any test change required is purely a
  selector update tied to a renamed class or restructured wrapper,
  not an assertion change.

## Assumptions

- Tailwind CSS is already configured in the frontend
  (`frontend/tailwind.config.*` / `index.css`) and remains the
  styling mechanism for this feature.
- React + Vite remains the frontend stack; no migration is
  performed as part of this work.
- Light theme only; dark theme is intentionally deferred.
- The current information architecture (routes, navigation
  structure) is correct; only its presentation is being refined.

## Dependencies

- Phase 1 MVP (`001-phase1-mvp`) frontend feature surface is the
  baseline to be refreshed.
- No backend or infrastructure dependencies.

## Open Questions

- Should the shared primitives live under
  `frontend/src/components/ui/` or `frontend/src/lib/ui/`? (Defer to
  `/speckit.plan`.)
- Are we adopting a headless primitive library (e.g., Radix, Headless
  UI) for dialog/menu accessibility, or hand-rolling the few we need?
  (Defer to `/speckit.plan`; NFR-001 allows either.)
- Final color palette specifics (exact hex values for primary /
  status badges) are deferred to the plan/implementation phase, as
  long as they satisfy NFR-003 contrast.
