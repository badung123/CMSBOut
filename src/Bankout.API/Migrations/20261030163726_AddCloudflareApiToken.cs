using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankout.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudflareApiToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ApiToken",
                table: "CloudflareAccounts",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiToken",
                table: "CloudflareAccounts");
        }
    }
}
