using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Receiving.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddReceiptNumberCounters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "receipt_number_counters",
                schema: "receiving",
                columns: table => new
                {
                    owner_id = table.Column<int>(type: "integer", nullable: false),
                    last_number = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receipt_number_counters", x => x.owner_id);
                    table.CheckConstraint("ck_receipt_number_counters_last_number", "last_number > 0");
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "receipt_number_counters",
                schema: "receiving");
        }
    }
}
