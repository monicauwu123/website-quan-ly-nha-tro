using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("TachLoaiPhongDichVuTheoChuTro")]
    public partial class TachLoaiPhongDichVuTheoChuTro : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('LoaiPhong', 'MaChuTro') IS NULL
BEGIN
    ALTER TABLE [LoaiPhong] ADD [MaChuTro] int NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('DichVu', 'MaChuTro') IS NULL
BEGIN
    ALTER TABLE [DichVu] ADD [MaChuTro] int NULL;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoaiPhong_MaChuTro' AND object_id = OBJECT_ID(N'[LoaiPhong]'))
BEGIN
    CREATE INDEX [IX_LoaiPhong_MaChuTro] ON [LoaiPhong] ([MaChuTro]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DichVu_MaChuTro' AND object_id = OBJECT_ID(N'[DichVu]'))
BEGIN
    CREATE INDEX [IX_DichVu_MaChuTro] ON [DichVu] ([MaChuTro]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_LoaiPhong_Users_MaChuTro]', N'F') IS NULL
   AND COL_LENGTH('LoaiPhong', 'MaChuTro') IS NOT NULL
   AND OBJECT_ID(N'[Users]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [LoaiPhong]
    ADD CONSTRAINT [FK_LoaiPhong_Users_MaChuTro]
    FOREIGN KEY ([MaChuTro]) REFERENCES [Users] ([MaNguoiDung]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_DichVu_Users_MaChuTro]', N'F') IS NULL
   AND COL_LENGTH('DichVu', 'MaChuTro') IS NOT NULL
   AND OBJECT_ID(N'[Users]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [DichVu]
    ADD CONSTRAINT [FK_DichVu_Users_MaChuTro]
    FOREIGN KEY ([MaChuTro]) REFERENCES [Users] ([MaNguoiDung]);
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('LoaiPhong', 'MaChuTro') IS NOT NULL
   AND COL_LENGTH('NhaTro', 'MaChuTro') IS NOT NULL
BEGIN
    INSERT INTO LoaiPhong (TenLoaiPhong, MoTa, MaChuTro)
    SELECT DISTINCT lp.TenLoaiPhong, lp.MoTa, n.MaChuTro
    FROM LoaiPhong lp
    INNER JOIN Phong p ON p.MaLoaiPhong = lp.MaLoaiPhong
    INNER JOIN NhaTro n ON n.MaNhaTro = p.MaNhaTro
    WHERE lp.MaChuTro IS NULL
      AND n.MaChuTro IS NOT NULL
      AND NOT EXISTS (
          SELECT 1
          FROM LoaiPhong x
          WHERE x.MaChuTro = n.MaChuTro
            AND x.TenLoaiPhong = lp.TenLoaiPhong
            AND ISNULL(x.MoTa, '') = ISNULL(lp.MoTa, '')
      );
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('LoaiPhong', 'MaChuTro') IS NOT NULL
   AND COL_LENGTH('NhaTro', 'MaChuTro') IS NOT NULL
BEGIN
    UPDATE p
    SET p.MaLoaiPhong = lpMoi.MaLoaiPhong
    FROM Phong p
    INNER JOIN NhaTro n ON n.MaNhaTro = p.MaNhaTro
    INNER JOIN LoaiPhong lpCu ON lpCu.MaLoaiPhong = p.MaLoaiPhong
    INNER JOIN LoaiPhong lpMoi ON lpMoi.MaChuTro = n.MaChuTro
        AND lpMoi.TenLoaiPhong = lpCu.TenLoaiPhong
        AND ISNULL(lpMoi.MoTa, '') = ISNULL(lpCu.MoTa, '')
    WHERE lpCu.MaChuTro IS NULL
      AND n.MaChuTro IS NOT NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('DichVu', 'MaChuTro') IS NOT NULL
BEGIN
    INSERT INTO DichVu (TenDichVu, Tiendichvu, MaChuTro)
    SELECT d.TenDichVu, d.Tiendichvu, u.MaNguoiDung
    FROM DichVu d
    CROSS JOIN Users u
    WHERE d.MaChuTro IS NULL
      AND u.VaiTro = 'ChuTro'
      AND NOT EXISTS (
          SELECT 1
          FROM DichVu x
          WHERE x.MaChuTro = u.MaNguoiDung
            AND x.TenDichVu = d.TenDichVu
      );
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_LoaiPhong_Users_MaChuTro]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [LoaiPhong] DROP CONSTRAINT [FK_LoaiPhong_Users_MaChuTro];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_DichVu_Users_MaChuTro]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [DichVu] DROP CONSTRAINT [FK_DichVu_Users_MaChuTro];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoaiPhong_MaChuTro' AND object_id = OBJECT_ID(N'[LoaiPhong]'))
BEGIN
    DROP INDEX [IX_LoaiPhong_MaChuTro] ON [LoaiPhong];
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DichVu_MaChuTro' AND object_id = OBJECT_ID(N'[DichVu]'))
BEGIN
    DROP INDEX [IX_DichVu_MaChuTro] ON [DichVu];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('LoaiPhong', 'MaChuTro') IS NOT NULL
BEGIN
    ALTER TABLE [LoaiPhong] DROP COLUMN [MaChuTro];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('DichVu', 'MaChuTro') IS NOT NULL
BEGIN
    ALTER TABLE [DichVu] DROP COLUMN [MaChuTro];
END
");
        }
    }
}
