using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPathBackend.Migrations
{
    /// <inheritdoc />
    public partial class SearchIndices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudyMaterialSearchIndices_Description",
                table: "StudyMaterialSearchIndices");

            migrationBuilder.DropIndex(
                name: "IX_StudyMaterialSearchIndices_Title",
                table: "StudyMaterialSearchIndices");

            migrationBuilder.DropIndex(
                name: "IX_PostSearchIndices_Content",
                table: "PostSearchIndices");

            migrationBuilder.DropIndex(
                name: "IX_PostSearchIndices_Title",
                table: "PostSearchIndices");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_Description",
                table: "StudyMaterialSearchIndices",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_Title",
                table: "StudyMaterialSearchIndices",
                column: "Title");

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_Content",
                table: "PostSearchIndices",
                column: "Content");

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_Title",
                table: "PostSearchIndices",
                column: "Title");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StudyMaterialSearchIndices_Description",
                table: "StudyMaterialSearchIndices");

            migrationBuilder.DropIndex(
                name: "IX_StudyMaterialSearchIndices_Title",
                table: "StudyMaterialSearchIndices");

            migrationBuilder.DropIndex(
                name: "IX_PostSearchIndices_Content",
                table: "PostSearchIndices");

            migrationBuilder.DropIndex(
                name: "IX_PostSearchIndices_Title",
                table: "PostSearchIndices");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_Description",
                table: "StudyMaterialSearchIndices",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_Title",
                table: "StudyMaterialSearchIndices",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_Content",
                table: "PostSearchIndices",
                column: "Content")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_Title",
                table: "PostSearchIndices",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }
    }
}
