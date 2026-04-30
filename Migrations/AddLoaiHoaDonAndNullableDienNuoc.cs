using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddLoaiHoaDonAndNullableDienNuoc")]
    public partial class AddLoaiHoaDonAndNullableDienNuoc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'LoaiHoaDon') IS NULL
BEGIN
    ALTER TABLE [HoaDon] ADD [LoaiHoaDon] nvarchar(20) NOT NULL CONSTRAINT [DF_HoaDon_LoaiHoaDon] DEFAULT N'HangThang';
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'LoaiHoaDon') IS NOT NULL
BEGIN
    UPDATE [HoaDon]
    SET [LoaiHoaDon] = N'HangThang'
    WHERE [LoaiHoaDon] IS NULL OR LTRIM(RTRIM([LoaiHoaDon])) = N'';
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'FK_HoaDon_ChiSoDien_MaDien', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [FK_HoaDon_ChiSoDien_MaDien];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'FK_HoaDon_ChiSoNuoc_MaNuoc', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [FK_HoaDon_ChiSoNuoc_MaNuoc];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'MaDien') IS NOT NULL
BEGIN
    ALTER TABLE [HoaDon] ALTER COLUMN [MaDien] int NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'MaNuoc') IS NOT NULL
BEGIN
    ALTER TABLE [HoaDon] ALTER COLUMN [MaNuoc] int NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL
   AND OBJECT_ID('ChiSoDien', 'U') IS NOT NULL
   AND COL_LENGTH('HoaDon', 'MaDien') IS NOT NULL
   AND OBJECT_ID(N'FK_HoaDon_ChiSoDien_MaDien', N'F') IS NULL
BEGIN
    ALTER TABLE [HoaDon] WITH CHECK ADD CONSTRAINT [FK_HoaDon_ChiSoDien_MaDien]
    FOREIGN KEY([MaDien]) REFERENCES [ChiSoDien] ([MaDien]);
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL
   AND OBJECT_ID('ChiSoNuoc', 'U') IS NOT NULL
   AND COL_LENGTH('HoaDon', 'MaNuoc') IS NOT NULL
   AND OBJECT_ID(N'FK_HoaDon_ChiSoNuoc_MaNuoc', N'F') IS NULL
BEGIN
    ALTER TABLE [HoaDon] WITH CHECK ADD CONSTRAINT [FK_HoaDon_ChiSoNuoc_MaNuoc]
    FOREIGN KEY([MaNuoc]) REFERENCES [ChiSoNuoc] ([MaNuoc]);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'FK_HoaDon_ChiSoDien_MaDien', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [FK_HoaDon_ChiSoDien_MaDien];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'FK_HoaDon_ChiSoNuoc_MaNuoc', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [HoaDon] DROP CONSTRAINT [FK_HoaDon_ChiSoNuoc_MaNuoc];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'MaDien') IS NOT NULL
BEGIN
    UPDATE [HoaDon]
    SET [MaDien] = ISNULL([MaDien], (SELECT TOP 1 [MaDien] FROM [ChiSoDien] ORDER BY [MaDien]))
    WHERE [MaDien] IS NULL;
    ALTER TABLE [HoaDon] ALTER COLUMN [MaDien] int NOT NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'MaNuoc') IS NOT NULL
BEGIN
    UPDATE [HoaDon]
    SET [MaNuoc] = ISNULL([MaNuoc], (SELECT TOP 1 [MaNuoc] FROM [ChiSoNuoc] ORDER BY [MaNuoc]))
    WHERE [MaNuoc] IS NULL;
    ALTER TABLE [HoaDon] ALTER COLUMN [MaNuoc] int NOT NULL;
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'LoaiHoaDon') IS NOT NULL
BEGIN
    DECLARE @constraintName nvarchar(128);
    SELECT @constraintName = dc.name
    FROM sys.default_constraints dc
    JOIN sys.columns c ON c.default_object_id = dc.object_id
    JOIN sys.tables t ON t.object_id = c.object_id
    WHERE t.name = N'HoaDon' AND c.name = N'LoaiHoaDon';

    IF @constraintName IS NOT NULL
    BEGIN
        EXEC(N'ALTER TABLE [HoaDon] DROP CONSTRAINT [' + @constraintName + N']');
    END

    ALTER TABLE [HoaDon] DROP COLUMN [LoaiHoaDon];
END
");
        }
    }
}
