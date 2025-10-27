using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPathBackend.Migrations
{
    /// <inheritdoc />
    public partial class addReputationCheckpoint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReputationCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<int>(type: "integer", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LikeBandsApplied = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DislikeBandsApplied = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReputationCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReputationCheckpoints_ContentType_ContentId",
                table: "ReputationCheckpoints",
                columns: new[] { "ContentType", "ContentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReputationCheckpoints");
        }
    }
}
