using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPromotionTargetAndStockInVoucher : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotion_targets_promotions_promotion_id1",
                table: "promotion_targets");

            migrationBuilder.DropIndex(
                name: "IX_roles_store_id_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_promotion_targets_promotion_id1",
                table: "promotion_targets");

            migrationBuilder.DropColumn(
                name: "promotion_id1",
                table: "promotion_targets");

            migrationBuilder.CreateIndex(
                name: "IX_roles_name",
                table: "roles",
                column: "name",
                unique: true,
                filter: "store_id IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_roles_store_id_name",
                table: "roles",
                columns: new[] { "store_id", "name" },
                unique: true,
                filter: "store_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_roles_name",
                table: "roles");

            migrationBuilder.DropIndex(
                name: "IX_roles_store_id_name",
                table: "roles");

            migrationBuilder.AddColumn<Guid>(
                name: "promotion_id1",
                table: "promotion_targets",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_store_id_name",
                table: "roles",
                columns: new[] { "store_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_promotion_id1",
                table: "promotion_targets",
                column: "promotion_id1");

            migrationBuilder.AddForeignKey(
                name: "FK_promotion_targets_promotions_promotion_id1",
                table: "promotion_targets",
                column: "promotion_id1",
                principalTable: "promotions",
                principalColumn: "id");
        }
    }
}
