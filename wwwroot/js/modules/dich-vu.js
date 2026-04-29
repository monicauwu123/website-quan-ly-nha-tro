// Module cấu hình: dichvu
window.AppModules = window.AppModules || {};
window.AppModules.dichvu = {
    title: 'Dịch Vụ',
    endpoint: '/api/DichVu',
    pk: 'maDichVu',
    headers: [
        { label: 'Tên dịch vụ', key: 'tenDichVu' },
        { label: 'Đơn giá', key: 'tiendichvu', render: v => window.AppFormat.currency(v) }
    ],
    fields: [
        { id: 'tenDichVu', label: 'Tên dịch vụ', type: 'text', required: true },
        { id: 'tiendichvu', label: 'Đơn giá (đ)', type: 'number', required: true }
    ]
};
