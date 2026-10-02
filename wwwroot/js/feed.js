/* ============================================================
   HiveCore — feed infinite-load helpers
   ============================================================ */
(function () {
    'use strict';

    const loadMoreBtn = document.getElementById('loadMoreBtn');
    if (!loadMoreBtn) return;

    const feedList = document.getElementById('feedList');
    const feedType = loadMoreBtn.dataset.feed || 'for-you';
    let currentPage = parseInt(loadMoreBtn.dataset.page, 10) || 2;
    let loading = false;
    let hasMore = true;

    async function loadPage() {
        if (loading || !hasMore) return;
        loading = true;
        loadMoreBtn.disabled = true;
        loadMoreBtn.textContent = 'Loading…';

        try {
            const res = await fetch(`/?feedType=${feedType}&page=${currentPage}`, {
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!res.ok) throw new Error('Failed to load');
            const html = await res.text();

            // Parse the response and extract post cards
            const doc = new DOMParser().parseFromString(html, 'text/html');
            const newPosts = doc.querySelectorAll('#feedList .post-card');

            if (newPosts.length === 0) {
                hasMore = false;
                loadMoreBtn.textContent = 'No more posts';
                return;
            }

            newPosts.forEach(node => feedList.appendChild(node));
            currentPage++;

            // Update "Load more" state
            const newBtn = doc.getElementById('loadMoreBtn');
            if (!newBtn) {
                hasMore = false;
                loadMoreBtn.textContent = 'No more posts';
            } else {
                loadMoreBtn.dataset.page = currentPage;
                loadMoreBtn.textContent = 'Load more';
                loadMoreBtn.disabled = false;
            }
        } catch (err) {
            console.error(err);
            loadMoreBtn.textContent = 'Try again';
            loadMoreBtn.disabled = false;
        } finally {
            loading = false;
        }
    }

    loadMoreBtn.addEventListener('click', loadPage);

    // Infinite scroll (optional): load when near bottom
    if ('IntersectionObserver' in window) {
        const observer = new IntersectionObserver(entries => {
            if (entries[0].isIntersecting && hasMore) loadPage();
        }, { rootMargin: '400px' });
        observer.observe(loadMoreBtn);
    }
})();