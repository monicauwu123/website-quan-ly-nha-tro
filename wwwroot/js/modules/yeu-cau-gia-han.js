// Bổ sung luồng yêu cầu gia hạn hợp đồng mà không phải sửa trực tiếp dashboard.js dài.
// File này được load sau dashboard.js để vá các hành động của bảng Yêu cầu thuê/gia hạn.
(function () {
    function getRowByRequestId(id) {
        const list = Array.isArray(currentData) ? currentData : [];
        return list.find(x => String(x.maYeuCauGiaHan || x.maYeuCau) === String(id));
    }

    function toInputDate(value) {
        if (!value) return '';
        const d = new Date(value);
        if (Number.isNaN(d.getTime())) return '';
        return d.toISOString().substring(0, 10);
    }

    function addMonthsMinusOneDay(dateValue, months) {
        if (!dateValue) return '';
        const d = new Date(`${dateValue}T00:00:00`);
        if (Number.isNaN(d.getTime())) return '';
        d.setDate(d.getDate() + 1);
        d.setMonth(d.getMonth() + Math.max(1, Number(months || 1)));
        d.setDate(d.getDate() - 1);
        return d.toISOString().substring(0, 10);
    }

    function getContractLabel(hd) {
        const phong = hd?.phong?.tenPhong || `Phòng #${hd?.maPhong || ''}`;
        const end = hd?.ngayKetThuc ? window.AppFormat.date(hd.ngayKetThuc) : 'Không xác định';
        return `${phong} - hết hạn hiện tại: ${end}`;
    }

    window.openYeuCauGiaHanModal = async function (maHopDong) {
        if (CURRENT_ROLE !== 'NguoiDung') {
            showToast('Chỉ người dùng/khách thuê mới được gửi yêu cầu gia hạn', 'error');
            return;
        }

        resetModalFooter();

        let hopDong = (Array.isArray(currentData) ? currentData : []).find(x => Number(x.maHopDong) === Number(maHopDong));
        try {
            hopDong = await apiFetch(`/api/HopDong/${maHopDong}`) || hopDong;
        } catch (e) {
            if (!hopDong) {
                showToast(e.message || 'Không tải được thông tin hợp đồng', 'error');
                return;
            }
        }

        if (!hopDong) {
            showToast('Không tìm thấy hợp đồng cần gia hạn', 'error');
            return;
        }

        const ngayCu = toInputDate(hopDong.ngayKetThuc || hopDong.ngayBatDau);
        const ngayMoiMacDinh = addMonthsMinusOneDay(ngayCu, 1);

        document.getElementById('modalTitle').textContent = 'Gửi yêu cầu gia hạn hợp đồng';
        document.getElementById('modalFields').innerHTML = `
            <div style="grid-column:1/-1;background:#f8fafc;border:1px solid #e2e8f0;border-radius:.85rem;padding:1rem;margin-bottom:.25rem;">
                <div style="font-weight:800;margin-bottom:.35rem;">${escapeHtmlDashboard(getContractLabel(hopDong))}</div>
                <div style="color:var(--text-light);font-size:.9rem;">Bạn có thể đề xuất ngày kết thúc mới. Chủ trọ sẽ duyệt hoặc từ chối yêu cầu này.</div>
            </div>
            <div class="form-group">
                <label>Ngày kết thúc hiện tại</label>
                <input type="date" class="form-control" value="${ngayCu}" disabled>
            </div>
            <div class="form-group">
                <label>Số tháng muốn gia hạn</label>
                <input type="number" id="f_soThangGiaHan" class="form-control" min="1" max="60" value="1">
            </div>
            <div class="form-group">
                <label>Gia hạn đến ngày <span style="color:var(--error)">*</span></label>
                <input type="date" id="f_ngayKetThucMoiDeXuat" class="form-control" value="${ngayMoiMacDinh}" required>
            </div>
            <div class="form-group" style="grid-column:1/-1;">
                <label>Ghi chú gửi chủ trọ</label>
                <textarea id="f_ghiChuGiaHan" class="form-control" maxlength="500" rows="4" placeholder="Ví dụ: Em muốn gia hạn thêm 1 tháng, giữ nguyên điều khoản cũ..."></textarea>
            </div>`;

        document.getElementById('f_soThangGiaHan')?.addEventListener('input', () => {
            const months = document.getElementById('f_soThangGiaHan').value;
            const endInput = document.getElementById('f_ngayKetThucMoiDeXuat');
            if (endInput) endInput.value = addMonthsMinusOneDay(ngayCu, months);
        });

        document.getElementById('modalForm').onsubmit = async (e) => {
            e.preventDefault();
            const payload = {
                maHopDong: Number(maHopDong),
                ngayKetThucMoiDeXuat: document.getElementById('f_ngayKetThucMoiDeXuat').value,
                ghiChuNguoiDung: document.getElementById('f_ghiChuGiaHan').value.trim()
            };

            if (!payload.ngayKetThucMoiDeXuat) {
                showToast('Vui lòng chọn ngày kết thúc mới', 'error');
                return;
            }

            try {
                await apiFetch('/api/YeuCauGiaHan', 'POST', payload);
                showToast('Gửi yêu cầu gia hạn thành công!');
                closeModal();
                if (currentSection === 'yeucauthue' || currentSection === 'phongdangthue') refreshData();
                if (typeof window.refreshSidebarBadges === 'function') window.refreshSidebarBadges();
            } catch (err) {
                showToast(err.message || 'Lỗi gửi yêu cầu gia hạn', 'error');
            }
        };

        document.getElementById('universalModal').style.display = 'flex';
    };

    window.openYeuCauGiaHanDuyetModal = async function (maYeuCauGiaHan) {
        if (!(CURRENT_ROLE === 'Admin' || CURRENT_ROLE === 'ChuTro')) {
            showToast('Bạn không có quyền duyệt yêu cầu gia hạn', 'error');
            return;
        }

        resetModalFooter();

        let yc = getRowByRequestId(maYeuCauGiaHan);
        try {
            const all = await apiFetch('/api/YeuCauGiaHan');
            const list = normalizeArrayResponse(all);
            yc = list.find(x => Number(x.maYeuCauGiaHan || x.maYeuCau) === Number(maYeuCauGiaHan)) || yc;
        } catch { /* dùng currentData nếu có */ }

        if (!yc) {
            showToast('Không tìm thấy yêu cầu gia hạn', 'error');
            return;
        }

        const ngayCu = toInputDate(yc.ngayKetThucCu || yc.hopDong?.ngayKetThuc);
        const ngayMoi = toInputDate(yc.ngayKetThucMoiChuTro || yc.ngayKetThucMoiDeXuat || yc.ngayKetThucMoi);

        document.getElementById('modalTitle').textContent = 'Duyệt yêu cầu gia hạn hợp đồng';
        document.getElementById('modalFields').innerHTML = `
            <div style="grid-column:1/-1;background:#f8fafc;border:1px solid #e2e8f0;border-radius:.85rem;padding:1rem;margin-bottom:.25rem;">
                <div style="font-weight:800;margin-bottom:.35rem;">${escapeHtmlDashboard(yc.nguoiDung?.hoTen || yc.nguoiDung?.email || 'Người dùng')} xin gia hạn ${escapeHtmlDashboard(yc.phong?.tenPhong || 'phòng')}</div>
                <div style="color:var(--text-light);font-size:.9rem;">Ghi chú người dùng: ${escapeHtmlDashboard(yc.ghiChuNguoiDung || '---')}</div>
            </div>
            <div class="form-group">
                <label>Ngày kết thúc hiện tại</label>
                <input type="date" class="form-control" value="${ngayCu}" disabled>
            </div>
            <div class="form-group">
                <label>Ngày kết thúc mới <span style="color:var(--error)">*</span></label>
                <input type="date" id="f_ngayKetThucGiaHanMoi" class="form-control" value="${ngayMoi}" required>
            </div>
            <div class="form-group">
                <label>Tiền cọc mới nếu có</label>
                <input type="number" id="f_tienCocMoiGiaHan" class="form-control" min="0" placeholder="Để trống nếu giữ nguyên">
            </div>
            <div class="form-group" style="grid-column:1/-1;">
                <label>Điều khoản hợp đồng mới nếu có</label>
                <textarea id="f_noiDungDieuKhoanMoi" class="form-control" maxlength="1000" rows="4" placeholder="Để trống nếu giữ nguyên điều khoản cũ"></textarea>
            </div>
            <div class="form-group" style="grid-column:1/-1;">
                <label>Ghi chú phản hồi</label>
                <textarea id="f_ghiChuChuTroGiaHan" class="form-control" maxlength="500" rows="3"></textarea>
            </div>`;

        document.getElementById('modalForm').onsubmit = async (e) => {
            e.preventDefault();
            const payload = {
                ngayKetThucMoi: document.getElementById('f_ngayKetThucGiaHanMoi').value,
                noiDungDieuKhoanMoi: document.getElementById('f_noiDungDieuKhoanMoi').value.trim(),
                ghiChuChuTro: document.getElementById('f_ghiChuChuTroGiaHan').value.trim()
            };
            const tienCoc = document.getElementById('f_tienCocMoiGiaHan').value;
            if (tienCoc !== '') payload.tienCocMoi = Number(tienCoc);

            try {
                const result = await apiFetch(`/api/YeuCauGiaHan/${maYeuCauGiaHan}/chap-nhan`, 'POST', payload);
                showToast('Đã chấp nhận yêu cầu gia hạn và cập nhật hợp đồng!');
                closeModal();
                await loadLookups();
                refreshData();
                if (typeof window.refreshSidebarBadges === 'function') window.refreshSidebarBadges();

                // Mở modal xuất PDF hợp đồng vừa gia hạn
                const maHopDong = result?.data?.maHopDong || result?.maHopDong || yc?.maHopDong || yc?.hopDong?.maHopDong;
                if (maHopDong && typeof HopDongPrint !== 'undefined') {
                    setTimeout(() => {
                        if (confirm('Hợp đồng đã được gia hạn thành công!\nBạn có muốn xem trước và xuất PDF hợp đồng ngay không?')) {
                            HopDongPrint.openModal(maHopDong);
                        }
                    }, 300);
                }
            } catch (err) {
                showToast(err.message || 'Lỗi duyệt yêu cầu gia hạn', 'error');
            }
        };

        document.getElementById('universalModal').style.display = 'flex';
    };

    window.rejectYeuCauGiaHan = async function (maYeuCauGiaHan) {
        const ghiChu = prompt('Lý do từ chối yêu cầu gia hạn:') || '';
        try {
            await apiFetch(`/api/YeuCauGiaHan/${maYeuCauGiaHan}/tu-choi`, 'POST', { ghiChuChuTro: ghiChu });
            showToast('Đã từ chối yêu cầu gia hạn');
            refreshData();
            if (typeof window.refreshSidebarBadges === 'function') window.refreshSidebarBadges();
        } catch (err) {
            showToast(err.message || 'Lỗi từ chối yêu cầu gia hạn', 'error');
        }
    };

    const originalOpenYeuCauThueDuyetModal = window.openYeuCauThueDuyetModal;
    window.openYeuCauThueDuyetModal = async function (maYeuCau) {
        const row = getRowByRequestId(maYeuCau);
        if (row?.loaiYeuCau === 'GiaHan') {
            return window.openYeuCauGiaHanDuyetModal(row.maYeuCauGiaHan || row.maYeuCau);
        }
        return originalOpenYeuCauThueDuyetModal(maYeuCau);
    };
    try { openYeuCauThueDuyetModal = window.openYeuCauThueDuyetModal; } catch { }

    const originalRejectYeuCauThue = window.rejectYeuCauThue;
    window.rejectYeuCauThue = async function (maYeuCau) {
        const row = getRowByRequestId(maYeuCau);
        if (row?.loaiYeuCau === 'GiaHan') {
            return window.rejectYeuCauGiaHan(row.maYeuCauGiaHan || row.maYeuCau);
        }
        return originalRejectYeuCauThue(maYeuCau);
    };
    try { rejectYeuCauThue = window.rejectYeuCauThue; } catch { }

    const originalDeleteItem = window.deleteItem;
    window.deleteItem = async function (section, id) {
        if (section === 'yeucauthue') {
            const row = getRowByRequestId(id);
            if (row?.loaiYeuCau === 'GiaHan') {
                if (!confirm('Bạn có chắc chắn muốn hủy yêu cầu gia hạn này?')) return;
                try {
                    await apiFetch(`/api/YeuCauGiaHan/${row.maYeuCauGiaHan || row.maYeuCau}`, 'DELETE');
                    showToast('Đã hủy yêu cầu gia hạn');
                    refreshData();
                    if (typeof window.refreshSidebarBadges === 'function') window.refreshSidebarBadges();
                } catch (err) {
                    showToast(err.message || 'Lỗi hủy yêu cầu gia hạn', 'error');
                }
                return;
            }
            if (row?.loaiYeuCau === 'Thue') {
                if (!confirm('Bạn có chắc chắn muốn hủy yêu cầu thuê này?')) return;
                try {
                    await apiFetch(`/api/YeuCauThue/${row.maYeuCau}`, 'DELETE');
                    showToast('Đã hủy yêu cầu thuê');
                    refreshData();
                    if (typeof window.refreshSidebarBadges === 'function') window.refreshSidebarBadges();
                } catch (err) {
                    showToast(err.message || 'Lỗi hủy yêu cầu thuê', 'error');
                }
                return;
            }
        }
        return originalDeleteItem(section, id);
    };
    try { deleteItem = window.deleteItem; } catch { }
})();

