// Module cấu hình: hoadon
// Đặt tại: wwwroot/js/modules/hoa-don.js
// Chức năng bổ sung: In hóa đơn (window.print) + Xuất Excel (SheetJS / CSV fallback)

window.AppModules = window.AppModules || {};
window.AppModules.hoadon = {
    title: 'Hóa Đơn',
    endpoint: '/api/HoaDon',
    pk: 'maHoaDon',
    customModal: true,
    headers: [
        { label: 'Phòng', key: 'tenPhong' },
        { label: 'Khách thuê', key: 'tenNguoiThue' },
        { label: 'Loại hóa đơn', key: 'tenLoaiHoaDon', render: v => `<span class="badge ${v === 'Hóa đơn thuê phòng' ? 'badge-amber' : 'badge-blue'}">${v || 'Hóa đơn hằng tháng'}</span>` },
        { label: 'Kỳ hóa đơn', key: 'kyHoaDon' },
        { label: 'Tiền phòng', key: 'tienPhong', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền điện', key: 'tienDien', render: v => window.AppFormat.currency(v) },
        { label: 'Tiền nước', key: 'tienNuoc', render: v => window.AppFormat.currency(v) },
        { label: 'Phát sinh khác', key: 'tienPhatSinhKhac', render: v => window.AppFormat.currency(v) },
        { label: 'Tổng tiền', key: 'tongTien', render: v => `<strong style="color:var(--primary)">${window.AppFormat.currency(v)}</strong>` },
        { label: 'Đã thanh toán', key: 'daThanhToan', render: v => window.AppFormat.currency(v) },
        { label: 'Còn lại', key: 'conLai', render: v => `<strong style="color:${Number(v || 0) > 0 ? 'var(--error)' : 'var(--success)'}">${window.AppFormat.currency(v)}</strong>` },
        { label: 'Trạng thái', key: 'trangThaiThanhToan', render: v => `<span class="badge ${v === 'Đã thanh toán' ? 'badge-green' : v === 'Thanh toán một phần' ? 'badge-amber' : 'badge-red'}">${v || 'Chưa thanh toán'}</span>` },
        { label: 'Ngày lập', key: 'ngayLap', render: v => window.AppFormat.date(v) }
    ]
};

// ============================================================
// HOA DON PRINT — In hóa đơn bằng window.print()
// ============================================================
window.HoaDonPrint = (function () {

    // Format tiền tệ
    const fmt = v => (v != null && v !== '') ? new Intl.NumberFormat('vi-VN').format(v) + 'đ' : '0đ';
    const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN') : '---';
    const fmtKy = v => {
        if (!v) return '---';
        const [yr, mo] = v.split('-');
        return `Tháng ${parseInt(mo)}/${yr}`;
    };

    function statusClass(tt) {
        if (tt === 'Đã thanh toán') return 'paid';
        if (tt === 'Thanh toán một phần') return 'partial';
        if (tt === 'Đã hủy') return 'cancelled';
        return 'unpaid';
    }

    function buildHtml(hd) {
        const rows = [];

        if (hd.loaiHoaDon === 'ThuePhong') {
            rows.push(['Tiền thuê phòng', hd.tienPhong]);
        } else {
            if (Number(hd.tienPhong) > 0)  rows.push(['Tiền thuê phòng', hd.tienPhong]);
            if (Number(hd.tienDien) > 0)   rows.push(['Tiền điện', hd.tienDien]);
            if (Number(hd.tienNuoc) > 0)   rows.push(['Tiền nước', hd.tienNuoc]);
            if (Number(hd.tienDichVu) > 0) rows.push(['Tiền dịch vụ', hd.tienDichVu]);
        }
        if (Number(hd.tienPhatSinhKhac) > 0) rows.push(['Phát sinh khác', hd.tienPhatSinhKhac]);

        const tableRows = rows.length
            ? rows.map(([label, amt]) =>
            `<tr><td>${label}</td><td>${fmt(amt)}</td></tr>`
            ).join('')
            : `<tr><td>Không có khoản phí phát sinh</td><td>${fmt(0)}</td></tr>`;

        const dichVuText = (hd.dichVuSuDung && hd.dichVuSuDung.length)
            ? hd.dichVuSuDung.join(', ')
            : '---';

        const tenNhaTro = hd.tenNhaTro || 'Nhà trọ';
        const tenChuTro = hd.tenChuTro || '---';
        const diaChi   = hd.diaChi || '';
        const sdt      = hd.sdtChuTro || '';

        const trangThaiText = hd.trangThaiThanhToan || 'Chưa thanh toán';
        const sc = statusClass(trangThaiText);

        const conLai = Number(hd.conLai || 0);
        const statusExtra = conLai > 0
            ? `<span>Còn lại: <strong>${fmt(conLai)}</strong></span>`
            : `<span>✓ Thanh toán đủ</span>`;

        return `
<div class="inv-header">
    <div class="inv-landlord-info">
        <h2><i class="fas fa-home" style="margin-right:6px;font-size:1rem;"></i>${tenNhaTro}</h2>
        ${diaChi ? `<p><i class="fas fa-map-marker-alt" style="margin-right:4px;"></i>${diaChi}</p>` : ''}
        ${tenChuTro !== '---' ? `<p><i class="fas fa-user" style="margin-right:4px;"></i>Chủ trọ: ${tenChuTro}</p>` : ''}
        ${sdt ? `<p><i class="fas fa-phone" style="margin-right:4px;"></i>${sdt}</p>` : ''}
    </div>
    <div class="inv-doc-info">
        <div class="inv-code">HĐ #${hd.maHoaDon}</div>
        <div class="inv-date">Ngày lập: ${fmtDate(hd.ngayLap)}</div>
    </div>
</div>

<div class="inv-title">
    <h1>Hóa Đơn Tiền Phòng</h1>
    <p class="inv-period">${fmtKy(hd.kyHoaDon)}</p>
</div>

<div class="inv-parties">
    <div class="inv-party-section">
        <h4><i class="fas fa-door-open" style="margin-right:4px;"></i>Thông tin phòng</h4>
        <p><strong>${hd.tenPhong || '---'}</strong></p>
        <p><span>Dịch vụ:</span> ${dichVuText}</p>
    </div>
    <div class="inv-party-section">
        <h4><i class="fas fa-user-circle" style="margin-right:4px;"></i>Người thuê</h4>
        <p><strong>${hd.tenNguoiThue || '---'}</strong></p>
        <p><span>Loại hóa đơn:</span> ${hd.tenLoaiHoaDon || 'Hóa đơn hằng tháng'}</p>
    </div>
</div>

<table class="inv-table">
    <thead>
        <tr>
            <th>Khoản mục</th>
            <th style="text-align:right;">Số tiền</th>
        </tr>
    </thead>
    <tbody>
        ${tableRows}
    </tbody>
    <tfoot>
        <tr>
            <td>TỔNG CỘNG</td>
            <td>${fmt(hd.tongTien)}</td>
        </tr>
    </tfoot>
</table>

<div class="inv-status-row ${sc}">
    <span><i class="fas fa-${sc === 'paid' ? 'check-circle' : sc === 'partial' ? 'clock' : 'exclamation-circle'}" style="margin-right:6px;"></i>${trangThaiText}</span>
    ${statusExtra}
</div>

${(hd.ghiChu || hd.tienPhatSinhKhac > 0) ? `
<div class="inv-note">
    <strong><i class="fas fa-sticky-note" style="margin-right:4px;"></i>Ghi chú</strong>
    ${hd.ghiChu ? hd.ghiChu : (hd.tienPhatSinhKhac > 0 ? `Phát sinh thêm: ${fmt(hd.tienPhatSinhKhac)}` : '')}
</div>` : ''}

<div class="inv-signatures">
    <div class="inv-sig-box">
        <h5>Chủ trọ</h5>
        <p>(Ký và ghi rõ họ tên)</p>
        <div class="inv-sig-line">${tenChuTro !== '---' ? tenChuTro : '................................'}</div>
    </div>
    <div class="inv-sig-box">
        <h5>Người thuê</h5>
        <p>(Ký và ghi rõ họ tên)</p>
        <div class="inv-sig-line">${hd.tenNguoiThue || '................................'}</div>
    </div>
</div>

<div class="inv-footer">
    Hóa đơn được tạo tự động bởi hệ thống quản lý nhà trọ &bull; Ngày in: ${new Date().toLocaleDateString('vi-VN')}
</div>`;
    }

    function ensureModal() {
        if (document.getElementById('invoicePrintModal')) return;

        // Inject link print.css nếu chưa có
        if (!document.querySelector('link[href*="print.css"]')) {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = 'css/print.css';
            document.head.appendChild(link);
        }

        const modal = document.createElement('div');
        modal.id = 'invoicePrintModal';
        modal.innerHTML = `
<div class="invoice-print-box">
    <div class="invoice-modal-toolbar">
        <h3><i class="fas fa-file-invoice" style="margin-right:6px;"></i>Xem trước hóa đơn</h3>
        <div class="toolbar-btns">
            <button class="btn-print-now" onclick="HoaDonPrint.doPrint()">
                <i class="fas fa-print"></i> In ngay
            </button>
            <button class="btn-close-modal" onclick="HoaDonPrint.closeModal()">
                <i class="fas fa-times"></i> Đóng
            </button>
        </div>
    </div>
    <div id="invoicePrintContent"></div>
</div>`;
        document.body.appendChild(modal);

        // Đóng khi click backdrop
        modal.addEventListener('click', (e) => {
            if (e.target === modal) HoaDonPrint.closeModal();
        });
    }

    async function openModal(maHoaDon) {
        ensureModal();
        const modal = document.getElementById('invoicePrintModal');
        const content = document.getElementById('invoicePrintContent');

        content.innerHTML = `<div style="padding:3rem;text-align:center;color:#6b7280;">
            <i class="fas fa-spinner fa-spin fa-2x"></i><br><br>Đang tải hóa đơn...
        </div>`;
        modal.classList.add('open');

        try {
            const hd = await apiFetch(`/api/HoaDon/ExportPdf/${maHoaDon}`);
            window._hoaDonCache = window._hoaDonCache || {};
            if (hd) window._hoaDonCache[maHoaDon] = hd;

            if (!hd) {
                content.innerHTML = `<div style="padding:2rem;text-align:center;color:#ef4444;">Không tìm thấy hóa đơn #${maHoaDon}</div>`;
                return;
            }

            content.innerHTML = buildHtml(hd);

        } catch (err) {
            content.innerHTML = `<div style="padding:2rem;text-align:center;color:#ef4444;">Lỗi: ${err.message}</div>`;
        }
    }

    function closeModal() {
        const modal = document.getElementById('invoicePrintModal');
        if (modal) modal.classList.remove('open');
    }

    function doPrint() {
        const content = document.getElementById('invoicePrintContent');
        if (!content) return;

        const html = `<!DOCTYPE html>
<html lang="vi">
<head>
<meta charset="UTF-8">
<title>Hóa Đơn Tiền Phòng</title>
<style>
  @page { size: A4; margin: 14mm; }
  * { box-sizing: border-box; }
  body { font-family: "Plus Jakarta Sans", Arial, sans-serif; font-size: 13px; color: #111827; line-height: 1.45; margin: 0; padding: 18mm; }
  .inv-header { display: grid; grid-template-columns: 1fr auto; gap: 1.25rem; align-items: start; padding-bottom: 1rem; border-bottom: 2px solid #111827; }
  .inv-landlord-info h2 { margin: 0 0 .35rem; font-size: 1.15rem; font-weight: 800; color: #111827; }
  .inv-landlord-info p, .inv-doc-info .inv-date { margin: .12rem 0; color: #4b5563; font-size: .82rem; }
  .inv-doc-info { min-width: 145px; text-align: right; }
  .inv-doc-info .inv-code { display: inline-block; padding: .35rem .55rem; border: 1px solid #111827; border-radius: 4px; font-weight: 800; color: #111827; }
  .inv-title { margin: 1.35rem 0 1.25rem; text-align: center; }
  .inv-title h1 { margin: 0; font-size: 1.45rem; font-weight: 800; text-transform: uppercase; color: #111827; }
  .inv-title .inv-period { margin: .35rem 0 0; color: #4b5563; font-size: .92rem; font-weight: 600; }
  .inv-parties { display: grid; grid-template-columns: 1fr 1fr; margin-bottom: 1.1rem; border: 1px solid #d1d5db; border-radius: 6px; overflow: hidden; }
  .inv-party-section { padding: .8rem .9rem; background: #fff; }
  .inv-party-section + .inv-party-section { border-left: 1px solid #d1d5db; }
  .inv-party-section h4 { margin: 0 0 .42rem; color: #374151; font-size: .72rem; font-weight: 800; text-transform: uppercase; }
  .inv-party-section p { margin: .18rem 0; color: #111827; font-size: .9rem; }
  .inv-party-section p span { color: #6b7280; font-size: .82rem; }
  .inv-table { width: 100%; margin: 0 0 .95rem; border-collapse: collapse; font-size: .9rem; }
  .inv-table th, .inv-table td { padding: .66rem .75rem; border: 1px solid #d1d5db; }
  .inv-table thead th { background: #f3f4f6; color: #111827; text-align: left; font-weight: 800; }
  .inv-table thead th:last-child, .inv-table tbody td:last-child, .inv-table tfoot td:last-child { text-align: right; }
  .inv-table tbody td { color: #1f2937; }
  .inv-table tfoot td { background: #f9fafb; color: #111827; font-weight: 800; font-size: 1rem; }
  .inv-status-row { display: flex; justify-content: space-between; align-items: center; gap: .75rem; margin: .9rem 0 1rem; padding: .7rem .85rem; border: 1px solid #d1d5db; border-radius: 6px; font-size: .9rem; font-weight: 700; color: #111827; background: #fff; }
  .inv-status-row.paid { border-color: #86efac; color: #166534; background: #f0fdf4; }
  .inv-status-row.partial { border-color: #fcd34d; color: #92400e; background: #fffbeb; }
  .inv-status-row.unpaid { border-color: #fca5a5; color: #991b1b; background: #fef2f2; }
  .inv-note { margin-bottom: 1rem; padding: .75rem .85rem; border: 1px solid #fcd34d; border-radius: 6px; background: #fffbeb; color: #78350f; font-size: .88rem; }
  .inv-note strong { display: block; margin-bottom: .25rem; }
  .inv-signatures { display: grid; grid-template-columns: 1fr 1fr; gap: 2rem; margin-top: 1.8rem; padding-top: 1.25rem; }
  .inv-sig-box { text-align: center; }
  .inv-sig-box h5 { margin: 0; color: #111827; font-size: .86rem; font-weight: 800; text-transform: uppercase; }
  .inv-sig-box p { margin: .25rem 0 3.25rem; color: #6b7280; font-size: .78rem; }
  .inv-sig-line { min-height: 1.25rem; color: #374151; font-size: .86rem; font-style: italic; }
  .inv-footer { margin-top: 1.1rem; padding-top: .75rem; border-top: 1px solid #e5e7eb; color: #6b7280; text-align: center; font-size: .78rem; }
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

    return { openModal, closeModal, doPrint };
})();


// ============================================================
// HOA DON EXCEL — Xuất Excel / CSV danh sách hóa đơn
// ============================================================
window.HoaDonExcel = (function () {

    // Format thuần
    const fmt = v => (v != null && v !== '') ? Number(v) : 0;
    const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN') : '';

    function buildToolbar(containerOrId) {
        const container = typeof containerOrId === 'string'
            ? document.getElementById(containerOrId)
            : containerOrId;
        if (!container) return;
        if (document.getElementById('hoaDonFilterBar')) return;

        container.innerHTML = `
<div id="hoaDonFilterBar" style="
    display:flex; flex-wrap:wrap; align-items:center; gap:0.6rem;
    padding:0.85rem 1rem; background:#ffffff; border-radius:8px;
    border:1px solid #e5e7eb; margin-bottom:1rem; box-shadow:0 1px 2px rgba(15,23,42,.04);">

    <span style="font-weight:700;color:#374151;font-size:0.9rem;">
        <i class="fas fa-filter" style="margin-right:4px;"></i>Lọc & Xuất
    </span>

    <select id="filterKyHoaDon" style="border:1px solid #d1d5db;border-radius:6px;padding:0.35rem 0.6rem;font-size:0.85rem;min-width:130px;">
        <option value="">-- Tất cả kỳ --</option>
    </select>

    <select id="filterTrangThai" style="border:1px solid #d1d5db;border-radius:6px;padding:0.35rem 0.6rem;font-size:0.85rem;">
        <option value="">-- Tất cả trạng thái --</option>
        <option value="Chưa thanh toán">Chưa thanh toán</option>
        <option value="Thanh toán một phần">Thanh toán một phần</option>
        <option value="Đã thanh toán">Đã thanh toán</option>
        <option value="Đã hủy">Đã hủy</option>
    </select>

    <select id="filterPhong" style="border:1px solid #d1d5db;border-radius:6px;padding:0.35rem 0.6rem;font-size:0.85rem;min-width:130px;">
        <option value="">-- Tất cả phòng --</option>
    </select>

    <button onclick="HoaDonExcel.exportExcel()" style="
        background:#15803d;color:white;border:none;border-radius:7px;
        padding:0.4rem 0.9rem;font-size:0.85rem;font-weight:600;cursor:pointer;
        display:flex;align-items:center;gap:0.35rem;">
        <i class="fas fa-file-excel"></i> Xuất Excel
    </button>

    <span id="hoaDonCount" style="font-size:0.8rem;color:#6b7280;margin-left:auto;"></span>
</div>`;

        // Populate bộ lọc sau khi dữ liệu load
        HoaDonExcel.refreshFilters();
    }

    function attachToolbar() {
        const container = document.getElementById('hoaDonFilterBarWrapper')
            || document.querySelector('[data-module="hoa-don"] [data-slot="toolbar"]');
        if (container) buildToolbar(container);
    }

    async function getAllData() {
        if (!window._hoaDonAllData) {
            const res = await apiFetch('/api/HoaDon');
            window._hoaDonAllData = Array.isArray(res) ? res : [];
            // Cũng lưu cache cho print
            window._hoaDonCache = window._hoaDonCache || {};
            window._hoaDonAllData.forEach(x => { window._hoaDonCache[x.maHoaDon] = x; });
        }
        return window._hoaDonAllData;
    }

    async function refreshFilters() {
        const data = await getAllData();

        // Kỳ hóa đơn
        const kySet = [...new Set(data.map(x => x.kyHoaDon).filter(Boolean))].sort().reverse();
        const kySelect = document.getElementById('filterKyHoaDon');
        if (kySelect && kySelect.options.length <= 1) {
            kySet.forEach(ky => {
                const [yr, mo] = ky.split('-');
                const opt = document.createElement('option');
                opt.value = ky;
                opt.textContent = `Tháng ${parseInt(mo)}/${yr}`;
                kySelect.appendChild(opt);
            });
        }

        // Phòng
        const phongSet = [...new Map(data.map(x => [x.maPhong, x.tenPhong])).entries()];
        const phongSelect = document.getElementById('filterPhong');
        if (phongSelect && phongSelect.options.length <= 1) {
            phongSet.sort((a, b) => (a[1] || '').localeCompare(b[1] || '')).forEach(([ma, ten]) => {
                const opt = document.createElement('option');
                opt.value = ma;
                opt.textContent = ten || `Phòng ${ma}`;
                phongSelect.appendChild(opt);
            });
        }

        updateCount(data.length);
    }

    function getFiltered(data) {
        if (Array.isArray(window._HoaDonSearch?.filtered)) {
            return window._HoaDonSearch.filtered;
        }

        const ky = document.getElementById('filterKyHoaDon')?.value || '';
        const tt = document.getElementById('filterTrangThai')?.value || '';
        const ph = document.getElementById('filterPhong')?.value || '';

        return data.filter(x => {
            if (ky && x.kyHoaDon !== ky) return false;
            if (tt && x.trangThaiThanhToan !== tt) return false;
            if (ph && String(x.maPhong) !== String(ph)) return false;
            return true;
        });
    }

    function updateCount(n) {
        const el = document.getElementById('hoaDonCount');
        if (el) el.textContent = `${n} hóa đơn`;
    }

    function toCsv(rows) {
        const headers = [
            'Mã HĐ','Phòng','Người thuê','Kỳ hóa đơn','Loại hóa đơn',
            'Tiền phòng','Tiền điện','Tiền nước','Tiền dịch vụ','Phát sinh khác',
            'Tổng tiền','Đã thanh toán','Còn lại','Trạng thái','Ngày lập'
        ];

        const escape = v => {
            const s = String(v ?? '');
            return s.includes(',') || s.includes('"') || s.includes('\n')
                ? `"${s.replace(/"/g, '""')}"` : s;
        };

        const lines = [
            '\uFEFF' + headers.map(escape).join(','), // BOM for Excel UTF-8
            ...rows.map(r => [
                r.maHoaDon, r.tenPhong, r.tenNguoiThue, r.kyHoaDon, r.tenLoaiHoaDon,
                fmt(r.tienPhong), fmt(r.tienDien), fmt(r.tienNuoc), fmt(r.tienDichVu), fmt(r.tienPhatSinhKhac),
                fmt(r.tongTien), fmt(r.daThanhToan), fmt(r.conLai), r.trangThaiThanhToan, fmtDate(r.ngayLap)
            ].map(escape).join(','))
        ];

        return lines.join('\r\n');
    }

    function downloadCsv(content, filename) {
        const blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.click();
        URL.revokeObjectURL(url);
    }

    async function exportExcel() {
        const data = Array.isArray(window._HoaDonSearch?.rawData) && window._HoaDonSearch.rawData.length
            ? window._HoaDonSearch.rawData
            : await getAllData();
        const filtered = getFiltered(data);

        if (filtered.length === 0) {
            alert('Không có dữ liệu để xuất với bộ lọc hiện tại.');
            return;
        }

        // Thử dùng SheetJS nếu có (XLSX global)
        if (window.XLSX) {
            const wsData = [
                ['Mã HĐ','Phòng','Người thuê','Kỳ hóa đơn','Loại hóa đơn',
                 'Tiền phòng','Tiền điện','Tiền nước','Tiền dịch vụ','Phát sinh khác',
                 'Tổng tiền','Đã thanh toán','Còn lại','Trạng thái','Ngày lập'],
                ...filtered.map(r => [
                    r.maHoaDon, r.tenPhong, r.tenNguoiThue, r.kyHoaDon, r.tenLoaiHoaDon,
                    fmt(r.tienPhong), fmt(r.tienDien), fmt(r.tienNuoc), fmt(r.tienDichVu), fmt(r.tienPhatSinhKhac),
                    fmt(r.tongTien), fmt(r.daThanhToan), fmt(r.conLai), r.trangThaiThanhToan, fmtDate(r.ngayLap)
                ])
            ];
            const ws = XLSX.utils.aoa_to_sheet(wsData);
            // Style cột số tiền
            const numCols = [5,6,7,8,9,10,11,12];
            const range = XLSX.utils.decode_range(ws['!ref']);
            for (let r = 1; r <= range.e.r; r++) {
                numCols.forEach(c => {
                    const cell = ws[XLSX.utils.encode_cell({r, c})];
                    if (cell) cell.z = '#,##0';
                });
            }
            ws['!cols'] = [
                {wch:8},{wch:14},{wch:22},{wch:12},{wch:20},
                {wch:14},{wch:12},{wch:12},{wch:14},{wch:14},
                {wch:14},{wch:14},{wch:12},{wch:20},{wch:12}
            ];
            const wb = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(wb, ws, 'Hóa đơn');
            const ky = window._HoaDonSearch?.filterKyHoaDon || document.getElementById('filterKyHoaDon')?.value || 'tat-ca';
            XLSX.writeFile(wb, `hoa-don-${ky}.xlsx`);
        } else {
            // Fallback: CSV
            const ky = window._HoaDonSearch?.filterKyHoaDon || document.getElementById('filterKyHoaDon')?.value || 'tat-ca';
            downloadCsv(toCsv(filtered), `hoa-don-${ky}.csv`);
        }

        updateCount(filtered.length);
    }

    return { attachToolbar, buildToolbar, refreshFilters, exportExcel, getAllData, getFiltered, updateCount };
})();

// ── Gắn toolbar vào slot toolbar khi module hoa-don được render ──
// Generic module gọi sự kiện 'module:rendered' khi load xong
document.addEventListener('module:afterLoad', function (e) {
    if (e && e.detail && e.detail.module === 'hoadon') {
        // Tìm vùng toolbar của module
        HoaDonExcel.attachToolbar();

        // Cache dữ liệu sau khi bảng render để print modal dùng ngay
        HoaDonExcel.getAllData().then(data => {
            window._hoaDonCache = window._hoaDonCache || {};
            data.forEach(x => { window._hoaDonCache[x.maHoaDon] = x; });
        });
    }
});

// ── Fallback: gắn toolbar nếu không có event, dùng MutationObserver ──
(function () {
    let attached = false;

    function tryAttach() {
        if (attached) return;
        const toolbar = document.getElementById('hoaDonFilterBarWrapper')
            || document.querySelector('[data-module="hoa-don"] [data-slot="toolbar"]');
        if (toolbar && !document.getElementById('hoaDonFilterBar')) {
            attached = true;
            HoaDonExcel.buildToolbar(toolbar);
        }
    }

    const observer = new MutationObserver(tryAttach);
    observer.observe(document.body, { childList: true, subtree: true });
    // Cũng thử ngay khi DOM ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', tryAttach);
    } else {
        setTimeout(tryAttach, 500);
    }
})();
