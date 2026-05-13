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
    };

    window._HoaDonSearch = HDN;

    // ── Init ─────────────────────────────────────────────────────────────────
    function init(data) {
        HDN.rawData = data || [];
        HDN.page = 1;
        // Nếu là NguoiDung: lấy thêm danh sách biên lai đang chờ để đánh dấu
        _mergeBienLaiChoXacNhan().then(() => {
            _buildToolbar();
            _applyAndRender();
        });
    }

    // ── Merge trạng thái biên lai chờ xác nhận vào rawData ──────────────────
    // Chỉ gọi khi role = NguoiDung; các role khác bỏ qua
    async function _mergeBienLaiChoXacNhan() {
        if (window.CURRENT_ROLE !== 'NguoiDung') return;
        // Backend đã trả DaCoBienLaiChoXacNhan trong HoaDonDto — dùng trực tiếp,
        // không cần gọi thêm /api/ThanhToan (tránh race condition & sai conLai)
        HDN.rawData = HDN.rawData.map(r => ({
            ...r,
            _daCoBienLaiChoXacNhan: r.daCoBienLaiChoXacNhan === true
        }));
    }


    // (Backend đã trả về daThanhToan, conLai, trangThaiThanhToan, trangThai trong HoaDonDto)
    function _trangThaiHienThi(row) {
        // Quá hạn: ChuaThanhToan và ngayHetHan < hôm nay (nếu BE trả ngayHetHan)
        // Hiện tại dùng trangThaiThanhToan từ BE, bổ sung QuaHan nếu có thể tính
        if (row.trangThai === 'Huy' || row.trangThaiThanhToan === 'Đã hủy') return 'Huy';
        if (row.trangThaiThanhToan === 'Đã thanh toán')    return 'DaThanhToan';
        if (row.trangThaiThanhToan === 'Thanh toán một phần') return 'ThanhToanMotPhan';
        // Quá hạn: chưa thanh toán và kỳ HĐ < tháng hiện tại
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

        // Lấy danh sách kỳ HĐ duy nhất từ data
        const kyList = [...new Set(HDN.rawData.map(r => r.kyHoaDon).filter(Boolean))].sort().reverse();
        const kyOpts = kyList.map(k =>
            `<option value="${k}" ${HDN.filterKyHoaDon===k?'selected':''}>${k}</option>`
        ).join('');

        slot.innerHTML = `
        <!-- Hàng 1: Search + Filter chính -->
        <div class="hdn-toolbar" style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:.75rem;">

            <!-- Tìm kiếm nhanh -->
            <div style="position:relative;flex:1;min-width:220px;max-width:400px;">
                <i class="fas fa-search" style="position:absolute;left:.85rem;top:50%;transform:translateY(-50%);color:var(--text-light);pointer-events:none;"></i>
                <input type="text" id="hdnKeyword" class="form-control" style="padding-left:2.5rem;"
                    placeholder="Mã HĐ, tên phòng, người thuê, kỳ, trạng thái..."
                    value="${_esc(HDN.keyword)}"
                    oninput="window._HoaDonSearch.onKeyword(this.value)">
            </div>

            <!-- Filter Trạng thái -->
            <div style="min-width:185px;">
                <select class="form-control" onchange="window._HoaDonSearch.onTrangThai(this.value)">
                    <option value="">Tất cả trạng thái</option>
                    <option value="ChuaThanhToan"    ${HDN.filterTrangThai==='ChuaThanhToan'    ?'selected':''}>Chưa thanh toán</option>
                    <option value="DaThanhToan"      ${HDN.filterTrangThai==='DaThanhToan'      ?'selected':''}>Đã thanh toán</option>
                    <option value="ThanhToanMotPhan" ${HDN.filterTrangThai==='ThanhToanMotPhan' ?'selected':''}>Thanh toán một phần</option>
                    <option value="QuaHan"           ${HDN.filterTrangThai==='QuaHan'           ?'selected':''}>Quá hạn</option>
                    <option value="Huy"              ${HDN.filterTrangThai==='Huy'              ?'selected':''}>Đã hủy</option>
                </select>
            </div>

            <!-- Filter Kỳ hóa đơn -->
            <div style="min-width:150px;">
                <select class="form-control" onchange="window._HoaDonSearch.onKyHoaDon(this.value)">
                    <option value="">Tất cả kỳ</option>
                    ${kyOpts}
                </select>
            </div>

            ${nhaTroList.length > 1 ? `
            <!-- Filter Nhà trọ -->
            <div style="min-width:160px;">
                <select id="hdnFilterNhaTro" class="form-control" onchange="window._HoaDonSearch.onNhaTro(this.value)">
                    <option value="">Tất cả nhà trọ</option>
                    ${nhaTroOpts}
                </select>
            </div>` : ''}

            <!-- Filter Phòng -->
            <div style="min-width:150px;">
                <select id="hdnFilterPhong" class="form-control" onchange="window._HoaDonSearch.onPhong(this.value)">
                    <option value="">Tất cả phòng</option>
                    ${phongOpts}
                </select>
            </div>

        </div>

        <!-- Hàng 2: Khoảng ngày lập + khoảng tổng tiền + nút -->
        <div style="display:flex;flex-wrap:wrap;gap:.65rem;align-items:flex-end;margin-bottom:.75rem;">

            <!-- Khoảng ngày lập -->
            <div style="display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;">
                <span style="font-size:.85rem;color:var(--text-light);white-space:nowrap;">Ngày lập:</span>
                <input type="date" class="form-control" style="width:150px;" value="${HDN.filterNgayLapTu}"
                    onchange="window._HoaDonSearch.onNgayLapTu(this.value)">
                <span style="color:var(--text-light);">–</span>
                <input type="date" class="form-control" style="width:150px;" value="${HDN.filterNgayLapDen}"
                    onchange="window._HoaDonSearch.onNgayLapDen(this.value)">
            </div>

            <!-- Khoảng tổng tiền -->
            <div style="display:flex;gap:.4rem;align-items:center;flex-wrap:wrap;">
                <span style="font-size:.85rem;color:var(--text-light);white-space:nowrap;">Tổng tiền:</span>
                <input type="number" class="form-control" style="width:120px;" placeholder="Từ" min="0"
                    value="${HDN.filterTongTienTu}"
                    onchange="window._HoaDonSearch.onTongTienTu(this.value)">
                <span style="color:var(--text-light);">–</span>
                <input type="number" class="form-control" style="width:120px;" placeholder="Đến" min="0"
                    value="${HDN.filterTongTienDen}"
                    onchange="window._HoaDonSearch.onTongTienDen(this.value)">
            </div>

            <!-- Nút Xóa bộ lọc + Làm mới -->
            <div style="display:flex;gap:.4rem;margin-left:auto;">
                <button class="btn btn-secondary" style="white-space:nowrap;" onclick="window._HoaDonSearch.reset()">
                    <i class="fas fa-filter-circle-xmark"></i> Xóa bộ lọc
                </button>
                <button class="btn btn-secondary" style="white-space:nowrap;" onclick="window._HoaDonSearch.laiMoi()">
                    <i class="fas fa-rotate-right"></i> Làm mới
                </button>
            </div>
        </div>`;
    }

    // ── Handlers ──────────────────────────────────────────────────────────────
    function onKeyword(v)       { HDN.keyword = v.trim(); HDN.page = 1; _applyAndRender(); }
    function onTrangThai(v)     { HDN.filterTrangThai = v; HDN.page = 1; _applyAndRender(); }
    function onKyHoaDon(v)      { HDN.filterKyHoaDon = v; HDN.page = 1; _applyAndRender(); }
    function onNhaTro(v)        { HDN.filterNhaTro = v; HDN.filterPhong = ''; HDN.page = 1; _rebuildPhong(); _applyAndRender(); }
    function onPhong(v)         { HDN.filterPhong = v; HDN.page = 1; _applyAndRender(); }
    function onNgayLapTu(v)     { HDN.filterNgayLapTu = v;  HDN.page = 1; _applyAndRender(); }
    function onNgayLapDen(v)    { HDN.filterNgayLapDen = v; HDN.page = 1; _applyAndRender(); }
    function onTongTienTu(v)    { HDN.filterTongTienTu = v;  HDN.page = 1; _applyAndRender(); }
    function onTongTienDen(v)   { HDN.filterTongTienDen = v; HDN.page = 1; _applyAndRender(); }

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
            sortKey:'ngayLap', sortDir:'desc', page:1, pageSize:10
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

        // Gắn trangThaiHienThi vào mỗi row (cache để tránh tính lại nhiều lần)
        result = result.map(r => ({ ...r, _tt: _trangThaiHienThi(r) }));

        const kw = HDN.keyword.toLowerCase();
        if (kw) {
            result = result.filter(r => {
                const maHD   = String(r.maHoaDon || '');
                const phong  = (r.tenPhong || '').toLowerCase();
                const nguoi  = (r.tenNguoiThue || '').toLowerCase();
                const ky     = (r.kyHoaDon || '').toLowerCase();
                const tt     = _ttLabel(r._tt).toLowerCase();
                return maHD.includes(kw) || phong.includes(kw) || nguoi.includes(kw) || ky.includes(kw) || tt.includes(kw);
            });
        }

        if (HDN.filterTrangThai) {
            result = result.filter(r => r._tt === HDN.filterTrangThai);
        }

        if (HDN.filterKyHoaDon) {
            result = result.filter(r => r.kyHoaDon === HDN.filterKyHoaDon);
        }

        if (HDN.filterNhaTro) {
            result = result.filter(r => {
                const phong = (window.lookups?.phong || []).find(p => Number(p.maPhong) === Number(r.maPhong));
                return phong && String(phong.maNhaTro) === String(HDN.filterNhaTro);
            });
        }

        if (HDN.filterPhong) {
            result = result.filter(r => String(r.maPhong) === String(HDN.filterPhong));
        }

        const _toTs = s => s ? new Date(s).getTime() : null;
        if (HDN.filterNgayLapTu)  result = result.filter(r => _toTs(r.ngayLap)  >= _toTs(HDN.filterNgayLapTu + 'T00:00:00'));
        if (HDN.filterNgayLapDen) result = result.filter(r => _toTs(r.ngayLap)  <= _toTs(HDN.filterNgayLapDen + 'T23:59:59'));

        if (HDN.filterTongTienTu)  result = result.filter(r => Number(r.tongTien) >= Number(HDN.filterTongTienTu));
        if (HDN.filterTongTienDen) result = result.filter(r => Number(r.tongTien) <= Number(HDN.filterTongTienDen));

        // Sort
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

    // ── Status helpers ─────────────────────────────────────────────────────────
    const STATUS_CFG = {
        ChuaThanhToan:    { cls: 'hdn-badge-warn',    icon: 'fa-clock',         label: 'Chưa thanh toán' },
        DaThanhToan:      { cls: 'hdn-badge-ok',      icon: 'fa-check-circle',  label: 'Đã thanh toán' },
        ThanhToanMotPhan: { cls: 'hdn-badge-partial', icon: 'fa-adjust',        label: 'Thanh toán 1 phần' },
        QuaHan:           { cls: 'hdn-badge-overdue', icon: 'fa-exclamation-triangle', label: 'Quá hạn' },
        Huy:              { cls: 'hdn-badge-cancel',  icon: 'fa-ban',           label: 'Đã hủy' },
    };

    function _ttLabel(tt) { return (STATUS_CFG[tt] || {}).label || tt; }

    function _statusBadge(row) {
        const tt = row._tt || 'ChuaThanhToan';
        const cfg = STATUS_CFG[tt] || { cls: 'hdn-badge-cancel', icon: 'fa-circle', label: tt };
        const isOverdue = tt === 'QuaHan';
        const extra = isOverdue ? ' hdn-badge-pulse' : '';
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
        const tongTienTatCa   = HDN.filtered.reduce((s, r) => s + (Number(r.tongTien) || 0), 0);
        const tongDaTT        = HDN.filtered.reduce((s, r) => s + (Number(r.daThanhToan) || 0), 0);
        const tongConLai      = HDN.filtered.reduce((s, r) => s + (Number(r.conLai) || 0), 0);

        const fmtCur  = v => new Intl.NumberFormat('vi-VN').format(v) + 'đ';
        const fmtDate = v => v ? new Date(v).toLocaleDateString('vi-VN') : '---';

        const thead = `<thead><tr>
            <th style="white-space:nowrap;">Mã HĐ</th>
            ${_th('tenPhong',     'Phòng')}
            ${_th('tenNguoiThue', 'Người thuê')}
            ${_th('kyHoaDon',     'Kỳ HĐ')}
            ${_th('ngayLap',      'Ngày lập')}
            ${_th('tongTien',     'Tổng tiền')}
            <th style="white-space:nowrap;">Đã TT / Còn lại</th>
            ${_th('trangThai',    'Trạng thái')}
            <th>Thao tác</th>
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
                const tt = item._tt || 'ChuaThanhToan';
                const isOverdue  = tt === 'QuaHan';
                const isUnpaid   = tt === 'ChuaThanhToan';
                const rowStyle   = isOverdue ? 'background:rgba(239,68,68,.05);'
                                 : isUnpaid  ? 'background:rgba(251,191,36,.04);'
                                 : '';

                // Phòng + nhà trọ
                const nhaTroName = _getNhaTroName(item.maPhong);
                const phongCell  = nhaTroMulti && nhaTroName
                    ? `<div style="font-weight:500;">${_esc(item.tenPhong)}</div><div style="font-size:.8rem;color:var(--text-light);">${_esc(nhaTroName)}</div>`
                    : _esc(item.tenPhong || `Phòng #${item.maPhong}`);

                // Tiền còn lại màu đỏ nếu còn nợ
                const conLai   = Number(item.conLai) || 0;
                const conLaiHtml = conLai > 0
                    ? `<div style="color:#dc2626;font-weight:600;">${fmtCur(conLai)}</div>`
                    : `<div style="color:#16a34a;">0đ</div>`;

                // Nút In (tất cả role)
                let actionHtml = `<button class="btn-action" style="background:#6366f1;" onclick="HoaDonPrint.openModal(${item.maHoaDon})"><i class="fas fa-print"></i> In</button>`;

                // Nút Gửi biên lai — chỉ NguoiDung, chỉ khi còn nợ, chưa có biên lai đang chờ
                if (canSend && conLai > 0 && tt !== 'Huy' && tt !== 'DaThanhToan') {
                    const daCoBienLai = item._daCoBienLaiChoXacNhan === true;
                    if (daCoBienLai) {
                        actionHtml += `<span class="btn-action" style="background:#f59e0b;cursor:default;opacity:.85;"
                            title="Đang chờ chủ trọ xác nhận biên lai bạn đã gửi">
                            <i class="fas fa-clock"></i> Chờ duyệt</span>`;
                    } else {
                        // Truyền item dưới dạng JSON an toàn để moModalGuiBienLai dùng
                        const itemJson = JSON.stringify({
                            maHoaDon: item.maHoaDon,
                            kyHoaDon: item.kyHoaDon,
                            tongTien: item.tongTien,
                            conLai: item.conLai,
                            tenPhong: item.tenPhong,
                        }).replace(/"/g, '&quot;');
                        actionHtml += `<button class="btn-action hdn-btn-guibienlai"
                            onclick="moModalGuiBienLai(JSON.parse(this.dataset.item))"
                            data-item="${itemJson}"
                            title="Gửi biên lai thanh toán cho chủ trọ xác nhận">
                            <i class="fas fa-paper-plane"></i> Gửi biên lai</button>`;
                    }
                }

                // Nút Sửa / Xóa — chỉ ChuTro / Admin
                if (canWrite) {
                    actionHtml += `<button class="btn-action btn-edit" onclick="editItem('hoadon',${item.maHoaDon})"><i class="fas fa-edit"></i> Sửa</button>`;
                    if (tt !== 'Huy') {
                        actionHtml += `<button class="btn-action btn-delete" onclick="deleteItem('hoadon',${item.maHoaDon})"><i class="fas fa-trash"></i> Xóa</button>`;
                    }
                }

                return `<tr style="${rowStyle}">
                    <td style="font-weight:600;color:var(--primary);">#${item.maHoaDon}</td>
                    <td>${phongCell}</td>
                    <td>${_esc(item.tenNguoiThue || '---')}</td>
                    <td style="white-space:nowrap;font-weight:500;">${_esc(item.kyHoaDon || '---')}</td>
                    <td style="white-space:nowrap;">${fmtDate(item.ngayLap)}</td>
                    <td style="white-space:nowrap;font-weight:500;">${fmtCur(item.tongTien || 0)}</td>
                    <td>
                        <div style="font-size:.8rem;color:var(--text-light);">Đã TT: ${fmtCur(item.daThanhToan || 0)}</div>
                        ${conLaiHtml}
                    </td>
                    <td>${_statusBadge(item)}</td>
                    <td style="white-space:nowrap;">${actionHtml}</td>
                </tr>`;
            }).join('') + '</tbody>';
        }

        // Banner cảnh báo quá hạn
        const soQuaHan = HDN.filtered.filter(r => r._tt === 'QuaHan').length;
        const quaHanBanner = soQuaHan > 0 && !HDN.filterTrangThai ? `
            <div style="background:#fee2e2;border:1px solid #fca5a5;border-radius:8px;padding:.6rem 1rem;margin-bottom:.75rem;font-size:.875rem;display:flex;gap:.5rem;align-items:center;">
                <i class="fas fa-exclamation-triangle" style="color:#dc2626;"></i>
                <span>Có <strong>${soQuaHan}</strong> hóa đơn quá hạn chưa thanh toán.</span>
                <button class="btn btn-secondary" style="padding:.2rem .65rem;font-size:.8rem;margin-left:auto;"
                    onclick="window._HoaDonSearch.onTrangThai('QuaHan')">Xem</button>
            </div>` : '';

        // Tóm tắt tổng tiền kết quả lọc
        const tongTienMeta = total > 0 ? `
            <div class="hdn-summary" style="display:flex;flex-wrap:wrap;gap:.75rem;padding:.5rem .75rem;background:var(--bg-secondary,#f8fafc);border-radius:8px;margin-bottom:.75rem;font-size:.85rem;">
                <span>Tổng HĐ: <strong>${total}</strong></span>
                <span style="color:var(--text-light);">|</span>
                <span>Tổng tiền: <strong>${fmtCur(tongTienTatCa)}</strong></span>
                <span style="color:var(--text-light);">|</span>
                <span style="color:#16a34a;">Đã TT: <strong>${fmtCur(tongDaTT)}</strong></span>
                <span style="color:var(--text-light);">|</span>
                <span style="color:#dc2626;">Còn lại: <strong>${fmtCur(tongConLai)}</strong></span>
                ${total > HDN.pageSize ? `<span style="color:var(--text-light);">(hiển thị ${start+1}–${Math.min(start+HDN.pageSize, total)})</span>` : ''}
            </div>` : `<div style="font-size:.85rem;color:var(--text-light);margin-bottom:.5rem;">Không có kết quả.</div>`;

        slot.innerHTML = `
            ${quaHanBanner}
            ${tongTienMeta}
            <div class="table-container">
                <table>${thead}${tbody}</table>
            </div>

            <style>
                /* Badge trạng thái */
                .hdn-badge{display:inline-flex;align-items:center;gap:.3rem;padding:.2rem .6rem;border-radius:20px;font-size:.78rem;font-weight:600;white-space:nowrap;}
                .hdn-badge-ok      {background:#dcfce7;color:#15803d;}
                .hdn-badge-warn    {background:#fef9c3;color:#a16207;}
                .hdn-badge-partial {background:#dbeafe;color:#1d4ed8;}
                .hdn-badge-overdue {background:#fee2e2;color:#b91c1c;}
                .hdn-badge-cancel  {background:#f1f5f9;color:#64748b;}
                /* Pulse cho quá hạn */
                @keyframes hdn-pulse {0%,100%{opacity:1}50%{opacity:.65}}
                .hdn-badge-pulse   {animation:hdn-pulse 1.8s ease-in-out infinite;}
                /* Nút Gửi biên lai */
                .hdn-btn-guibienlai{background:#0ea5e9 !important;transition:background .15s,transform .1s;}
                .hdn-btn-guibienlai:hover{background:#0284c7 !important;transform:translateY(-1px);}
            </style>`;
    }

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
            if (cur > 1)       btns += `<button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(${cur-1})"><i class="fas fa-chevron-left"></i></button>`;
            if (lo > 1)        btns += `<button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(1)">1</button><span class="hdn-pg-ell">…</span>`;
            for (let i=lo;i<=hi;i++) btns += `<button class="hdn-pg-btn${i===cur?' hdn-pg-active':''}" onclick="window._HoaDonSearch.onPage(${i})">${i}</button>`;
            if (hi < totalPg)  btns += `<span class="hdn-pg-ell">…</span><button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(${totalPg})">${totalPg}</button>`;
            if (cur < totalPg) btns += `<button class="hdn-pg-btn" onclick="window._HoaDonSearch.onPage(${cur+1})"><i class="fas fa-chevron-right"></i></button>`;
        }

        slot.innerHTML = `
        <div style="display:flex;align-items:center;justify-content:space-between;flex-wrap:wrap;gap:.5rem;margin-top:.75rem;">
            <div style="display:flex;align-items:center;gap:.5rem;font-size:.875rem;color:var(--text-light);">
                Hiển thị
                <select class="form-control" style="width:auto;padding:.25rem .5rem;font-size:.875rem;"
                    onchange="window._HoaDonSearch.onPageSize(this.value)">${sizeOpts}</select>
                dòng / trang
            </div>
            <div style="display:flex;gap:.25rem;align-items:center;">${btns}</div>
        </div>
        <style>
            .hdn-pg-btn{padding:.3rem .65rem;border:1px solid var(--border-color,#e2e8f0);border-radius:6px;background:#fff;cursor:pointer;font-size:.85rem;color:var(--text-primary,#1e293b);transition:all .15s;}
            .hdn-pg-btn:hover{background:var(--primary-light,#eff6ff);border-color:var(--primary,#3b82f6);}
            .hdn-pg-active{background:var(--primary,#3b82f6)!important;color:#fff!important;border-color:var(--primary,#3b82f6)!important;}
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
        onSort, onPageSize, onPage, reset, laiMoi,
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
