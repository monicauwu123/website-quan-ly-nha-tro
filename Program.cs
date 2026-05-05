using Microsoft.EntityFrameworkCore;
using DoAnSE104.Data;
using DoAnSE104.Configurations;
using CloudinaryDotNet;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using DoAnSE104.Services;
using DoAnSE104.Models;
using DoAnSE104.Helpers;

var builder = WebApplication.CreateBuilder(args);

// ─── DB ───────────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null
        )
    );
});

// ─── Logging ──────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// ─── JWT Authentication ───────────────────────────────────────────────────────
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),

            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };
    });

builder.Services.AddAuthorization();

// ─── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IRentalPeriodResetService, RentalPeriodResetService>();
builder.Services.AddScoped<IMonthlyInvoiceService, MonthlyInvoiceService>();
builder.Services.AddScoped<IThongBaoService, ThongBaoService>();

// ─── Controllers ──────────────────────────────────────────────────────────────
builder.Services.AddControllers(options =>
{
    // Tránh ASP.NET tự bắt buộc navigation property như NhaTro, LoaiPhong, TrangThai...
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
})
.ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value != null && x.Value.Errors.Count > 0)
            .SelectMany(x => x.Value!.Errors.Select(error =>
            {
                var fieldName = x.Key;

                var errorMessage = string.IsNullOrWhiteSpace(error.ErrorMessage)
                    ? "Dữ liệu không hợp lệ"
                    : error.ErrorMessage;

                return string.IsNullOrWhiteSpace(fieldName)
                    ? errorMessage
                    : $"{fieldName}: {errorMessage}";
            }))
            .ToList();

        var message = errors.Any()
            ? string.Join("; ", errors)
            : "Dữ liệu gửi lên không hợp lệ";

        return new BadRequestObjectResult(ApiResponse<object>.Loi(message));
    };
});

builder.Services.AddEndpointsApiExplorer();

// ─── Swagger với JWT ──────────────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DoAnSE104 API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization. Nhập: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─── Cloudinary ───────────────────────────────────────────────────────────────
builder.Services.Configure<CloudinarySettings>(
    builder.Configuration.GetSection("CloudinarySettings")
);

builder.Services.AddSingleton<Cloudinary>(sp =>
{
    var settings = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;

    var account = new Account(
        settings.CloudName,
        settings.ApiKey,
        settings.ApiSecret
    );

    return new Cloudinary(account);
});

// ─── CORS ─────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// ─── Tự động cập nhật database + Seed Admin mặc định ──────────────────────────
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Tự động tạo/cập nhật database theo migration
        var recreateDatabase = builder.Configuration.GetValue<bool>("Database:RecreateOnStartup");

        if (recreateDatabase)
        {
            context.Database.EnsureDeleted();
        }

        context.Database.EnsureCreated();

        if (!context.TrangThai.Any())
        {
            context.TrangThai.AddRange(
                new TrangThai { TenTrangThai = "Còn trống" },
                new TrangThai { TenTrangThai = "Đã thuê" },
                new TrangThai { TenTrangThai = "Đang sửa chữa" },
                new TrangThai { TenTrangThai = "Ngưng hoạt động" }
            );

            context.SaveChanges();
        }

        // Tạo tài khoản Admin mặc định nếu chưa có
        if (!context.Users.Any(u => u.TenDangNhap == "Admin"))
        {
            context.Users.Add(new User
            {
                TenDangNhap = "Admin",
                HoTen = "Administrator",
                Email = "admin@example.com",
                SoDienThoai = "0123456789",
                VaiTro = "Admin",
                MatKhau = BCrypt.Net.BCrypt.HashPassword("Admin123")
            });

            context.SaveChanges();
        }

        if (builder.Configuration.GetValue<bool>("Database:SeedSampleData"))
        {
            SeedSampleData(context);
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        logger.LogError(ex, "Lỗi khi tự động cập nhật database hoặc seed dữ liệu mặc định.");
    }
}

// ─── Middleware Pipeline ───────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();

app.MapFallbackToFile("index.html");

app.Run();

static void SeedSampleData(ApplicationDbContext context)
{
    if (context.NhaTro.Any())
    {
        return;
    }

    var password = BCrypt.Net.BCrypt.HashPassword("123456");

    var chuTro = new User
    {
        TenDangNhap = "chutro",
        HoTen = "Chủ Trọ Demo",
        Email = "chutro@example.com",
        SoDienThoai = "0900000001",
        VaiTro = "ChuTro",
        MatKhau = password,
        TenNganHang = "Vietcombank",
        MaNganHang = "VCB",
        SoTaiKhoan = "1234567890",
        TenChuTaiKhoan = "CHU TRO DEMO",
        NoiDungChuyenKhoanMacDinh = "Thanh toán tiền phòng"
    };

    var chuTro2 = new User
    {
        TenDangNhap = "chutro2",
        HoTen = "Nguyễn Minh Quân",
        Email = "chutro2@example.com",
        SoDienThoai = "0900000003",
        VaiTro = "ChuTro",
        MatKhau = password,
        TenNganHang = "Techcombank",
        MaNganHang = "TCB",
        SoTaiKhoan = "9876543210",
        TenChuTaiKhoan = "NGUYEN MINH QUAN",
        NoiDungChuyenKhoanMacDinh = "Tiền phòng hàng tháng"
    };

    var nguoiDung = new User
    {
        TenDangNhap = "nguoithue",
        HoTen = "Người Thuê Demo",
        Email = "nguoithue@example.com",
        SoDienThoai = "0900000002",
        CCCD = "012345678901",
        VaiTro = "NguoiDung",
        MatKhau = password
    };

    var nguoiDung2 = new User
    {
        TenDangNhap = "nguoithue2",
        HoTen = "Trần Thị Mai",
        Email = "nguoithue2@example.com",
        SoDienThoai = "0900000004",
        CCCD = "012345678902",
        VaiTro = "NguoiDung",
        MatKhau = password
    };

    var nguoiDung3 = new User
    {
        TenDangNhap = "nguoithue3",
        HoTen = "Lê Văn Nam",
        Email = "nguoithue3@example.com",
        SoDienThoai = "0900000005",
        CCCD = "012345678903",
        VaiTro = "NguoiDung",
        MatKhau = password
    };

    context.Users.AddRange(chuTro, chuTro2, nguoiDung, nguoiDung2, nguoiDung3);
    context.SaveChanges();

    var nhaTroA = new NhaTro
    {
        TenNhaTro = "Nhà trọ An Bình",
        DiaChi = "123 Đường Lê Lợi, Quận 1",
        MoTa = "Khu nhà trọ gần trung tâm, có camera an ninh",
        MaChuTro = chuTro.MaNguoiDung
    };

    var nhaTroB = new NhaTro
    {
        TenNhaTro = "Ký túc xá Mini Hoa Sen",
        DiaChi = "45 Nguyễn Văn Cừ, Quận 5",
        MoTa = "Phòng nhỏ giá tốt cho sinh viên",
        MaChuTro = chuTro.MaNguoiDung
    };

    var nhaTroC = new NhaTro
    {
        TenNhaTro = "Căn hộ dịch vụ Minh Quân",
        DiaChi = "88 Phạm Văn Đồng, Thủ Đức",
        MoTa = "Căn hộ dịch vụ đầy đủ nội thất",
        MaChuTro = chuTro2.MaNguoiDung
    };

    var nhaTroTrong = new NhaTro
    {
        TenNhaTro = "Nhà trọ mới chưa phát sinh",
        DiaChi = "12 Đường Số 7, Bình Thạnh",
        MoTa = "Dữ liệu mẫu để test xóa cứng nhà trọ",
        MaChuTro = chuTro2.MaNguoiDung
    };

    context.NhaTro.AddRange(nhaTroA, nhaTroB, nhaTroC, nhaTroTrong);
    context.SaveChanges();

    var loaiPhongThuongA = new LoaiPhong
    {
        TenLoaiPhong = "Phòng thường",
        MoTa = "Phòng cơ bản, phù hợp một đến hai người",
        MaNhaTro = nhaTroA.MaNhaTro,
        MaChuTro = chuTro.MaNguoiDung
    };

    var loaiPhongVipA = new LoaiPhong
    {
        TenLoaiPhong = "Phòng VIP",
        MoTa = "Phòng rộng, có nội thất",
        MaNhaTro = nhaTroA.MaNhaTro,
        MaChuTro = chuTro.MaNguoiDung
    };

    var loaiPhongSinhVienB = new LoaiPhong
    {
        TenLoaiPhong = "Phòng sinh viên",
        MoTa = "Phòng tiết kiệm chi phí",
        MaNhaTro = nhaTroB.MaNhaTro,
        MaChuTro = chuTro.MaNguoiDung
    };

    var loaiCanHoC = new LoaiPhong
    {
        TenLoaiPhong = "Căn hộ mini",
        MoTa = "Có bếp riêng và ban công",
        MaNhaTro = nhaTroC.MaNhaTro,
        MaChuTro = chuTro2.MaNguoiDung
    };

    context.LoaiPhong.AddRange(loaiPhongThuongA, loaiPhongVipA, loaiPhongSinhVienB, loaiCanHoC);
    context.SaveChanges();

    var trangThaiTrong = GetTrangThaiId(context, "trống", "trong");
    var trangThaiDaThue = GetTrangThaiId(context, "thuê", "thue");
    var trangThaiSuaChua = GetTrangThaiId(context, "sửa", "sua");

    var phongA101 = new Phong { TenPhong = "A101", MaNhaTro = nhaTroA.MaNhaTro, MaLoaiPhong = loaiPhongThuongA.MaLoaiPhong, MaTrangThai = trangThaiDaThue, DienTich = 22, GiaPhong = 2500000, SucChua = 2, SoNguoiHienTai = 1, MoTa = "Phòng đang có hợp đồng", DiaChiPhong = "Tầng 1" };
    var phongA102 = new Phong { TenPhong = "A102", MaNhaTro = nhaTroA.MaNhaTro, MaLoaiPhong = loaiPhongVipA.MaLoaiPhong, MaTrangThai = trangThaiTrong, DienTich = 28, GiaPhong = 3500000, SucChua = 3, SoNguoiHienTai = 0, MoTa = "Phòng trống để test yêu cầu thuê", DiaChiPhong = "Tầng 1" };
    var phongA201 = new Phong { TenPhong = "A201", MaNhaTro = nhaTroA.MaNhaTro, MaLoaiPhong = loaiPhongThuongA.MaLoaiPhong, MaTrangThai = trangThaiSuaChua, DienTich = 20, GiaPhong = 2300000, SucChua = 2, SoNguoiHienTai = 0, MoTa = "Phòng đang sửa chữa", DiaChiPhong = "Tầng 2" };
    var phongB101 = new Phong { TenPhong = "B101", MaNhaTro = nhaTroB.MaNhaTro, MaLoaiPhong = loaiPhongSinhVienB.MaLoaiPhong, MaTrangThai = trangThaiDaThue, DienTich = 18, GiaPhong = 1800000, SucChua = 2, SoNguoiHienTai = 1, MoTa = "Phòng sinh viên đang thuê", DiaChiPhong = "Dãy B" };
    var phongB102 = new Phong { TenPhong = "B102", MaNhaTro = nhaTroB.MaNhaTro, MaLoaiPhong = loaiPhongSinhVienB.MaLoaiPhong, MaTrangThai = trangThaiTrong, DienTich = 18, GiaPhong = 1800000, SucChua = 2, SoNguoiHienTai = 0, MoTa = "Phòng trống giá tốt", DiaChiPhong = "Dãy B" };
    var phongC301 = new Phong { TenPhong = "C301", MaNhaTro = nhaTroC.MaNhaTro, MaLoaiPhong = loaiCanHoC.MaLoaiPhong, MaTrangThai = trangThaiDaThue, DienTich = 32, GiaPhong = 4500000, SucChua = 3, SoNguoiHienTai = 1, MoTa = "Căn hộ mini đang thuê", DiaChiPhong = "Tầng 3" };
    var phongC302 = new Phong { TenPhong = "C302", MaNhaTro = nhaTroC.MaNhaTro, MaLoaiPhong = loaiCanHoC.MaLoaiPhong, MaTrangThai = trangThaiTrong, DienTich = 35, GiaPhong = 4800000, SucChua = 3, SoNguoiHienTai = 0, MoTa = "Căn hộ trống có ban công", DiaChiPhong = "Tầng 3" };

    context.Phong.AddRange(phongA101, phongA102, phongA201, phongB101, phongB102, phongC301, phongC302);
    context.SaveChanges();

    var khachA = new NguoiThue { HoTen = "Người Thuê Demo", CCCD = "012345678901", SDT = "0900000002", Email = "nguoithue@example.com", GioiTinh = "Nam", QuocTich = "Việt Nam", DiaChi = "456 Đường Test", MaPhong = phongA101.MaPhong, MaNguoiDung = nguoiDung.MaNguoiDung };
    var khachB = new NguoiThue { HoTen = "Trần Thị Mai", CCCD = "012345678902", SDT = "0900000004", Email = "nguoithue2@example.com", GioiTinh = "Nữ", QuocTich = "Việt Nam", DiaChi = "22 Nguyễn Trãi", MaPhong = phongB101.MaPhong, MaNguoiDung = nguoiDung2.MaNguoiDung };
    var khachC = new NguoiThue { HoTen = "Lê Văn Nam", CCCD = "012345678903", SDT = "0900000005", Email = "nguoithue3@example.com", GioiTinh = "Nam", QuocTich = "Việt Nam", DiaChi = "9 Võ Văn Tần", MaPhong = phongC301.MaPhong, MaNguoiDung = nguoiDung3.MaNguoiDung };
    var khachCu = new NguoiThue { HoTen = "Phạm Hoài An", CCCD = "012345678904", SDT = "0900000006", Email = "an@example.com", GioiTinh = "Nữ", QuocTich = "Việt Nam", DiaChi = "77 Cách Mạng Tháng 8", MaPhong = phongA101.MaPhong, TrangThai = "KhongHoatDong" };

    context.NguoiThue.AddRange(khachA, khachB, khachC, khachCu);
    context.SaveChanges();

    context.HopDong.AddRange(
        new HopDong { MaNguoiThue = khachA.MaNguoiThue, MaPhong = phongA101.MaPhong, NgayBatDau = DateTime.Today.AddMonths(-2), NgayKetThuc = DateTime.Today.AddMonths(10), TienCoc = 2500000, NoiDung = "Hợp đồng phòng A101", TrangThai = "DangHieuLuc" },
        new HopDong { MaNguoiThue = khachB.MaNguoiThue, MaPhong = phongB101.MaPhong, NgayBatDau = DateTime.Today.AddMonths(-1), NgayKetThuc = DateTime.Today.AddMonths(5), TienCoc = 1800000, NoiDung = "Hợp đồng phòng B101", TrangThai = "DangHieuLuc" },
        new HopDong { MaNguoiThue = khachC.MaNguoiThue, MaPhong = phongC301.MaPhong, NgayBatDau = DateTime.Today.AddMonths(-3), NgayKetThuc = DateTime.Today.AddMonths(9), TienCoc = 4500000, NoiDung = "Hợp đồng căn hộ C301", TrangThai = "DangHieuLuc" },
        new HopDong { MaNguoiThue = khachCu.MaNguoiThue, MaPhong = phongA101.MaPhong, NgayBatDau = DateTime.Today.AddMonths(-14), NgayKetThuc = DateTime.Today.AddMonths(-3), TienCoc = 2000000, NoiDung = "Hợp đồng đã kết thúc", TrangThai = "KetThuc" }
    );

    var internetA = new DichVu { TenDichVu = "Internet", Tiendichvu = 100000, MaNhaTro = nhaTroA.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var veSinhA = new DichVu { TenDichVu = "Vệ sinh", Tiendichvu = 50000, MaNhaTro = nhaTroA.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var giuXeA = new DichVu { TenDichVu = "Giữ xe", Tiendichvu = 80000, MaNhaTro = nhaTroA.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var internetB = new DichVu { TenDichVu = "Internet sinh viên", Tiendichvu = 70000, MaNhaTro = nhaTroB.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var mayGiatB = new DichVu { TenDichVu = "Máy giặt chung", Tiendichvu = 60000, MaNhaTro = nhaTroB.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var donPhongC = new DichVu { TenDichVu = "Dọn phòng", Tiendichvu = 150000, MaNhaTro = nhaTroC.MaNhaTro, MaChuTro = chuTro2.MaNguoiDung };

    context.DichVu.AddRange(internetA, veSinhA, giuXeA, internetB, mayGiatB, donPhongC);
    context.SaveChanges();

    var now = DateTime.Today;
    var kyHoaDon = now.ToString("yyyy-MM");

    context.DangKyDichVu.AddRange(
        new DangKyDichVu { MaNguoiDung = nguoiDung.MaNguoiDung, MaNguoiThue = khachA.MaNguoiThue, MaPhong = phongA101.MaPhong, MaDichVu = internetA.MaDichVu, NgayDangKy = now.AddMonths(-2), GhiChu = "Đăng ký mặc định" },
        new DangKyDichVu { MaNguoiDung = nguoiDung.MaNguoiDung, MaNguoiThue = khachA.MaNguoiThue, MaPhong = phongA101.MaPhong, MaDichVu = veSinhA.MaDichVu, NgayDangKy = now.AddMonths(-2) },
        new DangKyDichVu { MaNguoiDung = nguoiDung2.MaNguoiDung, MaNguoiThue = khachB.MaNguoiThue, MaPhong = phongB101.MaPhong, MaDichVu = internetB.MaDichVu, NgayDangKy = now.AddMonths(-1) },
        new DangKyDichVu { MaNguoiDung = nguoiDung3.MaNguoiDung, MaNguoiThue = khachC.MaNguoiThue, MaPhong = phongC301.MaPhong, MaDichVu = donPhongC.MaDichVu, NgayDangKy = now.AddMonths(-3) }
    );

    var dienA = new ChiSoDien { MaPhong = phongA101.MaPhong, SoDienCu = 120, SoDienMoi = 160, GiaDien = 3500, TienDien = 140000, NgayThangDien = now };
    var nuocA = new ChiSoNuoc { MaPhong = phongA101.MaPhong, SoNuocCu = 35, SoNuocMoi = 45, GiaNuoc = 12000, TienNuoc = 120000, NgayThangNuoc = now };
    var dienB = new ChiSoDien { MaPhong = phongB101.MaPhong, SoDienCu = 45, SoDienMoi = 70, GiaDien = 3500, TienDien = 87500, NgayThangDien = now };
    var nuocB = new ChiSoNuoc { MaPhong = phongB101.MaPhong, SoNuocCu = 18, SoNuocMoi = 25, GiaNuoc = 12000, TienNuoc = 84000, NgayThangNuoc = now };
    var dienC = new ChiSoDien { MaPhong = phongC301.MaPhong, SoDienCu = 210, SoDienMoi = 275, GiaDien = 4000, TienDien = 260000, NgayThangDien = now };
    var nuocC = new ChiSoNuoc { MaPhong = phongC301.MaPhong, SoNuocCu = 52, SoNuocMoi = 64, GiaNuoc = 15000, TienNuoc = 180000, NgayThangNuoc = now };

    context.ChiSoDien.AddRange(dienA, dienB, dienC);
    context.ChiSoNuoc.AddRange(nuocA, nuocB, nuocC);
    context.SaveChanges();

    var hoaDonA = new HoaDon { MaNguoiThue = khachA.MaNguoiThue, MaPhong = phongA101.MaPhong, MaDien = dienA.MaDien, MaNuoc = nuocA.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 0, TongTien = phongA101.GiaPhong + dienA.TienDien + nuocA.TienNuoc + 150000, NgayLap = now, KyHoaDon = kyHoaDon, TrangThai = "ChuaThanhToan" };
    var hoaDonB = new HoaDon { MaNguoiThue = khachB.MaNguoiThue, MaPhong = phongB101.MaPhong, MaDien = dienB.MaDien, MaNuoc = nuocB.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 30000, TongTien = phongB101.GiaPhong + dienB.TienDien + nuocB.TienNuoc + 160000, NgayLap = now, KyHoaDon = kyHoaDon, TrangThai = "DaThanhToan" };
    var hoaDonC = new HoaDon { MaNguoiThue = khachC.MaNguoiThue, MaPhong = phongC301.MaPhong, MaDien = dienC.MaDien, MaNuoc = nuocC.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 50000, TongTien = phongC301.GiaPhong + dienC.TienDien + nuocC.TienNuoc + 150000, NgayLap = now, KyHoaDon = kyHoaDon, TrangThai = "ChuaThanhToan" };

    context.HoaDon.AddRange(hoaDonA, hoaDonB, hoaDonC);
    context.SaveChanges();

    context.ThanhToan.AddRange(
        new ThanhToan { MaHoaDon = hoaDonA.MaHoaDon, MaNguoiThue = khachA.MaNguoiThue, NgayThanhToan = now, TongTien = 1000000, HinhThucThanhToan = "Tiền mặt", GhiChu = "Thanh toán một phần hóa đơn" },
        new ThanhToan { MaHoaDon = hoaDonB.MaHoaDon, MaNguoiThue = khachB.MaNguoiThue, NgayThanhToan = now, TongTien = hoaDonB.TongTien, HinhThucThanhToan = "Chuyển khoản", GhiChu = "Đã thanh toán đủ" },
        new ThanhToan { MaHoaDon = hoaDonC.MaHoaDon, MaNguoiThue = khachC.MaNguoiThue, NgayThanhToan = now.AddDays(-1), TongTien = 2000000, HinhThucThanhToan = "Chuyển khoản", GhiChu = "Thanh toán trước một phần" }
    );

    // ─── Dữ liệu mở rộng để test reset kỳ thuê, hóa đơn, dịch vụ và yêu cầu thuê ───

    var nguoiDung4 = new User
    {
        TenDangNhap = "nguoithue4",
        HoTen = "Võ Thị Hạnh",
        Email = "nguoithue4@example.com",
        SoDienThoai = "0900000007",
        CCCD = "012345678905",
        VaiTro = "NguoiDung",
        MatKhau = password
    };

    var nguoiDung5 = new User
    {
        TenDangNhap = "nguoithue5",
        HoTen = "Đỗ Quốc Bảo",
        Email = "nguoithue5@example.com",
        SoDienThoai = "0900000008",
        CCCD = "012345678906",
        VaiTro = "NguoiDung",
        MatKhau = password
    };

    var nguoiDung6 = new User
    {
        TenDangNhap = "nguoithue6",
        HoTen = "Phạm Ngọc Linh",
        Email = "nguoithue6@example.com",
        SoDienThoai = "0900000009",
        CCCD = "012345678907",
        VaiTro = "NguoiDung",
        MatKhau = password
    };

    var nguoiDung7 = new User
    {
        TenDangNhap = "nguoithue7",
        HoTen = "Hoàng Gia Huy",
        Email = "nguoithue7@example.com",
        SoDienThoai = "0900000010",
        CCCD = "012345678908",
        VaiTro = "NguoiDung",
        MatKhau = password
    };

    context.Users.AddRange(nguoiDung4, nguoiDung5, nguoiDung6, nguoiDung7);
    context.SaveChanges();

    var phongA103 = new Phong { TenPhong = "A103", MaNhaTro = nhaTroA.MaNhaTro, MaLoaiPhong = loaiPhongThuongA.MaLoaiPhong, MaTrangThai = trangThaiDaThue, DienTich = 24, GiaPhong = 2700000, SucChua = 2, SoNguoiHienTai = 1, MoTa = "Phòng có hợp đồng vừa hết hạn để test tự trả về trống", DiaChiPhong = "Tầng 1" };
    var phongA202 = new Phong { TenPhong = "A202", MaNhaTro = nhaTroA.MaNhaTro, MaLoaiPhong = loaiPhongVipA.MaLoaiPhong, MaTrangThai = trangThaiDaThue, DienTich = 30, GiaPhong = 3700000, SucChua = 3, SoNguoiHienTai = 2, MoTa = "Phòng sắp hết hạn hợp đồng trong vài ngày", DiaChiPhong = "Tầng 2" };
    var phongB103 = new Phong { TenPhong = "B103", MaNhaTro = nhaTroB.MaNhaTro, MaLoaiPhong = loaiPhongSinhVienB.MaLoaiPhong, MaTrangThai = trangThaiDaThue, DienTich = 16, GiaPhong = 1600000, SucChua = 1, SoNguoiHienTai = 1, MoTa = "Phòng thuê theo kỳ tháng, có dịch vụ tháng cũ cần hết hạn", DiaChiPhong = "Dãy B" };
    var phongC303 = new Phong { TenPhong = "C303", MaNhaTro = nhaTroC.MaNhaTro, MaLoaiPhong = loaiCanHoC.MaLoaiPhong, MaTrangThai = trangThaiSuaChua, DienTich = 34, GiaPhong = 4700000, SucChua = 3, SoNguoiHienTai = 0, MoTa = "Căn hộ đang sửa chữa để test filter trạng thái", DiaChiPhong = "Tầng 3" };
    var phongC304 = new Phong { TenPhong = "C304", MaNhaTro = nhaTroC.MaNhaTro, MaLoaiPhong = loaiCanHoC.MaLoaiPhong, MaTrangThai = trangThaiTrong, DienTich = 36, GiaPhong = 5000000, SucChua = 4, SoNguoiHienTai = 0, MoTa = "Căn hộ trống để test yêu cầu thuê nhiều tháng", DiaChiPhong = "Tầng 4" };

    context.Phong.AddRange(phongA103, phongA202, phongB103, phongC303, phongC304);
    context.SaveChanges();

    var khachD = new NguoiThue { HoTen = "Võ Thị Hạnh", CCCD = "012345678905", SDT = "0900000007", Email = "nguoithue4@example.com", GioiTinh = "Nữ", QuocTich = "Việt Nam", DiaChi = "31 Pasteur", MaPhong = phongA103.MaPhong, MaNguoiDung = nguoiDung4.MaNguoiDung };
    var khachE = new NguoiThue { HoTen = "Đỗ Quốc Bảo", CCCD = "012345678906", SDT = "0900000008", Email = "nguoithue5@example.com", GioiTinh = "Nam", QuocTich = "Việt Nam", DiaChi = "15 Lý Tự Trọng", MaPhong = phongA202.MaPhong, MaNguoiDung = nguoiDung5.MaNguoiDung };
    var khachF = new NguoiThue { HoTen = "Phạm Ngọc Linh", CCCD = "012345678907", SDT = "0900000009", Email = "nguoithue6@example.com", GioiTinh = "Nữ", QuocTich = "Việt Nam", DiaChi = "101 Hoàng Diệu", MaPhong = phongB103.MaPhong, MaNguoiDung = nguoiDung6.MaNguoiDung };
    var khachG = new NguoiThue { HoTen = "Hoàng Gia Huy", CCCD = "012345678908", SDT = "0900000010", Email = "nguoithue7@example.com", GioiTinh = "Nam", QuocTich = "Việt Nam", DiaChi = "6 Trường Sa", MaPhong = phongC304.MaPhong, MaNguoiDung = nguoiDung7.MaNguoiDung, TrangThai = "ChoDuyet" };

    context.NguoiThue.AddRange(khachD, khachE, khachF, khachG);
    context.SaveChanges();

    context.HopDong.AddRange(
        new HopDong { MaNguoiThue = khachD.MaNguoiThue, MaPhong = phongA103.MaPhong, NgayBatDau = now.AddMonths(-1).AddDays(-2), NgayKetThuc = now.AddDays(-1), TienCoc = 2700000, NoiDung = "Hợp đồng đã hết hạn hôm qua - dùng để test reset phòng/dịch vụ", TrangThai = "DangHieuLuc" },
        new HopDong { MaNguoiThue = khachE.MaNguoiThue, MaPhong = phongA202.MaPhong, NgayBatDau = now.AddMonths(-5), NgayKetThuc = now.AddDays(5), TienCoc = 3700000, NoiDung = "Hợp đồng sắp hết hạn trong 5 ngày", TrangThai = "DangHieuLuc" },
        new HopDong { MaNguoiThue = khachF.MaNguoiThue, MaPhong = phongB103.MaPhong, NgayBatDau = now.AddMonths(-1).AddDays(-8), NgayKetThuc = now.AddMonths(2), TienCoc = 1600000, NoiDung = "Hợp đồng còn hiệu lực nhưng đã sang kỳ thuê mới", TrangThai = "DangHieuLuc" }
    );

    var mayLanhA = new DichVu { TenDichVu = "Máy lạnh", Tiendichvu = 250000, MaNhaTro = nhaTroA.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var racA = new DichVu { TenDichVu = "Thu gom rác", Tiendichvu = 30000, MaNhaTro = nhaTroA.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var cameraB = new DichVu { TenDichVu = "Camera an ninh", Tiendichvu = 40000, MaNhaTro = nhaTroB.MaNhaTro, MaChuTro = chuTro.MaNguoiDung };
    var guiXeC = new DichVu { TenDichVu = "Giữ xe ô tô", Tiendichvu = 500000, MaNhaTro = nhaTroC.MaNhaTro, MaChuTro = chuTro2.MaNguoiDung };
    var veSinhC = new DichVu { TenDichVu = "Vệ sinh căn hộ", Tiendichvu = 200000, MaNhaTro = nhaTroC.MaNhaTro, MaChuTro = chuTro2.MaNguoiDung };

    context.DichVu.AddRange(mayLanhA, racA, cameraB, guiXeC, veSinhC);
    context.SaveChanges();

    var dauThangNay = new DateTime(now.Year, now.Month, 1);
    var thangTruoc = now.AddMonths(-1);
    var kyThangTruoc = thangTruoc.ToString("yyyy-MM");
    var kyHaiThangTruoc = now.AddMonths(-2).ToString("yyyy-MM");

    context.DangKyDichVu.AddRange(
        new DangKyDichVu { MaNguoiDung = nguoiDung4.MaNguoiDung, MaNguoiThue = khachD.MaNguoiThue, MaPhong = phongA103.MaPhong, MaDichVu = internetA.MaDichVu, NgayDangKy = now.AddMonths(-1).AddDays(-2), KyDangKy = kyThangTruoc, TrangThai = "DangSuDung", GhiChu = "Dịch vụ của hợp đồng đã hết hạn - reset sẽ chuyển HetHan" },
        new DangKyDichVu { MaNguoiDung = nguoiDung4.MaNguoiDung, MaNguoiThue = khachD.MaNguoiThue, MaPhong = phongA103.MaPhong, MaDichVu = racA.MaDichVu, NgayDangKy = now.AddMonths(-1).AddDays(-1), KyDangKy = kyThangTruoc, TrangThai = "DangSuDung", GhiChu = "Dịch vụ tháng cũ cần hết hạn" },
        new DangKyDichVu { MaNguoiDung = nguoiDung5.MaNguoiDung, MaNguoiThue = khachE.MaNguoiThue, MaPhong = phongA202.MaPhong, MaDichVu = mayLanhA.MaDichVu, NgayDangKy = dauThangNay.AddDays(1), KyDangKy = kyHoaDon, TrangThai = "DangSuDung", GhiChu = "Dịch vụ mới trong kỳ hiện tại - không bị reset" },
        new DangKyDichVu { MaNguoiDung = nguoiDung5.MaNguoiDung, MaNguoiThue = khachE.MaNguoiThue, MaPhong = phongA202.MaPhong, MaDichVu = giuXeA.MaDichVu, NgayDangKy = now.AddMonths(-1), NgayHuy = now.AddDays(-3), KyDangKy = kyThangTruoc, TrangThai = "DaHuy", GhiChu = "Dịch vụ người dùng đã hủy thủ công" },
        new DangKyDichVu { MaNguoiDung = nguoiDung6.MaNguoiDung, MaNguoiThue = khachF.MaNguoiThue, MaPhong = phongB103.MaPhong, MaDichVu = internetB.MaDichVu, NgayDangKy = now.AddMonths(-1).AddDays(-5), KyDangKy = kyThangTruoc, TrangThai = "DangSuDung", GhiChu = "Dịch vụ kỳ trước của hợp đồng còn hiệu lực - reset sẽ chuyển HetHan khi sang kỳ mới" },
        new DangKyDichVu { MaNguoiDung = nguoiDung6.MaNguoiDung, MaNguoiThue = khachF.MaNguoiThue, MaPhong = phongB103.MaPhong, MaDichVu = mayGiatB.MaDichVu, NgayDangKy = dauThangNay.AddDays(2), KyDangKy = kyHoaDon, TrangThai = "DangSuDung", GhiChu = "Dịch vụ đăng ký lại cho kỳ hiện tại" },
        new DangKyDichVu { MaNguoiDung = nguoiDung3.MaNguoiDung, MaNguoiThue = khachC.MaNguoiThue, MaPhong = phongC301.MaPhong, MaDichVu = guiXeC.MaDichVu, NgayDangKy = now.AddMonths(-2), NgayHuy = now.AddMonths(-1), NgayHetHan = now.AddMonths(-1), KyDangKy = kyHaiThangTruoc, TrangThai = "HetHan", GhiChu = "Dịch vụ cũ đã hết hạn để test lịch sử" }
    );

    var dienD = new ChiSoDien { MaPhong = phongA103.MaPhong, SoDienCu = 80, SoDienMoi = 118, GiaDien = 3500, TienDien = 133000, NgayThangDien = thangTruoc };
    var nuocD = new ChiSoNuoc { MaPhong = phongA103.MaPhong, SoNuocCu = 20, SoNuocMoi = 28, GiaNuoc = 12000, TienNuoc = 96000, NgayThangNuoc = thangTruoc };
    var dienE = new ChiSoDien { MaPhong = phongA202.MaPhong, SoDienCu = 300, SoDienMoi = 366, GiaDien = 3500, TienDien = 231000, NgayThangDien = now };
    var nuocE = new ChiSoNuoc { MaPhong = phongA202.MaPhong, SoNuocCu = 70, SoNuocMoi = 85, GiaNuoc = 12000, TienNuoc = 180000, NgayThangNuoc = now };
    var dienFThangTruoc = new ChiSoDien { MaPhong = phongB103.MaPhong, SoDienCu = 20, SoDienMoi = 40, GiaDien = 3500, TienDien = 70000, NgayThangDien = thangTruoc };
    var nuocFThangTruoc = new ChiSoNuoc { MaPhong = phongB103.MaPhong, SoNuocCu = 8, SoNuocMoi = 13, GiaNuoc = 12000, TienNuoc = 60000, NgayThangNuoc = thangTruoc };
    var dienF = new ChiSoDien { MaPhong = phongB103.MaPhong, SoDienCu = 40, SoDienMoi = 59, GiaDien = 3500, TienDien = 66500, NgayThangDien = now };
    var nuocF = new ChiSoNuoc { MaPhong = phongB103.MaPhong, SoNuocCu = 13, SoNuocMoi = 18, GiaNuoc = 12000, TienNuoc = 60000, NgayThangNuoc = now };

    context.ChiSoDien.AddRange(dienD, dienE, dienFThangTruoc, dienF);
    context.ChiSoNuoc.AddRange(nuocD, nuocE, nuocFThangTruoc, nuocF);
    context.SaveChanges();

    var hoaDonDThangTruoc = new HoaDon { MaNguoiThue = khachD.MaNguoiThue, MaPhong = phongA103.MaPhong, MaDien = dienD.MaDien, MaNuoc = nuocD.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 20000, TongTien = phongA103.GiaPhong + dienD.TienDien + nuocD.TienNuoc + 150000 + 20000, NgayLap = thangTruoc, KyHoaDon = kyThangTruoc, TrangThai = "ChuaThanhToan" };
    var hoaDonE = new HoaDon { MaNguoiThue = khachE.MaNguoiThue, MaPhong = phongA202.MaPhong, MaDien = dienE.MaDien, MaNuoc = nuocE.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 75000, TongTien = phongA202.GiaPhong + dienE.TienDien + nuocE.TienNuoc + 330000 + 75000, NgayLap = now, KyHoaDon = kyHoaDon, TrangThai = "ChuaThanhToan" };
    var hoaDonFThangTruoc = new HoaDon { MaNguoiThue = khachF.MaNguoiThue, MaPhong = phongB103.MaPhong, MaDien = dienFThangTruoc.MaDien, MaNuoc = nuocFThangTruoc.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 0, TongTien = phongB103.GiaPhong + dienFThangTruoc.TienDien + nuocFThangTruoc.TienNuoc + 130000, NgayLap = thangTruoc, KyHoaDon = kyThangTruoc, TrangThai = "ChuaThanhToan" };
    var hoaDonF = new HoaDon { MaNguoiThue = khachF.MaNguoiThue, MaPhong = phongB103.MaPhong, MaDien = dienF.MaDien, MaNuoc = nuocF.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 0, TongTien = phongB103.GiaPhong + dienF.TienDien + nuocF.TienNuoc + 130000, NgayLap = now, KyHoaDon = kyHoaDon, TrangThai = "ChuaThanhToan" };

    context.HoaDon.AddRange(hoaDonDThangTruoc, hoaDonE, hoaDonFThangTruoc, hoaDonF);
    context.SaveChanges();

    context.ThanhToan.AddRange(
        new ThanhToan { MaHoaDon = hoaDonDThangTruoc.MaHoaDon, MaNguoiThue = khachD.MaNguoiThue, NgayThanhToan = thangTruoc.AddDays(2), TongTien = 500000, HinhThucThanhToan = "Tiền mặt", GhiChu = "Thanh toán thiếu, hóa đơn tháng cũ sẽ bị chốt khi reset" },
        new ThanhToan { MaHoaDon = hoaDonE.MaHoaDon, MaNguoiThue = khachE.MaNguoiThue, NgayThanhToan = now.AddDays(-2), TongTien = 2000000, HinhThucThanhToan = "Chuyển khoản", GhiChu = "Thanh toán một phần hóa đơn hiện tại" },
        new ThanhToan { MaHoaDon = hoaDonFThangTruoc.MaHoaDon, MaNguoiThue = khachF.MaNguoiThue, NgayThanhToan = thangTruoc.AddDays(5), TongTien = hoaDonFThangTruoc.TongTien, HinhThucThanhToan = "Chuyển khoản", GhiChu = "Đã trả đủ nhưng trạng thái hóa đơn cố ý để ChuaThanhToan nhằm test đối soát" }
    );

    context.YeuCauThue.AddRange(
        new YeuCauThue { MaNguoiDung = nguoiDung7.MaNguoiDung, MaPhong = phongC304.MaPhong, NgayGui = now.AddDays(-1), TrangThai = "ChoDuyet", GhiChuNguoiDung = "Muốn thuê 6 tháng, nhận phòng đầu tháng sau", SoThangMuonThue = 6, NgayBatDauMongMuon = dauThangNay.AddMonths(1) },
        new YeuCauThue { MaNguoiDung = nguoiDung4.MaNguoiDung, MaPhong = phongA102.MaPhong, NgayGui = now.AddDays(-4), TrangThai = "ChoDuyet", GhiChuNguoiDung = "Muốn xem phòng trước khi đặt cọc", SoThangMuonThue = 3, NgayBatDauMongMuon = now.AddDays(7) },
        new YeuCauThue { MaNguoiDung = nguoiDung5.MaNguoiDung, MaPhong = phongB102.MaPhong, NgayGui = now.AddDays(-10), TrangThai = "TuChoi", GhiChuNguoiDung = "Cần phòng cho 2 người", GhiChuChuTro = "Phòng chỉ phù hợp 1 người ở thời điểm hiện tại", NgayXuLy = now.AddDays(-8), SoThangMuonThue = 2, NgayBatDauMongMuon = now.AddDays(3) },
        new YeuCauThue { MaNguoiDung = nguoiDung6.MaNguoiDung, MaPhong = phongA102.MaPhong, NgayGui = now.AddDays(-20), TrangThai = "DaDuyet", GhiChuNguoiDung = "Đã duyệt thử nghiệm", GhiChuChuTro = "Dữ liệu mẫu để test lọc trạng thái yêu cầu", NgayXuLy = now.AddDays(-18), SoThangMuonThue = 1, NgayBatDauMongMuon = now.AddDays(-15) }
    );

    context.SaveChanges();
}

static int GetTrangThaiId(ApplicationDbContext context, params string[] keywords)
{
    var trangThai = context.TrangThai
        .AsEnumerable()
        .FirstOrDefault(t => keywords.Any(k =>
            t.TenTrangThai.Contains(k, StringComparison.OrdinalIgnoreCase)));

    if (trangThai == null)
    {
        throw new InvalidOperationException($"Không tìm thấy trạng thái phòng phù hợp: {string.Join(", ", keywords)}");
    }

    return trangThai.MaTrangThai;
}
