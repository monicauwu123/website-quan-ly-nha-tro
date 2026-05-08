using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Models;

namespace DoAnSE104.Services
{
    public interface INotificationEmailService
    {
        Task GuiEmailHoaDonMoiAsync(int maHoaDon);
        Task GuiNhacHoaDonChuaThanhToanAsync(DateTime today);
        Task GuiNhacHopDongSapHetHanAsync(DateTime today);
        Task GuiNhacDichVuSapResetAsync(DateTime today);
        Task GuiEmailThongBaoMoiAsync(int thongBaoId);
        Task GuiEmailBaoCaoSuCoMoiAsync(int maBaoCao);
        Task GuiEmailYeuCauThueAsync(int maYeuCau, bool daDuyet);
    }

    public class NotificationEmailService : INotificationEmailService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationEmailService> _logger;

        public NotificationEmailService(
            ApplicationDbContext context,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<NotificationEmailService> logger)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task GuiEmailHoaDonMoiAsync(int maHoaDon)
        {
            var hoaDon = await LoadHoaDon(maHoaDon);
            if (hoaDon == null) return;

            var dueDate = TinhHanThanhToan(hoaDon);
            await GuiHoaDonAsync(hoaDon, "HoaDonMoi", dueDate, "Bạn có hóa đơn mới cần thanh toán.");
        }

        public async Task GuiNhacHoaDonChuaThanhToanAsync(DateTime today)
        {
            var ngay = today.Date;
            var hoaDons = await _context.HoaDon
                .Include(h => h.NguoiThue).ThenInclude(nt => nt.NguoiDungTK)
                .Include(h => h.Phong)
                .Where(h => h.TrangThai != "DaThanhToan" && h.TrangThai != "Huy")
                .ToListAsync();

            foreach (var hoaDon in hoaDons)
            {
                var dueDate = TinhHanThanhToan(hoaDon);
                var days = (dueDate.Date - ngay).Days;

                if (days == 3)
                    await GuiHoaDonAsync(hoaDon, "HoaDonTruocHan3Ngay", dueDate, "Hóa đơn của bạn sắp đến hạn thanh toán.");
                else if (days < 0)
                    await GuiHoaDonAsync(hoaDon, "HoaDonQuaHan", dueDate, "Hóa đơn của bạn đã quá hạn thanh toán.");
            }
        }

        public async Task GuiNhacHopDongSapHetHanAsync(DateTime today)
        {
            var ngay = today.Date;
            var hopDongs = await _context.HopDong
                .Include(h => h.NguoiThue).ThenInclude(nt => nt.NguoiDungTK)
                .Include(h => h.Phong)
                .Where(h => h.TrangThai == "DangHieuLuc" && h.NgayKetThuc != null)
                .ToListAsync();

            foreach (var hopDong in hopDongs)
            {
                var ngayKetThuc = hopDong.NgayKetThuc!.Value.Date;
                var days = (ngayKetThuc - ngay).Days;
                if (days != 7 && days != 3 && days != 1) continue;

                var email = GetTenantEmail(hopDong.NguoiThue);
                if (string.IsNullOrWhiteSpace(email)) continue;

                var tenPhong = hopDong.Phong?.TenPhong ?? $"#{hopDong.MaPhong}";
                var subject = $"Hợp đồng thuê phòng {tenPhong} sắp hết hạn";
                var body = EmailService.WrapTemplate(subject, $@"
                    <p>Xin chào <strong>{Html(hopDong.NguoiThue?.HoTen ?? "bạn")}</strong>,</p>
                    <p>Hợp đồng thuê phòng <strong>{Html(tenPhong)}</strong> của bạn sẽ hết hạn vào ngày <strong>{FormatDate(ngayKetThuc)}</strong>.</p>
                    <p>Vui lòng liên hệ chủ trọ nếu muốn gia hạn.</p>");

                await SendOnceAsync("HopDongSapHetHan" + days, "HopDong", hopDong.MaHopDong, email, hopDong.NguoiThue?.HoTen, subject, body, ngayKetThuc);
            }
        }

        public async Task GuiNhacDichVuSapResetAsync(DateTime today)
        {
            var ngay = today.Date;
            var lastDay = new DateTime(ngay.Year, ngay.Month, DateTime.DaysInMonth(ngay.Year, ngay.Month));
            var days = (lastDay - ngay).Days;
            if (days < 1 || days > 3) return;

            var dangKys = await _context.DangKyDichVu
                .Include(dk => dk.NguoiDung)
                .Include(dk => dk.NguoiThue)
                .Include(dk => dk.Phong)
                .Include(dk => dk.DichVu)
                .Where(dk => dk.TrangThai == "DangSuDung")
                .ToListAsync();

            var groups = dangKys
                .GroupBy(dk => new
                {
                    dk.MaNguoiDung,
                    dk.MaPhong,
                    Email = dk.NguoiThue != null && !string.IsNullOrWhiteSpace(dk.NguoiThue.Email) ? dk.NguoiThue.Email : dk.NguoiDung.Email,
                    Name = dk.NguoiThue != null && !string.IsNullOrWhiteSpace(dk.NguoiThue.HoTen) ? dk.NguoiThue.HoTen : dk.NguoiDung.HoTen
                })
                .Where(g => !string.IsNullOrWhiteSpace(g.Key.Email));

            foreach (var group in groups)
            {
                var first = group.First();
                var tenPhong = first.Phong?.TenPhong ?? $"#{first.MaPhong}";
                var services = string.Join(", ", group.Select(x => x.DichVu?.TenDichVu).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct());
                var subject = $"Dịch vụ phòng {tenPhong} sắp hết hiệu lực";
                var body = EmailService.WrapTemplate(subject, $@"
                    <p>Xin chào <strong>{Html(group.Key.Name ?? "bạn")}</strong>,</p>
                    <p>Các dịch vụ bạn đã đăng ký cho phòng <strong>{Html(tenPhong)}</strong> sẽ hết hiệu lực khi kết thúc kỳ thuê tháng này.</p>
                    <p><strong>Dịch vụ:</strong> {Html(services)}</p>
                    <p>Nếu muốn tiếp tục sử dụng, vui lòng đăng ký lại trong tháng mới.</p>");

                await SendOnceAsync("DichVuSapReset" + days, "Phong", first.MaPhong, group.Key.Email!, group.Key.Name, subject, body, lastDay);
            }
        }

        public async Task GuiEmailThongBaoMoiAsync(int thongBaoId)
        {
            var tb = await _context.ThongBao
                .Include(x => x.NguoiNhan)
                .Include(x => x.Phong)
                .FirstOrDefaultAsync(x => x.ThongBaoId == thongBaoId);
            if (tb == null) return;

            var recipients = await LayNguoiNhanThongBaoAsync(tb);
            foreach (var user in recipients)
            {
                if (string.IsNullOrWhiteSpace(user.Email)) continue;

                var subject = "Thông báo mới từ chủ trọ";
                var body = EmailService.WrapTemplate(subject, $@"
                    <p>Xin chào <strong>{Html(user.HoTen ?? user.TenDangNhap)}</strong>,</p>
                    <p>Bạn có thông báo mới: <strong>{Html(tb.TieuDe)}</strong>.</p>
                    <p>{Html(tb.NoiDung)}</p>
                    <p>Vui lòng đăng nhập hệ thống để xem chi tiết.</p>");

                await SendOnceAsync("ThongBaoMoi", "ThongBao", tb.ThongBaoId, user.Email, user.HoTen, subject, body, tb.NgayTao.Date);
            }
        }

        public async Task GuiEmailBaoCaoSuCoMoiAsync(int maBaoCao)
        {
            var baoCao = await _context.BaoCaoSuCo
                .Include(b => b.NguoiDung)
                .Include(b => b.Phong).ThenInclude(p => p.NhaTro).ThenInclude(n => n.ChuTro)
                .FirstOrDefaultAsync(b => b.MaBaoCao == maBaoCao);
            var chuTro = baoCao?.Phong?.NhaTro?.ChuTro;
            if (baoCao == null || chuTro == null || string.IsNullOrWhiteSpace(chuTro.Email)) return;

            var tenPhong = baoCao.Phong?.TenPhong ?? $"#{baoCao.MaPhong}";
            var subject = $"Có báo cáo sự cố mới từ phòng {tenPhong}";
            var body = EmailService.WrapTemplate(subject, $@"
                <p>Người thuê vừa gửi báo cáo sự cố cho phòng <strong>{Html(tenPhong)}</strong>.</p>
                <p><strong>Tiêu đề:</strong> {Html(baoCao.TieuDe)}</p>
                <p><strong>Nội dung:</strong> {Html(baoCao.NoiDung)}</p>
                <p><strong>Mức độ:</strong> {Html(baoCao.MucDo)}</p>
                <p>Vui lòng kiểm tra và xử lý.</p>");

            await SendOnceAsync("BaoCaoSuCoMoi", "BaoCaoSuCo", baoCao.MaBaoCao, chuTro.Email, chuTro.HoTen, subject, body, baoCao.NgayGui.Date);
        }

        public async Task GuiEmailYeuCauThueAsync(int maYeuCau, bool daDuyet)
        {
            var yeuCau = await _context.YeuCauThue
                .Include(y => y.NguoiDung)
                .Include(y => y.Phong)
                .FirstOrDefaultAsync(y => y.MaYeuCau == maYeuCau);
            if (yeuCau?.NguoiDung == null || string.IsNullOrWhiteSpace(yeuCau.NguoiDung.Email)) return;

            var tenPhong = yeuCau.Phong?.TenPhong ?? $"#{yeuCau.MaPhong}";
            var subject = daDuyet
                ? $"Yêu cầu thuê phòng {tenPhong} đã được duyệt"
                : $"Yêu cầu thuê phòng {tenPhong} đã bị từ chối";
            var body = daDuyet
                ? EmailService.WrapTemplate(subject, $@"
                    <p>Xin chào <strong>{Html(yeuCau.NguoiDung.HoTen ?? yeuCau.NguoiDung.TenDangNhap)}</strong>,</p>
                    <p>Yêu cầu thuê phòng <strong>{Html(tenPhong)}</strong> của bạn đã được chủ trọ duyệt.</p>
                    <p><strong>Thời gian thuê:</strong> {yeuCau.SoThangMuonThue} tháng.</p>
                    <p><strong>Ngày bắt đầu:</strong> {FormatDate(yeuCau.NgayBatDauMongMuon)}.</p>")
                : EmailService.WrapTemplate(subject, $@"
                    <p>Xin chào <strong>{Html(yeuCau.NguoiDung.HoTen ?? yeuCau.NguoiDung.TenDangNhap)}</strong>,</p>
                    <p>Yêu cầu thuê phòng <strong>{Html(tenPhong)}</strong> của bạn đã bị từ chối.</p>
                    <p><strong>Lý do/Ghi chú:</strong> {Html(yeuCau.GhiChuChuTro ?? "Chủ trọ chưa nhập lý do.")}</p>");

            await SendOnceAsync(daDuyet ? "YeuCauThueDuyet" : "YeuCauThueTuChoi", "YeuCauThue", yeuCau.MaYeuCau, yeuCau.NguoiDung.Email, yeuCau.NguoiDung.HoTen, subject, body, yeuCau.NgayXuLy?.Date);
        }

        private async Task GuiHoaDonAsync(HoaDon hoaDon, string eventType, DateTime dueDate, string intro)
        {
            var email = GetTenantEmail(hoaDon.NguoiThue);
            if (string.IsNullOrWhiteSpace(email)) return;

            var tenPhong = hoaDon.Phong?.TenPhong ?? $"#{hoaDon.MaPhong}";
            var ky = FormatKyHoaDon(hoaDon.KyHoaDon);
            var subject = $"Nhắc thanh toán hóa đơn phòng {tenPhong} tháng {ky}";
            var body = EmailService.WrapTemplate(subject, $@"
                <p>Xin chào <strong>{Html(hoaDon.NguoiThue?.HoTen ?? "bạn")}</strong>,</p>
                <p>{Html(intro)}</p>
                <p>Bạn có hóa đơn phòng <strong>{Html(tenPhong)}</strong> kỳ <strong>{Html(ky)}</strong> cần thanh toán.</p>
                <p><strong>Tổng tiền:</strong> {FormatMoney(hoaDon.TongTien)}</p>
                <p><strong>Hạn thanh toán:</strong> {FormatDate(dueDate)}</p>
                <p>Vui lòng thanh toán đúng hạn.</p>");

            await SendOnceAsync(eventType, "HoaDon", hoaDon.MaHoaDon, email, hoaDon.NguoiThue?.HoTen, subject, body, dueDate.Date);
        }

        private async Task<bool> SendOnceAsync(string eventType, string entityType, int entityId, string email, string? name, string subject, string body, DateTime? referenceDate)
        {
            var refDate = referenceDate?.Date;
            var existed = await _context.EmailLog.AnyAsync(x =>
                x.EventType == eventType &&
                x.EntityType == entityType &&
                x.EntityId == entityId &&
                x.RecipientEmail == email &&
                x.ReferenceDate == refDate &&
                x.Status == "Sent");

            if (existed) return false;

            var sent = await _emailService.GuiEmailAsync(email, name ?? email, subject, body);
            _context.EmailLog.Add(new EmailLog
            {
                EventType = eventType,
                EntityType = entityType,
                EntityId = entityId,
                RecipientEmail = email,
                RecipientName = name,
                ReferenceDate = refDate,
                Subject = subject,
                Status = sent ? "Sent" : "Skipped",
                ErrorMessage = sent ? null : "SMTP chưa cấu hình hoặc gửi thất bại.",
                SentAt = DateTime.Now
            });
            await _context.SaveChangesAsync();
            return sent;
        }

        private async Task<HoaDon?> LoadHoaDon(int maHoaDon)
        {
            return await _context.HoaDon
                .Include(h => h.NguoiThue).ThenInclude(nt => nt.NguoiDungTK)
                .Include(h => h.Phong)
                .FirstOrDefaultAsync(h => h.MaHoaDon == maHoaDon);
        }

        private async Task<List<User>> LayNguoiNhanThongBaoAsync(ThongBao tb)
        {
            if (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId.HasValue)
                return await _context.Users.Where(u => u.MaNguoiDung == tb.NguoiNhanId.Value && u.TrangThai).ToListAsync();

            if (tb.LoaiNguoiNhan == "Phong" && tb.PhongId.HasValue)
            {
                var today = DateTime.Today;
                var userIds = await _context.HopDong
                    .Include(h => h.NguoiThue)
                    .Where(h => h.MaPhong == tb.PhongId.Value
                        && h.NgayBatDau.Date <= today
                        && (h.NgayKetThuc == null || h.NgayKetThuc.Value.Date >= today)
                        && h.NguoiThue.MaNguoiDung != null)
                    .Select(h => h.NguoiThue.MaNguoiDung!.Value)
                    .Distinct()
                    .ToListAsync();

                return await _context.Users
                    .Where(u => userIds.Contains(u.MaNguoiDung) && u.TrangThai)
                    .ToListAsync();
            }

            return await _context.Users
                .Where(u => u.VaiTro == "NguoiDung" && u.TrangThai)
                .ToListAsync();
        }

        private DateTime TinhHanThanhToan(HoaDon hoaDon)
        {
            var dueDay = _configuration.GetValue<int?>("EmailReminder:InvoiceDueDay") ?? 25;
            if (!TryParseKy(hoaDon.KyHoaDon, out var year, out var month))
                return hoaDon.NgayLap.Date.AddDays(7);

            var day = Math.Min(Math.Max(dueDay, 1), DateTime.DaysInMonth(year, month));
            return new DateTime(year, month, day);
        }

        private static bool TryParseKy(string? ky, out int year, out int month)
        {
            year = 0;
            month = 0;
            var parts = (ky ?? "").Split('-');
            return parts.Length == 2
                && int.TryParse(parts[0], out year)
                && int.TryParse(parts[1], out month)
                && year >= 1900
                && month is >= 1 and <= 12;
        }

        private static string? GetTenantEmail(NguoiThue? nguoiThue)
        {
            if (!string.IsNullOrWhiteSpace(nguoiThue?.Email))
                return nguoiThue.Email;
            return nguoiThue?.NguoiDungTK?.Email;
        }

        private static string FormatMoney(decimal value)
            => string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), "{0:N0}đ", value);

        private static string FormatDate(DateTime? value)
            => value.HasValue ? value.Value.ToString("dd/MM/yyyy") : "---";

        private static string FormatKyHoaDon(string? ky)
        {
            return TryParseKy(ky, out var year, out var month) ? $"{month:00}/{year:0000}" : (ky ?? "---");
        }

        private static string Html(string? value) => EmailService.EscapeHtml(value);
    }
}
