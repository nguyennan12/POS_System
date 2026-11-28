using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStoreLayoutCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "picking_orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    order_id = table.Column<Guid>(type: "uuid", nullable: true),
                    assigned_to = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())"),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picking_orders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "product_placement_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    from_shelf_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    to_shelf_level_id = table.Column<Guid>(type: "uuid", nullable: true),
                    performed_by = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_placement_histories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "store_layouts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    store_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_number = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    grid_width = table.Column<int>(type: "integer", nullable: false),
                    grid_height = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    revision = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())"),
                    published_by = table.Column<Guid>(type: "uuid", nullable: true),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_layouts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "picking_order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    picking_order_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    picked_qty = table.Column<decimal>(type: "numeric(18,3)", precision: 18, scale: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    placement_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shelf_code_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    level_number_snapshot = table.Column<int>(type: "integer", nullable: false),
                    zone_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_picking_order_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_picking_order_items_picking_orders_picking_order_id",
                        column: x => x.picking_order_id,
                        principalTable: "picking_orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_layout_cells",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    x = table.Column<int>(type: "integer", nullable: false),
                    y = table.Column<int>(type: "integer", nullable: false),
                    cell_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    traversal_cost = table.Column<int>(type: "integer", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_layout_cells", x => x.id);
                    table.ForeignKey(
                        name: "FK_store_layout_cells_store_layouts_layout_id",
                        column: x => x.layout_id,
                        principalTable: "store_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "store_zones",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "#3B82F6"),
                    position_x = table.Column<int>(type: "integer", nullable: false),
                    position_y = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_store_zones", x => x.id);
                    table.ForeignKey(
                        name: "FK_store_zones_store_layouts_layout_id",
                        column: x => x.layout_id,
                        principalTable: "store_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shelves",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    layout_id = table.Column<Guid>(type: "uuid", nullable: false),
                    zone_id = table.Column<Guid>(type: "uuid", nullable: true),
                    code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    position_x = table.Column<int>(type: "integer", nullable: false),
                    position_y = table.Column<int>(type: "integer", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    orientation = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shelves", x => x.id);
                    table.ForeignKey(
                        name: "FK_shelves_store_layouts_layout_id",
                        column: x => x.layout_id,
                        principalTable: "store_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_shelves_store_zones_zone_id",
                        column: x => x.zone_id,
                        principalTable: "store_zones",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "shelf_levels",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shelf_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level_number = table.Column<int>(type: "integer", nullable: false),
                    label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shelf_levels", x => x.id);
                    table.ForeignKey(
                        name: "FK_shelf_levels_shelves_shelf_id",
                        column: x => x.shelf_id,
                        principalTable: "shelves",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "product_placements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shelf_level_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sku_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "TIMEZONE('utc', now())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_placements", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_placements_shelf_levels_shelf_level_id",
                        column: x => x.shelf_level_id,
                        principalTable: "shelf_levels",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_picking_order_items_picking_order_id",
                table: "picking_order_items",
                column: "picking_order_id");

            migrationBuilder.CreateIndex(
                name: "IX_picking_orders_store_id_status_created_at",
                table: "picking_orders",
                columns: new[] { "store_id", "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_product_placement_histories_layout_id_sku_id",
                table: "product_placement_histories",
                columns: new[] { "layout_id", "sku_id" });

            migrationBuilder.CreateIndex(
                name: "IX_product_placements_shelf_level_id_sku_id",
                table: "product_placements",
                columns: new[] { "shelf_level_id", "sku_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_product_placements_sku_id",
                table: "product_placements",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "IX_shelf_levels_shelf_id_level_number",
                table: "shelf_levels",
                columns: new[] { "shelf_id", "level_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shelves_layout_id_code",
                table: "shelves",
                columns: new[] { "layout_id", "code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shelves_zone_id",
                table: "shelves",
                column: "zone_id");

            migrationBuilder.CreateIndex(
                name: "IX_store_layout_cells_layout_id_x_y",
                table: "store_layout_cells",
                columns: new[] { "layout_id", "x", "y" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_layouts_store_id_version_number",
                table: "store_layouts",
                columns: new[] { "store_id", "version_number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_store_zones_layout_id_code",
                table: "store_zones",
                columns: new[] { "layout_id", "code" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "picking_order_items");

            migrationBuilder.DropTable(
                name: "product_placement_histories");

            migrationBuilder.DropTable(
                name: "product_placements");

            migrationBuilder.DropTable(
                name: "store_layout_cells");

            migrationBuilder.DropTable(
                name: "picking_orders");

            migrationBuilder.DropTable(
                name: "shelf_levels");

            migrationBuilder.DropTable(
                name: "shelves");

            migrationBuilder.DropTable(
                name: "store_zones");

            migrationBuilder.DropTable(
                name: "store_layouts");
        }
    }
}
