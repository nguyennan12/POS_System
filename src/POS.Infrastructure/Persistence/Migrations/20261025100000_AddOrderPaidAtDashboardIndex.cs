using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using POS.Infrastructure.Persistence;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261025100000_AddOrderPaidAtDashboardIndex")]
public partial class AddOrderPaidAtDashboardIndex : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_orders_store_id_paid_at",
            table: "orders",
            columns: new[] { "store_id", "paid_at" },
            filter: "paid_at IS NOT NULL AND status = 'Paid'");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_orders_store_id_paid_at",
            table: "orders");
    }
}
