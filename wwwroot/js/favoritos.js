/* SpotifyNet Bloque G. Favoritos persistentes sin reiniciar reproductor. */
(() => {
    'use strict';
    let requestId = 0;
    const $ = (root, id) => root.querySelector('#' + id);
    const element = (tag, className, content) => {
        const el = document.createElement(tag);
        if (className) el.className = className;
        if (content !== undefined) el.textContent = String(content);
        return el;
    };
    const safe = value => typeof value === 'string' ? value : '';
    function antiForgery() {
        return document.querySelector('#globalAntiForgeryToken input[name="__RequestVerificationToken"]')?.value || '';
    }
    function asTrack(t) {
        return { id: t.id, title: safe(t.title) || 'Sin título',
            artist: safe(t.artist) || 'Archivo local', album: safe(t.album) || 'Sin álbum',
            url: safe(t.url), cover: '', local: false, date: safe(t.addedDate) };
    }
    function play(tracks, id) {
        const items = tracks.filter(t => t.url);
        const index = items.findIndex(t => t.id === id);
        if (items.length && index >= 0 && window.SpotifyNetPlayer?.playTracks) {
            window.SpotifyNetPlayer.playTracks(items, index);
        }
    }
    async function unlike(id) {
        const body = new FormData(); body.append('id', id); body.append('slug', 'tus-me-gusta');
        const response = await fetch('/Biblioteca/QuitarDeLista', {
            method:'POST', credentials:'same-origin',
            headers: { 'RequestVerificationToken': antiForgery() }, body
        });
        const json = await response.json().catch(() => ({}));
        if (!response.ok) throw new Error(json.error || 'No se pudo quitar esta canción.');
    }
    async function activate() {
        const root = document.getElementById('snGFavorites');
        if (!root || root.dataset.snGInit === '1') return;
        root.dataset.snGInit = '1';
        const version = ++requestId;
        const list = $(root,'snGTracks'), count=$(root,'snGCount');
        const search=$(root,'snGSearch'), sort=$(root,'snGSort');
        const playAll=$(root,'snGPlayAll'), feedback=$(root,'snGFeedback');
        let songs=[];
        function status(message) { feedback.textContent=message; }
        function sorted() {
            let array=songs.slice();
            const q=search.value.trim().toLocaleLowerCase('es');
            if(q) array=array.filter(t=>[t.title,t.artist,t.album].some(v=>safe(v).toLocaleLowerCase('es').includes(q)));
            const compare=(a,b,key)=>safe(a[key]).localeCompare(safe(b[key]),'es',{sensitivity:'base'});
            if(sort.value==='recent') array.reverse();
            if(sort.value==='title') array.sort((a,b)=>compare(a,b,'title'));
            if(sort.value==='artist') array.sort((a,b)=>compare(a,b,'artist'));
            return array;
        }
        function render() {
            if(!root.isConnected) return;
            list.replaceChildren();
            count.textContent=`${songs.length} ${songs.length===1?'canción guardada':'canciones guardadas'}`;
            playAll.disabled=!songs.some(t=>t.url);
            const shown=sorted();
            if(!songs.length) {
                const empty=element('div','sn-g-empty');
                empty.append(element('strong','','Todavía no tienes canciones favoritas'));
                empty.append(element('p','','Pulsa el corazón ♡ junto a una canción de tu biblioteca.'));
                const link=element('a','','Ir a mi música');link.href='/Catalogo';empty.append(link);
                list.append(empty);return;
            }
            if(!shown.length) { list.append(element('p','sn-g-message','No hay canciones que coincidan con tu búsqueda.'));return; }
            const fragment=document.createDocumentFragment();
            shown.forEach((t,i)=>{
                const row=element('div','sn-g-item');
                row.append(element('span','sn-g-number',i+1));
                const titleArea=element('div','sn-g-titlearea');
                titleArea.append(element('span','sn-g-track-icon','♫'));
                const info=element('div','sn-g-info');
                info.append(element('strong','sn-g-title',t.title));
                info.append(element('small','sn-g-sub',t.artist));
                titleArea.append(info);row.append(titleArea);
                row.append(element('span','sn-g-album',t.album));
                row.append(element('span','sn-g-date',t.date));
                const actions=element('div','sn-g-item-actions');
                const btn=element('button','sn-g-track-play','▶');
                btn.type='button';btn.title='Reproducir '+t.title;btn.setAttribute('aria-label',btn.title);
                btn.addEventListener('click',()=>play(sorted().map(asTrack),t.id));actions.append(btn);
                const heart=element('button','sn-g-unlike','♥');
                heart.type='button';heart.title='Quitar de Tus me gusta';heart.setAttribute('aria-label','Quitar '+t.title+' de Tus me gusta');
                heart.addEventListener('click',async()=>{
                    if(heart.disabled)return;heart.disabled=true;
                    try {await unlike(t.id);songs=songs.filter(x=>x.id!==t.id);status('Canción quitada de Tus me gusta.');render();}
                    catch(e){status(e.message||'Error al quitar favorito.');heart.disabled=false;}
                });actions.append(heart);row.append(actions);fragment.append(row);
            });list.append(fragment);
        }
        search.addEventListener('input',render);
        sort.addEventListener('change',render);
        playAll.addEventListener('click',()=>{const tracks=sorted().map(asTrack);if(tracks.length)play(tracks,tracks[0].id);});
        try {
            const response=await fetch('/Biblioteca/Canciones?slug=tus-me-gusta',{credentials:'same-origin',cache:'no-store',headers:{'Accept':'application/json'}});
            if(!response.ok)throw new Error('No se pudo consultar Tus me gusta.');
            const data=await response.json();
            if(version!==requestId||!root.isConnected)return;
            songs=Array.isArray(data.tracks)?data.tracks.filter(t=>t && t.id && t.url).map(t=>({...asTrack(t)})):[];
            render();
        } catch(err) { if(root.isConnected)list.replaceChildren(element('p','sn-g-message',err.message||'No se pudo cargar la lista.')); }
    }
    document.addEventListener('DOMContentLoaded',activate);
    document.addEventListener('spotifynet:navegacion',activate);
})();
