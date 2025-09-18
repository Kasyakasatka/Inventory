using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryManagement.Web.Migrations
{
    /// <inheritdoc />
    public partial class AddSalesforceConnectionStatusToUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSalesforceConnected",
                table: "AspNetUsers",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSalesforceConnected",
                table: "AspNetUsers");
        }
    }
}
