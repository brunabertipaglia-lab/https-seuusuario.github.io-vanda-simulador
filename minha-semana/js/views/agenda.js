// Agenda com visualização semanal e mensal.
import { h, icone } from '../ui.js';
import {
  compromissosDoDia, inicioDaSemana, diasDaSemana, somarDias, formatarCurta, formatarLonga,
  deIso, isoData, MESES, DIAS_CURTOS, CATEGORIAS, maiuscula,
} from '../logic.js';
import { itemCompromisso } from './comum.js';

const estado = { modo: 'semana', selecionada: null };

export function dataSelecionadaAgenda(hoje) {
  return estado.selecionada || hoje;
}

export function renderAgenda(raiz, { dados, hoje, redesenhar }) {
  if (!estado.selecionada) estado.selecionada = hoje;
  const sel = estado.selecionada;
  const mover = (dias) => { estado.selecionada = somarDias(sel, dias); redesenhar(); };
  const moverMes = (n) => {
    const d = deIso(sel);
    d.setDate(1);
    d.setMonth(d.getMonth() + n);
    estado.selecionada = isoData(d);
    redesenhar();
  };

  const titulo = estado.modo === 'semana'
    ? `Semana de ${formatarCurta(inicioDaSemana(sel))}`
    : maiuscula(`${MESES[deIso(sel).getMonth()]} de ${deIso(sel).getFullYear()}`);

  raiz.append(
    h('header', { class: 'cabecalho' }, h('h1', {}, 'Agenda')),
    h('div', { class: 'alternar-modo', role: 'tablist', 'aria-label': 'Modo de visualização' },
      ['semana', 'mes'].map((m) =>
        h('button', {
          type: 'button', role: 'tab', 'aria-selected': String(estado.modo === m),
          class: estado.modo === m ? 'ativo' : '',
          onclick: () => { estado.modo = m; redesenhar(); },
        }, m === 'semana' ? 'Semana' : 'Mês'),
      ),
    ),
    h('div', { class: 'navegador' },
      h('button', {
        class: 'botao-icone', type: 'button', 'aria-label': estado.modo === 'semana' ? 'Semana anterior' : 'Mês anterior',
        onclick: () => (estado.modo === 'semana' ? mover(-7) : moverMes(-1)),
      }, icone('esquerda')),
      h('div', { class: 'navegador-centro' },
        h('strong', { 'aria-live': 'polite' }, titulo),
        sel !== hoje ? h('button', { class: 'botao texto pequeno', type: 'button', onclick: () => { estado.selecionada = hoje; redesenhar(); } }, 'Voltar para hoje') : null,
      ),
      h('button', {
        class: 'botao-icone', type: 'button', 'aria-label': estado.modo === 'semana' ? 'Próxima semana' : 'Próximo mês',
        onclick: () => (estado.modo === 'semana' ? mover(7) : moverMes(1)),
      }, icone('direita')),
    ),
    estado.modo === 'semana' ? vistaSemana(dados, sel, hoje, redesenhar) : vistaMes(dados, sel, hoje, redesenhar),
  );
}

function vistaSemana(dados, sel, hoje, redesenhar) {
  return h('div', { class: 'semana' },
    diasDaSemana(inicioDaSemana(sel)).map((dia) => {
      const itens = compromissosDoDia(dados.compromissos, dia);
      return h('section', { class: `dia ${dia === hoje ? 'hoje' : ''} ${dia === sel ? 'selecionado' : ''}` },
        h('button', {
          class: 'dia-cabecalho', type: 'button', 'aria-pressed': String(dia === sel),
          onclick: () => { estado.selecionada = dia; redesenhar(); },
        },
          h('span', {}, maiuscula(formatarLonga(dia))),
          dia === hoje ? h('span', { class: 'selo-hoje' }, 'hoje') : null,
        ),
        itens.length
          ? h('ul', { class: 'lista-itens' }, itens.map((c) => itemCompromisso(c, dia, { compacto: true })))
          : h('p', { class: 'apoio vazio-dia' }, 'Livre'),
      );
    }),
  );
}

function vistaMes(dados, sel, hoje, redesenhar) {
  const d = deIso(sel);
  const primeiro = isoData(new Date(d.getFullYear(), d.getMonth(), 1));
  const inicioGrade = inicioDaSemana(primeiro);
  const mes = d.getMonth();
  const celulas = [];
  for (let i = 0; i < 42; i++) {
    const dia = somarDias(inicioGrade, i);
    if (i >= 35 && deIso(dia).getMonth() !== mes) break;
    const itens = compromissosDoDia(dados.compromissos, dia);
    const foraDoMes = deIso(dia).getMonth() !== mes;
    const cats = [...new Set(itens.map((c) => c.categoria))];
    celulas.push(
      h('button', {
        type: 'button',
        class: `celula ${foraDoMes ? 'fora' : ''} ${dia === hoje ? 'hoje' : ''} ${dia === sel ? 'selecionado' : ''}`,
        'aria-label': `${formatarLonga(dia)}: ${itens.length} ${itens.length === 1 ? 'compromisso' : 'compromissos'}`,
        'aria-pressed': String(dia === sel),
        onclick: () => { estado.selecionada = dia; redesenhar(); },
      },
        h('span', { class: 'num' }, String(deIso(dia).getDate())),
        h('span', { class: 'pontos', 'aria-hidden': 'true' },
          cats.slice(0, 4).map((c) => h('span', { class: `ponto cat-${CATEGORIAS[c].cor}` }))),
      ),
    );
  }
  const ordemCab = [1, 2, 3, 4, 5, 6, 0];
  const itensSel = compromissosDoDia(dados.compromissos, sel);
  return h('div', {},
    h('div', { class: 'mes', role: 'grid' },
      ordemCab.map((i) => h('span', { class: 'mes-cab', 'aria-hidden': 'true' }, DIAS_CURTOS[i])),
      celulas,
    ),
    h('section', { class: 'secao' },
      h('h2', {}, maiuscula(formatarLonga(sel))),
      itensSel.length
        ? h('ul', { class: 'lista-itens' }, itensSel.map((c) => itemCompromisso(c, sel)))
        : h('p', { class: 'apoio' }, 'Nenhum compromisso. Toque em + para adicionar neste dia.'),
    ),
  );
}
