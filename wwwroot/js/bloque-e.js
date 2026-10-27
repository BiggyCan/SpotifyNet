/* SpotifyNet Bloque E: mejoras progresivas que respetan los controladores de música. */
(() => {
  'use strict';
  function foto(cont, cover) {
    if (!cover || !/^\/(?!\/)/.test(cover)) return;
    const fallback=cont.textContent;
    const img=document.createElement('img');
    img.src=cover; img.alt=''; img.loading='lazy';
    cont.textContent='';
    img.addEventListener('error',()=>{img.remove();cont.textContent=fallback;},{once:true});
    cont.append(img);
  }
  function filtrosBiblioteca() {
    const list=document.querySelector('.u-library-filters');
    if(!list || list.parentElement?.classList.contains('sn-e-filter-shell'))return;
    const shell=document.createElement('div'); shell.className='sn-e-filter-shell';
    list.parentElement.insertBefore(shell,list);shell.append(list);
    const next=document.createElement('button');next.type='button';
    next.className='sn-e-filter-next';next.textContent='›';
    next.title='Mostrar más categorías de la biblioteca';
    next.setAttribute('aria-label','Desplazar categorías de la biblioteca');
    next.addEventListener('click',()=>{
      const atEnd=list.scrollLeft+list.clientWidth >= list.scrollWidth-12;
      list.scrollTo({left:atEnd?0:list.scrollLeft+Math.max(140,list.clientWidth*.65),behavior:'smooth'});
    });
    shell.append(next);
    const update=()=>{next.hidden=list.scrollWidth<=list.clientWidth+4;};
    new ResizeObserver(update).observe(list);
    update();
  }
  function expandirSecciones() {
    document.querySelectorAll('.u-home-content .u-music-section').forEach(section=>{
      if(section.dataset.snEEnhanced==='1')return;
      const head=section.querySelector('.u-section-head');
      const grid=section.querySelector('.u-music-grid');
      if(!head||!grid||grid.querySelectorAll('.u-music-card').length<3)return;
      section.dataset.snEEnhanced='1';
      const ref=head.querySelector('span');
      if(ref)ref.hidden=true;
      const button=document.createElement('button');button.type='button';
      button.className='sn-e-show-all';button.textContent='Mostrar todo';
      button.setAttribute('aria-expanded','false');
      button.addEventListener('click',()=>{
        const expanded=section.classList.toggle('sn-e-expanded');
        button.textContent=expanded?'Mostrar menos':'Mostrar todo';
        button.setAttribute('aria-expanded',String(expanded));
      });
      head.append(button);
    });
  }
  function catalogo() {
    const catalog=document.querySelector('#snCatalogPage');
    if(!catalog)return;
    const albumes=new URLSearchParams(location.search).get('vista')==='albumes';
    catalog.querySelectorAll('.sn-cat-section').forEach(section=>{
      if(section.dataset.snEGroup!=='1'){
        const heading=section.querySelector('.sn-cat-section-heading');
        const first=section.querySelector('.sn-cat-song');
        if(heading){
          const wrapper=document.createElement('div');wrapper.className='sn-e-group-heading';
          const art=document.createElement('span');art.className='sn-e-group-art';
          if(!albumes)art.classList.add('sn-e-is-artist');
          art.textContent=albumes?'♫':'♪';
          foto(art,first?.dataset.songCover);
          section.insertBefore(wrapper,heading);wrapper.append(art,heading);
        }
        section.dataset.snEGroup='1';
      }
      section.querySelectorAll('.sn-cat-song').forEach(row=>{
        if(row.dataset.snERow==='1')return;
        row.dataset.snERow='1';
        const art=document.createElement('span');art.className='sn-e-row-art';
        art.textContent='♪';foto(art,row.dataset.songCover);
        const play=row.querySelector('.sn-cat-play');
        if(play)row.insertBefore(art,play);else row.prepend(art);
      });
    });
  }
  let scheduled=false;
  function update() {
    if(scheduled)return;
    scheduled=true;
    requestAnimationFrame(()=>{
      scheduled=false;
      filtrosBiblioteca();expandirSecciones();catalogo();
    });
  }
  document.addEventListener('DOMContentLoaded',()=>{
    update();
    const main=document.querySelector('.u-main');
    if(main)new MutationObserver(update).observe(main,{childList:true,subtree:false});
  });
  document.addEventListener('spotifynet:navegacion',update);
})();
