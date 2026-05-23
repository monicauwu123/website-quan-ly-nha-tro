using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DoAnSE104.Data;
using DoAnSE104.Helpers;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Services;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ThongBaoController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IServiceScopeFactory _scopeFactory;

        public ThongBaoController(
            ApplicationDbContext context,
            IServiceScopeFactory scopeFactory)
        {
            _context = context;
            _scopeFactory = scopeFactory;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung") ?? User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        private string GetCurrentRole()
            => (User.FindFirstValue(ClaimTypes.Role)
                ?? User.FindFirstValue("VaiTro")
                ?? string.Empty).Trim();

        private static ThongBaoDto MapDto(ThongBao tb, bool daDoc = false, bool coTheDanhDauDoc = false)
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
                DaDoc = daDoc,
                CoTheDanhDauDoc = coTheDanhDauDoc,
                NgayDoc = null,
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

        private async Task<List<int>> GetPhongCuaNguoiDungAsync(int userId)
        {
            return await _context.NguoiThue
                .Where(nt => nt.MaNguoiDung == userId)
                .Select(nt => nt.MaPhong)
                .ToListAsync();
        }

        private async Task<List<int>> GetPhongCuaChuTroAsync(int userId)
        {
            return await _context.Phong
                .Include(p => p.NhaTro)
                .Where(p => p.NhaTro != null && p.NhaTro.MaChuTro == userId)
                .Select(p => p.MaPhong)
                .ToListAsync();
        }

        private static bool ThongBaoThuocNguoiDung(ThongBao tb, int userId, List<int> phongCuaToi)
        {
            return tb.LoaiNguoiNhan == "TatCa"
                || (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId == userId)
                || (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && phongCuaToi.Contains(tb.PhongId.Value));
        }

        // GET: Admin/ChuTro xem thông báo đã gửi; NguoiDung xem thông báo nhận được
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            List<ThongBao> list;
            var readIds = new HashSet<int>();
            var coTheDanhDauDoc = role == VaiTroConst.NguoiDung;

            if (role == VaiTroConst.Admin)
            {
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
                var maPhongCuaToi = await GetPhongCuaChuTroAsync(userId);

                list = await _context.ThongBao
                    .Include(tb => tb.NguoiNhan)
                    .Include(tb => tb.Phong)
                    .Include(tb => tb.NguoiTao)
                    .Where(tb => tb.TrangThai != "An" && (
                        tb.NguoiTaoId == userId ||
                        (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && maPhongCuaToi.Contains(tb.PhongId.Value))
                    ))
                    .OrderByDescending(tb => tb.NgayTao)
                    .ToListAsync();
            }
            else
            {
                var phongCuaToi = await GetPhongCuaNguoiDungAsync(userId);

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

                var ids = list.Select(tb => tb.ThongBaoId).ToList();
                var readList = await _context.ThongBaoDaDoc
                    .Where(x => x.MaNguoiDung == userId && ids.Contains(x.ThongBaoId))
                    .Select(x => x.ThongBaoId)
                    .ToListAsync();
                readIds = readList.ToHashSet();
            }

            var result = list.Select(tb => MapDto(tb, readIds.Contains(tb.ThongBaoId), coTheDanhDauDoc)).ToList();
            return Ok(new { thanhCong = true, duLieu = result });
        }

        // GET: đếm thông báo chưa đọc của người nhận. Admin/ChuTro không cần badge đã đọc.
        [HttpGet("chua-doc")]
        public async Task<IActionResult> GetChuaDoc()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            if (role != VaiTroConst.NguoiDung)
                return Ok(new { thanhCong = true, duLieu = 0 });

            var phongCuaToi = await GetPhongCuaNguoiDungAsync(userId);

            var count = await _context.ThongBao
                .Where(tb => tb.TrangThai != "An" && (
                    tb.LoaiNguoiNhan == "TatCa" ||
                    (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId == userId) ||
                    (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && phongCuaToi.Contains(tb.PhongId.Value))
                ))
                .Where(tb => !_context.ThongBaoDaDoc.Any(x => x.ThongBaoId == tb.ThongBaoId && x.MaNguoiDung == userId))
                .CountAsync();

            return Ok(new { thanhCong = true, duLieu = count });
        }

        // POST: Tạo thông báo (Admin/ChuTro)
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> Create([FromBody] ThongBaoCreateDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { thanhCong = false, thongBao = "Dữ liệu không hợp lệ." });

            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var loaiNhanHopLe = new[] { "TatCa", "NhaTro", "Phong", "NguoiDung" };
            if (!loaiNhanHopLe.Contains(dto.LoaiNguoiNhan))
                return BadRequest(new { thanhCong = false, thongBao = "Loại người nhận không hợp lệ." });

            if (dto.LoaiNguoiNhan == "NhaTro" && !dto.NhaTroId.HasValue)
                return BadRequest(new { thanhCong = false, thongBao = "Vui lòng chọn nhà trọ nhận thông báo." });

            if (dto.LoaiNguoiNhan == "Phong" && !dto.PhongId.HasValue)
                return BadRequest(new { thanhCong = false, thongBao = "Vui lòng chọn phòng nhận thông báo." });

            if (dto.LoaiNguoiNhan == "NguoiDung" && !dto.NguoiNhanId.HasValue)
                return BadRequest(new { thanhCong = false, thongBao = "Vui lòng chọn người dùng nhận thông báo." });

            if (dto.LoaiNguoiNhan == "NhaTro" && dto.NhaTroId.HasValue)
            {
                var nhaTroQuery = _context.NhaTro.Where(n => n.MaNhaTro == dto.NhaTroId);
                if (role == VaiTroConst.ChuTro)
                    nhaTroQuery = nhaTroQuery.Where(n => n.MaChuTro == userId);

                var coQuyen = await nhaTroQuery.AnyAsync();
                if (!coQuyen)
                    return StatusCode(403, new { thanhCong = false, thongBao = "Bạn không có quyền gửi thông báo cho nhà trọ này." });
            }

            if (dto.LoaiNguoiNhan == "Phong" && dto.PhongId.HasValue && role == VaiTroConst.ChuTro)
            {
                var coQuyen = await _context.Phong
                    .Include(p => p.NhaTro)
                    .AnyAsync(p => p.MaPhong == dto.PhongId && p.NhaTro != null && p.NhaTro.MaChuTro == userId);
                if (!coQuyen)
                    return StatusCode(403, new { thanhCong = false, thongBao = "Bạn không có quyền gửi thông báo cho phòng này." });
            }

            if (dto.LoaiNguoiNhan == "NguoiDung" && dto.NguoiNhanId.HasValue)
            {
                var exists = await _context.Users.AnyAsync(u => u.MaNguoiDung == dto.NguoiNhanId && u.VaiTro == VaiTroConst.NguoiDung && u.TrangThai);
                if (!exists)
                    return BadRequest(new { thanhCong = false, thongBao = "Người nhận không tồn tại, không phải người dùng hoặc đã bị khóa." });
            }

            if (dto.LoaiNguoiNhan == "NhaTro" && dto.NhaTroId.HasValue)
            {
                var phongIds = await _context.Phong
                    .Where(p => p.MaNhaTro == dto.NhaTroId)
                    .Select(p => p.MaPhong)
                    .ToListAsync();

                if (!phongIds.Any())
                    return BadRequest(new { thanhCong = false, thongBao = "Nhà trọ này chưa có phòng để gửi thông báo." });

                var now = DateTime.Now;
                var thongBaos = phongIds.Select(phongId => new ThongBao
                {
                    TieuDe = dto.TieuDe.Trim(),
                    NoiDung = dto.NoiDung.Trim(),
                    LoaiThongBao = string.IsNullOrWhiteSpace(dto.LoaiThongBao) ? "ThuCong" : dto.LoaiThongBao.Trim(),
                    LoaiNguoiNhan = "Phong",
                    PhongId = phongId,
                    NguoiTaoId = userId,
                    NgayTao = now,
                    TrangThai = "HienThi",
                    DaDoc = false
                }).ToList();

                _context.ThongBao.AddRange(thongBaos);
                await _context.SaveChangesAsync();

                foreach (var thongBao in thongBaos)
                    QueueThongBaoEmail(thongBao.ThongBaoId);

                var ids = thongBaos.Select(x => x.ThongBaoId).ToList();
                var savedList = await _context.ThongBao
                    .Include(x => x.NguoiNhan)
                    .Include(x => x.Phong)
                    .Include(x => x.NguoiTao)
                    .Where(x => ids.Contains(x.ThongBaoId))
                    .OrderByDescending(x => x.ThongBaoId)
                    .ToListAsync();

                return Ok(new
                {
                    thanhCong = true,
                    thongBao = $"Tạo thông báo thành công cho {savedList.Count} phòng trong nhà trọ.",
                    duLieu = savedList.Select(x => MapDto(x, false, false)).ToList()
                });
            }

            var tb = new ThongBao
            {
                TieuDe = dto.TieuDe.Trim(),
                NoiDung = dto.NoiDung.Trim(),
                LoaiThongBao = string.IsNullOrWhiteSpace(dto.LoaiThongBao) ? "ThuCong" : dto.LoaiThongBao.Trim(),
                LoaiNguoiNhan = dto.LoaiNguoiNhan,
                NguoiNhanId = dto.LoaiNguoiNhan == "NguoiDung" ? dto.NguoiNhanId : null,
                PhongId = dto.LoaiNguoiNhan == "Phong" ? dto.PhongId : null,
                NguoiTaoId = userId,
                NgayTao = DateTime.Now,
                TrangThai = "HienThi",
                DaDoc = false
            };

            _context.ThongBao.Add(tb);
            await _context.SaveChangesAsync();
            QueueThongBaoEmail(tb.ThongBaoId);

            await _context.Entry(tb).Reference(x => x.NguoiNhan).LoadAsync();
            await _context.Entry(tb).Reference(x => x.Phong).LoadAsync();
            await _context.Entry(tb).Reference(x => x.NguoiTao).LoadAsync();

            return Ok(new { thanhCong = true, thongBao = "Tạo thông báo thành công.", duLieu = MapDto(tb, false, false) });
        }

        private void QueueThongBaoEmail(int thongBaoId)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<INotificationEmailService>();
                    await service.GuiEmailThongBaoMoiAsync(thongBaoId);
                }
                catch
                {
                    // Email là tác vụ phụ; không để lỗi SMTP làm chậm thao tác tạo thông báo.
                }
            });
        }

        // PUT: Đánh dấu đã đọc 1 thông báo của chính người nhận
        [HttpPut("{id}/da-doc")]
        public async Task<IActionResult> DaDoc(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            if (role != VaiTroConst.NguoiDung)
                return BadRequest(new { thanhCong = false, thongBao = "Chỉ người nhận thông báo mới cần đánh dấu đã đọc." });

            var tb = await _context.ThongBao.FirstOrDefaultAsync(x => x.ThongBaoId == id && x.TrangThai != "An");
            if (tb == null)
                return NotFound(new { thanhCong = false, thongBao = "Không tìm thấy thông báo." });

            var phongCuaToi = await GetPhongCuaNguoiDungAsync(userId);
            if (!ThongBaoThuocNguoiDung(tb, userId, phongCuaToi))
                return Forbid();

            var existed = await _context.ThongBaoDaDoc
                .AnyAsync(x => x.ThongBaoId == id && x.MaNguoiDung == userId);

            if (!existed)
            {
                _context.ThongBaoDaDoc.Add(new ThongBaoDaDoc
                {
                    ThongBaoId = id,
                    MaNguoiDung = userId,
                    NgayDoc = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            return Ok(new { thanhCong = true, thongBao = "Đã đánh dấu đọc." });
        }

        // PUT: Đánh dấu tất cả thông báo của tôi là đã đọc
        [HttpPut("doc-tat-ca")]
        public async Task<IActionResult> DocTatCa()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            if (role != VaiTroConst.NguoiDung)
                return Ok(new { thanhCong = true, thongBao = "Không có thông báo người nhận cần đánh dấu đọc." });

            var phongCuaToi = await GetPhongCuaNguoiDungAsync(userId);

            var chuaDocIds = await _context.ThongBao
                .Where(tb => tb.TrangThai != "An" && (
                    tb.LoaiNguoiNhan == "TatCa" ||
                    (tb.LoaiNguoiNhan == "NguoiDung" && tb.NguoiNhanId == userId) ||
                    (tb.LoaiNguoiNhan == "Phong" && tb.PhongId != null && phongCuaToi.Contains(tb.PhongId.Value))
                ))
                .Where(tb => !_context.ThongBaoDaDoc.Any(x => x.ThongBaoId == tb.ThongBaoId && x.MaNguoiDung == userId))
                .Select(tb => tb.ThongBaoId)
                .ToListAsync();

            var now = DateTime.Now;
            foreach (var tbId in chuaDocIds)
            {
                _context.ThongBaoDaDoc.Add(new ThongBaoDaDoc
                {
                    ThongBaoId = tbId,
                    MaNguoiDung = userId,
                    NgayDoc = now
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { thanhCong = true, thongBao = $"Đã đánh dấu {chuaDocIds.Count} thông báo là đã đọc." });
        }

        // PUT: Ẩn thông báo
        [HttpPut("{id}/an")]
        public async Task<IActionResult> AnThongBao(int id)
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            var tb = await _context.ThongBao.FindAsync(id);
            if (tb == null)
                return NotFound(new { thanhCong = false, thongBao = "Không tìm thấy thông báo." });

            if (role == VaiTroConst.NguoiDung)
                return Forbid();

            if (tb.NguoiTaoId != userId && role != VaiTroConst.Admin)
                return Forbid();

            tb.TrangThai = "An";
            await _context.SaveChangesAsync();

            return Ok(new { thanhCong = true, thongBao = "Đã ẩn thông báo." });
        }

        // GET: Danh sách người dùng & phòng để chọn khi tạo (Admin/ChuTro)
        [HttpGet("init-data")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> GetInitData()
        {
            var userId = GetCurrentUserId();
            var role = GetCurrentRole();

            IQueryable<Phong> phongQuery = _context.Phong.Include(p => p.NhaTro);
            if (role == VaiTroConst.ChuTro)
                phongQuery = phongQuery.Where(p => p.NhaTro != null && p.NhaTro.MaChuTro == userId);

            IQueryable<NhaTro> nhaTroQuery = _context.NhaTro;
            if (role == VaiTroConst.ChuTro)
                nhaTroQuery = nhaTroQuery.Where(n => n.MaChuTro == userId);

            var nhaTros = await nhaTroQuery
                .Select(n => new { n.MaNhaTro, n.TenNhaTro, n.DiaChi })
                .ToListAsync();

            var phongs = await phongQuery
                .Select(p => new { p.MaPhong, p.TenPhong, TenNhaTro = p.NhaTro != null ? p.NhaTro.TenNhaTro : "" })
                .ToListAsync();

            var nguoiDungs = await _context.Users
                .Where(u => u.VaiTro == VaiTroConst.NguoiDung && u.TrangThai)
                .Select(u => new { u.MaNguoiDung, u.HoTen, u.Email, u.SoDienThoai })
                .ToListAsync();

            return Ok(new { thanhCong = true, duLieu = new { nhaTros, phongs, nguoiDungs } });
        }
    }
}
