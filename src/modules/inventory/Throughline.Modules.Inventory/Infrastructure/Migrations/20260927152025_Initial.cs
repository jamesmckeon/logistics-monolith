using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "inventory");

            migrationBuilder.CreateTable(
                name: "order_allocations",
                schema: "inventory",
                columns: table => new
                {
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    allocating = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_allocations", x => x.order_id);
                });

            migrationBuilder.CreateTable(
                name: "owners",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false),
                    allocation_policy = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_owners", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                schema: "inventory",
                columns: table => new
                {
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skus", x => x.sku_id);
                });

            migrationBuilder.CreateTable(
                name: "orderline_allocations",
                schema: "inventory",
                columns: table => new
                {
                    orderline_allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_requested = table.Column<int>(type: "integer", nullable: false),
                    quantity_allocated = table.Column<int>(type: "integer", nullable: false),
                    quantity_short = table.Column<int>(type: "integer", nullable: false),
                    last_updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orderline_allocations", x => x.orderline_allocation_id);
                    table.ForeignKey(
                        name: "fk_orderline_allocations_order_allocations_order_id",
                        column: x => x.order_id,
                        principalSchema: "inventory",
                        principalTable: "order_allocations",
                        principalColumn: "order_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sku_receipts",
                schema: "inventory",
                columns: table => new
                {
                    sku_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_received = table.Column<int>(type: "integer", nullable: false),
                    quantity_allocated = table.Column<int>(type: "integer", nullable: false),
                    received_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    last_updated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    quantity_available = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sku_receipts", x => x.sku_receipt_id);
                    table.ForeignKey(
                        name: "fk_sku_receipts_skus_sku_id",
                        column: x => x.sku_id,
                        principalSchema: "inventory",
                        principalTable: "skus",
                        principalColumn: "sku_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "receipt_allocations",
                schema: "inventory",
                columns: table => new
                {
                    receipt_allocation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity_allocated = table.Column<int>(type: "integer", nullable: false),
                    allocated_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    orderline_allocation_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_allocations", x => x.receipt_allocation_id);
                    table.ForeignKey(
                        name: "fk_receipt_allocations_orderline_allocations_orderline_allocat",
                        column: x => x.orderline_allocation_id,
                        principalSchema: "inventory",
                        principalTable: "orderline_allocations",
                        principalColumn: "orderline_allocation_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_allocations_owner_id",
                schema: "inventory",
                table: "order_allocations",
                column: "owner_id");

            migrationBuilder.CreateIndex(
                name: "ix_orderline_allocations_order_id",
                schema: "inventory",
                table: "orderline_allocations",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_allocations_orderline_allocation_id",
                schema: "inventory",
                table: "receipt_allocations",
                column: "orderline_allocation_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipt_allocations_sku_receipt_id",
                schema: "inventory",
                table: "receipt_allocations",
                column: "sku_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_sku_receipts_received_on",
                schema: "inventory",
                table: "sku_receipts",
                column: "received_on");

            migrationBuilder.CreateIndex(
                name: "ix_sku_receipts_sku_id",
                schema: "inventory",
                table: "sku_receipts",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "ix_skus_owner_id_code",
                schema: "inventory",
                table: "skus",
                columns: new[] { "owner_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "owners",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "receipt_allocations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "sku_receipts",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "orderline_allocations",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "skus",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "order_allocations",
                schema: "inventory");
        }
    }
}
