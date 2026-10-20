// Teste ponta a ponta dos critérios de aceitação, em um navegador real com tela de celular.
// Requer Playwright: npm i -D playwright && npx playwright install chromium
// Uso: node tests/e2e/fluxo.e2e.mjs [pasta-para-capturas]
import { createServer } from 'node:http';
import { readFile, mkdir } from 'node:fs/promises';
import { extname, join, normalize, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';
import assert from 'node:assert/strict';
import { chromium } from 'playwright';

const RAIZ = join(dirname(fileURLToPath(import.meta.url)), '..', '..');
const CAPTURAS = process.argv[2] || null;
const TIPOS = {
  '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.svg': 'image/svg+xml',
  '.png': 'image/png', '.webmanifest': 'application/manifest+json', '.json': 'application/json',
};

const servidor = createServer(async (req, res) => {
  const caminho = normalize(decodeURIComponent(new URL(req.url, 'http://x').pathname)).replace(/^([/\\])+/, '');
  try {
    const corpo = await readFile(join(RAIZ, caminho || 'index.html'));
    res.writeHead(200, { 'content-type': TIPOS[extname(caminho || 'index.html')] || 'application/octet-stream' });
    res.end(corpo);
  } catch {
    res.writeHead(404);
    res.end();
  }
});
await new Promise((r) => servidor.listen(0, '127.0.0.1', r));
const BASE = `http://127.0.0.1:${servidor.address().port}/index.html?hoje=2026-10-20`;

const navegador = await chromium.launch({ executablePath: process.env.CHROMIUM_PATH || undefined });
const contexto = await navegador.newContext({ viewport: { width: 390, height: 844 }, locale: 'pt-BR', hasTouch: true, isMobile: true });
const pagina = await contexto.newPage();
const errosConsole = [];
pagina.on('pageerror', (e) => errosConsole.push(e.message));
pagina.on('console', (m) => { if (m.type() === 'error') errosConsole.push(m.text()); });

const captura = async (nome) => {
  if (!CAPTURAS) return;
  await mkdir(CAPTURAS, { recursive: true });
  await pagina.screenshot({ path: join(CAPTURAS, `${nome}.png`), fullPage: true });
};
const ok = (msg) => console.log(`  ✔ ${msg}`);
const continuar = () => pagina.getByRole('button', { name: 'Continuar' }).click();

try {
  // 1. Boas-vindas
  await pagina.goto(BASE);
  await captura('01-boas-vindas');
  await pagina.getByLabel('Como você quer ser chamada? (opcional)').fill('Sophie');
  await pagina.getByRole('button', { name: 'Começar' }).click();
  await pagina.getByRole('heading', { level: 1, name: /Sophie/ }).waitFor();
  ok('boas-vindas e saudação com nome');

  // 2. Adicionar compromisso em menos de 10 segundos (toque no +, título, Salvar)
  const t0 = Date.now();
  await pagina.getByRole('button', { name: 'Adicionar compromisso' }).click();
  await pagina.keyboard.type('Lista de frações');
  await pagina.getByRole('button', { name: 'Salvar' }).click();
  await pagina.getByText('Lista de frações').waitFor();
  const duracao = Date.now() - t0;
  assert.ok(duracao < 10000);
  ok(`compromisso adicionado com 2 toques + digitação (${duracao} ms de automação)`);

  // 3. Prova com sugestão de blocos de revisão
  await pagina.getByRole('button', { name: 'Adicionar compromisso' }).click();
  await pagina.keyboard.type('Prova de Geografia');
  await pagina.locator('label.ficha', { hasText: 'Prova' }).click();
  await pagina.getByLabel('Data').fill('2026-10-23');
  await pagina.getByText('Horário, matéria, observação e repetição').click();
  await pagina.getByLabel('Matéria (opcional)').fill('Geografia');
  await pagina.getByRole('button', { name: 'Salvar' }).click();
  await pagina.getByRole('heading', { name: 'Criar blocos de revisão?' }).waitFor();
  await captura('02-sugestao-revisao');
  await pagina.getByRole('button', { name: 'Criar revisões' }).click();
  await pagina.getByText('Revisão: Geografia').first().waitFor();
  await pagina.getByText('Em 3 dias').waitFor();
  ok('prova cria contagem regressiva e blocos de revisão');

  // 4. Treino recorrente (terça e quinta)
  await pagina.getByRole('button', { name: 'Adicionar compromisso' }).click();
  await pagina.keyboard.type('Treino de vôlei');
  await pagina.locator('label.ficha', { hasText: 'Treino' }).click();
  await pagina.getByText('Horário, matéria, observação e repetição').click();
  await pagina.getByLabel('Horário (opcional)').fill('17:30');
  await pagina.getByText('Repetir toda semana').click();
  await pagina.locator('dialog .fichas.dias label', { hasText: 'Qui' }).click();
  await pagina.getByRole('button', { name: 'Salvar' }).click();
  await pagina.getByText('Treino de vôlei').waitFor();
  ok('compromisso recorrente criado');

  // 5. Meta guiada, com validação de métrica e de plano Se–Então
  await pagina.getByRole('link', { name: 'Metas', exact: true }).click();
  await pagina.getByRole('link', { name: 'Nova meta' }).click();
  await pagina.getByLabel('Minha meta').fill('Praticar matemática para a olimpíada');
  await continuar();
  await continuar();
  await pagina.getByText('A meta precisa de um número maior que zero.').waitFor();
  ok('não avança sem métrica numérica');
  await pagina.getByLabel('Quanto').fill('5');
  await pagina.getByLabel('De quê').fill('exercícios');
  await pagina.getByText('Alvo da semana: 20 exercícios').waitFor();
  await captura('03-meta-medida');
  await continuar();
  await pagina.getByLabel('Minha razão').fill('Quero chegar confiante na segunda fase.');
  await continuar();
  await pagina.getByLabel('O obstáculo mais provável').fill('Chegar cansada do treino');
  await continuar();
  await continuar();
  await pagina.getByText('Complete as duas partes').waitFor();
  ok('não avança sem plano Se–Então completo');
  await pagina.getByLabel('então eu…').fill('faço só os 5 exercícios antes do banho');
  await continuar();
  await pagina.getByText('Adicionar outra pessoa').click();
  await pagina.getByLabel('Nome', { exact: true }).fill('Mãe');
  await pagina.getByRole('button', { name: 'Adicionar', exact: true }).click();
  await pagina.locator('label.ficha', { hasText: 'Meta e progresso' }).click();
  await captura('04-meta-parceiro');
  await continuar();
  await captura('05-meta-confirmar');
  await pagina.getByRole('button', { name: 'Salvar meta' }).click();
  await pagina.getByRole('heading', { name: 'Mensagem para Mãe' }).waitFor();
  const mensagem = await pagina.locator('dialog textarea').inputValue();
  assert.match(mensagem, /Oi, Mãe!/);
  assert.match(mensagem, /Até agora: 0 de 20 exercícios/);
  await pagina.getByRole('button', { name: 'Fechar', exact: true }).last().click();
  ok('meta salva e mensagem para o parceiro gerada (sem envio automático)');

  // 6. Mais duas metas e o limite de 3
  for (const [desc, valor, unidade] of [['Ler o livro de literatura', '10', 'páginas'], ['Treinar redação', '1', 'redação']]) {
    await pagina.getByRole('link', { name: 'Nova meta' }).click();
    await pagina.getByLabel('Minha meta').fill(desc);
    await continuar();
    await pagina.getByLabel('Quanto').fill(valor);
    await pagina.getByLabel('De quê').fill(unidade);
    if (unidade === 'redação') await pagina.locator('label.ficha', { hasText: 'No total da semana' }).click();
    await continuar();
    await pagina.getByLabel('Minha razão').fill('Importa para mim.');
    await continuar();
    await pagina.getByLabel('O obstáculo mais provável').fill('Celular');
    await continuar();
    await pagina.getByLabel('então eu…').fill('deixo o celular na sala');
    await continuar();
    await continuar();
    await pagina.getByRole('button', { name: 'Salvar meta' }).click();
    await pagina.getByText('3 metas').or(pagina.getByText('2 de 3 metas')).first().waitFor();
  }
  await pagina.getByText('Você já tem 3 metas nesta semana.').waitFor();
  assert.equal(await pagina.getByRole('link', { name: 'Nova meta' }).count(), 0);
  await captura('06-metas-limite');
  await pagina.goto(BASE + '#/metas/nova?semana=2026-10-19');
  await pagina.getByText('já tem 3 metas').waitFor();
  ok('limite de 3 metas ativo na lista e no acesso direto ao formulário');

  // 7. Hoje: ação da meta e regra "nunca falhar dois dias seguidos"
  await pagina.goto(BASE + '#/hoje');
  await pagina.getByRole('button', { name: 'Fiz 5 exercícios' }).waitFor();
  assert.equal(await pagina.getByText('Ontem não deu certo').count(), 0);
  ok('meta criada hoje não gera aviso sobre ontem');
  await pagina.getByRole('button', { name: 'Fiz 5 exercícios' }).click();
  await pagina.getByText('5/20 exercícios').waitFor();
  await pagina.getByLabel('Concluir: Lista de frações').check();
  await captura('07-hoje');
  ok('progresso registrado (5/20) e compromisso concluído');

  // Quinta-feira: a quarta era dia planejado e ficou sem registro
  await pagina.goto(BASE.replace('2026-10-20', '2026-10-22') + '#/hoje');
  await pagina.getByText('Ontem não deu certo').first().waitFor();
  ok('aviso sem culpa quando o dia anterior planejado ficou sem registro');
  await pagina.goto(BASE + '#/hoje');

  // 8. Agenda semanal e mensal
  await pagina.getByRole('link', { name: 'Agenda', exact: true }).click();
  await pagina.getByText('Semana de 19/10').waitFor();
  assert.equal(await pagina.getByText('Treino de vôlei').count(), 2);
  await captura('08-agenda-semana');
  await pagina.getByRole('tab', { name: 'Mês' }).click();
  await pagina.getByText('outubro de 2026').waitFor();
  await captura('09-agenda-mes');
  ok('agenda semanal mostra a recorrência terça/quinta; visão mensal ok');

  // 9. Revisão: resumo automático correto
  await pagina.getByRole('link', { name: 'Revisão', exact: true }).click();
  await pagina.locator('.navegador-centro', { hasText: 'Semana de 12/10 a 18/10' }).waitFor();
  await pagina.getByRole('button', { name: 'Próxima semana' }).click();
  await pagina.locator('.navegador-centro', { hasText: 'Semana de 19/10 a 25/10' }).waitFor();
  const placar = await pagina.locator('.placar-num').allInnerTexts();
  assert.deepEqual(placar, ['0', '1', '2']);
  const linhaLicao = await pagina.locator('tr', { hasText: 'Lição de casa' }).innerText();
  assert.match(linhaLicao, /1 de 1/);
  const linhaTreino = await pagina.locator('tr', { hasText: 'Treino' }).innerText();
  assert.match(linhaTreino, /0 de 2/);
  await pagina.getByLabel('O que funcionou?').fill('Fazer os exercícios logo depois do lanche.');
  await pagina.getByRole('button', { name: 'Salvar revisão' }).click();
  await captura('10-revisao');
  ok('revisão: 0 cumpridas, 1 parcial, 2 não cumpridas; compromissos por categoria corretos');

  // 10. Parceiros e histórico
  await pagina.goto(BASE + '#/parceiros');
  await pagina.getByText('Semana de 19/10 — Comprometi-me com Mãe a Praticar matemática para a olimpíada').waitFor();
  await captura('11-parceiros');
  ok('histórico de compromissos com parceiros');

  // 11. Persistência e funcionamento offline
  await pagina.evaluate(() => navigator.serviceWorker.ready);
  await pagina.reload();
  await pagina.evaluate(() => navigator.serviceWorker.ready);
  await contexto.setOffline(true);
  await pagina.goto(BASE + '#/hoje');
  await pagina.getByText('5/20 exercícios').waitFor();
  await contexto.setOffline(false);
  ok('dados persistem e o app abre offline');

  // 12. Modo escuro
  await pagina.emulateMedia({ colorScheme: 'dark' });
  await captura('12-hoje-escuro');

  assert.deepEqual(errosConsole, []);
  ok('nenhum erro no console');
  console.log('\nTodos os critérios de aceitação verificados.');
} catch (e) {
  await captura('falha');
  console.error(e);
  console.error('Erros de console:', errosConsole);
  process.exitCode = 1;
} finally {
  await navegador.close();
  servidor.close();
}
