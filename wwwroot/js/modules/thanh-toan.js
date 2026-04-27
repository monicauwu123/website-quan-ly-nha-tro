// Module cấu hình: thanhtoan
window.AppModules = window.AppModules || {};
window.AppModules.thanhtoan = {
        title: 'Thanh Toán',
        endpoint: '/api/ThanhToan',
        pk: 'maThanhToan',
        headers: [
            { label: 'Hóa đơn', key: 'maHoaDon', render: v => `HĐ#${v}` },
            { label: 'Khách thuê', key: 'maNguoiThue', render: v => lookups.nguoithue.find(n => n.maNguoiThue === v)?.hoTen || `#${v}` },
            { label: 'Ngày thanh toán', key: 'ngayThanhToan', render: fmtDate },
            { label: 'Số tiền', key: 'tongTien', render: fmtCurrency },
            { label: 'Hình thức', key: 'hinhThucThanhToan' },
            { label: 'Ghi chú', key: 'ghiChu' }
        ],
        fields: [
            { id: 'MaHoaDon', label: 'Hóa đơn', type: 'lookup', lookup: 'hoadon', valField: 'maHoaDon', txtField: 'maHoaDon', required: true },
            { id: 'MaNguoiThue', label: 'Khách thuê', type: 'lookup', lookup: 'nguoithue', valField: 'maNguoiThue', txtField: 'hoTen', required: true },
            { id: 'TongTien', label: 'Số tiền thanh toán (đ)', type: 'number', required: true },
            { id: 'HinhThucThanhToan', label: 'Hình thức thanh toán', type: 'lookup', lookup: 'hinhthuc', valField: 'val', txtField: 'label', required: true },
            { id: 'GhiChu', label: 'Ghi chú', type: 'text' }
        ]
    };
