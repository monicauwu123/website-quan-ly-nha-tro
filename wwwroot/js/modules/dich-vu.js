// Module cấu hình: dichvu
window.AppModules = window.AppModules || {};
window.AppModules.dichvu = {
    title: 'Dịch Vụ',
    endpoint: '/api/DichVu',
    pk: 'maDichVu',
    headers: [
        { label: 'Nhà trọ', key: 'maNhaTro', render: (v, item) => item.nhaTro?.tenNhaTro || (window.lookups?.nhatro || []).find(n => n.maNhaTro == v)?.tenNhaTro || '---' },
        { label: 'Tên dịch vụ', key: 'tenDichVu' },
        { label: 'Đơn giá', key: 'tiendichvu', render: v => window.AppFormat.currency(v) }
    ],
    fields: [
        { id: 'maNhaTro', label: 'Nhà trọ', type: 'lookup', lookup: 'nhatro', valField: 'maNhaTro', txtField: 'tenNhaTro', required: true },
        { id: 'tenDichVu', label: 'Tên dịch vụ', type: 'text', required: true },
        { id: 'tiendichvu', label: 'Đơn giá (đ)', type: 'number', required: true }
    ]
};
