using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "citizen_id",
                table: "employees",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "employees",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "employment_type",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "hire_date",
                table: "employees",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "hourly_wage",
                table: "employees",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "phone",
                table: "employees",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "citizen_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "email",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "employment_type",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "hire_date",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "hourly_wage",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "phone",
                table: "employees");
        }
    }
}
