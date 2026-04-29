using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddThongTinThanhToanChuTro")]
    public partial class AddThongTinThanhToanChuTro : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'TenNganHang') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [TenNganHang] nvarchar(100) NULL;
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'MaNganHang') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [MaNganHang] nvarchar(50) NULL;
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'SoTaiKhoan') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [SoTaiKhoan] nvarchar(50) NULL;
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'TenChuTaiKhoan') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [TenChuTaiKhoan] nvarchar(100) NULL;
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'NoiDungChuyenKhoanMacDinh') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [NoiDungChuyenKhoanMacDinh] nvarchar(255) NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'NoiDungChuyenKhoanMacDinh') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [NoiDungChuyenKhoanMacDinh];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'TenChuTaiKhoan') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [TenChuTaiKhoan];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'SoTaiKhoan') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [SoTaiKhoan];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'MaNganHang') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [MaNganHang];
END
");
            migrationBuilder.Sql(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL AND COL_LENGTH('Users', 'TenNganHang') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [TenNganHang];
END
");
        }
    }
}
