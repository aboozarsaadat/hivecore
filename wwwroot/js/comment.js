/* ============================================================
   HiveCore — comment submission (AJAX)
   ============================================================ */
(function () {
    'use strict';

    document.querySelectorAll('.js-comment-form').forEach(form => {
        form.addEventListener('submit', async e => {
            e.preventDefault();

            const textarea = form.querySelector('textarea');
            const submitBtn = form.querySelector('button[type="submit"]');
            const content = textarea?.value.trim();

            if (!content) return;

            submitBtn.disabled = true;
            const originalText = submitBtn.innerHTML;
            submitBtn.innerHTML = '<i class="bi bi-hourglass-split"></i> Sending…';

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
                    alert(err.error || 'Failed to post comment.');
                    return;
                }

                const data = await res.json();
                textarea.value = '';

                // Reload to show the new comment (simplest approach)
                location.reload();
            } catch {
                alert('Network error. Please try again.');
            } finally {
                submitBtn.disabled = false;
                submitBtn.innerHTML = originalText;
            }
        });
    });

    // ============================================================
    // Reply button focuses the form (minimal UX improvement)
    // ============================================================
    document.addEventListener('click', e => {
        const btn = e.target.closest('.js-reply-comment');
        if (!btn) return;

        const commentId = btn.dataset.id;
        const form = document.querySelector('.js-comment-form');
        if (!form) return;

        const parentInput = form.querySelector('input[name="ParentCommentId"]');
        if (parentInput) parentInput.value = commentId;

        form.querySelector('textarea')?.focus();
        form.scrollIntoView({ behavior: 'smooth', block: 'center' });
    });
})();