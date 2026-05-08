using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;

namespace DoAnSE104.Services
{
    public interface IMonthlyInvoiceService
    {
        Task<MonthlyInvoiceResult> TaoHoaDonHangThangAsync(string? kyHoaDon = null, int? maChuTro = null, DateTime? ngayLap = null);
    }

    public class MonthlyInvoiceResult
    {
        public string KyHoaDon { get; set; } = DateTime.Today.ToString("yyyy-MM");
        public int TongHopDongHopLe { get; set; }
        public int SoHoaDonDaTao { get; set; }
        public int SoHoaDonBoQua { get; set; }
        public List<MonthlyInvoiceItemResult> ChiTiet { get; set; } = new();
        public List<string> CanhBao { get; set; } = new();
    }

    public class MonthlyInvoiceItemResult
    {
        public int MaPhong { get; set; }
        public string TenPhong { get; set; } = string.Empty;
        public int? MaHoaDon { get; set; }
        public string TrangThai { get; set; } = string.Empty; // DaTao | DaTonTai | Loi
        public decimal TienPhong { get; set; }
        public decimal TienDien { get; set; }
        public decimal TienNuoc { get; set; }
        public decimal TienDichVu { get; set; }
        public decimal TienPhatSinhKhac { get; set; }
        public decimal TongTien { get; set; }
        public bool ThieuChiSoDien { get; set; }
        public bool ThieuChiSoNuoc { get; set; }
        public List<string> DichVuSuDung { get; set; } = new();
        public string? GhiChu { get; set; }
    }

    public class MonthlyInvoiceService : IMonthlyInvoiceService
    {
        private const string LoaiHoaDonHangThang = "HangThang";
        private readonly ApplicationDbContext _context;
        private readonly INotificationEmailService _notificationEmailService;

        public MonthlyInvoiceService(ApplicationDbContext context, INotificationEmailService notificationEmailService)
        {
            _context = context;
            _notificationEmailService = notificationEmailService;
        }

        public async Task<MonthlyInvoiceResult> TaoHoaDonHangThangAsync(string? kyHoaDon = null, int? maChuTro = null, DateTime? ngayLap = null)
        {
            var ky = string.IsNullOrWhiteSpace(kyHoaDon) ? DateTime.Today.ToString("yyyy-MM") : kyHoaDon.Trim();
            if (!TryParseKyHoaDon(ky, out var nam, out var thang))
                throw new ArgumentException("Kỳ hóa đơn không hợp lệ. Vui lòng dùng định dạng yyyy-MM, ví dụ 2026-05.");

            var ngayLapHoaDon = ngayLap ?? DateTime.Today;
            var dauKy = new DateTime(nam, thang, 1);
            var cuoiKy = dauKy.AddMonths(1).AddTicks(-1);

            var result = new MonthlyInvoiceResult { KyHoaDon = ky };

            var hopDongQuery = _context.HopDong
                .Include(hd => hd.Phong)
                    .ThenInclude(p => p.NhaTro)
                .Include(hd => hd.NguoiThue)
                .Where(hd => hd.TrangThai == "DangHieuLuc"
                    && hd.NgayBatDau <= cuoiKy
                    && (hd.NgayKetThuc == null || hd.NgayKetThuc >= dauKy));

            if (maChuTro.HasValue)
            {
                hopDongQuery = hopDongQuery.Where(hd => hd.Phong.NhaTro.MaChuTro == maChuTro.Value);
            }

            var hopDongs = await hopDongQuery
                .OrderBy(hd => hd.MaPhong)
                .ThenByDescending(hd => hd.NgayBatDau)
                .ToListAsync();

            // Một phòng chỉ lấy hợp đồng hợp lệ mới nhất trong kỳ để tránh tạo trùng khi có lịch sử nhiều hợp đồng.
            var hopDongTheoPhong = hopDongs
                .GroupBy(hd => hd.MaPhong)
                .Select(g => g.First())
                .ToList();

            result.TongHopDongHopLe = hopDongTheoPhong.Count;

            foreach (var hopDong in hopDongTheoPhong)
            {
                var phong = hopDong.Phong;
                var item = new MonthlyInvoiceItemResult
                {
                    MaPhong = hopDong.MaPhong,
                    TenPhong = phong?.TenPhong ?? $"Phòng #{hopDong.MaPhong}",
                    TienPhong = 0m  // Hóa đơn hằng tháng không bao gồm tiền phòng
                };

                try
                {
                    var daCoHoaDon = await _context.HoaDon
                        .Where(hd => hd.MaPhong == hopDong.MaPhong
                            && hd.KyHoaDon == ky
                            && hd.LoaiHoaDon == LoaiHoaDonHangThang
                            && hd.TrangThai != "Huy")
                        .Select(hd => new { hd.MaHoaDon, hd.TongTien })
                        .FirstOrDefaultAsync();

                    if (daCoHoaDon != null)
                    {
                        item.MaHoaDon = daCoHoaDon.MaHoaDon;
                        item.TongTien = daCoHoaDon.TongTien;
                        item.TrangThai = "DaTonTai";
                        item.GhiChu = "Đã có hóa đơn hằng tháng cho phòng này trong kỳ.";
                        result.SoHoaDonBoQua++;
                        result.ChiTiet.Add(item);
                        continue;
                    }

                    var chiSoDien = await _context.ChiSoDien
                        .Where(cd => cd.MaPhong == hopDong.MaPhong
                            && cd.NgayThangDien.Year == nam
                            && cd.NgayThangDien.Month == thang)
                        .OrderByDescending(cd => cd.NgayThangDien)
                        .FirstOrDefaultAsync();

                    var chiSoNuoc = await _context.ChiSoNuoc
                        .Where(cn => cn.MaPhong == hopDong.MaPhong
                            && cn.NgayThangNuoc.Year == nam
                            && cn.NgayThangNuoc.Month == thang)
                        .OrderByDescending(cn => cn.NgayThangNuoc)
                        .FirstOrDefaultAsync();

                    item.ThieuChiSoDien = chiSoDien == null;
                    item.ThieuChiSoNuoc = chiSoNuoc == null;
                    item.TienDien = chiSoDien?.TienDien ?? 0m;
                    item.TienNuoc = chiSoNuoc?.TienNuoc ?? 0m;

                    if (item.ThieuChiSoDien)
                        result.CanhBao.Add($"Phòng {item.TenPhong} chưa có chỉ số điện kỳ {ky}, tiền điện tạm tính 0đ.");
                    if (item.ThieuChiSoNuoc)
                        result.CanhBao.Add($"Phòng {item.TenPhong} chưa có chỉ số nước kỳ {ky}, tiền nước tạm tính 0đ.");

                    var dichVuDangDungRaw = await _context.DangKyDichVu
                        .Include(dk => dk.DichVu)
                        .Where(dk => dk.MaPhong == hopDong.MaPhong
                            && dk.TrangThai == "DangSuDung"
                            && dk.DichVu.MaNhaTro == phong.MaNhaTro
                            && dk.DichVu.LoaiDichVu == "TinhPhi")
                        .ToListAsync();

                    var dichVuDangDung = dichVuDangDungRaw
                        .GroupBy(dk => dk.MaDichVu)
                        .Select(g => g.OrderByDescending(x => x.NgayDangKy).First())
                        .ToList();

                    item.TienDichVu = dichVuDangDung.Sum(dk => (decimal)dk.DichVu.Tiendichvu);
                    item.DichVuSuDung = dichVuDangDung
                        .Select(dk => dk.DichVu.TenDichVu ?? "Dịch vụ")
                        .OrderBy(x => x)
                        .ToList();

                    item.TongTien = item.TienDien + item.TienNuoc + item.TienDichVu + item.TienPhatSinhKhac;

                    var hoaDon = new HoaDon
                    {
                        MaNguoiThue = hopDong.MaNguoiThue,
                        MaPhong = hopDong.MaPhong,
                        MaDien = chiSoDien?.MaDien,
                        MaNuoc = chiSoNuoc?.MaNuoc,
                        LoaiHoaDon = LoaiHoaDonHangThang,
                        TienPhatSinhKhac = item.TienPhatSinhKhac,
                        TongTien = item.TongTien,
                        NgayLap = ngayLapHoaDon,
                        KyHoaDon = ky,
                        TrangThai = "ChuaThanhToan"
                    };

                    _context.HoaDon.Add(hoaDon);
                    await _context.SaveChangesAsync();

                    ThemChiTiet(hoaDon.MaHoaDon, "TienDien", item.TienDien);
                    ThemChiTiet(hoaDon.MaHoaDon, "TienNuoc", item.TienNuoc);
                    ThemChiTiet(hoaDon.MaHoaDon, "PhatSinhKhac", item.TienPhatSinhKhac);

                    foreach (var dangKy in dichVuDangDung)
                    {
                        _context.ChiTietHoaDon.Add(new ChiTietHoaDon
                        {
                            MaHoaDon = hoaDon.MaHoaDon,
                            LoaiKhoan = TaoLoaiKhoanDichVu(dangKy.DichVu),
                            SoTien = (decimal)dangKy.DichVu.Tiendichvu
                        });
                    }

                    await _context.SaveChangesAsync();

                    item.MaHoaDon = hoaDon.MaHoaDon;
                    item.TrangThai = "DaTao";
                    item.GhiChu = "Tạo hóa đơn thành công.";
                    result.SoHoaDonDaTao++;
                    result.ChiTiet.Add(item);
                    await _notificationEmailService.GuiEmailHoaDonMoiAsync(hoaDon.MaHoaDon);
                }
                catch (Exception ex)
                {
                    item.TrangThai = "Loi";
                    item.GhiChu = ex.Message;
                    result.CanhBao.Add($"Lỗi tạo hóa đơn phòng {item.TenPhong}: {ex.Message}");
                    result.ChiTiet.Add(item);
                }
            }

            return result;
        }

        private static bool TryParseKyHoaDon(string kyHoaDon, out int nam, out int thang)
        {
            nam = 0;
            thang = 0;

            var parts = kyHoaDon.Split('-');
            if (parts.Length != 2) return false;
            if (!int.TryParse(parts[0], out nam) || !int.TryParse(parts[1], out thang)) return false;

            return nam >= 1900 && nam <= 9999 && thang >= 1 && thang <= 12;
        }

        private void ThemChiTiet(int maHoaDon, string loaiKhoan, decimal soTien)
        {
            if (soTien <= 0) return;
            _context.ChiTietHoaDon.Add(new ChiTietHoaDon
            {
                MaHoaDon = maHoaDon,
                LoaiKhoan = loaiKhoan,
                SoTien = soTien
            });
        }

        private static string TaoLoaiKhoanDichVu(DichVu dichVu)
        {
            var ten = string.IsNullOrWhiteSpace(dichVu.TenDichVu)
                ? "Dịch vụ"
                : dichVu.TenDichVu.Trim();

            var loaiKhoan = $"DichVu:{dichVu.MaDichVu}:{ten}";
            return loaiKhoan.Length <= 50 ? loaiKhoan : loaiKhoan.Substring(0, 50);
        }
    }
}
