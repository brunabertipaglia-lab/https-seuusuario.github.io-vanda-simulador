# Registro de decisões e backlog

## Decisões

| # | Data | Decisão | Motivo | Revisável quando |
|---|---|---|---|---|
| D1 | 12/10/2026 | Núcleo em C# puro (`noEngineReferences`) | Permite testar as regras sem Unity, inclusive em CI | — |
| D2 | 12/10/2026 | `double` na simulação; `float` nos ScriptableObjects, arredondado a 6 casas | O Inspector não oferece `Range` e `Min` para `double`; o arredondamento evita 0,9f → 0,8999… | — |
| D3 | 12/10/2026 | Prioridade com "uso solar primeiro, depois bateria", atendimento parcial permitido | Regra simples e explicável; o déficit é registrado | Ao modelar limite de descarga (M3) |
| D4 | 12/10/2026 | Previsão por `ComputeTurn`, sem mutação, em vez de clonar o estado | Uma única rotina de cálculo garante que previsão e resultado coincidam | — |
| D5 | 12/10/2026 | Textos em tabela própria pt-BR/en, com teste de paridade | Volume pequeno; evita dependência do pacote Localization agora | Quando houver mais de ~150 chaves ou tradutores externos |
| D6 | 12/10/2026 | Capi como função pura do estado (`CapiAdvisor`) | Testável; garante que o Capi não age pelo jogador | — |
| D7 | 12/10/2026 | Pistas graduais por proximidade: pergunta (≥ 2 turnos antes) → dica (< 2 turnos) | Estimula o raciocínio antes de entregar a regra | Após testes com estudantes |
| D8 | 12/10/2026 | O Capi não sugere religar sistemas se isso criar déficit adiante | Evitar um conselho que leve ao erro (encontrado no balanceamento) | — |
| D9 | 12/10/2026 | Construtor de cena por menu do Editor | Reduz erros de ligação manual na equipe | — |
| D10 | 12/10/2026 | Unity 6.3 LTS como alvo | LTS mais recente; 6.0 LTS sai do suporte padrão em outubro de 2026 | Se a equipe já tiver outra versão instalada |
| D11 | 12/10/2026 | Etapa 1 sem aleatoriedade | Os eventos com semente entram no M4 com PRNG próprio (ex.: xorshift), pois `System.Random` não garante a mesma sequência entre runtimes | M4 |

## Backlog priorizado

| Prioridade | Item | Marco |
|---|---|---|
| P0 | Validar no Editor: compilar, rodar a cena e os testes Play Mode; corrigir o que surgir | M1 |
| P0 | Confirmar os enunciados oficiais (previstos para 28/10/2026) e atualizar a matriz | — |
| P1 | Equipamentos com massa e custo; validador de orçamento e massa com motivo exibido | M2 |
| P1 | Duas alternativas de missão (ex.: mais baterias × mais painéis) | M2 |
| P1 | O₂, água e alimento com consumo da tripulação e demanda não atendida | M3 |
| P1 | Comunicação com janelas de disponibilidade; dados de pesquisa só contam após o envio | M3 |
| P1 | Índice didático de radiação e blindagem (massa × proteção) | M3 |
| P1 | PRNG com semente e três eventos; teste de reprodução | M4 |
| P2 | Relatório final com dimensões separadas: segurança, autonomia e pesquisa | M6 |
| P2 | Limite de potência de descarga da bateria | M3 |
| P2 | Persistência local versionada (preferências e histórico) | após M6 |
| P2 | Legendas para falas com voz, se houver áudio | M6 |
| P3 | WebGL: testar cultura `pt-BR` (há *fallback* para a cultura invariante) e desempenho | M7 |
| P3 | Protocolo de avaliação de aprendizagem com estudantes (pré e pós, observação) | M7 |
