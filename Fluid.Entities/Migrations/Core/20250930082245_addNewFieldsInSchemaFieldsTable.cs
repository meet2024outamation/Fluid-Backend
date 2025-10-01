using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluid.Entities.Migrations.Core
{
    /// <inheritdoc />
    public partial class addNewFieldsInSchemaFieldsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "max_length",
                table: "schema_fields",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "min_length",
                table: "schema_fields",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "precision",
                table: "schema_fields",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                table: "order_flows",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_length",
                table: "schema_fields");

            migrationBuilder.DropColumn(
                name: "min_length",
                table: "schema_fields");

            migrationBuilder.DropColumn(
                name: "precision",
                table: "schema_fields");

            migrationBuilder.DropColumn(
                name: "is_active",
                table: "order_flows");
        }
    }
}
