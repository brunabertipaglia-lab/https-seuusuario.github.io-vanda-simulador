using System;

namespace CapiTinkering.Simulation
{
    /// <summary>Utilidades numéricas compartilhadas pela simulação.</summary>
    public static class SimMath
    {
        /// <summary>Tolerância para comparações de energia (kWh). Evita falsos déficits por arredondamento.</summary>
        public const double EnergyEpsilonKwh = 1e-9;

        public static bool IsFinite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);

        public static bool IsFiniteNonNegative(double value) => IsFinite(value) && value >= 0.0;

        public static bool IsFraction(double value) => IsFinite(value) && value >= 0.0 && value <= 1.0;

        /// <summary>Energia (kWh) = potência (kW) × duração (h). Única conversão usada no jogo.</summary>
        public static double EnergyKwh(double powerKw, double durationHours)
        {
            if (!IsFiniteNonNegative(powerKw))
                throw new ArgumentOutOfRangeException(nameof(powerKw), powerKw, "Potência inválida.");
            if (!IsFiniteNonNegative(durationHours))
                throw new ArgumentOutOfRangeException(nameof(durationHours), durationHours, "Duração inválida.");
            return powerKw * durationHours;
        }

        /// <summary>
        /// Remove apenas resíduos de ponto flutuante (ex.: -1e-15). Não serve para esconder déficits:
        /// valores negativos relevantes geram exceção, pois indicam erro de lógica.
        /// </summary>
        public static double CleanNonNegative(double value, string label)
        {
            if (!IsFinite(value))
                throw new InvalidOperationException($"Valor não finito em '{label}': {value}.");
            if (value < -EnergyEpsilonKwh)
                throw new InvalidOperationException($"Valor negativo em '{label}': {value}.");
            return value < 0.0 ? 0.0 : value;
        }
    }
}
