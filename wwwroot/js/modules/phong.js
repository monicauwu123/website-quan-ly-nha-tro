// Module cấu hình: phong
window.AppModules = window.AppModules || {};
window.AppModules.phong = {
        title: 'Phòng Trọ',
        endpoint: '/api/Phong',
        pk: 'maPhong',
        headers: [
            { label: 'Tên phòng', key: 'tenPhong' },
            { label: 'Nhà trọ', key: 'maNhaTro', render: v => lookups.nhatro.find(n => n.maNhaTro === v)?.tenNhaTro || `#${v}` },
            { label: 'Loại phòng', key: 'maLoaiPhong', render: v => lookups.loaiphong.find(l => l.maLoaiPhong === v)?.tenLoaiPhong || `#${v}` },
            { label: 'Giá thuê', key: 'giaPhong', render: fmtCurrency },
            { label: 'Diện tích', key: 'dienTich', render: v => v ? `${v} m²` : '---' },
            { label: 'Sức chứa', key: 'sucChua' },
            {
                label: 'Trạng thái', key: 'maTrangThai', render: v => {
                    const t = lookups.trangthai.find(t => t.maTrangThai === v);
                    const cls = v === 1 ? 'badge-success' : v === 2 ? 'badge-danger' : 'badge-warning';
                    return `<span class="badge ${cls}">${t?.tenTrangThai || v}</span>`;
                }
            }
        ],
        fields: [
            { id: 'tenPhong', label: 'Tên phòng', type: 'text', required: true },
            { id: 'maNhaTro', label: 'Nhà trọ', type: 'lookup', lookup: 'nhatro', valField: 'maNhaTro', txtField: 'tenNhaTro', required: true },
            { id: 'maLoaiPhong', label: 'Loại phòng', type: 'lookup', lookup: 'loaiphong', valField: 'maLoaiPhong', txtField: 'tenLoaiPhong', required: true },
            { id: 'maTrangThai', label: 'Trạng thái', type: 'lookup', lookup: 'trangthai', valField: 'maTrangThai', txtField: 'tenTrangThai', required: true },
            { id: 'giaPhong', label: 'Giá thuê (đ)', type: 'number', required: true },
            { id: 'dienTich', label: 'Diện tích (m²)', type: 'number' },
            { id: 'sucChua', label: 'Sức chứa (người)', type: 'number', required: true },
            { id: 'fileUpload', label: 'Tải ảnh mới', type: 'file' },
            { id: 'soNguoiHienTai', label: 'Số người hiện tại', type: 'number', defaultVal: 0, hidden: true },

            { id: 'diaChiPhong', label: 'Địa chỉ phòng', type: 'text' },
            { id: 'moTa', label: 'Mô tả', type: 'textarea' }
        ]
    };
