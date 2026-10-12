# Etapa 1 — Configuração no Unity e verificação

## Resultado esperado

Uma cena isométrica com a base lunar em formas simples, um painel de energia (iluminação, geração, bateria, consumo por sistema e previsão), botões para ligar e desligar a comunicação e a pesquisa, o botão **Avançar turno**, o botão **Reiniciar missão** e o painel do Capi com mensagens de severidade textual.

## Versão do Unity (hipótese declarada)

- **Alvo:** Unity **6.3 LTS** (série 6000.3), indicada como a LTS mais recente na página de lançamentos da Unity em 12/10/2026. O número exato do patch não foi verificado: usar o mais recente oferecido pelo Unity Hub.
- **Compatível também com Unity 6.0 LTS**, cujo suporte padrão termina em outubro de 2026. Por isso, ela não é recomendada para começar.
- **Não use Unity 2022 ou anterior:** o construtor de cena usa `TextMeshProUGUI.textWrappingMode`, disponível no TextMesh Pro integrado ao uGUI 2.0 do Unity 6.
- **Pacotes:** apenas os que já vêm no modelo (uGUI com TextMesh Pro e Test Framework). Nenhuma dependência externa.

## Passo a passo

1. **Criar o projeto.** No Unity Hub: *New project* → modelo **Universal 3D** (URP) → nome `CapiTinkering` → pasta fora do repositório.
2. **Trazer o código.** Feche o Unity e copie a pasta `unity/Assets/CapiTinkering` deste repositório para `Assets/` do projeto. Para versionar o projeto inteiro, copie também `Packages/` e `ProjectSettings/` do projeto criado para `unity/` e faça *commit* (o `.gitignore` já exclui `Library/`, `Temp/` e similares).
3. **Abrir o projeto** e aguardar a compilação. O Console deve ficar sem erros.
4. **Importar o TextMesh Pro:** *Window ▸ TextMeshPro ▸ Import TMP Essential Resources*.
5. **Gerar dados e cena:** menu **Capi Tinkering ▸ Etapa 1 ▸ Criar dados e cena**. O menu cria:
   - `Data/Equipment_LifeSupport.asset`, `Equipment_Communication.asset`, `Equipment_Research.asset`;
   - `Data/Mission_Stage1_Energy.asset`;
   - `Data/Materials/*.mat`;
   - `Scenes/Etapa1_Energia.unity`, já incluída em *Build Settings*.
6. **Pressionar Play.**

### Ligação manual de referências (se não usar o menu)

| Objeto | Componente | Campo → referência |
|---|---|---|
| `MissionController` | `MissionController` | `Mission` → `Mission_Stage1_Energy` |
| `MissionController` | `PowerSceneView` | `Controller`, `Sun Light`, `Battery Fill` (pivô na base), `Research Module`, `Communication Antenna` |
| `Canvas` | `MissionHUD` | `Controller`; textos TMP (`Turn`, `Objective`, `Energy`, `Flow`, `Systems`, `Forecast`, `Outcome`, `Capi Severity`, `Capi Message`); `Capi Panel` (Image); `Battery Fill` (Image com *Image Type = Filled*, *Horizontal*); botões com rótulos; `Equipment Toggles`: `communication` e `research` |
| `EventSystem` | `InputSystemUIInputModule` (Input System) ou `StandaloneInputModule` | — |

O retrato do Capi (`Art/Capi_Portrait.png`) e o logo (`Art/CapyTinkering_Logo.png`) são aplicados automaticamente pelo menu. Na montagem manual, arraste-os para os componentes `Image` de `Canvas/CapiPanel/CapiPortrait` e de `Canvas/MissionPanel/TitleRow/Logo`; o `CapiArtImporter` já os importa como Sprite. Paleta, contraste e limitações da arte estão em `05_IDENTIDADE_VISUAL.md`.

## Como verificar

| Verificação | Procedimento | Esperado |
|---|---|---|
| Testes Edit Mode | *Window ▸ General ▸ Test Runner ▸ EditMode ▸ Run All* | 31 testes aprovados (os mesmos executados fora da Unity) |
| Testes Play Mode | *Test Runner ▸ PlayMode ▸ Run All* | 3 testes aprovados |
| Estratégia inadequada | Play; avance sem desligar nada | Turno 2: o Capi alerta com base na projeção. Turno 6: a pergunta vira dica. Após o turno 7: suporte de vida "SEM ENERGIA". Após o turno 8: "Missão encerrada: condição crítica" |
| Estratégia válida | Reinicie; com o HUD em "Turno 5", desligue a pesquisa e a comunicação antes de avançar; religue-as em "Turno 9" | "Objetivo científico atingido", com 142,5 h de pesquisa |
| Concordância | Compare a previsão com o fluxo exibido após avançar | Os valores coincidem |
| Reinício | Clique em *Reiniciar* no meio da partida | Turno 1, bateria 30,0 kWh, todos os sistemas ligados |
| Idioma | Clique em *Idioma* | Todos os textos mudam para inglês |
| Identidade | Observe o painel do Capi em atenção e o botão "Avançar turno" | Retrato do Capi visível; texto azul-marinho sobre laranja, legível |

Fora da Unity (sem Editor): `dotnet test tools/CoreTests` executa os testes Edit Mode do núcleo.

## Validação realizada nesta entrega

| Item | Situação |
|---|---|
| Núcleo `Simulation` compilado e 31 testes NUnit executados (.NET 8) | **Executado: 31/31 aprovados** |
| Scripts `Game` e testes Play Mode compilados contra *stubs* mínimos das APIs Unity (checagem de sintaxe e tipos) | **Executado: compilou sem erros.** Não substitui a compilação real |
| Construtor de cena (`Editor`) | **Não compilado:** depende de APIs do Editor |
| Compilação no Unity, execução da cena e testes Play Mode | **Pendente:** não há Editor Unity neste ambiente |
| Build executável para computador | **Pendente** |
