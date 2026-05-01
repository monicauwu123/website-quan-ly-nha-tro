// Module cấu hình: đăng ký dịch vụ
window.AppModules = window.AppModules || {};
window.AppModules.dangkydichvu = {
    title: 'Đăng Ký Dịch Vụ',
    endpoint: '/api/DangKyDichVu',
    pk: 'maDangKyDichVu',
    customModal: true,
    headers: [
        { label: 'Phòng', key: 'tenPhong', render: (v, item) => `${window.AppFormat.escapeHtml(v || '---')}${item.tenNhaTro ? '<br><small style="color:var(--text-light);">' + window.AppFormat.escapeHtml(item.tenNhaTro) + '</small>' : ''}` },
        { label: 'Dịch vụ', key: 'tenDichVu' },
        { label: 'Đơn giá', key: 'tienDichVu', render: v => window.AppFormat.currency(v) },
        { label: 'Ngày đăng ký', key: 'ngayDangKy', render: v => window.AppFormat.date(v) },
        { label: 'Trạng thái', key: 'trangThai', render: (v, item) => `<span class="badge ${v === 'DaHuy' ? 'badge-red' : 'badge-green'}">${item.tenTrangThai || (v === 'DaHuy' ? 'Đã hủy' : 'Đang sử dụng')}</span>` },
        { label: 'Ghi chú', key: 'ghiChu' }
    ],
    fields: []
};
