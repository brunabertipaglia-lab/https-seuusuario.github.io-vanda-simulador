using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CapiTinkering.Simulation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CapiTinkering.Game
{
    /// <summary>
    /// Apresentação da etapa 1. Só lê a sessão e chama ações do <see cref="MissionController"/>;
    /// todo número exibido vem do estado ou da previsão da simulação (interface e simulação concordam).
    /// </summary>
    public sealed class MissionHUD : MonoBehaviour
    {
        [Serializable]
        public sealed class EquipmentToggle
        {
            public string equipmentId;
            public Button button;
            public TMP_Text label;
        }

        [Header("Referências")]
        [SerializeField] private MissionController controller;

        [Header("Textos")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text turnText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text energyText;
        [SerializeField] private TMP_Text flowText;
        [SerializeField] private TMP_Text systemsText;
        [SerializeField] private TMP_Text forecastText;
        [SerializeField] private TMP_Text outcomeText;

        [Header("Capi")]
        [SerializeField] private Image capiPanel;
        [SerializeField] private TMP_Text capiSeverityText;
        [SerializeField] private TMP_Text capiMessageText;

        [Header("Bateria")]
        [Tooltip("Image com Image Type = Filled (horizontal).")]
        [SerializeField] private Image batteryFill;

        [Header("Botões")]
        [SerializeField] private Button advanceButton;
        [SerializeField] private TMP_Text advanceLabel;
        [SerializeField] private Button restartButton;
        [SerializeField] private TMP_Text restartLabel;
        [SerializeField] private Button languageButton;
        [SerializeField] private TMP_Text languageLabel;
        [SerializeField] private Button motionButton;
        [SerializeField] private TMP_Text motionLabel;
        [SerializeField] private List<EquipmentToggle> equipmentToggles = new List<EquipmentToggle>();

        private MissionSession _subscribedSession;
        private readonly List<UnityEngine.Events.UnityAction> _toggleActions = new List<UnityEngine.Events.UnityAction>();

        private void OnEnable()
        {
            if (controller == null)
            {
                Debug.LogError("[MissionHUD] Referência ao MissionController não atribuída.", this);
                return;
            }

            // Comparação explícita com null: o operador "?." ignora o "null falso" de objetos Unity.
            if (advanceButton != null) advanceButton.onClick.AddListener(controller.AdvanceTurn);
            if (restartButton != null) restartButton.onClick.AddListener(controller.RestartMission);
            if (languageButton != null) languageButton.onClick.AddListener(ToggleLanguage);
            if (motionButton != null) motionButton.onClick.AddListener(ToggleReduceMotion);

            _toggleActions.Clear();
            foreach (var toggle in equipmentToggles)
            {
                if (toggle == null || toggle.button == null) { _toggleActions.Add(null); continue; }
                string id = toggle.equipmentId;
                UnityEngine.Events.UnityAction action = () => controller.ToggleEquipment(id);
                toggle.button.onClick.AddListener(action);
                _toggleActions.Add(action);
            }

            AccessibilityOptions.Changed += Refresh;
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            // Garantia caso a sessão tenha sido criada depois do OnEnable (ex.: Initialize chamado em teste).
            Subscribe();
            Refresh();
        }

        private void OnDisable()
        {
            if (controller != null)
            {
                if (advanceButton != null) advanceButton.onClick.RemoveListener(controller.AdvanceTurn);
                if (restartButton != null) restartButton.onClick.RemoveListener(controller.RestartMission);
            }
            if (languageButton != null) languageButton.onClick.RemoveListener(ToggleLanguage);
            if (motionButton != null) motionButton.onClick.RemoveListener(ToggleReduceMotion);

            for (int i = 0; i < equipmentToggles.Count && i < _toggleActions.Count; i++)
            {
                if (equipmentToggles[i] != null && equipmentToggles[i].button != null && _toggleActions[i] != null)
                    equipmentToggles[i].button.onClick.RemoveListener(_toggleActions[i]);
            }
            _toggleActions.Clear();

            AccessibilityOptions.Changed -= Refresh;
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (controller == null || !controller.IsReady || _subscribedSession == controller.Session) return;
            Unsubscribe();
            _subscribedSession = controller.Session;
            _subscribedSession.StateChanged += Refresh;
        }

        private void Unsubscribe()
        {
            if (_subscribedSession == null) return;
            _subscribedSession.StateChanged -= Refresh;
            _subscribedSession = null;
        }

        private void ToggleLanguage()
        {
            TextTable.Language = TextTable.Language == GameLanguage.PortugueseBrazil
                ? GameLanguage.English
                : GameLanguage.PortugueseBrazil;
            Refresh();
        }

        private void ToggleReduceMotion()
        {
            AccessibilityOptions.ReduceMotion = !AccessibilityOptions.ReduceMotion;
        }

        /// <summary>Redesenha tudo a partir do estado atual. Barato o suficiente para uma UI por turnos.</summary>
        public void Refresh()
        {
            if (controller == null || !controller.IsReady) return;

            var session = controller.Session;
            var state = session.State;
            var p = state.Parameters;
            var last = state.History.Count > 0 ? state.History[state.History.Count - 1] : null;
            var preview = session.PreviewNextTurn();

            Set(titleText, TextTable.Get("hud.title", TextTable.TeamName));

            // Turno e tempo simulado
            int displayTurn = Math.Min(state.TurnsCompleted + 1, p.TotalTurns);
            double elapsed = state.ElapsedHours;
            Set(turnText, TextTable.Get("hud.turn",
                displayTurn, p.TotalTurns, (int)(elapsed / 24.0) + 1, TextTable.Num(elapsed % 24.0, 0),
                TextTable.Num(p.TurnDurationHours, 0)));

            // Objetivo
            double pct = p.ResearchGoalHours > 0 ? 100.0 * state.ResearchHours / p.ResearchGoalHours : 100.0;
            Set(objectiveText, TextTable.Get("hud.objective",
                TextTable.Num(p.ResearchGoalHours, 0), TextTable.Num(state.ResearchHours), TextTable.Num(Math.Min(pct, 100.0), 0)));

            // Energia: iluminação do próximo turno e bateria
            var energy = new StringBuilder();
            if (preview != null)
                energy.AppendLine(TextTable.Get("hud.illumination",
                    TextTable.Num(preview.IlluminationFactor * 100.0, 0), TextTable.Num(preview.SolarPowerKw)));
            energy.Append(TextTable.Get("hud.battery",
                TextTable.Num(state.BatteryStoredKwh), TextTable.Num(p.BatteryCapacityKwh, 0),
                TextTable.Num(state.BatteryFraction * 100.0, 0), TextTable.Num(p.BatteryMinKwh)));
            Set(energyText, energy.ToString());
            if (batteryFill != null) batteryFill.fillAmount = (float)state.BatteryFraction;

            // Fluxo do último turno
            Set(flowText, last == null
                ? TextTable.Get("hud.flow_none")
                : TextTable.Get("hud.flow",
                    TextTable.Num(last.GenerationKwh), TextTable.Num(last.DemandKwh), TextTable.Num(last.DeliveredKwh),
                    TextTable.Num(last.UnmetKwh), TextTable.Num(last.CurtailedKwh)));

            // Sistemas
            var systems = new StringBuilder();
            foreach (var e in state.Equipment)
            {
                var supply = last?.Supplies.FirstOrDefault(s => s.EquipmentId == e.Spec.Id);
                string status = StatusText(e, supply);
                if (!e.Spec.CanBeSwitchedOff) status += " " + TextTable.Get("status.locked");
                systems.AppendLine(TextTable.Get("hud.system_line",
                    TextTable.Get(e.Spec.NameKey), status, TextTable.Num(e.Spec.PowerKw),
                    TextTable.Num(SimMath.EnergyKwh(e.Spec.PowerKw, p.TurnDurationHours))));
            }
            Set(systemsText, systems.ToString().TrimEnd());

            // Previsão (consequência prevista da configuração atual)
            if (preview != null)
            {
                string risk = preview.LifeSupportDeficit ? TextTable.Get("hud.forecast_ls_risk") : string.Empty;
                Set(forecastText,
                    TextTable.Get("hud.forecast",
                        TextTable.Num(preview.IlluminationFactor * 100.0, 0), TextTable.Num(preview.GenerationKwh),
                        TextTable.Num(preview.DemandKwh), TextTable.Num(preview.BatteryEndKwh), risk)
                    + "\n" + TextTable.Get("hud.forecast_note"));
            }
            else
            {
                Set(forecastText, string.Empty);
            }

            // Resultado
            Set(outcomeText, TextTable.Get(OutcomeKey(state.Outcome)));

            // Capi
            var message = CapiAdvisor.Evaluate(state);
            Set(capiSeverityText, TextTable.Get("hud.capi_name") + " · " + message.SeverityLabel);
            Set(capiMessageText, message.Text);
            if (capiPanel != null) capiPanel.color = BrandPalette.ForSeverity(message.Severity);
            var capiTextColor = BrandPalette.TextOnSeverity(message.Severity);
            if (capiSeverityText != null) capiSeverityText.color = capiTextColor;
            if (capiMessageText != null) capiMessageText.color = capiTextColor;

            // Botões
            if (advanceButton != null) advanceButton.interactable = session.CanAdvance;
            Set(advanceLabel, TextTable.Get("hud.button_advance"));
            Set(restartLabel, TextTable.Get("hud.button_restart"));
            Set(languageLabel, TextTable.Get("hud.button_language"));
            Set(motionLabel, TextTable.Get(AccessibilityOptions.ReduceMotion ? "hud.button_motion_reduced" : "hud.button_motion_full"));

            foreach (var toggle in equipmentToggles)
            {
                if (toggle == null || toggle.button == null) continue;
                var e = state.Equipment.FirstOrDefault(x => x.Spec.Id == toggle.equipmentId);
                if (e == null)
                {
                    toggle.button.gameObject.SetActive(false);
                    continue;
                }
                toggle.button.interactable = !state.IsFinished && e.Spec.CanBeSwitchedOff;
                Set(toggle.label, TextTable.Get(e.IsOn ? "hud.button_turn_off" : "hud.button_turn_on", TextTable.Get(e.Spec.NameKey)));
            }
        }

        private static string StatusText(EquipmentState e, EquipmentSupply lastSupply)
        {
            if (!e.IsOn) return TextTable.Get("status.off");
            // Ligado agora, mas o último turno pode ter sido com outra configuração: só usamos o
            // atendimento anterior se o equipamento também estava ligado nele.
            if (lastSupply == null || !lastSupply.WasOn || lastSupply.FullySupplied) return TextTable.Get("status.on");
            return lastSupply.DeliveredKwh > SimMath.EnergyEpsilonKwh
                ? TextTable.Get("status.partial")
                : TextTable.Get("status.unpowered");
        }

        private static string OutcomeKey(MissionOutcome outcome)
        {
            switch (outcome)
            {
                case MissionOutcome.ObjectiveAchieved: return "outcome.objective_achieved";
                case MissionOutcome.CompletedObjectiveNotMet: return "outcome.completed_not_met";
                case MissionOutcome.CriticalFailure: return "outcome.critical";
                default: return "outcome.in_progress";
            }
        }

        private static void Set(TMP_Text target, string value)
        {
            if (target != null) target.text = value;
        }
    }
}
