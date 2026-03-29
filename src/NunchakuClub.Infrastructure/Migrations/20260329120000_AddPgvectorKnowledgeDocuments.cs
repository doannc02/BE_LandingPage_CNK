using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace NunchakuClub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPgvectorKnowledgeDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Enable pgvector extension (idempotent)
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            // 2. Create knowledge_documents table
            migrationBuilder.CreateTable(
                name: "knowledge_documents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    embedding = table.Column<Vector>(type: "vector(768)", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_knowledge_documents", x => x.id);
                });

            // 3. HNSW index for fast cosine similarity search
            migrationBuilder.Sql("""
                CREATE INDEX "IX_knowledge_documents_embedding"
                ON knowledge_documents
                USING hnsw (embedding vector_cosine_ops)
                WITH (m = 16, ef_construction = 64);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "knowledge_documents");
        }
    }
}
