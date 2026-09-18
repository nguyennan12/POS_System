using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixPendingChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "promotion_id1",
                table: "promotion_targets",
                type: "uuid",
                nullable: true);

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_promotion_targets_promotions_promotion_id1",
                table: "promotion_targets");

            migrationBuilder.DropIndex(
                name: "IX_promotion_targets_promotion_id1",
                table: "promotion_targets");

            migrationBuilder.DropColumn(
                name: "promotion_id1",
                table: "promotion_targets");
        }
    }
}
