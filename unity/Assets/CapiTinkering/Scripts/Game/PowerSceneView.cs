using CapiTinkering.Simulation;
using UnityEngine;

namespace CapiTinkering.Game
{
    /// <summary>
    /// Representação visual simples da base: intensidade do "Sol", nível da bateria e estado do
    /// módulo de pesquisa. Apenas lê a simulação. Respeita a opção de movimento reduzido.
    /// </summary>
    public sealed class PowerSceneView : MonoBehaviour
    {
        [SerializeField] private MissionController controller;

        [Header("Iluminação")]
        [SerializeField] private Light sunLight;
        [SerializeField] private float sunMaxIntensity = 1.4f;
        [SerializeField] private float sunMinIntensity = 0.08f;

        [Header("Bateria")]
        [Tooltip("Objeto cuja escala Y representa a carga (pivô na base).")]
        [SerializeField] private Transform batteryFill;
        [SerializeField] private float batteryFullHeight = 1f;

        [Header("Equipamentos")]
        [SerializeField] private Renderer researchModule;
        [SerializeField] private Renderer communicationAntenna;

        [Header("Animação")]
        [SerializeField] private float transitionSpeed = 3f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP
        private static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in

        private MissionSession _session;
        private MaterialPropertyBlock _block;
        private float _targetSun;
        private float _targetBattery;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (controller != null && controller.IsReady)
            {
                _session = controller.Session;
                _session.StateChanged += Apply;
            }
            Apply();
            SnapToTargets();
        }

        private void OnDisable()
        {
            if (_session != null) _session.StateChanged -= Apply;
            _session = null;
        }

        private void Apply()
        {
            if (controller == null || !controller.IsReady) return;
            var state = controller.Session.State;
            var preview = controller.Session.PreviewNextTurn();
            var last = state.History.Count > 0 ? state.History[state.History.Count - 1] : null;

            float illumination = preview != null ? (float)preview.IlluminationFactor
                : last != null ? (float)last.IlluminationFactor : 0f;
            _targetSun = Mathf.Lerp(sunMinIntensity, sunMaxIntensity, illumination);
            _targetBattery = Mathf.Clamp01((float)state.BatteryFraction);

            Tint(researchModule, EquipmentColor(state, last, EquipmentIds.Research));
            Tint(communicationAntenna, EquipmentColor(state, last, EquipmentIds.Communication));

            if (AccessibilityOptions.ReduceMotion) SnapToTargets();
        }

        private void Update()
        {
            if (AccessibilityOptions.ReduceMotion) return;
            float t = 1f - Mathf.Exp(-transitionSpeed * Time.deltaTime);
            if (sunLight != null) sunLight.intensity = Mathf.Lerp(sunLight.intensity, _targetSun, t);
            if (batteryFill != null) SetBatteryHeight(Mathf.Lerp(batteryFill.localScale.y / batteryFullHeight, _targetBattery, t));
        }

        private void SnapToTargets()
        {
            if (sunLight != null) sunLight.intensity = _targetSun;
            if (batteryFill != null) SetBatteryHeight(_targetBattery);
        }

        private void SetBatteryHeight(float fraction)
        {
            var s = batteryFill.localScale;
            batteryFill.localScale = new Vector3(s.x, Mathf.Max(0.001f, fraction * batteryFullHeight), s.z);
        }

        private static Color EquipmentColor(MissionState state, TurnReport last, string id)
        {
            EquipmentState equipment = null;
            foreach (var e in state.Equipment)
                if (e.Spec.Id == id) equipment = e;
            if (equipment == null || !equipment.IsOn) return BrandPalette.Muted;

            if (last != null)
            {
                foreach (var supply in last.Supplies)
                {
                    if (supply.EquipmentId != id || !supply.WasOn) continue;
                    if (!supply.FullySupplied) return BrandPalette.Orange;
                }
            }
            return BrandPalette.Turquoise;
        }

        private void Tint(Renderer target, Color color)
        {
            if (target == null) return;
            target.GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, color);
            _block.SetColor(ColorId, color);
            target.SetPropertyBlock(_block);
        }
    }
}
