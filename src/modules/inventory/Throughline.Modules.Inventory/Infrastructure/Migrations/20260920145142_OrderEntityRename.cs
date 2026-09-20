using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OrderEntityRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_lines_orders_owner_id_order_id",
                schema: "inventory",
                table: "order_lines");

            migrationBuilder.DropPrimaryKey(
                name: "pk_order_lines",
                schema: "inventory",
                table: "order_lines");

            migrationBuilder.RenameTable(
                name: "orders",
                schema: "inventory",
                newName: "order_allocations",
                newSchema: "inventory");

            migrationBuilder.RenameTable(
                name: "order_lines",
                schema: "inventory",
                newName: "orderline_allocations",
                newSchema: "inventory");

            migrationBuilder.RenameIndex(
                name: "ix_order_lines_owner_id_order_id",
                schema: "inventory",
                table: "orderline_allocations",
                newName: "ix_orderline_allocations_owner_id_order_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_orderline_allocations",
                schema: "inventory",
                table: "orderline_allocations",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_orderline_allocations_order_allocations_owner_id_order_id",
                schema: "inventory",
                table: "orderline_allocations",
                columns: new[] { "owner_id", "order_id" },
                principalSchema: "inventory",
                principalTable: "order_allocations",
                principalColumns: new[] { "owner_id", "order_id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_orderline_allocations_order_allocations_owner_id_order_id",
                schema: "inventory",
                table: "orderline_allocations");

            migrationBuilder.DropPrimaryKey(
                name: "pk_orderline_allocations",
                schema: "inventory",
                table: "orderline_allocations");

            migrationBuilder.RenameTable(
                name: "orderline_allocations",
                schema: "inventory",
                newName: "order_lines",
                newSchema: "inventory");

            migrationBuilder.RenameTable(
                name: "order_allocations",
                schema: "inventory",
                newName: "orders",
                newSchema: "inventory");

            migrationBuilder.RenameIndex(
                name: "ix_orderline_allocations_owner_id_order_id",
                schema: "inventory",
                table: "order_lines",
                newName: "ix_order_lines_owner_id_order_id");

            migrationBuilder.AddPrimaryKey(
                name: "pk_order_lines",
                schema: "inventory",
                table: "order_lines",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_order_lines_orders_owner_id_order_id",
                schema: "inventory",
                table: "order_lines",
                columns: new[] { "owner_id", "order_id" },
                principalSchema: "inventory",
                principalTable: "orders",
                principalColumns: new[] { "owner_id", "order_id" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
