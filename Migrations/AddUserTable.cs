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
            // Database hiện tại của đồ án đã có sẵn các bảng gốc như DichVu, LoaiPhong, Phong, Users...
            // Migration này trước đó tạo lại các bảng gốc nên gây lỗi:
            // "There is already an object named 'DichVu' in the database."
            //
            // Để sửa dứt điểm cho database hiện tại, migration này được để no-op.
            // EF vẫn sẽ đánh dấu migration là đã chạy, sau đó các migration bổ sung phía sau sẽ tiếp tục chạy.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Không drop bảng gốc để tránh mất dữ liệu.
        }
    }
}
