// =====================================================================
// DARK MODE TOGGLE
// File: wwwroot/js/dark-mode.js
// Load TRƯỚC dashboard.js trong modules-loader.js:
//   thêm `js/dark-mode.js?v=6.5` vào mảng appScripts (hoặc businessScripts)
// =====================================================================

(function () {
    const KEY    = 'darkMode';
    const CLASS  = 'dark-mode';
    const ICON   = { dark: 'fa-sun', light: 'fa-moon' };
    const TITLE  = { dark: 'Chuyển sang sáng', light: 'Chuyển sang tối' };

    // ── Áp dụng theme ngay khi script load (tránh flash) ─────────────
    const saved = localStorage.getItem(KEY);
    // Nếu chưa có setting, theo system preference
    const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
    const isDark = saved !== null ? saved === '1' : prefersDark;
    if (isDark) document.body.classList.add(CLASS);

    // ── Hàm toggle ───────────────────────────────────────────────────
    function toggle() {
        const nowDark = document.body.classList.toggle(CLASS);
        localStorage.setItem(KEY, nowDark ? '1' : '0');
        updateBtn(nowDark);
    }

    // ── Cập nhật icon + tooltip của nút ──────────────────────────────
    function updateBtn(dark) {
        const btn = document.getElementById('darkModeToggle');
        if (!btn) return;
        const icon = btn.querySelector('i');
        if (icon) {
            icon.className = `fas ${dark ? ICON.dark : ICON.light}`;
        }
        btn.title = dark ? TITLE.dark : TITLE.light;
    }

    // ── Tạo nút và inject vào topbar ─────────────────────────────────
    function injectBtn() {
        if (document.getElementById('darkModeToggle')) return; // đã có rồi

        const btn = document.createElement('button');
        btn.id    = 'darkModeToggle';
        btn.title = isDark ? TITLE.dark : TITLE.light;
        btn.innerHTML = `<i class="fas ${isDark ? ICON.dark : ICON.light}"></i>`;
        btn.addEventListener('click', toggle);

        // Tìm vị trí inject: div chứa addBtn trong top-bar
        const topBar = document.querySelector('.top-bar');
        if (!topBar) return;

        const rightGroup = topBar.querySelector('div[style*="flex"]') || topBar.lastElementChild;
        if (rightGroup) {
            // Thêm vào trước nút đầu tiên
            rightGroup.insertBefore(btn, rightGroup.firstChild);
        } else {
            topBar.appendChild(btn);
        }
    }

    // ── Khởi động sau khi DOM / modules sẵn sàng ────────────────────
    function init() {
        injectBtn();
        updateBtn(document.body.classList.contains(CLASS));
    }

    // Thử inject ngay, sau đó listen thêm event app:ready
    if (document.readyState !== 'loading') {
        setTimeout(init, 0);
    } else {
        document.addEventListener('DOMContentLoaded', init);
    }

    // modules-loader.js dispatch 'app:ready' sau khi render xong
    window.addEventListener('app:ready', init);

    // Đề phòng topbar được inject muộn hơn → dùng MutationObserver
    const _obs = new MutationObserver(() => {
        if (document.querySelector('.top-bar') && !document.getElementById('darkModeToggle')) {
            init();
        }
    });
    _obs.observe(document.body, { childList: true, subtree: true });

    // Expose ra global để có thể gọi từ nơi khác nếu cần
    window.DarkMode = { toggle, isDark: () => document.body.classList.contains(CLASS) };
})();
