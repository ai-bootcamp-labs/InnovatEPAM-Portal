# Phase 1 — Data Model: InnovatEPAM Portal MVP

**Feature**: Phase 1 MVP — InnovatEPAM Portal
**Date**: 2026-05-12
**ORM**: Entity Framework Core 8 with `Npgsql.EntityFrameworkCore.PostgreSQL`
**Naming**: snake_case (via `EFCore.NamingConventions.UseSnakeCaseNamingConvention()`)
**Schema**: `portal`
**Identity tables**: `aspnet_*` (provided by `IdentityDbContext<AppUser, AppRole, Guid>`) live in the same schema for one-shot backups.

This document is the source of truth for the EF Core entities, their
relationships, validation rules, and PostgreSQL-level constraints. The
`/contracts/openapi.yaml` file is derived from these entities.

---

## Conventions

- All primary keys are `uuid` (`Guid`), generated server-side via
  `gen_random_uuid()` (the `pgcrypto` extension, enabled by the first migration).
- All timestamp columns are `timestamp with time zone` (`timestamptz`),
  written in UTC. EF property type is `DateTimeOffset`.
- All textual columns use `text` with explicit length checks via `MaxLength`
  attributes; lengths are enforced both by EF (`HasMaxLength`) and by
  PostgreSQL `CHECK` constraints (defence in depth).
- `created_at` and `updated_at` columns are populated via a `SaveChanges`
  interceptor; they are never set by the API caller.
- All foreign keys use `ON DELETE RESTRICT` unless otherwise noted —
  attachments and history rows must be deliberately handled, not orphaned.
- Soft-delete is **not** used in Phase 1; rows are immutable by convention
  except for `Idea.Status`, `Idea.UpdatedAt`, and `User` profile fields.

## Enumerations

```csharp
public enum IdeaStatus
{
    Submitted   = 1,
    UnderReview = 2,
    Accepted    = 3,
    Rejected    = 4
}

public enum DecisionAction
{
    MoveToUnderReview = 1,
    Accept            = 2,
    Reject            = 3
}
```

Stored as `int` columns (`smallint` in PG) with a `CHECK` constraint enumerating
valid values, so a database-level audit can reject bad data without trusting
the application.

State transitions enforced by the **domain layer** (`Idea.TransitionTo(...)`)
and re-asserted by a `CHECK` constraint:

| From            | Allowed targets                  |
|-----------------|----------------------------------|
| `Submitted`     | `UnderReview`, `Accepted`, `Rejected` |
| `UnderReview`   | `Accepted`, `Rejected`           |
| `Accepted`      | *(terminal)*                     |
| `Rejected`      | *(terminal)*                     |

---

## Entities

### 1. `AppUser` (extends `IdentityUser<Guid>`)

Represents a person with portal access (FR-001…008).

| Property        | Type             | Constraints                                            |
|-----------------|------------------|--------------------------------------------------------|
| `Id`            | `Guid` (PK)      | `gen_random_uuid()`                                    |
| `Email`         | `text`           | required, unique (case-insensitive via `citext`), ≤ 256|
| `NormalizedEmail` | `text`         | unique index (Identity-managed)                        |
| `UserName`      | `text`           | mirrors Email in Phase 1                               |
| `PasswordHash`  | `text`           | required (PBKDF2 from Identity)                        |
| `DisplayName`   | `text`           | required, 2 ≤ len ≤ 120                                |
| `RoleId`        | `Guid` (FK)      | required, → `AppRole.Id` (ON DELETE RESTRICT)          |
| `IsActive`      | `boolean`        | default `true`                                         |
| `CreatedAt`     | `timestamptz`    | required, set on insert                                |

Indexes:
- `ux_app_user_email_ci` UNIQUE on `lower(email)` *(in addition to Identity's index)*
- `ix_app_user_role_id` on `role_id`

Notes:
- Email column uses the `citext` extension to make uniqueness case-insensitive
  natively — supports FR-008 without runtime lower-casing.
- `RoleId` is a hard FK (single-role-per-user, FR-004); we do **not** use
  `aspnet_user_roles` join semantics in Phase 1.

### 2. `AppRole` (extends `IdentityRole<Guid>`)

| Property      | Type           | Constraints                                |
|---------------|----------------|--------------------------------------------|
| `Id`          | `Guid` (PK)    |                                            |
| `Name`        | `text`         | required, unique, ∈ {`"Submitter"`, `"Admin"`} (CHECK) |
| `NormalizedName` | `text`      | unique (Identity-managed)                  |

Seeded by migration with the two fixed roles and their stable GUIDs.

### 3. `Category`

Closed list (FR-009 / spec assumption).

| Property     | Type           | Constraints                       |
|--------------|----------------|-----------------------------------|
| `Id`         | `Guid` (PK)    |                                   |
| `Code`       | `text`         | required, unique, snake_case, ≤ 32 |
| `Name`       | `text`         | required, unique, ≤ 64            |
| `IsActive`   | `boolean`      | default `true`                    |
| `SortOrder`  | `int`          | default 0                         |

Seeded with: `process`, `product`, `technology`, `people`, `other`.

### 4. `Idea`

The submitted innovation proposal (FR-009…014, FR-016…021).

| Property         | Type            | Constraints                                                |
|------------------|-----------------|------------------------------------------------------------|
| `Id`             | `Guid` (PK)     |                                                            |
| `Title`          | `text`          | required, 5 ≤ len ≤ 120 (CHECK)                            |
| `Description`    | `text`          | required, 1 ≤ len ≤ 4 000 (CHECK)                          |
| `CategoryId`     | `Guid` (FK)     | required, → `Category.Id` (ON DELETE RESTRICT)             |
| `Status`         | `int`           | required, ∈ enum (CHECK), default `1` (Submitted)          |
| `SubmitterId`    | `Guid` (FK)     | required, → `AppUser.Id` (ON DELETE RESTRICT)              |
| `AttachmentId`   | `Guid?` (FK)    | optional, → `Attachment.Id` (ON DELETE SET NULL)           |
| `LastDecisionId` | `Guid?` (FK)    | optional, → `Decision.Id` (denormalised pointer; ON DELETE SET NULL) |
| `RowVersion`     | `uint` (`xmin`) | concurrency token (PostgreSQL system column)               |
| `CreatedAt`      | `timestamptz`   | required, set on insert                                    |
| `UpdatedAt`      | `timestamptz`   | required, updated on every change                          |

Constraints:
- `ck_idea_attachment_at_most_one`: enforced by FK uniqueness — see Attachment.
- `ck_idea_terminal_status_immutable`: trigger on UPDATE that raises if
  `OLD.status IN (3,4) AND NEW.status <> OLD.status` (FR-020).

Indexes:
- `ix_idea_status_created_at` on (`status`, `created_at desc`) — primary listing query.
- `ix_idea_category_id` on `category_id` — filter (FR-014).
- `ix_idea_submitter_id` on `submitter_id` — "my ideas" view.
- `ix_idea_created_at` on `created_at desc` — fallback sort.
- Optional GIN trigram index on `title` (Phase 2 search) — **not** added in Phase 1.

Concurrency:
- `RowVersion` mapped to PostgreSQL's `xmin` system column via
  `Property(p => p.RowVersion).IsRowVersion()`. Prevents the "two admins
  decide simultaneously" race (spec edge case) at the database level — the
  second `SaveChanges` throws `DbUpdateConcurrencyException` which the API
  translates into HTTP 409.

### 5. `Attachment`

Metadata only — bytes live on disk via `IAttachmentStorage` (R3).

| Property          | Type            | Constraints                                              |
|-------------------|-----------------|----------------------------------------------------------|
| `Id`              | `Guid` (PK)     |                                                          |
| `IdeaId`          | `Guid` (FK)     | required, → `Idea.Id`, **UNIQUE** (FR-010 ⇒ ≤ 1 per idea)|
| `OriginalFileName`| `text`          | required, ≤ 255                                          |
| `ContentType`     | `text`          | required, ≤ 100, ∈ allow-list (CHECK)                    |
| `SizeBytes`       | `bigint`        | required, > 0 AND ≤ 10 485 760 (CHECK)                   |
| `StorageKey`      | `text`          | required, unique, ≤ 512 (path/key returned by storage)   |
| `Sha256`          | `bytea`         | required, length = 32                                    |
| `UploadedById`    | `Guid` (FK)     | required, → `AppUser.Id`                                 |
| `UploadedAt`      | `timestamptz`   | required                                                 |

Indexes:
- `ux_attachment_idea_id` UNIQUE on `idea_id` — implements the
  "at most one attachment per idea" rule structurally.
- `ux_attachment_storage_key` UNIQUE on `storage_key`.

CHECK on `content_type`:
```
content_type IN (
  'application/pdf',
  'image/png',
  'image/jpeg',
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'application/vnd.openxmlformats-officedocument.presentationml.presentation',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
)
```

### 6. `Decision`

A point-in-time admin action (FR-018, FR-019, FR-021).

| Property      | Type           | Constraints                                                |
|---------------|----------------|------------------------------------------------------------|
| `Id`          | `Guid` (PK)    |                                                            |
| `IdeaId`      | `Guid` (FK)    | required, → `Idea.Id` (ON DELETE RESTRICT)                 |
| `Action`      | `int`          | required, ∈ enum (CHECK)                                   |
| `Comment`     | `text`         | nullable, 1 ≤ len ≤ 2 000 (CHECK when not null)            |
| `DecidedById` | `Guid` (FK)    | required, → `AppUser.Id`                                   |
| `DecidedAt`   | `timestamptz`  | required                                                   |

Constraints:
- `ck_decision_comment_required_for_terminal`:
  `(action = 1) OR (action IN (2,3) AND comment IS NOT NULL AND char_length(comment) BETWEEN 1 AND 2000)` (FR-019).
- A trigger `trg_decision_after_insert` updates the parent `Idea.Status`,
  `Idea.LastDecisionId`, and `Idea.UpdatedAt` in the same transaction —
  guarantees the audit trail and the idea status can never diverge.

Indexes:
- `ix_decision_idea_id_decided_at` on (`idea_id`, `decided_at desc`).
- `ix_decision_decided_by_id` on `decided_by_id`.

### 7. `IdeaStatusHistory`

Append-only audit (FR-021).

| Property     | Type           | Constraints                                                |
|--------------|----------------|------------------------------------------------------------|
| `Id`         | `Guid` (PK)    |                                                            |
| `IdeaId`     | `Guid` (FK)    | required, → `Idea.Id` (ON DELETE RESTRICT)                 |
| `FromStatus` | `int?`         | nullable (null on initial creation), ∈ enum                |
| `ToStatus`   | `int`          | required, ∈ enum                                           |
| `ActorId`    | `Guid` (FK)    | required, → `AppUser.Id`                                   |
| `Comment`    | `text?`        | optional, ≤ 2 000                                          |
| `OccurredAt` | `timestamptz`  | required                                                   |
| `DecisionId` | `Guid?` (FK)   | optional, → `Decision.Id` — links audit row to its decision|

Constraints:
- `ck_status_history_transition_valid`: `(from_status IS NULL AND to_status = 1) OR ...` enumerating all legal transitions from the table above.
- Rows are inserted by:
  - The `Idea` aggregate creator (initial row, `from = NULL`, `to = 1`).
  - The `trg_decision_after_insert` trigger (transition rows).

Indexes:
- `ix_status_history_idea_id_occurred_at` on (`idea_id`, `occurred_at`).

### 8. `Notification`

Per-user in-portal notification (FR-022, R14).

| Property     | Type           | Constraints                                            |
|--------------|----------------|--------------------------------------------------------|
| `Id`         | `Guid` (PK)    |                                                        |
| `RecipientId`| `Guid` (FK)    | required, → `AppUser.Id`                               |
| `IdeaId`     | `Guid?` (FK)   | optional, → `Idea.Id` (ON DELETE CASCADE)              |
| `Kind`       | `text`         | required, ≤ 64, e.g. `idea_status_changed`             |
| `Payload`    | `jsonb`        | required, schema-less but small (< 4 KB)               |
| `ReadAt`     | `timestamptz?` | nullable                                               |
| `CreatedAt`  | `timestamptz`  | required                                               |

Indexes:
- `ix_notification_recipient_unread` on (`recipient_id`) `WHERE read_at IS NULL` (partial index — keeps the unread-count poll trivially cheap).
- `ix_notification_idea_id` on `idea_id`.

### 9. `OutboxMessage` *(infrastructure helper)*

Reserved for Phase 2 email/SignalR fan-out. **Created by migration but not
read by Phase 1 code** to avoid future schema churn.

| Property      | Type           | Notes                          |
|---------------|----------------|--------------------------------|
| `Id`          | `Guid` (PK)    |                                |
| `OccurredAt`  | `timestamptz`  | required                       |
| `Type`        | `text`         | required, ≤ 256                |
| `Payload`     | `jsonb`        | required                       |
| `ProcessedAt` | `timestamptz?` | null until shipped             |

Index: `ix_outbox_unprocessed` on (`occurred_at`) `WHERE processed_at IS NULL`.

---

## Entity-Relationship Summary

```
AppRole 1 ── * AppUser
AppUser 1 ── * Idea           (Idea.SubmitterId)
AppUser 1 ── * Decision       (Decision.DecidedById)
AppUser 1 ── * Attachment     (Attachment.UploadedById)
AppUser 1 ── * Notification   (Notification.RecipientId)
AppUser 1 ── * IdeaStatusHistory (Actor)

Category 1 ── * Idea          (Idea.CategoryId)

Idea     1 ── 0..1 Attachment (UNIQUE Attachment.IdeaId)
Idea     1 ── *  Decision     (Decision.IdeaId)
Idea     1 ── *  IdeaStatusHistory (IdeaStatusHistory.IdeaId)
Idea     1 ── *  Notification (Notification.IdeaId, ON DELETE CASCADE)

Decision 1 ── 0..1 IdeaStatusHistory (IdeaStatusHistory.DecisionId)
Decision 0..1 ── 1 Idea       (Idea.LastDecisionId; denormalised)
```

## Validation rule sources (mapped to spec)

| Rule                                                    | Spec ref       | Enforced by                                      |
|---------------------------------------------------------|----------------|--------------------------------------------------|
| Email uniqueness, hashing, password strength            | FR-006/7/8     | Identity options + `IdentityOptions.Password` + `citext` |
| Title length 5–120, Description length 1–4000           | FR-009         | EF + CHECK + FluentValidation DTO                |
| Category required, from closed list                     | FR-009         | FK + seed                                        |
| ≤ 1 attachment per idea                                 | FR-010         | UNIQUE on `attachment.idea_id`                   |
| Allowed file types & size ≤ 10 MB                       | FR-010, FR-015 | CHECK on `content_type`, CHECK on `size_bytes`, magic-byte sniff (R4) |
| Initial status = Submitted                              | FR-017         | Default value + initial history row              |
| Allowed transitions                                     | FR-018         | Domain method + CHECK on history transitions     |
| Comment required for Accept/Reject                      | FR-019         | `ck_decision_comment_required_for_terminal`      |
| Terminal statuses are immutable                         | FR-020         | `trg_decision_after_insert` + immutability trigger |
| Audit trail completeness                                | FR-021         | `IdeaStatusHistory` + insertion trigger          |
| Notification on status change                           | FR-022         | Application service writes Notification row in same `SaveChanges` |
| Concurrent decision rejection                           | Edge case      | `xmin` row version on `Idea`                     |

## Migrations

The first migration `20260512_Initial.cs` MUST:

1. `CREATE EXTENSION IF NOT EXISTS pgcrypto;`
2. `CREATE EXTENSION IF NOT EXISTS citext;`
3. Create schema `portal` and all tables/indexes/CHECKs above.
4. Create the two triggers (`trg_decision_after_insert`,
   `trg_idea_terminal_status_immutable`) in raw SQL (`migrationBuilder.Sql`).
5. Seed `app_role` and `category` rows with stable GUIDs.

All migrations MUST include matching `Down()` methods (constitution Dev
Workflow gate #6 — reversibility tested in Testcontainers).
