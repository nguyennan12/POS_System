using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveEmployeeStoreAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "employee_store_access");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "employee_store_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    employee_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    granted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_store_access", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_store_access_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_store_access_employees_granted_by",
                        column: x => x.granted_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employee_store_access_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_employee_store_access_employee_id_store_id",
                table: "employee_store_access",
                columns: new[] { "employee_id", "store_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_employee_store_access_granted_by",
                table: "employee_store_access",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_employee_store_access_store_id",
                table: "employee_store_access",
                column: "store_id");
        }
    }
}
