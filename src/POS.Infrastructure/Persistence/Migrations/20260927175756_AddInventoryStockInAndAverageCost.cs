using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryStockInAndAverageCost : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "unit_cost",
                table: "stock_transactions",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "stock_in_vouchers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Draft",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Completed");

            migrationBuilder.AddColumn<decimal>(
                name: "average_cost",
                table: "stock_entries",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddCheckConstraint(
                name: "ck_stock_entries_average_cost",
                table: "stock_entries",
                sql: "average_cost >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_stock_entries_average_cost",
                table: "stock_entries");

            migrationBuilder.DropColumn(
                name: "unit_cost",
                table: "stock_transactions");

            migrationBuilder.DropColumn(
                name: "average_cost",
                table: "stock_entries");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "stock_in_vouchers",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Completed",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValue: "Draft");
        }
    }
}
