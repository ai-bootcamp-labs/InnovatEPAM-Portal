# Quickstart & Manual Testing — InnovatEPAM Portal Phase 1 MVP

**Feature**: Phase 1 MVP — InnovatEPAM Portal
**Date**: 2026-05-12
**Audience**: Developers, QA, and demo operators bringing the MVP up locally
or smoke-testing it before promotion.

This document is split in two parts:

1. **Quickstart** — get the stack running on a dev workstation.
2. **Manual Testing Guidelines** — checklists per user story (P1, P2, P3) plus
   cross-cutting accessibility, responsiveness, and security checks. These are
   the canonical scripts demo operators and QA run before any release.

---

## Part 1 — Quickstart

### 1.1 Prerequisites

| Tool                    | Version             |
|-------------------------|---------------------|
| .NET SDK                | 8.0.x (LTS)         |
| Node.js                 | 20.x (LTS)          |
| npm                     | 10.x (bundled)      |
| Docker Desktop          | 4.x or newer        |
| PostgreSQL client (`psql`) | 15.x (optional, for inspection) |
| Git                     | 2.40+               |

### 1.2 Start PostgreSQL

```powershell
docker run -d --name innovatepam-pg `
  -e POSTGRES_DB=innovatepam `
  -e POSTGRES_USER=innovatepam `
  -e POSTGRES_PASSWORD=devpassword `
  -p 5432:5432 `
  postgres:15
```

### 1.3 Configure the backend

`backend/src/InnovatEpam.Portal.Api/appsettings.Development.json` (excerpt):

```json
{
  "ConnectionStrings": {
    "Postgres": "Host=localhost;Port=5432;Database=innovatepam;Username=innovatepam;Password=devpassword"
  },
  "Jwt": {
    "Issuer": "innovatepam-portal",
    "Audience": "innovatepam-portal",
    "SigningKey": "dev-only-key-please-replace-in-prod-32+chars"
  },
  "Attachments": {
    "RootPath": "./.attachments"
  },
  "Seed": {
    "AdminEmail": "admin@innovatepam.test",
    "AdminPassword": "Adm1n!Pass"
  },
  "Cors": {
    "AllowedOrigins": [ "http://localhost:5173" ]
  }
}
```

> **Never commit production secrets.** Use User Secrets in dev and environment
> variables / Key Vault in higher environments (constitution Tech Stack).

### 1.4 Apply EF Core migrations and run the API

```powershell
cd backend
dotnet tool restore
dotnet ef database update `
  --project src/InnovatEpam.Portal.Infrastructure `
  --startup-project src/InnovatEpam.Portal.Api
dotnet run --project src/InnovatEpam.Portal.Api
```

The API listens on `https://localhost:7081` (Swagger UI at `/swagger`).

### 1.5 Run the SPA

```powershell
cd frontend
npm ci
npm run dev
```

The SPA listens on `http://localhost:5173`. It reads the API base URL from
`VITE_API_BASE_URL` (defaulted in `.env.development` to
`https://localhost:7081/api/v1`).

### 1.6 Smoke-test the stack

1. Open the SPA at `http://localhost:5173`.
2. You should be redirected to **Login**.
3. Sign in with the seeded admin: `admin@innovatepam.test / Adm1n!Pass`.
4. The Idea listing loads and shows an empty-state card.
5. Open `https://localhost:7081/swagger` and confirm `GET /api/v1/health` returns `200`.

### 1.7 Run automated tests

```powershell
# Backend
cd backend
dotnet test

# Frontend
cd ../frontend
npm run lint
npm run typecheck
npm run test
npx playwright install --with-deps   # first run only
npm run test:e2e
```

---

## Part 2 — Manual Testing Guidelines

The constitution requires every screen to handle the four canonical states
(loading, empty, error, success) and to meet WCAG 2.1 AA. The checklists
below are organised by user story so they can be run independently — finishing
the **P1** checklist alone proves the MVP can deliver value end-to-end.

### How to use these checklists

- Run them against a **fresh database** (delete the dev DB and re-apply
  migrations, or `docker rm -f innovatepam-pg && re-create`).
- Use **two browser profiles** (or one normal + one Incognito) so you can be
  Submitter and Admin simultaneously.
- Capture results: tick the box, paste a short note for any failure, and link
  a screenshot.
- Test on at least **one desktop browser** (1440 px) and **one mobile profile**
  (DevTools → iPhone 13, 390 × 844).

> Date display checks: every visible date MUST be rendered through one of the
> three `date-fns` helpers in `frontend/src/lib/date.ts`
> (`formatIdeaDate`, `formatIdeaDateTime`, `formatRelative`). If you see a raw
> ISO string anywhere in the UI, mark it as a defect.

---

### Checklist M1 — User Story P1: Submitter captures and tracks an idea

**Persona**: Bahar, a new EPAM staff member with no portal account.

| #     | Step                                                                                  | Expected outcome                                                                                                               | ✓/✗ |
|-------|---------------------------------------------------------------------------------------|--------------------------------------------------------------------------------------------------------------------------------|-----|
| M1.1  | Open the portal in a clean browser session.                                           | Redirected to **/login** with **Register** link visible.                                                                       |     |
| M1.2  | Click **Register**, leave fields blank, submit.                                       | Field-level errors appear under each field; no network request is made.                                                        |     |
| M1.3  | Register with `bahar@innovatepam.test` / `Welcome1!` and display name `Bahar S.`      | Account created; user is logged in; landed on **/ideas**.                                                                      |     |
| M1.4  | Re-register with the same email in a new tab.                                         | API returns 409; UI shows generic "could not create account" message — no leak of existing-account info.                        |     |
| M1.5  | Click **Submit Idea**. Submit blank form.                                             | Validation errors on title, description, category.                                                                             |     |
| M1.6  | Title `Bot for triaging tickets`, description (paste 4 100 chars).                    | Description shows length error; submit blocked.                                                                                |     |
| M1.7  | Title `Bot for triaging tickets`, description ~500 chars, category **Technology**, attach `bad.exe`. | UI rejects file before upload (extension allow-list); message names the allowed types.                                          |     |
| M1.8  | Replace with a 12 MB PDF.                                                             | UI rejects with "file is larger than 10 MB" before upload.                                                                     |     |
| M1.9  | Replace with a valid 1.2 MB PDF and submit.                                           | Toast confirms creation; navigated to idea detail; status badge shows **Submitted**; attachment download link present.          |     |
| M1.10 | Open the attachment download link in a new tab.                                       | PDF downloads and opens.                                                                                                       |     |
| M1.11 | Go back to **/ideas**.                                                                | Idea appears at the top of the list with title, category, submitter `Bahar S.`, formatted date (e.g. `12 May 2026`), status.   |     |
| M1.12 | Click **Logout**.                                                                     | Redirected to **/login**; visiting `/ideas` directly redirects to login with a `returnUrl`.                                     |     |
| M1.13 | Log back in.                                                                          | After login, redirected back to `/ideas`; idea is still listed.                                                                |     |
| M1.14 | On the idea card, hover the date.                                                     | Tooltip shows full date+time via `formatIdeaDateTime`; relative "X minutes ago" via `formatRelative`.                          |     |

### Checklist M2 — User Story P2: Admin reviews and decides

**Persona**: Onur, the seeded Admin (second browser profile).

| #     | Step                                                                                  | Expected outcome                                                                                                                | ✓/✗ |
|-------|---------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|-----|
| M2.1  | Sign in as `admin@innovatepam.test`.                                                  | Top-right shows **Admin** role badge; **Decide** controls appear on each idea row.                                              |     |
| M2.2  | Filter the listing by status **Submitted**.                                           | Only Bahar's idea is shown.                                                                                                     |     |
| M2.3  | Open the idea, click **Move to Under Review**.                                        | Status badge updates to **Under Review**; status history shows new entry with Onur's name and timestamp; no comment required.   |     |
| M2.4  | Click **Accept**, leave comment empty, click **Confirm**.                             | Inline error: "Comment is required". No state change.                                                                           |     |
| M2.5  | Enter comment "Approved — proceed to discovery." and confirm.                         | Status becomes **Accepted**; decision comment, decision-maker name, decision date displayed; status badge color = green.        |     |
| M2.6  | Try to click **Reject** on the same idea.                                             | Decision controls are disabled with tooltip "Idea has reached a terminal status".                                              |     |
| M2.7  | Open the same idea in a third tab as a Submitter.                                     | Submitter view shows new status, the comment, and "Decided by Onur on 12 May 2026, 14:30" formatted via `formatIdeaDateTime`.   |     |
| M2.8  | (Concurrency) Submit a second idea as Bahar. As Onur, open it in two tabs and click **Accept** in both with different comments. | First wins (201); second receives 409 ProblemDetails with `title: "Concurrent decision"`. UI surfaces a clear error and refreshes. |     |
| M2.9  | (Authorization) As Bahar, copy the Accept request URL from the network tab and replay via DevTools/`curl` with her JWT. | API responds 403 with ProblemDetails. Audit trail unchanged.                                                                    |     |
| M2.10 | (Notification) After M2.5, switch back to Bahar's tab.                                | Bell icon shows unread count = 1 within 30 s; opening the panel shows "Your idea 'Bot for triaging tickets' was Accepted."      |     |

### Checklist M3 — User Story P3: Browse and filter the catalog

| #     | Step                                                                                  | Expected outcome                                                                                                                | ✓/✗ |
|-------|---------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------|-----|
| M3.1  | Seed at least 10 ideas across all 4 statuses and 3 categories (use `npm run seed:demo`).| Listing shows 10 items, newest first; pagination control hidden (≤ 20 items).                                                  |     |
| M3.2  | Filter by status **Accepted**.                                                        | Only Accepted items shown; URL reflects `?status=Accepted` (deep-linkable).                                                     |     |
| M3.3  | Add category filter **Process**.                                                      | Both filters compose; result count is correct against database (`SELECT COUNT(*) FROM portal.idea WHERE status=3 ...`).         |     |
| M3.4  | Clear filters.                                                                        | Listing resets; URL clean.                                                                                                      |     |
| M3.5  | Open one idea.                                                                        | Detail view: title, full description, category, submitter, dates (date-fns formatted), current status, attachment link, full status history (oldest first). |     |
| M3.6  | Empty state: filter by **Rejected** when none exist.                                  | Empty state component renders with friendly copy and a CTA back to "All ideas". No spinner.                                     |     |
| M3.7  | Error state: stop the API container and reload the listing.                           | Error state renders with retry button; never a blank page or unhandled exception toast.                                         |     |

---

### Checklist X1 — Cross-cutting: Responsiveness (Principle III)

Test each of the three primary screens (Listing, Submit form, Detail) at **all
four** required breakpoints. Use Chrome DevTools' **Responsive** tool.

| Screen      | 360 px | 768 px | 1024 px | 1440 px |
|-------------|--------|--------|---------|---------|
| Listing     |  □     |  □     |  □      |  □      |
| Submit form |  □     |  □     |  □      |  □      |
| Detail      |  □     |  □     |  □      |  □      |

For each cell verify:
- No horizontal scrollbar.
- Tap targets ≥ 44 × 44 CSS px on mobile.
- Primary actions are visible without scrolling on the relevant screen.

### Checklist X2 — Cross-cutting: Accessibility (WCAG 2.1 AA)

| #    | Check                                                                            | Tool                | ✓/✗ |
|------|----------------------------------------------------------------------------------|---------------------|-----|
| X2.1 | All form fields have associated `<label>`s.                                      | Axe DevTools        |     |
| X2.2 | Tab order on Submit form is logical: Title → Description → Category → File → Submit. | Keyboard only      |     |
| X2.3 | Focus ring is visible on every interactive element on every screen.              | Keyboard only       |     |
| X2.4 | Color contrast ≥ 4.5:1 for all text and status badges.                            | Axe DevTools        |     |
| X2.5 | Screen reader announces page title, status badges, and validation errors.        | NVDA / VoiceOver    |     |
| X2.6 | Decision dialog traps focus while open, returns focus on close.                  | Keyboard only       |     |
| X2.7 | No Axe violations of severity "Serious" or "Critical" on Listing, Detail, Submit. | Axe DevTools        |     |

### Checklist X3 — Cross-cutting: Security & data integrity

| #    | Check                                                                                                        | ✓/✗ |
|------|--------------------------------------------------------------------------------------------------------------|-----|
| X3.1 | Without a JWT, `GET /api/v1/ideas` returns 401.                                                              |     |
| X3.2 | With a Submitter JWT, `POST /api/v1/ideas/{id}/decisions` returns 403.                                       |     |
| X3.3 | Login response and any error response do not contain the password hash, JWT secret, or stack trace.          |     |
| X3.4 | API logs (Serilog) never contain plaintext passwords or JWTs (grep dev log file).                             |     |
| X3.5 | Uploaded files of disallowed type are rejected even if extension is renamed (e.g. `payload.exe` → `payload.pdf`). | |
| X3.6 | A migration `dotnet ef migrations remove` followed by `dotnet ef database update <Previous>` succeeds (reversibility). | |
| X3.7 | `dotnet list package --vulnerable --include-transitive` and `npm audit --production` report **no high or critical** findings. | |

### Checklist X4 — Cross-cutting: Date formatting (date-fns)

| #    | Check                                                                                                        | ✓/✗ |
|------|--------------------------------------------------------------------------------------------------------------|-----|
| X4.1 | Idea card "submitted" label uses `formatIdeaDate` → `12 May 2026`.                                            |     |
| X4.2 | Idea detail "Decided on" label uses `formatIdeaDateTime` → `12 May 2026, 14:30`.                              |     |
| X4.3 | Notifications panel shows relative times via `formatRelative` (e.g., "3 minutes ago").                       |     |
| X4.4 | A grep for `toLocaleDateString` and `new Date().toString()` in `frontend/src/**/*.tsx` returns **no hits** (all formatting must go through `lib/date.ts`). | |
| X4.5 | Bundle analyzer shows date-fns contributes ≤ 20 KB gzipped (constitution dependency budget).                  |     |

---

## Sign-off

A release is approved for promotion when:

- M1, M2, M3 are 100 % green on at least one desktop browser and one mobile profile.
- X1 is fully green.
- X2 has no Serious/Critical Axe violations.
- X3 is fully green.
- X4 is fully green.

Record the completed checklists in the release ticket and link them from the
PR description per constitution governance (compliance one-liner).
