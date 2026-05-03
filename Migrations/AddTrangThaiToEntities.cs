using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddTrangThaiToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NhaTro: thêm TrangThai (HoatDong | NgungHoatDong | DaXoa)
            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "NhaTro",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "HoatDong");

            // LoaiPhong: thêm TrangThai (DangSuDung | NgungSuDung | DaXoa)
            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "LoaiPhong",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "DangSuDung");

            // DichVu: thêm TrangThai (DangSuDung | NgungSuDung | DaXoa)
            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "DichVu",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "DangSuDung");

            // HopDong: thêm TrangThai (DangHieuLuc | KetThuc | Huy)
            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "HopDong",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "DangHieuLuc");

            // HoaDon: thêm TrangThai (ChuaThanhToan | DaThanhToan | Huy)
            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "HoaDon",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "ChuaThanhToan");

            // NguoiThue: thêm TrangThai (DangThue | KhongHoatDong | DaXoa)
            migrationBuilder.AddColumn<string>(
                name: "TrangThai",
                table: "NguoiThue",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "DangThue");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "TrangThai", table: "NhaTro");
            migrationBuilder.DropColumn(name: "TrangThai", table: "LoaiPhong");
            migrationBuilder.DropColumn(name: "TrangThai", table: "DichVu");
            migrationBuilder.DropColumn(name: "TrangThai", table: "HopDong");
            migrationBuilder.DropColumn(name: "TrangThai", table: "HoaDon");
            migrationBuilder.DropColumn(name: "TrangThai", table: "NguoiThue");
        }
    }
}
