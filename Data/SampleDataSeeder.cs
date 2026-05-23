using System.Text.Json;
using DoAnSE104.Models;

namespace DoAnSE104.Data
{
    public static class SampleDataSeeder
    {
        public static void Seed(ApplicationDbContext context)
        {
            if (context.NhaTro.Any())
                return;

            if (context.Users.Any(u => u.TenDangNhap != "Admin"))
                return;

            var password = BCrypt.Net.BCrypt.HashPassword("123456");
            var today = DateTime.Today;
            var kyHienTai = today.ToString("yyyy-MM");
            var thangTruoc = today.AddMonths(-1);
            var kyThangTruoc = thangTruoc.ToString("yyyy-MM");

            var trangThaiTrong = GetTrangThaiId(context, 1, "trong");
            var trangThaiDaThue = GetTrangThaiId(context, 2, "thue");
            var trangThaiSuaChua = GetTrangThaiId(context, 3, "sua");

            var chuTros = CreateLandlords(password);
            var nguoiDungs = CreateTenantUsers(password);
            context.Users.AddRange(chuTros);
            context.Users.AddRange(nguoiDungs);
            context.SaveChanges();

            var nhaTros = CreateHouses(chuTros);
            context.NhaTro.AddRange(nhaTros);
            context.SaveChanges();

            var loaiPhongs = CreateRoomTypes(nhaTros);
            context.LoaiPhong.AddRange(loaiPhongs);
            context.SaveChanges();

            var dichVus = CreateServices(nhaTros);
            context.DichVu.AddRange(dichVus);
            context.SaveChanges();

            var phongs = CreateRooms(nhaTros, loaiPhongs, trangThaiTrong, trangThaiDaThue, trangThaiSuaChua);
            context.Phong.AddRange(phongs);
            context.SaveChanges();

            var phongDangThue = phongs.Where(p => p.MaTrangThai == trangThaiDaThue).Take(15).ToList();
            var nguoiThues = CreateTenants(nguoiDungs, phongDangThue);
            context.NguoiThue.AddRange(nguoiThues);
            context.SaveChanges();

            var hopDongs = CreateContracts(nguoiThues, today);
            context.HopDong.AddRange(hopDongs);

            var yeuCauThues = CreateRentalRequests(nguoiDungs, phongs.Where(p => p.MaTrangThai == trangThaiTrong).ToList(), today);
            context.YeuCauThue.AddRange(yeuCauThues);
            context.SaveChanges();

            var dangKyDichVus = CreateServiceRegistrations(nguoiThues, dichVus, phongs, today, kyHienTai, kyThangTruoc);
            context.DangKyDichVu.AddRange(dangKyDichVus);

            var dien = CreateElectricReadings(phongDangThue, today, thangTruoc);
            var nuoc = CreateWaterReadings(phongDangThue, today, thangTruoc);
            context.ChiSoDien.AddRange(dien);
            context.ChiSoNuoc.AddRange(nuoc);
            context.SaveChanges();

            var hoaDons = CreateInvoices(nguoiThues, dien, nuoc, dangKyDichVus, today, kyHienTai, thangTruoc, kyThangTruoc);
            context.HoaDon.AddRange(hoaDons);
            context.SaveChanges();

            var chiTietHoaDons = CreateInvoiceDetails(hoaDons, dien, nuoc);
            context.ChiTietHoaDon.AddRange(chiTietHoaDons);

            var thanhToans = CreatePayments(hoaDons, today);
            context.ThanhToan.AddRange(thanhToans);

            var suCos = CreateIncidentReports(nguoiDungs, phongDangThue, today);
            context.BaoCaoSuCo.AddRange(suCos);

            context.SaveChanges();
        }

        private static List<User> CreateLandlords(string password)
        {
            var data = new[]
            {
                ("chutro", "Nguyễn Minh Quân", "chutro@example.com", "0901000001", "Vietcombank", "VCB"),
                ("chutro2", "Trần Thị Thu Hà", "chutro2@example.com", "0901000002", "Techcombank", "TCB"),
                ("chutro3", "Lê Hoàng Phúc", "chutro3@example.com", "0901000003", "ACB", "ACB"),
                ("chutro4", "Phạm Gia Hân", "chutro4@example.com", "0901000004", "MB Bank", "MB"),
                ("chutro5", "Đỗ Thành Đạt", "chutro5@example.com", "0901000005", "BIDV", "BIDV")
            };

            return data.Select((x, i) => new User
            {
                TenDangNhap = x.Item1,
                HoTen = x.Item2,
                Email = x.Item3,
                SoDienThoai = x.Item4,
                VaiTro = "ChuTro",
                MatKhau = password,
                TenNganHang = x.Item5,
                MaNganHang = x.Item6,
                SoTaiKhoan = $"10203040{i + 1:D2}",
                TenChuTaiKhoan = RemoveDiacriticsUpper(x.Item2),
                NoiDungChuyenKhoanMacDinh = "Thanh toan hoa don {MaHoaDon} phong {TenPhong} ky {KyHoaDon}"
            }).ToList();
        }

        private static List<User> CreateTenantUsers(string password)
        {
            var names = new[]
            {
                "Người Thuê Demo", "Trần Thị Mai", "Lê Văn Nam", "Võ Thị Hạnh", "Đỗ Quốc Bảo",
                "Phạm Ngọc Linh", "Hoàng Gia Huy", "Nguyễn Hoài An", "Bùi Khánh Vy", "Đặng Minh Khang",
                "Phan Tuấn Kiệt", "Vũ Thanh Tâm", "Mai Phương Anh", "Cao Nhật Minh", "Tạ Hồng Nhung",
                "Lâm Đức Anh", "Trịnh Bảo Châu", "Hồ Quang Vinh", "Ngô Mỹ Duyên", "Dương Hải Đăng"
            };

            return names.Select((name, i) => new User
            {
                TenDangNhap = i == 0 ? "nguoithue" : $"nguoithue{i + 1}",
                HoTen = name,
                Email = i == 0 ? "nguoithue@example.com" : $"nguoithue{i + 1}@example.com",
                SoDienThoai = $"091{i + 1:D7}",
                CCCD = $"07920{i + 1:D7}",
                NgaySinh = DateTime.Today.AddYears(-20 - (i % 12)).AddDays(i * 19),
                GioiTinh = i % 2 == 0 ? "Nam" : "Nữ",
                QuocTich = "Việt Nam",
                DiaChi = $"{10 + i} Nguyễn Trãi, TP.HCM",
                NoiCongTac = i % 3 == 0 ? "Sinh viên" : i % 3 == 1 ? "Nhân viên văn phòng" : "Kinh doanh tự do",
                VaiTro = "NguoiDung",
                MatKhau = password
            }).ToList();
        }

        private static List<NhaTro> CreateHouses(List<User> chuTros)
        {
            var houseNames = new[]
            {
                "Nhà trọ An Bình", "Ký túc xá Mini Hoa Sen", "Căn hộ dịch vụ Minh Quân", "Nhà trọ Bình Minh",
                "Studio Green Home", "Nhà trọ Tân Phú", "Căn hộ Blue Sky", "Nhà trọ Gần Đại Học",
                "Khu phòng trọ Sunrise", "Nhà trọ Mộc Lan"
            };

            return houseNames.Select((name, i) => new NhaTro
            {
                TenNhaTro = name,
                DiaChi = $"{100 + i * 7} {new[] { "Lê Lợi", "Nguyễn Văn Cừ", "Phạm Văn Đồng", "Cộng Hòa", "Điện Biên Phủ" }[i % 5]}, TP.HCM",
                MoTa = i % 2 == 0 ? "Khu nhà trọ an ninh, gần tiện ích và trạm xe buýt." : "Phòng đa dạng diện tích, phù hợp sinh viên và người đi làm.",
                MaChuTro = chuTros[i % chuTros.Count].MaNguoiDung,
                HinhAnh = $"https://picsum.photos/seed/nhatro{i + 1}/640/420",
                DanhSachHinhAnh = JsonSerializer.Serialize(new[]
                {
                    $"https://picsum.photos/seed/nhatro{i + 1}a/900/600",
                    $"https://picsum.photos/seed/nhatro{i + 1}b/900/600"
                })
            }).ToList();
        }

        private static List<LoaiPhong> CreateRoomTypes(List<NhaTro> nhaTros)
        {
            var result = new List<LoaiPhong>();
            foreach (var nhaTro in nhaTros)
            {
                result.Add(new LoaiPhong { TenLoaiPhong = "Phòng thường", MoTa = "Phòng cơ bản, chi phí hợp lý", MaNhaTro = nhaTro.MaNhaTro, MaChuTro = nhaTro.MaChuTro });
                result.Add(new LoaiPhong { TenLoaiPhong = "Phòng gác lửng", MoTa = "Có gác, tối ưu không gian", MaNhaTro = nhaTro.MaNhaTro, MaChuTro = nhaTro.MaChuTro });
                result.Add(new LoaiPhong { TenLoaiPhong = "Studio", MoTa = "Rộng, có bếp và nội thất cơ bản", MaNhaTro = nhaTro.MaNhaTro, MaChuTro = nhaTro.MaChuTro });
            }
            return result;
        }

        private static List<DichVu> CreateServices(List<NhaTro> nhaTros)
        {
            var templates = new[]
            {
                ("Internet", 100000f, "TinhPhi"),
                ("Vệ sinh", 60000f, "TinhPhi"),
                ("Giữ xe máy", 90000f, "TinhPhi"),
                ("Máy giặt chung", 70000f, "TinhPhi"),
                ("Camera an ninh", 0f, "TienIch"),
                ("Thang máy", 0f, "TienIch"),
                ("Máy lạnh", 0f, "TienNghi"),
                ("Ban công", 0f, "TienNghi")
            };

            return nhaTros.SelectMany(nhaTro => templates.Select(t => new DichVu
            {
                TenDichVu = t.Item1,
                Tiendichvu = t.Item2,
                LoaiDichVu = t.Item3,
                MaNhaTro = nhaTro.MaNhaTro,
                MaChuTro = nhaTro.MaChuTro
            })).ToList();
        }

        private static List<Phong> CreateRooms(List<NhaTro> nhaTros, List<LoaiPhong> loaiPhongs, int trangThaiTrong, int trangThaiDaThue, int trangThaiSuaChua)
        {
            var result = new List<Phong>();
            var index = 0;

            foreach (var nhaTro in nhaTros)
            {
                var roomTypes = loaiPhongs.Where(lp => lp.MaNhaTro == nhaTro.MaNhaTro).ToList();
                for (var i = 1; i <= 4; i++)
                {
                    var status = i switch
                    {
                        1 or 2 => trangThaiDaThue,
                        3 => trangThaiTrong,
                        _ => index % 3 == 0 ? trangThaiSuaChua : trangThaiTrong
                    };

                    result.Add(new Phong
                    {
                        TenPhong = $"{(char)('A' + index % 5)}{i:00}",
                        MaNhaTro = nhaTro.MaNhaTro,
                        MaLoaiPhong = roomTypes[(i - 1) % roomTypes.Count].MaLoaiPhong,
                        MaTrangThai = status,
                        DienTich = 18 + (i * 4) + (index % 3),
                        GiaPhong = 1800000 + (i * 450000) + ((index % 4) * 250000),
                        SucChua = i % 3 + 1,
                        MoTa = status == trangThaiDaThue ? "Phòng đang có hợp đồng hiệu lực" : status == trangThaiSuaChua ? "Phòng đang sửa chữa nhẹ" : "Phòng trống sẵn sàng cho thuê",
                        DiaChiPhong = $"Tầng {i}",
                        HinhAnh = $"https://picsum.photos/seed/phong{index + 1}/640/420",
                        DanhSachHinhAnh = JsonSerializer.Serialize(new[] { $"https://picsum.photos/seed/phong{index + 1}a/900/600", $"https://picsum.photos/seed/phong{index + 1}b/900/600" })
                    });
                    index++;
                }
            }

            return result;
        }

        private static List<NguoiThue> CreateTenants(List<User> nguoiDungs, List<Phong> phongDangThue)
        {
            return phongDangThue.Select((phong, i) =>
            {
                var user = nguoiDungs[i % nguoiDungs.Count];
                return new NguoiThue
                {
                    HoTen = user.HoTen,
                    CCCD = user.CCCD,
                    SDT = user.SoDienThoai,
                    Email = user.Email,
                    NgaySinh = user.NgaySinh,
                    GioiTinh = user.GioiTinh,
                    QuocTich = "Việt Nam",
                    DiaChi = user.DiaChi,
                    NoiCongTac = user.NoiCongTac,
                    MaPhong = phong.MaPhong,
                    MaNguoiDung = user.MaNguoiDung,
                    TrangThai = "DangThue"
                };
            }).ToList();
        }

        private static List<HopDong> CreateContracts(List<NguoiThue> nguoiThues, DateTime today)
        {
            return nguoiThues.Select((nt, i) => new HopDong
            {
                MaNguoiThue = nt.MaNguoiThue,
                MaPhong = nt.MaPhong,
                NgayBatDau = today.AddMonths(-(i % 8 + 1)).AddDays(-(i % 7)),
                NgayKetThuc = i % 5 == 0 ? today.AddDays(5 + i) : today.AddMonths(4 + (i % 8)),
                TienCoc = 1500000 + (i % 5) * 500000,
                NoiDung = $"Hợp đồng thuê phòng mẫu #{i + 1}",
                TrangThai = i == nguoiThues.Count - 1 ? "KetThuc" : "DangHieuLuc"
            }).ToList();
        }

        private static List<YeuCauThue> CreateRentalRequests(List<User> users, List<Phong> phongTrong, DateTime today)
        {
            var statuses = new[] { "ChoDuyet", "ChoDuyet", "TuChoi", "DaDuyet" };
            return phongTrong.Take(8).Select((phong, i) => new YeuCauThue
            {
                MaNguoiDung = users[(i + 12) % users.Count].MaNguoiDung,
                MaPhong = phong.MaPhong,
                NgayGui = today.AddDays(-i - 1),
                TrangThai = statuses[i % statuses.Length],
                GhiChuNguoiDung = $"Muốn thuê phòng trong {2 + i} tháng, cần xem phòng trước.",
                GhiChuChuTro = i % 4 == 2 ? "Phòng chưa phù hợp số người đăng ký." : null,
                NgayXuLy = i % 4 >= 2 ? today.AddDays(-i) : null,
                SoThangMuonThue = 2 + (i % 10),
                NgayBatDauMongMuon = today.AddDays(5 + i)
            }).ToList();
        }

        private static List<DangKyDichVu> CreateServiceRegistrations(List<NguoiThue> tenants, List<DichVu> services, List<Phong> rooms, DateTime today, string kyHienTai, string kyThangTruoc)
        {
            var result = new List<DangKyDichVu>();
            foreach (var tenant in tenants)
            {
                var room = rooms.FirstOrDefault(r => r.MaPhong == tenant.MaPhong);
                var roomServices = services
                    .Where(s => s.LoaiDichVu == "TinhPhi" && s.MaNhaTro == room?.MaNhaTro)
                    .ToList();
                foreach (var service in roomServices.Take(2 + tenant.MaNguoiThue % 3))
                {
                    result.Add(new DangKyDichVu
                    {
                        MaNguoiDung = tenant.MaNguoiDung ?? 0,
                        MaNguoiThue = tenant.MaNguoiThue,
                        MaPhong = tenant.MaPhong,
                        MaDichVu = service.MaDichVu,
                        NgayDangKy = today.AddMonths(-(tenant.MaNguoiThue % 3 + 1)),
                        KyDangKy = tenant.MaNguoiThue % 2 == 0 ? kyHienTai : kyThangTruoc,
                        TrangThai = tenant.MaNguoiThue % 7 == 0 ? "DaHuy" : "DangSuDung",
                        GhiChu = "Dữ liệu mẫu đăng ký dịch vụ"
                    });
                }
            }
            return result;
        }

        private static List<ChiSoDien> CreateElectricReadings(List<Phong> rooms, DateTime today, DateTime lastMonth)
        {
            var result = new List<ChiSoDien>();
            foreach (var room in rooms)
            {
                var oldValue = 50 + room.MaPhong * 3 % 180;
                var usage = 18 + room.MaPhong % 70;
                result.Add(new ChiSoDien { MaPhong = room.MaPhong, SoDienCu = oldValue, SoDienMoi = oldValue + usage, GiaDien = 3500, TienDien = usage * 3500, NgayThangDien = today.AddDays(-(room.MaPhong % 5)) });
                result.Add(new ChiSoDien { MaPhong = room.MaPhong, SoDienCu = oldValue - usage, SoDienMoi = oldValue, GiaDien = 3500, TienDien = usage * 3500, NgayThangDien = lastMonth.AddDays(room.MaPhong % 10) });
            }
            return result;
        }

        private static List<ChiSoNuoc> CreateWaterReadings(List<Phong> rooms, DateTime today, DateTime lastMonth)
        {
            var result = new List<ChiSoNuoc>();
            foreach (var room in rooms)
            {
                var oldValue = 10 + room.MaPhong % 45;
                var usage = 4 + room.MaPhong % 12;
                result.Add(new ChiSoNuoc { MaPhong = room.MaPhong, SoNuocCu = oldValue, SoNuocMoi = oldValue + usage, GiaNuoc = 15000, TienNuoc = usage * 15000, NgayThangNuoc = today.AddDays(-(room.MaPhong % 5)) });
                result.Add(new ChiSoNuoc { MaPhong = room.MaPhong, SoNuocCu = oldValue - usage, SoNuocMoi = oldValue, GiaNuoc = 15000, TienNuoc = usage * 15000, NgayThangNuoc = lastMonth.AddDays(room.MaPhong % 10) });
            }
            return result;
        }

        private static List<HoaDon> CreateInvoices(List<NguoiThue> tenants, List<ChiSoDien> dien, List<ChiSoNuoc> nuoc, List<DangKyDichVu> dangKy, DateTime today, string kyHienTai, DateTime thangTruoc, string kyThangTruoc)
        {
            var result = new List<HoaDon>();
            foreach (var tenant in tenants)
            {
                var dienNow = dien.Where(d => d.MaPhong == tenant.MaPhong).OrderByDescending(d => d.NgayThangDien).First();
                var nuocNow = nuoc.Where(n => n.MaPhong == tenant.MaPhong).OrderByDescending(n => n.NgayThangNuoc).First();
                var tienDv = dangKy.Count(dk => dk.MaNguoiThue == tenant.MaNguoiThue && dk.TrangThai == "DangSuDung") * 90000m;
                result.Add(new HoaDon { MaNguoiThue = tenant.MaNguoiThue, MaPhong = tenant.MaPhong, MaDien = dienNow.MaDien, MaNuoc = nuocNow.MaNuoc, LoaiHoaDon = "HangThang", TienPhatSinhKhac = tenant.MaNguoiThue % 4 == 0 ? 50000 : 0, TongTien = dienNow.TienDien + nuocNow.TienNuoc + tienDv + (tenant.MaNguoiThue % 4 == 0 ? 50000 : 0), NgayLap = today, KyHoaDon = kyHienTai, TrangThai = tenant.MaNguoiThue % 5 == 0 ? "DaThanhToan" : "ChuaThanhToan" });
                if (tenant.MaNguoiThue % 3 == 0)
                {
                    result.Add(new HoaDon { MaNguoiThue = tenant.MaNguoiThue, MaPhong = tenant.MaPhong, LoaiHoaDon = "ThuePhong", TienPhatSinhKhac = 0, TongTien = 2000000 + tenant.MaNguoiThue * 100000, NgayLap = today.AddDays(-10), KyHoaDon = kyHienTai, TrangThai = "ChuaThanhToan" });
                }
                if (tenant.MaNguoiThue % 2 == 0)
                {
                    result.Add(new HoaDon { MaNguoiThue = tenant.MaNguoiThue, MaPhong = tenant.MaPhong, LoaiHoaDon = "HangThang", TienPhatSinhKhac = 0, TongTien = 300000 + tenant.MaNguoiThue * 10000, NgayLap = thangTruoc, KyHoaDon = kyThangTruoc, TrangThai = "ChuaThanhToan" });
                }
            }
            return result;
        }

        private static List<ChiTietHoaDon> CreateInvoiceDetails(List<HoaDon> invoices, List<ChiSoDien> dien, List<ChiSoNuoc> nuoc)
        {
            var result = new List<ChiTietHoaDon>();
            foreach (var invoice in invoices)
            {
                if (invoice.LoaiHoaDon == "ThuePhong")
                {
                    result.Add(new ChiTietHoaDon { MaHoaDon = invoice.MaHoaDon, LoaiKhoan = "TienPhong", SoTien = invoice.TongTien });
                    continue;
                }
                var tienDien = dien.FirstOrDefault(d => d.MaDien == invoice.MaDien)?.TienDien ?? 0;
                var tienNuoc = nuoc.FirstOrDefault(n => n.MaNuoc == invoice.MaNuoc)?.TienNuoc ?? 0;
                if (tienDien > 0) result.Add(new ChiTietHoaDon { MaHoaDon = invoice.MaHoaDon, LoaiKhoan = "TienDien", SoTien = tienDien });
                if (tienNuoc > 0) result.Add(new ChiTietHoaDon { MaHoaDon = invoice.MaHoaDon, LoaiKhoan = "TienNuoc", SoTien = tienNuoc });
                if (invoice.TienPhatSinhKhac > 0) result.Add(new ChiTietHoaDon { MaHoaDon = invoice.MaHoaDon, LoaiKhoan = "PhatSinhKhac", SoTien = invoice.TienPhatSinhKhac });
            }
            return result;
        }

        private static List<ThanhToan> CreatePayments(List<HoaDon> invoices, DateTime today)
        {
            return invoices.Where((invoice, i) => i % 3 != 1).Select((invoice, i) => new ThanhToan
            {
                MaHoaDon = invoice.MaHoaDon,
                MaNguoiThue = invoice.MaNguoiThue,
                NgayThanhToan = today.AddDays(-(i % 12)),
                TongTien = i % 3 == 0 ? invoice.TongTien : Math.Round(invoice.TongTien * 0.45m, 0),
                HinhThucThanhToan = i % 2 == 0 ? "ChuyenKhoan" : "TienMat",
                GhiChu = i % 3 == 0 ? "Đã thanh toán đủ" : "Thanh toán một phần",
                TrangThaiXacNhan = i % 4 == 0 ? "ChoXacNhan" : "DaXacNhan",
                MaGiaoDich = $"GD{today:yyyyMMdd}{i:D4}"
            }).ToList();
        }

        private static List<BaoCaoSuCo> CreateIncidentReports(List<User> users, List<Phong> rooms, DateTime today)
        {
            var titles = new[] { "Rò nước trong phòng", "Mất điện khu vực", "Khóa cửa bị kẹt", "Máy lạnh không mát", "Wifi yếu", "Đèn hành lang hỏng", "Ống thoát nước nghẹt", "Cần kiểm tra camera" };
            var levels = new[] { "Bình thường", "Gấp", "Rất gấp" };
            var statuses = new[] { "Moi", "DangXuLy", "DaXuLy", "Huy" };
            return titles.Select((title, i) => new BaoCaoSuCo
            {
                MaNguoiDung = users[i % users.Count].MaNguoiDung,
                MaPhong = rooms[i % rooms.Count].MaPhong,
                TieuDe = title,
                NoiDung = $"Mô tả chi tiết sự cố mẫu: {title.ToLower()}.",
                MucDo = levels[i % levels.Length],
                TrangThai = statuses[i % statuses.Length],
                NgayGui = today.AddDays(-i),
                NgayXuLy = i % 4 >= 1 ? today.AddDays(-Math.Max(i - 1, 0)) : null,
                PhanHoiChuTro = i % 4 >= 1 ? "Chủ trọ đã tiếp nhận và đang xử lý theo lịch." : null
            }).ToList();
        }

        private static int GetTrangThaiId(ApplicationDbContext context, int fallbackOrder, params string[] keywords)
        {
            var trangThais = context.TrangThai
                .AsEnumerable()
                .OrderBy(t => t.MaTrangThai)
                .ToList();

            var trangThai = trangThais.FirstOrDefault(t =>
            {
                var normalizedName = RemoveDiacriticsUpper(t.TenTrangThai);
                return keywords.Any(k => normalizedName.Contains(RemoveDiacriticsUpper(k), StringComparison.OrdinalIgnoreCase));
            });

            if (trangThai == null)
            {
                trangThai = trangThais.Skip(fallbackOrder - 1).FirstOrDefault();
            }

            if (trangThai == null)
                throw new InvalidOperationException($"Khong tim thay trang thai phong phu hop: {string.Join(", ", keywords)}");

            return trangThai.MaTrangThai;
        }
        private static string RemoveDiacriticsUpper(string value)
        {
            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            var chars = normalized
                .Where(c => System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray();
            return new string(chars).Normalize(System.Text.NormalizationForm.FormC).ToUpperInvariant();
        }
    }
}
