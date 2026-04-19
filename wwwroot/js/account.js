// ==========================================
// ACCOUNT.JS — Tài khoản của tôi
// ==========================================

// ─── Hook vào showSection của dashboard.js ───────────────────────────────────
const _origShowSection = showSection;
showSection = function(section, el) {
    _origShowSection(section, el);
    const taikhoanSec = document.getElementById('taikhoanSection');

    if (section === 'taikhoan') {
        // Ẩn các section khác
        document.getElementById('overviewSection').style.display = 'none';
        document.getElementById('genericSection').style.display  = 'none';
        taikhoanSec.style.display = 'block';

        // Ẩn nút "Thêm mới"
        document.getElementById('addBtn').style.display = 'none';
        document.getElementById('sectionTitle').textContent = 'Tài khoản của tôi';

        loadProfile();
    } else {
        taikhoanSec.style.display = 'none';
    }
};

// ─── Helper: toggle password field ───────────────────────────────────────────
function togglePasswordField(fieldId, btn) {
    const input = document.getElementById(fieldId);
    const icon  = btn.querySelector('i');
    if (input.type === 'password') {
        input.type = 'text';
        icon.classList.replace('fa-eye', 'fa-eye-slash');
    } else {
        input.type = 'password';
        icon.classList.replace('fa-eye-slash', 'fa-eye');
    }
}

// ─── Password strength ────────────────────────────────────────────────────────
function checkStrength(val) {
    const fill  = document.getElementById('strengthFill');
    const label = document.getElementById('strengthLabel');
    if (!fill) return;

    let score = 0;
    if (val.length >= 6)  score++;
    if (val.length >= 10) score++;
    if (/[A-Z]/.test(val)) score++;
    if (/[0-9]/.test(val)) score++;
    if (/[^a-zA-Z0-9]/.test(val)) score++;

    const levels = [
        { pct: '0%',   color: '#e5e7eb', text: '' },
        { pct: '25%',  color: '#ef4444', text: 'Rất yếu' },
        { pct: '50%',  color: '#f59e0b', text: 'Yếu' },
        { pct: '70%',  color: '#3b82f6', text: 'Trung bình' },
        { pct: '85%',  color: '#10b981', text: 'Mạnh' },
        { pct: '100%', color: '#0d9488', text: 'Rất mạnh' },
    ];
    const lv = levels[Math.min(score, 5)];
    fill.style.width      = lv.pct;
    fill.style.background = lv.color;
    label.textContent     = lv.text;
    label.style.color     = lv.color;
}

// ─── Load & render profile ────────────────────────────────────────────────────
async function loadProfile() {
    const body = document.getElementById('profileInfoBody');
    body.innerHTML = '<div style="text-align:center;padding:1.5rem;color:var(--text-light);"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>';

    try {
        const res  = await apiFetch('/api/Account/thong-tin');
        const data = res.duLieu || res;

        // Avatar header
        const initial = (data.hoTen || data.tenDangNhap || 'A').charAt(0).toUpperCase();
        document.getElementById('profileAvatarBig').textContent = initial;
        document.getElementById('profileDisplayName').textContent = data.hoTen || '—';

        const roleLabel = { Admin: 'Admin', ChuTro: 'Chủ trọ', NguoiDung: 'Người dùng' };
        document.getElementById('profileDisplayRole').innerHTML =
            `<span class="badge badge-teal">${roleLabel[data.vaiTro] || data.vaiTro}</span>`;
        document.getElementById('profileDisplayEmail').textContent = data.email || '—';

        // Info card (read-only rows)
        const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN', { year:'numeric', month:'long', day:'numeric' }) : '—';
        const rows = [
            { icon: 'fa-user',        label: 'Tên đăng nhập',  value: data.tenDangNhap },
            { icon: 'fa-id-card',     label: 'Họ tên',          value: data.hoTen || '—' },
            { icon: 'fa-envelope',    label: 'Email',            value: data.email },
            { icon: 'fa-phone',       label: 'Số điện thoại',   value: data.soDienThoai || '—' },
            { icon: 'fa-shield-alt',  label: 'Vai trò',          value: roleLabel[data.vaiTro] || data.vaiTro },
            { icon: 'fa-calendar',    label: 'Ngày tạo',         value: fmtDate(data.ngayTao) },
            { icon: 'fa-circle',      label: 'Trạng thái',       value: data.trangThai ? '✅ Đang hoạt động' : '🔒 Bị khóa' },
        ];

        body.innerHTML = rows.map(r => `
            <div class="profile-row">
                <div class="pr-icon"><i class="fas ${r.icon}"></i></div>
                <div>
                    <div class="pr-label">${r.label}</div>
                    <div class="pr-value">${r.value}</div>
                </div>
            </div>`).join('');

        // Điền vào form sửa
        document.getElementById('editHoTen').value = data.hoTen || '';
        document.getElementById('editEmail').value  = data.email || '';
        document.getElementById('editPhone').value  = data.soDienThoai || '';

        // Điền vào card bảo mật (readonly)
        document.getElementById('readUsername').value  = data.tenDangNhap;
        document.getElementById('readVaiTro').value    = roleLabel[data.vaiTro] || data.vaiTro;
        document.getElementById('readTrangThai').value = data.trangThai ? 'Đang hoạt động' : 'Bị khóa';
        document.getElementById('readNgayTao').value   = fmtDate(data.ngayTao);

        // Sync sidebar avatar
        document.getElementById('userAvatar').textContent  = initial;
        document.getElementById('userName').textContent    = data.hoTen || data.tenDangNhap;

    } catch (e) {
        body.innerHTML = `<div style="color:var(--error);padding:1rem;"><i class="fas fa-exclamation-circle"></i> ${e.message || 'Lỗi tải thông tin'}</div>`;
    }
}

// ─── Form: Cập nhật thông tin ─────────────────────────────────────────────────
document.getElementById('profileEditForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const btn  = e.target.querySelector('button[type=submit]');
    const orig = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang lưu...';
    btn.disabled  = true;

    try {
        await apiFetch('/api/Account/cap-nhat', 'PUT', {
            hoTen:       document.getElementById('editHoTen').value.trim(),
            email:       document.getElementById('editEmail').value.trim(),
            soDienThoai: document.getElementById('editPhone').value.trim()
        });
        showToast('Cập nhật thông tin thành công!', 'success');
        loadProfile();
    } catch (err) {
        showToast(err.message || 'Lỗi cập nhật thông tin', 'error');
    } finally {
        btn.innerHTML = orig;
        btn.disabled  = false;
    }
});

// ─── Form: Đổi mật khẩu ──────────────────────────────────────────────────────
document.getElementById('changePasswordForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const matKhauCu      = document.getElementById('oldPassword').value;
    const matKhauMoi     = document.getElementById('newPassword').value;
    const nhapLaiMatKhau = document.getElementById('confirmPassword').value;

    if (matKhauMoi !== nhapLaiMatKhau) {
        showToast('Mật khẩu nhập lại không khớp', 'error');
        return;
    }

    const btn  = e.target.querySelector('button[type=submit]');
    const orig = btn.innerHTML;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang xử lý...';
    btn.disabled  = true;

    try {
        await apiFetch('/api/Account/doi-mat-khau', 'POST', {
            matKhauCu,
            matKhauMoi,
            nhapLaiMatKhau
        });
        showToast('Đổi mật khẩu thành công!', 'success');
        e.target.reset();
        // Reset strength bar
        const fill = document.getElementById('strengthFill');
        if (fill) { fill.style.width = '0%'; fill.style.background = '#e5e7eb'; }
        document.getElementById('strengthLabel').textContent = '';
    } catch (err) {
        showToast(err.message || 'Lỗi đổi mật khẩu', 'error');
    } finally {
        btn.innerHTML = orig;
        btn.disabled  = false;
    }
});
