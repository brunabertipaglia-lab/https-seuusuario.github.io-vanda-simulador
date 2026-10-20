// Ponto de entrada: rotas por hash, navegação inferior, botão "+" e registro do service worker.
import { isoData } from './logic.js';
import * as store from './store.js';
import { renderHoje } from './views/hoje.js';
import { renderAgenda, dataSelecionadaAgenda } from './views/agenda.js';
import { renderMetas, renderNovaMeta } from './views/metas.js';
import { renderParceiros } from './views/parceiros.js';
import { renderRevisao } from './views/revisao.js';
import { renderAjustes, renderBoasVindas } from './views/ajustes.js';
import { abrirCompromisso } from './views/compromisso.js';
import { iniciarLembretes } from './lembretes.js';

const ROTAS = {
  hoje: { render: renderHoje, aba: 'hoje', titulo: 'Hoje', fab: true },
  agenda: { render: renderAgenda, aba: 'agenda', titulo: 'Agenda', fab: true },
  metas: { render: renderMetas, aba: 'metas', titulo: 'Metas da semana' },
  'metas/nova': { render: renderNovaMeta, aba: 'metas', titulo: 'Nova meta' },
  parceiros: { render: renderParceiros, aba: 'metas', titulo: 'Parceiros' },
  revisao: { render: renderRevisao, aba: 'revisao', titulo: 'Revisão da semana' },
  ajustes: { render: renderAjustes, aba: 'hoje', titulo: 'Ajustes' },
};

// Permite simular outra data em testes e demonstrações: index.html?hoje=2026-10-25
const dataSimulada = new URLSearchParams(location.search).get('hoje');
function agora() {
  const real = new Date();
  if (!dataSimulada || !/^\d{4}-\d{2}-\d{2}$/.test(dataSimulada)) return real;
  const [a, m, d] = dataSimulada.split('-').map(Number);
  return new Date(a, m - 1, d, real.getHours(), real.getMinutes());
}

function rotaAtual() {
  const bruto = location.hash.replace(/^#\/?/, '') || 'hoje';
  const [caminho, consulta = ''] = bruto.split('?');
  return { caminho: ROTAS[caminho] ? caminho : 'hoje', params: new URLSearchParams(consulta) };
}

const principal = document.getElementById('principal');
const fab = document.getElementById('fab');
let ultimaRota = null;

function desenhar() {
  const dados = store.dados();
  const momento = agora();
  const hoje = isoData(momento);
  const { caminho, params } = rotaAtual();
  const rota = ROTAS[caminho];
  const rolagem = ultimaRota === caminho ? window.scrollY : 0;

  principal.replaceChildren();
  if (!dados.perfil.boasVindasVistas) {
    document.body.classList.add('sem-navegacao');
    renderBoasVindas(principal, { redesenhar: desenhar });
    fab.hidden = true;
    return;
  }
  document.body.classList.remove('sem-navegacao');

  rota.render(principal, { dados, hoje, agora: momento, params, redesenhar: desenhar });
  document.title = `${rota.titulo} · Minha Semana`;
  fab.hidden = !rota.fab;

  document.querySelectorAll('.navegacao a').forEach((a) => {
    const ativo = a.dataset.aba === rota.aba;
    a.classList.toggle('ativo', ativo);
    if (ativo) a.setAttribute('aria-current', 'page');
    else a.removeAttribute('aria-current');
  });

  if (ultimaRota !== caminho) {
    window.scrollTo(0, 0);
    principal.focus({ preventScroll: true });
  } else {
    window.scrollTo(0, rolagem);
  }
  ultimaRota = caminho;
}

fab.addEventListener('click', () => {
  const hoje = isoData(agora());
  const { caminho } = rotaAtual();
  abrirCompromisso({ data: caminho === 'agenda' ? dataSelecionadaAgenda(hoje) : hoje });
});

window.addEventListener('hashchange', desenhar);
store.aoMudar(desenhar);

// Redesenha na virada do dia, caso o app fique aberto.
let diaDesenhado = isoData(agora());
setInterval(() => {
  const d = isoData(agora());
  if (d !== diaDesenhado) { diaDesenhado = d; desenhar(); }
}, 60 * 1000);

desenhar();
iniciarLembretes();

if ('serviceWorker' in navigator && location.protocol !== 'file:') {
  navigator.serviceWorker.register('sw.js').catch(() => {});
}
