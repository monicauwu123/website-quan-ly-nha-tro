using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddNhaTroForLoaiPhongDichVu")]
    public partial class AddNhaTroForLoaiPhongDichVu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('LoaiPhong', 'MaNhaTro') IS NULL
BEGIN
    ALTER TABLE [LoaiPhong] ADD [MaNhaTro] int NULL;
END

IF COL_LENGTH('DichVu', 'MaNhaTro') IS NULL
BEGIN
    ALTER TABLE [DichVu] ADD [MaNhaTro] int NULL;
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoaiPhong_MaNhaTro' AND object_id = OBJECT_ID(N'[LoaiPhong]'))
BEGIN
    CREATE INDEX [IX_LoaiPhong_MaNhaTro] ON [LoaiPhong] ([MaNhaTro]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DichVu_MaNhaTro' AND object_id = OBJECT_ID(N'[DichVu]'))
BEGIN
    CREATE INDEX [IX_DichVu_MaNhaTro] ON [DichVu] ([MaNhaTro]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_LoaiPhong_NhaTro_MaNhaTro]', N'F') IS NULL
   AND COL_LENGTH('LoaiPhong', 'MaNhaTro') IS NOT NULL
   AND OBJECT_ID(N'[NhaTro]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [LoaiPhong]
    ADD CONSTRAINT [FK_LoaiPhong_NhaTro_MaNhaTro]
    FOREIGN KEY ([MaNhaTro]) REFERENCES [NhaTro] ([MaNhaTro]);
END

IF OBJECT_ID(N'[FK_DichVu_NhaTro_MaNhaTro]', N'F') IS NULL
   AND COL_LENGTH('DichVu', 'MaNhaTro') IS NOT NULL
   AND OBJECT_ID(N'[NhaTro]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [DichVu]
    ADD CONSTRAINT [FK_DichVu_NhaTro_MaNhaTro]
    FOREIGN KEY ([MaNhaTro]) REFERENCES [NhaTro] ([MaNhaTro]);
END
");

            // Loại phòng: tách theo từng nhà trọ đang sử dụng loại phòng đó, sau đó gắn lại phòng về loại mới theo nhà trọ.
            migrationBuilder.Sql(@"
IF COL_LENGTH('LoaiPhong', 'MaNhaTro') IS NOT NULL
   AND OBJECT_ID(N'[Phong]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[NhaTro]', N'U') IS NOT NULL
BEGIN
    INSERT INTO LoaiPhong (TenLoaiPhong, MoTa, MaChuTro, MaNhaTro)
    SELECT DISTINCT lp.TenLoaiPhong, lp.MoTa, n.MaChuTro, p.MaNhaTro
    FROM Phong p
    INNER JOIN LoaiPhong lp ON lp.MaLoaiPhong = p.MaLoaiPhong
    INNER JOIN NhaTro n ON n.MaNhaTro = p.MaNhaTro
    WHERE lp.MaNhaTro IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM LoaiPhong x
          WHERE x.MaNhaTro = p.MaNhaTro
            AND x.TenLoaiPhong = lp.TenLoaiPhong
            AND ISNULL(x.MoTa, '') = ISNULL(lp.MoTa, '')
      );

    UPDATE p
    SET p.MaLoaiPhong = lpMoi.MaLoaiPhong
    FROM Phong p
    INNER JOIN LoaiPhong lpCu ON lpCu.MaLoaiPhong = p.MaLoaiPhong
    INNER JOIN LoaiPhong lpMoi ON lpMoi.MaNhaTro = p.MaNhaTro
        AND lpMoi.TenLoaiPhong = lpCu.TenLoaiPhong
        AND ISNULL(lpMoi.MoTa, '') = ISNULL(lpCu.MoTa, '')
    WHERE lpCu.MaNhaTro IS NULL;

    UPDATE lp
    SET lp.MaNhaTro = n.MaNhaTro,
        lp.MaChuTro = n.MaChuTro
    FROM LoaiPhong lp
    INNER JOIN NhaTro n ON n.MaChuTro = lp.MaChuTro
    WHERE lp.MaNhaTro IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM LoaiPhong x
          WHERE x.MaNhaTro = n.MaNhaTro
            AND x.TenLoaiPhong = lp.TenLoaiPhong
            AND ISNULL(x.MoTa, '') = ISNULL(lp.MoTa, '')
      )
      AND n.MaNhaTro = (
          SELECT TOP 1 n2.MaNhaTro FROM NhaTro n2 WHERE n2.MaChuTro = lp.MaChuTro ORDER BY n2.MaNhaTro
      );
END
");

            // Dịch vụ: nhân bản dịch vụ hiện có của chủ trọ cho từng nhà trọ của chủ trọ đó.
            migrationBuilder.Sql(@"
IF COL_LENGTH('DichVu', 'MaNhaTro') IS NOT NULL
   AND OBJECT_ID(N'[NhaTro]', N'U') IS NOT NULL
BEGIN
    INSERT INTO DichVu (TenDichVu, Tiendichvu, MaChuTro, MaNhaTro)
    SELECT dv.TenDichVu, dv.Tiendichvu, n.MaChuTro, n.MaNhaTro
    FROM DichVu dv
    INNER JOIN NhaTro n ON n.MaChuTro = dv.MaChuTro
    WHERE dv.MaNhaTro IS NULL
      AND NOT EXISTS (
          SELECT 1 FROM DichVu x
          WHERE x.MaNhaTro = n.MaNhaTro
            AND x.TenDichVu = dv.TenDichVu
      );

    UPDATE dv
    SET dv.MaNhaTro = n.MaNhaTro,
        dv.MaChuTro = n.MaChuTro
    FROM DichVu dv
    INNER JOIN NhaTro n ON n.MaChuTro = dv.MaChuTro
    WHERE dv.MaNhaTro IS NULL
      AND n.MaNhaTro = (
          SELECT TOP 1 n2.MaNhaTro FROM NhaTro n2 WHERE n2.MaChuTro = dv.MaChuTro ORDER BY n2.MaNhaTro
      );

    IF OBJECT_ID(N'[DangKyDichVu]', N'U') IS NOT NULL AND OBJECT_ID(N'[Phong]', N'U') IS NOT NULL
    BEGIN
        UPDATE dk
        SET dk.MaDichVu = dvMoi.MaDichVu
        FROM DangKyDichVu dk
        INNER JOIN Phong p ON p.MaPhong = dk.MaPhong
        INNER JOIN DichVu dvCu ON dvCu.MaDichVu = dk.MaDichVu
        INNER JOIN DichVu dvMoi ON dvMoi.MaNhaTro = p.MaNhaTro
            AND dvMoi.TenDichVu = dvCu.TenDichVu
        WHERE ISNULL(dvCu.MaNhaTro, -1) <> p.MaNhaTro;
    END
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_LoaiPhong_NhaTro_MaNhaTro]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [LoaiPhong] DROP CONSTRAINT [FK_LoaiPhong_NhaTro_MaNhaTro];
END

IF OBJECT_ID(N'[FK_DichVu_NhaTro_MaNhaTro]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [DichVu] DROP CONSTRAINT [FK_DichVu_NhaTro_MaNhaTro];
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_LoaiPhong_MaNhaTro' AND object_id = OBJECT_ID(N'[LoaiPhong]'))
BEGIN
    DROP INDEX [IX_LoaiPhong_MaNhaTro] ON [LoaiPhong];
END

IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DichVu_MaNhaTro' AND object_id = OBJECT_ID(N'[DichVu]'))
BEGIN
    DROP INDEX [IX_DichVu_MaNhaTro] ON [DichVu];
END

IF COL_LENGTH('LoaiPhong', 'MaNhaTro') IS NOT NULL
BEGIN
    ALTER TABLE [LoaiPhong] DROP COLUMN [MaNhaTro];
END

IF COL_LENGTH('DichVu', 'MaNhaTro') IS NOT NULL
BEGIN
    ALTER TABLE [DichVu] DROP COLUMN [MaNhaTro];
END
");
        }
    }
}
