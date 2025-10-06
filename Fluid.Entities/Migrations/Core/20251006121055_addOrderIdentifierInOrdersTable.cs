using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluid.Entities.Migrations.Core
{
    /// <inheritdoc />
    public partial class addOrderIdentifierInOrdersTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "order_identifier",
                table: "orders",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "order_identifier",
                table: "orders");
        }
    }
}
