// Module cấu hình: nguoithue
window.AppModules = window.AppModules || {};
const _ktLookups = () => window.lookups || { phong: [] };
window.AppModules.nguoithue = {
    title: 'Khách Thuê',
    endpoint: '/api/NguoiThue',
    pk: 'maNguoiThue',
    headers: [
        { label: 'Họ tên', key: 'hoTen' },
        { label: 'CCCD', key: 'cccd' },
        { label: 'Số điện thoại', key: 'sdt' },
        { label: 'Email', key: 'email' },
        { label: 'Phòng đang thuê', key: 'danhSachPhongText', render: (v, row) => v || (_ktLookups().phong?.find(p => p.maPhong === row.maPhong)?.tenPhong || `#${row.maPhong}`) },
        { label: 'Số phòng', key: 'soPhongDangThue', render: v => v || 1 },
        { label: 'Ảnh CCCD', key: 'anhCccdMatTruoc', render: (v, row) => {
            const hasFront = !!row.anhCccdMatTruoc;
            const hasBack = !!row.anhCccdMatSau;
            if (!hasFront && !hasBack) return '<span style="color:var(--text-light);">Chưa có</span>';
            return `<span class="badge badge-success">${hasFront ? 'Mặt trước' : ''}${hasFront && hasBack ? ' / ' : ''}${hasBack ? 'Mặt sau' : ''}</span>`;
        }},
        { label: 'Giới tính', key: 'gioiTinh' },
        { label: 'Ngày sinh', key: 'ngaySinh', render: v => window.AppFormat.date(v) }
    ],
    fields: [
        { id: 'hoTen', label: 'Họ tên', type: 'text', required: true },
        { id: 'maPhong', label: 'Phòng', type: 'lookup', lookup: 'phong', valField: 'maPhong', txtField: 'tenPhong', required: true },
        { id: 'cccd', label: 'CCCD/CMND', type: 'text' },
        { id: 'anhCccdMatTruoc', label: 'Ảnh CCCD mặt trước', type: 'file' },
        { id: 'anhCccdMatSau', label: 'Ảnh CCCD mặt sau', type: 'file' },
        { id: 'sdt', label: 'Số điện thoại', type: 'text' },
        { id: 'email', label: 'Email', type: 'email' },
        { id: 'ngaySinh', label: 'Ngày sinh', type: 'date' },
        { id: 'gioiTinh', label: 'Giới tính', type: 'options', options: ['Nam', 'Nữ', 'Khác'] },
        { id: 'diaChi', label: 'Địa chỉ', type: 'text' },
        { id: 'quocTich', label: 'Quốc tịch', type: 'text', defaultVal: 'Việt Nam' },
        { id: 'noiCongTac', label: 'Nơi công tác', type: 'text' }
    ]
};
