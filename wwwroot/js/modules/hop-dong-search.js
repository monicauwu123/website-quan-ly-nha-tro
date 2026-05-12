// ==========================================
// MODULE: Hợp Đồng – Search / Filter / Sort / Paging
// File: js/modules/hop-dong-search.js
// ==========================================
(function () {
    'use strict';

    // ── State ──────────────────────────────────────────────────────────────────
    const HD = {
        keyword: '',
        filterTrangThai: '',
        filterNhaTro: '',
        filterPhong: '',
        filterNgayBatDauTu: '',
        filterNgayBatDauDen: '',
        filterNgayKetThucTu: '',
        filterNgayKetThucDen: '',
        filterSapHetHan: '',     // '7' | '15' | '30' | ''
        sortKey: '',
        sortDir: 'asc',
        page: 1,
        pageSize: 10,
        rawData: [],
        filtered: [],
    };

    window._HopDongSearch = HD;

    // ── Init ──────────────────────────────────────────────────────────────────
    function init(data) {
        HD.rawData = data || [];
        HD.page = 1;
        _buildToolbar();
        _applyAndRender();
    }

    // ── Build toolbar ─────────────────────────────────────────────────────────
    function _buildToolbar() {
        const slot = document.getElementById('hdToolbarSlot');
        if (!slot) return;

        const nhaTroList = window.normalizeArrayResponse(window.lookups?.nhatro || []);
        const phongList  = window.normalizeArrayResponse(window.lookups?.phong  || []);

        const nhaTroOpts = nhaTroList.map(n =>
            `<option value="${n.maNhaTro}" ${String(n.maNhaTro)===HD.filterNhaTro?'selected':''}>${_esc(n.tenNhaTro||'Nhà trọ #'+n.maNhaTro)}</option>`
        ).join('');

        const visiblePhong = HD.filterNhaTro
            ? phongList.filter(p => String(p.maNhaTro) === String(HD.filterNhaTro))
            : phongList;
        const phongOpts = visiblePhong.map(p =>
            `<option value="${p.maPhong}" ${String(p.maPhong)===HD.filterPhong?'selected':''}>${_esc(p.tenPhong||'Phòng #'+p.maPhong)}</option>`
        ).join('');

        slot.innerHTML = `
        <div class="hd-toolbar" style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:1rem;">

            <!-- Tìm kiếm nhanh -->
            <div style="position:relative;flex:1;min-width:220px;max-width:400px;">
                <i class="fas fa-search" style="position:absolute;left:.85rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
                <input type="text" id="hdKeyword" class="form-control" style="padding-left:2.5rem;"
                    placeholder="Mã HD, tên KH, phòng, nhà trọ, trạng thái..."
                    value="${_esc(HD.keyword)}"
                    oninput="window._HopDongSearch.onKeyword(this.value)">
            </div>

            <!-- Filter Trạng thái -->
            <div style="min-width:170px;">
                <select class="form-control" onchange="window._HopDongSearch.onTrangThai(this.value)">
                    <option value="">Tất cả trạng thái</option>
                    <option value="DangHieuLuc"  ${HD.filterTrangThai==='DangHieuLuc'  ?'selected':''}>Đang hiệu lực</option>
                    <option value="SapHetHan"    ${HD.filterTrangThai==='SapHetHan'    ?'selected':''}>Sắp hết hạn</option>
                    <option value="KetThuc"      ${HD.filterTrangThai==='KetThuc'      ?'selected':''}>Đã kết thúc</option>
                    <option value="Huy"          ${HD.filterTrangThai==='Huy'          ?'selected':''}>Đã hủy</option>
                </select>
            </div>

            <!-- Sắp hết hạn trong -->
            <div style="min-width:160px;">
                <select class="form-control" onchange="window._HopDongSearch.onSapHetHan(this.value)">
                    <option value="">Sắp hết hạn trong...</option>
                    <option value="7"  ${HD.filterSapHetHan==='7'  ?'selected':''}>7 ngày tới</option>
                    <option value="15" ${HD.filterSapHetHan==='15' ?'selected':''}>15 ngày tới</option>
                    <option value="30" ${HD.filterSapHetHan==='30' ?'selected':''}>30 ngày tới</option>
                </select>
            </div>

            ${nhaTroList.length > 1 ? `
            <!-- Filter Nhà trọ -->
            <div style="min-width:160px;">
                <select id="hdFilterNhaTro" class="form-control" onchange="window._HopDongSearch.onNhaTro(this.value)">
                    <option value="">Tất cả nhà trọ</option>
                    ${nhaTroOpts}
                </select>
            </div>` : ''}

            <!-- Filter Phòng -->
            <div style="min-width:150px;">
                <select id="hdFilterPhong" class="form-control" onchange="window._HopDongSearch.onPhong(this.value)">
                    <option value="">Tất cả phòng</option>
                    ${phongOpts}
                </select>
            </div>

        </div>

        <!-- Hàng 2: khoảng ngày -->
        <div style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:1rem;">
            <div style="display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;">
                <span style="font-size:.85rem;color:var(--text-light);white-space:nowrap;">Ngày bắt đầu:</span>
                <input type="date" class="form-control" style="width:155px;" value="${HD.filterNgayBatDauTu}"
                    onchange="window._HopDongSearch.onNgayBatDauTu(this.value)">
                <span style="color:var(--text-light);">–</span>
                <input type="date" class="form-control" style="width:155px;" value="${HD.filterNgayBatDauDen}"
                    onchange="window._HopDongSearch.onNgayBatDauDen(this.value)">
            </div>
            <div style="display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;">
                <span style="font-size:.85rem;color:var(--text-light);white-space:nowrap;">Ngày kết thúc:</span>
                <input type="date" class="form-control" style="width:155px;" value="${HD.filterNgayKetThucTu}"
                    onchange="window._HopDongSearch.onNgayKetThucTu(this.value)">
                <span style="color:var(--text-light);">–</span>
                <input type="date" class="form-control" style="width:155px;" value="${HD.filterNgayKetThucDen}"
                    onchange="window._HopDongSearch.onNgayKetThucDen(this.value)">
            </div>
            <button class="btn btn-secondary" style="white-space:nowrap;" onclick="window._HopDongSearch.reset()">
                <i class="fas fa-undo"></i> Đặt lại
            </button>
        </div>`;
    }

    // ── Handlers ────────────────────────────────────────────────────────────────
    function onKeyword(v)         { HD.keyword = v.trim(); HD.page = 1; _applyAndRender(); }
    function onTrangThai(v)       { HD.filterTrangThai = v; HD.filterSapHetHan = ''; HD.page = 1; _applyAndRender(); }
    function onSapHetHan(v)       { HD.filterSapHetHan = v; HD.filterTrangThai = ''; HD.page = 1; _applyAndRender(); }
    function onNhaTro(v)          { HD.filterNhaTro = v; HD.filterPhong = ''; HD.page = 1; _rebuildPhong(); _applyAndRender(); }
    function onPhong(v)           { HD.filterPhong = v; HD.page = 1; _applyAndRender(); }
    function onNgayBatDauTu(v)    { HD.filterNgayBatDauTu = v;  HD.page = 1; _applyAndRender(); }
    function onNgayBatDauDen(v)   { HD.filterNgayBatDauDen = v; HD.page = 1; _applyAndRender(); }
    function onNgayKetThucTu(v)   { HD.filterNgayKetThucTu = v;  HD.page = 1; _applyAndRender(); }
    function onNgayKetThucDen(v)  { HD.filterNgayKetThucDen = v; HD.page = 1; _applyAndRender(); }
    function onSort(key) {
        HD.sortDir = HD.sortKey === key && HD.sortDir === 'asc' ? 'desc' : 'asc';
        HD.sortKey = key;
        HD.page = 1;
        _applyAndRender();
    }
    function onPageSize(v) { HD.pageSize = parseInt(v) || 10; HD.page = 1; _applyAndRender(); }
    function onPage(p)     { HD.page = p; _applyAndRender(); }
    function reset() {
        Object.assign(HD, {
            keyword:'', filterTrangThai:'', filterNhaTro:'', filterPhong:'',
            filterNgayBatDauTu:'', filterNgayBatDauDen:'',
            filterNgayKetThucTu:'', filterNgayKetThucDen:'',
            filterSapHetHan:'', sortKey:'', sortDir:'asc', page:1, pageSize:10
        });
        _buildToolbar();
        _applyAndRender();
    }

    function _rebuildPhong() {
        const sel = document.getElementById('hdFilterPhong');
        if (!sel) return;
        const phongList = window.normalizeArrayResponse(window.lookups?.phong || []);
        const visible = HD.filterNhaTro ? phongList.filter(p => String(p.maNhaTro) === String(HD.filterNhaTro)) : phongList;
        sel.innerHTML = `<option value="">Tất cả phòng</option>` +
            visible.map(p => `<option value="${p.maPhong}">${_esc(p.tenPhong||'Phòng #'+p.maPhong)}</option>`).join('');
    }

    // ── Helper: tính số ngày còn lại đến ngày kết thúc ───────────────────────
    function daysUntil(dateStr) {
        if (!dateStr) return null;
        const d = new Date(dateStr);
        const today = new Date(); today.setHours(0,0,0,0);
        return Math.ceil((d - today) / 86400000);
    }

    // ── Filter + Sort ─────────────────────────────────────────────────────────
    function _filterAndSort(data) {
        let result = data.slice();
        const kw = HD.keyword.toLowerCase();

        if (kw) {
            result = result.filter(r => {
                const tenPhong   = (r.phong?.tenPhong || '').toLowerCase();
                const hoTen      = (r.nguoiThue?.hoTen || '').toLowerCase();
                const nhaTroName = _getNhaTroName(r.maPhong).toLowerCase();
                const stText     = (r.trangThaiText || '').toLowerCase();
                const maHD       = String(r.maHopDong || '');
                return maHD.includes(kw) || hoTen.includes(kw) || tenPhong.includes(kw) || nhaTroName.includes(kw) || stText.includes(kw);
            });
        }

        if (HD.filterTrangThai) {
            result = result.filter(r => {
                if (HD.filterTrangThai === 'SapHetHan') {
                    const days = daysUntil(r.ngayKetThuc);
                    return r.trangThai === 'DangHieuLuc' && days !== null && days >= 0 && days <= 30;
                }
                return r.trangThai === HD.filterTrangThai;
            });
        }

        if (HD.filterSapHetHan) {
            const limit = parseInt(HD.filterSapHetHan);
            result = result.filter(r => {
                const days = daysUntil(r.ngayKetThuc);
                return r.trangThai === 'DangHieuLuc' && days !== null && days >= 0 && days <= limit;
            });
        }

        if (HD.filterNhaTro) {
            result = result.filter(r => {
                const phong = (window.lookups?.phong || []).find(p => Number(p.maPhong) === Number(r.maPhong));
                return phong && String(phong.maNhaTro) === String(HD.filterNhaTro);
            });
        }

        if (HD.filterPhong) {
            result = result.filter(r => String(r.maPhong) === String(HD.filterPhong));
        }

        const _toTs = s => s ? new Date(s).getTime() : null;

        if (HD.filterNgayBatDauTu)  result = result.filter(r => _toTs(r.ngayBatDau)  >= _toTs(HD.filterNgayBatDauTu));
        if (HD.filterNgayBatDauDen) result = result.filter(r => _toTs(r.ngayBatDau)  <= _toTs(HD.filterNgayBatDauDen));
        if (HD.filterNgayKetThucTu) result = result.filter(r => r.ngayKetThuc && _toTs(r.ngayKetThuc) >= _toTs(HD.filterNgayKetThucTu));
        if (HD.filterNgayKetThucDen) result = result.filter(r => r.ngayKetThuc && _toTs(r.ngayKetThuc) <= _toTs(HD.filterNgayKetThucDen));

        if (HD.sortKey) {
            const dir = HD.sortDir === 'asc' ? 1 : -1;
            result.sort((a, b) => {
                const va = _sortVal(a, HD.sortKey);
                const vb = _sortVal(b, HD.sortKey);
                if (va < vb) return -1 * dir;
                if (va > vb) return 1 * dir;
                return 0;
            });
        }

        return result;
    }

    function _sortVal(row, key) {
        switch (key) {
            case 'ngayBatDau':  return row.ngayBatDau  ? new Date(row.ngayBatDau).getTime()  : 0;
            case 'ngayKetThuc': return row.ngayKetThuc ? new Date(row.ngayKetThuc).getTime() : Infinity;
            case 'tienCoc':     return Number(row.tienCoc) || 0;
            case 'trangThai':   return (row.trangThai || '').toLowerCase();
            case 'tenPhong':    return (row.phong?.tenPhong || '').toLowerCase();
            case 'nguoiThue':   return (row.nguoiThue?.hoTen || '').toLowerCase();
            default: return '';
        }
    }

    function _getNhaTroName(maPhong) {
        const phong = (window.lookups?.phong || []).find(p => Number(p.maPhong) === Number(maPhong));
        if (!phong) return '';
        const nhaTro = (window.lookups?.nhatro || []).find(n => Number(n.maNhaTro) === Number(phong.maNhaTro));
        return nhaTro?.tenNhaTro || '';
    }

    // ── Apply & Render ────────────────────────────────────────────────────────
    function _applyAndRender() {
        HD.filtered = _filterAndSort(HD.rawData);
        _renderTable();
        _renderPaging();
    }

    // ── Status helpers ────────────────────────────────────────────────────────
    const STATUS_MAP = {
        'DangHieuLuc': { cls: 'badge-success', label: 'Đang hiệu lực' },
        'KetThuc':     { cls: 'badge-secondary', label: 'Đã kết thúc' },
        'Huy':         { cls: 'badge-red',      label: 'Đã hủy' },
    };

    function _statusBadge(row) {
        const days = daysUntil(row.ngayKetThuc);
        const isSapHet = row.trangThai === 'DangHieuLuc' && days !== null && days >= 0 && days <= 30;

        let badge = '';
        if (row.trangThai === 'DangHieuLuc') {
            badge = `<span class="badge badge-success">Đang hiệu lực</span>`;
        } else {
            const { cls, label } = STATUS_MAP[row.trangThai] || { cls: 'badge-secondary', label: row.trangThai };
            badge = `<span class="badge ${cls}">${label}</span>`;
        }

        if (isSapHet) {
            const urgCls = days <= 7 ? 'hd-warn-red' : days <= 15 ? 'hd-warn-orange' : 'hd-warn-yellow';
            badge += ` <span class="hd-expiry-badge ${urgCls}"><i class="fas fa-exclamation-triangle"></i> ${days === 0 ? 'Hôm nay' : days + ' ngày'}</span>`;
        }
        return badge;
    }

    // ── Render bảng ───────────────────────────────────────────────────────────
    function _th(key, label) {
        const active = HD.sortKey === key;
        const icon = active ? (HD.sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down') : 'fa-sort';
        const style = active ? 'color:var(--primary);' : 'color:var(--text-light);';
        return `<th style="cursor:pointer;white-space:nowrap;user-select:none;"
                    onclick="window._HopDongSearch.onSort('${key}')">
                    ${label} <i class="fas ${icon}" style="${style}font-size:.75rem;"></i>
                </th>`;
    }

    function _renderTable() {
        const slot = document.getElementById('hdTableSlot');
        if (!slot) return;

        const total    = HD.filtered.length;
        const start    = (HD.page - 1) * HD.pageSize;
        const pageData = HD.filtered.slice(start, start + HD.pageSize);
        const canWrite = window.CURRENT_ROLE === 'Admin' || window.CURRENT_ROLE === 'ChuTro';

        const thead = `<thead><tr>
            <th style="white-space:nowrap;">Mã HD</th>
            ${_th('tenPhong',  'Phòng')}
            ${_th('nguoiThue', 'Khách thuê')}
            ${_th('ngayBatDau',  'Ngày bắt đầu')}
            ${_th('ngayKetThuc', 'Ngày kết thúc')}
            ${_th('tienCoc',     'Tiền cọc')}
            ${_th('trangThai',   'Trạng thái')}
            <th>Thao tác</th>
        </tr></thead>`;

        let tbody;
        if (!pageData.length) {
            tbody = `<tbody><tr><td colspan="8" style="text-align:center;padding:2.5rem;color:var(--text-light);">
                <i class="fas fa-file-contract" style="font-size:2rem;display:block;margin-bottom:.5rem;opacity:.35;"></i>
                Không tìm thấy hợp đồng phù hợp.
            </td></tr></tbody>`;
        } else {
            const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN') : '---';
            const fmtCur  = v => (v !== null && v !== undefined) ? new Intl.NumberFormat('vi-VN').format(v) + 'đ' : '---';

            const nhaTroCol = window.normalizeArrayResponse(window.lookups?.nhatro || []).length > 1;

            tbody = '<tbody>' + pageData.map(item => {
                const days = daysUntil(item.ngayKetThuc);
                const isSapHet = item.trangThai === 'DangHieuLuc' && days !== null && days >= 0 && days <= 30;
                const rowStyle = isSapHet && days <= 7 ? 'background:rgba(239,68,68,.04);' : '';

                const tenPhong = item.phong?.tenPhong || `Phòng #${item.maPhong}`;
                const nhaTroName = _getNhaTroName(item.maPhong);
                const phongCell = nhaTroName
                    ? `<div style="font-weight:500;">${_esc(tenPhong)}</div><div style="font-size:.8rem;color:var(--text-light);">${_esc(nhaTroName)}</div>`
                    : _esc(tenPhong);

                let actionHtml = `<button class="btn-action" style="background:#6366f1;" onclick="HopDongPrint.openModal(${item.maHopDong})"><i class="fas fa-print"></i> In</button>`;
                if (canWrite) {
                    actionHtml += `<button class="btn-action btn-edit" onclick="editItem('hopdong',${item.maHopDong})"><i class="fas fa-edit"></i> Sửa</button>`;
                    if (item.trangThai === 'DangHieuLuc') {
                        actionHtml += `
                            <button class="btn-action" style="background:#0f766e;" onclick="ketThucHopDong(${item.maHopDong})"><i class="fas fa-flag-checkered"></i> Kết thúc</button>
                            <button class="btn-action btn-delete" onclick="huyHopDong(${item.maHopDong})"><i class="fas fa-ban"></i> Hủy</button>`;
                    } else {
                        actionHtml += `<button class="btn-action btn-delete" onclick="deleteItem('hopdong',${item.maHopDong})"><i class="fas fa-trash"></i> Xóa</button>`;
                    }
                }

                return `<tr style="${rowStyle}">
                    <td style="font-weight:600;color:var(--primary);">#${item.maHopDong}</td>
                    <td>${phongCell}</td>
                    <td>${_esc(item.nguoiThue?.hoTen) || '---'}</td>
                    <td style="white-space:nowrap;">${fmtDate(item.ngayBatDau)}</td>
                    <td style="white-space:nowrap;">${fmtDate(item.ngayKetThuc)}</td>
                    <td style="white-space:nowrap;">${fmtCur(item.tienCoc)}</td>
                    <td>${_statusBadge(item)}</td>
                    <td style="white-space:nowrap;">${actionHtml}</td>
                </tr>`;
            }).join('') + '</tbody>';
        }

        // Đếm sắp hết hạn để hiện cảnh báo tóm tắt
        const sapHet30 = HD.filtered.filter(r => {
            const d = daysUntil(r.ngayKetThuc);
            return r.trangThai === 'DangHieuLuc' && d !== null && d >= 0 && d <= 30;
        }).length;

        const warningBanner = sapHet30 > 0 ? `
            <div style="background:#fef3c7;border:1px solid #fbbf24;border-radius:8px;padding:.6rem 1rem;margin-bottom:.75rem;font-size:.875rem;display:flex;gap:.5rem;align-items:center;">
                <i class="fas fa-exclamation-triangle" style="color:#d97706;"></i>
                <span>Có <strong>${sapHet30}</strong> hợp đồng sắp hết hạn trong 30 ngày tới.</span>
                <button class="btn btn-secondary" style="padding:.2rem .65rem;font-size:.8rem;margin-left:auto;"
                    onclick="window._HopDongSearch.onSapHetHan('30')">Xem</button>
            </div>` : '';

        slot.innerHTML = `
            ${warningBanner}
            <div class="hd-result-meta" style="font-size:.85rem;color:var(--text-light);margin-bottom:.5rem;">
                Tìm thấy <strong>${total}</strong> hợp đồng
                ${total > 0 && pageData.length < total ? `(hiển thị ${start+1}–${Math.min(start+HD.pageSize, total)})` : ''}
            </div>
            <div class="table-container">
                <table>${thead}${tbody}</table>
            </div>

            <style>
                .hd-expiry-badge{display:inline-flex;align-items:center;gap:.25rem;font-size:.75rem;font-weight:600;padding:.15rem .45rem;border-radius:20px;margin-left:.25rem;}
                .hd-warn-red   {background:#fee2e2;color:#b91c1c;}
                .hd-warn-orange{background:#ffedd5;color:#c2410c;}
                .hd-warn-yellow{background:#fef9c3;color:#a16207;}
            </style>`;
    }

    // ── Render phân trang ─────────────────────────────────────────────────────
    function _renderPaging() {
        const slot = document.getElementById('hdPagingSlot');
        if (!slot) return;

        const total   = HD.filtered.length;
        const totalPg = Math.max(1, Math.ceil(total / HD.pageSize));
        const cur     = Math.min(HD.page, totalPg);

        const sizeOpts = [10, 20, 50].map(s =>
            `<option value="${s}" ${HD.pageSize===s?'selected':''}>${s}</option>`
        ).join('');

        let btns = '';
        if (totalPg > 1) {
            const lo = Math.max(1, cur - 2);
            const hi = Math.min(totalPg, lo + 4);
            if (cur > 1)      btns += `<button class="hd-pg-btn" onclick="window._HopDongSearch.onPage(${cur-1})"><i class="fas fa-chevron-left"></i></button>`;
            if (lo > 1)       btns += `<button class="hd-pg-btn" onclick="window._HopDongSearch.onPage(1)">1</button><span class="hd-pg-ell">…</span>`;
            for (let i=lo;i<=hi;i++) btns += `<button class="hd-pg-btn${i===cur?' hd-pg-active':''}" onclick="window._HopDongSearch.onPage(${i})">${i}</button>`;
            if (hi < totalPg) btns += `<span class="hd-pg-ell">…</span><button class="hd-pg-btn" onclick="window._HopDongSearch.onPage(${totalPg})">${totalPg}</button>`;
            if (cur < totalPg) btns += `<button class="hd-pg-btn" onclick="window._HopDongSearch.onPage(${cur+1})"><i class="fas fa-chevron-right"></i></button>`;
        }

        slot.innerHTML = `
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;margin-top:.75rem;">
            <div style="display:flex;align-items:center;gap:.5rem;font-size:.875rem;color:var(--text-light);">
                Hiển thị
                <select class="form-control" style="width:auto;padding:.25rem .5rem;font-size:.875rem;"
                    onchange="window._HopDongSearch.onPageSize(this.value)">${sizeOpts}</select>
                dòng / trang
            </div>
            <div style="display:flex;gap:.25rem;align-items:center;">${btns}</div>
        </div>
        <style>
            .hd-pg-btn{padding:.3rem .65rem;border:1px solid var(--border-color,#e2e8f0);border-radius:6px;background:#fff;cursor:pointer;font-size:.85rem;color:var(--text-primary,#1e293b);transition:all .15s;}
            .hd-pg-btn:hover{background:var(--primary-light,#eff6ff);border-color:var(--primary,#3b82f6);}
            .hd-pg-active{background:var(--primary,#3b82f6)!important;color:#fff!important;border-color:var(--primary,#3b82f6)!important;}
            .hd-pg-ell{padding:0 .25rem;color:var(--text-light);}
        </style>`;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    function _esc(v) {
        if (v == null) return '';
        return String(v).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    // ── Public API ────────────────────────────────────────────────────────────
    Object.assign(HD, {
        init, onKeyword, onTrangThai, onSapHetHan, onNhaTro, onPhong,
        onNgayBatDauTu, onNgayBatDauDen, onNgayKetThucTu, onNgayKetThucDen,
        onSort, onPageSize, onPage, reset,
        refresh: (data) => { HD.rawData = data || []; HD.page = 1; _applyAndRender(); }
    });

})();
