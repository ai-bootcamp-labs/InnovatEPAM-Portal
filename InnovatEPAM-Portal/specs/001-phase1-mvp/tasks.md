---
description: "Task list for Phase 1 MVP — InnovatEPAM Portal"
---

# Tasks: Phase 1 MVP — InnovatEPAM Portal

**Input**: Design documents from `specs/001-phase1-mvp/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/openapi.yaml](./contracts/openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: Included. The plan defines a backend test stack (xUnit + FluentAssertions + Testcontainers) and a frontend test stack (Vitest + Playwright). Constitution Dev Workflow gate #4 requires `dotnet test` and `npm run test` to pass in CI, so test tasks are first-class.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no cross-task dependencies)
- **[Story]**: `[US1]`, `[US2]`, `[US3]` — maps to user stories from spec.md
- File paths are repository-relative and resolve under `backend/` or `frontend/` per the structure in plan.md

## Path Conventions

- **Web app monorepo** (per plan.md):
  - Backend: `backend/src/InnovatEpam.Portal.{Api,Application,Domain,Infrastructure}/...`
  - Backend tests: `backend/tests/InnovatEpam.Portal.{UnitTests,IntegrationTests}/...`
  - Frontend: `frontend/src/{app,features,components,lib,routes}/...`
  - Frontend tests: `frontend/tests/{unit,e2e}/...`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Stand up the monorepo skeleton, toolchains, and dev environment so any story can begin.

- [x] T001 Create monorepo top-level structure (`backend/`, `frontend/`, `docs/`, `.editorconfig`, root `README.md`) per [plan.md](./plan.md) "Project Structure" section
- [x] T002 [P] Create `.gitignore` covering `bin/`, `obj/`, `node_modules/`, `dist/`, `.attachments/`, `.env*`, `*.user`, `.vs/`, `.idea/`
- [x] T003 [P] Add `.editorconfig` at repo root enforcing UTF-8, LF, 4-space indent for `*.cs`, 2-space for `*.{ts,tsx,json,yml,yaml,md}`
- [x] T004 Create the .NET solution `backend/InnovatEpam.Portal.sln` and four class library / web projects: `InnovatEpam.Portal.Api` (webapi), `InnovatEpam.Portal.Application` (classlib), `InnovatEpam.Portal.Domain` (classlib), `InnovatEpam.Portal.Infrastructure` (classlib); add project references Api → Application → Domain, Application → Domain, Infrastructure → Application + Domain, Api → Infrastructure
- [x] T005 [P] Create the two test projects under `backend/tests/`: `InnovatEpam.Portal.UnitTests` and `InnovatEpam.Portal.IntegrationTests` (xUnit), referencing the relevant src projects
- [x] T006 [P] Add `Directory.Build.props` at `backend/` enforcing `<TargetFramework>net8.0</TargetFramework>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`, `<LangVersion>latest</LangVersion>`
- [x] T007 [P] Add `backend/.config/dotnet-tools.json` with `dotnet-ef` and `dotnet-format` as local tools; commit `dotnet tool restore` instructions to `backend/README.md`
- [x] T008 [P] Add NuGet packages to the appropriate backend projects per [research.md](./research.md): Api — `Microsoft.AspNetCore.Authentication.JwtBearer`, `Swashbuckle.AspNetCore`, `Serilog.AspNetCore`, `Serilog.Sinks.Console`, `FluentValidation.AspNetCore`; Infrastructure — `Microsoft.EntityFrameworkCore.Design`, `Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Identity.EntityFrameworkCore`, `EFCore.NamingConventions`; UnitTests — `xunit`, `FluentAssertions`, `Microsoft.NET.Test.Sdk`; IntegrationTests — additionally `Microsoft.AspNetCore.Mvc.Testing`, `Testcontainers.PostgreSql`
- [x] T009 Initialize the Vite + React + TypeScript workspace at `frontend/` (`npm create vite@latest -- --template react-ts`); set Node engines to `>=20` in `frontend/package.json`
- [x] T010 [P] Add Tailwind CSS to `frontend/`: install `tailwindcss postcss autoprefixer`, generate `tailwind.config.ts` and `postcss.config.cjs`, add Tailwind directives to `frontend/src/index.css`
- [x] T011 [P] Initialize shadcn/ui in `frontend/` (`npx shadcn@latest init`) producing `frontend/components.json`; configure path alias `@/*` → `frontend/src/*` in `frontend/tsconfig.json` and `frontend/vite.config.ts`
- [x] T012 [P] Install the remaining frontend deps (pinned): `@tanstack/react-query`, `react-router-dom`, `react-hook-form`, `zod`, `@hookform/resolvers`, `date-fns`, `clsx`, `tailwind-merge`
- [x] T013 [P] Install frontend dev/test deps: `vitest`, `@testing-library/react`, `@testing-library/jest-dom`, `@testing-library/user-event`, `jsdom`, `@playwright/test`, `eslint`, `@typescript-eslint/{eslint-plugin,parser}`, `eslint-plugin-react`, `eslint-plugin-react-hooks`, `eslint-plugin-jsx-a11y`, `prettier`
- [x] T014 [P] Configure ESLint (`frontend/.eslintrc.cjs` with `react`, `@typescript-eslint`, `jsx-a11y`, `react-hooks`), Prettier (`frontend/.prettierrc`), TypeScript strict mode (`tsconfig.json` `strict: true`, `noUncheckedIndexedAccess: true`)
- [x] T015 [P] Add npm scripts in `frontend/package.json`: `dev`, `build`, `preview`, `lint`, `typecheck`, `test`, `test:e2e`, `format`
- [x] T016 [P] Create `docker-compose.dev.yml` at repo root with the PostgreSQL 15 service from [quickstart.md](./quickstart.md) §1.2 (named `innovatepam-pg`, env, port mapping)
- [x] T017 [P] Add GitHub Actions CI workflow at `.github/workflows/ci.yml` running, on every PR: `dotnet restore`, `dotnet format --verify-no-changes`, `dotnet build -warnaserror`, `dotnet test`, `dotnet list package --vulnerable --include-transitive`, `npm ci` (frontend), `npm run lint`, `npm run typecheck`, `npm run test`, `npm run build`, `npm audit --production`
- [x] T018 [P] Create `docs/manual-testing.md` referencing [quickstart.md](./quickstart.md) Part 2 as the canonical manual checklist

**Checkpoint**: `dotnet build`, `dotnet test`, `npm run lint`, `npm run typecheck`, `npm run build` all succeed against an empty solution and SPA scaffold.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Stand up the cross-cutting backbone every user story depends on — DB context with Identity, auth pipeline, error handling, logging, OpenAPI, frontend providers and routing shell.

**⚠️ CRITICAL**: No user story work begins until this phase is done.

### Backend foundation

- [ ] T019 Define `IdeaStatus` and `DecisionAction` enums in `backend/src/InnovatEpam.Portal.Domain/Enums/`
- [ ] T020 Create `AppUser` and `AppRole` Identity entities in `backend/src/InnovatEpam.Portal.Domain/Identity/AppUser.cs` and `AppRole.cs` (see [data-model.md](./data-model.md) §1, §2)
- [ ] T021 Create `PortalDbContext : IdentityDbContext<AppUser, AppRole, Guid>` in `backend/src/InnovatEpam.Portal.Infrastructure/Persistence/PortalDbContext.cs` with empty `DbSet`s for `Idea`, `Category`, `Attachment`, `Decision`, `IdeaStatusHistory`, `Notification`, `OutboxMessage`; apply `UseSnakeCaseNamingConvention()` and schema `portal`
- [ ] T022 Add `SaveChangesInterceptor` `AuditFieldsInterceptor` in `backend/src/InnovatEpam.Portal.Infrastructure/Persistence/Interceptors/` to populate `CreatedAt` and `UpdatedAt` (`DateTimeOffset.UtcNow`) on entities implementing `IAuditable`; register in DI
- [ ] T023 Configure ASP.NET Core Identity options in `backend/src/InnovatEpam.Portal.Api/Program.cs`: PBKDF2 password hashing, `RequireDigit`, `RequireLowercase`, `RequireUppercase=false`, `RequiredLength=8`, lockout disabled in Phase 1 (FR-006/7)
- [ ] T024 Configure JWT bearer auth in `Program.cs` per [research.md](./research.md) R1: HS256, 60-min lifetime, claims `sub`, `email`, `role`; bind options from `Jwt:` config section
- [ ] T025 Register authorization in `Program.cs`: define the `Admin` policy (`requires role == "Admin"`) per R13 **and** set `AuthorizationOptions.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();` so every endpoint is auth-required by default (FR-023). Allow-list `AuthController.Register` and `AuthController.Login` with `[AllowAnonymous]`; the `/api/v1/health` endpoint is also `[AllowAnonymous]`
- [ ] T026 Configure RFC 7807 ProblemDetails: register `AddProblemDetails()`, add custom `IExceptionHandler` `DomainExceptionHandler` in `backend/src/InnovatEpam.Portal.Api/ErrorHandling/` mapping `NotFoundException → 404`, `ConflictException → 409`, `ForbiddenException → 403`, `ValidationException → 400` (per R5)
- [ ] T027 Configure Serilog with JSON console sink and request enrichment (`RequestId`, `UserId`, `Endpoint`); add destructuring policy that scrubs `password`, `passwordHash`, `accessToken` properties (R7)
- [ ] T028 Configure Swashbuckle to emit OpenAPI at `/swagger/v1/swagger.json`; map XML doc comments from `*.xml` files; add JWT bearer security scheme so Swagger UI supports auth
- [ ] T029 Add CORS allow-list configuration in `Program.cs` reading `Cors:AllowedOrigins` from configuration; default policy applied to all controllers
- [ ] T030 Configure controllers under route prefix `/api/v1/` via a `RoutePrefix` convention in `backend/src/InnovatEpam.Portal.Api/Conventions/ApiVersionRouteConvention.cs` (Constitution Principle II); register in `AddControllers`
- [ ] T030a Enforce HTTPS-only transport in `Program.cs` (Constitution Tech Stack & Constraints): in non-Development environments call `app.UseHsts()` (max-age 365 days, includeSubDomains) and `app.UseHttpsRedirection()`; configure Kestrel HTTPS endpoint via `appsettings.Production.json`; add an integration test asserting a request to `http://...` is redirected with 308 in production configuration
- [ ] T031 Define `IAttachmentStorage` interface in `backend/src/InnovatEpam.Portal.Application/Storage/IAttachmentStorage.cs` with `SaveAsync(stream, contentType, ct) → StorageKey`, `OpenReadAsync(key, ct)`, `DeleteAsync(key, ct)`
- [ ] T032 Implement `FileSystemAttachmentStorage : IAttachmentStorage` in `backend/src/InnovatEpam.Portal.Infrastructure/Storage/FileSystemAttachmentStorage.cs` rooted at `Attachments:RootPath` config; layout `{root}/{yyyy}/{MM}/{guid}{ext}`; register in DI
- [ ] T033 Add file-validation utility `FileSignatureValidator` in `backend/src/InnovatEpam.Portal.Application/Validation/FileSignatureValidator.cs` with magic-byte tables for the 6 allowed types (per R4); unit-test stub created in T036
- [ ] T034 Create the initial EF migration `20260512_Initial` in `backend/src/InnovatEpam.Portal.Infrastructure/Migrations/`: enable `pgcrypto` and `citext` extensions, create schema `portal`, all entity tables, indexes, and CHECK constraints from [data-model.md](./data-model.md); seed `app_role` rows (Submitter, Admin) and `category` rows (process, product, technology, people, other)
- [ ] T035 Add raw-SQL portion of the migration: `trg_decision_after_insert` trigger that updates `idea.status`, `idea.last_decision_id`, `idea.updated_at` and inserts a row into `idea_status_history`; `trg_idea_terminal_status_immutable` trigger that raises on UPDATE when prior status was Accepted/Rejected and status changed (FR-020, FR-021)
- [ ] T036 [P] Add `DatabaseFixture` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Fixtures/DatabaseFixture.cs` using `Testcontainers.PostgreSql` to start a Postgres 15 container, apply migrations once per fixture; expose `WebApplicationFactory<Program>` configured with the throwaway connection string
- [ ] T037 [P] Add migration-reversibility test in `backend/tests/InnovatEpam.Portal.IntegrationTests/Migrations/InitialMigrationTests.cs`: apply `20260512_Initial`, then `MigrateAsync` to "0" (down), then re-apply; assert no exception and all extensions/triggers recreated (Constitution Dev Workflow gate #6)

### Frontend foundation

- [ ] T038 Set up the React Router shell in `frontend/src/app/Router.tsx` with routes: `/login`, `/register`, `/ideas` (protected), `/ideas/:id` (protected), `/ideas/new` (protected, Submitter), `/admin` (protected, Admin). Implement a `<RequireAuth />` wrapper and a `<RequireRole role="Admin" />` wrapper that redirect to `/login?returnUrl=...`
- [ ] T039 Wire providers in `frontend/src/app/Providers.tsx`: `QueryClientProvider` (TanStack Query, `staleTime: 30_000` for lists), `AuthProvider`, `Toaster` (shadcn `sonner`), `ThemeProvider`
- [ ] T040 [P] Generate the OpenAPI TypeScript client into `frontend/src/lib/api/generated/` using `openapi-typescript` from [contracts/openapi.yaml](./contracts/openapi.yaml); expose a typed `apiClient` with the JWT injected from `AuthProvider`
- [ ] T041 [P] Create the date-fns helpers module `frontend/src/lib/date.ts` exporting exactly `formatIdeaDate`, `formatIdeaDateTime`, `formatRelative` per R10; unit-test in T087
- [ ] T042 [P] Create `frontend/src/lib/utils.ts` with the standard shadcn `cn(...inputs)` helper using `clsx` + `tailwind-merge`
- [ ] T043 [P] Create reusable layout primitives in `frontend/src/components/layout/`: `AppShell.tsx` (header with logo, user menu, role badge, notification bell, logout), `MainContainer.tsx` (responsive max-width wrapper), `PageHeader.tsx`
- [ ] T044 [P] Create `frontend/src/components/state/`: `LoadingState.tsx` (skeleton variants), `EmptyState.tsx` (icon + copy + CTA), `ErrorState.tsx` (message + retry callback), `StatusBadge.tsx` (`IdeaStatus` → color/label) — covers Constitution Principle III "four canonical states" requirement
- [ ] T045 [P] Add shadcn primitives via CLI into `frontend/src/components/ui/`: `button`, `input`, `textarea`, `select`, `label`, `form`, `card`, `dialog`, `sheet`, `dropdown-menu`, `table`, `tooltip`, `badge`, `sonner`
- [ ] T046 [P] Configure Vitest in `frontend/vitest.config.ts` with `jsdom`, setup file `frontend/tests/unit/setup.ts` importing `@testing-library/jest-dom`; add `tests/unit/` and a smoke test
- [ ] T047 [P] Configure Playwright in `frontend/playwright.config.ts` with `webServer` starting `npm run dev` and the API container; create a `tests/e2e/fixtures/` baseline auth helper

**Checkpoint**: `dotnet ef database update` produces the full schema; `Program.cs` boots; the SPA renders the AppShell and redirects to `/login`. No user-story behaviour yet.

---

## Phase 3: User Story 1 — Submitter captures and tracks an idea (Priority: P1) 🎯 MVP

**Goal**: A new user can register, log in, submit an idea (with optional attachment), see it in the listing with status **Submitted**, log out, and log back in.

**Independent Test**: Execute checklist [M1 in quickstart.md](./quickstart.md#checklist-m1--user-story-p1-submitter-captures-and-tracks-an-idea) end-to-end against a fresh database.

### Tests for User Story 1

- [ ] T048 [P] [US1] Contract test `AuthEndpointsTests` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Auth/AuthEndpointsTests.cs`: `POST /api/v1/auth/register` returns 201 + JWT; duplicate email → 409; weak password → 400 with ProblemDetails errors; `POST /auth/login` happy path → 200; bad password → 401; `POST /auth/logout` → 204; `GET /auth/me` returns the registered user
- [ ] T049 [P] [US1] Contract test `IdeasCreateAndListTests` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Ideas/IdeasCreateAndListTests.cs`: authenticated `POST /api/v1/ideas` with valid payload → 201 + Location header + status `Submitted`; missing fields → 400 with field errors; `GET /api/v1/ideas` returns the new idea with `total = 1`
- [ ] T050 [P] [US1] Contract test `AttachmentUploadTests` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Attachments/AttachmentUploadTests.cs`: `PUT /api/v1/ideas/{id}/attachment` with PDF on a fresh idea → 201 + `AttachmentSummary`; second `PUT` (replacement) → 200 + new `AttachmentSummary`; `.exe` → 415; 11 MB → 413; non-submitter non-admin caller → 403; download returns the same bytes
- [ ] T051 [P] [US1] Vitest component test `RegisterForm.test.tsx` in `frontend/tests/unit/auth/RegisterForm.test.tsx`: renders fields, displays validation errors, calls `register` mutation on submit
- [ ] T052 [P] [US1] Vitest component test `IdeaSubmitForm.test.tsx` in `frontend/tests/unit/ideas/IdeaSubmitForm.test.tsx`: rejects oversized file before upload, rejects disallowed extension, calls create mutation on valid submit
- [ ] T053 [P] [US1] Playwright E2E `submit-idea.spec.ts` in `frontend/tests/e2e/submit-idea.spec.ts` covering checklist M1.3, M1.9, M1.11, M1.13

### Domain & data layer

- [ ] T054 [P] [US1] Create `Category` entity in `backend/src/InnovatEpam.Portal.Domain/Categories/Category.cs` (per data-model §3) with private setters and a static factory
- [ ] T055 [P] [US1] Create `Idea` aggregate root in `backend/src/InnovatEpam.Portal.Domain/Ideas/Idea.cs` per data-model §4: factory `Create(title, description, categoryId, submitterId)`, instance method `TransitionTo(IdeaStatus next, AppUser actor, string? comment)` enforcing the legal-transitions table; raises `ConflictException` on illegal transitions; emits a domain event for status changes
- [ ] T056 [P] [US1] Create `Attachment` entity in `backend/src/InnovatEpam.Portal.Domain/Attachments/Attachment.cs` per data-model §5
- [ ] T057 [P] [US1] Create `IdeaStatusHistory` entity in `backend/src/InnovatEpam.Portal.Domain/Ideas/IdeaStatusHistory.cs` per data-model §7
- [ ] T058 [US1] Add EF Core configurations for `Category`, `Idea`, `Attachment`, `IdeaStatusHistory` in `backend/src/InnovatEpam.Portal.Infrastructure/Persistence/Configurations/` mapping all CHECK constraints, indexes, FK delete behaviours, and the `xmin` row-version on `Idea`; wire them into `PortalDbContext.OnModelCreating` (depends on T054–T057)

### Application services & DTOs

- [ ] T059 [P] [US1] Define DTOs `RegisterRequest`, `LoginRequest`, `AuthResponse`, `UserSummary` in `backend/src/InnovatEpam.Portal.Application/Auth/Dtos/` matching [contracts/openapi.yaml](./contracts/openapi.yaml)
- [ ] T060 [P] [US1] Define DTOs `CreateIdeaRequest`, `IdeaListItem`, `IdeaDetail`, `PagedIdeas`, `AttachmentSummary` in `backend/src/InnovatEpam.Portal.Application/Ideas/Dtos/`
- [ ] T061 [P] [US1] Add FluentValidation validators in `backend/src/InnovatEpam.Portal.Application/Auth/Validators/` and `.../Ideas/Validators/` (length and required-field rules per FR-007 & FR-009); register via `AddValidatorsFromAssembly`
- [ ] T062 [US1] Implement `AuthService` in `backend/src/InnovatEpam.Portal.Application/Auth/AuthService.cs`: `RegisterAsync` (assigns Submitter role), `LoginAsync` (issues JWT), `LogoutAsync` (no-op in Phase 1), `GetMeAsync` (depends on T020, T024, T059, T061)
- [ ] T063 [US1] Implement `IdeaService` in `backend/src/InnovatEpam.Portal.Application/Ideas/IdeaService.cs`: `CreateAsync(CreateIdeaRequest, submitterId)` writes the idea + initial `IdeaStatusHistory` row in a single transaction; `ListAsync(filter, page, pageSize)` returns `PagedIdeas`; `GetByIdAsync(id)` returns `IdeaDetail` (or throws `NotFoundException`) joining `Category`, submitter `AppUser`, current `Attachment`, and the latest `Decision` (depends on T055, T058, T060, T061)
- [ ] T064 [US1] Implement `AttachmentService` in `backend/src/InnovatEpam.Portal.Application/Attachments/AttachmentService.cs`: `UploadAsync(ideaId, stream, fileName, contentType, uploaderId)` invokes `FileSignatureValidator` (T033), saves via `IAttachmentStorage` (T031), persists `Attachment` row, replaces any existing attachment in a single transaction (depends on T031, T032, T033, T056, T058)

### API endpoints

- [ ] T065 [P] [US1] Implement `AuthController` in `backend/src/InnovatEpam.Portal.Api/Controllers/AuthController.cs` with `[ApiController] [Route("api/v1/auth")]` and actions `Register`, `Login`, `Logout`, `Me`; XML doc comments on every action; returns `ProblemDetails` on errors via the global handler (depends on T062)
- [ ] T066 [P] [US1] Implement `IdeasController` in `backend/src/InnovatEpam.Portal.Api/Controllers/IdeasController.cs` with `GET /api/v1/ideas`, `POST /api/v1/ideas`, `GET /api/v1/ideas/{id}`, `GET /api/v1/ideas/{id}/history`; pagination + filtering wired to `IdeaService` (depends on T063)
- [ ] T067 [P] [US1] Implement `AttachmentsController` in `backend/src/InnovatEpam.Portal.Api/Controllers/AttachmentsController.cs` with `PUT /api/v1/ideas/{id}/attachment` (multipart, idempotent create-or-replace, returns 201 on first create / 200 on replace, `[RequestSizeLimit(10_485_760)]`), `GET /api/v1/ideas/{id}/attachment` (streamed FileResult), `DELETE /api/v1/ideas/{id}/attachment`. Authorize: submitter or Admin only (depends on T064)
- [ ] T068 [P] [US1] Implement `CategoriesController` in `backend/src/InnovatEpam.Portal.Api/Controllers/CategoriesController.cs` with `GET /api/v1/categories` returning the closed seeded list (depends on T054, T058)
- [ ] T069 [US1] Configure Kestrel `MaxRequestBodySize = 10 MB` and form options `MultipartBodyLengthLimit = 10 MB` in `Program.cs`, returning a clean 413 ProblemDetails on overflow (FR-010)

### Frontend (auth + idea capture)

- [ ] T070 [P] [US1] Implement `AuthProvider` in `frontend/src/features/auth/AuthProvider.tsx`: stores JWT + user in `localStorage`, exposes `useAuth()` with `register`, `login`, `logout`, `user`; injects `Authorization` header into the generated API client
- [ ] T071 [P] [US1] Build `RegisterPage` (`frontend/src/features/auth/RegisterPage.tsx`) with shadcn `Form`, `Input` fields wired to react-hook-form + zod schema mirroring `RegisterRequest`; on success navigate to `/ideas`
- [ ] T072 [P] [US1] Build `LoginPage` (`frontend/src/features/auth/LoginPage.tsx`) likewise; honour `?returnUrl=` after success
- [ ] T073 [P] [US1] Build `useIdeasQuery` and `useCreateIdeaMutation` in `frontend/src/features/ideas/api.ts` using TanStack Query; query key `['ideas', filter, page]`; mutation invalidates `['ideas']`
- [ ] T074 [US1] Build `IdeaSubmitForm` in `frontend/src/features/ideas/IdeaSubmitForm.tsx` (shadcn `Form`, `Input`, `Textarea`, `Select`, file input). Client-side validation: title 5–120, description 1–4000, category required, file ≤ 10 MB and extension in allow-list. On submit, calls create mutation, then `PUT /ideas/{id}/attachment` if a file is present (depends on T070, T073)
- [ ] T075 [US1] Build `IdeasListPage` in `frontend/src/features/ideas/IdeasListPage.tsx` rendering a shadcn `Table`/`Card` list with columns Title, Category, Submitter, Date (`formatIdeaDate` from T041), Status (`StatusBadge` from T044). Implement loading/empty/error/success states using primitives from T044 (depends on T073, T044, T041)
- [ ] T076 [US1] Build `IdeaDetailPage` (read-only portion for US1) in `frontend/src/features/ideas/IdeaDetailPage.tsx` showing title, description, category, submitter, dates (date-fns), current status, and attachment download link when present (depends on T073, T041, T044)
- [ ] T077 [US1] Add the `AppShell` Logout control wired to `useAuth().logout`; on logout, clear query cache and navigate to `/login` (depends on T043, T070)

**Checkpoint**: Run checklist M1. All steps pass against the running stack. The MVP can be demoed.

---

## Phase 4: User Story 2 — Admin reviews and decides on an idea (Priority: P2)

**Goal**: An Admin can move an idea to **Under Review** without a comment, and Accept or Reject it with a mandatory comment. Status, comment, and decision-maker become visible to the submitter; concurrent decisions are rejected.

**Independent Test**: Execute checklist [M2 in quickstart.md](./quickstart.md#checklist-m2--user-story-p2-admin-reviews-and-decides) using two browser profiles.

### Tests for User Story 2

- [ ] T078 [P] [US2] Contract test `DecisionsEndpointTests` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Decisions/DecisionsEndpointTests.cs`: as Admin — `MoveToUnderReview` succeeds with no comment; `Accept` requires non-empty comment (400 if missing); transition from Accepted is rejected with 409; concurrent Accepts on the same idea — second returns 409 (DbUpdateConcurrencyException → mapped). As Submitter — same endpoint returns 403 (FR-005, SC-005)
- [ ] T079 [P] [US2] Contract test `IdeaHistoryTests` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Ideas/IdeaHistoryTests.cs`: after a decision, `GET /api/v1/ideas/{id}/history` returns the chronological transitions including the decision row with comment and actor
- [ ] T080 [P] [US2] Vitest component test `DecisionDialog.test.tsx` in `frontend/tests/unit/admin/DecisionDialog.test.tsx`: comment-required validation for Accept/Reject; no comment required for `Move to Under Review`; calls correct mutation
- [ ] T081 [P] [US2] Playwright E2E `admin-decision.spec.ts` in `frontend/tests/e2e/admin-decision.spec.ts` covering M2.3, M2.4, M2.5, M2.6, M2.10

### Backend implementation

- [ ] T082 [P] [US2] Create `Decision` entity in `backend/src/InnovatEpam.Portal.Domain/Decisions/Decision.cs` per data-model §6; constructor enforces "comment required for Accept/Reject"
- [ ] T083 [US2] Add EF configuration for `Decision` in `backend/src/InnovatEpam.Portal.Infrastructure/Persistence/Configurations/DecisionConfiguration.cs`; wire `Idea.LastDecisionId` FK; ensure `Idea` `xmin` row version is honoured (depends on T058, T082)
- [ ] T084 [P] [US2] Define DTOs `CreateDecisionRequest`, `StatusHistoryEntry` in `backend/src/InnovatEpam.Portal.Application/Decisions/Dtos/`; FluentValidation validator enforces the comment rule
- [ ] T085 [US2] Implement `DecisionService.RecordAsync(ideaId, action, comment, adminId)` in `backend/src/InnovatEpam.Portal.Application/Decisions/DecisionService.cs`: loads `Idea`, calls `Idea.TransitionTo(...)` (T055), inserts `Decision` row, persists; relies on the trigger from T035 to update history + idea pointer; catches `DbUpdateConcurrencyException` and rethrows `ConflictException` (depends on T055, T082, T084)
- [ ] T086 [US2] Implement `POST /api/v1/ideas/{id}/decisions` in `IdeasController` (or a new `DecisionsController`) decorated with `[Authorize(Policy = "Admin")]`; returns updated `IdeaDetail` (depends on T085, T025)
- [ ] T087 [US2] Extend `IdeaDetail` projection (T063 `GetByIdAsync`) and the `GET /ideas/{id}/history` endpoint to surface `lastDecisionComment`, `lastDecisionBy`, `lastDecisionAt`, and the full chronological `IdeaStatusHistory` (FR-021)

### Frontend implementation

- [ ] T088 [P] [US2] Build `useDecisionMutation` in `frontend/src/features/admin/api.ts` using TanStack Query; on success invalidates `['ideas']` and `['idea', id]`
- [ ] T089 [US2] Build `DecisionDialog` in `frontend/src/features/admin/DecisionDialog.tsx` (shadcn `Dialog` + `Form`): three actions (`Move to Under Review`, `Accept`, `Reject`); `Accept`/`Reject` require non-empty comment; on success closes dialog and toasts "Decision recorded" (depends on T088, T044)
- [ ] T090 [US2] Add `<DecideControls />` to `IdeaDetailPage` visible only when `useAuth().user.role === 'Admin'` and idea status is not terminal; disable controls otherwise with tooltip "Idea has reached a terminal status" (FR-020) (depends on T076, T089)
- [ ] T091 [US2] Extend `IdeaDetailPage` to render `lastDecisionComment`, `lastDecisionBy.displayName`, `formatIdeaDateTime(lastDecisionAt)` and the full `StatusHistoryEntry` timeline (depends on T076, T087)
- [ ] T092 [US2] Show conflict (409) errors from the decision mutation as inline error using `ErrorState`/toast and refresh the idea (handles "two admins decide simultaneously" edge case)

**Checkpoint**: Run checklist M2. All steps pass.

---

## Phase 5: User Story 3 — Browse and filter the catalog (Priority: P3)

**Goal**: Any signed-in user can filter the listing by status and category, paginate results, and open a full detail view.

**Independent Test**: Execute checklist [M3 in quickstart.md](./quickstart.md#checklist-m3--user-story-p3-browse-and-filter-the-catalog).

### Tests for User Story 3

- [ ] T093 [P] [US3] Contract test `IdeasFilterAndPagingTests` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Ideas/IdeasFilterAndPagingTests.cs`: seed 25 ideas across all statuses + 3 categories; assert `?status=Accepted&categoryCode=process` filters correctly; `?page=2&pageSize=10` returns the right slice with accurate `total`
- [ ] T094 [P] [US3] Vitest component test `IdeasFilters.test.tsx` in `frontend/tests/unit/ideas/IdeasFilters.test.tsx`: changing filters updates the URL search params and triggers a refetch
- [ ] T095 [P] [US3] Playwright E2E `browse-filter.spec.ts` in `frontend/tests/e2e/browse-filter.spec.ts` covering M3.2, M3.3, M3.6, M3.7

### Implementation

- [ ] T096 [US3] Extend `IdeaService.ListAsync` to support `status`, `categoryCode`, `submitterId`, `sort` (default `-createdAt`), `page`, `pageSize` ≤ 100; ensure index `ix_idea_status_created_at` is used (data-model §4) (depends on T063)
- [ ] T097 [P] [US3] Build `IdeasFilters` component in `frontend/src/features/ideas/IdeasFilters.tsx` using shadcn `Select` for status and category; bind filter state to URL search params via `useSearchParams` so filters are deep-linkable (M3.2)
- [ ] T098 [US3] Add pagination controls (`PreviousPage`/`NextPage` + page size) to `IdeasListPage`; render only when `total > pageSize`; show total count summary (depends on T075, T097)
- [ ] T099 [US3] Wire `IdeasListPage` empty-state copy to filter context (e.g., "No Rejected ideas yet") and ensure error-state retry refetches the current filter (depends on T044, T075)
- [ ] T100 [US3] Add `useIdeaQuery(id)` and finalize `IdeaDetailPage` to render the full `IdeaDetail` (description, attachment download, full status history) for any signed-in user (depends on T076, T091)

**Checkpoint**: Run checklist M3. All three user stories independently testable.

---

## Phase 6: Notifications & Polling (supports US2 → US1 feedback loop)

**Purpose**: Implement FR-022 (in-portal notification on status change) discovered in research R14 and tested by M2.10.

- [ ] T101 [P] Create `Notification` entity in `backend/src/InnovatEpam.Portal.Domain/Notifications/Notification.cs` per data-model §8; EF configuration adds the partial unread index
- [ ] T102 In `DecisionService.RecordAsync` (T085), within the same `SaveChangesAsync` transaction, insert a `Notification` row for the idea's submitter with `kind = 'idea_status_changed'` and a payload containing `{ ideaId, ideaTitle, fromStatus, toStatus, decidedBy }`
- [ ] T103 [P] Implement `NotificationsController` in `backend/src/InnovatEpam.Portal.Api/Controllers/NotificationsController.cs` with `GET /api/v1/notifications`, `GET /api/v1/notifications/unread-count`, `POST /api/v1/notifications/{id}/read`
- [ ] T104 [P] Backend test `NotificationsTests` in `backend/tests/InnovatEpam.Portal.IntegrationTests/Notifications/NotificationsTests.cs`: a decision creates exactly one unread notification for the submitter; mark-read endpoint sets `read_at`; unread count drops accordingly
- [ ] T105 [P] Build `useUnreadCount` polling hook in `frontend/src/features/notifications/useUnreadCount.ts` using TanStack Query with `refetchInterval: 30_000` and `refetchOnWindowFocus: true`
- [ ] T106 [P] Build `NotificationsPanel` in `frontend/src/features/notifications/NotificationsPanel.tsx` (shadcn `Sheet` triggered by the bell icon in `AppShell`); list items use `formatRelative` for timestamps; clicking an item navigates to the idea and marks the notification read

---

## Phase 7: Polish & Cross-Cutting Concerns

- [ ] T107 [P] Add `Seed` static method to `InnovatEpam.Portal.Infrastructure` invoked at `Program.cs` startup that seeds the Admin user from `Seed:AdminEmail` / `Seed:AdminPassword` config when no Admin exists (R15, quickstart §1.3)
- [ ] T108 [P] Add health-check endpoint `GET /api/v1/health` in `Program.cs` (`AddHealthChecks().AddNpgSql(...)`) returning the `Health` schema; surfaces DB readiness for container probes
- [ ] T109 [P] Generate XML doc files (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`) for Api, Application, Domain projects so Swashbuckle can include them; address the `1591` warning for any intentionally undocumented internals via `1591` suppression files only (Constitution Principle I)
- [ ] T110 [P] Write `frontend/tests/unit/lib/date.test.ts` covering `formatIdeaDate`, `formatIdeaDateTime`, `formatRelative` (R10, X4.1–X4.3); write a custom ESLint rule or repo-level grep CI step that fails the build if `toLocaleDateString(` or `new Date().toString()` appear in `frontend/src/**/*.tsx` (X4.4)
- [ ] T111 [P] Add Axe-based accessibility check to Playwright suite `frontend/tests/e2e/a11y.spec.ts` running `@axe-core/playwright` against `/login`, `/ideas`, `/ideas/new`, `/ideas/:id` and asserting no Serious/Critical violations (X2.7)
- [ ] T112 [P] Configure Vite bundle analyzer (`rollup-plugin-visualizer`) and add a CI step that fails the build if total `date-fns` chunk size exceeds 20 KB gzipped (X4.5, Constitution Principle IV)
- [ ] T113 [P] Add request/response logging middleware that records method, path, status, duration, and `RequestId` via Serilog; verify with a unit test that PII fields (`password`, `accessToken`) are scrubbed (X3.4)
- [ ] T113a [P] Inject `ILogger<AuthService>` into `AuthService` (T062) and emit structured Serilog events (Constitution Cross-cutting + FR-025): `Information "auth.login.success" {{ UserId, Email }}` on successful login, `Warning "auth.login.failure" {{ Email, Reason }}` on failed login (no password material logged), `Information "auth.logout" {{ UserId }}` on logout, `Information "auth.register.success" {{ UserId, Email }}` on registration. Property names match the PII-scrub allow-list configured in T027
- [ ] T113b [P] Inject `ILogger<DecisionService>` into `DecisionService` (T085) and emit `Information "decision.recorded" {{ IdeaId, AdminId, Action, FromStatus, ToStatus }}` after each successful decision; emit `Warning "decision.conflict" {{ IdeaId, AdminId }}` when a `ConflictException` is thrown. Add a unit test in `backend/tests/InnovatEpam.Portal.UnitTests/Logging/SecurityEventTests.cs` using a `TestSink`/`InMemoryLogger` that asserts all five event names (`auth.login.success`, `auth.login.failure`, `auth.logout`, `auth.register.success`, `decision.recorded`) are emitted exactly once for the corresponding flows and that no property contains the literal password text
- [ ] T114 [P] Add `dotnet list package --vulnerable --include-transitive` and `npm audit --production` as required CI gates (T017) — fail the build on High/Critical (Constitution Dev Workflow gate #5)
- [ ] T115 [P] Update root `README.md` with badges, quickstart pointer to [quickstart.md](./quickstart.md), the constitution one-liner compliance template for PR descriptions, and contributor guidance to follow [.specify/memory/constitution.md](../../.specify/memory/constitution.md)
- [ ] T116 Run the full quickstart Part 2 checklists (M1, M2, M3, X1, X2, X3, X4) end-to-end against a freshly migrated database; record results in the release ticket per quickstart §"Sign-off"

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 Setup** → no dependencies; start immediately.
- **Phase 2 Foundational** → depends on Phase 1; **blocks all stories**.
- **Phase 3 (US1)** → depends on Phase 2.
- **Phase 4 (US2)** → depends on Phase 2 + the `Idea` aggregate from Phase 3 (`T055`, `T058`, `T076`).
- **Phase 5 (US3)** → depends on Phase 2 + the `IdeasListPage`/`IdeaService.ListAsync` from Phase 3 (`T063`, `T075`).
- **Phase 6 Notifications** → depends on Phase 4 (`T085`).
- **Phase 7 Polish** → depends on all desired user stories being complete.

### User Story Independence

- US1 is fully self-contained — completing only Phase 1 → 2 → 3 yields a demoable MVP.
- US2 layers on top of US1 (Admin needs ideas to decide on) but is testable without US3.
- US3 is a presentation-layer enhancement of US1 + US2 — neither blocks it.

### Within Each User Story

- Tests authored alongside or before the implementation tasks they cover (constitution does not mandate strict TDD; CI gates require tests to exist and pass).
- Domain/data tasks → application services → API controllers → frontend wiring.
- Same-file tasks are sequential; different-file tasks marked [P] can run in parallel.

### Parallel Opportunities

- All Phase 1 [P] tasks can be done concurrently after T001/T004/T009 land.
- All Phase 2 frontend foundation tasks (T038–T047) parallelise with backend foundation tasks (T019–T037).
- Inside US1: `T054`, `T055`, `T056`, `T057` (entities) all [P]; `T065`, `T066`, `T067`, `T068` (controllers) all [P]; `T070`, `T071`, `T072`, `T073` (FE auth + queries) all [P].
- Inside US2: `T078`, `T080`, `T081` test scaffolding parallel; `T082`, `T084`, `T088` parallel.
- Polish tasks T107–T115 are essentially all [P].

### Parallel Example: User Story 1

```text
After Foundational checkpoint, fan out:
  Dev A: T054 + T055 + T058 (domain/EF) → T063 (IdeaService) → T066 (IdeasController)
  Dev B: T056 + T057 + T064 (attachment service) → T067 (AttachmentsController)
  Dev C: T070 + T071 + T072 (auth FE) → T077 (logout)
  Dev D: T073 + T074 + T075 + T076 (ideas FE)
  Dev E: T048 + T049 + T050 + T051 + T052 + T053 (tests)
Converge on the M1 checklist run.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → repo scaffolds and CI green.
2. Phase 2 Foundational → DB migrated, auth pipeline, RFC 7807, OpenAPI, SPA shell.
3. Phase 3 US1 → submit + list + auth.
4. **STOP and run quickstart checklist M1**. If green, demo the MVP.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. + US1 → MVP shippable; checklist M1.
3. + US2 → evaluation workflow live; checklist M2.
4. + US3 → catalog browse polished; checklist M3.
5. + Notifications + Polish → release-ready; checklists X1–X4 + sign-off.

### Parallel Team Strategy

Once Phase 2 is complete, three streams can ship in parallel:
- Stream A: US1 → US3 (idea capture and catalog).
- Stream B: US2 → Notifications (admin workflow + feedback loop).
- Stream C: Polish track (a11y, bundle budget, security CI).

---

## Notes

- [P] = different files, no incomplete dependencies in the same task.
- Every task names exact file paths from [plan.md](./plan.md) "Project Structure".
- Tests live next to their stack (`backend/tests/...` and `frontend/tests/...`); the constitution requires both to be runnable in CI before merge.
- All endpoints created by these tasks are versioned under `/api/v1/` and emit `application/problem+json` errors per Constitution Principle II.
- Date display in the SPA goes exclusively through `frontend/src/lib/date.ts` (T041); a CI grep (T110) enforces this.
- After each task or logical group, commit using the constitution-aligned compliance line in the message (e.g., `[US1] add IdeaService — complies with Principles I, II`).
