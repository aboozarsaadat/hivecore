/* ============================================================
   HiveCore — composer inline submit + media preview
   ============================================================ */
(function () {
    'use strict';

    document.querySelectorAll('.js-composer').forEach(form => {
        const textarea = form.querySelector('.js-composer-input');
        const fileInput = form.querySelector('.js-composer-file');
        const preview = form.querySelector('.js-composer-preview');
        const counterEl = form.querySelector('.js-counter-value');
        const submitBtn = form.querySelector('.js-composer-submit');
        const sensitiveToggle = form.querySelector('.js-sensitive-toggle');
        const sensitiveBadge = form.querySelector('.js-sensitive-badge');

        // ============================================================
        // Character counter
        // ============================================================
        const max = parseInt(textarea?.dataset.maxlength || '2000', 10);
        const updateCounter = () => {
            if (!counterEl || !textarea) return;
            const len = textarea.value.length;
            counterEl.textContent = len;
            counterEl.parentElement.classList.toggle('warn', len > max * 0.85 && len <= max);
            counterEl.parentElement.classList.toggle('danger', len > max);
        };
        textarea?.addEventListener('input', updateCounter);
        updateCounter();

        // ============================================================
        // Media preview
        // ============================================================
        fileInput?.addEventListener('change', e => {
            if (!preview) return;
            preview.innerHTML = '';
            const files = Array.from(e.target.files).slice(0, 4);
            files.forEach(file => {
                if (file.type.startsWith('image/')) {
                    const wrap = document.createElement('div');
                    wrap.className = 'composer-preview-item';
                    const img = document.createElement('img');
                    img.src = URL.createObjectURL(file);
                    wrap.appendChild(img);
                    preview.appendChild(wrap);
                } else if (file.type.startsWith('video/')) {
                    const wrap = document.createElement('div');
                    wrap.className = 'composer-preview-item';
                    const video = document.createElement('video');
                    video.src = URL.createObjectURL(file);
                    video.muted = true;
                    wrap.appendChild(video);
                    preview.appendChild(wrap);
                }
            });
        });

        // ============================================================
        // Sensitive toggle
        // ============================================================
        sensitiveToggle?.addEventListener('change', () => {
            if (!sensitiveBadge) return;
            sensitiveBadge.style.display = sensitiveToggle.checked ? 'flex' : 'none';
        });

        // ============================================================
        // Submit via AJAX
        // ============================================================
        form.addEventListener('submit', async e => {
            e.preventDefault();

            const content = textarea?.value.trim() || '';
            const hasFiles = fileInput?.files?.length > 0;

            if (!content && !hasFiles) {
                textarea?.focus();
                return;
            }

            const originalHtml = submitBtn.innerHTML;
            submitBtn.disabled = true;
            submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i> Posting…';

            const formData = new FormData(form);

            try {
                const res = await fetch(form.action, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest',
                        'RequestVerificationToken': window.hcCsrfToken()
                    },
                    body: formData
                });

                if (!res.ok) {
                    const err = await res.json().catch(() => ({}));
                    alert(err.error || err.title || 'Failed to publish post.');
                    return;
                }

                const data = await res.json();

                // Reset form
                textarea.value = '';
                fileInput.value = '';
                if (preview) preview.innerHTML = '';
                if (sensitiveToggle) sensitiveToggle.checked = false;
                if (sensitiveBadge) sensitiveBadge.style.display = 'none';
                updateCounter();

                // Prepend new post card if we're on a feed page
                const feedList = document.getElementById('feedList');
                if (feedList && data.post) {
                    // Reload to render the server-rendered partial
                    location.reload();
                } else {
                    // Redirect to the post details
                    location.href = `/Post/${data.post.id}`;
                }
            } catch {
                alert('Network error. Please try again.');
            } finally {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalHtml;
            }
        });

        // ============================================================
        // Auto-resize textarea
        // ============================================================
        const autoGrow = () => {
            if (!textarea) return;
            textarea.style.height = 'auto';
            textarea.style.height = Math.min(textarea.scrollHeight, 320) + 'px';
        };
        textarea?.addEventListener('input', autoGrow);
        autoGrow();
    });
})();