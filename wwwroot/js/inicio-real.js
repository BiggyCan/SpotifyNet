/* SpotifyNet, Bloque F: inicio conectado a SQLite. Sin interrumpir el audio. */
(() => {
    'use strict';

    let secuencia = 0;

    function nodo(tag, clase, texto) {
        const e = document.createElement(tag);
        if (clase) e.className = clase;
        if (texto !== undefined) e.textContent = String(texto);
        return e;
    }

    function enlace(texto, href, clase) {
        const e = nodo('a', clase, texto);
        e.href = typeof href === 'string' && href.startsWith('/') && !href.startsWith('//')
            ? href : '/Catalogo';
        return e;
    }

    function cabecera(titulo, textoLink, href) {
        const head = nodo('div', 'u-section-head sn-f-heading');
        head.append(nodo('h2', '', titulo), enlace(textoLink, href, 'sn-f-link'));
        return head;
    }

    function portada(url, clase) {
        const art = nodo('span', clase || 'sn-f-cover');
        art.textContent = '♫';
        if (typeof url === 'string' && /^\/(?!\/)/.test(url)) {
            const img = document.createElement('img');
            img.src = url;
            img.loading = 'lazy';
            img.alt = '';
            img.addEventListener('error', () => img.remove(), { once: true });
            art.append(img);
        }
        return art;
    }

    function token() {
        return document.querySelector('#globalAntiForgeryToken input[name="__RequestVerificationToken"]')?.value || '';
    }

    async function cambiarFavorito(id, agregar) {
        const form = new FormData();
        form.append('id', String(id));
        form.append('slug', 'tus-me-gusta');
        const respuesta = await fetch(agregar ? '/Biblioteca/AgregarALista' : '/Biblioteca/QuitarDeLista', {
            method: 'POST', credentials: 'same-origin',
            headers: { 'RequestVerificationToken': token() },
            body: form
        });
        const datos = await respuesta.json().catch(() => ({}));
        if (!respuesta.ok) throw new Error(datos.error || 'No se pudo modificar Tus me gusta.');
        return datos;
    }

    function misPlaylists(musicSections, areaLocal, listas) {
        // Si hay una sección previa de este bloque, la renovamos sin duplicarla.
        musicSections.querySelector('#snFPlaylists')?.remove();
        if (!listas.length) return;
        const section = nodo('section', 'u-music-section sn-f-playlist-section');
        section.id = 'snFPlaylists';
        section.append(cabecera('Mis playlists personales', 'Mi espacio', '/Social/MiEspacio'));
        const grid = nodo('div', 'sn-f-grid');
        listas.forEach(p => {
            const card = enlace('', p.url, 'sn-f-card');
            card.append(portada(p.cover, 'sn-f-playlist-cover'));
            card.append(nodo('strong', 'sn-f-card-name', p.name || 'Mi playlist'));
            card.append(nodo('small', 'sn-f-card-sub', 'Tu playlist personal'));
            grid.append(card);
        });
        section.append(grid);
        areaLocal.before(section);
    }

    function agregarLocalesTemporales(area) {
        // Conservar el botón original de carga local sin mezclarlo con
        // los archivos persistentes guardados en la base de datos.
        const preview = nodo('div', 'sn-f-temporales');
        const head = nodo('div', 'sn-f-temporales-head');
        head.append(nodo('strong', '', 'Archivos temporales del equipo'));
        const add = nodo('button', 'u-section-action', '+ Probar audio local');
        add.type = 'button';
        add.setAttribute('data-add-files', '');
        head.append(add);
        preview.append(head);
        preview.append(nodo('p', 'sn-f-counter',
            'Estos archivos se escuchan sin guardarse. Para conservar un MP3, súbelo a una playlist.'));
        const localList = nodo('div', 'u-uploaded-list');
        localList.id = 'uploadedSongs';
        preview.append(localList);
        area.append(preview);
    }

    function rellenarCanciones(area, data) {
        // Reemplazar solo la sección local de Inicio; el resto de la página
        // (incluidos playlists de ejemplo y controles) queda como estaba.
        area.replaceChildren();
        area.classList.add('sn-f-library');
        area.append(cabecera('Mis canciones guardadas', 'Abrir catálogo', '/Catalogo'));
        const actions = nodo('div', 'sn-f-actions');
        const texto = data.total === 1 ? '1 canción guardada en tu cuenta' :
            `${data.total || 0} canciones guardadas en tu cuenta`;
        actions.append(nodo('p', 'sn-f-counter', texto));
        actions.append(enlace('Gestionar mi música', '/Catalogo', 'sn-f-manage'));
        area.append(actions);

        const canciones = Array.isArray(data.canciones) ? data.canciones : [];
        if (!canciones.length) {
            const vacio = nodo('div', 'sn-f-empty');
            vacio.append(nodo('span', 'sn-f-empty-icon', '♫'));
            vacio.append(nodo('strong', '', 'Aún no has guardado canciones en esta cuenta'));
            vacio.append(nodo('p', '', 'Abre una playlist y pulsa «+ Añadir canciones» para guardar tus MP3.'));
            vacio.append(enlace('Abrir playlist CANO', '/Home/Playlist?slug=cano', 'sn-f-manage'));
            area.append(vacio);
            agregarLocalesTemporales(area);
            return;
        }

        const search = document.createElement('input');
        search.type = 'search';
        search.className = 'sn-f-search';
        search.placeholder = 'Buscar en mis canciones guardadas';
        search.setAttribute('aria-label', 'Buscar entre mis canciones');
        area.append(search);

        const list = nodo('div', 'sn-f-tracks');
        area.append(list);
        const showAll = enlace(`Ver todas las canciones (${data.total})`, '/Catalogo', 'sn-f-see-all');
        if (data.total > 12) area.append(showAll);

        const tracks = canciones.filter(t => t.url).map(t => ({
            id: t.id, title: t.title || 'Sin título',
            artist: t.artist || 'Archivo local',
            url: t.url, cover: t.cover || '', local: false
        }));

        function dibujar() {
            list.replaceChildren();
            const query = search.value.trim().toLocaleLowerCase('es');
            const filtradas = canciones.map((c, index) => ({ c, index }))
                .filter(({ c }) => !query ||
                    [c.title, c.artist, c.album, c.playlistName]
                        .some(v => String(v || '').toLocaleLowerCase('es').includes(query)));
            if (!filtradas.length) {
                list.append(nodo('p', 'sn-f-no-results', 'No hay resultados para esa búsqueda.'));
                return;
            }
            filtradas.slice(0, query ? 40 : 12).forEach(({ c, index }) => {
                const item = nodo('div', 'sn-f-track');
                const btn = nodo('button', 'sn-f-play', '▶');
                btn.type = 'button';
                btn.title = `Reproducir ${c.title || 'canción'}`;
                btn.setAttribute('aria-label', btn.title);
                btn.addEventListener('click', () => {
                    if (!window.SpotifyNetPlayer?.playTracks) return;
                    // Conservar el índice de la biblioteca completa, no de la búsqueda.
                    const target = tracks.findIndex(t => t.id === c.id);
                    if (target >= 0) window.SpotifyNetPlayer.playTracks(tracks, target);
                });
                const info = nodo('span', 'sn-f-song-info');
                info.append(nodo('strong', 'sn-f-song-title', c.title || 'Sin título'));
                info.append(nodo('small', 'sn-f-song-sub',
                    [c.artist || 'Archivo local', c.album || 'Sin álbum'].join(' · ')));
                item.append(portada(c.cover, 'sn-f-song-cover'), btn, info);
                const heart = nodo('button', 'sn-g-heart' + (c.liked ? ' is-liked' : ''), c.liked ? '♥' : '♡');
                heart.type = 'button';
                heart.title = c.liked ? 'Quitar de Tus me gusta' : 'Agregar a Tus me gusta';
                heart.setAttribute('aria-label', heart.title);
                heart.setAttribute('aria-pressed', String(Boolean(c.liked)));
                heart.addEventListener('click', async () => {
                    if (heart.disabled) return;
                    heart.disabled = true;
                    try {
                        await cambiarFavorito(c.id, !c.liked);
                        c.liked = !c.liked;
                        heart.classList.toggle('is-liked', Boolean(c.liked));
                        heart.textContent = c.liked ? '♥' : '♡';
                        heart.title = c.liked ? 'Quitar de Tus me gusta' : 'Agregar a Tus me gusta';
                        heart.setAttribute('aria-label', heart.title);
                        heart.setAttribute('aria-pressed', String(Boolean(c.liked)));
                    } catch (err) { alert(err.message || 'No se pudo guardar el favorito.'); }
                    finally { heart.disabled = false; }
                });
                item.append(heart);
                item.append(enlace(c.playlistName || 'Ver playlist', c.playlistUrl, 'sn-f-source'));
                list.append(item);
            });
            if (query && filtradas.length > 40) {
                list.append(nodo('small', 'sn-f-no-results',
                    'Se muestran los primeros 40 resultados. Busca en el catálogo para ver todos.'));
            }
        }
        search.addEventListener('input', dibujar);
        dibujar();
        agregarLocalesTemporales(area);
    }

    async function activar() {
        const music = document.getElementById('musicSections');
        const local = document.getElementById('localMusicSection');
        if (!music || !local) return;
        const ticket = ++secuencia;
        if (local.dataset.snFLoaded === '1') return;
        local.dataset.snFLoaded = '1';
        const label = nodo('p', 'sn-f-loading', 'Cargando tu biblioteca guardada…');
        local.replaceChildren(label);
        try {
            const resp = await fetch('/InicioReal/Datos', {
                credentials: 'same-origin', cache: 'no-store',
                headers: { 'Accept': 'application/json' }
            });
            if (!resp.ok) throw new Error('No se pudo consultar la biblioteca.');
            const data = await resp.json();
            if (ticket !== secuencia || !local.isConnected) return;
            misPlaylists(music, local, Array.isArray(data.playlists) ? data.playlists : []);
            rellenarCanciones(local, data);
        } catch (err) {
            if (ticket !== secuencia || !local.isConnected) return;
            local.replaceChildren();
            local.append(cabecera('Mis canciones guardadas', 'Abrir catálogo', '/Catalogo'));
            local.append(nodo('p', 'sn-f-error',
                'No se pudo cargar esta sección. Comprueba que el servidor esté funcionando.'));
            local.dataset.snFLoaded = '0';
            console.warn('SpotifyNet, Inicio:', err);
        }
    }

    // Al regresar mediante navegación parcial se vuelve a leer SQLite;
    // el audio de musicAudio no se reinicia.
    document.addEventListener('DOMContentLoaded', activar);
    document.addEventListener('spotifynet:navegacion', () => {
        ++secuencia;
        activar();
    });
})();
