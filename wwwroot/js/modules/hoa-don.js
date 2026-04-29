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
        { label: 'Kỳ hóa đơn', key: 'kyHoaDon' },
        { label: 'Tiền phòng', key: 'tienPhong', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền điện', key: 'tienDien', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền nước', key: 'tienNuoc', render: v => window.AppFormat.currency(v) },
        { label: 'Dịch vụ', key: 'tienDichVu', render: v => window.AppFormat.currency(v) },
        { label: 'Phát sinh khác', key: 'tienPhatSinhKhac', render: v => window.AppFormat.currency(v) },
        { label: 'Tổng tiền', key: 'tongTien', render: v => `<strong style="color:var(--primary)">${window.AppFormat.currency(v)}</strong>` },
        { label: 'Đã thanh toán', key: 'daThanhToan', render: v => window.AppFormat.currency(v || 0) },
        { label: 'Trạng thái', key: 'trangThaiThanhToan', render: (v, row) => {
            const conLai = row.conLai ?? Math.max((row.tongTien || 0) - (row.daThanhToan || 0), 0);
            const text = v || (conLai <= 0 ? 'Đã thanh toán' : ((row.daThanhToan || 0) > 0 ? 'Thanh toán một phần' : 'Chưa thanh toán'));
            const cls = conLai <= 0 ? 'badge-success' : ((row.daThanhToan || 0) > 0 ? 'badge-warning' : 'badge-danger');
            return `<span class="badge ${cls}">${text}</span>`;
        } },
        { label: 'Ngày lập', key: 'ngayLap', render: v => window.AppFormat.date(v) },
        { label: 'PDF', key: 'maHoaDon', render: v => `<button class="btn" style="padding:0.2rem 0.5rem;background:#6366f1;color:white;" onclick="API.hoadon.exportPdf(${v})"><i class="fas fa-file-pdf"></i> PDF</button>` }
    ]
};
