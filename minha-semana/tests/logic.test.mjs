// Testes do núcleo de regras. Executar com: node --test minha-semana/tests
import { test } from 'node:test';
import assert from 'node:assert/strict';
import * as L from '../js/logic.js';

const meta = (extra = {}) => ({
  id: 'm1',
  semana_inicio: '2026-10-19',
  descricao: 'Praticar matemática',
  valor_por_vez: 5,
  unidade: 'exercícios',
  frequencia: { tipo: 'dias', dias: [1, 2, 3, 4] },
  valor_alvo: 20,
  plano_se: 'chegar cansada',
  plano_entao: 'faço só 5 exercícios',
  ...extra,
});

test('a semana começa na segunda-feira', () => {
  assert.equal(L.inicioDaSemana('2026-10-20'), '2026-10-19'); // terça
  assert.equal(L.inicioDaSemana('2026-10-19'), '2026-10-19'); // segunda
  assert.equal(L.inicioDaSemana('2026-10-25'), '2026-10-19'); // domingo
  assert.equal(L.inicioDaSemana('2026-11-01'), '2026-10-26');
});

test('aritmética de datas atravessa meses e anos', () => {
  assert.equal(L.somarDias('2026-12-30', 3), '2027-01-02');
  assert.equal(L.diferencaDias('2026-10-20', '2026-10-27'), 7);
});

test('no domingo, o planejamento sugere a próxima semana', () => {
  assert.equal(L.semanaParaPlanejar('2026-10-25'), '2026-10-26');
  assert.equal(L.semanaParaPlanejar('2026-10-21'), '2026-10-19');
});

test('revisão sugere a semana anterior no início da semana, se ainda não revisada', () => {
  assert.equal(L.semanaParaRevisar('2026-10-20', []), '2026-10-12');
  assert.equal(L.semanaParaRevisar('2026-10-20', [{ semana_inicio: '2026-10-12' }]), '2026-10-19');
  assert.equal(L.semanaParaRevisar('2026-10-25', []), '2026-10-19');
});

test('compromissos do dia: com horário em ordem, sem horário depois', () => {
  const lista = [
    { id: 'a', titulo: 'Zeta', categoria: 'outro', data: '2026-10-20', horario: '', recorrencia: null },
    { id: 'b', titulo: 'Treino', categoria: 'treino', data: '2026-10-20', horario: '17:00', recorrencia: null },
    { id: 'c', titulo: 'Aula', categoria: 'estudo', data: '2026-10-20', horario: '08:00', recorrencia: null },
    { id: 'd', titulo: 'Alfa', categoria: 'licao', data: '2026-10-20', horario: '', recorrencia: null },
    { id: 'e', titulo: 'Outro dia', categoria: 'licao', data: '2026-10-21', horario: '', recorrencia: null },
  ];
  assert.deepEqual(L.compromissosDoDia(lista, '2026-10-20').map((c) => c.id), ['c', 'b', 'd', 'a']);
});

test('recorrência semanal respeita dias, início e data final', () => {
  const treino = { id: 't', titulo: 'Treino', categoria: 'treino', data: '2026-10-20', recorrencia: { dias: [2, 4], ate: '2026-11-05' } };
  assert.equal(L.ocorreEm(treino, '2026-10-20'), true); // terça
  assert.equal(L.ocorreEm(treino, '2026-10-22'), true); // quinta
  assert.equal(L.ocorreEm(treino, '2026-10-21'), false); // quarta
  assert.equal(L.ocorreEm(treino, '2026-10-15'), false); // antes do início
  assert.equal(L.ocorreEm(treino, '2026-11-10'), false); // depois do fim
});

test('conclusão de recorrentes é por ocorrência', () => {
  let t = { id: 't', titulo: 'Treino', categoria: 'treino', data: '2026-10-20', recorrencia: { dias: [2, 4], ate: null }, concluidas: [] };
  t = L.alternarConclusao(t, '2026-10-20');
  assert.equal(L.estaConcluido(t, '2026-10-20'), true);
  assert.equal(L.estaConcluido(t, '2026-10-22'), false);
  t = L.alternarConclusao(t, '2026-10-20');
  assert.equal(L.estaConcluido(t, '2026-10-20'), false);
});

test('provas nos próximos 7 dias com contagem regressiva', () => {
  const lista = [
    { id: 'p1', titulo: 'Prova de História', categoria: 'prova', data: '2026-10-23', recorrencia: null },
    { id: 'p2', titulo: 'Prova distante', categoria: 'prova', data: '2026-11-10', recorrencia: null },
    { id: 'x', titulo: 'Lição', categoria: 'licao', data: '2026-10-21', recorrencia: null },
  ];
  const r = L.provasProximas(lista, '2026-10-20');
  assert.equal(r.length, 1);
  assert.equal(r[0].faltam, 3);
  assert.equal(L.textoContagem(0), 'Hoje');
  assert.equal(L.textoContagem(1), 'Amanhã');
  assert.equal(L.textoContagem(3), 'Em 3 dias');
});

test('sugestão de revisões não cria blocos no passado', () => {
  const prova = { titulo: 'Prova', materia: 'Geografia', data: '2026-10-22' };
  const s = L.sugerirRevisoes(prova, '2026-10-20');
  assert.deepEqual(s.map((x) => x.data), ['2026-10-20', '2026-10-21']);
  assert.equal(s[0].categoria, 'estudo');
  assert.equal(s[0].titulo, 'Revisão: Geografia');
});

test('alvo semanal = valor × dias, ou o próprio valor no total da semana', () => {
  assert.equal(L.calcularAlvo(5, { tipo: 'dias', dias: [1, 2, 3, 4] }), 20);
  assert.equal(L.calcularAlvo(3, { tipo: 'semana', dias: [] }), 3);
  assert.equal(L.calcularAlvo('', { tipo: 'semana' }), 0);
});

test('meta sem métrica numérica ou sem plano Se–Então não é válida', () => {
  assert.deepEqual(L.validarMeta(meta()), []);
  assert.ok(L.validarMeta(meta({ valor_por_vez: '' })).length > 0);
  assert.ok(L.validarMeta(meta({ valor_por_vez: 0 })).length > 0);
  assert.ok(L.validarMeta(meta({ unidade: ' ' })).length > 0);
  assert.ok(L.validarMeta(meta({ plano_se: '' })).length > 0);
  assert.ok(L.validarMeta(meta({ plano_entao: '  ' })).length > 0);
  assert.ok(L.validarMeta(meta({ frequencia: { tipo: 'dias', dias: [] } })).length > 0);
});

test('limite de 3 metas por semana', () => {
  const metas = [meta({ id: 'a' }), meta({ id: 'b' })];
  assert.equal(L.podeCriarMeta(metas, '2026-10-19'), true);
  metas.push(meta({ id: 'c' }));
  assert.equal(L.podeCriarMeta(metas, '2026-10-19'), false);
  assert.equal(L.podeCriarMeta(metas, '2026-10-26'), true);
});

test('status da meta: cumprida, parcial e não cumprida', () => {
  const m = meta();
  assert.equal(L.statusDaMeta(m, []), 'nao_cumprida');
  assert.equal(L.statusDaMeta(m, [{ meta_id: 'm1', data: '2026-10-19', valor: 12 }]), 'parcial');
  assert.equal(L.statusDaMeta(m, [{ meta_id: 'm1', data: '2026-10-19', valor: 20 }]), 'cumprida');
});

test('ações do dia só aparecem nos dias planejados da semana da meta', () => {
  const m = meta(); // seg a qui
  assert.equal(L.acoesDoDia([m], [], '2026-10-20').length, 1); // terça
  assert.equal(L.acoesDoDia([m], [], '2026-10-24').length, 0); // sábado
  assert.equal(L.acoesDoDia([m], [], '2026-10-27').length, 0); // outra semana
  const semanal = meta({ id: 'm2', frequencia: { tipo: 'semana', dias: [] }, valor_por_vez: 3, valor_alvo: 3 });
  assert.equal(L.acoesDoDia([semanal], [], '2026-10-24').length, 1);
});

test('regra "nunca falhar dois dias seguidos"', () => {
  const m = meta();
  assert.equal(L.falhouOntem(m, [], '2026-10-20'), true); // segunda planejada sem registro
  assert.equal(L.falhouOntem(m, [{ meta_id: 'm1', data: '2026-10-19', valor: 5 }], '2026-10-20'), false);
  assert.equal(L.falhouOntem(m, [], '2026-10-24'), false); // sexta não era planejada
  assert.equal(L.falhouOntem(meta({ criada_em: '2026-10-20' }), [], '2026-10-20'), false); // meta criada hoje
  assert.equal(L.falhouOntem(meta({ criada_em: '2026-10-20' }), [], '2026-10-22'), true);
});

test('mensagem para parceiro respeita o nível de compartilhamento', () => {
  const registros = [{ meta_id: 'm1', data: '2026-10-19', valor: 12 }];
  const soMeta = L.mensagemParaParceiro(meta({ compartilhamento: 'meta' }), 'Ana', registros, 'Sophie');
  assert.ok(soMeta.startsWith('Oi, Ana!'));
  assert.ok(!soMeta.includes('Até agora'));
  const comProgresso = L.mensagemParaParceiro(meta({ compartilhamento: 'progresso' }), 'Ana', registros, 'Sophie');
  assert.ok(comProgresso.includes('Até agora: 12 de 20 exercícios'));
});

test('histórico de compromissos com parceiros', () => {
  const h = L.historicoCompromissos([meta({ parceiro_id: 'p', parceiro_nome: 'Ana' }), meta({ id: 'x' })]);
  assert.equal(h.length, 1);
  assert.equal(h[0].texto, 'Semana de 19/10 — Comprometi-me com Ana a Praticar matemática');
});

test('resumo semanal conta metas por status e compromissos por categoria', () => {
  const dados = {
    metas: [meta(), meta({ id: 'm2', descricao: 'Ler', valor_alvo: 10 }), meta({ id: 'm3', descricao: 'Correr', valor_alvo: 3 })],
    registros: [
      { meta_id: 'm1', data: '2026-10-19', valor: 20 },
      { meta_id: 'm2', data: '2026-10-20', valor: 4 },
    ],
    compromissos: [
      { id: 'a', titulo: 'Lição 1', categoria: 'licao', data: '2026-10-20', recorrencia: null, concluido: true },
      { id: 'b', titulo: 'Lição 2', categoria: 'licao', data: '2026-10-21', recorrencia: null, concluido: false },
      { id: 't', titulo: 'Treino', categoria: 'treino', data: '2026-10-20', recorrencia: { dias: [2, 4], ate: null }, concluidas: ['2026-10-20'] },
      { id: 'z', titulo: 'Fora', categoria: 'licao', data: '2026-10-27', recorrencia: null, concluido: true },
    ],
  };
  const r = L.resumoDaSemana(dados, '2026-10-19');
  assert.equal(r.metas.cumprida.length, 1);
  assert.equal(r.metas.parcial.length, 1);
  assert.equal(r.metas.nao_cumprida.length, 1);
  assert.deepEqual(r.porCategoria.licao, { feitos: 1, total: 2 });
  assert.deepEqual(r.porCategoria.treino, { feitos: 1, total: 2 });
});

test('evolução das últimas 4 semanas', () => {
  const dados = {
    metas: [meta(), meta({ id: 'old', semana_inicio: '2026-10-05', valor_alvo: 10 })],
    registros: [{ meta_id: 'm1', data: '2026-10-19', valor: 10 }, { meta_id: 'old', data: '2026-10-05', valor: 15 }],
    compromissos: [],
  };
  const e = L.evolucao(dados, '2026-10-19');
  assert.deepEqual(e.map((s) => s.semana_inicio), ['2026-09-28', '2026-10-05', '2026-10-12', '2026-10-19']);
  assert.deepEqual(e.map((s) => s.media), [null, 100, null, 50]);
  assert.equal(e[1].cumpridas, 1);
});

test('reaproveitar meta remove identificador e progresso', () => {
  const r = L.reaproveitarMeta(meta({ progresso_atual: 12, status: 'ativa' }));
  assert.equal(r.id, undefined);
  assert.equal(r.semana_inicio, undefined);
  assert.equal(r.progresso_atual, undefined);
  assert.equal(r.descricao, 'Praticar matemática');
});

test('lembretes: horário, domingo, silêncio, janela e limite diário', () => {
  const ajustes = {
    notificacoes: {
      silenciado: false,
      manha: { ativo: true, hora: '07:00' },
      tarde: { ativo: true, hora: '15:00' },
      domingo: { ativo: true, hora: '15:00' },
    },
  };
  const tercaManha = new Date(2026, 9, 20, 7, 30);
  assert.deepEqual(L.lembretesDevidos(ajustes, [], tercaManha), ['manha']);
  assert.deepEqual(L.lembretesDevidos(ajustes, ['manha'], tercaManha), []);
  const tercaNoite = new Date(2026, 9, 20, 21, 0);
  assert.deepEqual(L.lembretesDevidos(ajustes, [], tercaNoite), []); // fora da janela de 3 h
  const domingo = new Date(2026, 9, 25, 15, 5);
  assert.deepEqual(L.lembretesDevidos(ajustes, ['manha'], domingo), ['tarde', 'domingo']);
  assert.deepEqual(L.lembretesDevidos(ajustes, ['manha', 'x'], domingo).length, 1); // limite de 3 por dia
  assert.deepEqual(L.lembretesDevidos({ notificacoes: { ...ajustes.notificacoes, silenciado: true } }, [], tercaManha), []);
});
