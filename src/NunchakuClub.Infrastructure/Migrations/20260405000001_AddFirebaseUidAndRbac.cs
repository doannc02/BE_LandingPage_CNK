using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFirebaseUidAndRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Thêm cột firebase_uid
            migrationBuilder.AddColumn<string>(
                name: "firebase_uid",
                table: "users",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            // 2. Unique index cho firebase_uid (partial — chỉ khi không null)
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_users_firebase_uid ON users (firebase_uid) WHERE firebase_uid IS NOT NULL;");

            // 3. Migrate role values: Admin→SuperAdmin, Editor→SubAdmin, Coach/Member→Student
            migrationBuilder.Sql("UPDATE users SET role = 'SuperAdmin' WHERE role = 'Admin';");
            migrationBuilder.Sql("UPDATE users SET role = 'SubAdmin'   WHERE role IN ('Editor', 'Coach');");
            migrationBuilder.Sql("UPDATE users SET role = 'Student'    WHERE role = 'Member';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback role values
            migrationBuilder.Sql("UPDATE users SET role = 'Admin'  WHERE role = 'SuperAdmin';");
            migrationBuilder.Sql("UPDATE users SET role = 'Editor' WHERE role = 'SubAdmin';");
            migrationBuilder.Sql("UPDATE users SET role = 'Member' WHERE role = 'Student';");

            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_users_firebase_uid;");

            migrationBuilder.DropColumn(
                name: "firebase_uid",
                table: "users");
        }
    }
}
