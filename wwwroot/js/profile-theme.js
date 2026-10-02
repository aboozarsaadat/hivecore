/* ============================================================
   HiveCore — apply user's profile theme to profile pages
   ============================================================ */
(function () {
    'use strict';

    const profilePage = document.querySelector('.profile-page[data-theme-json]');
    if (!profilePage) return;

    let theme;
    try {
        theme = JSON.parse(profilePage.dataset.themeJson);
    } catch {
        return;
    }

    const root = profilePage;
    if (theme.primaryColor) root.style.setProperty('--profile-primary', theme.primaryColor);
    if (theme.secondaryColor) root.style.setProperty('--profile-secondary', theme.secondaryColor);
    if (theme.accentColor) root.style.setProperty('--profile-accent', theme.accentColor);
    if (theme.backgroundColor) root.style.setProperty('--profile-bg', theme.backgroundColor);
    if (theme.textColor) root.style.setProperty('--profile-text', theme.textColor);
    if (theme.fontFamily) root.style.setProperty('--profile-font', theme.fontFamily);

    if (theme.backgroundMode === 'gradient' && theme.backgroundGradient) {
        const info = root.querySelector('.profile-info');
        if (info) info.style.background = theme.backgroundGradient;
    }

    if (theme.backgroundMode === 'image' && theme.backgroundImageUrl) {
        const cover = root.querySelector('.profile-cover');
        if (cover) cover.style.backgroundImage = `url(${theme.backgroundImageUrl})`;
    }

    if (theme.layoutStyle) root.dataset.layout = theme.layoutStyle;

    // Music player (auto-mount if a track is provided)
    const tracksRaw = profilePage.dataset.musicTracks;
    if (tracksRaw && theme.showMusicPlayer !== false) {
        try {
            const tracks = JSON.parse(tracksRaw);
            if (Array.isArray(tracks) && tracks.length > 0) {
                mountMusicPlayer(root, tracks[0]);
            }
        } catch { /* ignore */ }
    }

    function mountMusicPlayer(root, track) {
        const info = root.querySelector('.profile-info');
        if (!info) return;

        const audio = new Audio(track.audioUrl || track.externalUrl || '');
        audio.loop = false;

        const wrapper = document.createElement('div');
        wrapper.className = 'profile-music';
        wrapper.innerHTML = `
            <img class="profile-music-cover" src="${track.coverUrl || '/images/default-avatar.png'}" alt="" />
            <div class="profile-music-info">
                <div class="profile-music-title">${escape(track.title || 'Untitled')}</div>
                <div class="profile-music-artist">${escape(track.artist || 'Unknown artist')}</div>
            </div>
            <button type="button" class="profile-music-btn" aria-label="Play">
                <i class="bi bi-play-fill"></i>
            </button>
        `;

        const btn = wrapper.querySelector('.profile-music-btn');
        btn.addEventListener('click', () => {
            const icon = btn.querySelector('i');
            if (audio.paused) {
                audio.play().catch(() => {});
                if (icon) icon.className = 'bi bi-pause-fill';
            } else {
                audio.pause();
                if (icon) icon.className = 'bi bi-play-fill';
            }
        });

        audio.addEventListener('ended', () => {
            const icon = btn.querySelector('i');
            if (icon) icon.className = 'bi bi-play-fill';
        });

        info.appendChild(wrapper);
    }

    function escape(s) {
        return String(s).replace(/[&<>"']/g, c => ({
            '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
        }[c]));
    }
})();