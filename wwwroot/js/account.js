// ==========================================
// ACCOUNT.JS — Tài khoản của tôi
// ==========================================

// ─── Hook vào showSection của dashboard.js ───────────────────────────────────
const _origShowSection = showSection;
showSection = function(section, el) {
    _origShowSection(section, el);
    const taikhoanSec = document.getElementById('taikhoanSection');

    if (section === 'taikhoan') {
        document.getElementById('overviewSection').style.display = 'none';
        document.getElementById('genericSection').style.display  = 'none';
        taikhoanSec.style.display = 'block';

        document.getElementById('addBtn').style.display = 'none';
        document.getElementById('sectionTitle').textContent = 'Tài khoản của tôi';

        loadProfile();
    } else {
        taikhoanSec.style.display = 'none';
    }
};

function escapeHtml(v) {
    return v === null || v === undefined ? '' : String(v)
        .replaceAll('&', '&amp;')
        .replaceAll('"', '&quot;')
        .replaceAll('<', '&lt;')
        .replaceAll('>', '&gt;');
}

function displayValue(v) {
    const text = escapeHtml(v);
    return text || '—';
}

function formatDateInput(v) {
    return v ? String(v).substring(0, 10) : '';
}

function formatDateDisplay(v) {
    if (!v) return '—';
    const d = new Date(v);
    if (Number.isNaN(d.getTime())) return '—';
    return d.toLocaleDateString('vi-VN', { year: 'numeric', month: 'long', day: 'numeric' });
}

function renderCccdPreview(url, label) {
    if (!url) {
        return `<div style="padding:1rem;border:1px dashed #d1d5db;border-radius:.75rem;color:var(--text-light);text-align:center;">Chưa có ${label.toLowerCase()}</div>`;
    }

    const safeUrl = escapeHtml(url);
    return `<a href="${safeUrl}" target="_blank"><img src="${safeUrl}" alt="${escapeHtml(label)}" style="width:100%;max-height:220px;object-fit:contain;border-radius:.75rem;background:#f8fafc;border:1px solid #e5e7eb;"></a>`;
}

async function uploadAccountCccdFile(inputId, hiddenId, previewId, label) {
    const fileInput = document.getElementById(inputId);
    const hidden = document.getElementById(hiddenId);
    const preview = document.getElementById(previewId);

    if (!fileInput || !fileInput.files || fileInput.files.length === 0) {
        return hidden?.value || '';
    }

    showToast(`Đang tải ${label}...`, 'info');
    const uploadRes = await API.nguoithue.uploadCccdImage(fileInput.files[0]);
    const url = uploadRes?.url || uploadRes?.duLieu?.url;

    if (!url) throw new Error(`Upload ${label} thất bại`);

    hidden.value = url;
    if (preview) preview.innerHTML = renderCccdPreview(url, label);
    return url;
}

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

        const initial = (data.hoTen || data.tenDangNhap || 'A').charAt(0).toUpperCase();
        document.getElementById('profileAvatarBig').textContent = initial;
        document.getElementById('profileDisplayName').textContent = data.hoTen || '—';

        const roleLabel = { Admin: 'Admin', ChuTro: 'Chủ trọ', NguoiDung: 'Người dùng' };
        document.getElementById('profileDisplayRole').innerHTML =
            `<span class="badge badge-teal">${roleLabel[data.vaiTro] || data.vaiTro}</span>`;
        document.getElementById('profileDisplayEmail').textContent = data.email || '—';

        const rows = [
            { icon: 'fa-user',        label: 'Tên đăng nhập',  value: data.tenDangNhap },
            { icon: 'fa-id-card',     label: 'Họ tên',          value: data.hoTen || '—' },
            { icon: 'fa-envelope',    label: 'Email',            value: data.email },
            { icon: 'fa-phone',       label: 'Số điện thoại',   value: data.soDienThoai || '—' },
            { icon: 'fa-shield-alt',  label: 'Vai trò',          value: roleLabel[data.vaiTro] || data.vaiTro },
        ];

        if (data.vaiTro === 'NguoiDung') {
            rows.push(
                { icon: 'fa-address-card', label: 'CCCD/CMND', value: data.cccd || '—' },
                { icon: 'fa-birthday-cake', label: 'Ngày sinh', value: formatDateDisplay(data.ngaySinh) },
                { icon: 'fa-venus-mars', label: 'Giới tính', value: data.gioiTinh || '—' },
                { icon: 'fa-flag', label: 'Quốc tịch', value: data.quocTich || '—' },
                { icon: 'fa-map-marker-alt', label: 'Địa chỉ', value: data.diaChi || '—' },
                { icon: 'fa-briefcase', label: 'Nơi công tác', value: data.noiCongTac || '—' }
            );
        }

        if (data.vaiTro === 'ChuTro' || data.vaiTro === 'Admin') {
            rows.push(
                { icon: 'fa-university', label: 'Ngân hàng nhận tiền', value: data.tenNganHang || '—' },
                { icon: 'fa-qrcode', label: 'Mã ngân hàng VietQR', value: data.maNganHang || '—' },
                { icon: 'fa-credit-card', label: 'Số tài khoản', value: data.soTaiKhoan || '—' },
                { icon: 'fa-user-check', label: 'Tên chủ tài khoản', value: data.tenChuTaiKhoan || '—' },
                { icon: 'fa-comment-dollar', label: 'Nội dung CK mặc định', value: data.noiDungChuyenKhoanMacDinh || '—' }
            );
        }

        rows.push(
            { icon: 'fa-calendar',    label: 'Ngày tạo',         value: formatDateDisplay(data.ngayTao) },
            { icon: 'fa-circle',      label: 'Trạng thái',       value: data.trangThai ? '✅ Đang hoạt động' : '🔒 Bị khóa' }
        );

        body.innerHTML = rows.map(r => `
            <div class="profile-row">
                <div class="pr-icon"><i class="fas ${r.icon}"></i></div>
                <div>
                    <div class="pr-label">${escapeHtml(r.label)}</div>
                    <div class="pr-value">${displayValue(r.value)}</div>
                </div>
            </div>`).join('') + (data.vaiTro === 'NguoiDung' ? `
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:1rem;margin-top:1rem;">
                <div>
                    <div class="pr-label" style="margin-bottom:.35rem;">Ảnh CCCD mặt trước</div>
                    ${renderCccdPreview(data.anhCccdMatTruoc, 'CCCD mặt trước')}
                </div>
                <div>
                    <div class="pr-label" style="margin-bottom:.35rem;">Ảnh CCCD mặt sau</div>
                    ${renderCccdPreview(data.anhCccdMatSau, 'CCCD mặt sau')}
                </div>
            </div>` : '');

        document.getElementById('editHoTen').value = data.hoTen || '';
        document.getElementById('editEmail').value  = data.email || '';
        document.getElementById('editPhone').value  = data.soDienThoai || '';

        const extraEls = document.querySelectorAll('.account-user-extra');
        extraEls.forEach(el => el.style.display = data.vaiTro === 'NguoiDung' ? '' : 'none');

        const paymentEls = document.querySelectorAll('.account-owner-payment');
        paymentEls.forEach(el => el.style.display = (data.vaiTro === 'ChuTro' || data.vaiTro === 'Admin') ? '' : 'none');

        if (document.getElementById('editTenNganHang')) {
            document.getElementById('editTenNganHang').value = data.tenNganHang || '';
            document.getElementById('editMaNganHang').value = data.maNganHang || '';
            document.getElementById('editSoTaiKhoan').value = data.soTaiKhoan || '';
            document.getElementById('editTenChuTaiKhoan').value = data.tenChuTaiKhoan || '';
            document.getElementById('editNoiDungChuyenKhoanMacDinh').value = data.noiDungChuyenKhoanMacDinh || '';
        }

        if (document.getElementById('editCCCD')) {
            document.getElementById('editCCCD').value = data.cccd || '';
            document.getElementById('editNgaySinh').value = formatDateInput(data.ngaySinh);
            document.getElementById('editGioiTinh').value = data.gioiTinh || '';
            document.getElementById('editQuocTich').value = data.quocTich || 'Việt Nam';
            document.getElementById('editDiaChi').value = data.diaChi || '';
            document.getElementById('editNoiCongTac').value = data.noiCongTac || '';
            document.getElementById('editAnhCccdMatTruoc').value = data.anhCccdMatTruoc || '';
            document.getElementById('editAnhCccdMatSau').value = data.anhCccdMatSau || '';
            document.getElementById('editAnhCccdMatTruocPreview').innerHTML = renderCccdPreview(data.anhCccdMatTruoc, 'CCCD mặt trước');
            document.getElementById('editAnhCccdMatSauPreview').innerHTML = renderCccdPreview(data.anhCccdMatSau, 'CCCD mặt sau');
        }

        document.getElementById('readUsername').value  = data.tenDangNhap;
        document.getElementById('readVaiTro').value    = roleLabel[data.vaiTro] || data.vaiTro;
        document.getElementById('readTrangThai').value = data.trangThai ? 'Đang hoạt động' : 'Bị khóa';
        document.getElementById('readNgayTao').value   = formatDateDisplay(data.ngayTao);

        document.getElementById('userAvatar').textContent  = initial;
        document.getElementById('userName').textContent    = data.hoTen || data.tenDangNhap;

        loadTenantProfiles();

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
        const payload = {
            hoTen:       document.getElementById('editHoTen').value.trim(),
            email:       document.getElementById('editEmail').value.trim(),
            soDienThoai: document.getElementById('editPhone').value.trim()
        };

        if (CURRENT_ROLE === 'NguoiDung') {
            const frontUrl = await uploadAccountCccdFile('editAnhCccdMatTruocFile', 'editAnhCccdMatTruoc', 'editAnhCccdMatTruocPreview', 'CCCD mặt trước');
            const backUrl = await uploadAccountCccdFile('editAnhCccdMatSauFile', 'editAnhCccdMatSau', 'editAnhCccdMatSauPreview', 'CCCD mặt sau');

            Object.assign(payload, {
                cccd: document.getElementById('editCCCD').value.trim(),
                ngaySinh: document.getElementById('editNgaySinh').value || null,
                gioiTinh: document.getElementById('editGioiTinh').value,
                quocTich: document.getElementById('editQuocTich').value.trim(),
                diaChi: document.getElementById('editDiaChi').value.trim(),
                noiCongTac: document.getElementById('editNoiCongTac').value.trim(),
                anhCccdMatTruoc: frontUrl,
                anhCccdMatSau: backUrl
            });
        }

        if (CURRENT_ROLE === 'ChuTro' || CURRENT_ROLE === 'Admin') {
            Object.assign(payload, {
                tenNganHang: document.getElementById('editTenNganHang')?.value.trim() || '',
                maNganHang: document.getElementById('editMaNganHang')?.value.trim() || '',
                soTaiKhoan: document.getElementById('editSoTaiKhoan')?.value.trim() || '',
                tenChuTaiKhoan: document.getElementById('editTenChuTaiKhoan')?.value.trim() || '',
                noiDungChuyenKhoanMacDinh: document.getElementById('editNoiDungChuyenKhoanMacDinh')?.value.trim() || ''
            });
        }

        await apiFetch('/api/Account/cap-nhat', 'PUT', payload);
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

// ─── Danh sách phòng đang thuê trong mục Tài khoản của tôi ──────────────────
async function loadTenantProfiles() {
    const box = document.getElementById('tenantProfileBody');
    if (!box) return;

    if (CURRENT_ROLE !== 'NguoiDung') {
        box.innerHTML = '<div style="padding:1rem;color:var(--text-light);">Mục này chỉ áp dụng cho tài khoản người dùng/khách thuê.</div>';
        return;
    }

    box.innerHTML = '<div style="text-align:center;padding:1.5rem;color:var(--text-light);"><i class="fas fa-spinner fa-spin"></i> Đang tải danh sách phòng...</div>';

    try {
        const profiles = await apiFetch('/api/NguoiThue/cua-toi') || [];
        const list = Array.isArray(profiles) ? profiles : [];

        if (!list.length) {
            box.innerHTML = '<div style="padding:1rem;color:var(--text-light);">Bạn chưa có phòng đang thuê. Khi chủ trọ duyệt yêu cầu thuê/lập hợp đồng, phòng sẽ xuất hiện tại đây.</div>';
            return;
        }

        box.innerHTML = `
            <div style="font-size:.85rem;color:var(--text-light);margin-bottom:1rem;">
                Thông tin cá nhân và ảnh CCCD được quản lý ở phần <b>Cập nhật thông tin</b> bên trên. Danh sách dưới đây chỉ thể hiện các phòng bạn đang/từng thuê.
            </div>
            <div style="display:grid;gap:.75rem;">
                ${list.map((p, idx) => `
                    <div style="display:flex;justify-content:space-between;align-items:center;gap:1rem;padding:.85rem 1rem;border:1px solid #e5e7eb;border-radius:.75rem;background:#f8fafc;">
                        <div>
                            <div style="font-weight:700;color:var(--text);">${idx + 1}. ${displayValue(p.tenPhong || ('Phòng #' + p.maPhong))}</div>
                            <div style="font-size:.85rem;color:var(--text-light);"><i class="fas fa-building"></i> ${displayValue(p.tenNhaTro)}</div>
                        </div>
                        <span class="badge badge-teal">Hồ sơ #${displayValue(p.maNguoiThue)}</span>
                    </div>`).join('')}
            </div>`;
    } catch (err) {
        box.innerHTML = `<div style="color:var(--error);padding:1rem;"><i class="fas fa-exclamation-circle"></i> ${err.message || 'Lỗi tải danh sách phòng đang thuê'}</div>`;
    }
}
