using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPathBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddPostAIReviewFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<bool>(
                name: "AiCategoryMatch",
                table: "Posts",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "AiConfidence",
                table: "Posts",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AiReason",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AiSuggestedCategoryId",
                table: "Posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectReason",
                table: "Posts",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReviewedAt",
                table: "Posts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ReviewedByAdminId",
                table: "Posts",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiCategoryMatch",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AiConfidence",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AiReason",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "AiSuggestedCategoryId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "RejectReason",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ReviewedAt",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ReviewedByAdminId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Posts");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "Posts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);
        }
    }
}
