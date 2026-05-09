using DoAnSE104.Data;
using DoAnSE104.Models;
using Microsoft.EntityFrameworkCore;

namespace DoAnSE104.Services
{
    public class RentalPeriodResetResult
    {
        public int HopDongHetHan { get; set; }
        public int DangKyDichVuHetHan { get; set; }
        public int HoaDonDaChot { get; set; }
        public int PhongDaTraVeTrong { get; set; }
        public DateTime NgayXuLy { get; set; }
        public string KyHienTai { get; set; } = string.Empty;
    }

    public interface IRentalPeriodResetService
    {
        Task<RentalPeriodResetResult> ChotKyThueAsync(int? maChuTro = null, DateTime? mocThoiGian = null);
    }

    public class RentalPeriodResetService : IRentalPeriodResetService
    {
        private const string TrangThaiHopDongDangHieuLuc = "DangHieuLuc";
        private const string TrangThaiHopDongKetThuc = "KetThuc";
        private const string TrangThaiDichVuDangSuDung = "DangSuDung";
        private const string TrangThaiDichVuHetHan = "HetHan";
        private const string TrangThaiHoaDonChuaThanhToan = "ChuaThanhToan";
        private const string TrangThaiHoaDonDaChot = "DaChot";

        private readonly ApplicationDbContext _context;
        private readonly ILogger<RentalPeriodResetService> _logger;

        public RentalPeriodResetService(ApplicationDbContext context, ILogger<RentalPeriodResetService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<RentalPeriodResetResult> ChotKyThueAsync(int? maChuTro = null, DateTime? mocThoiGian = null)
        {
            var today = (mocThoiGian ?? DateTime.Now).Date;
            var kyHienTai = today.ToString("yyyy-MM");
            var result = new RentalPeriodResetResult
            {
                NgayXuLy = today,
                KyHienTai = kyHienTai
            };

            var strategy = _context.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    result.HopDongHetHan = await KetThucHopDongHetHanAsync(today, maChuTro);
                    result.DangKyDichVuHetHan = await HetHanDichVuCuTheoKyThueAsync(today, maChuTro);
                    result.HoaDonDaChot = await ChotHoaDonKyCuAsync(kyHienTai, maChuTro);
                    result.PhongDaTraVeTrong = await CapNhatPhongTrongChoHopDongHetHanAsync(today, maChuTro);

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Lỗi khi reset/chốt kỳ thuê tháng");
                    throw;
                }
            });

            return result;
        }

        private async Task<int> KetThucHopDongHetHanAsync(DateTime today, int? maChuTro)
        {
            var query = _context.HopDong
                .Include(h => h.Phong)
                    .ThenInclude(p => p.NhaTro)
                .Where(h => h.TrangThai == TrangThaiHopDongDangHieuLuc
                    && h.NgayKetThuc.HasValue
                    && h.NgayKetThuc.Value.Date < today);

            if (maChuTro.HasValue)
                query = query.Where(h => h.Phong.NhaTro.MaChuTro == maChuTro.Value);

            var hopDongHetHan = await query.ToListAsync();
            foreach (var hopDong in hopDongHetHan)
            {
                hopDong.TrangThai = TrangThaiHopDongKetThuc;
            }

            return hopDongHetHan.Count;
        }

        private async Task<int> HetHanDichVuCuTheoKyThueAsync(DateTime today, int? maChuTro)
        {
            var hopDongQuery = _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                    .ThenInclude(p => p.NhaTro)
                .Where(h => h.TrangThai == TrangThaiHopDongDangHieuLuc
                    && h.NgayBatDau.Date <= today
                    && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value.Date >= today));

            if (maChuTro.HasValue)
                hopDongQuery = hopDongQuery.Where(h => h.Phong.NhaTro.MaChuTro == maChuTro.Value);

            var hopDongDangHieuLuc = await hopDongQuery.ToListAsync();
            var tongSoDichVuHetHan = 0;

            foreach (var hopDong in hopDongDangHieuLuc)
            {
                var maNguoiDung = hopDong.NguoiThue.MaNguoiDung;
                if (!maNguoiDung.HasValue)
                    continue;

                var dauKyHienTai = TinhDauKyThueHienTai(hopDong.NgayBatDau, today);

                var dangKyCu = await _context.DangKyDichVu
                    .Where(dk => dk.MaPhong == hopDong.MaPhong
                        && dk.TrangThai == TrangThaiDichVuDangSuDung
                        && dk.NgayDangKy.Date < dauKyHienTai
                        && (!dk.MaNguoiThue.HasValue || dk.MaNguoiThue == hopDong.MaNguoiThue)
                        && dk.MaNguoiDung == maNguoiDung.Value)
                    .ToListAsync();

                foreach (var dangKy in dangKyCu)
                {
                    dangKy.TrangThai = TrangThaiDichVuHetHan;
                    dangKy.NgayHetHan = dauKyHienTai;
                    dangKy.NgayHuy ??= dauKyHienTai;
                    dangKy.GhiChu = ThemGhiChuHeThong(dangKy.GhiChu, $"Tự hết hạn khi sang kỳ thuê mới {dauKyHienTai:yyyy-MM-dd}");
                }

                tongSoDichVuHetHan += dangKyCu.Count;
            }

            return tongSoDichVuHetHan;
        }

        private async Task<int> ChotHoaDonKyCuAsync(string kyHienTai, int? maChuTro)
        {
            var query = _context.HoaDon
                .Include(h => h.Phong)
                    .ThenInclude(p => p.NhaTro)
                .Where(h => h.TrangThai == TrangThaiHoaDonChuaThanhToan
                    && string.Compare(h.KyHoaDon, kyHienTai) < 0);

            if (maChuTro.HasValue)
                query = query.Where(h => h.Phong.NhaTro.MaChuTro == maChuTro.Value);

            var hoaDonKyCu = await query.ToListAsync();
            foreach (var hoaDon in hoaDonKyCu)
            {
                hoaDon.TrangThai = TrangThaiHoaDonDaChot;
            }

            return hoaDonKyCu.Count;
        }

        private async Task<int> CapNhatPhongTrongChoHopDongHetHanAsync(DateTime today, int? maChuTro)
        {
            var trangThaiTrong = await _context.TrangThai
                .Where(t => t.TenTrangThai.Contains("trống") || t.TenTrangThai.Contains("trong"))
                .OrderBy(t => t.MaTrangThai)
                .FirstOrDefaultAsync();

            if (trangThaiTrong == null)
                return 0;

            var phongQuery = _context.Phong
                .Include(p => p.NhaTro)
                .Where(p => !_context.HopDong.Any(h => h.MaPhong == p.MaPhong
                    && h.TrangThai == TrangThaiHopDongDangHieuLuc
                    && h.NgayBatDau.Date <= today
                    && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value.Date >= today)));

            if (maChuTro.HasValue)
                phongQuery = phongQuery.Where(p => p.NhaTro.MaChuTro == maChuTro.Value);

            var phongCanTrong = await phongQuery
                .Where(p => p.MaTrangThai != trangThaiTrong.MaTrangThai)
                .ToListAsync();

            foreach (var phong in phongCanTrong)
            {
                phong.MaTrangThai = trangThaiTrong.MaTrangThai;
            }

            return phongCanTrong.Count;
        }

        private static DateTime TinhDauKyThueHienTai(DateTime ngayBatDau, DateTime today)
        {
            var start = ngayBatDau.Date;
            if (today <= start)
                return start;

            var months = ((today.Year - start.Year) * 12) + today.Month - start.Month;
            var candidate = start.AddMonths(months);
            if (candidate > today)
                candidate = candidate.AddMonths(-1);

            return candidate;
        }

        private static string ThemGhiChuHeThong(string? ghiChuCu, string ghiChuMoi)
        {
            if (string.IsNullOrWhiteSpace(ghiChuCu))
                return ghiChuMoi;

            if (ghiChuCu.Contains(ghiChuMoi, StringComparison.OrdinalIgnoreCase))
                return ghiChuCu;

            return ghiChuCu.Length + ghiChuMoi.Length + 3 <= 500
                ? $"{ghiChuCu} | {ghiChuMoi}"
                : ghiChuCu;
        }
    }
}
