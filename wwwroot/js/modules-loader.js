// ==========================================
// MODULES LOADER - bản tối ưu
// Chỉ load HTML cần gắn lên trang + load JS module song song.
// ==========================================

(function () {
    const VERSION = '3.1';

    const mountedHtmlModules = [
        { name: 'sidebar', path: `modules/sidebar.html?v=${VERSION}`, mount: 'sidebarMount' },
        { name: 'topbar', path: `modules/topbar.html?v=${VERSION}`, mount: 'topbarMount' },
        { name: 'overview', path: `modules/overview.html?v=${VERSION}`, mount: 'overviewMount' },
        { name: 'generic', path: `modules/generic.html?v=${VERSION}`, mount: 'genericMount' },
        { name: 'account', path: `modules/account.html?v=${VERSION}`, mount: 'accountMount' },
        { name: 'modal', path: `modules/modal.html?v=${VERSION}`, mount: 'modalMount' }
    ];

    const apiScript = `js/api.js?v=${VERSION}`;

    const businessScripts = [
        `js/modules/nha-tro.js?v=${VERSION}`,
        `js/modules/loai-phong.js?v=${VERSION}`,
        `js/modules/phong.js?v=${VERSION}`,
        `js/modules/khach-thue.js?v=${VERSION}`,
        `js/modules/hop-dong.js?v=${VERSION}`,
        `js/modules/yeu-cau-thue.js?v=${VERSION}`,
        `js/modules/hoa-don.js?v=${VERSION}`,
        `js/modules/thanh-toan.js?v=${VERSION}`,
        `js/modules/dich-vu.js?v=${VERSION}`,
        `js/modules/nguoi-dung.js?v=${VERSION}`,
        `js/modules/bao-cao-su-co.js?v=${VERSION}`,
        `js/modules/dien-nuoc.js?v=${VERSION}`
    ];

    const appScripts = [
        `js/dashboard.js?v=${VERSION}`,
        `js/account.js?v=${VERSION}`
    ];

    window.AppHtmlModules = window.AppHtmlModules || {};
    window.AppModules = window.AppModules || {};
    window.AppDienNuocModules = window.AppDienNuocModules || {};

    async function loadHtmlModule(item) {
        const res = await fetch(item.path, { cache: 'force-cache' });
        if (!res.ok) throw new Error('Không tải được module: ' + item.path);
        const html = await res.text();
        window.AppHtmlModules[item.name] = html;

        const mount = document.getElementById(item.mount);
        if (mount) mount.innerHTML = html;
    }

    function loadScript(src, asyncMode = true) {
        return new Promise((resolve, reject) => {
            const script = document.createElement('script');
            script.src = src;
            script.async = asyncMode;
            script.onload = resolve;
            script.onerror = () => reject(new Error('Không tải được script: ' + src));
            document.body.appendChild(script);
        });
    }

    async function loadScriptsSequential(scripts) {
        for (const src of scripts) {
            await loadScript(src, false);
        }
    }

    async function boot() {
        try {
            // 1) HTML bắt buộc load song song, bỏ các file HTML nghiệp vụ không dùng để tránh đơ.
            await Promise.all(mountedHtmlModules.map(loadHtmlModule));

            // 2) api.js load trước.
            await loadScript(apiScript, false);

            // 3) Các module cấu hình nghiệp vụ độc lập nên load song song.
            await Promise.all(businessScripts.map(src => loadScript(src, true)));

            // 4) dashboard.js và account.js cần chạy sau khi module cấu hình đã có.
            await loadScriptsSequential(appScripts);

            window.dispatchEvent(new Event('app:ready'));
        } catch (err) {
            console.error(err);
            document.body.innerHTML = `<div style="padding:2rem;color:#991b1b;font-family:sans-serif;">
                <h2>Không tải được giao diện</h2>
                <p>${err.message || err}</p>
            </div>`;
        }
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', boot);
    } else {
        boot();
    }
})();
