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
            { label: 'Tiền phòng', key: 'tienPhong', render: fmtCurrency },
            { label: 'Tiền điện', key: 'tienDien', render: fmtCurrency },
            { label: 'Tiền nước', key: 'tienNuoc', render: fmtCurrency },
            { label: 'Phát sinh khác', key: 'tienPhatSinhKhac', render: fmtCurrency },
            { label: 'Tổng tiền', key: 'tongTien', render: v => `<strong style="color:var(--primary)">${fmtCurrency(v)}</strong>` },
            { label: 'Ngày lập', key: 'ngayLap', render: fmtDate },
            { label: 'In', key: 'maHoaDon', render: v => `<button class="btn" style="padding:0.2rem 0.5rem; background:#6366f1; color:white;" onclick="API.hoadon.exportPdf(${v})"><i class="fas fa-file-pdf"></i> PDF</button>` }
        ]
    };
