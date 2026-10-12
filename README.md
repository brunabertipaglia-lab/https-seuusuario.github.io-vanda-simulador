# Capi Tinkering — Missão Lunar

Jogo educativo da equipe **CAPI TINKERING** para o NASA Space Apps Challenge 2026 (desafio de referência: *Space Mission Design Game*). O jogador planeja, monta e opera uma missão lunar em turnos, guiado pelo Capi, uma capivara astronauta. O objetivo é que cada escolha de engenharia mostre seus benefícios, custos e restrições.

**Estado atual:** Marco 1, subsistema de energia. Inclui a simulação testada, a camada Unity e um construtor automático de cena.

| Documento | Conteúdo |
|---|---|
| [docs/01_CONCEITO_ESCOPO_PLANO.md](docs/01_CONCEITO_ESCOPO_PLANO.md) | Conceito, matriz de requisitos, escopo do MVP, arquitetura e marcos |
| [docs/02_MODELO_SIMULACAO.md](docs/02_MODELO_SIMULACAO.md) | Variáveis, unidades, equações, ordem de atualização, classificação dos dados e estratégias verificadas |
| [docs/03_ETAPA1_CONFIGURACAO_UNITY.md](docs/03_ETAPA1_CONFIGURACAO_UNITY.md) | Como montar, executar e verificar a etapa 1 no Unity |
| [docs/04_DECISOES_E_BACKLOG.md](docs/04_DECISOES_E_BACKLOG.md) | Registro de decisões e backlog priorizado |

## Início rápido

```bash
# Testes do núcleo sem Unity (.NET 8)
dotnet test tools/CoreTests
```

No Unity 6.3 LTS:

1. Copie `unity/Assets/CapiTinkering` para `Assets/` de um projeto criado com o modelo Universal 3D.
2. Importe os recursos essenciais do TMP.
3. Execute o menu **Capi Tinkering ▸ Etapa 1 ▸ Criar dados e cena**.
4. Pressione **Play**.

Os detalhes estão no documento 03.
