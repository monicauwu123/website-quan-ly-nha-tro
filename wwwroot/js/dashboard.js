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
window.CURRENT_ROLE = CURRENT_ROLE;
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

function applySidebarCollapsePreference() {
    const collapsed = localStorage.getItem('sidebarCollapsed') === '1';
    document.body.classList.toggle('sidebar-collapsed', collapsed);
}

function toggleSidebarCollapse() {
    const collapsed = !document.body.classList.contains('sidebar-collapsed');
    document.body.classList.toggle('sidebar-collapsed', collapsed);
    localStorage.setItem('sidebarCollapsed', collapsed ? '1' : '0');
}
window.toggleSidebarCollapse = toggleSidebarCollapse;
applySidebarCollapsePreference();

// --- STATE ---
let currentSection = 'overview';
let currentSubSection = 'dien';
let selectedDienNuocNhaTroId = null;
let selectedRoomHouseId = null;
let currentData = [];
let lookups = { nhatro: [], loaiphong: [], trangthai: [], phong: [], nguoithue: [], dichvu: [], hoadon: [], hinhthuc: [] };
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
        if (res.status === 204) return true;
        const text = await res.text();
        if (!text) return true;
        let json;
        try { json = JSON.parse(text); } catch { return true; }
        if (!res.ok) {
            const msg = extractApiErrorMessage(json) || `Lỗi HTTP ${res.status}`;
            throw new Error(msg);
        }
        if (json && json.thanhCong === false) {
            throw new Error(extractApiErrorMessage(json) || 'Lỗi xử lý yêu cầu');
        }
        if (['POST', 'PUT', 'DELETE'].includes(method) && typeof window.refreshSidebarBadges === 'function') {
            setTimeout(() => window.refreshSidebarBadges(), 250);
        }

        if (method === 'DELETE') {
            return json;
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

function parseJsonArraySafe(value) {
    if (Array.isArray(value)) return value;
    if (!value) return [];
    try {
        const parsed = JSON.parse(value);
        return Array.isArray(parsed) ? parsed : [];
    } catch {
        return String(value).split(',').map(x => x.trim()).filter(Boolean);
    }
}
window.parseJsonArraySafe = parseJsonArraySafe;

function getImageListFromEntity(item) {
    const images = parseJsonArraySafe(item?.danhSachHinhAnh);
    if (item?.hinhAnh && !images.includes(item.hinhAnh)) images.unshift(item.hinhAnh);
    return images.filter(Boolean);
}
window.getImageListFromEntity = getImageListFromEntity;

function getServiceIdsFromItem(item) {
    return parseJsonArraySafe(item?.dichVuGanPhong).map(Number).filter(Number.isFinite);
}
window.getServiceIdsFromItem = getServiceIdsFromItem;

function servicesForRoom(item, loaiDichVu = null) {
    const ids = new Set(getServiceIdsFromItem(item));
    const houseId = Number(item?.maNhaTro || 0);
    return normalizeArrayResponse(lookups.dichvu).filter(dv => {
        if (loaiDichVu && dv.loaiDichVu !== loaiDichVu) return false;
        if (ids.has(Number(dv.maDichVu))) return true;
        return houseId
            && Number(dv.maNhaTro) === houseId
            && (dv.loaiDichVu === 'TienIch' || dv.loaiDichVu === 'TinhPhi');
    });
}
window.servicesForRoom = servicesForRoom;

function renderServiceBadges(item, loaiDichVu = null) {
    const services = servicesForRoom(item, loaiDichVu);
    if (!services.length) return '<span style="color:var(--text-light);">---</span>';
    return services.slice(0, 6).map(dv =>
        `<span class="badge badge-blue" style="margin:0 .25rem .25rem 0;">${escapeHtmlDashboard(dv.tenDichVu || '')}</span>`
    ).join('') + (services.length > 6 ? `<span style="color:var(--text-light);font-size:.8rem;">+${services.length - 6}</span>` : '');
}
window.renderServiceBadges = renderServiceBadges;

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
        apiFetch('/api/DichVu'),
    ]);
    lookups.nhatro = normalizeArrayResponse(results[0].value);
    lookups.loaiphong = normalizeArrayResponse(results[1].value);
    lookups.trangthai = normalizeArrayResponse(results[2].value);
    if (lookups.trangthai.length === 0) {
        console.warn('Không tải được danh sách trạng thái từ API /api/TrangThai');
    }
    lookups.phong = normalizeArrayResponse(results[3].value);
    lookups.nguoithue = normalizeArrayResponse(results[4].value);
    lookups.dichvu = normalizeArrayResponse(results[5].value);

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
        'phong-cua-toi': 'phongdangthue',
        'phong-dang-thue': 'phongdangthue',
        'loai-phong': 'loaiphong',
        'dich-vu': 'dichvu',
        'dang-ky-dich-vu': 'dangkydichvu',
        'khach-thue': 'nguoithue',
        'hop-dong': 'hopdong',
        'hoa-don': 'hoadon',
        'thanh-toan': 'thanhtoan',
        'dien-nuoc': 'diennuoc',
        'yeu-cau-thue': 'yeucauthue',
        'yeuCauTongHop': 'yeucauthue',
        'yeu-cau-tong-hop': 'yeucauthue',
        'bao-cao-su-co': 'baocaosuco',
        'thong-bao': 'thongbao',
        'bien-lai': 'bienlai',
        'bien-lai-cho-duyet': 'bienlai',
        'nguoi-dung': 'user',
        'tai-khoan': 'taikhoan'
    };

    return aliases[raw] || raw;
}

function sectionToHash(section) {
    const map = {
        nhatro: 'nha-tro',
        phong: 'phong',
        phongdangthue: 'phong-cua-toi',
        loaiphong: 'loai-phong',
        dichvu: 'dich-vu',
        dangkydichvu: 'dang-ky-dich-vu',
        nguoithue: 'khach-thue',
        hopdong: 'hop-dong',
        hoadon: 'hoa-don',
        thanhtoan: 'thanh-toan',
        diennuoc: 'dien-nuoc',
        yeucauthue: 'yeu-cau-thue',
        baocaosuco: 'bao-cao-su-co',
        thongbao: 'thong-bao',
        bienlai: 'bien-lai',
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
    if (CURRENT_ROLE === 'NguoiDung' && section === 'nhatro') {
        section = 'phong';
    }
    currentSection = section;

    if (typeof window.dismissSidebarBadgeForSection === 'function') {
        window.dismissSidebarBadgeForSection(section);
    }

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

    // Quyền hiển thị nút "Thêm mới" theo từng nghiệp vụ.
    // Báo cáo sự cố: chỉ Người dùng/khách thuê được tạo; Chủ trọ/Admin chỉ xem và xử lý.
    const canCreate =
        ((CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') && !['yeucauthue', 'baocaosuco', 'dangkydichvu', 'phongdangthue', 'thongbao', 'bienlai'].includes(section))
        || (CURRENT_ROLE === 'NguoiDung' && ['yeucauthue', 'dangkydichvu', 'baocaosuco'].includes(section));
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
        sectionTitle.textContent = 'Điện & Nước';
        updateDienNuocAddButton();
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
    else if (section === 'bienlai') {
        if (sectionTitle) sectionTitle.textContent = 'Biên lai chờ xác nhận';
        if (addBtn) addBtn.style.display = 'none';
        const generic = document.getElementById('genericSection');
        if (generic) {
            generic.innerHTML = `
                <div class="section-header" style="margin-bottom:1rem;">
                    <div>
                        <h2 class="section-title"><i class="fas fa-receipt"></i> Biên lai chờ xác nhận</h2>
                        <p class="section-subtitle">Danh sách biên lai người thuê đã gửi, cần xem xét và xác nhận</p>
                    </div>
                    <button class="btn btn-secondary" onclick="renderBienLaiChoXacNhan()">
                        <i class="fas fa-sync-alt"></i> Làm mới
                    </button>
                </div>
                <div id="bienLaiContainer" class="data-card" style="padding:1rem;">
                    <div style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>
                </div>`;
            if (typeof window.renderBienLaiChoXacNhan === 'function') {
                window.renderBienLaiChoXacNhan();
            }
        }
    }
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

// ==========================================
// OVERVIEW  –  Chủ trọ / Admin
// ==========================================
function renderChuTroAdminOverview(data) {
    document.getElementById('sectionTitle').textContent =
        CURRENT_ROLE === 'ChuTro' ? 'Tổng quan nhà trọ của tôi' : 'Tổng quan hệ thống';

    const now = new Date();
    const thangNam = `Tháng ${now.getMonth() + 1}/${now.getFullYear()}`;

    // ── KPI Cards ──────────────────────────────────────────────────────────
    const tyLePhong = data.tongPhong ? Math.round((data.phongDangThue / data.tongPhong) * 100) : 0;

    document.getElementById('dashStatsGrid').innerHTML = `
        <div class="stat-card stat-card-indigo kpi-card" onclick="showSection('phong')">
            <div class="stat-icon"><i class="fas fa-building"></i></div>
            <div class="stat-info">
                <h3>Tổng số phòng</h3>
                <div class="value">${data.tongPhong ?? 0}</div>
                <div class="kpi-sub">${data.tongNhaTro ?? 0} nhà trọ</div>
            </div>
        </div>
        <div class="stat-card stat-card-blue kpi-card" onclick="showSection('phong')">
            <div class="stat-icon"><i class="fas fa-door-closed"></i></div>
            <div class="stat-info">
                <h3>Đang cho thuê</h3>
                <div class="value">${data.phongDangThue ?? 0}</div>
                <div class="kpi-bar-wrap"><div class="kpi-bar kpi-bar-blue" style="width:${tyLePhong}%"></div></div>
                <div class="kpi-sub">${tyLePhong}% công suất</div>
            </div>
        </div>
        <div class="stat-card stat-card-green kpi-card" onclick="showSection('phong')">
            <div class="stat-icon"><i class="fas fa-door-open"></i></div>
            <div class="stat-info">
                <h3>Phòng trống</h3>
                <div class="value">${data.phongTrong ?? 0}</div>
                <div class="kpi-sub">${data.tongKhachThue ?? 0} khách thuê</div>
            </div>
        </div>
        <div class="stat-card stat-card-green kpi-card revenue-card" onclick="showSection('thanhtoan')">
            <div class="stat-icon" style="background:#d1fae5;color:#065f46;"><i class="fas fa-coins"></i></div>
            <div class="stat-info">
                <h3>Doanh thu ${thangNam}</h3>
                <div class="value" style="font-size:1.25rem;">${fmtCurrency(data.doanhThuThang ?? 0)}</div>
                <div class="kpi-sub">Đã thu thực tế</div>
            </div>
        </div>
    `;

    // ── Alert Cards ────────────────────────────────────────────────────────
    const alerts = [
        {
            count: data.hoaDonChuaThanhToan ?? 0,
            label: 'Hóa đơn chưa thu',
            icon: 'fa-file-invoice-dollar',
            color: 'alert-orange',
            section: 'hoadon',
            urgent: (data.hoaDonChuaThanhToan ?? 0) > 0
        },
        {
            count: data.yeuCauChoDuyet ?? 0,
            label: 'Yêu cầu chờ duyệt',
            icon: 'fa-clipboard-list',
            color: 'alert-blue',
            section: 'yeucauthue',
            urgent: (data.yeuCauChoDuyet ?? 0) > 0
        },
        {
            count: data.baoCaoMoi ?? 0,
            label: 'Sự cố chưa xử lý',
            icon: 'fa-exclamation-triangle',
            color: 'alert-red',
            section: 'baocaosuco',
            urgent: (data.baoCaoMoi ?? 0) > 0
        },
        {
            count: data.hopDongSapHetHan ?? 0,
            label: 'Hợp đồng sắp hết hạn',
            icon: 'fa-calendar-times',
            color: 'alert-purple',
            section: 'hopdong',
            urgent: (data.hopDongSapHetHan ?? 0) > 0
        }
    ];

    document.getElementById('dashAlertRow').innerHTML = `
        <div class="alert-grid">
            ${alerts.map(a => `
                <div class="alert-card ${a.color} ${a.urgent ? 'alert-urgent' : 'alert-ok'}" onclick="showSection('${a.section}')">
                    <div class="alert-icon-wrap">
                        <i class="fas ${a.icon}"></i>
                    </div>
                    <div class="alert-body">
                        <div class="alert-count">${a.count}</div>
                        <div class="alert-label">${a.label}</div>
                    </div>
                    ${a.urgent ? '<div class="alert-dot"></div>' : ''}
                </div>
            `).join('')}
        </div>
    `;

    // ── Main Grid ──────────────────────────────────────────────────────────
    const sapHet = data.danhSachHopDongSapHet || [];
    const rooms  = data.danhSachPhongGanDay || [];

    const sapHetHtml = sapHet.length
        ? sapHet.map(h => {
            const days = Math.ceil((new Date(h.ngayKetThuc) - new Date()) / 86400000);
            const urgentCls = days <= 7 ? 'badge-danger' : 'badge-warning';
            return `<div class="mini-list-item">
                <div class="mini-list-left">
                    <i class="fas fa-calendar-alt" style="color:#8b5cf6;"></i>
                    <div>
                        <div class="mini-list-title">${h.tenPhong || '---'}</div>
                        <div class="mini-list-sub">${h.tenNhaTro || ''}</div>
                    </div>
                </div>
                <span class="badge ${urgentCls}">${days} ngày</span>
            </div>`;
          }).join('')
        : '<div class="empty-state-sm"><i class="fas fa-check-circle" style="color:var(--success);"></i> Không có hợp đồng sắp hết hạn</div>';

    const roomsHtml = rooms.length
        ? rooms.slice(0, 8).map(r => {
            const cls = r.maTrangThai === 1 ? 'badge-success' : r.maTrangThai === 2 ? 'badge-danger' : 'badge-warning';
            return `<div class="mini-list-item">
                <div class="mini-list-left">
                    <i class="fas fa-door-open" style="color:var(--primary);"></i>
                    <div>
                        <div class="mini-list-title">${r.tenPhong || '---'}</div>
                        <div class="mini-list-sub">${r.tenNhaTro || ''}</div>
                    </div>
                </div>
                <div style="text-align:right;">
                    <span class="badge ${cls}">${r.trangThai || '---'}</span>
                    <div style="font-size:.78rem;color:var(--text-light);margin-top:.2rem;">${fmtCurrency(r.giaPhong)}</div>
                </div>
            </div>`;
          }).join('')
        : '<div class="empty-state-sm">Chưa có phòng nào</div>';

    // Biểu đồ doanh thu 6 tháng
    const dt6 = data.doanhThu6Thang || [];
    const maxDt = Math.max(...dt6.map(d => d.doanhThu), 1);
    const chartHtml = dt6.length ? `
        <div class="bar-chart-wrap">
            ${dt6.map(d => {
                const pct = Math.round((d.doanhThu / maxDt) * 100);
                const isCurrentMonth = d.thang === `${(new Date().getMonth()+1).toString().padStart(2,'0')}/${new Date().getFullYear()}`;
                return `<div class="bar-col">
                    <div class="bar-val">${d.doanhThu > 0 ? fmtCurrency(d.doanhThu) : ''}</div>
                    <div class="bar-outer">
                        <div class="bar-inner ${isCurrentMonth ? 'bar-current' : ''}" style="height:${Math.max(pct,2)}%"></div>
                    </div>
                    <div class="bar-label">${d.thang}</div>
                </div>`;
            }).join('')}
        </div>` : '';

    document.getElementById('dashMainGrid').innerHTML = `
        <div class="dash-main-grid">
            <div class="data-card dash-card-lg">
                <div class="dash-card-header">
                    <span><i class="fas fa-chart-bar" style="color:#6366f1;"></i> Doanh thu 6 tháng gần nhất</span>
                </div>
                ${chartHtml}
            </div>

            <div class="data-card">
                <div class="dash-card-header">
                    <span><i class="fas fa-calendar-times" style="color:#8b5cf6;"></i> Hợp đồng sắp hết hạn</span>
                    <button class="btn-link-sm" onclick="showSection('hopdong')">Xem tất cả <i class="fas fa-arrow-right"></i></button>
                </div>
                <div class="mini-list">${sapHetHtml}</div>
            </div>

            <div class="data-card">
                <div class="dash-card-header">
                    <span><i class="fas fa-door-open" style="color:var(--primary);"></i> Danh sách phòng</span>
                    <button class="btn-link-sm" onclick="showSection('phong')">Xem tất cả <i class="fas fa-arrow-right"></i></button>
                </div>
                <div class="mini-list">${roomsHtml}</div>
            </div>
        </div>
    `;
}


// ==========================================
// OVERVIEW  –  Người dùng
// ==========================================
function renderNguoiDungOverview(data) {
    document.getElementById('sectionTitle').textContent = 'Tổng quan của tôi';

    const tk = data?.taiKhoan || {};
    const phongList = data?.danhSachPhongDangThue || (data?.phongDangThue ? [data.phongDangThue] : []);
    const hopDongList = data?.danhSachHopDongHienTai || (data?.hopDongHienTai ? [data.hopDongHienTai] : []);
    const hoaDonChuaTT = data?.hoaDonChuaThanhToan || [];
    const dichVuList = data?.dichVuDangDung || [];
    const thongBaoList = data?.thongBaoChuaDoc || [];
    const baoCaoList = data?.baoCaoGanDay || [];
    const soHoaDonChuaTT = data?.soHoaDonChuaTT ?? hoaDonChuaTT.length;
    const soThongBaoChuaDoc = data?.soThongBaoChuaDoc ?? thongBaoList.length;

    // ── KPI Cards ──────────────────────────────────────────────────────────
    document.getElementById('dashStatsGrid').innerHTML = `
        <div class="stat-card stat-card-indigo kpi-card">
            <div class="stat-icon"><i class="fas fa-user-circle"></i></div>
            <div class="stat-info">
                <h3>Tài khoản</h3>
                <div class="value" style="font-size:1.1rem;line-height:1.3;">${tk.hoTen || '---'}</div>
                <div class="kpi-sub">${tk.email || '---'}</div>
            </div>
        </div>
        <div class="stat-card stat-card-blue kpi-card" onclick="showSection('phongdangthue')">
            <div class="stat-icon"><i class="fas fa-home"></i></div>
            <div class="stat-info">
                <h3>Phòng đang thuê</h3>
                <div class="value">${phongList.length}</div>
                <div class="kpi-sub">${phongList.map(p => p.tenPhong).join(', ') || 'Chưa có phòng'}</div>
            </div>
        </div>
        <div class="stat-card ${soHoaDonChuaTT > 0 ? 'stat-card-red' : 'stat-card-green'} kpi-card" onclick="showSection('hoadon')">
            <div class="stat-icon"><i class="fas fa-file-invoice"></i></div>
            <div class="stat-info">
                <h3>Hóa đơn chưa trả</h3>
                <div class="value">${soHoaDonChuaTT}</div>
                <div class="kpi-sub">${soHoaDonChuaTT > 0 ? 'Cần thanh toán' : 'Đã thanh toán hết'}</div>
            </div>
        </div>
        <div class="stat-card ${soThongBaoChuaDoc > 0 ? 'stat-card-purple' : 'stat-card-green'} kpi-card" onclick="showSection('thongbao')">
            <div class="stat-icon"><i class="fas fa-bell"></i></div>
            <div class="stat-info">
                <h3>Thông báo mới</h3>
                <div class="value">${soThongBaoChuaDoc}</div>
                <div class="kpi-sub">${soThongBaoChuaDoc > 0 ? 'Chưa đọc' : 'Đã đọc hết'}</div>
            </div>
        </div>
    `;

    // ── Alert Row ──────────────────────────────────────────────────────────
    document.getElementById('dashAlertRow').innerHTML = soHoaDonChuaTT > 0 ? `
        <div class="user-alert-banner" onclick="showSection('hoadon')">
            <i class="fas fa-exclamation-circle"></i>
            <span>Bạn có <strong>${soHoaDonChuaTT} hóa đơn</strong> chưa thanh toán — Nhấn để xem chi tiết</span>
            <i class="fas fa-arrow-right" style="margin-left:auto;"></i>
        </div>` : '';

    // ── Rooms & contracts ──────────────────────────────────────────────────
    const phongHtml = phongList.length
        ? phongList.map(p => `
            <div class="room-info-card">
                <div class="room-info-icon"><i class="fas fa-door-open"></i></div>
                <div>
                    <div class="room-info-name">${p.tenPhong || '---'}</div>
                    <div class="room-info-sub">${p.tenNhaTro || ''}</div>
                    <div class="room-info-price">${fmtCurrency(p.giaPhong)}<span>/tháng</span></div>
                </div>
            </div>`).join('')
        : '<div class="empty-state-sm"><i class="fas fa-home"></i> Chưa có phòng đang thuê</div>';

    // ── Hóa đơn chưa TT ───────────────────────────────────────────────────
    const hoaDonHtml = hoaDonChuaTT.length
        ? hoaDonChuaTT.map(h => `
            <div class="mini-list-item">
                <div class="mini-list-left">
                    <i class="fas fa-file-invoice-dollar" style="color:#f59e0b;"></i>
                    <div>
                        <div class="mini-list-title">${h.tenPhong || '---'} — ${h.kyHoaDon || ''}</div>
                        <div class="mini-list-sub">${fmtDate(h.ngayLap)}</div>
                    </div>
                </div>
                <span style="font-weight:700;color:#dc2626;">${fmtCurrency(h.tongTien)}</span>
            </div>`).join('')
        : '<div class="empty-state-sm"><i class="fas fa-check-circle" style="color:var(--success);"></i> Không có hóa đơn nợ</div>';

    // ── Dịch vụ đang dùng ─────────────────────────────────────────────────
    const dichVuHtml = dichVuList.length
        ? dichVuList.map(dv => `
            <div class="mini-list-item">
                <div class="mini-list-left">
                    <i class="fas fa-cog" style="color:#06b6d4;"></i>
                    <div>
                        <div class="mini-list-title">${dv.tenDichVu || '---'}</div>
                        <div class="mini-list-sub">${dv.tenPhong || ''}</div>
                    </div>
                </div>
                <span class="badge badge-teal">${fmtCurrency(dv.tienDichVu)}</span>
            </div>`).join('')
        : '<div class="empty-state-sm">Chưa đăng ký dịch vụ nào</div>';

    // ── Thông báo ──────────────────────────────────────────────────────────
    const thongBaoHtml = thongBaoList.length
        ? thongBaoList.map(tb => `
            <div class="mini-list-item">
                <div class="mini-list-left">
                    <i class="fas fa-bell" style="color:#8b5cf6;"></i>
                    <div>
                        <div class="mini-list-title">${tb.tieuDe || '---'}</div>
                        <div class="mini-list-sub">${fmtDate(tb.ngayTao)}</div>
                    </div>
                </div>
                <span class="badge badge-info">Mới</span>
            </div>`).join('')
        : '<div class="empty-state-sm"><i class="fas fa-check-circle" style="color:var(--success);"></i> Không có thông báo mới</div>';

    // -- Báo cáo sự cố -----------------------------------------------------
    const baoCaoHtml = baoCaoList.length
        ? baoCaoList.map(b => {
            const statusMap = { Moi: ['badge-warning','Mới gửi'], DangXuLy: ['badge-info','Đang xử lý'], DaXuLy: ['badge-success','Đã xử lý'] };
            const [cls, label] = statusMap[b.trangThai] || ['badge-secondary', b.trangThai || '---'];
            const mucDoCls = b.mucDo === 'Khẩn cấp' ? 'badge-danger' : b.mucDo === 'Cao' ? 'badge-warning' : 'badge-secondary';
            return `<div class="mini-list-item">
                <div class="mini-list-left">
                    <i class="fas fa-tools" style="color:#f59e0b;"></i>
                    <div>
                        <div class="mini-list-title">${b.tieuDe || '---'}</div>
                        <div class="mini-list-sub">${b.tenPhong || ''} · ${fmtDate(b.ngayGui)}</div>
                    </div>
                </div>
                <div style="text-align:right;display:flex;flex-direction:column;gap:.2rem;align-items:flex-end;">
                    <span class="badge ${cls}">${label}</span>
                    <span class="badge ${mucDoCls}" style="font-size:.7rem;">${b.mucDo || ''}</span>
                </div>
            </div>`;
          }).join('')
        : '<div class="empty-state-sm">Chưa có sự cố nào được gửi</div>';

    document.getElementById('dashMainGrid').innerHTML = `
        <div class="dash-user-grid">

            <div class="data-card">
                <div class="dash-card-header">
                    <span><i class="fas fa-home" style="color:var(--primary);"></i> Phòng đang thuê</span>
                    <button class="btn-link-sm" onclick="showSection('phongdangthue')">Chi tiết <i class="fas fa-arrow-right"></i></button>
                </div>
                ${phongHtml}
            </div>

            <div class="data-card">
                <div class="dash-card-header">
                    <span><i class="fas fa-file-invoice-dollar" style="color:#f59e0b;"></i> Hóa đơn chưa thanh toán</span>
                    <button class="btn-link-sm" onclick="showSection('hoadon')">Xem tất cả <i class="fas fa-arrow-right"></i></button>
                </div>
                <div class="mini-list">${hoaDonHtml}</div>
            </div>

            <div class="data-card">
                <div class="dash-card-header">
                    <span><i class="fas fa-cog" style="color:#06b6d4;"></i> Dịch vụ đang dùng</span>
                    <button class="btn-link-sm" onclick="showSection('dangkydichvu')">Xem tất cả <i class="fas fa-arrow-right"></i></button>
                </div>
                <div class="mini-list">${dichVuHtml}</div>
            </div>

            <div class="data-card">
                <div class="dash-card-header">
                    <span><i class="fas fa-bell" style="color:#8b5cf6;"></i> Thông báo chưa đọc</span>
                    <button class="btn-link-sm" onclick="showSection('thongbao')">Xem tất cả <i class="fas fa-arrow-right"></i></button>
                </div>
                <div class="mini-list">${thongBaoHtml}</div>
            </div>

            <div class="data-card">
                <div class="dash-card-header">
                    <span><i class="fas fa-tools" style="color:#f59e0b;"></i> Sự cố gần đây</span>
                    <button class="btn-link-sm" onclick="showSection('baocaosuco')">Xem tất cả <i class="fas fa-arrow-right"></i></button>
                </div>
                <div class="mini-list">${baoCaoHtml}</div>
            </div>

        </div>
    `;
}


// ==========================================
// ROOM GRID
// ==========================================
async function renderRoomGrid() {
    // ── Nút thêm mới ─────────────────────────────────────────────────
    const _addBtn = document.getElementById('addBtn');
    if (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') {
        _addBtn.style.display = 'inline-flex';
        _addBtn.onclick = () => openModal();
    } else {
        _addBtn.style.display = 'none';
    }

    const container = document.getElementById('genericSection');

    // ── Admin / ChuTro → dùng PhongTable (bảng + filter + paging) ────
    if (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') {
        if (typeof window.PhongTable === 'undefined' || typeof window.PhongTable.init !== 'function') {
            container.innerHTML = `<div class="data-card" style="padding:2rem;text-align:center;color:var(--error);">
                <i class="fas fa-exclamation-triangle" style="font-size:2rem;margin-bottom:.5rem;display:block;"></i>
                <strong>Lỗi: Module PhongTable chưa được tải.</strong><br>
                <span style="color:var(--text-light);font-size:.9rem;">Vui lòng đảm bảo đã thay file <code>wwwroot/js/modules/phong.js</code> bằng phiên bản mới nhất rồi tải lại trang.</span>
            </div>`;
            return;
        }
        await window.PhongTable.init(container);
        // Đồng bộ currentData để editItem / deleteItem vẫn hoạt động
        try {
            const raw = await apiFetch('/api/Phong');
            currentData = normalizeArrayResponse(raw);
        } catch (_) {}
        return;
    }

    // ── NguoiDung → giữ dạng card + bộ lọc đơn giản có paging ───────
    selectedRoomHouseId = null;

    container.innerHTML = `
        <!-- Browser nhà trọ cho NguoiDung -->
        <div class="room-house-browser">
            <div class="room-house-browser-head">
                <div>
                    <h2>Chọn nhà trọ để xem phòng</h2>
                    <p>Xem ảnh nhà trọ, dịch vụ và danh sách phòng còn trống.</p>
                </div>
                <button class="btn btn-secondary" onclick="selectRoomHouse(null)">
                    <i class="fas fa-border-all"></i> Tất cả nhà trọ
                </button>
            </div>
            <div id="roomHouseSelector" class="room-house-selector"></div>
        </div>

<!-- Thanh tìm kiếm / lọc -->
        <div class="data-card" style="margin-bottom:1rem;padding:1rem 1.25rem;">
            <div style="display:flex;gap:.75rem;flex-wrap:wrap;align-items:center;margin-bottom:.75rem;">
                <div style="flex:1;min-width:200px;position:relative;">
                    <i class="fas fa-search" style="position:absolute;left:1rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
                    <input type="text" id="roomSearch" class="form-control" style="padding-left:2.5rem;"
                        placeholder="Tìm tên phòng, nhà trọ, địa chỉ..." oninput="filterRooms()">
                </div>
                <button class="btn btn-secondary" onclick="filterRooms()">
                    <i class="fas fa-sync-alt"></i> Làm mới
                </button>
                <button class="btn btn-secondary" onclick="clearRoomFilters()">
                    <i class="fas fa-times-circle"></i> Xóa bộ lọc
                </button>
            </div>
            <div style="display:flex;gap:.75rem;flex-wrap:wrap;align-items:flex-end;">
                <div>
                    <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Loại phòng</label>
                    <select id="roomLoaiFilter" class="form-control" style="min-width:150px;" onchange="filterRooms()">
                        <option value="">Tất cả loại phòng</option>
${[...new Map(lookups.loaiphong.map(l => [l.tenLoaiPhong.trim().toLowerCase(), l])).values()]
                            .map(l => `<option value="${l.tenLoaiPhong.trim().toLowerCase()}">${escapeHtmlDashboard(l.tenLoaiPhong)}</option>`).join('')}
                    </select>
                </div>
                <div>
                    <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Giá từ (đ)</label>
                    <input type="number" id="roomGiaMin" class="form-control" style="width:130px;" placeholder="VD: 1000000" min="0" oninput="filterRooms()">
                </div>
                <div>
                    <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Giá đến (đ)</label>
                    <input type="number" id="roomGiaMax" class="form-control" style="width:130px;" placeholder="VD: 5000000" min="0" oninput="filterRooms()">
                </div>
                <div>
                    <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Sức chứa ≥</label>
                    <input type="number" id="roomSucChua" class="form-control" style="width:100px;" placeholder="VD: 2" min="0" oninput="filterRooms()">
                </div>
                <div>
                    <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Diện tích ≥ (m²)</label>
                    <input type="number" id="roomDienTich" class="form-control" style="width:120px;" placeholder="VD: 20" min="0" oninput="filterRooms()">
                </div>
            </div>
        </div>

        <!-- Thông tin tổng kết + chọn số dòng -->
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;margin-bottom:.75rem;">
            <span id="roomInfo" style="font-size:.875rem;color:var(--text-light);"></span>
            <div style="display:flex;align-items:center;gap:.5rem;">
                <span style="font-size:.85rem;color:var(--text-light);">Hiển thị:</span>
                <select id="roomPageSize" class="form-control" style="width:75px;padding:.35rem .5rem;" onchange="filterRooms()">
                    <option value="10">10</option>
                    <option value="20">20</option>
                    <option value="50">50</option>
                </select>
            </div>
        </div>

        <!-- Grid phòng -->
        <div id="roomGrid" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(280px,1fr));gap:1.25rem;align-items:start;justify-content:start;"></div>
        <!-- Phân trang -->
        <div id="roomPager" style="display:flex;justify-content:center;gap:.3rem;flex-wrap:wrap;margin-top:1.25rem;"></div>`;

    try {
        currentData = await apiFetch('/api/Phong');
        renderRoomHouseSelector();
        _roomCurrentPage = 1;
        filterRooms();
    } catch (e) {
        showToast('Lỗi tải danh sách phòng', 'error');
    }
}

// -- Trạng thái phân trang cho card view (NguoiDung) -----------------
let _roomCurrentPage = 1;

function clearRoomFilters() {
    ['roomSearch', 'roomLoaiFilter', 'roomGiaMin', 'roomGiaMax', 'roomSucChua', 'roomDienTich']
        .forEach(id => { const el = document.getElementById(id); if (el) el.value = ''; });
    _roomCurrentPage = 1;
    filterRooms();
}
window.clearRoomFilters = clearRoomFilters;

function filterRooms() {
    const q        = (document.getElementById('roomSearch')?.value    || '').toLowerCase();
    const lf       = document.getElementById('roomLoaiFilter')?.value  || '';
    const giaMin   = parseFloat(document.getElementById('roomGiaMin')?.value)   || 0;
    const giaMax   = parseFloat(document.getElementById('roomGiaMax')?.value)   || 0;
    const sucChua  = parseInt(document.getElementById('roomSucChua')?.value)    || 0;
    const dienTich = parseFloat(document.getElementById('roomDienTich')?.value) || 0;
    const ps       = parseInt(document.getElementById('roomPageSize')?.value)   || 10;

    let data = normalizeArrayResponse(currentData);

    // Lọc nhà trọ (NguoiDung chọn nhà)
    if (selectedRoomHouseId) data = data.filter(r => Number(r.maNhaTro) === Number(selectedRoomHouseId));

    // Tìm kiếm text
    if (q) data = data.filter(r => {
        const house = lookups.nhatro.find(n => Number(n.maNhaTro) === Number(r.maNhaTro));
        return (r.tenPhong    || '').toLowerCase().includes(q)
            || (r.diaChiPhong || '').toLowerCase().includes(q)
            || (house?.tenNhaTro || '').toLowerCase().includes(q)
            || (house?.diaChi    || '').toLowerCase().includes(q)
            || (r.moTa          || '').toLowerCase().includes(q);
    });

    // Filter loại phòng (so sánh theo tên vì mỗi nhà trọ có maLoaiPhong riêng)
    if (lf) data = data.filter(r => {
        const loai = lookups.loaiphong.find(l => l.maLoaiPhong === r.maLoaiPhong);
        return loai && loai.tenLoaiPhong.trim().toLowerCase() === lf;
    });

    // Khoảng giá
    if (giaMin > 0) data = data.filter(r => (r.giaPhong || 0) >= giaMin);
    if (giaMax > 0) data = data.filter(r => (r.giaPhong || 0) <= giaMax);

    // Sức chứa tối thiểu
    if (sucChua > 0) data = data.filter(r => (r.sucChua || 0) >= sucChua);

    // Diện tích tối thiểu
    if (dienTich > 0) data = data.filter(r => (r.dienTich || 0) >= dienTich);
    const total = data.length;
    const totalPg = Math.max(1, Math.ceil(total / ps));
    if (_roomCurrentPage > totalPg) _roomCurrentPage = 1;

    const start = (_roomCurrentPage - 1) * ps;
    const pageData = data.slice(start, start + ps);

    // Tổng kết
    const infoEl = document.getElementById('roomInfo');
    if (infoEl) {
        const from = total === 0 ? 0 : start + 1;
        const to   = Math.min(_roomCurrentPage * ps, total);
        infoEl.textContent = `Hiển thị ${from}–${to} trong tổng số ${total} kết quả`;
    }

    renderRooms(pageData);
    renderRoomPager(totalPg, ps, data);
}

function renderRoomPager(totalPg, ps, filteredData) {
    const pager = document.getElementById('roomPager');
    if (!pager) return;
    if (totalPg <= 1) { pager.innerHTML = ''; return; }

    const cp = _roomCurrentPage;
    const btn = (label, page, disabled, extraClass = '') =>
        `<button type="button"
            class="pt-page-btn ${extraClass} ${disabled ? 'disabled' : ''} ${page === cp && typeof label === 'number' ? 'active' : ''}"
            ${disabled ? 'disabled' : ''} onclick="_roomGoPage(${page})">${label}</button>`;

    const range = 2;
    let pages = [];
    for (let i = Math.max(1, cp - range); i <= Math.min(totalPg, cp + range); i++) pages.push(i);

    pager.innerHTML = `
        <div class="room-pager-inner">
        ${btn('<i class="fas fa-angles-left"></i><span>Đầu</span>', 1, cp === 1, 'pt-page-wide')}
        ${btn('<i class="fas fa-angle-left"></i><span>Trước</span>', cp - 1, cp === 1, 'pt-page-wide')}
        ${pages.map(p => btn(p, p, false)).join('')}
        ${btn('<span>Sau</span><i class="fas fa-angle-right"></i>', cp + 1, cp >= totalPg, 'pt-page-wide')}
        ${btn('<span>Cuối</span><i class="fas fa-angles-right"></i>', totalPg, cp >= totalPg, 'pt-page-wide')}
        </div>
        <div class="room-pager-meta">Trang ${cp} / ${totalPg}</div>`;
}

function _roomGoPage(p) {
    _roomCurrentPage = p;
    filterRooms();
}
window._roomGoPage = _roomGoPage;

function renderRoomHouseSelector() {
    const selector = document.getElementById('roomHouseSelector');
    if (!selector) return;

    const houses = normalizeArrayResponse(lookups.nhatro).filter(h =>
        normalizeArrayResponse(currentData).some(r => Number(r.maNhaTro) === Number(h.maNhaTro))
    );

    if (!houses.length) {
        selector.innerHTML = '<div class="empty-state-inline">Chưa có nhà trọ nào có phòng đang hiển thị.</div>';
        return;
    }

    selector.innerHTML = houses.map(house => {
        const images  = getImageListFromEntity(house);
        const count   = normalizeArrayResponse(currentData).filter(r => Number(r.maNhaTro) === Number(house.maNhaTro)).length;
        const active  = selectedRoomHouseId && Number(selectedRoomHouseId) === Number(house.maNhaTro);
        const services = normalizeArrayResponse(lookups.dichvu).filter(dv =>
            Number(dv.maNhaTro) === Number(house.maNhaTro) && dv.loaiDichVu !== 'TienNghi'
        );
        return `
            <button type="button" class="room-house-card ${active ? 'active' : ''}" onclick="selectRoomHouse(${house.maNhaTro})">
                <div class="room-house-thumb">
                    ${images[0] ? `<img src="${escapeHtmlDashboard(images[0])}" alt="" onerror="this.style.display='none'">` : `<i class="fas fa-building"></i>`}
                </div>
                <div class="room-house-body">
                    <strong>${escapeHtmlDashboard(house.tenNhaTro || 'Nhà trọ')}</strong>
                    <span>${escapeHtmlDashboard(house.diaChi || '')}</span>
                    <small>${count} phòng - ${services.length} dịch vụ/tiện ích</small>
                    <div class="room-house-badges">
                        ${services.slice(0, 4).map(x => `<span class="badge badge-green">${escapeHtmlDashboard(x.tenDichVu || '')}</span>`).join('')}
                    </div>
                </div>
                ${images.length ? `<span class="room-house-photo-count" onclick="event.stopPropagation(); openHouseGallery(${house.maNhaTro});"><i class="fas fa-images"></i> ${images.length}</span>` : ''}
            </button>`;
    }).join('');
}

function selectRoomHouse(maNhaTro) {
    selectedRoomHouseId = maNhaTro ? Number(maNhaTro) : null;
    _roomCurrentPage = 1;
    renderRoomHouseSelector();
    filterRooms();
}
window.selectRoomHouse = selectRoomHouse;

function renderRooms(rooms) {
    const grid = document.getElementById('roomGrid');
    if (!grid) return;
    if (!rooms?.length) {
        grid.innerHTML = `<p style="color:var(--text-light);text-align:center;padding:2rem;grid-column:1/-1;">
            <i class="fas fa-inbox" style="font-size:2rem;display:block;margin-bottom:.5rem;opacity:.4;"></i>
            Không tìm thấy phòng phù hợp.
        </p>`;
        return;
    }
    grid.innerHTML = rooms.map(r => {
        const status   = lookups.trangthai.find(t => t.maTrangThai === r.maTrangThai);
        const house    = lookups.nhatro.find(n => n.maNhaTro === r.maNhaTro);
        const loai     = lookups.loaiphong.find(l => l.maLoaiPhong === r.maLoaiPhong);
        const color    = r.maTrangThai === 1 ? '#22c55e' : r.maTrangThai === 2 ? '#ef4444' : '#f59e0b';
        const images   = getImageListFromEntity(r);
        const houseImages = getImageListFromEntity(house);
        const imageUrl = images[0] || houseImages[0];
        const tienIch       = servicesForRoom(r, 'TienIch');
        const tienNghi      = servicesForRoom(r, 'TienNghi');
        const dichVuTinhPhi = servicesForRoom(r, 'TinhPhi');
        return `<div class="data-card animate-fade-in" style="border-top:4px solid ${color};padding:1.25rem;display:flex;flex-direction:column;">
            ${imageUrl ? `
            <div class="room-card-image" onclick="${images.length ? `openRoomGallery(${r.maPhong})` : `openHouseGallery(${r.maNhaTro})`}">
                <img src="${escapeHtmlDashboard(imageUrl)}" onerror="this.style.display='none'">
                <span><i class="fas fa-images"></i> ${(images.length || houseImages.length)} ảnh</span>
            </div>` : ''}
            <div style="display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:0.75rem;">
                <h3 style="font-size:1.1rem;font-weight:700;flex:1;">${escapeHtmlDashboard(r.tenPhong)}</h3>
                <span class="badge" style="background:${color}20;color:${color};margin-left:0.5rem;white-space:nowrap;">${status?.tenTrangThai || '---'}</span>
            </div>
            <div style="font-size:0.875rem;color:var(--text-light);flex:1;display:flex;flex-direction:column;gap:0.4rem;margin-bottom:1rem;">
                <p><i class="fas fa-building" style="width:1.25rem;color:var(--primary);"></i> ${escapeHtmlDashboard(house?.tenNhaTro || '---')}</p>
                ${house?.diaChi ? `<p><i class="fas fa-map-marker-alt" style="width:1.25rem;color:var(--primary);"></i> ${escapeHtmlDashboard(house.diaChi)}</p>` : ''}
                ${loai ? `<p><i class="fas fa-tag" style="width:1.25rem;color:var(--primary);"></i> ${escapeHtmlDashboard(loai.tenLoaiPhong)}</p>` : ''}
                ${r.dienTich ? `<p><i class="fas fa-expand-arrows-alt" style="width:1.25rem;color:var(--primary);"></i> ${r.dienTich} m²</p>` : ''}
                <p><i class="fas fa-money-bill-wave" style="width:1.25rem;color:var(--primary);"></i> <strong style="color:var(--text);">${fmtCurrency(r.giaPhong)}</strong>/tháng</p>
                <p><i class="fas fa-users" style="width:1.25rem;color:var(--primary);"></i> Sức chứa: ${r.sucChua} người</p>
                ${tienNghi.length ? `<div><i class="fas fa-bed" style="width:1.25rem;color:var(--primary);"></i> ${tienNghi.slice(0,5).map(x=>`<span class="badge badge-blue" style="margin:.15rem;">${escapeHtmlDashboard(x.tenDichVu)}</span>`).join('')}</div>` : ''}
                ${tienIch.length ? `<div><i class="fas fa-shield-alt" style="width:1.25rem;color:var(--primary);"></i> ${tienIch.slice(0,5).map(x=>`<span class="badge badge-green" style="margin:.15rem;">${escapeHtmlDashboard(x.tenDichVu)}</span>`).join('')}</div>` : ''}
                ${dichVuTinhPhi.length ? `<div><i class="fas fa-concierge-bell" style="width:1.25rem;color:var(--primary);"></i> ${dichVuTinhPhi.slice(0,5).map(x=>`<span class="badge badge-warning" style="margin:.15rem;">${escapeHtmlDashboard(x.tenDichVu)}${x.tiendichvu?' - '+fmtCurrency(x.tiendichvu):''}</span>`).join('')}</div>` : ''}
            </div>
            ${(CURRENT_ROLE === 'NguoiDung' && houseImages.length) ? `
            <button class="btn btn-secondary" style="margin-bottom:.65rem;padding:.5rem;font-size:.875rem;" onclick="openHouseGallery(${r.maNhaTro})">
                <i class="fas fa-images"></i> Xem ảnh nhà trọ
            </button>` : ''}
            ${CURRENT_ROLE === 'NguoiDung' ? `
            <div style="display:flex;gap:0.5rem;">
                <button class="btn btn-primary" style="flex:1;padding:0.5rem;font-size:0.875rem;" onclick="openYeuCauThueModal(null, ${r.maPhong})"><i class="fas fa-paper-plane"></i> Gửi yêu cầu thuê</button>
            </div>` : ''}
        </div>`;
    }).join('');
}

function openRoomGallery(maPhong) {
    const room = normalizeArrayResponse(currentData).find(r => Number(r.maPhong) === Number(maPhong))
        || normalizeArrayResponse(lookups.phong).find(r => Number(r.maPhong) === Number(maPhong));
    const house = room ? lookups.nhatro.find(n => Number(n.maNhaTro) === Number(room.maNhaTro)) : null;
    openImageGallery(getImageListFromEntity(room), room?.tenPhong || 'Ảnh phòng', house?.tenNhaTro || '');
}
window.openRoomGallery = openRoomGallery;

function openHouseGallery(maNhaTro) {
    const house = lookups.nhatro.find(n => Number(n.maNhaTro) === Number(maNhaTro));
    openImageGallery(getImageListFromEntity(house), house?.tenNhaTro || 'Ảnh nhà trọ', house?.diaChi || '');
}
window.openHouseGallery = openHouseGallery;

function openImageGallery(images, title, subtitle) {
    const list = normalizeArrayResponse(images).filter(Boolean);
    const modal = document.getElementById('universalModal');
    const modalTitle = document.getElementById('modalTitle');
    const modalBody = document.getElementById('modalFields');
    if (!modal || !modalBody) return;

    if (modalTitle) modalTitle.textContent = title || 'Xem ảnh';
    modalBody.innerHTML = list.length ? `
        <div class="gallery-preview">
            ${subtitle ? `<p>${escapeHtmlDashboard(subtitle)}</p>` : ''}
            <img id="galleryMainImage" src="${escapeHtmlDashboard(list[0])}" alt="" onerror="this.style.display='none'">
            <div class="gallery-thumbs">
                ${list.map((url, idx) => `
                    <button type="button" class="${idx === 0 ? 'active' : ''}" onclick="setGalleryImage('${escapeHtmlDashboard(url).replaceAll("'", "\\'")}', this)">
                        <img src="${escapeHtmlDashboard(url)}" alt="" onerror="this.style.display='none'">
                    </button>`).join('')}
            </div>
        </div>` : '<div class="empty-state-inline">Chưa có ảnh để xem trước.</div>';

    const form = document.getElementById('modalForm');
    if (form) form.onsubmit = e => e.preventDefault();
    const saveBtn = document.querySelector('#universalModal .btn-primary');
    if (saveBtn) saveBtn.style.display = 'none';
    modal.style.display = 'flex';
}
window.openImageGallery = openImageGallery;

function setGalleryImage(url, button) {
    const img = document.getElementById('galleryMainImage');
    if (img) img.src = url;
    document.querySelectorAll('.gallery-thumbs button').forEach(btn => btn.classList.remove('active'));
    if (button) button.classList.add('active');
}
window.setGalleryImage = setGalleryImage;

// ==========================================
// GENERIC TABLE
// ==========================================
const genericTableState = {};

function getGenericState(section) {
    genericTableState[section] ||= {
        keyword: '',
        filterStatus: '',
        filterNhaTro: '',
        filterPhong: '',
        filterDateFrom: '',
        filterDateTo: '',
        filterMoneyFrom: '',
        filterMoneyTo: '',
        filters: {},
        sortKey: '',
        sortDir: 'asc',
        page: 1,
        pageSize: 10
    };
    return genericTableState[section];
}

function stripHtmlDashboard(value) {
    const div = document.createElement('div');
    div.innerHTML = value == null ? '' : String(value);
    return div.textContent || div.innerText || '';
}

function escapeJsStringDashboard(value) {
    return value === null || value === undefined ? '' : String(value)
        .replaceAll('\\', '\\\\')
        .replaceAll("'", "\\'")
        .replaceAll('\n', '\\n')
        .replaceAll('\r', '\\r');
}

function getGenericCellText(header, item) {
    const raw = header.key ? item[header.key] : null;
    if (!header.render) return raw == null ? '' : String(raw);
    try {
        return stripHtmlDashboard(header.render(raw, item));
    } catch {
        return raw == null ? '' : String(raw);
    }
}

function getGenericSortValue(header, item) {
    if (!header) return '';
    const raw = header.key ? item[header.key] : getGenericCellText(header, item);
    if (raw == null) return '';
    if (raw instanceof Date) return raw.getTime();
    if (typeof raw === 'number') return raw;
    if (typeof raw === 'boolean') return raw ? 1 : 0;

    const text = String(raw).trim();
    const dateMs = Date.parse(text);
    if (/^\d{4}-\d{2}-\d{2}/.test(text) && !Number.isNaN(dateMs)) return dateMs;
    const normalizedNumber = Number(text.replace(/[^\d.-]/g, ''));
    if (text && !Number.isNaN(normalizedNumber) && /[\d]/.test(text) && !/[a-zA-ZÀ-ỹ]/.test(text)) {
        return normalizedNumber;
    }
    return text.toLowerCase();
}

function getGenericCapabilities(cfg, data, section) {
    const rows = normalizeArrayResponse(data);
    const headers = cfg.headers || [];
    const keys = headers.map(h => h.key).filter(Boolean);

    const statusKey = keys.find(k => /^(trangThai|loaiDichVu|vaiTro|hinhThucThanhToan|trangThaiXacNhan)$/i.test(k))
        || keys.find(k => /trangThai|loai|vaiTro|hinhThuc/i.test(k));
    const dateKey = keys.find(k => /ngay|date|created|updated|thoiDiem/i.test(k));
    const moneyKey = keys.find(k => /tongTien|soTien|tien|gia|donGia/i.test(k));
    const hasNhaTro = section !== 'nhatro' && (
        rows.some(r => r && Object.prototype.hasOwnProperty.call(r, 'maNhaTro')) ||
        rows.some(r => r && Object.prototype.hasOwnProperty.call(r, 'maPhong'))
    );
    const hasPhong = section !== 'phong' && rows.some(r => r && Object.prototype.hasOwnProperty.call(r, 'maPhong'));

    const statusOptions = statusKey
        ? [...new Map(rows
            .map(item => {
                const header = headers.find(h => h.key === statusKey);
                const raw = item?.[statusKey];
                if (raw === null || raw === undefined || raw === '') return null;
                return [String(raw), getGenericCellText(header, item) || String(raw)];
            })
            .filter(Boolean)
        ).entries()].sort((a, b) => a[1].localeCompare(b[1], 'vi'))
        : [];

    return { statusKey, dateKey, moneyKey, hasNhaTro, hasPhong, statusOptions };
}

function getGenericFilterDefs(cfg, data) {
    return normalizeArrayResponse(cfg.filters || []).map(filter => {
        const options = [...new Map(normalizeArrayResponse(data)
            .map(row => {
                const raw = filter.getValue ? filter.getValue(row) : row?.[filter.key || filter.id];
                if (raw === null || raw === undefined || raw === '') return null;
                const value = String(raw);
                const label = filter.getLabel ? filter.getLabel(raw, row) : value;
                return [value, label || value];
            })
            .filter(Boolean)
        ).entries()].sort((a, b) => String(a[1]).localeCompare(String(b[1]), 'vi'));

        return { ...filter, options };
    }).filter(filter => filter.options.length > 0);
}

function getGenericItemNhaTroId(item) {
    if (item?.maNhaTro != null) return item.maNhaTro;
    if (item?.maPhong != null) {
        const phong = normalizeArrayResponse(window.lookups?.phong || []).find(p => Number(p.maPhong) === Number(item.maPhong));
        return phong?.maNhaTro;
    }
    return null;
}

function getGenericNumberValue(value) {
    if (value === null || value === undefined || value === '') return null;
    if (typeof value === 'number') return value;
    const n = Number(String(value).replace(/[^\d.-]/g, ''));
    return Number.isNaN(n) ? null : n;
}

function filterSortGenericData(cfg, data, section) {
    const state = getGenericState(section);
    let rows = normalizeArrayResponse(data);
    const caps = getGenericCapabilities(cfg, rows, section);
    const keyword = state.keyword.trim().toLowerCase();

    if (keyword) {
        rows = rows.filter(item =>
            cfg.headers.some(h => getGenericCellText(h, item).toLowerCase().includes(keyword))
        );
    }

    for (const filter of getGenericFilterDefs(cfg, rows)) {
        const selected = state.filters?.[filter.id];
        if (!selected) continue;
        rows = rows.filter(item => {
            const raw = filter.getValue ? filter.getValue(item) : item?.[filter.key || filter.id];
            return String(raw ?? '') === String(selected);
        });
    }

    if (!cfg.filters?.length && caps.statusKey && state.filterStatus) {
        rows = rows.filter(item => String(item?.[caps.statusKey] ?? '') === String(state.filterStatus));
    }

    if (caps.hasNhaTro && state.filterNhaTro) {
        rows = rows.filter(item => String(getGenericItemNhaTroId(item) ?? '') === String(state.filterNhaTro));
    }

    if (caps.hasPhong && state.filterPhong) {
        rows = rows.filter(item => String(item?.maPhong ?? '') === String(state.filterPhong));
    }

    if (caps.dateKey && state.filterDateFrom) {
        const from = new Date(state.filterDateFrom + 'T00:00:00').getTime();
        rows = rows.filter(item => {
            const ms = Date.parse(item?.[caps.dateKey]);
            return !Number.isNaN(ms) && ms >= from;
        });
    }

    if (caps.dateKey && state.filterDateTo) {
        const to = new Date(state.filterDateTo + 'T23:59:59').getTime();
        rows = rows.filter(item => {
            const ms = Date.parse(item?.[caps.dateKey]);
            return !Number.isNaN(ms) && ms <= to;
        });
    }

    if (caps.moneyKey && state.filterMoneyFrom) {
        rows = rows.filter(item => (getGenericNumberValue(item?.[caps.moneyKey]) ?? 0) >= Number(state.filterMoneyFrom));
    }

    if (caps.moneyKey && state.filterMoneyTo) {
        rows = rows.filter(item => (getGenericNumberValue(item?.[caps.moneyKey]) ?? 0) <= Number(state.filterMoneyTo));
    }

    if (state.sortKey) {
        const header = cfg.headers.find(h => (h.key || h.label) === state.sortKey);
        if (header) {
            const dir = state.sortDir === 'desc' ? -1 : 1;
            rows = rows.slice().sort((a, b) => {
                const va = getGenericSortValue(header, a);
                const vb = getGenericSortValue(header, b);
                if (va < vb) return -1 * dir;
                if (va > vb) return 1 * dir;
                return 0;
            });
        }
    }

    return rows;
}

function renderGenericToolbar(cfg, data, section, filteredCount) {
    const slot = document.getElementById('genericToolbarSlot');
    if (!slot) return;
    const state = getGenericState(section);
    const rows = normalizeArrayResponse(data);
    const total = rows.length;
    const caps = getGenericCapabilities(cfg, rows, section);
    const customFilters = getGenericFilterDefs(cfg, rows);
    const nhaTroList = normalizeArrayResponse(window.lookups?.nhatro || []);
    const phongList = normalizeArrayResponse(window.lookups?.phong || []);
    const visiblePhong = state.filterNhaTro
        ? phongList.filter(p => String(p.maNhaTro) === String(state.filterNhaTro))
        : phongList;
    const moneyTotal = caps.moneyKey
        ? filterSortGenericData(cfg, rows, section).reduce((sum, item) => sum + (getGenericNumberValue(item?.[caps.moneyKey]) ?? 0), 0)
        : null;

    const customFilterHtml = customFilters.map(filter => `
            <div style="min-width:180px;">
                <select class="form-control" onchange="window.GenericTable.onCustomFilter('${section}', '${escapeJsStringDashboard(filter.id)}', this.value)">
                    <option value="">Tất cả ${escapeHtmlDashboard(filter.label || filter.id).toLowerCase()}</option>
                    ${filter.options.map(([value, label]) =>
                        `<option value="${escapeHtmlDashboard(value)}" ${String(state.filters?.[filter.id] || '') === String(value) ? 'selected' : ''}>${escapeHtmlDashboard(label)}</option>`
                    ).join('')}
                </select>
            </div>`).join('');

    const statusFilter = !customFilters.length && caps.statusOptions.length ? `
            <div style="min-width:180px;">
                <select class="form-control" onchange="window.GenericTable.onStatus('${section}', this.value)">
                    <option value="">Tất cả trạng thái/loại</option>
                    ${caps.statusOptions.map(([value, label]) =>
                        `<option value="${escapeHtmlDashboard(value)}" ${String(state.filterStatus) === String(value) ? 'selected' : ''}>${escapeHtmlDashboard(label)}</option>`
                    ).join('')}
                </select>
            </div>` : '';

    const nhaTroFilter = caps.hasNhaTro && nhaTroList.length > 1 ? `
            <div style="min-width:170px;">
                <select class="form-control" onchange="window.GenericTable.onNhaTro('${section}', this.value)">
                    <option value="">Tất cả nhà trọ</option>
                    ${nhaTroList.map(n =>
                        `<option value="${n.maNhaTro}" ${String(state.filterNhaTro) === String(n.maNhaTro) ? 'selected' : ''}>${escapeHtmlDashboard(n.tenNhaTro || 'Nhà trọ #' + n.maNhaTro)}</option>`
                    ).join('')}
                </select>
            </div>` : '';

    const phongFilter = caps.hasPhong ? `
            <div style="min-width:160px;">
                <select class="form-control" onchange="window.GenericTable.onPhong('${section}', this.value)">
                    <option value="">Tất cả phòng</option>
                    ${visiblePhong.map(p =>
                        `<option value="${p.maPhong}" ${String(state.filterPhong) === String(p.maPhong) ? 'selected' : ''}>${escapeHtmlDashboard(p.tenPhong || 'Phòng #' + p.maPhong)}</option>`
                    ).join('')}
                </select>
            </div>` : '';

    const dateFilter = caps.dateKey ? `
            <div style="display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;">
                <span style="font-size:.85rem;color:var(--text-light);white-space:nowrap;">Ngày:</span>
                <input type="date" class="form-control" style="width:150px;" value="${escapeHtmlDashboard(state.filterDateFrom)}"
                    onchange="window.GenericTable.onDateFrom('${section}', this.value)">
                <span style="color:var(--text-light);">-</span>
                <input type="date" class="form-control" style="width:150px;" value="${escapeHtmlDashboard(state.filterDateTo)}"
                    onchange="window.GenericTable.onDateTo('${section}', this.value)">
            </div>` : '';

    const moneyFilter = caps.moneyKey ? `
            <div style="display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;">
                <span style="font-size:.85rem;color:var(--text-light);white-space:nowrap;">Tiền:</span>
                <input type="number" class="form-control" style="width:120px;" min="0" placeholder="Từ"
                    value="${escapeHtmlDashboard(state.filterMoneyFrom)}"
                    onchange="window.GenericTable.onMoneyFrom('${section}', this.value)">
                <span style="color:var(--text-light);">-</span>
                <input type="number" class="form-control" style="width:120px;" min="0" placeholder="Đến"
                    value="${escapeHtmlDashboard(state.filterMoneyTo)}"
                    onchange="window.GenericTable.onMoneyTo('${section}', this.value)">
            </div>` : '';

    slot.innerHTML = `
        <div class="generic-table-toolbar" style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:.75rem;">
            <div style="position:relative;flex:1;min-width:220px;max-width:420px;">
                <i class="fas fa-search" style="position:absolute;left:.85rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
                <input type="text" class="form-control" style="padding-left:2.5rem;"
                    placeholder="Tìm kiếm trong ${escapeHtmlDashboard(cfg.title || 'danh sách')}..."
                    value="${escapeHtmlDashboard(state.keyword)}"
                    oninput="window.GenericTable.onKeyword('${section}', this.value)">
            </div>
            ${customFilterHtml}
            ${statusFilter}
            ${nhaTroFilter}
            ${phongFilter}
        </div>
        <div class="generic-table-toolbar" style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:.75rem;">
            ${dateFilter}
            ${moneyFilter}
            <div style="font-size:.875rem;color:var(--text-light);">
                Tìm thấy <strong>${filteredCount}</strong> / ${total}
                ${moneyTotal !== null ? ` | Tổng: <strong>${fmtCurrency(moneyTotal)}</strong>` : ''}
            </div>
            <div style="display:flex;gap:.4rem;margin-left:auto;">
                <button class="btn btn-secondary" style="white-space:nowrap;" onclick="window.GenericTable.reset('${section}')">
                    <i class="fas fa-filter-circle-xmark"></i> Xóa lọc
                </button>
                <button class="btn btn-secondary" style="white-space:nowrap;" onclick="window.GenericTable.refresh('${section}')">
                    <i class="fas fa-rotate-right"></i> Làm mới
                </button>
            </div>
        </div>`;
}

function renderGenericPaging(total, section) {
    const slot = document.getElementById('genericPagingSlot');
    if (!slot) return;
    const state = getGenericState(section);
    const totalPages = Math.max(1, Math.ceil(total / state.pageSize));
    if (state.page > totalPages) state.page = totalPages;

    const sizeOpts = [10, 20, 50].map(size =>
        `<option value="${size}" ${state.pageSize === size ? 'selected' : ''}>${size}</option>`
    ).join('');

    const current = state.page;
    const lo = Math.max(1, current - 2);
    const hi = Math.min(totalPages, lo + 4);
    let buttons = '';
    if (totalPages > 1) {
        if (current > 1) buttons += `<button class="generic-pg-btn" onclick="window.GenericTable.onPage('${section}', ${current - 1})"><i class="fas fa-chevron-left"></i></button>`;
        if (lo > 1) buttons += `<button class="generic-pg-btn" onclick="window.GenericTable.onPage('${section}', 1)">1</button><span class="generic-pg-ell">...</span>`;
        for (let i = lo; i <= hi; i++) {
            buttons += `<button class="generic-pg-btn${i === current ? ' generic-pg-active' : ''}" onclick="window.GenericTable.onPage('${section}', ${i})">${i}</button>`;
        }
        if (hi < totalPages) buttons += `<span class="generic-pg-ell">...</span><button class="generic-pg-btn" onclick="window.GenericTable.onPage('${section}', ${totalPages})">${totalPages}</button>`;
        if (current < totalPages) buttons += `<button class="generic-pg-btn" onclick="window.GenericTable.onPage('${section}', ${current + 1})"><i class="fas fa-chevron-right"></i></button>`;
    }

    const start = total === 0 ? 0 : ((current - 1) * state.pageSize) + 1;
    const end = Math.min(current * state.pageSize, total);
    slot.innerHTML = `
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;margin-top:.75rem;">
            <div style="display:flex;align-items:center;gap:.5rem;font-size:.875rem;color:var(--text-light);">
                Hiển thị ${start}-${end} / ${total}
                <select class="form-control" style="width:auto;padding:.25rem .5rem;font-size:.875rem;"
                    onchange="window.GenericTable.onPageSize('${section}', this.value)">${sizeOpts}</select>
                dòng / trang
            </div>
            <div style="display:flex;gap:.25rem;align-items:center;">${buttons}</div>
        </div>
        <style>
            .generic-pg-btn{padding:.3rem .65rem;border:1px solid var(--border-color,#e2e8f0);border-radius:6px;background:#fff;cursor:pointer;font-size:.85rem;color:var(--text-primary,#1e293b);transition:all .15s;}
            .generic-pg-btn:hover{background:var(--primary-light,#eff6ff);border-color:var(--primary,#3b82f6);}
            .generic-pg-active{background:var(--primary,#3b82f6)!important;color:#fff!important;border-color:var(--primary,#3b82f6)!important;}
            .generic-pg-ell{padding:0 .25rem;color:var(--text-light);}
        </style>`;
}

function renderGenericHeader(cfg, section) {
    const state = getGenericState(section);
    return cfg.headers.map(h => {
        const key = h.key || h.label;
        const active = state.sortKey === key;
        const icon = active ? (state.sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down') : 'fa-sort';
        const color = active ? 'var(--primary)' : 'var(--text-light)';
        return `<th style="cursor:pointer;white-space:nowrap;user-select:none;" onclick="window.GenericTable.onSort('${section}', '${escapeJsStringDashboard(key)}')">
            ${h.label} <i class="fas ${icon}" style="color:${color};font-size:.75rem;"></i>
        </th>`;
    }).join('') + '<th>Thao tác</th>';
}

window.GenericTable = {
    onKeyword(section, value) {
        const state = getGenericState(section);
        state.keyword = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onStatus(section, value) {
        const state = getGenericState(section);
        state.filterStatus = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onCustomFilter(section, id, value) {
        const state = getGenericState(section);
        state.filters ||= {};
        state.filters[id] = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onNhaTro(section, value) {
        const state = getGenericState(section);
        state.filterNhaTro = value || '';
        state.filterPhong = '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onPhong(section, value) {
        const state = getGenericState(section);
        state.filterPhong = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onDateFrom(section, value) {
        const state = getGenericState(section);
        state.filterDateFrom = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onDateTo(section, value) {
        const state = getGenericState(section);
        state.filterDateTo = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onMoneyFrom(section, value) {
        const state = getGenericState(section);
        state.filterMoneyFrom = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onMoneyTo(section, value) {
        const state = getGenericState(section);
        state.filterMoneyTo = value || '';
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onSort(section, key) {
        const state = getGenericState(section);
        if (state.sortKey === key) {
            state.sortDir = state.sortDir === 'asc' ? 'desc' : 'asc';
        } else {
            state.sortKey = key;
            state.sortDir = 'asc';
        }
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    onPage(section, page) {
        getGenericState(section).page = page;
        renderTable(modules[section], currentData, section);
    },
    onPageSize(section, value) {
        const state = getGenericState(section);
        state.pageSize = parseInt(value) || 10;
        state.page = 1;
        renderTable(modules[section], currentData, section);
    },
    reset(section) {
        Object.assign(getGenericState(section), {
            keyword: '',
            filterStatus: '',
            filterNhaTro: '',
            filterPhong: '',
            filterDateFrom: '',
            filterDateTo: '',
            filterMoneyFrom: '',
            filterMoneyTo: '',
            filters: {},
            sortKey: '',
            sortDir: 'asc',
            page: 1,
            pageSize: 10
        });
        renderTable(modules[section], currentData, section);
    },
    refresh(section) {
        loadGenericSection(section);
    }
};

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


    let extraToolbarHtml = '';
    if (section === 'thongbao') {
        // Nút "Thêm mới" cho Admin/ChuTro
        const addBtn = document.getElementById('addBtn');
        if (addBtn && (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro')) {
            addBtn.style.display = 'inline-flex';
            addBtn.onclick = () => {
                if (typeof window.AppThongBao !== 'undefined') {
                    window.AppThongBao.openCreateModal();
                }
            };
        }

        // Mount container & gọi onLoad của module thongbao
        document.getElementById('genericSection').innerHTML = `
            <div id="thongBaoContainer" style="padding:.25rem 0;"></div>`;
        const container = document.getElementById('thongBaoContainer');

        const cfg = modules['thongbao'];
        if (cfg?.onLoad) {
            await cfg.onLoad(container);
        }
        return;
    }

    if (section === 'hoadon') {
        extraToolbarHtml = `<div id="hoaDonFilterBarWrapper"></div>`;
    }

    if (section === 'nguoithue') {
        document.getElementById('genericSection').innerHTML = `
            <div id="ntToolbarSlot"></div>
            <div class="data-card">
                <div id="ntTableSlot">
                    <div style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>
                </div>
                <div id="ntPagingSlot"></div>
            </div>`;

        try {
            const rawData = await apiFetch(cfg.endpoint);
            currentData = normalizeArrayResponse(rawData);
            const merged = mergeNguoiThueDisplayRows(currentData);
            if (window._NguoiThueSearch) {
                window._NguoiThueSearch.init(merged);
            }
        } catch (e) {
            const slot = document.getElementById('ntTableSlot');
            if (slot) slot.innerHTML = `<div style="text-align:center;color:var(--error);padding:1.5rem;">Lỗi tải dữ liệu: ${e.message}</div>`;
            showToast('Lỗi tải dữ liệu', 'error');
        }
        return;
    }

    // ── Hợp đồng dùng module Search riêng ────────────────────────────
    if (section === 'hopdong') {
        document.getElementById('genericSection').innerHTML = `
            <div id="hdToolbarSlot"></div>
            <div class="data-card">
                <div id="hdTableSlot">
                    <div style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>
                </div>
                <div id="hdPagingSlot"></div>
            </div>`;

        try {
            const rawData = await apiFetch(cfg.endpoint);
            currentData = normalizeArrayResponse(rawData);
            if (window._HopDongSearch) {
                window._HopDongSearch.init(currentData);
            }
        } catch (e) {
            const slot = document.getElementById('hdTableSlot');
            if (slot) slot.innerHTML = `<div style="text-align:center;color:var(--error);padding:1.5rem;">Lỗi tải dữ liệu: ${e.message}</div>`;
            showToast('Lỗi tải dữ liệu', 'error');
        }
        return;
    }

    // ── Hóa đơn dùng module Search riêng ─────────────────────────────
    if (section === 'hoadon') {
        document.getElementById('genericSection').innerHTML = `
            ${extraToolbarHtml}
            <div id="hdnToolbarSlot"></div>
            <div class="data-card">
                <div id="hdnTableSlot">
                    <div style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>
                </div>
                <div id="hdnPagingSlot"></div>
            </div>`;

        try {
            const rawData = await apiFetch(cfg.endpoint);
            currentData = normalizeArrayResponse(rawData);
            if (window._HoaDonSearch) {
                window._HoaDonSearch.init(currentData);
            }
        } catch (e) {
            const slot = document.getElementById('hdnTableSlot');
            if (slot) slot.innerHTML = `<div style="text-align:center;color:var(--error);padding:1.5rem;">Lỗi tải dữ liệu: ${e.message}</div>`;
            showToast('Lỗi tải dữ liệu', 'error');
        }
        return;
    }




    // ─────────────────────────────────────────────────────────────────

    document.getElementById('genericSection').innerHTML = `
        ${searchHtml}
        ${extraToolbarHtml}
        <div id="genericToolbarSlot"></div>
        <div class="data-card">
            <div class="table-container">
                <table>
                    <thead><tr id="genericTableHead">${renderGenericHeader(cfg, section)}</tr></thead>
                    <tbody id="genericTableBody">
                        <tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</td></tr>
                    </tbody>
                </table>
            </div>
            <div id="genericPagingSlot"></div>
        </div>`;

    try {
        const rawData = await apiFetch(cfg.endpoint);
        currentData = normalizeArrayResponse(rawData);
        if (section === 'hoadon' && window.HoaDonExcel) {
            window._hoaDonAllData = currentData;
            window._hoaDonCache = window._hoaDonCache || {};
            currentData.forEach(x => { window._hoaDonCache[x.maHoaDon] = x; });
            window.HoaDonExcel.attachToolbar?.();
            window.HoaDonExcel.refreshFilters?.();
        }
        renderTable(cfg, currentData, section);
        if (typeof cfg.afterRender === 'function') {
            cfg.afterRender(currentData, section).catch(err => console.warn('afterRender error:', err));
        }
    } catch (e) {
        const tbody = document.getElementById('genericTableBody');
        if (tbody) tbody.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;color:var(--error);padding:1.5rem;">Lỗi: ${e.message}</td></tr>`;
        showToast('Lỗi tải dữ liệu', 'error');
    }
}

function renderTable(cfg, data, section) {
    if (section === 'hopdong') {
        if (window._HopDongSearch) window._HopDongSearch.refresh(normalizeArrayResponse(data));
        return;
    }
    if (section === 'nguoithue') {
        if (window._NguoiThueSearch) window._NguoiThueSearch.refresh(mergeNguoiThueDisplayRows(normalizeArrayResponse(data)));
        return;
    }
    if (section === 'hoadon') {
        if (window._HoaDonSearch) window._HoaDonSearch.refresh(normalizeArrayResponse(data));
        return;
    }
    const tbody = document.getElementById('genericTableBody');
    if (!tbody) return;

    const safeData = normalizeArrayResponse(data);
    const displayData = section === 'nguoithue' ? mergeNguoiThueDisplayRows(safeData) : safeData;
    const filteredData = filterSortGenericData(cfg, displayData, section);
    const state = getGenericState(section);
    const totalPages = Math.max(1, Math.ceil(filteredData.length / state.pageSize));
    if (state.page > totalPages) state.page = totalPages;
    const start = (state.page - 1) * state.pageSize;
    const pageData = filteredData.slice(start, start + state.pageSize);

    const head = document.getElementById('genericTableHead');
    if (head) head.innerHTML = renderGenericHeader(cfg, section);
    renderGenericToolbar(cfg, displayData, section, filteredData.length);
    renderGenericPaging(filteredData.length, section);

    if (!pageData?.length) {
        tbody.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;color:var(--text-light);">Không có dữ liệu</td></tr>`;
        return;
    }
    const canWrite = (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro');

    tbody.innerHTML = pageData.map(item => {
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
            // trangThai = 'Huy' khi hóa đơn bị hủy (field từ HoaDonDto).
            // trangThaiThanhToan = 'Đã hủy' là fallback nếu server chưa build lại.
            const isHuy = item.trangThai === 'Huy' || item.trangThaiThanhToan === 'Đã hủy';
            if (isHuy) {
                actionHtml = `<span class="badge badge-red">Đã hủy</span>`;
            } else {
                actionHtml = `<button class="btn-action btn-edit" style="background:#6366f1;" onclick="HoaDonPrint.openModal(${item.maHoaDon})"><i class="fas fa-print"></i> In</button>
                    <button class="btn-action btn-edit" onclick="openHoaDonThanhToanModal(${item.maHoaDon})"><i class="fas fa-qrcode"></i> Thanh toán</button>`;
                if (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') {
                    actionHtml += `
                    <button class="btn-action btn-edit" onclick="editItem('hoadon',${item.maHoaDon})"><i class="fas fa-edit"></i> Sửa</button>
                    <button class="btn-action btn-delete" onclick="deleteItem('hoadon',${item.maHoaDon})"><i class="fas fa-trash"></i> Xóa</button>`;
                }
                if (CURRENT_ROLE === 'NguoiDung' && item.trangThai !== 'DaThanhToan') {
                    const rowJson = JSON.stringify(item).replace(/\\/g, '\\\\').replace(/'/g, "\\'");
                    actionHtml += `<button class="btn-action btn-edit" style="background:#10b981;" 
                        onclick='window.moModalGuiBienLai(${rowJson})'><i class="fas fa-receipt"></i> Gửi biên lai</button>`;
               }
            }
        } else if (section === 'phongdangthue') {
            if (CURRENT_ROLE === 'NguoiDung') {
                actionHtml = `
                    <button class="btn-action btn-edit" onclick="openDangKyDichVuModal(null, ${item.maPhong})"><i class="fas fa-concierge-bell"></i> Yêu cầu dịch vụ</button>`;
            }
        } else if (section === 'dangkydichvu') {
            if (item.trangThai === 'DangSuDung') {
                actionHtml = `<button class="btn-action btn-delete" onclick="huyDangKyDichVu(${item.maDangKyDichVu})"><i class="fas fa-times"></i> Hủy</button>`;
            }
        } else if (section === 'baocaosuco') {
            if (CURRENT_ROLE === 'NguoiDung') {
                if (item.trangThai === 'Moi') {
                    actionHtml = `<button class="btn-action btn-delete" onclick="deleteItem('baocaosuco',${item.maBaoCao})"><i class="fas fa-times"></i> Hủy</button>`;
                } else {
                    actionHtml = `<span style="color:var(--text-light);font-size:.85rem;">---</span>`;
                }
            } else if (CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro') {
                actionHtml = `<button class="btn-action btn-edit" onclick="openBaoCaoSuCoXuLyModal(${item.maBaoCao})"><i class="fas fa-clipboard-check"></i> Xử lý</button>`;
            }
        } else if (section === 'hopdong') {
            actionHtml = `<button class="btn-action" style="background:#6366f1;" onclick="HopDongPrint.openModal(${item.maHopDong})"><i class="fas fa-print"></i> In</button>`;
            if (canWrite) {
                actionHtml += `<button class="btn-action btn-edit" onclick="editItem('hopdong',${item.maHopDong})"><i class="fas fa-edit"></i> Sửa</button>`;
                if (item.trangThai === 'DangHieuLuc') {
                    actionHtml += `
                <button class="btn-action" style="background:#0f766e;" onclick="ketThucHopDong(${item.maHopDong})"><i class="fas fa-flag-checkered"></i> Kết thúc hợp đồng</button>
                <button class="btn-action btn-delete" onclick="huyHopDong(${item.maHopDong})"><i class="fas fa-ban"></i> Hủy hợp đồng</button>`;
                } else {
                    actionHtml += `
                <button class="btn-action btn-delete" onclick="deleteItem('hopdong',${item.maHopDong})"><i class="fas fa-trash"></i> Xóa</button>`;
                }
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
    if (window._NguoiThueSearch) {
        window._NguoiThueSearch.onKeyword(q || '');
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
const dienNuocTableState = {
    dien: { keyword: '', filterPhong: '', dateFrom: '', dateTo: '', sortKey: 'ngayThangDien', sortDir: 'desc', page: 1, pageSize: 10 },
    nuoc: { keyword: '', filterPhong: '', dateFrom: '', dateTo: '', sortKey: 'ngayThangNuoc', sortDir: 'desc', page: 1, pageSize: 10 }
};

function getDienNuocState(tab) {
    return dienNuocTableState[tab] || dienNuocTableState.dien;
}

function getDienNuocMetricKeys(tab) {
    return tab === 'dien'
        ? { oldKey: 'soDienCu', newKey: 'soDienMoi', priceKey: 'giaDien', totalKey: 'tienDien', dateKey: 'ngayThangDien', unit: 'kWh' }
        : { oldKey: 'soNuocCu', newKey: 'soNuocMoi', priceKey: 'giaNuoc', totalKey: 'tienNuoc', dateKey: 'ngayThangNuoc', unit: 'm³' };
}

function getDienNuocCellText(header, item) {
    const raw = header.key ? item[header.key] : null;
    if (!header.render) return raw == null ? '' : String(raw);
    try { return stripHtmlDashboard(header.render(raw, item)); }
    catch { return raw == null ? '' : String(raw); }
}

function getDienNuocSortValue(header, item, tab) {
    const metric = getDienNuocMetricKeys(tab);
    const key = header.key || header.label;
    if (key === '_tieuThu') return (Number(item[metric.newKey]) || 0) - (Number(item[metric.oldKey]) || 0);
    return getGenericSortValue(header, item);
}

function filterSortDienNuocData(tab, cfg, data) {
    const state = getDienNuocState(tab);
    const metric = getDienNuocMetricKeys(tab);
    let rows = normalizeArrayResponse(data);
    const keyword = state.keyword.trim().toLowerCase();

    if (keyword) {
        rows = rows.filter(item => cfg.headers.some(h => getDienNuocCellText(h, item).toLowerCase().includes(keyword)));
    }
    if (state.filterPhong) rows = rows.filter(item => String(item.maPhong) === String(state.filterPhong));
    if (state.dateFrom) {
        const from = new Date(state.dateFrom + 'T00:00:00').getTime();
        rows = rows.filter(item => {
            const ms = Date.parse(item[metric.dateKey]);
            return !Number.isNaN(ms) && ms >= from;
        });
    }
    if (state.dateTo) {
        const to = new Date(state.dateTo + 'T23:59:59').getTime();
        rows = rows.filter(item => {
            const ms = Date.parse(item[metric.dateKey]);
            return !Number.isNaN(ms) && ms <= to;
        });
    }
    if (state.sortKey) {
        const header = cfg.headers.find(h => (h.key || h.label) === state.sortKey);
        if (header) {
            const dir = state.sortDir === 'desc' ? -1 : 1;
            rows = rows.slice().sort((a, b) => {
                const va = getDienNuocSortValue(header, a, tab);
                const vb = getDienNuocSortValue(header, b, tab);
                if (va < vb) return -1 * dir;
                if (va > vb) return 1 * dir;
                return 0;
            });
        }
    }
    return rows;
}

function renderDienNuocToolbar(tab, cfg, data, filtered) {
    const slot = document.getElementById('dienNuocToolbar');
    if (!slot) return;
    const state = getDienNuocState(tab);
    const metric = getDienNuocMetricKeys(tab);
    const phongOptions = normalizeArrayResponse(lookups.phongDienNuoc || []);
    const totalTien = filtered.reduce((sum, item) => sum + (Number(item[metric.totalKey]) || 0), 0);
    const totalTieuThu = filtered.reduce((sum, item) => sum + Math.max((Number(item[metric.newKey]) || 0) - (Number(item[metric.oldKey]) || 0), 0), 0);

    slot.innerHTML = `
        <div style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:.75rem;">
            <div style="position:relative;flex:1;min-width:220px;max-width:420px;">
                <i class="fas fa-search" style="position:absolute;left:.85rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
                <input type="text" class="form-control" style="padding-left:2.5rem;"
                    placeholder="Tìm phòng, chỉ số, ngày ghi..."
                    value="${escapeHtmlDashboard(state.keyword)}"
                    oninput="window.DienNuocTable.onKeyword('${tab}', this.value)">
            </div>
            <div style="min-width:160px;">
                <select class="form-control" onchange="window.DienNuocTable.onPhong('${tab}', this.value)">
                    <option value="">Tất cả phòng</option>
                    ${phongOptions.map(p => `<option value="${p.maPhong}" ${String(state.filterPhong) === String(p.maPhong) ? 'selected' : ''}>${escapeHtmlDashboard(p.tenPhong || 'Phòng #' + p.maPhong)}</option>`).join('')}
                </select>
            </div>
            <div style="display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;">
                <span style="font-size:.85rem;color:var(--text-light);white-space:nowrap;">Ngày ghi:</span>
                <input type="date" class="form-control" style="width:150px;" value="${escapeHtmlDashboard(state.dateFrom)}" onchange="window.DienNuocTable.onDateFrom('${tab}', this.value)">
                <span style="color:var(--text-light);">-</span>
                <input type="date" class="form-control" style="width:150px;" value="${escapeHtmlDashboard(state.dateTo)}" onchange="window.DienNuocTable.onDateTo('${tab}', this.value)">
            </div>
        </div>
        <div style="display:flex;flex-wrap:wrap;gap:.75rem;align-items:center;margin-bottom:.75rem;font-size:.875rem;">
            <span>Tìm thấy <strong>${filtered.length}</strong> / ${normalizeArrayResponse(data).length}</span>
            <span style="color:var(--text-light);">|</span>
            <span>Tiêu thụ: <strong>${new Intl.NumberFormat('vi-VN').format(totalTieuThu)} ${metric.unit}</strong></span>
            <span style="color:var(--text-light);">|</span>
            <span>Tổng tiền: <strong>${fmtCurrency(totalTien)}</strong></span>
            <div style="display:flex;gap:.4rem;margin-left:auto;">
                <button class="btn btn-secondary" style="white-space:nowrap;" onclick="window.DienNuocTable.reset('${tab}')"><i class="fas fa-filter-circle-xmark"></i> Xóa lọc</button>
                <button class="btn btn-secondary" style="white-space:nowrap;" onclick="loadDienNuocData('${tab}')"><i class="fas fa-rotate-right"></i> Làm mới</button>
            </div>
        </div>`;
}

function renderDienNuocHead(tab, cfg) {
    const state = getDienNuocState(tab);
    return `<tr>${cfg.headers.map(h => {
        const key = h.key || h.label;
        const active = state.sortKey === key;
        const icon = active ? (state.sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down') : 'fa-sort';
        const color = active ? 'var(--primary)' : 'var(--text-light)';
        return `<th style="cursor:pointer;white-space:nowrap;user-select:none;" onclick="window.DienNuocTable.onSort('${tab}', '${escapeJsStringDashboard(key)}')">${h.label} <i class="fas ${icon}" style="color:${color};font-size:.75rem;"></i></th>`;
    }).join('')}<th>Thao tác</th></tr>`;
}

function renderDienNuocPaging(tab, total) {
    const slot = document.getElementById('dienNuocPaging');
    if (!slot) return;
    const state = getDienNuocState(tab);
    const totalPages = Math.max(1, Math.ceil(total / state.pageSize));
    if (state.page > totalPages) state.page = totalPages;
    const current = state.page;
    const start = total === 0 ? 0 : ((current - 1) * state.pageSize) + 1;
    const end = Math.min(current * state.pageSize, total);
    const sizeOpts = [10, 20, 50].map(s => `<option value="${s}" ${state.pageSize === s ? 'selected' : ''}>${s}</option>`).join('');
    let buttons = '';
    if (totalPages > 1) {
        const lo = Math.max(1, current - 2);
        const hi = Math.min(totalPages, lo + 4);
        if (current > 1) buttons += `<button class="generic-pg-btn" onclick="window.DienNuocTable.onPage('${tab}', ${current - 1})"><i class="fas fa-chevron-left"></i></button>`;
        if (lo > 1) buttons += `<button class="generic-pg-btn" onclick="window.DienNuocTable.onPage('${tab}', 1)">1</button><span class="generic-pg-ell">...</span>`;
        for (let i = lo; i <= hi; i++) buttons += `<button class="generic-pg-btn${i === current ? ' generic-pg-active' : ''}" onclick="window.DienNuocTable.onPage('${tab}', ${i})">${i}</button>`;
        if (hi < totalPages) buttons += `<span class="generic-pg-ell">...</span><button class="generic-pg-btn" onclick="window.DienNuocTable.onPage('${tab}', ${totalPages})">${totalPages}</button>`;
        if (current < totalPages) buttons += `<button class="generic-pg-btn" onclick="window.DienNuocTable.onPage('${tab}', ${current + 1})"><i class="fas fa-chevron-right"></i></button>`;
    }
    slot.innerHTML = `
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;margin-top:.75rem;">
            <div style="display:flex;align-items:center;gap:.5rem;font-size:.875rem;color:var(--text-light);">
                Hiển thị ${start}-${end} / ${total}
                <select class="form-control" style="width:auto;padding:.25rem .5rem;font-size:.875rem;" onchange="window.DienNuocTable.onPageSize('${tab}', this.value)">${sizeOpts}</select>
                dòng / trang
            </div>
            <div style="display:flex;gap:.25rem;align-items:center;">${buttons}</div>
        </div>`;
}

function renderDienNuocTable(tab, cfg, data) {
    const head = document.getElementById('dienNuocHead');
    const body = document.getElementById('dienNuocBody');
    if (!head || !body) return;

    const filtered = filterSortDienNuocData(tab, cfg, data);
    const state = getDienNuocState(tab);
    const totalPages = Math.max(1, Math.ceil(filtered.length / state.pageSize));
    if (state.page > totalPages) state.page = totalPages;
    const start = (state.page - 1) * state.pageSize;
    const pageRows = filtered.slice(start, start + state.pageSize);

    head.innerHTML = renderDienNuocHead(tab, cfg);
    renderDienNuocToolbar(tab, cfg, data, filtered);
    renderDienNuocPaging(tab, filtered.length);

    if (!pageRows.length) {
        body.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;color:var(--text-light);">Không có dữ liệu phù hợp.</td></tr>`;
        return;
    }

    body.innerHTML = pageRows.map(item => `<tr>
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
}

window.DienNuocTable = {
    onKeyword(tab, value) { const s = getDienNuocState(tab); s.keyword = value || ''; s.page = 1; renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); },
    onPhong(tab, value) { const s = getDienNuocState(tab); s.filterPhong = value || ''; s.page = 1; renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); },
    onDateFrom(tab, value) { const s = getDienNuocState(tab); s.dateFrom = value || ''; s.page = 1; renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); },
    onDateTo(tab, value) { const s = getDienNuocState(tab); s.dateTo = value || ''; s.page = 1; renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); },
    onSort(tab, key) { const s = getDienNuocState(tab); if (s.sortKey === key) s.sortDir = s.sortDir === 'asc' ? 'desc' : 'asc'; else { s.sortKey = key; s.sortDir = 'asc'; } s.page = 1; renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); },
    onPage(tab, page) { getDienNuocState(tab).page = page; renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); },
    onPageSize(tab, value) { const s = getDienNuocState(tab); s.pageSize = parseInt(value) || 10; s.page = 1; renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); },
    reset(tab) { const metric = getDienNuocMetricKeys(tab); Object.assign(getDienNuocState(tab), { keyword: '', filterPhong: '', dateFrom: '', dateTo: '', sortKey: metric.dateKey, sortDir: 'desc', page: 1, pageSize: 10 }); renderDienNuocTable(tab, tab === 'dien' ? dienModule : nuocModule, currentData); }
};

function renderDienNuocSection() {
    const container = document.getElementById('genericSection');
    const nhaTroOptions = normalizeArrayResponse(lookups.nhatro)
        .map(n => `<option value="${n.maNhaTro}" ${Number(selectedDienNuocNhaTroId) === Number(n.maNhaTro) ? 'selected' : ''}>${n.tenNhaTro || ('Nhà trọ #' + n.maNhaTro)}</option>`)
        .join('');

    container.innerHTML = `
        <div class="data-card" style="margin-bottom:1rem;">
            <div style="display:flex;gap:1rem;align-items:flex-end;flex-wrap:wrap;">
                <div class="form-group" style="min-width:280px;margin-bottom:0;">
                    <label><i class="fas fa-building"></i> Chọn nhà trọ</label>
                    <select id="dienNuocNhaTroSelect" class="form-control" onchange="onDienNuocNhaTroChange(this.value)">
                        <option value="">-- Chọn nhà trọ để xem điện nước --</option>
                        ${nhaTroOptions || '<option value="" disabled>Chưa có nhà trọ</option>'}
                    </select>
                </div>
                <div style="color:var(--text-light);font-size:.9rem;padding-bottom:.7rem;">
                    Chỉ số điện/nước sẽ được lọc theo từng nhà trọ.
                </div>
            </div>
        </div>
        <div id="dienNuocContent" style="display:${selectedDienNuocNhaTroId ? 'block' : 'none'};">
            <div style="display:flex;gap:0.5rem;margin-bottom:1.5rem;">
                <button id="tabDien" class="tab-btn${currentSubSection === 'dien' ? ' tab-active' : ''}" onclick="switchDienNuocTab('dien')"><i class="fas fa-bolt"></i> Chỉ số Điện</button>
                <button id="tabNuoc" class="tab-btn${currentSubSection === 'nuoc' ? ' tab-active' : ''}" onclick="switchDienNuocTab('nuoc')"><i class="fas fa-tint"></i> Chỉ số Nước</button>
            </div>
            <div class="data-card">
                <div id="dienNuocToolbar"></div>
                <div class="table-container">
                    <table>
                        <thead id="dienNuocHead"></thead>
                        <tbody id="dienNuocBody">
                            <tr><td style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i></td></tr>
                        </tbody>
                    </table>
                </div>
                <div id="dienNuocPaging"></div>
            </div>
        </div>
        <div id="dienNuocEmptyGuide" class="data-card" style="display:${selectedDienNuocNhaTroId ? 'none' : 'block'};text-align:center;padding:2rem;color:var(--text-light);">
            <i class="fas fa-building" style="font-size:2rem;margin-bottom:.75rem;color:var(--primary);"></i>
            <div>Vui lòng chọn nhà trọ trước, sau đó danh sách chỉ số điện/nước mới xuất hiện.</div>
        </div>`;

    updateDienNuocAddButton();
    if (selectedDienNuocNhaTroId) loadDienNuocData(currentSubSection || 'dien');
}

function onDienNuocNhaTroChange(value) {
    selectedDienNuocNhaTroId = value ? Number(value) : null;
    lookups.phongDienNuoc = selectedDienNuocNhaTroId
        ? normalizeArrayResponse(lookups.phong).filter(p => Number(p.maNhaTro) === Number(selectedDienNuocNhaTroId))
        : [];
    renderDienNuocSection();
}
window.onDienNuocNhaTroChange = onDienNuocNhaTroChange;

function updateDienNuocAddButton() {
    const addBtn = document.getElementById('addBtn');
    if (!addBtn) return;

    const canWrite = CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro';
    addBtn.style.display = canWrite && selectedDienNuocNhaTroId ? 'inline-flex' : 'none';
    addBtn.onclick = () => openDienNuocModal();
}

async function switchDienNuocTab(tab) {
    document.getElementById('tabDien').className = 'tab-btn' + (tab === 'dien' ? ' tab-active' : '');
    document.getElementById('tabNuoc').className = 'tab-btn' + (tab === 'nuoc' ? ' tab-active' : '');
    await loadDienNuocData(tab);
}

async function loadDienNuocData(tab) {
    currentSubSection = tab;
    updateDienNuocAddButton();
    const cfg = tab === 'dien' ? dienModule : nuocModule;

    const head = document.getElementById('dienNuocHead');
    const body = document.getElementById('dienNuocBody');
    if (!head || !body) return;

    if (!selectedDienNuocNhaTroId) {
        currentData = [];
        updateDienNuocAddButton();
        return;
    }

    lookups.phongDienNuoc = normalizeArrayResponse(lookups.phong).filter(p => Number(p.maNhaTro) === Number(selectedDienNuocNhaTroId));

    head.innerHTML = renderDienNuocHead(tab, cfg);
    body.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</td></tr>`;

    try {
        currentData = normalizeArrayResponse(await apiFetch(`${cfg.endpoint}/nha-tro/${selectedDienNuocNhaTroId}`));
        renderDienNuocTable(tab, cfg, currentData);
    } catch (e) {
        body.innerHTML = `<tr><td colspan="${cfg.headers.length + 1}" style="text-align:center;color:var(--error);">Lỗi: ${e.message}</td></tr>`;
        showToast('Lỗi tải dữ liệu', 'error');
    }
}

function openDienNuocModal(id = null) {
    if (!selectedDienNuocNhaTroId) {
        showToast('Vui lòng chọn nhà trọ trước khi thêm chỉ số điện/nước', 'error');
        return;
    }
    lookups.phongDienNuoc = normalizeArrayResponse(lookups.phong).filter(p => Number(p.maNhaTro) === Number(selectedDienNuocNhaTroId));
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
    if (!confirm('Bạn có chắc chắn muốn xóa? Dữ liệu có thể được xóa mềm nếu đã phát sinh lịch sử.')) return;
    const cfg = tab === 'dien' ? dienModule : nuocModule;
    try {
        const result = await apiFetch(`${cfg.endpoint}/${id}`, 'DELETE');
        showToast(result?.thongBao || 'Đã xử lý yêu cầu xóa!');
        refreshData();
        loadLookups();
        return;
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
        if (f.type === 'optionsMap') {
            return `<div class="form-group">
                <label for="f_${f.id}">${f.label}${f.required ? ' <span style="color:var(--error)">*</span>' : ''}</label>
                <select id="f_${f.id}" class="form-control" ${f.required ? 'required' : ''}>
                    ${f.options.map(o => `<option value="${o.value}" ${displayVal === o.value ? 'selected' : ''}>${o.label}</option>`).join('')}
                </select>
            </div>`;
        }
        if (f.type === 'hiddenJsonArray') {
            return `<input type="hidden" id="f_${f.id}" value='${escapeHtmlDashboard(displayVal)}'>`;
        }
        if (f.type === 'serviceCheckboxes') {
            const selectedIds = new Set(parseJsonArraySafe(displayVal).map(Number));
            const currentHouseId = Number(item.maNhaTro || 0);
            const services = normalizeArrayResponse(lookups.dichvu)
                .filter(dv => dv.loaiDichVu === 'TienIch' || dv.loaiDichVu === 'TienNghi')
                .filter(dv => !currentHouseId || Number(dv.maNhaTro) === currentHouseId);
            return `<div class="form-group" style="grid-column:1/-1;">
                <label>${f.label}</label>
                <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(190px,1fr));gap:.45rem;padding:.75rem;border:1px solid #e5e7eb;border-radius:.65rem;background:#f8fafc;">
                    ${services.length ? services.map(dv => {
                        const typeLabel = dv.loaiDichVu === 'TienIch' ? 'Tiện ích' : 'Tiện nghi';
                        const house = lookups.nhatro.find(n => Number(n.maNhaTro) === Number(dv.maNhaTro));
                        return `<label style="display:flex;gap:.5rem;align-items:flex-start;font-size:.9rem;">
                            <input type="checkbox" name="f_${f.id}" value="${dv.maDichVu}" ${selectedIds.has(Number(dv.maDichVu)) ? 'checked' : ''} style="margin-top:.2rem;">
                            <span><strong>${escapeHtmlDashboard(dv.tenDichVu)}</strong><br><small style="color:var(--text-light);">${typeLabel}${house ? ' - ' + escapeHtmlDashboard(house.tenNhaTro) : ''}</small></span>
                        </label>`;
                    }).join('') : '<span style="color:var(--text-light);">Chưa có tiện ích/tiện nghi. Hãy thêm trong mục Dịch vụ.</span>'}
                </div>
            </div>`;
        }
        if (f.type === 'textarea') {
            return `<div class="form-group" style="grid-column:1/-1;">
                <label for="f_${f.id}">${f.label}${f.required ? ' <span style="color:var(--error)">*</span>' : ''}</label>
                <textarea id="f_${f.id}" class="form-control" ${f.required ? 'required' : ''}>${displayVal}</textarea>
            </div>`;
        }
        if (f.type === 'fileMultiple') {
            const urls = parseJsonArraySafe(displayVal || item.danhSachHinhAnh || item.hinhAnh);
            return `<div class="form-group" style="grid-column:1/-1;">
                <label for="f_${f.id}">${f.label}</label>
                ${urls.length ? `<div style="display:flex;gap:.5rem;flex-wrap:wrap;margin-bottom:.6rem;">${urls.map(url => `<img src="${url}" style="width:86px;height:58px;object-fit:cover;border-radius:6px;border:1px solid #e5e7eb;" onerror="this.style.display='none'">`).join('')}</div>` : ''}
                <input type="file" id="f_${f.id}" class="form-control" accept="image/jpeg,image/png,image/webp" multiple>
                <small style="color:var(--text-light);display:block;margin-top:.35rem;">JPG, PNG hoặc WEBP, tối đa 5MB mỗi ảnh.</small>
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

    const updateNgayKetThucThue = () => {
        const start = document.getElementById('f_ngayBatDau')?.value;
        const months = document.getElementById('f_soThangThue')?.value;
        const endInput = document.getElementById('f_ngayKetThuc');
        if (start && months && endInput) endInput.value = tinhNgayKetThucTheoSoThang(start, months);
    };
    document.getElementById('f_ngayBatDau')?.addEventListener('change', updateNgayKetThucThue);
    document.getElementById('f_soThangThue')?.addEventListener('input', updateNgayKetThucThue);

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const payload = {};

        // Handle image uploads first if any
        for (const f of fields.filter(x => x.type === 'fileMultiple')) {
            const fileEl = document.getElementById(`f_${f.id}`);
            if (!fileEl || fileEl.files.length === 0) continue;

            try {
                const uploader = API[f.uploadTarget || 'phong']?.uploadImage;
                if (!uploader) throw new Error('Không tìm thấy API upload ảnh');

                showToast('Đang tải ảnh lên...', 'info');
                const oldUrls = parseJsonArraySafe(document.getElementById('f_danhSachHinhAnh')?.value || item.danhSachHinhAnh || item.hinhAnh);
                const newUrls = [];

                for (const file of Array.from(fileEl.files)) {
                    const uploadRes = await uploader(file);
                    const imageUrl = uploadRes?.url || uploadRes?.duLieu?.url;
                    if (!imageUrl) throw new Error('Backend không trả về đường dẫn ảnh');
                    newUrls.push(imageUrl);
                }

                const allUrls = [...oldUrls, ...newUrls].filter(Boolean);
                payload.danhSachHinhAnh = JSON.stringify(allUrls);
                payload.hinhAnh = allUrls[0] || null;
            } catch (e) {
                showToast('Lỗi upload ảnh: ' + (e.message || 'Không tải được ảnh'), 'error');
                return;
            }
        }

        const fileEl = fields.some(f => f.id === 'fileUpload' && f.type === 'file') ? document.getElementById('f_fileUpload') : null;
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
            if (['fileUpload', 'fileUploadNhaTro', 'anhCccdMatTruoc', 'anhCccdMatSau'].includes(f.id) || f.type === 'fileMultiple') return; // Handled separately
            const el = document.getElementById(`f_${f.id}`);
            if (f.type === 'hiddenJsonArray' && Object.prototype.hasOwnProperty.call(payload, f.id)) {
                return;
            }
            if (f.type === 'serviceCheckboxes') {
                payload[f.id] = JSON.stringify(Array.from(document.querySelectorAll(`input[name="f_${f.id}"]:checked`)).map(x => Number(x.value)));
                return;
            }
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
        if (section === 'dangkydichvu') return openDangKyDichVuModal(id);
        if (section === 'baocaosuco') return openBaoCaoSuCoModal(id);
        if (section === 'thongbao') return window.AppThongBao?.openCreateModal();
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
    resetModalFooter();
}


function tinhNgayKetThucTheoSoThang(ngayBatDauValue, soThangValue) {
    if (!ngayBatDauValue) return '';
    const months = Math.max(1, Number(soThangValue || 1));
    const d = new Date(`${ngayBatDauValue}T00:00:00`);
    d.setMonth(d.getMonth() + months);
    d.setDate(d.getDate() - 1);
    return d.toISOString().substring(0, 10);
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
        <div class="form-group">
            <label>Số tháng muốn thuê <span style="color:var(--error)">*</span></label>
            <input type="number" id="f_soThangMuonThue" class="form-control" value="1" min="1" max="60" required>
            <small style="color:var(--text-light);">Hợp đồng sẽ tính theo kỳ từng tháng; hết mỗi kỳ, dịch vụ đã đăng ký trong kỳ cũ sẽ hết hạn.</small>
        </div>
        <div class="form-group">
            <label>Ngày bắt đầu mong muốn</label>
            <input type="date" id="f_ngayBatDauMongMuon" class="form-control">
        </div>
        <div class="form-group" style="grid-column:1/-1;background:#f8fafc;border:1px solid #e2e8f0;border-radius:.75rem;padding:.85rem;color:var(--text-light);">
            <strong>Thông tin kỳ thuê:</strong> Người dùng có thể đăng ký nhiều tháng. Mỗi tháng là một kỳ thuê riêng; dịch vụ/yêu cầu dịch vụ của kỳ cũ không tự cộng sang kỳ mới.
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Ghi chú gửi chủ trọ</label>
            <textarea id="f_ghiChuNguoiDung" class="form-control" placeholder="Ví dụ: Em muốn thuê 3 tháng, bắt đầu từ đầu tháng sau..."></textarea>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();

        const payload = {
            maPhong: Number(document.getElementById('f_maPhong').value),
            soThangMuonThue: Number(document.getElementById('f_soThangMuonThue').value || 1),
            ghiChuNguoiDung: document.getElementById('f_ghiChuNguoiDung').value
        };

        const ngayBatDauMongMuon = document.getElementById('f_ngayBatDauMongMuon').value;
        if (ngayBatDauMongMuon) payload.ngayBatDauMongMuon = ngayBatDauMongMuon;

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
    const soThangMacDinh = Number(yc.soThangMuonThue || 1);
    const ngayBatDauMacDinh = yc.ngayBatDauMongMuon ? yc.ngayBatDauMongMuon.substring(0, 10) : today;
    const ngayKetThucMacDinh = tinhNgayKetThucTheoSoThang(ngayBatDauMacDinh, soThangMacDinh);

    document.getElementById('modalTitle').textContent = 'Duyệt yêu cầu và lập hợp đồng';
    document.getElementById('modalFields').innerHTML = `
        <div style="grid-column:1/-1;background:#f8fafc;border-radius:.75rem;padding:1rem;margin-bottom:.5rem;">
            <strong>${yc.nguoiDung?.hoTen || 'Người dùng'}</strong> muốn thuê <strong>${yc.phong?.tenPhong || 'phòng'}</strong><br>
            <small>${yc.phong?.nhaTro?.tenNhaTro || ''}</small><br>
            <small>Thời hạn người dùng đề xuất: <strong>${soThangMacDinh} tháng</strong>${yc.ngayBatDauMongMuon ? `, bắt đầu khoảng ${window.AppFormat.date(yc.ngayBatDauMongMuon)}` : ''}</small>
        </div>
        <div class="form-group">
            <label>Ngày bắt đầu <span style="color:var(--error)">*</span></label>
            <input type="date" id="f_ngayBatDau" class="form-control" value="${ngayBatDauMacDinh}" required>
        </div>
        <div class="form-group">
            <label>Số tháng thuê</label>
            <input type="number" id="f_soThangThue" class="form-control" value="${soThangMacDinh}" min="1" max="60">
        </div>
        <div class="form-group">
            <label>Ngày kết thúc</label>
            <input type="date" id="f_ngayKetThuc" class="form-control" value="${ngayKetThucMacDinh}">
            <small style="color:var(--text-light);">Có thể sửa thủ công nếu chủ trọ muốn chốt ngày khác.</small>
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

    const updateNgayKetThucDuyet = () => {
        const start = document.getElementById('f_ngayBatDau')?.value;
        const months = document.getElementById('f_soThangThue')?.value;
        const endInput = document.getElementById('f_ngayKetThuc');
        if (start && months && endInput) endInput.value = tinhNgayKetThucTheoSoThang(start, months);
    };
    document.getElementById('f_ngayBatDau')?.addEventListener('change', updateNgayKetThucDuyet);
    document.getElementById('f_soThangThue')?.addEventListener('input', updateNgayKetThucDuyet);

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();

        const payload = {
            ngayBatDau: document.getElementById('f_ngayBatDau').value,
            soThangThue: Number(document.getElementById('f_soThangThue').value || 1),
            tienCoc: Number(document.getElementById('f_tienCoc').value),
            noiDung: document.getElementById('f_noiDung').value,
            ghiChuChuTro: document.getElementById('f_ghiChuChuTro').value
        };

        const ngayKetThuc = document.getElementById('f_ngayKetThuc').value;
        if (ngayKetThuc) payload.ngayKetThuc = ngayKetThuc;

        try {
            const result = await apiFetch(`/api/YeuCauThue/${maYeuCau}/chap-nhan`, 'POST', payload);
            showToast('Đã duyệt yêu cầu và lập hợp đồng!');
            closeModal();
            await loadLookups();
            refreshData();

            // Mở modal xuất PDF hợp đồng vừa tạo
            const maHopDong = result?.data?.maHopDong || result?.maHopDong;
            if (maHopDong && typeof HopDongPrint !== 'undefined') {
                setTimeout(() => {
                    if (confirm('H?p d?ng d� du?c t?o th�nh c�ng!\nB?n c� mu?n xem tru?c v� xu?t PDF h?p d?ng ngay kh�ng?')) {
                        HopDongPrint.openModal(maHopDong);
                    }
                }, 300);
            }
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
        showToast('�� t? ch?i y�u c?u thu�');
        refreshData();
    } catch (e) {
        showToast(e.message || 'Lỗi từ chối yêu cầu thuê', 'error');
    }
}


// ==========================================
// BÁO CÁO SỰ CỐ CUSTOM MODAL
// ==========================================
async function openBaoCaoSuCoModal(id = null) {
    if (CURRENT_ROLE !== 'NguoiDung') {
        showToast('Chỉ người dùng/khách thuê mới được gửi báo cáo sự cố', 'error');
        return;
    }

    resetModalFooter();
    document.getElementById('modalTitle').textContent = 'Gửi báo cáo sự cố';

    let taoMoiData = { phongDangThue: [], mucDo: ['Bình thường', 'Gấp', 'Rất gấp'] };
    try {
        taoMoiData = await apiFetch('/api/BaoCaoSuCo/TaoMoi') || taoMoiData;
    } catch (e) {
        showToast(e.message || 'Không tải được danh sách phòng đang thuê', 'error');
    }

    const phongDangThue = normalizeArrayResponse(taoMoiData.phongDangThue || taoMoiData.phong || taoMoiData.rooms);
    const mucDoList = normalizeArrayResponse(taoMoiData.mucDo).length
        ? normalizeArrayResponse(taoMoiData.mucDo)
        : ['Bình thường', 'Gấp', 'Rất gấp'];

    document.getElementById('modalFields').innerHTML = `
        <div class="form-group" style="grid-column:1/-1;">
            <label>Phòng đang thuê <span style="color:var(--error)">*</span></label>
            <select id="f_maPhongSuCo" class="form-control" required>
                <option value="">-- Chọn phòng cần báo cáo --</option>
                ${phongDangThue.map(p => `<option value="${p.maPhong}">${escapeHtmlDashboard(p.tenPhong || ('Phòng #' + p.maPhong))}${p.nhaTro?.tenNhaTro ? ' - ' + escapeHtmlDashboard(p.nhaTro.tenNhaTro) : ''}</option>`).join('')}
            </select>
            ${!phongDangThue.length ? '<small style="color:var(--error);">Bạn chưa có phòng đang thuê còn hiệu lực nên chưa thể gửi báo cáo sự cố.</small>' : ''}
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Tiêu đề sự cố <span style="color:var(--error)">*</span></label>
            <input type="text" id="f_tieuDeSuCo" class="form-control" maxlength="150" placeholder="Ví dụ: Hỏng bóng đèn, rò nước..." required>
        </div>
        <div class="form-group">
            <label>Mức độ</label>
            <select id="f_mucDoSuCo" class="form-control">
                ${mucDoList.map(m => `<option value="${escapeHtmlDashboard(m)}">${escapeHtmlDashboard(m)}</option>`).join('')}
            </select>
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Nội dung chi tiết <span style="color:var(--error)">*</span></label>
            <textarea id="f_noiDungSuCo" class="form-control" maxlength="1000" rows="5" placeholder="Mô tả rõ sự cố để chủ trọ xử lý..." required></textarea>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();

        const payload = {
            maPhong: Number(document.getElementById('f_maPhongSuCo').value),
            tieuDe: document.getElementById('f_tieuDeSuCo').value.trim(),
            noiDung: document.getElementById('f_noiDungSuCo').value.trim(),
            mucDo: document.getElementById('f_mucDoSuCo').value
        };

        if (!payload.maPhong) {
            showToast('Vui lòng chọn phòng cần báo cáo', 'error');
            return;
        }

        try {
            await apiFetch('/api/BaoCaoSuCo', 'POST', payload);
            showToast('Gửi báo cáo sự cố thành công!');
            closeModal();
            if (currentSection === 'baocaosuco') refreshData();
        } catch (e) {
            showToast(e.message || 'Lỗi gửi báo cáo sự cố', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

async function openBaoCaoSuCoXuLyModal(maBaoCao) {
    if (!(CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro')) {
        showToast('Bạn không có quyền xử lý báo cáo sự cố', 'error');
        return;
    }

    resetModalFooter();

    let baoCao = currentData.find(x => Number(x.maBaoCao) === Number(maBaoCao));
    try {
        baoCao = await apiFetch(`/api/BaoCaoSuCo/${maBaoCao}`) || baoCao;
    } catch (e) {
        if (!baoCao) {
            showToast(e.message || 'Không tải được báo cáo sự cố', 'error');
            return;
        }
    }

    document.getElementById('modalTitle').textContent = 'Xử lý báo cáo sự cố';
    document.getElementById('modalFields').innerHTML = `
        <div style="grid-column:1/-1;background:#f8fafc;border:1px solid #e2e8f0;border-radius:.85rem;padding:1rem;margin-bottom:.25rem;">
            <div style="font-weight:800;font-size:1rem;margin-bottom:.35rem;">${escapeHtmlDashboard(baoCao?.tieuDe || 'Báo cáo sự cố')}</div>
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:.5rem;color:var(--text-light);font-size:.9rem;">
                <div><strong>Người gửi:</strong> ${escapeHtmlDashboard(baoCao?.nguoiDung?.hoTen || baoCao?.nguoiDung?.email || '---')}</div>
                <div><strong>Phòng:</strong> ${escapeHtmlDashboard(baoCao?.phong?.tenPhong || '---')}</div>
                <div><strong>Nhà trọ:</strong> ${escapeHtmlDashboard(baoCao?.phong?.nhaTro?.tenNhaTro || '---')}</div>
                <div><strong>Mức độ:</strong> ${escapeHtmlDashboard(baoCao?.mucDo || 'Bình thường')}</div>
            </div>
            <div style="margin-top:.75rem;white-space:pre-wrap;">${escapeHtmlDashboard(baoCao?.noiDung || '')}</div>
        </div>
        <div class="form-group">
            <label>Trạng thái xử lý <span style="color:var(--error)">*</span></label>
            <select id="f_trangThaiSuCo" class="form-control" required>
                <option value="Moi" ${baoCao?.trangThai === 'Moi' ? 'selected' : ''}>Mới gửi</option>
                <option value="DangXuLy" ${baoCao?.trangThai === 'DangXuLy' ? 'selected' : ''}>Đang xử lý</option>
                <option value="DaXuLy" ${baoCao?.trangThai === 'DaXuLy' ? 'selected' : ''}>�� x? l�</option>
            </select>
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Phản hồi cho khách thuê</label>
            <textarea id="f_phanHoiSuCo" class="form-control" maxlength="1000" rows="4" placeholder="Nhập phản hồi hoặc hướng xử lý...">${escapeHtmlDashboard(baoCao?.phanHoiChuTro || '')}</textarea>
        </div>`;

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const payload = {
            trangThai: document.getElementById('f_trangThaiSuCo').value,
            phanHoiChuTro: document.getElementById('f_phanHoiSuCo').value.trim()
        };

        try {
            await apiFetch(`/api/BaoCaoSuCo/${maBaoCao}`, 'PUT', payload);
            showToast('Cập nhật báo cáo sự cố thành công!');
            closeModal();
            refreshData();
        } catch (e) {
            showToast(e.message || 'Lỗi cập nhật báo cáo sự cố', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
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
            <label>Số tháng thuê</label>
            <input type="number" id="f_soThangThue" class="form-control" value="1" min="1" max="60">
            <small style="color:var(--text-light);">Nhập số tháng để tự tính ngày kết thúc.</small>
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

    const updateNgayKetThucHopDong = () => {
        const start = document.getElementById('f_ngayBatDau')?.value;
        const months = document.getElementById('f_soThangThue')?.value;
        const endInput = document.getElementById('f_ngayKetThuc');
        if (start && months && endInput) endInput.value = tinhNgayKetThucTheoSoThang(start, months);
    };
    document.getElementById('f_ngayBatDau')?.addEventListener('change', updateNgayKetThucHopDong);
    document.getElementById('f_soThangThue')?.addEventListener('input', updateNgayKetThucHopDong);

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const payload = {
            maNguoiThue: Number(document.getElementById('f_maNguoiThue').value),
            maPhong: Number(document.getElementById('f_maPhong').value),
            ngayBatDau: document.getElementById('f_ngayBatDau').value,
            soThangThue: Number(document.getElementById('f_soThangThue').value || 1),
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
                H?ng th�ng t? t�nh di?n, nu?c, d?ch v? ngu?i thu� d� dang k� v� ph�t sinh kh�c. Thu� ph�ng ch? t�nh ti?n ph�ng v� ph�t sinh kh�c.
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
                <div class="info-item thue-phong-only"><label>Tiền phòng</label><span id="infoTienPhong">---</span></div>
                <div class="info-item hang-thang-only"><label>Tiền điện</label><span id="infoTienDien">---</span></div>
                <div class="info-item hang-thang-only"><label>Tiền nước</label><span id="infoTienNuoc">---</span></div>
                <div class="info-item hang-thang-only"><label>Ti?n d?ch v? d� dang k�</label><span id="infoTienDichVu">---</span></div>
                <div class="info-item info-total"><label>Dự tính tổng tiền</label><span id="infoTongTien">---</span></div>
            </div>
            <div id="dichVuHoaDonBox" class="hang-thang-only" style="margin-top:1rem;display:none;">
                <label style="font-weight:700;margin-bottom:.5rem;display:block;">D?ch v? ngu?i thu� d� dang k�</label>
                <div id="dichVuHoaDonList" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:.65rem;"></div>
                <small style="color:var(--text-light);display:block;margin-top:.5rem;">Ch? hi?n th? c�c d?ch v? m� ngu?i thu� d� dang k�. C�c d?ch v? n�y s? t? d?ng c?ng v�o h�a don h?ng th�ng.</small>
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
        const tienPhong = loaiHoaDon === 'ThuePhong' ? Number(info.phong?.giaPhong || 0) : 0;
        const tienDien = loaiHoaDon === 'HangThang' ? Number(info.tienDien || 0) : 0;
        const tienNuoc = loaiHoaDon === 'HangThang' ? Number(info.tienNuoc || 0) : 0;

        const payload = {
            loaiHoaDon,
            maNguoiThue: info.nguoiThue.maNguoiThue,
            maPhong: info.phong.maPhong,
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

async function loadPhongInfo(phongId) {
    if (!phongId) { document.getElementById('phongInfoBox').style.display = 'none'; return; }
    try {
        const kyHoaDon = document.getElementById('f_kyHoaDon')?.value || '';
        const qs = kyHoaDon ? `?kyHoaDon=${encodeURIComponent(kyHoaDon)}` : '';
        const info = await apiFetch(`/api/HoaDon/GetThongTinPhong/${phongId}${qs}`);
        window._hoaDonInfo = info;
        document.getElementById('phongInfoBox').style.display = 'block';
        document.getElementById('infoNguoiThue').textContent = info.nguoiThue?.hoTen || '---';
        document.getElementById('infoTienPhong').textContent = fmtCurrency(info.phong?.giaPhong);
        document.getElementById('infoTienDien').textContent = Number(info.tienDien || 0) > 0 ? fmtCurrency(info.tienDien) : '0đ';
        document.getElementById('infoTienNuoc').textContent = Number(info.tienNuoc || 0) > 0 ? fmtCurrency(info.tienNuoc) : '0đ';

        const rawServices = info.dichVuDaDangKy || info.DichVuDaDangKy || info.danhSachDichVu || info.DanhSachDichVu || [];
        const services = Array.isArray(rawServices)
            ? rawServices
            : (rawServices?.$values || []);
        const serviceBox = document.getElementById('dichVuHoaDonBox');
        const serviceList = document.getElementById('dichVuHoaDonList');
        if (serviceList) {
            if (services.length) {
                serviceList.innerHTML = services.map(dv => {
                    const id = dv.maDichVu ?? dv.MaDichVu;
                    const name = dv.tenDichVu ?? dv.TenDichVu ?? 'Dịch vụ';
                    const price = Number(dv.tienDichVu ?? dv.TienDichVu ?? 0);
                    return `
                        <label style="display:flex;gap:.65rem;align-items:flex-start;padding:.75rem;border:1px solid #bbf7d0;border-radius:.75rem;background:#f0fdf4;cursor:default;">
                            <input type="checkbox" class="hoa-don-dich-vu" value="${id}" data-price="${price}" checked disabled style="margin-top:.2rem;">
                            <span style="flex:1;">
                                <strong>${escapeHtmlDashboard(name)}</strong><br>
                                <small style="color:var(--text-light);">${fmtCurrency(price)}</small>
                            </span>
                        </label>`;
                }).join('');
            } else {
                serviceList.innerHTML = `<div style="color:var(--text-light);padding:.75rem;border:1px dashed #d1d5db;border-radius:.75rem;">Phòng này chưa đăng ký dịch vụ nào.</div>`;
            }
        }
        if (serviceBox) serviceBox.style.display = 'block';

        onHoaDonTypeChanged();
        recalcTotal();
    } catch (e) {
        window._hoaDonInfo = null;
        document.getElementById('phongInfoBox').style.display = 'none';
        showToast('Lỗi: ' + (e.message || 'Không tải được thông tin phòng'), 'error');
    }
}


function reloadHoaDonPhongInfo() {
    const phongId = document.getElementById('f_maPhong')?.value;
    if (phongId) {
        loadPhongInfo(phongId);
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

    document.querySelectorAll('.thue-phong-only').forEach(el => {
        el.style.display = isMonthly ? 'none' : '';
    });

    recalcTotal();
}

function recalcTotal() {
    if (!window._hoaDonInfo) return;
    const info = window._hoaDonInfo;
    const loai = document.getElementById('f_loaiHoaDon')?.value || 'HangThang';
    const ps = Number(document.getElementById('f_tienPhatSinhKhac')?.value) || 0;
    const serviceTotal = loai === 'HangThang' ? calcSelectedServiceTotal() : 0;
    const total = loai === 'ThuePhong'
        ? (Number(info.phong?.giaPhong || 0) + ps)
        : (Number(info.tienDien || 0) + Number(info.tienNuoc || 0) + serviceTotal + ps);

    const dvEl = document.getElementById('infoTienDichVu');
    if (dvEl) dvEl.textContent = fmtCurrency(serviceTotal);
    const totalEl = document.getElementById('infoTongTien');
    if (totalEl) totalEl.textContent = fmtCurrency(total);
}



// ==========================================
// ĐĂNG KÝ DỊCH VỤ
// ==========================================
async function openDangKyDichVuModal(id = null, maPhongChon = null) {
    if (CURRENT_ROLE !== 'NguoiDung') {
        showToast('Chỉ người dùng mới được đăng ký dịch vụ', 'error');
        return;
    }

    resetModalFooter();
    document.getElementById('modalTitle').textContent = 'Đăng ký dịch vụ sử dụng';
    document.getElementById('modalFields').innerHTML = `
        <div class="form-group" style="grid-column:1/-1;">
            <label>Chọn phòng đang thuê <span style="color:var(--error)">*</span></label>
            <select id="f_dkdv_maPhong" class="form-control" required onchange="loadDichVuDangKyTheoPhong(this.value)">
                <option value="">-- Đang tải phòng đang thuê --</option>
            </select>
            <small style="color:var(--text-light);display:block;margin-top:.35rem;">Bạn chỉ đăng ký được dịch vụ cho các phòng đang thuê còn hợp đồng hiệu lực.</small>
        </div>
        <div style="grid-column:1/-1;">
            <label style="font-weight:700;margin-bottom:.5rem;display:block;">Dịch vụ có thể đăng ký</label>
            <div id="dichVuDangKyList" style="display:grid;grid-template-columns:repeat(auto-fit,minmax(230px,1fr));gap:.75rem;">
                <div style="color:var(--text-light);padding:1rem;border:1px dashed #d1d5db;border-radius:.75rem;">Vui lòng chọn phòng trước.</div>
            </div>
        </div>
        <div class="form-group" style="grid-column:1/-1;">
            <label>Ghi chú</label>
            <textarea id="f_dkdv_ghiChu" class="form-control" placeholder="Ví dụ: đăng ký internet từ tháng này..."></textarea>
        </div>`;

    const footer = document.querySelector('#universalModal .modal-footer');
    if (footer) {
        footer.innerHTML = `
            <button type="button" class="btn btn-secondary" style="width:auto;" onclick="closeModal()">Đóng</button>
            <button type="submit" class="btn btn-primary" style="width:auto;"><i class="fas fa-plus"></i> Đăng ký dịch vụ</button>`;
    }

    try {
        const rooms = normalizeArrayResponse(await apiFetch('/api/DangKyDichVu/PhongDangThue'));
        const select = document.getElementById('f_dkdv_maPhong');
        if (!select) return;
        select.innerHTML = `<option value="">-- Chọn phòng --</option>` + rooms.map(p => `
            <option value="${p.maPhong}">${escapeHtmlDashboard(p.tenPhong || ('Phòng #' + p.maPhong))}${p.tenNhaTro ? ' - ' + escapeHtmlDashboard(p.tenNhaTro) : ''}</option>`).join('');
        const phongMacDinh = maPhongChon || (rooms.length === 1 ? rooms[0].maPhong : null);
        if (phongMacDinh) {
            select.value = phongMacDinh;
            await loadDichVuDangKyTheoPhong(phongMacDinh);
        }
        if (!rooms.length) {
            document.getElementById('dichVuDangKyList').innerHTML = `<div style="color:var(--text-light);padding:1rem;border:1px dashed #d1d5db;border-radius:.75rem;">Bạn chưa có phòng đang thuê hợp lệ.</div>`;
        }
    } catch (e) {
        showToast(e.message || 'Không tải được phòng đang thuê', 'error');
    }

    document.getElementById('modalForm').onsubmit = async (e) => {
        e.preventDefault();
        const maPhong = Number(document.getElementById('f_dkdv_maPhong')?.value || 0);
        const maDichVu = Number(document.querySelector('input[name="dkdv_service"]:checked')?.value || 0);
        if (!maPhong) { showToast('Vui lòng chọn phòng', 'error'); return; }
        if (!maDichVu) { showToast('Vui lòng chọn dịch vụ muốn đăng ký', 'error'); return; }

        try {
            await apiFetch('/api/DangKyDichVu', 'POST', {
                maPhong,
                maDichVu,
                ghiChu: document.getElementById('f_dkdv_ghiChu')?.value || ''
            });
            showToast('Đăng ký dịch vụ thành công! Dịch vụ này sẽ tự động được cộng vào hóa đơn hằng tháng.');
            closeModal();
            refreshData();
        } catch (err) {
            showToast(err.message || 'Lỗi đăng ký dịch vụ', 'error');
        }
    };

    document.getElementById('universalModal').style.display = 'flex';
}

async function loadDichVuDangKyTheoPhong(maPhong) {
    const list = document.getElementById('dichVuDangKyList');
    if (!list) return;
    if (!maPhong) {
        list.innerHTML = `<div style="color:var(--text-light);padding:1rem;border:1px dashed #d1d5db;border-radius:.75rem;">Vui lòng chọn phòng trước.</div>`;
        return;
    }

    list.innerHTML = `<div style="color:var(--text-light);"><i class="fas fa-spinner fa-spin"></i> Đang tải dịch vụ...</div>`;
    try {
        const services = normalizeArrayResponse(await apiFetch(`/api/DangKyDichVu/DichVuTheoPhong/${maPhong}`));
        if (!services.length) {
            list.innerHTML = `<div style="color:var(--text-light);padding:1rem;border:1px dashed #d1d5db;border-radius:.75rem;">Chủ trọ chưa khai báo dịch vụ nào cho phòng này.</div>`;
            return;
        }

        list.innerHTML = services.map(dv => {
            const id = dv.maDichVu ?? dv.MaDichVu;
            const name = dv.tenDichVu ?? dv.TenDichVu ?? 'Dịch vụ';
            const price = Number(dv.tienDichVu ?? dv.TienDichVu ?? 0);
            const registered = Boolean(dv.daDangKy ?? dv.DaDangKy);
            return `
                <label style="display:flex;gap:.65rem;align-items:flex-start;padding:.85rem;border:1px solid ${registered ? '#bbf7d0' : '#e5e7eb'};border-radius:.9rem;background:${registered ? '#f0fdf4' : 'white'};${registered ? 'opacity:.75;' : 'cursor:pointer;'}">
                    <input type="radio" name="dkdv_service" value="${id}" ${registered ? 'disabled' : ''} style="margin-top:.25rem;">
                    <span style="flex:1;">
                        <strong>${escapeHtmlDashboard(name)}</strong><br>
                        <small style="color:var(--text-light);">${fmtCurrency(price)} / tháng</small><br>
                        <small style="color:${registered ? 'var(--success)' : 'var(--primary)'};font-weight:700;">${registered ? '�� dang k�' : 'C� th? dang k�'}</small>
                    </span>
                </label>`;
        }).join('');
    } catch (e) {
        list.innerHTML = `<div style="color:var(--error);padding:1rem;border:1px dashed #fecaca;border-radius:.75rem;">${escapeHtmlDashboard(e.message || 'Không tải được dịch vụ')}</div>`;
    }
}

async function huyDangKyDichVu(id) {
    if (!confirm('B?n c� ch?c mu?n h?y dang k� d?ch v? n�y? D?ch v? d� h?y s? kh�ng t? d?ng c?ng v�o c�c h�a don h?ng th�ng l?p sau d�.')) return;
    try {
        const result = await apiFetch(`/api/DangKyDichVu/${id}`, 'DELETE');
        showToast(result?.thongBao || '�� h?y dang k� d?ch v?');
        refreshData();
    } catch (e) {
        showToast(e.message || 'Lỗi hủy đăng ký dịch vụ', 'error');
    }
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
                <div class="info-item"><label>Ng�n h�ng</label><span>${escapeHtmlDashboard(hoaDon.tenNganHang || hoaDon.maNganHang || '---')}</span></div>
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
                <div class="info-item"><label>�� thanh to�n</label><span>${fmtCurrency(hoaDon.daThanhToan || 0)}</span></div>
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
                    <h3 style="font-size:1rem;font-weight:800;margin-bottom:.75rem;color:var(--text);"><i class="fas fa-user"></i> Th�ng tin c� nh�n</h3>
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
                        <div class="info-item"><label>M� t�i kho?n li�n k?t</label><span>${safeText(nt.maNguoiDung)}</span></div>
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
    const messages = [];
    const errors = [];

    for (const maNguoiThue of ids) {
        try {
            const result = await apiFetch(`/api/NguoiThue/${maNguoiThue}`, 'DELETE');
            success++;
            if (result?.thongBao) messages.push(result.thongBao);
        } catch (e) {
            errors.push(e.message || `Không xóa được hồ sơ #${maNguoiThue}`);
        }
    }

    if (success > 0) showToast(messages[0] || `�� x? l� ${success} h? so kh�ch thu�`, 'success');
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
    if (!confirm('B?n c� ch?c ch?n mu?n x�a m?c n�y? D? li?u c� th? du?c x�a m?m n?u d� ph�t sinh l?ch s?.')) return;
    const cfg = modules[section];
    if (!cfg) return;
    try {
        const res = await fetch(`${cfg.endpoint}/${id}`, {
            method: 'DELETE',
            headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }
        });
        if (res.status === 401) { logout(); return; }
        const text = await res.text();
        let json = {};
        try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }

        if (!res.ok || json.thanhCong === false) {
            throw new Error(extractApiErrorMessage(json) || `Lỗi HTTP ${res.status}`);
        }

        // Lấy thông báo trực tiếp từ ApiResponse (không qua apiFetch vì apiFetch trả về duLieu)
        const msg = json.thongBao || '�� x? l� y�u c?u x�a!';
        showToast(msg);
        refreshData();
        loadLookups();
    } catch (e) {
        showToast(e.message || 'Lỗi xóa dữ liệu', 'error');
    }
}

async function ketThucHopDong(id) {
    if (!confirm('Kết thúc hợp đồng này? Hệ thống sẽ chặn nếu còn hóa đơn chưa thanh toán hoặc thanh toán chưa đủ.')) return;
    try {
        const result = await postHopDongAction(`/api/HopDong/${id}/ket-thuc`);
        showToast(result?.thongBao || result?.message || '�� k?t th�c h?p d?ng');
        refreshData();
        loadLookups();
    } catch (e) {
        showToast(e.message || 'Lỗi kết thúc hợp đồng', 'error');
    }
}

async function huyHopDong(id) {
    if (!confirm('H?y h?p d?ng n�y? N?u h?p d?ng d� ph�t sinh d? li?u, h? th?ng ch? chuy?n sang tr?ng th�i d� h?y.')) return;
    try {
        const result = await postHopDongAction(`/api/HopDong/${id}/huy`);
        showToast(result?.thongBao || result?.message || '�� h?y h?p d?ng');
        refreshData();
        loadLookups();
    } catch (e) {
        showToast(e.message || 'Lỗi hủy hợp đồng', 'error');
    }
}

async function postHopDongAction(endpoint) {
    const res = await fetch(endpoint, {
        method: 'POST',
        headers: { 'Authorization': `Bearer ${token}`, 'Content-Type': 'application/json' }
    });
    if (res.status === 401) { logout(); return null; }
    const text = await res.text();
    let json = {};
    try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }
    if (!res.ok || json.thanhCong === false) {
        throw new Error(extractApiErrorMessage(json) || `Lỗi HTTP ${res.status}`);
    }
    return json;
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

    if (typeof window.refreshSidebarBadges === 'function') {
        window.refreshSidebarBadges();
    }
}
window.startDashboard = startDashboard;

if (!window.__USING_MODULE_LOADER) {
    startDashboard();
}
