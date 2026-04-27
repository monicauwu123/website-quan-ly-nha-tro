// Module cấu hình: loaiphong
window.AppModules = window.AppModules || {};
window.AppModules.loaiphong = {
        title: 'Loại Phòng',
        endpoint: '/api/LoaiPhong',
        pk: 'maLoaiPhong',
        headers: [
            { label: 'Tên loại phòng', key: 'tenLoaiPhong' },
            { label: 'Mô tả', key: 'moTa' }
        ],
        fields: [
            { id: 'tenLoaiPhong', label: 'Tên loại phòng', type: 'text', required: true },
            { id: 'moTa', label: 'Mô tả', type: 'textarea' }
        ]
    };
