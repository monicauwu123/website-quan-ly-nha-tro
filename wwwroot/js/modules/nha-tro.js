// Module cấu hình: nhatro
window.AppModules = window.AppModules || {};
window.AppModules.nhatro = {
        title: 'Nhà Trọ',
        endpoint: '/api/NhaTro',
        pk: 'maNhaTro',
        headers: [
            { label: 'Tên nhà trọ', key: 'tenNhaTro' },
            { label: 'Địa chỉ', key: 'diaChi' },
            { label: 'Mô tả', key: 'moTa' }
        ],
        fields: [
            { id: 'tenNhaTro', label: 'Tên nhà trọ', type: 'text', required: true },
            { id: 'diaChi', label: 'Địa chỉ', type: 'text', required: true },
            { id: 'moTa', label: 'Mô tả', type: 'textarea' }
        ]
    };
