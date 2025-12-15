using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SmartPathBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddStudyMaterialAndAISearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "MaterialCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "MaterialCategories",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PostSearchIndices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PostId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsQuestion = table.Column<bool>(type: "boolean", nullable: false),
                    IsSolved = table.Column<bool>(type: "boolean", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    LikeCount = table.Column<int>(type: "integer", nullable: false),
                    CommentCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorName = table.Column<string>(type: "text", nullable: false),
                    AuthorUsername = table.Column<string>(type: "text", nullable: false),
                    AuthorAvatar = table.Column<string>(type: "text", nullable: false),
                    CategoryIds = table.Column<string>(type: "text", nullable: false),
                    CategoryNames = table.Column<string>(type: "text", nullable: false),
                    CategorySlugs = table.Column<string>(type: "text", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    LastIndexedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostSearchIndices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostSearchIndices_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PostSearchIndices_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SearchQueryLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Query = table.Column<string>(type: "text", nullable: false),
                    NormalizedQuery = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserIdentifier = table.Column<string>(type: "text", nullable: true),
                    SearchType = table.Column<string>(type: "text", nullable: false),
                    Filters = table.Column<string>(type: "text", nullable: false),
                    ResultCount = table.Column<int>(type: "integer", nullable: false),
                    PostResults = table.Column<int>(type: "integer", nullable: false),
                    StudyMaterialResults = table.Column<int>(type: "integer", nullable: false),
                    QueryTime = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Page = table.Column<int>(type: "integer", nullable: false),
                    PageSize = table.Column<int>(type: "integer", nullable: false),
                    SortBy = table.Column<string>(type: "text", nullable: false),
                    SortOrder = table.Column<string>(type: "text", nullable: false),
                    UserAgent = table.Column<string>(type: "text", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SearchQueryLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SearchQueryLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudyMaterialSearchIndices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudyMaterialId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    ResourceType = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    DownloadUrl = table.Column<string>(type: "text", nullable: false),
                    ViewCount = table.Column<int>(type: "integer", nullable: false),
                    DownloadCount = table.Column<int>(type: "integer", nullable: false),
                    AverageRating = table.Column<float>(type: "real", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UploaderId = table.Column<Guid>(type: "uuid", nullable: false),
                    UploaderName = table.Column<string>(type: "text", nullable: false),
                    UploaderUsername = table.Column<string>(type: "text", nullable: false),
                    UploaderAvatar = table.Column<string>(type: "text", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryName = table.Column<string>(type: "text", nullable: false),
                    CategoryPath = table.Column<string>(type: "text", nullable: false),
                    CategoryLevel = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: false),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    AiConfidence = table.Column<float>(type: "real", nullable: false),
                    AiReason = table.Column<string>(type: "text", nullable: true),
                    LastIndexedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudyMaterialSearchIndices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudyMaterialSearchIndices_MaterialCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "MaterialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyMaterialSearchIndices_StudyMaterials_StudyMaterialId",
                        column: x => x.StudyMaterialId,
                        principalTable: "StudyMaterials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudyMaterialSearchIndices_Users_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_AuthorId",
                table: "PostSearchIndices",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_Content",
                table: "PostSearchIndices",
                column: "Content")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_CreatedAt_IsQuestion",
                table: "PostSearchIndices",
                columns: new[] { "CreatedAt", "IsQuestion" });

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_PostId",
                table: "PostSearchIndices",
                column: "PostId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostSearchIndices_Title",
                table: "PostSearchIndices",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueryLogs_CreatedAt",
                table: "SearchQueryLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueryLogs_Query_CreatedAt",
                table: "SearchQueryLogs",
                columns: new[] { "Query", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SearchQueryLogs_UserId",
                table: "SearchQueryLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_CategoryId_IsApproved_CreatedAt",
                table: "StudyMaterialSearchIndices",
                columns: new[] { "CategoryId", "IsApproved", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_Description",
                table: "StudyMaterialSearchIndices",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_StudyMaterialId",
                table: "StudyMaterialSearchIndices",
                column: "StudyMaterialId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_Title",
                table: "StudyMaterialSearchIndices",
                column: "Title")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_StudyMaterialSearchIndices_UploaderId",
                table: "StudyMaterialSearchIndices",
                column: "UploaderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostSearchIndices");

            migrationBuilder.DropTable(
                name: "SearchQueryLogs");

            migrationBuilder.DropTable(
                name: "StudyMaterialSearchIndices");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "MaterialCategories");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "MaterialCategories");
        }
    }
}
