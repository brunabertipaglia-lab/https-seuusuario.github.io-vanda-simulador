// Folha de criação e edição de compromissos.
import { h, abrirFolha, campo, fichas, seletorDias, aviso, confirmar, icone } from '../ui.js';
import { CATEGORIAS, validarCompromisso, sugerirRevisoes, formatarLonga, diaDaSemana, isoData } from '../logic.js';
import * as store from '../store.js';

const OPCOES_CATEGORIA = Object.entries(CATEGORIAS).map(([valor, c]) => ({
  valor, rotulo: c.nome, icone: valor, classe: `cat-${c.cor}`,
}));

export function abrirCompromisso({ data, existente } = {}) {
  const c = existente
    ? { ...existente }
    : { titulo: '', categoria: 'licao', data, horario: '', materia: '', observacao: '', recorrencia: null };

  abrirFolha(existente ? 'Editar compromisso' : 'Novo compromisso', (fechar) => {
    const erros = h('div', { class: 'erros', role: 'alert' });

    const titulo = h('input', {
      type: 'text', value: c.titulo, autofocus: !existente, maxlength: '80', autocomplete: 'off',
      placeholder: 'Ex.: Exercícios de frações', oninput: (e) => { c.titulo = e.target.value; },
    });
    const dataInput = h('input', { type: 'date', value: c.data, required: true, oninput: (e) => { c.data = e.target.value; } });
    const horario = h('input', { type: 'time', value: c.horario, oninput: (e) => { c.horario = e.target.value; } });
    const materia = h('input', {
      type: 'text', value: c.materia, maxlength: '40', placeholder: 'Ex.: Matemática',
      oninput: (e) => { c.materia = e.target.value; },
    });
    const obs = h('textarea', { rows: '2', maxlength: '300', oninput: (e) => { c.observacao = e.target.value; } });
    obs.value = c.observacao || '';

    const areaDias = h('div', {});
    const ate = h('input', {
      type: 'date', value: c.recorrencia?.ate || '',
      oninput: (e) => { if (c.recorrencia) c.recorrencia.ate = e.target.value || null; },
    });
    const desenharRecorrencia = () => {
      areaDias.replaceChildren();
      if (!c.recorrencia) return;
      areaDias.append(
        seletorDias(c.recorrencia.dias, (dias) => { c.recorrencia.dias = dias; }, 'Repetir nos dias'),
        campo('Repetir até (opcional)', ate),
      );
    };
    const repetir = h('input', {
      type: 'checkbox', checked: Boolean(c.recorrencia),
      onchange: (e) => {
        c.recorrencia = e.target.checked ? { dias: [diaDaSemana(c.data)], ate: null } : null;
        desenharRecorrencia();
      },
    });
    desenharRecorrencia();

    const temDetalhes = Boolean(c.horario || c.materia || c.observacao || c.recorrencia);
    const detalhes = h('details', { class: 'detalhes', open: temDetalhes },
      h('summary', {}, 'Horário, matéria, observação e repetição'),
      h('div', { class: 'grade-2' }, campo('Horário (opcional)', horario), campo('Matéria (opcional)', materia)),
      campo('Observação (opcional)', obs),
      h('label', { class: 'alternador' }, repetir, h('span', {}, 'Repetir toda semana')),
      areaDias,
    );

    const salvar = (e) => {
      e.preventDefault();
      const dadosC = {
        ...c,
        titulo: c.titulo.trim(),
        materia: (c.materia || '').trim(),
        observacao: (c.observacao || '').trim(),
      };
      const lista = validarCompromisso(dadosC);
      erros.replaceChildren(...lista.map((t) => h('p', {}, t)));
      if (lista.length) return;
      if (existente) {
        store.atualizarCompromisso(existente.id, dadosC);
        aviso('Compromisso atualizado.');
        fechar();
        return;
      }
      const novo = store.adicionarCompromisso(dadosC);
      fechar();
      aviso('Compromisso adicionado.');
      if (novo.categoria === 'prova' && !novo.recorrencia) oferecerRevisoes(novo);
    };

    const remover = async () => {
      fechar();
      const ok = await confirmar('Excluir compromisso', `Excluir "${existente.titulo}"${existente.recorrencia ? ' e todas as repetições' : ''}?`, 'Excluir');
      if (ok) {
        store.removerCompromisso(existente.id);
        aviso('Compromisso excluído.');
      }
    };

    return h('form', { onsubmit: salvar, novalidate: true },
      campo('O que é?', titulo),
      h('fieldset', { class: 'campo' },
        h('legend', {}, 'Categoria'),
        fichas('categoria', OPCOES_CATEGORIA, c.categoria, (v) => { c.categoria = v; }, 'Categoria'),
      ),
      campo('Data', dataInput),
      detalhes,
      erros,
      h('div', { class: 'acoes-linha' },
        existente ? h('button', { type: 'button', class: 'botao perigo-texto', onclick: remover }, icone('lixeira', 'icone pequeno'), 'Excluir') : null,
        h('button', { type: 'submit', class: 'botao primario' }, 'Salvar'),
      ),
    );
  });
}

function oferecerRevisoes(prova) {
  const hoje = isoData(new Date());
  const sugestoes = sugerirRevisoes(prova, hoje);
  if (!sugestoes.length) return;
  const escolhidas = new Set(sugestoes.map((s) => s.data));

  abrirFolha('Criar blocos de revisão?', (fechar) =>
    h('div', {},
      h('p', {}, 'Estudar em vários dias antes da prova costuma render mais do que estudar tudo na véspera. Quer reservar revisões?'),
      h('div', { class: 'lista-opcoes' },
        sugestoes.map((s) =>
          h('label', { class: 'opcao-linha' },
            h('input', {
              type: 'checkbox', checked: true,
              onchange: (e) => { if (e.target.checked) escolhidas.add(s.data); else escolhidas.delete(s.data); },
            }),
            h('span', {}, `${s.titulo} — ${formatarLonga(s.data)}`),
          ),
        ),
      ),
      h('div', { class: 'acoes-linha' },
        h('button', { class: 'botao secundario', type: 'button', onclick: fechar }, 'Agora não'),
        h('button', {
          class: 'botao primario', type: 'button',
          onclick: () => {
            const criar = sugestoes.filter((s) => escolhidas.has(s.data));
            criar.forEach((s) => store.adicionarCompromisso(s));
            fechar();
            if (criar.length) aviso(`${criar.length} ${criar.length === 1 ? 'bloco criado' : 'blocos criados'}.`);
          },
        }, 'Criar revisões'),
      ),
    ),
  );
}
