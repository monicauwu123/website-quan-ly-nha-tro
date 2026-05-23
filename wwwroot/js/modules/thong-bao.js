// ==========================================
// MODULE: thongbao
// Quản lý thông báo - Admin/ChuTro tạo, NguoiDung xem
// ==========================================

window.AppModules = window.AppModules || {};

(function () {
    'use strict';

    // ── Constants ──────────────────────────────────────────────────────────────
    const LOAI_THONG_BAO = {
        HoaDon: 'Hóa đơn',
        HopDong: 'Hợp đồng',
        DichVu: 'Dịch vụ',
        BaoCaoSuCo: 'Sự cố',
        ThuCong: 'Thủ công'
    };

    const LOAI_NGUOI_NHAN = {
        TatCa: 'Tất cả người thuê',
        NhaTro: 'Một nhà trọ',
        Phong: 'Một phòng',
        NguoiDung: 'Một người dùng'
    };

    const ICON_LOAI = {
        HoaDon: 'fa-file-invoice-dollar',
        HopDong: 'fa-file-contract',
        DichVu: 'fa-concierge-bell',
        BaoCaoSuCo: 'fa-tools',
        ThuCong: 'fa-bell'
    };

    const COLOR_LOAI = {
        HoaDon: '#6366f1',
        HopDong: '#0891b2',
        DichVu: '#7c3aed',
        BaoCaoSuCo: '#d97706',
        ThuCong: '#16a34a'
    };

    // ── State ──────────────────────────────────────────────────────────────────
    let _initData = null;
    let _badgeInterval = null;

    // ── Helpers ────────────────────────────────────────────────────────────────
    function unwrapApiResponse(res) {
        // api.js trả nguyên wrapper { thanhCong, duLieu }, còn dashboard.js lại trả thẳng duLieu.
        // Module này có thể chạy trước hoặc sau dashboard.js nên cần nhận cả hai dạng.
        return res && Object.prototype.hasOwnProperty.call(res, 'duLieu') ? res.duLieu : res;
    }

    function normalizeList(value) {
        const data = unwrapApiResponse(value);
        if (Array.isArray(data)) return data;
        if (Array.isArray(data?.$values)) return data.$values;
        return [];
    }

    function getRole() {
        // Dự án hiện lưu role chủ yếu trong localStorage.user.vaiTro,
        // không phải localStorage.vaiTro. Nếu chỉ đọc localStorage.vaiTro
        // thì tài khoản ChuTro/Admin sẽ bị hiểu là không có quyền tạo thông báo.
        const directRole = (localStorage.getItem('vaiTro') || '').trim();
        if (directRole) return directRole;

        try {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            return (user.vaiTro || user.role || '').trim();
        } catch (_) {
            return '';
        }
    }

    function isAdminOrChuTro() {
        const role = getRole();
        return role === 'Admin' || role === 'ChuTro';
    }

    function getThongBaoContentSlot() {
        return document.getElementById('thongBaoContainer')
            || document.querySelector('[data-module="thong-bao"] [data-slot="content"]')
            || document.querySelector('[data-module="thongbao"] [data-slot="content"]');
    }

    function thoiGianTuongDoi(dateStr) {
        if (!dateStr) return '---';
        const diff = Date.now() - new Date(dateStr).getTime();
        const minutes = Math.floor(diff / 60000);
        if (minutes < 1) return 'Vừa xong';
        if (minutes < 60) return `${minutes} phút trước`;
        const hours = Math.floor(minutes / 60);
        if (hours < 24) return `${hours} giờ trước`;
        const days = Math.floor(hours / 24);
        if (days < 7) return `${days} ngày trước`;
        return window.AppFormat.date(dateStr);
    }

    // ── Badge (số thông báo chưa đọc) ─────────────────────────────────────────
    async function capNhatBadge() {
        try {
            const badge = document.getElementById('thongBaoBadge');
            if (!badge) return;

            // Chủ trọ/Admin là người gửi/quản lý thông báo nên không cần chấm đỏ "chưa đọc".
            if (isAdminOrChuTro()) {
                badge.textContent = '';
                badge.style.display = 'none';
                return;
            }

            const res = await apiFetch('/api/ThongBao/chua-doc');
            const count = Number(unwrapApiResponse(res) ?? 0);
            badge.textContent = count > 99 ? '99+' : String(count);
            badge.style.display = count > 0 ? 'inline-flex' : 'none';
        } catch (_) { /* silent */ }
    }

    function startBadgePoller() {
        capNhatBadge();
        if (_badgeInterval) clearInterval(_badgeInterval);
        _badgeInterval = setInterval(capNhatBadge, 60000); // mỗi 1 phút
    }

    // ── Load init data (danh sách phòng & người dùng để chọn) ─────────────────
    async function loadInitData() {
        if (!isAdminOrChuTro()) return;
        try {
            const res = await apiFetch('/api/ThongBao/init-data');
            _initData = unwrapApiResponse(res) ?? null;
        } catch (_) { _initData = null; }
    }

    // ── Render danh sách dạng card (không dùng generic table vì UX đặc thù) ──
    function renderThongBaoList(list, container) {
        if (!list || list.length === 0) {
            container.innerHTML = `
                <div style="text-align:center;padding:3rem 1rem;color:#6b7280;">
                    <i class="fas fa-bell-slash" style="font-size:2.5rem;margin-bottom:1rem;display:block;opacity:.4;"></i>
                    <p>Không có thông báo nào.</p>
                </div>`;
            return;
        }

        const html = list.map(tb => {
            const icon = ICON_LOAI[tb.loaiThongBao] || 'fa-bell';
            const color = COLOR_LOAI[tb.loaiThongBao] || '#6b7280';
            const canMarkRead = tb.coTheDanhDauDoc !== false && !isAdminOrChuTro();
            const chuaDoc = canMarkRead && !tb.daDoc;
            const loaiText = LOAI_THONG_BAO[tb.loaiThongBao] || tb.loaiThongBao;

            let metaHtml = '';
            if (isAdminOrChuTro()) {
                const nguoiNhanText = tb.loaiNguoiNhan === 'TatCa' ? 'Tất cả người thuê'
                    : tb.loaiNguoiNhan === 'NhaTro' ? 'Nhà trọ'
                    : tb.loaiNguoiNhan === 'Phong' ? `Phòng: ${tb.tenPhong || '#' + tb.phongId}`
                    : `Người dùng: ${tb.tenNguoiNhan || '#' + tb.nguoiNhanId}`;
                metaHtml = `<span class="badge badge-info" style="font-size:.7rem;">${nguoiNhanText}</span>`;
            }

            const actionsHtml = canMarkRead
                ? (chuaDoc
                    ? `<button class="btn" style="padding:.2rem .6rem;font-size:.78rem;background:#e0e7ff;color:#3730a3;"
                            onclick="window.AppThongBao.danhDauDoc(${tb.thongBaoId}, this)">
                            <i class="fas fa-check"></i> Đánh dấu đọc
                       </button>`
                    : `<span style="color:#9ca3af;font-size:.78rem;"><i class="fas fa-check-double"></i> Đã đọc</span>`)
                : `<span style="color:#0f766e;font-size:.78rem;"><i class="fas fa-paper-plane"></i> Đã gửi</span>`;

            const anBtn = isAdminOrChuTro()
                ? `<button class="btn" style="padding:.2rem .6rem;font-size:.78rem;background:#fee2e2;color:#991b1b;margin-left:.4rem;"
                        onclick="window.AppThongBao.anThongBao(${tb.thongBaoId}, this)" title="Ẩn thông báo">
                        <i class="fas fa-eye-slash"></i>
                   </button>` : '';

            return `
            <div class="thongbao-card${chuaDoc ? ' chua-doc' : ''}" data-id="${tb.thongBaoId}"
                 style="border-left:4px solid ${color};background:${chuaDoc ? '#f0f9ff' : '#fff'};
                        border-radius:.5rem;padding:1rem 1.25rem;margin-bottom:.75rem;
                        box-shadow:0 1px 4px rgba(0,0,0,.07);transition:background .2s;">
                <div style="display:flex;align-items:flex-start;gap:1rem;">
                    <div style="width:2.4rem;height:2.4rem;border-radius:50%;background:${color}22;
                                display:flex;align-items:center;justify-content:center;flex-shrink:0;">
                        <i class="fas ${icon}" style="color:${color};font-size:1rem;"></i>
                    </div>
                    <div style="flex:1;min-width:0;">
                        <div style="display:flex;align-items:center;gap:.5rem;flex-wrap:wrap;">
                            ${chuaDoc ? '<span style="width:.5rem;height:.5rem;border-radius:50%;background:#3b82f6;display:inline-block;flex-shrink:0;" title="Chưa đọc"></span>' : ''}
                            <strong style="font-size:.95rem;">${window.AppFormat.escapeHtml(tb.tieuDe)}</strong>
                            <span class="badge badge-secondary" style="font-size:.68rem;">${loaiText}</span>
                            ${metaHtml}
                        </div>
                        <p style="color:#374151;font-size:.875rem;margin:.4rem 0;white-space:pre-line;">${window.AppFormat.escapeHtml(tb.noiDung)}</p>
                        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;">
                            <span style="color:#9ca3af;font-size:.78rem;">
                                <i class="fas fa-clock"></i> ${thoiGianTuongDoi(tb.ngayTao)}
                                ${tb.nguoiTaoId ? `&nbsp;·&nbsp;<i class="fas fa-user"></i> ${window.AppFormat.escapeHtml(tb.tenNguoiTao || 'Hệ thống')}` : '· Hệ thống'}
                            </span>
                            <div style="display:flex;align-items:center;">
                                ${actionsHtml}
                                ${anBtn}
                            </div>
                        </div>
                    </div>
                </div>
            </div>`;
        }).join('');

        container.innerHTML = html;
    }

    // ── Load & render section ──────────────────────────────────────────────────
    async function loadThongBaoSection(contentSlot) {
        contentSlot.innerHTML = '<div style="padding:2rem;color:#6b7280;text-align:center;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>';
        try {
            const res = await apiFetch('/api/ThongBao');
            const list = normalizeList(res);

            const toolbar = buildToolbar(list);
            const contentDiv = document.createElement('div');
            contentDiv.style.cssText = 'padding:.5rem 0;';
            renderThongBaoList(list, contentDiv);

            contentSlot.innerHTML = '';
            contentSlot.appendChild(toolbar);
            contentSlot.appendChild(contentDiv);
        } catch (e) {
            contentSlot.innerHTML = `<div style="padding:2rem;color:#991b1b;">Lỗi tải thông báo: ${e.message}</div>`;
        }
    }

    function buildToolbar(list) {
        const chuaDocCount = list.filter(tb => tb.coTheDanhDauDoc !== false && !tb.daDoc).length;
        const adminView = isAdminOrChuTro();

        const wrapper = document.createElement('div');
        wrapper.className = 'generic-table-toolbar';
        wrapper.style.cssText = 'display:flex;align-items:center;gap:.75rem;flex-wrap:wrap;margin-bottom:1rem;';

        const info = document.createElement('span');
        info.style.cssText = 'color:#6b7280;font-size:.875rem;flex:1;';
        info.innerHTML = adminView
            ? `<strong>${list.length}</strong> thông báo đã gửi/đang hiển thị`
            : `<strong>${list.length}</strong> thông báo · <strong style="color:#3b82f6;">${chuaDocCount}</strong> chưa đọc`;
        wrapper.appendChild(info);

        if (adminView) {
            const btnTao = document.createElement('button');
            btnTao.className = 'module-btn module-btn-primary';
            btnTao.innerHTML = '<i class="fas fa-plus"></i> Thêm mới';
            btnTao.onclick = () => window.AppThongBao.openCreateModal();
            wrapper.appendChild(btnTao);
        }

        if (!adminView && chuaDocCount > 0) {
            const btnDocTatCa = document.createElement('button');
            btnDocTatCa.className = 'module-btn module-btn-muted';
            btnDocTatCa.innerHTML = '<i class="fas fa-check-double"></i> Đánh dấu tất cả đã đọc';
            btnDocTatCa.onclick = () => window.AppThongBao.docTatCa();
            wrapper.appendChild(btnDocTatCa);
        }

        return wrapper;
    }

    // ── Đánh dấu đọc 1 ────────────────────────────────────────────────────────
    async function danhDauDoc(id, btn) {
        try {
            if (btn) btn.disabled = true;
            await apiFetch(`/api/ThongBao/${id}/da-doc`, 'PUT');

            // Cập nhật UI ngay
            const card = document.querySelector(`.thongbao-card[data-id="${id}"]`);
            if (card) {
                card.classList.remove('chua-doc');
                card.style.background = '#fff';
                const dot = card.querySelector('span[title="Chưa đọc"]');
                if (dot) dot.remove();
                const actDiv = card.querySelector('div[style*="justify-content:space-between"] > div');
                if (actDiv) {
                    actDiv.innerHTML = `<span style="color:#9ca3af;font-size:.78rem;"><i class="fas fa-check-double"></i> Đã đọc</span>` +
                        (actDiv.innerHTML.includes('fa-eye-slash') ? actDiv.innerHTML.replace(/.*btn.*fa-eye-slash.*?<\/button>/s, m => m) : '');
                }
            }

            await capNhatBadge();
            if (typeof window.refreshSidebarBadges === 'function') await window.refreshSidebarBadges();
        } catch (e) {
            showToast('Lỗi: ' + e.message, 'error');
            if (btn) btn.disabled = false;
        }
    }

    // ── Đánh dấu tất cả đã đọc ────────────────────────────────────────────────
    async function docTatCa() {
        try {
            const res = await apiFetch('/api/ThongBao/doc-tat-ca', 'PUT');
            showToast(res?.thongBao || 'Đã đánh dấu tất cả đã đọc', 'success');
            // Reload section
            const contentSlot = getThongBaoContentSlot();
            if (contentSlot) await loadThongBaoSection(contentSlot);
            await capNhatBadge();
            if (typeof window.refreshSidebarBadges === 'function') await window.refreshSidebarBadges();
        } catch (e) {
            showToast('Lỗi: ' + e.message, 'error');
        }
    }

    // ── Ẩn thông báo (Admin/ChuTro) ───────────────────────────────────────────
    async function anThongBao(id, btn) {
        if (!confirm('Ẩn thông báo này? Người nhận sẽ không thấy nữa.')) return;
        try {
            if (btn) btn.disabled = true;
            await apiFetch(`/api/ThongBao/${id}/an`, 'PUT');
            showToast('Đã ẩn thông báo.', 'success');
            const card = document.querySelector(`.thongbao-card[data-id="${id}"]`);
            if (card) card.style.opacity = '0', setTimeout(() => card.remove(), 300);
        } catch (e) {
            showToast('Lỗi: ' + e.message, 'error');
            if (btn) btn.disabled = false;
        }
    }

    // ── Modal tạo thông báo (Admin/ChuTro) ────────────────────────────────────
    async function openCreateModal() {
        if (!isAdminOrChuTro()) return;
        if (!_initData) await loadInitData();

        const nhaTros = _initData?.nhaTros ?? [];
        const phongs = _initData?.phongs ?? [];
        const nguoiDungs = _initData?.nguoiDungs ?? [];

        const nhaTroOptions = nhaTros.map(n =>
            `<option value="${n.maNhaTro}">${window.AppFormat.escapeHtml(n.tenNhaTro || 'Nhà trọ #' + n.maNhaTro)}${n.diaChi ? ' - ' + window.AppFormat.escapeHtml(n.diaChi) : ''}</option>`
        ).join('');

        const phongOptions = phongs.map(p =>
            `<option value="${p.maPhong}">${window.AppFormat.escapeHtml(p.tenPhong)} (${window.AppFormat.escapeHtml(p.tenNhaTro || '')})</option>`
        ).join('');

        const nguoiDungOptions = nguoiDungs.map(u =>
            `<option value="${u.maNguoiDung}">${window.AppFormat.escapeHtml(u.hoTen || u.email)} - ${u.soDienThoai || ''}</option>`
        ).join('');

        const modalBody = document.getElementById('modalFields');
        const modalTitle = document.getElementById('modalTitle');
        const modalFooter = document.querySelector('#universalModal .modal-footer');
        const modal = document.getElementById('universalModal');
        const modalForm = document.getElementById('modalForm');

        if (!modalBody || !modal) {
            showToast('Không tìm thấy modal container.', 'error');
            return;
        }

        if (typeof resetModalFooter === 'function') resetModalFooter();
        if (modalTitle) modalTitle.textContent = 'Tạo thông báo mới';

        modalBody.innerHTML = `
            <div class="form-group" style="grid-column:1/-1;">
                <label>Tiêu đề <span style="color:var(--error)">*</span></label>
                <input id="tbTieuDe" type="text" class="form-control" placeholder="Nhập tiêu đề thông báo..." maxlength="200" required>
            </div>

            <div class="form-group">
                <label>Gửi đến <span style="color:var(--error)">*</span></label>
                <select id="tbLoaiNguoiNhan" class="form-control" onchange="window.AppThongBao._onLoaiNguoiNhanChange()" required>
                    <option value="TatCa">Tất cả người thuê</option>
                    <option value="NhaTro">Một nhà trọ cụ thể</option>
                    <option value="Phong">Một phòng cụ thể</option>
                    <option value="NguoiDung">Một người dùng cụ thể</option>
                </select>
            </div>

            <div id="tbNhaTroGroup" class="form-group" style="display:none;">
                <label>Chọn nhà trọ <span style="color:var(--error)">*</span></label>
                <select id="tbNhaTroId" class="form-control">
                    <option value="">-- Chọn nhà trọ --</option>
                    ${nhaTroOptions}
                </select>
            </div>

            <div id="tbPhongGroup" class="form-group" style="display:none;">
                <label>Chọn phòng <span style="color:var(--error)">*</span></label>
                <select id="tbPhongId" class="form-control">
                    <option value="">-- Chọn phòng --</option>
                    ${phongOptions}
                </select>
            </div>

            <div id="tbNguoiDungGroup" class="form-group" style="display:none;">
                <label>Chọn người dùng <span style="color:var(--error)">*</span></label>
                <select id="tbNguoiNhanId" class="form-control">
                    <option value="">-- Chọn người dùng --</option>
                    ${nguoiDungOptions}
                </select>
            </div>

            <div class="form-group" style="grid-column:1/-1;">
                <label>Nội dung <span style="color:var(--error)">*</span></label>
                <textarea id="tbNoiDung" class="form-control" rows="5" placeholder="Nhập nội dung thông báo..." maxlength="2000" required></textarea>
            </div>

            <div class="form-group" style="grid-column:1/-1;background:#f8fffe;border:1px solid #d1fae5;border-radius:.75rem;padding:.85rem;color:var(--text-light);">
                <strong>Lưu ý:</strong> Chủ trọ/Admin có thể gửi thông báo cho tất cả người thuê, một nhà trọ, một phòng cụ thể hoặc một người dùng cụ thể.
            </div>`;

        if (modalFooter) {
            modalFooter.innerHTML = `
                <button type="button" class="btn btn-secondary" onclick="closeModal()">Hủy</button>
                <button type="submit" class="btn btn-primary">
                    <i class="fas fa-save"></i> Lưu
                </button>`;
        }

        if (modalForm) {
            modalForm.onsubmit = (e) => {
                e.preventDefault();
                window.AppThongBao._submitCreate();
            };
        }

        modal.style.display = 'flex';
    }

    function _onLoaiNguoiNhanChange() {
        const val = document.getElementById('tbLoaiNguoiNhan')?.value;
        const nhaTroGroup = document.getElementById('tbNhaTroGroup');
        const phongGroup = document.getElementById('tbPhongGroup');
        const nguoiDungGroup = document.getElementById('tbNguoiDungGroup');
        if (nhaTroGroup) nhaTroGroup.style.display = val === 'NhaTro' ? 'block' : 'none';
        if (phongGroup) phongGroup.style.display = val === 'Phong' ? 'block' : 'none';
        if (nguoiDungGroup) nguoiDungGroup.style.display = val === 'NguoiDung' ? 'block' : 'none';
    }

    async function _submitCreate() {
        const tieuDe = document.getElementById('tbTieuDe')?.value?.trim();
        const noiDung = document.getElementById('tbNoiDung')?.value?.trim();
        const loaiNguoiNhan = document.getElementById('tbLoaiNguoiNhan')?.value;
        const nhaTroId = document.getElementById('tbNhaTroId')?.value;
        const phongId = document.getElementById('tbPhongId')?.value;
        const nguoiNhanId = document.getElementById('tbNguoiNhanId')?.value;

        if (!tieuDe) { showToast('Vui lòng nhập tiêu đề.', 'error'); return; }
        if (!noiDung) { showToast('Vui lòng nhập nội dung.', 'error'); return; }
        if (loaiNguoiNhan === 'NhaTro' && !nhaTroId) { showToast('Vui lòng chọn nhà trọ.', 'error'); return; }
        if (loaiNguoiNhan === 'Phong' && !phongId) { showToast('Vui lòng chọn phòng.', 'error'); return; }
        if (loaiNguoiNhan === 'NguoiDung' && !nguoiNhanId) { showToast('Vui lòng chọn người dùng.', 'error'); return; }

        const payload = {
            tieuDe,
            noiDung,
            loaiThongBao: 'ThuCong',
            loaiNguoiNhan,
            nhaTroId: loaiNguoiNhan === 'NhaTro' ? parseInt(nhaTroId) : null,
            phongId: loaiNguoiNhan === 'Phong' ? parseInt(phongId) : null,
            nguoiNhanId: loaiNguoiNhan === 'NguoiDung' ? parseInt(nguoiNhanId) : null
        };

        try {
            const btn = document.querySelector('#universalModal .modal-footer .btn-primary');
            if (btn) { btn.disabled = true; btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang lưu...'; }

            await apiFetch('/api/ThongBao', 'POST', payload);
            showToast('Thông báo đã được gửi thành công!', 'success');
            if (typeof closeModal === 'function') closeModal();

            // Reload section nếu đang ở mục thông báo
            const contentSlot = getThongBaoContentSlot();
            if (contentSlot) await loadThongBaoSection(contentSlot);
            if (typeof window.refreshSidebarBadges === 'function') await window.refreshSidebarBadges();
        } catch (e) {
            showToast('Lỗi: ' + e.message, 'error');
            const btn = document.querySelector('#universalModal .modal-footer .btn-primary');
            if (btn) { btn.disabled = false; btn.innerHTML = '<i class="fas fa-save"></i> Lưu'; }
        }
    }

    // ── Đăng ký module (tích hợp với generic loader) ──────────────────────────
    window.AppModules.thongbao = {
        title: 'Thông Báo',
        endpoint: '/api/ThongBao',
        pk: 'thongBaoId',
        customModal: true,

        // Chỉ dùng khi bảng mặc định được gọi.
        headers: [
            { label: 'Tiêu đề', key: 'tieuDe' },
            { label: 'Nội dung', key: 'noiDung' },
            { label: 'Loại', key: 'loaiThongBaoText' },
            { label: 'Gửi đến', key: 'loaiNguoiNhanText' },
            { label: 'Ngày tạo', key: 'ngayTao', render: v => window.AppFormat.date(v) },
            {
                label: 'Trạng thái', key: 'daDoc', render: v =>
                    v ? '<span class="badge badge-success">Đã đọc</span>'
                      : '<span class="badge badge-warning">Chưa đọc</span>'
            }
        ],

        // Hook: override phần render content nếu dashboard gọi loadGenericSection
        onLoad: async function (contentSlot) {
            await loadThongBaoSection(contentSlot);
        }
    };

    // ── Public API ─────────────────────────────────────────────────────────────
    window.AppThongBao = {
        init: async function () {
            await startBadgePoller();
            if (isAdminOrChuTro()) await loadInitData();
        },
        openCreateModal,
        danhDauDoc,
        docTatCa,
        anThongBao,
        refreshBadge: capNhatBadge,
        _onLoaiNguoiNhanChange,
        _submitCreate
    };

    // Tự khởi động badge ngay khi script load xong
    if (document.readyState !== 'loading') {
        window.AppThongBao.init();
    } else {
        document.addEventListener('DOMContentLoaded', () => window.AppThongBao.init());
    }
})();
