// Module cấu hình: yeucauthue
window.AppModules = window.AppModules || {};
window.AppModules.yeucauthue = {
        title: 'Yêu cầu thuê / gia hạn',
        endpoint: '/api/YeuCauTongHop',
        pk: 'idTongHop',
        customModal: true,
        headers: [
            { label: 'Loại yêu cầu', key: 'loaiYeuCauText', render: (v, row) => `<span class="badge ${row.loaiYeuCau === 'GiaHan' ? 'badge-info' : 'badge-teal'}">${v || '---'}</span>` },
            { label: 'Người gửi', key: 'nguoiDung', render: v => v?.hoTen || v?.email || '---' },
            { label: 'Phòng', key: 'phong', render: v => v?.tenPhong || '---' },
            { label: 'Nhà trọ', key: 'phong', render: v => v?.nhaTro?.tenNhaTro || '---' },
            { label: 'Ngày gửi', key: 'ngayGui', render: v => window.AppFormat.date(v) },
            { label: 'Số tháng thuê', key: 'soThangMuonThue', render: v => v ? `${v} tháng` : '---' },
            { label: 'Gia hạn đến', key: 'ngayKetThucMoi', render: (v, row) => row.loaiYeuCau === 'GiaHan' ? window.AppFormat.date(v || row.ngayKetThucMoiDeXuat) : '---' },
            { label: 'Trạng thái', key: 'trangThaiText', render: (v, row) => {
                const cls = row.trangThai === 'ChoDuyet'
                    ? 'badge-warning'
                    : (row.trangThai === 'DaLapHopDong' || row.trangThai === 'DaChapNhan')
                        ? 'badge-success'
                        : (row.trangThai === 'TuChoi' || row.trangThai === 'NguoiThueTuChoi')
                            ? 'badge-danger'
                            : 'badge-info';
                return `<span class="badge ${cls}">${v || row.trangThai || '---'}</span>`;
            }},
            { label: 'Ghi chú', key: 'ghiChuNguoiDung' }
        ]
    };
