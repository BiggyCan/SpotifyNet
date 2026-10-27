/* SpotifyNet: buscador unificado, reproducción directa y navegación con teclado. */
(() => {
    'use strict';
    const input = document.getElementById('userSearch');
    const contenedor = input?.closest('.u-search');
    if (!input || !contenedor || input.dataset.snSearchReady === '1') return;
    input.dataset.snSearchReady = '1';
    input.setAttribute('autocomplete', 'off');
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-expanded', 'false');
    input.setAttribute('aria-controls', 'buscadorResultados');
    input.setAttribute('aria-label', 'Buscar canciones, playlists, artistas y álbumes');

    const panel = document.createElement('div');
    panel.id = 'buscadorResultados';
    panel.className = 'b-search-results';
    panel.setAttribute('role', 'listbox');
    panel.setAttribute('aria-label', 'Resultados de la búsqueda');
    panel.hidden = true;
    contenedor.append(panel);
    let timer = 0;
    let peticion = null;
    let secuencia = 0;
    let opcionActiva = -1;

    function abrir() { panel.hidden = false; input.setAttribute('aria-expanded', 'true'); }
    function cerrar() {
        panel.hidden = true;
        input.setAttribute('aria-expanded', 'false');
        opcionActiva = -1;
        input.removeAttribute('aria-activedescendant');
    }
    function cancelar() {
        secuencia++;
        clearTimeout(timer);
        if (peticion) { peticion.abort(); peticion = null; }
    }
    function aviso(mensaje) {
        panel.replaceChildren();
        const texto = document.createElement('p');
        texto.className = 'b-search-message';
        texto.textContent = mensaje;
        panel.append(texto);
        abrir();
    }
    function label(texto) {
        const el = document.createElement('div');
        el.className = 'b-search-title';
        el.textContent = texto;
        return el;
    }
    function portada(url) {
        const art = document.createElement('span');
        art.className = 'b-search-cover';
        art.textContent = '♫';
        if (typeof url === 'string' && url.startsWith('/') && !url.startsWith('//')) {
            const img = new Image();
            img.src = url;
            img.alt = '';
            img.loading = 'lazy';
            img.addEventListener('error', () => img.remove(), { once: true });
            art.append(img);
        }
        return art;
    }
    function informacion(item, subtitulo) {
        const cont = document.createElement('span');
        cont.className = 'b-search-info';
        const titulo = document.createElement('strong');
        titulo.textContent = item.title || 'Sin título';
        const desc = document.createElement('small');
        desc.textContent = subtitulo || '';
        cont.append(titulo, desc);
        return cont;
    }
    function link(item, tipo) {
        const a = document.createElement('a');
        a.className = 'b-search-result';
        a.href = item.url || '/';
        a.setAttribute('role', 'option');
        a.setAttribute('aria-selected', 'false');
        a.append(portada(item.image), informacion(item, item.subtitle));
        const tag = document.createElement('span');
        tag.className = 'b-search-type';
        tag.textContent = tipo;
        a.append(tag);
        return a;
    }
    let cancionesEncontradas = [];
    function play(cancion) {
        if (!cancion.audioUrl || !window.SpotifyNetPlayer?.playTracks) {
            // Ante un error del reproductor, la playlist sigue siendo accesible.
            window.SpotifyNetNavigate?.(cancion.url);
            return;
        }
        const lista = cancionesEncontradas.length ? cancionesEncontradas : [cancion];
        const pistas = lista.filter(item => !!item.audioUrl).map(item => ({
            id: item.trackId,
            title: item.title,
            artist: item.artist || 'Archivo local',
            url: item.audioUrl,
            cover: item.image,
            local: false
        }));
        const indice = pistas.findIndex(p => p.id === cancion.trackId);
        window.SpotifyNetPlayer.playTracks(pistas, Math.max(0, indice));
        cerrar();
    }
    function song(cancion) {
        const row = document.createElement('div');
        row.className = 'b-search-song';
        const btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'b-search-result b-search-play';
        btn.setAttribute('role', 'option');
        btn.setAttribute('aria-selected', 'false');
        btn.title = 'Reproducir ' + cancion.title;
        btn.append(portada(cancion.image),
            informacion(cancion, (cancion.artist || 'Archivo local') + ' · ' + (cancion.album || 'Sin álbum')));
        const ico = document.createElement('span');
        ico.className = 'b-search-play-icon';
        ico.textContent = '▶';
        btn.append(ico);
        btn.addEventListener('click', () => play(cancion));
        const abrirLista = document.createElement('a');
        abrirLista.className = 'b-search-open-playlist';
        abrirLista.href = cancion.url || '/';
        abrirLista.title = 'Abrir playlist ' + (cancion.playlistName || '');
        abrirLista.setAttribute('aria-label', abrirLista.title);
        abrirLista.textContent = '↗';
        row.append(btn, abrirLista);
        return row;
    }
    function imprimir(datos, termino) {
        panel.replaceChildren();
        cancionesEncontradas = Array.isArray(datos.songs) ? datos.songs : [];
        const grupos = [
            ['Canciones', datos.songs, song],
            ['Playlists', datos.playlists, x => link(x, 'Playlist')],
            ['Artistas', datos.artists, x => link(x, 'Artista')],
            ['Álbumes', datos.albums, x => link(x, 'Álbum')]
        ];
        let total = 0;
        for (const [titulo, items, construir] of grupos) {
            if (!Array.isArray(items) || !items.length) continue;
            total += items.length;
            panel.append(label(titulo));
            const max = titulo === 'Canciones' ? 5 : 4;
            for (const item of items.slice(0, max)) panel.append(construir(item));
        }
        if (!total) {
            aviso('Sin coincidencias en tu música. Prueba otro nombre o artista.');
            return;
        }
        const todos = document.createElement('a');
        todos.className = 'b-search-see-all';
        todos.href = '/Buscar/Explorar?q=' + encodeURIComponent(termino);
        todos.textContent = 'Ver todos los resultados de «' + termino + '»';
        panel.append(todos);
        const ayuda = document.createElement('div');
        ayuda.className = 'b-search-footer';
        ayuda.textContent = '▶ Reproduce desde aquí · ↗ Abre la playlist · ↑ ↓ Navega';
        panel.append(ayuda);
        abrir();
    }
    async function consultar(texto) {
        const actual = ++secuencia;
        if (peticion) peticion.abort();
        peticion = new AbortController();
        aviso('Buscando en tu biblioteca…');
        try {
            const response = await fetch('/Buscar/Resultados?q=' + encodeURIComponent(texto),
                { credentials: 'same-origin', signal: peticion.signal });
            if (!response.ok) throw new Error('La búsqueda no está disponible');
            const datos = await response.json();
            if (actual !== secuencia || input.value.trim() !== texto) return;
            imprimir(datos, texto);
        } catch (error) {
            if (error.name !== 'AbortError' && actual === secuencia) aviso('No pudimos buscar. Inténtalo otra vez.');
        }
    }
    function opciones() { return Array.from(panel.querySelectorAll('.b-search-result')); }
    function mover(direccion) {
        const items = opciones();
        if (!items.length) return;
        opcionActiva = (opcionActiva + direccion + items.length) % items.length;
        items.forEach((item, i) => {
            const activo = i === opcionActiva;
            item.classList.toggle('b-search-active', activo);
            item.setAttribute('aria-selected', String(activo));
            if (activo) { item.id = 'bSearchSelected'; item.scrollIntoView({ block: 'nearest' }); }
            else if (item.id === 'bSearchSelected') item.removeAttribute('id');
        });
        input.setAttribute('aria-activedescendant', 'bSearchSelected');
    }
    // Captura: evita que el buscador antiguo filtre y oculte tarjetas de la biblioteca.
    input.addEventListener('input', event => {
        event.stopImmediatePropagation();
        cancelar();
        opcionActiva = -1;
        const q = input.value.trim();
        if (!q) { cerrar(); panel.replaceChildren(); }
        else if (q.length < 2) aviso('Escribe al menos dos caracteres.');
        else timer = setTimeout(() => consultar(q), 220);
    }, true);
    input.addEventListener('focus', () => {
        const q = input.value.trim();
        if (q.length >= 2) { cancelar(); consultar(q); }
    });
    input.addEventListener('keydown', event => {
        if (event.key === 'Escape') { cancelar(); cerrar(); input.blur(); return; }
        if (event.key === 'ArrowDown' && !panel.hidden) { event.preventDefault(); mover(1); }
        if (event.key === 'ArrowUp' && !panel.hidden) { event.preventDefault(); mover(-1); }
        if (event.key === 'Enter' && input.value.trim().length >= 2) {
            event.preventDefault();
            const seleccionado = opciones()[opcionActiva];
            if (seleccionado) seleccionado.click();
            else {
                const url = '/Buscar/Explorar?q=' + encodeURIComponent(input.value.trim());
                if (window.SpotifyNetNavigate) window.SpotifyNetNavigate(url);
                else window.location.assign(url);
            }
        }
    });
    document.addEventListener('pointerdown', e => {
        if (!contenedor.contains(e.target)) cerrar();
    });
    document.addEventListener('spotifynet:navegacion', () => {
        cancelar(); input.value = ''; panel.replaceChildren(); cerrar();
    });
})();
