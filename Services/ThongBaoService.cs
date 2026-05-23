using DoAnSE104.Data;
using DoAnSE104.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnSE104.Services
{
    /// <summary>
    /// Tạo thông báo cho các sự kiện hệ thống.
    /// </summary>
    public interface IThongBaoService
    {
        Task TaoThongBaoHoaDonMoiAsync(int maHoaDon);
        Task TaoThongBaoHopDongSapHetHanAsync(int maHopDong, int soNgayConLai);
        Task TaoThongBaoDichVuAsync(int maDangKyDichVu, string trangThaiMoi);
        Task TaoThongBaoBaoCaoSuCoAsync(int maBaoCao, string phanHoi);
    }

    public class ThongBaoService : IThongBaoService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ThongBaoService> _logger;

        public ThongBaoService(ApplicationDbContext context, ILogger<ThongBaoService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Hóa đơn mới ────────────────────────────────────────────────────────
        public async Task TaoThongBaoHoaDonMoiAsync(int maHoaDon)
        {
            try
            {
                var hoaDon = await _context.HoaDon
                    .Include(h => h.Phong)
                    .Include(h => h.NguoiThue).ThenInclude(nt => nt.NguoiDungTK)
                    .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);

                if (hoaDon == null) return;

                var nguoiNhanId = hoaDon.NguoiThue?.MaNguoiDung;
                if (nguoiNhanId == null) return;

                var tb = new ThongBao
                {
                    TieuDe = $"Hóa đơn mới - {hoaDon.Phong?.TenPhong ?? $"Phòng #{hoaDon.MaPhong}"}",
                    NoiDung = $"Bạn có hóa đơn mới kỳ {hoaDon.KyHoaDon} cho phòng {hoaDon.Phong?.TenPhong}. " +
                              $"Tổng tiền: {hoaDon.TongTien:N0}đ. Vui lòng kiểm tra và thanh toán đúng hạn.",
                    LoaiThongBao = "HoaDon",
                    LoaiNguoiNhan = "NguoiDung",
                    NguoiNhanId = nguoiNhanId,
                    PhongId = hoaDon.MaPhong,
                    NgayTao = DateTime.Now
                };

                _context.ThongBao.Add(tb);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo hóa đơn mới #{MaHoaDon}", maHoaDon);
            }
        }

        // ── Hợp đồng sắp hết hạn ───────────────────────────────────────────────
        public async Task TaoThongBaoHopDongSapHetHanAsync(int maHopDong, int soNgayConLai)
        {
            try
            {
                var hopDong = await _context.HopDong
                    .Include(h => h.Phong)
                    .Include(h => h.NguoiThue).ThenInclude(nt => nt.NguoiDungTK)
                    .FirstOrDefaultAsync(h => h.MaHopDong == maHopDong);

                if (hopDong == null) return;

                var nguoiNhanId = hopDong.NguoiThue?.MaNguoiDung;
                if (nguoiNhanId == null) return;

                // Tránh tạo trùng trong cùng ngày
                var daCoHomNay = await _context.ThongBao.AnyAsync(tb =>
                    tb.LoaiThongBao == "HopDong" &&
                    tb.NguoiNhanId == nguoiNhanId &&
                    tb.PhongId == hopDong.MaPhong &&
                    tb.NgayTao.Date == DateTime.Today);

                if (daCoHomNay) return;

                var tb = new ThongBao
                {
                    TieuDe = $"Hợp đồng sắp hết hạn - {hopDong.Phong?.TenPhong}",
                    NoiDung = $"Hợp đồng thuê phòng {hopDong.Phong?.TenPhong} của bạn còn {soNgayConLai} ngày nữa là hết hạn " +
                              $"(ngày {hopDong.NgayKetThuc:dd/MM/yyyy}). Vui lòng liên hệ chủ trọ để gia hạn hoặc sắp xếp.",
                    LoaiThongBao = "HopDong",
                    LoaiNguoiNhan = "NguoiDung",
                    NguoiNhanId = nguoiNhanId,
                    PhongId = hopDong.MaPhong,
                    NgayTao = DateTime.Now
                };

                _context.ThongBao.Add(tb);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo hợp đồng sắp hết hạn #{MaHopDong}", maHopDong);
            }
        }

        // ── Dịch vụ được duyệt / từ chối / hết hạn ────────────────────────────
        public async Task TaoThongBaoDichVuAsync(int maDangKyDichVu, string trangThaiMoi)
        {
            try
            {
                var dkdv = await _context.DangKyDichVu
                    .Include(d => d.DichVu)
                    .Include(d => d.Phong)
                    .FirstOrDefaultAsync(d => d.MaDangKyDichVu == maDangKyDichVu);

                if (dkdv == null) return;

                string tieuDe, noiDung;
                switch (trangThaiMoi)
                {
                    case "DangSuDung":
                        tieuDe = $"Đăng ký dịch vụ được duyệt - {dkdv.DichVu?.TenDichVu}";
                        noiDung = $"Yêu cầu đăng ký dịch vụ \"{dkdv.DichVu?.TenDichVu}\" cho phòng {dkdv.Phong?.TenPhong} đã được chấp thuận.";
                        break;
                    case "TuChoi":
                        tieuDe = $"Đăng ký dịch vụ bị từ chối - {dkdv.DichVu?.TenDichVu}";
                        noiDung = $"Yêu cầu đăng ký dịch vụ \"{dkdv.DichVu?.TenDichVu}\" cho phòng {dkdv.Phong?.TenPhong} đã bị từ chối. Vui lòng liên hệ chủ trọ để biết thêm.";
                        break;
                    case "HetHan":
                        tieuDe = $"Dịch vụ hết hạn - {dkdv.DichVu?.TenDichVu}";
                        noiDung = $"Dịch vụ \"{dkdv.DichVu?.TenDichVu}\" của bạn tại phòng {dkdv.Phong?.TenPhong} đã hết hạn theo kỳ thuê. Đăng ký lại nếu bạn muốn tiếp tục sử dụng.";
                        break;
                    default:
                        return;
                }

                var tb = new ThongBao
                {
                    TieuDe = tieuDe,
                    NoiDung = noiDung,
                    LoaiThongBao = "DichVu",
                    LoaiNguoiNhan = "NguoiDung",
                    NguoiNhanId = dkdv.MaNguoiDung,
                    PhongId = dkdv.MaPhong,
                    NgayTao = DateTime.Now
                };

                _context.ThongBao.Add(tb);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo dịch vụ #{MaDangKy} trangThai={TrangThai}", maDangKyDichVu, trangThaiMoi);
            }
        }

        // ── Báo cáo sự cố được phản hồi ───────────────────────────────────────
        public async Task TaoThongBaoBaoCaoSuCoAsync(int maBaoCao, string phanHoi)
        {
            try
            {
                var baoCao = await _context.BaoCaoSuCo
                    .Include(b => b.Phong)
                    .FirstOrDefaultAsync(b => b.MaBaoCao == maBaoCao);

                if (baoCao == null) return;

                var tb = new ThongBao
                {
                    TieuDe = $"Phản hồi báo cáo sự cố - {baoCao.TieuDe}",
                    NoiDung = $"Báo cáo sự cố \"{baoCao.TieuDe}\" của bạn đã được phản hồi:\n{phanHoi}",
                    LoaiThongBao = "BaoCaoSuCo",
                    LoaiNguoiNhan = "NguoiDung",
                    NguoiNhanId = baoCao.MaNguoiDung,
                    PhongId = baoCao.MaPhong,
                    NgayTao = DateTime.Now
                };

                _context.ThongBao.Add(tb);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo thông báo phản hồi báo cáo #{MaBaoCao}", maBaoCao);
            }
        }
    }
}
