using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeNormalizedUsername : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "normalized_username",
                table: "employees",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                computedColumnSql: "lower(username)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_employees_normalized_username",
                table: "employees",
                column: "normalized_username",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employees_normalized_username",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "normalized_username",
                table: "employees");
        }
    }
}
