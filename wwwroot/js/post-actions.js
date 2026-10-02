/* ============================================================
   HiveCore — post interaction handlers (like, bookmark, share, delete, repost)
   ============================================================ */
(function () {
    'use strict';

    // ============================================================
    // Like toggle
    // ============================================================
    document.addEventListener('click', async e => {
        const btn = e.target.closest('.js-like');
        if (!btn) return;

        e.preventDefault();

        const postId = btn.dataset.id;
        const wasLiked = btn.classList.contains('liked');
        const url = `/Like/Post/${postId}/Toggle`;

        // Optimistic UI
        btn.disabled = true;
        const icon = btn.querySelector('i');
        const countEl = btn.querySelector('.js-like-count');
        let count = parseInt(countEl?.textContent || '0', 10);

        if (wasLiked) {
            btn.classList.remove('liked');
            if (icon) icon.className = 'bi bi-heart';
            if (countEl) countEl.textContent = Math.max(0, count - 1);
        } else {
            btn.classList.add('liked');
            if (icon) icon.className = 'bi bi-heart-fill';
            if (countEl) countEl.textContent = count + 1;
        }

        try {
            const res = await window.hcPost(url, {});
            if (!res.ok) throw new Error('failed');
        } catch {
            // Roll back on error
            if (wasLiked) {
                btn.classList.add('liked');
                if (icon) icon.className = 'bi bi-heart-fill';
                if (countEl) countEl.textContent = count;
            } else {
                btn.classList.remove('liked');
                if (icon) icon.className = 'bi bi-heart';
                if (countEl) countEl.textContent = count;
            }
        } finally {
            btn.disabled = false;
        }
    });

    // ============================================================
    // Comment like toggle
    // ============================================================
    document.addEventListener('click', async e => {
        const btn = e.target.closest('.js-like-comment');
        if (!btn) return;

        e.preventDefault();

        const commentId = btn.dataset.id;
        const wasLiked = btn.classList.contains('liked');
        const url = `/Like/Comment/${commentId}/Toggle`;

        btn.disabled = true;
        const icon = btn.querySelector('i');
        const countEl = btn.querySelector('.js-like-count');
        let count = parseInt(countEl?.textContent || '0', 10);

        if (wasLiked) {
            btn.classList.remove('liked');
            if (icon) icon.className = 'bi bi-heart';
            if (countEl) countEl.textContent = Math.max(0, count - 1);
        } else {
            btn.classList.add('liked');
            if (icon) icon.className = 'bi bi-heart-fill';
            if (countEl) countEl.textContent = count + 1;
        }

        try {
            const res = await window.hcPost(url, {});
            if (!res.ok) throw new Error('failed');
        } catch {
            if (wasLiked) {
                btn.classList.add('liked');
                if (icon) icon.className = 'bi bi-heart-fill';
                if (countEl) countEl.textContent = count;
            } else {
                btn.classList.remove('liked');
                if (icon) icon.className = 'bi bi-heart';
                if (countEl) countEl.textContent = count;
            }
        } finally {
            btn.disabled = false;
        }
    });

    // ============================================================
    // Bookmark toggle
    // ============================================================
    document.addEventListener('click', async e => {
        const btn = e.target.closest('.js-bookmark');
        if (!btn) return;

        e.preventDefault();

        const postId = btn.dataset.id;
        const wasBookmarked = btn.classList.contains('bookmarked');
        const url = `/Bookmark/Toggle/${postId}`;

        btn.disabled = true;
        const icon = btn.querySelector('i');
        const countEl = btn.querySelector('.js-bookmark-count');
        let count = parseInt(countEl?.textContent || '0', 10);

        if (wasBookmarked) {
            btn.classList.remove('bookmarked');
            if (icon) icon.className = 'bi bi-bookmark';
            if (countEl) countEl.textContent = Math.max(0, count - 1);
        } else {
            btn.classList.add('bookmarked');
            if (icon) icon.className = 'bi bi-bookmark-fill';
            if (countEl) countEl.textContent = count + 1;
        }

        try {
            const res = await window.hcPost(url, {});
            if (!res.ok) throw new Error('failed');
        } catch {
            if (wasBookmarked) {
                btn.classList.add('bookmarked');
                if (icon) icon.className = 'bi bi-bookmark-fill';
                if (countEl) countEl.textContent = count;
            } else {
                btn.classList.remove('bookmarked');
                if (icon) icon.className = 'bi bi-bookmark';
                if (countEl) countEl.textContent = count;
            }
        } finally {
            btn.disabled = false;
        }
    });

    // ============================================================
    // Repost
    // ============================================================
    document.addEventListener('click', async e => {
        const btn = e.target.closest('.js-repost');
        if (!btn) return;

        e.preventDefault();
        const postId = btn.dataset.id;

        if (!confirm('Repost this to your followers?')) return;

        try {
            const res = await window.hcPost(`/Post/Repost/${postId}`, {});
            if (res.ok) {
                btn.classList.add('active');
                const countEl = btn.querySelector('span');
                if (countEl) countEl.textContent = (parseInt(countEl.textContent, 10) || 0) + 1;
            }
        } catch {
            alert('Failed to repost.');
        }
    });

    // ============================================================
    // Share (copy link to clipboard)
    // ============================================================
    document.addEventListener('click', async e => {
        const btn = e.target.closest('.js-share');
        if (!btn) return;

        e.preventDefault();
        const url = new URL(btn.dataset.url, window.location.origin).toString();

        try {
            if (navigator.share) {
                await navigator.share({ url });
            } else if (navigator.clipboard) {
                await navigator.clipboard.writeText(url);
                showToast('Link copied to clipboard');
            } else {
                prompt('Copy this link:', url);
            }
        } catch {
            // user cancelled
        }
    });

    // ============================================================
    // Delete post
    // ============================================================
    document.addEventListener('click', async e => {
        const btn = e.target.closest('.js-delete-post');
        if (!btn) return;

        e.preventDefault();
        const postId = btn.dataset.id;
        if (!confirm('Delete this post? This cannot be undone.')) return;

        try {
            const res = await window.hcPost(`/Post/Delete/${postId}`, {});
            if (res.ok) {
                const card = document.querySelector(`.post-card[data-post-id="${postId}"]`);
                card?.remove();
            } else {
                alert('Failed to delete.');
            }
        } catch {
            alert('Network error.');
        }
    });

    // ============================================================
    // Delete comment
    // ============================================================
    document.addEventListener('click', async e => {
        const btn = e.target.closest('.js-delete-comment');
        if (!btn) return;

        e.preventDefault();
        const commentId = btn.dataset.id;
        if (!confirm('Delete this comment?')) return;

        try {
            const res = await window.hcPost(`/Comment/Delete/${commentId}`, {});
            if (res.ok) {
                document.getElementById(`comment-${commentId}`)?.remove();
            }
        } catch {
            alert('Failed to delete comment.');
        }
    });

    // ============================================================
    // Simple toast
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
})();

// Toast styling injected once
(function () {
    if (document.getElementById('hc-toast-style')) return;
    const style = document.createElement('style');
    style.id = 'hc-toast-style';
    style.textContent = `
        .hc-toast {
            position: fixed;
            bottom: 24px;
            left: 50%;
            transform: translateX(-50%) translateY(20px);
            background: #14181d;
            color: #e6e9ee;
            padding: 10px 20px;
            border-radius: 12px;
            border: 1px solid #262c34;
            box-shadow: 0 8px 24px rgba(0,0,0,0.4);
            opacity: 0;
            transition: all 0.25s ease;
            z-index: 9999;
            font-size: 0.9rem;
        }
        .hc-toast.show { opacity: 1; transform: translateX(-50%) translateY(0); }
    `;
    document.head.appendChild(style);
})();