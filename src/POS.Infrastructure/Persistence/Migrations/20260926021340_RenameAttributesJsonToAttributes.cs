using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameAttributesJsonToAttributes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "attributes_json",
                table: "skus",
                newName: "attributes");

            migrationBuilder.Sql(@"
                ALTER TABLE skus 
                ALTER COLUMN attributes TYPE jsonb 
                USING (
                    CASE 
                        WHEN attributes IS NULL OR trim(attributes) = '' THEN NULL 
                        ELSE attributes::jsonb 
                    END
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE skus 
                ALTER COLUMN attributes TYPE text 
                USING attributes::text;
            ");

            migrationBuilder.RenameColumn(
                name: "attributes",
                table: "skus",
                newName: "attributes_json");
        }
    }
}
