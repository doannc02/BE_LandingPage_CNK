using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateBranchStatsViewForArrayAgg : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_branch_stats;");
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW v_branch_stats AS
                SELECT 
                    b.id,
                    b.code,
                    b.name,
                    b.address,
                    b.thumbnail,
                    b.is_active,
                    (SELECT COUNT(*) FROM student_profiles sp WHERE sp.branch_id = b.id AND sp.learning_status = 'Active') as active_student_count,
                    COALESCE((SELECT array_agg(coach_id) FROM branch_coach bc WHERE bc.branch_id = b.id AND bc.title = 'HeadCoach'), '{}') as head_coach_ids,
                    COALESCE((SELECT array_agg(coach_id) FROM branch_coach bc WHERE bc.branch_id = b.id AND bc.title = 'AssistantCoach'), '{}') as assistant_coach_ids
                FROM branches b;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_branch_stats;");
            migrationBuilder.Sql(@"
                CREATE OR REPLACE VIEW v_branch_stats AS
                SELECT 
                    b.id,
                    b.code,
                    b.name,
                    b.address,
                    b.thumbnail,
                    b.is_active,
                    (SELECT COUNT(*) FROM student_profiles sp WHERE sp.branch_id = b.id AND sp.learning_status = 'Active') as active_student_count,
                    (SELECT COUNT(*) FROM branch_coach bc WHERE bc.branch_id = b.id AND bc.title = 'HeadCoach') as head_coach_count,
                    (SELECT COUNT(*) FROM branch_coach bc WHERE bc.branch_id = b.id AND bc.title = 'AssistantCoach') as assistant_coach_count
                FROM branches b;
            ");
        }
    }
}
