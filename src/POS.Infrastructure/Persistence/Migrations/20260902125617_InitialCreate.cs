using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace POS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "member_tiers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    min_spending = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    point_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    discount_rate = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    display_color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_tiers", x => x.id);
                    table.CheckConstraint("ck_member_tiers_name", "name IN ('Normal','Silver','Gold','VIP')");
                });

            migrationBuilder.CreateTable(
                name: "resources",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_resources", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "stores",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    timezone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "Asia/Ho_Chi_Minh"),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    tax_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    receipt_header = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    receipt_footer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "suppliers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    tax_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    contact_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    credit_terms = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_suppliers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "translations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    language_code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    key = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_translations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    dob = table.Column<DateOnly>(type: "date", nullable: true),
                    barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    member_tier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    total_spending = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.id);
                    table.ForeignKey(
                        name: "FK_customers_member_tiers_member_tier_id",
                        column: x => x.member_tier_id,
                        principalTable: "member_tiers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    resource_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    code = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.id);
                    table.CheckConstraint("ck_permissions_action", "action IN ('Create','Read','Update','Delete','Approve','Export','Override')");
                    table.ForeignKey(
                        name: "FK_permissions_resources_resource_id",
                        column: x => x.resource_id,
                        principalTable: "resources",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    parent_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    display_order = table.Column<int>(type: "int", nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_visible = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.id);
                    table.ForeignKey(
                        name: "FK_categories_categories_parent_id",
                        column: x => x.parent_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_categories_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "faq_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    question = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    answer = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    keywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faq_entries", x => x.id);
                    table.ForeignKey(
                        name: "FK_faq_entries_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    is_system_role = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.id);
                    table.ForeignKey(
                        name: "FK_roles_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    key = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_system_configs_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "chat_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    session_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    message_count = table.Column<int>(type: "int", nullable: false),
                    started_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    ended_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_conversations", x => x.id);
                    table.ForeignKey(
                        name: "FK_chat_conversations_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_chat_conversations_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "loyalty_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    points_balance = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    last_updated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_loyalty_accounts", x => x.id);
                    table.CheckConstraint("ck_loyalty_accounts_points_balance", "points_balance >= 0");
                    table.ForeignKey(
                        name: "FK_loyalty_accounts_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    base_unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    image_url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                    table.CheckConstraint("ck_products_status", "status IN ('Active','Inactive')");
                    table.ForeignKey(
                        name: "FK_products_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_products_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_chain_owner = table.Column<bool>(type: "bit", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    pin_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    pin_lookup_hash = table.Column<string>(type: "char(64)", nullable: true),
                    failed_login_count = table.Column<short>(type: "smallint", nullable: false),
                    locked_until = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.id);
                    table.CheckConstraint("ck_employees_store_required_unless_chain_owner", "store_id IS NOT NULL OR is_chain_owner = 1");
                    table.ForeignKey(
                        name: "FK_employees_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_employees_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    conversation_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.id);
                    table.CheckConstraint("ck_chat_messages_sender", "sender IN ('Customer','Bot')");
                    table.ForeignKey(
                        name: "FK_chat_messages_chat_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "chat_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skus",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    barcode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    attributes_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    cost_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    sell_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_rate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_skus", x => x.id);
                    table.CheckConstraint("ck_skus_cost_price", "cost_price >= 0");
                    table.CheckConstraint("ck_skus_sell_price", "sell_price >= 0");
                    table.CheckConstraint("ck_skus_tax_rate", "tax_rate IN (0,5,8,10)");
                    table.ForeignKey(
                        name: "FK_skus_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_skus_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "audit_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    employee_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entity_type = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    entity_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ip_address = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_audit_logs_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_audit_logs_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "employee_store_access",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    employee_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    granted_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    granted_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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

            migrationBuilder.CreateTable(
                name: "promotions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    min_order_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    max_discount_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    conditions_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_stackable = table.Column<bool>(type: "bit", nullable: false),
                    is_exclusive = table.Column<bool>(type: "bit", nullable: false),
                    applies_to = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    valid_from = table.Column<DateTime>(type: "datetime2", nullable: false),
                    valid_to = table.Column<DateTime>(type: "datetime2", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Active"),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotions", x => x.id);
                    table.CheckConstraint("ck_promotions_applies_to", "applies_to IN ('All','Category','SKU')");
                    table.CheckConstraint("ck_promotions_status", "status IN ('Active','Inactive')");
                    table.CheckConstraint("ck_promotions_type", "type IN ('PercentSku','FixedSku','BuyXGetY','CartPercent','CartFixed','HappyHour')");
                    table.CheckConstraint("ck_promotions_valid_range", "valid_to IS NULL OR valid_to >= valid_from");
                    table.CheckConstraint("ck_promotions_value", "value >= 0");
                    table.ForeignKey(
                        name: "FK_promotions_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_promotions_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    employee_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    token_hash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "role_permissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    role_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    permission_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    granted_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    granted_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_role_permissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_role_permissions_employees_granted_by",
                        column: x => x.granted_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_role_permissions_permissions_permission_id",
                        column: x => x.permission_id,
                        principalTable: "permissions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_role_permissions_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shifts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    employee_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    opening_cash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    closing_cash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    actual_cash = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Open"),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    opened_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    closed_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shifts", x => x.id);
                    table.CheckConstraint("ck_shifts_actual_cash", "actual_cash IS NULL OR actual_cash >= 0");
                    table.CheckConstraint("ck_shifts_closing_cash", "closing_cash IS NULL OR closing_cash >= 0");
                    table.CheckConstraint("ck_shifts_opening_cash", "opening_cash >= 0");
                    table.CheckConstraint("ck_shifts_status", "status IN ('Open','Closed')");
                    table.ForeignKey(
                        name: "FK_shifts_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_shifts_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_in_vouchers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    total_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Completed"),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_in_vouchers", x => x.id);
                    table.CheckConstraint("ck_stock_in_vouchers_status", "status IN ('Draft','Completed','Cancelled')");
                    table.CheckConstraint("ck_stock_in_vouchers_total_amount", "total_amount >= 0");
                    table.ForeignKey(
                        name: "FK_stock_in_vouchers_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_vouchers_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_vouchers_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_takes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    approved_by = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    approved_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_takes", x => x.id);
                    table.CheckConstraint("ck_stock_takes_status", "status IN ('Draft','Pending','Approved')");
                    table.ForeignKey(
                        name: "FK_stock_takes_employees_approved_by",
                        column: x => x.approved_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_takes_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_takes_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "price_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    valid_from = table.Column<DateTime>(type: "datetime2", nullable: false),
                    valid_to = table.Column<DateTime>(type: "datetime2", nullable: true),
                    customer_group = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_price_lists", x => x.id);
                    table.CheckConstraint("ck_price_lists_price", "price >= 0");
                    table.CheckConstraint("ck_price_lists_valid_range", "valid_to IS NULL OR valid_to >= valid_from");
                    table.ForeignKey(
                        name: "FK_price_lists_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_price_lists_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_price_lists_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_no = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    expiry_date = table.Column<DateOnly>(type: "date", nullable: true),
                    received_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_batches", x => x.id);
                    table.CheckConstraint("ck_stock_batches_qty", "qty >= 0");
                    table.ForeignKey(
                        name: "FK_stock_batches_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_batches_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    qty_on_hand = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    min_stock = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    last_updated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_entries", x => x.id);
                    table.CheckConstraint("ck_stock_entries_qty_on_hand", "qty_on_hand >= 0");
                    table.ForeignKey(
                        name: "FK_stock_entries_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_entries_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "unit_conversions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    unit_name = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    conversion_factor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    sell_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_unit_conversions", x => x.id);
                    table.CheckConstraint("ck_unit_conversions_conversion_factor", "conversion_factor > 0");
                    table.CheckConstraint("ck_unit_conversions_sell_price", "sell_price >= 0");
                    table.ForeignKey(
                        name: "FK_unit_conversions_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "promotion_targets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    promotion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    category_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promotion_targets", x => x.id);
                    table.CheckConstraint("ck_promotion_targets_exactly_one_target", "(category_id IS NOT NULL AND sku_id IS NULL) OR (category_id IS NULL AND sku_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_promotion_targets_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_promotion_targets_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_promotion_targets_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "vouchers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    promotion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    max_uses = table.Column<int>(type: "int", nullable: false),
                    used_count = table.Column<int>(type: "int", nullable: false),
                    per_customer_limit = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    expires_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_vouchers", x => x.id);
                    table.CheckConstraint("ck_vouchers_max_uses", "max_uses > 0");
                    table.CheckConstraint("ck_vouchers_used_count", "used_count <= max_uses");
                    table.ForeignKey(
                        name: "FK_vouchers_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    shift_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    currency_code = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    subtotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    grand_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.id);
                    table.CheckConstraint("ck_orders_status", "status IN ('Draft','Confirmed','Paid','Cancelled','Refunded','PartiallyRefunded')");
                    table.ForeignKey(
                        name: "FK_orders_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_orders_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_shifts_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shifts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_orders_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_in_voucher_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    voucher_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_in_voucher_items", x => x.id);
                    table.CheckConstraint("ck_stock_in_voucher_items_qty", "qty > 0");
                    table.CheckConstraint("ck_stock_in_voucher_items_total_price", "total_price >= 0");
                    table.CheckConstraint("ck_stock_in_voucher_items_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_stock_in_voucher_items_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_in_voucher_items_stock_in_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "stock_in_vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "supplier_payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    supplier_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    voucher_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_supplier_payments", x => x.id);
                    table.CheckConstraint("ck_supplier_payments_amount", "amount > 0");
                    table.CheckConstraint("ck_supplier_payments_method", "method IN ('Cash','BankTransfer','Other')");
                    table.ForeignKey(
                        name: "FK_supplier_payments_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payments_stock_in_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "stock_in_vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_supplier_payments_suppliers_supplier_id",
                        column: x => x.supplier_id,
                        principalTable: "suppliers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_take_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    take_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    system_qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    actual_qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    diff_qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false, computedColumnSql: "actual_qty - system_qty", stored: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_take_items", x => x.id);
                    table.ForeignKey(
                        name: "FK_stock_take_items_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_take_items_stock_takes_take_id",
                        column: x => x.take_id,
                        principalTable: "stock_takes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    invoice_no = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    buyer_name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    buyer_tax_code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    buyer_address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    total_before_tax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    grand_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    issued_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.id);
                    table.ForeignKey(
                        name: "FK_invoices_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_discounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    promotion_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    voucher_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    discount_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    description = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    applied_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_discounts", x => x.id);
                    table.CheckConstraint("ck_order_discounts_amount", "discount_amount >= 0");
                    table.CheckConstraint("ck_order_discounts_source", "promotion_id IS NOT NULL OR voucher_id IS NOT NULL");
                    table.ForeignKey(
                        name: "FK_order_discounts_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_discounts_promotions_promotion_id",
                        column: x => x.promotion_id,
                        principalTable: "promotions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_order_discounts_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "order_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    unit_price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    discount_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    tax_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    line_total = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_items", x => x.id);
                    table.CheckConstraint("ck_order_items_qty", "qty > 0");
                    table.CheckConstraint("ck_order_items_unit_price", "unit_price >= 0");
                    table.ForeignKey(
                        name: "FK_order_items_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_items_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_returns",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    reason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    refund_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    processed_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
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
                name: "payments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    method = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    change_amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    transaction_ref = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    gateway_response_json = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_payments", x => x.id);
                    table.CheckConstraint("ck_payments_amount", "amount > 0");
                    table.CheckConstraint("ck_payments_change_amount", "change_amount IS NULL OR change_amount >= 0");
                    table.CheckConstraint("ck_payments_method", "method IN ('Cash','MoMo','VietQR','Card','Points')");
                    table.CheckConstraint("ck_payments_status", "status IN ('Pending','Success','Failed','Timeout')");
                    table.ForeignKey(
                        name: "FK_payments_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "point_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    points = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_point_transactions", x => x.id);
                    table.CheckConstraint("ck_point_transactions_type", "type IN ('Earn','Redeem','Adjust')");
                    table.ForeignKey(
                        name: "FK_point_transactions_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_point_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "stock_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    store_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sku_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    qty = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    stock_in_voucher_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_by = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_stock_transactions", x => x.id);
                    table.CheckConstraint("ck_stock_transactions_type", "type IN ('StockIn','SaleOut','Dispose','Adjust')");
                    table.ForeignKey(
                        name: "FK_stock_transactions_employees_created_by",
                        column: x => x.created_by,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transactions_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transactions_skus_sku_id",
                        column: x => x.sku_id,
                        principalTable: "skus",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transactions_stock_in_vouchers_stock_in_voucher_id",
                        column: x => x.stock_in_voucher_id,
                        principalTable: "stock_in_vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_stock_transactions_stores_store_id",
                        column: x => x.store_id,
                        principalTable: "stores",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "voucher_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    voucher_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    customer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    used_at = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_voucher_usages", x => x.id);
                    table.ForeignKey(
                        name: "FK_voucher_usages_customers_customer_id",
                        column: x => x.customer_id,
                        principalTable: "customers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_usages_orders_order_id",
                        column: x => x.order_id,
                        principalTable: "orders",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_voucher_usages_vouchers_voucher_id",
                        column: x => x.voucher_id,
                        principalTable: "vouchers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "order_return_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false, defaultValueSql: "NEWID()"),
                    return_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    order_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_employee_id_created_at",
                table: "audit_logs",
                columns: new[] { "employee_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_entity_type_entity_id",
                table: "audit_logs",
                columns: new[] { "entity_type", "entity_id" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_store_id",
                table: "audit_logs",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_parent_id",
                table: "categories",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "IX_categories_store_id",
                table: "categories",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_customer_id",
                table: "chat_conversations",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_session_id",
                table: "chat_conversations",
                column: "session_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_chat_conversations_store_id",
                table: "chat_conversations",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_conversation_id_created_at",
                table: "chat_messages",
                columns: new[] { "conversation_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_customers_barcode",
                table: "customers",
                column: "barcode",
                unique: true,
                filter: "[barcode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_member_tier_id",
                table: "customers",
                column: "member_tier_id");

            migrationBuilder.CreateIndex(
                name: "IX_customers_phone",
                table: "customers",
                column: "phone",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_employees_role_id",
                table: "employees",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_store_id_pin_lookup_hash",
                table: "employees",
                columns: new[] { "store_id", "pin_lookup_hash" },
                unique: true,
                filter: "[store_id] IS NOT NULL AND [pin_lookup_hash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employees_username",
                table: "employees",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_faq_entries_store_id",
                table: "faq_entries",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_invoice_no",
                table: "invoices",
                column: "invoice_no",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_order_id",
                table: "invoices",
                column: "order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_accounts_customer_id",
                table: "loyalty_accounts",
                column: "customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_member_tiers_name",
                table: "member_tiers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_order_discounts_order_id",
                table: "order_discounts",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_discounts_promotion_id",
                table: "order_discounts",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_discounts_voucher_id",
                table: "order_discounts",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_order_id",
                table: "order_items",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_order_items_sku_id",
                table: "order_items",
                column: "sku_id");

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

            migrationBuilder.CreateIndex(
                name: "IX_orders_created_by",
                table: "orders",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_orders_customer_id",
                table: "orders",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_shift_id",
                table: "orders",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "IX_orders_store_id_created_at",
                table: "orders",
                columns: new[] { "store_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_payments_method_transaction_ref",
                table: "payments",
                columns: new[] { "method", "transaction_ref" },
                unique: true,
                filter: "[transaction_ref] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_payments_order_id",
                table: "payments",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_permissions_code",
                table: "permissions",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_resource_id_action",
                table: "permissions",
                columns: new[] { "resource_id", "action" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_customer_id_created_at",
                table: "point_transactions",
                columns: new[] { "customer_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_point_transactions_order_id",
                table: "point_transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_created_by",
                table: "price_lists",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_sku_id",
                table: "price_lists",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_store_id",
                table: "price_lists",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_category_id",
                table: "products",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_products_store_id_status",
                table: "products",
                columns: new[] { "store_id", "status" },
                filter: "status = 'Active'");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_category_id",
                table: "promotion_targets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_promotion_id",
                table: "promotion_targets",
                column: "promotion_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotion_targets_sku_id",
                table: "promotion_targets",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_created_by",
                table: "promotions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_store_id_status_valid_from_valid_to",
                table: "promotions",
                columns: new[] { "store_id", "status", "valid_from", "valid_to" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_employee_id_revoked_at",
                table: "refresh_tokens",
                columns: new[] { "employee_id", "revoked_at" });

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_resources_code",
                table: "resources",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_granted_by",
                table: "role_permissions",
                column: "granted_by");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_permission_id",
                table: "role_permissions",
                column: "permission_id");

            migrationBuilder.CreateIndex(
                name: "IX_role_permissions_role_id_permission_id",
                table: "role_permissions",
                columns: new[] { "role_id", "permission_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_store_id_name",
                table: "roles",
                columns: new[] { "store_id", "name" },
                unique: true,
                filter: "[store_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_employee_id",
                table: "shifts",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_shifts_store_id_status",
                table: "shifts",
                columns: new[] { "store_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_skus_product_id",
                table: "skus",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "IX_skus_store_id_barcode",
                table: "skus",
                columns: new[] { "store_id", "barcode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_skus_store_id_is_active",
                table: "skus",
                columns: new[] { "store_id", "is_active" },
                filter: "is_active = 1");

            migrationBuilder.CreateIndex(
                name: "IX_skus_store_id_sku_code",
                table: "skus",
                columns: new[] { "store_id", "sku_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_expiry_date",
                table: "stock_batches",
                column: "expiry_date");

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_sku_id",
                table: "stock_batches",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_batches_store_id_sku_id_batch_no",
                table: "stock_batches",
                columns: new[] { "store_id", "sku_id", "batch_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_entries_sku_id",
                table: "stock_entries",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_entries_store_id_sku_id",
                table: "stock_entries",
                columns: new[] { "store_id", "sku_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_voucher_items_sku_id",
                table: "stock_in_voucher_items",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_voucher_items_voucher_id",
                table: "stock_in_voucher_items",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_vouchers_created_by",
                table: "stock_in_vouchers",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_vouchers_store_id",
                table: "stock_in_vouchers",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_in_vouchers_supplier_id",
                table: "stock_in_vouchers",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_take_items_sku_id",
                table: "stock_take_items",
                column: "sku_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_take_items_take_id",
                table: "stock_take_items",
                column: "take_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_takes_approved_by",
                table: "stock_takes",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_takes_created_by",
                table: "stock_takes",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_takes_store_id",
                table: "stock_takes",
                column: "store_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_created_by",
                table: "stock_transactions",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_order_id",
                table: "stock_transactions",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_sku_id_created_at",
                table: "stock_transactions",
                columns: new[] { "sku_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_stock_in_voucher_id",
                table: "stock_transactions",
                column: "stock_in_voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_stock_transactions_store_id_created_at",
                table: "stock_transactions",
                columns: new[] { "store_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_created_by",
                table: "supplier_payments",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_supplier_id",
                table: "supplier_payments",
                column: "supplier_id");

            migrationBuilder.CreateIndex(
                name: "IX_supplier_payments_voucher_id",
                table: "supplier_payments",
                column: "voucher_id");

            migrationBuilder.CreateIndex(
                name: "IX_system_configs_store_id_key",
                table: "system_configs",
                columns: new[] { "store_id", "key" },
                unique: true,
                filter: "[store_id] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_translations_language_code_key",
                table: "translations",
                columns: new[] { "language_code", "key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_unit_conversions_sku_id_unit_name",
                table: "unit_conversions",
                columns: new[] { "sku_id", "unit_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_customer_id",
                table: "voucher_usages",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_order_id",
                table: "voucher_usages",
                column: "order_id");

            migrationBuilder.CreateIndex(
                name: "IX_voucher_usages_voucher_id_order_id",
                table: "voucher_usages",
                columns: new[] { "voucher_id", "order_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_code",
                table: "vouchers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_vouchers_promotion_id",
                table: "vouchers",
                column: "promotion_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "employee_store_access");

            migrationBuilder.DropTable(
                name: "faq_entries");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "loyalty_accounts");

            migrationBuilder.DropTable(
                name: "order_discounts");

            migrationBuilder.DropTable(
                name: "order_return_items");

            migrationBuilder.DropTable(
                name: "payments");

            migrationBuilder.DropTable(
                name: "point_transactions");

            migrationBuilder.DropTable(
                name: "price_lists");

            migrationBuilder.DropTable(
                name: "promotion_targets");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "role_permissions");

            migrationBuilder.DropTable(
                name: "stock_batches");

            migrationBuilder.DropTable(
                name: "stock_entries");

            migrationBuilder.DropTable(
                name: "stock_in_voucher_items");

            migrationBuilder.DropTable(
                name: "stock_take_items");

            migrationBuilder.DropTable(
                name: "stock_transactions");

            migrationBuilder.DropTable(
                name: "supplier_payments");

            migrationBuilder.DropTable(
                name: "system_configs");

            migrationBuilder.DropTable(
                name: "translations");

            migrationBuilder.DropTable(
                name: "unit_conversions");

            migrationBuilder.DropTable(
                name: "voucher_usages");

            migrationBuilder.DropTable(
                name: "chat_conversations");

            migrationBuilder.DropTable(
                name: "order_items");

            migrationBuilder.DropTable(
                name: "order_returns");

            migrationBuilder.DropTable(
                name: "permissions");

            migrationBuilder.DropTable(
                name: "stock_takes");

            migrationBuilder.DropTable(
                name: "stock_in_vouchers");

            migrationBuilder.DropTable(
                name: "vouchers");

            migrationBuilder.DropTable(
                name: "skus");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "resources");

            migrationBuilder.DropTable(
                name: "suppliers");

            migrationBuilder.DropTable(
                name: "promotions");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "customers");

            migrationBuilder.DropTable(
                name: "shifts");

            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "member_tiers");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "stores");
        }
    }
}
