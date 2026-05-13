// Module cấu hình: phong
window.AppModules = window.AppModules || {};
const _pLookups = () => window.lookups || { nhatro: [], loaiphong: [], trangthai: [] };
const renderPhongImage = item => {
    const url = item?.hinhAnh || parseJsonArraySafe(item?.danhSachHinhAnh)[0];
    return url ? `<img src="${url}" style="width:72px;height:48px;object-fit:cover;border-radius:6px;border:1px solid #e5e7eb;" onerror="this.style.display='none'">` : '---';
};
window.AppModules.phong = {
    title: 'Phòng Trọ',
    endpoint: '/api/Phong',
    pk: 'maPhong',
    headers: [
        { label: 'Ảnh', key: '_image', render: (v, item) => renderPhongImage(item) },
        { label: 'Tên phòng', key: 'tenPhong' },
        { label: 'Nhà trọ', key: 'maNhaTro', render: v => _pLookups().nhatro?.find(n => n.maNhaTro === v)?.tenNhaTro || `#${v}` },
        { label: 'Loại phòng', key: 'maLoaiPhong', render: v => _pLookups().loaiphong?.find(l => l.maLoaiPhong === v)?.tenLoaiPhong || `#${v}` },
        { label: 'Giá thuê', key: 'giaPhong', render: v => window.AppFormat.currency(v) },
        { label: 'Diện tích', key: 'dienTich', render: v => v ? `${v} m²` : '---' },
        { label: 'Sức chứa', key: 'sucChua' },
        { label: 'Tiện nghi', key: '_tienNghi', render: (v, item) => renderServiceBadges(item, 'TienNghi') },
        { label: 'Trạng thái', key: 'maTrangThai', render: v => {
            const t = _pLookups().trangthai?.find(t => t.maTrangThai === v);
            const cls = v === 1 ? 'badge-success' : v === 2 ? 'badge-danger' : 'badge-warning';
            return `<span class="badge ${cls}">${t?.tenTrangThai || v}</span>`;
        }}
    ],
    fields: [
        { id: 'tenPhong', label: 'Tên phòng', type: 'text', required: true },
        { id: 'maNhaTro', label: 'Nhà trọ', type: 'lookup', lookup: 'nhatro', valField: 'maNhaTro', txtField: 'tenNhaTro', required: true },
        { id: 'maLoaiPhong', label: 'Loại phòng', type: 'lookup', lookup: 'loaiphong', valField: 'maLoaiPhong', txtField: 'tenLoaiPhong', required: true },
        { id: 'maTrangThai', label: 'Trạng thái', type: 'lookup', lookup: 'trangthai', valField: 'maTrangThai', txtField: 'tenTrangThai', required: true },
        { id: 'giaPhong', label: 'Giá thuê (đ)', type: 'number', required: true },
        { id: 'dienTich', label: 'Diện tích (m²)', type: 'number' },
        { id: 'sucChua', label: 'Sức chứa (người)', type: 'number', required: true },
        { id: 'fileUpload', label: 'Ảnh phòng', type: 'fileMultiple', uploadTarget: 'phong' },
        { id: 'danhSachHinhAnh', label: 'URL ảnh đã lưu', type: 'hiddenJsonArray' },
        { id: 'dichVuGanPhong', label: 'Tiện ích / tiện nghi gắn với phòng', type: 'serviceCheckboxes' },
        { id: 'diaChiPhong', label: 'Địa chỉ phòng', type: 'text' },
        { id: 'moTa', label: 'Mô tả', type: 'textarea' }
    ]
};
