using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "orders",
                schema: "inventory",
                columns: table => new
                {
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    allocation_status = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_orders", x => new { x.owner_id, x.order_id });
                });

            migrationBuilder.CreateTable(
                name: "order_lines",
                schema: "inventory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    quantity_requested = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_order_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_order_lines_orders_owner_id_order_id",
                        columns: x => new { x.owner_id, x.order_id },
                        principalSchema: "inventory",
                        principalTable: "orders",
                        principalColumns: new[] { "owner_id", "order_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_order_lines_owner_id_order_id",
                schema: "inventory",
                table: "order_lines",
                columns: new[] { "owner_id", "order_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_lines",
                schema: "inventory");

            migrationBuilder.DropTable(
                name: "orders",
                schema: "inventory");
        }
    }
}
