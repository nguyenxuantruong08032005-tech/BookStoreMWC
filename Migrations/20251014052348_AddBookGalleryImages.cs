using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookStoreMVC.Migrations
{
    /// <inheritdoc />
    public partial class AddBookGalleryImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookGalleryImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BookId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ImageFileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    ImageContentType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ImageFileSize = table.Column<long>(type: "bigint", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookGalleryImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookGalleryImages_Books_BookId",
                        column: x => x.BookId,
                        principalTable: "Books",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookGalleryImages_BookId",
                table: "BookGalleryImages",
                column: "BookId");

            migrationBuilder.CreateIndex(
                name: "IX_BookGalleryImages_BookId_DisplayOrder",
                table: "BookGalleryImages",
                columns: new[] { "BookId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BookGalleryImages_IsActive",
                table: "BookGalleryImages",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookGalleryImages");
        }
    }
}
