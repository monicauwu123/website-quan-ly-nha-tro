using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddBaoCaoSuCo")]
    public partial class AddBaoCaoSuCo : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[BaoCaoSuCo]', N'U') IS NULL
BEGIN
    CREATE TABLE [BaoCaoSuCo] (
        [MaBaoCao] int NOT NULL IDENTITY,
        [MaNguoiDung] int NOT NULL,
        [MaPhong] int NOT NULL,
        [TieuDe] nvarchar(150) NOT NULL,
        [NoiDung] nvarchar(1000) NOT NULL,
        [MucDo] nvarchar(30) NOT NULL CONSTRAINT [DF_BaoCaoSuCo_MucDo] DEFAULT N'Bình thường',
        [TrangThai] nvarchar(50) NOT NULL CONSTRAINT [DF_BaoCaoSuCo_TrangThai] DEFAULT N'Moi',
        [NgayGui] datetime2 NOT NULL CONSTRAINT [DF_BaoCaoSuCo_NgayGui] DEFAULT GETDATE(),
        [NgayXuLy] datetime2 NULL,
        [PhanHoiChuTro] nvarchar(1000) NULL,
        CONSTRAINT [PK_BaoCaoSuCo] PRIMARY KEY ([MaBaoCao])
    );
END

IF OBJECT_ID(N'[FK_BaoCaoSuCo_Users_MaNguoiDung]', N'F') IS NULL
AND OBJECT_ID(N'[BaoCaoSuCo]', N'U') IS NOT NULL
AND OBJECT_ID(N'[Users]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [BaoCaoSuCo]
    ADD CONSTRAINT [FK_BaoCaoSuCo_Users_MaNguoiDung]
    FOREIGN KEY ([MaNguoiDung]) REFERENCES [Users] ([MaNguoiDung]);
END

IF OBJECT_ID(N'[FK_BaoCaoSuCo_Phong_MaPhong]', N'F') IS NULL
AND OBJECT_ID(N'[BaoCaoSuCo]', N'U') IS NOT NULL
AND OBJECT_ID(N'[Phong]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [BaoCaoSuCo]
    ADD CONSTRAINT [FK_BaoCaoSuCo_Phong_MaPhong]
    FOREIGN KEY ([MaPhong]) REFERENCES [Phong] ([MaPhong]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaoCaoSuCo_MaNguoiDung_NgayGui' AND object_id = OBJECT_ID(N'[BaoCaoSuCo]'))
BEGIN
    CREATE INDEX [IX_BaoCaoSuCo_MaNguoiDung_NgayGui]
    ON [BaoCaoSuCo] ([MaNguoiDung], [NgayGui]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BaoCaoSuCo_MaPhong_TrangThai' AND object_id = OBJECT_ID(N'[BaoCaoSuCo]'))
BEGIN
    CREATE INDEX [IX_BaoCaoSuCo_MaPhong_TrangThai]
    ON [BaoCaoSuCo] ([MaPhong], [TrangThai]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[BaoCaoSuCo]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [BaoCaoSuCo];
END
");
        }
    }
}
