using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluid.Entities.Migrations.Core
{
    /// <inheritdoc />
    public partial class addKeyingValueInOrderDataTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "keying_value",
                table: "order_data",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "keying_value",
                table: "order_data");
        }
    }
}
