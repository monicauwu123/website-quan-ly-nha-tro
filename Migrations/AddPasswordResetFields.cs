using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'PasswordResetToken') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetToken] nvarchar(200) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'PasswordResetTokenExpiry') IS NULL
BEGIN
    ALTER TABLE [Users] ADD [PasswordResetTokenExpiry] datetime2 NULL;
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'PasswordResetToken') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [PasswordResetToken];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('Users', 'PasswordResetTokenExpiry') IS NOT NULL
BEGIN
    ALTER TABLE [Users] DROP COLUMN [PasswordResetTokenExpiry];
END
");
        }
    }
}
