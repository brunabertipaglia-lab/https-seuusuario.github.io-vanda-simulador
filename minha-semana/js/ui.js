// Utilitários de interface: criação de elementos, ícones, folhas modais e avisos.

export function h(tag, attrs = {}, ...filhos) {
  const el = document.createElement(tag);
  for (const [k, v] of Object.entries(attrs || {})) {
    if (v === false || v === null || v === undefined) continue;
    if (k === 'class') el.className = v;
    else if (k === 'style') el.setAttribute('style', v);
    else if (k.startsWith('on') && typeof v === 'function') el.addEventListener(k.slice(2).toLowerCase(), v);
    else if (k === 'value') el.value = v;
    else if (k === 'checked') el.checked = Boolean(v);
    else if (v === true) el.setAttribute(k, '');
    else el.setAttribute(k, v);
  }
  anexar(el, filhos);
  return el;
}

function anexar(el, filhos) {
  for (const f of filhos.flat(Infinity)) {
    if (f === null || f === undefined || f === false) continue;
    el.append(f instanceof Node ? f : document.createTextNode(String(f)));
  }
}

// Ícones de traço simples, desenhados em grade 24×24. São decorativos: o texto ao lado sempre nomeia a função.
const CAMINHOS = {
  licao: '<path d="M3 5h6a3 3 0 0 1 3 3v12a2.5 2.5 0 0 0-2.5-2.5H3z"/><path d="M21 5h-6a3 3 0 0 0-3 3v12a2.5 2.5 0 0 1 2.5-2.5H21z"/>',
  prova: '<rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V3h6v1"/><path d="M9 13l2 2 4-4"/>',
  treino: '<path d="M3 12h4l3-7 4 14 3-7h4"/>',
  estudo: '<path d="M9 18h6M10 21h4"/><path d="M12 3a6 6 0 0 0-3.5 10.9V16h7v-2.1A6 6 0 0 0 12 3z"/>',
  projeto: '<path d="M5 21V4"/><path d="M5 4h12l-2.5 4L17 12H5"/>',
  outro: '<circle cx="12" cy="12" r="8"/><circle cx="12" cy="12" r="1.5" fill="currentColor"/>',
  hoje: '<circle cx="12" cy="12" r="4"/><path d="M12 2v2M12 20v2M2 12h2M20 12h2M4.9 4.9l1.4 1.4M17.7 17.7l1.4 1.4M4.9 19.1l1.4-1.4M17.7 6.3l1.4-1.4"/>',
  agenda: '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
  metas: '<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1.5" fill="currentColor"/>',
  revisao: '<path d="M4 20V10M10 20V4M16 20v-7M22 20H2"/>',
  mais: '<path d="M12 5v14M5 12h14"/>',
  check: '<path d="M5 12.5l4.5 4.5L19 7.5"/>',
  esquerda: '<path d="M15 5l-7 7 7 7"/>',
  direita: '<path d="M9 5l7 7-7 7"/>',
  fechar: '<path d="M6 6l12 12M18 6L6 18"/>',
  ajustes: '<path d="M4 6h10M18 6h2M4 12h4M12 12h8M4 18h12"/><circle cx="16" cy="6" r="2"/><circle cx="10" cy="12" r="2"/><circle cx="18" cy="18" r="2"/>',
  pessoas: '<circle cx="9" cy="8" r="3.5"/><path d="M2.5 20a6.5 6.5 0 0 1 13 0"/><path d="M16 4.5a3.5 3.5 0 0 1 0 7M18 14a6.5 6.5 0 0 1 3.5 6"/>',
  copiar: '<rect x="8" y="8" width="12" height="12" rx="2"/><path d="M16 8V5a1 1 0 0 0-1-1H5a1 1 0 0 0-1 1v10a1 1 0 0 0 1 1h3"/>',
  lixeira: '<path d="M4 7h16M10 11v6M14 11v6M6 7l1 13h10l1-13M9 7V4h6v3"/>',
  relogio: '<circle cx="12" cy="12" r="9"/><path d="M12 7v5l3 2"/>',
};

export function icone(nome, classe = 'icone') {
  const span = document.createElement('span');
  span.className = classe;
  span.setAttribute('aria-hidden', 'true');
  span.innerHTML = `<svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">${CAMINHOS[nome] || ''}</svg>`;
  return span;
}

let temporizadorAviso;
export function aviso(texto) {
  const el = document.getElementById('aviso');
  el.textContent = texto;
  el.classList.add('visivel');
  clearTimeout(temporizadorAviso);
  temporizadorAviso = setTimeout(() => el.classList.remove('visivel'), 3200);
}

// Folha modal na parte de baixo da tela, baseada em <dialog> (foco e tecla Esc nativos).
export function abrirFolha(titulo, montarConteudo) {
  const dlg = h('dialog', { class: 'folha', 'aria-labelledby': 'folha-titulo' });
  const fechar = () => dlg.close();
  dlg.addEventListener('close', () => dlg.remove());
  dlg.addEventListener('click', (e) => {
    if (e.target === dlg) fechar();
  });
  const corpo = h('div', { class: 'folha-corpo' });
  dlg.append(
    h('div', { class: 'folha-topo' },
      h('h2', { id: 'folha-titulo' }, titulo),
      h('button', { class: 'botao-icone', type: 'button', 'aria-label': 'Fechar', onclick: fechar }, icone('fechar')),
    ),
    corpo,
  );
  anexar(corpo, [montarConteudo(fechar)]);
  document.body.append(dlg);
  dlg.showModal();
  const primeiro = corpo.querySelector('[autofocus]');
  if (primeiro) primeiro.focus();
  return dlg;
}

export function confirmar(titulo, texto, rotuloOk = 'Confirmar') {
  return new Promise((resolve) => {
    let resposta = false;
    const dlg = abrirFolha(titulo, (f) =>
      h('div', {},
        h('p', {}, texto),
        h('div', { class: 'acoes-linha' },
          h('button', { class: 'botao secundario', type: 'button', onclick: f }, 'Cancelar'),
          h('button', { class: 'botao perigo', type: 'button', onclick: () => { resposta = true; f(); } }, rotuloOk),
        ),
      ),
    );
    dlg.addEventListener('close', () => resolve(resposta));
  });
}

export function barraProgresso(atual, alvo, rotulo) {
  const pct = alvo ? Math.min(100, Math.round((atual / alvo) * 100)) : 0;
  return h('div', { class: 'progresso' },
    h('div', {
      class: 'progresso-trilho',
      role: 'progressbar',
      'aria-valuemin': '0',
      'aria-valuemax': String(alvo),
      'aria-valuenow': String(Math.min(atual, alvo)),
      'aria-label': rotulo,
    }, h('div', { class: 'progresso-barra', style: `width:${pct}%` })),
    h('span', { class: 'progresso-texto' }, rotulo),
  );
}

export function campo(rotulo, controle, ajuda) {
  const id = controle.id || `c-${Math.random().toString(36).slice(2, 9)}`;
  controle.id = id;
  return h('div', { class: 'campo' },
    h('label', { for: id }, rotulo),
    controle,
    ajuda ? h('p', { class: 'ajuda' }, ajuda) : null,
  );
}

// Grupo de opções exclusivas (botões de rádio estilizados como fichas).
export function fichas(nome, opcoes, selecionado, aoMudar, rotuloGrupo) {
  return h('div', { class: 'fichas', role: 'radiogroup', 'aria-label': rotuloGrupo },
    opcoes.map((o) =>
      h('label', { class: `ficha ${o.classe || ''}` },
        h('input', {
          type: 'radio', name: nome, value: o.valor, checked: o.valor === selecionado,
          onchange: () => aoMudar(o.valor),
        }),
        o.icone ? icone(o.icone, 'icone pequeno') : null,
        h('span', {}, o.rotulo),
      ),
    ),
  );
}

// Seleção múltipla de dias da semana (segunda a domingo).
export function seletorDias(selecionados, aoMudar, rotuloGrupo = 'Dias da semana') {
  const nomes = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];
  const valores = [1, 2, 3, 4, 5, 6, 0];
  const atual = new Set(selecionados);
  return h('div', { class: 'fichas dias', role: 'group', 'aria-label': rotuloGrupo },
    valores.map((v, i) =>
      h('label', { class: 'ficha' },
        h('input', {
          type: 'checkbox', value: String(v), checked: atual.has(v),
          onchange: (e) => {
            if (e.target.checked) atual.add(v);
            else atual.delete(v);
            aoMudar([...atual]);
          },
        }),
        h('span', {}, nomes[i]),
      ),
    ),
  );
}

export async function copiarTexto(texto) {
  try {
    await navigator.clipboard.writeText(texto);
    return true;
  } catch {
    return false;
  }
}

export function baixarArquivo(nome, conteudo, tipo) {
  const url = URL.createObjectURL(new Blob([conteudo], { type: tipo }));
  const a = h('a', { href: url, download: nome });
  document.body.append(a);
  a.click();
  a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}
