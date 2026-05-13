// Module cấu hình: user
window.AppModules = window.AppModules || {};
window.AppModules.user = {
        title: 'Người Dùng',
        endpoint: '/api/User',
        pk: 'maNguoiDung',
        customModal: true,
        headers: [
            { label: 'Tên đăng nhập', key: 'tenDangNhap' },
            { label: 'Họ tên', key: 'hoTen' },
            { label: 'Email', key: 'email' },
            {
                label: 'Vai trò', key: 'vaiTro', render: v => {
                    const cls = v === 'Admin' ? 'badge-danger' : v === 'ChuTro' ? 'badge-warning' : 'badge-info';
                    return `<span class="badge ${cls}">${v}</span>`;
                }
            },
            { label: 'SĐT', key: 'soDienThoai' },
            { label: 'Trạng thái', key: 'trangThai', render: v => v ? '<span class="badge badge-success">Hoạt động</span>' : '<span class="badge badge-secondary">Khóa</span>' }
        ]
    };
