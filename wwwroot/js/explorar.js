/* SpotifyNet: vista completa del buscador. Compatible con navegación sin cortar audio. */
(() => {
    'use strict';
    let secuencia = 0;
    const texto = (tag, clase, contenido) => {
        const e = document.createElement(tag);
        if (clase) e.className = clase;
        if (contenido !== undefined) e.textContent = contenido;
        return e;
    };
    function portada(src, clase) {
        if (typeof src !== 'string' || !src.startsWith('/') || src.startsWith('//')) {
            return texto('span', clase || 'sn-explore-image', '♫');
        }
        const img = document.createElement('img');
        img.src = src;
        img.alt = '';
        img.loading = 'lazy';
        img.className = clase || 'sn-explore-image';
        img.addEventListener('error', () => {
            const fallback = texto('span', clase || 'sn-explore-image', '♫');
            img.replaceWith(fallback);
        }, { once: true });
        return img;
    }
    function albumCard(item) {
        const link = document.createElement('a');
        link.className = 'sn-explore-card';
        link.href = item.url || '/Catalogo';
        link.append(portada(item.image), texto('strong', '', item.title || 'Sin nombre'),
            texto('small', '', item.subtitle || 'Tu biblioteca'));
        return link;
    }
    let listaCanciones = [];
    function reproducir(item) {
        if (!window.SpotifyNetPlayer?.playTracks || !item.audioUrl) return;
        const pistas = listaCanciones.filter(c => !!c.audioUrl).map(c => ({
            id: c.trackId, title: c.title || 'Sin título',
            artist: c.artist || 'Archivo local', cover: c.image || '',
            url: c.audioUrl, local: false
        }));
        const indice = pistas.findIndex(c => c.id === item.trackId);
        if (pistas.length) window.SpotifyNetPlayer.playTracks(pistas, Math.max(indice, 0));
    }
    function songRow(item) {
        const row = texto('div', 'sn-explore-track');
        const play = texto('button', '', '▶');
        play.type = 'button';
        play.title = 'Reproducir ' + item.title;
        play.setAttribute('aria-label', play.title);
        play.addEventListener('click', () => reproducir(item));
        const info = texto('span', 'sn-explore-info');
        info.append(texto('strong', '', item.title || 'Sin título'),
            texto('small', '', (item.artist || 'Archivo local') + ' · ' + (item.album || 'Sin álbum')));
        const link = texto('a', '', item.playlistName || 'Abrir playlist');
        link.href = item.url || '/';
        row.append(play, portada(item.image, 'sn-explore-tcover'), info, link);
        return row;
    }
    function section(titulo, tipo, items) {
        const wrapper = texto('section', 'sn-explore-section');
        wrapper.dataset.category = tipo;
        wrapper.append(texto('h2', '', titulo + ' (' + items.length + ')'));
        const list = texto('div', tipo === 'songs' ? 'sn-explore-tracklist' : 'sn-explore-grid');
        for (const item of items) list.append(tipo === 'songs' ? songRow(item) : albumCard(item));
        wrapper.append(list);
        return wrapper;
    }
    function aplicarFiltro(root) {
        const activo = root.querySelector('.sn-explore-tabs button.selected')?.dataset.exploreTab || 'all';
        root.querySelectorAll('.sn-explore-section').forEach(s => {
            s.hidden = activo !== 'all' && s.dataset.category !== activo;
        });
    }
    function renderizar(root, data, consulta) {
        const results = root.querySelector('#snExploreResults');
        const status = root.querySelector('#snExploreStatus');
        if (!results) return;
        results.replaceChildren();
        listaCanciones = Array.isArray(data.songs) ? data.songs : [];
        const grupos = [
            ['Canciones', 'songs', data.songs || []],
            ['Playlists', 'playlists', data.playlists || []],
            ['Artistas', 'artists', data.artists || []],
            ['Álbumes', 'albums', data.albums || []]
        ];
        const total = grupos.reduce((n, g) => n + g[2].length, 0);
        if (status) status.textContent = total ? total + ' resultados de «' + consulta + '»' : '';
        if (!total) {
            const empty = texto('div', 'sn-explore-empty');
            empty.append(texto('h2', '', 'No encontramos coincidencias'),
                texto('p', '', 'Comprueba la ortografía o busca otra canción, artista o playlist.'));
            results.append(empty);
            return;
        }
        for (const [titulo, tipo, items] of grupos) {
            if (items.length) results.append(section(titulo, tipo, items));
        }
        const full = texto('div', 'sn-explore-shortcuts');
        const catalog = texto('a', '', 'Ver toda mi biblioteca musical');
        catalog.href = '/Catalogo';
        full.append(catalog);
        results.append(full);
        aplicarFiltro(root);
    }
    function activar() {
        const root = document.getElementById('snExplore');
        if (!root || root.dataset.snExploreReady === '1') return;
        root.dataset.snExploreReady = '1';
        const q = (root.dataset.query || '').trim();
        if (q.length < 2) return;
        const status = root.querySelector('#snExploreStatus');
        if (status) status.textContent = 'Buscando «' + q + '»…';
        const actual = ++secuencia;
        fetch('/Buscar/Resultados?q=' + encodeURIComponent(q), { credentials: 'same-origin' })
            .then(res => { if (!res.ok) throw new Error('Búsqueda no disponible'); return res.json(); })
            .then(data => {
                if (actual !== secuencia || !document.contains(root)) return;
                renderizar(root, data, q);
            })
            .catch(() => {
                if (actual !== secuencia || !document.contains(root)) return;
                if (status) status.textContent = 'No se pudo completar la búsqueda. Vuelve a intentarlo.';
            });
    }
    document.addEventListener('submit', e => {
        if (e.target?.id !== 'snExploreForm') return;
        e.preventDefault();
        const input = e.target.querySelector('[name="q"]');
        const q = (input?.value || '').trim();
        if (q.length < 2) { input?.focus(); return; }
        const destino = '/Buscar/Explorar?q=' + encodeURIComponent(q);
        if (window.SpotifyNetNavigate) window.SpotifyNetNavigate(destino);
        else window.location.assign(destino);
    });
    document.addEventListener('click', e => {
        const tab = e.target.closest('[data-explore-tab]');
        const root = tab?.closest('#snExplore');
        if (!tab || !root) return;
        root.querySelectorAll('[data-explore-tab]').forEach(b => {
            const isCurrent = b === tab;
            b.classList.toggle('selected', isCurrent);
            b.setAttribute('aria-pressed', String(isCurrent));
        });
        aplicarFiltro(root);
    });
    document.addEventListener('DOMContentLoaded', activar);
    document.addEventListener('spotifynet:navegacion', activar);
    if (document.readyState !== 'loading') activar();
})();
