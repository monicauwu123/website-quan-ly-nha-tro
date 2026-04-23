using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddYeuCauThue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "YeuCauThue",
                columns: table => new
                {
                    MaYeuCau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MaNguoiDung = table.Column<int>(type: "int", nullable: false),
                    MaPhong = table.Column<int>(type: "int", nullable: false),
                    NgayGui = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TrangThai = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    GhiChuNguoiDung = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    GhiChuChuTro = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MaNguoiThue = table.Column<int>(type: "int", nullable: true),
                    MaHopDong = table.Column<int>(type: "int", nullable: true),
                    NgayXuLy = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_YeuCauThue", x => x.MaYeuCau);
                    table.ForeignKey(
                        name: "FK_YeuCauThue_HopDong_MaHopDong",
                        column: x => x.MaHopDong,
                        principalTable: "HopDong",
                        principalColumn: "MaHopDong");
                    table.ForeignKey(
                        name: "FK_YeuCauThue_NguoiThue_MaNguoiThue",
                        column: x => x.MaNguoiThue,
                        principalTable: "NguoiThue",
                        principalColumn: "MaNguoiThue");
                    table.ForeignKey(
                        name: "FK_YeuCauThue_Phong_MaPhong",
                        column: x => x.MaPhong,
                        principalTable: "Phong",
                        principalColumn: "MaPhong");
                    table.ForeignKey(
                        name: "FK_YeuCauThue_Users_MaNguoiDung",
                        column: x => x.MaNguoiDung,
                        principalTable: "Users",
                        principalColumn: "MaNguoiDung");
                });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThue_MaHopDong",
                table: "YeuCauThue",
                column: "MaHopDong");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThue_MaNguoiDung_MaPhong_TrangThai",
                table: "YeuCauThue",
                columns: new[] { "MaNguoiDung", "MaPhong", "TrangThai" });

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThue_MaNguoiThue",
                table: "YeuCauThue",
                column: "MaNguoiThue");

            migrationBuilder.CreateIndex(
                name: "IX_YeuCauThue_MaPhong",
                table: "YeuCauThue",
                column: "MaPhong");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "YeuCauThue");
        }
    }
}
