using System;
using CapiTinkering.Simulation;
using UnityEngine;

namespace CapiTinkering.Game
{
    /// <summary>
    /// Adaptador entre a cena e a simulação. Cria a <see cref="MissionSession"/> a partir do asset
    /// e expõe ações simples para a interface. Não contém regras de jogo.
    /// Executa antes da interface para que a sessão exista quando o HUD se inscrever nos eventos.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class MissionController : MonoBehaviour
    {
        [SerializeField] private MissionDefinition mission;

        public MissionSession Session { get; private set; }

        public bool IsReady => Session != null;

        private void Awake()
        {
            if (Session == null) Initialize(mission);
        }

        /// <summary>Cria a sessão. Público para testes de Play Mode e para trocar de missão no futuro.</summary>
        public void Initialize(MissionDefinition definition)
        {
            if (definition == null)
            {
                Debug.LogError("[MissionController] Nenhuma MissionDefinition atribuída no Inspector.", this);
                enabled = false;
                return;
            }

            mission = definition;
            try
            {
                Session = new MissionSession(definition.BuildParameters());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[MissionController] Missão '{definition.name}' inválida: {ex.Message}", this);
                enabled = false;
            }
        }

        public void AdvanceTurn()
        {
            if (Session == null || !Session.CanAdvance) return;
            Session.AdvanceTurn();
        }

        public void ToggleEquipment(string equipmentId)
        {
            if (Session == null || Session.State.IsFinished) return;
            var equipment = Session.State.FindEquipment(equipmentId);
            if (!equipment.Spec.CanBeSwitchedOff && equipment.IsOn) return;
            Session.SetEquipmentOn(equipmentId, !equipment.IsOn);
        }

        public void RestartMission()
        {
            Session?.Restart();
        }
    }
}
