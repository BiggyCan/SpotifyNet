
document.addEventListener("DOMContentLoaded", function () {

    const $ = id => document.getElementById(id);

    const app = $("userApp");
    const audio = $("musicAudio");
    const fileInput = $("musicFileInput");

    if (!app || !audio) return;

    const playButton = $("playButton");
    const previousButton = $("previousButton");
    const nextButton = $("nextButton");
    const shuffleButton = $("shuffleButton");
    const repeatButton = $("repeatButton");
    const progressBar = $("progressBar");
    const volumeControl = $("volumeControl");

    let cancionesLocales = [];
    let colaActual = [];
    let indiceActual = -1;
    let aleatorio = false;
    let repetir = false;
    let historial = [];
    let ultimaCancionHistorial = null;
    let arrastreIndice = -1;

    const queuePanel = $("queuePanel");
    const queueToggle = $("toggleQueue");
    const queueClose = $("closeQueue");
    const queueCurrent = $("queueCurrent");
    const queueNext = $("queueNext");
    const queueHistory = $("queueHistory");
    const queueCount = $("queueCount");

    audio.volume = 0.7;

    function tiempo(segundos) {
        if (!Number.isFinite(segundos)) return "0:00";

        const minutos = Math.floor(segundos / 60);
        const resto = Math.floor(segundos % 60);

        return minutos + ":" +
            String(resto).padStart(2, "0");
    }

    function obtenerToken() {
        return document.querySelector(
            '#globalAntiForgeryToken input[name="__RequestVerificationToken"]'
        )?.value ||
        document.querySelector(
            'input[name="__RequestVerificationToken"]'
        )?.value || "";
    }

    async function enviarFormulario(url, datos) {
        const form = new FormData();

        Object.entries(datos).forEach(([clave, valor]) => {
            form.append(clave, valor);
        });

        const respuesta = await fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "RequestVerificationToken": obtenerToken()
            },
            body: form
        });

        const json = await respuesta.json().catch(() => ({}));

        if (!respuesta.ok) {
            throw new Error(
                json.error ||
                "No se pudo completar la operación."
            );
        }

        return json;
    }

    // =====================================
    // PANEL DE BIBLIOTECA
    // =====================================

    $("toggleLibrary")?.addEventListener("click", function () {
        app.classList.toggle("is-library-collapsed");
    });

    // =====================================
    // PANEL DERECHO
    // =====================================

    $("toggleDetails")?.addEventListener("click", function () {
        app.classList.toggle("is-details-hidden");
    });

    $("closeDetails")?.addEventListener("click", function () {
        app.classList.add("is-details-hidden");
    });

    // =====================================
    // PERFIL
    // =====================================

    const profileButton = $("profileButton");
    const profileMenu = $("profileMenu");

    profileButton?.addEventListener("click", function (e) {
        e.stopPropagation();

        if (profileMenu) {
            profileMenu.hidden = !profileMenu.hidden;
        }
    });

    document.addEventListener("click", function (e) {
        if (!e.target.closest(".u-profile-wrapper") &&
            profileMenu) {
            profileMenu.hidden = true;
        }
    });

    // =====================================
    // FILTROS DE BIBLIOTECA
    // =====================================

    document.querySelectorAll("[data-library-filter]")
        .forEach(button => {

            button.addEventListener("click", function () {
                document.querySelectorAll(
                    "[data-library-filter]"
                ).forEach(b => b.classList.remove("selected"));

                button.classList.add("selected");

                const filtro = button.dataset.libraryFilter;

                document.querySelectorAll(
                    "[data-library-kind]"
                ).forEach(item => {
                    item.hidden =
                        filtro !== "todo" &&
                        item.dataset.libraryKind !== filtro;
                });
            });
        });

    // =====================================
    // FILTROS DEL INICIO
    // =====================================

    document.addEventListener("click", function (e) {
        const button = e.target.closest(
            "[data-section-filter]"
        );

        if (!button) return;

        document.querySelectorAll(
            "[data-section-filter]"
        ).forEach(b => b.classList.remove("active"));

        button.classList.add("active");

        const filtro = button.dataset.sectionFilter;

        const music = $("musicSections");
        const podcasts = $("podcastsSection");
        const books = $("audiobooksSection");

        if (music) {
            music.hidden =
                filtro === "podcasts" ||
                filtro === "audiolibros";
        }

        if (podcasts) {
            podcasts.hidden = filtro !== "podcasts";
        }

        if (books) {
            books.hidden = filtro !== "audiolibros";
        }
    });

    // =====================================
    // PLAYLISTS PERSONALES
    // =====================================

    async function cargarPlaylistsPersonales() {
        const biblioteca = document.querySelector(
            ".u-library-items"
        );

        if (!biblioteca) return;

        try {
            const respuesta = await fetch(
                "/Biblioteca/MisListas",
                { credentials: "same-origin" }
            );

            if (!respuesta.ok) return;

            const listas = await respuesta.json();

            biblioteca.querySelectorAll(
                ".u-personal-playlist"
            ).forEach(nodo => nodo.remove());

            listas.forEach(lista => {
                const enlace = document.createElement("a");

                enlace.href = lista.url;
                enlace.className =
                    "u-lib-item u-personal-playlist";

                enlace.dataset.libraryKind = "playlist";

                const portada = document.createElement("span");
                portada.className = "u-lib-art art-purple";
                portada.textContent = lista.coverUrl ? "" : "♫";
                if (lista.coverUrl) {
                    portada.style.backgroundImage = `url("${lista.coverUrl}")`;
                    portada.style.backgroundSize = "cover";
                    portada.style.backgroundPosition = "center";
                }

                const contenido = document.createElement("span");
                contenido.className = "u-lib-copy";

                const titulo = document.createElement("strong");
                titulo.textContent = lista.name;

                const subtitulo = document.createElement("small");
                subtitulo.textContent = "Mi playlist personal";

                contenido.append(titulo, subtitulo);
                enlace.append(portada, contenido);

                biblioteca.appendChild(enlace);
            });

        } catch (error) {
            console.error(
                "No se pudo cargar la biblioteca:",
                error
            );
        }
    }

    // Permite actualizar el panel sin recargar la página.
    window.SpotifyNetLibraryRefresh =
        cargarPlaylistsPersonales;

    // =====================================
    // CREAR PLAYLIST SIN DETENER LA MÚSICA
    // =====================================

    $("addSongButton")?.addEventListener(
        "click",
        async function () {

            const nombre = prompt(
                "Nombre de tu nueva playlist:",
                "Mi nueva playlist"
            );

            if (nombre === null) return;

            if (nombre.trim().length < 2) {
                alert(
                    "Escribe un nombre de al menos 2 caracteres."
                );
                return;
            }

            try {
                const resultado = await enviarFormulario(
                    "/Biblioteca/CrearLista",
                    { nombre: nombre.trim() }
                );

                await cargarPlaylistsPersonales();

                // Aquí está la corrección principal:
                // navegar sin destruir el reproductor.
                if (resultado.url) {
                    if (window.SpotifyNetNavigate) {
                        await window.SpotifyNetNavigate(
                            resultado.url
                        );
                    } else {
                        // Solo como respaldo.
                        window.location.href = resultado.url;
                    }
                }

            } catch (error) {
                alert(error.message);
            }
        }
    );

    // =====================================
    // ARCHIVOS LOCALES TEMPORALES
    // =====================================

    document.addEventListener("click", function (e) {
        const boton = e.target.closest("[data-add-files]");

        if (!boton) return;

        // La navegación de playlists la controla
        // fidelidad.js mediante captura de eventos.
        if (boton.hasAttribute("data-playlist-slug")) {
            return;
        }

        fileInput?.click();
    });

    fileInput?.addEventListener("change", function () {
        const archivos = Array.from(fileInput.files || []);
        const inicio = cancionesLocales.length;

        archivos.forEach(archivo => {
            const valido =
                archivo.type.startsWith("audio/") ||
                /\.(mp3|wav|ogg|m4a|aac)$/i.test(archivo.name);

            if (!valido) return;

            cancionesLocales.push({
                title: archivo.name.replace(/\.[^.]+$/, ""),
                artist: "Archivo local",
                url: URL.createObjectURL(archivo),
                cover: "",
                local: true
            });
        });

        mostrarCancionesLocales();

        if (cancionesLocales.length > inicio) {
            reproducirCola(cancionesLocales, inicio);
        }

        fileInput.value = "";
    });

    function mostrarCancionesLocales() {
        const contenedor = $("uploadedSongs");

        if (!contenedor) return;

        contenedor.replaceChildren();

        if (cancionesLocales.length === 0) {
            contenedor.textContent =
                "Todavía no has añadido canciones.";
            return;
        }

        $("emptyLibrary")?.remove();

        cancionesLocales.forEach((cancion, indice) => {
            const fila = document.createElement("button");

            fila.type = "button";
            fila.className = "u-track-row";

            const icono = document.createElement("span");
            icono.textContent = "▶";

            const titulo = document.createElement("strong");
            titulo.textContent = cancion.title;

            const origen = document.createElement("small");
            origen.textContent = "Audio local";

            fila.append(icono, titulo, origen);

            fila.addEventListener("click", () => {
                reproducirCola(cancionesLocales, indice);
            });

            contenedor.appendChild(fila);
        });
    }

    // =====================================
    // REPRODUCTOR
    // =====================================

    function normalizarCancion(cancion) {
        return {
            id: cancion.id || null,
            title: cancion.title || "Sin título",
            artist: cancion.artist || "Archivo local",
            url: cancion.url,
            cover: cancion.cover || "",
            local: !!cancion.local,
            prioritaria: !!cancion.prioritaria
        };
    }

    function reproducirCola(canciones, indice = 0) {
        if (!Array.isArray(canciones) ||
            canciones.length === 0) {
            return;
        }

        colaActual = canciones.map(normalizarCancion);
        seleccionarCancion(indice, true);
    }

    function seleccionarCancion(indice, autoplay = false) {
        if (indice < 0 || indice >= colaActual.length) {
            return;
        }

        indiceActual = indice;

        const cancion = colaActual[indice];
        cancion.prioritaria = false;

        // Historial de la sesión, sin registrar contraseñas ni datos privados.
        if (ultimaCancionHistorial !== cancion.url || historial.length === 0) {
            historial.unshift({ ...cancion, escuchada: Date.now() });
            historial = historial.slice(0, 25);
            ultimaCancionHistorial = cancion.url;
        }

        audio.src = cancion.url;
        audio.load();

        if ($("playerTitle")) {
            $("playerTitle").textContent = cancion.title;
        }

        if ($("playerArtist")) {
            $("playerArtist").textContent = cancion.artist;
        }

        if ($("detailsTitle")) {
            $("detailsTitle").textContent = cancion.title;
        }

        if ($("detailsArtist")) {
            $("detailsArtist").textContent = cancion.artist;
        }

        if ($("detailsDescription")) {
            $("detailsDescription").textContent =
                cancion.local
                    ? "Reproduciendo desde tu equipo."
                    : "Reproduciendo una canción guardada.";
        }

        const portada = $("detailsArt");

        if (portada) {
            portada.classList.remove("u-reference-art");

            if (cancion.cover) {
                portada.style.backgroundImage =
                    `url("${cancion.cover}")`;
                portada.style.backgroundSize = "cover";
                portada.textContent = "";
            } else {
                portada.style.backgroundImage = "";
                portada.textContent = "♫";
            }
        }

        if (playButton) playButton.disabled = false;
        if (progressBar) {
            progressBar.disabled = false;
            progressBar.value = 0;
        }

        if ($("currentTime")) {
            $("currentTime").textContent = "0:00";
        }

        if ($("totalTime")) {
            $("totalTime").textContent = "0:00";
        }

        if (autoplay) {
            audio.play().catch(console.error);
        }
        dibujarCola();
    }

    function siguienteCancion() {
        if (colaActual.length === 0) return;

        let siguiente = (indiceActual + 1) % colaActual.length;

        // Las canciones marcadas «Reproducir a continuación» tienen
        // prioridad, incluso con aleatorio activado.
        if (!colaActual[siguiente]?.prioritaria && aleatorio && colaActual.length > 1) {
            const pendientes = [];
            for (let i = indiceActual + 1; i < colaActual.length; i++) {
                pendientes.push(i);
            }
            if (pendientes.length > 1) {
                const elegido = pendientes[Math.floor(Math.random() * pendientes.length)];
                [colaActual[siguiente], colaActual[elegido]] =
                    [colaActual[elegido], colaActual[siguiente]];
            }
        }

        seleccionarCancion(siguiente, true);
    }

    function agregarACola(cancion) {
        if (!cancion?.url) return false;

        colaActual.push(normalizarCancion(cancion));
        if (indiceActual === -1) seleccionarCancion(0, true);
        dibujarCola();
        return true;
    }

    function reproducirProxima(cancion) {
        if (!cancion?.url) return false;

        const nueva = normalizarCancion(cancion);
        nueva.prioritaria = true;
        if (indiceActual < 0) {
            colaActual = [nueva];
            seleccionarCancion(0, true);
        } else {
            colaActual.splice(indiceActual + 1, 0, nueva);
        }
        dibujarCola();
        return true;
    }

    // =====================================================
    // COLA VISUAL: reproducir, mover, quitar e historial
    // =====================================================

    function nodoVacio(texto) {
        const p = document.createElement("p");
        p.className = "sn-queue-empty";
        p.textContent = texto;
        return p;
    }

    function crearBotonCola(accion, etiqueta, simbolo, indice) {
        const boton = document.createElement("button");
        boton.type = "button";
        boton.className = "sn-queue-action";
        boton.dataset.queueAction = accion;
        boton.dataset.queueIndex = String(indice);
        boton.title = etiqueta;
        boton.setAttribute("aria-label", etiqueta);
        boton.textContent = simbolo;
        return boton;
    }

    function filaCola(cancion, indice, seccion) {
        const fila = document.createElement("div");
        fila.className = "sn-queue-row";
        if (seccion === "next") {
            fila.draggable = true;
            fila.dataset.queueIndex = String(indice);
            fila.title = "Arrastra para reordenar";
        }

        const cover = document.createElement("div");
        cover.className = "sn-queue-cover";
        if (cancion.cover) {
            const img = document.createElement("img");
            img.src = cancion.cover;
            img.alt = "";
            img.loading = "lazy";
            cover.appendChild(img);
        } else {
            cover.textContent = "♫";
        }

        const informacion = document.createElement("div");
        informacion.className = "sn-queue-song";
        const titulo = document.createElement("strong");
        titulo.textContent = cancion.title;
        const artista = document.createElement("small");
        artista.textContent = cancion.artist;
        informacion.append(titulo, artista);
        fila.append(cover, informacion);

        const acciones = document.createElement("div");
        acciones.className = "sn-queue-row-actions";
        if (seccion === "next") {
            acciones.append(
                crearBotonCola("play", "Reproducir esta canción", "▶", indice),
                crearBotonCola("up", "Mover arriba", "↑", indice),
                crearBotonCola("down", "Mover abajo", "↓", indice),
                crearBotonCola("remove", "Quitar de la cola", "×", indice)
            );
        } else if (seccion === "history") {
            acciones.append(crearBotonCola("history", "Reproducir de nuevo", "▶", indice));
        } else {
            fila.classList.add("sn-queue-playing");
            const actual = document.createElement("span");
            actual.className = "sn-queue-live";
            actual.textContent = "Sonando";
            acciones.appendChild(actual);
        }
        fila.appendChild(acciones);
        return fila;
    }

    function dibujarCola() {
        if (!queuePanel) return;
        const scroll = queuePanel.scrollTop;
        if (queueCurrent) {
            queueCurrent.replaceChildren();
            queueCurrent.appendChild(
                indiceActual >= 0 && colaActual[indiceActual]
                    ? filaCola(colaActual[indiceActual], indiceActual, "current")
                    : nodoVacio("Todavía no hay ninguna canción reproduciéndose.")
            );
        }
        if (queueNext) {
            queueNext.replaceChildren();
            const pendientes = colaActual.slice(indiceActual + 1);
            if (pendientes.length === 0) {
                queueNext.appendChild(nodoVacio("No hay canciones pendientes."));
            } else {
                pendientes.forEach((cancion, desplazamiento) => {
                    queueNext.appendChild(filaCola(cancion, indiceActual + desplazamiento + 1, "next"));
                });
            }
            if (queueCount) queueCount.textContent = String(pendientes.length);
        }
        if (queueHistory) {
            queueHistory.replaceChildren();
            if (historial.length === 0) {
                queueHistory.appendChild(nodoVacio("El historial de esta sesión está vacío."));
            } else {
                historial.slice(0, 10).forEach((cancion, i) => {
                    queueHistory.appendChild(filaCola(cancion, i, "history"));
                });
            }
        }
        queuePanel.scrollTop = scroll;
    }

    function mostrarCola(mostrar) {
        if (!queuePanel) return;
        queuePanel.hidden = !mostrar;
        queueToggle?.classList.toggle("is-active", mostrar);
        queueToggle?.setAttribute("aria-expanded", String(mostrar));
        dibujarCola();
    }

    function intercambiarPendientes(a, b) {
        if (a <= indiceActual || b <= indiceActual ||
            a >= colaActual.length || b >= colaActual.length || a === b) return;
        const [elemento] = colaActual.splice(a, 1);
        colaActual.splice(b, 0, elemento);
        dibujarCola();
    }

    queueToggle?.addEventListener("click", () => mostrarCola(queuePanel?.hidden));
    queueClose?.addEventListener("click", () => mostrarCola(false));

    document.addEventListener("keydown", evento => {
        if (evento.key === "Escape" && queuePanel && !queuePanel.hidden) mostrarCola(false);
    });

    queuePanel?.addEventListener("click", evento => {
        const boton = evento.target.closest("[data-queue-action]");
        if (!boton) return;
        const accion = boton.dataset.queueAction;
        const i = Number(boton.dataset.queueIndex);
        if (!Number.isInteger(i)) return;

        switch (accion) {
            case "play":
                if (i > indiceActual && i < colaActual.length) seleccionarCancion(i, true);
                break;
            case "remove":
                if (i > indiceActual && i < colaActual.length) {
                    colaActual.splice(i, 1);
                    dibujarCola();
                }
                break;
            case "up": intercambiarPendientes(i, i - 1); break;
            case "down": intercambiarPendientes(i, i + 1); break;
            case "history":
                if (historial[i]?.url) reproducirCola([historial[i]], 0);
                break;
        }
    });

    queueNext?.addEventListener("dragstart", evento => {
        const fila = evento.target.closest(".sn-queue-row[data-queue-index]");
        if (!fila) return;
        arrastreIndice = Number(fila.dataset.queueIndex);
        evento.dataTransfer.effectAllowed = "move";
        evento.dataTransfer.setData("text/plain", String(arrastreIndice));
        fila.classList.add("sn-queue-dragging");
    });

    queueNext?.addEventListener("dragover", evento => {
        if (evento.target.closest(".sn-queue-row[data-queue-index]")) {
            evento.preventDefault();
            evento.dataTransfer.dropEffect = "move";
        }
    });

    queueNext?.addEventListener("drop", evento => {
        const fila = evento.target.closest(".sn-queue-row[data-queue-index]");
        if (!fila) return;
        evento.preventDefault();
        intercambiarPendientes(arrastreIndice, Number(fila.dataset.queueIndex));
        arrastreIndice = -1;
    });

    queueNext?.addEventListener("dragend", () => {
        arrastreIndice = -1;
        queueNext.querySelectorAll(".sn-queue-dragging").forEach(n => n.classList.remove("sn-queue-dragging"));
    });

    dibujarCola();

    // =====================================
    // BOTONES DEL REPRODUCTOR
    // =====================================

    playButton?.addEventListener("click", function () {
        if (indiceActual < 0) return;

        if (audio.paused) {
            audio.play().catch(console.error);
        } else {
            audio.pause();
        }
    });

    audio.addEventListener("play", function () {
        if (playButton) {
            playButton.textContent = "Ⅱ";
            playButton.title = "Pausar";
        }
    });

    audio.addEventListener("pause", function () {
        if (playButton) {
            playButton.textContent = "▶";
            playButton.title = "Reproducir";
        }
    });

    nextButton?.addEventListener(
        "click",
        siguienteCancion
    );

    previousButton?.addEventListener("click", function () {
        if (colaActual.length === 0) return;

        if (audio.currentTime > 3) {
            audio.currentTime = 0;
            return;
        }

        const anterior =
            (indiceActual - 1 + colaActual.length) %
            colaActual.length;

        seleccionarCancion(anterior, true);
    });

    shuffleButton?.addEventListener("click", function () {
        aleatorio = !aleatorio;

        shuffleButton.classList.toggle(
            "is-active",
            aleatorio
        );
    });

    repeatButton?.addEventListener("click", function () {
        repetir = !repetir;

        repeatButton.classList.toggle(
            "is-active",
            repetir
        );
    });

    audio.addEventListener("ended", function () {
        if (repetir) {
            audio.currentTime = 0;
            audio.play().catch(console.error);
        } else {
            siguienteCancion();
        }
    });

    // =====================================
    // DURACIÓN Y PROGRESO
    // =====================================

    audio.addEventListener("loadedmetadata", function () {
        if (Number.isFinite(audio.duration)) {
            if (progressBar) {
                progressBar.max = Math.max(
                    1,
                    Math.floor(audio.duration)
                );
            }

            if ($("totalTime")) {
                $("totalTime").textContent =
                    tiempo(audio.duration);
            }
        }
    });

    audio.addEventListener("timeupdate", function () {
        if (Number.isFinite(audio.currentTime)) {
            if (progressBar) {
                progressBar.value =
                    Math.floor(audio.currentTime);
            }

            if ($("currentTime")) {
                $("currentTime").textContent =
                    tiempo(audio.currentTime);
            }
        }
    });

    progressBar?.addEventListener("input", function () {
        if (Number.isFinite(audio.duration)) {
            audio.currentTime = Number(
                progressBar.value
            );
        }
    });

    volumeControl?.addEventListener("input", function () {
        audio.volume =
            Number(volumeControl.value) / 100;
    });

    // =====================================
    // BUSCADOR ANTERIOR
    // =====================================
    // buscador.js intercepta el evento durante captura.
    $("userSearch")?.addEventListener("input", function () {
        const consulta = this.value.toLowerCase().trim();

        document.querySelectorAll(
            ".u-track-row, .u-lib-item"
        ).forEach(elemento => {
            elemento.hidden =
                !elemento.textContent
                    .toLowerCase()
                    .includes(consulta);
        });
    });

    // =====================================
    // FUNCIONES PARA OTRAS PÁGINAS
    // =====================================

    window.SpotifyNetPlayer = {
        playTracks: reproducirCola,
        queueTrack: agregarACola,
        playNextTrack: reproducirProxima,
        getQueue: () => colaActual.slice(indiceActual + 1).map(c => ({ ...c })),
        getHistory: () => historial.map(c => ({ ...c })),
        openQueue: () => mostrarCola(true),

        stop: function () {
            audio.pause();
            audio.removeAttribute("src");
            audio.load();

            colaActual = [];
            indiceActual = -1;
            dibujarCola();

            if (playButton) {
                playButton.disabled = true;
                playButton.textContent = "▶";
            }

            if (progressBar) {
                progressBar.disabled = true;
                progressBar.value = 0;
            }

            if ($("playerTitle")) {
                $("playerTitle").textContent =
                    "Sin reproducción";
            }

            if ($("playerArtist")) {
                $("playerArtist").textContent =
                    "Añade tu música";
            }

            if ($("currentTime")) {
                $("currentTime").textContent = "0:00";
            }

            if ($("totalTime")) {
                $("totalTime").textContent = "0:00";
            }
        }
    };

    cargarPlaylistsPersonales();

    window.addEventListener("pagehide", function () {
        cancionesLocales.forEach(cancion => {
            URL.revokeObjectURL(cancion.url);
        });
    });
});
