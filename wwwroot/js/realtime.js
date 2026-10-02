/* ============================================================
   HiveCore — SignalR real-time client
   ============================================================ */
(function () {
    'use strict';

    if (typeof signalR === 'undefined') {
        console.warn('SignalR client not loaded; real-time disabled.');
        return;
    }

    // ============================================================
    // NotificationHub
    // ============================================================
    const notifConn = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications')
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    notifConn.on('ReceiveNotification', payload => {
        // Increment badge locally
        const badges = [document.getElementById('notifBadge'),
        document.getElementById('navNotifBadge')];
        badges.forEach(b => {
            if (!b) return;
            const current = parseInt(b.textContent || '0', 10) || 0;
            const next = current + 1;
            b.textContent = next > 99 ? '99+' : next;
            b.style.display = '';
        });

        // Toast
        showToast(`New ${payload.type || 'notification'}`);
    });

    notifConn.on('UnreadCountUpdated', count => {
        const badges = [document.getElementById('notifBadge'),
        document.getElementById('navNotifBadge')];
        badges.forEach(b => {
            if (!b) return;
            if (count > 0) {
                b.textContent = count > 99 ? '99+' : count;
                b.style.display = '';
            } else {
                b.style.display = 'none';
            }
        });
    });

    notifConn.on('NotificationRead', id => {
        document.querySelector(`.notif-item[data-id="${id}"]`)?.classList.remove('unread');
    });

    notifConn.start().catch(err => console.warn('NotificationHub connection failed', err));

    // ============================================================
    // FeedHub
    // ============================================================
    const feedConn = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/feed')
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    feedConn.on('NewPost', post => {
        // Only auto-prepend when the user is at the top of the home feed
        const feedList = document.getElementById('feedList');
        if (!feedList) return;
        if (window.scrollY > 600) {
            showToast('New post available — refresh to see it');
            return;
        }

        // Optional: use template-driven insertion (here we just toast)
        showToast('New post published');
    });

    feedConn.on('CountsUpdated', (postId, likes, comments, reposts, bookmarks) => {
        const card = document.querySelector(`.post-card[data-post-id="${postId}"]`);
        if (!card) return;

        const likeCount = card.querySelector('.js-like .js-like-count');
        const bookmarkCount = card.querySelector('.js-bookmark .js-bookmark-count');

        if (likeCount) likeCount.textContent = likes;
        if (bookmarkCount) bookmarkCount.textContent = bookmarks;
    });

    feedConn.on('PostDeleted', postId => {
        document.querySelector(`.post-card[data-post-id="${postId}"]`)?.remove();
    });

    feedConn.start().catch(err => console.warn('FeedHub connection failed', err));

    // Subscribe to a post's thread when on the details page
    const detailsPost = document.querySelector('.post-card[data-post-id]');
    if (detailsPost && document.querySelector('.comments-section')) {
        const postId = detailsPost.dataset.postId;
        feedConn.onreconnected(() => feedConn.invoke('JoinPostGroup', postId).catch(() => { }));
        feedConn.start().then(() => feedConn.invoke('JoinPostGroup', postId).catch(() => { }));
    }

    // ============================================================
    // Toast helper (shared with post-actions.js — safe duplicate)
    // ============================================================
    function showToast(message) {
        const el = document.createElement('div');
        el.className = 'hc-toast';
        el.textContent = message;
        document.body.appendChild(el);
        setTimeout(() => el.classList.add('show'), 10);
        setTimeout(() => {
            el.classList.remove('show');
            setTimeout(() => el.remove(), 300);
        }, 2500);
    }

    window.hcSignalR = { notifConn, feedConn };
})();