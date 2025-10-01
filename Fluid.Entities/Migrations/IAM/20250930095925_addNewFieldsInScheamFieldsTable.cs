using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluid.Entities.Migrations.IAM
{
    /// <inheritdoc />
    public partial class addNewFieldsInScheamFieldsTable : Migration
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
        }
    }
}
