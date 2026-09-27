using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SkuReceiptQuantityAllocated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "quantity_allocated",
                schema: "inventory",
                table: "sku_receipts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "quantity_available",
                schema: "inventory",
                table: "sku_receipts",
                type: "integer",
                nullable: false,
                computedColumnSql: "quantity_received - quantity_allocated",
                stored: true);

            // backfill from existing allocations; quantity_available recomputes on update
            migrationBuilder.Sql(
                """
                UPDATE inventory.sku_receipts AS r
                SET quantity_allocated = a.total
                FROM (
                    SELECT sku_receipt_id, SUM(quantity_allocated) AS total
                    FROM inventory.receipt_allocations
                    GROUP BY sku_receipt_id) AS a
                WHERE a.sku_receipt_id = r.sku_receipt_id;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "quantity_available",
                schema: "inventory",
                table: "sku_receipts");

            migrationBuilder.DropColumn(
                name: "quantity_allocated",
                schema: "inventory",
                table: "sku_receipts");
        }
    }
}
