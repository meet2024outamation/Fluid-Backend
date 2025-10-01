using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluid.Entities.Migrations.Core
{
    /// <inheritdoc />
    public partial class removeOrderIdFromOrderFlowTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_order_flows_orders_order_id",
                table: "order_flows");

            migrationBuilder.DropIndex(
                name: "ix_order_flows_order_id_rank",
                table: "order_flows");

            migrationBuilder.DropColumn(
                name: "order_id",
                table: "order_flows");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "order_id",
                table: "order_flows",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_order_flows_order_id_rank",
                table: "order_flows",
                columns: new[] { "order_id", "rank" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_order_flows_orders_order_id",
                table: "order_flows",
                column: "order_id",
                principalTable: "orders",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
