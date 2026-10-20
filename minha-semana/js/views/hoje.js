// Tela inicial: compromissos do dia, ações das metas e provas próximas.
import { h, icone } from '../ui.js';
import {
  compromissosDoDia, acoesDoDia, provasProximas, textoContagem, formatarLonga, saudacao,
  metasDaSemana, inicioDaSemana, CATEGORIAS, formatarCurta, maiuscula,
} from '../logic.js';
import { itemCompromisso, cartaoAcao } from './comum.js';

export function renderHoje(raiz, { dados, hoje, agora }) {
  const nome = dados.perfil.nome ? `, ${dados.perfil.nome}` : '';
  const itens = compromissosDoDia(dados.compromissos, hoje);
  const acoes = acoesDoDia(dados.metas, dados.registros, hoje);
  const provas = provasProximas(dados.compromissos, hoje, 7);
  const temMetas = metasDaSemana(dados.metas, inicioDaSemana(hoje)).length > 0;
  const feitos = itens.filter((i) => i.feito).length;

  raiz.append(
    h('header', { class: 'cabecalho' },
      h('div', {},
        h('p', { class: 'data-hoje' }, maiuscula(formatarLonga(hoje))),
        h('h1', {}, `${saudacao(agora.getHours())}${nome}`),
      ),
      h('a', { class: 'botao-icone', href: '#/ajustes', 'aria-label': 'Ajustes e privacidade' }, icone('ajustes')),
    ),

    provas.length
      ? h('section', { class: 'secao', 'aria-labelledby': 's-provas' },
        h('h2', { id: 's-provas' }, 'Provas nos próximos 7 dias'),
        h('ul', { class: 'lista-provas' },
          provas.map((p) =>
            h('li', { class: 'prova-cartao' },
              h('span', { class: `contagem ${p.faltam <= 1 ? 'urgente' : ''}` }, textoContagem(p.faltam)),
              h('span', { class: 'prova-info' },
                h('strong', {}, p.titulo),
                h('span', {}, [p.materia, formatarCurta(p.ocorrencia), p.horario].filter(Boolean).join(' · ')),
              ),
            ),
          ),
        ),
      )
      : null,

    h('section', { class: 'secao', 'aria-labelledby': 's-acoes' },
      h('h2', { id: 's-acoes' }, 'Ações das minhas metas hoje'),
      acoes.length
        ? h('ul', { class: 'lista-cartoes' }, acoes.map((a) => cartaoAcao(a, hoje, { horaAtual: agora.getHours(), registros: dados.registros })))
        : h('div', { class: 'vazio' },
          temMetas
            ? h('p', {}, 'Nenhuma ação de meta planejada para hoje. Aproveite para cuidar dos outros compromissos.')
            : [
              h('p', {}, 'Você ainda não definiu metas para esta semana.'),
              h('a', { class: 'botao secundario', href: '#/metas/nova' }, 'Definir metas da semana'),
            ],
        ),
    ),

    h('section', { class: 'secao', 'aria-labelledby': 's-comp' },
      h('div', { class: 'secao-topo' },
        h('h2', { id: 's-comp' }, 'Compromissos de hoje'),
        itens.length ? h('span', { class: 'contador' }, `${feitos} de ${itens.length}`) : null,
      ),
      itens.length
        ? h('ul', { class: 'lista-itens' }, itens.map((c) => itemCompromisso(c, hoje)))
        : h('div', { class: 'vazio' }, h('p', {}, 'Nada marcado para hoje. Toque em + para adicionar.')),
    ),

    h('p', { class: 'legenda-cores', 'aria-hidden': 'true' },
      Object.entries(CATEGORIAS).map(([k, c]) => h('span', { class: `cat-${c.cor}` }, icone(k, 'icone mini'), c.nome)),
    ),
  );
}
