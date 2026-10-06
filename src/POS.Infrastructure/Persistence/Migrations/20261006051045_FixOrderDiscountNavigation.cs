using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderDiscountNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_discounts_orders_order_id1",
                table: "order_discounts");

            migrationBuilder.DropIndex(
                name: "IX_order_discounts_order_id1",
                table: "order_discounts");

            migrationBuilder.DropColumn(
                name: "order_id1",
                table: "order_discounts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "order_id1",
                table: "order_discounts",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_discounts_order_id1",
                table: "order_discounts",
                column: "order_id1");

            migrationBuilder.AddForeignKey(
                name: "FK_order_discounts_orders_order_id1",
                table: "order_discounts",
                column: "order_id1",
                principalTable: "orders",
                principalColumn: "id");
        }
    }
}
