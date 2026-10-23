(() => {
  'use strict';
  const defaults = {primary:'#4f46e5',success:'#16a34a',warning:'#d97706',danger:'#dc2626',info:'#0891b2',sidebar_bg:'#18243a',body_bg:'#f5f7fb',density:'compact',sidebar_mode:'dark',header_mode:'light',mode:'light',font_family:'system',font_size:13,radius:8};
  const presets = {Indigo:{},AdminLTE:{primary:'#0d6efd',sidebar_bg:'#343a40'},Ocean:{primary:'#0891b2',sidebar_bg:'#0f2b3d'},Emerald:{primary:'#059669',sidebar_bg:'#064e3b'},Sunset:{primary:'#ea580c',sidebar_bg:'#1c1917'},Slate:{primary:'#475569',sidebar_bg:'#0f172a',radius:3},Rose:{primary:'#e11d48',sidebar_bg:'#fff7f8',sidebar_mode:'light'}};
  const keys=['primary','sidebar_bg','body_bg','success','warning','danger','info'];
  const choices={density:['compact','comfortable'],sidebar_mode:['dark','light'],header_mode:['light','dark','primary'],mode:['light','dark','system'],font_family:['system','inter','roboto','nunito','manrope']};
  let theme = read();
  function read(){try{return {...defaults,...JSON.parse(localStorage.getItem('peoplehub-theme')||'{}')}}catch{return {...defaults}}}
  function validate(o){
    if (!o || typeof o!=='object' || Array.isArray(o))throw Error('Theme must be an object');
    for(const [key,v] of Object.entries(o)){
      if(!(key in defaults))throw Error('Unknown token: '+key);
      if(keys.includes(key)&&!(typeof v==='string'&&/^#[0-9a-fA-F]{6}$/.test(v)))throw Error('Invalid hex colour for '+key);
      if(key in choices&&!choices[key].includes(v))throw Error('Invalid '+key);
      if(key==='radius'&&!(Number.isInteger(v)&&v>=0&&v<=16))throw Error('Invalid radius');
      if(key==='font_size'&&!(Number.isInteger(v)&&v>=12&&v<=16))throw Error('Invalid font size');
    }
    return {...defaults,...o};
  }
  const colors=document.querySelector('#theme-colors'),options=document.querySelector('#theme-options'),presetsHost=document.querySelector('#theme-presets');
  if(!colors)return;
  const human=s=>s.replaceAll('_',' ').replace(/\b\w/g,c=>c.toUpperCase());
  const escape=s=>String(s).replace(/[&<>"']/g,c=>({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;',"'":'&#39;'}[c]));
  presetsHost.innerHTML=Object.entries(presets).map(([name,p])=>'<button class="ph-preset" type="button" data-preset="'+name+'"><span style="background:'+escape(p.sidebar_bg||defaults.sidebar_bg)+'"><i style="background:'+escape(p.primary||defaults.primary)+'"></i></span>'+name+'</button>').join('');
  colors.innerHTML=keys.map(key=>'<div class="ph-color-row"><label for="color-'+key+'">'+human(key)+'</label><div><input id="color-'+key+'" type="color" data-color="'+key+'"/><span data-color-label="'+key+'"></span></div></div>').join('');
  options.innerHTML=Object.entries(choices).map(([key,values])=>'<div class="ph-choice-row"><label>'+human(key)+'</label><div class="ph-segmented">'+values.map(v=>'<button type="button" data-choice="'+key+'" data-value="'+v+'">'+human(v)+'</button>').join('')+'</div></div>').join('');
  function css(){return ':root {\n'+keys.map(k=>'  --ph-'+k.replaceAll('_','-')+': '+theme[k]+';').join('\n')+'\n  --ph-font-size: '+theme.font_size+'px;\n  --ph-radius: '+theme.radius+'px;\n}';}
  function render(){
    const root=document.documentElement;
    for(const k of keys)root.style.setProperty('--ph-'+k.replaceAll('_','-'),theme[k]);
    root.style.setProperty('--ph-font-size',theme.font_size+'px');root.style.setProperty('--ph-radius',theme.radius+'px');
    root.style.setProperty('--ph-font',theme.font_family==='system'?'system-ui,sans-serif':theme.font_family+',system-ui,sans-serif');
    root.dataset.bsTheme=theme.mode==='system'?(matchMedia('(prefers-color-scheme:dark)').matches?'dark':'light'):theme.mode;
    root.dataset.density=theme.density;root.dataset.sidebar=theme.sidebar_mode;root.dataset.header=theme.header_mode;
    for(const k of keys){document.querySelector('[data-color="'+k+'"]').value=theme[k];document.querySelector('[data-color-label="'+k+'"]').textContent=theme[k]}
    document.querySelectorAll('[data-choice]').forEach(b=>b.classList.toggle('active',theme[b.dataset.choice]===b.dataset.value));
    document.querySelector('#theme-font-size').value=theme.font_size;document.querySelector('#theme-radius').value=theme.radius;
    document.querySelector('#theme-font-output').textContent=theme.font_size+'px';document.querySelector('#theme-radius-output').textContent=theme.radius+'px';
    const json=JSON.stringify(theme,null,2);const jsonEditor=document.querySelector('#theme-json');if(document.activeElement!==jsonEditor)jsonEditor.value=json;
    document.querySelector('#theme-hidden-tokens').value=JSON.stringify(theme);
    localStorage.setItem('peoplehub-theme',JSON.stringify(theme));
  }
  function patch(values){try{theme=validate({...theme,...values});render()}catch(e){alert(e.message)}}
  presetsHost.addEventListener('click',e=>{const btn=e.target.closest('[data-preset]');if(btn){theme=validate({...defaults,...presets[btn.dataset.preset]});render()}});
  colors.addEventListener('input',e=>{if(e.target.dataset.color)patch({[e.target.dataset.color]:e.target.value})});
  options.addEventListener('click',e=>{const btn=e.target.closest('[data-choice]');if(btn)patch({[btn.dataset.choice]:btn.dataset.value})});
  for(const [element,key] of [['#theme-font-size','font_size'],['#theme-radius','radius']])document.querySelector(element).addEventListener('input',e=>patch({[key]:Number(e.target.value)}));
  document.querySelector('#theme-reset').addEventListener('click',()=>{theme={...defaults};render()});
  document.querySelector('#theme-generate').addEventListener('click',()=>patch({success:'#198754',warning:'#ffc107',danger:'#dc3545',info:'#0dcaf0'}));
  document.querySelector('#theme-random').addEventListener('click',()=>{const palette=['#6366f1','#059669','#7c3aed','#e11d48','#0891b2','#ea580c'];patch({primary:palette[Math.floor(Math.random()*palette.length)]})});
  document.querySelectorAll('[data-theme-tab]').forEach(btn=>btn.addEventListener('click',()=>{
    document.querySelectorAll('[data-theme-tab]').forEach(x=>x.classList.toggle('active',x===btn));
    document.querySelectorAll('[data-theme-panel]').forEach(x=>x.classList.toggle('d-none',x.dataset.themePanel!==btn.dataset.themeTab));
  }));
  document.querySelectorAll('[data-theme-json]').forEach(btn=>btn.addEventListener('click',()=>{try{theme=validate(JSON.parse(decodeURIComponent(escape(window.atob(btn.dataset.themeJson)))));render()}catch(err){alert('Invalid saved theme: '+err.message)}}));
  document.querySelector('#theme-import').addEventListener('click',()=>{try{theme=validate(JSON.parse(document.querySelector('#theme-json').value));render()}catch(e){alert(e.message)}});
  document.querySelector('#theme-copy-json').addEventListener('click',()=>navigator.clipboard.writeText(JSON.stringify(theme,null,2)));
  document.querySelector('#theme-copy-css').addEventListener('click',()=>navigator.clipboard.writeText(css()));
  render();
})();
