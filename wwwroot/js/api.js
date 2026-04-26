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

    // 3. HÓA ĐƠN (INVOICES)
    hoadon: {
        getAll: () => apiFetch('/api/HoaDon'),
        getInfoByPhong: (phongId) => apiFetch(`/api/HoaDon/GetThongTinPhong/${phongId}`),
        exportPdf: (id) => {
            window.open(`/api/HoaDon/ExportPdf/${id}`, '_blank');
        }
    },

    // 4. HỢP ĐỒNG (CONTRACTS)
    hopdong: {
        getAll: () => apiFetch('/api/HopDong'),
        getInitData: () => apiFetch('/api/HopDong/TaoMoi'),
    },

    // 5. KHÁCH THUÊ (TENANTS)
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

    // 6. YÊU CẦU THUÊ
    yeucauthue: {
        getAll: () => apiFetch('/api/YeuCauThue'),
        create: (data) => apiFetch('/api/YeuCauThue', 'POST', data),
        accept: (id, data) => apiFetch(`/api/YeuCauThue/${id}/chap-nhan`, 'POST', data),
        reject: (id, data) => apiFetch(`/api/YeuCauThue/${id}/tu-choi`, 'POST', data),
        delete: (id) => apiFetch(`/api/YeuCauThue/${id}`, 'DELETE'),
    },

    // 7. ĐIỆN & NƯỚC
    dien: {
        getAll: () => apiFetch('/api/ChiSoDien'),
    },
    nuoc: {
        getAll: () => apiFetch('/api/ChiSoNuoc'),
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
    if (method === 'DELETE' || res.status === 204) return true;

    const text = await res.text();
    let json = {};
    try { json = text ? JSON.parse(text) : {}; } catch { json = {}; }

    if (!res.ok || json.thanhCong === false) {
        throw new Error(extractApiErrorMessage(json) || 'Lỗi hệ thống');
    }

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
