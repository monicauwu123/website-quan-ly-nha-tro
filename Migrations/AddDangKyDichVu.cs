using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddDangKyDichVu")]
    public partial class AddDangKyDichVu : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[DangKyDichVu]', N'U') IS NULL
BEGIN
    CREATE TABLE [DangKyDichVu] (
        [MaDangKyDichVu] int NOT NULL IDENTITY,
        [MaNguoiDung] int NOT NULL,
        [MaPhong] int NOT NULL,
        [MaDichVu] int NOT NULL,
        [MaNguoiThue] int NULL,
        [NgayDangKy] datetime2 NOT NULL DEFAULT GETDATE(),
        [NgayHuy] datetime2 NULL,
        [TrangThai] nvarchar(30) NOT NULL DEFAULT N'DangSuDung',
        [GhiChu] nvarchar(500) NULL,
        CONSTRAINT [PK_DangKyDichVu] PRIMARY KEY ([MaDangKyDichVu])
    );
END

IF OBJECT_ID(N'[FK_DangKyDichVu_Users_MaNguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [DangKyDichVu]
    ADD CONSTRAINT [FK_DangKyDichVu_Users_MaNguoiDung]
    FOREIGN KEY ([MaNguoiDung]) REFERENCES [Users]([MaNguoiDung]);
END

IF OBJECT_ID(N'[FK_DangKyDichVu_Phong_MaPhong]', N'F') IS NULL
BEGIN
    ALTER TABLE [DangKyDichVu]
    ADD CONSTRAINT [FK_DangKyDichVu_Phong_MaPhong]
    FOREIGN KEY ([MaPhong]) REFERENCES [Phong]([MaPhong]);
END

IF OBJECT_ID(N'[FK_DangKyDichVu_DichVu_MaDichVu]', N'F') IS NULL
BEGIN
    ALTER TABLE [DangKyDichVu]
    ADD CONSTRAINT [FK_DangKyDichVu_DichVu_MaDichVu]
    FOREIGN KEY ([MaDichVu]) REFERENCES [DichVu]([MaDichVu]);
END

IF OBJECT_ID(N'[FK_DangKyDichVu_NguoiThue_MaNguoiThue]', N'F') IS NULL
BEGIN
    ALTER TABLE [DangKyDichVu]
    ADD CONSTRAINT [FK_DangKyDichVu_NguoiThue_MaNguoiThue]
    FOREIGN KEY ([MaNguoiThue]) REFERENCES [NguoiThue]([MaNguoiThue]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DangKyDichVu_MaNguoiDung_MaPhong_MaDichVu_TrangThai' AND object_id = OBJECT_ID(N'[DangKyDichVu]'))
BEGIN
    CREATE INDEX [IX_DangKyDichVu_MaNguoiDung_MaPhong_MaDichVu_TrangThai]
    ON [DangKyDichVu] ([MaNguoiDung], [MaPhong], [MaDichVu], [TrangThai]);
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_DangKyDichVu_MaPhong_TrangThai' AND object_id = OBJECT_ID(N'[DangKyDichVu]'))
BEGIN
    CREATE INDEX [IX_DangKyDichVu_MaPhong_TrangThai]
    ON [DangKyDichVu] ([MaPhong], [TrangThai]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[DangKyDichVu]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [DangKyDichVu];
END
");
        }
    }
}
