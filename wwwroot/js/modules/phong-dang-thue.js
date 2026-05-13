// Module cấu hình: phòng đang thuê của người dùng
window.AppModules = window.AppModules || {};
window.AppModules.phongdangthue = {
    title: 'Phòng của tôi',
    endpoint: '/api/HopDong',
    pk: 'maHopDong',
    customModal: true,
    headers: [
        { label: 'Phòng', key: 'phong', render: (v, row) => v?.tenPhong || `Phòng #${row.maPhong}` },
        { label: 'Khách thuê', key: 'nguoiThue', render: (v, row) => v?.hoTen || `Khách #${row.maNguoiThue}` },
        { label: 'Ngày bắt đầu', key: 'ngayBatDau', render: v => window.AppFormat.date(v) },
        { label: 'Ngày kết thúc', key: 'ngayKetThuc', render: v => v ? window.AppFormat.date(v) : 'Không xác định' },
        { label: 'Tiền cọc', key: 'tienCoc', render: v => window.AppFormat.currency(v) },
        { label: 'Chủ trọ', key: null, render: (v, row) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (user.vaiTro !== 'NguoiDung') return '---';
            return `<button class="btn-action btn-edit" style="background:#0891b2;" onclick="openChuTroModal(${row.maHopDong})"><i class="fas fa-user-tie"></i> Xem chủ trọ</button>`;
        }},
        { label: 'Gia hạn', key: null, render: (v, row) => {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            if (user.vaiTro !== 'NguoiDung') return '---';
            const disabled = row.trangThai === 'KetThuc' || row.trangThai === 'Huy';
            return disabled
                ? '<span style="color:var(--text-light);font-size:.85rem;">---</span>'
                : `<button class="btn-action btn-edit" style="background:#6366f1;" onclick="openYeuCauGiaHanModal(${row.maHopDong})"><i class="fas fa-calendar-plus"></i> Gia hạn</button>`;
        }},
        { label: 'Trạng thái', key: 'trangThaiText', render: v => {
            const cls = v === 'Đang còn hiệu lực' ? 'badge-success' : v === 'Sắp hết hợp đồng' ? 'badge-warning' : 'badge-danger';
            return `<span class="badge ${cls}">${v || '---'}</span>`;
        }}
    ]
};

// ─── Modal thông tin chủ trọ ───────────────────────────────────────────────

async function openChuTroModal(maHopDong) {
    // Tạo hoặc lấy modal overlay riêng
    let overlay = document.getElementById('chuTroModal');
    if (!overlay) {
        overlay = document.createElement('div');
        overlay.id = 'chuTroModal';
        overlay.className = 'modal-overlay';
        overlay.style.cssText = 'display:none;position:fixed;inset:0;background:rgba(0,0,0,.45);z-index:9999;align-items:center;justify-content:center;';
        overlay.innerHTML = `
            <div class="modal-card animate-fade-in" style="max-width:540px;width:90%;max-height:90vh;overflow-y:auto;">
                <div class="modal-header">
                    <h2 id="chuTroModalTitle" style="display:flex;align-items:center;gap:.5rem;">
                        <span style="background:#ede9fe;border-radius:.5rem;padding:.3rem .55rem;">
                            <i class="fas fa-user-tie" style="color:#6366f1;"></i>
                        </span>
                        Thông tin chủ trọ
                    </h2>
                    <button class="close-btn" onclick="closeChuTroModal()">&times;</button>
                </div>
                <div id="chuTroModalBody" class="modal-body" style="padding:1.25rem;">
                    <div style="text-align:center;padding:1rem;color:var(--text-light);">
                        <i class="fas fa-spinner fa-spin"></i> Đang tải...
                    </div>
                </div>
            </div>`;
        // Đóng khi click nền
        overlay.addEventListener('click', function(e) {
            if (e.target === overlay) closeChuTroModal();
        });
        document.body.appendChild(overlay);
    }

    overlay.style.display = 'flex';
    const body = document.getElementById('chuTroModalBody');
    body.innerHTML = `<div style="text-align:center;padding:1rem;color:var(--text-light);"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>`;

    try {
        const raw = await apiFetch('/api/HopDong/ThongTinChuTro');
        const data = raw?.duLieu ?? raw;

        // Lọc đúng hợp đồng đang xem
        const hopDong = data?.danhSach?.find(ct => ct.maHopDong === maHopDong);

        if (!hopDong) {
            body.innerHTML = `<p style="color:var(--text-light);text-align:center;">Không tìm thấy thông tin chủ trọ cho phòng này.</p>`;
            return;
        }

        body.innerHTML = renderChuTroModalContent(hopDong);
    } catch (e) {
        body.innerHTML = `<p style="color:#f59e0b;text-align:center;"><i class="fas fa-exclamation-circle"></i> Không thể tải thông tin chủ trọ.</p>`;
    }
}

function closeChuTroModal() {
    const overlay = document.getElementById('chuTroModal');
    if (overlay) overlay.style.display = 'none';
}

function renderChuTroModalContent(ct) {
    const val = v => (v && String(v).trim()) ? window.AppFormat.escapeHtml(v) : '—';
    const hasBank = ct.soTaiKhoan && ct.soTaiKhoan.trim();

    const bankSection = hasBank ? `
        <div style="margin-top:1rem;padding-top:1rem;border-top:1px solid #e5e7eb;">
            <div style="font-weight:700;font-size:.85rem;color:#6366f1;margin-bottom:.6rem;text-transform:uppercase;letter-spacing:.03em;">
                <i class="fas fa-university"></i> Thông tin thanh toán
            </div>
            <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:.5rem .75rem;">
                ${ct.tenNganHang    ? `<div><span style="font-size:.8rem;color:var(--text-light);">Ngân hàng</span><div style="font-weight:600;">${val(ct.tenNganHang)}</div></div>` : ''}
                ${ct.soTaiKhoan    ? `<div><span style="font-size:.8rem;color:var(--text-light);">Số tài khoản</span><div style="font-weight:600;font-family:monospace;">${val(ct.soTaiKhoan)}</div></div>` : ''}
                ${ct.tenChuTaiKhoan? `<div><span style="font-size:.8rem;color:var(--text-light);">Chủ tài khoản</span><div style="font-weight:600;">${val(ct.tenChuTaiKhoan)}</div></div>` : ''}
                ${ct.maNganHang    ? `<div><span style="font-size:.8rem;color:var(--text-light);">Mã VietQR</span><div style="font-weight:600;">${val(ct.maNganHang)}</div></div>` : ''}
                ${ct.noiDungCK     ? `<div style="grid-column:1/-1;"><span style="font-size:.8rem;color:var(--text-light);">Nội dung CK mặc định</span><div style="font-weight:600;">${val(ct.noiDungCK)}</div></div>` : ''}
            </div>
        </div>` : '';

    const roomBadge = ct.tenPhong
        ? `<span class="badge badge-teal" style="font-weight:500;">${window.AppFormat.escapeHtml(ct.tenPhong)}</span>`
        : '';
    const nhaTroLabel = ct.tenNhaTro
        ? `<span style="font-weight:400;color:var(--text-light);font-size:.9rem;">— ${window.AppFormat.escapeHtml(ct.tenNhaTro)}</span>`
        : '';

    return `
        <div style="margin-bottom:.5rem;display:flex;align-items:center;gap:.5rem;flex-wrap:wrap;">
            ${roomBadge} ${nhaTroLabel}
        </div>
        <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:.5rem .75rem;margin-top:.75rem;">
            <div>
                <span style="font-size:.8rem;color:var(--text-light);">Họ tên</span>
                <div style="font-weight:600;">${val(ct.hoTen)}</div>
            </div>
            <div>
                <span style="font-size:.8rem;color:var(--text-light);">Số điện thoại</span>
                <div style="font-weight:600;">
                    ${ct.soDienThoai && ct.soDienThoai.trim()
                        ? `<a href="tel:${window.AppFormat.escapeHtml(ct.soDienThoai)}" style="color:var(--primary);text-decoration:none;">${val(ct.soDienThoai)}</a>`
                        : '—'}
                </div>
            </div>
            <div>
                <span style="font-size:.8rem;color:var(--text-light);">Email</span>
                <div style="font-weight:600;">
                    ${ct.email && ct.email.trim()
                        ? `<a href="mailto:${window.AppFormat.escapeHtml(ct.email)}" style="color:var(--primary);text-decoration:none;">${val(ct.email)}</a>`
                        : '—'}
                </div>
            </div>
            ${ct.diaChiNhaTro ? `<div style="grid-column:1/-1;"><span style="font-size:.8rem;color:var(--text-light);">Địa chỉ nhà trọ</span><div style="font-weight:600;">${val(ct.diaChiNhaTro)}</div></div>` : ''}
        </div>
        ${bankSection}`;
}

window.openChuTroModal  = openChuTroModal;
window.closeChuTroModal = closeChuTroModal;
