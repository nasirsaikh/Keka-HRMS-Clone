/* Apply local theme before first paint to avoid a flash of the wrong mode. */
(function () {
  try {
    const t = JSON.parse(localStorage.getItem('peoplehub-theme') || '{}');
    const root = document.documentElement;
    const mode = t.mode === 'system' ? (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light') : t.mode;
    root.dataset.bsTheme = mode || 'light';
    root.dataset.density = t.density || 'compact';
    root.dataset.sidebar = t.sidebar_mode || 'dark';
    root.dataset.header = t.header_mode || 'light';
    Object.entries(t).forEach(([k,v]) => {
      if (/^(primary|success|warning|danger|info|sidebar_bg|body_bg)$/.test(k) && /^#[0-9a-f]{6}$/i.test(v)) root.style.setProperty('--ph-' + k.replaceAll('_','-'), v);
      if (k === 'font_size' && Number.isFinite(v) && v >= 12 && v <= 16) root.style.setProperty('--ph-font-size',v+'px');
      if (k === 'radius' && Number.isInteger(v) && v >= 0 && v <= 16) root.style.setProperty('--ph-radius',v+'px');
      if (k === 'font_family' && /^(system|inter|roboto|nunito|manrope)$/.test(v)) root.style.setProperty('--ph-font',v==='system'?'system-ui,sans-serif':v+',system-ui,sans-serif');
    });
  } catch {}
})();
