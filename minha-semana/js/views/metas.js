// Metas da semana: lista com progresso e fluxo guiado de criação (uma pergunta por tela).
import { h, icone, campo, fichas, seletorDias, barraProgresso, aviso, confirmar } from '../ui.js';
import {
  LIMITE_METAS, RELACOES, metasDaSemana, podeCriarMeta, progressoDaMeta, statusDaMeta, NOMES_STATUS,
  validarMeta, calcularAlvo, descreverMedida, inicioDaSemana, somarDias, formatarCurta,
  semanaParaPlanejar, reaproveitarMeta,
} from '../logic.js';
import * as store from '../store.js';
import { abrirRegistro } from './comum.js';
import { abrirMensagem } from './parceiros.js';

const estadoLista = { semana: null };

export function renderMetas(raiz, { dados, hoje, redesenhar }) {
  const atual = inicioDaSemana(hoje);
  if (!estadoLista.semana) estadoLista.semana = atual;
  const semana = estadoLista.semana;
  const metas = metasDaSemana(dados.metas, semana);
  const passada = semana < atual;
  const mover = (n) => { estadoLista.semana = somarDias(semana, 7 * n); redesenhar(); };

  raiz.append(
    h('header', { class: 'cabecalho' },
      h('h1', {}, 'Metas da semana'),
      h('a', { class: 'botao secundario pequeno', href: '#/parceiros' }, icone('pessoas', 'icone pequeno'), 'Parceiros'),
    ),
    h('div', { class: 'navegador' },
      h('button', { class: 'botao-icone', type: 'button', 'aria-label': 'Semana anterior', onclick: () => mover(-1) }, icone('esquerda')),
      h('div', { class: 'navegador-centro' },
        h('strong', { 'aria-live': 'polite' }, `Semana de ${formatarCurta(semana)} a ${formatarCurta(somarDias(semana, 6))}`),
        h('span', { class: 'apoio' }, `${metas.length} de ${LIMITE_METAS} metas`),
      ),
      h('button', { class: 'botao-icone', type: 'button', 'aria-label': 'Próxima semana', onclick: () => mover(1) }, icone('direita')),
    ),
    metas.length
      ? h('ul', { class: 'lista-cartoes' }, metas.map((m) => cartaoMeta(m, dados, hoje, passada)))
      : h('div', { class: 'vazio' },
        h('p', {}, passada ? 'Nenhuma meta registrada nesta semana.' : 'Nenhuma meta para esta semana ainda.'),
        h('p', { class: 'apoio' }, 'Poucas metas, bem definidas, têm mais chance de sair do papel. Escolha até três.'),
      ),
    passada ? null : botaoNovaMeta(dados, semana),
  );
}

function botaoNovaMeta(dados, semana) {
  if (!podeCriarMeta(dados.metas, semana)) {
    return h('div', { class: 'aviso-limite', role: 'note' },
      h('strong', {}, 'Você já tem 3 metas nesta semana.'),
      h('p', {}, 'Esse limite é proposital: concentrar energia em poucas metas aumenta a chance de cumpri-las. Se quiser trocar uma meta, exclua-a primeiro.'),
    );
  }
  return h('a', { class: 'botao primario largo', href: `#/metas/nova?semana=${semana}` }, icone('mais', 'icone pequeno'), 'Nova meta');
}

function cartaoMeta(m, dados, hoje, passada) {
  const atual = progressoDaMeta(m, dados.registros);
  const status = statusDaMeta(m, dados.registros);
  const remover = async () => {
    const ok = await confirmar('Excluir meta', `Excluir a meta "${m.descricao}" e os registros de progresso dela?`, 'Excluir');
    if (ok) { store.removerMeta(m.id); aviso('Meta excluída.'); }
  };
  const diaRegistro = inicioDaSemana(hoje) === m.semana_inicio ? hoje : somarDias(m.semana_inicio, 6);
  return h('li', { class: 'cartao meta' },
    h('div', { class: 'meta-topo' },
      h('h2', { class: 'meta-titulo' }, m.descricao),
      passada || status === 'cumprida' ? h('span', { class: `status st-${status}` }, NOMES_STATUS[status]) : null,
    ),
    h('p', { class: 'apoio' }, descreverMedida(m)),
    barraProgresso(atual, m.valor_alvo, `${atual}/${m.valor_alvo} ${m.unidade}`),
    h('dl', { class: 'meta-detalhes' },
      h('dt', {}, 'Por que importa'), h('dd', {}, m.motivacao),
      h('dt', {}, 'O que pode atrapalhar'), h('dd', {}, m.obstaculo),
      h('dt', {}, 'Meu plano'), h('dd', { class: 'plano' }, m.plano_se_entao),
      h('dt', {}, 'Compromisso com'),
      h('dd', {}, m.parceiro_nome
        ? `${m.parceiro_nome} (${m.compartilhamento === 'progresso' ? 'vê a meta e o progresso' : 'vê apenas a meta'})`
        : 'Só comigo mesma'),
    ),
    h('div', { class: 'acoes-linha esquerda' },
      h('button', { class: 'botao secundario pequeno', type: 'button', onclick: () => abrirRegistro(m, diaRegistro, m.frequencia.tipo === 'dias' ? m.valor_por_vez : 1) }, 'Registrar avanço'),
      m.parceiro_nome ? h('button', { class: 'botao secundario pequeno', type: 'button', onclick: () => abrirMensagem(m) }, icone('copiar', 'icone pequeno'), 'Mensagem') : null,
      h('button', { class: 'botao texto pequeno perigo-texto', type: 'button', onclick: remover }, 'Excluir'),
    ),
  );
}

// ---------- Fluxo guiado ----------

let rascunho = null;

function novoRascunho(semana) {
  return {
    passo: 0,
    semana_inicio: semana,
    descricao: '',
    valor_por_vez: '',
    unidade: '',
    frequencia: { tipo: 'dias', dias: [1, 2, 3, 4] },
    motivacao: '',
    obstaculo: '',
    plano_se: '',
    plano_entao: '',
    parceiro_id: null,
    compartilhamento: 'meta',
    reaproveitarVisto: false,
  };
}

const PASSOS = ['descricao', 'medida', 'motivacao', 'obstaculo', 'plano', 'parceiro', 'confirmar'];

export function renderNovaMeta(raiz, { dados, hoje, params, redesenhar }) {
  const semanaPedida = params.get('semana') || semanaParaPlanejar(hoje);
  if (!rascunho || rascunho.semanaPedida !== semanaPedida) {
    rascunho = { ...novoRascunho(semanaPedida), semanaPedida };
  }
  const r = rascunho;

  if (!podeCriarMeta(dados.metas, r.semana_inicio)) {
    raiz.append(
      cabecalhoFluxo('Nova meta'),
      h('div', { class: 'aviso-limite', role: 'note' },
        h('strong', {}, `A semana de ${formatarCurta(r.semana_inicio)} já tem 3 metas.`),
        h('p', {}, 'Menos metas aumentam a chance de cumprimento. Se uma delas deixou de fazer sentido, exclua-a na tela de metas e crie a nova.'),
        h('a', { class: 'botao secundario', href: '#/metas' }, 'Ver minhas metas'),
      ),
    );
    return;
  }

  const anteriores = metasAnteriores(dados, r.semana_inicio);
  if (!r.reaproveitarVisto && anteriores.length) {
    raiz.append(
      cabecalhoFluxo('Nova meta'),
      h('section', { class: 'passo' },
        h('h2', { class: 'pergunta' }, 'Quer reaproveitar uma meta anterior?'),
        h('p', { class: 'apoio' }, 'Repetir uma meta que ainda está em formação é uma boa estratégia: hábitos levam semanas para se firmar.'),
        h('ul', { class: 'lista-opcoes' },
          anteriores.map((m) =>
            h('li', {},
              h('button', {
                class: 'opcao-botao', type: 'button',
                onclick: () => {
                  rascunho = { ...r, ...reaproveitarMeta(m), semana_inicio: r.semana_inicio, semanaPedida: r.semanaPedida, passo: 0, reaproveitarVisto: true };
                  if (rascunho.parceiro_id && !dados.parceiros.some((p) => p.id === rascunho.parceiro_id)) rascunho.parceiro_id = null;
                  redesenhar();
                },
              },
                h('strong', {}, m.descricao),
                h('span', {}, `${descreverMedida(m)} · semana de ${formatarCurta(m.semana_inicio)}`),
              ),
            ),
          ),
        ),
        h('button', { class: 'botao secundario largo', type: 'button', onclick: () => { r.reaproveitarVisto = true; redesenhar(); } }, 'Começar do zero'),
      ),
    );
    return;
  }

  const passo = PASSOS[r.passo];
  const erros = h('div', { class: 'erros', role: 'alert' });
  const conteudo = montarPasso(passo, r, dados, hoje, redesenhar);

  const avancar = (e) => {
    e.preventDefault();
    const problemas = validarPasso(passo, r);
    erros.replaceChildren(...problemas.map((t) => h('p', {}, t)));
    if (problemas.length) return;
    if (passo === 'confirmar') return salvar(r, dados, hoje);
    r.passo += 1;
    redesenhar();
  };

  raiz.append(
    cabecalhoFluxo(`Nova meta · semana de ${formatarCurta(r.semana_inicio)}`),
    h('div', { class: 'etapas', 'aria-label': `Etapa ${r.passo + 1} de ${PASSOS.length}` },
      PASSOS.map((_, i) => h('span', { class: i <= r.passo ? 'feita' : '' }))),
    h('form', { class: 'passo', onsubmit: avancar, oninput: () => erros.replaceChildren(), novalidate: true },
      conteudo,
      erros,
      h('div', { class: 'acoes-linha' },
        r.passo > 0 ? h('button', { class: 'botao secundario', type: 'button', onclick: () => { r.passo -= 1; redesenhar(); } }, 'Voltar') : null,
        h('button', { class: 'botao primario', type: 'submit' }, passo === 'confirmar' ? 'Salvar meta' : 'Continuar'),
      ),
    ),
  );
  const foco = raiz.querySelector('[autofocus]');
  if (foco) foco.focus();
}

function cabecalhoFluxo(titulo) {
  return h('header', { class: 'cabecalho' },
    h('h1', { class: 'menor' }, titulo),
    h('a', { class: 'botao-icone', href: '#/metas', 'aria-label': 'Cancelar e voltar às metas', onclick: () => { rascunho = null; } }, icone('fechar')),
  );
}

function metasAnteriores(dados, semana) {
  const limite = somarDias(semana, -28);
  const vistas = new Set();
  return dados.metas
    .filter((m) => m.semana_inicio < semana && m.semana_inicio >= limite)
    .sort((a, b) => (a.semana_inicio < b.semana_inicio ? 1 : -1))
    .filter((m) => {
      const chave = m.descricao.trim().toLowerCase();
      if (vistas.has(chave)) return false;
      vistas.add(chave);
      return true;
    });
}

function texto(r, chave, { multilinha = false, placeholder = '', max = 200 } = {}) {
  const el = h(multilinha ? 'textarea' : 'input', {
    type: multilinha ? null : 'text', rows: multilinha ? '3' : null, maxlength: String(max), placeholder, autofocus: true,
    oninput: (e) => { r[chave] = e.target.value; },
  });
  el.value = r[chave];
  return el;
}

function montarPasso(passo, r, dados, hoje, redesenhar) {
  switch (passo) {
    case 'descricao': {
      const atual = inicioDaSemana(hoje);
      const proxima = somarDias(atual, 7);
      return [
        h('h2', { class: 'pergunta' }, 'O que você quer alcançar esta semana?'),
        campo('Minha meta', texto(r, 'descricao', { placeholder: 'Ex.: Praticar matemática para a olimpíada', max: 120 })),
        h('fieldset', { class: 'campo' },
          h('legend', {}, 'Para qual semana?'),
          fichas('semana', [
            { valor: atual, rotulo: `Esta (${formatarCurta(atual)})` },
            { valor: proxima, rotulo: `Próxima (${formatarCurta(proxima)})` },
          ], r.semana_inicio, (v) => { r.semana_inicio = v; }, 'Semana'),
        ),
      ];
    }
    case 'medida': {
      const alvo = h('p', { class: 'alvo', 'aria-live': 'polite' });
      const atualizarAlvo = () => {
        const n = calcularAlvo(r.valor_por_vez, r.frequencia);
        alvo.textContent = n > 0 && r.unidade.trim() ? `Alvo da semana: ${n} ${r.unidade.trim()}` : 'Preencha o número e a unidade para ver o alvo da semana.';
      };
      const valor = h('input', {
        type: 'number', inputmode: 'decimal', min: '0', step: 'any', value: String(r.valor_por_vez), autofocus: true,
        oninput: (e) => { r.valor_por_vez = e.target.value; atualizarAlvo(); },
      });
      const unidade = h('input', {
        type: 'text', value: r.unidade, maxlength: '30', placeholder: 'exercícios, páginas, minutos…',
        oninput: (e) => { r.unidade = e.target.value; atualizarAlvo(); },
      });
      const areaDias = h('div', {});
      const desenharDias = () => {
        areaDias.replaceChildren();
        if (r.frequencia.tipo === 'dias') {
          areaDias.append(seletorDias(r.frequencia.dias, (d) => { r.frequencia.dias = d; atualizarAlvo(); }, 'Em quais dias'));
        }
      };
      desenharDias();
      atualizarAlvo();
      return [
        h('h2', { class: 'pergunta' }, 'Como você vai medir?'),
        h('p', { class: 'apoio' }, 'Uma meta com número fica mais clara: dá para saber exatamente quando ela foi cumprida.'),
        h('div', { class: 'grade-2' }, campo('Quanto', valor), campo('De quê', unidade)),
        h('fieldset', { class: 'campo' },
          h('legend', {}, 'Com que frequência?'),
          fichas('freq', [
            { valor: 'dias', rotulo: 'Por dia, em dias escolhidos' },
            { valor: 'semana', rotulo: 'No total da semana' },
          ], r.frequencia.tipo, (v) => {
            r.frequencia = v === 'dias' ? { tipo: 'dias', dias: r.frequencia.dias?.length ? r.frequencia.dias : [1, 2, 3, 4] } : { tipo: 'semana', dias: [] };
            desenharDias();
            atualizarAlvo();
          }, 'Frequência'),
          areaDias,
        ),
        alvo,
      ];
    }
    case 'motivacao':
      return [
        h('h2', { class: 'pergunta' }, 'Por que isso importa para você?'),
        h('p', { class: 'apoio' }, 'Pense no que vai ser melhor quando você conseguir. Escreva com as suas palavras.'),
        campo('Minha razão', texto(r, 'motivacao', { multilinha: true, max: 300 })),
      ];
    case 'obstaculo':
      return [
        h('h2', { class: 'pergunta' }, 'O que pode atrapalhar?'),
        h('p', { class: 'apoio' }, 'Pense em algo seu, que costuma acontecer de verdade: cansaço, celular, deixar para depois…'),
        campo('O obstáculo mais provável', texto(r, 'obstaculo', { multilinha: true, max: 300 })),
      ];
    case 'plano':
      if (!r.plano_se && r.obstaculo) r.plano_se = r.obstaculo.trim().replace(/[.!]+$/, '').toLowerCase();
      return [
        h('h2', { class: 'pergunta' }, 'Qual é o seu plano Se–Então?'),
        h('p', { class: 'apoio' }, 'Decidir antes o que fazer quando o obstáculo aparecer torna a ação quase automática.'),
        campo('Se…', texto(r, 'plano_se', { placeholder: 'chegar cansada do treino', max: 150 })),
        campo('então eu…', (() => {
          const el = h('input', {
            type: 'text', maxlength: '150', placeholder: 'faço só os 5 exercícios antes do banho',
            oninput: (e) => { r.plano_entao = e.target.value; },
          });
          el.value = r.plano_entao;
          return el;
        })()),
      ];
    case 'parceiro':
      return passoParceiro(r, dados, redesenhar);
    case 'confirmar': {
      const parceiro = dados.parceiros.find((p) => p.id === r.parceiro_id);
      const prevista = { ...r, valor_por_vez: Number(r.valor_por_vez), unidade: r.unidade.trim() };
      return [
        h('h2', { class: 'pergunta' }, 'Confira sua meta'),
        h('dl', { class: 'meta-detalhes resumo' },
          h('dt', {}, 'Meta'), h('dd', {}, r.descricao.trim()),
          h('dt', {}, 'Medida'), h('dd', {}, `${descreverMedida(prevista)} → alvo de ${calcularAlvo(r.valor_por_vez, r.frequencia)} ${prevista.unidade}`),
          h('dt', {}, 'Por que importa'), h('dd', {}, r.motivacao.trim()),
          h('dt', {}, 'O que pode atrapalhar'), h('dd', {}, r.obstaculo.trim()),
          h('dt', {}, 'Meu plano'), h('dd', { class: 'plano' }, `Se ${r.plano_se.trim()}, então eu ${r.plano_entao.trim()}.`),
          h('dt', {}, 'Compromisso com'),
          h('dd', {}, parceiro ? `${parceiro.nome} (${r.compartilhamento === 'progresso' ? 'meta e progresso' : 'apenas a meta'})` : 'Só comigo mesma'),
        ),
      ];
    }
    default:
      return [];
  }
}

function passoParceiro(r, dados, redesenhar) {
  const opcoes = [
    { valor: '', rotulo: 'Só comigo mesma' },
    ...dados.parceiros.map((p) => ({ valor: p.id, rotulo: `${p.nome} · ${p.relacao}` })),
  ];
  const nome = h('input', { type: 'text', maxlength: '40', placeholder: 'Nome' });
  const relacao = h('select', {}, RELACOES.map((rel) => h('option', { value: rel }, rel)));
  const adicionar = () => {
    if (!nome.value.trim()) { aviso('Escreva o nome da pessoa.'); return; }
    const p = store.adicionarParceiro({ nome: nome.value.trim(), relacao: relacao.value });
    r.parceiro_id = p.id;
    r.compartilhamento = p.nivel_compartilhamento;
    redesenhar();
  };
  return [
    h('h2', { class: 'pergunta' }, 'Com quem você se compromete?'),
    h('p', { class: 'apoio' }, 'Contar a alguém de confiança ajuda a manter o compromisso. Você decide quem e o que essa pessoa vai saber.'),
    h('fieldset', { class: 'campo' },
      h('legend', {}, 'Parceiro de compromisso'),
      fichas('parceiro', opcoes, r.parceiro_id || '', (v) => {
        r.parceiro_id = v || null;
        const p = dados.parceiros.find((x) => x.id === v);
        if (p) r.compartilhamento = p.nivel_compartilhamento || 'meta';
        redesenhar();
      }, 'Parceiro'),
    ),
    r.parceiro_id
      ? h('fieldset', { class: 'campo' },
        h('legend', {}, 'O que essa pessoa vai ver'),
        fichas('compart', [
          { valor: 'meta', rotulo: 'Apenas a meta' },
          { valor: 'progresso', rotulo: 'Meta e progresso' },
        ], r.compartilhamento, (v) => { r.compartilhamento = v; }, 'Compartilhamento'),
        h('p', { class: 'ajuda' }, 'Nada é enviado automaticamente. Você copia a mensagem e envia se quiser.'),
      )
      : null,
    h('details', { class: 'detalhes' },
      h('summary', {}, 'Adicionar outra pessoa'),
      h('div', { class: 'grade-2' }, campo('Nome', nome), campo('Relação', relacao)),
      h('button', { class: 'botao secundario pequeno', type: 'button', onclick: adicionar }, 'Adicionar'),
    ),
  ];
}

function validarPasso(passo, r) {
  const v = validarMeta({ ...r, plano_se: r.plano_se || 'x', plano_entao: r.plano_entao || 'x' });
  switch (passo) {
    case 'descricao':
      return r.descricao.trim() ? [] : ['Descreva o que você quer alcançar.'];
    case 'medida':
      return v.filter((e) => !e.startsWith('Descreva'));
    case 'motivacao':
      return r.motivacao.trim() ? [] : ['Escreva, mesmo que em poucas palavras, por que isso importa.'];
    case 'obstaculo':
      return r.obstaculo.trim() ? [] : ['Pensar no obstáculo antes ajuda a superá-lo. Escreva pelo menos um.'];
    case 'plano':
      return r.plano_se.trim() && r.plano_entao.trim() ? [] : ['Complete as duas partes: "Se ___" e "então eu ___".'];
    case 'confirmar':
      return validarMeta(r);
    default:
      return [];
  }
}

function salvar(r, dados, hoje) {
  const parceiro = dados.parceiros.find((p) => p.id === r.parceiro_id);
  let meta;
  try {
    meta = store.adicionarMeta({
      semana_inicio: r.semana_inicio,
      descricao: r.descricao.trim(),
      valor_por_vez: r.valor_por_vez,
      unidade: r.unidade.trim(),
      frequencia: r.frequencia.tipo === 'dias' ? { tipo: 'dias', dias: [...r.frequencia.dias] } : { tipo: 'semana', dias: [] },
      motivacao: r.motivacao.trim(),
      obstaculo: r.obstaculo.trim(),
      plano_se: r.plano_se.trim(),
      plano_entao: r.plano_entao.trim(),
      parceiro_id: parceiro ? parceiro.id : null,
      parceiro_nome: parceiro ? parceiro.nome : '',
      compartilhamento: parceiro ? r.compartilhamento : 'meta',
      criada_em: hoje,
    });
  } catch (e) {
    aviso(e.message);
    return;
  }
  rascunho = null;
  estadoLista.semana = meta.semana_inicio;
  location.hash = '#/metas';
  aviso('Meta salva. Ela aparecerá na tela Hoje nos dias planejados.');
  if (parceiro) setTimeout(() => abrirMensagem(meta), 150);
}

export function definirSemanaMetas(semana) {
  estadoLista.semana = semana;
}
