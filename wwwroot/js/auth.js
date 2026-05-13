// ==========================================
// AUTH.JS – Đăng nhập + lưu role vào localStorage
// ==========================================

document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();

    const username  = document.getElementById('username').value;
    const password  = document.getElementById('password').value;
    const btnText   = document.getElementById('btnText');
    const btnLoader = document.getElementById('btnLoader');
    const errorMsg  = document.getElementById('errorMessage');

    errorMsg.style.display  = 'none';
    btnText.style.display   = 'none';
    btnLoader.style.display = 'inline-block';

    try {
        const response = await fetch('/api/Auth/dang-nhap', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ tenDangNhap: username, matKhau: password })
        });

        if (response.ok) {
            const data = await response.json();

            // Hỗ trợ cả ApiResponse wrapper { data: {...} } lẫn flat object
            const payload = data.data ?? data;

            // Chuẩn hoá tên field role (backend có thể trả vaiTro hoặc role)
            const role = payload.vaiTro ?? payload.role ?? '';

            const userInfo = {
                maNguoiDung : payload.maNguoiDung ?? payload.id ?? null,
                hoTen       : payload.hoTen ?? payload.fullName ?? '',
                tenDangNhap : payload.tenDangNhap ?? username,
                email       : payload.email ?? '',
                vaiTro      : role,
                token       : payload.token
            };

            localStorage.setItem('token', payload.token);
            localStorage.setItem('user',  JSON.stringify(userInfo));

            // Redirect về dashboard – dashboard.js tự ẩn/hiện menu theo role
            window.location.href = '/dashboard.html';

        } else {
            const err = await response.json().catch(() => ({}));
            errorMsg.textContent = err.thongBao ?? err.message ?? 'Đăng nhập thất bại. Vui lòng thử lại.';
            errorMsg.style.display = 'block';
        }
    } catch (error) {
        console.error('Login error:', error);
        errorMsg.textContent = 'Có lỗi kết nối đến hệ thống.';
        errorMsg.style.display = 'block';
    } finally {
        btnText.style.display   = 'inline-block';
        btnLoader.style.display = 'none';
    }
});
