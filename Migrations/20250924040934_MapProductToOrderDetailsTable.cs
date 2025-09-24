using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HandyBackend.Migrations
{
    /// <inheritdoc />
    public partial class MapProductToOrderDetailsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Products",
                table: "Products");

            migrationBuilder.RenameTable(
                name: "Products",
                newName: "orderdetails");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "orderdetails",
                newName: "SalesQuantity");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "orderdetails",
                newName: "OrderDetailId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "orderdetails",
                newName: "OrderDetailID");

            migrationBuilder.AlterColumn<double>(
                name: "Amount",
                table: "orderdetails",
                type: "double",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddPrimaryKey(
                name: "PK_orderdetails",
                table: "orderdetails",
                column: "OrderDetailID");

            migrationBuilder.CreateIndex(
                name: "IX_orderdetails_OrderDetailId",
                table: "orderdetails",
                column: "OrderDetailId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_orderdetails",
                table: "orderdetails");

            migrationBuilder.DropIndex(
                name: "IX_orderdetails_OrderDetailId",
                table: "orderdetails");

            migrationBuilder.RenameTable(
                name: "orderdetails",
                newName: "Products");

            migrationBuilder.RenameColumn(
                name: "SalesQuantity",
                table: "Products",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "OrderDetailId",
                table: "Products",
                newName: "Name");

            migrationBuilder.RenameColumn(
                name: "OrderDetailID",
                table: "Products",
                newName: "Id");

            migrationBuilder.AlterColumn<int>(
                name: "Amount",
                table: "Products",
                type: "int",
                nullable: false,
                oldClrType: typeof(double),
                oldType: "double");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Products",
                table: "Products",
                column: "Id");
        }
    }
}
