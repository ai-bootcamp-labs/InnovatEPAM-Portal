using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnovatEpam.Portal.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBlindReviewAndScoring : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "was_blind",
                schema: "portal",
                table: "decisions",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "idea_scores",
                schema: "portal",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    idea_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reviewer_id = table.Column<Guid>(type: "uuid", nullable: false),
                    impact = table.Column<int>(type: "integer", nullable: false),
                    feasibility = table.Column<int>(type: "integer", nullable: false),
                    innovation = table.Column<int>(type: "integer", nullable: false),
                    alignment = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_idea_scores", x => x.id);
                    table.CheckConstraint("ck_idea_score_alignment", "alignment between 1 and 5");
                    table.CheckConstraint("ck_idea_score_feasibility", "feasibility between 1 and 5");
                    table.CheckConstraint("ck_idea_score_impact", "impact between 1 and 5");
                    table.CheckConstraint("ck_idea_score_innovation", "innovation between 1 and 5");
                    table.ForeignKey(
                        name: "fk_idea_scores_app_users_reviewer_id",
                        column: x => x.reviewer_id,
                        principalSchema: "portal",
                        principalTable: "app_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_idea_scores_ideas_idea_id",
                        column: x => x.idea_id,
                        principalSchema: "portal",
                        principalTable: "ideas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idea_score_idea_id",
                schema: "portal",
                table: "idea_scores",
                column: "idea_id");

            migrationBuilder.CreateIndex(
                name: "ix_idea_scores_reviewer_id",
                schema: "portal",
                table: "idea_scores",
                column: "reviewer_id");

            migrationBuilder.CreateIndex(
                name: "ux_idea_score_idea_reviewer",
                schema: "portal",
                table: "idea_scores",
                columns: new[] { "idea_id", "reviewer_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idea_scores",
                schema: "portal");

            migrationBuilder.DropColumn(
                name: "was_blind",
                schema: "portal",
                table: "decisions");
        }
    }
}
