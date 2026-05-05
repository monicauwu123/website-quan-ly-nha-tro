using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Helpers;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ThongBaoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ThongBaoController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentRole()
            => (User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("VaiTro")
                ?? string.Empty).Trim();

        // ── Helper map DTO ──────────────────────────────────────────────────────
        private static ThongBaoDto MapDto(ThongBao tb)
        {
            return new ThongBaoDto
            {
                ThongBaoId = tb.ThongBaoId,
                TieuDe = tb.TieuDe,
                NoiDung = tb.NoiDung,
                LoaiThongBao = tb.LoaiThongBao,
                LoaiNguoiNhan = tb.LoaiNguoiNhan,
                NguoiNhanId = tb.NguoiNhanId,
                TenNguoiNhan = tb.NguoiNhan?.HoTen ?? tb.NguoiNhan?.Email,
                PhongId = tb.PhongId,
                TenPhong = tb.Phong?.TenPhong,
                NguoiTaoId = tb.NguoiTaoId,
                TenNguoiTao = tb.NguoiTao?.HoTen ?? tb.NguoiTao?.Email,
                DaDoc = tb.DaDoc,
                NgayDoc = tb.NgayDoc,
                NgayTao = tb.NgayTao,
                TrangThai = tb.TrangThai,
                LoaiThongBaoText = LoaiThongBaoText(tb.LoaiThongBao),
                LoaiNguoiNhanText = LoaiNguoiNhanText(tb.LoaiNguoiNhan)
            };
        }

        private static string LoaiThongBaoText(string? loai) => loai switch
        {
            "HoaDon" => "Hóa đơn",
            "HopDong" => "Hợp đồng",
            "DichVu" => "Dịch vụ",
            "BaoCaoSuCo" => "Sự cố",
            "ThuCong" => "Thủ công",
            _ => loai ?? "---"
        };

        private static string LoaiNguoiNhanText(string? loai) => loai switch
        {
            "TatCa" => "Tất cả người thuê",
            "Phong" => "Một phòng",
            "NguoiDung" => "Một người dùng",
            _ => loai ?? "---"
        };

        // ── GET: tất cả (Admin/ChuTro) hoặc của tôi (NguoiDung) ───────────────
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            List<ThongBao> list;

            if (role == VaiTroConst.Admin)
            {
                // Admin thấy toàn bộ
                list = await _context.ThongBao
                    .Include(tb => tb.NguoiNhan)
                    .Include(tb => tb.Phong)
                    .Include(tb => tb.NguoiTao)
                    .Where(tb => tb.TrangThai != "An")
                    .OrderByDescending(tb => tb.NgayTao)
                    .ToListAsync();
            }
            else if (role == VaiTroConst.ChuTro)
            {
                // ChuTro thấy thông báo mà mình tạo + thông báo nhận cho phòng của mình
                var maPhongCuaToi = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Where(p => p.NhaTro.MaChuTro == userId)
                    .Select(p => p.MaPhong)
                    .ToListAsync();

                list = await _context.ThongBao
                    .Include(tb => tb.NguoiNhan)
                    .Include(tb => tb.Phong)
                    .Include(tb => tb.NguoiTao)
                    .Where(tb => tb.TrangThai != "An" && (
                        tb.NguoiTaoId == userId ||
                        tb.LoaiNguoiNhan == "TatCa" ||
                        (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && maPhongCuaToi.Contains(tb.PhongId.Value))
                    ))
                    .OrderByDescending(tb => tb.NgayTao)
                    .ToListAsync();
            }
            else
            {
                // NguoiDung: thấy thông báo gửi TatCa, gửi cho phòng mình, gửi cho chính mình
                var phongCuaToi = await _context.NguoiThue
                    .Where(nt => nt.MaNguoiDung == userId)
                    .Select(nt => nt.MaPhong)
                    .ToListAsync();

                list = await _context.ThongBao
                    .Include(tb => tb.NguoiNhan)
                    .Include(tb => tb.Phong)
                    .Include(tb => tb.NguoiTao)
                    .Where(tb => tb.TrangThai != "An" && (
                        tb.LoaiNguoiNhan == "TatCa" ||
                        (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId == userId) ||
                        (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && phongCuaToi.Contains(tb.PhongId.Value))
                    ))
                    .OrderByDescending(tb => tb.NgayTao)
                    .ToListAsync();
            }

            var result = list.Select(MapDto).ToList();
            return Ok(new { thanhCong = true, duLieu = result });
        }

        // ── GET: đếm chưa đọc (dùng cho badge) ────────────────────────────────
        [HttpGet("chua-doc")]
        public async Task<IActionResult> GetChuaDoc()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            int count;

            if (role == VaiTroConst.NguoiDung)
            {
                var phongCuaToi = await _context.NguoiThue
                    .Where(nt => nt.MaNguoiDung == userId)
                    .Select(nt => nt.MaPhong)
                    .ToListAsync();

                count = await _context.ThongBao
                    .Where(tb => tb.TrangThai != "An" && !tb.DaDoc && (
                        tb.LoaiNguoiNhan == "TatCa" ||
                        (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId == userId) ||
                        (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && phongCuaToi.Contains(tb.PhongId.Value))
                    ))
                    .CountAsync();
            }
            else
            {
                // Admin/ChuTro: đếm thông báo chưa đọc trong scope của mình
                count = await _context.ThongBao
                    .Where(tb => tb.TrangThai != "An" && !tb.DaDoc && tb.NguoiTaoId == userId)
                    .CountAsync();
            }

            return Ok(new { thanhCong = true, duLieu = count });
        }

        // ── POST: Tạo thông báo (Admin/ChuTro) ───────────────────────────────
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> Create([FromBody] ThongBaoCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { thanhCong = false, thongBao = "Dữ liệu không hợp lệ." });

            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var loaiNhanHopLe = new[] { "TatCa", "Phong", "NguoiDung" };
            if (!loaiNhanHopLe.Contains(dto.LoaiNguoiNhan))
                return BadRequest(new { thanhCong = false, thongBao = "Loại người nhận không hợp lệ." });

            if (dto.LoaiNguoiNhan == "Phong" && !dto.PhongId.HasValue)
                return BadRequest(new { thanhCong = false, thongBao = "Vui lòng chọn phòng nhận thông báo." });

            if (dto.LoaiNguoiNhan == "NguoiDung" && !dto.NguoiNhanId.HasValue)
                return BadRequest(new { thanhCong = false, thongBao = "Vui lòng chọn người dùng nhận thông báo." });

            // Kiểm tra quyền nếu chỉ định phòng
            if (dto.LoaiNguoiNhan == "Phong" && dto.PhongId.HasValue)
            {
                if (role == VaiTroConst.ChuTro)
                {
                    var coQuyen = await _context.Phong
                        .Include(p => p.NhaTro)
                        .AnyAsync(p => p.MaPhong == dto.PhongId && p.NhaTro != null && p.NhaTro.MaChuTro == userId);
                    if (!coQuyen)
                        return StatusCode(403, new { thanhCong = false, thongBao = "Bạn không có quyền gửi thông báo cho phòng này." });
                }
            }

            // Kiểm tra người nhận tồn tại
            if (dto.LoaiNguoiNhan == "NguoiDung" && dto.NguoiNhanId.HasValue)
            {
                var exists = await _context.Users.AnyAsync(u => u.MaNguoiDung == dto.NguoiNhanId && u.TrangThai);
                if (!exists)
                    return BadRequest(new { thanhCong = false, thongBao = "Người nhận không tồn tại hoặc đã bị khóa." });
            }

            var tb = new ThongBao
            {
                TieuDe = dto.TieuDe.Trim(),
                NoiDung = dto.NoiDung.Trim(),
                LoaiThongBao = dto.LoaiThongBao,
                LoaiNguoiNhan = dto.LoaiNguoiNhan,
                NguoiNhanId = dto.LoaiNguoiNhan == "NguoiDung" ? dto.NguoiNhanId : null,
                PhongId = dto.LoaiNguoiNhan == "Phong" ? dto.PhongId : null,
                NguoiTaoId = userId,
                NgayTao = DateTime.Now
            };

            _context.ThongBao.Add(tb);
            await _context.SaveChangesAsync();

            await _context.Entry(tb).Reference(x => x.NguoiNhan).LoadAsync();
            await _context.Entry(tb).Reference(x => x.Phong).LoadAsync();
            await _context.Entry(tb).Reference(x => x.NguoiTao).LoadAsync();

            return Ok(new { thanhCong = true, thongBao = "Tạo thông báo thành công.", duLieu = MapDto(tb) });
        }

        // ── PUT: Đánh dấu đã đọc ──────────────────────────────────────────────
        [HttpPut("{id}/da-doc")]
        public async Task<IActionResult> DaDoc(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var tb = await _context.ThongBao.FindAsync(id);
            if (tb == null)
                return NotFound(new { thanhCong = false, thongBao = "Không tìm thấy thông báo." });

            // Chỉ cho phép đánh dấu đọc nếu thông báo thuộc về người dùng
            if (role == VaiTroConst.NguoiDung)
            {
                var phongCuaToi = await _context.NguoiThue
                    .Where(nt => nt.MaNguoiDung == userId)
                    .Select(nt => nt.MaPhong)
                    .ToListAsync();

                bool coQuyen = tb.LoaiNguoiNhan == "TatCa"
                    || (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId == userId)
                    || (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && phongCuaToi.Contains(tb.PhongId.Value));

                if (!coQuyen)
                    return Forbid();
            }

            if (!tb.DaDoc)
            {
                tb.DaDoc = true;
                tb.NgayDoc = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return Ok(new { thanhCong = true, thongBao = "Đã đánh dấu đọc." });
        }

        // ── PUT: Đánh dấu tất cả đã đọc ──────────────────────────────────────
        [HttpPut("doc-tat-ca")]
        public async Task<IActionResult> DocTatCa()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            List<ThongBao> chuaDoc;

            if (role == VaiTroConst.NguoiDung)
            {
                var phongCuaToi = await _context.NguoiThue
                    .Where(nt => nt.MaNguoiDung == userId)
                    .Select(nt => nt.MaPhong)
                    .ToListAsync();

                chuaDoc = await _context.ThongBao
                    .Where(tb => !tb.DaDoc && tb.TrangThai != "An" && (
                        tb.LoaiNguoiNhan == "TatCa" ||
                        (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId == userId) ||
                        (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && phongCuaToi.Contains(tb.PhongId.Value))
                    ))
                    .ToListAsync();
            }
            else
            {
                chuaDoc = await _context.ThongBao
                    .Where(tb => !tb.DaDoc && tb.NguoiTaoId == userId)
                    .ToListAsync();
            }

            var now = DateTime.Now;
            foreach (var tb in chuaDoc)
            {
                tb.DaDoc = true;
                tb.NgayDoc = now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { thanhCong = true, thongBao = $"Đã đánh dấu {chuaDoc.Count} thông báo là đã đọc." });
        }

        // ── PUT: Ẩn thông báo ────────────────────────────────────────────────
        [HttpPut("{id}/an")]
        public async Task<IActionResult> AnThongBao(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var tb = await _context.ThongBao.FindAsync(id);
            if (tb == null)
                return NotFound(new { thanhCong = false, thongBao = "Không tìm thấy thông báo." });

            // Chỉ admin/chủ trọ tạo mới được ẩn
            if (role == VaiTroConst.NguoiDung)
                return Forbid();

            if (tb.NguoiTaoId != userId && role != VaiTroConst.Admin)
                return Forbid();

            tb.TrangThai = "An";
            await _context.SaveChangesAsync();

            return Ok(new { thanhCong = true, thongBao = "Đã ẩn thông báo." });
        }

        // ── GET: Danh sách người dùng & phòng để chọn khi tạo (Admin/ChuTro) ─
        [HttpGet("init-data")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> GetInitData()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            IQueryable<Phong> phongQuery = _context.Phong.Include(p => p.NhaTro);
            if (role == VaiTroConst.ChuTro)
                phongQuery = phongQuery.Where(p => p.NhaTro.MaChuTro == userId);

            var phongs = await phongQuery
                .Select(p => new { p.MaPhong, p.TenPhong, TenNhaTro = p.NhaTro.TenNhaTro })
                .ToListAsync();

            var nguoiDungs = await _context.Users
                .Where(u => u.VaiTro == VaiTroConst.NguoiDung && u.TrangThai)
                .Select(u => new { u.MaNguoiDung, u.HoTen, u.Email, u.SoDienThoai })
                .ToListAsync();

            return Ok(new { thanhCong = true, duLieu = new { phongs, nguoiDungs } });
        }
    }
}
