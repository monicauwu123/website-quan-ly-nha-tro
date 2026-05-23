// ==========================================
// MODULE: Hóa Đơn – Search / Filter / Sort / Paging
// File: js/modules/hoa-don-search.js
// ==========================================
(function () {
    'use strict';

    // ── State ─────────────────────────────────────────────────────────────────
    const HDN = {
        keyword: '',
        filterNhaTro: '',
        filterPhong: '',
        filterKyHoaDon: '',
        filterTrangThai: '',
        filterNgayLapTu: '',
        filterNgayLapDen: '',
        filterTongTienTu: '',
        filterTongTienDen: '',
        sortKey: 'ngayLap',
        sortDir: 'desc',
        page: 1,
        pageSize: 10,
        rawData: [],
        filtered: [],
        advancedOpen: false,
    };

    window._HoaDonSearch = HDN;

    // ── Init ─────────────────────────────────────────────────────────────────
    function init(data) {
        HDN.rawData = data || [];
        HDN.page = 1;
        _mergeBienLaiChoXacNhan().then(() => {
            _buildToolbar();
            _applyAndRender();
        });
    }

    // ── Merge trạng thái biên lai ────────────────────────────────────────────
    async function _mergeBienLaiChoXacNhan() {
        if (window.CURRENT_ROLE !== 'NguoiDung') return;
        HDN.rawData = HDN.rawData.map(r => ({
            ...r,
            _daCoBienLaiChoXacNhan: r.daCoBienLaiChoXacNhan === true
        }));
    }

    function _trangThaiHienThi(row) {
        if (row.trangThai === 'Huy' || row.trangThaiThanhToan === 'Đã hủy') return 'Huy';
        if (row.trangThaiThanhToan === 'Đã thanh toán')    return 'DaThanhToan';
        if (row.trangThaiThanhToan === 'Thanh toán một phần') return 'ThanhToanMotPhan';
        if (row.trangThaiThanhToan === 'Chưa thanh toán') {
            if (_isQuaHan(row.kyHoaDon)) return 'QuaHan';
            return 'ChuaThanhToan';
        }
        return row.trangThai || 'ChuaThanhToan';
    }

    function _isQuaHan(kyHoaDon) {
        if (!kyHoaDon) return false;
        const parts = kyHoaDon.split('-');
        if (parts.length !== 2) return false;
        const now = new Date();
        const kyNam = parseInt(parts[0]);
        const kyThang = parseInt(parts[1]);
        return (kyNam < now.getFullYear()) ||
               (kyNam === now.getFullYear() && kyThang < now.getMonth() + 1);
    }

    // ── Build toolbar ─────────────────────────────────────────────────────────
    function _buildToolbar() {
        const slot = document.getElementById('hdnToolbarSlot');
        if (!slot) return;

        const nhaTroList = window.normalizeArrayResponse(window.lookups?.nhatro || []);
        const phongList  = window.normalizeArrayResponse(window.lookups?.phong  || []);

        const nhaTroOpts = nhaTroList.map(n =>
            `<option value="${n.maNhaTro}" ${String(n.maNhaTro)===HDN.filterNhaTro?'selected':''}>${_esc(n.tenNhaTro||'Nhà trọ #'+n.maNhaTro)}</option>`
        ).join('');

        const visiblePhong = HDN.filterNhaTro
            ? phongList.filter(p => String(p.maNhaTro) === String(HDN.filterNhaTro))
            : phongList;
        const phongOpts = visiblePhong.map(p =>
            `<option value="${p.maPhong}" ${String(p.maPhong)===HDN.filterPhong?'selected':''}>${_esc(p.tenPhong||'Phòng #'+p.maPhong)}</option>`
        ).join('');

        const kyList = [...new Set(HDN.rawData.map(r => r.kyHoaDon).filter(Boolean))].sort().reverse();
        const kyOpts = kyList.map(k => {
            const [yr, mo] = k.split('-');
            return `<option value="${k}" ${HDN.filterKyHoaDon===k?'selected':''}>Tháng ${parseInt(mo)}/${yr}</option>`;
        }).join('');

        // Build active filter chips
        const chips = _buildChips();

        const canWrite = window.CURRENT_ROLE === 'Admin' || window.CURRENT_ROLE === 'ChuTro';
        const addBtn = canWrite ? `
            <button class="btn btn-primary hdn-btn-add" onclick="openModal()" style="white-space:nowrap;font-weight:700;gap:.4rem;display:flex;align-items:center;padding:.5rem 1rem;">
                <i class="fas fa-plus"></i> Thêm mới
            </button>` : '';

        slot.innerHTML = `
        <div class="hdn-toolbar-wrap">
            <!-- Hàng 1: Search chính + buttons -->
            <div class="hdn-toolbar-main">
                <div class="hdn-search-box">
                    <i class="fas fa-search hdn-search-icon"></i>
                    <input type="text" id="hdnKeyword" class="hdn-search-input"
                        placeholder="Mã HĐ, tên phòng, người thuê, kỳ, trạng thái..."
                        value="${_esc(HDN.keyword)}"
                        oninput="window._HoaDonSearch.onKeyword(this.value)">
                    ${HDN.keyword ? `<button class="hdn-search-clear" onclick="window._HoaDonSearch.onKeyword('');document.getElementById('hdnKeyword').value='';"><i class="fas fa-times"></i></button>` : ''}
                </div>

                <select class="hdn-select" onchange="window._HoaDonSearch.onTrangThai(this.value)" title="Trạng thái">
                    <option value="">Tất cả trạng thái</option>
                    <option value="ChuaThanhToan"    ${HDN.filterTrangThai==='ChuaThanhToan'    ?'selected':''}>Chưa thanh toán</option>
                    <option value="DaThanhToan"      ${HDN.filterTrangThai==='DaThanhToan'      ?'selected':''}>Đã thanh toán</option>
                    <option value="ThanhToanMotPhan" ${HDN.filterTrangThai==='ThanhToanMotPhan' ?'selected':''}>Thanh toán 1 phần</option>
                    <option value="QuaHan"           ${HDN.filterTrangThai==='QuaHan'           ?'selected':''}>Quá hạn</option>
                    <option value="Huy"              ${HDN.filterTrangThai==='Huy'              ?'selected':''}>Đã hủy</option>
                </select>

                <select class="hdn-select" onchange="window._HoaDonSearch.onKyHoaDon(this.value)" title="Kỳ hóa đơn">
                    <option value="">Tất cả kỳ</option>
                    ${kyOpts}
                </select>

                <div class="hdn-toolbar-actions">
                    <button class="hdn-btn-icon hdn-btn-advanced ${HDN.advancedOpen?'active':''}" onclick="window._HoaDonSearch.toggleAdvanced()" title="Bộ lọc nâng cao">
                        <i class="fas fa-sliders-h"></i>
                        <span>Nâng cao</span>
                        <i class="fas fa-chevron-${HDN.advancedOpen?'up':'down'}" style="font-size:.7rem;"></i>
                    </button>
                    <button class="hdn-btn-icon" onclick="window._HoaDonSearch.laiMoi()" title="Làm mới dữ liệu">
                        <i class="fas fa-rotate-right"></i>
                    </button>
                    <button class="hdn-btn-icon hdn-btn-excel" onclick="HoaDonExcel.exportExcel()" title="Xuất Excel">
                        <i class="fas fa-file-excel"></i>
                        <span>Xuất Excel</span>
                    </button>
                    ${addBtn}
                </div>
            </div>

            <!-- Hàng 2: Filter nâng cao (ẩn/hiện) -->
            <div id="hdnAdvancedPanel" class="hdn-advanced-panel ${HDN.advancedOpen?'open':''}">
                <div class="hdn-advanced-grid">
                    ${nhaTroList.length > 1 ? `
                    <div class="hdn-field">
                        <label>Nhà trọ</label>
                        <select id="hdnFilterNhaTro" class="hdn-select" onchange="window._HoaDonSearch.onNhaTro(this.value)">
                            <option value="">Tất cả nhà trọ</option>
                            ${nhaTroOpts}
                        </select>
                    </div>` : ''}
                    <div class="hdn-field">
                        <label>Phòng</label>
                        <select id="hdnFilterPhong" class="hdn-select" onchange="window._HoaDonSearch.onPhong(this.value)">
                            <option value="">Tất cả phòng</option>
                            ${phongOpts}
                        </select>
                    </div>
                    <div class="hdn-field">
                        <label>Ngày lập từ</label>
                        <input type="date" class="hdn-select" value="${HDN.filterNgayLapTu}"
                            onchange="window._HoaDonSearch.onNgayLapTu(this.value)">
                    </div>
                    <div class="hdn-field">
                        <label>Ngày lập đến</label>
                        <input type="date" class="hdn-select" value="${HDN.filterNgayLapDen}"
                            onchange="window._HoaDonSearch.onNgayLapDen(this.value)">
                    </div>
                    <div class="hdn-field">
                        <label>Tổng tiền từ (đ)</label>
                        <input type="number" class="hdn-select" placeholder="0" min="0"
                            value="${HDN.filterTongTienTu}"
                            onchange="window._HoaDonSearch.onTongTienTu(this.value)">
                    </div>
                    <div class="hdn-field">
                        <label>Tổng tiền đến (đ)</label>
                        <input type="number" class="hdn-select" placeholder="Không giới hạn" min="0"
                            value="${HDN.filterTongTienDen}"
                            onchange="window._HoaDonSearch.onTongTienDen(this.value)">
                    </div>
                    <div class="hdn-field" style="align-self:flex-end;">
                        <button class="btn btn-secondary" onclick="window._HoaDonSearch.reset()" style="width:100%;">
                            <i class="fas fa-filter-circle-xmark"></i> Xóa tất cả bộ lọc
                        </button>
                    </div>
                </div>
            </div>

            <!-- Hàng 3: Active filter chips -->
            ${chips ? `<div class="hdn-chips" id="hdnChipsRow">${chips}</div>` : ''}
        </div>

        <style>
        /* ── Toolbar wrapper ── */
        .hdn-toolbar-wrap{display:flex;flex-direction:column;gap:.6rem;margin-bottom:1rem;}
        .hdn-toolbar-main{display:flex;flex-wrap:wrap;align-items:center;gap:.5rem;}

        /* ── Search box ── */
        .hdn-search-box{position:relative;flex:1;min-width:240px;max-width:420px;}
        .hdn-search-icon{position:absolute;left:.85rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;font-size:.9rem;}
        .hdn-search-input{width:100%;padding:.5rem .5rem .5rem 2.4rem;border:1.5px solid var(--border-color,#e2e8f0);border-radius:8px;font-size:.9rem;background:#fff;transition:border-color .15s,box-shadow .15s;}
        .hdn-search-input:focus{outline:none;border-color:var(--primary,#3b82f6);box-shadow:0 0 0 3px rgba(59,130,246,.12);}
        .hdn-search-clear{position:absolute;right:.6rem;top:50%;transform:translateY(-50%);background:none;border:none;color:var(--text-light);cursor:pointer;padding:.2rem;font-size:.85rem;}
        .hdn-search-clear:hover{color:var(--text-primary);}

        /* ── Selects ── */
        .hdn-select{padding:.48rem .7rem;border:1.5px solid var(--border-color,#e2e8f0);border-radius:8px;font-size:.875rem;background:#fff;color:var(--text-primary,#1e293b);cursor:pointer;transition:border-color .15s;}
        .hdn-select:focus{outline:none;border-color:var(--primary,#3b82f6);}

        /* ── Action buttons ── */
        .hdn-toolbar-actions{display:flex;align-items:center;gap:.4rem;margin-left:auto;flex-wrap:wrap;}
        .hdn-btn-icon{display:inline-flex;align-items:center;gap:.35rem;padding:.45rem .75rem;border:1.5px solid var(--border-color,#e2e8f0);border-radius:8px;background:#fff;color:var(--text-primary,#1e293b);font-size:.85rem;font-weight:500;cursor:pointer;white-space:nowrap;transition:all .15s;}
        .hdn-btn-icon:hover{border-color:var(--primary,#3b82f6);color:var(--primary,#3b82f6);background:#f0f7ff;}
        .hdn-btn-icon.active{border-color:var(--primary,#3b82f6);color:var(--primary,#3b82f6);background:#eff6ff;}
        .hdn-btn-excel{border-color:#16a34a;background:#16a34a;color:#fff;font-weight:700;}
        .hdn-btn-excel:hover{border-color:#15803d;color:#fff;background:#15803d;}
        .hdn-btn-add{background:var(--primary,#3b82f6);color:#fff;border-color:var(--primary,#3b82f6);font-weight:700;}
        .hdn-btn-add:hover{background:#2563eb;border-color:#2563eb;color:#fff;}

        /* ── Advanced panel ── */
        .hdn-advanced-panel{display:none;padding:.85rem 1rem;background:#f8fafc;border:1.5px solid var(--border-color,#e2e8f0);border-radius:10px;}
        .hdn-advanced-panel.open{display:block;}
        .hdn-advanced-grid{display:flex;flex-wrap:wrap;gap:.75rem;align-items:flex-start;}
        .hdn-field{display:flex;flex-direction:column;gap:.3rem;min-width:160px;}
        .hdn-field label{font-size:.78rem;font-weight:600;color:var(--text-light,#64748b);text-transform:uppercase;letter-spacing:.03em;}

        /* ── Filter chips ── */
        .hdn-chips{display:flex;flex-wrap:wrap;gap:.4rem;align-items:center;}
        .hdn-chip{display:inline-flex;align-items:center;gap:.35rem;padding:.2rem .6rem .2rem .65rem;background:#eff6ff;color:#1d4ed8;border:1px solid #bfdbfe;border-radius:20px;font-size:.8rem;font-weight:500;white-space:nowrap;}
        .hdn-chip-remove{background:none;border:none;cursor:pointer;color:#93c5fd;padding:0;font-size:.75rem;line-height:1;margin-left:.1rem;}
        .hdn-chip-remove:hover{color:#1d4ed8;}
        .hdn-chip-clear-all{display:inline-flex;align-items:center;gap:.3rem;padding:.2rem .6rem;background:#fee2e2;color:#b91c1c;border:1px solid #fca5a5;border-radius:20px;font-size:.8rem;font-weight:600;cursor:pointer;white-space:nowrap;}
        .hdn-chip-clear-all:hover{background:#fecaca;}
        </style>`;
    }

    function _buildChips() {
        const parts = [];
        const nhaTroList = window.normalizeArrayResponse(window.lookups?.nhatro || []);
        const phongList  = window.normalizeArrayResponse(window.lookups?.phong  || []);

        const STATUS_LABEL = {
            ChuaThanhToan: 'Chưa thanh toán', DaThanhToan: 'Đã thanh toán',
            ThanhToanMotPhan: 'TT 1 phần', QuaHan: 'Quá hạn', Huy: 'Đã hủy'
        };

        if (HDN.keyword)
            parts.push({ label: `"${HDN.keyword}"`, clear: `window._HoaDonSearch.onKeyword('');document.getElementById('hdnKeyword').value='';` });
        if (HDN.filterTrangThai)
            parts.push({ label: STATUS_LABEL[HDN.filterTrangThai] || HDN.filterTrangThai, clear: `window._HoaDonSearch.onTrangThai('')` });
        if (HDN.filterKyHoaDon) {
            const [yr, mo] = HDN.filterKyHoaDon.split('-');
            parts.push({ label: `Kỳ: T${parseInt(mo)}/${yr}`, clear: `window._HoaDonSearch.onKyHoaDon('')` });
        }
        if (HDN.filterNhaTro) {
            const nt = nhaTroList.find(n => String(n.maNhaTro) === HDN.filterNhaTro);
            parts.push({ label: nt?.tenNhaTro || `NT #${HDN.filterNhaTro}`, clear: `window._HoaDonSearch.onNhaTro('')` });
        }
        if (HDN.filterPhong) {
            const p = phongList.find(p => String(p.maPhong) === HDN.filterPhong);
            parts.push({ label: p?.tenPhong || `P #${HDN.filterPhong}`, clear: `window._HoaDonSearch.onPhong('')` });
        }
        if (HDN.filterNgayLapTu)
            parts.push({ label: `Từ: ${HDN.filterNgayLapTu}`, clear: `window._HoaDonSearch.onNgayLapTu('')` });
        if (HDN.filterNgayLapDen)
            parts.push({ label: `Đến: ${HDN.filterNgayLapDen}`, clear: `window._HoaDonSearch.onNgayLapDen('')` });
        if (HDN.filterTongTienTu)
            parts.push({ label: `≥ ${Number(HDN.filterTongTienTu).toLocaleString('vi-VN')}đ`, clear: `window._HoaDonSearch.onTongTienTu('')` });
        if (HDN.filterTongTienDen)
            parts.push({ label: `≤ ${Number(HDN.filterTongTienDen).toLocaleString('vi-VN')}đ`, clear: `window._HoaDonSearch.onTongTienDen('')` });

        if (!parts.length) return '';

        const chipHtml = parts.map(p =>
            `<span class="hdn-chip"><i class="fas fa-tag" style="font-size:.7rem;opacity:.6;"></i>${_esc(p.label)}<button class="hdn-chip-remove" onclick="${p.clear}" title="Xóa bộ lọc này"><i class="fas fa-times"></i></button></span>`
        ).join('');

        const clearAll = parts.length > 1
            ? `<button class="hdn-chip-clear-all" onclick="window._HoaDonSearch.reset()"><i class="fas fa-times-circle"></i> Xóa tất cả</button>`
            : '';

        return chipHtml + clearAll;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────
    let _kwTimer;
    function onKeyword(v) {
        HDN.keyword = v.trim();
        HDN.page = 1;
        clearTimeout(_kwTimer);
        _kwTimer = setTimeout(_applyAndRender, 300);
    }
    function onTrangThai(v)     { HDN.filterTrangThai = v; HDN.page = 1; _buildToolbar(); _applyAndRender(); }
    function onKyHoaDon(v)      { HDN.filterKyHoaDon = v; HDN.page = 1; _buildToolbar(); _applyAndRender(); }
    function onNhaTro(v)        { HDN.filterNhaTro = v; HDN.filterPhong = ''; HDN.page = 1; _rebuildPhong(); _buildToolbar(); _applyAndRender(); }
    function onPhong(v)         { HDN.filterPhong = v; HDN.page = 1; _buildToolbar(); _applyAndRender(); }
    function onNgayLapTu(v)     { HDN.filterNgayLapTu = v;  HDN.page = 1; _buildToolbar(); _applyAndRender(); }
    function onNgayLapDen(v)    { HDN.filterNgayLapDen = v; HDN.page = 1; _buildToolbar(); _applyAndRender(); }
    function onTongTienTu(v)    { HDN.filterTongTienTu = v;  HDN.page = 1; _buildToolbar(); _applyAndRender(); }
    function onTongTienDen(v)   { HDN.filterTongTienDen = v; HDN.page = 1; _buildToolbar(); _applyAndRender(); }

    function toggleAdvanced() {
        HDN.advancedOpen = !HDN.advancedOpen;
        _buildToolbar();
    }

    function onSort(key) {
        HDN.sortDir = HDN.sortKey === key && HDN.sortDir === 'asc' ? 'desc' : 'asc';
        HDN.sortKey = key;
        HDN.page = 1;
        _applyAndRender();
    }
    function onPageSize(v) { HDN.pageSize = parseInt(v) || 10; HDN.page = 1; _applyAndRender(); }
    function onPage(p)     { HDN.page = p; _applyAndRender(); }

    function reset() {
        Object.assign(HDN, {
            keyword:'', filterNhaTro:'', filterPhong:'', filterKyHoaDon:'',
            filterTrangThai:'', filterNgayLapTu:'', filterNgayLapDen:'',
            filterTongTienTu:'', filterTongTienDen:'',
            sortKey:'ngayLap', sortDir:'desc', page:1, pageSize:10,
            advancedOpen: false,
        });
        _buildToolbar();
        _applyAndRender();
    }

    async function laiMoi() {
        const slot = document.getElementById('hdnTableSlot');
        if (slot) slot.innerHTML = `<div style="text-align:center;padding:2rem;"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>`;
        try {
            const rawData = await window.apiFetch(window.modules?.hoadon?.endpoint || 'api/HoaDon');
            HDN.rawData = window.normalizeArrayResponse(rawData);
            HDN.page = 1;
            _buildToolbar();
            _applyAndRender();
        } catch (e) {
            if (slot) slot.innerHTML = `<div style="text-align:center;color:var(--error);padding:1.5rem;">Lỗi tải lại: ${e.message}</div>`;
        }
    }

    function _rebuildPhong() {
        const sel = document.getElementById('hdnFilterPhong');
        if (!sel) return;
        const phongList = window.normalizeArrayResponse(window.lookups?.phong || []);
        const visible = HDN.filterNhaTro
            ? phongList.filter(p => String(p.maNhaTro) === String(HDN.filterNhaTro))
            : phongList;
        sel.innerHTML = `<option value="">Tất cả phòng</option>` +
            visible.map(p => `<option value="${p.maPhong}">${_esc(p.tenPhong||'Phòng #'+p.maPhong)}</option>`).join('');
    }

    // ── Filter + Sort ─────────────────────────────────────────────────────────
    function _filterAndSort(data) {
        let result = data.slice();
        result = result.map(r => ({ ...r, _tt: _trangThaiHienThi(r) }));

        const kw = HDN.keyword.toLowerCase();
        if (kw) {
            result = result.filter(r => {
                const maHD  = String(r.maHoaDon || '');
                const phong = (r.tenPhong || '').toLowerCase();
                const nguoi = (r.tenNguoiThue || '').toLowerCase();
                const ky    = (r.kyHoaDon || '').toLowerCase();
                const tt    = _ttLabel(r._tt).toLowerCase();
                return maHD.includes(kw) || phong.includes(kw) || nguoi.includes(kw) || ky.includes(kw) || tt.includes(kw);
            });
        }
        if (HDN.filterTrangThai)  result = result.filter(r => r._tt === HDN.filterTrangThai);
        if (HDN.filterKyHoaDon)   result = result.filter(r => r.kyHoaDon === HDN.filterKyHoaDon);
        if (HDN.filterNhaTro) {
            result = result.filter(r => {
                const phong = (window.lookups?.phong || []).find(p => Number(p.maPhong) === Number(r.maPhong));
                return phong && String(phong.maNhaTro) === String(HDN.filterNhaTro);
            });
        }
        if (HDN.filterPhong) result = result.filter(r => String(r.maPhong) === String(HDN.filterPhong));

        const _toTs = s => s ? new Date(s).getTime() : null;
        if (HDN.filterNgayLapTu)  result = result.filter(r => _toTs(r.ngayLap) >= _toTs(HDN.filterNgayLapTu + 'T00:00:00'));
        if (HDN.filterNgayLapDen) result = result.filter(r => _toTs(r.ngayLap) <= _toTs(HDN.filterNgayLapDen + 'T23:59:59'));
        if (HDN.filterTongTienTu)  result = result.filter(r => Number(r.tongTien) >= Number(HDN.filterTongTienTu));
        if (HDN.filterTongTienDen) result = result.filter(r => Number(r.tongTien) <= Number(HDN.filterTongTienDen));

        const dir = HDN.sortDir === 'asc' ? 1 : -1;
        result.sort((a, b) => {
            const va = _sortVal(a, HDN.sortKey);
            const vb = _sortVal(b, HDN.sortKey);
            if (va < vb) return -1 * dir;
            if (va > vb) return 1 * dir;
            return 0;
        });
        return result;
    }

    function _sortVal(row, key) {
        switch (key) {
            case 'ngayLap':      return row.ngayLap ? new Date(row.ngayLap).getTime() : 0;
            case 'kyHoaDon':     return row.kyHoaDon || '';
            case 'tongTien':     return Number(row.tongTien) || 0;
            case 'trangThai':    return _ttLabel(row._tt || '');
            case 'tenPhong':     return (row.tenPhong || '').toLowerCase();
            case 'tenNguoiThue': return (row.tenNguoiThue || '').toLowerCase();
            default:             return row.ngayLap ? new Date(row.ngayLap).getTime() : 0;
        }
    }

    // ── Apply & Render ─────────────────────────────────────────────────────────
    function _applyAndRender() {
        HDN.filtered = _filterAndSort(HDN.rawData);
        _renderTable();
        _renderPaging();
    }

    // ── Hiển thị trạng thái ───────────────────────────────────────────────────
    const STATUS_CFG = {
        ChuaThanhToan:    { cls: 'hdn-badge-warn',    icon: 'fa-clock',                  label: 'Chưa thanh toán',  rowCls: 'hdn-row-warn' },
        DaThanhToan:      { cls: 'hdn-badge-ok',      icon: 'fa-check-circle',            label: 'Đã thanh toán',    rowCls: 'hdn-row-ok' },
        ThanhToanMotPhan: { cls: 'hdn-badge-partial', icon: 'fa-adjust',                  label: 'Thanh toán 1 phần',rowCls: 'hdn-row-partial' },
        QuaHan:           { cls: 'hdn-badge-overdue', icon: 'fa-exclamation-triangle',    label: 'Quá hạn',          rowCls: 'hdn-row-overdue' },
        Huy:              { cls: 'hdn-badge-cancel',  icon: 'fa-ban',                     label: 'Đã hủy',           rowCls: 'hdn-row-cancel' },
    };

    function _ttLabel(tt) { return (STATUS_CFG[tt] || {}).label || tt; }

    function _statusBadge(row) {
        const tt  = row._tt || 'ChuaThanhToan';
        const cfg = STATUS_CFG[tt] || { cls: 'hdn-badge-cancel', icon: 'fa-circle', label: tt };
        const extra = tt === 'QuaHan' ? ' hdn-badge-pulse' : '';
        return `<span class="hdn-badge ${cfg.cls}${extra}"><i class="fas ${cfg.icon}"></i> ${cfg.label}</span>`;
    }

    // ── Render bảng ───────────────────────────────────────────────────────────
    function _th(key, label) {
        const active = HDN.sortKey === key;
        const icon   = active ? (HDN.sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down') : 'fa-sort';
        const style  = active ? 'color:var(--primary);' : 'color:var(--text-light);';
        return `<th style="cursor:pointer;white-space:nowrap;user-select:none;"
                    onclick="window._HoaDonSearch.onSort('${key}')">
                    ${label} <i class="fas ${icon}" style="${style}font-size:.75rem;"></i>
                </th>`;
    }

    function _renderTable() {
        const slot = document.getElementById('hdnTableSlot');
        if (!slot) return;

        const total    = HDN.filtered.length;
        const start    = (HDN.page - 1) * HDN.pageSize;
        const pageData = HDN.filtered.slice(start, start + HDN.pageSize);
        const canWrite = window.CURRENT_ROLE === 'Admin' || window.CURRENT_ROLE === 'ChuTro';
        const canSend  = window.CURRENT_ROLE === 'NguoiDung';

        // Tổng tiền toàn bộ kết quả lọc
        const tongTienTatCa = HDN.filtered.reduce((s, r) => s + (Number(r.tongTien) || 0), 0);
        const tongDaTT      = HDN.filtered.reduce((s, r) => s + (Number(r.daThanhToan) || 0), 0);
        const tongConLai    = HDN.filtered.reduce((s, r) => s + (Number(r.conLai) || 0), 0);
        const pctDaTT       = tongTienTatCa > 0 ? Math.round((tongDaTT / tongTienTatCa) * 100) : 0;

        const fmtCur  = v => new Intl.NumberFormat('vi-VN').format(v) + 'đ';
        const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN', {day:'2-digit',month:'2-digit',year:'numeric'}) : '---';

        const thead = `<thead><tr>
            <th style="white-space:nowrap;">Mã HĐ</th>
            ${_th('tenPhong',     'Phòng')}
            ${_th('tenNguoiThue', 'Người thuê')}
            ${_th('kyHoaDon',     'Kỳ HĐ')}
            ${_th('ngayLap',      'Ngày lập')}
            ${_th('tongTien',     'Tổng tiền')}
            <th style="white-space:nowrap;">Đã TT / Còn lại</th>
            ${_th('trangThai',    'Trạng thái')}
            <th style="text-align:center;">Thao tác</th>
        </tr></thead>`;

        let tbody;
        if (!pageData.length) {
            tbody = `<tbody><tr><td colspan="9" style="text-align:center;padding:2.5rem;color:var(--text-light);">
                <i class="fas fa-file-invoice" style="font-size:2rem;display:block;margin-bottom:.5rem;opacity:.35;"></i>
                Không tìm thấy hóa đơn phù hợp.
            </td></tr></tbody>`;
        } else {
            const nhaTroMulti = window.normalizeArrayResponse(window.lookups?.nhatro || []).length > 1;

            tbody = '<tbody>' + pageData.map(item => {
                const tt     = item._tt || 'ChuaThanhToan';
                const cfg    = STATUS_CFG[tt] || {};
                const rowCls = cfg.rowCls || '';

                // Phòng + nhà trọ
                const nhaTroName = _getNhaTroName(item.maPhong);
                const phongCell  = nhaTroMulti && nhaTroName
                    ? `<div style="font-weight:500;">${_esc(item.tenPhong)}</div><div style="font-size:.8rem;color:var(--text-light);">${_esc(nhaTroName)}</div>`
                    : _esc(item.tenPhong || `Phòng #${item.maPhong}`);

                // Progress bar Đã TT / Còn lại
                const tong   = Number(item.tongTien) || 0;
                const daTT   = Number(item.daThanhToan) || 0;
                const conLai = Number(item.conLai) || 0;
                const pct    = tong > 0 ? Math.round((daTT / tong) * 100) : (tt === 'DaThanhToan' ? 100 : 0);
                const progressBar = `
                    <div style="font-size:.78rem;color:var(--text-light);margin-bottom:.2rem;">
                        <span style="color:#16a34a;">${fmtCur(daTT)}</span>
                        ${conLai > 0 ? ` / <span style="color:#dc2626;font-weight:600;">${fmtCur(conLai)}</span>` : ''}
                    </div>
                    <div style="height:5px;background:#f1f5f9;border-radius:99px;overflow:hidden;min-width:80px;">
                        <div style="height:100%;width:${pct}%;background:${pct===100?'#16a34a':'#3b82f6'};border-radius:99px;transition:width .3s;"></div>
                    </div>`;

                // Nút thao tác dạng dropdown ⋮
                const menuItems = [];
                if (tt === 'DaThanhToan' || tt === 'ThanhToanMotPhan') {
                    menuItems.push(`<button class="btn-action" style="background:#0891b2;" onclick="openHoaDonBienLaiGallery(${item.maHoaDon})"><i class="fas fa-receipt"></i> Xem ảnh biên lai</button>`);
                }
                menuItems.push(`<button class="btn-action" style="background:#6366f1;" onclick="HoaDonPrint.openModal(${item.maHoaDon})"><i class="fas fa-print"></i> In hóa đơn</button>`);

                if (conLai > 0 && tt !== 'Huy' && tt !== 'DaThanhToan') {
                    menuItems.push(`<button class="btn-action" style="background:#0f766e;" onclick="openHoaDonThanhToanModal(${item.maHoaDon})"><i class="fas fa-qrcode"></i> Thông tin thanh toán</button>`);
                }

                if (canSend && conLai > 0 && tt !== 'Huy' && tt !== 'DaThanhToan') {
                    const daCoBienLai = item._daCoBienLaiChoXacNhan === true;
                    if (daCoBienLai) {
                        menuItems.push(`<span class="btn-action" style="cursor:default;opacity:.65;"><i class="fas fa-clock"></i> Chờ duyệt biên lai</span>`);
                    } else {
                        const itemJson = JSON.stringify({
                            maHoaDon: item.maHoaDon, kyHoaDon: item.kyHoaDon,
                            tongTien: item.tongTien, conLai: item.conLai, tenPhong: item.tenPhong,
                        }).replace(/"/g, '&quot;');
                        menuItems.push(`<button class="btn-action btn-edit" style="background:#10b981;" onclick="moModalGuiBienLai(JSON.parse(this.dataset.item))" data-item="${itemJson}"><i class="fas fa-paper-plane"></i> Gửi biên lai</button>`);
                    }
                }
                if (canWrite) {
                    menuItems.push(`<button class="btn-action btn-edit" onclick="editItem('hoadon',${item.maHoaDon})"><i class="fas fa-edit"></i> Sửa</button>`);
                    if (tt !== 'Huy') {
                        menuItems.push(`<button class="btn-action btn-delete" onclick="deleteItem('hoadon',${item.maHoaDon})"><i class="fas fa-trash"></i> Xóa</button>`);
                    }
                }

                const actionHtml = `
                    <details class="module-action-menu">
                        <summary title="Thao tác"><i class="fas fa-ellipsis-vertical"></i></summary>
                        <div class="module-action-list">
                            ${menuItems.join('')}
                        </div>
                    </details>`;

                return `<tr class="hdn-tr ${rowCls}">
                    <td style="font-weight:700;color:var(--primary);">#${item.maHoaDon}</td>
                    <td>${phongCell}</td>
                    <td>${_esc(item.tenNguoiThue || '---')}</td>
                    <td style="white-space:nowrap;font-weight:500;">${_esc(item.kyHoaDon || '---')}</td>
                    <td style="white-space:nowrap;">${fmtDate(item.ngayLap)}</td>
                    <td style="white-space:nowrap;font-weight:600;">${fmtCur(item.tongTien || 0)}</td>
                    <td style="min-width:110px;">${progressBar}</td>
                    <td>${_statusBadge(item)}</td>
                    <td style="text-align:center;">${actionHtml}</td>
                </tr>`;
            }).join('') + '</tbody>';
        }

        // Banner quá hạn
        const soQuaHan = HDN.filtered.filter(r => r._tt === 'QuaHan').length;
        const quaHanBanner = soQuaHan > 0 && !HDN.filterTrangThai ? `
            <div class="hdn-overdue-banner">
                <i class="fas fa-exclamation-triangle"></i>
                <span>Có <strong>${soQuaHan}</strong> hóa đơn quá hạn chưa thanh toán.</span>
                <button class="btn btn-secondary hdn-overdue-action"
                    onclick="window._HoaDonSearch.onTrangThai('QuaHan')">Xem</button>
            </div>` : '';

        // ── Summary cards (to và rõ) ──
        const summaryHtml = total > 0 ? `
            <div class="hdn-summary-cards">
                <div class="hdn-sum-card hdn-sum-total">
                    <div class="hdn-sum-icon"><i class="fas fa-file-invoice-dollar"></i></div>
                    <div class="hdn-sum-body">
                        <div class="hdn-sum-label">Tổng hóa đơn</div>
                        <div class="hdn-sum-value">${total} HĐ</div>
                        <div class="hdn-sum-amount">${fmtCur(tongTienTatCa)}</div>
                    </div>
                </div>
                <div class="hdn-sum-card hdn-sum-paid">
                    <div class="hdn-sum-icon"><i class="fas fa-check-circle"></i></div>
                    <div class="hdn-sum-body">
                        <div class="hdn-sum-label">Đã thanh toán</div>
                        <div class="hdn-sum-value" style="color:#16a34a;">${fmtCur(tongDaTT)}</div>
                        <div class="hdn-sum-pct">${pctDaTT}% tổng</div>
                    </div>
                </div>
                <div class="hdn-sum-card hdn-sum-remain">
                    <div class="hdn-sum-icon"><i class="fas fa-hourglass-half"></i></div>
                    <div class="hdn-sum-body">
                        <div class="hdn-sum-label">Còn lại</div>
                        <div class="hdn-sum-value" style="color:${tongConLai>0?'#dc2626':'#16a34a'};">${fmtCur(tongConLai)}</div>
                        <div class="hdn-sum-pct">${100-pctDaTT}% tổng</div>
                    </div>
                </div>
                <div class="hdn-sum-card hdn-sum-progress">
                    <div class="hdn-sum-body" style="width:100%;">
                        <div style="display:flex;justify-content:space-between;margin-bottom:.4rem;">
                            <span class="hdn-sum-label">Tiến độ thu</span>
                            <span style="font-weight:700;font-size:.95rem;">${pctDaTT}%</span>
                        </div>
                        <div style="height:10px;background:#f1f5f9;border-radius:99px;overflow:hidden;">
                            <div style="height:100%;width:${pctDaTT}%;background:linear-gradient(90deg,#3b82f6,#16a34a);border-radius:99px;transition:width .5s;"></div>
                        </div>
                        <div style="display:flex;justify-content:space-between;margin-top:.35rem;font-size:.78rem;color:var(--text-light);">
                            <span>0đ</span><span>${fmtCur(tongTienTatCa)}</span>
                        </div>
                    </div>
                </div>
            </div>` : '';

        // Legend
        const legendHtml = `
            <div class="hdn-legend">
                <span class="hdn-legend-dot hdn-legend-warn"></span><span>Chưa TT</span>
                <span class="hdn-legend-dot hdn-legend-partial"></span><span>TT 1 phần</span>
                <span class="hdn-legend-dot hdn-legend-ok"></span><span>Đã TT</span>
                <span class="hdn-legend-dot hdn-legend-overdue"></span><span>Quá hạn</span>
                <span class="hdn-legend-dot hdn-legend-cancel"></span><span>Đã hủy</span>
            </div>`;

        slot.innerHTML = `
            ${quaHanBanner}
            ${summaryHtml}
            ${total > HDN.pageSize ? `<div style="font-size:.8rem;color:var(--text-light);margin-bottom:.4rem;">Hiển thị ${start+1}–${Math.min(start+HDN.pageSize,total)} / ${total} hóa đơn</div>` : ''}
            ${legendHtml}
            <div class="table-container">
                <table>${thead}${tbody}</table>
            </div>

            <style>
            /* ── Row colors ── */
            .hdn-overdue-banner{background:var(--hdn-overdue-bg,#fee2e2);border:1px solid var(--hdn-overdue-border,#fca5a5);border-radius:8px;padding:.6rem 1rem;margin-bottom:.75rem;font-size:.875rem;display:flex;gap:.5rem;align-items:center;color:var(--hdn-overdue-text,#7f1d1d);}
            .hdn-overdue-banner i{color:var(--hdn-overdue-icon,#dc2626);}
            .hdn-overdue-action{width:auto!important;min-width:64px;flex:0 0 auto;margin-left:auto;padding:.2rem .65rem!important;font-size:.8rem!important;}
            body.dark-mode{--hdn-overdue-bg:#2d1212;--hdn-overdue-border:#7f1d1d;--hdn-overdue-text:#fecaca;--hdn-overdue-icon:#f87171;}
            body.dark-mode .hdn-overdue-action{background:#162032!important;border-color:#334155!important;color:#2dd4bf!important;}
            body.dark-mode .hdn-overdue-action:hover{background:#1e3a5f!important;border-color:#14b8a6!important;color:#5eead4!important;}

            .hdn-tr{transition:background .12s;}
            .hdn-tr:hover{background:rgba(59,130,246,.04) !important;}
            .hdn-row-warn    {background:rgba(251,191,36,.08);}
            .hdn-row-partial {background:rgba(59,130,246,.06);}
            .hdn-row-ok      {background:rgba(22,163,74,.05);}
            .hdn-row-overdue {background:rgba(239,68,68,.06);}
            .hdn-row-cancel  {background:rgba(100,116,139,.05);opacity:.8;}

            /* ── Badge ── */
            .hdn-badge{display:inline-flex;align-items:center;gap:.3rem;padding:.25rem .65rem;border-radius:20px;font-size:.78rem;font-weight:700;white-space:nowrap;}
            .hdn-badge-ok      {background:#dcfce7;color:#15803d;border:1px solid #bbf7d0;}
            .hdn-badge-warn    {background:#fef9c3;color:#a16207;border:1px solid #fde68a;}
            .hdn-badge-partial {background:#dbeafe;color:#1d4ed8;border:1px solid #bfdbfe;}
            .hdn-badge-overdue {background:#fee2e2;color:#b91c1c;border:1px solid #fca5a5;}
            .hdn-badge-cancel  {background:#f1f5f9;color:#64748b;border:1px solid #e2e8f0;}
            body.dark-mode .hdn-badge-ok{background:#052e16;color:#86efac;border-color:#14532d;}
            body.dark-mode .hdn-badge-warn{background:#2d1f00;color:#fbbf24;border-color:#78350f;}
            body.dark-mode .hdn-badge-partial{background:#0c2840;color:#93c5fd;border-color:#1e3a8a;}
            body.dark-mode .hdn-badge-overdue{background:#2d1212;color:#f87171;border-color:#7f1d1d;}
            body.dark-mode .hdn-badge-cancel{background:#273548;color:#94a3b8;border-color:#334155;}
            @keyframes hdn-pulse{0%,100%{opacity:1}50%{opacity:.6}}
            .hdn-badge-pulse{animation:hdn-pulse 1.8s ease-in-out infinite;}

            /* ── Summary cards ── */
            .hdn-summary-cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(180px,1fr));gap:.75rem;margin-bottom:1rem;}
            .hdn-sum-card{display:flex;align-items:center;gap:.85rem;padding:.9rem 1.1rem;background:#fff;border:1.5px solid var(--border-color,#e2e8f0);border-radius:12px;box-shadow:0 1px 4px rgba(15,23,42,.06);}
            .hdn-sum-progress{grid-column:span 2;}
            @media(max-width:640px){.hdn-sum-progress{grid-column:span 1;}}
            .hdn-sum-icon{width:2.4rem;height:2.4rem;border-radius:10px;display:flex;align-items:center;justify-content:center;font-size:1.1rem;flex-shrink:0;}
            .hdn-sum-total .hdn-sum-icon{background:#eff6ff;color:#3b82f6;}
            .hdn-sum-paid  .hdn-sum-icon{background:#f0fdf4;color:#16a34a;}
            .hdn-sum-remain.hdn-sum-icon{background:#fef2f2;color:#dc2626;}
            .hdn-sum-remain .hdn-sum-icon{background:#fef2f2;color:#dc2626;}
            .hdn-sum-label{font-size:.75rem;color:var(--text-light,#64748b);font-weight:600;text-transform:uppercase;letter-spacing:.03em;}
            .hdn-sum-value{font-size:1.1rem;font-weight:800;color:var(--text-primary,#1e293b);line-height:1.2;margin-top:.1rem;}
            .hdn-sum-amount{font-size:.82rem;color:var(--text-light,#64748b);margin-top:.1rem;}
            .hdn-sum-pct{font-size:.78rem;color:var(--text-light,#64748b);margin-top:.1rem;}

            /* ── Legend ── */
            .hdn-legend{display:flex;flex-wrap:wrap;align-items:center;gap:.4rem .8rem;font-size:.78rem;color:var(--text-light);margin-bottom:.5rem;}
            .hdn-legend-dot{width:.55rem;height:.55rem;border-radius:50%;display:inline-block;}
            .hdn-legend-warn   {background:#f59e0b;}
            .hdn-legend-partial{background:#3b82f6;}
            .hdn-legend-ok     {background:#16a34a;}
            .hdn-legend-overdue{background:#ef4444;}
            .hdn-legend-cancel {background:#94a3b8;}

            /* ── Action menu ── */
            .hdn-action-wrap{position:relative;display:inline-block;}
            .hdn-action-btn{width:2rem;height:2rem;border-radius:8px;border:1.5px solid var(--border-color,#e2e8f0);background:#fff;color:var(--text-light);cursor:pointer;font-size:.9rem;display:flex;align-items:center;justify-content:center;transition:all .15s;}
            .hdn-action-btn:hover{border-color:var(--primary,#3b82f6);color:var(--primary,#3b82f6);background:#eff6ff;}
            .hdn-menu{position:absolute;right:0;top:calc(100% + 4px);z-index:999;background:#fff;border:1.5px solid var(--border-color,#e2e8f0);border-radius:10px;box-shadow:0 8px 24px rgba(15,23,42,.12);min-width:160px;padding:.3rem;display:none;animation:hdn-menu-in .12s ease;}
            @keyframes hdn-menu-in{from{opacity:0;transform:translateY(-4px)}to{opacity:1;transform:none}}
            .hdn-menu.open{display:block;}
            .hdn-menu-item{display:flex;align-items:center;gap:.55rem;width:100%;padding:.5rem .75rem;border:none;background:none;font-size:.85rem;color:var(--text-primary,#1e293b);cursor:pointer;border-radius:7px;text-align:left;transition:background .1s;}
            .hdn-menu-item:hover{background:#f8fafc;}
            .hdn-menu-item i{width:1rem;text-align:center;font-size:.85rem;color:var(--text-light);}
            .hdn-menu-danger{color:#dc2626 !important;}
            .hdn-menu-danger i{color:#dc2626 !important;}
            .hdn-menu-danger:hover{background:#fef2f2 !important;}
            .hdn-menu-print{color:#3730a3 !important;}
            .hdn-menu-print i{color:#4f46e5 !important;}
            .hdn-menu-print:hover{background:#eef2ff !important;}
            .hdn-menu-edit{color:#075985 !important;}
            .hdn-menu-edit i{color:#0284c7 !important;}
            .hdn-menu-edit:hover{background:#e0f2fe !important;}
            .hdn-menu-send{color:#0ea5e9 !important;}
            .hdn-menu-send i{color:#0ea5e9 !important;}
            .hdn-menu-disabled{color:var(--text-light);cursor:default;pointer-events:none;}
            .hdn-menu-divider{height:1px;background:var(--border-color,#e2e8f0);margin:.3rem 0;}
            </style>`;

        // Đóng menu khi click ra ngoài
        setTimeout(() => {
            document.addEventListener('click', _closeAllMenus, { once: false, capture: true });
        }, 0);
    }

    function _closeAllMenus(e) {
        if (!e.target.closest('.hdn-action-wrap')) {
            document.querySelectorAll('.hdn-menu.open').forEach(m => m.classList.remove('open'));
        }
    }

    // Toggle dropdown menu
    window.hdnToggleMenu = function(e, id) {
        e.stopPropagation();
        const menu = document.getElementById(`hdnMenu_${id}`);
        if (!menu) return;
        const wasOpen = menu.classList.contains('open');
        document.querySelectorAll('.hdn-menu.open').forEach(m => m.classList.remove('open'));
        if (!wasOpen) menu.classList.add('open');
    };

    // ── Render phân trang ─────────────────────────────────────────────────────
    function _renderPaging() {
        const slot = document.getElementById('hdnPagingSlot');
        if (!slot) return;

        const total   = HDN.filtered.length;
        const totalPg = Math.max(1, Math.ceil(total / HDN.pageSize));
        const cur     = Math.min(HDN.page, totalPg);

        const sizeOpts = [10, 20, 50].map(s =>
            `<option value="${s}" ${HDN.pageSize===s?'selected':''}>${s}</option>`
        ).join('');

        let btns = '';
        if (totalPg > 1) {
            const lo = Math.max(1, cur - 2);
            const hi = Math.min(totalPg, lo + 4);
            // Trang đầu
            btns += `<button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(1)" title="Trang đầu" ${cur===1?'disabled':''}><i class="fas fa-angle-double-left"></i></button>`;
            btns += `<button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(${cur-1})" ${cur===1?'disabled':''}><i class="fas fa-chevron-left"></i></button>`;
            if (lo > 1) btns += `<span class="hdn-pg-ell">…</span>`;
            for (let i=lo; i<=hi; i++)
                btns += `<button class="hdn-pg-btn${i===cur?' hdn-pg-active':''}" onclick="window._HoaDonSearch.onPage(${i})">${i}</button>`;
            if (hi < totalPg) btns += `<span class="hdn-pg-ell">…</span>`;
            btns += `<button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(${cur+1})" ${cur===totalPg?'disabled':''}><i class="fas fa-chevron-right"></i></button>`;
            // Trang cuối
            btns += `<button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(${totalPg})" title="Trang cuối" ${cur===totalPg?'disabled':''}><i class="fas fa-angle-double-right"></i></button>`;
        }

        slot.innerHTML = `
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;margin-top:.75rem;">
            <div style="display:flex;align-items:center;gap:.5rem;font-size:.875rem;color:var(--text-light);">
                Hiển thị
                <select class="form-control" style="width:auto;padding:.25rem .5rem;font-size:.875rem;"
                    onchange="window._HoaDonSearch.onPageSize(this.value)">${sizeOpts}</select>
                dòng / trang
                <span style="color:var(--text-light);font-size:.8rem;">(${total} kết quả)</span>
            </div>
            <div style="display:flex;gap:.25rem;align-items:center;">${btns}</div>
        </div>
        <style>
            .hdn-pg-btn{padding:.35rem .65rem;border:1.5px solid var(--border-color,#e2e8f0);border-radius:7px;background:#fff;cursor:pointer;font-size:.85rem;color:var(--text-primary,#1e293b);transition:all .15s;min-width:2rem;}
            .hdn-pg-btn:hover:not(:disabled){background:#eff6ff;border-color:var(--primary,#3b82f6);color:var(--primary,#3b82f6);}
            .hdn-pg-btn:disabled{opacity:.35;cursor:not-allowed;}
            .hdn-pg-active{background:var(--primary,#3b82f6)!important;color:#fff!important;border-color:var(--primary,#3b82f6)!important;font-weight:700;}
            .hdn-pg-ell{padding:0 .25rem;color:var(--text-light);}
        </style>`;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    function _getNhaTroName(maPhong) {
        const phong  = (window.lookups?.phong  || []).find(p => Number(p.maPhong)  === Number(maPhong));
        if (!phong) return '';
        const nhaTro = (window.lookups?.nhatro || []).find(n => Number(n.maNhaTro) === Number(phong.maNhaTro));
        return nhaTro?.tenNhaTro || '';
    }

    function _esc(v) {
        if (v == null) return '';
        return String(v).replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    // ── Public API ─────────────────────────────────────────────────────────────
    Object.assign(HDN, {
        init, onKeyword, onTrangThai, onKyHoaDon, onNhaTro, onPhong,
        onNgayLapTu, onNgayLapDen, onTongTienTu, onTongTienDen,
        onSort, onPageSize, onPage, reset, laiMoi, toggleAdvanced,
        refresh: (data) => {
            HDN.rawData = data || [];
            HDN.page = 1;
            _mergeBienLaiChoXacNhan().then(() => {
                _buildToolbar();
                _applyAndRender();
            });
        }
    });

})();
