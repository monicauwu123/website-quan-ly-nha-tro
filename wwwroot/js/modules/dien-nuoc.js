// Module cấu hình: điện & nước
window.AppDienNuocModules = window.AppDienNuocModules || {};
const _dnLookups = () => window.lookups || { phong: [] };
window.AppDienNuocModules.dien = {
    title: 'Chỉ Số Điện',
    endpoint: '/api/ChiSoDien',
    pk: 'maDien',
    headers: [
        { label: 'Phòng', key: 'maPhong', render: v => _dnLookups().phong?.find(p => p.maPhong === v)?.tenPhong || `#${v}` },
        { label: 'Chỉ số cũ', key: 'soDienCu', render: v => `${v} kWh` },
        { label: 'Chỉ số mới', key: 'soDienMoi', render: v => `${v} kWh` },
        { label: 'Tiêu thụ', key: '_tieuThu', render: (_, row) => `${(row.soDienMoi || 0) - (row.soDienCu || 0)} kWh` },
        { label: 'Giá điện/kWh', key: 'giaDien', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền điện', key: 'tienDien', render: v => `<strong>${window.AppFormat.currency(v)}</strong>` },
        { label: 'Ngày ghi', key: 'ngayThangDien', render: v => window.AppFormat.date(v) }
    ],
    fields: [
        { id: 'maPhong', label: 'Phòng', type: 'lookup', lookup: 'phongDienNuoc', valField: 'maPhong', txtField: 'tenPhong', required: true },
        { id: 'soDienCu', label: 'Chỉ số cũ (kWh)', type: 'number', required: true },
        { id: 'soDienMoi', label: 'Chỉ số mới (kWh)', type: 'number', required: true },
        { id: 'giaDien', label: 'Giá điện (đ/kWh)', type: 'number', required: true, defaultVal: 3500 },
        { id: 'ngayThangDien', label: 'Ngày ghi', type: 'date', required: true }
    ]
};
window.AppDienNuocModules.nuoc = {
    title: 'Chỉ Số Nước',
    endpoint: '/api/ChiSoNuoc',
    pk: 'maNuoc',
    headers: [
        { label: 'Phòng', key: 'maPhong', render: v => _dnLookups().phong?.find(p => p.maPhong === v)?.tenPhong || `#${v}` },
        { label: 'Chỉ số cũ', key: 'soNuocCu', render: v => `${v} m³` },
        { label: 'Chỉ số mới', key: 'soNuocMoi', render: v => `${v} m³` },
        { label: 'Tiêu thụ', key: '_tieuThu', render: (_, row) => `${(row.soNuocMoi || 0) - (row.soNuocCu || 0)} m³` },
        { label: 'Giá nước/m³', key: 'giaNuoc', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền nước', key: 'tienNuoc', render: v => `<strong>${window.AppFormat.currency(v)}</strong>` },
        { label: 'Ngày ghi', key: 'ngayThangNuoc', render: v => window.AppFormat.date(v) }
    ],
    fields: [
        { id: 'maPhong', label: 'Phòng', type: 'lookup', lookup: 'phongDienNuoc', valField: 'maPhong', txtField: 'tenPhong', required: true },
        { id: 'soNuocCu', label: 'Chỉ số cũ (m³)', type: 'number', required: true },
        { id: 'soNuocMoi', label: 'Chỉ số mới (m³)', type: 'number', required: true },
        { id: 'giaNuoc', label: 'Giá nước (đ/m³)', type: 'number', required: true, defaultVal: 20000 },
        { id: 'ngayThangNuoc', label: 'Ngày ghi', type: 'date', required: true }
    ]
};
