// ==========================================
// DASHBOARD.JS – Quản Lý Phòng Trọ PRO
// ==========================================

// --- AUTH CHECK ---
const token = localStorage.getItem('token');
if (!token) window.location.href = '/index.html';

const user = JSON.parse(localStorage.getItem('user') || '{}');
if (user.hoTen) {
    document.getElementById('userName').textContent  = user.hoTen;
    document.getElementById('userRole').textContent  = user.vaiTro || '';
    document.getElementById('userAvatar').textContent = (user.hoTen || 'A').charAt(0).toUpperCase();
}

// ==========================================
// ROLE-BASED UI  – ẩn/hiện menu theo role
// ==========================================
const CURRENT_ROLE = (user.vaiTro || '').trim(); // 'Admin' | 'ChuTro' | 'NguoiDung'

function applyRoleUI() {
    // Ẩn mọi nav-link không thuộc role hiện tại
    document.querySelectorAll('.sidebar-nav .nav-link[data-role]').forEach(link => {
        const allowed = link.getAttribute('data-role').split(',').map(r => r.trim());
        link.style.display = (CURRENT_ROLE && allowed.includes(CURRENT_ROLE)) ? '' : 'none';
    });

    // Ẩn nút "Thêm mới" với NguoiDung (chỉ xem)
    if (CURRENT_ROLE === 'NguoiDung') {
        const addBtn = document.getElementById('addBtn');
        if (addBtn) addBtn.style.display = 'none';
    }
}

// Chạy ngay khi DOM sẵn sàng
applyRoleUI();

// --- STATE ---
let currentSection = 'overview';
let currentSubSection = 'dien';
let currentData = [];
let lookups = { nhatro: [], loaiphong: [], trangthai: [], phong: [], nguoithue: [] };

// --- FORMATTERS ---
const fmtCurrency = v => (v != null && v !== '') ? new Intl.NumberFormat('vi-VN').format(v) + 'đ' : '---';
const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN') : '---';

// ==========================================
// API WRAPPER
// ==========================================
async function apiFetch(endpoint, method = 'GET', body = null) {
    const opts = {
        method,
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }
    };
    if (body) opts.body = JSON.stringify(body);
    try {
        const res = await fetch(endpoint, opts);
        if (res.status === 401) { logout(); return null; }
        if (method === 'DELETE' || res.status === 204) return true;
        const text = await res.text();
        if (!text) return true;
        let json;
        try { json = JSON.parse(text); } catch { return true; }
        if (!res.ok) {
            const msg = extractApiErrorMessage(json) || `Lỗi HTTP ${res.status}`;
            throw new Error(msg);
        }
        // Chuẩn hoá response kiểu ApiResponse<T>: { thanhCong, thongBao, duLieu }
        // để frontend luôn nhận trực tiếp mảng/object dữ liệu.
        if (json && typeof json === 'object' && Object.prototype.hasOwnProperty.call(json, 'duLieu')) {
            return json.duLieu;
        }
        return json;
    } catch (e) {
        throw e;
    }
}


function extractApiErrorMessage(json) {
    if (!json) return '';
    if (typeof json === 'string') return json;
    if (json.thongBao) return json.thongBao;
    if (json.message) return json.message;

    if (json.errors) {
        const errors = Object.values(json.errors).flat().filter(Boolean);
        if (errors.length > 0) return errors.join('; ');
    }

    if (json.title && json.title !== 'One or more validation errors occurred.') {
        return json.title;
    }

    return '';
}

// ==========================================
// TOAST NOTIFICATIONS
// ==========================================
function showToast(msg, type = 'success') {
    const icon = type === 'success' ? 'check-circle' : type === 'error' ? 'exclamation-circle' : 'info-circle';
    const t = document.createElement('div');
    t.className = `toast toast-${type}`;
    t.innerHTML = `<i class="fas fa-${icon}"></i> ${msg}`;
    document.getElementById('toastContainer').appendChild(t);
    setTimeout(() => t.classList.add('show'), 10);
    setTimeout(() => { t.classList.remove('show'); setTimeout(() => t.remove(), 350); }, 3500);
}

// ==========================================
// LOOKUP DATA
// ==========================================
async function loadLookups() {
    const results = await Promise.allSettled([
        apiFetch('/api/NhaTro'),
        apiFetch('/api/LoaiPhong'),
        apiFetch('/api/TrangThai'),
        apiFetch('/api/Phong'),
        apiFetch('/api/NguoiThue'),
    ]);
    lookups.nhatro = results[0].value || [];
    lookups.loaiphong = results[1].value || [];
    lookups.trangthai = results[2].value || [];
    if (lookups.trangthai.length === 0) {
        console.warn('Không tải được danh sách trạng thái từ API /api/TrangThai');
    }
    lookups.phong = results[3].value || [];
    lookups.nguoithue = results[4].value || [];

    // Bổ sung lookups cho Thanh toán
    try {
        const hd = await apiFetch('/api/HoaDon');
        lookups.hoadon = hd || [];
    } catch (e) { console.warn('Load hoadon lookup failed'); }

    lookups.hinhthuc = [
        { val: 'Tiền mặt', label: 'Tiền mặt' },
        { val: 'Chuyển khoản', label: 'Chuyển khoản' }
    ];
}

// ==========================================
// MODULE CONFIGS
// ==========================================
const modules = {
    nhatro: {
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
    },

    loaiphong: {
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
    },

    phong: {
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
    },

    nguoithue: {
        title: 'Khách Thuê',
        endpoint: '/api/NguoiThue',
        pk: 'maNguoiThue',
        headers: [
            { label: 'Họ tên', key: 'hoTen' },
            { label: 'CCCD', key: 'cccd' },
            { label: 'Số điện thoại', key: 'sdt' },
            { label: 'Email', key: 'email' },
            { label: 'Giới tính', key: 'gioiTinh' },
            { label: 'Ngày sinh', key: 'ngaySinh', render: fmtDate }
        ],
        fields: [
            { id: 'hoTen', label: 'Họ tên', type: 'text', required: true },
            { id: 'cccd', label: 'CCCD/CMND', type: 'text' },
            { id: 'sdt', label: 'Số điện thoại', type: 'text' },
            { id: 'email', label: 'Email', type: 'email' },
            { id: 'ngaySinh', label: 'Ngày sinh', type: 'date' },
            { id: 'gioiTinh', label: 'Giới tính', type: 'options', options: ['Nam', 'Nữ', 'Khác'] },
            { id: 'diaChi', label: 'Địa chỉ', type: 'text' },
            { id: 'quocTich', label: 'Quốc tịch', type: 'text', defaultVal: 'Việt Nam' },
            { id: 'noiCongTac', label: 'Nơi công tác', type: 'text' }
        ]
    },

    hopdong: {
        title: 'Hợp Đồng',
        endpoint: '/api/HopDong',
        pk: 'maHopDong',
        customModal: true,
        headers: [
            { label: 'Phòng', key: 'phong', render: (v, row) => v?.tenPhong || `Phòng #${row.maPhong}` },
            { label: 'Khách thuê', key: 'nguoiThue', render: (v, row) => v?.hoTen || `Khách #${row.maNguoiThue}` },
            { label: 'Ngày bắt đầu', key: 'ngayBatDau', render: fmtDate },
            { label: 'Ngày kết thúc', key: 'ngayKetThuc', render: v => v ? fmtDate(v) : 'Không xác định' },
            { label: 'Tiền cọc', key: 'tienCoc', render: fmtCurrency },
            {
                label: 'Trạng thái', key: 'trangThaiText', render: v => {
                    const cls = v === 'Đang còn hiệu lực' ? 'badge-success' : v === 'Sắp hết hợp đồng' ? 'badge-warning' : 'badge-danger';
                    return `<span class="badge ${cls}">${v || '---'}</span>`;
                }
            }
        ]
    },

    hoadon: {
        title: 'Hóa Đơn',
        endpoint: '/api/HoaDon',
        pk: 'maHoaDon',
        customModal: true,
        headers: [
            { label: 'Phòng', key: 'tenPhong' },
            { label: 'Khách thuê', key: 'tenNguoiThue' },
            { label: 'Kỳ hóa đơn', key: 'kyHoaDon' },
            { label: 'Tiền phòng', key: 'tienPhong', render: fmtCurrency },
            { label: 'Tiền điện', key: 'tienDien', render: fmtCurrency },
            { label: 'Tiền nước', key: 'tienNuoc', render: fmtCurrency },
            { label: 'Phát sinh khác', key: 'tienPhatSinhKhac', render: fmtCurrency },
            { label: 'Tổng tiền', key: 'tongTien', render: v => `<strong style="color:var(--primary)">${fmtCurrency(v)}</strong>` },
            { label: 'Ngày lập', key: 'ngayLap', render: fmtDate },
            { label: 'In', key: 'maHoaDon', render: v => `<button class="btn" style="padding:0.2rem 0.5rem; background:#6366f1; color:white;" onclick="API.hoadon.exportPdf(${v})"><i class="fas fa-file-pdf"></i> PDF</button>` }
        ]
    },

    thanhtoan: {
        title: 'Thanh Toán',
        endpoint: '/api/ThanhToan',
        pk: 'maThanhToan',
        headers: [
            { label: 'Hóa đơn', key: 'maHoaDon', render: v => `HĐ#${v}` },
            { label: 'Khách thuê', key: 'maNguoiThue', render: v => lookups.nguoithue.find(n => n.maNguoiThue === v)?.hoTen || `#${v}` },
            { label: 'Ngày thanh toán', key: 'ngayThanhToan', render: fmtDate },
            { label: 'Số tiền', key: 'tongTien', render: fmtCurrency },
            { label: 'Hình thức', key: 'hinhThucThanhToan' },
            { label: 'Ghi chú', key: 'ghiChu' }
        ],
        fields: [
            { id: 'MaHoaDon', label: 'Hóa đơn', type: 'lookup', lookup: 'hoadon', valField: 'maHoaDon', txtField: 'maHoaDon', required: true },
            { id: 'MaNguoiThue', label: 'Khách thuê', type: 'lookup', lookup: 'nguoithue', valField: 'maNguoiThue', txtField: 'hoTen', required: true },
            { id: 'TongTien', label: 'Số tiền thanh toán (đ)', type: 'number', required: true },
            { id: 'HinhThucThanhToan', label: 'Hình thức thanh toán', type: 'lookup', lookup: 'hinhthuc', valField: 'val', txtField: 'label', required: true },
            { id: 'GhiChu', label: 'Ghi chú', type: 'text' }
        ]
    },

    dichvu: {
        title: 'Dịch Vụ',
        endpoint: '/api/DichVu',
        pk: 'maDichVu',
        headers: [
            { label: 'Tên dịch vụ', key: 'tenDichVu' },
            { label: 'Đơn giá', key: 'tiendichvu', render: fmtCurrency }
        ],
        fields: [
            { id: 'tenDichVu', label: 'Tên dịch vụ', type: 'text', required: true },
            { id: 'tiendichvu', label: 'Đơn giá (đ)', type: 'number', required: true }
        ]
    },

    user: {
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
    }
};

// --- Điện & Nước modules ---
const dienModule = {
    title: 'Chỉ Số Điện',
    endpoint: '/api/ChiSoDien',
    pk: 'maDien',
    headers: [
        { label: 'Phòng', key: 'maPhong', render: v => lookups.phong.find(p => p.maPhong === v)?.tenPhong || `#${v}` },
        { label: 'Chỉ số cũ', key: 'soDienCu', render: v => `${v} kWh` },
        { label: 'Chỉ số mới', key: 'soDienMoi', render: v => `${v} kWh` },
        { label: 'Tiêu thụ', key: null, render: (_, row) => `${(row.soDienMoi || 0) - (row.soDienCu || 0)} kWh` },
        { label: 'Giá điện/kWh', key: 'giaDien', render: fmtCurrency },
        { label: 'Tiền điện', key: 'tienDien', render: v => `<strong>${fmtCurrency(v)}</strong>` },
        { label: 'Ngày ghi', key: 'ngayThangDien', render: fmtDate }
    ],
    fields: [
        { id: 'maPhong', label: 'Phòng', type: 'lookup', lookup: 'phong', valField: 'maPhong', txtField: 'tenPhong', required: true },
        { id: 'soDienCu', label: 'Chỉ số cũ (kWh)', type: 'number', required: true },
        { id: 'soDienMoi', label: 'Chỉ số mới (kWh)', type: 'number', required: true },
        { id: 'giaDien', label: 'Giá điện (đ/kWh)', type: 'number', required: true, defaultVal: 3500 },
        { id: 'ngayThangDien', label: 'Ngày ghi', type: 'date', required: true }
    ]
};

const nuocModule = {
    title: 'Chỉ Số Nước',
    endpoint: '/api/ChiSoNuoc',
    pk: 'maNuoc',
    headers: [
        { label: 'Phòng', key: 'maPhong', render: v => lookups.phong.find(p => p.maPhong === v)?.tenPhong || `#${v}` },
        { label: 'Chỉ số cũ', key: 'soNuocCu', render: v => `${v} m³` },
        { label: 'Chỉ số mới', key: 'soNuocMoi', render: v => `${v} m³` },
        { label: 'Tiêu thụ', key: null, render: (_, row) => `${(row.soNuocMoi || 0) - (row.soNuocCu || 0)} m³` },
        { label: 'Giá nước/m³', key: 'giaNuoc', render: fmtCurrency },
        { label: 'Tiền nước', key: 'tienNuoc', render: v => `<strong>${fmtCurrency(v)}</strong>` },
        { label: 'Ngày ghi', key: 'ngayThangNuoc', render: fmtDate }
    ],
    fields: [
        { id: 'maPhong', label: 'Phòng', type: 'lookup', lookup: 'phong', valField: 'maPhong', txtField: 'tenPhong', required: true },
        { id: 'soNuocCu', label: 'Chỉ số cũ (m³)', type: 'number', required: true },
        { id: 'soNuocMoi', label: 'Chỉ số mới (m³)', type: 'number', required: true },
        { id: 'giaNuoc', label: 'Giá nước (đ/m³)', type: 'number', required: true, defaultVal: 20000 },
        { id: 'ngayThangNuoc', label: 'Ngày ghi', type: 'date', required: true }
    ]
};

// ==========================================
// SECTION NAVIGATION
// ==========================================
function showSection(section, el) {
    currentSection = section;
    document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
    if (el) el.classList.add('active');

    const addBtn = document.getElementById('addBtn');
    const sectionTitle = document.getElementById('sectionTitle');

    // NguoiDung chỉ xem, không được tạo/sửa/xóa
    const canCreate = (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro')
    || (CURRENT_ROLE === 'NguoiDung' && section === 'hopdong');
    const canWrite = canCreate;

    if (section === 'overview') {
        addBtn.style.display = 'none';
        sectionTitle.textContent = 'Tổng quan hệ thống';
    } else if (section === 'diennuoc') {
        addBtn.style.display = canWrite ? 'inline-flex' : 'none';
        sectionTitle.textContent = 'Điện & Nước';
        addBtn.onclick = () => openDienNuocModal();
    } else {
        addBtn.style.display = canWrite ? 'inline-flex' : 'none';
        sectionTitle.textContent = modules[section]?.title || section;
        addBtn.onclick = () => openModal();
    }

    document.getElementById('overviewSection').style.display = section === 'overview' ? 'block' : 'none';
    document.getElementById('genericSection').style.display = section !== 'overview' ? 'block' : 'none';

    if (section === 'overview') loadOverview();
    else if (section === 'phong') renderRoomGrid();
    else if (section === 'diennuoc') renderDienNuocSection();
    else loadGenericSection(section);
}

// ==========================================
// OVERVIEW
// ==========================================
async function loadOverview() {
    try {
        const data = await apiFetch('/api/Dashboard/overview');

        if (CURRENT_ROLE === 'NguoiDung') {
            renderNguoiDungOverview(data);
        } else {
            renderChuTroAdminOverview(data);
        }
    } catch (e) {
        console.error('Overview error:', e);
        showToast('Lỗi tải dữ liệu tổng quan', 'error');
    }
}

function renderChuTroAdminOverview(data) {
    document.getElementById('sectionTitle').textContent = CURRENT_ROLE === 'ChuTro' ? 'Tổng quan nhà trọ của tôi' : 'Tổng quan hệ thống';

    const statsGrid = document.querySelector('#overviewSection .stats-grid');
    statsGrid.innerHTML = `
        <div class="stat-card stat-card-indigo">
            <div class="stat-icon"><i class="fas fa-building"></i></div>
            <div class="stat-info"><h3>Tổng số nhà trọ</h3><div id="statTotalNhaTro" class="value">${data?.tongNhaTro ?? 0}</div></div>
        </div>
        <div class="stat-card stat-card-blue">
            <div class="stat-icon"><i class="fas fa-door-open"></i></div>
            <div class="stat-info"><h3>Tổng số phòng</h3><div id="statTotalRooms" class="value">${data?.tongPhong ?? 0}</div></div>
        </div>
        <div class="stat-card stat-card-red">
            <div class="stat-icon"><i class="fas fa-user-check"></i></div>
            <div class="stat-info"><h3>Đang thuê</h3><div id="statOccupiedRooms" class="value">${data?.phongDangThue ?? 0}</div></div>
        </div>
        <div class="stat-card stat-card-green">
            <div class="stat-icon"><i class="fas fa-door-open"></i></div>
            <div class="stat-info"><h3>Phòng trống</h3><div id="statEmptyRooms" class="value">${data?.phongTrong ?? 0}</div></div>
        </div>
        <div class="stat-card stat-card-purple">
            <div class="stat-icon"><i class="fas fa-users"></i></div>
            <div class="stat-info"><h3>Khách thuê</h3><div id="statTotalTenants" class="value">${data?.tongKhachThue ?? 0}</div></div>
        </div>
        <div class="stat-card stat-card-purple">
            <div class="stat-icon"><i class="fas fa-coins"></i></div>
            <div class="stat-info"><h3>Doanh thu tháng</h3><div id="statRevenue" class="value">${fmtCurrency(data?.doanhThuThang ?? 0)}</div></div>
        </div>
    `;

    const rooms = data?.danhSachPhongGanDay || [];
    const tbody = document.querySelector('#recentRoomsTable tbody');
    const title = document.querySelector('#overviewSection .data-card h2');
    if (title) title.textContent = 'Danh sách phòng gần đây';

    if (!rooms.length) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:2rem;color:var(--text-light);">Chưa có phòng nào</td></tr>';
        return;
    }

    tbody.innerHTML = rooms.map(r => {
        const cls = r.maTrangThai === 1 ? 'badge-success' : r.maTrangThai === 2 ? 'badge-danger' : 'badge-warning';
        return `<tr>
            <td><strong>${r.tenPhong || '---'}</strong></td>
            <td>${r.tenNhaTro || '---'}</td>
            <td>${fmtCurrency(r.giaPhong)}</td>
            <td><span class="badge ${cls}">${r.trangThai || '---'}</span></td>
            <td>
                ${(CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') ? `
                <button class="btn-action btn-edit" onclick="editItem('phong',${r.maPhong})"><i class="fas fa-edit"></i> Sửa</button>
                <button class="btn-action btn-delete" onclick="deleteItem('phong',${r.maPhong})"><i class="fas fa-trash"></i> Xóa</button>
                ` : ''}
            </td>
        </tr>`;
    }).join('');
}

function renderNguoiDungOverview(data) {
    document.getElementById('sectionTitle').textContent = 'Tổng quan của tôi';

    const tk = data?.taiKhoan || {};
    const phong = data?.phongDangThue;
    const hd = data?.hopDongHienTai;
    const hoaDon = data?.hoaDonThangNay;
    const daThanhToan = hoaDon?.daThanhToan ?? 0;
    const tongTien = hoaDon?.tongTien ?? 0;
    const conLai = Math.max(tongTien - daThanhToan, 0);
    const trangThai = !hoaDon ? 'Chưa có hóa đơn tháng này' : (conLai <= 0 ? 'Đã trả' : 'Chưa trả');
    const badgeClass = !hoaDon ? 'badge-warning' : (conLai <= 0 ? 'badge-success' : 'badge-danger');

    const statsGrid = document.querySelector('#overviewSection .stats-grid');
    statsGrid.innerHTML = `
        <div class="stat-card stat-card-indigo">
            <div class="stat-icon"><i class="fas fa-user"></i></div>
            <div class="stat-info"><h3>Thông tin tài khoản</h3><div class="value" style="font-size:1rem;line-height:1.45">${tk.hoTen || '---'}<br><small>${tk.email || '---'}</small><br><small>${tk.soDienThoai || '---'}</small></div></div>
        </div>
        <div class="stat-card stat-card-blue">
            <div class="stat-icon"><i class="fas fa-home"></i></div>
            <div class="stat-info"><h3>Phòng đang thuê</h3><div class="value" style="font-size:1.25rem">${phong?.tenPhong || 'Chưa có phòng'}</div><small>${phong?.tenNhaTro || ''}</small></div>
        </div>
        <div class="stat-card stat-card-green">
            <div class="stat-icon"><i class="fas fa-file-contract"></i></div>
            <div class="stat-info"><h3>Hợp đồng hiện tại</h3><div class="value" style="font-size:1rem;line-height:1.45">${hd ? `${fmtDate(hd.ngayBatDau)}<br>đến ${fmtDate(hd.ngayKetThuc)}` : 'Chưa có hợp đồng'}</div></div>
        </div>
        <div class="stat-card stat-card-purple">
            <div class="stat-icon"><i class="fas fa-coins"></i></div>
            <div class="stat-info"><h3>Tổng tiền tháng này</h3><div class="value">${hoaDon ? fmtCurrency(tongTien) : '---'}</div></div>
        </div>
        <div class="stat-card stat-card-red">
            <div class="stat-icon"><i class="fas fa-exclamation-triangle"></i></div>
            <div class="stat-info"><h3>Trạng thái thanh toán</h3><div class="value" style="font-size:1.1rem"><span class="badge ${badgeClass}">${trangThai}</span></div>${hoaDon ? `<small>Còn lại: ${fmtCurrency(conLai)}</small>` : ''}</div>
        </div>
    `;

    const title = document.querySelector('#overviewSection .data-card h2');
    if (title) title.textContent = 'Chi tiết hóa đơn tháng này';

    const tbody = document.querySelector('#recentRoomsTable tbody');
    if (!hoaDon) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:2rem;color:var(--text-light);">Chưa có hóa đơn tháng này</td></tr>';
        return;
    }

    tbody.innerHTML = `<tr>
        <td><strong>${hoaDon.kyHoaDon || '---'}</strong></td>
        <td>${phong?.tenPhong || '---'}</td>
        <td>${fmtCurrency(hoaDon.tongTien)}</td>
        <td><span class="badge ${badgeClass}">${trangThai}</span></td>
        <td>Đã trả: ${fmtCurrency(daThanhToan)}</td>
    </tr>`;
}


// ==========================================
// ROOM GRID
// ==========================================
async function renderRoomGrid() {
    const _addBtn = document.getElementById('addBtn');
    if (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') {
        _addBtn.style.display = 'inline-flex';
        _addBtn.onclick = () => openModal();
    } else {
        _addBtn.style.display = 'none';
    }
    const container = document.getElementById('genericSection');
    container.innerHTML = `
        <div style="display:flex;gap:1rem;margin-bottom:1.5rem;flex-wrap:wrap;align-items:center;">
            <div style="flex:1;min-width:200px;position:relative;">
                <i class="fas fa-search" style="position:absolute;left:1rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
                <input type="text" id="roomSearch" class="form-control" style="padding-left:2.5rem;" placeholder="Tìm kiếm tên phòng..." oninput="filterRooms()">
            </div>
            <select id="roomStatusFilter" class="form-control" style="width:auto;min-width:160px;" onchange="filterRooms()">
                <option value="">Tất cả trạng thái</option>
                ${lookups.trangthai.map(t => `<option value="${t.maTrangThai}">${t.tenTrangThai}</option>`).join('')}
            </select>
        </div>
        <div id="roomGrid" style="display:grid;grid-template-columns:repeat(auto-fill,minmax(280px,1fr));gap:1.25rem;"></div>`;

    try {
        currentData = await apiFetch('/api/Phong');
        renderRooms(currentData);
    } catch (e) {
        showToast('Lỗi tải danh sách phòng', 'error');
    }
}

function renderRooms(rooms) {
    const grid = document.getElementById('roomGrid');
    if (!grid) return;
    if (!rooms?.length) {
        grid.innerHTML = '<p style="color:var(--text-light);text-align:center;padding:2rem;grid-column:1/-1;">Không có phòng nào phù hợp.</p>';
        return;
    }
    grid.innerHTML = rooms.map(r => {
        const status = lookups.trangthai.find(t => t.maTrangThai === r.maTrangThai);
        const house = lookups.nhatro.find(n => n.maNhaTro === r.maNhaTro);
        const loai = lookups.loaiphong.find(l => l.maLoaiPhong === r.maLoaiPhong);
        const color = r.maTrangThai === 1 ? '#22c55e' : r.maTrangThai === 2 ? '#ef4444' : '#f59e0b';
        return `<div class="data-card animate-fade-in" style="border-top:4px solid ${color};padding:1.25rem;display:flex;flex-direction:column;">
            ${r.hinhAnh ? `<img src="${r.hinhAnh}" style="width:100%;height:140px;object-fit:cover;border-radius:0.5rem;margin-bottom:1rem;" onerror="this.style.display='none'">` : ''}
            <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:0.75rem;">
                <h3 style="font-size:1.1rem;font-weight:700;flex:1;">${r.tenPhong}</h3>
                <span class="badge" style="background:${color}20;color:${color};margin-left:0.5rem;white-space:nowrap;">${status?.tenTrangThai || '---'}</span>
            </div>
            <div style="font-size:0.875rem;color:var(--text-light);flex:1;display:flex;flex-direction:column;gap:0.4rem;margin-bottom:1rem;">
                <p><i class="fas fa-building" style="width:1.25rem;color:var(--primary);"></i> ${house?.tenNhaTro || '---'}</p>
                ${loai ? `<p><i class="fas fa-tag" style="width:1.25rem;color:var(--primary);"></i> ${loai.tenLoaiPhong}</p>` : ''}
                ${r.dienTich ? `<p><i class="fas fa-expand-arrows-alt" style="width:1.25rem;color:var(--primary);"></i> ${r.dienTich} m²</p>` : ''}
                <p><i class="fas fa-money-bill-wave" style="width:1.25rem;color:var(--primary);"></i> <strong style="color:var(--text);">${fmtCurrency(r.giaPhong)}</strong>/tháng</p>
                <p><i class="fas fa-users" style="width:1.25rem;color:var(--primary);"></i> ${r.soNguoiHienTai}/${r.sucChua} người</p>
            </div>
            ${(CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') ? `
            <div style="display:flex;gap:0.5rem;">
                <button class="btn btn-primary" style="flex:1;padding:0.5rem;font-size:0.875rem;" onclick="editItem('phong',${r.maPhong})"><i class="fas fa-edit"></i> Chỉnh sửa</button>
                <button class="btn btn-danger" style="padding:0.5rem;" onclick="deleteItem('phong',${r.maPhong})"><i class="fas fa-trash"></i></button>
            </div>
            ` : ''}
        </div>`;
    }).join('');
}

function filterRooms() {
    const q = (document.getElementById('roomSearch')?.value || '').toLowerCase();
    const sf = document.getElementById('roomStatusFilter')?.value;
    let data = currentData;
    if (q) data = data.filter(r => (r.tenPhong || '').toLowerCase().includes(q) || (r.diaChiPhong || '').toLowerCase().includes(q));
    if (sf) data = data.filter(r => r.maTrangThai == sf);
    renderRooms(data);
}

// ==========================================
// GENERIC TABLE
// ==========================================
async function loadGenericSection(section) {
    const cfg = modules[section];
    if (!cfg) return;
    document.getElementById('addBtn').onclick = () => openModal();

    let searchHtml = '';
    if (section === 'nguoithue') {
        searchHtml = `<div style="margin-bottom:1rem;max-width:420px;position:relative;">
            <i class="fas fa-search" style="position:absolute;left:1rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
            <input type="text" id="nguoiThueSearch" class="form-control" style="padding-left:2.5rem;" placeholder="Tìm theo tên, CCCD, SĐT, email..." oninput="searchNguoiThue(this.value)">
        </div>`;
    }

    document.getElementById('genericSection').innerHTML = `
        ${searchHtml}
        <div class="data-card">
            <div class="table-container">
                <table>
                    <thead><tr>${cfg.headers.map(h => `<th>${h.label}</th>`).join('')}<th>Thao tác</th></tr></thead>
                    <tbody id="genericTableBody">
                        <tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</td></tr>
                    </tbody>
                </table>
            </div>
        </div>`;

    try {
        currentData = await apiFetch(cfg.endpoint);
        renderTable(cfg, currentData, section);
    } catch (e) {
        const tbody = document.getElementById('genericTableBody');
        if (tbody) tbody.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;color:var(--error);padding:1.5rem;">Lỗi: ${e.message}</td></tr>`;
        showToast('Lỗi tải dữ liệu', 'error');
    }
}

function renderTable(cfg, data, section) {
    const tbody = document.getElementById('genericTableBody');
    if (!tbody) return;
    if (!data?.length) {
        tbody.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;color:var(--text-light);">Không có dữ liệu</td></tr>`;
        return;
    }
    const canWrite = (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro');

    tbody.innerHTML = data.map(item => `<tr>
        ${cfg.headers.map(h => {
        const val = h.key ? item[h.key] : null;
        const rendered = h.render ? h.render(val, item) : (val != null && val !== '' ? val : '---');
        return `<td>${rendered}</td>`;
    }).join('')}
        <td style="white-space:nowrap;">
            ${canWrite ? `
            <button class="btn-action btn-edit" onclick="editItem('${section}',${item[cfg.pk]})"><i class="fas fa-edit"></i> Sửa</button>
            <button class="btn-action btn-delete" onclick="deleteItem('${section}',${item[cfg.pk]})"><i class="fas fa-trash"></i> Xóa</button>
            ` : ''}
        </td>
    </tr>`).join('');
}

async function searchNguoiThue(q) {
    if (!q) { renderTable(modules.nguoithue, currentData, 'nguoithue'); return; }
    try {
        const results = await apiFetch(`/api/NguoiThue/Search?keyword=${encodeURIComponent(q)}`);
        renderTable(modules.nguoithue, results || [], 'nguoithue');
    } catch {
        const lower = q.toLowerCase();
        const filtered = currentData.filter(n =>
            (n.hoTen || '').toLowerCase().includes(lower) ||
            (n.cccd || '').includes(q) ||
            (n.sdt || '').includes(q) ||
            (n.email || '').toLowerCase().includes(lower)
        );
        renderTable(modules.nguoithue, filtered, 'nguoithue');
    }
}

// ==========================================
// ĐIỆN & NƯỚC SECTION
// ==========================================
function renderDienNuocSection() {
    const container = document.getElementById('genericSection');
    container.innerHTML = `
        <div style="display:flex;gap:0.5rem;margin-bottom:1.5rem;">
            <button id="tabDien" class="tab-btn tab-active" onclick="switchDienNuocTab('dien')"><i class="fas fa-bolt"></i> Chỉ số Điện</button>
            <button id="tabNuoc" class="tab-btn" onclick="switchDienNuocTab('nuoc')"><i class="fas fa-tint"></i> Chỉ số Nước</button>
        </div>
        <div class="data-card">
            <div class="table-container">
                <table>
                    <thead id="dienNuocHead"></thead>
                    <tbody id="dienNuocBody">
                        <tr><td style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i></td></tr>
                    </tbody>
                </table>
            </div>
        </div>`;
    loadDienNuocData('dien');
}

async function switchDienNuocTab(tab) {
    document.getElementById('tabDien').className = 'tab-btn' + (tab === 'dien' ? ' tab-active' : '');
    document.getElementById('tabNuoc').className = 'tab-btn' + (tab === 'nuoc' ? ' tab-active' : '');
    await loadDienNuocData(tab);
}

async function loadDienNuocData(tab) {
    currentSubSection = tab;
    document.getElementById('addBtn').onclick = () => openDienNuocModal();
    const cfg = tab === 'dien' ? dienModule : nuocModule;

    const head = document.getElementById('dienNuocHead');
    const body = document.getElementById('dienNuocBody');
    if (!head || !body) return;

    head.innerHTML = `<tr>${cfg.headers.map(h => `<th>${h.label}</th>`).join('')}<th>Thao tác</th></tr>`;
    body.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</td></tr>`;

    try {
        currentData = await apiFetch(cfg.endpoint);
        if (!currentData?.length) {
            body.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;color:var(--text-light);">Không có dữ liệu</td></tr>`;
            return;
        }
        body.innerHTML = currentData.map(item => `<tr>
            ${cfg.headers.map(h => {
            const val = h.key ? item[h.key] : null;
            const rendered = h.render ? h.render(val, item) : (val != null ? val : '---');
            return `<td>${rendered}</td>`;
        }).join('')}
            <td style="white-space:nowrap;">
                ${(CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') ? `
                <button class="btn-action btn-edit" onclick="editDienNuoc('${tab}',${item[cfg.pk]})"><i class="fas fa-edit"></i> Sửa</button>
                <button class="btn-action btn-delete" onclick="deleteDienNuoc('${tab}',${item[cfg.pk]})"><i class="fas fa-trash"></i> Xóa</button>
                ` : ''}
            </td>
        </tr>`).join('');
    } catch (e) {
        body.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;color:var(--error);">Lỗi: ${e.message}</td></tr>`;
        showToast('Lỗi tải dữ liệu', 'error');
    }
}

function openDienNuocModal(id = null) {
    const cfg = currentSubSection === 'dien' ? dienModule : nuocModule;
    const item = id ? currentData.find(i => i[cfg.pk] == id) : null;
    buildModal(
        (id ? 'Cập nhật ' : 'Thêm mới ') + cfg.title,
        cfg.fields,
        item || {},
        async (payload) => {
            if (id) {
                payload[cfg.pk] = id;
                await apiFetch(`${cfg.endpoint}/${id}`, 'PUT', payload);
                showToast('Cập nhật thành công!');
            } else {
                await apiFetch(cfg.endpoint, 'POST', payload);
                showToast('Thêm mới thành công!');
            }
            closeModal();
            loadDienNuocData(currentSubSection);
            loadLookups();
        }
    );
}

function editDienNuoc(tab, id) { currentSubSection = tab; openDienNuocModal(id); }

async function deleteDienNuoc(tab, id) {
    if (!confirm('Bạn có chắc chắn muốn xóa?')) return;
    const cfg = tab === 'dien' ? dienModule : nuocModule;
    try {
        await apiFetch(`${cfg.endpoint}/${id}`, 'DELETE');
        showToast('Đã xóa thành công!');
        loadDienNuocData(tab);
    } catch (e) {
        showToast(e.message || 'Lỗi xóa dữ liệu', 'error');
    }
}

// ==========================================
// GENERIC MODAL BUILDER
// ==========================================
function buildModal(title, fields, item, onSubmit) {
    document.getElementById('modalTitle').textContent = title;
    const body = document.getElementById('modalFields');

    body.innerHTML = fields.map(f => {
        const val = item[f.id];
        const displayVal = val != null ? val : (f.defaultVal ?? '');

        if (f.type === 'lookup') {
            const opts = lookups[f.lookup] || [];
            return `<div class="form-group">
                <label for="f_${f.id}">${f.label}${f.required ? ' <span style="color:var(--error)">*</span>' : ''}</label>
                <select id="f_${f.id}" class="form-control" ${f.required ? 'required' : ''}>
                    <option value="">-- Chọn ${f.label} --</option>
                    ${opts.length === 0
                        ? `<option value="" disabled>Chưa có dữ liệu ${f.label.toLowerCase()}</option>`
                        : opts.map(o => `<option value="${o[f.valField]}" ${val == o[f.valField] ? 'selected' : ''}>${o[f.txtField]}</option>`).join('')}
                </select>
            </div>`;
        }
        if (f.type === 'options') {
            return `<div class="form-group">
                <label for="f_${f.id}">${f.label}${f.required ? ' <span style="color:var(--error)">*</span>' : ''}</label>
                <select id="f_${f.id}" class="form-control" ${f.required ? 'required' : ''}>
                    <option value="">-- Chọn --</option>
                    ${f.options.map(o => `<option value="${o}" ${val === o ? 'selected' : ''}>${o}</option>`).join('')}
                </select>
            </div>`;
        }
        if (f.type === 'textarea') {
            return `<div class="form-group" style="grid-column:1/-1;">
                <label for="f_${f.id}">${f.label}${f.required ? ' <span style="color:var(--error)">*</span>' : ''}</label>
                <textarea id="f_${f.id}" class="form-control" ${f.required ? 'required' : ''}>${displayVal}</textarea>
            </div>`;
        }
        if (f.type === 'file') {
            return `<div class="form-group">
                <label for="f_${f.id}">${f.label}</label>
                <input type="file" id="f_${f.id}" class="form-control" accept="image/*">
            </div>`;
        }
        return `<div class="form-group">
            <label for="f_${f.id}">${f.label}${f.required ? ' <span style="color:var(--error)">*</span>' : ''}</label>
            <input type="${f.type}" id="f_${f.id}" class="form-control" value="${displayVal}" ${f.required ? 'required' : ''}>
        </div>`;
    }).join('');

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const payload = {};

        // Handle file upload first if any
        const fileEl = document.getElementById('f_fileUpload');
        if (fileEl && fileEl.files.length > 0) {
            try {
                showToast('Đang tải ảnh lên...', 'info');
                const uploadRes = await API.phong.uploadImage(fileEl.files[0]);
                const imageUrl = uploadRes?.url || uploadRes?.duLieu?.url;

                if (!imageUrl) {
                    showToast('Upload ảnh thất bại: Backend không trả về đường dẫn ảnh', 'error');
                    return;
                }

                payload.hinhAnh = imageUrl;
            } catch (e) {
                showToast('Lỗi upload ảnh: ' + (e.message || 'Không tải được ảnh'), 'error');
                return;
            }
        }

        fields.forEach(f => {
            if (f.id === 'fileUpload') return; // Handled separately
            const el = document.getElementById(`f_${f.id}`);
            if (!el) return;
            const v = el.value;
            if (f.type === 'number') {
                if (v !== '') payload[f.id] = Number(v);
            } else if (f.type === 'lookup') {
                const n = Number(v);
                payload[f.id] = (v !== '' && !isNaN(n) && v.trim() !== '') ? n : v;
            } else if (f.type === 'password') {
                if (v) payload[f.id] = v;
            } else {
                payload[f.id] = v;
            }
        });
        try {
            await onSubmit(payload);
        } catch (e) {
            showToast(e.message || 'Lỗi lưu dữ liệu', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

// ==========================================
// OPEN MODAL (dispatch by section)
// ==========================================
function openModal(id = null) {
    const section = currentSection;
    const cfg = modules[section];
    if (!cfg) return;

    if (cfg.customModal) {
        if (section === 'hopdong') return openHopDongModal(id);
        if (section === 'hoadon') return openHoaDonModal(id);
        if (section === 'user') return openUserModal(id);
        return;
    }

    const item = id ? (currentData.find(i => i[cfg.pk] == id) || {}) : {};

    buildModal(
        (id ? 'Cập nhật ' : 'Thêm mới ') + cfg.title,
        cfg.fields,
        item,
        async (payload) => {
            if (id) {
                payload[cfg.pk] = id;
                await apiFetch(`${cfg.endpoint}/${id}`, 'PUT', payload);
                showToast('Cập nhật thành công!');
            } else {
                await apiFetch(cfg.endpoint, 'POST', payload);
                showToast('Thêm mới thành công!');
            }
            closeModal();
            refreshData();
            loadLookups();
        }
    );
}

function closeModal() {
    document.getElementById('universalModal').style.display = 'none';
}

// ==========================================
// HỢP ĐỒNG CUSTOM MODAL
// ==========================================
async function openHopDongModal(id = null) {
    document.getElementById('modalTitle').textContent = id ? 'Cập nhật Hợp Đồng' : 'Thêm mới Hợp Đồng';

    let item = id ? currentData.find(i => i.maHopDong == id) : null;
    let availableNguoiThue = lookups.nguoithue;
    let availablePhong = lookups.phong;

    if (!id) {
        try {
            const taoMoi = await apiFetch('/api/HopDong/TaoMoi');
            if (taoMoi?.nguoiThue) availableNguoiThue = taoMoi.nguoiThue;
            if (taoMoi?.phong) availablePhong = taoMoi.phong;
        } catch (e) {
            console.warn('TaoMoi failed, using all tenants/rooms', e);
        }
    }

    document.getElementById('modalFields').innerHTML = `
        <div class="form-group">
            <label>Khách thuê <span style="color:var(--error)">*</span></label>
            <select id="f_maNguoiThue" class="form-control" required>
                <option value="">-- Chọn khách thuê --</option>
                ${availableNguoiThue.map(n => `<option value="${n.maNguoiThue}" ${item?.maNguoiThue == n.maNguoiThue ? 'selected' : ''}>${n.hoTen}${n.sdt ? ' – ' + n.sdt : ''}</option>`).join('')}
            </select>
        </div>
        <div class="form-group">
            <label>Phòng <span style="color:var(--error)">*</span></label>
            <select id="f_maPhong" class="form-control" required>
                <option value="">-- Chọn phòng --</option>
                ${availablePhong.map(p => `<option value="${p.maPhong}" ${item?.maPhong == p.maPhong ? 'selected' : ''}>${p.tenPhong}</option>`).join('')}
            </select>
        </div>
        <div class="form-group">
            <label>Ngày bắt đầu <span style="color:var(--error)">*</span></label>
            <input type="date" id="f_ngayBatDau" class="form-control" value="${item?.ngayBatDau ? item.ngayBatDau.substring(0, 10) : ''}" required>
        </div>
        <div class="form-group">
            <label>Ngày kết thúc</label>
            <input type="date" id="f_ngayKetThuc" class="form-control" value="${item?.ngayKetThuc ? item.ngayKetThuc.substring(0, 10) : ''}">
        </div>
        <div class="form-group">
            <label>Tiền cọc (đ) <span style="color:var(--error)">*</span></label>
            <input type="number" id="f_tienCoc" class="form-control" value="${item?.tienCoc || ''}" required min="0">
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Nội dung hợp đồng</label>
            <textarea id="f_noiDung" class="form-control">${item?.noiDung || ''}</textarea>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const payload = {
            maNguoiThue: Number(document.getElementById('f_maNguoiThue').value),
            maPhong: Number(document.getElementById('f_maPhong').value),
            ngayBatDau: document.getElementById('f_ngayBatDau').value,
            tienCoc: Number(document.getElementById('f_tienCoc').value),
            noiDung: document.getElementById('f_noiDung').value
        };
        const kt = document.getElementById('f_ngayKetThuc').value;
        if (kt) payload.ngayKetThuc = kt;
        try {
            if (id) {
                payload.maHopDong = id;
                await apiFetch(`/api/HopDong/${id}`, 'PUT', payload);
                showToast('Cập nhật hợp đồng thành công!');
            } else {
                await apiFetch('/api/HopDong', 'POST', payload);
                showToast('Thêm hợp đồng thành công!');
            }
            closeModal();
            refreshData();
        } catch (e) {
            showToast(e.message || 'Lỗi lưu hợp đồng', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

// ==========================================
// HÓA ĐƠN CUSTOM MODAL
// ==========================================
async function openHoaDonModal(id = null) {
    document.getElementById('modalTitle').textContent = id ? 'Cập nhật Hóa Đơn' : 'Lập Hóa Đơn Mới';
    window._hoaDonInfo = null;

    const now = new Date();
    const todayStr = now.toISOString().substring(0, 10);
    const kyDefault = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;

    document.getElementById('modalFields').innerHTML = `
        <div class="form-group">
            <label>Chọn phòng <span style="color:var(--error)">*</span></label>
            <select id="f_maPhong" class="form-control" required onchange="loadPhongInfo(this.value)">
                <option value="">-- Chọn phòng --</option>
                ${lookups.phong.map(p => `<option value="${p.maPhong}">${p.tenPhong}</option>`).join('')}
            </select>
        </div>
        <div class="form-group">
            <label>Kỳ hóa đơn (YYYY-MM) <span style="color:var(--error)">*</span></label>
            <input type="month" id="f_kyHoaDon" class="form-control" value="${kyDefault}" required>
        </div>
        <div id="phongInfoBox" style="grid-column:1/-1;display:none;">
            <div class="info-grid">
                <div class="info-item"><label>Khách thuê</label><span id="infoNguoiThue">---</span></div>
                <div class="info-item"><label>Tiền phòng</label><span id="infoTienPhong">---</span></div>
                <div class="info-item"><label>Tiền điện</label><span id="infoTienDien">---</span></div>
                <div class="info-item"><label>Tiền nước</label><span id="infoTienNuoc">---</span></div>
                <div class="info-item"><label>Tiền dịch vụ</label><span id="infoTienDichVu">---</span></div>
                <div class="info-item info-total"><label>Dự tính tổng tiền</label><span id="infoTongTien">---</span></div>
            </div>
        </div>
        <div class="form-group">
            <label>Phát sinh khác (đ)</label>
            <input type="number" id="f_tienPhatSinhKhac" class="form-control" value="0" min="0" oninput="recalcTotal()">
        </div>
        <div class="form-group">
            <label>Ngày lập <span style="color:var(--error)">*</span></label>
            <input type="date" id="f_ngayLap" class="form-control" value="${todayStr}" required>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const info = window._hoaDonInfo;
        if (!info) { showToast('Vui lòng chọn phòng có hợp đồng hợp lệ!', 'error'); return; }

        const phatSinh = Number(document.getElementById('f_tienPhatSinhKhac').value) || 0;
        const payload = {
            maNguoiThue: info.nguoiThue.maNguoiThue,
            maPhong: info.phong.maPhong,
            tienPhong: info.phong.giaPhong,
            tienDien: info.tienDien,
            tienNuoc: info.tienNuoc,
            tienDichVu: info.tongTienDichVu,
            tienPhatSinhKhac: phatSinh,
            tongTien: info.phong.giaPhong + info.tienDien + info.tienNuoc + info.tongTienDichVu + phatSinh,
            ngayLap: document.getElementById('f_ngayLap').value,
            kyHoaDon: document.getElementById('f_kyHoaDon').value
        };
        try {
            if (id) {
                payload.maHoaDon = id;
                await apiFetch(`/api/HoaDon/${id}`, 'PUT', payload);
                showToast('Cập nhật hóa đơn thành công!');
            } else {
                await apiFetch('/api/HoaDon', 'POST', payload);
                showToast('Lập hóa đơn thành công!');
            }
            closeModal();
            refreshData();
        } catch (e) {
            showToast(e.message || 'Lỗi lập hóa đơn', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

async function loadPhongInfo(phongId) {
    if (!phongId) { document.getElementById('phongInfoBox').style.display = 'none'; return; }
    try {
        const info = await apiFetch(`/api/HoaDon/GetThongTinPhong/${phongId}`);
        window._hoaDonInfo = info;
        document.getElementById('phongInfoBox').style.display = 'block';
        document.getElementById('infoNguoiThue').textContent = info.nguoiThue?.hoTen || '---';
        document.getElementById('infoTienPhong').textContent = fmtCurrency(info.phong?.giaPhong);
        document.getElementById('infoTienDien').textContent = fmtCurrency(info.tienDien);
        document.getElementById('infoTienNuoc').textContent = fmtCurrency(info.tienNuoc);
        document.getElementById('infoTienDichVu').textContent = fmtCurrency(info.tongTienDichVu);
        document.getElementById('infoTongTien').textContent = fmtCurrency(info.tongTienHoaDon);
    } catch (e) {
        window._hoaDonInfo = null;
        document.getElementById('phongInfoBox').style.display = 'none';
        showToast('Lỗi: ' + (e.message || 'Không tải được thông tin phòng'), 'error');
    }
}

function recalcTotal() {
    if (!window._hoaDonInfo) return;
    const info = window._hoaDonInfo;
    const ps = Number(document.getElementById('f_tienPhatSinhKhac')?.value) || 0;
    const total = (info.phong?.giaPhong || 0) + (info.tienDien || 0) + (info.tienNuoc || 0) + (info.tongTienDichVu || 0) + ps;
    document.getElementById('infoTongTien').textContent = fmtCurrency(total);
}

// ==========================================
// USER CUSTOM MODAL
// ==========================================
async function openUserModal(id = null) {
    document.getElementById('modalTitle').textContent = id ? 'Cập nhật Người Dùng' : 'Thêm Người Dùng Mới';

    const item = id ? currentData.find(i => i.maNguoiDung == id) : null;

    document.getElementById('modalFields').innerHTML = `
        <div class="form-group">
            <label>Tên đăng nhập <span style="color:var(--error)">*</span></label>
            <input type="text" id="f_tenDangNhap" class="form-control" value="${item?.tenDangNhap || ''}" required>
        </div>
        <div class="form-group">
            <label>Họ tên <span style="color:var(--error)">*</span></label>
            <input type="text" id="f_hoTen" class="form-control" value="${item?.hoTen || ''}" required>
        </div>
        <div class="form-group">
            <label>Email <span style="color:var(--error)">*</span></label>
            <input type="email" id="f_email" class="form-control" value="${item?.email || ''}" ${id ? 'required' : 'required'}>
        </div>
        <div class="form-group">
            <label>Số điện thoại</label>
            <input type="text" id="f_soDienThoai" class="form-control" value="${item?.soDienThoai || ''}">
        </div>
        <div class="form-group">
            <label>Vai trò <span style="color:var(--error)">*</span></label>
            <select id="f_vaiTro" class="form-control" required>
                <option value="">-- Chọn vai trò --</option>
                ${['Admin', 'ChuTro', 'NguoiDung'].map(v => `<option value="${v}" ${item?.vaiTro === v ? 'selected' : ''}>${v === 'ChuTro' ? 'Chủ trọ' : v === 'NguoiDung' ? 'Người dùng' : v}</option>`).join('')}
            </select>
        </div>
        ${id ? `<div class="form-group">
            <label>Trạng thái</label>
            <select id="f_trangThai" class="form-control">
                <option value="true" ${item?.trangThai ? 'selected' : ''}>Hoạt động</option>
                <option value="false" ${!item?.trangThai ? 'selected' : ''}>Khóa</option>
            </select>
        </div>` : `<div class="form-group">
            <label>Mật khẩu <span style="color:var(--error)">*</span></label>
            <input type="password" id="f_matKhau" class="form-control" required minlength="6">
        </div>`}
    `;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        try {
            if (id) {
                const payload = {
                    tenDangNhap: document.getElementById('f_tenDangNhap').value,
                    hoTen: document.getElementById('f_hoTen').value,
                    email: document.getElementById('f_email').value,
                    soDienThoai: document.getElementById('f_soDienThoai').value,
                    vaiTro: document.getElementById('f_vaiTro').value,
                    trangThai: document.getElementById('f_trangThai').value === 'true'
                };
                await apiFetch(`/api/User/${id}`, 'PUT', payload);
                showToast('Cập nhật người dùng thành công!');
            } else {
                const payload = {
                    tenDangNhap: document.getElementById('f_tenDangNhap').value,
                    hoTen: document.getElementById('f_hoTen').value,
                    email: document.getElementById('f_email').value,
                    soDienThoai: document.getElementById('f_soDienThoai').value,
                    vaiTro: document.getElementById('f_vaiTro').value,
                    matKhau: document.getElementById('f_matKhau').value
                };
                await apiFetch('/api/Auth/dang-ky', 'POST', payload);
                showToast('Thêm người dùng thành công!');
            }
            closeModal();
            refreshData();
        } catch (e) {
            showToast(e.message || 'Lỗi lưu người dùng', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

// ==========================================
// CRUD HELPERS
// ==========================================
function editItem(section, id) {
    currentSection = section;
    openModal(id);
}

async function deleteItem(section, id) {
    if (!confirm('Bạn có chắc chắn muốn xóa mục này?')) return;
    const cfg = modules[section];
    if (!cfg) return;
    try {
        await apiFetch(`${cfg.endpoint}/${id}`, 'DELETE');
        showToast('Đã xóa thành công!');
        refreshData();
        loadLookups();
    } catch (e) {
        showToast(e.message || 'Lỗi xóa dữ liệu', 'error');
    }
}

function refreshData() {
    if (currentSection === 'overview') loadOverview();
    else if (currentSection === 'phong') renderRoomGrid();
    else if (currentSection === 'diennuoc') loadDienNuocData(currentSubSection);
    else loadGenericSection(currentSection);
}

function logout() {
    localStorage.clear();
    window.location.href = '/index.html';
}

// ==========================================
// STARTUP
// ==========================================
(async () => {
    await loadLookups();
    loadOverview();
})();
