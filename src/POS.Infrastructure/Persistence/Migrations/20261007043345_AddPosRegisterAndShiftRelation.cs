using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPosRegisterAndShiftRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "register_id",
                table: "shifts",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "pos_registers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pos_registers", x => x.id);
                    table.ForeignKey(
                        name: "FK_pos_registers_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_shifts_register_id_status",
                table: "shifts",
                columns: new[] { "register_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_pos_registers_store_id_code",
                table: "pos_registers",
                columns: new[] { "store_id", "code" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_shifts_pos_registers_register_id",
                table: "shifts",
                column: "register_id",
                principalTable: "pos_registers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_shifts_pos_registers_register_id",
                table: "shifts");

            migrationBuilder.DropTable(
                name: "pos_registers");

            migrationBuilder.DropIndex(
                name: "IX_shifts_register_id_status",
                table: "shifts");

            migrationBuilder.DropColumn(
                name: "register_id",
                table: "shifts");
        }
    }
}
