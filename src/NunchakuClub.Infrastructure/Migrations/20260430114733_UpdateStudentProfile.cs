using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStudentProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Safely convert the "title" column from string (text) to integer using a USING cast.
            migrationBuilder.Sql(@"
                ALTER TABLE coaches
                ALTER COLUMN title TYPE integer USING (title::integer);
                UPDATE coaches SET title = 0 WHERE title IS NULL;
                ALTER TABLE coaches ALTER COLUMN title SET NOT NULL;
                ALTER TABLE coaches ALTER COLUMN title SET DEFAULT 0;
            ");

            // The following tables were already created in the earlier migration (AddStudentManagementAndAttendance).
            // They are omitted here to avoid duplicate creation errors.
            // migrationBuilder.CreateTable for belt_ranks, branches, and attendance_sessions has been removed.


            migrationBuilder.CreateTable(
                name: "branch_coach",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    coach_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_coach", x => x.id);
                    table.ForeignKey(
                        name: "fk_branch_coach_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_branch_coach_coaches_coach_id",
                        column: x => x.coach_id,
                        principalTable: "coaches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "branch_gallery",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    media_url = table.Column<string>(type: "text", nullable: false),
                    media_type = table.Column<int>(type: "integer", nullable: false),
                    caption = table.Column<string>(type: "text", nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_gallery", x => x.id);
                    table.ForeignKey(
                        name: "fk_branch_gallery_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Add new columns to existing student_profiles table
            migrationBuilder.AddColumn<string>(
                name: "address",
                table: "student_profiles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "height_cm",
                table: "student_profiles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "weight_kg",
                table: "student_profiles",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "gender",
                table: "student_profiles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // attendance_records already exists, no changes needed.


            migrationBuilder.CreateTable(
                name: "belt_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_belt_rank_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_belt_rank_id = table.Column<Guid>(type: "uuid", nullable: false),
                    promotion_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    instructor_note = table.Column<string>(type: "text", nullable: true),
                    media_url = table.Column<string>(type: "text", nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_belt_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_belt_history_belt_ranks_from_belt_rank_id",
                        column: x => x.from_belt_rank_id,
                        principalTable: "belt_ranks",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_belt_history_belt_ranks_to_belt_rank_id",
                        column: x => x.to_belt_rank_id,
                        principalTable: "belt_ranks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_belt_history_student_profiles_student_profile_id",
                        column: x => x.student_profile_id,
                        principalTable: "student_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Redundant indexes already created in previous migration.


            migrationBuilder.CreateIndex(
                name: "ix_belt_history_from_belt_rank_id",
                table: "belt_history",
                column: "from_belt_rank_id");

            migrationBuilder.CreateIndex(
                name: "ix_belt_history_student_profile_id",
                table: "belt_history",
                column: "student_profile_id");

            migrationBuilder.CreateIndex(
                name: "ix_belt_history_to_belt_rank_id",
                table: "belt_history",
                column: "to_belt_rank_id");

            // Redundant indexes already created in previous migration.


            migrationBuilder.CreateIndex(
                name: "ix_branch_coach_branch_id",
                table: "branch_coach",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_branch_coach_coach_id",
                table: "branch_coach",
                column: "coach_id");

            migrationBuilder.CreateIndex(
                name: "ix_branch_gallery_branch_id",
                table: "branch_gallery",
                column: "branch_id");

            // Redundant index already created in previous migration.


            // Redundant indexes already created in previous migration.

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropTable(
                name: "belt_history");

            migrationBuilder.DropTable(
                name: "branch_coach");

            migrationBuilder.DropTable(
                name: "branch_gallery");

            migrationBuilder.DropTable(
                name: "attendance_sessions");

            migrationBuilder.DropTable(
                name: "student_profiles");

            migrationBuilder.DropTable(
                name: "belt_ranks");

            migrationBuilder.DropTable(
                name: "branches");

            migrationBuilder.AlterColumn<string>(
                name: "title",
                table: "coaches",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");
        }
    }
}
