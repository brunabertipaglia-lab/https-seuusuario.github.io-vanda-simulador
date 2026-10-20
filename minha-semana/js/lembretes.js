// Lembretes locais. Sem servidor de push: a verificação roda enquanto o app está aberto ou em segundo plano.
import {
  lembretesDevidos, isoData, compromissosDoDia, acoesDoDia,
} from './logic.js';
import * as store from './store.js';
import { aviso } from './ui.js';

export const suportaNotificacoes = () => 'Notification' in globalThis;
export const permissaoAtual = () => (suportaNotificacoes() ? Notification.permission : 'unsupported');

export async function pedirPermissao() {
  if (!suportaNotificacoes()) return 'unsupported';
  try {
    return await Notification.requestPermission();
  } catch {
    return 'denied';
  }
}

function conteudo(tipo, dados, hoje) {
  if (tipo === 'manha') {
    const n = compromissosDoDia(dados.compromissos, hoje).length;
    const a = acoesDoDia(dados.metas, dados.registros, hoje).length;
    return { titulo: 'Sua semana: resumo de hoje', corpo: `${n} ${n === 1 ? 'compromisso' : 'compromissos'} e ${a} ${a === 1 ? 'ação de meta' : 'ações de metas'} para hoje.` };
  }
  if (tipo === 'tarde') {
    const pend = compromissosDoDia(dados.compromissos, hoje).filter((c) => c.categoria === 'licao' && !c.feito).length;
    if (!pend) return null;
    return { titulo: 'Lição de casa', corpo: `${pend} ${pend === 1 ? 'lição pendente' : 'lições pendentes'} para hoje.` };
  }
  return { titulo: 'Revisão da semana', corpo: 'Que tal olhar como foi a semana e planejar a próxima?' };
}

async function mostrar({ titulo, corpo }) {
  if (permissaoAtual() === 'granted') {
    try {
      const reg = await navigator.serviceWorker?.getRegistration();
      if (reg) { await reg.showNotification(titulo, { body: corpo, icon: 'icons/icone-192.png', tag: titulo }); return; }
      new Notification(titulo, { body: corpo, icon: 'icons/icone-192.png' });
      return;
    } catch {
      // Cai para o aviso interno.
    }
  }
  aviso(`${titulo}: ${corpo}`);
}

export function verificarLembretes(agora = new Date()) {
  const dados = store.dados();
  const hoje = isoData(agora);
  const enviados = dados.notificacoesEnviadas[hoje] || [];
  for (const tipo of lembretesDevidos(dados.ajustes, enviados, agora)) {
    // Registra mesmo quando não há conteúdo (ex.: sem lição pendente) para não repetir a checagem no dia.
    store.registrarNotificacaoEnviada(hoje, tipo);
    const c = conteudo(tipo, dados, hoje);
    if (c) mostrar(c);
  }
}

export function iniciarLembretes() {
  verificarLembretes();
  setInterval(verificarLembretes, 60 * 1000);
  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState === 'visible') verificarLembretes();
  });
}
