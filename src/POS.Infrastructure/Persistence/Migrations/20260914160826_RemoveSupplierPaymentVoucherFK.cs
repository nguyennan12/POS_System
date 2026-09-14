using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSupplierPaymentVoucherFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_supplier_payments_stock_in_vouchers_voucher_id",
                table: "supplier_payments");

            migrationBuilder.DropIndex(
                name: "IX_supplier_payments_voucher_id",
                table: "supplier_payments");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_voucher_id",
                table: "supplier_payments",
                column: "voucher_id");

            migrationBuilder.AddForeignKey(
                name: "FK_supplier_payments_stock_in_vouchers_voucher_id",
                table: "supplier_payments",
                column: "voucher_id",
                principalTable: "stock_in_vouchers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
