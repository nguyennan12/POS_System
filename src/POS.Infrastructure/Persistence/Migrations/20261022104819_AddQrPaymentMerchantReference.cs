using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddQrPaymentMerchantReference : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "merchant_reference",
                table: "payments",
                type: "character varying(25)",
                maxLength: 25,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_method_merchant_reference",
                table: "payments",
                columns: new[] { "method", "merchant_reference" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_payments_pending_qr",
                table: "payments",
                columns: new[] { "order_id", "method" },
                unique: true,
                filter: "status = 'Pending' AND merchant_reference IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_payments_method_merchant_reference",
                table: "payments");

            migrationBuilder.DropIndex(
                name: "IX_payments_pending_qr",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "merchant_reference",
                table: "payments");
        }
    }
}
