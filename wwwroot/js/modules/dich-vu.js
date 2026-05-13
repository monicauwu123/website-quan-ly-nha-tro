// Module cấu hình: dichvu
window.AppModules = window.AppModules || {};
window.AppModules.dichvu = {
    title: 'Dịch Vụ',
    endpoint: '/api/DichVu',
    pk: 'maDichVu',
    headers: [
        { label: 'Nhà trọ', key: 'maNhaTro', render: (v, item) => item.nhaTro?.tenNhaTro || (window.lookups?.nhatro || []).find(n => n.maNhaTro == v)?.tenNhaTro || '---' },
        { label: 'Tên dịch vụ', key: 'tenDichVu' },
        { label: 'Phân loại', key: 'loaiDichVu', render: v => {
            const map = { TinhPhi: 'Tính phí', TienIch: 'Tiện ích nhà trọ', TienNghi: 'Tiện nghi phòng' };
            const cls = v === 'TinhPhi' ? 'badge-blue' : v === 'TienIch' ? 'badge-green' : 'badge-amber';
            return `<span class="badge ${cls}">${map[v] || map.TinhPhi}</span>`;
        }},
        { label: 'Đơn giá', key: 'tiendichvu', render: v => window.AppFormat.currency(v) }
    ],
    fields: [
        { id: 'maNhaTro', label: 'Nhà trọ', type: 'lookup', lookup: 'nhatro', valField: 'maNhaTro', txtField: 'tenNhaTro', required: true },
        { id: 'loaiDichVu', label: 'Phân loại', type: 'optionsMap', required: true, options: [
            { value: 'TinhPhi', label: 'Dịch vụ tính phí' },
            { value: 'TienIch', label: 'Tiện ích nhà trọ' },
            { value: 'TienNghi', label: 'Tiện nghi phòng' }
        ], defaultVal: 'TinhPhi' },
        { id: 'tenDichVu', label: 'Tên dịch vụ', type: 'text', required: true },
        { id: 'tiendichvu', label: 'Đơn giá (đ)', type: 'number', required: true }
    ]
};
