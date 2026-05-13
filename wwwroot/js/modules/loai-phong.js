// Module cấu hình: loaiphong
window.AppModules = window.AppModules || {};
window.AppModules.loaiphong = {
    title: 'Loại Phòng',
    endpoint: '/api/LoaiPhong',
    pk: 'maLoaiPhong',
    headers: [
        { label: 'Nhà trọ', key: 'maNhaTro', render: (v, item) => item.nhaTro?.tenNhaTro || (window.lookups?.nhatro || []).find(n => n.maNhaTro == v)?.tenNhaTro || '---' },
        { label: 'Tên loại phòng', key: 'tenLoaiPhong' },
        { label: 'Mô tả', key: 'moTa' }
    ],
    fields: [
        { id: 'maNhaTro', label: 'Nhà trọ', type: 'lookup', lookup: 'nhatro', valField: 'maNhaTro', txtField: 'tenNhaTro', required: true },
        { id: 'tenLoaiPhong', label: 'Tên loại phòng', type: 'text', required: true },
        { id: 'moTa', label: 'Mô tả', type: 'textarea' }
    ]
};
