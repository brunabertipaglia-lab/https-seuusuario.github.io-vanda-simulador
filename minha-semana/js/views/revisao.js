// Revisão semanal: resumo automático, perguntas reflexivas e evolução de 4 semanas.
import { h, icone, campo, aviso, EMBUTIDO } from '../ui.js';
import {
  resumoDaSemana, evolucao, semanaParaRevisar, somarDias, formatarCurta, CATEGORIAS, NOMES_STATUS,
} from '../logic.js';
import * as store from '../store.js';

const estado = { semana: null };

export function renderRevisao(raiz, { dados, hoje, redesenhar }) {
  if (!estado.semana) estado.semana = semanaParaRevisar(hoje, dados.revisoes);
  const semana = estado.semana;
  const resumo = resumoDaSemana(dados, semana);
  const salva = dados.revisoes.find((r) => r.semana_inicio === semana) || {};
  const respostas = { funcionou: salva.funcionou || '', atrapalhou: salva.atrapalhou || '', ajuste: salva.ajuste || '' };
  const mover = (n) => { estado.semana = somarDias(semana, 7 * n); redesenhar(); };

  const pergunta = (chave, rotulo, ajuda) => {
    const t = h('textarea', { rows: '3', maxlength: '600', oninput: (e) => { respostas[chave] = e.target.value; } });
    t.value = respostas[chave];
    return campo(rotulo, t, ajuda);
  };

  const categorias = Object.entries(resumo.porCategoria).filter(([, v]) => v.total > 0);

  raiz.append(
    h('header', { class: 'cabecalho' },
      h('h1', {}, 'Revisão da semana'),
      EMBUTIDO ? null : h('button', { class: 'botao secundario pequeno nao-imprimir', type: 'button', onclick: () => window.print() }, 'Salvar em PDF'),
    ),
    h('div', { class: 'navegador nao-imprimir' },
      h('button', { class: 'botao-icone', type: 'button', 'aria-label': 'Semana anterior', onclick: () => mover(-1) }, icone('esquerda')),
      h('div', { class: 'navegador-centro' }, h('strong', { 'aria-live': 'polite' }, `Semana de ${formatarCurta(semana)} a ${formatarCurta(somarDias(semana, 6))}`)),
      h('button', { class: 'botao-icone', type: 'button', 'aria-label': 'Próxima semana', onclick: () => mover(1) }, icone('direita')),
    ),
    h('p', { class: 'so-imprimir' }, `Semana de ${formatarCurta(semana)} a ${formatarCurta(somarDias(semana, 6))}`),

    h('section', { class: 'secao' },
      h('h2', {}, 'Metas'),
      resumo.totalMetas
        ? [
          h('div', { class: 'placar' },
            ['cumprida', 'parcial', 'nao_cumprida'].map((st) =>
              h('div', { class: `placar-item st-${st}` },
                h('span', { class: 'placar-num' }, String(resumo.metas[st].length)),
                h('span', {}, NOMES_STATUS[st]),
              ),
            ),
          ),
          h('ul', { class: 'lista-simples' },
            ['cumprida', 'parcial', 'nao_cumprida'].flatMap((st) =>
              resumo.metas[st].map(({ meta, atual }) =>
                h('li', {},
                  h('span', { class: `status st-${st}` }, NOMES_STATUS[st]),
                  ` ${meta.descricao} — ${atual}/${meta.valor_alvo} ${meta.unidade}`,
                ),
              ),
            ),
          ),
        ]
        : h('p', { class: 'apoio' }, 'Nenhuma meta definida nesta semana.'),
    ),

    h('section', { class: 'secao' },
      h('h2', {}, 'Compromissos concluídos'),
      categorias.length
        ? h('table', { class: 'tabela' },
          h('thead', {}, h('tr', {}, h('th', { scope: 'col' }, 'Categoria'), h('th', { scope: 'col' }, 'Concluídos'))),
          h('tbody', {},
            categorias.map(([k, v]) =>
              h('tr', {},
                h('td', {}, h('span', { class: `etiqueta cat-${CATEGORIAS[k].cor}` }, icone(k, 'icone mini'), CATEGORIAS[k].nome)),
                h('td', {}, `${v.feitos} de ${v.total}`),
              ),
            ),
          ),
        )
        : h('p', { class: 'apoio' }, 'Nenhum compromisso registrado nesta semana.'),
    ),

    h('section', { class: 'secao' },
      h('h2', {}, 'Evolução nas últimas 4 semanas'),
      graficoEvolucao(evolucao(dados, semana, 4)),
    ),

    h('section', { class: 'secao' },
      h('h2', {}, 'Reflexão'),
      h('form', {
        onsubmit: (e) => {
          e.preventDefault();
          store.salvarRevisao(semana, {
            funcionou: respostas.funcionou.trim(),
            atrapalhou: respostas.atrapalhou.trim(),
            ajuste: respostas.ajuste.trim(),
          });
          aviso('Revisão salva.');
        },
      },
        pergunta('funcionou', 'O que funcionou?', 'Que estratégia ou hábito ajudou você?'),
        pergunta('atrapalhou', 'O que atrapalhou?', 'Sem julgamento: só observe o que aconteceu.'),
        pergunta('ajuste', 'O que vou ajustar na próxima semana?'),
        h('div', { class: 'acoes-linha nao-imprimir' },
          h('button', { class: 'botao secundario', type: 'submit' }, 'Salvar revisão'),
          h('a', { class: 'botao primario', href: `#/metas/nova?semana=${somarDias(semana, 7)}` }, 'Planejar próxima semana'),
        ),
      ),
    ),
  );
}

// Barras verticais com a média de progresso das metas em cada semana (0–100%).
function graficoEvolucao(semanas) {
  if (semanas.every((s) => s.media === null)) {
    return h('p', { class: 'apoio' }, 'O gráfico aparece quando houver metas registradas.');
  }
  const L = 320; const A = 170; const base = 130; const alturaMax = 100; const largura = 44;
  const passo = L / semanas.length;
  const ns = 'http://www.w3.org/2000/svg';
  const svg = document.createElementNS(ns, 'svg');
  svg.setAttribute('viewBox', `0 0 ${L} ${A}`);
  svg.setAttribute('class', 'grafico');
  svg.setAttribute('role', 'img');
  const descricao = semanas.map((s) => `semana de ${formatarCurta(s.semana_inicio)}: ${s.media === null ? 'sem metas' : `${s.media}% de progresso médio, ${s.cumpridas} de ${s.total} metas cumpridas`}`).join('; ');
  svg.setAttribute('aria-label', `Progresso médio das metas por semana — ${descricao}`);

  const el = (tag, attrs, texto) => {
    const n = document.createElementNS(ns, tag);
    for (const [k, v] of Object.entries(attrs)) n.setAttribute(k, v);
    if (texto !== undefined) n.textContent = texto;
    svg.append(n);
    return n;
  };
  el('line', { x1: 0, x2: L, y1: base, y2: base, class: 'eixo' });
  el('line', { x1: 0, x2: L, y1: base - alturaMax, y2: base - alturaMax, class: 'grade' });
  semanas.forEach((s, i) => {
    const cx = passo * i + passo / 2;
    if (s.media !== null) {
      const alt = Math.max(2, (s.media / 100) * alturaMax);
      el('rect', { x: cx - largura / 2, y: base - alt, width: largura, height: alt, rx: 4, class: i === semanas.length - 1 ? 'barra atual' : 'barra' });
      el('text', { x: cx, y: base - alt - 6, class: 'valor', 'text-anchor': 'middle' }, `${s.media}%`);
      el('text', { x: cx, y: base + 32, class: 'rotulo-sec', 'text-anchor': 'middle' }, `${s.cumpridas}/${s.total} cumpridas`);
    } else {
      el('text', { x: cx, y: base - 6, class: 'rotulo-sec', 'text-anchor': 'middle' }, '—');
    }
    el('text', { x: cx, y: base + 17, class: 'rotulo', 'text-anchor': 'middle' }, formatarCurta(s.semana_inicio));
  });
  return h('figure', { class: 'figura' }, svg, h('figcaption', { class: 'apoio' }, 'Média do progresso das metas de cada semana (cada meta conta até 100%).'));
}
