using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Throughline.Modules.Inventory.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOwnerName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "name",
                schema: "inventory",
                table: "owners",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "name",
                schema: "inventory",
                table: "owners");
        }
    }
}
