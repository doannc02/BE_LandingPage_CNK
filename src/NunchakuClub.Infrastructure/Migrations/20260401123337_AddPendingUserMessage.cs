using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingUserMessage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "pending_user_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    user_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    admin_reply = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    assigned_admin_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    replied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    notification_retry_count = table.Column<int>(type: "integer", nullable: false),
                    next_notification_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pending_user_messages", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_pending_user_messages_next_notification_at",
                table: "pending_user_messages",
                column: "next_notification_at");

            migrationBuilder.CreateIndex(
                name: "ix_pending_user_messages_session_id",
                table: "pending_user_messages",
                column: "session_id");

            migrationBuilder.CreateIndex(
                name: "ix_pending_user_messages_status",
                table: "pending_user_messages",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pending_user_messages");
        }
    }
}
