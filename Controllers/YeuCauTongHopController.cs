using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Helpers;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class YeuCauTongHopController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public YeuCauTongHopController(ApplicationDbContext context) => _context = context;

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("VaiTro") ?? string.Empty;

        [HttpGet]
        public async Task<IActionResult> GetTongHop()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var thueQuery = _context.YeuCauThue
                .Include(y => y.NguoiDung)
                .Include(y => y.Phong).ThenInclude(p => p.NhaTro)
                .Include(y => y.HopDong)
                .AsQueryable();

            if (role == VaiTroConst.NguoiDung)
                thueQuery = thueQuery.Where(y => y.MaNguoiDung == userId);
            else if (role == VaiTroConst.ChuTro)
                thueQuery = thueQuery.Where(y => y.Phong.NhaTro.MaChuTro == userId);

            var thue = await thueQuery.ToListAsync();

            var giaHanQuery = _context.YeuCauGiaHan
                .Include(y => y.NguoiDung)
                .Include(y => y.HopDong).ThenInclude(h => h.NguoiThue)
                .Include(y => y.HopDong).ThenInclude(h => h.Phong).ThenInclude(p => p.NhaTro)
                .AsQueryable();

            if (role == VaiTroConst.NguoiDung)
                giaHanQuery = giaHanQuery.Where(y => y.MaNguoiDung == userId);
            else if (role == VaiTroConst.ChuTro)
                giaHanQuery = giaHanQuery.Where(y => y.HopDong.Phong.NhaTro.MaChuTro == userId);

            var giaHan = await giaHanQuery.ToListAsync();

            var result = new List<object>();

            result.AddRange(thue.Select(y => new
            {
                IdTongHop = $"Thue_{y.MaYeuCau}",
                LoaiYeuCau = "Thue",
                LoaiYeuCauText = "Thuê phòng",
                y.MaYeuCau,
                MaYeuCauGiaHan = (int?)null,
                y.MaPhong,
                MaHopDong = y.MaHopDong,
                y.MaNguoiDung,
                y.NgayGui,
                y.NgayXuLy,
                y.TrangThai,
                TrangThaiText = y.TrangThai switch
                {
                    "ChoDuyet" => "Chờ duyệt",
                    "DaChapNhan" => "Đã chấp nhận",
                    "ChoNguoiThueXacNhan" => "Chờ người thuê xác nhận",
                    "DaLapHopDong" => "Đã lập hợp đồng",
                    "NguoiThueTuChoi" => "Người thuê từ chối hợp đồng",
                    "TuChoi" => "Từ chối",
                    _ => y.TrangThai
                },
                y.GhiChuNguoiDung,
                y.GhiChuChuTro,
                y.SoThangMuonThue,
                y.NgayBatDauMongMuon,
                HopDong = y.HopDong == null ? null : new
                {
                    y.HopDong.MaHopDong,
                    y.HopDong.NgayBatDau,
                    y.HopDong.NgayKetThuc,
                    y.HopDong.TienCoc,
                    y.HopDong.NoiDung,
                    y.HopDong.TrangThai
                },
                NgayKetThucCu = (DateTime?)null,
                NgayKetThucMoiDeXuat = (DateTime?)null,
                NgayKetThucMoi = (DateTime?)null,
                NguoiDung = new { y.NguoiDung.MaNguoiDung, y.NguoiDung.HoTen, y.NguoiDung.Email, y.NguoiDung.SoDienThoai },
                Phong = new
                {
                    y.Phong.MaPhong,
                    y.Phong.TenPhong,
                    NhaTro = y.Phong.NhaTro == null ? null : new { y.Phong.NhaTro.MaNhaTro, y.Phong.NhaTro.TenNhaTro }
                }
            }));

            result.AddRange(giaHan.Select(y => new
            {
                IdTongHop = $"GiaHan_{y.MaYeuCauGiaHan}",
                LoaiYeuCau = "GiaHan",
                LoaiYeuCauText = "Gia hạn hợp đồng",
                MaYeuCau = y.MaYeuCauGiaHan,
                y.MaYeuCauGiaHan,
                MaPhong = y.HopDong.MaPhong,
                y.MaHopDong,
                y.MaNguoiDung,
                y.NgayGui,
                y.NgayXuLy,
                y.TrangThai,
                TrangThaiText = y.TrangThai switch
                {
                    "ChoDuyet" => "Chờ duyệt",
                    "DaChapNhan" => "Đã chấp nhận",
                    "TuChoi" => "Từ chối",
                    _ => y.TrangThai
                },
                y.GhiChuNguoiDung,
                y.GhiChuChuTro,
                y.NgayKetThucCu,
                y.NgayKetThucMoiDeXuat,
                NgayKetThucMoi = y.NgayKetThucMoiChuTro ?? y.NgayKetThucMoiDeXuat,
                y.TienCocMoi,
                y.NoiDungDieuKhoanMoi,
                NguoiDung = new { y.NguoiDung.MaNguoiDung, y.NguoiDung.HoTen, y.NguoiDung.Email, y.NguoiDung.SoDienThoai },
                Phong = new
                {
                    y.HopDong.Phong.MaPhong,
                    y.HopDong.Phong.TenPhong,
                    NhaTro = y.HopDong.Phong.NhaTro == null ? null : new { y.HopDong.Phong.NhaTro.MaNhaTro, y.HopDong.Phong.NhaTro.TenNhaTro }
                }
            }));

            var ordered = result.OrderByDescending(x => (DateTime)x.GetType().GetProperty("NgayGui")!.GetValue(x)!).ToList();
            return Ok(ApiResponse<List<object>>.Ok(ordered));
        }
    }
}
