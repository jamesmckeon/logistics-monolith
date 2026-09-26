using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AllocationPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sku_receipts_owner_id_sku_id",
                schema: "inventory",
                table: "sku_receipts");

            migrationBuilder.DropColumn(
                name: "owner_id",
                schema: "inventory",
                table: "sku_receipts");

            migrationBuilder.CreateIndex(
                name: "ix_sku_receipts_received_on",
                schema: "inventory",
                table: "sku_receipts",
                column: "received_on");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_sku_receipts_received_on",
                schema: "inventory",
                table: "sku_receipts");

            migrationBuilder.AddColumn<int>(
                name: "owner_id",
                schema: "inventory",
                table: "sku_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_sku_receipts_owner_id_sku_id",
                schema: "inventory",
                table: "sku_receipts",
                columns: new[] { "owner_id", "sku_id" });
        }
    }
}
