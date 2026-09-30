using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FIX01_BatchNoExpiryOnVoucherItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "batch_no",
                table: "stock_in_voucher_items",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "expiry_date",
                table: "stock_in_voucher_items",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "batch_no",
                table: "stock_in_voucher_items");

            migrationBuilder.DropColumn(
                name: "expiry_date",
                table: "stock_in_voucher_items");
        }
    }
}
