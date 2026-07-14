using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bankout.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameBankToBankNoAddBankNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Bank",
                table: "BankoutRequests",
                newName: "BankNo");

            migrationBuilder.AlterColumn<string>(
                name: "BankNo",
                table: "BankoutRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "BankoutRequests",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ShortBankName",
                table: "BankoutRequests",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BankName",
                table: "BankoutRequests");

            migrationBuilder.DropColumn(
                name: "ShortBankName",
                table: "BankoutRequests");

            migrationBuilder.AlterColumn<string>(
                name: "BankNo",
                table: "BankoutRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.RenameColumn(
                name: "BankNo",
                table: "BankoutRequests",
                newName: "Bank");
        }
    }
}
