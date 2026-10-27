/* SpotifyNet · Bloque C: privacidad, perfiles y colaboraciones.
   No recrea el reproductor de audio para conservar la música. */
(() => {
    "use strict";
    const el = id => document.getElementById(id);
    const antiForgery = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value || "";

    async function post(path, fields) {
        const data = new FormData();
        Object.entries(fields).forEach(([key, val]) => data.append(key, String(val)));
        const response = await fetch(path, {
            method: "POST",
            credentials: "same-origin",
            headers: { RequestVerificationToken: antiForgery() },
            body: data
        });
        const result = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(result.error || `Error ${response.status}`);
        return result;
    }

    async function copy(text) {
        if (navigator.clipboard?.writeText) {
            try { await navigator.clipboard.writeText(text); return; } catch { /* fallback */ }
        }
        window.prompt("Copia el enlace:", text);
    }

    async function initManagement() {
        const panel = el("socialManagement");
        if (!panel || panel.dataset.bound === "1") return;
        panel.dataset.bound = "1";
        const slug = panel.dataset.slug;
        const feedback = el("socialFeedback");
        const notify = (msg, err = false) => {
            feedback.textContent = msg;
            feedback.style.color = err ? "#ff9999" : "#1ed760";
        };
        let state = null;

        async function refresh() {
            const response = await fetch(`/Social/Estado?slug=${encodeURIComponent(slug)}`, {
                credentials: "same-origin", cache: "no-store"
            });
            if (!response.ok) throw new Error("No se pudo comprobar la privacidad.");
            state = await response.json();
            panel.hidden = !state.found;
            if (!state.found) return;
            el("socialPrivacyLabel").textContent = state.isPublic ? "Pública" : "Privada";
            el("socialTogglePublic").textContent = state.isPublic ? "Volver privada" : "Hacer pública";
            const container = el("socialMembers");
            container.replaceChildren();
            for (const member of state.members || []) {
                const row = document.createElement("div");
                row.className = "sc-member";
                const label = document.createElement("span");
                label.textContent = member.email;
                const button = document.createElement("button");
                button.type = "button"; button.className = "sc-button";
                button.textContent = "Retirar acceso";
                button.addEventListener("click", async () => {
                    if (!confirm(`¿Retirar acceso a ${member.email}?`)) return;
                    try {
                        await post("/Social/QuitarColaborador", { slug, userId: member.id });
                        notify("Colaborador retirado.");
                        await refresh();
                    } catch (e) { notify(e.message, true); }
                });
                row.append(label, button); container.appendChild(row);
            }
        }
        el("socialTogglePublic")?.addEventListener("click", async () => {
            if (!state?.found) return;
            try {
                await post("/Social/Visibilidad", { slug, publica: !state.isPublic });
                await refresh();
                notify(state.isPublic ? "Tu playlist ahora es pública." : "Tu playlist ahora es privada.");
            } catch (e) { notify(e.message, true); }
        });
        el("socialCopy")?.addEventListener("click", async () => {
            if (!state?.url) { notify("Primero haz pública la playlist o invita a un colaborador.", true); return; }
            await copy(state.url);
            notify(state.isPublic ? "Enlace copiado." : "Enlace copiado. Solo podrán entrar los colaboradores autorizados.");
        });
        el("socialInviteForm")?.addEventListener("submit", async e => {
            e.preventDefault();
            const email = el("socialInviteEmail").value.trim();
            if (!email) return;
            try {
                await post("/Social/Invitar", { slug, email });
                el("socialInviteEmail").value = "";
                await refresh();
                notify("Invitación concedida. Comparte el enlace con esa persona.");
            } catch (err) { notify(err.message, true); }
        });
        try { await refresh(); } catch (e) { notify(e.message, true); }
    }

    function initShared() {
        const root = document.querySelector(".sc-shared[data-share-token]");
        if (!root || root.dataset.bound === "1") return;
        root.dataset.bound = "1";
        const token = root.dataset.shareToken;
        const nodes = Array.from(root.querySelectorAll("#scSharedSongs [data-track-id]"));
        const tracks = nodes.map(node => ({
            id: node.dataset.trackId,
            title: node.dataset.trackName,
            artist: node.dataset.trackArtist,
            url: `/Social/Audio?token=${encodeURIComponent(token)}&id=${encodeURIComponent(node.dataset.trackId)}`,
            cover: `/Social/Portada?token=${encodeURIComponent(token)}`,
            local: false
        }));
        function play(i) {
            if (!tracks.length || !window.SpotifyNetPlayer) return;
            window.SpotifyNetPlayer.playTracks(tracks, i);
        }
        nodes.forEach((node, i) => node.addEventListener("click", () => play(i)));
        el("scPlayAll")?.addEventListener("click", () => play(0));

        const addForm = root.querySelector(".sc-add form");
        addForm?.addEventListener("submit", async event => {
            event.preventDefault();
            const btn = addForm.querySelector("button");
            if (btn) btn.disabled = true;
            try {
                const response = await fetch(addForm.action, {
                    method: "POST", body: new FormData(addForm),
                    credentials: "same-origin", redirect: "follow"
                });
                if (!response.ok) throw new Error("No se pudo añadir la canción.");
                const html = await response.text();
                const doc = new DOMParser().parseFromString(html, "text/html");
                const newSection = doc.querySelector(".sc-shared");
                if (newSection) {
                    root.replaceWith(newSection);
                    initShared();
                }
            } catch (err) {
                alert(err.message);
            } finally { if (btn) btn.disabled = false; }
        });
    }

    function initialize() {
        initManagement();
        initShared();
    }
    document.addEventListener("DOMContentLoaded", initialize);
    document.addEventListener("spotifynet:navegacion", initialize);
})();
