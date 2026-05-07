using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentManagementAndAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "belt_ranks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    color_hex = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    display_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_belt_ranks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    short_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    area = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    latitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(10,7)", nullable: true),
                    schedule = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    fee = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    is_free = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "attendance_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    session_label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_sessions_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_attendance_sessions_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "student_profiles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_belt_rank_id = table.Column<Guid>(type: "uuid", nullable: true),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    join_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    learning_status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    class_role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    guardian_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    guardian_phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_student_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_student_profiles_belt_ranks_current_belt_rank_id",
                        column: x => x.current_belt_rank_id,
                        principalTable: "belt_ranks",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_student_profiles_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_student_profiles_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "attendance_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    attendance_session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    student_profile_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    note = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_attendance_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_attendance_records_attendance_sessions_attendance_session_id",
                        column: x => x.attendance_session_id,
                        principalTable: "attendance_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_attendance_records_student_profiles_student_profile_id",
                        column: x => x.student_profile_id,
                        principalTable: "student_profiles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_attendance_session_id_student_profile_id",
                table: "attendance_records",
                columns: new[] { "attendance_session_id", "student_profile_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_attendance_records_student_profile_id_status",
                table: "attendance_records",
                columns: new[] { "student_profile_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sessions_branch_id_session_date",
                table: "attendance_sessions",
                columns: new[] { "branch_id", "session_date" });

            migrationBuilder.CreateIndex(
                name: "ix_attendance_sessions_recorded_by_user_id",
                table: "attendance_sessions",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_belt_ranks_code",
                table: "belt_ranks",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_belt_ranks_display_order",
                table: "belt_ranks",
                column: "display_order");

            migrationBuilder.CreateIndex(
                name: "ix_branches_code",
                table: "branches",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_profiles_branch_id_learning_status",
                table: "student_profiles",
                columns: new[] { "branch_id", "learning_status" });

            migrationBuilder.CreateIndex(
                name: "ix_student_profiles_current_belt_rank_id",
                table: "student_profiles",
                column: "current_belt_rank_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_profiles_student_code",
                table: "student_profiles",
                column: "student_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_student_profiles_user_id",
                table: "student_profiles",
                column: "user_id",
                unique: true);

            // migrationBuilder.InsertData(
            //     table: "belt_ranks",
            //     columns: new[] { "id", "code", "name", "color_hex", "display_order", "is_active", "created_at", "updated_at" },
            //     values: new object[,]
            //     {
            //         { new Guid("11111111-1111-1111-1111-111111111111"), "white", "Trắng", "#F5F5F5", 1, true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("22222222-2222-2222-2222-222222222222"), "yellow", "Vàng", "#EEBB00", 2, true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("33333333-3333-3333-3333-333333333333"), "light-blue", "Xanh lam", "#1A8A5A", 3, true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("44444444-4444-4444-4444-444444444444"), "blue", "Xanh dương", "#2244AA", 4, true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("55555555-5555-5555-5555-555555555555"), "red", "Đỏ", "#E85D24", 5, true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("66666666-6666-6666-6666-666666666666"), "brown", "Nâu", "#8B5E3C", 6, true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("77777777-7777-7777-7777-777777777777"), "black", "Đen", "#111111", 7, true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) }
            //     });

            // migrationBuilder.InsertData(
            //     table: "branches",
            //     columns: new[] { "id", "code", "name", "short_name", "address", "area", "latitude", "longitude", "schedule", "fee", "is_free", "description", "is_active", "created_at", "updated_at" },
            //     values: new object[,]
            //     {
            //         { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "van-yen", "Cơ sở 1: Trường TH Văn Yên - Hà Đông", "Cơ sở Hà Đông (Văn Yên)", "Trường Tiểu học Văn Yên, Hà Đông, Hà Nội", "Hà Đông", 20.9719786m, 105.7844575m, "Thứ 2-4-6 | 18:30-20:30", "MIỄN PHÍ", true, "Cơ sở chính, miễn phí hoàn toàn cho mọi lứa tuổi", true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "kien-hung", "Cơ sở 2: Vườn hoa Hằng Bè - Kiến Hưng", "Cơ sở Kiến Hưng", "Vườn hoa Hàng Bè, Kiến Hưng, Hà Đông, Hà Nội", "Hà Đông", 20.9492939m, 105.7893257m, "Thứ 3-5-7 | 17:45-19:00", "MIỄN PHÍ", true, "Cơ sở 2 tại Hà Đông, miễn phí hoàn toàn", true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "thong-nhat", "Cơ sở 3: Công viên Thống Nhất - Hai Bà Trưng", "Cơ sở Thống Nhất", "Công viên Thống Nhất, Hai Bà Trưng, Hà Nội", "Hai Bà Trưng", 21.0144927m, 105.8439907m, "Thứ 3-5-7 | 19:00-21:00", "300.000đ/tháng", false, "Công viên Thống Nhất, quận Hai Bà Trưng", true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), "hoa-binh", "Cơ sở 4: Công viên Hòa Bình - Bắc Từ Liêm", "Cơ sở Hòa Bình", "Công viên Hòa Bình, Bắc Từ Liêm, Hà Nội", "Bắc Từ Liêm", 21.0642800m, 105.7877731m, "Thứ 3-5-7 | 19:00-21:00", "300.000đ/tháng", false, "Công viên Hòa Bình, quận Bắc Từ Liêm", true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) },
            //         { new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), "kim-giang", "Cơ sở 5: Kim Giang - Thanh Xuân", "Cơ sở Kim Giang", "Sân chơi cạnh TH Ngôi sao Hoàng Mai cổng số 4, Kim Giang, Hà Nội", "Thanh Xuân", 20.9747256m, 105.8223827m, "Thứ 3-5-7 | 19:00-21:00", "300.000đ/tháng", false, "Khu vực Kim Giang, quận Hoàng Mai", true, new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 4, 27, 0, 0, 0, DateTimeKind.Utc) }
            //     });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "attendance_records");

            migrationBuilder.DropTable(
                name: "attendance_sessions");

            migrationBuilder.DropTable(
                name: "student_profiles");

            migrationBuilder.DropTable(
                name: "belt_ranks");

            migrationBuilder.DropTable(
                name: "branches");
        }
    }
}
