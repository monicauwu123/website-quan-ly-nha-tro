// Module cấu hình: hoadon
window.AppModules = window.AppModules || {};
window.AppModules.hoadon = {
    title: 'Hóa Đơn',
    endpoint: '/api/HoaDon',
    pk: 'maHoaDon',
    customModal: true,
    headers: [
        { label: 'Phòng', key: 'tenPhong' },
        { label: 'Khách thuê', key: 'tenNguoiThue' },
        { label: 'Loại hóa đơn', key: 'tenLoaiHoaDon', render: v => `<span class="badge ${v === 'Hóa đơn thuê phòng' ? 'badge-amber' : 'badge-blue'}">${v || 'Hóa đơn hằng tháng'}</span>` },
        { label: 'Kỳ hóa đơn', key: 'kyHoaDon' },
        { label: 'Tiền phòng', key: 'tienPhong', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền điện', key: 'tienDien', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền nước', key: 'tienNuoc', render: v => window.AppFormat.currency(v) },
        { label: 'Phát sinh khác', key: 'tienPhatSinhKhac', render: v => window.AppFormat.currency(v) },
        { label: 'Tổng tiền', key: 'tongTien', render: v => `<strong style="color:var(--primary)">${window.AppFormat.currency(v)}</strong>` },
        { label: 'Đã thanh toán', key: 'daThanhToan', render: v => window.AppFormat.currency(v) },
        { label: 'Còn lại', key: 'conLai', render: v => `<strong style="color:${Number(v || 0) > 0 ? 'var(--error)' : 'var(--success)'}">${window.AppFormat.currency(v)}</strong>` },
        { label: 'Trạng thái', key: 'trangThaiThanhToan', render: v => `<span class="badge ${v === 'Đã thanh toán' ? 'badge-green' : v === 'Thanh toán một phần' ? 'badge-amber' : 'badge-red'}">${v || 'Chưa thanh toán'}</span>` },
        { label: 'Ngày lập', key: 'ngayLap', render: v => window.AppFormat.date(v) }
    ]
};
