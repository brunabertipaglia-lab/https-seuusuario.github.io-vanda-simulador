using System.Linq;
using CapiTinkering.Simulation;
using NUnit.Framework;

namespace CapiTinkering.Tests.EditMode
{
    /// <summary>Estratégias da missão padrão da etapa 1 e invariantes de partida.</summary>
    public class MissionFlowTests
    {
        private static readonly (int, string, bool)[] KeepEverythingOn = new (int, string, bool)[0];

        /// <summary>Desliga pesquisa e comunicação antes da sombra e religa quando o Sol volta.</summary>
        private static readonly (int, string, bool)[] PlanForShadow =
        {
            (4, EquipmentIds.Research, false),
            (4, EquipmentIds.Communication, false),
            (8, EquipmentIds.Research, true),
            (8, EquipmentIds.Communication, true)
        };

        /// <summary>Protege demais: deixa a pesquisa desligada metade da missão (e a comunicação durante a sombra).</summary>
        private static readonly (int, string, bool)[] OverCautious =
        {
            (0, EquipmentIds.Research, false),
            (4, EquipmentIds.Communication, false),
            (8, EquipmentIds.Research, true),
            (8, EquipmentIds.Communication, true)
        };

        /// <summary>Desliga só a pesquisa durante a escuridão: sobrevive, mas com um turno de déficit (consequência explicável).</summary>
        private static readonly (int, string, bool)[] ResearchOffOnlyInDarkness =
        {
            (5, EquipmentIds.Research, false),
            (8, EquipmentIds.Research, true)
        };

        /// <summary>Reage tarde: desliga tudo só quando a bateria já está na reserva.</summary>
        private static readonly (int, string, bool)[] ReactsTooLate =
        {
            (6, EquipmentIds.Research, false),
            (6, EquipmentIds.Communication, false)
        };

        [Test]
        public void KeepingEverythingOn_EndsInCriticalFailure()
        {
            var state = TestMissions.Play(MissionParameters.CreateStage1Default(), KeepEverythingOn);
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.CriticalFailure));
            Assert.That(state.History.Any(r => r.LifeSupportDeficit), Is.True);
        }

        [Test]
        public void PlanningForTheShadow_AchievesTheObjective()
        {
            var state = TestMissions.Play(MissionParameters.CreateStage1Default(), PlanForShadow);
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.ObjectiveAchieved));
            Assert.That(state.TotalLifeSupportDeficitTurns, Is.EqualTo(0));
        }

        [Test]
        public void OverCautiousStrategy_IsSafeButMissesTheObjective()
        {
            var state = TestMissions.Play(MissionParameters.CreateStage1Default(), OverCautious);
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.CompletedObjectiveNotMet));
            Assert.That(state.TotalLifeSupportDeficitTurns, Is.EqualTo(0));
        }

        [Test]
        public void ReactingTooLate_EndsInCriticalFailure()
        {
            var state = TestMissions.Play(MissionParameters.CreateStage1Default(), ReactsTooLate);
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.CriticalFailure));
        }

        [Test]
        public void ResearchOffOnlyInDarkness_SurvivesWithAnExplainableDeficit()
        {
            var state = TestMissions.Play(MissionParameters.CreateStage1Default(), ResearchOffOnlyInDarkness);
            Assert.That(state.Outcome, Is.Not.EqualTo(MissionOutcome.CriticalFailure));
            Assert.That(state.TotalLifeSupportDeficitTurns, Is.EqualTo(1));
            Assert.That(CapiAdvisor.Evaluate(state).Key, Is.EqualTo("capi.end.achieved_with_risk"),
                "atingir a meta com risco à tripulação não pode ser apresentado como sucesso pleno");
        }

        [Test]
        public void SameInitialStateAndActions_ReproduceTheSameResult()
        {
            var p = MissionParameters.CreateStage1Default();
            var a = TestMissions.Play(p, PlanForShadow);
            var b = TestMissions.Play(p, PlanForShadow);

            Assert.That(a.History.Count, Is.EqualTo(b.History.Count));
            for (int i = 0; i < a.History.Count; i++)
            {
                Assert.That(a.History[i].BatteryEndKwh, Is.EqualTo(b.History[i].BatteryEndKwh));
                Assert.That(a.History[i].UnmetKwh, Is.EqualTo(b.History[i].UnmetKwh));
                Assert.That(a.History[i].ResearchHoursGained, Is.EqualTo(b.History[i].ResearchHoursGained));
            }
            Assert.That(a.Outcome, Is.EqualTo(b.Outcome));
        }

        [Test]
        public void Restart_CreatesStateWithoutResidue()
        {
            var p = MissionParameters.CreateStage1Default();
            var session = new MissionSession(p);
            session.SetEquipmentOn(EquipmentIds.Research, false);
            session.AdvanceTurn();
            session.AdvanceTurn();

            session.Restart();
            var s = session.State;

            Assert.That(s.TurnsCompleted, Is.EqualTo(0));
            Assert.That(s.BatteryStoredKwh, Is.EqualTo(p.BatteryInitialKwh));
            Assert.That(s.ResearchHours, Is.EqualTo(0.0));
            Assert.That(s.ConsecutiveLifeSupportDeficitTurns, Is.EqualTo(0));
            Assert.That(s.TotalLifeSupportDeficitTurns, Is.EqualTo(0));
            Assert.That(s.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(s.History, Is.Empty);
            Assert.That(s.Decisions, Is.Empty);
            Assert.That(s.Equipment.All(e => e.IsOn == e.Spec.StartsOn), Is.True);
        }

        [Test]
        public void Decisions_AreRecordedWithTurn()
        {
            var state = MissionState.CreateInitial(MissionParameters.CreateStage1Default());
            SimulationEngine.AdvanceTurn(state);
            Assert.That(state.SetEquipmentOn(EquipmentIds.Research, false), Is.True);
            Assert.That(state.SetEquipmentOn(EquipmentIds.Research, false), Is.False, "sem mudança não gera decisão");

            Assert.That(state.Decisions.Count, Is.EqualTo(1));
            Assert.That(state.Decisions[0].Turn, Is.EqualTo(1));
            Assert.That(state.Decisions[0].EquipmentId, Is.EqualTo(EquipmentIds.Research));
            Assert.That(state.Decisions[0].TurnedOn, Is.False);
        }

        [Test]
        public void Capi_WarnsBeforeTheFirstDeficit_WhenEverythingStaysOn()
        {
            var state = MissionState.CreateInitial(MissionParameters.CreateStage1Default());
            bool warnedBeforeDeficit = false;
            while (!state.IsFinished)
            {
                var message = CapiAdvisor.Evaluate(state);
                if (message.Key.StartsWith("capi.projected_ls_deficit")) warnedBeforeDeficit = true;
                var report = SimulationEngine.AdvanceTurn(state);
                if (report.LifeSupportDeficit) break;
            }
            Assert.That(warnedBeforeDeficit, Is.True);
        }

        [Test]
        public void Capi_DoesNotChangeTheState()
        {
            var state = MissionState.CreateInitial(MissionParameters.CreateStage1Default());
            SimulationEngine.AdvanceTurn(state);
            double battery = state.BatteryStoredKwh;
            int turns = state.TurnsCompleted;

            CapiAdvisor.Evaluate(state);

            Assert.That(state.BatteryStoredKwh, Is.EqualTo(battery));
            Assert.That(state.TurnsCompleted, Is.EqualTo(turns));
            Assert.That(state.Decisions, Is.Empty);
        }

        [Test]
        public void EveryTextKey_ExistsInBothLanguages()
        {
            var missingInEnglish = TextTable.PortugueseKeys.Where(k => !TextTable.Has(k, GameLanguage.English)).ToList();
            var missingInPortuguese = TextTable.EnglishKeys.Where(k => !TextTable.Has(k, GameLanguage.PortugueseBrazil)).ToList();
            Assert.That(missingInEnglish, Is.Empty);
            Assert.That(missingInPortuguese, Is.Empty);
        }
    }
}
