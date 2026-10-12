using System.Collections;
using CapiTinkering.Game;
using CapiTinkering.Simulation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CapiTinkering.Tests.PlayMode
{
    /// <summary>
    /// Integração mínima com a Unity: assets criados em memória (sem tocar nos assets do projeto),
    /// controlador em um GameObject real e ciclo de quadros do Play Mode.
    /// </summary>
    public class MissionControllerPlayModeTests
    {
        private GameObject _go;
        private MissionDefinition _mission;
        private EquipmentDefinition[] _equipment;

        [SetUp]
        public void SetUp()
        {
            _equipment = new[]
            {
                Equipment(EquipmentIds.LifeSupport, "equip.life_support", EquipmentRole.LifeSupport, 1.0f, 0, false),
                Equipment(EquipmentIds.Communication, "equip.communication", EquipmentRole.Communication, 0.5f, 1, true),
                Equipment(EquipmentIds.Research, "equip.research", EquipmentRole.Research, 2.0f, 2, true)
            };
            _mission = ScriptableObject.CreateInstance<MissionDefinition>(); // valores padrão = etapa 1
            _mission.SetEquipment(_equipment);

            _go = new GameObject("MissionController (test)");
            _go.SetActive(false); // evita Awake sem missão atribuída
            var controller = _go.AddComponent<MissionController>();
            controller.Initialize(_mission);
            _go.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(_go);
            Object.Destroy(_mission);
            foreach (var e in _equipment) Object.Destroy(e);
        }

        [UnityTest]
        public IEnumerator Controller_AdvancesTurnAndMatchesPureSimulation()
        {
            yield return null;
            var controller = _go.GetComponent<MissionController>();
            Assert.That(controller.IsReady, Is.True);

            controller.AdvanceTurn();
            yield return null;

            // Mesmo cálculo feito sem Unity: interface e simulação devem concordar
            var reference = MissionState.CreateInitial(MissionParameters.CreateStage1Default());
            SimulationEngine.AdvanceTurn(reference);

            Assert.That(controller.Session.State.TurnsCompleted, Is.EqualTo(1));
            Assert.That(controller.Session.State.BatteryStoredKwh, Is.EqualTo(reference.BatteryStoredKwh).Within(1e-6));
        }

        [UnityTest]
        public IEnumerator Controller_RestartLeavesNoResidualState()
        {
            var controller = _go.GetComponent<MissionController>();
            controller.ToggleEquipment(EquipmentIds.Research);
            controller.AdvanceTurn();
            controller.AdvanceTurn();
            yield return null;

            controller.RestartMission();
            yield return null;

            var state = controller.Session.State;
            Assert.That(state.TurnsCompleted, Is.EqualTo(0));
            Assert.That(state.History, Is.Empty);
            Assert.That(state.Decisions, Is.Empty);
            Assert.That(state.FindEquipment(EquipmentIds.Research).IsOn, Is.True);
        }

        [UnityTest]
        public IEnumerator Controller_IgnoresAttemptToSwitchOffLifeSupport()
        {
            var controller = _go.GetComponent<MissionController>();
            controller.ToggleEquipment(EquipmentIds.LifeSupport);
            yield return null;
            Assert.That(controller.Session.State.FindEquipment(EquipmentIds.LifeSupport).IsOn, Is.True);
        }

        private static EquipmentDefinition Equipment(string id, string key, EquipmentRole role, float kw, int priority, bool switchable)
        {
            var e = ScriptableObject.CreateInstance<EquipmentDefinition>();
            e.Configure(id, key, role, kw, priority, switchable, true);
            return e;
        }
    }
}
