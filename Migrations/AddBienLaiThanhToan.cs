using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddBienLaiThanhToan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HinhAnhBienLai",
                table: "ThanhToan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MaGiaoDich",
                table: "ThanhToan",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TrangThaiXacNhan",
                table: "ThanhToan",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LyDoTuChoi",
                table: "ThanhToan",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NguoiXacNhanId",
                table: "ThanhToan",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NgayXacNhan",
                table: "ThanhToan",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ThanhToan_NguoiXacNhanId",
                table: "ThanhToan",
                column: "NguoiXacNhanId");

            migrationBuilder.AddForeignKey(
                name: "FK_ThanhToan_Users_NguoiXacNhanId",
                table: "ThanhToan",
                column: "NguoiXacNhanId",
                principalTable: "Users",
                principalColumn: "MaNguoiDung",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ThanhToan_Users_NguoiXacNhanId",
                table: "ThanhToan");

            migrationBuilder.DropIndex(
                name: "IX_ThanhToan_NguoiXacNhanId",
                table: "ThanhToan");

            migrationBuilder.DropColumn(name: "HinhAnhBienLai", table: "ThanhToan");
            migrationBuilder.DropColumn(name: "MaGiaoDich", table: "ThanhToan");
            migrationBuilder.DropColumn(name: "TrangThaiXacNhan", table: "ThanhToan");
            migrationBuilder.DropColumn(name: "LyDoTuChoi", table: "ThanhToan");
            migrationBuilder.DropColumn(name: "NguoiXacNhanId", table: "ThanhToan");
            migrationBuilder.DropColumn(name: "NgayXacNhan", table: "ThanhToan");
        }
    }
}
