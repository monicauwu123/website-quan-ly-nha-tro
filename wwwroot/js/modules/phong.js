// Module cấu hình: phong
// =====================================================================
// PHIÊN BẢN NÂNG CẤP: Search / Filter / Sort / Paging
// =====================================================================
window.AppModules = window.AppModules || {};

const _pLookups = () => window.lookups || { nhatro: [], loaiphong: [], trangthai: [] };

const renderPhongImage = item => {
    const url = item?.hinhAnh || parseJsonArraySafe(item?.danhSachHinhAnh)[0];
    return url
        ? `<img src="${url}" style="width:72px;height:48px;object-fit:cover;border-radius:6px;border:1px solid #e5e7eb;" onerror="this.style.display='none'">`
        : '---';
};

window.AppModules.phong = {
    title: 'Phòng Trọ',
    endpoint: '/api/Phong',
    pk: 'maPhong',

    // ── Cột hiển thị trong bảng ──────────────────────────────────────
    headers: [
        { label: 'Ảnh',        key: '_image',      render: (v, item) => renderPhongImage(item), sortable: false },
        { label: 'Tên phòng',  key: 'tenPhong',    sortable: true },
        { label: 'Nhà trọ',    key: 'maNhaTro',    sortable: true,
          render: v => _pLookups().nhatro?.find(n => n.maNhaTro === v)?.tenNhaTro || `#${v}` },
        { label: 'Loại phòng', key: 'maLoaiPhong', sortable: true,
          render: v => _pLookups().loaiphong?.find(l => l.maLoaiPhong === v)?.tenLoaiPhong || `#${v}` },
        { label: 'Giá thuê',   key: 'giaPhong',    sortable: true,
          render: v => window.AppFormat.currency(v) },
        { label: 'Diện tích',  key: 'dienTich',    sortable: true,
          render: v => v ? `${v} m²` : '---' },
        { label: 'Sức chứa',   key: 'sucChua',     sortable: true },
        { label: 'Tiện nghi',  key: '_tienNghi',   sortable: false,
          render: (v, item) => renderServiceBadges(item, 'TienNghi') },
        { label: 'Trạng thái', key: 'maTrangThai', sortable: true,
          render: (v, item) => {
              const t = _pLookups().trangthai?.find(t => t.maTrangThai === v);
              const name = (t?.tenTrangThai || '').toLowerCase();
              const cls = name.includes('trống') || name.includes('trong')
                  ? 'badge-success'
                  : name.includes('thuê') || name.includes('thue')
                  ? 'badge-danger'
                  : name.includes('sửa') || name.includes('sua')
                  ? 'badge-warning'
                  : 'badge-secondary';
              return `<span class="badge ${cls}">${t?.tenTrangThai || v}</span>`;
          }
        }
    ],

    // ── Form fields ──────────────────────────────────────────────────
    fields: [
        { id: 'tenPhong',        label: 'Tên phòng',                  type: 'text',             required: true },
        { id: 'maNhaTro',        label: 'Nhà trọ',                    type: 'lookup',            lookup: 'nhatro',    valField: 'maNhaTro',    txtField: 'tenNhaTro',    required: true },
        { id: 'maLoaiPhong',     label: 'Loại phòng',                 type: 'lookup',            lookup: 'loaiphong', valField: 'maLoaiPhong', txtField: 'tenLoaiPhong', required: true },
        { id: 'maTrangThai',     label: 'Trạng thái',                 type: 'lookup',            lookup: 'trangthai', valField: 'maTrangThai', txtField: 'tenTrangThai', required: true },
        { id: 'giaPhong',        label: 'Giá thuê (đ)',               type: 'number',            required: true },
        { id: 'dienTich',        label: 'Diện tích (m²)',             type: 'number' },
        { id: 'sucChua',         label: 'Sức chứa (người)',           type: 'number',            required: true },
        { id: 'fileUpload',      label: 'Ảnh phòng',                  type: 'fileMultiple',      uploadTarget: 'phong' },
        { id: 'danhSachHinhAnh', label: 'URL ảnh đã lưu',             type: 'hiddenJsonArray' },
        { id: 'dichVuGanPhong',  label: 'Tiện ích / tiện nghi gắn với phòng', type: 'serviceCheckboxes' },
        { id: 'soNguoiHienTai',  label: 'Số người hiện tại',          type: 'number',            defaultVal: 0, hidden: true },
        { id: 'diaChiPhong',     label: 'Địa chỉ phòng',              type: 'text' },
        { id: 'moTa',            label: 'Mô tả',                      type: 'textarea' }
    ]
};

// =====================================================================
// PHONG TABLE MODULE – Search / Filter / Sort / Paging (Admin & ChuTro)
// Gọi window.PhongTable.init() để khởi động màn hình bảng danh sách
// =====================================================================
window.PhongTable = (function () {

    // ── State ─────────────────────────────────────────────────────────
    let _allData    = [];
    let _filtered   = [];
    let _sortKey    = '';
    let _sortDir    = 'asc';   // 'asc' | 'desc'
    let _page       = 1;
    let _pageSize   = 10;

    // ── Helpers ───────────────────────────────────────────────────────
    const lk = () => window.lookups || {};
    const esc = v => window.AppFormat.escapeHtml(String(v ?? ''));

    function nhaTroName(v) {
        return lk().nhatro?.find(n => n.maNhaTro === v)?.tenNhaTro || `#${v}`;
    }
    function loaiPhongName(v) {
        return lk().loaiphong?.find(l => l.maLoaiPhong === v)?.tenLoaiPhong || `#${v}`;
    }
    function trangThaiName(v) {
        return lk().trangthai?.find(t => t.maTrangThai === v)?.tenTrangThai || String(v ?? '');
    }
    function trangThaiClass(v) {
        const name = trangThaiName(v).toLowerCase();
        if (name.includes('trống') || name.includes('trong')) return 'badge-success';
        if (name.includes('thuê') || name.includes('thue'))   return 'badge-danger';
        if (name.includes('sửa') || name.includes('sua'))     return 'badge-warning';
        return 'badge-secondary';
    }
    function trangThaiRowClass(v) {
        const name = trangThaiName(v)
            .normalize('NFD')
            .replace(/[\u0300-\u036f]/g, '')
            .toLowerCase();
        if (name.includes('trong')) return 'module-row-ok';
        if (name.includes('thue')) return 'module-row-danger';
        if (name.includes('sua') || name.includes('bao tri')) return 'module-row-warn';
        if (name.includes('ngung') || name.includes('huy')) return 'module-row-cancel';
        return '';
    }

    // ── Lấy giá trị các control filter ───────────────────────────────
    function getFilters() {
        return {
            q:          (document.getElementById('ptSearch')?.value   || '').toLowerCase().trim(),
            nhaTro:     document.getElementById('ptNhaTro')?.value     || '',
            loaiPhong:  document.getElementById('ptLoaiPhong')?.value  || '',
            trangThai:  document.getElementById('ptTrangThai')?.value  || '',
            giaMin:     parseFloat(document.getElementById('ptGiaMin')?.value) || 0,
            giaMax:     parseFloat(document.getElementById('ptGiaMax')?.value) || 0,
            sucChua:    parseInt(document.getElementById('ptSucChua')?.value)  || 0,
            dienTich:   parseFloat(document.getElementById('ptDienTich')?.value) || 0,
        };
    }

    // ── Lọc dữ liệu ──────────────────────────────────────────────────
    function applyFilter() {
        const f = getFilters();
        _filtered = _allData.filter(r => {
            // Search text (không phân biệt hoa thường)
            if (f.q) {
                const nhaTen  = nhaTroName(r.maNhaTro).toLowerCase();
                const loaiTen = loaiPhongName(r.maLoaiPhong).toLowerCase();
                const ttTen   = trangThaiName(r.maTrangThai).toLowerCase();
                const matched = (r.tenPhong   || '').toLowerCase().includes(f.q)
                             || nhaTen.includes(f.q)
                             || loaiTen.includes(f.q)
                             || ttTen.includes(f.q)
                             || (r.moTa || '').toLowerCase().includes(f.q);
                if (!matched) return false;
            }
            // Filter nhà trọ
            if (f.nhaTro   && String(r.maNhaTro)    !== f.nhaTro)   return false;
            // Filter loại phòng
            if (f.loaiPhong && String(r.maLoaiPhong) !== f.loaiPhong) return false;
            // Filter trạng thái
            if (f.trangThai && String(r.maTrangThai) !== f.trangThai) return false;
            // Khoảng giá
            if (f.giaMin > 0 && (r.giaPhong || 0) < f.giaMin) return false;
            if (f.giaMax > 0 && (r.giaPhong || 0) > f.giaMax) return false;
            // Sức chứa tối thiểu
            if (f.sucChua > 0 && (r.sucChua || 0) < f.sucChua) return false;
            // Diện tích tối thiểu
            if (f.dienTich > 0 && (r.dienTich || 0) < f.dienTich) return false;
            return true;
        });
        _page = 1;
    }

    // ── Sắp xếp ──────────────────────────────────────────────────────
    function applySort() {
        if (!_sortKey) return;
        _filtered.sort((a, b) => {
            let va, vb;
            if (_sortKey === 'maNhaTro')    { va = nhaTroName(a.maNhaTro);    vb = nhaTroName(b.maNhaTro); }
            else if (_sortKey === 'maLoaiPhong') { va = loaiPhongName(a.maLoaiPhong); vb = loaiPhongName(b.maLoaiPhong); }
            else if (_sortKey === 'maTrangThai') { va = trangThaiName(a.maTrangThai); vb = trangThaiName(b.maTrangThai); }
            else { va = a[_sortKey]; vb = b[_sortKey]; }

            // So sánh số
            if (typeof va === 'number' && typeof vb === 'number') {
                return _sortDir === 'asc' ? va - vb : vb - va;
            }
            // So sánh chuỗi
            va = String(va ?? '').toLowerCase();
            vb = String(vb ?? '').toLowerCase();
            if (va < vb) return _sortDir === 'asc' ? -1 : 1;
            if (va > vb) return _sortDir === 'asc' ?  1 : -1;
            return 0;
        });
    }

    // ── Phân trang ────────────────────────────────────────────────────
    function getPageData() {
        const start = (_page - 1) * _pageSize;
        return _filtered.slice(start, start + _pageSize);
    }
    function totalPages() { return Math.max(1, Math.ceil(_filtered.length / _pageSize)); }

    // ── Render bảng ──────────────────────────────────────────────────
    function renderTable() {
        const tbody = document.getElementById('ptTableBody');
        if (!tbody) return;

        const pageData = getPageData();
        const _role = (typeof CURRENT_ROLE !== 'undefined' ? CURRENT_ROLE : window.CURRENT_ROLE) || '';
        const canWrite = (_role === 'Admin' || _role === 'ChuTro');

        if (!pageData.length) {
            tbody.innerHTML = `<tr><td colspan="10" style="text-align:center;padding:2.5rem;color:var(--text-light);">
                <i class="fas fa-inbox" style="font-size:2rem;margin-bottom:.5rem;display:block;opacity:.4;"></i>
                Không tìm thấy phòng phù hợp.
            </td></tr>`;
            return;
        }

        tbody.innerHTML = pageData.map(r => {
            const imgUrl  = r.hinhAnh || (parseJsonArraySafe(r.danhSachHinhAnh))[0];
            const imgHtml = imgUrl
                ? `<img src="${esc(imgUrl)}" style="width:64px;height:44px;object-fit:cover;border-radius:5px;border:1px solid #e5e7eb;" onerror="this.style.display='none'">`
                : '<span style="color:var(--text-light);font-size:.8rem;">---</span>';

            const ttName = trangThaiName(r.maTrangThai);
            const ttCls  = trangThaiClass(r.maTrangThai);
            const rowCls = trangThaiRowClass(r.maTrangThai);

            const actionHtml = canWrite
                ? `<button class="btn-action" style="background:#0891b2;" onclick="openRoomGallery(${r.maPhong})"><i class="fas fa-images"></i> Xem ảnh</button>
                   <button class="btn-action btn-edit" onclick="editItem('phong',${r.maPhong})"><i class="fas fa-edit"></i> Sửa</button>
                   <button class="btn-action btn-delete" onclick="deleteItem('phong',${r.maPhong})"><i class="fas fa-trash"></i></button>`
                : `<button class="btn-action btn-edit" onclick="openYeuCauThueModal(null,${r.maPhong})"><i class="fas fa-paper-plane"></i> Thuê</button>`;

            return `<tr class="${rowCls}">
                <td style="text-align:center;">${imgHtml}</td>
                <td><strong>${esc(r.tenPhong)}</strong>${r.diaChiPhong ? `<br><small style="color:var(--text-light);">${esc(r.diaChiPhong)}</small>` : ''}</td>
                <td>${esc(nhaTroName(r.maNhaTro))}</td>
                <td>${esc(loaiPhongName(r.maLoaiPhong))}</td>
                <td style="white-space:nowrap;"><strong>${window.AppFormat.currency(r.giaPhong)}</strong></td>
                <td style="text-align:center;">${r.dienTich ? r.dienTich + ' m²' : '---'}</td>
                <td style="text-align:center;">${r.sucChua ?? '---'}</td>
                <td>${renderServiceBadges(r, 'TienNghi')}</td>
                <td><span class="badge ${ttCls}">${esc(ttName)}</span></td>
                <td style="white-space:nowrap;">${actionMenu(actionHtml)}</td>
            </tr>`;
        }).join('');
    }

    // ── Render phân trang ─────────────────────────────────────────────
    function renderPaging() {
        const total   = _filtered.length;
        const tp      = totalPages();
        const start   = total === 0 ? 0 : (_page - 1) * _pageSize + 1;
        const end     = Math.min(_page * _pageSize, total);

        // Tổng kết quả
        const info = document.getElementById('ptInfo');
        if (info) info.textContent = `Hiển thị ${start}–${end} trong tổng số ${total} kết quả`;

        const pager = document.getElementById('ptPager');
        if (!pager) return;

        const btn = (label, page, disabled, title = '') => {
            const dis = disabled ? 'disabled' : '';
            return `<button class="pt-page-btn ${disabled ? 'disabled' : ''}" ${dis} onclick="PhongTable.goPage(${page})" title="${title}">${label}</button>`;
        };

        // Tính range trang hiển thị (tối đa 5 trang)
        let pages = [];
        const range = 2;
        for (let i = Math.max(1, _page - range); i <= Math.min(tp, _page + range); i++) pages.push(i);

        const pageBtns = pages.map(p =>
            `<button class="pt-page-btn ${p === _page ? 'active' : ''}" onclick="PhongTable.goPage(${p})">${p}</button>`
        ).join('');

        pager.innerHTML = `
            ${btn('<i class="fas fa-angle-double-left"></i>', 1,          _page === 1,  'Trang đầu')}
            ${btn('<i class="fas fa-angle-left"></i>',        _page - 1,  _page === 1,  'Trang trước')}
            ${pageBtns}
            ${btn('<i class="fas fa-angle-right"></i>',       _page + 1,  _page >= tp,  'Trang sau')}
            ${btn('<i class="fas fa-angle-double-right"></i>',tp,          _page >= tp,  'Trang cuối')}`;
    }

    function actionMenu(actionHtml) {
        if (!actionHtml || !String(actionHtml).trim()) return '---';
        return `<details class="module-action-menu"><summary title="Thao tác"><i class="fas fa-ellipsis-vertical"></i></summary><div class="module-action-list">${actionHtml}</div></details>`;
    }

    // ── Cập nhật icon sort trên header ────────────────────────────────
    function updateSortIcons() {
        document.querySelectorAll('#ptTable th[data-sort]').forEach(th => {
            const icon = th.querySelector('.sort-icon');
            if (!icon) return;
            if (th.dataset.sort === _sortKey) {
                icon.className = `sort-icon fas fa-sort-${_sortDir === 'asc' ? 'up' : 'down'} active`;
            } else {
                icon.className = 'sort-icon fas fa-sort';
            }
        });
    }

    // ── Refresh (filter + sort + render) ─────────────────────────────
    function refresh() {
        applyFilter();
        applySort();
        renderTable();
        renderPaging();
        updateSortIcons();
    }

    // ── HTML toolbar + bảng ──────────────────────────────────────────
    function buildHtml() {
        const nhaTroOpts = (lk().nhatro || []).map(n =>
            `<option value="${n.maNhaTro}">${esc(n.tenNhaTro)}</option>`).join('');
        const loaiOpts = (lk().loaiphong || []).map(l =>
            `<option value="${l.maLoaiPhong}">${esc(l.tenLoaiPhong)}</option>`).join('');
        const ttOpts = (lk().trangthai || []).map(t =>
            `<option value="${t.maTrangThai}">${esc(t.tenTrangThai)}</option>`).join('');

        const colHeaders = [
            { label: 'Ảnh',        key: '' },
            { label: 'Tên phòng',  key: 'tenPhong' },
            { label: 'Nhà trọ',    key: 'maNhaTro' },
            { label: 'Loại phòng', key: 'maLoaiPhong' },
            { label: 'Giá thuê',   key: 'giaPhong' },
            { label: 'Diện tích',  key: 'dienTich' },
            { label: 'Sức chứa',   key: 'sucChua' },
            { label: 'Tiện nghi',  key: '' },
            { label: 'Trạng thái', key: 'maTrangThai' },
            { label: 'Thao tác',   key: '' },
        ];

        const theadHtml = colHeaders.map(h =>
            h.key
                ? `<th data-sort="${h.key}" style="cursor:pointer;user-select:none;white-space:nowrap;" onclick="PhongTable.sortBy('${h.key}')">
                       ${h.label} <i class="sort-icon fas fa-sort" style="font-size:.7rem;opacity:.5;margin-left:.25rem;"></i>
                   </th>`
                : `<th>${h.label}</th>`
        ).join('');

        const canWrite = window.CURRENT_ROLE === 'Admin' || window.CURRENT_ROLE === 'ChuTro';
        const addButton = canWrite ? `
        <button class="module-btn module-btn-primary" onclick="openModal()" title="Thêm phòng">
            <i class="fas fa-plus"></i> Thêm mới
        </button>` : '';

        return `
<!-- ── Bộ lọc ── -->
<div class="data-card" style="margin-bottom:1rem;padding:1rem 1.25rem;">
    <!-- Hàng 1: Search + nút -->
    <div style="display:flex;gap:.75rem;flex-wrap:wrap;align-items:center;margin-bottom:.75rem;">
        <div style="flex:1;min-width:220px;position:relative;">
            <i class="fas fa-search" style="position:absolute;left:.9rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
            <input type="text" id="ptSearch" class="form-control" style="padding-left:2.4rem;" placeholder="Tìm tên phòng, nhà trọ, loại phòng, trạng thái, mô tả..."
                oninput="PhongTable.onFilterChange()">
        </div>
        ${addButton}
        <button class="module-btn module-btn-muted" onclick="PhongTable.reload()" title="Làm mới dữ liệu từ server">
            <i class="fas fa-sync-alt"></i> Làm mới
        </button>
        <button class="module-btn module-btn-muted" onclick="PhongTable.clearFilters()" title="Xóa tất cả bộ lọc">
            <i class="fas fa-times-circle"></i> Xóa bộ lọc
        </button>
    </div>
    <!-- Hàng 2: Dropdown filters -->
    <div style="display:flex;gap:.75rem;flex-wrap:wrap;align-items:flex-end;">
        <div>
            <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Nhà trọ</label>
            <select id="ptNhaTro" class="form-control" style="min-width:160px;" onchange="PhongTable.onFilterChange()">
                <option value="">Tất cả nhà trọ</option>
                ${nhaTroOpts}
            </select>
        </div>
        <div>
            <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Loại phòng</label>
            <select id="ptLoaiPhong" class="form-control" style="min-width:140px;" onchange="PhongTable.onFilterChange()">
                <option value="">Tất cả loại</option>
                ${loaiOpts}
            </select>
        </div>
        <div>
            <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Trạng thái</label>
            <select id="ptTrangThai" class="form-control" style="min-width:140px;" onchange="PhongTable.onFilterChange()">
                <option value="">Tất cả trạng thái</option>
                ${ttOpts}
            </select>
        </div>
        <div>
            <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Giá từ (đ)</label>
            <input type="number" id="ptGiaMin" class="form-control" style="width:130px;" placeholder="VD: 1000000" min="0" oninput="PhongTable.onFilterChange()">
        </div>
        <div>
            <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Giá đến (đ)</label>
            <input type="number" id="ptGiaMax" class="form-control" style="width:130px;" placeholder="VD: 5000000" min="0" oninput="PhongTable.onFilterChange()">
        </div>
        <div>
            <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Sức chứa ≥</label>
            <input type="number" id="ptSucChua" class="form-control" style="width:100px;" placeholder="VD: 2" min="0" oninput="PhongTable.onFilterChange()">
        </div>
        <div>
            <label style="font-size:.8rem;font-weight:600;color:var(--text-light);display:block;margin-bottom:.2rem;">Diện tích ≥ (m²)</label>
            <input type="number" id="ptDienTich" class="form-control" style="width:120px;" placeholder="VD: 20" min="0" oninput="PhongTable.onFilterChange()">
        </div>
    </div>
</div>

<!-- ── Loading ── -->
<div id="ptLoading" style="display:none;text-align:center;padding:2rem;">
    <i class="fas fa-spinner fa-spin" style="font-size:1.5rem;color:var(--primary);"></i>
    <div style="margin-top:.5rem;color:var(--text-light);">Đang tải dữ liệu...</div>
</div>

<!-- ── Bảng ── -->
<div class="data-card" id="ptTableWrapper">
    <div class="table-container">
        <table id="ptTable" style="min-width:900px;">
            <thead><tr>${theadHtml}</tr></thead>
            <tbody id="ptTableBody">
                <tr><td colspan="10" style="text-align:center;padding:2rem;">
                    <i class="fas fa-spinner fa-spin"></i> Đang tải...
                </td></tr>
            </tbody>
        </table>
    </div>

    <!-- ── Footer: info + pageSize + pager ── -->
    <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.75rem;padding:.75rem 1rem .5rem;border-top:1px solid var(--border);">
        <span id="ptInfo" style="font-size:.875rem;color:var(--text-light);"></span>
        <div style="display:flex;align-items:center;gap:.5rem;">
            <span style="font-size:.85rem;color:var(--text-light);">Dòng/trang:</span>
            <select id="ptPageSize" class="form-control" style="width:75px;padding:.35rem .5rem;" onchange="PhongTable.onPageSizeChange(this.value)">
                <option value="10">10</option>
                <option value="20">20</option>
                <option value="50">50</option>
            </select>
        </div>
        <div id="ptPager" style="display:flex;gap:.3rem;flex-wrap:wrap;"></div>
    </div>
</div>`;
    }

    // ── Public API ────────────────────────────────────────────────────
    async function init(container) {
        if (!container) container = document.getElementById('genericSection');
        if (!container) return;

        // Hiển thị loading ban đầu
        container.innerHTML = `<div style="text-align:center;padding:3rem;"><i class="fas fa-spinner fa-spin" style="font-size:1.5rem;color:var(--primary);"></i><div style="margin-top:.5rem;color:var(--text-light);">Đang tải danh sách phòng...</div></div>`;

        try {
            const raw = await apiFetch('/api/Phong');
            _allData = normalizeArrayResponse(raw);
        } catch (e) {
            container.innerHTML = `<div class="data-card" style="padding:2rem;text-align:center;color:var(--error);">
                <i class="fas fa-exclamation-triangle" style="font-size:2rem;margin-bottom:.5rem;display:block;"></i>
                Lỗi tải dữ liệu phòng: ${e.message}
            </div>`;
            return;
        }

        // Khởi tạo state
        _sortKey  = '';
        _sortDir  = 'asc';
        _page     = 1;
        _pageSize = 10;

        container.innerHTML = buildHtml();
        refresh();
    }

    function onFilterChange() {
        _page = 1;
        applyFilter();
        applySort();
        renderTable();
        renderPaging();
    }

    function sortBy(key) {
        if (_sortKey === key) {
            _sortDir = _sortDir === 'asc' ? 'desc' : 'asc';
        } else {
            _sortKey = key;
            _sortDir = 'asc';
        }
        applySort();
        renderTable();
        renderPaging();
        updateSortIcons();
    }

    function goPage(p) {
        const tp = totalPages();
        _page = Math.max(1, Math.min(p, tp));
        renderTable();
        renderPaging();
    }

    function onPageSizeChange(val) {
        _pageSize = parseInt(val) || 10;
        _page = 1;
        renderTable();
        renderPaging();
    }

    function clearFilters() {
        ['ptSearch','ptNhaTro','ptLoaiPhong','ptTrangThai','ptGiaMin','ptGiaMax','ptSucChua','ptDienTich']
            .forEach(id => {
                const el = document.getElementById(id);
                if (el) el.value = '';
            });
        onFilterChange();
    }

    async function reload() {
        const loading = document.getElementById('ptLoading');
        const wrapper = document.getElementById('ptTableWrapper');
        if (loading) loading.style.display = 'block';
        if (wrapper) wrapper.style.opacity = '.5';

        try {
            const raw = await apiFetch('/api/Phong');
            _allData = normalizeArrayResponse(raw);
            window.showToast && window.showToast('Đã làm mới danh sách phòng', 'success');
        } catch (e) {
            window.showToast && window.showToast('Lỗi tải dữ liệu: ' + e.message, 'error');
        } finally {
            if (loading) loading.style.display = 'none';
            if (wrapper) wrapper.style.opacity = '1';
        }

        onFilterChange();
    }

    return { init, onFilterChange, sortBy, goPage, onPageSizeChange, clearFilters, reload };
})();
