
/*
    SPOTIFYNET - FIDELIDAD VISUAL Y NAVEGACION CONTINUA

    PASO 22

    Objetivos:
    - Mantener las portadas de SpotifyNet.
    - Conservar los enlaces de las playlists.
    - Navegar sin reiniciar el reproductor.
    - Mantener la canción mientras cambiamos de vista.
    - Permitir Inicio, playlists y navegación Atrás.
    - Conservar las playlists personales.
*/

(function () {
    "use strict";

    const BASE = "/images/referencias/";

    const app = document.getElementById("userApp");

    if (!app) return;

    const main = app.querySelector(".u-main");

    if (!main) return;

    let navegacionActual = 0;
    let urlMostrada =
        window.location.pathname + window.location.search;

    // ====================================================
    // 1. UTILIDADES
    // ====================================================

    function elemento(selector, contexto = document) {
        return contexto.querySelector(selector);
    }

    function elementos(selector, contexto = document) {
        return Array.from(contexto.querySelectorAll(selector));
    }

    function nombreUsuario() {
        return (
            elemento(".u-account-name")?.textContent
            || "Usuario"
        ).trim();
    }

    function rutaInterna(url) {

        const ruta = url.pathname
            .replace(/\/+$/, "")
            .toLowerCase() || "/";

        return [
            "/",
            "/home",
            "/home/index",
            "/home/iniciousuario",
            "/home/playlist",
            "/biblioteca/playlist",
            "/social/miespacio",
            "/social/compartida",
            "/social/perfil",
            "/perfil/editar",
            "/catalogo",
            "/catalogo/index",
            "/buscar/explorar",
            "/mixes/detalle"
        ].includes(ruta);
    }

    function esMismaPagina(url) {
        return (
            url.pathname + url.search
        ) === urlMostrada;
    }

    // ====================================================
    // 2. FOTOGRAFIAS
    // ====================================================

    function foto(nodo, archivo) {

        if (!nodo || !archivo) return;

        nodo.style.backgroundImage =
            `url("${BASE}${archivo}")`;

        nodo.classList.add("u-photo-cover");

        nodo.setAttribute(
            "aria-label",
            nodo.textContent.trim() || archivo
        );
    }

    function actualizarTarjeta(
        tarjeta,
        archivo,
        titulo,
        subtitulo,
        slug
    ) {

        if (!tarjeta) return;

        foto(
            tarjeta.querySelector(".u-album-art"),
            archivo
        );

        const texto = tarjeta.querySelector(
            ":scope > strong"
        );

        const subtexto = tarjeta.querySelector(
            ":scope > small"
        );

        if (texto) {
            texto.textContent = titulo;
        }

        if (subtexto) {
            subtexto.textContent = subtitulo;
        }

        if (slug) {
            tarjeta.dataset.playlistSlug = slug;
            tarjeta.title = "Abrir playlist: " + titulo;
        }
    }

    // ====================================================
    // 3. PLAYLISTS DE ACCESO RAPIDO
    // ====================================================

    const rapidas = [
        [
            "rapido-kiss.jpg",
            "KISS ME KISS ME KISS ME AGAIN",
            "kiss-me-kiss-me-kiss-me-again"
        ],
        [
            "rapido-trap.jpg",
            "Trap Cano",
            "trap-cano"
        ],
        [
            "rapido-hits.jpg",
            "Hits de Internet",
            "hits-de-internet"
        ],
        [
            "rapido-gta.jpg",
            "Grand Theft Auto Official Playlist",
            "grand-theft-auto-official-playlist"
        ],
        [
            "rapido-programar.jpg",
            "Música para programar",
            "musica-para-programar"
        ],
        [
            "rapido-fiesta.jpg",
            "Fiesta!!!",
            "fiesta"
        ],
        [
            "rapido-astrix.jpg",
            "This Is Astrix",
            "this-is-astrix"
        ],
        [
            "rapido-liked.jpg",
            "Tus me gusta",
            "tus-me-gusta"
        ]
    ];

    function aplicarAccesosRapidos() {

        elementos(".u-quick-item").forEach(
            (tarjeta, indice) => {

                const dato = rapidas[indice];

                if (!dato) return;

                foto(
                    tarjeta.querySelector(".u-quick-cover"),
                    dato[0]
                );

                const titulo = tarjeta.querySelector(
                    "strong"
                );

                if (titulo) {
                    titulo.textContent = dato[1];
                }

                tarjeta.dataset.playlistSlug = dato[2];

                tarjeta.title =
                    "Abrir playlist: " + dato[1];
            }
        );
    }

    // ====================================================
    // 4. PLAYLISTS DE LA COLECCION
    // ====================================================

    const playlists = [
        [
            "playlist-cano.jpg",
            "CANO",
            "cano"
        ],
        [
            "playlist-trap-bastardo.jpg",
            "Trap Bastardo",
            "trap-bastardo"
        ],
        [
            "playlist-tryhard.jpg",
            "Para jugar Tryhard 😎",
            "para-jugar-tryhard"
        ],
        [
            "playlist-ia-trap.jpg",
            "I.A. Trap",
            "ia-trap"
        ],
        [
            "playlist-gym.jpg",
            "GYM",
            "gym"
        ],
        [
            "playlist-blood.jpg",
            "Blood Oceans (How Many?)",
            "blood-oceans-how-many"
        ],
        [
            "playlist-trippy.jpg",
            "Trippy Cano",
            "trippy-cano"
        ]
    ];

    function aplicarColeccion() {

        const grids = elementos(".u-music-grid");

        if (!grids[0]) return;

        const tarjetas = elementos(
            ".u-music-card",
            grids[0]
        );

        while (
            tarjetas.length < playlists.length &&
            tarjetas.length > 0
        ) {

            const clon = tarjetas[0].cloneNode(true);

            grids[0].appendChild(clon);
            tarjetas.push(clon);
        }

        tarjetas.forEach((tarjeta, indice) => {

            const dato = playlists[indice];

            if (!dato) return;

            actualizarTarjeta(
                tarjeta,
                dato[0],
                dato[1],
                "De " + nombreUsuario(),
                dato[2]
            );
        });

        const texto = grids[0]
            .closest("section")
            ?.querySelector(".u-section-head > span");

        if (texto) {
            texto.textContent = "Mostrar todo";
        }
    }

    // ====================================================
    // 5. MIXES DIARIOS
    // ====================================================

    function aplicarMixes() {

        const grids = elementos(".u-music-grid");

        if (!grids[1]) return;

        const tarjetas = elementos(
            ".u-music-card",
            grids[1]
        );

        while (
            tarjetas.length < 6 &&
            tarjetas.length > 0
        ) {

            const clon = tarjetas[0].cloneNode(true);

            grids[1].appendChild(clon);
            tarjetas.push(clon);
        }

        const subtitulos = [
            "Alemán, La Santa Grifa, Kidd Keo y más",
            "Rauw Alejandro, KAROL G, Bad Bunny y más",
            "YSY A, Duki y más",
            "Kanye West, Young Thug y más",
            "Akapellah, Neutro Shorty y más",
            "Bambee, TR3NACRIA y más"
        ];

        tarjetas.forEach((tarjeta, indice) => {

            if (indice >= 6) return;

            actualizarTarjeta(
                tarjeta,
                `mix-${indice + 1}.jpg`,
                `Mix diario ${indice + 1}`,
                subtitulos[indice],
                null
            );
        });

        const seccion = grids[1].closest("section");

        const titulo = seccion?.querySelector(
            ".u-section-head h2"
        );

        const enlace = seccion?.querySelector(
            ".u-section-head > span"
        );

        if (titulo) {
            titulo.textContent =
                "Hecho para " + nombreUsuario();
        }

        if (enlace) {
            enlace.textContent = "Mostrar todo";
        }
    }

    // ====================================================
    // 6. BIBLIOTECA LATERAL
    // ====================================================

    const bibliotecaReferencias = [
        [
            "rapido-liked.jpg",
            "Tus me gusta",
            "tus-me-gusta"
        ],
        ...playlists
    ];

    function aplicarBiblioteca() {

        const biblioteca = elemento(".u-library-items");

        if (!biblioteca) return;

        // No modificar playlists personales creadas en SQLite.
        const tarjetas = elementos(
            ".u-lib-item:not(.u-personal-playlist)",
            biblioteca
        );

        while (
            tarjetas.length < bibliotecaReferencias.length &&
            tarjetas.length > 0
        ) {

            const clon = tarjetas[0].cloneNode(true);

            biblioteca.appendChild(clon);
            tarjetas.push(clon);
        }

        tarjetas.forEach((tarjeta, indice) => {

            const dato = bibliotecaReferencias[indice];

            if (!dato) return;

            foto(
                tarjeta.querySelector(".u-lib-art"),
                dato[0]
            );

            const titulo = tarjeta.querySelector(
                ".u-lib-copy strong"
            );

            const subtitulo = tarjeta.querySelector(
                ".u-lib-copy small"
            );

            if (titulo) {
                titulo.textContent = dato[1];
            }

            if (subtitulo) {
                subtitulo.textContent =
                    indice === 0
                    ? "Playlist · Favoritas"
                    : "Playlist · " + nombreUsuario();
            }

            tarjeta.dataset.libraryKind = "playlist";
            tarjeta.dataset.playlistSlug = dato[2];

            tarjeta.title =
                "Abrir playlist: " + dato[1];
        });
    }

    // ====================================================
    // 7. PANEL DERECHO
    // ====================================================

    function aplicarPanelDerecho() {

        const arte = elemento("#detailsArt");
        const audio = elemento("#musicAudio");

        if (!arte || !audio) return;

        // No modificar la portada de la canción que
        // ya se está reproduciendo.
        if (!audio.currentSrc && !audio.src) {

            arte.style.backgroundImage =
                `url("${BASE}nowplaying.jpg")`;

            arte.classList.add("u-reference-art");
        }
    }

    // ====================================================
    // 8. ACTUALIZAR IMAGENES DE LA VISTA
    // ====================================================

    function aplicarFidelidad() {

        aplicarAccesosRapidos();
        aplicarColeccion();
        aplicarMixes();
        aplicarBiblioteca();
        aplicarPanelDerecho();
    }

    // ====================================================
    // 9. ACTIVAR SCRIPTS PROPIOS DE LAS PLAYLISTS
    // ====================================================

    function activarScriptDePlaylist(documento) {

        // El código de Playlist.cshtml usa DOMContentLoaded.
        // En una navegación interna ese evento ya ocurrió.
        // Ejecutamos exclusivamente la inicialización de
        // la vista nueva sin reiniciar usuario.js.

        const scripts = elementos(
            "script:not([src])",
            documento
        );

        const scriptPlaylist = scripts.find(script =>
            script.textContent.includes("playlistSavedTracks") &&
            script.textContent.includes("playlistAudioFiles")
        );

        if (!scriptPlaylist) return;

        const callbacks = [];

        const addEventOriginal = document.addEventListener;

        try {

            document.addEventListener = function (
                nombreEvento,
                funcion,
                opciones
            ) {

                if (nombreEvento === "DOMContentLoaded") {
                    callbacks.push(funcion);
                    return;
                }

                return addEventOriginal.call(
                    this,
                    nombreEvento,
                    funcion,
                    opciones
                );
            };

            const scriptNuevo =
                document.createElement("script");

            scriptNuevo.textContent =
                scriptPlaylist.textContent;

            document.body.appendChild(scriptNuevo);
            scriptNuevo.remove();

        } finally {

            document.addEventListener = addEventOriginal;
        }

        callbacks.forEach(funcion => {

            try {

                if (typeof funcion === "function") {
                    funcion.call(
                        document,
                        new Event("DOMContentLoaded")
                    );
                }

            } catch (error) {

                console.error(
                    "Error al iniciar la playlist:",
                    error
                );
            }
        });
    }

    // ====================================================
    // 10. NAVEGACION INTERNA SIN REINICIAR MUSICA
    // ====================================================

    async function navegar(
        destino,
        guardarHistorial = true
    ) {

        let url;

        try {
            url = new URL(destino, window.location.href);
        } catch {
            return;
        }

        // Otros sitios y rutas no compatibles usan
        // navegación normal.
        if (
            url.origin !== window.location.origin ||
            !rutaInterna(url)
        ) {

            if (guardarHistorial) {
                window.location.assign(url.href);
            }

            return;
        }

        if (esMismaPagina(url)) {
            return;
        }

        const peticionActual = ++navegacionActual;

        try {

            const respuesta = await fetch(url.href, {
                method: "GET",
                credentials: "same-origin",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (!respuesta.ok) {

                throw new Error(
                    "El servidor respondió con estado " +
                    respuesta.status
                );
            }

            const html = await respuesta.text();

            const documento = new DOMParser()
                .parseFromString(html, "text/html");

            const nuevoApp =
                documento.querySelector("#userApp");

            const nuevoMain =
                nuevoApp?.querySelector(".u-main");

            // Si cambia el tipo de página, por ejemplo
            // al iniciar o cerrar sesión, se requiere
            // una navegación normal.
            if (!nuevoMain) {

                if (guardarHistorial) {
                    window.location.assign(url.href);
                } else {
                    window.location.replace(url.href);
                }

                return;
            }

            // Ignorar respuestas de navegaciones anteriores.
            if (peticionActual !== navegacionActual) {
                return;
            }

            /*
                IMPORTANTE:

                Se reemplaza ÚNICAMENTE .u-main.

                Permanecen intactos:
                - musicAudio
                - u-player
                - panel derecho
                - biblioteca lateral
                - controles de reproducción

                De esta manera, el audio sigue sonando.
            */

            main.innerHTML = nuevoMain.innerHTML;

            // Actualizar título del navegador.
            document.title =
                documento.title || "SpotifyNet";

            if (guardarHistorial) {

                window.history.pushState(
                    { spotifyNetShell: true },
                    "",
                    url.pathname + url.search + url.hash
                );
            }

            urlMostrada = url.pathname + url.search;

            // Volver arriba solamente en el área central.
            main.scrollTop = 0;

            // Volver a aplicar las imágenes en Inicio.
            aplicarFidelidad();

            // Activar botones y canciones de Playlist.cshtml.
            if (elemento("#playlistPage")) {
                activarScriptDePlaylist(documento);
            }

            // Avisar a otros componentes de la navegación.
            document.dispatchEvent(
                new CustomEvent("spotifynet:navegacion", {
                    detail: {
                        url: url.href,
                        path: url.pathname
                    }
                })
            );

        } catch (error) {

            console.error(
                "Error de navegación de SpotifyNet:",
                error
            );

            // Conservar el reproductor y la página anterior
            // si hay un error de conexión.
            alert(
                "No se pudo abrir la página. " +
                "Comprueba que SpotifyNet siga ejecutándose."
            );
        }
    }

    // Exponer navegación para posibles controles futuros.
    window.SpotifyNetNavigate = navegar;

    // ====================================================
    // 11. INTERCEPTAR CLICS EN LAS PLAYLISTS
    // ====================================================

    document.addEventListener("click", function (evento) {

        if (
            evento.defaultPrevented ||
            evento.button !== 0 ||
            evento.ctrlKey ||
            evento.metaKey ||
            evento.shiftKey ||
            evento.altKey
        ) {
            return;
        }

        const origen = evento.target;

        if (!(origen instanceof Element)) return;

        // Se usa solamente la instancia actual del sitio.
        if (!app.contains(origen)) return;

        // PRIMER CASO: Tarjetas con slug.
        const tarjeta = origen.closest(
            "[data-playlist-slug]"
        );

        if (tarjeta) {

            const slug = tarjeta.dataset.playlistSlug;

            if (!slug) return;

            evento.preventDefault();
            evento.stopImmediatePropagation();

            const destino =
                "/Home/Playlist?slug=" +
                encodeURIComponent(slug);

            navegar(destino);

            return;
        }

        // SEGUNDO CASO: Enlaces a Inicio o playlists.
        const enlace = origen.closest("a[href]");

        if (!enlace) return;

        if (
            enlace.hasAttribute("download") ||
            (enlace.target && enlace.target !== "_self")
        ) {
            return;
        }

        let url;

        try {
            url = new URL(enlace.href);
        } catch {
            return;
        }

        if (
            url.origin !== window.location.origin ||
            !rutaInterna(url)
        ) {
            return;
        }

        evento.preventDefault();
        evento.stopImmediatePropagation();

        navegar(url.href);

    }, true);

    // ====================================================
    // 12. BOTON ATRAS Y ADELANTE DEL NAVEGADOR
    // ====================================================

    window.addEventListener("popstate", function () {

        const destino =
            window.location.pathname +
            window.location.search;

        if (destino === urlMostrada) return;

        navegar(window.location.href, false);
    });

    // ====================================================
    // 13. INICIALIZACION
    // ====================================================

    if (window.history.state?.spotifyNetShell !== true) {

        window.history.replaceState(
            {
                ...(window.history.state || {}),
                spotifyNetShell: true
            },
            "",
            window.location.href
        );
    }

    if (document.readyState === "loading") {

        document.addEventListener(
            "DOMContentLoaded",
            aplicarFidelidad,
            { once: true }
        );

    } else {

        aplicarFidelidad();
    }

})();
