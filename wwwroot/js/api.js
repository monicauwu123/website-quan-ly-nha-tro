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
            if (!res.ok) throw new Error('Upload thất bại');
            return await res.json();
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
        search: (keyword) => apiFetch(`/api/NguoiThue/Search?keyword=${encodeURIComponent(keyword)}`),
    },

    // 6. ĐIỆN & NƯỚC
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
    const json = await res.json();
    if (!res.ok) throw new Error(json.message || 'Lỗi hệ thống');
    return json;
}
