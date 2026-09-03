using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixOrderDiscountCascadePaths : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_discounts_promotions_promotion_id",
                table: "order_discounts");

            migrationBuilder.DropForeignKey(
                name: "FK_order_discounts_vouchers_voucher_id",
                table: "order_discounts");

            migrationBuilder.AddForeignKey(
                name: "FK_order_discounts_promotions_promotion_id",
                table: "order_discounts",
                column: "promotion_id",
                principalTable: "promotions",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_order_discounts_vouchers_voucher_id",
                table: "order_discounts",
                column: "voucher_id",
                principalTable: "vouchers",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_order_discounts_promotions_promotion_id",
                table: "order_discounts");

            migrationBuilder.DropForeignKey(
                name: "FK_order_discounts_vouchers_voucher_id",
                table: "order_discounts");

            migrationBuilder.AddForeignKey(
                name: "FK_order_discounts_promotions_promotion_id",
                table: "order_discounts",
                column: "promotion_id",
                principalTable: "promotions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_order_discounts_vouchers_voucher_id",
                table: "order_discounts",
                column: "voucher_id",
                principalTable: "vouchers",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
