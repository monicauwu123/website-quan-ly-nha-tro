using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DoAnSE104.Services
{
    public enum DeleteAction
    {
        HardDeleted,
        SoftDeleted,
        Blocked,
        NotFound
    }

    public class DeleteResult
    {
        public DeleteAction Action { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool Success => Action == DeleteAction.HardDeleted || Action == DeleteAction.SoftDeleted;

        public static DeleteResult Hard(string message = "Đã xóa thành công.")
            => new() { Action = DeleteAction.HardDeleted, Message = message };

        public static DeleteResult Soft(string message)
            => new() { Action = DeleteAction.SoftDeleted, Message = message };

        public static DeleteResult Block(string message)
            => new() { Action = DeleteAction.Blocked, Message = message };

        public static DeleteResult Missing(string message = "Không tìm thấy dữ liệu.")
            => new() { Action = DeleteAction.NotFound, Message = message };
    }

    public class DeleteValidationService : IDeleteValidationService
    {
        private const string DangHieuLuc = "DangHieuLuc";
        private const string KetThuc = "KetThuc";
        private const string DaHuy = "DaHuy";
        private const string Huy = "Huy";
        private const string ChuaThanhToan = "ChuaThanhToan";
        private const string DaThanhToan = "DaThanhToan";
        private const string DaChot = "DaChot";
        private const string ChoDuyet = "ChoDuyet";
        private const string DangSuDung = "DangSuDung";
        private const string HetHan = "HetHan";
        private const string DaXacNhan = "DaXacNhan";
        private const string ChoXacNhan = "ChoXacNhan";

        private readonly ApplicationDbContext _context;

        public DeleteValidationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<DeleteResult> DeleteNhaTroAsync(int id)
        {
            var nhaTro = await _context.NhaTro.FindAsync(id);
            if (nhaTro == null || nhaTro.TrangThai == "DaXoa")
                return DeleteResult.Missing("Không tìm thấy nhà trọ.");

            var maPhong = _context.Phong.Where(p => p.MaNhaTro == id).Select(p => p.MaPhong);
            if (await CoHopDongHieuLucTheoPhongAsync(maPhong))
                return DeleteResult.Block("Nhà trọ đang có phòng được thuê, vui lòng kết thúc hợp đồng trước.");

            if (await CoHoaDonChuaThanhToanTheoPhongAsync(maPhong))
                return DeleteResult.Block("Nhà trọ đang có hóa đơn chưa thanh toán, vui lòng xử lý hóa đơn trước.");

            var coLienKet = await _context.Phong.AnyAsync(p => p.MaNhaTro == id)
                || await _context.LoaiPhong.AnyAsync(lp => lp.MaNhaTro == id)
                || await _context.DichVu.AnyAsync(dv => dv.MaNhaTro == id)
                || await _context.HopDong.AnyAsync(h => maPhong.Contains(h.MaPhong))
                || await _context.HoaDon.AnyAsync(hd => maPhong.Contains(hd.MaPhong))
                || await _context.YeuCauThue.AnyAsync(y => maPhong.Contains(y.MaPhong));

            if (!coLienKet)
            {
                _context.NhaTro.Remove(nhaTro);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            nhaTro.TrangThai = "NgungHoatDong";
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Nhà trọ đang có dữ liệu liên quan, hệ thống đã chuyển sang ngừng hoạt động.");
        }

        public async Task<DeleteResult> DeletePhongAsync(int id)
        {
            var phong = await _context.Phong.FindAsync(id);
            if (phong == null)
                return DeleteResult.Missing("Không tìm thấy phòng.");

            if (await CoHopDongHieuLucTheoPhongAsync(_context.Phong.Where(p => p.MaPhong == id).Select(p => p.MaPhong)))
                return DeleteResult.Block("Phòng đang có hợp đồng hiệu lực, vui lòng kết thúc hợp đồng trước khi xóa.");

            if (await _context.HoaDon.AnyAsync(hd => hd.MaPhong == id && hd.TrangThai == ChuaThanhToan))
                return DeleteResult.Block("Phòng đang có hóa đơn chưa thanh toán, vui lòng xử lý hóa đơn trước khi xóa.");

            if (await _context.NguoiThue.AnyAsync(nt => nt.MaPhong == id && nt.TrangThai == "DangThue"))
                return DeleteResult.Block("Phòng đang có người thuê đang hoạt động, vui lòng kết thúc hợp đồng trước.");

            if (await _context.DangKyDichVu.AnyAsync(dk => dk.MaPhong == id && dk.TrangThai == DangSuDung))
                return DeleteResult.Block("Phòng đang có dịch vụ đang sử dụng, không thể xóa.");

            var coLichSu = await _context.HopDong.AnyAsync(h => h.MaPhong == id)
                || await _context.HoaDon.AnyAsync(hd => hd.MaPhong == id)
                || await _context.YeuCauThue.AnyAsync(y => y.MaPhong == id)
                || await _context.BaoCaoSuCo.AnyAsync(b => b.MaPhong == id)
                || await _context.ChiSoDien.AnyAsync(c => c.MaPhong == id)
                || await _context.ChiSoNuoc.AnyAsync(c => c.MaPhong == id)
                || await _context.DangKyDichVu.AnyAsync(dk => dk.MaPhong == id);

            if (!coLichSu)
            {
                _context.Phong.Remove(phong);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            var trangThaiNgung = await TimTrangThaiPhongAsync("ngung", "ngừng", "không", "khong");
            if (trangThaiNgung != null)
                phong.MaTrangThai = trangThaiNgung.MaTrangThai;

            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Phòng đã có lịch sử sử dụng nên chỉ được ngừng sử dụng, không xóa vĩnh viễn.");
        }

        public async Task<DeleteResult> DeleteNguoiThueAsync(int id)
        {
            var nguoiThue = await _context.NguoiThue.FindAsync(id);
            if (nguoiThue == null || nguoiThue.TrangThai == "DaXoa")
                return DeleteResult.Missing("Không tìm thấy người thuê.");

            if (await _context.HopDong.AnyAsync(h => h.MaNguoiThue == id
                && h.TrangThai == DangHieuLuc
                && h.NgayBatDau <= DateTime.Now
                && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value >= DateTime.Now)))
                return DeleteResult.Block("Người thuê đang có hợp đồng hiệu lực. Vui lòng kết thúc hợp đồng trước khi xóa.");

            if (await _context.HoaDon.AnyAsync(hd => hd.MaNguoiThue == id && hd.TrangThai == ChuaThanhToan))
                return DeleteResult.Block("Người thuê đang có hóa đơn chưa thanh toán, không thể xóa.");

            if (await _context.ThanhToan.AnyAsync(t => t.MaNguoiThue == id && t.TrangThaiXacNhan == ChoXacNhan))
                return DeleteResult.Block("Người thuê đang có thanh toán chưa xử lý, không thể xóa.");

            var coLichSu = await _context.HopDong.AnyAsync(h => h.MaNguoiThue == id)
                || await _context.HoaDon.AnyAsync(hd => hd.MaNguoiThue == id)
                || await _context.ThanhToan.AnyAsync(t => t.MaNguoiThue == id)
                || await _context.YeuCauThue.AnyAsync(y => y.MaNguoiThue == id);

            if (!coLichSu)
            {
                _context.NguoiThue.Remove(nguoiThue);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            nguoiThue.TrangThai = "KhongHoatDong";
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Người thuê đã có lịch sử thuê, hệ thống đã chuyển sang không hoạt động.");
        }

        public async Task<DeleteResult> DeleteHopDongAsync(int id)
        {
            var hopDong = await _context.HopDong.FindAsync(id);
            if (hopDong == null || hopDong.TrangThai == Huy || hopDong.TrangThai == DaHuy)
                return DeleteResult.Missing("Không tìm thấy hợp đồng.");

            if (hopDong.TrangThai == DangHieuLuc)
                return DeleteResult.Block("Không thể xóa hợp đồng đang hiệu lực. Vui lòng kết thúc hoặc hủy hợp đồng trước.");

            var coLichSu = await CoDuLieuLienQuanHopDongAsync(hopDong);

            if (!coLichSu)
            {
                _context.HopDong.Remove(hopDong);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            hopDong.TrangThai = KetThuc;
            hopDong.NgayKetThuc ??= DateTime.Now;
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Hợp đồng đã có lịch sử liên quan, hệ thống đã chuyển sang kết thúc.");
        }

        public async Task<DeleteResult> HuyHopDongAsync(int id)
        {
            var hopDong = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .FirstOrDefaultAsync(h => h.MaHopDong == id);

            if (hopDong == null || hopDong.TrangThai == Huy || hopDong.TrangThai == DaHuy)
                return DeleteResult.Missing("Không tìm thấy hợp đồng.");

            var coLichSu = await CoDuLieuLienQuanHopDongAsync(hopDong);
            if (!coLichSu)
            {
                _context.HopDong.Remove(hopDong);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã hủy và xóa hợp đồng chưa phát sinh dữ liệu.");
            }

            var today = DateTime.Now.Date;
            hopDong.TrangThai = DaHuy;
            if (!hopDong.NgayKetThuc.HasValue || hopDong.NgayKetThuc.Value.Date > today)
                hopDong.NgayKetThuc = today;

            await HuyDuLieuChoDuyetVaDongActiveAsync(hopDong, today);

            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Hợp đồng đã phát sinh dữ liệu nên hệ thống đã chuyển sang đã hủy.");
        }

        public async Task<DeleteResult> KetThucHopDongAsync(int id)
        {
            var hopDong = await _context.HopDong
                .Include(h => h.NguoiThue)
                .Include(h => h.Phong)
                .FirstOrDefaultAsync(h => h.MaHopDong == id);

            if (hopDong == null || hopDong.TrangThai == Huy || hopDong.TrangThai == DaHuy)
                return DeleteResult.Missing("Không tìm thấy hợp đồng.");

            if (await CoHoaDonChuaThanhToanCuaHopDongAsync(hopDong))
                return DeleteResult.Block("Hợp đồng còn hóa đơn chưa thanh toán. Vui lòng xử lý hóa đơn trước khi kết thúc hợp đồng.");

            var today = DateTime.Now.Date;
            hopDong.TrangThai = KetThuc;
            if (!hopDong.NgayKetThuc.HasValue || hopDong.NgayKetThuc.Value.Date > today)
                hopDong.NgayKetThuc = today;

            var yeuCauGiaHan = await _context.YeuCauGiaHan
                .Where(y => y.MaHopDong == id && y.TrangThai == ChoDuyet)
                .ToListAsync();
            foreach (var yeuCau in yeuCauGiaHan)
            {
                yeuCau.TrangThai = DaHuy;
                yeuCau.NgayXuLy = DateTime.Now;
            }

            var dangKyDichVu = await _context.DangKyDichVu
                .Where(dk => dk.MaPhong == hopDong.MaPhong
                    && dk.TrangThai == DangSuDung
                    && (!dk.MaNguoiThue.HasValue || dk.MaNguoiThue == hopDong.MaNguoiThue))
                .ToListAsync();
            foreach (var dangKy in dangKyDichVu)
            {
                dangKy.TrangThai = HetHan;
                dangKy.NgayHetHan = today;
                dangKy.NgayHuy ??= today;
            }

            if (!await _context.HopDong.AnyAsync(h => h.MaHopDong != id
                && h.MaPhong == hopDong.MaPhong
                && h.TrangThai == DangHieuLuc
                && h.NgayBatDau <= DateTime.Now
                && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value >= DateTime.Now)))
            {
                var trangThaiTrong = await TimTrangThaiPhongAsync("trống", "trong");
                if (trangThaiTrong != null && hopDong.Phong != null)
                    hopDong.Phong.MaTrangThai = trangThaiTrong.MaTrangThai;
            }

            if (!await _context.HopDong.AnyAsync(h => h.MaHopDong != id
                && h.MaNguoiThue == hopDong.MaNguoiThue
                && h.TrangThai == DangHieuLuc
                && h.NgayBatDau <= DateTime.Now
                && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value >= DateTime.Now)))
                hopDong.NguoiThue.TrangThai = "KhongHoatDong";

            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Đã kết thúc hợp đồng và cập nhật các dữ liệu liên quan.");
        }

        public async Task<DeleteResult> DeleteHoaDonAsync(int id)
        {
            var hoaDon = await _context.HoaDon.Include(hd => hd.ChiTietHoaDon).FirstOrDefaultAsync(hd => hd.MaHoaDon == id);
            if (hoaDon == null || hoaDon.TrangThai == Huy || hoaDon.TrangThai == DaHuy)
                return DeleteResult.Missing("Không tìm thấy hóa đơn.");

            var coThanhToanXacNhan = await _context.ThanhToan.AnyAsync(t => t.MaHoaDon == id && t.TrangThaiXacNhan == DaXacNhan);
            if (hoaDon.TrangThai == DaThanhToan || hoaDon.TrangThai == DaChot || coThanhToanXacNhan)
                return DeleteResult.Block("Hóa đơn đã chốt hoặc đã thanh toán, không thể xóa.");

            var coThanhToan = await _context.ThanhToan.AnyAsync(t => t.MaHoaDon == id);
            if (!coThanhToan)
            {
                _context.ChiTietHoaDon.RemoveRange(hoaDon.ChiTietHoaDon ?? new List<ChiTietHoaDon>());
                _context.HoaDon.Remove(hoaDon);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            hoaDon.TrangThai = DaHuy;
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Hóa đơn đã có thanh toán liên quan, không thể xóa vĩnh viễn. Hệ thống đã chuyển sang đã hủy.");
        }

        public async Task<DeleteResult> DeleteThanhToanAsync(int id)
        {
            var thanhToan = await _context.ThanhToan.FindAsync(id);
            if (thanhToan == null)
                return DeleteResult.Missing("Không tìm thấy thanh toán.");

            if (thanhToan.TrangThaiXacNhan == DaXacNhan)
                return DeleteResult.Block("Thanh toán đã được xác nhận, không thể xóa.");

            if (thanhToan.TrangThaiXacNhan == ChoXacNhan || string.IsNullOrWhiteSpace(thanhToan.TrangThaiXacNhan))
            {
                _context.ThanhToan.Remove(thanhToan);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            thanhToan.TrangThaiXacNhan = "TuChoi";
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Thanh toán đã phát sinh xử lý nên đã chuyển sang từ chối.");
        }

        public async Task<DeleteResult> DeleteLoaiPhongAsync(int id)
        {
            var loaiPhong = await _context.LoaiPhong.FindAsync(id);
            if (loaiPhong == null || loaiPhong.TrangThai == "DaXoa")
                return DeleteResult.Missing("Không tìm thấy loại phòng.");

            if (await _context.Phong.AnyAsync(p => p.MaLoaiPhong == id))
                return DeleteResult.Block("Loại phòng đang được sử dụng bởi phòng trọ, không thể xóa.");

            _context.LoaiPhong.Remove(loaiPhong);
            await _context.SaveChangesAsync();
            return DeleteResult.Hard("Đã xóa thành công.");
        }

        public async Task<DeleteResult> DeleteDichVuAsync(int id)
        {
            var dichVu = await _context.DichVu.FindAsync(id);
            if (dichVu == null || dichVu.TrangThai == "DaXoa")
                return DeleteResult.Missing("Không tìm thấy dịch vụ.");

            if (await _context.DangKyDichVu.AnyAsync(dk => dk.MaDichVu == id && dk.TrangThai == DangSuDung))
                return DeleteResult.Block("Dịch vụ đang có người sử dụng, không thể xóa.");

            var coLichSu = await _context.DangKyDichVu.AnyAsync(dk => dk.MaDichVu == id)
                || await _context.LichSuGiaDichVu.AnyAsync(l => l.MaDichVu == id)
                || await _context.ChiTietHoaDon.AnyAsync(ct => ct.LoaiKhoan.StartsWith($"DichVu:{id}:"));

            if (!coLichSu)
            {
                _context.DichVu.Remove(dichVu);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            dichVu.TrangThai = "NgungCungCap";
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Dịch vụ đã phát sinh lịch sử, hệ thống đã chuyển sang ngừng cung cấp.");
        }

        public async Task<DeleteResult> DeleteDangKyDichVuAsync(int id)
        {
            var dangKy = await _context.DangKyDichVu.FindAsync(id);
            if (dangKy == null)
                return DeleteResult.Missing("Không tìm thấy đăng ký dịch vụ.");

            var daTinhHoaDon = await _context.ChiTietHoaDon.AnyAsync(ct => ct.LoaiKhoan.StartsWith($"DichVu:{dangKy.MaDichVu}:")
                && _context.HoaDon.Any(hd => hd.MaHoaDon == ct.MaHoaDon && hd.MaPhong == dangKy.MaPhong));

            if (daTinhHoaDon)
                return DeleteResult.Block("Đăng ký dịch vụ đã được tính vào hóa đơn, không thể xóa.");

            if (dangKy.TrangThai == ChoDuyet)
            {
                _context.DangKyDichVu.Remove(dangKy);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            dangKy.TrangThai = DaHuy;
            dangKy.NgayHuy = DateTime.Now;
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Đăng ký dịch vụ đã có lịch sử nên hệ thống đã chuyển sang đã hủy.");
        }

        public async Task<DeleteResult> DeleteYeuCauThueAsync(int id)
        {
            var yeuCau = await _context.YeuCauThue.FindAsync(id);
            if (yeuCau == null)
                return DeleteResult.Missing("Không tìm thấy yêu cầu thuê.");

            if ((yeuCau.TrangThai == "DaDuyet" || yeuCau.TrangThai == "DaLapHopDong" || yeuCau.TrangThai == "ChoNguoiThueXacNhan") && (yeuCau.MaHopDong.HasValue || yeuCau.MaNguoiThue.HasValue))
                return DeleteResult.Block("Yêu cầu đã được duyệt và đã sinh dữ liệu, không thể xóa.");

            if (yeuCau.TrangThai == ChoDuyet && !yeuCau.MaHopDong.HasValue && !yeuCau.MaNguoiThue.HasValue)
            {
                _context.YeuCauThue.Remove(yeuCau);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            yeuCau.TrangThai = DaHuy;
            yeuCau.NgayXuLy = DateTime.Now;
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Yêu cầu thuê đã có lịch sử xử lý nên hệ thống đã chuyển sang đã hủy.");
        }

        public async Task<DeleteResult> DeleteYeuCauGiaHanAsync(int id)
        {
            var yeuCau = await _context.YeuCauGiaHan.Include(y => y.HopDong).FirstOrDefaultAsync(y => y.MaYeuCauGiaHan == id);
            if (yeuCau == null)
                return DeleteResult.Missing("Không tìm thấy yêu cầu gia hạn.");

            if (yeuCau.TrangThai == "DaDuyet")
                return DeleteResult.Block("Yêu cầu gia hạn đã được duyệt vì đã thay đổi hợp đồng, không thể xóa.");

            if (yeuCau.TrangThai == ChoDuyet)
            {
                _context.YeuCauGiaHan.Remove(yeuCau);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            yeuCau.TrangThai = DaHuy;
            yeuCau.NgayXuLy = DateTime.Now;
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Yêu cầu gia hạn đã có lịch sử xử lý nên hệ thống đã chuyển sang đã hủy.");
        }

        public async Task<DeleteResult> DeleteBaoCaoSuCoAsync(int id)
        {
            var baoCao = await _context.BaoCaoSuCo.FindAsync(id);
            if (baoCao == null)
                return DeleteResult.Missing("Không tìm thấy báo cáo sự cố.");

            if (baoCao.TrangThai == "DangXuLy" || baoCao.TrangThai == "HoanThanh")
                return DeleteResult.Block("Báo cáo sự cố đang hoặc đã được xử lý, không thể xóa để giữ lịch sử.");

            if (baoCao.TrangThai == "Moi")
            {
                _context.BaoCaoSuCo.Remove(baoCao);
                await _context.SaveChangesAsync();
                return DeleteResult.Hard("Đã xóa thành công.");
            }

            baoCao.TrangThai = "DaAn";
            await _context.SaveChangesAsync();
            return DeleteResult.Soft("Báo cáo sự cố đã có lịch sử nên hệ thống đã chuyển sang đã ẩn.");
        }

        public async Task<DeleteResult> DeleteChiSoDienAsync(int id)
        {
            var chiSo = await _context.ChiSoDien.FindAsync(id);
            if (chiSo == null)
                return DeleteResult.Missing("Không tìm thấy chỉ số điện.");

            if (await _context.HoaDon.AnyAsync(hd => hd.MaDien == id && (hd.TrangThai == DaChot || hd.TrangThai == DaThanhToan)))
                return DeleteResult.Block("Chỉ số điện/nước đã được dùng trong hóa đơn đã chốt, không thể xóa.");

            if (await _context.HoaDon.AnyAsync(hd => hd.MaDien == id))
                return DeleteResult.Block("Chỉ số điện đã được dùng trong hóa đơn, vui lòng xử lý hóa đơn trước.");

            _context.ChiSoDien.Remove(chiSo);
            await _context.SaveChangesAsync();
            return DeleteResult.Hard("Đã xóa thành công.");
        }

        public async Task<DeleteResult> DeleteChiSoNuocAsync(int id)
        {
            var chiSo = await _context.ChiSoNuoc.FindAsync(id);
            if (chiSo == null)
                return DeleteResult.Missing("Không tìm thấy chỉ số nước.");

            if (await _context.HoaDon.AnyAsync(hd => hd.MaNuoc == id && (hd.TrangThai == DaChot || hd.TrangThai == DaThanhToan)))
                return DeleteResult.Block("Chỉ số điện/nước đã được dùng trong hóa đơn đã chốt, không thể xóa.");

            if (await _context.HoaDon.AnyAsync(hd => hd.MaNuoc == id))
                return DeleteResult.Block("Chỉ số nước đã được dùng trong hóa đơn, vui lòng xử lý hóa đơn trước.");

            _context.ChiSoNuoc.Remove(chiSo);
            await _context.SaveChangesAsync();
            return DeleteResult.Hard("Đã xóa thành công.");
        }

        private static bool LaHopDongHieuLuc(HopDong hopDong)
        {
            var now = DateTime.Now;
            return hopDong.TrangThai == DangHieuLuc
                && hopDong.NgayBatDau <= now
                && (!hopDong.NgayKetThuc.HasValue || hopDong.NgayKetThuc.Value >= now);
        }

        private async Task<bool> CoHopDongHieuLucTheoPhongAsync(IQueryable<int> maPhong)
            => await _context.HopDong.AnyAsync(h => maPhong.Contains(h.MaPhong)
                && h.TrangThai == DangHieuLuc
                && h.NgayBatDau <= DateTime.Now
                && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value >= DateTime.Now));

        private async Task<bool> CoHoaDonChuaThanhToanTheoPhongAsync(IQueryable<int> maPhong)
            => await _context.HoaDon.AnyAsync(hd => maPhong.Contains(hd.MaPhong) && hd.TrangThai == ChuaThanhToan);

        private async Task<bool> CoDuLieuLienQuanHopDongAsync(HopDong hopDong)
        {
            return await _context.HoaDon.AnyAsync(hd => hd.MaPhong == hopDong.MaPhong && hd.MaNguoiThue == hopDong.MaNguoiThue)
                || await _context.ThanhToan.AnyAsync(t => t.MaNguoiThue == hopDong.MaNguoiThue
                    && _context.HoaDon.Any(hd => hd.MaHoaDon == t.MaHoaDon
                        && hd.MaPhong == hopDong.MaPhong
                        && hd.MaNguoiThue == hopDong.MaNguoiThue))
                || await _context.YeuCauGiaHan.AnyAsync(y => y.MaHopDong == hopDong.MaHopDong)
                || await _context.DangKyDichVu.AnyAsync(dk => dk.MaPhong == hopDong.MaPhong
                    && (!dk.MaNguoiThue.HasValue || dk.MaNguoiThue == hopDong.MaNguoiThue));
        }

        private async Task HuyDuLieuChoDuyetVaDongActiveAsync(HopDong hopDong, DateTime today)
        {
            var yeuCauGiaHan = await _context.YeuCauGiaHan
                .Where(y => y.MaHopDong == hopDong.MaHopDong && y.TrangThai == ChoDuyet)
                .ToListAsync();

            foreach (var yeuCau in yeuCauGiaHan)
            {
                yeuCau.TrangThai = DaHuy;
                yeuCau.NgayXuLy = DateTime.Now;
            }

            var dangKyDichVu = await _context.DangKyDichVu
                .Where(dk => dk.MaPhong == hopDong.MaPhong
                    && dk.TrangThai == DangSuDung
                    && (!dk.MaNguoiThue.HasValue || dk.MaNguoiThue == hopDong.MaNguoiThue))
                .ToListAsync();

            foreach (var dangKy in dangKyDichVu)
            {
                dangKy.TrangThai = HetHan;
                dangKy.NgayHetHan = today;
                dangKy.NgayHuy ??= today;
            }

            if (!await _context.HopDong.AnyAsync(h => h.MaHopDong != hopDong.MaHopDong
                && h.MaPhong == hopDong.MaPhong
                && h.TrangThai == DangHieuLuc
                && h.NgayBatDau <= DateTime.Now
                && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value >= DateTime.Now)))
            {
                var trangThaiTrong = await TimTrangThaiPhongAsync("trống", "trong");
                if (trangThaiTrong != null && hopDong.Phong != null)
                    hopDong.Phong.MaTrangThai = trangThaiTrong.MaTrangThai;
            }

            if (!await _context.HopDong.AnyAsync(h => h.MaHopDong != hopDong.MaHopDong
                && h.MaNguoiThue == hopDong.MaNguoiThue
                && h.TrangThai == DangHieuLuc
                && h.NgayBatDau <= DateTime.Now
                && (!h.NgayKetThuc.HasValue || h.NgayKetThuc.Value >= DateTime.Now))
                && hopDong.NguoiThue != null)
            {
                hopDong.NguoiThue.TrangThai = "KhongHoatDong";
            }
        }

        private async Task<bool> CoHoaDonChuaThanhToanCuaHopDongAsync(HopDong hopDong)
        {
            var hoaDonList = await _context.HoaDon
                .Where(hd => hd.MaPhong == hopDong.MaPhong
                    && hd.MaNguoiThue == hopDong.MaNguoiThue
                    && hd.TrangThai != Huy
                    && hd.TrangThai != DaHuy)
                .ToListAsync();

            foreach (var hoaDon in hoaDonList)
            {
                if (hoaDon.TrangThai == ChuaThanhToan)
                    return true;

                var daXacNhan = await _context.ThanhToan
                    .Where(t => t.MaHoaDon == hoaDon.MaHoaDon && t.TrangThaiXacNhan == DaXacNhan)
                    .SumAsync(t => (decimal?)t.TongTien) ?? 0m;

                if (daXacNhan < hoaDon.TongTien)
                    return true;
            }

            return false;
        }

        private async Task<TrangThai?> TimTrangThaiPhongAsync(params string[] keywords)
        {
            var trangThai = await _context.TrangThai.ToListAsync();
            return trangThai.FirstOrDefault(t =>
                keywords.Any(k => t.TenTrangThai.Contains(k, StringComparison.OrdinalIgnoreCase)));
        }
    }
}
