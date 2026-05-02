using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddYeuCauGiaHan")]
    public partial class AddYeuCauGiaHan : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[YeuCauGiaHan]', N'U') IS NULL
BEGIN
    CREATE TABLE [YeuCauGiaHan] (
        [MaYeuCauGiaHan] int NOT NULL IDENTITY,
        [MaHopDong] int NOT NULL,
        [MaNguoiDung] int NOT NULL,
        [NgayGui] datetime2 NOT NULL DEFAULT GETDATE(),
        [TrangThai] nvarchar(30) NOT NULL DEFAULT N'ChoDuyet',
        [NgayKetThucCu] datetime2 NULL,
        [NgayKetThucMoiDeXuat] datetime2 NOT NULL,
        [NgayKetThucMoiChuTro] datetime2 NULL,
        [TienCocMoi] decimal(18,2) NULL,
        [NoiDungDieuKhoanMoi] nvarchar(1000) NULL,
        [GhiChuNguoiDung] nvarchar(500) NULL,
        [GhiChuChuTro] nvarchar(500) NULL,
        [NgayXuLy] datetime2 NULL,
        CONSTRAINT [PK_YeuCauGiaHan] PRIMARY KEY ([MaYeuCauGiaHan])
    );
END

IF OBJECT_ID(N'[FK_YeuCauGiaHan_HopDong_MaHopDong]', N'F') IS NULL
BEGIN
    ALTER TABLE [YeuCauGiaHan]
    ADD CONSTRAINT [FK_YeuCauGiaHan_HopDong_MaHopDong]
    FOREIGN KEY ([MaHopDong]) REFERENCES [HopDong]([MaHopDong]);
END

IF OBJECT_ID(N'[FK_YeuCauGiaHan_Users_MaNguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [YeuCauGiaHan]
    ADD CONSTRAINT [FK_YeuCauGiaHan_Users_MaNguoiDung]
    FOREIGN KEY ([MaNguoiDung]) REFERENCES [Users]([MaNguoiDung]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauGiaHan_MaNguoiDung_MaHopDong_TrangThai' AND object_id = OBJECT_ID(N'[YeuCauGiaHan]'))
BEGIN
    CREATE INDEX [IX_YeuCauGiaHan_MaNguoiDung_MaHopDong_TrangThai]
    ON [YeuCauGiaHan] ([MaNguoiDung], [MaHopDong], [TrangThai]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauGiaHan_MaHopDong_TrangThai' AND object_id = OBJECT_ID(N'[YeuCauGiaHan]'))
BEGIN
    CREATE INDEX [IX_YeuCauGiaHan_MaHopDong_TrangThai]
    ON [YeuCauGiaHan] ([MaHopDong], [TrangThai]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[YeuCauGiaHan]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [YeuCauGiaHan];
END
");
        }
    }
}
