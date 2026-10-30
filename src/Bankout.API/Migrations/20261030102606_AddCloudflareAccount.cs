using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankout.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudflareAccount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CloudflareAccounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserCloudflare = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ZoneId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    AccountId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ListDomainRedirect = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    UpdatedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudflareAccounts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CloudflareAccounts_ZoneId",
                table: "CloudflareAccounts",
                column: "ZoneId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudflareAccounts");
        }
    }
}
