using System;
using System.Linq;
using CapiTinkering.Simulation;
using NUnit.Framework;

namespace CapiTinkering.Tests.EditMode
{
    public class EnergyShortageTests
    {
        private const double Tol = 1e-9;

        [Test]
        public void EmptyBatteryAtNight_RecordsAllDemandAsUnmet()
        {
            var p = TestMissions.Simple(initialFraction: 0.0);
            var state = MissionState.CreateInitial(p);
            var report = SimulationEngine.AdvanceTurn(state);

            Assert.That(report.DemandKwh, Is.EqualTo(30.0).Within(Tol));
            Assert.That(report.DeliveredKwh, Is.EqualTo(0.0).Within(Tol));
            Assert.That(report.UnmetKwh, Is.EqualTo(30.0).Within(Tol));
            Assert.That(report.LifeSupportUnmetKwh, Is.EqualTo(10.0).Within(Tol));
            Assert.That(state.BatteryStoredKwh, Is.EqualTo(0.0).Within(Tol));
        }

        [Test]
        public void Shortage_LifeSupportIsServedBeforeResearch_RegardlessOfListOrder()
        {
            // 15 kWh disponíveis; suporte de vida pede 10, pesquisa pede 20
            var p = TestMissions.Simple(batteryCapacityKwh: 100.0, initialFraction: 0.15);
            var report = SimulationEngine.AdvanceTurn(MissionState.CreateInitial(p));

            var ls = report.Supplies.Single(s => s.Role == EquipmentRole.LifeSupport);
            var research = report.Supplies.Single(s => s.Role == EquipmentRole.Research);
            Assert.That(ls.DeliveredKwh, Is.EqualTo(10.0).Within(Tol));
            Assert.That(ls.FullySupplied, Is.True);
            Assert.That(research.DeliveredKwh, Is.EqualTo(5.0).Within(Tol));
            Assert.That(research.UnmetKwh, Is.EqualTo(15.0).Within(Tol));
            Assert.That(report.LifeSupportDeficit, Is.False);
        }

        [Test]
        public void PartialResearchPower_GivesProportionalResearchHours()
        {
            var p = TestMissions.Simple(batteryCapacityKwh: 100.0, initialFraction: 0.15); // pesquisa recebe 5 de 20 kWh
            var report = SimulationEngine.AdvanceTurn(MissionState.CreateInitial(p));
            Assert.That(report.ResearchHoursGained, Is.EqualTo(10.0 * 0.25).Within(Tol));
        }

        [Test]
        public void SwitchingResearchOff_SavesEnergyButStopsResearch()
        {
            var p = TestMissions.Simple(initialFraction: 0.5);
            var on = MissionState.CreateInitial(p);
            var off = MissionState.CreateInitial(p);
            off.SetEquipmentOn(EquipmentIds.Research, false);

            var rOn = SimulationEngine.AdvanceTurn(on);
            var rOff = SimulationEngine.AdvanceTurn(off);

            Assert.That(off.BatteryStoredKwh - on.BatteryStoredKwh, Is.EqualTo(20.0).Within(Tol));
            Assert.That(rOn.ResearchHoursGained, Is.EqualTo(10.0).Within(Tol));
            Assert.That(rOff.ResearchHoursGained, Is.EqualTo(0.0).Within(Tol));
        }

        [Test]
        public void ConsecutiveLifeSupportDeficits_EndMissionAsCritical()
        {
            var p = TestMissions.Simple(initialFraction: 0.0, maxDeficitTurns: 2, totalTurns: 10);
            var state = MissionState.CreateInitial(p);

            SimulationEngine.AdvanceTurn(state);
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.InProgress));
            Assert.That(state.ConsecutiveLifeSupportDeficitTurns, Is.EqualTo(1));

            SimulationEngine.AdvanceTurn(state);
            Assert.That(state.Outcome, Is.EqualTo(MissionOutcome.CriticalFailure));
            Assert.Throws<InvalidOperationException>(() => SimulationEngine.AdvanceTurn(state));
        }

        [Test]
        public void DeficitCounter_ResetsAfterAFullySuppliedTurn()
        {
            // Turno 0 escuro (déficit), turno 1 iluminado (recupera), turno 2 escuro (déficit) → não encerra
            var p = TestMissions.Simple(solarKw: 10.0, illumination: new[] { 0.0, 1.0, 0.0 }, initialFraction: 0.0,
                chargeEfficiency: 1.0, maxDeficitTurns: 2, totalTurns: 3, researchKw: 0.0, batteryCapacityKwh: 5.0);
            var state = MissionState.CreateInitial(p);

            SimulationEngine.AdvanceTurn(state);
            Assert.That(state.ConsecutiveLifeSupportDeficitTurns, Is.EqualTo(1));
            SimulationEngine.AdvanceTurn(state);
            Assert.That(state.ConsecutiveLifeSupportDeficitTurns, Is.EqualTo(0));
            SimulationEngine.AdvanceTurn(state);
            Assert.That(state.ConsecutiveLifeSupportDeficitTurns, Is.EqualTo(1));
            Assert.That(state.TotalLifeSupportDeficitTurns, Is.EqualTo(2));
            Assert.That(state.Outcome, Is.Not.EqualTo(MissionOutcome.CriticalFailure));
        }

        [Test]
        public void LifeSupport_CannotBeSwitchedOff()
        {
            var state = MissionState.CreateInitial(TestMissions.Simple());
            Assert.Throws<InvalidOperationException>(() => state.SetEquipmentOn(EquipmentIds.LifeSupport, false));
            Assert.That(state.FindEquipment(EquipmentIds.LifeSupport).IsOn, Is.True);
            Assert.That(state.Decisions, Is.Empty);
        }

        [Test]
        public void ComputeTurn_DoesNotMutateState_AndMatchesAdvance()
        {
            var state = MissionState.CreateInitial(MissionParameters.CreateStage1Default());
            double before = state.BatteryStoredKwh;

            var preview = SimulationEngine.ComputeTurn(state);
            Assert.That(state.BatteryStoredKwh, Is.EqualTo(before));
            Assert.That(state.TurnsCompleted, Is.EqualTo(0));
            Assert.That(state.History, Is.Empty);

            var actual = SimulationEngine.AdvanceTurn(state);
            Assert.That(actual.BatteryEndKwh, Is.EqualTo(preview.BatteryEndKwh).Within(Tol));
            Assert.That(actual.UnmetKwh, Is.EqualTo(preview.UnmetKwh).Within(Tol));
        }

        [TestCase(double.NaN)]
        [TestCase(-1.0)]
        [TestCase(1.5)]
        public void InvalidIllumination_IsRejected(double value)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => TestMissions.Simple(illumination: new[] { value }));
        }

        [Test]
        public void InvalidBatteryConfiguration_IsRejected()
        {
            Assert.Throws<ArgumentException>(() => TestMissions.Simple(initialFraction: 0.1, minFraction: 0.2));
            Assert.Throws<ArgumentOutOfRangeException>(() => TestMissions.Simple(chargeEfficiency: 0.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => TestMissions.Simple(batteryCapacityKwh: double.NaN));
        }
    }
}
