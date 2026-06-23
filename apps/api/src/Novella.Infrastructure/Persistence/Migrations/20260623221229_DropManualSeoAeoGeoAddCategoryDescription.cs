using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novella.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropManualSeoAeoGeoAddCategoryDescription : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) Add the new visible, customer-facing category description columns.
            migrationBuilder.AddColumn<string>(
                name: "DescriptionAr",
                table: "Categories",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEn",
                table: "Categories",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            // 2) Preserve useful existing category text before the legacy columns are dropped.
            // Deterministic priority per language: AEO summary -> SEO description -> GEO content ->
            // a generated description. An existing non-empty description is never overwritten.
            migrationBuilder.Sql(@"
UPDATE [Categories]
SET [DescriptionAr] = COALESCE(
        NULLIF([AeoSummaryAr], N''),
        NULLIF([SeoDescriptionAr], N''),
        NULLIF([GeoContentAr], N''),
        N'تشكيلة ' + [NameAr] + N' من نوفيلا أكسسوارات بتصميم ناعم وأنيق، مع توصيل داخل مصر ودعم عبر واتساب.')
WHERE [DescriptionAr] IS NULL OR [DescriptionAr] = N'';");

            migrationBuilder.Sql(@"
UPDATE [Categories]
SET [DescriptionEn] = COALESCE(
        NULLIF([AeoSummaryEn], N''),
        NULLIF([SeoDescriptionEn], N''),
        NULLIF([GeoContentEn], N''),
        N'Discover ' + [NameEn] + N' from Novella Accessories — elegant accessories with delivery across Egypt.')
WHERE [DescriptionEn] IS NULL OR [DescriptionEn] = N'';");

            // 3) Drop the legacy manual SEO/AEO/GEO columns from all applicable tables.
            migrationBuilder.DropColumn(
                name: "AeoSummaryAr",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "AeoSummaryEn",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "GeoContentAr",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "GeoContentEn",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionAr",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionEn",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "SeoTitleAr",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "SeoTitleEn",
                table: "StaticPages");

            migrationBuilder.DropColumn(
                name: "AeoSummaryAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AeoSummaryEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GeoContentAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GeoContentEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SeoTitleAr",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SeoTitleEn",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AeoSummaryAr",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "AeoSummaryEn",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "GeoContentAr",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "GeoContentEn",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionAr",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SeoDescriptionEn",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SeoTitleAr",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SeoTitleEn",
                table: "Categories");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DescriptionAr",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DescriptionEn",
                table: "Categories");

            migrationBuilder.AddColumn<string>(
                name: "AeoSummaryAr",
                table: "StaticPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AeoSummaryEn",
                table: "StaticPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoContentAr",
                table: "StaticPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoContentEn",
                table: "StaticPages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionAr",
                table: "StaticPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionEn",
                table: "StaticPages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleAr",
                table: "StaticPages",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleEn",
                table: "StaticPages",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AeoSummaryAr",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AeoSummaryEn",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoContentAr",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoContentEn",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionAr",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionEn",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleAr",
                table: "Products",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleEn",
                table: "Products",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AeoSummaryAr",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AeoSummaryEn",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoContentAr",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GeoContentEn",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionAr",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescriptionEn",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleAr",
                table: "Categories",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitleEn",
                table: "Categories",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }
    }
}
