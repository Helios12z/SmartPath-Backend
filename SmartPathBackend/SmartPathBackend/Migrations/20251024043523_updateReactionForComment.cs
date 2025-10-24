using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SmartPathBackend.Migrations
{
    /// <inheritdoc />
    public partial class updateReactionForComment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reactions_UserId",
                table: "Reactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PostId",
                table: "Reactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<Guid>(
                name: "CommentId",
                table: "Reactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_CommentId",
                table: "Reactions",
                column: "CommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId_CommentId",
                table: "Reactions",
                columns: new[] { "UserId", "CommentId" },
                unique: true,
                filter: "\"CommentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId_PostId",
                table: "Reactions",
                columns: new[] { "UserId", "PostId" },
                unique: true,
                filter: "\"PostId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_reactions_one_target",
                table: "Reactions",
                sql: "((\"PostId\" IS NOT NULL AND \"CommentId\" IS NULL) OR (\"PostId\" IS NULL AND \"CommentId\" IS NOT NULL))");

            migrationBuilder.AddForeignKey(
                name: "FK_Reactions_Comments_CommentId",
                table: "Reactions",
                column: "CommentId",
                principalTable: "Comments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reactions_Comments_CommentId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_CommentId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_UserId_CommentId",
                table: "Reactions");

            migrationBuilder.DropIndex(
                name: "IX_Reactions_UserId_PostId",
                table: "Reactions");

            migrationBuilder.DropCheckConstraint(
                name: "ck_reactions_one_target",
                table: "Reactions");

            migrationBuilder.DropColumn(
                name: "CommentId",
                table: "Reactions");

            migrationBuilder.AlterColumn<Guid>(
                name: "PostId",
                table: "Reactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reactions_UserId",
                table: "Reactions",
                column: "UserId");
        }
    }
}
