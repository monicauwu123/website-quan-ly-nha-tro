using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using DoAnSE104.Data;

#nullable disable

namespace DoAnSE104.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("TachLoaiPhongDichVuTheoChuTro")]
    public partial class TachLoaiPhongDichVuTheoChuTro : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaChuTro",
                table: "LoaiPhong",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaChuTro",
                table: "DichVu",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoaiPhong_MaChuTro",
                table: "LoaiPhong",
                column: "MaChuTro");

            migrationBuilder.CreateIndex(
                name: "IX_DichVu_MaChuTro",
                table: "DichVu",
                column: "MaChuTro");

            migrationBuilder.AddForeignKey(
                name: "FK_LoaiPhong_Users_MaChuTro",
                table: "LoaiPhong",
                column: "MaChuTro",
                principalTable: "Users",
                principalColumn: "MaNguoiDung");

            migrationBuilder.AddForeignKey(
                name: "FK_DichVu_Users_MaChuTro",
                table: "DichVu",
                column: "MaChuTro",
                principalTable: "Users",
                principalColumn: "MaNguoiDung");

            // Tách các loại phòng cũ theo chủ trọ đang sở hữu phòng dùng loại đó.
            migrationBuilder.Sql(@"
                INSERT INTO LoaiPhong (TenLoaiPhong, MoTa, MaChuTro)
                SELECT DISTINCT lp.TenLoaiPhong, lp.MoTa, n.MaChuTro
                FROM LoaiPhong lp
                INNER JOIN Phong p ON p.MaLoaiPhong = lp.MaLoaiPhong
                INNER JOIN NhaTro n ON n.MaNhaTro = p.MaNhaTro
                WHERE lp.MaChuTro IS NULL
                  AND n.MaChuTro IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM LoaiPhong x
                      WHERE x.MaChuTro = n.MaChuTro
                        AND x.TenLoaiPhong = lp.TenLoaiPhong
                        AND ISNULL(x.MoTa, '') = ISNULL(lp.MoTa, '')
                  );
            ");

            // Gắn lại phòng sang bản loại phòng riêng của chủ trọ tương ứng.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.MaLoaiPhong = lpMoi.MaLoaiPhong
                FROM Phong p
                INNER JOIN NhaTro n ON n.MaNhaTro = p.MaNhaTro
                INNER JOIN LoaiPhong lpCu ON lpCu.MaLoaiPhong = p.MaLoaiPhong
                INNER JOIN LoaiPhong lpMoi ON lpMoi.MaChuTro = n.MaChuTro
                    AND lpMoi.TenLoaiPhong = lpCu.TenLoaiPhong
                    AND ISNULL(lpMoi.MoTa, '') = ISNULL(lpCu.MoTa, '')
                WHERE lpCu.MaChuTro IS NULL
                  AND n.MaChuTro IS NOT NULL;
            ");

            // Tách các dịch vụ cũ thành bản riêng cho từng chủ trọ.
            migrationBuilder.Sql(@"
                INSERT INTO DichVu (TenDichVu, Tiendichvu, MaChuTro)
                SELECT d.TenDichVu, d.Tiendichvu, u.MaNguoiDung
                FROM DichVu d
                CROSS JOIN Users u
                WHERE d.MaChuTro IS NULL
                  AND u.VaiTro = 'ChuTro'
                  AND NOT EXISTS (
                      SELECT 1
                      FROM DichVu x
                      WHERE x.MaChuTro = u.MaNguoiDung
                        AND x.TenDichVu = d.TenDichVu
                  );
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LoaiPhong_Users_MaChuTro",
                table: "LoaiPhong");

            migrationBuilder.DropForeignKey(
                name: "FK_DichVu_Users_MaChuTro",
                table: "DichVu");

            migrationBuilder.DropIndex(
                name: "IX_LoaiPhong_MaChuTro",
                table: "LoaiPhong");

            migrationBuilder.DropIndex(
                name: "IX_DichVu_MaChuTro",
                table: "DichVu");

            migrationBuilder.DropColumn(
                name: "MaChuTro",
                table: "LoaiPhong");

            migrationBuilder.DropColumn(
                name: "MaChuTro",
                table: "DichVu");
        }
    }
}
