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
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
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

            var nguoiThue = await _context.NguoiThue
                .Where(nt => nt.MaNguoiDung == userId)
                .OrderByDescending(nt => nt.MaNguoiThue)
                .FirstOrDefaultAsync();

            if (nguoiThue == null)
            {
                return new
                {
                    TaiKhoan = taiKhoan,
                    PhongDangThue = (object?)null,
                    HopDongHienTai = (object?)null,
                    HoaDonThangNay = (object?)null
                };
            }

            var hopDongHienTai = await _context.HopDong
                .Include(h => h.Phong)
                .ThenInclude(p => p.NhaTro)
                .Where(h => h.MaNguoiThue == nguoiThue.MaNguoiThue
                    && h.NgayBatDau <= now
                    && (h.NgayKetThuc == null || h.NgayKetThuc >= now))
                .OrderByDescending(h => h.NgayBatDau)
                .Select(h => new
                {
                    h.MaHopDong,
                    h.NgayBatDau,
                    h.NgayKetThuc,
                    h.TienCoc,
                    h.MaPhong,
                    h.Phong.TenPhong,
                    h.Phong.NhaTro.TenNhaTro
                })
                .FirstOrDefaultAsync();

            var maPhong = hopDongHienTai != null ? hopDongHienTai.MaPhong : nguoiThue.MaPhong;

            var phongDangThue = await _context.Phong
                .Include(p => p.NhaTro)
                .Where(p => p.MaPhong == maPhong)
                .Select(p => new
                {
                    p.MaPhong,
                    p.TenPhong,
                    p.GiaPhong,
                    p.DiaChiPhong,
                    TenNhaTro = p.NhaTro.TenNhaTro
                })
                .FirstOrDefaultAsync();

            var hoaDon = await _context.HoaDon
                .Where(h => h.MaNguoiThue == nguoiThue.MaNguoiThue
                    && (h.KyHoaDon == kyHoaDon || (h.NgayLap.Month == now.Month && h.NgayLap.Year == now.Year)))
                .OrderByDescending(h => h.NgayLap)
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.KyHoaDon,
                    h.NgayLap,
                    h.TongTien
                })
                .FirstOrDefaultAsync();

            object? hoaDonThangNay = null;
            if (hoaDon != null)
            {
                var daThanhToan = await _context.ThanhToan
                    .Where(t => t.MaHoaDon == hoaDon.MaHoaDon)
                    .SumAsync(t => (decimal?)t.TongTien) ?? 0m;

                hoaDonThangNay = new
                {
                    hoaDon.MaHoaDon,
                    hoaDon.KyHoaDon,
                    hoaDon.NgayLap,
                    hoaDon.TongTien,
                    DaThanhToan = daThanhToan,
                    ConLai = Math.Max(hoaDon.TongTien - daThanhToan, 0m),
                    TrangThaiThanhToan = daThanhToan >= hoaDon.TongTien ? "Đã trả" : "Chưa trả"
                };
            }

            return new
            {
                TaiKhoan = taiKhoan,
                PhongDangThue = phongDangThue,
                HopDongHienTai = hopDongHienTai,
                HoaDonThangNay = hoaDonThangNay
            };
        }
    }
}
