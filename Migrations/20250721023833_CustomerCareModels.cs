using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace shopping_tutorial.Migrations
{
    public partial class CustomerCareModels : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VoucherType",
                table: "CustomerVouchers",
                type: "nvarchar(max)",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VoucherType",
                table: "CustomerVouchers");
        }
    }
}
