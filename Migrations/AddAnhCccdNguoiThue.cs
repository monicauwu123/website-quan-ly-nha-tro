using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("AddAnhCccdNguoiThue")]
    public partial class AddAnhCccdNguoiThue : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'AnhCccdMatTruoc') IS NULL
BEGIN
    ALTER TABLE [NguoiThue] ADD [AnhCccdMatTruoc] nvarchar(500) NULL;
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'AnhCccdMatSau') IS NULL
BEGIN
    ALTER TABLE [NguoiThue] ADD [AnhCccdMatSau] nvarchar(500) NULL;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'AnhCccdMatTruoc') IS NOT NULL
BEGIN
    ALTER TABLE [NguoiThue] DROP COLUMN [AnhCccdMatTruoc];
END
");

            migrationBuilder.Sql(@"
IF COL_LENGTH('NguoiThue', 'AnhCccdMatSau') IS NOT NULL
BEGIN
    ALTER TABLE [NguoiThue] DROP COLUMN [AnhCccdMatSau];
END
");
        }
    }
}
