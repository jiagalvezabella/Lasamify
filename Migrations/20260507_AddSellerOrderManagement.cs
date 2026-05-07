using Microsoft.EntityFrameworkCore.Migrations;

namespace Lasamify.Migrations
{
    public partial class AddSellerOrderManagement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SellerStatus",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true,
                defaultValue: "Pending");

            migrationBuilder.AddColumn<DateTime>(
                name: "SellerResponseDate",
                table: "Transactions",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReceiptPath",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SellerStatus",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "SellerResponseDate",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "ReceiptPath",
                table: "Transactions");
        }
    }
}
