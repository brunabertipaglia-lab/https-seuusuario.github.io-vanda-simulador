using System;
using System.Collections.Generic;
using System.Linq;

namespace CapiTinkering.Simulation
{
    /// <summary>
    /// Parâmetros imutáveis de uma missão (etapa 1: subsistema de energia).
    /// São criados a partir dos ScriptableObjects e nunca alterados durante a partida.
    /// Todos os números padrão do projeto são DIDÁTICOS/FICTÍCIOS (ver docs/MODELO_SIMULACAO.md).
    /// </summary>
    public sealed class MissionParameters
    {
        /// <summary>Duração simulada de um turno, em horas.</summary>
        public double TurnDurationHours { get; }

        /// <summary>Número de turnos da missão.</summary>
        public int TotalTurns { get; }

        /// <summary>Meta do objetivo científico, em horas de pesquisa com energia plena.</summary>
        public double ResearchGoalHours { get; }

        /// <summary>Potência nominal do conjunto de painéis com iluminação plena (fator 1), em kW.</summary>
        public double SolarArrayKw { get; }

        /// <summary>Capacidade de armazenamento da bateria, em kWh.</summary>
        public double BatteryCapacityKwh { get; }

        /// <summary>Carga inicial como fração da capacidade (0–1).</summary>
        public double BatteryInitialFraction { get; }

        /// <summary>Reserva mínima protegida: a bateria não descarrega abaixo desta fração (0–1).</summary>
        public double BatteryMinFraction { get; }

        /// <summary>Fração da energia excedente que efetivamente fica armazenada ao carregar (0–1].</summary>
        public double ChargeEfficiency { get; }

        /// <summary>
        /// Fator de iluminação solar por turno (0–1). Repetido ciclicamente se for menor que TotalTurns.
        /// </summary>
        public IReadOnlyList<double> IlluminationProfile { get; }

        /// <summary>Turnos consecutivos com suporte de vida incompleto que encerram a missão.</summary>
        public int MaxConsecutiveLifeSupportDeficitTurns { get; }

        public IReadOnlyList<EquipmentSpec> Equipment { get; }

        public MissionParameters(
            double turnDurationHours,
            int totalTurns,
            double researchGoalHours,
            double solarArrayKw,
            double batteryCapacityKwh,
            double batteryInitialFraction,
            double batteryMinFraction,
            double chargeEfficiency,
            IEnumerable<double> illuminationProfile,
            int maxConsecutiveLifeSupportDeficitTurns,
            IEnumerable<EquipmentSpec> equipment)
        {
            if (!SimMath.IsFinite(turnDurationHours) || turnDurationHours <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(turnDurationHours), turnDurationHours, "Duração do turno deve ser > 0 h.");
            if (totalTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(totalTurns), totalTurns, "A missão precisa de ao menos 1 turno.");
            if (!SimMath.IsFiniteNonNegative(researchGoalHours))
                throw new ArgumentOutOfRangeException(nameof(researchGoalHours), researchGoalHours, "Meta de pesquisa inválida.");
            if (!SimMath.IsFiniteNonNegative(solarArrayKw))
                throw new ArgumentOutOfRangeException(nameof(solarArrayKw), solarArrayKw, "Potência solar inválida.");
            if (!SimMath.IsFiniteNonNegative(batteryCapacityKwh))
                throw new ArgumentOutOfRangeException(nameof(batteryCapacityKwh), batteryCapacityKwh, "Capacidade da bateria inválida.");
            if (!SimMath.IsFraction(batteryInitialFraction))
                throw new ArgumentOutOfRangeException(nameof(batteryInitialFraction), batteryInitialFraction, "Carga inicial deve estar entre 0 e 1.");
            if (!SimMath.IsFraction(batteryMinFraction))
                throw new ArgumentOutOfRangeException(nameof(batteryMinFraction), batteryMinFraction, "Reserva mínima deve estar entre 0 e 1.");
            if (batteryInitialFraction < batteryMinFraction)
                throw new ArgumentException("A carga inicial não pode ficar abaixo da reserva mínima.", nameof(batteryInitialFraction));
            if (!SimMath.IsFinite(chargeEfficiency) || chargeEfficiency <= 0.0 || chargeEfficiency > 1.0)
                throw new ArgumentOutOfRangeException(nameof(chargeEfficiency), chargeEfficiency, "Eficiência de carga deve estar em (0, 1].");
            if (maxConsecutiveLifeSupportDeficitTurns <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxConsecutiveLifeSupportDeficitTurns), maxConsecutiveLifeSupportDeficitTurns, "Limite de déficit deve ser >= 1.");

            var profile = (illuminationProfile ?? throw new ArgumentNullException(nameof(illuminationProfile))).ToArray();
            if (profile.Length == 0)
                throw new ArgumentException("O perfil de iluminação precisa de ao menos um valor.", nameof(illuminationProfile));
            for (int i = 0; i < profile.Length; i++)
            {
                if (!SimMath.IsFraction(profile[i]))
                    throw new ArgumentOutOfRangeException(nameof(illuminationProfile), profile[i], $"Iluminação do índice {i} deve estar entre 0 e 1.");
            }

            var specs = (equipment ?? throw new ArgumentNullException(nameof(equipment))).ToArray();
            if (specs.Any(s => s == null))
                throw new ArgumentException("A lista de equipamentos contém item nulo.", nameof(equipment));
            var duplicated = specs.GroupBy(s => s.Id).FirstOrDefault(g => g.Count() > 1);
            if (duplicated != null)
                throw new ArgumentException($"Id de equipamento repetido: '{duplicated.Key}'.", nameof(equipment));
            if (!specs.Any(s => s.Role == EquipmentRole.LifeSupport))
                throw new ArgumentException("A missão precisa de ao menos um equipamento de suporte de vida.", nameof(equipment));

            TurnDurationHours = turnDurationHours;
            TotalTurns = totalTurns;
            ResearchGoalHours = researchGoalHours;
            SolarArrayKw = solarArrayKw;
            BatteryCapacityKwh = batteryCapacityKwh;
            BatteryInitialFraction = batteryInitialFraction;
            BatteryMinFraction = batteryMinFraction;
            ChargeEfficiency = chargeEfficiency;
            IlluminationProfile = Array.AsReadOnly(profile);
            MaxConsecutiveLifeSupportDeficitTurns = maxConsecutiveLifeSupportDeficitTurns;
            Equipment = Array.AsReadOnly(specs);
        }

        public double BatteryMinKwh => BatteryCapacityKwh * BatteryMinFraction;

        public double BatteryInitialKwh => BatteryCapacityKwh * BatteryInitialFraction;

        /// <summary>Fator de iluminação do turno indicado (0 = primeiro turno).</summary>
        public double IlluminationAt(int turnIndex)
        {
            if (turnIndex < 0) throw new ArgumentOutOfRangeException(nameof(turnIndex));
            return IlluminationProfile[turnIndex % IlluminationProfile.Count];
        }

        /// <summary>
        /// Parâmetros padrão da etapa 1. Valores FICTÍCIOS de balanceamento, iguais aos criados
        /// pelo menu do Editor; servem para testes e para o primeiro protótipo.
        /// </summary>
        public static MissionParameters CreateStage1Default()
        {
            return new MissionParameters(
                turnDurationHours: 12.0,
                totalTurns: 16,
                researchGoalHours: 120.0,
                solarArrayKw: 5.0,
                batteryCapacityKwh: 50.0,
                batteryInitialFraction: 0.6,
                batteryMinFraction: 0.1,
                chargeEfficiency: 0.9,
                illuminationProfile: new[]
                {
                    0.9, 0.9, 0.9, 0.8, 0.5, 0.0, 0.0, 0.0,
                    0.5, 0.8, 0.9, 0.9, 0.9, 0.9, 0.8, 0.5
                },
                maxConsecutiveLifeSupportDeficitTurns: 2,
                equipment: new[]
                {
                    new EquipmentSpec(EquipmentIds.LifeSupport, "equip.life_support", EquipmentRole.LifeSupport, 1.0, 0, false, true),
                    new EquipmentSpec(EquipmentIds.Communication, "equip.communication", EquipmentRole.Communication, 0.5, 1, true, true),
                    new EquipmentSpec(EquipmentIds.Research, "equip.research", EquipmentRole.Research, 2.0, 2, true, true)
                });
        }
    }

    /// <summary>Ids usados pelos dados padrão. Os ScriptableObjects devem usar os mesmos valores.</summary>
    public static class EquipmentIds
    {
        public const string LifeSupport = "life_support";
        public const string Communication = "communication";
        public const string Research = "research";
    }
}
