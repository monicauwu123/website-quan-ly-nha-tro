using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoAnSE104.Migrations
{
    /// <inheritdoc />
    public partial class AddUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Database của đồ án đã có sẵn các bảng gốc như DichVu, LoaiPhong, Phong, Users...
            // Bản migration cũ từng tạo lại các bảng này nên gây lỗi:
            // "There is already an object named 'DichVu' in the database."
            //
            // Giữ migration này ở dạng no-op để database hiện tại chạy tiếp được.
            // EF vẫn đánh dấu là đã chạy, các migration phía sau tiếp tục xử lý phần còn thiếu.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không drop bảng gốc để tránh mất dữ liệu.
        }
    }
}
