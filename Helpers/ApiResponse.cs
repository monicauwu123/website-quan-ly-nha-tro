namespace DoAnSE104.Helpers
{
    public class ApiResponse<T>
    {
        public bool ThanhCong { get; set; }
        public string ThongBao { get; set; }
        public T? DuLieu { get; set; }

        public static ApiResponse<T> Ok(T data, string thongBao = "Thành công")
            => new ApiResponse<T> { ThanhCong = true, ThongBao = thongBao, DuLieu = data };

        public static ApiResponse<object> Loi(string thongBao)
            => new ApiResponse<object> { ThanhCong = false, ThongBao = thongBao, DuLieu = null };
    }
}
