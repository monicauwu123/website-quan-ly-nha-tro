using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddYeuCauThue")]
    public partial class AddYeuCauThue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[YeuCauThue]', N'U') IS NULL
BEGIN
    CREATE TABLE [YeuCauThue] (
        [MaYeuCau] int NOT NULL IDENTITY,
        [MaNguoiDung] int NOT NULL,
        [MaPhong] int NOT NULL,
        [NgayGui] datetime2 NOT NULL,
        [TrangThai] nvarchar(30) NOT NULL,
        [GhiChuNguoiDung] nvarchar(255) NULL,
        [GhiChuChuTro] nvarchar(255) NULL,
        [MaNguoiThue] int NULL,
        [MaHopDong] int NULL,
        [NgayXuLy] datetime2 NULL,
        CONSTRAINT [PK_YeuCauThue] PRIMARY KEY ([MaYeuCau])
    );
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_YeuCauThue_HopDong_MaHopDong]', N'F') IS NULL
   AND OBJECT_ID(N'[YeuCauThue]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[HopDong]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [YeuCauThue]
    ADD CONSTRAINT [FK_YeuCauThue_HopDong_MaHopDong]
    FOREIGN KEY ([MaHopDong]) REFERENCES [HopDong] ([MaHopDong]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_YeuCauThue_NguoiThue_MaNguoiThue]', N'F') IS NULL
   AND OBJECT_ID(N'[YeuCauThue]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[NguoiThue]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [YeuCauThue]
    ADD CONSTRAINT [FK_YeuCauThue_NguoiThue_MaNguoiThue]
    FOREIGN KEY ([MaNguoiThue]) REFERENCES [NguoiThue] ([MaNguoiThue]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_YeuCauThue_Phong_MaPhong]', N'F') IS NULL
   AND OBJECT_ID(N'[YeuCauThue]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[Phong]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [YeuCauThue]
    ADD CONSTRAINT [FK_YeuCauThue_Phong_MaPhong]
    FOREIGN KEY ([MaPhong]) REFERENCES [Phong] ([MaPhong]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_YeuCauThue_Users_MaNguoiDung]', N'F') IS NULL
   AND OBJECT_ID(N'[YeuCauThue]', N'U') IS NOT NULL
   AND OBJECT_ID(N'[Users]', N'U') IS NOT NULL
BEGIN
    ALTER TABLE [YeuCauThue]
    ADD CONSTRAINT [FK_YeuCauThue_Users_MaNguoiDung]
    FOREIGN KEY ([MaNguoiDung]) REFERENCES [Users] ([MaNguoiDung]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThue_MaHopDong' AND object_id = OBJECT_ID(N'[YeuCauThue]'))
BEGIN
    CREATE INDEX [IX_YeuCauThue_MaHopDong] ON [YeuCauThue] ([MaHopDong]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThue_MaNguoiDung_MaPhong_TrangThai' AND object_id = OBJECT_ID(N'[YeuCauThue]'))
BEGIN
    CREATE INDEX [IX_YeuCauThue_MaNguoiDung_MaPhong_TrangThai] ON [YeuCauThue] ([MaNguoiDung], [MaPhong], [TrangThai]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThue_MaNguoiThue' AND object_id = OBJECT_ID(N'[YeuCauThue]'))
BEGIN
    CREATE INDEX [IX_YeuCauThue_MaNguoiThue] ON [YeuCauThue] ([MaNguoiThue]);
END
");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_YeuCauThue_MaPhong' AND object_id = OBJECT_ID(N'[YeuCauThue]'))
BEGIN
    CREATE INDEX [IX_YeuCauThue_MaPhong] ON [YeuCauThue] ([MaPhong]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[YeuCauThue]', N'U') IS NOT NULL
BEGIN
    DROP TABLE [YeuCauThue];
END
");
        }
    }
}
