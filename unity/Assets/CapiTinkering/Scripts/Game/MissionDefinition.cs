using System;
using System.Collections.Generic;
using System.Linq;
using CapiTinkering.Simulation;
using UnityEngine;

namespace CapiTinkering.Game
{
    /// <summary>
    /// Definição editável da missão (asset). Nunca guarda progresso do jogador e nunca é alterada
    /// durante a partida: <see cref="BuildParameters"/> gera parâmetros imutáveis para a simulação.
    /// Valores padrão: PARÂMETROS FICTÍCIOS DE BALANCEAMENTO (docs/MODELO_SIMULACAO.md).
    /// </summary>
    [CreateAssetMenu(fileName = "Mission_", menuName = "Capi Tinkering/Missão", order = 0)]
    public sealed class MissionDefinition : ScriptableObject
    {
        [Header("Tempo")]
        [Tooltip("Horas simuladas por turno.")]
        [SerializeField] private float turnDurationHours = 12.0f;
        [Min(1)] [SerializeField] private int totalTurns = 16;

        [Header("Objetivo científico")]
        [Tooltip("Horas de pesquisa com energia plena necessárias para cumprir o objetivo.")]
        [SerializeField] private float researchGoalHours = 120.0f;

        [Header("Energia")]
        [Tooltip("Potência nominal dos painéis com iluminação plena (kW).")]
        [SerializeField] private float solarArrayKw = 5.0f;
        [Tooltip("Capacidade da bateria (kWh).")]
        [SerializeField] private float batteryCapacityKwh = 50.0f;
        [Range(0f, 1f)] [SerializeField] private float batteryInitialFraction = 0.6f;
        [Tooltip("Reserva protegida: a bateria não descarrega abaixo desta fração.")]
        [Range(0f, 1f)] [SerializeField] private float batteryMinFraction = 0.1f;
        [Tooltip("Fração do excedente que fica armazenada ao carregar.")]
        [Range(0.01f, 1f)] [SerializeField] private float chargeEfficiency = 0.9f;

        [Tooltip("Fator de iluminação por turno (0–1), repetido se for menor que o total de turnos. " +
                 "Perfil FICTÍCIO que representa uma sombra temporária num local próximo ao polo.")]
        [SerializeField] private float[] illuminationProfile =
        {
            0.9f, 0.9f, 0.9f, 0.8f, 0.5f, 0f, 0f, 0f,
            0.5f, 0.8f, 0.9f, 0.9f, 0.9f, 0.9f, 0.8f, 0.5f
        };

        [Header("Segurança")]
        [Min(1)] [SerializeField] private int maxConsecutiveLifeSupportDeficitTurns = 2;

        [Header("Equipamentos")]
        [SerializeField] private List<EquipmentDefinition> equipment = new List<EquipmentDefinition>();

        public IReadOnlyList<EquipmentDefinition> Equipment => equipment;

        /// <summary>Converte o asset em parâmetros imutáveis. Lança exceção com mensagem clara se algo for inválido.</summary>
        public MissionParameters BuildParameters()
        {
            if (equipment.Any(e => e == null))
                throw new InvalidOperationException($"'{name}': há um equipamento vazio na lista.");

            if (illuminationProfile == null)
                throw new InvalidOperationException($"'{name}': perfil de iluminação ausente.");

            return new MissionParameters(
                ToDouble(turnDurationHours),
                totalTurns,
                ToDouble(researchGoalHours),
                ToDouble(solarArrayKw),
                ToDouble(batteryCapacityKwh),
                ToDouble(batteryInitialFraction),
                ToDouble(batteryMinFraction),
                ToDouble(chargeEfficiency),
                illuminationProfile.Select(ToDouble),
                maxConsecutiveLifeSupportDeficitTurns,
                equipment.Select(e => e.ToSpec()));
        }

        /// <summary>
        /// O Inspector da Unity edita float; a simulação usa double. Arredondar a 6 casas evita que
        /// 0.9f vire 0.899999976 e faz os dados do asset coincidirem com os valores digitados.
        /// </summary>
        private static double ToDouble(float value) => Math.Round((double)value, 6);

        /// <summary>Usado pelo menu do Editor para preencher a lista de equipamentos padrão.</summary>
        public void SetEquipment(IEnumerable<EquipmentDefinition> definitions)
        {
            equipment = definitions.ToList();
        }

        /// <summary>Validação no Inspector: mostra o problema no Console sem impedir a edição.</summary>
        public bool TryValidate(out string error)
        {
            try
            {
                BuildParameters();
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private void OnValidate()
        {
            if (!TryValidate(out var error))
                Debug.LogWarning($"[MissionDefinition] '{name}' inválida: {error}", this);
        }
    }
}
