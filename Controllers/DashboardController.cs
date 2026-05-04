using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Helpers;
using DoAnSE104.Services;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRentalPeriodResetService _rentalPeriodResetService;

        public DashboardController(ApplicationDbContext context, IRentalPeriodResetService rentalPeriodResetService)
        {
            _context = context;
            _rentalPeriodResetService = rentalPeriodResetService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("VaiTro") ?? string.Empty;

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview()
        {
            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            if (role == VaiTroConst.Admin)
                await _rentalPeriodResetService.ChotKyThueAsync();
            else if (role == VaiTroConst.ChuTro)
                await _rentalPeriodResetService.ChotKyThueAsync(userId);

            if (role == VaiTroConst.NguoiDung)
                return Ok(await BuildNguoiDungDashboard(userId));

            return Ok(await BuildAdminChuTroDashboard(role, userId));
        }

        private async Task<object> BuildAdminChuTroDashboard(string role, int userId)
        {
            var now = DateTime.Now;

            var nhaTroQuery = _context.NhaTro.AsQueryable();
            var phongQuery = _context.Phong
                .Include(p => p.NhaTro)
                .Include(p => p.TrangThai)
                .AsQueryable();
            var nguoiThueQuery = _context.NguoiThue
                .Include(nt => nt.NguoiDungTK)
                .AsQueryable();
            var hoaDonQuery = _context.HoaDon.AsQueryable();
            var thanhToanQuery = _context.ThanhToan
                .Include(t => t.HoaDon)
                .AsQueryable();

            if (role == VaiTroConst.ChuTro)
            {
                nhaTroQuery = nhaTroQuery.Where(n => n.MaChuTro == userId);
                phongQuery = phongQuery.Where(p => p.NhaTro.MaChuTro == userId);
                nguoiThueQuery = nguoiThueQuery.Where(nt => _context.Phong.Any(p => p.MaPhong == nt.MaPhong && p.NhaTro.MaChuTro == userId));
                hoaDonQuery = hoaDonQuery.Where(h => h.Phong.NhaTro.MaChuTro == userId);
                thanhToanQuery = thanhToanQuery.Where(t => t.HoaDon.Phong.NhaTro.MaChuTro == userId);
            }

            var tongNhaTro = await nhaTroQuery.CountAsync();
            var tongPhong = await phongQuery.CountAsync();
            var phongDangThue = await phongQuery.CountAsync(p => p.MaTrangThai == 2);
            var phongTrong = await phongQuery.CountAsync(p => p.MaTrangThai == 1);
            var tongKhachThue = await nguoiThueQuery.CountAsync();
            var tongHopDong = await _context.HopDong.CountAsync(h => role != VaiTroConst.ChuTro || h.Phong.NhaTro.MaChuTro == userId);
            var tongHoaDon = await hoaDonQuery.CountAsync();

            var doanhThuThang = await thanhToanQuery
                .Where(t => t.NgayThanhToan.Month == now.Month && t.NgayThanhToan.Year == now.Year)
                .SumAsync(t => (decimal?)t.TongTien) ?? 0m;

            var danhSachPhongGanDay = await phongQuery
                .OrderByDescending(p => p.MaPhong)
                .Take(10)
                .Select(p => new
                {
                    p.MaPhong,
                    p.TenPhong,
                    p.GiaPhong,
                    p.MaTrangThai,
                    TrangThai = p.TrangThai.TenTrangThai,
                    TenNhaTro = p.NhaTro.TenNhaTro
                })
                .ToListAsync();

            return new
            {
                TongNhaTro = tongNhaTro,
                TongPhong = tongPhong,
                PhongDangThue = phongDangThue,
                PhongTrong = phongTrong,
                TongKhachThue = tongKhachThue,
                TongHopDong = tongHopDong,
                TongHoaDon = tongHoaDon,
                DoanhThuThang = doanhThuThang,
                DanhSachPhongGanDay = danhSachPhongGanDay
            };
        }

        private async Task<object> BuildNguoiDungDashboard(int userId)
        {
            var now = DateTime.Now;
            var kyHoaDon = $"{now.Year:0000}-{now.Month:00}";

            var taiKhoan = await _context.Users
                .Where(u => u.MaNguoiDung == userId)
                .Select(u => new
                {
                    u.MaNguoiDung,
                    u.HoTen,
                    u.Email,
                    u.SoDienThoai,
                    u.TenDangNhap,
                    u.VaiTro
                })
                .FirstOrDefaultAsync();

            var hopDongHienTaiList = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .ThenInclude(p => p.NhaTro)
                .Where(h => h.NguoiThue.MaNguoiDung == userId
                    && h.NgayBatDau <= now
                    && (h.NgayKetThuc == null || h.NgayKetThuc >= now))
                .OrderByDescending(h => h.NgayBatDau)
                .Select(h => new
                {
                    h.MaHopDong,
                    h.MaNguoiThue,
                    h.NgayBatDau,
                    h.NgayKetThuc,
                    h.TienCoc,
                    h.MaPhong,
                    h.Phong.TenPhong,
                    h.Phong.NhaTro.TenNhaTro
                })
                .ToListAsync();

            var maNguoiThueList = await _context.NguoiThue
                .Where(nt => nt.MaNguoiDung == userId)
                .Select(nt => nt.MaNguoiThue)
                .ToListAsync();

            var maPhongDangThueList = hopDongHienTaiList
                .Select(h => h.MaPhong)
                .Distinct()
                .ToList();

            var phongDangThueList = await _context.Phong
                .Include(p => p.NhaTro)
                .Where(p => maPhongDangThueList.Contains(p.MaPhong))
                .Select(p => new
                {
                    p.MaPhong,
                    p.TenPhong,
                    p.GiaPhong,
                    p.DiaChiPhong,
                    TenNhaTro = p.NhaTro.TenNhaTro
                })
                .ToListAsync();

            var hoaDonList = await _context.HoaDon
                .Include(h => h.Phong)
                .Where(h => maNguoiThueList.Contains(h.MaNguoiThue)
                    && (h.KyHoaDon == kyHoaDon || (h.NgayLap.Month == now.Month && h.NgayLap.Year == now.Year)))
                .OrderByDescending(h => h.NgayLap)
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.MaNguoiThue,
                    h.MaPhong,
                    TenPhong = h.Phong.TenPhong,
                    h.KyHoaDon,
                    h.NgayLap,
                    h.TongTien
                })
                .ToListAsync();

            var maHoaDonList = hoaDonList.Select(h => h.MaHoaDon).ToList();
            var thanhToanTheoHoaDon = await _context.ThanhToan
                .Where(t => maHoaDonList.Contains(t.MaHoaDon))
                .GroupBy(t => t.MaHoaDon)
                .Select(g => new
                {
                    MaHoaDon = g.Key,
                    DaThanhToan = g.Sum(t => t.TongTien)
                })
                .ToListAsync();

            var hoaDonThangNayList = hoaDonList.Select(h =>
            {
                var daThanhToan = thanhToanTheoHoaDon.FirstOrDefault(t => t.MaHoaDon == h.MaHoaDon)?.DaThanhToan ?? 0m;
                var conLai = Math.Max(h.TongTien - daThanhToan, 0m);

                return new
                {
                    h.MaHoaDon,
                    h.MaNguoiThue,
                    h.MaPhong,
                    h.TenPhong,
                    h.KyHoaDon,
                    h.NgayLap,
                    h.TongTien,
                    DaThanhToan = daThanhToan,
                    ConLai = conLai,
                    TrangThaiThanhToan = daThanhToan >= h.TongTien ? "Đã trả" : "Chưa trả"
                };
            }).ToList();

            var tongTienThangNay = hoaDonThangNayList.Sum(h => h.TongTien);
            var daThanhToanThangNay = hoaDonThangNayList.Sum(h => h.DaThanhToan);
            var conLaiThangNay = Math.Max(tongTienThangNay - daThanhToanThangNay, 0m);

            return new
            {
                TaiKhoan = taiKhoan,

                // Giữ lại field cũ để frontend cũ không bị vỡ.
                PhongDangThue = phongDangThueList.FirstOrDefault(),
                HopDongHienTai = hopDongHienTaiList.FirstOrDefault(),
                HoaDonThangNay = hoaDonThangNayList.FirstOrDefault(),

                // Field mới: một người có thể thuê nhiều phòng/nhà trọ.
                DanhSachPhongDangThue = phongDangThueList,
                DanhSachHopDongHienTai = hopDongHienTaiList,
                DanhSachHoaDonThangNay = hoaDonThangNayList,
                TongPhongDangThue = phongDangThueList.Count,
                TongHopDongHienTai = hopDongHienTaiList.Count,
                TongTienThangNay = tongTienThangNay,
                DaThanhToanThangNay = daThanhToanThangNay,
                ConLaiThangNay = conLaiThangNay,
                TrangThaiThanhToan = hoaDonThangNayList.Count == 0
                    ? "Chưa có hóa đơn tháng này"
                    : (conLaiThangNay <= 0 ? "Đã trả" : "Chưa trả")
            };
        }
    }
}
