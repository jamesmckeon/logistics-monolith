using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Ordering.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCreatedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // IF NOT EXISTS because these columns were first added to the Azure database by hand
            // (2026-10-18). Equivalent to the AddColumn calls EF generated; an existing column is
            // left as it is, so a hand-added one must already match this definition.
            migrationBuilder.Sql(
                "ALTER TABLE orders.orders " +
                "ADD COLUMN IF NOT EXISTS created_date timestamp with time zone NOT NULL DEFAULT now();");

            migrationBuilder.Sql(
                "ALTER TABLE orders.order_lines " +
                "ADD COLUMN IF NOT EXISTS created_date timestamp with time zone NOT NULL DEFAULT now();");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "created_date",
                schema: "orders",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "created_date",
                schema: "orders",
                table: "order_lines");
        }
    }
}
