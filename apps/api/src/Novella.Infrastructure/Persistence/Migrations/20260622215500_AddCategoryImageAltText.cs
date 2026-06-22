using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novella.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryImageAltText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageAltAr",
                table: "Categories",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageAltEn",
                table: "Categories",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageAltAr",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ImageAltEn",
                table: "Categories");
        }
    }
}
