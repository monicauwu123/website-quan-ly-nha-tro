/**
 * Module: bien-lai
 * Chức năng gửi biên lai thanh toán dành cho NguoiDung
 * và xem danh sách biên lai chờ xác nhận dành cho ChuTro/Admin
 */

// ── Gửi biên lai (NguoiDung) ─────────────────────────────────────────────────

/**
 * Hiển thị modal gửi biên lai cho một hóa đơn
 * @param {Object} hoaDon - object hóa đơn (từ API)
 */
function moModalGuiBienLai(hoaDon) {
    // Xóa modal cũ nếu có
    document.getElementById('modalGuiBienLai')?.remove();

    const formatMoney = window.AppFormat?.currency || ((v) => new Intl.NumberFormat('vi-VN').format(v || 0) + 'đ');
    const tongTien = Number(hoaDon.tongTien || 0);
    const daThanhToan = Number(hoaDon.daThanhToan || 0);
    const conLai = Number(hoaDon.conLai ?? Math.max(tongTien - daThanhToan, 0));
    const soTienMacDinh = conLai || tongTien || 0;

    const modal = document.createElement('div');
    modal.id = 'modalGuiBienLai';
    modal.className = 'modal-overlay';
    modal.innerHTML = `
        <div class="modal-card animate-fade-in" style="max-width:640px;">
            <div class="modal-header">
                <h2><i class="fas fa-receipt"></i> Gửi biên lai thanh toán</h2>
                <button type="button" class="close-btn" onclick="document.getElementById('modalGuiBienLai').remove()">&times;</button>
            </div>
            <form id="formGuiBienLai">
                <div class="modal-body" style="grid-template-columns:1fr;gap:1rem;">
                    <div class="bl-invoice-summary" style="background:#f0fdf9;border:1px solid #d1fae5;border-radius:.9rem;padding:1rem;display:grid;gap:.35rem;">
                        <div style="font-size:.85rem;color:var(--text-light);">Hóa đơn cần thanh toán</div>
                        <strong style="color:var(--primary-dark);font-size:1rem;">
                            HĐ#${hoaDon.maHoaDon} ${hoaDon.kyHoaDon ? '– ' + hoaDon.kyHoaDon : ''} – còn lại ${formatMoney(soTienMacDinh)}
                        </strong>
                    </div>

                    <div class="form-group">
                        <label>Kiểu thanh toán <span style="color:#ef4444;">*</span></label>
                        <div style="display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.65rem;">
                            <label class="bl-pay-option active" data-pay-option="ThanhToanHet">
                                <input type="radio" name="blKieuThanhToan" value="ThanhToanHet" checked>
                                <span><strong>Thanh toán hết</strong><small>Gửi đúng số tiền còn lại</small></span>
                            </label>
                            <label class="bl-pay-option" data-pay-option="MotPhan">
                                <input type="radio" name="blKieuThanhToan" value="MotPhan">
                                <span><strong>Thanh toán 1 phần</strong><small>Tự nhập số tiền đã chuyển</small></span>
                            </label>
                        </div>
                    </div>

                    <div style="display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:1rem;">
                        <div class="form-group">
                            <label for="blTongTien">Số tiền đã chuyển <span style="color:#ef4444;">*</span></label>
                            <input id="blTongTien" type="number" class="form-control" placeholder="VD: 2500000"
                                   min="1" max="${soTienMacDinh}" value="${soTienMacDinh}" readonly required />
                            <small id="blAmountHint" style="color:var(--text-light);display:block;margin-top:.35rem;">Đang chọn thanh toán hết nên số tiền bằng phần còn lại.</small>
                        </div>

                        <div class="form-group">
                            <label for="blHinhThuc">Hình thức thanh toán <span style="color:#ef4444;">*</span></label>
                            <select id="blHinhThuc" class="form-control" required>
                                <option value="ChuyenKhoan">Chuyển khoản ngân hàng</option>
                                <option value="TienMat">Tiền mặt</option>
                                <option value="MoMo">Ví MoMo</option>
                                <option value="ZaloPay">ZaloPay</option>
                                <option value="VNPay">VNPay</option>
                                <option value="Khac">Khác</option>
                            </select>
                        </div>
                    </div>

                    <div class="form-group">
                        <label for="blMaGiaoDich">Mã giao dịch <span style="color:var(--text-light);font-weight:400;">(nếu có)</span></label>
                        <input id="blMaGiaoDich" type="text" class="form-control" placeholder="VD: FT2405170001" />
                    </div>

                    <div class="form-group">
                        <label for="blAnhBienLai">Ảnh biên lai <span style="color:var(--text-light);font-weight:400;">(tùy chọn, tối đa 10MB)</span></label>
                        <input id="blAnhBienLai" type="file" class="form-control" accept=".jpg,.jpeg,.png,.gif,.webp" />
                        <div id="blAnhPreview" style="margin-top:.75rem;display:none;">
                            <img id="blAnhPreviewImg" style="max-width:100%;max-height:220px;border-radius:.75rem;border:1px solid #d1fae5;" />
                        </div>
                    </div>

                    <div class="form-group">
                        <label for="blGhiChu">Ghi chú</label>
                        <textarea id="blGhiChu" class="form-control" rows="3" placeholder="Nhập ghi chú nếu có..."></textarea>
                    </div>

                    <div id="blError" class="alert alert-error" style="display:none;"></div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" style="width:auto;" onclick="document.getElementById('modalGuiBienLai').remove()">
                        Hủy
                    </button>
                    <button id="blBtnGui" type="submit" class="btn btn-primary" style="width:auto;">
                        <i class="fas fa-paper-plane"></i> Gửi biên lai
                    </button>
                </div>
            </form>
        </div>
    `;
    document.body.appendChild(modal);

    const grid = modal.querySelector('div[style*="repeat(2"]');
    const applyResponsive = () => {
        if (grid) grid.style.gridTemplateColumns = window.innerWidth < 640 ? '1fr' : 'repeat(2,minmax(0,1fr))';
    };
    applyResponsive();
    window.addEventListener('resize', applyResponsive, { once: true });

    modal.querySelectorAll('input[name="blKieuThanhToan"]').forEach(input => {
        input.addEventListener('change', () => {
            const selected = modal.querySelector('input[name="blKieuThanhToan"]:checked')?.value || 'ThanhToanHet';
            const amountInput = document.getElementById('blTongTien');
            const hint = document.getElementById('blAmountHint');
            modal.querySelectorAll('.bl-pay-option').forEach(label => {
                label.classList.toggle('active', label.dataset.payOption === selected);
            });
            if (selected === 'ThanhToanHet') {
                amountInput.value = soTienMacDinh;
                amountInput.readOnly = true;
                if (hint) hint.textContent = 'Đang chọn thanh toán hết nên số tiền bằng phần còn lại.';
            } else {
                amountInput.readOnly = false;
                amountInput.value = '';
                amountInput.focus();
                if (hint) hint.textContent = `Nhập số tiền nhỏ hơn ${formatMoney(soTienMacDinh)}.`;
            }
        });
    });

    document.getElementById('formGuiBienLai').addEventListener('submit', function (e) {
        e.preventDefault();
        guiBienLai(hoaDon.maHoaDon);
    });

    // Preview ảnh khi chọn
    document.getElementById('blAnhBienLai').addEventListener('change', function () {
        const file = this.files[0];
        if (!file) { document.getElementById('blAnhPreview').style.display = 'none'; return; }
        if (file.size > 10 * 1024 * 1024) {
            const errorEl = document.getElementById('blError');
            errorEl.textContent = 'Ảnh biên lai không được vượt quá 10MB';
            errorEl.style.display = 'block';
            this.value = '';
            document.getElementById('blAnhPreview').style.display = 'none';
            return;
        }
        const reader = new FileReader();
        reader.onload = (e) => {
            document.getElementById('blAnhPreviewImg').src = e.target.result;
            document.getElementById('blAnhPreview').style.display = 'block';
        };
        reader.readAsDataURL(file);
    });
}

async function guiBienLai(maHoaDon) {
    const tongTien = parseFloat(document.getElementById('blTongTien').value);
    const kieuThanhToan = document.querySelector('input[name="blKieuThanhToan"]:checked')?.value || 'ThanhToanHet';
    const maxTien = parseFloat(document.getElementById('blTongTien').max || '0');
    const hinhThuc = document.getElementById('blHinhThuc').value;
    const maGiaoDich = document.getElementById('blMaGiaoDich').value.trim();
    const ghiChu = document.getElementById('blGhiChu').value.trim();
    const fileInput = document.getElementById('blAnhBienLai');
    const anhBienLai = fileInput.files[0] || null;
    const errorEl = document.getElementById('blError');
    const btn = document.getElementById('blBtnGui');

    errorEl.style.display = 'none';

    if (!tongTien || tongTien <= 0) {
        errorEl.textContent = 'Vui lòng nhập số tiền hợp lệ (lớn hơn 0)';
        errorEl.style.display = 'block';
        return;
    }
    if (maxTien > 0 && tongTien > maxTien) {
        errorEl.textContent = 'Số tiền gửi biên lai không được vượt quá số tiền còn lại';
        errorEl.style.display = 'block';
        return;
    }
    if (kieuThanhToan === 'MotPhan' && maxTien > 0 && tongTien >= maxTien) {
        errorEl.textContent = 'Thanh toán 1 phần phải nhỏ hơn số tiền còn lại. Nếu đã chuyển đủ, hãy chọn Thanh toán hết.';
        errorEl.style.display = 'block';
        return;
    }

    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gửi...';

    try {
        const res = await API.thanhtoan.guiBienLai(
            { maHoaDon, tongTien, kieuThanhToan, hinhThucThanhToan: hinhThuc, maGiaoDich, ghiChu },
            anhBienLai
        );
        document.getElementById('modalGuiBienLai').remove();
        showToast(res.thongBao || 'Đã gửi biên lai thành công! Vui lòng đợi chủ trọ xác nhận.', 'success');
        // Reload lại danh sách hóa đơn nếu đang ở màn hình hóa đơn
        if (typeof loadGenericSection === 'function') loadGenericSection('hoadon');
    } catch (err) {
        errorEl.textContent = err.message || 'Gửi biên lai thất bại';
        errorEl.style.display = 'block';
        btn.disabled = false;
        btn.innerHTML = '<i class="fas fa-paper-plane"></i> Gửi biên lai';
    }
}

// ── Xác nhận biên lai (ChuTro / Admin) ──────────────────────────────────────

function moModalXacNhanBienLai(thanhToan) {
    document.getElementById('modalXacNhanBienLai')?.remove();

    const modal = document.createElement('div');
    modal.id = 'modalXacNhanBienLai';
    modal.className = 'modal-overlay';
    modal.innerHTML = `
        <div class="modal-card animate-fade-in" style="max-width:640px;">
            <div class="modal-header">
                <h2><i class="fas fa-check-circle"></i> Xác nhận biên lai #${thanhToan.maThanhToan}</h2>
                <button type="button" class="close-btn" onclick="document.getElementById('modalXacNhanBienLai').remove()">&times;</button>
            </div>
            <div class="modal-body" style="grid-template-columns:1fr;gap:1rem;">
                <div class="bl-confirm-summary">
                    <div><span>Người thuê:</span> <strong>${thanhToan.tenNguoiThue || '#' + thanhToan.maNguoiThue}</strong></div>
                    <div><span>Hóa đơn:</span> <strong>HĐ#${thanhToan.maHoaDon}</strong></div>
                    <div><span>Số tiền:</span> <strong class="bl-confirm-amount">${AppFormat.currency(thanhToan.tongTien)}</strong></div>
                    <div><span>Hình thức:</span> ${thanhToan.hinhThucThanhToan}</div>
                    ${thanhToan.maGiaoDich ? `<div><span>Mã GD:</span> ${thanhToan.maGiaoDich}</div>` : ''}
                    ${thanhToan.ghiChu ? `<div><span>Ghi chú:</span> ${thanhToan.ghiChu}</div>` : ''}
                </div>

                ${thanhToan.hinhAnhBienLai ? `
                <div class="bl-receipt-preview">
                    <p>Ảnh biên lai:</p>
                    <a href="${thanhToan.hinhAnhBienLai}" target="_blank">
                        <img src="${thanhToan.hinhAnhBienLai}" 
                             class="bl-receipt-image"
                             title="Click để xem full" />
                    </a>
                </div>
                ` : '<p class="bl-receipt-empty"><i class="fas fa-image"></i> Không có ảnh biên lai</p>'}

                <div id="tuChoiGroup" style="display:none;" class="form-group">
                    <label class="form-label required">Lý do từ chối</label>
                    <textarea id="xnLyDoTuChoi" class="form-control" rows="3"
                              placeholder="Nhập lý do từ chối để thông báo cho người thuê..."></textarea>
                </div>

                <div id="xnError" class="alert alert-error" style="display:none;"></div>
            </div>
            <div class="modal-footer" style="gap:8px;">
                <button class="btn btn-secondary" onclick="document.getElementById('modalXacNhanBienLai').remove()">
                    Đóng
                </button>
                <button class="btn btn-danger" onclick="thucHienXacNhan(${thanhToan.maThanhToan}, false)">
                    <i class="fas fa-times-circle"></i> Từ chối
                </button>
                <button class="btn btn-success" onclick="thucHienXacNhan(${thanhToan.maThanhToan}, true)">
                    <i class="fas fa-check"></i> Xác nhận thanh toán
                </button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);
}

async function thucHienXacNhan(maThanhToan, chapNhan) {
    const errorEl = document.getElementById('xnError');
    const tuChoiGroup = document.getElementById('tuChoiGroup');
    errorEl.style.display = 'none';

    if (!chapNhan) {
        // Hiển thị ô nhập lý do nếu chưa hiển thị
        if (tuChoiGroup.style.display === 'none') {
            tuChoiGroup.style.display = 'block';
            return; // Đợi user nhập rồi bấm lại
        }
        const lyDo = document.getElementById('xnLyDoTuChoi').value.trim();
        if (!lyDo) {
            errorEl.textContent = 'Vui lòng nhập lý do từ chối';
            errorEl.style.display = 'block';
            return;
        }

        try {
            const res = await API.thanhtoan.xacNhan(maThanhToan, false, lyDo);
            document.getElementById('modalXacNhanBienLai').remove();
            showToast(res.thongBao || 'Đã từ chối biên lai', 'warning');
            if (typeof renderBienLaiChoXacNhan === 'function') renderBienLaiChoXacNhan();
            if (typeof window.refreshSidebarBadges === 'function') window.refreshSidebarBadges();
        } catch (err) {
            errorEl.textContent = err.message;
            errorEl.style.display = 'block';
        }
    } else {
        try {
            const res = await API.thanhtoan.xacNhan(maThanhToan, true);
            document.getElementById('modalXacNhanBienLai').remove();
            showToast(res.thongBao || 'Đã xác nhận thanh toán thành công', 'success');
            if (typeof renderBienLaiChoXacNhan === 'function') renderBienLaiChoXacNhan();
            if (typeof window.refreshSidebarBadges === 'function') window.refreshSidebarBadges();
        } catch (err) {
            errorEl.textContent = err.message;
            errorEl.style.display = 'block';
        }
    }
}

// ── Render danh sách biên lai chờ xác nhận ───────────────────────────────────

async function renderBienLaiChoXacNhan() {
    const container = document.getElementById('bienLaiContainer');
    if (!container) return;

    container.innerHTML = `<div class="loading-spinner"><i class="fas fa-spinner fa-spin"></i> Đang tải...</div>`;

    try {
        const res = await API.thanhtoan.getChoXacNhan();
        const list = res.duLieu || res;

        if (!list || list.length === 0) {
            container.innerHTML = `
                <div class="empty-state">
                    <i class="fas fa-check-circle" style="font-size:2.5rem;color:var(--success);"></i>
                    <p>Không có biên lai nào đang chờ xác nhận</p>
                </div>`;
            return;
        }

        container.innerHTML = `
            <table class="data-table">
                <thead>
                    <tr>
                        <th>Hóa đơn</th>
                        <th>Người thuê</th>
                        <th>Số tiền</th>
                        <th>Hình thức</th>
                        <th>Mã GD</th>
                        <th>Biên lai</th>
                        <th>Ngày gửi</th>
                        <th>Thao tác</th>
                    </tr>
                </thead>
                <tbody>
                    ${list.map(t => `
                    <tr>
                        <td><strong>HĐ#${t.maHoaDon}</strong></td>
                        <td>${t.tenNguoiThue || '#' + t.maNguoiThue}</td>
                        <td><strong style="color:var(--primary)">${AppFormat.currency(t.tongTien)}</strong></td>
                        <td>${t.hinhThucThanhToan}</td>
                        <td>${t.maGiaoDich || '<span style="color:var(--text-muted)">—</span>'}</td>
                        <td>
                            ${t.hinhAnhBienLai
                                ? `<a href="${t.hinhAnhBienLai}" target="_blank" class="btn btn-sm" style="padding:2px 8px;font-size:.8rem;">
                                     <i class="fas fa-image"></i> Xem
                                   </a>`
                                : '<span style="color:var(--text-muted);font-size:.8rem;">Không có</span>'}
                        </td>
                        <td>${AppFormat.date(t.ngayThanhToan)}</td>
                        <td>
                            <button class="btn btn-sm btn-primary"
                                    onclick="moModalXacNhanBienLai(${JSON.stringify(t).replace(/"/g, '&quot;')})">
                                <i class="fas fa-check-square"></i> Xem & Xác nhận
                            </button>
                        </td>
                    </tr>`).join('')}
                </tbody>
            </table>
        `;
    } catch (err) {
        container.innerHTML = `<div class="alert alert-error">Lỗi tải dữ liệu: ${err.message}</div>`;
    }
}

async function capNhatBadgeBienLai() {
    const badge = document.getElementById('bienLaiBadge');
    if (!badge || !window.API?.thanhtoan?.getChoXacNhan) return;

    try {
        const res = await API.thanhtoan.getChoXacNhan();
        const list = res?.duLieu || res || [];
        const count = Array.isArray(list) ? list.length : 0;
        if (count > 0) {
            badge.textContent = count > 99 ? '99+' : String(count);
            badge.style.display = 'inline-flex';
        } else {
            badge.textContent = '';
            badge.style.display = 'none';
        }
    } catch (err) {
        console.warn('Không cập nhật được badge biên lai:', err);
        badge.style.display = 'none';
    }
}

// Export để dùng global
window.moModalGuiBienLai = moModalGuiBienLai;
window.guiBienLai = guiBienLai;
window.moModalXacNhanBienLai = moModalXacNhanBienLai;
window.thucHienXacNhan = thucHienXacNhan;
window.renderBienLaiChoXacNhan = renderBienLaiChoXacNhan;
window.capNhatBadgeBienLai = capNhatBadgeBienLai;

