document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    const btnText = document.getElementById('btnText');
    const btnLoader = document.getElementById('btnLoader');
    const errorMessage = document.getElementById('errorMessage');

    // Reset state
    errorMessage.style.display = 'none';
    btnText.style.display = 'none';
    btnLoader.style.display = 'inline-block';

    try {
        const response = await fetch('/api/Auth/dang-nhap', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                tenDangNhap: username,
                matKhau: password
            })
        });

        if (response.ok) {
            const data = await response.json();
            // Store token and user info
            localStorage.setItem('token', data.token);
            localStorage.setItem('user', JSON.stringify(data));
            
            // Redirect to dashboard
            window.location.href = '/dashboard.html';
        } else {
            const errorData = await response.json();
            errorMessage.textContent = errorData.thongBao || 'Đăng nhập thất bại. Vui lòng thử lại.';
            errorMessage.style.display = 'block';
        }
    } catch (error) {
        console.error('Login error:', error);
        errorMessage.textContent = 'Có lỗi kết nối đến hệ thống.';
        errorMessage.style.display = 'block';
    } finally {
        btnText.style.display = 'inline-block';
        btnLoader.style.display = 'none';
    }
});
