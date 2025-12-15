using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPathBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyMaterialRatingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "StudyMaterialRatings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Rating = table.Column<int>(type: "integer", nullable: false),
                    Comment = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyMaterialRatings", x => x.Id);
                    table.CheckConstraint("ck_rating_range", "\"Rating\" >= 1 AND \"Rating\" <= 5");
                    table.ForeignKey(
                        name: "FK_StudyMaterialRatings_StudyMaterials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "StudyMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyMaterialRatings_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialRatings_CreatedAt",
                table: "StudyMaterialRatings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialRatings_MaterialId",
                table: "StudyMaterialRatings",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialRatings_MaterialId_UserId",
                table: "StudyMaterialRatings",
                columns: new[] { "MaterialId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialRatings_UserId",
                table: "StudyMaterialRatings",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudyMaterialRatings");
        }
    }
}
