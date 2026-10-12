# Modelo de simulação — etapa 1 (energia)

Versão 0.1 · 12/10/2026 · implementação: `unity/Assets/CapiTinkering/Scripts/Simulation/`

## 1. Variáveis e unidades

| Símbolo | Variável | Unidade | Tipo |
|---|---|---|---|
| Δt | Duração do turno | h | parâmetro |
| N | Turnos da missão | turnos | parâmetro |
| P_sol | Potência nominal dos painéis com iluminação plena | kW | parâmetro (capacidade instalada) |
| f_t | Fator de iluminação no turno t | 0–1 | parâmetro por turno |
| C | Capacidade da bateria | kWh | parâmetro (capacidade) |
| E_t | Energia armazenada no início do turno t | kWh | **estado** (quantidade) |
| r_min | Reserva mínima protegida | fração de C | parâmetro |
| η | Fração do excedente que fica armazenada | 0–1 | parâmetro |
| P_i | Potência do equipamento i quando ligado | kW | parâmetro |
| p_i | Prioridade do equipamento i (0 primeiro) | inteiro | parâmetro |
| H | Horas de pesquisa acumuladas | h | **estado** |
| D | Turnos consecutivos com déficit no suporte de vida | turnos | **estado** |

Potência (kW) é taxa; energia (kWh) é quantidade. A única conversão é **energia = potência × duração do turno**, implementada em `SimMath.EnergyKwh`.

## 2. Ordem de atualização de um turno

1. **Geração:** G = P_sol × f_t × Δt.
2. **Demanda:** d_i = P_i × Δt se ligado; 0 se desligado (sem consumo em espera — simplificação).
3. **Atendimento por prioridade:** equipamentos ordenados por p_i (empate: ordem da lista). Cada um recebe primeiro da geração do turno e depois da bateria, limitada a E_t − r_min·C. Registra-se entregue e **não atendido** (u_i = d_i − entregue_i).
4. **Carga:** excedente S = G − uso direto. Entrada na bateria = min(S, (C − E')/η); armazenado = entrada × η; o restante é **desperdiçado** e registrado.
5. **Consequências:**
   - Pesquisa: H += Δt × (entregue/demanda) do módulo de pesquisa (proporcional; simplificação).
   - Suporte de vida: se u_SV > 0, D += 1; caso contrário, D = 0.
6. **Término:** D ≥ D_max → encerramento crítico; senão, se t = N → objetivo atingido (H ≥ meta) ou missão concluída sem objetivo.

### Balanços garantidos por teste

- G = uso direto + entrada na bateria + desperdício
- E_{t+1} = E_t − descarga + entrada × η
- demanda = entregue + não atendido
- r_min·C ≤ E ≤ C; nenhum valor negativo, NaN ou infinito.

**Demanda não atendida nunca é escondida:** não há subtração seguida de corte em zero. A falta é calculada, registrada no `TurnReport` e gera consequência (contador de déficit, perda de pesquisa).

**Projeção e previsão:** `ComputeTurn` calcula o próximo turno sem alterar o estado; `Project` simula até 6 turnos em uma cópia. A previsão é determinística porque a etapa 1 não tem eventos aleatórios; a interface informa isso.

## 3. Classificação dos dados

### 3.1 Dados científicos verificados

| Dado | Valor | Fonte | Contexto | Acesso | Uso no jogo |
|---|---|---|---|---|---|
| Período sinódico lunar (ciclo dia–noite em relação ao Sol) | 29,53 dias | NASA NSSDC, *Moon Fact Sheet* | Fora dos polos, ~14,8 dias de luz e ~14,8 de escuridão | 12/10/2026, **por cópia secundária**; a página oficial não pôde ser aberta deste ambiente | **Ainda não usado numericamente.** Motiva a escolha de um local próximo ao polo com sombras curtas. **Pendente:** confirmar na página oficial antes de citar no jogo. |

Nenhum outro número do jogo é atribuído à NASA.

### 3.2 Simplificações educacionais

| Simplificação | Justificativa | Risco de interpretação |
|---|---|---|
| Iluminação por turno (fator 0–1) em vez de geometria solar | Torna visível a relação geração × tempo | O perfil não representa um local real |
| Eficiência de carga única; descarga sem perdas | Uma perda basta para ensinar que armazenar custa energia | Subestima perdas reais |
| Sem limite de potência de descarga | Reduz variáveis na etapa 1 | Backlog M3 |
| Consumo zero quando desligado | Clareza do trade-off | Sistemas reais têm consumo em espera |
| Pesquisa proporcional à energia | Liga energia e objetivo de forma direta | Instrumentos reais podem ter limiar mínimo |
| Déficit de suporte de vida como contador | Consequência clara e reversível | Não é modelo fisiológico nem médico |

### 3.3 Parâmetros fictícios de balanceamento (etapa 1)

| Parâmetro | Valor | Motivo |
|---|---|---|
| Δt | 12 h | 16 turnos ≈ 8 dias simulados em poucos minutos |
| N | 16 | Duração da sessão |
| Meta de pesquisa | 120 h (de 192 h possíveis) | Permite desligar a pesquisa por até ~6 turnos |
| P_sol | 5 kW | Excedente pequeno em turnos iluminados |
| C / carga inicial / reserva | 50 kWh / 60% / 10% | Bateria insuficiente para a sombra com tudo ligado |
| η | 0,9 | Perda perceptível, mas não dominante |
| Perfil de iluminação | 0,9 0,9 0,9 0,8 0,5 **0 0 0** 0,5 0,8 0,9 0,9 0,9 0,9 0,8 0,5 | Uma sombra de 36 h |
| Suporte de vida / comunicação / pesquisa | 1,0 / 0,5 / 2,0 kW; prioridades 0 / 1 / 2 | Pesquisa é o maior consumidor |
| D_max | 2 turnos | Um turno de déficit é alerta; dois encerram |

## 4. Estratégias verificadas (executadas em 12/10/2026)

| Estratégia | Resultado | Pesquisa | Turnos com déficit |
|---|---|---|---|
| Manter tudo ligado | Encerramento crítico no turno 8 | 67,5 h | 2 |
| Reagir tarde (desligar no turno 7) | Encerramento crítico | 67,5 h | 2 |
| Desligar pesquisa e comunicação do turno 5 ao 8 | **Objetivo atingido** | 142,5 h | 0 |
| Desligar só a pesquisa no escuro (turnos 6–8) | Objetivo atingido **com risco** (o Capi destaca) | 150,0 h | 1 |
| Cauteloso demais (pesquisa desligada até o turno 8) | Concluída **sem** objetivo | 94,5 h | 0 |

Turnos numerados a partir de 1, como na interface. Os casos estão cobertos em `MissionFlowTests`.

**Observação crítica:** a estratégia "só a pesquisa no escuro" obtém mais pesquisa *aceitando* um turno de risco à tripulação. No relatório do M6, segurança e pesquisa devem aparecer como dimensões separadas, para que essa troca seja discutida e não escondida por uma pontuação única.
