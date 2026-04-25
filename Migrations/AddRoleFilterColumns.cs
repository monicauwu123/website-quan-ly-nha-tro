using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleFilterColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Migration này đổi tên cột FK do EF sinh tự động:
            // NguoiThue.UserMaNguoiDung -> NguoiThue.MaNguoiDung
            // NhaTro.UserMaNguoiDung    -> NhaTro.MaChuTro
            //
            // Một số database đã được chỉnh trước đó nên constraint/index có thể không còn đúng tên cũ.
            // Vì vậy dùng SQL có kiểm tra tồn tại trước khi drop/rename/add để tránh lỗi:
            // "... is not a constraint. Could not drop constraint."

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_NguoiThue_Users_UserMaNguoiDung]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [NguoiThue] DROP CONSTRAINT [FK_NguoiThue_Users_UserMaNguoiDung];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_NhaTro_Users_UserMaNguoiDung]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [NhaTro] DROP CONSTRAINT [FK_NhaTro_Users_UserMaNguoiDung];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NhaTro', 'UserMaNguoiDung') IS NOT NULL
   AND COL_LENGTH('NhaTro', 'MaChuTro') IS NULL
BEGIN
    EXEC sp_rename N'[NhaTro].[UserMaNguoiDung]', N'MaChuTro', N'COLUMN';
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhaTro_UserMaNguoiDung' AND object_id = OBJECT_ID(N'[NhaTro]'))
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhaTro_MaChuTro' AND object_id = OBJECT_ID(N'[NhaTro]'))
BEGIN
    EXEC sp_rename N'[NhaTro].[IX_NhaTro_UserMaNguoiDung]', N'IX_NhaTro_MaChuTro', N'INDEX';
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'UserMaNguoiDung') IS NOT NULL
   AND COL_LENGTH('NguoiThue', 'MaNguoiDung') IS NULL
BEGIN
    EXEC sp_rename N'[NguoiThue].[UserMaNguoiDung]', N'MaNguoiDung', N'COLUMN';
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NguoiThue_UserMaNguoiDung' AND object_id = OBJECT_ID(N'[NguoiThue]'))
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NguoiThue_MaNguoiDung' AND object_id = OBJECT_ID(N'[NguoiThue]'))
BEGIN
    EXEC sp_rename N'[NguoiThue].[IX_NguoiThue_UserMaNguoiDung]', N'IX_NguoiThue_MaNguoiDung', N'INDEX';
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'MaNguoiDung') IS NOT NULL
   AND OBJECT_ID(N'[FK_NguoiThue_Users_MaNguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [NguoiThue]
    ADD CONSTRAINT [FK_NguoiThue_Users_MaNguoiDung]
    FOREIGN KEY ([MaNguoiDung]) REFERENCES [Users] ([MaNguoiDung]);
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NhaTro', 'MaChuTro') IS NOT NULL
   AND OBJECT_ID(N'[FK_NhaTro_Users_MaChuTro]', N'F') IS NULL
BEGIN
    ALTER TABLE [NhaTro]
    ADD CONSTRAINT [FK_NhaTro_Users_MaChuTro]
    FOREIGN KEY ([MaChuTro]) REFERENCES [Users] ([MaNguoiDung]);
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_NguoiThue_Users_MaNguoiDung]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [NguoiThue] DROP CONSTRAINT [FK_NguoiThue_Users_MaNguoiDung];
END
");

            migrationBuilder.Sql(@"
IF OBJECT_ID(N'[FK_NhaTro_Users_MaChuTro]', N'F') IS NOT NULL
BEGIN
    ALTER TABLE [NhaTro] DROP CONSTRAINT [FK_NhaTro_Users_MaChuTro];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NhaTro', 'MaChuTro') IS NOT NULL
   AND COL_LENGTH('NhaTro', 'UserMaNguoiDung') IS NULL
BEGIN
    EXEC sp_rename N'[NhaTro].[MaChuTro]', N'UserMaNguoiDung', N'COLUMN';
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhaTro_MaChuTro' AND object_id = OBJECT_ID(N'[NhaTro]'))
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NhaTro_UserMaNguoiDung' AND object_id = OBJECT_ID(N'[NhaTro]'))
BEGIN
    EXEC sp_rename N'[NhaTro].[IX_NhaTro_MaChuTro]', N'IX_NhaTro_UserMaNguoiDung', N'INDEX';
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'MaNguoiDung') IS NOT NULL
   AND COL_LENGTH('NguoiThue', 'UserMaNguoiDung') IS NULL
BEGIN
    EXEC sp_rename N'[NguoiThue].[MaNguoiDung]', N'UserMaNguoiDung', N'COLUMN';
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NguoiThue_MaNguoiDung' AND object_id = OBJECT_ID(N'[NguoiThue]'))
   AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_NguoiThue_UserMaNguoiDung' AND object_id = OBJECT_ID(N'[NguoiThue]'))
BEGIN
    EXEC sp_rename N'[NguoiThue].[IX_NguoiThue_MaNguoiDung]', N'IX_NguoiThue_UserMaNguoiDung', N'INDEX';
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'UserMaNguoiDung') IS NOT NULL
   AND OBJECT_ID(N'[FK_NguoiThue_Users_UserMaNguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [NguoiThue]
    ADD CONSTRAINT [FK_NguoiThue_Users_UserMaNguoiDung]
    FOREIGN KEY ([UserMaNguoiDung]) REFERENCES [Users] ([MaNguoiDung]);
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NhaTro', 'UserMaNguoiDung') IS NOT NULL
   AND OBJECT_ID(N'[FK_NhaTro_Users_UserMaNguoiDung]', N'F') IS NULL
BEGIN
    ALTER TABLE [NhaTro]
    ADD CONSTRAINT [FK_NhaTro_Users_UserMaNguoiDung]
    FOREIGN KEY ([UserMaNguoiDung]) REFERENCES [Users] ([MaNguoiDung]);
END
");
        }
    }
}
