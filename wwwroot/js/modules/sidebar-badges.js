// ==========================================
// SIDEBAR BADGES - chấm đỏ/số lượng cảnh báo nhanh
// - Yêu cầu thuê chờ duyệt
// - Báo cáo sự cố chưa xử lý
// - Thông báo chưa đọc
// - Biên lai chờ duyệt
// ==========================================
(function () {
    const POLL_MS = 60000;
    const DISMISS_KEY = 'sidebarBadgeDismissedCounts';
    const GROUP_DISMISS_KEY = 'sidebarGroupBadgeDismissedCounts';
    const SECTION_TO_BADGE = {
        yeucauthue: 'yeuCauThueBadge',
        baocaosuco: 'baoCaoSuCoBadge',
        thongbao: 'thongBaoBadge',
        bienlai: 'bienLaiBadge'
    };
    const GROUP_BADGES = {
        groupNhaTroBadge: [],
        groupThueTroBadge: ['yeuCauThueBadge'],
        groupTaiChinhBadge: ['bienLaiBadge'],
        groupHoTroBadge: ['baoCaoSuCoBadge', 'thongBaoBadge']
    };
    const latestCounts = {};
    let poller = null;

    function getCurrentRole() {
        try {
            const user = JSON.parse(localStorage.getItem('user') || '{}');
            return (user.vaiTro || localStorage.getItem('vaiTro') || '').trim();
        } catch {
            return (localStorage.getItem('vaiTro') || '').trim();
        }
    }

    function unwrap(value) {
        if (value == null) return value;
        if (Array.isArray(value)) return value;
        if (Array.isArray(value.duLieu)) return value.duLieu;
        if (Array.isArray(value.data)) return value.data;
        if (Array.isArray(value.$values)) return value.$values;
        if (value.duLieu && Array.isArray(value.duLieu.$values)) return value.duLieu.$values;
        if (value.data && Array.isArray(value.data.$values)) return value.data.$values;
        if (Object.prototype.hasOwnProperty.call(value, 'duLieu')) return value.duLieu;
        if (Object.prototype.hasOwnProperty.call(value, 'data')) return value.data;
        return value;
    }

    async function request(endpoint) {
        const token = localStorage.getItem('token');
        if (!token) return null;

        const res = await fetch(endpoint, {
            headers: {
                'Authorization': `Bearer ${token}`,
                'Content-Type': 'application/json'
            }
        });

        if (res.status === 401) return null;
        const text = await res.text();
        if (!text) return null;

        let json = null;
        try { json = JSON.parse(text); } catch { return null; }
        if (!res.ok || json?.thanhCong === false) return null;
        return unwrap(json);
    }

    function asArray(value) {
        value = unwrap(value);
        if (Array.isArray(value)) return value;
        if (value && Array.isArray(value.$values)) return value.$values;
        return [];
    }

    function asCount(value) {
        value = unwrap(value);
        if (typeof value === 'number') return value;
        if (typeof value === 'string' && value.trim() !== '' && !Number.isNaN(Number(value))) return Number(value);
        if (Array.isArray(value)) return value.length;
        return 0;
    }

    function getDismissedCounts() {
        try { return JSON.parse(localStorage.getItem(DISMISS_KEY) || '{}') || {}; }
        catch { return {}; }
    }

    function setDismissedCount(id, count) {
        const dismissed = getDismissedCounts();
        dismissed[id] = Math.max(0, Number(count || 0));
        localStorage.setItem(DISMISS_KEY, JSON.stringify(dismissed));
    }

    function getDismissedGroupCounts() {
        try { return JSON.parse(localStorage.getItem(GROUP_DISMISS_KEY) || '{}') || {}; }
        catch { return {}; }
    }

    function setDismissedGroupCount(id, count) {
        const dismissed = getDismissedGroupCounts();
        dismissed[id] = Math.max(0, Number(count || 0));
        localStorage.setItem(GROUP_DISMISS_KEY, JSON.stringify(dismissed));
    }

    function setBadge(id, count) {
        const badge = document.getElementById(id);
        if (!badge) return;

        const n = Number(count || 0);
        latestCounts[id] = n;

        const dismissed = getDismissedCounts();
        let dismissedCount = Number(dismissed[id] || 0);
        if (n < dismissedCount) {
            dismissedCount = n;
            setDismissedCount(id, n);
        }

        if (n > 0 && n > dismissedCount) {
            badge.textContent = n > 99 ? '99+' : String(n);
            badge.style.display = 'inline-flex';
            badge.title = `${n} mục cần xử lý`;
        } else {
            badge.textContent = '';
            badge.style.display = 'none';
            badge.removeAttribute('title');
        }

        refreshGroupBadges();
    }

    function getVisibleChildBadgeCount(id) {
        const badge = document.getElementById(id);
        if (!badge || badge.style.display === 'none') return 0;
        const text = (badge.textContent || '').trim();
        if (text === '99+') return 99;
        const parsed = Number(text);
        return Number.isFinite(parsed) ? parsed : 0;
    }

    function refreshGroupBadges() {
        const dismissedGroups = getDismissedGroupCounts();
        Object.entries(GROUP_BADGES).forEach(([groupId, childIds]) => {
            const badge = document.getElementById(groupId);
            if (!badge) return;
            const total = childIds.reduce((sum, childId) => sum + getVisibleChildBadgeCount(childId), 0);
            const dismissedTotal = Number(dismissedGroups[groupId] || 0);
            if (total > 0 && total > dismissedTotal) {
                badge.textContent = total > 99 ? '99+' : String(total);
                badge.style.display = 'inline-flex';
                badge.title = `${total} mục cần xử lý trong nhóm`;
            } else {
                badge.textContent = '';
                badge.style.display = 'none';
                badge.removeAttribute('title');
            }
        });
    }

    function acknowledgeSidebarGroup(groupId) {
        const childIds = GROUP_BADGES[groupId];
        if (!childIds) return;
        const total = childIds.reduce((sum, childId) => sum + getVisibleChildBadgeCount(childId), 0);
        setDismissedGroupCount(groupId, total);
        refreshGroupBadges();
    }

    function dismissSidebarBadgeForSection(section) {
        const id = SECTION_TO_BADGE[section];
        if (!id) return;
        setDismissedCount(id, latestCounts[id] || 0);
        setBadge(id, latestCounts[id] || 0);
    }

    function statusOf(item) {
        return String(item?.trangThai || item?.trangThaiText || item?.tenTrangThai || '').trim();
    }

    function isPendingRentalRequest(item) {
        const st = statusOf(item).toLowerCase();
        const role = getCurrentRole();
        if (role === 'NguoiDung') {
            return st === 'chonguoithuexacnhan' ||
                st.includes('chờ người thuê xác nhận') ||
                st.includes('cho nguoi thue xac nhan');
        }
        return st === 'choduyet' || st.includes('chờ duyệt') || st.includes('cho duyet');
    }

    function isOpenIncident(item) {
        const st = statusOf(item).toLowerCase();
        if (!st) return false;
        return !(
            st === 'daxuly' || st === 'dahoanthanh' || st === 'hoanthanh' ||
            st === 'huy' || st === 'tuchoi' ||
            st.includes('đã xử lý') || st.includes('da xu ly') ||
            st.includes('hoàn thành') || st.includes('hoan thanh') ||
            st.includes('hủy') || st.includes('huy') ||
            st.includes('từ chối') || st.includes('tu choi')
        );
    }

    async function countYeuCauThue() {
        const list = asArray(await request('/api/YeuCauThue'));
        return list.filter(isPendingRentalRequest).length;
    }

    async function countBaoCaoSuCo() {
        const list = asArray(await request('/api/BaoCaoSuCo'));
        return list.filter(isOpenIncident).length;
    }

    async function countThongBao() {
        // Badge thông báo chỉ dành cho người dùng nhận thông báo.
        // Chủ trọ/Admin là người gửi/quản lý nên không hiện chấm đỏ "chưa đọc".
        const role = getCurrentRole();
        if (role !== 'NguoiDung') return 0;
        return asCount(await request('/api/ThongBao/chua-doc'));
    }

    async function countBienLai() {
        const role = getCurrentRole();
        if (role !== 'Admin' && role !== 'ChuTro') return 0;
        const list = asArray(await request('/api/ThanhToan/cho-xac-nhan'));
        return list.length;
    }

    async function refreshSidebarBadges() {
        const tasks = [
            countYeuCauThue().then(n => setBadge('yeuCauThueBadge', n)).catch(() => setBadge('yeuCauThueBadge', 0)),
            countBaoCaoSuCo().then(n => setBadge('baoCaoSuCoBadge', n)).catch(() => setBadge('baoCaoSuCoBadge', 0)),
            countThongBao().then(n => setBadge('thongBaoBadge', n)).catch(() => setBadge('thongBaoBadge', 0)),
            countBienLai().then(n => setBadge('bienLaiBadge', n)).catch(() => setBadge('bienLaiBadge', 0))
        ];
        await Promise.allSettled(tasks);
    }

    function startSidebarBadges() {
        refreshSidebarBadges();
        if (poller) clearInterval(poller);
        poller = setInterval(refreshSidebarBadges, POLL_MS);
    }

    window.refreshSidebarBadges = refreshSidebarBadges;
    window.startSidebarBadges = startSidebarBadges;
    window.dismissSidebarBadgeForSection = dismissSidebarBadgeForSection;
    window.refreshSidebarGroupBadges = refreshGroupBadges;
    window.acknowledgeSidebarGroup = acknowledgeSidebarGroup;

    window.addEventListener('app:ready', startSidebarBadges);
    window.addEventListener('focus', refreshSidebarBadges);
    document.addEventListener('visibilitychange', () => {
        if (!document.hidden) refreshSidebarBadges();
    });

    if (document.readyState !== 'loading') {
        setTimeout(startSidebarBadges, 300);
    } else {
        document.addEventListener('DOMContentLoaded', () => setTimeout(startSidebarBadges, 300));
    }
})();
