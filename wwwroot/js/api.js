/**
 * API SERVICE LAYER
 * Chứa tất cả các hàm gọi API chuyên sâu từ Controllers
 */

const API = {
    // 1. AUTHENTICATION
    auth: {
        login: (data) => apiFetch('/api/Auth/dang-nhap', 'POST', data),
        register: (data) => apiFetch('/api/Auth/dang-ky', 'POST', data),
    },

    // 2. PHÒNG (ROOMS)
    phong: {
        getAll: () => apiFetch('/api/Phong'),
        getById: (id) => apiFetch('/api/Phong/' + id),
        create: (data) => apiFetch('/api/Phong', 'POST', data),
        update: (id, data) => apiFetch('/api/Phong/' + id, 'PUT', data),
        delete: (id) => apiFetch('/api/Phong/' + id, 'DELETE'),
        uploadImage: async (file) => {
            const formData = new FormData();
            formData.append('file', file);

            const res = await fetch('/api/Phong/upload-image', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
                body: formData
            });

            const text = await res.text();
            let json = {};
            try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }

            if (!res.ok || json.thanhCong === false) {
                throw new Error(extractApiErrorMessage(json) || 'Upload thất bại');
            }

            return json.duLieu || json;
        }
    },

    nhatro: {
        uploadImage: async (file) => {
            const formData = new FormData();
            formData.append('file', file);

            const res = await fetch('/api/NhaTro/upload-image', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
                body: formData
            });

            const text = await res.text();
            let json = {};
            try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }

            if (!res.ok || json.thanhCong === false) {
                throw new Error(extractApiErrorMessage(json) || 'Upload thất bại');
            }

            return json.duLieu || json;
        }
    },

    // 3. HÓA ĐƠN (INVOICES)
    hoadon: {
        getAll: () => apiFetch('/api/HoaDon'),
        getInfoByPhong: (phongId) => apiFetch(`/api/HoaDon/GetThongTinPhong/${phongId}`),
        // Lấy JSON data hóa đơn để in (dùng với HoaDonPrint.openModal)
        exportPdf: (id) => {
            return apiFetch(`/api/HoaDon/ExportPdf/${id}`);
        },
        // Xuất CSV/Excel từ backend với filter
        exportExcelBackend: (params = {}) => {
            const qs = new URLSearchParams();
            if (params.kyHoaDon)  qs.set('kyHoaDon',  params.kyHoaDon);
            if (params.trangThai) qs.set('trangThai',  params.trangThai);
            if (params.maPhong)   qs.set('maPhong',    params.maPhong);
            const token = localStorage.getItem('token');
            return fetch(`/api/HoaDon/ExportExcel?${qs.toString()}`, {
                headers: { 'Authorization': `Bearer ${token}` }
            }).then(res => {
                if (!res.ok) throw new Error('Xuất Excel thất bại');
                return res.blob();
            }).then(blob => {
                const url = URL.createObjectURL(blob);
                const a = document.createElement('a');
                a.href = url; a.download = `hoa-don.csv`; a.click();
                URL.revokeObjectURL(url);
            });
        }
    },

    // 4. THANH TOÁN & BIÊN LAI
    thanhtoan: {
        getByHoaDon: (hoaDonId) => apiFetch(`/api/ThanhToan/HoaDon/${hoaDonId}`),
        create: (data) => apiFetch('/api/ThanhToan', 'POST', data),

        /**
         * Người dùng gửi biên lai thanh toán (multipart/form-data)
         * @param {Object} dto - { maHoaDon, tongTien, hinhThucThanhToan, maGiaoDich, ghiChu }
         * @param {File|null} anhBienLai - File ảnh biên lai
         */
        guiBienLai: async (dto, anhBienLai) => {
            const formData = new FormData();
            formData.append('maHoaDon', dto.maHoaDon);
            formData.append('tongTien', dto.tongTien);
            formData.append('kieuThanhToan', dto.kieuThanhToan || 'ThanhToanHet');
            formData.append('hinhThucThanhToan', dto.hinhThucThanhToan || 'ChuyenKhoan');
            if (dto.maGiaoDich) formData.append('maGiaoDich', dto.maGiaoDich);
            if (dto.ghiChu) formData.append('ghiChu', dto.ghiChu);
            if (anhBienLai) formData.append('anhBienLai', anhBienLai);

            const res = await fetch('/api/ThanhToan/gui-bien-lai', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
                body: formData
            });

            const text = await res.text();
            let json = {};
            try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }

            if (!res.ok || json.thanhCong === false) {
                throw new Error(extractApiErrorMessage(json) || 'Gửi biên lai thất bại');
            }
            return json;
        },

        /** Chủ trọ / Admin lấy danh sách biên lai chờ xác nhận */
        getChoXacNhan: () => apiFetch('/api/ThanhToan/cho-xac-nhan'),

        /**
         * Chủ trọ / Admin xác nhận hoặc từ chối biên lai
         * @param {number} id - MaThanhToan
         * @param {boolean} chapNhan
         * @param {string} [lyDoTuChoi]
         */
        xacNhan: (id, chapNhan, lyDoTuChoi = '') =>
            apiFetch(`/api/ThanhToan/${id}/xac-nhan`, 'PUT', { chapNhan, lyDoTuChoi }),
    },

    // 5. HỢP ĐỒNG (CONTRACTS)
    hopdong: {
        getAll: () => apiFetch('/api/HopDong'),
        getInitData: () => apiFetch('/api/HopDong/TaoMoi'),
    },

    // 6. KHÁCH THUÊ (TENANTS)
    nguoithue: {
        getAll: () => apiFetch('/api/NguoiThue'),
        getById: (id) => apiFetch(`/api/NguoiThue/${id}`),
        search: (keyword) => apiFetch(`/api/NguoiThue/Search?keyword=${encodeURIComponent(keyword)}`),
        delete: (id) => apiFetch(`/api/NguoiThue/${id}`, 'DELETE'),
        getMine: () => apiFetch('/api/NguoiThue/cua-toi'),
        updateMine: (id, data) => apiFetch(`/api/NguoiThue/cua-toi/${id}`, 'PUT', data),
        uploadCccdImage: async (file) => {
            const formData = new FormData();
            formData.append('file', file);

            const res = await fetch('/api/NguoiThue/upload-cccd-image', {
                method: 'POST',
                headers: { 'Authorization': `Bearer ${localStorage.getItem('token')}` },
                body: formData
            });

            const text = await res.text();
            let json = {};
            try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }

            if (!res.ok || json.thanhCong === false) {
                throw new Error(extractApiErrorMessage(json) || 'Upload ảnh CCCD thất bại');
            }

            return json.duLieu || json;
        }
    },

    // 7. YÊU CẦU THUÊ
    yeucauthue: {
        getAll: () => apiFetch('/api/YeuCauThue'),
        create: (data) => apiFetch('/api/YeuCauThue', 'POST', data),
        accept: (id, data) => apiFetch(`/api/YeuCauThue/${id}/chap-nhan`, 'POST', data),
        confirmContract: (id) => apiFetch(`/api/YeuCauThue/${id}/xac-nhan-hop-dong`, 'POST', {}),
        rejectContract: (id, data) => apiFetch(`/api/YeuCauThue/${id}/tu-choi-hop-dong`, 'POST', data),
        reject: (id, data) => apiFetch(`/api/YeuCauThue/${id}/tu-choi`, 'POST', data),
        delete: (id) => apiFetch(`/api/YeuCauThue/${id}`, 'DELETE'),
    },

    // 7b. YÊU CẦU GIA HẠN HỢP ĐỒNG
    yeucaugiahan: {
        getAll: () => apiFetch('/api/YeuCauGiaHan'),
        create: (data) => apiFetch('/api/YeuCauGiaHan', 'POST', data),
        accept: (id, data) => apiFetch(`/api/YeuCauGiaHan/${id}/chap-nhan`, 'POST', data),
        reject: (id, data) => apiFetch(`/api/YeuCauGiaHan/${id}/tu-choi`, 'POST', data),
        delete: (id) => apiFetch(`/api/YeuCauGiaHan/${id}`, 'DELETE'),
    },

    // 8. ĐIỆN & NƯỚC
    dien: {
        getAll: () => apiFetch('/api/ChiSoDien'),
    },
    nuoc: {
        getAll: () => apiFetch('/api/ChiSoNuoc'),
    },

    // 9. THÔNG BÁO
    thongbao: {
        getAll: () => apiFetch('/api/ThongBao'),
        getChuaDoc: () => apiFetch('/api/ThongBao/chua-doc'),
        create: (data) => apiFetch('/api/ThongBao', 'POST', data),
        daDoc: (id) => apiFetch(`/api/ThongBao/${id}/da-doc`, 'PUT'),
        docTatCa: () => apiFetch('/api/ThongBao/doc-tat-ca', 'PUT'),
        an: (id) => apiFetch(`/api/ThongBao/${id}/an`, 'PUT'),
        getInitData: () => apiFetch('/api/ThongBao/init-data')
    }
};

/**
 * BASE FETCH WRAPPER
 */
async function apiFetch(endpoint, method = 'GET', body = null) {
    const token = localStorage.getItem('token');
    const opts = {
        method,
        headers: { 
            'Authorization': `Bearer ${token}`, 
            'Content-Type': 'application/json' 
        }
    };
    if (body) opts.body = JSON.stringify(body);

    const res = await fetch(endpoint, opts);
    if (res.status === 401) {
        localStorage.clear();
        window.location.href = '/index.html';
        return;
    }
    if (res.status === 204) return true;

    const text = await res.text();
    let json = {};
    try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }

    if (!res.ok || json.thanhCong === false) {
        throw new Error(extractApiErrorMessage(json) || 'Lỗi hệ thống');
    }

    if (method === 'DELETE') return json;

    return json;
}

function extractApiErrorMessage(json) {
    if (!json) return '';
    if (typeof json === 'string') return json;
    if (json.thongBao) return json.thongBao;
    if (json.message) return json.message;

    if (json.errors) {
        const errors = Object.values(json.errors).flat().filter(Boolean);
        if (errors.length > 0) return errors.join('; ');
    }

    if (json.title && json.title !== 'One or more validation errors occurred.') {
        return json.title;
    }

    return '';
}
