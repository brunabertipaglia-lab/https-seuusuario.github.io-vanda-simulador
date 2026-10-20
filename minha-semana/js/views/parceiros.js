// Parceiros de compromisso: cadastro, histórico e mensagem para envio manual.
import { h, icone, campo, fichas, abrirFolha, aviso, confirmar, copiarTexto } from '../ui.js';
import { RELACOES, historicoCompromissos, mensagemParaParceiro, metasDaSemana, inicioDaSemana } from '../logic.js';
import * as store from '../store.js';

export function abrirMensagem(meta) {
  const d = store.dados();
  const texto = mensagemParaParceiro(meta, meta.parceiro_nome, d.registros, d.perfil.nome);
  abrirFolha(`Mensagem para ${meta.parceiro_nome}`, (fechar) => {
    const area = h('textarea', { rows: '6', 'aria-label': 'Texto da mensagem' });
    area.value = texto;
    return h('div', {},
      h('p', { class: 'apoio' }, 'O app não envia nada sozinho. Copie o texto, ajuste se quiser e envie pelo aplicativo que preferir.'),
      area,
      h('div', { class: 'acoes-linha' },
        h('button', { class: 'botao secundario', type: 'button', onclick: fechar }, 'Fechar'),
        h('button', {
          class: 'botao primario', type: 'button',
          onclick: async () => {
            const ok = await copiarTexto(area.value);
            if (ok) { aviso('Mensagem copiada.'); fechar(); } else {
              area.select();
              aviso('Selecione o texto e copie manualmente.');
            }
          },
        }, icone('copiar', 'icone pequeno'), 'Copiar texto'),
      ),
    );
  });
}

function abrirParceiro(existente) {
  const p = existente ? { ...existente } : { nome: '', relacao: 'Família', nivel_compartilhamento: 'meta' };
  abrirFolha(existente ? 'Editar parceiro' : 'Novo parceiro', (fechar) => {
    const nome = h('input', { type: 'text', value: p.nome, maxlength: '40', autofocus: true, oninput: (e) => { p.nome = e.target.value; } });
    const relacao = h('select', { onchange: (e) => { p.relacao = e.target.value; } },
      RELACOES.map((r) => h('option', { value: r, selected: r === p.relacao }, r)));
    return h('form', {
      onsubmit: (e) => {
        e.preventDefault();
        if (!p.nome.trim()) { aviso('Escreva o nome.'); return; }
        const dadosP = { nome: p.nome.trim(), relacao: p.relacao, nivel_compartilhamento: p.nivel_compartilhamento };
        if (existente) store.atualizarParceiro(existente.id, dadosP);
        else store.adicionarParceiro(dadosP);
        fechar();
        aviso(existente ? 'Parceiro atualizado.' : 'Parceiro adicionado.');
      },
    },
      campo('Nome', nome),
      campo('Relação', relacao),
      h('fieldset', { class: 'campo' },
        h('legend', {}, 'O que essa pessoa vê, por padrão'),
        fichas('nivel', [
          { valor: 'meta', rotulo: 'Apenas a meta' },
          { valor: 'progresso', rotulo: 'Meta e progresso' },
        ], p.nivel_compartilhamento, (v) => { p.nivel_compartilhamento = v; }, 'Compartilhamento padrão'),
        h('p', { class: 'ajuda' }, 'Você pode mudar isso em cada meta.'),
      ),
      h('div', { class: 'acoes-linha' }, h('button', { class: 'botao primario', type: 'submit' }, 'Salvar')),
    );
  });
}

export function renderParceiros(raiz, { dados, hoje }) {
  const historico = historicoCompromissos(dados.metas);
  const ativas = metasDaSemana(dados.metas, inicioDaSemana(hoje)).filter((m) => m.parceiro_nome);

  const remover = async (p) => {
    const ok = await confirmar('Remover parceiro', `Remover ${p.nome}? O histórico de compromissos continua guardado.`, 'Remover');
    if (ok) { store.removerParceiro(p.id); aviso('Parceiro removido.'); }
  };

  raiz.append(
    h('header', { class: 'cabecalho' },
      h('div', {}, h('a', { class: 'voltar', href: '#/metas' }, icone('esquerda', 'icone pequeno'), 'Metas'), h('h1', {}, 'Meus parceiros de compromisso')),
    ),
    h('p', { class: 'apoio' }, 'São pessoas de confiança com quem você escolhe compartilhar uma meta. Você decide o que cada uma vê.'),

    h('section', { class: 'secao' },
      dados.parceiros.length
        ? h('ul', { class: 'lista-cartoes' },
          dados.parceiros.map((p) =>
            h('li', { class: 'cartao parceiro' },
              h('div', {},
                h('strong', {}, p.nome),
                h('p', { class: 'apoio' }, `${p.relacao} · por padrão vê ${p.nivel_compartilhamento === 'progresso' ? 'meta e progresso' : 'apenas a meta'}`),
              ),
              h('div', { class: 'acoes-linha esquerda' },
                h('button', { class: 'botao secundario pequeno', type: 'button', onclick: () => abrirParceiro(p) }, 'Editar'),
                h('button', { class: 'botao texto pequeno perigo-texto', type: 'button', onclick: () => remover(p) }, 'Remover'),
              ),
            ),
          ),
        )
        : h('div', { class: 'vazio' }, h('p', {}, 'Nenhum parceiro cadastrado ainda.')),
      h('button', { class: 'botao primario largo', type: 'button', onclick: () => abrirParceiro(null) }, icone('mais', 'icone pequeno'), 'Adicionar parceiro'),
    ),

    ativas.length
      ? h('section', { class: 'secao' },
        h('h2', {}, 'Avisar sobre as metas desta semana'),
        h('ul', { class: 'lista-cartoes' },
          ativas.map((m) =>
            h('li', { class: 'cartao' },
              h('p', {}, h('strong', {}, m.parceiro_nome), ` — ${m.descricao}`),
              h('button', { class: 'botao secundario pequeno', type: 'button', onclick: () => abrirMensagem(m) }, icone('copiar', 'icone pequeno'), 'Gerar mensagem'),
            ),
          ),
        ),
      )
      : null,

    h('section', { class: 'secao' },
      h('h2', {}, 'Histórico de compromissos'),
      historico.length
        ? h('ul', { class: 'historico' }, historico.map((x) => h('li', {}, x.texto)))
        : h('p', { class: 'apoio' }, 'Quando você escolher um parceiro para uma meta, o registro aparecerá aqui.'),
    ),
  );
}
