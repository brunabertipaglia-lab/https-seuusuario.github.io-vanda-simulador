using System;
using System.Linq;

namespace CapiTinkering.Simulation
{
    public enum CapiSeverity
    {
        Info,
        Warning,
        Critical,
        Success
    }

    /// <summary>Fala do Capi já resolvida no idioma atual, com severidade textual (não depende só de cor).</summary>
    public sealed class CapiMessage
    {
        public CapiSeverity Severity { get; }
        public string Key { get; }
        public string Text { get; }

        public string SeverityLabel => TextTable.Get(SeverityKey(Severity));

        public CapiMessage(CapiSeverity severity, string key, string text)
        {
            Severity = severity;
            Key = key;
            Text = text;
        }

        public static string SeverityKey(CapiSeverity severity)
        {
            switch (severity)
            {
                case CapiSeverity.Warning: return "severity.warning";
                case CapiSeverity.Critical: return "severity.critical";
                case CapiSeverity.Success: return "severity.success";
                default: return "severity.info";
            }
        }
    }

    /// <summary>
    /// Regras de feedback do Capi. Função pura do estado: explica consequências e faz perguntas,
    /// mas nunca altera equipamentos nem escolhe pelo jogador.
    /// Pistas graduais: longe do problema, uma pergunta; perto (≤ 2 turnos), uma dica explícita.
    /// </summary>
    public static class CapiAdvisor
    {
        public const int ProjectionHorizonTurns = 6;
        public const int HintThresholdTurns = 2;

        public static CapiMessage Evaluate(MissionState state)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            var p = state.Parameters;
            var last = state.History.Count > 0 ? state.History[state.History.Count - 1] : null;

            // 1. Fim de missão
            switch (state.Outcome)
            {
                case MissionOutcome.ObjectiveAchieved:
                    if (state.TotalLifeSupportDeficitTurns > 0)
                        return Msg(CapiSeverity.Warning, "capi.end.achieved_with_risk",
                            TextTable.Num(state.ResearchHours), state.TotalLifeSupportDeficitTurns);
                    return Msg(CapiSeverity.Success, "capi.end.achieved",
                        TextTable.Num(state.ResearchHours), TextTable.Num(p.ResearchGoalHours));
                case MissionOutcome.CompletedObjectiveNotMet:
                    return Msg(CapiSeverity.Warning, "capi.end.not_met",
                        TextTable.Num(state.ResearchHours), TextTable.Num(p.ResearchGoalHours));
                case MissionOutcome.CriticalFailure:
                    return Msg(CapiSeverity.Critical, "capi.end.critical", state.ConsecutiveLifeSupportDeficitTurns);
            }

            // 2. Déficit de suporte de vida acabou de acontecer
            if (last != null && last.LifeSupportDeficit)
            {
                var ls = last.Supplies.Where(s => s.Role == EquipmentRole.LifeSupport).ToList();
                double demand = ls.Sum(s => s.DemandKwh);
                double delivered = ls.Sum(s => s.DeliveredKwh);
                double pct = demand > 0 ? 100.0 * delivered / demand : 100.0;
                int remaining = p.MaxConsecutiveLifeSupportDeficitTurns - state.ConsecutiveLifeSupportDeficitTurns;
                return Msg(CapiSeverity.Critical, "capi.ls_deficit", TextTable.Num(pct, 0), remaining);
            }

            // 3. Projeção: déficit futuro mantendo a configuração atual
            var projection = SimulationEngine.Project(state, ProjectionHorizonTurns);
            var firstDeficit = projection.FirstOrDefault(r => r.LifeSupportDeficit);
            if (firstDeficit != null)
            {
                int turnsAhead = firstDeficit.TurnIndex - state.TurnsCompleted;
                if (turnsAhead < HintThresholdTurns)
                {
                    int sunReturns = NextLitTurn(p, firstDeficit.TurnIndex);
                    return Msg(CapiSeverity.Warning, "capi.projected_ls_deficit_hint", sunReturns + 1);
                }
                return Msg(CapiSeverity.Warning, "capi.projected_ls_deficit", firstDeficit.TurnIndex + 1);
            }

            // 4. Meta científica: inalcançável ou atrasada
            int turnsLeft = p.TotalTurns - state.TurnsCompleted;
            double needed = p.ResearchGoalHours - state.ResearchHours;
            if (needed > SimMath.EnergyEpsilonKwh && turnsLeft > 0)
            {
                double maxPossible = turnsLeft * p.TurnDurationHours;
                if (needed > maxPossible + SimMath.EnergyEpsilonKwh)
                    return Msg(CapiSeverity.Warning, "capi.research_unreachable", TextTable.Num(needed - maxPossible));

                double perTurn = needed / turnsLeft;
                if (perTurn > 0.75 * p.TurnDurationHours && state.TurnsCompleted > 0)
                    return Msg(CapiSeverity.Info, "capi.research_behind",
                        TextTable.Num(state.ResearchHours), TextTable.Num(p.ResearchGoalHours), turnsLeft,
                        TextTable.Num(perTurn), TextTable.Num(p.TurnDurationHours));
            }

            // 5. Energia desperdiçada: só sugere religar sistemas se isso não criar déficit adiante
            if (last != null && last.CurtailedKwh > 0.5)
            {
                return AllSwitchableOnIsSafe(state)
                    ? Msg(CapiSeverity.Info, "capi.curtailed", TextTable.Num(last.CurtailedKwh))
                    : Msg(CapiSeverity.Info, "capi.reserve_for_shadow", TextTable.Num(last.CurtailedKwh));
            }

            // 6. Introdução de conceito / balanço do turno
            if (last == null)
            {
                double exampleKw = p.SolarArrayKw;
                return Msg(CapiSeverity.Info, "capi.intro",
                    TextTable.Num(p.TurnDurationHours, 0), TextTable.Num(exampleKw),
                    TextTable.Num(SimMath.EnergyKwh(exampleKw, p.TurnDurationHours)));
            }

            return last.BatteryNetKwh >= 0
                ? Msg(CapiSeverity.Info, "capi.balance_positive",
                    TextTable.Num(last.GenerationKwh), TextTable.Num(last.DeliveredKwh), TextTable.Num(last.BatteryNetKwh))
                : Msg(CapiSeverity.Info, "capi.balance_negative",
                    TextTable.Num(last.GenerationKwh), TextTable.Num(last.DeliveredKwh), TextTable.Num(-last.BatteryNetKwh));
        }

        /// <summary>Verifica, numa cópia, se religar todos os sistemas desligáveis manteria o suporte de vida.</summary>
        private static bool AllSwitchableOnIsSafe(MissionState state)
        {
            var copy = state.Clone();
            foreach (var e in copy.Equipment)
                if (!e.IsOn) copy.SetEquipmentOn(e.Spec.Id, true);
            return !SimulationEngine.Project(copy, ProjectionHorizonTurns).Any(r => r.LifeSupportDeficit);
        }

        /// <summary>Primeiro turno (índice) a partir de <paramref name="fromTurn"/> com iluminação > 0.</summary>
        private static int NextLitTurn(MissionParameters p, int fromTurn)
        {
            for (int t = fromTurn; t < fromTurn + p.IlluminationProfile.Count; t++)
                if (p.IlluminationAt(t) > 0.0) return t;
            return fromTurn;
        }

        private static CapiMessage Msg(CapiSeverity severity, string key, params object[] args) =>
            new CapiMessage(severity, key, TextTable.Get(key, args));
    }
}
