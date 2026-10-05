using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Receiving.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "receiving");

            migrationBuilder.CreateTable(
                name: "carriers",
                schema: "receiving",
                columns: table => new
                {
                    carrier_id = table.Column<int>(type: "integer", nullable: false),
                    scac_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    carrier_name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_carriers", x => x.carrier_id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_receipts",
                schema: "receiving",
                columns: table => new
                {
                    delivery_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    receipt_number = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    operator_id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    bill_of_lading = table.Column<string>(type: "text", nullable: true),
                    container_number = table.Column<string>(type: "text", nullable: true),
                    shipper_name = table.Column<string>(type: "text", nullable: true),
                    shipper_reference = table.Column<string>(type: "text", nullable: false),
                    trailer_number = table.Column<string>(type: "text", nullable: true),
                    carrier_id = table.Column<int>(type: "integer", nullable: false),
                    carrier_name = table.Column<string>(type: "text", nullable: false),
                    carrier_scac = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_receipts", x => x.delivery_receipt_id);
                    table.CheckConstraint("ck_delivery_receipts_trailer_or_container", "trailer_number IS NOT NULL OR container_number IS NOT NULL");
                });

            migrationBuilder.CreateTable(
                name: "delivery_submissions",
                schema: "receiving",
                columns: table => new
                {
                    delivery_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    request = table.Column<string>(type: "text", nullable: false),
                    result = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_delivery_submissions", x => new { x.owner_id, x.delivery_id });
                });

            migrationBuilder.CreateTable(
                name: "hold_reasons",
                schema: "receiving",
                columns: table => new
                {
                    reason_code = table.Column<string>(type: "text", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_hold_reasons", x => new { x.owner_id, x.reason_code });
                });

            migrationBuilder.CreateTable(
                name: "receiving_locations",
                schema: "receiving",
                columns: table => new
                {
                    location_id = table.Column<string>(type: "text", nullable: false),
                    location_type = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receiving_locations", x => x.location_id);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                schema: "receiving",
                columns: table => new
                {
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    sku_code = table.Column<string>(type: "text", nullable: false),
                    is_lot_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    is_expiration_tracked = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skus", x => x.sku_id);
                });

            migrationBuilder.CreateTable(
                name: "invalid_pallets",
                schema: "receiving",
                columns: table => new
                {
                    invalid_pallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    sku_code = table.Column<string>(type: "text", nullable: false),
                    license_plate_number = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    lot_number = table.Column<string>(type: "text", nullable: true),
                    expires_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    exceptions = table.Column<string[]>(type: "text[]", nullable: false),
                    location_id = table.Column<string>(type: "text", nullable: false),
                    request_location_id = table.Column<string>(type: "text", nullable: false),
                    delivery_receipt_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_invalid_pallets", x => x.invalid_pallet_id);
                    table.CheckConstraint("ck_invalid_pallets_exceptions", "cardinality(exceptions) > 0");
                    table.CheckConstraint("ck_invalid_pallets_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_invalid_pallets_delivery_receipts_delivery_receipt_id",
                        column: x => x.delivery_receipt_id,
                        principalSchema: "receiving",
                        principalTable: "delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_invalid_pallets_receiving_locations_location_id",
                        column: x => x.location_id,
                        principalSchema: "receiving",
                        principalTable: "receiving_locations",
                        principalColumn: "location_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "received_pallets",
                schema: "receiving",
                columns: table => new
                {
                    received_pallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    license_plate_number = table.Column<string>(type: "text", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    location_id = table.Column<string>(type: "text", nullable: false),
                    hold_reason_owner_id = table.Column<int>(type: "integer", nullable: true),
                    hold_reason_code = table.Column<string>(type: "text", nullable: true),
                    expires_on = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lot_number = table.Column<string>(type: "text", nullable: true),
                    delivery_receipt_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_is_expiration_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    sku_is_lot_tracked = table.Column<bool>(type: "boolean", nullable: false),
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    sku_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_received_pallets", x => x.received_pallet_id);
                    table.CheckConstraint("ck_received_pallets_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_received_pallets_delivery_receipts_delivery_receipt_id",
                        column: x => x.delivery_receipt_id,
                        principalSchema: "receiving",
                        principalTable: "delivery_receipts",
                        principalColumn: "delivery_receipt_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_received_pallets_hold_reasons_hold_reason",
                        columns: x => new { x.hold_reason_owner_id, x.hold_reason_code },
                        principalSchema: "receiving",
                        principalTable: "hold_reasons",
                        principalColumns: new[] { "owner_id", "reason_code" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_received_pallets_receiving_locations_location_id",
                        column: x => x.location_id,
                        principalSchema: "receiving",
                        principalTable: "receiving_locations",
                        principalColumn: "location_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_carriers_scac_code",
                schema: "receiving",
                table: "carriers",
                column: "scac_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_delivery_receipts_owner_id_receipt_number",
                schema: "receiving",
                table: "delivery_receipts",
                columns: new[] { "owner_id", "receipt_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_invalid_pallets_delivery_receipt_id",
                schema: "receiving",
                table: "invalid_pallets",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_invalid_pallets_location_id",
                schema: "receiving",
                table: "invalid_pallets",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_received_pallets_delivery_receipt_id",
                schema: "receiving",
                table: "received_pallets",
                column: "delivery_receipt_id");

            migrationBuilder.CreateIndex(
                name: "ix_received_pallets_hold_reason_owner_id_hold_reason_code",
                schema: "receiving",
                table: "received_pallets",
                columns: new[] { "hold_reason_owner_id", "hold_reason_code" });

            migrationBuilder.CreateIndex(
                name: "ix_received_pallets_location_id",
                schema: "receiving",
                table: "received_pallets",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "ix_skus_owner_id_sku_code",
                schema: "receiving",
                table: "skus",
                columns: new[] { "owner_id", "sku_code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "carriers",
                schema: "receiving");

            migrationBuilder.DropTable(
                name: "delivery_submissions",
                schema: "receiving");

            migrationBuilder.DropTable(
                name: "invalid_pallets",
                schema: "receiving");

            migrationBuilder.DropTable(
                name: "received_pallets",
                schema: "receiving");

            migrationBuilder.DropTable(
                name: "skus",
                schema: "receiving");

            migrationBuilder.DropTable(
                name: "delivery_receipts",
                schema: "receiving");

            migrationBuilder.DropTable(
                name: "hold_reasons",
                schema: "receiving");

            migrationBuilder.DropTable(
                name: "receiving_locations",
                schema: "receiving");
        }
    }
}
