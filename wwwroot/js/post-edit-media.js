/* ============================================================
   HiveCore — media handling in post composer (create + edit)
   ============================================================ */
(function () {
    'use strict';

    document.querySelectorAll('.js-post-form').forEach(form => {
        const formId = form.dataset.formId;
        const textarea = form.querySelector('.js-composer-input');
        const fileInput = form.querySelector('.js-media-input');
        const preview = form.querySelector('.js-new-media-preview');
        const counterEl = form.querySelector('.js-counter-value');
        const deleteContainer = document.getElementById('mediaToDelete-' + formId);

        // ============================================================
        // Character counter
        // ============================================================
        const max = parseInt(textarea?.dataset.maxlength || '2000', 10);
        const updateCounter = () => {
            if (!counterEl || !textarea) return;
            const len = textarea.value.length;
            counterEl.textContent = len;
            counterEl.parentElement.classList.toggle(
                'warn', len > max * 0.85 && len <= max);
            counterEl.parentElement.classList.toggle(
                'danger', len > max);
        };
        textarea?.addEventListener('input', updateCounter);
        updateCounter();

        // ============================================================
        // Existing media — remove / undo
        // ============================================================
        form.querySelectorAll('.js-remove-media').forEach(btn => {
            btn.addEventListener('click', e => {
                e.preventDefault();
                const mediaId = btn.dataset.mediaId;
                const item = form.querySelector(
                    `.js-existing-item[data-media-id="${mediaId}"]`);
                if (!item) return;

                const isMarked = item.classList.toggle('marked-for-delete');
                const icon = btn.querySelector('i');
                if (icon) {
                    icon.className = isMarked ? 'bi bi-arrow-counterclockwise'
                                              : 'bi bi-x-lg';
                }

                // Sync hidden input
                let hidden = deleteContainer.querySelector(
                    `input[value="${mediaId}"]`);

                if (isMarked) {
                    if (!hidden) {
                        hidden = document.createElement('input');
                        hidden.type = 'hidden';
                        hidden.name = 'MediaIdsToDelete';
                        hidden.value = mediaId;
                        deleteContainer.appendChild(hidden);
                    }
                } else if (hidden) {
                    hidden.remove();
                }
            });
        });

        // ============================================================
        // New media preview
        // ============================================================
        fileInput?.addEventListener('change', () => {
            if (!preview) return;
            preview.innerHTML = '';

            const files = Array.from(fileInput.files || []);
            const remainingSlots = 4 - form.querySelectorAll('.js-existing-item:not(.marked-for-delete)').length;
            const allowed = files.slice(0, Math.max(0, remainingSlots));

            allowed.forEach(file => {
                if (!file.type.startsWith('image/') && !file.type.startsWith('video/')) return;

                const wrap = document.createElement('div');
                wrap.className = 'composer-preview-item';

                if (file.type.startsWith('image/')) {
                    const img = document.createElement('img');
                    img.src = URL.createObjectURL(file);
                    img.alt = file.name;
                    wrap.appendChild(img);
                } else {
                    const video = document.createElement('video');
                    video.src = URL.createObjectURL(file);
                    video.muted = true;
                    wrap.appendChild(video);
                }

                preview.appendChild(wrap);
            });

            if (files.length > allowed.length) {
                alert(`You can add at most ${remainingSlots} more media item(s) to this post.`);
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