using System;
using System.Collections.Generic;
using System.Linq;

namespace CapiTinkering.Simulation
{
    /// <summary>
    /// Regras da simulação. Sem estado próprio: tudo o que muda fica em <see cref="MissionState"/>.
    ///
    /// Ordem de atualização de um turno:
    ///  1. Geração solar  = potência nominal × fator de iluminação × duração do turno.
    ///  2. Demanda        = potência de cada equipamento ligado × duração do turno.
    ///  3. Atendimento por prioridade (0 primeiro), usando primeiro a geração do turno
    ///     e depois a bateria, sem descer abaixo da reserva mínima.
    ///  4. Excedente solar carrega a bateria (com perda de carga); o que não cabe é desperdiçado.
    ///  5. Consequências: pesquisa proporcional à energia recebida; déficit de suporte de vida.
    ///  6. Condições de término.
    /// </summary>
    public static class SimulationEngine
    {
        /// <summary>
        /// Calcula o próximo turno SEM alterar o estado. Usado para previsão na interface
        /// e internamente por <see cref="AdvanceTurn"/>.
        /// </summary>
        public static TurnReport ComputeTurn(MissionState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.IsFinished) throw new InvalidOperationException("A missão já terminou; reinicie para jogar novamente.");

            var p = state.Parameters;
            int turnIndex = state.TurnsCompleted;
            double hours = p.TurnDurationHours;
            double illumination = p.IlluminationAt(turnIndex);

            // 1. Geração
            double solarKw = p.SolarArrayKw * illumination;
            double generationKwh = SimMath.EnergyKwh(solarKw, hours);

            // 2–3. Demanda e atendimento por prioridade (empate: ordem da lista, estável)
            double batteryStart = state.BatteryStoredKwh;
            double solarLeft = generationKwh;
            double batteryAvailable = Math.Max(0.0, batteryStart - p.BatteryMinKwh);
            double batteryUsed = 0.0;

            var ordered = state.Equipment
                .Select((e, index) => new { e, index })
                .OrderBy(x => x.e.Spec.Priority)
                .ThenBy(x => x.index)
                .Select(x => x.e);

            var suppliesById = new Dictionary<string, EquipmentSupply>();
            foreach (var equipment in ordered)
            {
                double demand = equipment.IsOn ? SimMath.EnergyKwh(equipment.Spec.PowerKw, hours) : 0.0;

                double fromSolar = Math.Min(demand, solarLeft);
                solarLeft -= fromSolar;

                double fromBattery = Math.Min(demand - fromSolar, batteryAvailable - batteryUsed);
                fromBattery = Math.Max(0.0, fromBattery);
                batteryUsed += fromBattery;

                suppliesById[equipment.Spec.Id] = new EquipmentSupply(
                    equipment.Spec.Id, equipment.Spec.Role, equipment.IsOn, demand, fromSolar + fromBattery);
            }

            // 4. Carga da bateria com o excedente
            double directUse = generationKwh - solarLeft;
            double batteryAfterDischarge = batteryStart - batteryUsed;
            double room = Math.Max(0.0, p.BatteryCapacityKwh - batteryAfterDischarge);
            double chargeInput = Math.Min(solarLeft, room / p.ChargeEfficiency);
            double stored = chargeInput * p.ChargeEfficiency;
            double curtailed = solarLeft - chargeInput;
            double batteryEnd = Math.Min(p.BatteryCapacityKwh, batteryAfterDischarge + stored);

            // Relatório na ordem original dos equipamentos (estável para a interface)
            var supplies = state.Equipment.Select(e => suppliesById[e.Spec.Id]).ToList();

            // 5. Consequências
            double lifeSupportUnmet = supplies.Where(s => s.Role == EquipmentRole.LifeSupport).Sum(s => s.UnmetKwh);
            double researchHours = supplies
                .Where(s => s.Role == EquipmentRole.Research && s.WasOn)
                .Select(s => hours * s.SuppliedFraction)
                .DefaultIfEmpty(0.0)
                .Max(); // várias unidades de pesquisa = mesma frente de trabalho no protótipo

            var report = new TurnReport
            {
                TurnIndex = turnIndex,
                DurationHours = hours,
                IlluminationFactor = illumination,
                SolarPowerKw = solarKw,
                GenerationKwh = generationKwh,
                DemandKwh = supplies.Sum(s => s.DemandKwh),
                DeliveredKwh = supplies.Sum(s => s.DeliveredKwh),
                UnmetKwh = supplies.Sum(s => s.UnmetKwh),
                DirectSolarUseKwh = SimMath.CleanNonNegative(directUse, "directUse"),
                BatteryDischargeKwh = SimMath.CleanNonNegative(batteryUsed, "batteryUsed"),
                BatteryChargeInputKwh = SimMath.CleanNonNegative(chargeInput, "chargeInput"),
                ChargeLossKwh = SimMath.CleanNonNegative(chargeInput - stored, "chargeLoss"),
                CurtailedKwh = SimMath.CleanNonNegative(curtailed, "curtailed"),
                BatteryStartKwh = batteryStart,
                BatteryEndKwh = SimMath.CleanNonNegative(batteryEnd, "batteryEnd"),
                LifeSupportUnmetKwh = lifeSupportUnmet,
                ResearchHoursGained = researchHours,
                Supplies = supplies.AsReadOnly()
            };

            // 6. Término (calculado sobre uma projeção dos contadores, sem tocar no estado)
            int consecutive = report.LifeSupportDeficit ? state.ConsecutiveLifeSupportDeficitTurns + 1 : 0;
            report.OutcomeAfter = EvaluateOutcome(
                p, turnIndex + 1, state.ResearchHours + researchHours, consecutive);

            return report;
        }

        /// <summary>Única rotina que avança a partida: calcula e aplica um turno.</summary>
        public static TurnReport AdvanceTurn(MissionState state)
        {
            var report = ComputeTurn(state);

            state.BatteryStoredKwh = report.BatteryEndKwh;
            state.ResearchHours += report.ResearchHoursGained;
            if (report.LifeSupportDeficit)
            {
                state.ConsecutiveLifeSupportDeficitTurns++;
                state.TotalLifeSupportDeficitTurns++;
            }
            else
            {
                state.ConsecutiveLifeSupportDeficitTurns = 0;
            }
            state.TurnsCompleted = report.TurnIndex + 1;
            state.Outcome = report.OutcomeAfter;
            state.AddHistory(report);

            return report;
        }

        /// <summary>
        /// Projeta até <paramref name="maxTurns"/> turnos mantendo a configuração atual dos equipamentos.
        /// Não altera <paramref name="state"/>. Para no fim da missão.
        /// </summary>
        public static IReadOnlyList<TurnReport> Project(MissionState state, int maxTurns)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var reports = new List<TurnReport>();
            var copy = state.Clone();
            for (int i = 0; i < maxTurns && !copy.IsFinished; i++)
                reports.Add(AdvanceTurn(copy));
            return reports;
        }

        public static MissionOutcome EvaluateOutcome(
            MissionParameters p, int turnsCompleted, double researchHours, int consecutiveDeficitTurns)
        {
            if (consecutiveDeficitTurns >= p.MaxConsecutiveLifeSupportDeficitTurns)
                return MissionOutcome.CriticalFailure;
            if (turnsCompleted >= p.TotalTurns)
                return researchHours + SimMath.EnergyEpsilonKwh >= p.ResearchGoalHours
                    ? MissionOutcome.ObjectiveAchieved
                    : MissionOutcome.CompletedObjectiveNotMet;
            return MissionOutcome.InProgress;
        }
    }
}
