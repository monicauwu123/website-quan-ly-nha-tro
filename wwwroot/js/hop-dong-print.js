// ============================================================
// HOP DONG PRINT — Xem trước & In hợp đồng (window.print)
// ============================================================
window.HopDongPrint = (function () {

    const fmt     = v => (v != null && v !== '') ? new Intl.NumberFormat('vi-VN').format(v) + 'đ' : '0đ';
    const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN') : '---';

    function tinhSoThang(ngayBatDau, ngayKetThuc) {
        if (!ngayBatDau || !ngayKetThuc) return '---';
        const a = new Date(ngayBatDau), b = new Date(ngayKetThuc);
        const months = (b.getFullYear() - a.getFullYear()) * 12 + (b.getMonth() - a.getMonth());
        return months > 0 ? `${months} tháng` : '---';
    }

    function buildHtml(hd) {
        const soThang = tinhSoThang(hd.ngayBatDau, hd.ngayKetThuc);
        const ngayLapText = fmtDate(hd.ngayLap || new Date());

        return `
<div class="hd-header">
    <div class="hd-landlord-info">
        <h2><i class="fas fa-home" style="margin-right:6px;font-size:1rem;"></i>${hd.tenNhaTro || 'Nhà trọ'}</h2>
        ${hd.diaChiNhaTro ? `<p><i class="fas fa-map-marker-alt" style="margin-right:4px;"></i>${hd.diaChiNhaTro}</p>` : ''}
        ${hd.tenChuTro && hd.tenChuTro !== '---' ? `<p><i class="fas fa-user" style="margin-right:4px;"></i>Chủ trọ: ${hd.tenChuTro}</p>` : ''}
        ${hd.sdtChuTro ? `<p><i class="fas fa-phone" style="margin-right:4px;"></i>${hd.sdtChuTro}</p>` : ''}
    </div>
    <div class="hd-doc-info">
        <div class="hd-code">HỢP ĐỒNG #${hd.maHopDong}</div>
        <div class="hd-date">Ngày lập: ${ngayLapText}</div>
    </div>
</div>

<div class="hd-title">
    <h1>Hợp Đồng Thuê Phòng</h1>
    <p class="hd-subtitle">${hd.tenPhong || ''}${hd.tenNhaTro ? ' – ' + hd.tenNhaTro : ''}</p>
</div>

<div class="hd-parties">
    <div class="hd-party-section">
        <h4><i class="fas fa-user-tie" style="margin-right:4px;"></i>Bên A – Chủ trọ (Bên cho thuê)</h4>
        <p><strong>${hd.tenChuTro || '---'}</strong></p>
        ${hd.sdtChuTro ? `<p><span>Điện thoại:</span> ${hd.sdtChuTro}</p>` : ''}
        ${hd.emailChuTro ? `<p><span>Email:</span> ${hd.emailChuTro}</p>` : ''}
        ${hd.diaChiNhaTro ? `<p><span>Địa chỉ:</span> ${hd.diaChiNhaTro}</p>` : ''}
    </div>
    <div class="hd-party-section">
        <h4><i class="fas fa-user-circle" style="margin-right:4px;"></i>Bên B – Người thuê</h4>
        <p><strong>${hd.tenNguoiThue || '---'}</strong></p>
        ${hd.cccdNguoiThue ? `<p><span>CCCD:</span> ${hd.cccdNguoiThue}</p>` : ''}
        ${hd.sdtNguoiThue ? `<p><span>Điện thoại:</span> ${hd.sdtNguoiThue}</p>` : ''}
        ${hd.emailNguoiThue ? `<p><span>Email:</span> ${hd.emailNguoiThue}</p>` : ''}
        ${hd.diaChiNguoiThue ? `<p><span>Địa chỉ thường trú:</span> ${hd.diaChiNguoiThue}</p>` : ''}
    </div>
</div>

<table class="hd-table">
    <thead>
        <tr>
            <th>Điều khoản</th>
            <th>Chi tiết</th>
        </tr>
    </thead>
    <tbody>
        <tr><td>Phòng thuê</td><td><strong>${hd.tenPhong || '---'}</strong>${hd.diaChiPhong ? ' – ' + hd.diaChiPhong : ''}</td></tr>
        <tr><td>Nhà trọ</td><td>${hd.tenNhaTro || '---'}</td></tr>
        <tr><td>Ngày bắt đầu</td><td>${fmtDate(hd.ngayBatDau)}</td></tr>
        <tr><td>Ngày kết thúc</td><td>${hd.ngayKetThuc ? fmtDate(hd.ngayKetThuc) : 'Không xác định'}</td></tr>
        <tr><td>Thời hạn thuê</td><td>${soThang}</td></tr>
        <tr><td>Giá thuê phòng</td><td><strong>${fmt(hd.giaPhong)}/tháng</strong></td></tr>
        <tr><td>Tiền đặt cọc</td><td><strong style="color:#1d4ed8;">${fmt(hd.tienCoc)}</strong></td></tr>
    </tbody>
</table>

${hd.noiDung ? `
<div class="hd-content-box">
    <h4><i class="fas fa-file-alt" style="margin-right:4px;"></i>Nội dung & Điều khoản hợp đồng</h4>
    <p style="white-space:pre-line;">${hd.noiDung}</p>
</div>` : ''}

<div class="hd-signatures">
    <div class="hd-sig-box">
        <h5>Bên A – Chủ trọ</h5>
        <p>(Ký và ghi rõ họ tên)</p>
        <div class="hd-sig-space"></div>
        <div class="hd-sig-line">${hd.tenChuTro && hd.tenChuTro !== '---' ? hd.tenChuTro : '................................'}</div>
    </div>
    <div class="hd-sig-box">
        <h5>Bên B – Người thuê</h5>
        <p>(Ký và ghi rõ họ tên)</p>
        <div class="hd-sig-space"></div>
        <div class="hd-sig-line">${hd.tenNguoiThue || '................................'}</div>
    </div>
</div>

<div class="hd-footer">
    Hợp đồng được lập ngày ${ngayLapText} – Hệ thống quản lý nhà trọ &bull; Ngày in: ${new Date().toLocaleDateString('vi-VN')}
</div>`;
    }

    // ── Inject CSS hợp đồng vào head (chỉ inject 1 lần) ──────────────────────
    function injectStyles() {
        if (document.getElementById('hopDongPrintStyle')) return;
        const style = document.createElement('style');
        style.id = 'hopDongPrintStyle';
        style.textContent = `
/* ── Modal overlay ── */
#hopDongPrintModal {
    display: none;
    position: fixed;
    inset: 0;
    z-index: 9999;
    align-items: center;
    justify-content: center;
    padding: 1.25rem;
    background: rgba(15, 23, 42, 0.55);
}
#hopDongPrintModal.open { display: flex; }

.hd-print-box {
    width: min(900px, 96vw);
    max-height: 94vh;
    overflow: auto;
    background: #f3f4f6;
    border-radius: 10px;
    box-shadow: 0 24px 70px rgba(15, 23, 42, 0.35);
}

.hd-modal-toolbar {
    position: sticky;
    top: 0;
    z-index: 1;
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.75rem;
    padding: 0.85rem 1rem;
    background: #ffffff;
    border-bottom: 1px solid #e5e7eb;
}
.hd-modal-toolbar h3 { margin: 0; font-size: 0.98rem; font-weight: 700; }
.hd-toolbar-btns { display: flex; gap: 0.5rem; }
.btn-hd-print {
    display: inline-flex; align-items: center; gap: 6px;
    padding: 0.45rem 1.1rem; border-radius: 6px; border: none; cursor: pointer;
    font-size: 0.88rem; font-weight: 600;
    background: var(--primary, #0ea5e9); color: #fff;
}
.btn-hd-close {
    display: inline-flex; align-items: center; gap: 6px;
    padding: 0.45rem 1rem; border-radius: 6px; border: 1px solid #d1d5db; cursor: pointer;
    font-size: 0.88rem; background: #fff; color: #374151;
}

/* ── Nội dung hợp đồng ── */
#hopDongPrintContent {
    background: #fff;
    margin: 1rem;
    border-radius: 8px;
    padding: 2rem 2.5rem;
    font-family: 'Times New Roman', serif;
    font-size: 14px;
    color: #111;
    line-height: 1.65;
    box-shadow: 0 1px 4px rgba(0,0,0,.08);
}

.hd-header {
    display: flex; justify-content: space-between; align-items: flex-start;
    border-bottom: 2px solid #1d4ed8; padding-bottom: 1rem; margin-bottom: 1.25rem;
}
.hd-landlord-info h2 { margin: 0 0 .25rem; font-size: 1.1rem; color: #1d4ed8; }
.hd-landlord-info p { margin: 0 0 .15rem; font-size: .88rem; color: #374151; }
.hd-doc-info { text-align: right; }
.hd-code { font-size: 1.1rem; font-weight: 800; color: #1d4ed8; }
.hd-date { font-size: .85rem; color: #6b7280; margin-top: .15rem; }

.hd-title { text-align: center; margin-bottom: 1.5rem; }
.hd-title h1 { font-size: 1.4rem; letter-spacing: .5px; margin: 0 0 .25rem; color: #111; text-transform: uppercase; }
.hd-subtitle { margin: 0; font-size: .95rem; color: #374151; }

.hd-parties { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1.5rem; }
.hd-party-section {
    background: #f8fafc; border: 1px solid #e2e8f0; border-radius: .5rem; padding: .85rem 1rem;
}
.hd-party-section h4 { margin: 0 0 .5rem; font-size: .92rem; color: #1d4ed8; }
.hd-party-section p { margin: 0 0 .2rem; font-size: .87rem; }
.hd-party-section span { color: #6b7280; }

.hd-table { width: 100%; border-collapse: collapse; margin-bottom: 1.5rem; font-size: .92rem; }
.hd-table th {
    background: #1d4ed8; color: #fff; text-align: left;
    padding: .55rem .85rem; font-weight: 700; font-size: .88rem;
}
.hd-table td { padding: .5rem .85rem; border-bottom: 1px solid #e5e7eb; }
.hd-table tr:last-child td { border-bottom: none; }
.hd-table tr:hover td { background: #f8fafc; }

.hd-content-box {
    background: #f8fafc; border: 1px solid #e2e8f0; border-radius: .5rem;
    padding: 1rem; margin-bottom: 1.5rem;
}
.hd-content-box h4 { margin: 0 0 .5rem; font-size: .92rem; color: #1d4ed8; }
.hd-content-box p { margin: 0; font-size: .9rem; }

.hd-signatures {
    display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; margin-bottom: 1.5rem;
}
.hd-sig-box { text-align: center; }
.hd-sig-box h5 { margin: 0 0 .2rem; font-size: .92rem; font-weight: 700; }
.hd-sig-box p { margin: 0; font-size: .82rem; color: #6b7280; }
.hd-sig-space { height: 60px; }
.hd-sig-line { border-top: 1px solid #374151; padding-top: .35rem; font-size: .9rem; font-style: italic; }

.hd-footer {
    text-align: center; font-size: .8rem; color: #9ca3af;
    border-top: 1px solid #e5e7eb; padding-top: .75rem; margin-top: 1rem;
}

}`;
        document.head.appendChild(style);
    }

    function ensureModal() {
        if (document.getElementById('hopDongPrintModal')) return;
        injectStyles();

        const modal = document.createElement('div');
        modal.id = 'hopDongPrintModal';
        modal.innerHTML = `
<div class="hd-print-box">
    <div class="hd-modal-toolbar">
        <h3><i class="fas fa-file-contract" style="margin-right:6px;"></i>Xem trước hợp đồng</h3>
        <div class="hd-toolbar-btns">
            <button class="btn-hd-print" onclick="HopDongPrint.doPrint()">
                <i class="fas fa-print"></i> In / Xuất PDF
            </button>
            <button class="btn-hd-close" onclick="HopDongPrint.closeModal()">
                <i class="fas fa-times"></i> Đóng
            </button>
        </div>
    </div>
    <div id="hopDongPrintContent"></div>
</div>`;
        document.body.appendChild(modal);
        modal.addEventListener('click', (e) => { if (e.target === modal) HopDongPrint.closeModal(); });
    }

    async function openModal(maHopDong) {
        ensureModal();
        const modal   = document.getElementById('hopDongPrintModal');
        const content = document.getElementById('hopDongPrintContent');

        content.innerHTML = `<div style="padding:3rem;text-align:center;color:#6b7280;">
            <i class="fas fa-spinner fa-spin fa-2x"></i><br><br>Đang tải hợp đồng...
        </div>`;
        modal.classList.add('open');

        try {
            const hd = await apiFetch(`/api/HopDong/ExportPdf/${maHopDong}`);
            if (!hd) {
                content.innerHTML = `<div style="padding:2rem;text-align:center;color:#ef4444;">Không tìm thấy hợp đồng #${maHopDong}</div>`;
                return;
            }
            content.innerHTML = buildHtml(hd);
        } catch (err) {
            content.innerHTML = `<div style="padding:2rem;text-align:center;color:#ef4444;">Lỗi: ${err.message}</div>`;
        }
    }

    // Hiển thị hợp đồng từ object đã có sẵn (sau khi tạo mới, không cần gọi API lại)
    function openModalFromData(hdData) {
        ensureModal();
        const modal   = document.getElementById('hopDongPrintModal');
        const content = document.getElementById('hopDongPrintContent');
        content.innerHTML = buildHtml(hdData);
        modal.classList.add('open');
    }

    function closeModal() {
        const modal = document.getElementById('hopDongPrintModal');
        if (modal) modal.classList.remove('open');
    }

    function doPrint() {
        const content = document.getElementById('hopDongPrintContent');
        if (!content) return;

        const html = `<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="UTF-8">
<title>Hợp Đồng Thuê Phòng</title>
<style>
  @page { size: A4; margin: 14mm; }
  * { box-sizing: border-box; }
  body { font-family: 'Times New Roman', serif; font-size: 14px; color: #111; line-height: 1.65; margin: 0; padding: 1.5rem 2rem; }
  .hd-header { display: flex; justify-content: space-between; align-items: flex-start; border-bottom: 2px solid #1d4ed8; padding-bottom: 1rem; margin-bottom: 1.25rem; }
  .hd-landlord-info h2 { margin: 0 0 .25rem; font-size: 1.1rem; color: #1d4ed8; }
  .hd-landlord-info p { margin: 0 0 .15rem; font-size: .88rem; color: #374151; }
  .hd-doc-info { text-align: right; }
  .hd-code { font-size: 1.1rem; font-weight: 800; color: #1d4ed8; }
  .hd-date { font-size: .85rem; color: #6b7280; margin-top: .15rem; }
  .hd-title { text-align: center; margin-bottom: 1.5rem; }
  .hd-title h1 { font-size: 1.4rem; letter-spacing: .5px; margin: 0 0 .25rem; color: #111; text-transform: uppercase; }
  .hd-subtitle { margin: 0; font-size: .95rem; color: #374151; }
  .hd-parties { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; margin-bottom: 1.5rem; }
  .hd-party-section { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: .5rem; padding: .85rem 1rem; }
  .hd-party-section h4 { margin: 0 0 .5rem; font-size: .92rem; color: #1d4ed8; }
  .hd-party-section p { margin: 0 0 .2rem; font-size: .87rem; }
  .hd-party-section span { color: #6b7280; }
  .hd-table { width: 100%; border-collapse: collapse; margin-bottom: 1.5rem; font-size: .92rem; }
  .hd-table th { background: #1d4ed8; color: #fff; text-align: left; padding: .55rem .85rem; font-weight: 700; font-size: .88rem; }
  .hd-table td { padding: .5rem .85rem; border-bottom: 1px solid #e5e7eb; }
  .hd-table tr:last-child td { border-bottom: none; }
  .hd-content-box { background: #f8fafc; border: 1px solid #e2e8f0; border-radius: .5rem; padding: 1rem; margin-bottom: 1.5rem; }
  .hd-content-box h4 { margin: 0 0 .5rem; font-size: .92rem; color: #1d4ed8; }
  .hd-content-box p { margin: 0; font-size: .9rem; }
  .hd-signatures { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; margin-bottom: 1.5rem; }
  .hd-sig-box { text-align: center; }
  .hd-sig-box h5 { margin: 0 0 .2rem; font-size: .92rem; font-weight: 700; }
  .hd-sig-box p { margin: 0; font-size: .82rem; color: #6b7280; }
  .hd-sig-space { height: 60px; }
  .hd-sig-line { border-top: 1px solid #374151; padding-top: .35rem; font-size: .9rem; font-style: italic; }
  .hd-footer { text-align: center; font-size: .8rem; color: #9ca3af; border-top: 1px solid #e5e7eb; padding-top: .75rem; margin-top: 1rem; }
</style>
</head>
<body>${content.innerHTML}</body>
</html>`;

        const win = window.open('', '_blank', 'width=900,height=700');
        if (!win) { alert('Trình duyệt đã chặn cửa sổ pop-up. Vui lòng cho phép pop-up cho trang này.'); return; }
        win.document.write(html);
        win.document.close();
        win.focus();
        setTimeout(() => { win.print(); }, 400);
    }

    return { openModal, openModalFromData, closeModal, doPrint };
})();
