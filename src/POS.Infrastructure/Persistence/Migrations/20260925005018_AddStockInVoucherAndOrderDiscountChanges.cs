using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStockInVoucherAndOrderDiscountChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "applied_voucher_code",
                table: "orders",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "applied_voucher_id",
                table: "orders",
                type: "uuid",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_discounts_orders_order_id1",
                table: "order_discounts");

            migrationBuilder.DropIndex(
                name: "IX_order_discounts_order_id1",
                table: "order_discounts");

            migrationBuilder.DropColumn(
                name: "applied_voucher_code",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "applied_voucher_id",
                table: "orders");

            migrationBuilder.DropColumn(
                name: "order_id1",
                table: "order_discounts");
        }
    }
}
