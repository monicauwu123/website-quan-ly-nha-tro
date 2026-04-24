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
            migrationBuilder.DropForeignKey(
                name: "FK_NguoiThue_Users_UserMaNguoiDung",
                table: "NguoiThue");

            migrationBuilder.DropForeignKey(
                name: "FK_NhaTro_Users_UserMaNguoiDung",
                table: "NhaTro");

            migrationBuilder.RenameColumn(
                name: "UserMaNguoiDung",
                table: "NhaTro",
                newName: "MaChuTro");

            migrationBuilder.RenameIndex(
                name: "IX_NhaTro_UserMaNguoiDung",
                table: "NhaTro",
                newName: "IX_NhaTro_MaChuTro");

            migrationBuilder.RenameColumn(
                name: "UserMaNguoiDung",
                table: "NguoiThue",
                newName: "MaNguoiDung");

            migrationBuilder.RenameIndex(
                name: "IX_NguoiThue_UserMaNguoiDung",
                table: "NguoiThue",
                newName: "IX_NguoiThue_MaNguoiDung");

            migrationBuilder.AddForeignKey(
                name: "FK_NguoiThue_Users_MaNguoiDung",
                table: "NguoiThue",
                column: "MaNguoiDung",
                principalTable: "Users",
                principalColumn: "MaNguoiDung");

            migrationBuilder.AddForeignKey(
                name: "FK_NhaTro_Users_MaChuTro",
                table: "NhaTro",
                column: "MaChuTro",
                principalTable: "Users",
                principalColumn: "MaNguoiDung");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NguoiThue_Users_MaNguoiDung",
                table: "NguoiThue");

            migrationBuilder.DropForeignKey(
                name: "FK_NhaTro_Users_MaChuTro",
                table: "NhaTro");

            migrationBuilder.RenameColumn(
                name: "MaChuTro",
                table: "NhaTro",
                newName: "UserMaNguoiDung");

            migrationBuilder.RenameIndex(
                name: "IX_NhaTro_MaChuTro",
                table: "NhaTro",
                newName: "IX_NhaTro_UserMaNguoiDung");

            migrationBuilder.RenameColumn(
                name: "MaNguoiDung",
                table: "NguoiThue",
                newName: "UserMaNguoiDung");

            migrationBuilder.RenameIndex(
                name: "IX_NguoiThue_MaNguoiDung",
                table: "NguoiThue",
                newName: "IX_NguoiThue_UserMaNguoiDung");

            migrationBuilder.AddForeignKey(
                name: "FK_NguoiThue_Users_UserMaNguoiDung",
                table: "NguoiThue",
                column: "UserMaNguoiDung",
                principalTable: "Users",
                principalColumn: "MaNguoiDung");

            migrationBuilder.AddForeignKey(
                name: "FK_NhaTro_Users_UserMaNguoiDung",
                table: "NhaTro",
                column: "UserMaNguoiDung",
                principalTable: "Users",
                principalColumn: "MaNguoiDung");
        }
    }
}
