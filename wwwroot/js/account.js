// ==========================================
// ACCOUNT.JS — Tài khoản của tôi
// ==========================================

let currentProfileData = null;

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
        return `<div class="account-empty-state"><i class="fas fa-image"></i> Chưa có ${label.toLowerCase()}</div>`;
    }

    const safeUrl = escapeHtml(url);
    return `<a href="${safeUrl}" target="_blank"><img src="${safeUrl}" alt="${escapeHtml(label)}" style="width:100%;max-height:220px;object-fit:contain;border-radius:1rem;background:#f8fafc;border:1px solid #e5e7eb;box-shadow:0 8px 20px rgba(15,23,42,.06);"></a>`;
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


// ─── Toggle account panels ──────────────────────────────────────────────────
function toggleAccountPanel(panelId, button) {
    const panel = document.getElementById(panelId);
    if (!panel) return;

    const isHidden = panel.style.display === 'none' || !panel.style.display;
    panel.style.display = isHidden ? 'block' : 'none';

    if (button) {
        const icon = button.querySelector('i');
        if (icon) {
            icon.classList.toggle('fa-chevron-down', !isHidden);
            icon.classList.toggle('fa-chevron-up', isHidden);
        }
        const textNode = Array.from(button.childNodes).find(n => n.nodeType === Node.TEXT_NODE);
        if (textNode) textNode.textContent = isHidden ? ' Thu gọn' : ' Mở';
    }
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
    body.innerHTML = '<div class="account-loading"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>';

    try {
        const res  = await apiFetch('/api/Account/thong-tin');
        const data = res.duLieu || res;
        currentProfileData = data;

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

        body.innerHTML = `
            <div class="account-profile-grid">
                ${rows.map(r => `
                    <div class="profile-row">
                        <div class="pr-icon"><i class="fas ${r.icon}"></i></div>
                        <div>
                            <div class="pr-label">${escapeHtml(r.label)}</div>
                            <div class="pr-value">${displayValue(r.value)}</div>
                        </div>
                    </div>`).join('')}
            </div>
            ${data.vaiTro === 'NguoiDung' ? `
                <div class="account-cccd-block">
                    <div class="account-cccd-title"><i class="fas fa-address-card"></i> Ảnh căn cước công dân</div>
                    <div class="account-cccd-grid">
                        <div class="account-cccd-item">
                            <div class="account-cccd-label">Mặt trước</div>
                            ${renderCccdPreview(data.anhCccdMatTruoc, 'CCCD mặt trước')}
                        </div>
                        <div class="account-cccd-item">
                            <div class="account-cccd-label">Mặt sau</div>
                            ${renderCccdPreview(data.anhCccdMatSau, 'CCCD mặt sau')}
                        </div>
                    </div>
                </div>` : ''}`;

        const tenantCard = document.getElementById('tenantProfileCard');
        if (tenantCard) tenantCard.style.display = data.vaiTro === 'NguoiDung' ? '' : 'none';

        const readUsername = document.getElementById('readUsername');
        const readVaiTro = document.getElementById('readVaiTro');
        const readTrangThai = document.getElementById('readTrangThai');
        const readNgayTao = document.getElementById('readNgayTao');
        if (readUsername) readUsername.value = data.tenDangNhap || '';
        if (readVaiTro) readVaiTro.value = roleLabel[data.vaiTro] || data.vaiTro || '';
        if (readTrangThai) readTrangThai.value = data.trangThai ? 'Đang hoạt động' : 'Bị khóa';
        if (readNgayTao) readNgayTao.value = formatDateDisplay(data.ngayTao);

        document.getElementById('userAvatar').textContent  = initial;
        document.getElementById('userName').textContent    = data.hoTen || data.tenDangNhap;


    } catch (e) {
        body.innerHTML = `<div style="color:var(--error);padding:1rem;"><i class="fas fa-exclamation-circle"></i> ${e.message || 'Lỗi tải thông tin'}</div>`;
    }
}

// ─── Modal: Cập nhật thông tin ────────────────────────────────────────────────
function setAccountModalFooter(submitHtml, submitClass = 'btn-primary') {
    const footer = document.querySelector('#universalModal .modal-footer');
    if (!footer) return;
    footer.innerHTML = `
        <button type="button" class="btn btn-secondary" style="width:auto;" onclick="closeModal()">Hủy</button>
        <button type="submit" class="btn ${submitClass}" style="width:auto;">
            ${submitHtml}
        </button>`;
}

async function openProfileEditModal() {
    if (!currentProfileData) {
        await loadProfile();
    }
    const data = currentProfileData || {};
    const modal = document.getElementById('universalModal');
    const title = document.getElementById('modalTitle');
    const body = document.getElementById('modalFields');
    const form = document.getElementById('modalForm');
    if (!modal || !title || !body || !form) return;

    resetModalFooter();
    title.textContent = 'Cập nhật thông tin';
    body.innerHTML = `
        <div class="account-form-grid" style="grid-column:1/-1;">
            <div class="form-group">
                <label>Họ tên</label>
                <input type="text" id="editHoTen" class="form-control" value="${escapeHtml(data.hoTen || '')}" placeholder="Nguyễn Văn A" required maxlength="50">
            </div>
            <div class="form-group">
                <label>Email</label>
                <input type="email" id="editEmail" class="form-control" value="${escapeHtml(data.email || '')}" placeholder="email@example.com" required>
            </div>
            <div class="form-group">
                <label>Số điện thoại</label>
                <input type="text" id="editPhone" class="form-control" value="${escapeHtml(data.soDienThoai || '')}" placeholder="0912345678" maxlength="15">
            </div>

            ${(CURRENT_ROLE === 'ChuTro' || CURRENT_ROLE === 'Admin') ? `
                <div class="account-mini-divider">Thông tin nhận thanh toán</div>
                <div class="form-group">
                    <label>Tên ngân hàng</label>
                    <input type="text" id="editTenNganHang" class="form-control" value="${escapeHtml(data.tenNganHang || '')}" placeholder="VD: Vietcombank" maxlength="100">
                </div>
                <div class="form-group">
                    <label>Mã ngân hàng VietQR</label>
                    <input type="text" id="editMaNganHang" class="form-control" value="${escapeHtml(data.maNganHang || '')}" placeholder="VD: VCB, BIDV, MB, TCB..." maxlength="50">
                    <small class="account-help-text">Dùng mã ngân hàng VietQR để tạo mã QR thanh toán.</small>
                </div>
                <div class="form-group">
                    <label>Số tài khoản</label>
                    <input type="text" id="editSoTaiKhoan" class="form-control" value="${escapeHtml(data.soTaiKhoan || '')}" placeholder="Nhập số tài khoản nhận tiền" maxlength="50">
                </div>
                <div class="form-group">
                    <label>Tên chủ tài khoản</label>
                    <input type="text" id="editTenChuTaiKhoan" class="form-control" value="${escapeHtml(data.tenChuTaiKhoan || '')}" placeholder="Tên trên tài khoản ngân hàng" maxlength="100">
                </div>
                <div class="form-group account-span-2">
                    <label>Nội dung chuyển khoản mặc định</label>
                    <input type="text" id="editNoiDungChuyenKhoanMacDinh" class="form-control" value="${escapeHtml(data.noiDungChuyenKhoanMacDinh || '')}" placeholder="VD: Thanh toán hóa đơn {MaHoaDon} phòng {TenPhong}" maxlength="255">
                    <small class="account-help-text">Có thể dùng: {MaHoaDon}, {KyHoaDon}, {TenPhong}</small>
                </div>` : ''}

            ${CURRENT_ROLE === 'NguoiDung' ? `
                <div class="account-mini-divider">Thông tin định danh</div>
                <div class="form-group">
                    <label>CCCD/CMND</label>
                    <input type="text" id="editCCCD" class="form-control" value="${escapeHtml(data.cccd || '')}" placeholder="Nhập số CCCD/CMND" maxlength="20">
                </div>
                <div class="form-group">
                    <label>Ngày sinh</label>
                    <input type="date" id="editNgaySinh" class="form-control" value="${escapeHtml(formatDateInput(data.ngaySinh))}">
                </div>
                <div class="form-group">
                    <label>Giới tính</label>
                    <select id="editGioiTinh" class="form-control">
                        <option value="">-- Chọn --</option>
                        <option value="Nam" ${data.gioiTinh === 'Nam' ? 'selected' : ''}>Nam</option>
                        <option value="Nữ" ${data.gioiTinh === 'Nữ' ? 'selected' : ''}>Nữ</option>
                        <option value="Khác" ${data.gioiTinh === 'Khác' ? 'selected' : ''}>Khác</option>
                    </select>
                </div>
                <div class="form-group">
                    <label>Quốc tịch</label>
                    <input type="text" id="editQuocTich" class="form-control" value="${escapeHtml(data.quocTich || 'Việt Nam')}" placeholder="Việt Nam" maxlength="50">
                </div>
                <div class="form-group">
                    <label>Nơi công tác</label>
                    <input type="text" id="editNoiCongTac" class="form-control" value="${escapeHtml(data.noiCongTac || '')}" maxlength="100">
                </div>
                <div class="form-group account-span-2">
                    <label>Địa chỉ</label>
                    <input type="text" id="editDiaChi" class="form-control" value="${escapeHtml(data.diaChi || '')}" maxlength="255">
                </div>
                <div class="form-group">
                    <label>Ảnh CCCD mặt trước</label>
                    <input type="file" id="editAnhCccdMatTruocFile" class="form-control" accept="image/*">
                    <input type="hidden" id="editAnhCccdMatTruoc" value="${escapeHtml(data.anhCccdMatTruoc || '')}">
                    <div id="editAnhCccdMatTruocPreview" class="account-image-preview">${renderCccdPreview(data.anhCccdMatTruoc, 'CCCD mặt trước')}</div>
                </div>
                <div class="form-group">
                    <label>Ảnh CCCD mặt sau</label>
                    <input type="file" id="editAnhCccdMatSauFile" class="form-control" accept="image/*">
                    <input type="hidden" id="editAnhCccdMatSau" value="${escapeHtml(data.anhCccdMatSau || '')}">
                    <div id="editAnhCccdMatSauPreview" class="account-image-preview">${renderCccdPreview(data.anhCccdMatSau, 'CCCD mặt sau')}</div>
                </div>` : ''}
        </div>`;
    setAccountModalFooter('<i class="fas fa-save"></i> Lưu thay đổi');
    form.onsubmit = submitProfileEdit;
    modal.style.display = 'flex';
}

async function submitProfileEdit(e) {
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
        closeModal();
        await loadProfile();
    } catch (err) {
        showToast(err.message || 'Lỗi cập nhật thông tin', 'error');
    } finally {
        btn.innerHTML = orig;
        btn.disabled  = false;
    }
}

// ─── Modal: Đổi mật khẩu ─────────────────────────────────────────────────────
function openChangePasswordModal() {
    const modal = document.getElementById('universalModal');
    const title = document.getElementById('modalTitle');
    const body = document.getElementById('modalFields');
    const form = document.getElementById('modalForm');
    if (!modal || !title || !body || !form) return;

    resetModalFooter();
    title.textContent = 'Đổi mật khẩu';
    body.innerHTML = `
        <div class="account-form-grid" style="grid-column:1/-1;">
            <div class="form-group">
                <label>Mật khẩu cũ</label>
                <div style="position:relative;">
                    <input type="password" id="oldPassword" class="form-control" placeholder="••••••••" required style="padding-right:2.5rem;">
                    <button type="button" onclick="togglePasswordField('oldPassword', this)" class="account-eye-btn"><i class="fas fa-eye"></i></button>
                </div>
            </div>
            <div class="form-group">
                <label>Mật khẩu mới</label>
                <div style="position:relative;">
                    <input type="password" id="newPassword" class="form-control" placeholder="Tối thiểu 6 ký tự" required minlength="6" style="padding-right:2.5rem;" oninput="checkStrength(this.value)">
                    <button type="button" onclick="togglePasswordField('newPassword', this)" class="account-eye-btn"><i class="fas fa-eye"></i></button>
                </div>
                <div class="strength-bar"><div id="strengthFill" class="bar-fill" style="width:0%;"></div></div>
                <div id="strengthLabel" class="account-help-text"></div>
            </div>
            <div class="form-group">
                <label>Nhập lại mật khẩu mới</label>
                <input type="password" id="confirmPassword" class="form-control" placeholder="Nhập lại..." required>
            </div>
        </div>`;
    setAccountModalFooter('<i class="fas fa-shield-alt"></i> Đổi mật khẩu', 'btn-warning');
    form.onsubmit = submitChangePassword;
    modal.style.display = 'flex';
}

async function submitChangePassword(e) {
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
        closeModal();
    } catch (err) {
        showToast(err.message || 'Lỗi đổi mật khẩu', 'error');
    } finally {
        btn.innerHTML = orig;
        btn.disabled  = false;
    }
}

// ─── Danh sách phòng đang thuê trong mục Tài khoản của tôi ──────────────────
async function loadTenantProfiles() {
    const box = document.getElementById('tenantProfileBody');
    if (!box) return;

    const tenantCard = document.getElementById('tenantProfileCard');
    if (CURRENT_ROLE !== 'NguoiDung') {
        if (tenantCard) tenantCard.style.display = 'none';
        return;
    }
    if (tenantCard) tenantCard.style.display = '';

    box.innerHTML = '<div class="account-loading"><i class="fas fa-spinner fa-spin"></i> Đang tải danh sách phòng...</div>';

    try {
        const profiles = await apiFetch('/api/NguoiThue/cua-toi') || [];
        const list = Array.isArray(profiles) ? profiles : [];

        if (!list.length) {
            box.innerHTML = '<div class="account-empty-state"><i class="fas fa-home"></i> Bạn chưa có phòng đang thuê. Khi chủ trọ duyệt yêu cầu thuê/lập hợp đồng, phòng sẽ xuất hiện tại đây.</div>';
            return;
        }

        box.innerHTML = `
            <div class="account-tenant-note">
                <i class="fas fa-circle-info"></i>
                Thông tin cá nhân và ảnh CCCD được quản lý ở phần <b>Cập nhật thông tin</b>. Danh sách dưới đây chỉ thể hiện các phòng bạn đang/từng thuê.
            </div>
            <div class="account-tenant-grid">
                ${list.map((p, idx) => `
                    <div class="account-tenant-item">
                        <div class="account-tenant-title">${idx + 1}. ${displayValue(p.tenPhong || ('Phòng #' + p.maPhong))}</div>
                        <div class="account-tenant-sub"><i class="fas fa-building"></i> ${displayValue(p.tenNhaTro)}</div>
                        <span class="account-tenant-badge"><i class="fas fa-id-card"></i> Hồ sơ #${displayValue(p.maNguoiThue)}</span>
                    </div>`).join('')}
            </div>`;
    } catch (err) {
        box.innerHTML = `<div style="color:var(--error);padding:1rem;"><i class="fas fa-exclamation-circle"></i> ${err.message || 'Lỗi tải danh sách phòng đang thuê'}</div>`;
    }
}
