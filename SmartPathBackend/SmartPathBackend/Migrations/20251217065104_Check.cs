using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace SmartPathBackend.Migrations
{
    /// <inheritdoc />
    public partial class Check : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "StudyMaterialSearchIndices",
                type: "vector(1024)",
                nullable: true);

            migrationBuilder.AddColumn<Vector>(
                name: "Embedding",
                table: "PostSearchIndices",
                type: "vector(1024)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "StudyMaterialSearchIndices");

            migrationBuilder.DropColumn(
                name: "Embedding",
                table: "PostSearchIndices");
        }
    }
}
