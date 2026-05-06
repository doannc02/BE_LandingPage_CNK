using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSoftDeleteToStudentProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "student_profiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "student_profiles");
        }
    }
}
