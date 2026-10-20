// Núcleo de regras do "Minha Semana".
// Funções puras, sem acesso a DOM ou armazenamento, para que possam ser testadas no Node.

export const LIMITE_METAS = 3;
export const LIMITE_NOTIFICACOES_DIA = 3;

export const CATEGORIAS = {
  licao: { nome: 'Lição de casa', cor: 'licao' },
  prova: { nome: 'Prova', cor: 'prova' },
  treino: { nome: 'Treino', cor: 'treino' },
  estudo: { nome: 'Estudo', cor: 'estudo' },
  projeto: { nome: 'Projeto/Olimpíada', cor: 'projeto' },
  outro: { nome: 'Outro', cor: 'outro' },
};

export const RELACOES = ['Família', 'Amiga', 'Professora', 'Técnica', 'Mentora', 'Outra'];

export const DIAS_CURTOS = ['Dom', 'Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb'];
export const DIAS_LONGOS = ['domingo', 'segunda-feira', 'terça-feira', 'quarta-feira', 'quinta-feira', 'sexta-feira', 'sábado'];
export const MESES = ['janeiro', 'fevereiro', 'março', 'abril', 'maio', 'junho', 'julho', 'agosto', 'setembro', 'outubro', 'novembro', 'dezembro'];

// ---------- Datas (sempre no fuso local, formato AAAA-MM-DD) ----------

export function isoData(d) {
  const a = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const dia = String(d.getDate()).padStart(2, '0');
  return `${a}-${m}-${dia}`;
}

export function deIso(iso) {
  const [a, m, d] = iso.split('-').map(Number);
  return new Date(a, m - 1, d);
}

export function somarDias(iso, n) {
  const d = deIso(iso);
  d.setDate(d.getDate() + n);
  return isoData(d);
}

export function diaDaSemana(iso) {
  return deIso(iso).getDay();
}

export function diferencaDias(isoA, isoB) {
  // Dias de A até B (positivo se B for depois de A). Usa UTC para ignorar horário de verão.
  const [a1, m1, d1] = isoA.split('-').map(Number);
  const [a2, m2, d2] = isoB.split('-').map(Number);
  return Math.round((Date.UTC(a2, m2 - 1, d2) - Date.UTC(a1, m1 - 1, d1)) / 86400000);
}

// A semana vai de segunda a domingo; semana_inicio é sempre uma segunda-feira.
export function inicioDaSemana(iso) {
  const dow = diaDaSemana(iso);
  const recuo = dow === 0 ? 6 : dow - 1;
  return somarDias(iso, -recuo);
}

export function diasDaSemana(semanaInicio) {
  return Array.from({ length: 7 }, (_, i) => somarDias(semanaInicio, i));
}

export function formatarCurta(iso) {
  const [, m, d] = iso.split('-');
  return `${d}/${m}`;
}

export function formatarLonga(iso) {
  const d = deIso(iso);
  return `${DIAS_LONGOS[d.getDay()]}, ${d.getDate()} de ${MESES[d.getMonth()]}`;
}

export function maiuscula(texto) {
  return texto ? texto[0].toUpperCase() + texto.slice(1) : texto;
}

// Semana sugerida para planejar: no domingo, a próxima; nos demais dias, a atual.
export function semanaParaPlanejar(hojeIso) {
  const inicio = inicioDaSemana(hojeIso);
  return diaDaSemana(hojeIso) === 0 ? somarDias(inicio, 7) : inicio;
}

// Semana sugerida para revisar: no domingo, a atual; de segunda a quarta,
// a anterior, se ela ainda não tiver sido revisada; nos demais dias, a atual.
export function semanaParaRevisar(hojeIso, revisoes = []) {
  const atual = inicioDaSemana(hojeIso);
  const dow = diaDaSemana(hojeIso);
  if (dow >= 1 && dow <= 3) {
    const anterior = somarDias(atual, -7);
    if (!revisoes.some((r) => r.semana_inicio === anterior)) return anterior;
  }
  return atual;
}

export function saudacao(hora) {
  if (hora < 12) return 'Bom dia';
  if (hora < 18) return 'Boa tarde';
  return 'Boa noite';
}

// ---------- Compromissos ----------

export function ocorreEm(compromisso, iso) {
  if (!compromisso.recorrencia) return compromisso.data === iso;
  const { dias, ate } = compromisso.recorrencia;
  if (iso < compromisso.data) return false;
  if (ate && iso > ate) return false;
  return dias.includes(diaDaSemana(iso));
}

export function estaConcluido(compromisso, iso) {
  if (!compromisso.recorrencia) return Boolean(compromisso.concluido);
  return (compromisso.concluidas || []).includes(iso);
}

export function alternarConclusao(compromisso, iso) {
  if (!compromisso.recorrencia) return { ...compromisso, concluido: !compromisso.concluido };
  const concluidas = new Set(compromisso.concluidas || []);
  if (concluidas.has(iso)) concluidas.delete(iso);
  else concluidas.add(iso);
  return { ...compromisso, concluidas: [...concluidas].sort() };
}

// Itens com horário primeiro, em ordem cronológica; depois os sem horário, por título.
export function compromissosDoDia(compromissos, iso) {
  return compromissos
    .filter((c) => ocorreEm(c, iso))
    .map((c) => ({ ...c, ocorrencia: iso, feito: estaConcluido(c, iso) }))
    .sort((a, b) => {
      if (a.horario && !b.horario) return -1;
      if (!a.horario && b.horario) return 1;
      if (a.horario !== b.horario) return a.horario < b.horario ? -1 : 1;
      return a.titulo.localeCompare(b.titulo, 'pt-BR');
    });
}

export function provasProximas(compromissos, hojeIso, janela = 7) {
  const lista = [];
  for (let i = 0; i <= janela; i++) {
    const dia = somarDias(hojeIso, i);
    for (const c of compromissosDoDia(compromissos, dia)) {
      if (c.categoria === 'prova') lista.push({ ...c, faltam: i });
    }
  }
  return lista;
}

export function textoContagem(faltam) {
  if (faltam === 0) return 'Hoje';
  if (faltam === 1) return 'Amanhã';
  return `Em ${faltam} dias`;
}

// Sugere blocos de revisão nos dias anteriores à prova, sem cair em dias passados.
export function sugerirRevisoes(prova, hojeIso, diasAntes = [3, 2, 1]) {
  const assunto = prova.materia ? prova.materia : prova.titulo;
  return diasAntes
    .map((n) => somarDias(prova.data, -n))
    .filter((dia) => dia >= hojeIso)
    .map((dia) => ({
      titulo: `Revisão: ${assunto}`,
      categoria: 'estudo',
      data: dia,
      horario: '',
      materia: prova.materia || '',
      observacao: `Preparação para a prova de ${formatarCurta(prova.data)}`,
      recorrencia: null,
    }));
}

export function validarCompromisso(c) {
  const erros = [];
  if (!c.titulo || !c.titulo.trim()) erros.push('Escreva um título.');
  if (!CATEGORIAS[c.categoria]) erros.push('Escolha uma categoria.');
  if (!/^\d{4}-\d{2}-\d{2}$/.test(c.data || '')) erros.push('Escolha uma data.');
  if (c.horario && !/^\d{2}:\d{2}$/.test(c.horario)) erros.push('Horário inválido.');
  if (c.recorrencia && (!c.recorrencia.dias || c.recorrencia.dias.length === 0)) {
    erros.push('Escolha ao menos um dia para repetir.');
  }
  return erros;
}

// ---------- Metas ----------

// Alvo semanal: valor por vez × número de dias planejados, ou o próprio valor se for total semanal.
export function calcularAlvo(valor, frequencia) {
  const v = Number(valor);
  if (!Number.isFinite(v) || v <= 0) return 0;
  if (frequencia && frequencia.tipo === 'dias') return v * (frequencia.dias || []).length;
  return v;
}

export function descreverMedida(meta) {
  const { valor_por_vez: v, unidade, frequencia } = meta;
  if (frequencia.tipo === 'dias') {
    const n = frequencia.dias.length;
    const nomes = ordenarDias(frequencia.dias).map((d) => DIAS_CURTOS[d]).join(', ');
    return `${v} ${unidade} por dia, ${n} ${n === 1 ? 'dia' : 'dias'} na semana (${nomes})`;
  }
  return `${v} ${unidade} ao longo da semana`;
}

export function ordenarDias(dias) {
  // Segunda primeiro, domingo por último.
  return [...dias].sort((a, b) => ((a + 6) % 7) - ((b + 6) % 7));
}

export function validarMeta(m) {
  const erros = [];
  if (!m.descricao || !m.descricao.trim()) erros.push('Descreva o que você quer alcançar.');
  const v = Number(m.valor_por_vez);
  if (!Number.isFinite(v) || v <= 0) erros.push('A meta precisa de um número maior que zero.');
  if (!m.unidade || !m.unidade.trim()) erros.push('Informe a unidade (por exemplo: exercícios, páginas, minutos).');
  if (!m.frequencia || !['dias', 'semana'].includes(m.frequencia.tipo)) erros.push('Escolha a frequência.');
  else if (m.frequencia.tipo === 'dias' && m.frequencia.dias.length === 0) erros.push('Escolha ao menos um dia.');
  if (!m.plano_se || !m.plano_se.trim() || !m.plano_entao || !m.plano_entao.trim()) {
    erros.push('Complete o plano "Se ___, então eu ___".');
  }
  return erros;
}

export function metasDaSemana(metas, semanaInicio) {
  return metas.filter((m) => m.semana_inicio === semanaInicio);
}

export function podeCriarMeta(metas, semanaInicio) {
  return metasDaSemana(metas, semanaInicio).length < LIMITE_METAS;
}

export function progressoDaMeta(meta, registros) {
  return registros.filter((r) => r.meta_id === meta.id).reduce((s, r) => s + Number(r.valor), 0);
}

export function progressoNoDia(meta, registros, iso) {
  return registros
    .filter((r) => r.meta_id === meta.id && r.data === iso)
    .reduce((s, r) => s + Number(r.valor), 0);
}

export function percentual(atual, alvo) {
  if (!alvo) return 0;
  return Math.min(100, Math.round((atual / alvo) * 100));
}

// cumprida: atingiu o alvo; parcial: houve algum avanço; nao_cumprida: nenhum registro.
export function statusDaMeta(meta, registros) {
  const atual = progressoDaMeta(meta, registros);
  if (atual >= meta.valor_alvo) return 'cumprida';
  if (atual > 0) return 'parcial';
  return 'nao_cumprida';
}

export const NOMES_STATUS = {
  cumprida: 'Cumprida',
  parcial: 'Parcialmente cumprida',
  nao_cumprida: 'Não cumprida',
};

export function metaPlanejadaNoDia(meta, iso) {
  if (inicioDaSemana(iso) !== meta.semana_inicio) return false;
  if (meta.frequencia.tipo === 'semana') return true;
  return meta.frequencia.dias.includes(diaDaSemana(iso));
}

export function acoesDoDia(metas, registros, iso) {
  return metas
    .filter((m) => metaPlanejadaNoDia(m, iso))
    .map((m) => {
      const total = progressoDaMeta(m, registros);
      const hoje = progressoNoDia(m, registros, iso);
      const sugerido = m.frequencia.tipo === 'dias' ? m.valor_por_vez : 1;
      return { meta: m, total, hoje, feitoHoje: m.frequencia.tipo === 'dias' ? hoje >= m.valor_por_vez : hoje > 0, sugerido };
    });
}

// Regra "nunca falhar dois dias seguidos": ontem era dia planejado e não houve registro.
export function falhouOntem(meta, registros, hojeIso) {
  if (meta.frequencia.tipo !== 'dias') return false;
  const ontem = somarDias(hojeIso, -1);
  if (meta.criada_em && ontem < meta.criada_em) return false;
  if (!metaPlanejadaNoDia(meta, ontem)) return false;
  return progressoNoDia(meta, registros, ontem) === 0;
}

export const MENSAGEM_NOVA_CHANCE = 'Amanhã é uma nova chance. O importante é não falhar dois dias seguidos.';
export const MENSAGEM_HOJE_CONTA = 'Ontem não deu certo. Hoje é o dia que mais conta: o importante é não falhar dois dias seguidos.';

// ---------- Parceiros e mensagens ----------

export function mensagemParaParceiro(meta, nomeParceiro, registros, nomeUsuaria) {
  const saud = nomeParceiro ? `Oi, ${nomeParceiro}!` : 'Oi!';
  const assinatura = nomeUsuaria ? `\n${nomeUsuaria}` : '';
  let texto = `${saud} Nesta semana (${formatarCurta(meta.semana_inicio)}) eu me comprometi a: ${meta.descricao.trim()} — ${descreverMedida(meta)}.`;
  if (meta.compartilhamento === 'progresso') {
    const atual = progressoDaMeta(meta, registros);
    texto += ` Até agora: ${atual} de ${meta.valor_alvo} ${meta.unidade}.`;
  }
  texto += ' Queria que você soubesse.';
  return texto + assinatura;
}

export function historicoCompromissos(metas) {
  return metas
    .filter((m) => m.parceiro_id || m.parceiro_nome)
    .sort((a, b) => (a.semana_inicio < b.semana_inicio ? 1 : -1))
    .map((m) => ({
      semana_inicio: m.semana_inicio,
      texto: `Semana de ${formatarCurta(m.semana_inicio)} — Comprometi-me com ${m.parceiro_nome} a ${m.descricao.trim()}`,
      meta_id: m.id,
    }));
}

// ---------- Revisão ----------

export function resumoDaSemana(dados, semanaInicio) {
  const metas = metasDaSemana(dados.metas, semanaInicio);
  const porStatus = { cumprida: [], parcial: [], nao_cumprida: [] };
  for (const m of metas) {
    const st = statusDaMeta(m, dados.registros);
    porStatus[st].push({ meta: m, atual: progressoDaMeta(m, dados.registros) });
  }

  const porCategoria = {};
  for (const chave of Object.keys(CATEGORIAS)) porCategoria[chave] = { feitos: 0, total: 0 };
  for (const dia of diasDaSemana(semanaInicio)) {
    for (const c of compromissosDoDia(dados.compromissos, dia)) {
      porCategoria[c.categoria].total += 1;
      if (c.feito) porCategoria[c.categoria].feitos += 1;
    }
  }

  return { metas: porStatus, totalMetas: metas.length, porCategoria };
}

// Média do percentual das metas de cada semana (limitado a 100% por meta).
export function evolucao(dados, semanaFinal, quantidade = 4) {
  const semanas = [];
  for (let i = quantidade - 1; i >= 0; i--) {
    const inicio = somarDias(semanaFinal, -7 * i);
    const metas = metasDaSemana(dados.metas, inicio);
    const pcts = metas.map((m) => percentual(progressoDaMeta(m, dados.registros), m.valor_alvo));
    const media = pcts.length ? Math.round(pcts.reduce((s, p) => s + p, 0) / pcts.length) : null;
    const cumpridas = metas.filter((m) => statusDaMeta(m, dados.registros) === 'cumprida').length;
    semanas.push({ semana_inicio: inicio, media, cumpridas, total: metas.length });
  }
  return semanas;
}

// Copia uma meta antiga para outra semana, sem progresso e sem identificador.
export function reaproveitarMeta(meta) {
  const { id, semana_inicio, progresso_atual, status, criada_em, ...resto } = meta;
  return { ...resto, frequencia: { ...meta.frequencia, dias: [...(meta.frequencia.dias || [])] } };
}

// ---------- Lembretes ----------

// Decide quais lembretes devem disparar agora, respeitando o limite diário.
// Um lembrete só dispara até 3 horas depois do horário escolhido, para não chegar fora de contexto
// (por exemplo, o resumo da manhã às 21h, se o app só for aberto à noite).
export const JANELA_LEMBRETE_MIN = 180;

export function lembretesDevidos(ajustes, enviadosHoje, agora) {
  const cfg = ajustes.notificacoes;
  if (!cfg || cfg.silenciado) return [];
  const minutosAgora = agora.getHours() * 60 + agora.getMinutes();
  const devidos = [];
  for (const tipo of ['manha', 'tarde', 'domingo']) {
    const item = cfg[tipo];
    if (!item || !item.ativo) continue;
    if (tipo === 'domingo' && agora.getDay() !== 0) continue;
    if (enviadosHoje.includes(tipo)) continue;
    const [hh, mm] = item.hora.split(':').map(Number);
    const atraso = minutosAgora - (hh * 60 + mm);
    if (atraso >= 0 && atraso <= JANELA_LEMBRETE_MIN) devidos.push(tipo);
  }
  const restantes = Math.max(0, LIMITE_NOTIFICACOES_DIA - enviadosHoje.length);
  return devidos.slice(0, restantes);
}
