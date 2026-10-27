/* SpotifyNet · comportamiento de paneles en ventanas estrechas.
   Mejora la navegación sin sustituir los controladores del reproductor. */
document.addEventListener("DOMContentLoaded", () => {
    const app = document.getElementById("userApp");
    const libraryToggle = document.getElementById("toggleLibrary");
    const detailsToggle = document.getElementById("toggleDetails");
    const detailsClose = document.getElementById("closeDetails");
    const queueToggle = document.getElementById("toggleQueue");
    const library = app?.querySelector(".u-library");
    if (!app) return;

    const compactLibrary = window.matchMedia("(max-width: 1100px)");
    const compactDetails = window.matchMedia("(max-width: 1250px)");
    let forcedCollapse = false;

    const backdrop = document.createElement("button");
    backdrop.type = "button";
    backdrop.className = "sn-r-backdrop";
    backdrop.hidden = true;
    backdrop.setAttribute("aria-label", "Cerrar panel abierto");
    document.body.appendChild(backdrop);

    if (library) library.id = library.id || "libraryPanel";
    libraryToggle?.setAttribute("aria-controls", "libraryPanel");
    detailsToggle?.setAttribute("aria-controls", "detailsPanel");

    function syncBackdrop() {
        const openLibrary = compactLibrary.matches && app.classList.contains("is-library-mobile-open");
        const openDetails = compactDetails.matches && app.classList.contains("is-details-open");
        backdrop.hidden = !(openLibrary || openDetails);
        libraryToggle?.setAttribute("aria-expanded", String(openLibrary || (!compactLibrary.matches && !app.classList.contains("is-library-collapsed"))));
        detailsToggle?.setAttribute("aria-expanded", String(openDetails || (!compactDetails.matches && !app.classList.contains("is-details-hidden"))));
    }

    function closePanels() {
        app.classList.remove("is-library-mobile-open", "is-details-open");
        syncBackdrop();
    }

    function adapt() {
        if (compactLibrary.matches) {
            if (!app.classList.contains("is-library-collapsed") && !app.classList.contains("is-library-mobile-open")) {
                app.classList.add("is-library-collapsed");
                forcedCollapse = true;
            }
        } else {
            app.classList.remove("is-library-mobile-open");
            if (forcedCollapse) {
                app.classList.remove("is-library-collapsed");
                forcedCollapse = false;
            }
        }
        if (!compactDetails.matches) {
            app.classList.remove("is-details-open");
            // El panel reaparece como tercera columna al ampliar la ventana.
            app.classList.remove("is-details-hidden");
        }
        syncBackdrop();
    }

    libraryToggle?.addEventListener("click", () => {
        if (!compactLibrary.matches) { syncBackdrop(); return; }
        const willOpen = !app.classList.contains("is-library-mobile-open");
        app.classList.toggle("is-library-mobile-open", willOpen);
        app.classList.toggle("is-library-collapsed", !willOpen);
        if (willOpen) app.classList.remove("is-details-open");
        syncBackdrop();
    });

    detailsToggle?.addEventListener("click", () => {
        if (!compactDetails.matches) { syncBackdrop(); return; }
        const willOpen = !app.classList.contains("is-details-open");
        app.classList.toggle("is-details-open", willOpen);
        // usuario.js alterna is-details-hidden; en un cajón manda is-details-open.
        app.classList.remove("is-details-hidden");
        if (willOpen) {
            app.classList.remove("is-library-mobile-open");
            if (compactLibrary.matches) app.classList.add("is-library-collapsed");
        }
        syncBackdrop();
    });

    detailsClose?.addEventListener("click", () => {
        if (compactDetails.matches) app.classList.remove("is-details-hidden");
        app.classList.remove("is-details-open");
        syncBackdrop();
    });

    queueToggle?.addEventListener("click", () => {
        if (compactDetails.matches) closePanels();
    });

    backdrop.addEventListener("click", closePanels);
    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && !backdrop.hidden) closePanels();
    });

    compactLibrary.addEventListener("change", adapt);
    compactDetails.addEventListener("change", adapt);
    adapt();
});
