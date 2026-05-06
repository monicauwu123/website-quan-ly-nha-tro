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

    const modal = document.createElement('div');
    modal.id = 'modalGuiBienLai';
    modal.className = 'modal-overlay';
    modal.innerHTML = `
        <div class="modal-box" style="max-width:480px;">
            <div class="modal-header">
                <h3><i class="fas fa-receipt"></i> Gửi biên lai thanh toán</h3>
                <button class="modal-close" onclick="document.getElementById('modalGuiBienLai').remove()">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="modal-body">
                <div class="info-row" style="background:var(--bg-secondary);border-radius:8px;padding:10px 14px;margin-bottom:16px;">
                    <span style="color:var(--text-muted);font-size:.85rem;">Hóa đơn:</span>
                    <strong>HĐ#${hoaDon.maHoaDon} – ${hoaDon.kyHoaDon} – ${AppFormat.currency(hoaDon.tongTien)}</strong>
                </div>

                <div class="form-group">
                    <label class="form-label required">Số tiền đã chuyển (đ)</label>
                    <input id="blTongTien" type="number" class="form-input" placeholder="VD: 2500000"
                           min="1" value="${hoaDon.conLai || hoaDon.tongTien}" required />
                </div>

                <div class="form-group">
                    <label class="form-label required">Hình thức thanh toán</label>
                    <select id="blHinhThuc" class="form-input">
                        <option value="ChuyenKhoan">Chuyển khoản ngân hàng</option>
                        <option value="TienMat">Tiền mặt</option>
                        <option value="MoMo">Ví MoMo</option>
                        <option value="ZaloPay">ZaloPay</option>
                        <option value="VNPay">VNPay</option>
                        <option value="Khac">Khác</option>
                    </select>
                </div>

                <div class="form-group">
                    <label class="form-label">Mã giao dịch <span style="color:var(--text-muted)">(nếu có)</span></label>
                    <input id="blMaGiaoDich" type="text" class="form-input" placeholder="VD: FT2405170001" />
                </div>

                <div class="form-group">
                    <label class="form-label">Ảnh biên lai <span style="color:var(--text-muted)">(tùy chọn, tối đa 10MB)</span></label>
                    <input id="blAnhBienLai" type="file" class="form-input"
                           accept=".jpg,.jpeg,.png,.gif,.webp" />
                    <div id="blAnhPreview" style="margin-top:8px;display:none;">
                        <img id="blAnhPreviewImg" style="max-width:100%;max-height:200px;border-radius:6px;border:1px solid var(--border);" />
                    </div>
                </div>

                <div class="form-group">
                    <label class="form-label">Ghi chú</label>
                    <textarea id="blGhiChu" class="form-input" rows="2"
                              placeholder="Nhập ghi chú nếu có..."></textarea>
                </div>

                <div id="blError" class="alert alert-error" style="display:none;"></div>
            </div>
            <div class="modal-footer">
                <button class="btn btn-secondary"
                        onclick="document.getElementById('modalGuiBienLai').remove()">
                    Hủy
                </button>
                <button id="blBtnGui" class="btn btn-primary" onclick="guiBienLai(${hoaDon.maHoaDon})">
                    <i class="fas fa-paper-plane"></i> Gửi biên lai
                </button>
            </div>
        </div>
    `;
    document.body.appendChild(modal);

    // Preview ảnh khi chọn
    document.getElementById('blAnhBienLai').addEventListener('change', function () {
        const file = this.files[0];
        if (!file) { document.getElementById('blAnhPreview').style.display = 'none'; return; }
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

    btn.disabled = true;
    btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Đang gửi...';

    try {
        const res = await API.thanhtoan.guiBienLai(
            { maHoaDon, tongTien, hinhThucThanhToan: hinhThuc, maGiaoDich, ghiChu },
            anhBienLai
        );
        document.getElementById('modalGuiBienLai').remove();
        showToast(res.thongBao || 'Đã gửi biên lai thành công! Vui lòng đợi chủ trọ xác nhận.', 'success');
        // Reload section hiện tại
        if (typeof loadSection === 'function') loadSection('hoadon');
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
        <div class="modal-box" style="max-width:500px;">
            <div class="modal-header">
                <h3><i class="fas fa-check-circle"></i> Xác nhận biên lai #${thanhToan.maThanhToan}</h3>
                <button class="modal-close" onclick="document.getElementById('modalXacNhanBienLai').remove()">
                    <i class="fas fa-times"></i>
                </button>
            </div>
            <div class="modal-body">
                <div style="display:grid;gap:8px;background:var(--bg-secondary);border-radius:8px;padding:12px 14px;margin-bottom:16px;font-size:.9rem;">
                    <div><span style="color:var(--text-muted);">Người thuê:</span> <strong>${thanhToan.tenNguoiThue || '#' + thanhToan.maNguoiThue}</strong></div>
                    <div><span style="color:var(--text-muted);">Hóa đơn:</span> <strong>HĐ#${thanhToan.maHoaDon}</strong></div>
                    <div><span style="color:var(--text-muted);">Số tiền:</span> <strong style="color:var(--primary)">${AppFormat.currency(thanhToan.tongTien)}</strong></div>
                    <div><span style="color:var(--text-muted);">Hình thức:</span> ${thanhToan.hinhThucThanhToan}</div>
                    ${thanhToan.maGiaoDich ? `<div><span style="color:var(--text-muted);">Mã GD:</span> ${thanhToan.maGiaoDich}</div>` : ''}
                    ${thanhToan.ghiChu ? `<div><span style="color:var(--text-muted);">Ghi chú:</span> ${thanhToan.ghiChu}</div>` : ''}
                </div>

                ${thanhToan.hinhAnhBienLai ? `
                <div style="margin-bottom:16px;text-align:center;">
                    <p style="color:var(--text-muted);font-size:.85rem;margin-bottom:6px;">Ảnh biên lai:</p>
                    <a href="${thanhToan.hinhAnhBienLai}" target="_blank">
                        <img src="${thanhToan.hinhAnhBienLai}" 
                             style="max-width:100%;max-height:280px;border-radius:8px;border:1px solid var(--border);cursor:zoom-in;" 
                             title="Click để xem full" />
                    </a>
                </div>
                ` : '<p style="color:var(--text-muted);font-size:.85rem;margin-bottom:12px;"><i class="fas fa-image"></i> Không có ảnh biên lai</p>'}

                <div id="tuChoiGroup" style="display:none;" class="form-group">
                    <label class="form-label required">Lý do từ chối</label>
                    <textarea id="xnLyDoTuChoi" class="form-input" rows="3"
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

// Export để dùng global
window.moModalGuiBienLai = moModalGuiBienLai;
window.guiBienLai = guiBienLai;
window.moModalXacNhanBienLai = moModalXacNhanBienLai;
window.thucHienXacNhan = thucHienXacNhan;
window.renderBienLaiChoXacNhan = renderBienLaiChoXacNhan;
