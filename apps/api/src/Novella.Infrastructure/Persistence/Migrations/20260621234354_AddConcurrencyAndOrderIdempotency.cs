using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Novella.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurrencyAndOrderIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "ProductVariants",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Orders",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Orders",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.Sql("UPDATE [AnalyticsEvents] SET [MetadataJson] = LEFT([MetadataJson], 2048) WHERE LEN([MetadataJson]) > 2048");

            migrationBuilder.AlterColumn<string>(
                name: "MetadataJson",
                table: "AnalyticsEvents",
                type: "nvarchar(2048)",
                maxLength: 2048,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId_IdempotencyKey",
                table: "Orders",
                columns: new[] { "CustomerId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_CustomerId_Source",
                table: "Coupons",
                columns: new[] { "CustomerId", "Source" },
                unique: true,
                filter: "[CustomerId] IS NOT NULL AND [Source] = 'TwoDeliveredOrders'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId_IdempotencyKey",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Coupons_CustomerId_Source",
                table: "Coupons");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "ProductVariants");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Orders");

            migrationBuilder.AlterColumn<string>(
                name: "MetadataJson",
                table: "AnalyticsEvents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(2048)",
                oldMaxLength: 2048,
                oldNullable: true);
        }
    }
}
