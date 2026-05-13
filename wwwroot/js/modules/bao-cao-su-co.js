// Module cấu hình: baocaosuco
window.AppModules = window.AppModules || {};
window.AppModules.baocaosuco = {
    title: 'Báo Cáo Sự Cố',
    endpoint: '/api/BaoCaoSuCo',
    pk: 'maBaoCao',
    customModal: true,
    filters: [
        {
            id: 'mucDo',
            label: 'Mức độ',
            getValue: row => row.mucDo || '',
            getLabel: value => value || 'Bình thường'
        },
        {
            id: 'trangThai',
            label: 'Trạng thái',
            getValue: row => row.trangThai || '',
            getLabel: value => ({
                Moi: 'Mới gửi',
                DangXuLy: 'Đang xử lý',
                DaXuLy: 'Đã xử lý',
                Huy: 'Đã hủy'
            }[value] || value || '---')
        }
    ],
    headers: [
        { label: 'Tiêu đề', key: 'tieuDe' },
        { label: 'Người gửi', key: 'nguoiDung', render: v => v?.hoTen || v?.email || '---' },
        { label: 'Phòng', key: 'phong', render: v => v?.tenPhong || '---' },
        { label: 'Nhà trọ', key: 'phong', render: v => v?.nhaTro?.tenNhaTro || '---' },
        { label: 'Mức độ', key: 'mucDo', render: v => {
            const cls = v === 'Rất gấp' ? 'badge-danger' : v === 'Gấp' ? 'badge-warning' : 'badge-info';
            return `<span class="badge ${cls}">${v || 'Bình thường'}</span>`;
        }},
        { label: 'Trạng thái', key: 'trangThaiText', render: (v, row) => {
            const st = row.trangThai;
            const cls = st === 'DaXuLy' ? 'badge-success' : st === 'DangXuLy' ? 'badge-warning' : st === 'Huy' ? 'badge-secondary' : 'badge-info';
            return `<span class="badge ${cls}">${v || st || '---'}</span>`;
        }},
        { label: 'Ngày gửi', key: 'ngayGui', render: v => window.AppFormat.date(v) },
        { label: 'Phản hồi', key: 'phanHoiChuTro' }
    ]
};
