using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Receiving.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveHoldReasonOwner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_received_pallets_hold_reasons_hold_reason",
                schema: "receiving",
                table: "received_pallets");

            migrationBuilder.DropIndex(
                name: "ix_received_pallets_hold_reason_owner_id_hold_reason_code",
                schema: "receiving",
                table: "received_pallets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_hold_reasons",
                schema: "receiving",
                table: "hold_reasons");

            migrationBuilder.DropColumn(
                name: "hold_reason_owner_id",
                schema: "receiving",
                table: "received_pallets");

            migrationBuilder.DropColumn(
                name: "owner_id",
                schema: "receiving",
                table: "hold_reasons");

            migrationBuilder.AddPrimaryKey(
                name: "pk_hold_reasons",
                schema: "receiving",
                table: "hold_reasons",
                column: "reason_code");

            migrationBuilder.CreateIndex(
                name: "ix_received_pallets_hold_reason_code",
                schema: "receiving",
                table: "received_pallets",
                column: "hold_reason_code");

            migrationBuilder.AddForeignKey(
                name: "fk_received_pallets_hold_reasons_hold_reason",
                schema: "receiving",
                table: "received_pallets",
                column: "hold_reason_code",
                principalSchema: "receiving",
                principalTable: "hold_reasons",
                principalColumn: "reason_code",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_received_pallets_hold_reasons_hold_reason",
                schema: "receiving",
                table: "received_pallets");

            migrationBuilder.DropIndex(
                name: "ix_received_pallets_hold_reason_code",
                schema: "receiving",
                table: "received_pallets");

            migrationBuilder.DropPrimaryKey(
                name: "pk_hold_reasons",
                schema: "receiving",
                table: "hold_reasons");

            migrationBuilder.AddColumn<int>(
                name: "hold_reason_owner_id",
                schema: "receiving",
                table: "received_pallets",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "owner_id",
                schema: "receiving",
                table: "hold_reasons",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "pk_hold_reasons",
                schema: "receiving",
                table: "hold_reasons",
                columns: new[] { "owner_id", "reason_code" });

            migrationBuilder.CreateIndex(
                name: "ix_received_pallets_hold_reason_owner_id_hold_reason_code",
                schema: "receiving",
                table: "received_pallets",
                columns: new[] { "hold_reason_owner_id", "hold_reason_code" });

            migrationBuilder.AddForeignKey(
                name: "fk_received_pallets_hold_reasons_hold_reason",
                schema: "receiving",
                table: "received_pallets",
                columns: new[] { "hold_reason_owner_id", "hold_reason_code" },
                principalSchema: "receiving",
                principalTable: "hold_reasons",
                principalColumns: new[] { "owner_id", "reason_code" },
                onDelete: ReferentialAction.Restrict);
        }
    }
}
