// ==========================================
// MODULE: Người Thuê – Search / Filter / Sort / Paging
// File: js/modules/nguoi-thue-search.js
// ==========================================
(function () {
    'use strict';

    // ── State ──────────────────────────────────────────────────────────────────
    const NT = {
        keyword: '',
        filterTrangThai: '',
        filterNhaTro: '',
        filterPhong: '',
        filterGioiTinh: '',
        sortKey: '',
        sortDir: 'asc',
        page: 1,
        pageSize: 10,
        rawData: [],   // merged display rows từ mergeNguoiThueDisplayRows()
        filtered: [],  // sau khi search/filter/sort
        advancedOpen: false,
    };

    window._NguoiThueSearch = NT;

    // ── Khởi động (gọi sau khi data đã load) ──────────────────────────────────
    function init(mergedRows) {
        NT.rawData = mergedRows || [];
        NT.page = 1;
        _buildToolbar();
        _applyAndRender();
    }

    // ── Build thanh toolbar ────────────────────────────────────────────────────
    function _buildToolbar() {
        const slot = document.getElementById('ntToolbarSlot');
        if (!slot) return;

        const nhaTroList = window.normalizeArrayResponse(window.lookups?.nhatro || []);
        const phongList  = window.normalizeArrayResponse(window.lookups?.phong  || []);

        const nhaTroOptions = nhaTroList.map(n =>
            `<option value="${n.maNhaTro}">${n.tenNhaTro || 'Nhà trọ #' + n.maNhaTro}</option>`
        ).join('');

        // Danh sách phòng được lọc theo nhà trọ đang chọn (hoặc tất cả)
        const visiblePhong = NT.filterNhaTro
            ? phongList.filter(p => String(p.maNhaTro) === String(NT.filterNhaTro))
            : phongList;

        const phongOptions = visiblePhong.map(p =>
            `<option value="${p.maPhong}" ${String(p.maPhong) === String(NT.filterPhong) ? 'selected' : ''}>${p.tenPhong || 'Phòng #' + p.maPhong}</option>`
        ).join('');
        const canWrite = window.CURRENT_ROLE === 'Admin' || window.CURRENT_ROLE === 'ChuTro';
        const addBtn = canWrite ? `
            <button class="module-btn module-btn-primary" onclick="openModal()">
                <i class="fas fa-plus"></i> Thêm mới
            </button>` : '';

        slot.innerHTML = `
        <div class="nt-toolbar" style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:1rem;">

            <!-- Tìm kiếm nhanh -->
            <div style="position:relative;flex:1;min-width:220px;max-width:380px;">
                <i class="fas fa-search" style="position:absolute;left:.85rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
                <input type="text" id="ntKeyword" class="form-control" style="padding-left:2.5rem;"
                    placeholder="Tìm tên, CCCD, SĐT, email, phòng, nhà trọ..."
                    value="${_esc(NT.keyword)}"
                    oninput="window._NguoiThueSearch.onKeyword(this.value)">
            </div>

            <!-- Filter Trạng thái -->
            <div style="min-width:160px;">
                <select id="ntFilterTrangThai" class="form-control" onchange="window._NguoiThueSearch.onTrangThai(this.value)">
                    <option value="">Tất cả trạng thái</option>
                    <option value="DangThue"       ${NT.filterTrangThai==='DangThue'       ?'selected':''}>Hoạt động</option>
                    <option value="KhongHoatDong"  ${NT.filterTrangThai==='KhongHoatDong'  ?'selected':''}>Không hoạt động</option>
                    <option value="DaXoa"          ${NT.filterTrangThai==='DaXoa'          ?'selected':''}>Đã xóa</option>
                </select>
            </div>

            <div style="display:flex;gap:.4rem;margin-left:auto;flex-wrap:wrap;">
                ${addBtn}
                <button class="module-btn module-btn-muted ${NT.advancedOpen ? 'active' : ''}" onclick="window._NguoiThueSearch.toggleAdvanced()">
                    <i class="fas fa-sliders-h"></i> Nâng cao
                    <i class="fas fa-chevron-${NT.advancedOpen ? 'up' : 'down'}" style="font-size:.7rem;"></i>
                </button>
                <button class="module-btn module-btn-muted" onclick="window._NguoiThueSearch.reset()">
                    <i class="fas fa-filter-circle-xmark"></i> Xóa lọc
                </button>
            </div>

        </div>`;

        slot.innerHTML += `
        <div class="generic-advanced-panel ${NT.advancedOpen ? 'open' : ''}">
            ${nhaTroList.length > 1 ? `
            <div style="min-width:170px;">
                <select id="ntFilterNhaTro" class="form-control" onchange="window._NguoiThueSearch.onNhaTro(this.value)">
                    <option value="">Tất cả nhà trọ</option>
                    ${nhaTroOptions}
                </select>
            </div>` : ''}
            ${phongList.length > 0 ? `
            <div style="min-width:150px;">
                <select id="ntFilterPhong" class="form-control" onchange="window._NguoiThueSearch.onPhong(this.value)">
                    <option value="">Tất cả phòng</option>
                    ${phongOptions}
                </select>
            </div>` : ''}
            <div style="min-width:130px;">
                <select id="ntFilterGioiTinh" class="form-control" onchange="window._NguoiThueSearch.onGioiTinh(this.value)">
                    <option value="">Tất cả giới tính</option>
                    <option value="Nam"  ${NT.filterGioiTinh==='Nam'  ?'selected':''}>Nam</option>
                    <option value="Nữ"   ${NT.filterGioiTinh==='Nữ'   ?'selected':''}>Nữ</option>
                    <option value="Khác" ${NT.filterGioiTinh==='Khác' ?'selected':''}>Khác</option>
                </select>
            </div>
        </div>`;

        // Sync nhà trọ selected after render
        const ntEl = document.getElementById('ntFilterNhaTro');
        if (ntEl && NT.filterNhaTro) ntEl.value = NT.filterNhaTro;
    }

    // ── Handlers ────────────────────────────────────────────────────────────────
    // ✅ Mới – thêm debounce 300ms
    let _kwTimer;
    function onKeyword(v) {
        NT.keyword = v.trim();
        NT.page = 1;
        clearTimeout(_kwTimer);
        _kwTimer = setTimeout(_applyAndRender, 300);
    }
    function onTrangThai(v) { NT.filterTrangThai = v; NT.page = 1; _applyAndRender(); }
    function onNhaTro(v) {
        NT.filterNhaTro = v;
        NT.filterPhong  = '';          // reset phòng khi đổi nhà trọ
        NT.page = 1;
        _rebuildPhongOptions();
        _applyAndRender();
    }
    function onPhong(v) { NT.filterPhong = v; NT.page = 1; _applyAndRender(); }
    function onGioiTinh(v) { NT.filterGioiTinh = v; NT.page = 1; _applyAndRender(); }
    function onSort(key) {
        if (NT.sortKey === key) {
            NT.sortDir = NT.sortDir === 'asc' ? 'desc' : 'asc';
        } else {
            NT.sortKey = key;
            NT.sortDir = 'asc';
        }
        NT.page = 1;
        _applyAndRender();
    }
    function onPageSize(v) { NT.pageSize = parseInt(v) || 10; NT.page = 1; _applyAndRender(); }
    function onPage(p) { NT.page = p; _applyAndRender(); }
    function toggleAdvanced() { NT.advancedOpen = !NT.advancedOpen; _buildToolbar(); }
    function reset() {
        NT.keyword = ''; NT.filterTrangThai = ''; NT.filterNhaTro = '';
        NT.filterPhong = ''; NT.filterGioiTinh = '';
        NT.sortKey = ''; NT.sortDir = 'asc'; NT.page = 1; NT.pageSize = 10; NT.advancedOpen = false;
        _buildToolbar();
        _applyAndRender();
    }

    // ── Rebuild phòng dropdown khi đổi nhà trọ ───────────────────────────────
    function _rebuildPhongOptions() {
        const sel = document.getElementById('ntFilterPhong');
        if (!sel) return;
        const phongList = window.normalizeArrayResponse(window.lookups?.phong || []);
        const visible = NT.filterNhaTro
            ? phongList.filter(p => String(p.maNhaTro) === String(NT.filterNhaTro))
            : phongList;
        sel.innerHTML = `<option value="">Tất cả phòng</option>` +
            visible.map(p => `<option value="${p.maPhong}">${p.tenPhong || 'Phòng #' + p.maPhong}</option>`).join('');
    }

    // ── Filter + Sort logic ────────────────────────────────────────────────────
    function _filterAndSort(data) {
        let result = data.slice();
        const kw = NT.keyword.toLowerCase();

        if (kw) {
            result = result.filter(row => {
                const phongText = (row.danhSachPhongText || '').replace(/<[^>]*>/g, '').toLowerCase();
                return (
                    (row.hoTen  || '').toLowerCase().includes(kw) ||
                    (row.cccd   || '').toLowerCase().includes(kw) ||
                    (row.sdt    || '').toLowerCase().includes(kw) ||
                    (row.email  || '').toLowerCase().includes(kw) ||
                    phongText.includes(kw)
                );
            });
        }

        if (NT.filterTrangThai) {
            result = result.filter(row => {
                // Với grouped row, lấy trangThai của item đầu tiên
                const st = row.trangThai || row._nguoiThueItems?.[0]?.trangThai || 'DangThue';
                return st === NT.filterTrangThai;
            });
        }

        if (NT.filterNhaTro) {
            result = result.filter(row => {
                const rooms = row.danhSachPhong || [];
                if (rooms.length) {
                    return rooms.some(r => {
                        const phong = (window.lookups?.phong || []).find(p => Number(p.maPhong) === Number(r.maPhong));
                        return phong && String(phong.maNhaTro) === String(NT.filterNhaTro);
                    });
                }
                const phong = (window.lookups?.phong || []).find(p => Number(p.maPhong) === Number(row.maPhong));
                return phong && String(phong.maNhaTro) === String(NT.filterNhaTro);
            });
        }

        if (NT.filterPhong) {
            result = result.filter(row => {
                const rooms = row.danhSachPhong || [];
                if (rooms.length) return rooms.some(r => String(r.maPhong) === String(NT.filterPhong));
                return String(row.maPhong) === String(NT.filterPhong);
            });
        }

        if (NT.filterGioiTinh) {
            result = result.filter(row => (row.gioiTinh || '') === NT.filterGioiTinh);
        }

        // Sort
        if (NT.sortKey) {
            const dir = NT.sortDir === 'asc' ? 1 : -1;
            result.sort((a, b) => {
                let va = _sortVal(a, NT.sortKey);
                let vb = _sortVal(b, NT.sortKey);
                if (va < vb) return -1 * dir;
                if (va > vb) return 1 * dir;
                return 0;
            });
        }

        return result;
    }

    function _sortVal(row, key) {
        switch (key) {
            case 'hoTen':    return (row.hoTen  || '').toLowerCase();
            case 'sdt':      return (row.sdt    || '').toLowerCase();
            case 'email':    return (row.email  || '').toLowerCase();
            case 'trangThai': return (row.trangThai || row._nguoiThueItems?.[0]?.trangThai || '').toLowerCase();
            case 'tenPhong': {
                const txt = (row.danhSachPhongText || '').replace(/<[^>]*>/g, '');
                return txt.toLowerCase();
            }
            default: return '';
        }
    }

    // ── Apply & Render ────────────────────────────────────────────────────────
    function _applyAndRender() {
        NT.filtered = _filterAndSort(NT.rawData);
        _renderTable();
        _renderPaging();
    }

    // ── Render bảng ───────────────────────────────────────────────────────────
    const SORT_COLS = [
        { key: 'hoTen',    label: 'Họ tên' },
        { key: 'sdt',      label: 'Số điện thoại' },
        { key: 'email',    label: 'Email' },
        { key: 'trangThai',label: 'Trạng thái' },
        { key: 'tenPhong', label: 'Tên phòng' },
    ];

    const TRANG_THAI_MAP = {
        'DangThue':       { cls: 'badge-success', label: 'Hoạt động' },
        'KhongHoatDong':  { cls: 'badge-warning', label: 'Không hoạt động' },
        'DaXoa':          { cls: 'badge-red',      label: 'Đã xóa' },
    };

    const ROW_CLASS_MAP = {
        'DangThue': 'module-row-ok',
        'KhongHoatDong': 'module-row-warn',
        'DaXoa': 'module-row-cancel',
    };

    function _thHtml(key, label) {
        const active = NT.sortKey === key;
        const icon = active ? (NT.sortDir === 'asc' ? 'fa-sort-up' : 'fa-sort-down') : 'fa-sort';
        const style = active ? 'color:var(--primary);' : 'color:var(--text-light);';
        return `<th style="cursor:pointer;white-space:nowrap;user-select:none;"
                    onclick="window._NguoiThueSearch.onSort('${key}')">
                    ${label} <i class="fas ${icon}" style="${style}font-size:.75rem;"></i>
                </th>`;
    }

    function _renderTable() {
        const container = document.getElementById('ntTableSlot');
        if (!container) return;

        const canWrite = (window.CURRENT_ROLE === 'Admin' || window.CURRENT_ROLE === 'ChuTro');
        const total = NT.filtered.length;
        const start = (NT.page - 1) * NT.pageSize;
        const pageData = NT.filtered.slice(start, start + NT.pageSize);

        const thead = `<thead><tr>
            ${_thHtml('hoTen',    'Họ tên')}
            <th>CCCD</th>
            ${_thHtml('sdt',      'Số điện thoại')}
            ${_thHtml('email',    'Email')}
            <th>Phòng đang thuê</th>
            ${_thHtml('trangThai','Trạng thái')}
            <th>Giới tính</th>
            <th>Ảnh CCCD</th>
            <th>Thao tác</th>
        </tr></thead>`;

        let tbody;
        if (!pageData.length) {
            const colSpan = 9;
            tbody = `<tbody><tr><td colspan="${colSpan}" style="text-align:center;padding:2.5rem;color:var(--text-light);">
                <i class="fas fa-user-slash" style="font-size:2rem;display:block;margin-bottom:.5rem;opacity:.4;"></i>
                Không tìm thấy người thuê phù hợp.
            </td></tr></tbody>`;
        } else {
            tbody = '<tbody>' + pageData.map(item => {
                // Trạng thái
                const st = item.trangThai || item._nguoiThueItems?.[0]?.trangThai || 'DangThue';
                const { cls: stCls, label: stLabel } = TRANG_THAI_MAP[st] || { cls: 'badge-secondary', label: st };
                const rowCls = ROW_CLASS_MAP[st] || '';

                // Ảnh CCCD
                const hasFront = !!item.anhCccdMatTruoc;
                const hasBack  = !!item.anhCccdMatSau;
                let anhHtml;
                if (!hasFront && !hasBack) {
                    anhHtml = `<span style="color:var(--text-light);">Chưa có</span>`;
                } else {
                    anhHtml = `<span class="badge badge-success">${hasFront ? 'Mặt trước' : ''}${hasFront && hasBack ? ' / ' : ''}${hasBack ? 'Mặt sau' : ''}</span>`;
                }

                // Actions
                let actionHtml = `<button class="btn-action btn-edit" onclick="viewNguoiThueDetail(${item.maNguoiThue})"><i class="fas fa-eye"></i> Chi tiết</button>`;
                if (window.CURRENT_ROLE === 'Admin') {
                    actionHtml += `
                        <button class="btn-action btn-edit" onclick="editItem('nguoithue',${item.maNguoiThue})"><i class="fas fa-edit"></i> Sửa</button>
                        <button class="btn-action btn-delete" onclick="deleteNguoiThueDisplayGroup(${item.maNguoiThue})"><i class="fas fa-trash"></i> Xóa</button>`;
                } else if (window.CURRENT_ROLE === 'ChuTro') {
                    actionHtml += `
                        <button class="btn-action btn-delete" onclick="deleteNguoiThueDisplayGroup(${item.maNguoiThue})"><i class="fas fa-trash"></i> Xóa</button>`;
                }

                return `<tr class="${rowCls}">
                    <td>${_esc(item.hoTen)}</td>
                    <td>${_esc(item.cccd) || '---'}</td>
                    <td>${_esc(item.sdt) || '---'}</td>
                    <td>${_esc(item.email) || '---'}</td>
                    <td>${item.danhSachPhongText || '---'}</td>
                    <td><span class="badge ${stCls}">${stLabel}</span></td>
                    <td>${_esc(item.gioiTinh) || '---'}</td>
                    <td>${anhHtml}</td>
                    <td style="white-space:nowrap;">${_actionMenu(actionHtml)}</td>
                </tr>`;
            }).join('') + '</tbody>';
        }

        container.innerHTML = `
            <div class="module-summary-grid">
                <div class="module-summary-card">
                    <div class="module-summary-icon"><i class="fas fa-users"></i></div>
                    <div><div class="module-summary-label">Tổng người thuê</div><div class="module-summary-value">${total}</div></div>
                </div>
                <div class="module-summary-card">
                    <div class="module-summary-icon dark"><i class="fas fa-id-card"></i></div>
                    <div><div class="module-summary-label">Đang hiển thị</div><div class="module-summary-value dark">${pageData.length}</div></div>
                </div>
            </div>
            <div class="table-container">
                <table>${thead}${tbody}</table>
            </div>`;
    }

    // ── Render phân trang ─────────────────────────────────────────────────────
    function _renderPaging() {
        const slot = document.getElementById('ntPagingSlot');
        if (!slot) return;

        const total    = NT.filtered.length;
        const totalPg  = Math.max(1, Math.ceil(total / NT.pageSize));
        const cur      = Math.min(NT.page, totalPg);

        // Page size selector
        const sizeOpts = [10, 20, 50].map(s =>
            `<option value="${s}" ${NT.pageSize === s ? 'selected' : ''}>${s}</option>`
        ).join('');

        // Page buttons (window of 5)
        let pageButtons = '';
        if (totalPg > 1) {
            const lo = Math.max(1, cur - 2);
            const hi = Math.min(totalPg, lo + 4);

            pageButtons += `<button class="nt-pg-btn" onclick="window._NguoiThueSearch.onPage(1)" title="Trang đầu" ${cur===1?'disabled':''}><i class="fas fa-angle-double-left"></i></button>`;
            pageButtons += `<button class="nt-pg-btn" onclick="window._NguoiThueSearch.onPage(${cur - 1})" title="Trang trước" ${cur===1?'disabled':''}><i class="fas fa-chevron-left"></i></button>`;
            if (lo > 1)       pageButtons += `<button class="nt-pg-btn" onclick="window._NguoiThueSearch.onPage(1)">1</button><span class="nt-pg-ellipsis">…</span>`;

            for (let i = lo; i <= hi; i++) {
                pageButtons += `<button class="nt-pg-btn${i === cur ? ' nt-pg-active' : ''}" onclick="window._NguoiThueSearch.onPage(${i})">${i}</button>`;
            }

            if (hi < totalPg) pageButtons += `<span class="nt-pg-ellipsis">…</span><button class="nt-pg-btn" onclick="window._NguoiThueSearch.onPage(${totalPg})">${totalPg}</button>`;
            pageButtons += `<button class="nt-pg-btn" onclick="window._NguoiThueSearch.onPage(${cur + 1})" title="Trang sau" ${cur===totalPg?'disabled':''}><i class="fas fa-chevron-right"></i></button>`;
            pageButtons += `<button class="nt-pg-btn" onclick="window._NguoiThueSearch.onPage(${totalPg})" title="Trang cuối" ${cur===totalPg?'disabled':''}><i class="fas fa-angle-double-right"></i></button>`;
        }

        slot.innerHTML = `
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;margin-top:.75rem;">
            <div style="display:flex;align-items:center;gap:.5rem;font-size:.875rem;color:var(--text-light);">
                Hiển thị
                <select class="form-control" style="width:auto;padding:.25rem .5rem;font-size:.875rem;"
                    onchange="window._NguoiThueSearch.onPageSize(this.value)">${sizeOpts}</select>
                dòng / trang
            </div>
            <div class="nt-pg-wrap" style="display:flex;gap:.25rem;align-items:center;">
                ${pageButtons}
            </div>
        </div>
        <style>
            .nt-pg-btn{padding:.3rem .65rem;border:1px solid var(--border-color,#e2e8f0);border-radius:6px;background:#fff;cursor:pointer;font-size:.85rem;line-height:1.4;color:var(--text-primary,#1e293b);transition:all .15s;}
            .nt-pg-btn:hover{background:var(--primary-light,#eff6ff);border-color:var(--primary,#3b82f6);}
            .nt-pg-btn:disabled{opacity:.45;cursor:not-allowed;background:#f8fafc;}
            .nt-pg-active{background:var(--primary,#3b82f6)!important;color:#fff!important;border-color:var(--primary,#3b82f6)!important;}
            .nt-pg-ellipsis{padding:0 .25rem;color:var(--text-light);}
        </style>`;
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    function _esc(v) {
        if (v == null) return '';
        return String(v)
            .replace(/&/g,'&amp;').replace(/</g,'&lt;')
            .replace(/>/g,'&gt;').replace(/"/g,'&quot;');
    }

    function _actionMenu(actionHtml) {
        if (!actionHtml || !String(actionHtml).trim()) return '---';
        return `<details class="module-action-menu"><summary title="Thao tác"><i class="fas fa-ellipsis-vertical"></i></summary><div class="module-action-list">${actionHtml}</div></details>`;
    }

    // ── Public API ────────────────────────────────────────────────────────────
    NT.init       = init;
    NT.onKeyword  = onKeyword;
    NT.onTrangThai= onTrangThai;
    NT.onNhaTro   = onNhaTro;
    NT.onPhong    = onPhong;
    NT.onGioiTinh = onGioiTinh;
    NT.onSort     = onSort;
    NT.onPageSize = onPageSize;
    NT.onPage     = onPage;
    NT.toggleAdvanced = toggleAdvanced;
    NT.reset      = reset;
    NT.refresh    = function (newMergedRows) { NT.rawData = newMergedRows || []; NT.page = 1; _applyAndRender(); };

})();
