// Module cấu hình: yeucauthue
window.AppModules = window.AppModules || {};
window.AppModules.yeucauthue = {
        title: 'Yêu Cầu Thuê',
        endpoint: '/api/YeuCauThue',
        pk: 'maYeuCau',
        customModal: true,
        headers: [
            { label: 'Người gửi', key: 'nguoiDung', render: v => v?.hoTen || v?.email || '---' },
            { label: 'Phòng', key: 'phong', render: v => v?.tenPhong || '---' },
            { label: 'Nhà trọ', key: 'phong', render: v => v?.nhaTro?.tenNhaTro || '---' },
            { label: 'Ngày gửi', key: 'ngayGui', render: fmtDate },
            { label: 'Trạng thái', key: 'trangThaiText', render: (v, row) => {
                const cls = row.trangThai === 'ChoDuyet' ? 'badge-warning' : row.trangThai === 'DaLapHopDong' ? 'badge-success' : row.trangThai === 'TuChoi' ? 'badge-danger' : 'badge-info';
                return `<span class="badge ${cls}">${v || row.trangThai || '---'}</span>`;
            }},
            { label: 'Ghi chú', key: 'ghiChuNguoiDung' }
        ]
    };
