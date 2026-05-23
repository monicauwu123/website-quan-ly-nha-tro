// Module cấu hình: nhatro
window.AppModules = window.AppModules || {};
const renderNhaTroImage = item => {
    const url = item?.hinhAnh || parseJsonArraySafe(item?.danhSachHinhAnh)[0];
    return url ? `<img src="${url}" style="width:72px;height:48px;object-fit:cover;border-radius:6px;border:1px solid #e5e7eb;" onerror="this.style.display='none'">` : '---';
};
const renderNhaTroTienIch = item => renderServiceBadges(item, 'TienIch');
window.AppModules.nhatro = {
        title: 'Nhà Trọ',
        endpoint: '/api/NhaTro',
        pk: 'maNhaTro',
        disableAdvancedSearch: true,
        headers: [
            { label: 'Ảnh', key: '_image', render: (v, item) => renderNhaTroImage(item) },
            { label: 'Tên nhà trọ', key: 'tenNhaTro' },
            { label: 'Địa chỉ', key: 'diaChi' },
            { label: 'Tiện ích', key: '_tienIch', render: (v, item) => renderNhaTroTienIch(item) },
            { label: 'Mô tả', key: 'moTa' }
        ],
        fields: [
            { id: 'tenNhaTro', label: 'Tên nhà trọ', type: 'text', required: true },
            { id: 'diaChi', label: 'Địa chỉ', type: 'text', required: true },
            { id: 'fileUploadNhaTro', label: 'Ảnh nhà trọ', type: 'fileMultiple', uploadTarget: 'nhatro' },
            { id: 'danhSachHinhAnh', label: 'URL ảnh đã lưu', type: 'hiddenJsonArray' },
            { id: 'moTa', label: 'Mô tả', type: 'textarea' }
        ]
    };
