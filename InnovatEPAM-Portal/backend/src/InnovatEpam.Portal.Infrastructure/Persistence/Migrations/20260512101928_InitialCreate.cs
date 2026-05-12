using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace InnovatEpam.Portal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Required PostgreSQL extensions (T034). pgcrypto is used for
            // gen_random_uuid(); citext for case-insensitive email columns.
            migrationBuilder.Sql("create extension if not exists pgcrypto;");
            migrationBuilder.Sql("create extension if not exists citext;");

            migrationBuilder.EnsureSchema(
                name: "portal");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "app_roles",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_roles", x => x.id);
                    table.CheckConstraint("ck_app_role_name", "name in ('Submitter','Admin')");
                });

            migrationBuilder.CreateTable(
                name: "categories",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox_messages",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox_messages", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "app_role_claims",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_role_claims_app_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "portal",
                        principalTable: "app_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_users",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    user_name = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "citext", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_users_app_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "portal",
                        principalTable: "app_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "app_user_claims",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_app_user_claims_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "portal",
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_logins",
                schema: "portal",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_app_user_logins_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "portal",
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_roles",
                schema: "portal",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_app_user_roles_app_roles_role_id",
                        column: x => x.role_id,
                        principalSchema: "portal",
                        principalTable: "app_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_app_user_roles_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "portal",
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_tokens",
                schema: "portal",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_app_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_app_user_tokens_app_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "portal",
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ideas",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    submitter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    attachment_id = table.Column<Guid>(type: "uuid", nullable: true),
                    last_decision_id = table.Column<Guid>(type: "uuid", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ideas", x => x.id);
                    table.CheckConstraint("ck_idea_description_length", "char_length(description) between 1 and 4000");
                    table.CheckConstraint("ck_idea_status", "status between 1 and 4");
                    table.CheckConstraint("ck_idea_title_length", "char_length(title) between 5 and 120");
                    table.ForeignKey(
                        name: "fk_ideas_app_users_submitter_id",
                        column: x => x.submitter_id,
                        principalSchema: "portal",
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_ideas_categories_category_id",
                        column: x => x.category_id,
                        principalSchema: "portal",
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "attachments",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    storage_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    sha256 = table.Column<byte[]>(type: "bytea", nullable: false),
                    uploaded_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attachments", x => x.id);
                    table.CheckConstraint("ck_attachment_content_type", "content_type in ('application/pdf','image/png','image/jpeg','application/vnd.openxmlformats-officedocument.wordprocessingml.document','application/vnd.openxmlformats-officedocument.presentationml.presentation','application/vnd.openxmlformats-officedocument.spreadsheetml.sheet')");
                    table.CheckConstraint("ck_attachment_size", "size_bytes > 0 and size_bytes <= 10485760");
                    table.ForeignKey(
                        name: "fk_attachments_ideas_idea_id",
                        column: x => x.idea_id,
                        principalSchema: "portal",
                        principalTable: "ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "decisions",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    decided_by_id = table.Column<Guid>(type: "uuid", nullable: false),
                    decided_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_decisions", x => x.id);
                    table.CheckConstraint("ck_decision_action", "action between 1 and 3");
                    table.CheckConstraint("ck_decision_comment_required_for_terminal", "(action = 1) or (action in (2,3) and comment is not null and char_length(comment) between 1 and 2000)");
                    table.ForeignKey(
                        name: "fk_decisions_ideas_idea_id",
                        column: x => x.idea_id,
                        principalSchema: "portal",
                        principalTable: "ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    recipient_id = table.Column<Guid>(type: "uuid", nullable: false),
                    idea_id = table.Column<Guid>(type: "uuid", nullable: true),
                    kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    read_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notifications", x => x.id);
                    table.ForeignKey(
                        name: "fk_notifications_ideas_idea_id",
                        column: x => x.idea_id,
                        principalSchema: "portal",
                        principalTable: "ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "idea_status_history",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_status = table.Column<int>(type: "integer", nullable: true),
                    to_status = table.Column<int>(type: "integer", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    decision_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idea_status_history", x => x.id);
                    table.CheckConstraint("ck_status_history_from_status", "from_status is null or from_status between 1 and 4");
                    table.CheckConstraint("ck_status_history_to_status", "to_status between 1 and 4");
                    table.CheckConstraint("ck_status_history_transition_valid", "(from_status is null and to_status = 1) or (from_status = 1 and to_status in (2,3,4)) or (from_status = 2 and to_status in (3,4))");
                    table.ForeignKey(
                        name: "fk_idea_status_history_decisions_decision_id",
                        column: x => x.decision_id,
                        principalSchema: "portal",
                        principalTable: "decisions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_idea_status_history_ideas_idea_id",
                        column: x => x.idea_id,
                        principalSchema: "portal",
                        principalTable: "ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_app_role_claims_role_id",
                schema: "portal",
                table: "app_role_claims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "portal",
                table: "app_roles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_user_claims_user_id",
                schema: "portal",
                table: "app_user_claims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_logins_user_id",
                schema: "portal",
                table: "app_user_logins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_roles_role_id",
                schema: "portal",
                table: "app_user_roles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_user_role_id",
                schema: "portal",
                table: "app_users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "portal",
                table: "app_users",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_app_user_normalized_email",
                schema: "portal",
                table: "app_users",
                column: "normalized_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_attachment_idea_id",
                schema: "portal",
                table: "attachments",
                column: "idea_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_attachment_storage_key",
                schema: "portal",
                table: "attachments",
                column: "storage_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_category_code",
                schema: "portal",
                table: "categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_category_name",
                schema: "portal",
                table: "categories",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_decision_decided_by_id",
                schema: "portal",
                table: "decisions",
                column: "decided_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_decision_idea_id_decided_at",
                schema: "portal",
                table: "decisions",
                columns: new[] { "idea_id", "decided_at" });

            migrationBuilder.CreateIndex(
                name: "ix_idea_status_history_decision_id",
                schema: "portal",
                table: "idea_status_history",
                column: "decision_id");

            migrationBuilder.CreateIndex(
                name: "ix_status_history_idea_id_occurred_at",
                schema: "portal",
                table: "idea_status_history",
                columns: new[] { "idea_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_idea_category_id",
                schema: "portal",
                table: "ideas",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "ix_idea_created_at",
                schema: "portal",
                table: "ideas",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_idea_status_created_at",
                schema: "portal",
                table: "ideas",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_idea_submitter_id",
                schema: "portal",
                table: "ideas",
                column: "submitter_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_idea_id",
                schema: "portal",
                table: "notifications",
                column: "idea_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_recipient_unread",
                schema: "portal",
                table: "notifications",
                column: "recipient_id",
                filter: "read_at is null");

            migrationBuilder.CreateIndex(
                name: "ix_outbox_unprocessed",
                schema: "portal",
                table: "outbox_messages",
                column: "occurred_at",
                filter: "processed_at is null");

            // ─── Seed app_roles (T034) ──────────────────────────────────────
            migrationBuilder.Sql(@"
insert into portal.app_roles (id, name, normalized_name, concurrency_stamp)
values
  ('11111111-1111-1111-1111-111111111111', 'Submitter', 'SUBMITTER', gen_random_uuid()::text),
  ('22222222-2222-2222-2222-222222222222', 'Admin',     'ADMIN',     gen_random_uuid()::text);
");

            // ─── Seed categories (T034) ─────────────────────────────────────
            migrationBuilder.Sql(@"
insert into portal.categories (id, code, name, is_active, sort_order) values
  ('aaaaaaaa-0000-0000-0000-000000000001', 'process',    'Process',    true, 10),
  ('aaaaaaaa-0000-0000-0000-000000000002', 'product',    'Product',    true, 20),
  ('aaaaaaaa-0000-0000-0000-000000000003', 'technology', 'Technology', true, 30),
  ('aaaaaaaa-0000-0000-0000-000000000004', 'people',     'People',     true, 40),
  ('aaaaaaaa-0000-0000-0000-000000000005', 'other',      'Other',      true, 99);
");

            // ─── Triggers (T035) ────────────────────────────────────────────
            // trg_decision_after_insert: after a Decision row lands, update the
            // parent Idea (status, last_decision_id, updated_at) and append an
            // immutable IdeaStatusHistory row in the same transaction.
            migrationBuilder.Sql(@"
create or replace function portal.fn_decision_after_insert() returns trigger as $$
declare
  v_from_status int;
  v_to_status   int;
begin
  v_to_status := case new.action
    when 1 then 2  -- MoveToUnderReview -> UnderReview
    when 2 then 3  -- Accept            -> Accepted
    when 3 then 4  -- Reject            -> Rejected
    else null
  end;
  if v_to_status is null then
    raise exception 'Unknown decision action: %', new.action;
  end if;

  select status into v_from_status from portal.ideas where id = new.idea_id for update;
  if v_from_status is null then
    raise exception 'Idea % not found', new.idea_id;
  end if;

  update portal.ideas
     set status = v_to_status,
         last_decision_id = new.id,
         updated_at = new.decided_at
   where id = new.idea_id;

  insert into portal.idea_status_history
    (id, idea_id, from_status, to_status, actor_id, comment, occurred_at, decision_id)
  values
    (gen_random_uuid(), new.idea_id, v_from_status, v_to_status, new.decided_by_id,
     new.comment, new.decided_at, new.id);

  return new;
end;
$$ language plpgsql;

create trigger trg_decision_after_insert
after insert on portal.decisions
for each row execute function portal.fn_decision_after_insert();
");

            // trg_idea_terminal_status_immutable: enforce FR-020 — once an idea
            // is Accepted (3) or Rejected (4), its status cannot change.
            migrationBuilder.Sql(@"
create or replace function portal.fn_idea_terminal_status_immutable() returns trigger as $$
begin
  if old.status in (3, 4) and new.status <> old.status then
    raise exception 'Idea % is in a terminal status (%); status cannot be changed.',
      old.id, old.status using errcode = '23514';
  end if;
  return new;
end;
$$ language plpgsql;

create trigger trg_idea_terminal_status_immutable
before update of status on portal.ideas
for each row execute function portal.fn_idea_terminal_status_immutable();
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop triggers + functions before tables (T035).
            migrationBuilder.Sql("drop trigger if exists trg_idea_terminal_status_immutable on portal.ideas;");
            migrationBuilder.Sql("drop function if exists portal.fn_idea_terminal_status_immutable();");
            migrationBuilder.Sql("drop trigger if exists trg_decision_after_insert on portal.decisions;");
            migrationBuilder.Sql("drop function if exists portal.fn_decision_after_insert();");
            migrationBuilder.DropTable(
                name: "app_role_claims",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "app_user_claims",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "app_user_logins",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "app_user_roles",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "app_user_tokens",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "attachments",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "idea_status_history",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "notifications",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "outbox_messages",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "decisions",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "ideas",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "app_users",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "categories",
                schema: "portal");

            migrationBuilder.DropTable(
                name: "app_roles",
                schema: "portal");
        }
    }
}
