using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddThongBaoDaDoc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThongBaoDaDoc",
                columns: table => new
                {
                    ThongBaoDaDocId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ThongBaoId = table.Column<int>(type: "int", nullable: false),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false),
                    NgayDoc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBaoDaDoc", x => x.ThongBaoDaDocId);
                    table.ForeignKey(
                        name: "FK_ThongBaoDaDoc_ThongBao_ThongBaoId",
                        column: x => x.ThongBaoId,
                        principalTable: "ThongBao",
                        principalColumn: "ThongBaoId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ThongBaoDaDoc_Users_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "Users",
                        principalColumn: "MaNguoiDung");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaoDaDoc_MaNguoiDung_NgayDoc",
                table: "ThongBaoDaDoc",
                columns: new[] { "MaNguoiDung", "NgayDoc" });

            migrationBuilder.CreateIndex(
                name: "IX_ThongBaoDaDoc_ThongBaoId_MaNguoiDung",
                table: "ThongBaoDaDoc",
                columns: new[] { "ThongBaoId", "MaNguoiDung" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ThongBaoDaDoc");
        }
    }
}
