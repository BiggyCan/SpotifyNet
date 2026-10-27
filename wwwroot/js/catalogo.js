// Bloque D: catálogo y edición segura de los metadatos, sin reemplazar el reproductor.
(function () {
    "use strict";
    let dialogo;
    const $ = sel => document.querySelector(sel);
    function csrf() { return $('#globalAntiForgeryToken input[name="__RequestVerificationToken"]')?.value || ""; }
    function estado(msg) { if (dialogo) dialogo.querySelector('.sn-meta-status').textContent = msg || ""; }

    function crearDialogo() {
        if (dialogo) return dialogo;
        dialogo = document.createElement('dialog');
        dialogo.className = 'sn-meta-dialog';
        dialogo.innerHTML = `<form id="snMetaForm" method="dialog">
            <h2>Editar datos de la canción</h2>
            <input type="hidden" id="snMetaId">
            <label for="snMetaTitle">Título</label><input id="snMetaTitle" required maxlength="250">
            <label for="snMetaArtist">Artista</label><input id="snMetaArtist" required maxlength="150">
            <label for="snMetaAlbum">Álbum</label><input id="snMetaAlbum" maxlength="180">
            <div class="sn-meta-status" role="status"></div>
            <div class="sn-meta-buttons"><button type="button" id="snMetaCancel">Cancelar</button>
            <button type="submit" class="sn-meta-save">Guardar cambios</button></div>
        </form>`;
        document.body.append(dialogo);
        dialogo.querySelector('#snMetaCancel').addEventListener('click', () => dialogo.close());
        dialogo.querySelector('form').addEventListener('submit', async e => {
            e.preventDefault();
            const id = dialogo.querySelector('#snMetaId').value;
            const fd = new FormData();
            fd.append('id', id);
            fd.append('titulo', dialogo.querySelector('#snMetaTitle').value.trim());
            fd.append('artista', dialogo.querySelector('#snMetaArtist').value.trim());
            fd.append('album', dialogo.querySelector('#snMetaAlbum').value.trim());
            estado('Guardando...');
            try {
                const r = await fetch('/Catalogo/Actualizar', { method: 'POST', credentials:'same-origin',
                    headers: { 'RequestVerificationToken': csrf() }, body: fd });
                const d = await r.json().catch(() => ({}));
                if (!r.ok) throw new Error(d.error || 'No se pudo guardar.');
                // Mantener los datos visibles en la página sin interrumpir la música.
                document.querySelectorAll(`[data-track-id="${id}"]`).forEach(row => {
                    const title = row.querySelector('.pdetail-track-title');
                    const artist = row.querySelector('.pdetail-track-artist');
                    const album = row.querySelector('.pdetail-track-album');
                    if (title) title.textContent = d.title;
                    if (artist) artist.textContent = d.artist;
                    if (album) album.textContent = d.album;
                });
                document.querySelectorAll(`[data-song-id="${id}"]`).forEach(row => {
                    row.dataset.songTitle=d.title;row.dataset.songArtist=d.artist;row.dataset.songAlbum=d.album;
                    const title=row.querySelector('.sn-cat-song-title');
                    const sub=row.querySelector('.sn-cat-song-sub');
                    if(title)title.textContent=d.title;
                    if(sub)sub.textContent=d.artist+' · '+d.album;
                });
                dialogo.close();
            } catch (error) { estado(error.message); }
        });
        return dialogo;
    }

    async function abrirEditor(id) {
        if (!id || !/^[0-9a-f-]{36}$/i.test(id)) return;
        const d = crearDialogo();
        estado('Cargando...');
        try {
            const res = await fetch('/Catalogo/Datos?id='+encodeURIComponent(id), {credentials:'same-origin'});
            const data=await res.json().catch(() => ({}));
            if(!res.ok) throw new Error(data.error || 'No puedes editar una canción de otra cuenta.');
            d.querySelector('#snMetaId').value=id;
            d.querySelector('#snMetaTitle').value=data.title || '';
            d.querySelector('#snMetaArtist').value=data.artist || '';
            d.querySelector('#snMetaAlbum').value=data.album || '';
            estado('');
            d.showModal();
        } catch(err) { alert(err.message); }
    }

    function botonesPlaylist() {
        document.querySelectorAll('#playlistSavedTracks [data-track-id]').forEach(row => {
            if(row.querySelector('.sn-meta-row-button')) return;
            const acciones=row.querySelector('.p24-actions-cell');
            if(!acciones) return;
            const edit=document.createElement('button');
            edit.type='button';edit.className='sn-meta-row-button';
            edit.title='Editar título, artista y álbum';edit.textContent='✎';
            edit.dataset.editTrack=row.dataset.trackId;
            acciones.prepend(edit);
        });
    }

    document.addEventListener('submit', e => {
        const form = e.target.closest('.sn-cat-search');
        if (!form || !window.SpotifyNetNavigate) return;
        e.preventDefault();
        const params = new URLSearchParams(new FormData(form));
        window.SpotifyNetNavigate('/Catalogo?' + params.toString());
    });
    document.addEventListener('click', e => {
        const editar=e.target.closest('[data-edit-track]');
        if(editar){e.preventDefault();abrirEditor(editar.dataset.editTrack);return;}
        const btn=e.target.closest('.sn-cat-play');
        if(!btn) return;
        const row=btn.closest('.sn-cat-song');
        const container=btn.closest('.sn-cat-items');
        if(!row || !container || !window.SpotifyNetPlayer) return;
        const items=Array.from(container.querySelectorAll('.sn-cat-song'));
        const tracks=items.map(node=>({id:node.dataset.songId, title:node.dataset.songTitle,
            artist:node.dataset.songArtist,url:node.dataset.songUrl,cover:node.dataset.songCover,
            local:false}));
        window.SpotifyNetPlayer.playTracks(tracks,items.indexOf(row));
    });
    function init(){botonesPlaylist();}
    document.addEventListener('DOMContentLoaded', () => {
        init();
        const main=$('.u-main');
        if(main) new MutationObserver(() => botonesPlaylist()).observe(main, {subtree:true,childList:true});
    });
    document.addEventListener('spotifynet:navegacion',init);
})();
