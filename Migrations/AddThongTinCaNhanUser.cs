using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddThongTinCaNhanUser")]
    public partial class AddThongTinCaNhanUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'CCCD') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [CCCD] nvarchar(20) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'NgaySinh') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [NgaySinh] datetime2 NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'GioiTinh') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [GioiTinh] nvarchar(10) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'QuocTich') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [QuocTich] nvarchar(50) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'DiaChi') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [DiaChi] nvarchar(255) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'NoiCongTac') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [NoiCongTac] nvarchar(100) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'AnhCccdMatTruoc') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [AnhCccdMatTruoc] nvarchar(500) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'AnhCccdMatSau') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [AnhCccdMatSau] nvarchar(500) NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'AnhCccdMatSau') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [AnhCccdMatSau];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'AnhCccdMatTruoc') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [AnhCccdMatTruoc];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'NoiCongTac') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [NoiCongTac];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'DiaChi') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [DiaChi];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'QuocTich') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [QuocTich];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'GioiTinh') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [GioiTinh];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'NgaySinh') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [NgaySinh];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'CCCD') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [CCCD];
END
");
        }
    }
}
