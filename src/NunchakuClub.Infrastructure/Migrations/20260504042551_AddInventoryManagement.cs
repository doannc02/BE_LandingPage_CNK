using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inventory_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_categories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "inventory_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sku = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    image_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_items_inventory_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "inventory_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "branch_inventories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    branch_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: false, defaultValue: 5),
                    exported_this_month = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_branch_inventories", x => x.id);
                    table.ForeignKey(
                        name: "fk_branch_inventories_branches_branch_id",
                        column: x => x.branch_id,
                        principalTable: "branches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_branch_inventories_inventory_items_item_id",
                        column: x => x.item_id,
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_branch_inventories_branch_id",
                table: "branch_inventories",
                column: "branch_id");

            migrationBuilder.CreateIndex(
                name: "ix_branch_inventories_branch_id_item_id",
                table: "branch_inventories",
                columns: new[] { "branch_id", "item_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_branch_inventories_item_id",
                table: "branch_inventories",
                column: "item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_items_category_id",
                table: "inventory_items",
                column: "category_id");

            // Custom SQL: Create index for active students query performance
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS ix_student_profiles_branch_id_learning_status ON student_profiles (branch_id, learning_status);");

            // Custom SQL: Create view for branch stats
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "branch_inventories");

            migrationBuilder.DropTable(
                name: "inventory_items");

            migrationBuilder.DropTable(
                name: "inventory_categories");

            // Custom SQL: Drop view
            migrationBuilder.Sql("DROP VIEW IF EXISTS v_branch_stats;");

            // Custom SQL: Drop index
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_student_profiles_branch_id_learning_status;");
        }
    }
}
