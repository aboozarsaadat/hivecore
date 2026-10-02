/* ============================================================
   HiveCore — notification badge polling + interactions
   ============================================================ */
(function () {
    'use strict';

    const badge = document.getElementById('notifBadge');
    const navBadge = document.getElementById('navNotifBadge');

    async function refreshUnreadCount() {
        try {
            const res = await fetch('/Notification/UnreadCount', {
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });
            if (!res.ok) return;
            const data = await res.json();
            const count = data.count || 0;

            updateBadge(count);
        } catch {
            // Silent fail; retry next cycle
        }
    }

    function updateBadge(count) {
        [badge, navBadge].forEach(el => {
            if (!el) return;
            if (count > 0) {
                el.textContent = count > 99 ? '99+' : count;
                el.style.display = '';
            } else {
                el.style.display = 'none';
            }
        });

        document.title = count > 0
            ? `(${count}) HiveCore`
            : document.title.replace(/^\(\d+\+?\) /, '');
    }

    // Initial + polling every 30s
    refreshUnreadCount();
    setInterval(refreshUnreadCount, 30000);

    // ============================================================
    // Click on unread notification → mark it read silently
    // ============================================================
    document.addEventListener('click', e => {
        const item = e.target.closest('.notif-item.unread');
        if (!item) return;

        const id = item.dataset.id;
        if (!id) return;

        // Optimistic UI: remove unread class
        item.classList.remove('unread');

        // Fire-and-forget POST (navigation will happen normally)
        try {
            window.hcPost(`/Notification/MarkRead/${id}`, {});
        } catch {
            // silent
        }
    });
})();