// Armazenamento local (localStorage). Nenhum dado sai do dispositivo.
import { podeCriarMeta, calcularAlvo, progressoDaMeta, LIMITE_METAS, isoData } from './logic.js';

const CHAVE = 'minha-semana:v1';

function estadoInicial() {
  return {
    versao: 1,
    perfil: { nome: '', boasVindasVistas: false },
    compromissos: [],
    metas: [],
    registros: [],
    parceiros: [],
    revisoes: [],
    ajustes: {
      notificacoes: {
        silenciado: false,
        manha: { ativo: false, hora: '07:00' },
        tarde: { ativo: false, hora: '15:00' },
        domingo: { ativo: false, hora: '19:00' },
      },
    },
    notificacoesEnviadas: {},
  };
}

let estado = carregar();
const ouvintes = new Set();

function carregar() {
  try {
    const bruto = localStorage.getItem(CHAVE);
    if (!bruto) return estadoInicial();
    return mesclar(estadoInicial(), JSON.parse(bruto));
  } catch {
    return estadoInicial();
  }
}

function mesclar(base, salvo) {
  const r = { ...base, ...salvo };
  r.perfil = { ...base.perfil, ...(salvo.perfil || {}) };
  r.ajustes = { ...base.ajustes, ...(salvo.ajustes || {}) };
  r.ajustes.notificacoes = { ...base.ajustes.notificacoes, ...((salvo.ajustes || {}).notificacoes || {}) };
  return r;
}

function persistir() {
  try {
    localStorage.setItem(CHAVE, JSON.stringify(estado));
  } catch {
    // Armazenamento indisponível (modo privado, cota cheia): o app segue funcionando em memória.
  }
  ouvintes.forEach((fn) => fn(estado));
}

export function novoId() {
  if (globalThis.crypto && crypto.randomUUID) return crypto.randomUUID();
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}

export const dados = () => estado;
export const aoMudar = (fn) => ouvintes.add(fn);

// ---------- Perfil e ajustes ----------

export function salvarPerfil(parcial) {
  estado.perfil = { ...estado.perfil, ...parcial };
  persistir();
}

export function salvarNotificacoes(parcial) {
  estado.ajustes.notificacoes = { ...estado.ajustes.notificacoes, ...parcial };
  persistir();
}

export function registrarNotificacaoEnviada(dataIso, tipo) {
  const lista = estado.notificacoesEnviadas[dataIso] || [];
  // Guarda só os últimos dias para não crescer indefinidamente.
  const mantidos = Object.keys(estado.notificacoesEnviadas).sort().slice(-6);
  const novo = {};
  for (const k of mantidos) novo[k] = estado.notificacoesEnviadas[k];
  novo[dataIso] = [...lista, tipo];
  estado.notificacoesEnviadas = novo;
  persistir();
}

// ---------- Compromissos ----------

export function adicionarCompromisso(c) {
  const novo = { id: novoId(), concluido: false, concluidas: [], ...c };
  estado.compromissos.push(novo);
  persistir();
  return novo;
}

export function atualizarCompromisso(id, alteracoes) {
  estado.compromissos = estado.compromissos.map((c) => (c.id === id ? { ...c, ...alteracoes } : c));
  persistir();
}

export function removerCompromisso(id) {
  estado.compromissos = estado.compromissos.filter((c) => c.id !== id);
  persistir();
}

// ---------- Metas e progresso ----------

export class LimiteDeMetasError extends Error {}

export function adicionarMeta(m) {
  if (!podeCriarMeta(estado.metas, m.semana_inicio)) {
    throw new LimiteDeMetasError(`Já existem ${LIMITE_METAS} metas nesta semana.`);
  }
  const meta = {
    id: novoId(),
    status: 'ativa',
    progresso_atual: 0,
    criada_em: isoData(new Date()),
    ...m,
    valor_por_vez: Number(m.valor_por_vez),
    valor_alvo: calcularAlvo(m.valor_por_vez, m.frequencia),
    plano_se_entao: `Se ${m.plano_se.trim()}, então eu ${m.plano_entao.trim()}.`,
  };
  estado.metas.push(meta);
  persistir();
  return meta;
}

export function removerMeta(id) {
  estado.metas = estado.metas.filter((m) => m.id !== id);
  estado.registros = estado.registros.filter((r) => r.meta_id !== id);
  persistir();
}

function sincronizarProgresso(metaId) {
  estado.metas = estado.metas.map((m) =>
    m.id === metaId ? { ...m, progresso_atual: progressoDaMeta(m, estado.registros) } : m,
  );
}

export function registrarProgresso(metaId, dataIso, valor) {
  const v = Number(valor);
  if (!Number.isFinite(v) || v <= 0) return;
  estado.registros.push({ id: novoId(), meta_id: metaId, data: dataIso, valor: v });
  sincronizarProgresso(metaId);
  persistir();
}

export function desfazerProgressoDoDia(metaId, dataIso) {
  estado.registros = estado.registros.filter((r) => !(r.meta_id === metaId && r.data === dataIso));
  sincronizarProgresso(metaId);
  persistir();
}

// ---------- Parceiros ----------

export function adicionarParceiro(p) {
  const novo = { id: novoId(), nivel_compartilhamento: 'meta', ...p };
  estado.parceiros.push(novo);
  persistir();
  return novo;
}

export function atualizarParceiro(id, alteracoes) {
  estado.parceiros = estado.parceiros.map((p) => (p.id === id ? { ...p, ...alteracoes } : p));
  persistir();
}

// O histórico guarda o nome do parceiro na própria meta, então remover não apaga o registro.
export function removerParceiro(id) {
  estado.parceiros = estado.parceiros.filter((p) => p.id !== id);
  estado.metas = estado.metas.map((m) => (m.parceiro_id === id ? { ...m, parceiro_id: null } : m));
  persistir();
}

// ---------- Revisões ----------

export function salvarRevisao(semanaInicio, respostas) {
  const existente = estado.revisoes.find((r) => r.semana_inicio === semanaInicio);
  if (existente) {
    estado.revisoes = estado.revisoes.map((r) => (r.semana_inicio === semanaInicio ? { ...r, ...respostas } : r));
  } else {
    estado.revisoes.push({ id: novoId(), semana_inicio: semanaInicio, ...respostas });
  }
  persistir();
}

// ---------- Exportação e importação ----------

export function exportarJson() {
  return JSON.stringify({ ...estado, exportado_em: new Date().toISOString() }, null, 2);
}

export function importarJson(texto) {
  const obj = JSON.parse(texto);
  if (!obj || typeof obj !== 'object' || !Array.isArray(obj.metas) || !Array.isArray(obj.compromissos)) {
    throw new Error('Arquivo não reconhecido.');
  }
  const { exportado_em, ...resto } = obj;
  estado = mesclar(estadoInicial(), resto);
  persistir();
}

export function apagarTudo() {
  estado = estadoInicial();
  persistir();
}
