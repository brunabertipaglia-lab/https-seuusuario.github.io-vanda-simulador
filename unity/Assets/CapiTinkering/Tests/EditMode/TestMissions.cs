using System.Collections.Generic;
using CapiTinkering.Simulation;

namespace CapiTinkering.Tests.EditMode
{
    /// <summary>Missões pequenas e explícitas para testes. Números escolhidos para contas exatas.</summary>
    internal static class TestMissions
    {
        public static MissionParameters Simple(
            double solarKw = 0.0,
            double[] illumination = null,
            double batteryCapacityKwh = 100.0,
            double initialFraction = 0.5,
            double minFraction = 0.0,
            double chargeEfficiency = 1.0,
            double turnHours = 10.0,
            int totalTurns = 4,
            double researchGoalHours = 0.0,
            int maxDeficitTurns = 2,
            double lifeSupportKw = 1.0,
            double researchKw = 2.0,
            int lifeSupportPriority = 0,
            int researchPriority = 2)
        {
            return new MissionParameters(
                turnDurationHours: turnHours,
                totalTurns: totalTurns,
                researchGoalHours: researchGoalHours,
                solarArrayKw: solarKw,
                batteryCapacityKwh: batteryCapacityKwh,
                batteryInitialFraction: initialFraction,
                batteryMinFraction: minFraction,
                chargeEfficiency: chargeEfficiency,
                illuminationProfile: illumination ?? new[] { 1.0 },
                maxConsecutiveLifeSupportDeficitTurns: maxDeficitTurns,
                equipment: new List<EquipmentSpec>
                {
                    // Pesquisa propositalmente listada ANTES do suporte de vida: a ordem da lista não pode decidir a prioridade.
                    new EquipmentSpec(EquipmentIds.Research, "equip.research", EquipmentRole.Research, researchKw, researchPriority, true, true),
                    new EquipmentSpec(EquipmentIds.LifeSupport, "equip.life_support", EquipmentRole.LifeSupport, lifeSupportKw, lifeSupportPriority, false, true)
                });
        }

        /// <summary>Executa a missão inteira aplicando decisões por turno: (turno, id, ligado).</summary>
        public static MissionState Play(MissionParameters p, IEnumerable<(int turn, string id, bool on)> decisions)
        {
            var byTurn = new Dictionary<int, List<(string id, bool on)>>();
            foreach (var d in decisions)
            {
                if (!byTurn.TryGetValue(d.turn, out var list)) byTurn[d.turn] = list = new List<(string, bool)>();
                list.Add((d.id, d.on));
            }

            var state = MissionState.CreateInitial(p);
            while (!state.IsFinished)
            {
                if (byTurn.TryGetValue(state.TurnsCompleted, out var actions))
                    foreach (var a in actions) state.SetEquipmentOn(a.id, a.on);
                SimulationEngine.AdvanceTurn(state);
            }
            return state;
        }
    }
}
