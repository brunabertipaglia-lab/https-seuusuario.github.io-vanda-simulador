# Minha Semana

Aplicativo web progressivo (PWA), *mobile-first* e em português do Brasil, para que uma estudante adolescente organize compromissos diários, defina até três metas semanais mensuráveis, registre com quem se comprometeu e revise a semana. Os dados ficam apenas no aparelho.

O app não usa framework nem dependências de execução: é HTML, CSS e JavaScript (módulos ES) servidos como arquivos estáticos.

## Como rodar

Os módulos JavaScript não carregam pelo protocolo `file://`, então é preciso um servidor estático simples:

```bash
cd minha-semana
python3 -m http.server 8080
# abra http://localhost:8080 no navegador (ou no celular, pelo IP do computador na mesma rede)
```

Para instalar no celular, publique a pasta em um endereço HTTPS (por exemplo, GitHub Pages) e use **Adicionar à tela inicial**. O funcionamento offline depende do *service worker*, que só é ativado em HTTPS ou em `localhost`.

Para simular outra data em demonstrações: `index.html?hoje=2026-10-25`.

## Testes

```bash
cd minha-semana
npm test            # 21 testes das regras (Node 18+, sem dependências)
npm run test:e2e    # fluxo completo em Chromium com tela de celular (requer: npm i -D playwright)
```

O teste ponta a ponta verifica os critérios de aceitação: compromisso criado com dois toques e digitação; bloqueio de meta sem métrica numérica e sem plano Se–Então; limite de 3 metas (na lista e no acesso direto ao formulário); resumo automático da revisão com contagens conferidas; abertura offline; ausência de erros no console.

## Estrutura de pastas

```
minha-semana/
├── index.html              # casca do app, navegação inferior e botão +
├── manifest.webmanifest    # instalação como app
├── sw.js                   # cache offline (nenhum dado trafega pela rede)
├── css/styles.css          # tema claro/escuro, cores por categoria, impressão
├── icons/                  # ícones SVG e PNG (192, 512, maskable)
├── js/
│   ├── app.js              # rotas por hash e ciclo de desenho
│   ├── logic.js            # regras puras (datas, metas, revisão, lembretes) — testadas
│   ├── store.js            # persistência em localStorage, exportação/importação
│   ├── ui.js               # elementos, ícones, folhas modais, avisos
│   ├── lembretes.js        # notificações locais (máx. 3/dia)
│   └── views/              # hoje, agenda, metas, parceiros, revisao, ajustes, compromisso, comum
└── tests/
    ├── logic.test.mjs      # testes unitários (node:test)
    └── e2e/fluxo.e2e.mjs   # teste ponta a ponta (Playwright)
```

## Telas

| Tela | O que faz |
|---|---|
| **Hoje** | Data, saudação, provas nos próximos 7 dias com contagem regressiva, ações das metas do dia com registro em um toque, compromissos em ordem cronológica com conclusão. |
| **Agenda** | Visões semanal e mensal; compromissos recorrentes por dia da semana, com data final opcional; ao cadastrar uma prova, oferece blocos de revisão em até 3 dias anteriores. |
| **Metas** | Fluxo guiado, uma pergunta por tela: meta → medida (número + unidade + frequência, com alvo calculado) → por que importa → obstáculo → plano Se–Então → parceiro → confirmação. Permite reaproveitar metas das últimas 4 semanas. |
| **Parceiros** | Cadastro (nome e relação), nível padrão de compartilhamento, mensagem para copiar e histórico "Semana de 19/10 — Comprometi-me com…". |
| **Revisão** | Metas cumpridas, parciais e não cumpridas; compromissos concluídos por categoria; gráfico das últimas 4 semanas; três perguntas reflexivas; atalho para planejar a próxima semana; impressão em PDF. |
| **Ajustes** | Nome, lembretes (manhã, lição à tarde, domingo; silenciar tudo), exportar/importar JSON, apagar dados, texto de privacidade. |

## Decisões de implementação

| Tema | Decisão | Motivo |
|---|---|---|
| Semana | Segunda a domingo; `semana_inicio` é sempre uma segunda. No domingo, o planejamento sugere a semana seguinte. | Alinha a revisão de domingo ao fechamento da semana. |
| Alvo da meta | `valor por vez × dias escolhidos`, ou o próprio valor quando a frequência é "no total da semana". | Torna a meta verificável (ex.: 5 exercícios × 4 dias = 20). |
| Motivação e obstáculo | Obrigatórios no fluxo (texto livre, curto). | O efeito do contraste mental (WOOP) depende de explicitar o obstáculo antes do plano. |
| Status semanal | Cumprida: atingiu o alvo. Parcial: qualquer avanço registrado. Não cumprida: nenhum registro. | Critério transparente e não punitivo; o percentual exato aparece ao lado. |
| "Nunca falhar dois dias seguidos" | Se ontem era dia planejado e ficou sem registro, a ação de hoje mostra um aviso sem culpa. Dias anteriores à criação da meta são ignorados. Após as 20h, se a ação do dia ainda não foi feita, aparece "Amanhã é uma nova chance…". | Regra pedida, sem penalizar falhas pontuais. |
| Compartilhamento | Definido por meta (o cadastro do parceiro guarda só o padrão). O nome do parceiro é copiado para a meta. | Atende "para cada meta, Sophie define o que ele verá" e preserva o histórico mesmo se o parceiro for removido. |
| Gráfico | Média do progresso das metas de cada semana, limitada a 100% por meta. | Uma meta superada não mascara outra não iniciada. |
| Sem gamificação | Nenhum ponto, moeda, sequência ou ranking. | Recompensas extrínsecas podem reduzir motivação intrínseca. |

## Limitação conhecida: lembretes

Aplicativos web não conseguem agendar notificações para quando estão totalmente fechados sem um servidor de *push*, e este app, por decisão de privacidade, não tem servidor. Os lembretes disparam quando o app está aberto ou em segundo plano, até 3 horas depois do horário escolhido (para não chegar fora de contexto). Sem permissão de notificação, aparecem como aviso dentro do app. Para lembretes garantidos com o app fechado, há dois caminhos: empacotar como app nativo (Capacitor, com notificações locais) ou criar um servidor de *push*, o que exigiria consentimento do responsável (LGPD, art. 14).

## Privacidade

- Dados só no `localStorage` do aparelho; nenhum envio de rede, conta, anúncio, chat ou rede social.
- Nenhum dado de localização, foto ou contato.
- Mensagens a parceiros são copiadas e enviadas manualmente pela usuária.
- Exportação em JSON (cópia de segurança) e em PDF (impressão da revisão).
- Atenção: limpar os dados do navegador apaga os dados do app. Recomenda-se exportar a cópia JSON periodicamente.

## Melhorias para versões futuras

1. Notificações com o app fechado, via empacotamento nativo (Capacitor) com notificações locais.
2. Edição de uma meta já criada (hoje é possível excluir e recriar) e exclusão de uma única ocorrência de compromisso recorrente.
3. Cópia de segurança automática para um arquivo escolhido pela usuária (File System Access API, onde houver suporte).
4. Sincronização opcional entre aparelhos com criptografia de ponta a ponta, condicionada ao consentimento do responsável legal.
5. Personalização feita com a própria Sophie: nomes e cores das categorias, tema e textos de apoio.
6. Planejador de estudos para olimpíadas: dividir um conteúdo grande em blocos semanais com revisão espaçada.
7. Relatório mensal de reflexões (o que funcionou e o que foi ajustado), para identificar estratégias que se repetem.
8. Testes de acessibilidade automatizados (axe-core) e validação com leitor de tela (TalkBack e VoiceOver).

## Fundamentação

Locke & Latham (2002), *American Psychologist*; Gollwitzer & Sheeran (2006), *Advances in Experimental Social Psychology*; Oettingen (2014), *Rethinking Positive Thinking*; Harkin et al. (2016), *Psychological Bulletin*; Lally et al. (2010), *European Journal of Social Psychology*; Ryan & Deci (2000), *American Psychologist*; Deci, Koestner & Ryan (1999), *Psychological Bulletin*.
