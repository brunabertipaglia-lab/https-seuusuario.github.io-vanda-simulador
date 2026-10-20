// Componentes usados em mais de uma tela.
import { h, icone, abrirFolha, campo, barraProgresso, aviso } from '../ui.js';
import {
  CATEGORIAS, alternarConclusao, falhouOntem, MENSAGEM_HOJE_CONTA, MENSAGEM_NOVA_CHANCE,
} from '../logic.js';
import * as store from '../store.js';
import { abrirCompromisso } from './compromisso.js';

export function itemCompromisso(c, iso, { compacto = false } = {}) {
  const cat = CATEGORIAS[c.categoria] || CATEGORIAS.outro;
  const marcar = () => {
    const original = store.dados().compromissos.find((x) => x.id === c.id);
    const novo = alternarConclusao(original, iso);
    store.atualizarCompromisso(c.id, { concluido: novo.concluido, concluidas: novo.concluidas });
  };
  const detalhes = [c.horario, c.materia].filter(Boolean).join(' · ');
  return h('li', { class: `item cat-${cat.cor} ${c.feito ? 'feito' : ''} ${compacto ? 'compacto' : ''}` },
    h('label', { class: 'item-check' },
      h('input', { type: 'checkbox', checked: c.feito, onchange: marcar, 'aria-label': `Concluir: ${c.titulo}` }),
      h('span', { class: 'caixa', 'aria-hidden': 'true' }, icone('check', 'icone pequeno')),
    ),
    h('button', {
      class: 'item-corpo', type: 'button',
      onclick: () => abrirCompromisso({ existente: store.dados().compromissos.find((x) => x.id === c.id) }),
      'aria-label': `Editar ${c.titulo}`,
    },
      h('span', { class: 'item-titulo' }, c.titulo),
      h('span', { class: 'item-meta' },
        h('span', { class: 'etiqueta' }, icone(c.categoria, 'icone mini'), cat.nome),
        detalhes ? h('span', {}, detalhes) : null,
        c.recorrencia ? h('span', { class: 'repete' }, 'semanal') : null,
      ),
      !compacto && c.observacao ? h('span', { class: 'item-obs' }, c.observacao) : null,
    ),
  );
}

export function abrirRegistro(meta, iso, sugerido) {
  abrirFolha('Registrar avanço', (fechar) => {
    const valor = h('input', { type: 'number', inputmode: 'decimal', min: '0', step: 'any', value: String(sugerido), autofocus: true });
    return h('form', {
      onsubmit: (e) => {
        e.preventDefault();
        const v = Number(valor.value);
        if (!Number.isFinite(v) || v <= 0) {
          aviso('Informe um número maior que zero.');
          return;
        }
        store.registrarProgresso(meta.id, iso, v);
        fechar();
        aviso('Avanço registrado.');
      },
    },
      h('p', { class: 'apoio' }, meta.descricao),
      campo(`Quanto você fez (${meta.unidade})?`, valor),
      h('div', { class: 'acoes-linha' }, h('button', { class: 'botao primario', type: 'submit' }, 'Registrar')),
    );
  });
}

export function cartaoAcao(acao, iso, { horaAtual, registros }) {
  const { meta, total, hoje, feitoHoje, sugerido } = acao;
  const tipoDias = meta.frequencia.tipo === 'dias';
  let nota = null;
  if (!feitoHoje && tipoDias) {
    if (falhouOntem(meta, registros, iso)) nota = MENSAGEM_HOJE_CONTA;
    else if (horaAtual >= 20) nota = MENSAGEM_NOVA_CHANCE;
  }
  const rotuloRapido = tipoDias ? `Fiz ${sugerido} ${meta.unidade}` : 'Registrar avanço';

  return h('li', { class: `cartao acao ${feitoHoje ? 'feito' : ''}` },
    h('div', { class: 'acao-topo' },
      h('p', { class: 'acao-titulo' }, meta.descricao),
      h('p', { class: 'acao-hoje' },
        tipoDias ? `Hoje: ${sugerido} ${meta.unidade}` : `Avance hoje se puder (total da semana: ${meta.valor_alvo} ${meta.unidade})`),
    ),
    barraProgresso(total, meta.valor_alvo, `${total}/${meta.valor_alvo} ${meta.unidade}`),
    hoje > 0 ? h('p', { class: 'apoio' }, `Registrado hoje: ${hoje} ${meta.unidade}.`) : null,
    nota ? h('p', { class: 'nota' }, nota) : null,
    h('div', { class: 'acoes-linha esquerda' },
      feitoHoje
        ? h('span', { class: 'feito-selo' }, icone('check', 'icone pequeno'), 'Feito hoje')
        : h('button', {
          class: 'botao primario pequeno', type: 'button',
          onclick: () => {
            if (tipoDias) {
              store.registrarProgresso(meta.id, iso, sugerido);
              aviso('Avanço registrado.');
            } else abrirRegistro(meta, iso, sugerido);
          },
        }, rotuloRapido),
      h('button', { class: 'botao secundario pequeno', type: 'button', onclick: () => abrirRegistro(meta, iso, sugerido) }, 'Outro valor'),
      hoje > 0
        ? h('button', {
          class: 'botao texto pequeno', type: 'button',
          onclick: () => { store.desfazerProgressoDoDia(meta.id, iso); aviso('Registro de hoje desfeito.'); },
        }, 'Desfazer hoje')
        : null,
    ),
  );
}
