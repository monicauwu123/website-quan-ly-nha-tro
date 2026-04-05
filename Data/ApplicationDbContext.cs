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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.TenDangNhap)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // NhaTro -> ChuTro (User)
            modelBuilder.Entity<NhaTro>()
                .HasOne(n => n.ChuTro)
                .WithMany(u => u.DanhSachNhaTro)
                .HasForeignKey(n => n.MaChuTro)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // NguoiThue -> NguoiDungTK (User)
            modelBuilder.Entity<NguoiThue>()
                .HasOne(nt => nt.NguoiDungTK)
                .WithMany(u => u.DanhSachNguoiThue)
                .HasForeignKey(nt => nt.MaNguoiDung)
                .OnDelete(DeleteBehavior.NoAction)
                .IsRequired(false);

            // Phong relationships
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

            // HopDong relationships
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

            // ChiSo relationships
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

            // HoaDon relationships
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

            // ChiTietHoaDon
            modelBuilder.Entity<ChiTietHoaDon>()
                .HasOne(ct => ct.HoaDon)
                .WithMany()
                .HasForeignKey(ct => ct.MaHoaDon)
                .OnDelete(DeleteBehavior.NoAction);

            // ThanhToan
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
        }
    }
}
