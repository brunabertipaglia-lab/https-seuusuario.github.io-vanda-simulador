# Capi Tinkering — Missão Lunar · Conceito, escopo e plano

Equipe **CAPI TINKERING** · NASA Space Apps Challenge 2026 · documento vivo (versão 0.1, 12/10/2026)

> **Status da referência oficial.** Este documento parte dos textos fornecidos pela equipe. Uma fonte secundária indica que os enunciados completos dos desafios de 2026 serão publicados em **28/10/2026** e que o hackathon ocorrerá em **14–15/11/2026**. A página oficial não pôde ser acessada deste ambiente. **Ação obrigatória:** quando os enunciados forem publicados, revisar a matriz da seção 2 e marcar cada linha como confirmada ou ajustada. Nenhuma regra, critério de avaliação ou prazo oficial foi presumido aqui.

---

## 1. Síntese do conceito e diferencial educativo

O jogador recebe um objetivo científico em uma base lunar e precisa planejar, montar e operar a missão em turnos. Cada turno representa um intervalo declarado de tempo simulado (12 h na etapa 1). O Capi, capivara astronauta, acompanha o jogador: explica os conceitos no momento em que passam a importar, mostra a consequência prevista de cada configuração, faz perguntas e oferece pistas graduais, mas nunca escolhe pelo jogador.

O diferencial educativo está em tornar os *trade-offs* de engenharia **observáveis e reversíveis**:

| Princípio | Como aparece no jogo |
|---|---|
| Consequência visível antes da ação | Previsão do próximo turno e projeção de até 6 turnos com a configuração atual. |
| Causalidade rastreável | Cada turno gera um relatório com geração, demanda, energia atendida, não atendida e desperdiçada. |
| Erro como informação | Encerramento crítico explicado sem constrangimento; o reinício permite comparar estratégias. |
| Mais de uma estratégia válida | O balanceamento é verificado por testes automatizados (seção 4 do modelo). |
| Separação entre ciência e jogo | Dados verificados, simplificações e parâmetros fictícios são rotulados separadamente. |

A etapa 1 já ensina uma lição não trivial: **a prioridade de alimentação protege o suporte de vida dentro de um turno, mas não guarda energia para os turnos seguintes.** Manter a pesquisa ligada antes da sombra esvazia a bateria que o suporte de vida usará depois. Os testes `KeepingEverythingOn_EndsInCriticalFailure` e `ReactingTooLate_EndsInCriticalFailure` demonstram esse comportamento.

**Limite declarado:** concluir a missão não demonstra aprendizagem. Ganho educacional só poderá ser afirmado após avaliação com jogadores (backlog M7).

---

## 2. Matriz requisito → mecânica demonstrável

Fonte dos requisitos: textos fornecidos pela equipe (pendente de confirmação na página oficial).

| # | Requisito (referência inicial) | Mecânica no jogo | Como demonstrar | Marco | Situação |
|---|---|---|---|---|---|
| R1 | Projetar uma missão espacial | Seleção de alternativas de missão e equipamentos | Tela de planejamento com 2 alternativas comparáveis | M2 | Planejado |
| R2 | Gerenciar a missão | Operação por turnos, ligar/desligar sistemas | Botões de sistemas + "Avançar turno" | M1 | **Implementado (energia)** |
| R3 | Simular a missão completa | Ciclo planejar → lançar → instalar → operar → relatório | Fluxo de cenas encadeado | M2–M5 | Parcial (operar) |
| R4 | Tomar decisões de engenharia | Trade-offs bateria/massa, instrumentos/energia, blindagem/carga | Relatório comparativo entre partidas | M1–M4 | Parcial (energia × pesquisa) |
| R5 | Administrar recursos limitados | Energia, bateria, água, O₂, alimento, orçamento, massa | Painel de recursos com demanda não atendida | M1, M3 | Parcial (energia) |
| R6 | Jogo interativo para estudantes | Capi como guia, linguagem acessível, PT/EN | Sessão de 10–15 min | M1–M6 | Parcial |
| R7 | Operação de base lunar/marciana (desafio relacionado, tratado como cenário, não como o mesmo enunciado) | Base lunar com módulos e eventos | Cena isométrica | M1, M3 | Parcial |

---

## 3. Escopo do MVP e recursos adiados

### MVP (uma missão lunar)

| Área | Conteúdo mínimo |
|---|---|
| Objetivo científico | Meta mensurável em horas de pesquisa com energia plena. |
| Planejamento | Orçamento e massa de lançamento; validação de requisitos mínimos. |
| Tripulação | Necessidades declaradas de O₂, água, alimento e energia de suporte de vida. |
| Energia | Painéis, baterias, prioridade de alimentação e reserva mínima. |
| Consumíveis | Reservas de água, O₂ e alimento (sem reciclagem avançada). |
| Pesquisa | Módulo de pesquisa com consumo de energia. |
| Comunicação | Disponibilidade simplificada por janela. |
| Radiação | Índice didático de exposição e proteção (não é dose física nem avaliação médica). |
| Estratégias | Pelo menos duas viáveis, com vantagens diferentes. |
| Eventos | Queda de geração, falha de equipamento, alerta de radiação (semente controlável). |
| Fim de partida | Vitória, conclusão sem objetivo, encerramento crítico e reinício. |
| Relatório | Histórico de decisões e efeitos sobre segurança, autonomia e pesquisa. |

### Adiado

Produção de alimentos, reciclagem avançada, Marte, cenários adicionais, multiplayer, serviços online, IA generativa em tempo real, dinâmica orbital real, modelos 3D finais e persistência (entra após o ciclo principal funcionar).

---

## 4. Arquitetura e estrutura de pastas

```
unity/Assets/CapiTinkering/
├── Scripts/
│   ├── Simulation/        # C# puro (noEngineReferences = true): regras testáveis sem Unity
│   │   ├── SimMath.cs             conversões e validação numérica
│   │   ├── EquipmentSpec.cs       definição imutável de consumidor
│   │   ├── MissionParameters.cs   parâmetros imutáveis da missão
│   │   ├── MissionState.cs        estado da partida (separado das definições)
│   │   ├── TurnReport.cs          resultado de um turno (também usado como previsão)
│   │   ├── SimulationEngine.cs    ÚNICA rotina que avança o turno
│   │   ├── MissionSession.cs      ações do jogador + eventos C#
│   │   ├── CapiAdvisor.cs         regras de feedback do Capi (função pura)
│   │   └── TextTable.cs           textos pt-BR / en
│   ├── Game/              # MonoBehaviours e ScriptableObjects (adaptadores)
│   │   ├── EquipmentDefinition.cs, MissionDefinition.cs   (assets editáveis)
│   │   ├── MissionController.cs   cria a sessão, expõe ações
│   │   ├── MissionHUD.cs          apresentação (só lê e chama ações)
│   │   ├── PowerSceneView.cs      cena 3D (Sol, bateria, módulos)
│   │   └── BrandPalette.cs        cores + opções de acessibilidade
│   └── Editor/
│       └── Stage1SceneBuilder.cs  menu que cria dados e cena com referências ligadas
├── Tests/
│   ├── EditMode/          # regras isoladas (também rodam fora da Unity: tools/CoreTests)
│   └── PlayMode/          # integração com GameObjects
├── Data/                  # assets gerados pelo menu
└── Scenes/                # cena gerada pelo menu
tools/CoreTests/           # projeto .NET que compila os MESMOS .cs para testes sem Editor
docs/                      # conceito, modelo, configuração, decisões e backlog
```

Fluxo de dependências: `Simulation` ← `Game` ← `Editor`. A simulação não conhece a interface; a interface só lê o estado e chama ações. Os ScriptableObjects nunca guardam progresso e nunca são alterados durante a partida.

---

## 5. Plano por marcos verificáveis

| Marco | Entrega | Critério de aceite verificável |
|---|---|---|
| **M1** (esta entrega) | Energia: geração solar, bateria, prioridade, consumo, Capi, previsão, fim de partida | 31 testes do núcleo aprovados; cena criada pelo menu; jogar, encerrar e reiniciar no Editor |
| M2 | Planejamento: alternativas de missão, equipamentos com massa/custo, validador | Testes de orçamento e massa; carga inválida bloqueada com motivo exibido |
| M3 | Consumíveis (O₂, água, alimento), tripulação, comunicação e radiação didática | Demanda não atendida registrada e com consequência; nenhum valor negativo |
| M4 | Eventos com semente (queda de geração, falha, radiação) | Mesma semente + mesmas ações = mesmo histórico (teste) |
| M5 | Lançamento/viagem simplificados e instalação de módulos | Transições com verificação de viabilidade; sem dinâmica orbital |
| M6 | Relatório final e comparação entre partidas | Relatório mostra efeitos em segurança, autonomia e pesquisa (sem nota única) |
| M7 | Teste com estudantes, build para computador; WebGL condicionado | Protocolo de avaliação; executável validado na plataforma declarada |
