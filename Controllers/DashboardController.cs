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

        private static bool TryParseKyHoaDon(string? kyHoaDon, out int nam, out int thang)
        {
            nam = 0;
            thang = 0;
            if (string.IsNullOrWhiteSpace(kyHoaDon)) return false;

            var parts = kyHoaDon.Trim().Split('-');
            return parts.Length == 2 &&
                   int.TryParse(parts[0], out nam) &&
                   int.TryParse(parts[1], out thang) &&
                   thang >= 1 &&
                   thang <= 12;
        }

        private sealed class DashboardActivityDto
        {
            public string Title { get; set; } = string.Empty;
            public string Subtitle { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string StatusType { get; set; } = "neutral";
            public string Section { get; set; } = "overview";
            public DateTime OccurredAt { get; set; }
            public string TimeText { get; set; } = string.Empty;
            public string Avatar { get; set; } = string.Empty;
        }

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
            var sapHetHanNgay = now.AddDays(30);

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
            var hopDongQuery = _context.HopDong
                .Include(h => h.Phong).ThenInclude(p => p.NhaTro)
                .AsQueryable();
            var yeuCauThueQuery = _context.YeuCauThue.AsQueryable();
            var baoCaoQuery = _context.BaoCaoSuCo.AsQueryable();
            var thongBaoQuery = _context.ThongBao
                .Include(tb => tb.Phong).ThenInclude(p => p!.NhaTro)
                .Where(tb => tb.TrangThai != "An")
                .AsQueryable();

            if (role == VaiTroConst.ChuTro)
            {
                nhaTroQuery = nhaTroQuery.Where(n => n.MaChuTro == userId);
                phongQuery = phongQuery.Where(p => p.NhaTro.MaChuTro == userId);
                nguoiThueQuery = nguoiThueQuery.Where(nt => _context.Phong.Any(p => p.MaPhong == nt.MaPhong && p.NhaTro.MaChuTro == userId));
                hoaDonQuery = hoaDonQuery.Where(h => h.Phong.NhaTro.MaChuTro == userId);
                thanhToanQuery = thanhToanQuery.Where(t => t.HoaDon.Phong.NhaTro.MaChuTro == userId);
                hopDongQuery = hopDongQuery.Where(h => h.Phong.NhaTro.MaChuTro == userId);
                yeuCauThueQuery = yeuCauThueQuery.Where(y => y.Phong.NhaTro.MaChuTro == userId);
                baoCaoQuery = baoCaoQuery.Where(b => b.Phong.NhaTro.MaChuTro == userId);
                var maPhongCuaChuTro = await phongQuery.Select(p => p.MaPhong).ToListAsync();
                thongBaoQuery = thongBaoQuery.Where(tb =>
                    tb.NguoiTaoId == userId ||
                    (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && maPhongCuaChuTro.Contains(tb.PhongId.Value)));
            }

            // ── Thống kê phòng ──────────────────────────────────────────────
            var tongNhaTro = await nhaTroQuery.CountAsync();
            var tongPhong = await phongQuery.CountAsync();
            var phongDangThue = await phongQuery.CountAsync(p => p.MaTrangThai == 2);
            var phongTrong = await phongQuery.CountAsync(p => p.MaTrangThai == 1);
            var tongKhachThue = await nguoiThueQuery.CountAsync();
            var phongTheoTrangThai = await phongQuery
                .GroupBy(p => new { p.MaTrangThai, p.TrangThai.TenTrangThai })
                .Select(g => new
                {
                    MaTrangThai = g.Key.MaTrangThai,
                    TrangThai = g.Key.TenTrangThai,
                    SoLuong = g.Count()
                })
                .OrderBy(x => x.MaTrangThai)
                .ToListAsync();

            // ── Hóa đơn chưa thanh toán ─────────────────────────────────────
            var hoaDonChuaThanhToan = await hoaDonQuery
                .CountAsync(h => h.TrangThai == "ChuaThanhToan");
            var tongHoaDon = await hoaDonQuery.CountAsync();
            var hoaDonTheoTrangThai = await hoaDonQuery
                .GroupBy(h => h.TrangThai)
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToListAsync();

            // ── Yêu cầu thuê chờ duyệt ──────────────────────────────────────
            var yeuCauChoDuyet = await yeuCauThueQuery
                .CountAsync(y => y.TrangThai == "ChoDuyet");

            // ── Báo cáo sự cố mới/chưa xử lý ───────────────────────────────
            var baoCaoMoi = await baoCaoQuery
                .CountAsync(b => b.TrangThai == "Moi" || b.TrangThai == "DangXuLy");
            var soThongBaoGanDay = await thongBaoQuery.CountAsync();

            // ── Hợp đồng sắp hết hạn (trong 30 ngày) ───────────────────────
            var hopDongSapHetHan = await hopDongQuery
                .CountAsync(h =>
                    h.TrangThai == "DangHieuLuc" &&
                    h.NgayKetThuc != null &&
                    h.NgayKetThuc >= now &&
                    h.NgayKetThuc <= sapHetHanNgay);
            var tongHopDong = await hopDongQuery.CountAsync();
            var hopDongTheoTrangThaiRaw = await hopDongQuery
                .Select(h => new { h.TrangThai, h.NgayKetThuc })
                .ToListAsync();
            var hopDongTheoTrangThai = hopDongTheoTrangThaiRaw
                .Select(h =>
                    h.TrangThai == "DangHieuLuc" &&
                    h.NgayKetThuc != null &&
                    h.NgayKetThuc >= now &&
                    h.NgayKetThuc <= sapHetHanNgay
                        ? "SapHetHan"
                        : h.TrangThai)
                .GroupBy(trangThai => trangThai)
                .Select(g => new
                {
                    TrangThai = g.Key,
                    SoLuong = g.Count()
                })
                .ToList();

            // ── Doanh thu tháng hiện tại ────────────────────────────────────
            var doanhThuThang = await thanhToanQuery
                .Where(t => t.NgayThanhToan.Month == now.Month && t.NgayThanhToan.Year == now.Year)
                .SumAsync(t => (decimal?)t.TongTien) ?? 0m;

            // ── Doanh thu 6 tháng gần nhất ──────────────────────────────────
            var doanhThu6Thang = new List<object>();
            for (int i = 5; i >= 0; i--)
            {
                var thang = now.AddMonths(-i);
                var dt = await thanhToanQuery
                    .Where(t => t.NgayThanhToan.Month == thang.Month && t.NgayThanhToan.Year == thang.Year)
                    .SumAsync(t => (decimal?)t.TongTien) ?? 0m;
                doanhThu6Thang.Add(new
                {
                    thang = $"{thang.Month:00}/{thang.Year}",
                    doanhThu = dt
                });
            }

            // ── Danh sách phòng gần đây ─────────────────────────────────────
            var kyHoaDonHienTai = $"{now.Year:0000}-{now.Month:00}";
            var hoaDonTaiChinhRaw = await hoaDonQuery
                .Where(h => h.TrangThai == null || h.TrangThai != "Huy")
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.KyHoaDon,
                    h.NgayLap,
                    h.TongTien,
                    MaNhaTro = h.Phong.MaNhaTro,
                    TenNhaTro = h.Phong.NhaTro.TenNhaTro
                })
                .ToListAsync();

            var hoaDonTaiChinh = hoaDonTaiChinhRaw
                .Select(h =>
                {
                    var ky = !string.IsNullOrWhiteSpace(h.KyHoaDon) ? h.KyHoaDon.Trim() : $"{h.NgayLap.Year:0000}-{h.NgayLap.Month:00}";
                    if (!TryParseKyHoaDon(ky, out var nam, out var thang))
                        return null;

                    return new
                    {
                        h.MaHoaDon,
                        Ky = $"{nam:0000}-{thang:00}",
                        h.NgayLap,
                        h.TongTien,
                        h.MaNhaTro,
                        h.TenNhaTro
                    };
                })
                .Where(h => h != null)
                .Select(h => h!)
                .ToList();

            var kyTaiChinh = hoaDonTaiChinh
                .OrderByDescending(h => h!.Ky)
                .ThenByDescending(h => h!.NgayLap)
                .Select(h => h!.Ky)
                .FirstOrDefault() ?? kyHoaDonHienTai;

            var hoaDonThangList = hoaDonTaiChinh
                .Where(h => h.Ky == kyTaiChinh)
                .ToList();

            var maHoaDonThang = hoaDonThangList.Select(h => h.MaHoaDon).ToList();
            var thanhToanTheoHoaDon = await thanhToanQuery
                .Where(t => maHoaDonThang.Contains(t.MaHoaDon) && t.TrangThaiXacNhan == "DaXacNhan")
                .GroupBy(t => t.MaHoaDon)
                .Select(g => new { MaHoaDon = g.Key, DaThu = g.Sum(t => t.TongTien) })
                .ToDictionaryAsync(x => x.MaHoaDon, x => x.DaThu);

            var taiChinhTheoNhaTro = hoaDonThangList
                .GroupBy(h => new { h.MaNhaTro, h.TenNhaTro })
                .Select(g =>
                {
                    var phaiThu = g.Sum(h => h.TongTien);
                    var daThu = g.Sum(h => thanhToanTheoHoaDon.TryGetValue(h.MaHoaDon, out var paid) ? Math.Min(paid, h.TongTien) : 0m);
                    var soHoaDonChuaThu = g.Count(h =>
                    {
                        var paid = thanhToanTheoHoaDon.TryGetValue(h.MaHoaDon, out var value) ? value : 0m;
                        return paid < h.TongTien;
                    });

                    return new
                    {
                        g.Key.MaNhaTro,
                        g.Key.TenNhaTro,
                        SoHoaDon = g.Count(),
                        SoHoaDonChuaThu = soHoaDonChuaThu,
                        PhaiThu = phaiThu,
                        DaThu = daThu,
                        ConNo = Math.Max(0m, phaiThu - daThu)
                    };
                })
                .OrderByDescending(x => x.ConNo)
                .ThenBy(x => x.TenNhaTro)
                .ToList();

            var taiChinhThang = new
            {
                Ky = kyTaiChinh,
                Thang = $"{kyTaiChinh.Substring(5, 2)}/{kyTaiChinh.Substring(0, 4)}",
                CapNhatLuc = now.ToString("HH:mm"),
                PhaiThu = taiChinhTheoNhaTro.Sum(x => x.PhaiThu),
                DaThu = taiChinhTheoNhaTro.Sum(x => x.DaThu),
                ConNo = taiChinhTheoNhaTro.Sum(x => x.ConNo),
                SoHoaDon = hoaDonThangList.Count,
                SoHoaDonChuaThu = taiChinhTheoNhaTro.Sum(x => x.SoHoaDonChuaThu),
                TheoNhaTro = taiChinhTheoNhaTro
            };

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

            // ── Hợp đồng sắp hết hạn chi tiết (top 5) ──────────────────────
            var danhSachHopDongSapHet = await hopDongQuery
                .Where(h =>
                    h.TrangThai == "DangHieuLuc" &&
                    h.NgayKetThuc != null &&
                    h.NgayKetThuc >= now &&
                    h.NgayKetThuc <= sapHetHanNgay)
                .OrderBy(h => h.NgayKetThuc)
                .Take(5)
                .Select(h => new
                {
                    h.MaHopDong,
                    h.NgayKetThuc,
                    TenPhong = h.Phong.TenPhong,
                    TenNhaTro = h.Phong.NhaTro.TenNhaTro
                })
                .ToListAsync();

            var hoatDongGanDay = new List<DashboardActivityDto>();

            hoatDongGanDay.AddRange(await thongBaoQuery
                .OrderByDescending(tb => tb.NgayTao)
                .Take(5)
                .Select(tb => new DashboardActivityDto
                {
                    Title = tb.TieuDe,
                    Subtitle = tb.Phong != null ? "Phòng " + tb.Phong.TenPhong + " · " + tb.Phong.NhaTro.TenNhaTro : tb.NoiDung,
                    Status = "Thông báo",
                    StatusType = tb.LoaiThongBao == "HoaDon" ? "warning" : tb.LoaiThongBao == "HopDong" ? "danger" : "info",
                    Section = "thongbao",
                    OccurredAt = tb.NgayTao,
                    Avatar = "TB"
                })
                .ToListAsync());

            hoatDongGanDay.AddRange(await yeuCauThueQuery
                .OrderByDescending(y => y.NgayGui)
                .Where(y => y.TrangThai == "ChoDuyet" || y.TrangThai == "ChoNguoiThueXacNhan")
                .Take(4)
                .Select(y => new DashboardActivityDto
                {
                    Title = y.TrangThai == "ChoNguoiThueXacNhan" ? y.NguoiDung.HoTen + " cần xác nhận hợp đồng" : y.NguoiDung.HoTen + " gửi yêu cầu thuê",
                    Subtitle = "Phòng " + y.Phong.TenPhong + " · " + y.Phong.NhaTro.TenNhaTro,
                    Status = y.TrangThai == "ChoNguoiThueXacNhan" ? "Chờ xác nhận" : "Chờ duyệt",
                    StatusType = "warning",
                    Section = "yeucauthue",
                    OccurredAt = y.NgayGui,
                    Avatar = y.NguoiDung.HoTen
                })
                .ToListAsync());

            var hoaDonCanThuGanDay = await hoaDonQuery
                .Where(h => h.TrangThai == "ChuaThanhToan" || h.TrangThai == "ThanhToanMotPhan")
                .OrderByDescending(h => h.NgayLap)
                .Take(5)
                .Select(h => new
                {
                    h.MaHoaDon,
                    h.KyHoaDon,
                    h.NgayLap,
                    h.TrangThai,
                    h.TongTien,
                    TenPhong = h.Phong.TenPhong,
                    TenNhaTro = h.Phong.NhaTro.TenNhaTro
                })
                .ToListAsync();

            hoatDongGanDay.AddRange(hoaDonCanThuGanDay.Select(h =>
            {
                var quaHan = TryParseKyHoaDon(h.KyHoaDon, out var nam, out var thang) && now.Date > new DateTime(nam, thang, 10);
                return new DashboardActivityDto
                {
                    Title = quaHan ? $"Hóa đơn #{h.MaHoaDon} đã quá hạn" : $"Hóa đơn #{h.MaHoaDon} chưa thu",
                    Subtitle = $"Phòng {h.TenPhong} · {h.TenNhaTro} · {h.TongTien:n0}đ",
                    Status = quaHan ? "Quá hạn" : (h.TrangThai == "ThanhToanMotPhan" ? "Thu 1 phần" : "Chưa thu"),
                    StatusType = quaHan ? "danger" : "warning",
                    Section = "hoadon",
                    OccurredAt = h.NgayLap,
                    Avatar = "HD"
                };
            }));

            var hopDongSapHetGanDay = await hopDongQuery
                .Where(h =>
                    h.TrangThai == "DangHieuLuc" &&
                    h.NgayKetThuc != null &&
                    h.NgayKetThuc >= now &&
                    h.NgayKetThuc <= sapHetHanNgay)
                .OrderBy(h => h.NgayKetThuc)
                .Take(5)
                .Select(h => new
                {
                    h.NgayKetThuc,
                    TenPhong = h.Phong.TenPhong,
                    TenNhaTro = h.Phong.NhaTro.TenNhaTro
                })
                .ToListAsync();

            hoatDongGanDay.AddRange(hopDongSapHetGanDay.Select(h =>
            {
                var daysLeft = Math.Max(0, (int)Math.Ceiling(((h.NgayKetThuc ?? now).Date - now.Date).TotalDays));
                return new DashboardActivityDto
                {
                    Title = "Hợp đồng phòng " + h.TenPhong + " sắp hết hạn",
                    Subtitle = h.TenNhaTro,
                    Status = daysLeft + " ngày",
                    StatusType = "danger",
                    Section = "hopdong",
                    OccurredAt = now.AddMinutes(-Math.Max(1, daysLeft)),
                    TimeText = h.NgayKetThuc?.ToString("dd/MM/yyyy") ?? "Sắp tới",
                    Avatar = "HĐ"
                };
            }));

            hoatDongGanDay = hoatDongGanDay
                .OrderByDescending(x => x.OccurredAt)
                .Take(8)
                .ToList();

            return new
            {
                // Thống kê cơ bản
                TongNhaTro = tongNhaTro,
                TongPhong = tongPhong,
                PhongDangThue = phongDangThue,
                PhongTrong = phongTrong,
                TongKhachThue = tongKhachThue,
                DoanhThuThang = doanhThuThang,
                TongHopDong = tongHopDong,
                TongHoaDon = tongHoaDon,

                // Cảnh báo
                HoaDonChuaThanhToan = hoaDonChuaThanhToan,
                YeuCauChoDuyet = yeuCauChoDuyet,
                BaoCaoMoi = baoCaoMoi,
                HopDongSapHetHan = hopDongSapHetHan,
                SoThongBaoChuaDoc = soThongBaoGanDay,

                // Chi tiết
                PhongTheoTrangThai = phongTheoTrangThai,
                HopDongTheoTrangThai = hopDongTheoTrangThai,
                HoaDonTheoTrangThai = hoaDonTheoTrangThai,
                DoanhThu6Thang = doanhThu6Thang,
                TaiChinhThang = taiChinhThang,
                DanhSachPhongGanDay = danhSachPhongGanDay,
                DanhSachHopDongSapHet = danhSachHopDongSapHet,
                HoatDongGanDay = hoatDongGanDay
            };
        }

        private async Task<object> BuildNguoiDungDashboard(int userId)
        {
            var now = DateTime.Now;
            var kyHoaDon = $"{now.Year:0000}-{now.Month:00}";

            var taiKhoan = await _context.Users
                .Where(u => u.MaNguoiDung == userId)
                .Select(u => new { u.MaNguoiDung, u.HoTen, u.Email, u.SoDienThoai, u.TenDangNhap, u.VaiTro })
                .FirstOrDefaultAsync();

            // ── Hợp đồng đang hiệu lực ──────────────────────────────────────
            var hopDongHienTaiList = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong).ThenInclude(p => p.NhaTro)
                .Where(h => h.NguoiThue.MaNguoiDung == userId
                    && h.NgayBatDau <= now
                    && (h.NgayKetThuc == null || h.NgayKetThuc >= now))
                .OrderByDescending(h => h.NgayBatDau)
                .Select(h => new
                {
                    h.MaHopDong, h.MaNguoiThue, h.NgayBatDau, h.NgayKetThuc,
                    h.TienCoc, h.MaPhong, h.Phong.TenPhong, h.Phong.NhaTro.TenNhaTro
                })
                .ToListAsync();

            var maNguoiThueList = await _context.NguoiThue
                .Where(nt => nt.MaNguoiDung == userId)
                .Select(nt => nt.MaNguoiThue)
                .ToListAsync();

            var maPhongDangThueList = hopDongHienTaiList.Select(h => h.MaPhong).Distinct().ToList();

            var phongDangThueList = await _context.Phong
                .Include(p => p.NhaTro)
                .Where(p => maPhongDangThueList.Contains(p.MaPhong))
                .Select(p => new { p.MaPhong, p.TenPhong, p.GiaPhong, p.DiaChiPhong, TenNhaTro = p.NhaTro.TenNhaTro })
                .ToListAsync();

            // ── Hóa đơn chưa thanh toán ─────────────────────────────────────
            var hoaDonChuaThanhToanList = await _context.HoaDon
                .Include(h => h.Phong)
                .Where(h => maNguoiThueList.Contains(h.MaNguoiThue)
                    && h.TrangThai == "ChuaThanhToan")
                .OrderByDescending(h => h.NgayLap)
                .Take(5)
                .Select(h => new
                {
                    h.MaHoaDon, h.MaPhong, TenPhong = h.Phong.TenPhong,
                    h.KyHoaDon, h.NgayLap, h.TongTien
                })
                .ToListAsync();

            // ── Hóa đơn tháng này (backward compat) ─────────────────────────
            var hoaDonList = await _context.HoaDon
                .Include(h => h.Phong)
                .Where(h => maNguoiThueList.Contains(h.MaNguoiThue)
                    && (h.KyHoaDon == kyHoaDon || (h.NgayLap.Month == now.Month && h.NgayLap.Year == now.Year)))
                .OrderByDescending(h => h.NgayLap)
                .Select(h => new { h.MaHoaDon, h.MaNguoiThue, h.MaPhong, TenPhong = h.Phong.TenPhong, h.KyHoaDon, h.NgayLap, h.TongTien })
                .ToListAsync();

            var maHoaDonList = hoaDonList.Select(h => h.MaHoaDon).ToList();
            var thanhToanTheoHoaDon = await _context.ThanhToan
                .Where(t => maHoaDonList.Contains(t.MaHoaDon))
                .GroupBy(t => t.MaHoaDon)
                .Select(g => new { MaHoaDon = g.Key, DaThanhToan = g.Sum(t => t.TongTien) })
                .ToListAsync();

            var hoaDonThangNayList = hoaDonList.Select(h =>
            {
                var daThanhToan = thanhToanTheoHoaDon.FirstOrDefault(t => t.MaHoaDon == h.MaHoaDon)?.DaThanhToan ?? 0m;
                var conLai = Math.Max(h.TongTien - daThanhToan, 0m);
                return new
                {
                    h.MaHoaDon, h.MaNguoiThue, h.MaPhong, h.TenPhong,
                    h.KyHoaDon, h.NgayLap, h.TongTien,
                    DaThanhToan = daThanhToan, ConLai = conLai,
                    TrangThaiThanhToan = daThanhToan >= h.TongTien ? "Đã trả" : "Chưa trả"
                };
            }).ToList();

            // ── Dịch vụ đang dùng ────────────────────────────────────────────
            var dichVuDangDung = await _context.DangKyDichVu
                .Include(d => d.DichVu)
                .Include(d => d.Phong)
                .Where(d => d.MaNguoiDung == userId && d.TrangThai == "DangSuDung")
                .OrderByDescending(d => d.NgayDangKy)
                .Take(6)
                .Select(d => new
                {
                    d.MaDangKyDichVu, d.MaDichVu, TenDichVu = d.DichVu.TenDichVu,
                    TienDichVu = (decimal)d.DichVu.Tiendichvu, d.MaPhong, TenPhong = d.Phong.TenPhong,
                    d.NgayDangKy, d.TrangThai
                })
                .ToListAsync();

            // ── Thông báo chưa đọc ───────────────────────────────────────────
            var thongBaoChuaDoc = await _context.ThongBao
                .Where(tb => tb.TrangThai != "An" &&
                    (tb.LoaiNguoiNhan == "TatCa" ||
                     (tb.LoaiNguoiNhan == "NguoiDung" && (tb.NguoiNhanId == null || tb.NguoiNhanId == userId))))
                .Where(tb => !_context.ThongBaoDaDoc.Any(x => x.ThongBaoId == tb.ThongBaoId && x.MaNguoiDung == userId))
                .OrderByDescending(tb => tb.NgayTao)
                .Take(5)
                .Select(tb => new
                {
                    tb.ThongBaoId, tb.TieuDe, tb.NoiDung, tb.NgayTao, tb.LoaiThongBao
                })
                .ToListAsync();

            var soThongBaoChuaDoc = await _context.ThongBao
                .Where(tb => tb.TrangThai != "An" &&
                    (tb.LoaiNguoiNhan == "TatCa" ||
                     (tb.LoaiNguoiNhan == "NguoiDung" && (tb.NguoiNhanId == null || tb.NguoiNhanId == userId))))
                .CountAsync(tb => !_context.ThongBaoDaDoc.Any(x => x.ThongBaoId == tb.ThongBaoId && x.MaNguoiDung == userId));

            // ── Báo cáo sự cố gần đây ────────────────────────────────────────
            var baoCaoGanDay = await _context.BaoCaoSuCo
                .Include(b => b.Phong)
                .Where(b => b.MaNguoiDung == userId)
                .OrderByDescending(b => b.NgayGui)
                .Take(5)
                .Select(b => new
                {
                    b.MaBaoCao, b.TieuDe, b.MucDo, b.TrangThai,
                    b.NgayGui, TenPhong = b.Phong.TenPhong, b.PhanHoiChuTro
                })
                .ToListAsync();

            var tongTien = hoaDonThangNayList.Sum(h => h.TongTien);
            var daThanhToan = hoaDonThangNayList.Sum(h => h.DaThanhToan);
            var conLai = Math.Max(tongTien - daThanhToan, 0m);

            return new
            {
                TaiKhoan = taiKhoan,
                // Backward compat
                PhongDangThue = phongDangThueList.FirstOrDefault(),
                HopDongHienTai = hopDongHienTaiList.FirstOrDefault(),
                HoaDonThangNay = hoaDonThangNayList.FirstOrDefault(),
                // Multi-room
                DanhSachPhongDangThue = phongDangThueList,
                DanhSachHopDongHienTai = hopDongHienTaiList,
                DanhSachHoaDonThangNay = hoaDonThangNayList,
                TongPhongDangThue = phongDangThueList.Count,
                TongHopDongHienTai = hopDongHienTaiList.Count,
                TongTienThangNay = tongTien,
                DaThanhToanThangNay = daThanhToan,
                ConLaiThangNay = conLai,
                TrangThaiThanhToan = hoaDonThangNayList.Count == 0
                    ? "Chưa có hóa đơn tháng này"
                    : (conLai <= 0 ? "Đã trả" : "Chưa trả"),
                // Mới
                HoaDonChuaThanhToan = hoaDonChuaThanhToanList,
                SoHoaDonChuaTT = hoaDonChuaThanhToanList.Count,
                DichVuDangDung = dichVuDangDung,
                ThongBaoChuaDoc = thongBaoChuaDoc,
                SoThongBaoChuaDoc = soThongBaoChuaDoc,
                BaoCaoGanDay = baoCaoGanDay
            };
        }
    }
}
