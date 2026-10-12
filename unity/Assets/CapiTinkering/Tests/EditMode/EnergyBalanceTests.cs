using System;
using System.Linq;
using CapiTinkering.Simulation;
using NUnit.Framework;

namespace CapiTinkering.Tests.EditMode
{
    public class EnergyBalanceTests
    {
        private const double Tol = 1e-9;

        [Test]
        public void EnergyKwh_IsPowerTimesDuration()
        {
            Assert.That(SimMath.EnergyKwh(5.0, 12.0), Is.EqualTo(60.0).Within(Tol));
            Assert.That(SimMath.EnergyKwh(0.5, 6.0), Is.EqualTo(3.0).Within(Tol));
            Assert.That(SimMath.EnergyKwh(0.0, 12.0), Is.EqualTo(0.0).Within(Tol));
        }

        [Test]
        public void EnergyKwh_RejectsInvalidInput()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SimMath.EnergyKwh(-1.0, 12.0));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimMath.EnergyKwh(1.0, double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => SimMath.EnergyKwh(double.PositiveInfinity, 1.0));
        }

        [Test]
        public void Generation_UsesIlluminationAndTurnDuration()
        {
            // 4 kW × 0,5 × 10 h = 20 kWh
            var state = MissionState.CreateInitial(TestMissions.Simple(solarKw: 4.0, illumination: new[] { 0.5 }));
            var report = SimulationEngine.AdvanceTurn(state);
            Assert.That(report.SolarPowerKw, Is.EqualTo(2.0).Within(Tol));
            Assert.That(report.GenerationKwh, Is.EqualTo(20.0).Within(Tol));
        }

        [Test]
        public void TurnDuration_ChangesEnergyButNotPower()
        {
            var shortTurn = SimulationEngine.ComputeTurn(MissionState.CreateInitial(TestMissions.Simple(solarKw: 4.0, turnHours: 6.0)));
            var longTurn = SimulationEngine.ComputeTurn(MissionState.CreateInitial(TestMissions.Simple(solarKw: 4.0, turnHours: 12.0)));
            Assert.That(shortTurn.SolarPowerKw, Is.EqualTo(longTurn.SolarPowerKw).Within(Tol));
            Assert.That(longTurn.GenerationKwh, Is.EqualTo(2.0 * shortTurn.GenerationKwh).Within(Tol));
            Assert.That(longTurn.DemandKwh, Is.EqualTo(2.0 * shortTurn.DemandKwh).Within(Tol));
        }

        [Test]
        public void Surplus_ChargesBatteryWithEfficiencyLoss()
        {
            // Geração 50 kWh, demanda 30 kWh → excedente 20; eficiência 0,8 → +16 kWh armazenados
            var p = TestMissions.Simple(solarKw: 5.0, chargeEfficiency: 0.8, initialFraction: 0.2);
            var state = MissionState.CreateInitial(p);
            var report = SimulationEngine.AdvanceTurn(state);

            Assert.That(report.BatteryChargeInputKwh, Is.EqualTo(20.0).Within(Tol));
            Assert.That(report.ChargeLossKwh, Is.EqualTo(4.0).Within(Tol));
            Assert.That(state.BatteryStoredKwh, Is.EqualTo(20.0 + 16.0).Within(Tol));
            Assert.That(report.CurtailedKwh, Is.EqualTo(0.0).Within(Tol));
        }

        [Test]
        public void FullBattery_CurtailsSurplusAndNeverExceedsCapacity()
        {
            var p = TestMissions.Simple(solarKw: 10.0, batteryCapacityKwh: 40.0, initialFraction: 0.95);
            var state = MissionState.CreateInitial(p);
            var report = SimulationEngine.AdvanceTurn(state);

            // Geração 100, demanda 30, excedente 70; cabem 2 kWh → 68 desperdiçados
            Assert.That(state.BatteryStoredKwh, Is.EqualTo(40.0).Within(Tol));
            Assert.That(report.BatteryChargeInputKwh, Is.EqualTo(2.0).Within(Tol));
            Assert.That(report.CurtailedKwh, Is.EqualTo(68.0).Within(Tol));
        }

        [Test]
        public void Battery_DischargesOnlyDownToMinimumReserve()
        {
            // Sem Sol; bateria 100 kWh com 50 kWh, reserva 30% = 30 kWh → só 20 kWh utilizáveis
            var p = TestMissions.Simple(minFraction: 0.3);
            var state = MissionState.CreateInitial(p);
            var report = SimulationEngine.AdvanceTurn(state);

            Assert.That(report.BatteryDischargeKwh, Is.EqualTo(20.0).Within(Tol));
            Assert.That(state.BatteryStoredKwh, Is.EqualTo(30.0).Within(Tol));
            Assert.That(report.UnmetKwh, Is.EqualTo(10.0).Within(Tol));
        }

        [Test]
        public void BalanceIdentities_HoldOnEveryTurnOfTheDefaultMission()
        {
            var p = MissionParameters.CreateStage1Default();
            var state = MissionState.CreateInitial(p);
            while (!state.IsFinished)
            {
                var r = SimulationEngine.AdvanceTurn(state);
                Assert.That(r.GenerationKwh, Is.EqualTo(r.DirectSolarUseKwh + r.BatteryChargeInputKwh + r.CurtailedKwh).Within(1e-6), "geração");
                Assert.That(r.BatteryEndKwh, Is.EqualTo(r.BatteryStartKwh - r.BatteryDischargeKwh + r.BatteryChargeInputKwh * p.ChargeEfficiency).Within(1e-6), "bateria");
                Assert.That(r.DeliveredKwh, Is.EqualTo(r.DirectSolarUseKwh + r.BatteryDischargeKwh).Within(1e-6), "entregue");
                Assert.That(r.DemandKwh, Is.EqualTo(r.DeliveredKwh + r.UnmetKwh).Within(1e-6), "demanda");
                AssertAllFiniteAndNonNegative(r);
                Assert.That(r.BatteryEndKwh, Is.LessThanOrEqualTo(p.BatteryCapacityKwh + 1e-9));
                Assert.That(r.BatteryEndKwh, Is.GreaterThanOrEqualTo(p.BatteryMinKwh - 1e-9));
            }
        }

        private static void AssertAllFiniteAndNonNegative(TurnReport r)
        {
            double[] values =
            {
                r.GenerationKwh, r.DemandKwh, r.DeliveredKwh, r.UnmetKwh, r.DirectSolarUseKwh,
                r.BatteryDischargeKwh, r.BatteryChargeInputKwh, r.ChargeLossKwh, r.CurtailedKwh,
                r.BatteryStartKwh, r.BatteryEndKwh, r.LifeSupportUnmetKwh, r.ResearchHoursGained
            };
            foreach (var v in values.Concat(r.Supplies.SelectMany(s => new[] { s.DemandKwh, s.DeliveredKwh, s.UnmetKwh })))
            {
                Assert.That(SimMath.IsFiniteNonNegative(v), Is.True, $"valor inválido: {v}");
            }
        }
    }
}
