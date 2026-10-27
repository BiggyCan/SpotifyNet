/* SpotifyNet · Mixes diarios reales, sin selector de archivos ni catálogo externo.
   Funciona también tras navegación interna AJAX sin reiniciar el reproductor. */
(() => {
    'use strict';

    const descripciones = [
        'Tus canciones favoritas y el resto de tu biblioteca',
        'Tus canciones guardadas recientemente',
        'Una combinación diferente de tus canciones',
        'Tus canciones organizadas por artista',
        'Tu colección ordenada de otra manera',
        'Una mezcla sorpresa de tu biblioteca'
    ];
    let canciones = null;
    let peticion = null;
    let revision = 0;
    let avisoTimer;
    const esUrlLocal = url => typeof url === 'string' && url.startsWith('/') && !url.startsWith('//');

    function hash(cadena) {
        let h = 2166136261;
        for (let i = 0; i < cadena.length; i++) {
            h ^= cadena.charCodeAt(i);
            h = Math.imul(h, 16777619);
        }
        return h >>> 0;
    }

    function mezclar(lista, semilla) {
        return lista.slice().sort((a, b) =>
            hash(semilla + a.id) - hash(semilla + b.id) || a.title.localeCompare(b.title, 'es'));
    }

    function crearOrden(numero, lista) {
        const todos = lista.slice();
        if (numero === 0) {
            const meGusta = todos.filter(c => c.liked);
            return meGusta.length
                ? [...mezclar(meGusta, 'favoritos'), ...mezclar(todos.filter(c => !c.liked), 'restantes')]
                : mezclar(todos, 'mix-favoritos');
        }
        if (numero === 1) return todos;  // API entrega primero las subidas recientes.
        if (numero === 2) return mezclar(todos, 'descubrimiento');
        if (numero === 3) return todos.sort((a, b) =>
            a.artist.localeCompare(b.artist, 'es') || a.title.localeCompare(b.title, 'es'));
        if (numero === 4) return todos.sort((a, b) => b.title.localeCompare(a.title, 'es'));
        return mezclar(todos, 'sorpresa-' + new Date().toISOString().slice(0, 10));
    }

    async function cargar() {
        if (canciones) return canciones;
        if (peticion) return peticion;
        peticion = fetch('/InicioReal/Datos', {
            credentials: 'same-origin', cache: 'no-store', headers: { Accept: 'application/json' }
        }).then(async r => {
            if (!r.ok) throw new Error('No se pudieron consultar las canciones guardadas.');
            const data = await r.json();
            const encontrados = Array.isArray(data.canciones) ? data.canciones : [];
            const ids = new Set();
            canciones = encontrados.filter(c => {
                if (!c || !esUrlLocal(c.url)) return false;
                const id = String(c.id || c.url);
                if (ids.has(id)) return false;
                ids.add(id);
                return true;
            }).map((c, i) => ({
                id: String(c.id || `archivo-${i}`),
                title: String(c.title || 'Sin título'),
                artist: String(c.artist || 'Archivo local'),
                album: String(c.album || 'Sin álbum'),
                cover: esUrlLocal(c.cover) ? c.cover : '',
                url: c.url, liked: Boolean(c.liked), local: false
            }));
            return canciones;
        }).finally(() => { peticion = null; });
        return peticion;
    }

    function notificar(texto) {
        let n = document.getElementById('snIMixesNotice');
        if (!n) {
            n = document.createElement('div');
            n.id = 'snIMixesNotice';
            n.className = 'sn-i-notice';
            n.setAttribute('role', 'status');
            n.setAttribute('aria-live', 'polite');
            document.body.append(n);
        }
        n.textContent = texto;
        n.classList.add('is-visible');
        clearTimeout(avisoTimer);
        avisoTimer = setTimeout(() => n.classList.remove('is-visible'), 3500);
    }

    function buscarTarjetas() {
        const seccion = [...document.querySelectorAll('#musicSections .u-music-section')]
            .find(s => (s.querySelector('.u-section-head h2')?.textContent || '').includes('Hecho para'));
        return seccion ? [...seccion.querySelectorAll('.u-music-grid .u-music-card')].slice(0, 6) : [];
    }

    function prepararTarjetas() {
        buscarTarjetas().forEach((card, i) => {
            card.removeAttribute('data-add-files'); // Arregla la apertura errónea del selector de MP3.
            card.dataset.mixReal = String(i);
            if (card.tagName === 'A') card.href = `/Mixes/Detalle?numero=${i + 1}`;
            card.classList.add('sn-i-mix-card');
            card.title = `Abrir y reproducir Mix diario ${i + 1}`;
            card.setAttribute('aria-label', card.title);
            const sub = card.querySelector(':scope > small');
            if (sub) sub.textContent = canciones
                ? (canciones.length ? `${descripciones[i]} · ${canciones.length} canciones` : 'Añade música para crear este mix')
                : descripciones[i];
        });
    }

    async function reproducir(numero, desde = 0) {
        if (numero < 0 || numero > 5) return;
        try {
            const lista = crearOrden(numero, await cargar());
            if (!lista.length) {
                notificar('No tienes canciones guardadas en esta cuenta. Añade MP3 a una playlist para crear mixes.');
                return;
            }
            const player = window.SpotifyNetPlayer;
            if (typeof player?.playTracks !== 'function') {
                notificar('El reproductor todavía no está disponible. Recarga la página.');
                return;
            }
            player.playTracks(lista, Math.min(Math.max(desde, 0), lista.length - 1));
            notificar(`Mix diario ${numero + 1} · ${lista.length} canciones de tu cuenta`);
        } catch (error) {
            console.error('SpotifyNet / Mixes:', error);
            notificar('No pudimos cargar tu mix. Comprueba que hayas iniciado sesión.');
        }
    }

    async function encolar(numero) {
        try {
            const lista = crearOrden(numero, await cargar());
            if (!lista.length) return notificar('No tienes canciones para añadir.');
            if (typeof window.SpotifyNetPlayer?.queueTrack !== 'function') return;
            lista.forEach(c => window.SpotifyNetPlayer.queueTrack(c));
            notificar(`${lista.length} canciones añadidas a la cola`);
        } catch (error) {
            console.error(error);
            notificar('No se pudo actualizar la cola.');
        }
    }

    function elemento(tipo, clase, texto) {
        const n = document.createElement(tipo);
        if (clase) n.className = clase;
        if (texto !== undefined) n.textContent = String(texto);
        return n;
    }

    function fila(cancion, i) {
        const item = elemento('div', 'sn-j-song');
        const index = elemento('span', 'sn-j-song-index', i + 1);
        const cover = elemento('span', 'sn-j-song-cover', '♫');
        if (cancion.cover) {
            const img = document.createElement('img');
            img.src = cancion.cover;
            img.alt = '';
            img.loading = 'lazy';
            cover.replaceChildren(img);
        }
        const datos = elemento('span', 'sn-j-song-info');
        datos.append(elemento('strong', '', cancion.title), elemento('small', '', cancion.artist));
        const play = elemento('button', 'sn-j-song-play', '▶');
        play.type = 'button';
        play.dataset.mixPlay = String(i);
        play.title = `Reproducir ${cancion.title}`;
        play.setAttribute('aria-label', play.title);
        const queue = elemento('button', 'sn-j-song-queue', '+');
        queue.type = 'button';
        queue.dataset.mixQueue = String(i);
        queue.title = `Añadir ${cancion.title} a la cola`;
        queue.setAttribute('aria-label', queue.title);
        item.append(index, cover, datos, play, queue);
        return item;
    }

    async function pintarDetalle() {
        const page = document.getElementById('snMixPage');
        if (!page) return;
        const numero = Number(page.dataset.mixIndex);
        if (!Number.isInteger(numero) || numero < 0 || numero > 5) return;
        const turno = ++revision;
        const lista = page.querySelector('#snMixSongs');
        page.querySelector('#snMixDescription').textContent = descripciones[numero] + '.';
        try {
            const orden = crearOrden(numero, await cargar());
            if (turno !== revision || !page.isConnected) return;
            page.querySelector('#snMixCount').textContent = `${orden.length} canciones guardadas`;
            page.querySelector('#snMixState').textContent = orden.length ? 'Pulsa una canción para reproducirla.' : '';
            if (!orden.length) {
                const vacío = elemento('div', 'sn-j-mix-empty');
                vacío.append(elemento('p', '', 'Aún no tienes canciones para este mix.'));
                const link = elemento('a', 'sn-j-secondary', 'Abrir mis playlists');
                link.href = '/';
                vacío.append(link);
                lista.replaceChildren(vacío);
            } else {
                const frag = document.createDocumentFragment();
                orden.forEach((c, i) => frag.append(fila(c, i)));
                lista.replaceChildren(frag);
            }
        } catch (error) {
            if (turno !== revision || !page.isConnected) return;
            lista.replaceChildren(elemento('p', 'sn-j-mix-empty', 'No pudimos consultar tu biblioteca. Comprueba la sesión y vuelve a cargar.'));
            page.querySelector('#snMixCount').textContent = 'No disponible';
            console.error(error);
        }
    }

    // Interceptamos en window/captura ANTES de las acciones heredadas para archivos.
    window.addEventListener('click', event => {
        if (event.defaultPrevented || event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        const target = event.target;
        if (!(target instanceof Element)) return;
        const card = target.closest('#musicSections .u-music-card[data-mix-real]');
        if (card) {
            const numero = Number(card.dataset.mixReal);
            if (!Number.isInteger(numero) || numero < 0 || numero > 5) return;
            event.preventDefault();
            event.stopImmediatePropagation();
            void reproducir(numero);
            const url = `/Mixes/Detalle?numero=${numero + 1}`;
            if (typeof window.SpotifyNetNavigate === 'function') void window.SpotifyNetNavigate(url);
            else window.location.assign(url);
            return;
        }
        const page = target.closest('#snMixPage');
        if (!page) return;
        const numero = Number(page.dataset.mixIndex);
        const accion = target.closest('[data-mix-action]');
        const play = target.closest('[data-mix-play]');
        const queue = target.closest('[data-mix-queue]');
        if (!accion && !play && !queue) return;
        event.preventDefault();
        event.stopImmediatePropagation();
        if (accion?.dataset.mixAction === 'play') void reproducir(numero);
        else if (accion?.dataset.mixAction === 'queue') void encolar(numero);
        else if (play) void reproducir(numero, Number(play.dataset.mixPlay));
        else if (queue) {
            void cargar().then(lista => {
                const orden = crearOrden(numero, lista);
                const cancion = orden[Number(queue.dataset.mixQueue)];
                if (cancion && window.SpotifyNetPlayer?.queueTrack) {
                    window.SpotifyNetPlayer.queueTrack(cancion);
                    notificar('Canción añadida a la cola');
                }
            }).catch(console.error);
        }
    }, true);

    function iniciar() {
        prepararTarjetas();
        void pintarDetalle();
        if (buscarTarjetas().length) {
            void cargar().then(prepararTarjetas).catch(error => console.warn('Mixes:', error));
        }
    }

    document.addEventListener('DOMContentLoaded', iniciar);
    document.addEventListener('spotifynet:navegacion', () => {
        revision++;
        canciones = null;
        peticion = null;
        iniciar();
    });
    if (document.readyState !== 'loading') iniciar();
})();
