/* ============================================================
   HiveCore — global site scripts
   ============================================================ */
(function () {
    'use strict';

    // ============================================================
    // CSRF token helper
    // ============================================================
    window.hcCsrfToken = function () {
        const el = document.querySelector('input[name="__RequestVerificationToken"]');
        return el ? el.value : '';
    };

    // ============================================================
    // AJAX wrapper that injects CSRF
    // ============================================================
    window.hcPost = function (url, data, options) {
        options = options || {};
        const headers = Object.assign({
            'X-Requested-With': 'XMLHttpRequest',
            'RequestVerificationToken': window.hcCsrfToken()
        }, options.headers || {});

        const body = data instanceof FormData
            ? data
            : new URLSearchParams(data || {}).toString();

        if (!(data instanceof FormData) && !headers['Content-Type']) {
            headers['Content-Type'] = 'application/x-www-form-urlencoded';
        }

        return fetch(url, {
            method: 'POST',
            credentials: 'same-origin',
            headers: headers,
            body: body
        });
    };

    // ============================================================
    // Mobile sidebar toggle
    // ============================================================
    const menuToggle = document.querySelector('.app-menu-toggle');
    const sidebar = document.getElementById('appSidebar');

    menuToggle?.addEventListener('click', () => {
        sidebar?.classList.toggle('open');
    });

    document.addEventListener('click', e => {
        if (!sidebar?.classList.contains('open')) return;
        if (sidebar.contains(e.target) || menuToggle?.contains(e.target)) return;
        sidebar.classList.remove('open');
    });

    // ============================================================
    // Auto-dismiss alerts after 5 seconds
    // ============================================================
    document.querySelectorAll('.alert-dismissible').forEach(el => {
        setTimeout(() => {
            const alert = bootstrap.Alert.getOrCreateInstance(el);
            alert.close();
        }, 5000);
    });

    // ============================================================
    // Theme toggle (persisted in localStorage)
    // ============================================================
    const THEME_KEY = 'hc_theme';
    const savedTheme = localStorage.getItem(THEME_KEY);
    if (savedTheme) document.documentElement.setAttribute('data-theme', savedTheme);

    window.hcSetTheme = function (theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem(THEME_KEY, theme);
    };

    // ============================================================
    // Follow toggle button (profile page)
    // ============================================================
    document.querySelectorAll('.js-follow-toggle').forEach(btn => {
        btn.addEventListener('click', async () => {
            const userId = btn.dataset.userId;
            const isFollowing = btn.dataset.following === 'true';
            const url = isFollowing ? `/Profile/Unfollow/${userId}` : `/Profile/Follow/${userId}`;

            btn.disabled = true;
            try {
                const res = await window.hcPost(url, {});
                if (res.ok) {
                    // Reload to reflect counter and state changes
                    location.reload();
                } else {
                    alert('Action failed. Please try again.');
                }
            } catch {
                alert('Network error.');
            } finally {
                btn.disabled = false;
            }
        });
    });

    // ============================================================
    // Composer character counter
    // ============================================================
    document.querySelectorAll('textarea[data-maxlength]').forEach(ta => {
        const max = parseInt(ta.dataset.maxlength, 10);
        const form = ta.closest('form');
        const counter = form?.querySelector('.composer-counter span');
        if (!counter) return;

        const update = () => { counter.textContent = ta.value.length; };
        ta.addEventListener('input', update);
        update();
    });

    // ============================================================
    // Composer media preview
    // ============================================================
    document.querySelectorAll('.composer-tool input[type="file"]').forEach(input => {
        input.addEventListener('change', e => {
            const preview = input.closest('form')?.querySelector('.composer-preview');
            if (!preview) return;
            preview.innerHTML = '';
            Array.from(e.target.files).slice(0, 4).forEach(file => {
                if (!file.type.startsWith('image/')) return;
                const img = document.createElement('img');
                img.src = URL.createObjectURL(file);
                preview.appendChild(img);
            });
        });
    });

})();