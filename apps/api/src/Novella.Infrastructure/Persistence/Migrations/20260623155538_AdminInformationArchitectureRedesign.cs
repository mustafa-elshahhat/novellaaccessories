using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novella.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdminInformationArchitectureRedesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShippingSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FreeShippingThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsFreeShippingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingSettings", x => x.Id);
                });

            migrationBuilder.Sql(@"
                INSERT INTO ShippingSettings (Id, FreeShippingThreshold, IsFreeShippingEnabled, UpdatedAt)
                SELECT TOP (1) NEWID(), FreeShippingThreshold, IsFreeShippingEnabled, UpdatedAt
                FROM SiteSettings
                ORDER BY UpdatedAt DESC
            ");

            migrationBuilder.DropTable(
                name: "SiteSettings");

            migrationBuilder.DropColumn(
                name: "OrderConfirmationTemplate",
                table: "WhatsAppSettings");

            migrationBuilder.DropColumn(
                name: "OtpTemplate",
                table: "WhatsAppSettings");

            migrationBuilder.DropColumn(
                name: "ServiceBaseUrl",
                table: "WhatsAppSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShippingSettings");

            migrationBuilder.AddColumn<string>(
                name: "OrderConfirmationTemplate",
                table: "WhatsAppSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OtpTemplate",
                table: "WhatsAppSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceBaseUrl",
                table: "WhatsAppSettings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SiteSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DefaultSeoDescriptionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultSeoDescriptionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultSeoTitleAr = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DefaultSeoTitleEn = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Domain = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    FreeShippingThreshold = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    IsFreeShippingEnabled = table.Column<bool>(type: "bit", nullable: false),
                    SiteNameAr = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SiteNameEn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SiteSettings", x => x.Id);
                });
        }
    }
}
