using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DoAnSE104.Data;
using DoAnSE104.Models;
using DoAnSE104.Models.Dtos;
using DoAnSE104.Helpers;
using DoAnSE104.Services.Interfaces;

namespace DoAnSE104.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PhongController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly Cloudinary _cloudinary;
        private readonly IDeleteValidationService _deleteValidationService;

        public PhongController(ApplicationDbContext context, Cloudinary cloudinary, IDeleteValidationService deleteValidationService)
        {
            _context = context;
            _cloudinary = cloudinary;
            _deleteValidationService = deleteValidationService;
        }

        private int GetCurrentUserId()
            => int.Parse(User.FindFirstValue("MaNguoiDung")!);

        private string GetCurrentRole()
            => User.FindFirstValue(ClaimTypes.Role)!;

        private async Task<List<int>> GetMaPhongCuaChuTro(int userId)
        {
            var maNhaTroList = await _context.NhaTro
                .Where(n => n.MaChuTro == userId)
                .Select(n => n.MaNhaTro)
                .ToListAsync();

            return await _context.Phong
                .Where(p => maNhaTroList.Contains(p.MaNhaTro))
                .Select(p => p.MaPhong)
                .ToListAsync();
        }


        private IQueryable<Phong> ChiLayPhongConTrong(IQueryable<Phong> query)
        {
            // Chỉ hiển thị phòng có trạng thái "Còn trống".
            // Trạng thái là nguồn sự thật duy nhất — khi tạo/duyệt hợp đồng hoặc gia hạn,
            // phòng được chuyển sang "Đã thuê"; khi hợp đồng kết thúc sẽ chuyển lại "Còn trống".
            return query.Where(p =>
                p.TrangThai.TenTrangThai.Contains("trống") ||
                p.TrangThai.TenTrangThai.Contains("Trống") ||
                p.TrangThai.TenTrangThai.Contains("trong") ||
                p.TrangThai.TenTrangThai.Contains("Trong")
            );
        }

        private async Task<string?> ValidatePhong(Phong phong)
        {
            if (phong.GiaPhong < 0)
                return "Giá phòng phải lớn hơn hoặc bằng 0";

            if (phong.DienTich.HasValue && phong.DienTich.Value < 0)
                return "Diện tích phải lớn hơn hoặc bằng 0";

            if (phong.SucChua < 0)
                return "Sức chứa phải lớn hơn hoặc bằng 0";

            var role = GetCurrentRole();
            var userId = GetCurrentUserId();

            var nhaTro = await _context.NhaTro.FirstOrDefaultAsync(n => n.MaNhaTro == phong.MaNhaTro);
            if (nhaTro == null)
                return "Nhà trọ không tồn tại";

            if (role == VaiTroConst.ChuTro && nhaTro.MaChuTro != userId)
                return "Bạn không có quyền dùng nhà trọ này";

            var loaiPhong = await _context.LoaiPhong
                .Include(l => l.NhaTro)
                .FirstOrDefaultAsync(l => l.MaLoaiPhong == phong.MaLoaiPhong);
            if (loaiPhong == null)
                return "Loại phòng không tồn tại";

            if (loaiPhong.MaNhaTro != phong.MaNhaTro)
                return "Loại phòng không thuộc nhà trọ đã chọn";

            if (role == VaiTroConst.ChuTro)
            {
                var loaiPhongThuocChuTro = loaiPhong.NhaTro?.MaChuTro == userId ||
                    (loaiPhong.MaNhaTro == null && loaiPhong.MaChuTro == userId);

                if (!loaiPhongThuocChuTro)
                    return "Bạn không có quyền dùng loại phòng này";
            }

            if (!await _context.TrangThai.AnyAsync(t => t.MaTrangThai == phong.MaTrangThai))
                return "Trạng thái không tồn tại";

            return null;
        }

        // GET: api/Phong
        [HttpGet]
        public async Task<IActionResult> GetPhong()
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<Phong> query = _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai);

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    query = query.Where(p => maPhongList.Contains(p.MaPhong));
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    // Người dùng chỉ thấy phòng còn trống để lựa chọn thuê.
                    query = ChiLayPhongConTrong(query);
                }

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<Phong>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/Phong/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPhong(int id)
        {
            try
            {
                var phong = await _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai)
                    .FirstOrDefaultAsync(p => p.MaPhong == id);

                if (phong == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy phòng"));

                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    if (!maPhongList.Contains(id)) return Forbid();
                }
                // NguoiDung được xem chi tiết phòng để lựa chọn thuê.

                return Ok(ApiResponse<Phong>.Ok(phong));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/Phong/NhaTro/5
        [HttpGet("NhaTro/{nhaTroId}")]
        public async Task<IActionResult> GetPhongByNhaTro(int nhaTroId)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                // ChuTro kiểm tra nhà trọ có phải của mình không
                if (role == VaiTroConst.ChuTro)
                {
                    var nhaTro = await _context.NhaTro.FindAsync(nhaTroId);
                    if (nhaTro == null || nhaTro.MaChuTro != userId)
                        return Forbid();
                }

                IQueryable<Phong> query = _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai)
                    .Where(p => p.MaNhaTro == nhaTroId);

                if (role == VaiTroConst.NguoiDung)
                {
                    // Người dùng chỉ thấy phòng còn trống trong nhà trọ đã chọn.
                    query = ChiLayPhongConTrong(query);
                }

                var data = await query.ToListAsync();

                return Ok(ApiResponse<List<Phong>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // GET: api/Phong/TrangThai/5
        [HttpGet("TrangThai/{trangThaiId}")]
        public async Task<IActionResult> GetPhongByTrangThai(int trangThaiId)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                IQueryable<Phong> query = _context.Phong
                    .Include(p => p.NhaTro)
                    .Include(p => p.LoaiPhong)
                    .Include(p => p.TrangThai)
                    .Where(p => p.MaTrangThai == trangThaiId);

                if (role == VaiTroConst.ChuTro)
                {
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    query = query.Where(p => maPhongList.Contains(p.MaPhong));
                }
                else if (role == VaiTroConst.NguoiDung)
                {
                    // Người dùng chỉ thấy phòng còn trống để lựa chọn thuê.
                    query = ChiLayPhongConTrong(query);
                }

                var data = await query.ToListAsync();
                return Ok(ApiResponse<List<Phong>>.Ok(data));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/Phong
        [HttpPost]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PostPhong([FromBody] Phong phong)
        {
            try
            {
                var role = GetCurrentRole();
                var userId = GetCurrentUserId();

                // ChuTro chỉ được tạo phòng thuộc nhà trọ của mình
                if (role == VaiTroConst.ChuTro)
                {
                    var nhaTro = await _context.NhaTro.FindAsync(phong.MaNhaTro);
                    if (nhaTro == null || nhaTro.MaChuTro != userId)
                        return Forbid();
                }

                var loiValidation = await ValidatePhong(phong);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                var phongMoi = new Phong
                {
                    MaNhaTro = phong.MaNhaTro,
                    MaLoaiPhong = phong.MaLoaiPhong,
                    MaTrangThai = phong.MaTrangThai,
                    TenPhong = phong.TenPhong,
                    DienTich = phong.DienTich,
                    GiaPhong = phong.GiaPhong,
                    SucChua = phong.SucChua,
                    MoTa = phong.MoTa,
                    HinhAnh = phong.HinhAnh,
                    DanhSachHinhAnh = phong.DanhSachHinhAnh,
                    DichVuGanPhong = phong.DichVuGanPhong,
                    DiaChiPhong = phong.DiaChiPhong
                };

                _context.Phong.Add(phongMoi);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetPhong), new { id = phongMoi.MaPhong },
                    ApiResponse<Phong>.Ok(phongMoi, "Tạo phòng thành công"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // POST: api/Phong/UploadImage
        // POST: api/Phong/upload-image
        [HttpPost("UploadImage")]
        [HttpPost("upload-image")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage([FromForm] UploadImageDto dto)
        {
            try
            {
                var file = dto?.File;
                if (file == null || file.Length == 0)
                    return BadRequest(ApiResponse<object>.Loi("Vui lòng chọn file ảnh"));

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension) || !allowedContentTypes.Contains(file.ContentType.ToLowerInvariant()))
                    return BadRequest(ApiResponse<object>.Loi("Chỉ chấp nhận ảnh JPG, PNG hoặc WEBP"));

                if (file.Length > 5 * 1024 * 1024)
                    return BadRequest(ApiResponse<object>.Loi("Kích thước file không được vượt quá 5MB"));

                var uploadParams = new ImageUploadParams()
                {
                    File = new FileDescription(file.FileName, file.OpenReadStream()),
                    Folder = "phong_images",
                    Transformation = new Transformation().Width(800).Height(600).Crop("fill").Quality("auto")
                };

                var uploadResult = await _cloudinary.UploadAsync(uploadParams);

                if (uploadResult.StatusCode == System.Net.HttpStatusCode.OK && uploadResult.SecureUrl != null)
                {
                    var data = new { url = uploadResult.SecureUrl.AbsoluteUri, publicId = uploadResult.PublicId };
                    return Ok(ApiResponse<object>.Ok(data, "Upload ảnh thành công"));
                }

                var cloudinaryError = uploadResult.Error?.Message;
                return StatusCode(500, ApiResponse<object>.Loi(
                    string.IsNullOrWhiteSpace(cloudinaryError)
                        ? "Lỗi upload ảnh lên Cloudinary"
                        : $"Lỗi upload ảnh lên Cloudinary: {cloudinaryError}"));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi($"Lỗi xử lý ảnh: {ex.Message}"));
            }
        }

        // PUT: api/Phong/5
        [HttpPut("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> PutPhong(int id, [FromBody] Phong phong)
        {
            try
            {
                if (id != phong.MaPhong)
                    return BadRequest(ApiResponse<object>.Loi("Mã phòng không khớp với tham số URL"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro)
                {
                    var userId = GetCurrentUserId();
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    if (!maPhongList.Contains(id)) return Forbid();

                    var nhaTro = await _context.NhaTro.FindAsync(phong.MaNhaTro);
                    if (nhaTro == null || nhaTro.MaChuTro != userId)
                        return Forbid();
                }

                var loiValidation = await ValidatePhong(phong);
                if (loiValidation != null)
                    return BadRequest(ApiResponse<object>.Loi(loiValidation));

                _context.Entry(phong).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<Phong>.Ok(phong, "Cập nhật thành công"));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Phong.Any(e => e.MaPhong == id))
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy phòng cần cập nhật"));
                throw;
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }

        // DELETE: api/Phong/5
        // Logic:
        //   Chưa có dữ liệu liên quan → Xóa cứng
        //   Đã có hợp đồng / hóa đơn / khách thuê → Chuyển trạng thái NgungHoatDong
        [HttpDelete("{id}")]
        [Authorize(Roles = $"{VaiTroConst.Admin},{VaiTroConst.ChuTro}")]
        public async Task<IActionResult> DeletePhong(int id)
        {
            try
            {
                var phong = await _context.Phong
                    .Include(p => p.TrangThai)
                    .FirstOrDefaultAsync(p => p.MaPhong == id);

                if (phong == null)
                    return NotFound(ApiResponse<object>.Loi("Không tìm thấy phòng cần xóa"));

                var role = GetCurrentRole();
                if (role == VaiTroConst.ChuTro)
                {
                    var userId = GetCurrentUserId();
                    var maPhongList = await GetMaPhongCuaChuTro(userId);
                    if (!maPhongList.Contains(id)) return Forbid();
                }

                // Kiểm tra dữ liệu liên quan
                var result = await _deleteValidationService.DeletePhongAsync(id);
                return this.ToActionResult(result);

                var coHopDong   = await _context.HopDong.AnyAsync(h => h.MaPhong == id);
                var coHoaDon    = await _context.HoaDon.AnyAsync(hd => hd.MaPhong == id);
                var coNguoiThue = await _context.NguoiThue.AnyAsync(nt => nt.MaPhong == id);
                var coBaoCao    = await _context.BaoCaoSuCo.AnyAsync(b => b.MaPhong == id);
                var coYeuCau    = await _context.YeuCauThue.AnyAsync(y => y.MaPhong == id);

                if (!coHopDong && !coHoaDon && !coNguoiThue && !coBaoCao && !coYeuCau)
                {
                    // Chưa phát sinh dữ liệu → Xóa cứng
                    _context.Phong.Remove(phong);
                    await _context.SaveChangesAsync();
                    return Ok(ApiResponse<object>.Ok(null!, "Đã xóa phòng thành công"));
                }
                else
                {
                    // Đã có dữ liệu liên quan → Tìm trạng thái "Ngưng hoạt động" trong bảng TrangThai
                    var trangThaiNgung = await _context.TrangThai
                        .FirstOrDefaultAsync(t => t.TenTrangThai.Contains("Ngưng") || t.TenTrangThai.Contains("ngung"));

                    if (trangThaiNgung != null)
                        phong.MaTrangThai = trangThaiNgung.MaTrangThai;

                    await _context.SaveChangesAsync();

                    var lyDo = new List<string>();
                    if (coHopDong)   lyDo.Add("hợp đồng");
                    if (coHoaDon)    lyDo.Add("hóa đơn");
                    if (coNguoiThue) lyDo.Add("khách thuê");
                    if (coBaoCao)    lyDo.Add("báo cáo sự cố");
                    if (coYeuCau)    lyDo.Add("yêu cầu thuê");

                    return Ok(ApiResponse<object>.Ok(null!,
                        $"Phòng đã có {string.Join(", ", lyDo)}. " +
                        "Đã chuyển sang trạng thái \"Ngưng hoạt động\"."));
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.Loi(ex.Message));
            }
        }
    }
}
