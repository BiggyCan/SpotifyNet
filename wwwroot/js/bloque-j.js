/* SpotifyNet · Bloque J: controles de teclado y volumen persistente.
   No cambia datos de cuenta ni intercepta formularios. */
(() => {
    'use strict';
    const KEY = 'spotifynet.volumen.v1';
    const audio = document.getElementById('musicAudio');
    const volume = document.getElementById('volumeControl');
    const play = document.getElementById('playButton');
    if (!audio) return;
    let previo = 70;
    try {
        const saved = Number(localStorage.getItem(KEY));
        if (localStorage.getItem(KEY) !== null && Number.isFinite(saved)) {
            if (saved > 0) previo = Math.max(1, Math.min(100, saved));
            audio.volume = Math.max(0, Math.min(100, saved)) / 100;
            if (volume) volume.value = String(saved);
        }
    } catch { /* El navegador puede bloquear almacenamiento; el audio sigue funcionando. */ }

    volume?.addEventListener('input', () => {
        const n = Number(volume.value);
        if (n > 0) previo = n;
        try { localStorage.setItem(KEY, String(n)); } catch { /* sin persistencia */ }
    });

    // Solo activamos los atajos sin foco en campos de entrada.
    document.addEventListener('keydown', event => {
        if (event.altKey || event.metaKey || event.shiftKey) return;
        const el = event.target;
        const editando = el instanceof Element && Boolean(el.closest('input,textarea,select,[contenteditable="true"],[role="dialog"]'));
        if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
            event.preventDefault();
            const search = document.getElementById('userSearch');
            search?.focus();
            search?.select?.();
            return;
        }
        if (event.ctrlKey || editando || event.repeat) return;
        if ((event.key === ' ' || event.code === 'Space') && el === document.body) {
            if (!play?.disabled) {
                event.preventDefault();
                play.click();
            }
        } else if (event.key.toLowerCase() === 'm' && volume) {
            if (audio.volume > 0) {
                previo = Number(volume.value) || previo;
                volume.value = '0';
            } else {
                volume.value = String(previo || 70);
            }
            volume.dispatchEvent(new Event('input', { bubbles: true }));
        }
    });

    // El player sigue siendo el mismo <audio> entre vistas internas.
    const actualizar = () => {
        if (!('mediaSession' in navigator)) return;
        try {
            const title = document.getElementById('playerTitle')?.textContent?.trim();
            const artist = document.getElementById('playerArtist')?.textContent?.trim();
            if (title && title !== 'Sin reproducción') {
                navigator.mediaSession.metadata = new MediaMetadata({
                    title, artist: artist || 'SpotifyNet', album: 'Tu biblioteca'
                });
            }
            navigator.mediaSession.playbackState = audio.paused ? 'paused' : 'playing';
        } catch { /* integración del navegador no soportada */ }
    };
    audio.addEventListener('play', actualizar);
    audio.addEventListener('pause', actualizar);
    audio.addEventListener('loadedmetadata', actualizar);
    if ('mediaSession' in navigator) {
        try {
            navigator.mediaSession.setActionHandler('play', () => { void audio.play(); });
            navigator.mediaSession.setActionHandler('pause', () => audio.pause());
            navigator.mediaSession.setActionHandler('previoustrack', () => document.getElementById('previousButton')?.click());
            navigator.mediaSession.setActionHandler('nexttrack', () => document.getElementById('nextButton')?.click());
        } catch { /* navegador sin compatibilidad completa */ }
    }
})();
