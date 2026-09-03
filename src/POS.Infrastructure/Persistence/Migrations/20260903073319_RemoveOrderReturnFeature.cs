using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrderReturnFeature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "order_return_items");

            migrationBuilder.DropTable(
                name: "order_returns");

            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_status",
                table: "orders");

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_status",
                table: "orders",
                sql: "status IN ('Draft','Confirmed','Paid','Cancelled')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_orders_status",
                table: "orders");

            migrationBuilder.CreateTable(
                name: "order_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    processed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    refund_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_returns", x => x.id);
                    table.CheckConstraint("ck_order_returns_refund_amount", "refund_amount >= 0");
                    table.CheckConstraint("ck_order_returns_status", "status IN ('Pending','Approved','Rejected')");
                    table.ForeignKey(
                        name: "FK_order_returns_employees_processed_by",
                        column: x => x.processed_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_returns_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_return_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    return_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    refund_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_return_items", x => x.id);
                    table.CheckConstraint("ck_order_return_items_qty", "qty > 0");
                    table.CheckConstraint("ck_order_return_items_refund_amount", "refund_amount >= 0");
                    table.ForeignKey(
                        name: "FK_order_return_items_order_items_order_item_id",
                        column: x => x.order_item_id,
                        principalTable: "order_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_return_items_order_returns_return_id",
                        column: x => x.return_id,
                        principalTable: "order_returns",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_orders_status",
                table: "orders",
                sql: "status IN ('Draft','Confirmed','Paid','Cancelled','Refunded','PartiallyRefunded')");

            migrationBuilder.CreateIndex(
                name: "IX_order_return_items_order_item_id",
                table: "order_return_items",
                column: "order_item_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_return_items_return_id",
                table: "order_return_items",
                column: "return_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_returns_order_id",
                table: "order_returns",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_returns_processed_by",
                table: "order_returns",
                column: "processed_by");
        }
    }
}
