// Module cấu hình: thanhtoan
window.AppModules = window.AppModules || {};
const _ttLookups = () => window.lookups || { hoadon: [], nguoithue: [] };
window.AppModules.thanhtoan = {
    title: 'Thanh Toán',
    endpoint: '/api/ThanhToan',
    pk: 'maThanhToan',
    headers: [
        { label: 'Hóa đơn', key: 'maHoaDon', render: v => `HĐ#${v}` },
        { label: 'Khách thuê', key: 'maNguoiThue', render: v => _ttLookups().nguoithue?.find(n => n.maNguoiThue === v)?.hoTen || `#${v}` },
        { label: 'Ngày thanh toán', key: 'ngayThanhToan', render: v => window.AppFormat.date(v) },
        { label: 'Số tiền', key: 'tongTien', render: v => window.AppFormat.currency(v) },
        { label: 'Hình thức', key: 'hinhThucThanhToan' },
        { label: 'Ghi chú', key: 'ghiChu' }
    ],
    fields: [
        { id: 'maHoaDon', label: 'Hóa đơn', type: 'lookup', lookup: 'hoadon', valField: 'maHoaDon', txtField: 'maHoaDon', required: true },
        { id: 'maNguoiThue', label: 'Khách thuê', type: 'lookup', lookup: 'nguoithue', valField: 'maNguoiThue', txtField: 'hoTen', required: true },
        { id: 'tongTien', label: 'Số tiền thanh toán (đ)', type: 'number', required: true },
        { id: 'hinhThucThanhToan', label: 'Hình thức thanh toán', type: 'lookup', lookup: 'hinhthuc', valField: 'val', txtField: 'label', required: true },
        { id: 'ghiChu', label: 'Ghi chú', type: 'text' }
    ]
};
