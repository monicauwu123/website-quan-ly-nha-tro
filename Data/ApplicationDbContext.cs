using Microsoft.EntityFrameworkCore;
using DoAnSE104.Models;

namespace DoAnSE104.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<TrangThai> TrangThai { get; set; }
        public DbSet<NhaTro> NhaTro { get; set; }
        public DbSet<LoaiPhong> LoaiPhong { get; set; }
        public DbSet<Phong> Phong { get; set; }
        public DbSet<NguoiThue> NguoiThue { get; set; }
        public DbSet<HopDong> HopDong { get; set; }
        public DbSet<DichVu> DichVu { get; set; }
        public DbSet<LichSuGiaDichVu> LichSuGiaDichVu { get; set; }
        public DbSet<ChiSoDien> ChiSoDien { get; set; }
        public DbSet<ChiSoNuoc> ChiSoNuoc { get; set; }
        public DbSet<HoaDon> HoaDon { get; set; }
        public DbSet<ChiTietHoaDon> ChiTietHoaDon { get; set; }
        public DbSet<ThanhToan> ThanhToan { get; set; }
        public DbSet<YeuCauThue> YeuCauThue { get; set; }
        public DbSet<YeuCauGiaHan> YeuCauGiaHan { get; set; }
        public DbSet<BaoCaoSuCo> BaoCaoSuCo { get; set; }
        public DbSet<DangKyDichVu> DangKyDichVu { get; set; }

        // ── Thông Báo ────────────────────────────────────────────────────────────
        public DbSet<ThongBao> ThongBao { get; set; }
        public DbSet<ThongBaoDaDoc> ThongBaoDaDoc { get; set; }
        public DbSet<EmailLog> EmailLog { get; set; }

        /// <summary>
        /// Bổ sung các cột còn thiếu khi chạy trực tiếp bằng EnsureCreated().
        /// </summary>
        public void EnsureCustomSchema()
        {
            Database.ExecuteSqlRaw(@"
IF OBJECT_ID('Users', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Users', 'CCCD') IS NULL ALTER TABLE Users ADD CCCD NVARCHAR(20) NULL;
    IF COL_LENGTH('Users', 'NgaySinh') IS NULL ALTER TABLE Users ADD NgaySinh DATETIME2 NULL;
    IF COL_LENGTH('Users', 'GioiTinh') IS NULL ALTER TABLE Users ADD GioiTinh NVARCHAR(10) NULL;
    IF COL_LENGTH('Users', 'QuocTich') IS NULL ALTER TABLE Users ADD QuocTich NVARCHAR(50) NULL;
    IF COL_LENGTH('Users', 'DiaChi') IS NULL ALTER TABLE Users ADD DiaChi NVARCHAR(255) NULL;
    IF COL_LENGTH('Users', 'NoiCongTac') IS NULL ALTER TABLE Users ADD NoiCongTac NVARCHAR(100) NULL;
    IF COL_LENGTH('Users', 'AnhCccdMatTruoc') IS NULL ALTER TABLE Users ADD AnhCccdMatTruoc NVARCHAR(500) NULL;
    IF COL_LENGTH('Users', 'AnhCccdMatSau') IS NULL ALTER TABLE Users ADD AnhCccdMatSau NVARCHAR(500) NULL;
    IF COL_LENGTH('Users', 'TenNganHang') IS NULL ALTER TABLE Users ADD TenNganHang NVARCHAR(100) NULL;
    IF COL_LENGTH('Users', 'MaNganHang') IS NULL ALTER TABLE Users ADD MaNganHang NVARCHAR(50) NULL;
    IF COL_LENGTH('Users', 'SoTaiKhoan') IS NULL ALTER TABLE Users ADD SoTaiKhoan NVARCHAR(50) NULL;
    IF COL_LENGTH('Users', 'TenChuTaiKhoan') IS NULL ALTER TABLE Users ADD TenChuTaiKhoan NVARCHAR(100) NULL;
    IF COL_LENGTH('Users', 'NoiDungChuyenKhoanMacDinh') IS NULL ALTER TABLE Users ADD NoiDungChuyenKhoanMacDinh NVARCHAR(255) NULL;
    IF COL_LENGTH('Users', 'PasswordResetToken') IS NULL ALTER TABLE Users ADD PasswordResetToken NVARCHAR(200) NULL;
    IF COL_LENGTH('Users', 'PasswordResetTokenExpiry') IS NULL ALTER TABLE Users ADD PasswordResetTokenExpiry DATETIME2 NULL;
END

IF OBJECT_ID('NhaTro', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('NhaTro', 'MaChuTro') IS NULL ALTER TABLE NhaTro ADD MaChuTro INT NULL;
    IF COL_LENGTH('NhaTro', 'TrangThai') IS NULL ALTER TABLE NhaTro ADD TrangThai NVARCHAR(30) NOT NULL CONSTRAINT DF_NhaTro_TrangThai DEFAULT 'HoatDong';
END

IF COL_LENGTH('NhaTro', 'HinhAnh') IS NULL
BEGIN
    ALTER TABLE NhaTro ADD HinhAnh NVARCHAR(255) NULL;
END

IF COL_LENGTH('NhaTro', 'DanhSachHinhAnh') IS NULL
BEGIN
    ALTER TABLE NhaTro ADD DanhSachHinhAnh NVARCHAR(MAX) NULL;
END

IF OBJECT_ID('NguoiThue', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('NguoiThue', 'MaNguoiDung') IS NULL ALTER TABLE NguoiThue ADD MaNguoiDung INT NULL;
    IF COL_LENGTH('NguoiThue', 'TrangThai') IS NULL ALTER TABLE NguoiThue ADD TrangThai NVARCHAR(30) NOT NULL CONSTRAINT DF_NguoiThue_TrangThai DEFAULT 'DangThue';
    IF COL_LENGTH('NguoiThue', 'NgaySinh') IS NULL ALTER TABLE NguoiThue ADD NgaySinh DATETIME2 NULL;
    IF COL_LENGTH('NguoiThue', 'GioiTinh') IS NULL ALTER TABLE NguoiThue ADD GioiTinh NVARCHAR(10) NULL;
    IF COL_LENGTH('NguoiThue', 'QuocTich') IS NULL ALTER TABLE NguoiThue ADD QuocTich NVARCHAR(50) NULL;
    IF COL_LENGTH('NguoiThue', 'NoiCongTac') IS NULL ALTER TABLE NguoiThue ADD NoiCongTac NVARCHAR(100) NULL;
    IF COL_LENGTH('NguoiThue', 'AnhCccdMatTruoc') IS NULL ALTER TABLE NguoiThue ADD AnhCccdMatTruoc NVARCHAR(500) NULL;
    IF COL_LENGTH('NguoiThue', 'AnhCccdMatSau') IS NULL ALTER TABLE NguoiThue ADD AnhCccdMatSau NVARCHAR(500) NULL;
END

IF OBJECT_ID('LoaiPhong', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('LoaiPhong', 'MaNhaTro') IS NULL ALTER TABLE LoaiPhong ADD MaNhaTro INT NULL;
    IF COL_LENGTH('LoaiPhong', 'MaChuTro') IS NULL ALTER TABLE LoaiPhong ADD MaChuTro INT NULL;
    IF COL_LENGTH('LoaiPhong', 'TrangThai') IS NULL ALTER TABLE LoaiPhong ADD TrangThai NVARCHAR(30) NOT NULL CONSTRAINT DF_LoaiPhong_TrangThai DEFAULT 'DangSuDung';
END

IF OBJECT_ID('DichVu', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('DichVu', 'MaNhaTro') IS NULL ALTER TABLE DichVu ADD MaNhaTro INT NULL;
    IF COL_LENGTH('DichVu', 'MaChuTro') IS NULL ALTER TABLE DichVu ADD MaChuTro INT NULL;
    IF COL_LENGTH('DichVu', 'TrangThai') IS NULL ALTER TABLE DichVu ADD TrangThai NVARCHAR(30) NOT NULL CONSTRAINT DF_DichVu_TrangThai DEFAULT 'DangSuDung';
END

IF COL_LENGTH('Phong', 'DanhSachHinhAnh') IS NULL
BEGIN
    ALTER TABLE Phong ADD DanhSachHinhAnh NVARCHAR(MAX) NULL;
END

IF COL_LENGTH('Phong', 'DichVuGanPhong') IS NULL
BEGIN
    ALTER TABLE Phong ADD DichVuGanPhong NVARCHAR(MAX) NULL;
END

IF COL_LENGTH('DichVu', 'LoaiDichVu') IS NULL
BEGIN
    ALTER TABLE DichVu ADD LoaiDichVu NVARCHAR(30) NOT NULL CONSTRAINT DF_DichVu_LoaiDichVu DEFAULT 'TinhPhi';
END

UPDATE DichVu
SET LoaiDichVu = 'TinhPhi'
WHERE LoaiDichVu IS NULL OR LTRIM(RTRIM(LoaiDichVu)) = '';

IF OBJECT_ID('HopDong', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('HopDong', 'TrangThai') IS NULL ALTER TABLE HopDong ADD TrangThai NVARCHAR(20) NOT NULL CONSTRAINT DF_HopDong_TrangThai DEFAULT 'DangHieuLuc';
    IF COL_LENGTH('HopDong', 'TrangThai') IS NOT NULL ALTER TABLE HopDong ALTER COLUMN TrangThai NVARCHAR(30) NOT NULL;
    IF COL_LENGTH('HopDong', 'NoiDung') IS NOT NULL ALTER TABLE HopDong ALTER COLUMN NoiDung NVARCHAR(1000) NULL;
END

IF OBJECT_ID('HoaDon', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('HoaDon', 'LoaiHoaDon') IS NULL ALTER TABLE HoaDon ADD LoaiHoaDon NVARCHAR(20) NOT NULL CONSTRAINT DF_HoaDon_LoaiHoaDon DEFAULT 'HangThang';
    IF COL_LENGTH('HoaDon', 'TrangThai') IS NULL ALTER TABLE HoaDon ADD TrangThai NVARCHAR(20) NOT NULL CONSTRAINT DF_HoaDon_TrangThai DEFAULT 'ChuaThanhToan';
END

IF OBJECT_ID(N'FK_HoaDon_ChiSoDien_MaDien', N'F') IS NOT NULL
BEGIN
    ALTER TABLE HoaDon DROP CONSTRAINT FK_HoaDon_ChiSoDien_MaDien;
END

IF OBJECT_ID(N'FK_HoaDon_ChiSoNuoc_MaNuoc', N'F') IS NOT NULL
BEGIN
    ALTER TABLE HoaDon DROP CONSTRAINT FK_HoaDon_ChiSoNuoc_MaNuoc;
END

IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'MaDien') IS NOT NULL
BEGIN
    ALTER TABLE HoaDon ALTER COLUMN MaDien INT NULL;
END

IF OBJECT_ID('HoaDon', 'U') IS NOT NULL AND COL_LENGTH('HoaDon', 'MaNuoc') IS NOT NULL
BEGIN
    ALTER TABLE HoaDon ALTER COLUMN MaNuoc INT NULL;
END

IF OBJECT_ID('ThanhToan', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('ThanhToan', 'HinhAnhBienLai') IS NULL ALTER TABLE ThanhToan ADD HinhAnhBienLai NVARCHAR(500) NULL;
    IF COL_LENGTH('ThanhToan', 'MaGiaoDich') IS NULL ALTER TABLE ThanhToan ADD MaGiaoDich NVARCHAR(100) NULL;
    IF COL_LENGTH('ThanhToan', 'TrangThaiXacNhan') IS NULL ALTER TABLE ThanhToan ADD TrangThaiXacNhan NVARCHAR(20) NULL;
    IF COL_LENGTH('ThanhToan', 'LyDoTuChoi') IS NULL ALTER TABLE ThanhToan ADD LyDoTuChoi NVARCHAR(500) NULL;
    IF COL_LENGTH('ThanhToan', 'NguoiXacNhanId') IS NULL ALTER TABLE ThanhToan ADD NguoiXacNhanId INT NULL;
    IF COL_LENGTH('ThanhToan', 'NgayXacNhan') IS NULL ALTER TABLE ThanhToan ADD NgayXacNhan DATETIME2 NULL;
END

IF OBJECT_ID('DangKyDichVu', 'U') IS NULL AND OBJECT_ID('Users', 'U') IS NOT NULL AND OBJECT_ID('Phong', 'U') IS NOT NULL AND OBJECT_ID('DichVu', 'U') IS NOT NULL
BEGIN
    CREATE TABLE DangKyDichVu (
        MaDangKyDichVu INT NOT NULL IDENTITY,
        MaNguoiDung INT NOT NULL,
        MaPhong INT NOT NULL,
        MaDichVu INT NOT NULL,
        MaNguoiThue INT NULL,
        NgayDangKy DATETIME2 NOT NULL DEFAULT GETDATE(),
        NgayHuy DATETIME2 NULL,
        NgayHetHan DATETIME2 NULL,
        KyDangKy NVARCHAR(7) NULL,
        TrangThai NVARCHAR(30) NOT NULL DEFAULT 'DangSuDung',
        GhiChu NVARCHAR(500) NULL,
        CONSTRAINT PK_DangKyDichVu PRIMARY KEY (MaDangKyDichVu)
    );
END

IF OBJECT_ID('DangKyDichVu', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('DangKyDichVu', 'NgayHetHan') IS NULL ALTER TABLE DangKyDichVu ADD NgayHetHan DATETIME2 NULL;
    IF COL_LENGTH('DangKyDichVu', 'KyDangKy') IS NULL ALTER TABLE DangKyDichVu ADD KyDangKy NVARCHAR(7) NULL;
END

IF OBJECT_ID('YeuCauThue', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('YeuCauThue', 'MaNguoiThue') IS NULL ALTER TABLE YeuCauThue ADD MaNguoiThue INT NULL;
    IF COL_LENGTH('YeuCauThue', 'MaHopDong') IS NULL ALTER TABLE YeuCauThue ADD MaHopDong INT NULL;
    IF COL_LENGTH('YeuCauThue', 'NgayXuLy') IS NULL ALTER TABLE YeuCauThue ADD NgayXuLy DATETIME2 NULL;
    IF COL_LENGTH('YeuCauThue', 'SoThangMuonThue') IS NULL ALTER TABLE YeuCauThue ADD SoThangMuonThue INT NOT NULL CONSTRAINT DF_YeuCauThue_SoThangMuonThue DEFAULT(1);
    IF COL_LENGTH('YeuCauThue', 'NgayBatDauMongMuon') IS NULL ALTER TABLE YeuCauThue ADD NgayBatDauMongMuon DATETIME2 NULL;
END

IF OBJECT_ID('YeuCauGiaHan', 'U') IS NULL AND OBJECT_ID('HopDong', 'U') IS NOT NULL AND OBJECT_ID('Users', 'U') IS NOT NULL
BEGIN
    CREATE TABLE YeuCauGiaHan (
        MaYeuCauGiaHan INT NOT NULL IDENTITY,
        MaHopDong INT NOT NULL,
        MaNguoiDung INT NOT NULL,
        NgayGui DATETIME2 NOT NULL DEFAULT GETDATE(),
        TrangThai NVARCHAR(30) NOT NULL DEFAULT 'ChoDuyet',
        NgayKetThucCu DATETIME2 NULL,
        NgayKetThucMoiDeXuat DATETIME2 NOT NULL,
        NgayKetThucMoiChuTro DATETIME2 NULL,
        TienCocMoi DECIMAL(18,2) NULL,
        NoiDungDieuKhoanMoi NVARCHAR(1000) NULL,
        GhiChuNguoiDung NVARCHAR(500) NULL,
        GhiChuChuTro NVARCHAR(500) NULL,
        NgayXuLy DATETIME2 NULL,
        CONSTRAINT PK_YeuCauGiaHan PRIMARY KEY (MaYeuCauGiaHan)
    );
END

IF OBJECT_ID('EmailLog', 'U') IS NULL
BEGIN
    CREATE TABLE EmailLog (
        EmailLogId INT NOT NULL IDENTITY,
        EventType NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL,
        EntityId INT NOT NULL,
        RecipientEmail NVARCHAR(255) NOT NULL,
        RecipientName NVARCHAR(255) NULL,
        ReferenceDate DATETIME2 NULL,
        Subject NVARCHAR(255) NOT NULL,
        Status NVARCHAR(20) NOT NULL DEFAULT 'Sent',
        ErrorMessage NVARCHAR(1000) NULL,
        SentAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT PK_EmailLog PRIMARY KEY (EmailLogId)
    );
END

IF OBJECT_ID('EmailLog', 'U') IS NOT NULL AND NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = 'IX_EmailLog_Dedup' AND object_id = OBJECT_ID('EmailLog')
)
BEGIN
    CREATE INDEX IX_EmailLog_Dedup ON EmailLog(EventType, EntityType, EntityId, RecipientEmail, ReferenceDate);
END

-- Đồng bộ trạng thái phòng theo hợp đồng hiệu lực
-- Phòng đang có HĐ hiệu lực → Đã thuê (MaTrangThai = 2)
-- Phòng không có HĐ hiệu lực → Còn trống (MaTrangThai = 1)
-- Phòng đang sửa chữa (MaTrangThai = 3) giữ nguyên
IF OBJECT_ID('Phong', 'U') IS NOT NULL AND OBJECT_ID('HopDong', 'U') IS NOT NULL
BEGIN
    UPDATE Phong SET MaTrangThai = 2
    WHERE MaTrangThai = 1
      AND EXISTS (SELECT 1 FROM HopDong WHERE HopDong.MaPhong = Phong.MaPhong
          AND HopDong.TrangThai = 'DangHieuLuc'
          AND HopDong.NgayBatDau <= GETDATE()
          AND (HopDong.NgayKetThuc IS NULL OR HopDong.NgayKetThuc >= GETDATE()));

    UPDATE Phong SET MaTrangThai = 1
    WHERE MaTrangThai = 2
      AND NOT EXISTS (SELECT 1 FROM HopDong WHERE HopDong.MaPhong = Phong.MaPhong
          AND HopDong.TrangThai = 'DangHieuLuc'
          AND HopDong.NgayBatDau <= GETDATE()
          AND (HopDong.NgayKetThuc IS NULL OR HopDong.NgayKetThuc >= GETDATE()));
END
");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.TenDangNhap)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<NhaTro>()
                .HasOne(n => n.ChuTro)
                .WithMany(u => u.DanhSachNhaTro)
                .HasForeignKey(n => n.MaChuTro)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<NguoiThue>()
                .HasOne(nt => nt.NguoiDungTK)
                .WithMany(u => u.DanhSachNguoiThue)
                .HasForeignKey(nt => nt.MaNguoiDung)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);


            modelBuilder.Entity<LoaiPhong>()
                .HasOne(lp => lp.NhaTro)
                .WithMany()
                .HasForeignKey(lp => lp.MaNhaTro)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<LoaiPhong>()
                .HasOne(lp => lp.ChuTro)
                .WithMany()
                .HasForeignKey(lp => lp.MaChuTro)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<DichVu>()
                .HasOne(dv => dv.NhaTro)
                .WithMany()
                .HasForeignKey(dv => dv.MaNhaTro)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<DichVu>()
                .Property(dv => dv.LoaiDichVu)
                .HasMaxLength(30)
                .HasDefaultValue("TinhPhi");

            modelBuilder.Entity<DichVu>()
                .HasOne(dv => dv.ChuTro)
                .WithMany()
                .HasForeignKey(dv => dv.MaChuTro)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);
            modelBuilder.Entity<Phong>()
                .HasOne(p => p.NhaTro)
                .WithMany()
                .HasForeignKey(p => p.MaNhaTro)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Phong>()
                .HasOne(p => p.LoaiPhong)
                .WithMany()
                .HasForeignKey(p => p.MaLoaiPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<Phong>()
                .HasOne(p => p.TrangThai)
                .WithMany()
                .HasForeignKey(p => p.MaTrangThai)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HopDong>()
                .HasOne(h => h.NguoiThue)
                .WithMany()
                .HasForeignKey(h => h.MaNguoiThue)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HopDong>()
                .HasOne(h => h.Phong)
                .WithMany()
                .HasForeignKey(h => h.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChiSoDien>()
                .HasOne(c => c.Phong)
                .WithMany()
                .HasForeignKey(c => c.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChiSoNuoc>()
                .HasOne(c => c.Phong)
                .WithMany()
                .HasForeignKey(c => c.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.NguoiThue)
                .WithMany()
                .HasForeignKey(h => h.MaNguoiThue)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.Phong)
                .WithMany()
                .HasForeignKey(h => h.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.ChiSoDien)
                .WithMany()
                .HasForeignKey(h => h.MaDien)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<HoaDon>()
                .HasOne(h => h.ChiSoNuoc)
                .WithMany()
                .HasForeignKey(h => h.MaNuoc)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ChiTietHoaDon>()
                .HasOne(ct => ct.HoaDon)
                .WithMany(h => h.ChiTietHoaDon)
                .HasForeignKey(ct => ct.MaHoaDon)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ThanhToan>()
                .HasOne(t => t.HoaDon)
                .WithMany()
                .HasForeignKey(t => t.MaHoaDon)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ThanhToan>()
                .HasOne(t => t.NguoiThue)
                .WithMany()
                .HasForeignKey(t => t.MaNguoiThue)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<YeuCauThue>()
                .HasOne(y => y.NguoiDung)
                .WithMany()
                .HasForeignKey(y => y.MaNguoiDung)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<YeuCauThue>()
                .HasOne(y => y.Phong)
                .WithMany()
                .HasForeignKey(y => y.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<YeuCauThue>()
                .HasOne(y => y.NguoiThue)
                .WithMany()
                .HasForeignKey(y => y.MaNguoiThue)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<YeuCauThue>()
                .HasOne(y => y.HopDong)
                .WithMany()
                .HasForeignKey(y => y.MaHopDong)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<YeuCauThue>()
                .HasIndex(y => new { y.MaNguoiDung, y.MaPhong, y.TrangThai });

            modelBuilder.Entity<YeuCauGiaHan>()
                .HasOne(y => y.HopDong)
                .WithMany()
                .HasForeignKey(y => y.MaHopDong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<YeuCauGiaHan>()
                .HasOne(y => y.NguoiDung)
                .WithMany()
                .HasForeignKey(y => y.MaNguoiDung)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<YeuCauGiaHan>()
                .HasIndex(y => new { y.MaNguoiDung, y.MaHopDong, y.TrangThai });

            modelBuilder.Entity<BaoCaoSuCo>()
                .HasOne(b => b.NguoiDung)
                .WithMany()
                .HasForeignKey(b => b.MaNguoiDung)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BaoCaoSuCo>()
                .HasOne(b => b.Phong)
                .WithMany()
                .HasForeignKey(b => b.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BaoCaoSuCo>()
                .HasIndex(b => new { b.MaPhong, b.TrangThai });

            modelBuilder.Entity<BaoCaoSuCo>()
                .HasIndex(b => new { b.MaNguoiDung, b.NgayGui });

            modelBuilder.Entity<DangKyDichVu>()
                .HasOne(dk => dk.NguoiDung)
                .WithMany()
                .HasForeignKey(dk => dk.MaNguoiDung)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DangKyDichVu>()
                .HasOne(dk => dk.Phong)
                .WithMany()
                .HasForeignKey(dk => dk.MaPhong)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DangKyDichVu>()
                .HasOne(dk => dk.DichVu)
                .WithMany()
                .HasForeignKey(dk => dk.MaDichVu)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<DangKyDichVu>()
                .HasOne(dk => dk.NguoiThue)
                .WithMany()
                .HasForeignKey(dk => dk.MaNguoiThue)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<DangKyDichVu>()
                .HasIndex(dk => new { dk.MaNguoiDung, dk.MaPhong, dk.MaDichVu, dk.TrangThai });

            // ── ThongBao ──────────────────────────────────────────────────────────
            modelBuilder.Entity<ThongBao>()
                .HasOne(tb => tb.NguoiNhan)
                .WithMany()
                .HasForeignKey(tb => tb.NguoiNhanId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<ThongBao>()
                .HasOne(tb => tb.Phong)
                .WithMany()
                .HasForeignKey(tb => tb.PhongId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<ThongBao>()
                .HasOne(tb => tb.NguoiTao)
                .WithMany()
                .HasForeignKey(tb => tb.NguoiTaoId)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            modelBuilder.Entity<ThongBao>()
                .HasIndex(tb => new { tb.DaDoc, tb.TrangThai });

            modelBuilder.Entity<ThongBaoDaDoc>()
                .HasOne(td => td.ThongBao)
                .WithMany()
                .HasForeignKey(td => td.ThongBaoId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ThongBaoDaDoc>()
                .HasOne(td => td.NguoiDung)
                .WithMany()
                .HasForeignKey(td => td.MaNguoiDung)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<ThongBaoDaDoc>()
                .HasIndex(td => new { td.ThongBaoId, td.MaNguoiDung })
                .IsUnique();

            modelBuilder.Entity<ThongBaoDaDoc>()
                .HasIndex(td => new { td.MaNguoiDung, td.NgayDoc });

            modelBuilder.Entity<EmailLog>()
                .HasIndex(e => new { e.EventType, e.EntityType, e.EntityId, e.RecipientEmail, e.ReferenceDate });
        }
    }
}
