using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderCreatedOn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Added nullable, backfilled, then made required, so existing orders get their real created date
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "order_created_on",
                schema: "inventory",
                table: "order_allocations",
                type: "timestamp with time zone",
                nullable: true);

            // Ordering's table is only read if it exists: a database migrated for Inventory alone has none
            migrationBuilder.Sql("""
                                 DO $$
                                 BEGIN
                                     IF to_regclass('orders.orders') IS NOT NULL THEN
                                         UPDATE inventory.order_allocations a
                                         SET order_created_on = o.created_date
                                         FROM orders.orders o
                                         WHERE o.order_id = a.order_id;
                                     END IF;
                                 END $$;

                                 UPDATE inventory.order_allocations
                                 SET order_created_on = COALESCE(last_updated, now())
                                 WHERE order_created_on IS NULL;
                                 """);

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "order_created_on",
                schema: "inventory",
                table: "order_allocations",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_order_allocations_owner_id_order_created_on_order_id",
                schema: "inventory",
                table: "order_allocations",
                columns: new[] { "owner_id", "order_created_on", "order_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_order_allocations_owner_id_order_created_on_order_id",
                schema: "inventory",
                table: "order_allocations");

            migrationBuilder.DropColumn(
                name: "order_created_on",
                schema: "inventory",
                table: "order_allocations");
        }
    }
}
