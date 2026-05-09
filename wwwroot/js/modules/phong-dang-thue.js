// Module cấu hình: phòng đang thuê của người dùng
window.AppModules = window.AppModules || {};
window.AppModules.phongdangthue = {
    title: 'Phòng đang thuê',
    endpoint: '/api/HopDong',
    pk: 'maHopDong',
    customModal: true,
    headers: [
        { label: 'Phòng', key: 'phong', render: (v, row) => v?.tenPhong || `Phòng #${row.maPhong}` },
        { label: 'Khách thuê', key: 'nguoiThue', render: (v, row) => v?.hoTen || `Khách #${row.maNguoiThue}` },
        { label: 'Ngày bắt đầu', key: 'ngayBatDau', render: v => window.AppFormat.date(v) },
        { label: 'Ngày kết thúc', key: 'ngayKetThuc', render: v => v ? window.AppFormat.date(v) : 'Không xác định' },
        { label: 'Tiền cọc', key: 'tienCoc', render: v => window.AppFormat.currency(v) },
        { label: 'Gia hạn', key: null, render: (v, row) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (user.vaiTro !== 'NguoiDung') return '---';
            const disabled = row.trangThai === 'KetThuc' || row.trangThai === 'Huy';
            return disabled
                ? '<span style="color:var(--text-light);font-size:.85rem;">---</span>'
                : `<button class="btn-action btn-edit" style="background:#6366f1;" onclick="openYeuCauGiaHanModal(${row.maHopDong})"><i class="fas fa-calendar-plus"></i> Gia hạn</button>`;
        }},
        { label: 'Trạng thái', key: 'trangThaiText', render: v => {
            const cls = v === 'Đang còn hiệu lực' ? 'badge-success' : v === 'Sắp hết hợp đồng' ? 'badge-warning' : 'badge-danger';
            return `<span class="badge ${cls}">${v || '---'}</span>`;
        }}
    ]
};
