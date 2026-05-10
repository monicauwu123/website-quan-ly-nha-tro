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
    ],
    // Hook chạy sau khi bảng được render
    afterRender: async function() {
        const user = JSON.parse(localStorage.getItem('user') || '{}');
        if (user.vaiTro !== 'NguoiDung') return;
        await renderChuTroInfoCards();
    }
};

// ─── Cards thông tin chủ trọ ───────────────────────────────────────────────

async function renderChuTroInfoCards() {
    // Xóa cards cũ nếu có (tránh duplicate khi re-render)
    document.querySelectorAll('.chuTroInfoCard').forEach(el => el.remove());

    const generic = document.getElementById('genericSection');
    if (!generic) return;

    const wrapper = document.createElement('div');
    wrapper.className = 'chuTroInfoCard';
    wrapper.style.marginTop = '1.25rem';
    wrapper.innerHTML = `<div class="data-card" style="padding:1rem;text-align:center;color:var(--text-light);">
        <i class="fas fa-spinner fa-spin"></i> Đang tải thông tin chủ trọ...
    </div>`;
    generic.appendChild(wrapper);

    try {
        const raw = await apiFetch('/api/HopDong/ThongTinChuTro');
        const data = raw?.duLieu ?? raw;

        if (!data || !data.coChuTro || !data.danhSach?.length) {
            wrapper.innerHTML = `<div class="data-card" style="padding:1.25rem;">
                <h3 style="margin:0 0 .5rem;font-size:1rem;color:var(--text);">
                    <i class="fas fa-user-tie" style="color:#6366f1;"></i> Thông tin chủ trọ
                </h3>
                <p style="color:var(--text-light);margin:0;font-size:.9rem;">Chưa có hợp đồng thuê đang hiệu lực.</p>
            </div>`;
            return;
        }

        // Mỗi hợp đồng (mỗi phòng) render 1 card riêng
        wrapper.innerHTML = data.danhSach.map(ct => renderMotChuTroCard(ct)).join('');

    } catch (e) {
        wrapper.innerHTML = `<div class="data-card" style="padding:1rem;color:var(--text-light);font-size:.9rem;">
            <i class="fas fa-exclamation-circle" style="color:#f59e0b;"></i>
            Không thể tải thông tin chủ trọ.
        </div>`;
    }
}

function renderMotChuTroCard(ct) {
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

    return `
        <div class="data-card" style="margin-bottom:.75rem;">
            <div class="table-container" style="padding:1.25rem;">
                <h3 style="margin:0 0 1rem;font-size:1rem;color:var(--text);display:flex;align-items:center;gap:.5rem;flex-wrap:wrap;">
                    <span style="background:#ede9fe;border-radius:.5rem;padding:.3rem .55rem;">
                        <i class="fas fa-user-tie" style="color:#6366f1;"></i>
                    </span>
                    Thông tin chủ trọ
                    ${ct.tenPhong  ? `<span class="badge badge-teal" style="font-weight:500;">${window.AppFormat.escapeHtml(ct.tenPhong)}</span>` : ''}
                    ${ct.tenNhaTro ? `<span style="font-weight:400;color:var(--text-light);font-size:.9rem;">— ${window.AppFormat.escapeHtml(ct.tenNhaTro)}</span>` : ''}
                </h3>
                <div style="display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:.5rem .75rem;">
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
                ${bankSection}
            </div>
        </div>`;
}

window.renderChuTroInfoCards = renderChuTroInfoCards;
