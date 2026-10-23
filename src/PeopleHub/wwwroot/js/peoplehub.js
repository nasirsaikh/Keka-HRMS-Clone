(() => {
  'use strict';
  const token = document.querySelector('meta[name="csrf-token"]')?.content || '';
  document.body.addEventListener('htmx:configRequest', event => {
    event.detail.headers['RequestVerificationToken'] = token;
  });
  document.body.addEventListener('htmx:beforeSwap', event => {
    if (event.detail.xhr.status === 422) {
      event.detail.shouldSwap = true;
      event.detail.isError = false;
    }
  });
  document.body.addEventListener('htmx:responseError', event => {
    const message = (event.detail.xhr.responseText || 'Request failed').slice(0, 250);
    const holder = document.querySelector('#toast-holder');
    if (holder) holder.textContent = 'Unable to complete action: ' + message.replace(/<[^>]*>/g, '');
  });
  document.body.addEventListener('hrms:close-modal', () => {document.querySelector('#modal-host').replaceChildren();});
  document.body.addEventListener('click', event => {
    if (event.target.closest('[data-close-modal]') || event.target.matches('[data-modal-backdrop]')) document.querySelector('#modal-host').replaceChildren();
    const exportButton = event.target.closest('[data-export-table]');
    if (exportButton) {
      const table = document.querySelector(exportButton.dataset.exportTable);
      if (!table) return;
      const rows = [...table.querySelectorAll('tr')].map(row => [...row.querySelectorAll('th,td')].slice(0,-1).map(cell => '"' + cell.textContent.trim().replaceAll('"','""') + '"').join(','));
      const blob = new Blob([rows.join('\r\n')], {type:'text/csv;charset=utf-8'});
      const link = document.createElement('a');link.href=URL.createObjectURL(blob);link.download='peoplehub-export.csv';link.click();setTimeout(() => URL.revokeObjectURL(link.href), 500);
    }
    if (event.target.closest('[data-toggle-mode]')) {
      let t;try{t=JSON.parse(localStorage.getItem('peoplehub-theme')||'{}')}catch{t={}};
      t.mode=document.documentElement.dataset.bsTheme==='dark'?'light':'dark';localStorage.setItem('peoplehub-theme',JSON.stringify(t));location.reload();
    }
  });
  document.addEventListener('keydown', event => {if (event.key==='Escape')document.querySelector('#modal-host')?.replaceChildren()});
  document.querySelector('#nav-filter')?.addEventListener('input', event => {
    const needle=event.target.value.trim().toLowerCase();
    document.querySelectorAll('.ph-module-link').forEach(link=>{link.hidden=!link.dataset.moduleTitle.includes(needle)});
  });
})();
