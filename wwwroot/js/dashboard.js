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
let lookups = { nhatro: [], loaiphong: [], trangthai: [], phong: [], nguoithue: [], hoadon: [], hinhthuc: [] };
window.lookups = lookups;

// --- FORMATTERS ---
const fmtCurrency = v => (v != null && v !== '') ? new Intl.NumberFormat('vi-VN').format(v) + 'đ' : '---';
const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN') : '---';
window.AppFormat = window.AppFormat || {
    currency: fmtCurrency,
    date: fmtDate,
    escapeHtml: v => v === null || v === undefined ? '' : String(v)
        .replaceAll('&', '&amp;')
        .replaceAll('\"', '&quot;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;')
};
function escapeHtmlDashboard(v) {
    return v === null || v === undefined ? '' : String(v)
        .replaceAll('&', '&amp;')
        .replaceAll('"', '&quot;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;');
}

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


function normalizeArrayResponse(value) {
    if (Array.isArray(value)) return value;
    if (!value) return [];
    if (Array.isArray(value.duLieu)) return value.duLieu;
    if (Array.isArray(value.data)) return value.data;
    if (Array.isArray(value.$values)) return value.$values;
    if (value.duLieu && Array.isArray(value.duLieu.$values)) return value.duLieu.$values;
    if (value.data && Array.isArray(value.data.$values)) return value.data.$values;
    return [];
}
window.normalizeArrayResponse = normalizeArrayResponse;

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
    lookups.nhatro = normalizeArrayResponse(results[0].value);
    lookups.loaiphong = normalizeArrayResponse(results[1].value);
    lookups.trangthai = normalizeArrayResponse(results[2].value);
    if (lookups.trangthai.length === 0) {
        console.warn('Không tải được danh sách trạng thái từ API /api/TrangThai');
    }
    lookups.phong = normalizeArrayResponse(results[3].value);
    lookups.nguoithue = normalizeArrayResponse(results[4].value);

    // Bổ sung lookups cho Thanh toán
    try {
        const hd = await apiFetch('/api/HoaDon');
        lookups.hoadon = normalizeArrayResponse(hd);
    } catch (e) { console.warn('Load hoadon lookup failed'); }

    lookups.hinhthuc = [
        { val: 'Tiền mặt', label: 'Tiền mặt' },
        { val: 'Chuyển khoản', label: 'Chuyển khoản' }
    ];
}

// ==========================================
// MODULE CONFIGS
// ==========================================
const modules = window.AppModules || {};


// --- Điện & Nước modules ---
const dienModule = window.AppDienNuocModules?.dien || {};
const nuocModule = window.AppDienNuocModules?.nuoc || {};

// ==========================================
// SECTION NAVIGATION
// ==========================================
function normalizeSectionFromHash() {
    const raw = (window.location.hash || '').replace('#', '').trim();
    if (!raw) return 'overview';

    const aliases = {
        'nha-tro': 'nhatro',
        'phong-tro': 'phong',
        'loai-phong': 'loaiphong',
        'dich-vu': 'dichvu',
        'khach-thue': 'nguoithue',
        'hop-dong': 'hopdong',
        'hoa-don': 'hoadon',
        'thanh-toan': 'thanhtoan',
        'dien-nuoc': 'diennuoc',
        'yeu-cau-thue': 'yeucauthue',
        'bao-cao-su-co': 'baocaosuco',
        'nguoi-dung': 'user',
        'tai-khoan': 'taikhoan'
    };

    return aliases[raw] || raw;
}

function sectionToHash(section) {
    const map = {
        nhatro: 'nha-tro',
        phong: 'phong',
        loaiphong: 'loai-phong',
        dichvu: 'dich-vu',
        nguoithue: 'khach-thue',
        hopdong: 'hop-dong',
        hoadon: 'hoa-don',
        thanhtoan: 'thanh-toan',
        diennuoc: 'dien-nuoc',
        yeucauthue: 'yeu-cau-thue',
        baocaosuco: 'bao-cao-su-co',
        user: 'nguoi-dung',
        taikhoan: 'tai-khoan',
        account: 'tai-khoan',
        overview: 'overview'
    };
    return map[section] || section;
}

function activateNav(section, el) {
    document.querySelectorAll('.nav-link').forEach(l => l.classList.remove('active'));
    if (el) {
        el.classList.add('active');
        return;
    }

    const hash = sectionToHash(section);
    const candidates = [section, hash];
    const matched = Array.from(document.querySelectorAll('.nav-link')).find(link => {
        const attr = link.getAttribute('onclick') || '';
        return candidates.some(x => attr.includes(`'${x}'`) || attr.includes(`\"${x}\"`));
    });
    if (matched) matched.classList.add('active');
}

function showSection(section, el, skipHashUpdate = false) {
    currentSection = section;

    if (!skipHashUpdate) {
        const nextHash = '#' + sectionToHash(section);
        if (window.location.hash !== nextHash) {
            history.pushState(null, '', nextHash);
        }
    }

    activateNav(section, el);

    const addBtn = document.getElementById('addBtn');
    const sectionTitle = document.getElementById('sectionTitle');

    if (section === 'account') section = 'taikhoan';
    currentSection = section;

    // NguoiDung chỉ xem, không được tạo/sửa/xóa
    const canCreate = ((CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') && section !== 'yeucauthue')
        || (CURRENT_ROLE === 'NguoiDung' && section === 'yeucauthue');
    const canWrite = canCreate;

    const overviewEl = document.getElementById('overviewSection');
    const genericEl = document.getElementById('genericSection');
    const accountEl = document.getElementById('taikhoanSection');

    if (accountEl) accountEl.style.display = section === 'taikhoan' ? 'block' : 'none';

    if (section === 'taikhoan') {
        if (overviewEl) overviewEl.style.display = 'none';
        if (genericEl) genericEl.style.display = 'none';
        if (addBtn) addBtn.style.display = 'none';
        if (sectionTitle) sectionTitle.textContent = 'Tài khoản của tôi';
        if (typeof window.loadProfile === 'function') window.loadProfile();
        return;
    }

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
    document.getElementById('genericSection').style.display = section !== 'overview' && section !== 'taikhoan' ? 'block' : 'none';

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
    const phongList = data?.danhSachPhongDangThue || (data?.phongDangThue ? [data.phongDangThue] : []);
    const hopDongList = data?.danhSachHopDongHienTai || (data?.hopDongHienTai ? [data.hopDongHienTai] : []);
    const hoaDonList = data?.danhSachHoaDonThangNay || (data?.hoaDonThangNay ? [data.hoaDonThangNay] : []);

    const tongTien = data?.tongTienThangNay ?? hoaDonList.reduce((sum, h) => sum + (h.tongTien || 0), 0);
    const daThanhToan = data?.daThanhToanThangNay ?? hoaDonList.reduce((sum, h) => sum + (h.daThanhToan || 0), 0);
    const conLai = data?.conLaiThangNay ?? Math.max(tongTien - daThanhToan, 0);
    const trangThai = data?.trangThaiThanhToan || (!hoaDonList.length ? 'Chưa có hóa đơn tháng này' : (conLai <= 0 ? 'Đã trả' : 'Chưa trả'));
    const badgeClass = !hoaDonList.length ? 'badge-warning' : (conLai <= 0 ? 'badge-success' : 'badge-danger');

    const phongText = phongList.length
        ? `${phongList.length} phòng đang thuê`
        : 'Chưa có phòng';

    const phongSmall = phongList.length
        ? phongList.slice(0, 3).map(p => `${p.tenPhong || 'Phòng'}${p.tenNhaTro ? ' - ' + p.tenNhaTro : ''}`).join('<br>')
        : '';

    const hopDongText = hopDongList.length
        ? `${hopDongList.length} hợp đồng hiệu lực`
        : 'Chưa có hợp đồng';

    const hopDongSmall = hopDongList.length
        ? hopDongList.slice(0, 2).map(h => `${fmtDate(h.ngayBatDau)} - ${fmtDate(h.ngayKetThuc)}`).join('<br>')
        : '';

    const statsGrid = document.querySelector('#overviewSection .stats-grid');
    statsGrid.innerHTML = `
        <div class="stat-card stat-card-indigo">
            <div class="stat-icon"><i class="fas fa-user"></i></div>
            <div class="stat-info"><h3>Thông tin tài khoản</h3><div class="value" style="font-size:1rem;line-height:1.45">${tk.hoTen || '---'}<br><small>${tk.email || '---'}</small><br><small>${tk.soDienThoai || '---'}</small></div></div>
        </div>
        <div class="stat-card stat-card-blue">
            <div class="stat-icon"><i class="fas fa-home"></i></div>
            <div class="stat-info"><h3>Phòng đang thuê</h3><div class="value" style="font-size:1.15rem">${phongText}</div><small>${phongSmall}</small></div>
        </div>
        <div class="stat-card stat-card-green">
            <div class="stat-icon"><i class="fas fa-file-contract"></i></div>
            <div class="stat-info"><h3>Hợp đồng hiện tại</h3><div class="value" style="font-size:1rem;line-height:1.45">${hopDongText}</div>${hopDongSmall ? `<small>${hopDongSmall}</small>` : ''}</div>
        </div>
        <div class="stat-card stat-card-purple">
            <div class="stat-icon"><i class="fas fa-coins"></i></div>
            <div class="stat-info"><h3>Tổng tiền tháng này</h3><div class="value">${hoaDonList.length ? fmtCurrency(tongTien) : '---'}</div>${hoaDonList.length ? `<small>${hoaDonList.length} hóa đơn</small>` : ''}</div>
        </div>
        <div class="stat-card stat-card-red">
            <div class="stat-icon"><i class="fas fa-exclamation-triangle"></i></div>
            <div class="stat-info"><h3>Trạng thái thanh toán</h3><div class="value" style="font-size:1.1rem"><span class="badge ${badgeClass}">${trangThai}</span></div>${hoaDonList.length ? `<small>Còn lại: ${fmtCurrency(conLai)}</small>` : ''}</div>
        </div>
    `;

    const title = document.querySelector('#overviewSection .data-card h2');
    if (title) title.textContent = 'Chi tiết hóa đơn tháng này';

    const tbody = document.querySelector('#recentRoomsTable tbody');
    if (!hoaDonList.length) {
        tbody.innerHTML = '<tr><td colspan="5" style="text-align:center;padding:2rem;color:var(--text-light);">Chưa có hóa đơn tháng này</td></tr>';
        return;
    }

    tbody.innerHTML = hoaDonList.map(h => {
        const conLaiHoaDon = h.conLai ?? Math.max((h.tongTien || 0) - (h.daThanhToan || 0), 0);
        const daTra = h.daThanhToan ?? Math.max((h.tongTien || 0) - conLaiHoaDon, 0);
        const trangThaiHoaDon = h.trangThaiThanhToan || (conLaiHoaDon <= 0 ? 'Đã trả' : 'Chưa trả');
        const cls = conLaiHoaDon <= 0 ? 'badge-success' : 'badge-danger';

        return `<tr>
            <td><strong>${h.kyHoaDon || '---'}</strong></td>
            <td>${h.tenPhong || phongList.find(p => p.maPhong === h.maPhong)?.tenPhong || '---'}</td>
            <td>${fmtCurrency(h.tongTien)}</td>
            <td><span class="badge ${cls}">${trangThaiHoaDon}</span></td>
            <td>Đã trả: ${fmtCurrency(daTra)}</td>
        </tr>`;
    }).join('');
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
            ` : CURRENT_ROLE === 'NguoiDung' ? `
            <div style="display:flex;gap:0.5rem;">
                <button class="btn btn-primary" style="flex:1;padding:0.5rem;font-size:0.875rem;" onclick="openYeuCauThueModal(null, ${r.maPhong})"><i class="fas fa-paper-plane"></i> Gửi yêu cầu thuê</button>
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
    if (!cfg) {
        const sectionTitle = document.getElementById('sectionTitle');
        if (sectionTitle) sectionTitle.textContent = 'Không tìm thấy mục';
        const generic = document.getElementById('genericSection');
        if (generic) generic.innerHTML = '<div class="data-card" style="padding:1.5rem;color:var(--error);">Không tìm thấy cấu hình module cho mục này. Vui lòng tải lại trang.</div>';
        return;
    }
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
        const rawData = await apiFetch(cfg.endpoint);
        currentData = normalizeArrayResponse(rawData);
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

    const safeData = normalizeArrayResponse(data);
    const displayData = section === 'nguoithue' ? mergeNguoiThueDisplayRows(safeData) : safeData;

    if (!displayData?.length) {
        tbody.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;color:var(--text-light);">Không có dữ liệu</td></tr>`;
        return;
    }
    const canWrite = (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro');

    tbody.innerHTML = displayData.map(item => {
        let actionHtml = '';

        if (section === 'yeucauthue') {
            if ((CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') && item.trangThai === 'ChoDuyet') {
                actionHtml = `
                    <button class="btn-action btn-edit" onclick="openYeuCauThueDuyetModal(${item.maYeuCau})"><i class="fas fa-check"></i> Duyệt</button>
                    <button class="btn-action btn-delete" onclick="rejectYeuCauThue(${item.maYeuCau})"><i class="fas fa-times"></i> Từ chối</button>`;
            } else if (CURRENT_ROLE === 'NguoiDung' && item.trangThai === 'ChoDuyet') {
                actionHtml = `<button class="btn-action btn-delete" onclick="deleteItem('yeucauthue',${item.maYeuCau})"><i class="fas fa-trash"></i> Hủy</button>`;
            }
        } else if (section === 'nguoithue') {
            actionHtml = `<button class="btn-action btn-edit" onclick="viewNguoiThueDetail(${item.maNguoiThue})"><i class="fas fa-eye"></i> Chi tiết</button>`;

            if (CURRENT_ROLE === 'Admin') {
                actionHtml += `
                    <button class="btn-action btn-edit" onclick="editItem('nguoithue',${item.maNguoiThue})"><i class="fas fa-edit"></i> Sửa</button>
                    <button class="btn-action btn-delete" onclick="deleteNguoiThueDisplayGroup(${item.maNguoiThue})"><i class="fas fa-trash"></i> Xóa</button>`;
            } else if (CURRENT_ROLE === 'ChuTro') {
                actionHtml += `
                    <button class="btn-action btn-delete" onclick="deleteNguoiThueDisplayGroup(${item.maNguoiThue})"><i class="fas fa-trash"></i> Xóa</button>`;
            }
        } else if (section === 'hoadon') {
            actionHtml = `<button class="btn-action btn-edit" onclick="openHoaDonThanhToanModal(${item.maHoaDon})"><i class="fas fa-qrcode"></i> Thanh toán</button>`;
            if (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') {
                actionHtml += `
                    <button class="btn-action btn-edit" onclick="editItem('hoadon',${item.maHoaDon})"><i class="fas fa-edit"></i> Sửa</button>
                    <button class="btn-action btn-delete" onclick="deleteItem('hoadon',${item.maHoaDon})"><i class="fas fa-trash"></i> Xóa</button>`;
            }
        } else if (canWrite) {
            actionHtml = `
                <button class="btn-action btn-edit" onclick="editItem('${section}',${item[cfg.pk]})"><i class="fas fa-edit"></i> Sửa</button>
                <button class="btn-action btn-delete" onclick="deleteItem('${section}',${item[cfg.pk]})"><i class="fas fa-trash"></i> Xóa</button>`;
        }

        return `<tr>
            ${cfg.headers.map(h => {
                const val = h.key ? item[h.key] : null;
                const rendered = h.render ? h.render(val, item) : (val != null && val !== '' ? val : '---');
                return `<td>${rendered}</td>`;
            }).join('')}
            <td style="white-space:nowrap;">${actionHtml}</td>
        </tr>`;
    }).join('');
}

async function searchNguoiThue(q) {
    if (!q) { renderTable(modules.nguoithue, currentData, 'nguoithue'); return; }
    try {
        const results = await apiFetch(`/api/NguoiThue/Search?keyword=${encodeURIComponent(q)}`);
        renderTable(modules.nguoithue, normalizeArrayResponse(results), 'nguoithue');
    } catch {
        const lower = q.toLowerCase();
        const filtered = normalizeArrayResponse(currentData).filter(n =>
            (n.hoTen || '').toLowerCase().includes(lower) ||
            (n.cccd || '').includes(q) ||
            (n.sdt || '').includes(q) ||
            (n.email || '').toLowerCase().includes(lower)
        );
        renderTable(modules.nguoithue, filtered, 'nguoithue');
    }
}


// Gộp danh sách khách thuê theo cùng tài khoản/người thật để chủ trọ không thấy lặp
// khi một người thuê nhiều phòng. Dữ liệu gốc vẫn giữ nguyên trong currentData/lookups để các form nghiệp vụ dùng đúng hồ sơ phòng.
let nguoiThueGroupMap = {};
window.nguoiThueGroupMap = nguoiThueGroupMap;

function getNguoiThueGroupKey(item) {
    if (item.maNguoiDung) return `user_${item.maNguoiDung}`;

    const identity = [item.cccd, item.email, item.sdt]
        .filter(v => v !== null && v !== undefined && String(v).trim() !== '')
        .map(v => String(v).trim().toLowerCase())
        .join('|');

    return identity ? `identity_${identity}` : `profile_${item.maNguoiThue}`;
}

function mergeNguoiThueDisplayRows(data) {
    const source = normalizeArrayResponse(data);
    const groups = new Map();

    source.forEach(item => {
        const key = getNguoiThueGroupKey(item);
        if (!groups.has(key)) {
            groups.set(key, {
                ...item,
                _nguoiThueItems: [],
                danhSachMaNguoiThue: [],
                danhSachPhongText: '',
                soPhongDangThue: 0
            });
        }

        const group = groups.get(key);
        group._nguoiThueItems.push(item);
        group.danhSachMaNguoiThue.push(item.maNguoiThue);

        ['hoTen', 'cccd', 'sdt', 'email', 'ngaySinh', 'gioiTinh', 'quocTich', 'diaChi', 'noiCongTac', 'anhCccdMatTruoc', 'anhCccdMatSau'].forEach(k => {
            if ((group[k] === null || group[k] === undefined || group[k] === '') && item[k]) group[k] = item[k];
        });
    });

    const rows = Array.from(groups.values()).map(group => {
        const rooms = [];
        const seen = new Set();

        group._nguoiThueItems.forEach(nt => {
            const phong = lookups.phong.find(p => Number(p.maPhong) === Number(nt.maPhong));
            const nhaTro = phong ? lookups.nhatro.find(n => Number(n.maNhaTro) === Number(phong.maNhaTro)) : null;
            const label = `${phong?.tenPhong || ('Phòng #' + nt.maPhong)}${nhaTro?.tenNhaTro ? ' - ' + nhaTro.tenNhaTro : ''}`;
            const roomKey = String(nt.maPhong || label);
            if (!seen.has(roomKey)) {
                seen.add(roomKey);
                rooms.push({
                    maNguoiThue: nt.maNguoiThue,
                    maPhong: nt.maPhong,
                    tenPhong: phong?.tenPhong || ('Phòng #' + nt.maPhong),
                    tenNhaTro: nhaTro?.tenNhaTro || '',
                    label
                });
            }
        });

        return {
            ...group,
            danhSachPhong: rooms,
            soPhongDangThue: rooms.length || 1,
            danhSachPhongText: rooms.length ? rooms.map(r => r.label).join('<br>') : (lookups.phong.find(p => Number(p.maPhong) === Number(group.maPhong))?.tenPhong || ('#' + group.maPhong)),
            _isNguoiThueGroup: true
        };
    });

    nguoiThueGroupMap = {};
    rows.forEach(row => {
        nguoiThueGroupMap[row.maNguoiThue] = row;
    });
    window.nguoiThueGroupMap = nguoiThueGroupMap;

    return rows;
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
// MODAL FOOTER HELPER
// ==========================================
function resetModalFooter() {
    const footer = document.querySelector('#universalModal .modal-footer');
    if (!footer) return;
    footer.innerHTML = `
        <button type="button" class="btn btn-secondary" style="width:auto;" onclick="closeModal()">Hủy</button>
        <button type="submit" class="btn btn-primary" style="width:auto;">
            <i class="fas fa-save"></i> Lưu dữ liệu
        </button>`;
}

// ==========================================
// GENERIC MODAL BUILDER
// ==========================================
function buildModal(title, fields, item, onSubmit) {
    resetModalFooter();
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

        // Handle file uploads first if any
        const fileEl = document.getElementById('f_fileUpload');
        if (fileEl && fileEl.files.length > 0) {
            try {
                showToast('Đang tải ảnh phòng lên...', 'info');
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

        const cccdFrontEl = document.getElementById('f_anhCccdMatTruoc');
        if (cccdFrontEl && cccdFrontEl.files.length > 0) {
            try {
                showToast('Đang tải CCCD mặt trước...', 'info');
                const uploadRes = await API.nguoithue.uploadCccdImage(cccdFrontEl.files[0]);
                const imageUrl = uploadRes?.url || uploadRes?.duLieu?.url;
                if (!imageUrl) {
                    showToast('Upload CCCD mặt trước thất bại', 'error');
                    return;
                }
                payload.anhCccdMatTruoc = imageUrl;
            } catch (e) {
                showToast('Lỗi upload CCCD mặt trước: ' + (e.message || 'Không tải được ảnh'), 'error');
                return;
            }
        }

        const cccdBackEl = document.getElementById('f_anhCccdMatSau');
        if (cccdBackEl && cccdBackEl.files.length > 0) {
            try {
                showToast('Đang tải CCCD mặt sau...', 'info');
                const uploadRes = await API.nguoithue.uploadCccdImage(cccdBackEl.files[0]);
                const imageUrl = uploadRes?.url || uploadRes?.duLieu?.url;
                if (!imageUrl) {
                    showToast('Upload CCCD mặt sau thất bại', 'error');
                    return;
                }
                payload.anhCccdMatSau = imageUrl;
            } catch (e) {
                showToast('Lỗi upload CCCD mặt sau: ' + (e.message || 'Không tải được ảnh'), 'error');
                return;
            }
        }

        fields.forEach(f => {
            if (['fileUpload', 'anhCccdMatTruoc', 'anhCccdMatSau'].includes(f.id)) return; // Handled separately
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
    resetModalFooter();
    const section = currentSection;
    const cfg = modules[section];
    if (!cfg) return;

    if (cfg.customModal) {
        if (section === 'hopdong') return openHopDongModal(id);
        if (section === 'yeucauthue') return openYeuCauThueModal(id);
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
// YÊU CẦU THUÊ CUSTOM MODAL
// ==========================================
async function openYeuCauThueModal(id = null, maPhongChon = null) {
    if (CURRENT_ROLE !== 'NguoiDung') {
        showToast('Chỉ người dùng mới được gửi yêu cầu thuê', 'error');
        return;
    }

    resetModalFooter();
    document.getElementById('modalTitle').textContent = 'Gửi yêu cầu thuê phòng';

    document.getElementById('modalFields').innerHTML = `
        <div class="form-group">
            <label>Phòng muốn thuê <span style="color:var(--error)">*</span></label>
            <select id="f_maPhong" class="form-control" required>
                <option value="">-- Chọn phòng --</option>
                ${lookups.phong.map(p => {
                    const house = lookups.nhatro.find(n => n.maNhaTro === p.maNhaTro);
                    return `<option value="${p.maPhong}" ${maPhongChon == p.maPhong ? 'selected' : ''}>${p.tenPhong}${house ? ' – ' + house.tenNhaTro : ''} – ${fmtCurrency(p.giaPhong)}</option>`;
                }).join('')}
            </select>
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Ghi chú gửi chủ trọ</label>
            <textarea id="f_ghiChuNguoiDung" class="form-control" placeholder="Ví dụ: Em muốn xem phòng vào cuối tuần..."></textarea>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();

        const payload = {
            maPhong: Number(document.getElementById('f_maPhong').value),
            ghiChuNguoiDung: document.getElementById('f_ghiChuNguoiDung').value
        };

        try {
            await apiFetch('/api/YeuCauThue', 'POST', payload);
            showToast('Gửi yêu cầu thuê thành công!');
            closeModal();
            if (currentSection === 'yeucauthue') refreshData();
        } catch (e) {
            showToast(e.message || 'Lỗi gửi yêu cầu thuê', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

async function openYeuCauThueDuyetModal(maYeuCau) {
    const yc = currentData.find(x => x.maYeuCau == maYeuCau);
    if (!yc) {
        showToast('Không tìm thấy yêu cầu thuê', 'error');
        return;
    }

    const today = new Date().toISOString().substring(0, 10);

    document.getElementById('modalTitle').textContent = 'Duyệt yêu cầu và lập hợp đồng';
    document.getElementById('modalFields').innerHTML = `
        <div style="grid-column:1/-1;background:#f8fafc;border-radius:.75rem;padding:1rem;margin-bottom:.5rem;">
            <strong>${yc.nguoiDung?.hoTen || 'Người dùng'}</strong> muốn thuê <strong>${yc.phong?.tenPhong || 'phòng'}</strong><br>
            <small>${yc.phong?.nhaTro?.tenNhaTro || ''}</small>
        </div>
        <div class="form-group">
            <label>Ngày bắt đầu <span style="color:var(--error)">*</span></label>
            <input type="date" id="f_ngayBatDau" class="form-control" value="${today}" required>
        </div>
        <div class="form-group">
            <label>Ngày kết thúc</label>
            <input type="date" id="f_ngayKetThuc" class="form-control">
        </div>
        <div class="form-group">
            <label>Tiền cọc (đ) <span style="color:var(--error)">*</span></label>
            <input type="number" id="f_tienCoc" class="form-control" value="0" min="0" required>
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Nội dung hợp đồng</label>
            <textarea id="f_noiDung" class="form-control">Hợp đồng thuê phòng ${yc.phong?.tenPhong || ''}</textarea>
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Ghi chú phản hồi</label>
            <textarea id="f_ghiChuChuTro" class="form-control"></textarea>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();

        const payload = {
            ngayBatDau: document.getElementById('f_ngayBatDau').value,
            tienCoc: Number(document.getElementById('f_tienCoc').value),
            noiDung: document.getElementById('f_noiDung').value,
            ghiChuChuTro: document.getElementById('f_ghiChuChuTro').value
        };

        const ngayKetThuc = document.getElementById('f_ngayKetThuc').value;
        if (ngayKetThuc) payload.ngayKetThuc = ngayKetThuc;

        try {
            await apiFetch(`/api/YeuCauThue/${maYeuCau}/chap-nhan`, 'POST', payload);
            showToast('Đã duyệt yêu cầu và lập hợp đồng!');
            closeModal();
            await loadLookups();
            refreshData();
        } catch (e) {
            showToast(e.message || 'Lỗi duyệt yêu cầu thuê', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

async function rejectYeuCauThue(maYeuCau) {
    const ghiChu = prompt('Lý do từ chối yêu cầu thuê:') || '';
    try {
        await apiFetch(`/api/YeuCauThue/${maYeuCau}/tu-choi`, 'POST', { ghiChuChuTro: ghiChu });
        showToast('Đã từ chối yêu cầu thuê');
        refreshData();
    } catch (e) {
        showToast(e.message || 'Lỗi từ chối yêu cầu thuê', 'error');
    }
}

// ==========================================
// HỢP ĐỒNG CUSTOM MODAL
// ==========================================
async function openHopDongModal(id = null) {
    resetModalFooter();
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
    resetModalFooter();
    document.getElementById('modalTitle').textContent = id ? 'Cập nhật Hóa Đơn' : 'Lập Hóa Đơn Mới';
    window._hoaDonInfo = null;

    const now = new Date();
    const todayStr = now.toISOString().substring(0, 10);
    const kyDefault = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
    const editingItem = id ? (currentData || []).find(h => h.maHoaDon == id) : null;
    const loaiDefault = editingItem?.loaiHoaDon || 'HangThang';

    document.getElementById('modalFields').innerHTML = `
        <div class="form-group">
            <label>Loại hóa đơn <span style="color:var(--error)">*</span></label>
            <select id="f_loaiHoaDon" class="form-control" required onchange="onHoaDonTypeChanged()">
                <option value="HangThang" ${loaiDefault === 'HangThang' ? 'selected' : ''}>Hóa đơn hằng tháng</option>
                <option value="ThuePhong" ${loaiDefault === 'ThuePhong' ? 'selected' : ''}>Hóa đơn thuê phòng</option>
            </select>
            <small style="color:var(--text-light);display:block;margin-top:.35rem;">
                Hằng tháng chỉ tính điện, nước, dịch vụ đã chọn và phát sinh khác. Thuê phòng chỉ tính tiền phòng và phát sinh khác.
            </small>
        </div>
        <div class="form-group">
            <label>Chọn phòng <span style="color:var(--error)">*</span></label>
            <select id="f_maPhong" class="form-control" required onchange="loadPhongInfo(this.value)">
                <option value="">-- Chọn phòng --</option>
                ${lookups.phong.map(p => `<option value="${p.maPhong}" ${editingItem?.maPhong == p.maPhong ? 'selected' : ''}>${p.tenPhong}</option>`).join('')}
            </select>
        </div>
        <div class="form-group">
            <label>Kỳ hóa đơn (YYYY-MM) <span style="color:var(--error)">*</span></label>
            <input type="month" id="f_kyHoaDon" class="form-control" value="${editingItem?.kyHoaDon || kyDefault}" required onchange="reloadHoaDonPhongInfo()">
        </div>
        <div id="phongInfoBox" style="grid-column:1/-1;display:none;">
            <div class="info-grid">
                <div class="info-item"><label>Khách thuê</label><span id="infoNguoiThue">---</span></div>
                <div class="info-item"><label>Tiền phòng</label><span id="infoTienPhong">---</span></div>
                <div class="info-item hang-thang-only"><label>Tiền điện</label><span id="infoTienDien">---</span></div>
                <div class="info-item hang-thang-only"><label>Tiền nước</label><span id="infoTienNuoc">---</span></div>
                <div class="info-item hang-thang-only"><label>Tiền dịch vụ đã chọn</label><span id="infoTienDichVu">---</span></div>
                <div class="info-item info-total"><label>Dự tính tổng tiền</label><span id="infoTongTien">---</span></div>
            </div>
            <div id="hoaDonReadingWarning" class="hang-thang-only" style="display:none;margin-top:1rem;padding:.75rem 1rem;border-radius:.75rem;background:#fffbeb;color:#92400e;border:1px solid #fde68a;"></div>
            <div id="dichVuHoaDonBox" class="hang-thang-only" style="margin-top:1rem;display:none;">
                <label style="font-weight:700;margin-bottom:.5rem;display:block;">Dịch vụ phòng đã sử dụng trong kỳ</label>
                <div id="dichVuHoaDonList" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:.65rem;"></div>
                <small style="color:var(--text-light);display:block;margin-top:.5rem;">Chỉ các dịch vụ được tick mới được cộng vào hóa đơn hằng tháng.</small>
            </div>
        </div>
        <div class="form-group">
            <label>Phát sinh khác (đ)</label>
            <input type="number" id="f_tienPhatSinhKhac" class="form-control" value="${editingItem?.tienPhatSinhKhac || 0}" min="0" oninput="recalcTotal()">
        </div>
        <div class="form-group">
            <label>Ngày lập <span style="color:var(--error)">*</span></label>
            <input type="date" id="f_ngayLap" class="form-control" value="${editingItem?.ngayLap ? editingItem.ngayLap.substring(0, 10) : todayStr}" required>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const info = window._hoaDonInfo;
        if (!info) { showToast('Vui lòng chọn phòng có hợp đồng hợp lệ!', 'error'); return; }

        const loaiHoaDon = document.getElementById('f_loaiHoaDon').value || 'HangThang';
        const phatSinh = Number(document.getElementById('f_tienPhatSinhKhac').value) || 0;
        const selectedServices = Array.from(document.querySelectorAll('.hoa-don-dich-vu:checked')).map(x => Number(x.value));
        const tienDichVu = loaiHoaDon === 'HangThang' ? calcSelectedServiceTotal() : 0;
        const phongInfo = getProp(info, 'phong', 'Phong', {});
        const nguoiThueInfo = getProp(info, 'nguoiThue', 'NguoiThue', {});
        const tienPhong = loaiHoaDon === 'ThuePhong' ? Number(getProp(phongInfo, 'giaPhong', 'GiaPhong', 0)) : 0;
        const tienDien = loaiHoaDon === 'HangThang' ? Number(getProp(info, 'tienDien', 'TienDien', 0)) : 0;
        const tienNuoc = loaiHoaDon === 'HangThang' ? Number(getProp(info, 'tienNuoc', 'TienNuoc', 0)) : 0;

        const payload = {
            loaiHoaDon,
            maNguoiThue: Number(getProp(nguoiThueInfo, 'maNguoiThue', 'MaNguoiThue')),
            maPhong: Number(getProp(phongInfo, 'maPhong', 'MaPhong')),
            tienPhong,
            tienDien,
            tienNuoc,
            tienDichVu,
            tienPhatSinhKhac: phatSinh,
            maDichVuSuDung: loaiHoaDon === 'HangThang' ? selectedServices : [],
            tongTien: tienPhong + tienDien + tienNuoc + tienDichVu + phatSinh,
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

    if (editingItem?.maPhong) {
        await loadPhongInfo(editingItem.maPhong);
        onHoaDonTypeChanged();
    }
}

function getProp(obj, camelName, pascalName, fallback = undefined) {
    if (!obj) return fallback;
    if (Object.prototype.hasOwnProperty.call(obj, camelName)) return obj[camelName];
    if (Object.prototype.hasOwnProperty.call(obj, pascalName)) return obj[pascalName];
    return fallback;
}

function unwrapHoaDonInfo(raw) {
    const value = raw && raw.duLieu ? raw.duLieu : raw;
    return value || null;
}

function reloadHoaDonPhongInfo() {
    const phongId = document.getElementById('f_maPhong')?.value;
    if (phongId) loadPhongInfo(phongId);
}

async function loadPhongInfo(phongId) {
    const infoBox = document.getElementById('phongInfoBox');
    if (!phongId) {
        if (infoBox) infoBox.style.display = 'none';
        window._hoaDonInfo = null;
        return;
    }

    try {
        const kyHoaDon = document.getElementById('f_kyHoaDon')?.value || '';
        const raw = await apiFetch(`/api/HoaDon/GetThongTinPhong/${phongId}?kyHoaDon=${encodeURIComponent(kyHoaDon)}`);
        const info = unwrapHoaDonInfo(raw);
        if (!info) throw new Error('Không có dữ liệu phòng');

        window._hoaDonInfo = info;
        if (infoBox) infoBox.style.display = 'block';

        const phong = getProp(info, 'phong', 'Phong', {});
        const nguoiThue = getProp(info, 'nguoiThue', 'NguoiThue', {});
        const tienDien = Number(getProp(info, 'tienDien', 'TienDien', 0)) || 0;
        const tienNuoc = Number(getProp(info, 'tienNuoc', 'TienNuoc', 0)) || 0;

        document.getElementById('infoNguoiThue').textContent = getProp(nguoiThue, 'hoTen', 'HoTen', '---');
        document.getElementById('infoTienPhong').textContent = fmtCurrency(getProp(phong, 'giaPhong', 'GiaPhong', 0));
        document.getElementById('infoTienDien').textContent = fmtCurrency(tienDien);
        document.getElementById('infoTienNuoc').textContent = fmtCurrency(tienNuoc);

        const warning = getProp(info, 'canhBaoDienNuoc', 'CanhBaoDienNuoc', null);
        const warningMsg = getProp(warning, 'thongBao', 'ThongBao', '');
        const warningBox = document.getElementById('hoaDonReadingWarning');
        if (warningBox) {
            warningBox.style.display = warningMsg ? 'block' : 'none';
            warningBox.innerHTML = warningMsg ? `<i class="fas fa-triangle-exclamation"></i> ${escapeHtmlDashboard(warningMsg)}` : '';
        }

        const servicesRaw = getProp(info, 'danhSachDichVu', 'DanhSachDichVu', []);
        const services = normalizeArrayResponse(servicesRaw);
        const serviceBox = document.getElementById('dichVuHoaDonBox');
        const serviceList = document.getElementById('dichVuHoaDonList');
        if (serviceList) {
            if (services.length) {
                serviceList.innerHTML = services.map(dv => {
                    const id = getProp(dv, 'maDichVu', 'MaDichVu');
                    const name = getProp(dv, 'tenDichVu', 'TenDichVu', 'Dịch vụ');
                    const price = Number(getProp(dv, 'tienDichVu', 'TienDichVu', 0)) || 0;
                    return `
                        <label style="display:flex;gap:.65rem;align-items:flex-start;padding:.75rem;border:1px solid #e5e7eb;border-radius:.75rem;background:white;cursor:pointer;">
                            <input type="checkbox" class="hoa-don-dich-vu" value="${id}" data-price="${price}" onchange="recalcTotal()" style="margin-top:.2rem;">
                            <span style="flex:1;">
                                <strong>${escapeHtmlDashboard(name)}</strong><br>
                                <small style="color:var(--text-light);">${fmtCurrency(price)}</small>
                            </span>
                        </label>`;
                }).join('');
            } else {
                serviceList.innerHTML = `<div style="color:var(--text-light);">Chủ trọ chưa khai báo dịch vụ nào.</div>`;
            }
        }
        if (serviceBox) serviceBox.style.display = services.length ? 'block' : 'none';

        onHoaDonTypeChanged();
        recalcTotal();
    } catch (e) {
        window._hoaDonInfo = null;
        if (infoBox) infoBox.style.display = 'none';
        showToast('Lỗi: ' + (e.message || 'Không tải được thông tin phòng'), 'error');
    }
}

function calcSelectedServiceTotal() {
    return Array.from(document.querySelectorAll('.hoa-don-dich-vu:checked'))
        .reduce((sum, el) => sum + (Number(el.dataset.price) || 0), 0);
}

function onHoaDonTypeChanged() {
    const loai = document.getElementById('f_loaiHoaDon')?.value || 'HangThang';
    const isMonthly = loai === 'HangThang';
    document.querySelectorAll('.hang-thang-only').forEach(el => {
        el.style.display = isMonthly ? '' : 'none';
    });
    recalcTotal();
}

function recalcTotal() {
    if (!window._hoaDonInfo) return;
    const info = window._hoaDonInfo;
    const loai = document.getElementById('f_loaiHoaDon')?.value || 'HangThang';
    const ps = Number(document.getElementById('f_tienPhatSinhKhac')?.value) || 0;
    const serviceTotal = loai === 'HangThang' ? calcSelectedServiceTotal() : 0;
    const phong = getProp(info, 'phong', 'Phong', {});
    const total = loai === 'ThuePhong'
        ? (Number(getProp(phong, 'giaPhong', 'GiaPhong', 0)) + ps)
        : (Number(getProp(info, 'tienDien', 'TienDien', 0)) + Number(getProp(info, 'tienNuoc', 'TienNuoc', 0)) + serviceTotal + ps);

    const dvEl = document.getElementById('infoTienDichVu');
    if (dvEl) dvEl.textContent = fmtCurrency(serviceTotal);
    const totalEl = document.getElementById('infoTongTien');
    if (totalEl) totalEl.textContent = fmtCurrency(total);
}


// ==========================================
// THANH TOÁN HÓA ĐƠN
// ==========================================
async function openHoaDonThanhToanModal(maHoaDon) {
    const hoaDon = (currentData || []).find(h => h.maHoaDon == maHoaDon);
    if (!hoaDon) {
        showToast('Không tìm thấy hóa đơn', 'error');
        return;
    }

    resetModalFooter();
    document.getElementById('modalTitle').textContent = `Thanh toán hóa đơn #${hoaDon.maHoaDon}`;

    let history = [];
    try {
        history = await apiFetch(`/api/ThanhToan/HoaDon/${maHoaDon}`) || [];
    } catch (e) {
        console.warn('Không tải được lịch sử thanh toán', e);
    }

    const conLai = Number(hoaDon.conLai ?? Math.max((hoaDon.tongTien || 0) - (hoaDon.daThanhToan || 0), 0));
    const hasPaymentInfo = !!(hoaDon.maNganHang && hoaDon.soTaiKhoan);
    const isOwner = CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro';

    const historyHtml = history.length ? `
        <div style="margin-top:1rem;">
            <h4 style="margin-bottom:.5rem;">Lịch sử thanh toán</h4>
            <div class="table-container" style="max-height:220px;overflow:auto;">
                <table>
                    <thead><tr><th>Ngày</th><th>Số tiền</th><th>Hình thức</th><th>Ghi chú</th></tr></thead>
                    <tbody>${history.map(t => `
                        <tr>
                            <td>${fmtDate(t.ngayThanhToan)}</td>
                            <td>${fmtCurrency(t.tongTien)}</td>
                            <td>${escapeHtmlDashboard(t.hinhThucThanhToan || '---')}</td>
                            <td>${escapeHtmlDashboard(t.ghiChu || '---')}</td>
                        </tr>`).join('')}</tbody>
                </table>
            </div>
        </div>` : '<div style="margin-top:1rem;color:var(--text-light);">Chưa có lịch sử thanh toán.</div>';

    const qrHtml = hasPaymentInfo ? `
        <div style="display:grid;grid-template-columns:220px 1fr;gap:1rem;align-items:start;margin-top:1rem;">
            <div style="text-align:center;">
                ${hoaDon.qrThanhToanUrl ? `<img src="${escapeHtmlDashboard(hoaDon.qrThanhToanUrl)}" alt="QR thanh toán" style="width:220px;max-width:100%;border-radius:.75rem;border:1px solid #e5e7eb;background:white;">` : '<div style="padding:1rem;border:1px dashed #d1d5db;border-radius:.75rem;">Chưa tạo được QR</div>'}
                <div style="font-size:.8rem;color:var(--text-light);margin-top:.5rem;">QR VietQR theo số tiền còn lại</div>
            </div>
            <div class="info-grid" style="grid-template-columns:1fr;">
                <div class="info-item"><label>Ngân hàng</label><span>${escapeHtmlDashboard(hoaDon.tenNganHang || hoaDon.maNganHang || '---')}</span></div>
                <div class="info-item"><label>Số tài khoản</label><span>${escapeHtmlDashboard(hoaDon.soTaiKhoan || '---')}</span></div>
                <div class="info-item"><label>Tên chủ tài khoản</label><span>${escapeHtmlDashboard(hoaDon.tenChuTaiKhoan || hoaDon.tenChuTro || '---')}</span></div>
                <div class="info-item"><label>Nội dung chuyển khoản</label><span>${escapeHtmlDashboard(hoaDon.noiDungChuyenKhoan || `Thanh toan hoa don ${hoaDon.maHoaDon}`)}</span></div>
            </div>
        </div>` : `
        <div style="margin-top:1rem;padding:1rem;border-radius:.75rem;background:#fff7ed;color:#9a3412;">
            <i class="fas fa-exclamation-circle"></i>
            Chủ trọ chưa cập nhật thông tin nhận thanh toán. Vui lòng liên hệ chủ trọ.
        </div>`;

    document.getElementById('modalFields').innerHTML = `
        <div style="grid-column:1/-1;">
            <div class="info-grid">
                <div class="info-item"><label>Phòng</label><span>${escapeHtmlDashboard(hoaDon.tenPhong || '---')}</span></div>
                <div class="info-item"><label>Khách thuê</label><span>${escapeHtmlDashboard(hoaDon.tenNguoiThue || '---')}</span></div>
                <div class="info-item"><label>Kỳ hóa đơn</label><span>${escapeHtmlDashboard(hoaDon.kyHoaDon || '---')}</span></div>
                <div class="info-item"><label>Tổng tiền</label><span>${fmtCurrency(hoaDon.tongTien)}</span></div>
                <div class="info-item"><label>Đã thanh toán</label><span>${fmtCurrency(hoaDon.daThanhToan || 0)}</span></div>
                <div class="info-item info-total"><label>Còn lại</label><span>${fmtCurrency(conLai)}</span></div>
            </div>
            ${qrHtml}
            ${historyHtml}
        </div>
        ${isOwner && conLai > 0 ? `
            <div class="form-group">
                <label>Số tiền ghi nhận (đ)</label>
                <input type="number" id="f_payAmount" class="form-control" value="${conLai}" min="1" max="${conLai}" required>
            </div>
            <div class="form-group">
                <label>Hình thức thanh toán</label>
                <select id="f_payMethod" class="form-control" required>
                    <option value="Chuyển khoản">Chuyển khoản</option>
                    <option value="Tiền mặt">Tiền mặt</option>
                </select>
            </div>
            <div class="form-group" style="grid-column:1/-1;">
                <label>Ghi chú</label>
                <input type="text" id="f_payNote" class="form-control" value="Thanh toán hóa đơn ${hoaDon.maHoaDon}">
            </div>` : ''}
    `;

    const footer = document.querySelector('#universalModal .modal-footer');
    if (footer) {
        footer.innerHTML = `
            <button type="button" class="btn btn-secondary" style="width:auto;" onclick="closeModal()">Đóng</button>
            ${isOwner && conLai > 0 ? `<button type="submit" class="btn btn-primary" style="width:auto;"><i class="fas fa-check"></i> Ghi nhận thanh toán</button>` : ''}`;
    }

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        if (!isOwner || conLai <= 0) return;

        const amount = Number(document.getElementById('f_payAmount').value || 0);
        if (amount <= 0 || amount > conLai) {
            showToast('Số tiền thanh toán không hợp lệ', 'error');
            return;
        }

        try {
            await apiFetch('/api/ThanhToan', 'POST', {
                maHoaDon: hoaDon.maHoaDon,
                maNguoiThue: hoaDon.maNguoiThue,
                tongTien: amount,
                hinhThucThanhToan: document.getElementById('f_payMethod').value,
                ghiChu: document.getElementById('f_payNote').value
            });
            showToast('Ghi nhận thanh toán thành công!');
            closeModal();
            refreshData();
            loadLookups();
        } catch (err) {
            showToast(err.message || 'Lỗi ghi nhận thanh toán', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

// ==========================================
// USER CUSTOM MODAL
// ==========================================
async function openUserModal(id = null) {
    resetModalFooter();
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
// KHÁCH THUÊ DETAIL MODAL
// ==========================================
function safeText(value) {
    return value === null || value === undefined || value === '' ? '---' : String(value);
}

function imageBox(url, label) {
    if (!url) {
        return `<div style="padding:1rem;border:1px dashed #d1d5db;border-radius:.75rem;color:var(--text-light);text-align:center;">Chưa có ${label.toLowerCase()}</div>`;
    }

    return `<a href="${url}" target="_blank" title="Mở ảnh ${label}">
        <img src="${url}" alt="${label}" style="width:100%;max-height:260px;object-fit:contain;border-radius:.75rem;background:#f8fafc;border:1px solid #e5e7eb;">
    </a>`;
}

async function viewNguoiThueDetail(id) {
    try {
        const nt = await apiFetch(`/api/NguoiThue/${id}`);
        const group = window.nguoiThueGroupMap?.[id];
        const phong = lookups.phong.find(p => Number(p.maPhong) === Number(nt.maPhong));
        const nhaTro = phong ? lookups.nhatro.find(n => Number(n.maNhaTro) === Number(phong.maNhaTro)) : null;
        const rooms = group?.danhSachPhong?.length ? group.danhSachPhong : [{
            maNguoiThue: nt.maNguoiThue,
            maPhong: nt.maPhong,
            tenPhong: phong?.tenPhong || ('Phòng #' + nt.maPhong),
            tenNhaTro: nhaTro?.tenNhaTro || '',
            label: `${phong?.tenPhong || ('Phòng #' + nt.maPhong)}${nhaTro?.tenNhaTro ? ' - ' + nhaTro.tenNhaTro : ''}`
        }];

        document.getElementById('modalTitle').textContent = 'Thông tin chi tiết khách thuê';
        document.getElementById('modalFields').innerHTML = `
            <div style="grid-column:1/-1;display:grid;gap:1.25rem;">
                <div>
                    <h3 style="font-size:1rem;font-weight:800;margin-bottom:.75rem;color:var(--text);"><i class="fas fa-user"></i> Thông tin cá nhân</h3>
                    <div class="info-grid">
                        <div class="info-item"><label>Họ tên</label><span>${safeText(nt.hoTen)}</span></div>
                        <div class="info-item"><label>CCCD/CMND</label><span>${safeText(nt.cccd)}</span></div>
                        <div class="info-item"><label>Số điện thoại</label><span>${safeText(nt.sdt)}</span></div>
                        <div class="info-item"><label>Email</label><span>${safeText(nt.email)}</span></div>
                        <div class="info-item"><label>Ngày sinh</label><span>${fmtDate(nt.ngaySinh)}</span></div>
                        <div class="info-item"><label>Giới tính</label><span>${safeText(nt.gioiTinh)}</span></div>
                        <div class="info-item"><label>Quốc tịch</label><span>${safeText(nt.quocTich || 'Việt Nam')}</span></div>
                        <div class="info-item"><label>Nơi công tác</label><span>${safeText(nt.noiCongTac)}</span></div>
                        <div class="info-item" style="grid-column:1/-1;"><label>Địa chỉ</label><span>${safeText(nt.diaChi)}</span></div>
                    </div>
                </div>

                <div>
                    <h3 style="font-size:1rem;font-weight:800;margin-bottom:.75rem;color:var(--text);"><i class="fas fa-home"></i> Thông tin thuê phòng</h3>
                    <div class="info-grid">
                        <div class="info-item"><label>Số phòng thuê</label><span>${rooms.length}</span></div>
                        <div class="info-item"><label>Mã tài khoản liên kết</label><span>${safeText(nt.maNguoiDung)}</span></div>
                        <div class="info-item" style="grid-column:1/-1;">
                            <label>Danh sách phòng</label>
                            <span>${rooms.map(r => `• ${safeText(r.label)} <small style="color:var(--text-light);">(Hồ sơ #${safeText(r.maNguoiThue)})</small>`).join('<br>')}</span>
                        </div>
                    </div>
                </div>

                <div>
                    <h3 style="font-size:1rem;font-weight:800;margin-bottom:.75rem;color:var(--text);"><i class="fas fa-id-card"></i> Ảnh CCCD/CMND</h3>
                    <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(240px,1fr));gap:1rem;">
                        <div>
                            <label style="display:block;font-weight:700;margin-bottom:.5rem;">Mặt trước</label>
                            ${imageBox(nt.anhCccdMatTruoc, 'CCCD mặt trước')}
                        </div>
                        <div>
                            <label style="display:block;font-weight:700;margin-bottom:.5rem;">Mặt sau</label>
                            ${imageBox(nt.anhCccdMatSau, 'CCCD mặt sau')}
                        </div>
                    </div>
                </div>
            </div>`;

        const footer = document.querySelector('#universalModal .modal-footer');
        if (footer) {
            footer.innerHTML = `<button type="button" class="btn btn-secondary" style="width:auto;" onclick="closeModal()">Đóng</button>`;
        }

        document.getElementById('universalModal').style.display = 'flex';
    } catch (e) {
        showToast(e.message || 'Không tải được chi tiết khách thuê', 'error');
    }
}

async function deleteNguoiThueDisplayGroup(id) {
    const group = window.nguoiThueGroupMap?.[id];
    const ids = group?.danhSachMaNguoiThue?.length ? group.danhSachMaNguoiThue : [id];

    const message = ids.length > 1
        ? `Khách thuê này đang có ${ids.length} hồ sơ thuê phòng trong danh sách của bạn. Bạn có muốn xóa các hồ sơ có thể xóa không?`
        : 'Bạn có chắc chắn muốn xóa khách thuê này?';

    if (!confirm(message)) return;

    let success = 0;
    const errors = [];

    for (const maNguoiThue of ids) {
        try {
            await apiFetch(`/api/NguoiThue/${maNguoiThue}`, 'DELETE');
            success++;
        } catch (e) {
            errors.push(e.message || `Không xóa được hồ sơ #${maNguoiThue}`);
        }
    }

    if (success > 0) showToast(`Đã xóa ${success} hồ sơ khách thuê`, 'success');
    if (errors.length > 0) showToast(errors[0], 'error');

    await loadLookups();
    refreshData();
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
async function startDashboard() {
    if (window.__dashboardStarted) return;
    window.__dashboardStarted = true;

    await loadLookups();

    const firstSection = normalizeSectionFromHash();
    showSection(firstSection, null, true);

    window.addEventListener('hashchange', () => {
        const section = normalizeSectionFromHash();
        if (section !== currentSection) {
            showSection(section, null, true);
        }
    });
}

window.startDashboard = startDashboard;

if (!window.__USING_MODULE_LOADER) {
    startDashboard();
}
