using Microsoft.EntityFrameworkCore.Migrations;

namespace InventoryManagement.Web.Migrations
{
    public partial class FixSearchVectorTriggers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS update_user_search_vector ON ""AspNetUsers"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_user_search_vector_trigger();");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS update_inventory_search_vector ON ""Inventories"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_inventory_search_vector_trigger();");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS update_item_search_vector ON ""Items"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_item_search_vector_trigger();");

            migrationBuilder.Sql(
                @"CREATE OR REPLACE FUNCTION update_user_search_vector_trigger() RETURNS trigger AS $$
                BEGIN
                    new.""SearchVector"" := to_tsvector('english', coalesce(new.""UserName"", '') || ' ' || coalesce(new.""Email"", ''));
                    return new;
                END
                $$ LANGUAGE plpgsql;");
            migrationBuilder.Sql(
                @"CREATE TRIGGER update_user_search_vector
                BEFORE INSERT OR UPDATE ON ""AspNetUsers""
                FOR EACH ROW EXECUTE FUNCTION update_user_search_vector_trigger();");

            migrationBuilder.Sql(
                @"CREATE OR REPLACE FUNCTION update_inventory_search_vector_trigger() RETURNS trigger AS $$
                BEGIN
                    new.""SearchVector"" := to_tsvector('english', coalesce(new.""Title"", '') || ' ' || coalesce(new.""Description"", '') || ' ' || array_to_string(new.""Tags"", ' '));
                    return new;
                END
                $$ LANGUAGE plpgsql;");
            migrationBuilder.Sql(
                @"CREATE TRIGGER update_inventory_search_vector
                BEFORE INSERT OR UPDATE ON ""Inventories""
                FOR EACH ROW EXECUTE FUNCTION update_inventory_search_vector_trigger();");

            migrationBuilder.Sql(
                @"CREATE OR REPLACE FUNCTION update_item_search_vector_from_custom_fields() RETURNS trigger AS $$
                BEGIN
                    UPDATE ""Items""
                    SET ""SearchVector"" = (
                        SELECT to_tsvector('english', string_agg(coalesce(cf.""StringValue"", ''), ' ') ||
                                                       string_agg(coalesce(cf.""IntValue""::text, ''), ' ') ||
                                                       string_agg(coalesce(cf.""BoolValue""::text, ''), ' '))
                        FROM ""CustomFields"" AS cf
                        WHERE cf.""ItemId"" = NEW.""ItemId""
                    )
                    WHERE ""Id"" = NEW.""ItemId"";
                    RETURN NEW;
                END
                $$ LANGUAGE plpgsql;");

            migrationBuilder.Sql(
                @"CREATE TRIGGER update_item_search_vector
                AFTER INSERT OR UPDATE OR DELETE ON ""CustomFields""
                FOR EACH ROW EXECUTE FUNCTION update_item_search_vector_from_custom_fields();");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS update_user_search_vector ON ""AspNetUsers"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_user_search_vector_trigger();");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS update_inventory_search_vector ON ""Inventories"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_inventory_search_vector_trigger();");
            migrationBuilder.Sql(@"DROP TRIGGER IF EXISTS update_item_search_vector ON ""CustomFields"";");
            migrationBuilder.Sql(@"DROP FUNCTION IF EXISTS update_item_search_vector_from_custom_fields();");
        }
    }
}