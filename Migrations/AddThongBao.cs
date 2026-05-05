using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddThongBao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ThongBao",
                columns: table => new
                {
                    ThongBaoId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TieuDe = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NoiDung = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    LoaiThongBao = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "ThuCong"),
                    LoaiNguoiNhan = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "TatCa"),
                    NguoiNhanId = table.Column<int>(type: "int", nullable: true),
                    PhongId = table.Column<int>(type: "int", nullable: true),
                    NguoiTaoId = table.Column<int>(type: "int", nullable: true),
                    DaDoc = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    NgayDoc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NgayTao = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    TrangThai = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "HienThi")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ThongBao", x => x.ThongBaoId);
                    table.ForeignKey(
                        name: "FK_ThongBao_Users_NguoiNhanId",
                        column: x => x.NguoiNhanId,
                        principalTable: "Users",
                        principalColumn: "MaNguoiDung");
                    table.ForeignKey(
                        name: "FK_ThongBao_Phong_PhongId",
                        column: x => x.PhongId,
                        principalTable: "Phong",
                        principalColumn: "MaPhong");
                    table.ForeignKey(
                        name: "FK_ThongBao_Users_NguoiTaoId",
                        column: x => x.NguoiTaoId,
                        principalTable: "Users",
                        principalColumn: "MaNguoiDung");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NguoiNhanId",
                table: "ThongBao",
                column: "NguoiNhanId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_PhongId",
                table: "ThongBao",
                column: "PhongId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_NguoiTaoId",
                table: "ThongBao",
                column: "NguoiTaoId");

            migrationBuilder.CreateIndex(
                name: "IX_ThongBao_DaDoc_TrangThai",
                table: "ThongBao",
                columns: new[] { "DaDoc", "TrangThai" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ThongBao");
        }
    }
}
