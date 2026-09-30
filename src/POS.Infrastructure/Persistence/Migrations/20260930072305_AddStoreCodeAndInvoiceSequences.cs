using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreCodeAndInvoiceSequences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "code",
                table: "stores",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            // Historical stores have no code. Full UUID text is stable and unique,
            // and avoids assigning a made-up short-code convention during migration.
            migrationBuilder.Sql("UPDATE stores SET code = upper(replace(id::text, '-', '')) WHERE code IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "code",
                table: "stores",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "invoice_no",
                table: "invoices",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(30)",
                oldMaxLength: 30);

            migrationBuilder.CreateTable(
                name: "invoice_sequences",
                columns: table => new
                {
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invoice_date = table.Column<DateOnly>(type: "date", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoice_sequences", x => new { x.store_id, x.invoice_date });
                    table.ForeignKey(
                        name: "FK_invoice_sequences_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Resume above the highest legacy suffix for each store/date. Legacy
            // invoice numbers carry their own UTC date; new numbers use store time.
            migrationBuilder.Sql("""
                WITH legacy AS (
                    SELECT o.store_id,
                           CASE WHEN i.invoice_no ~ '^HD-.+-[0-9]{8}-[0-9]+$'
                                THEN to_date(substring(i.invoice_no from '-([0-9]{8})-[0-9]+$'), 'YYYYMMDD')
                                ELSE (i.issued_at AT TIME ZONE s.timezone)::date END AS invoice_date,
                           CASE WHEN i.invoice_no ~ '-[0-9]{1,18}$'
                                THEN substring(i.invoice_no from '-([0-9]+)$')::bigint
                                ELSE 0 END AS sequence
                    FROM invoices i
                    JOIN orders o ON o.id = i.order_id
                    JOIN stores s ON s.id = o.store_id
                )
                INSERT INTO invoice_sequences (store_id, invoice_date, last_value)
                SELECT store_id, invoice_date, GREATEST(count(*), max(sequence))
                FROM legacy GROUP BY store_id, invoice_date;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_stores_code",
                table: "stores",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "invoice_sequences");

            migrationBuilder.DropIndex(
                name: "IX_stores_code",
                table: "stores");

            migrationBuilder.DropColumn(
                name: "code",
                table: "stores");

            migrationBuilder.AlterColumn<string>(
                name: "invoice_no",
                table: "invoices",
                type: "character varying(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(64)",
                oldMaxLength: 64);
        }
    }
}
