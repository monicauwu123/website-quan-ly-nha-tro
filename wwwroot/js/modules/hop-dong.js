// Module cấu hình: hopdong
window.AppModules = window.AppModules || {};
window.AppModules.hopdong = {
    title: 'Hợp Đồng',
    endpoint: '/api/HopDong',
    pk: 'maHopDong',
    customModal: true,
    headers: [
        { label: 'Phòng', key: 'phong', render: (v, row) => v?.tenPhong || `Phòng #${row.maPhong}` },
        { label: 'Khách thuê', key: 'nguoiThue', render: (v, row) => v?.hoTen || `Khách #${row.maNguoiThue}` },
        { label: 'Ngày bắt đầu', key: 'ngayBatDau', render: v => window.AppFormat.date(v) },
        { label: 'Ngày kết thúc', key: 'ngayKetThuc', render: v => v ? window.AppFormat.date(v) : 'Không xác định' },
        { label: 'Tiền cọc', key: 'tienCoc', render: v => window.AppFormat.currency(v) },
        { label: 'Trạng thái', key: 'trangThaiText', render: v => {
            const cls = v === 'Đang còn hiệu lực' ? 'badge-success' : v === 'Sắp hết hợp đồng' ? 'badge-warning' : 'badge-danger';
            return `<span class="badge ${cls}">${v || '---'}</span>`;
        }}
    ]
};
