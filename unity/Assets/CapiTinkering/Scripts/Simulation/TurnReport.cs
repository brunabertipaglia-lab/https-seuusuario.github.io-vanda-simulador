using System.Collections.Generic;

namespace CapiTinkering.Simulation
{
    /// <summary>Energia pedida e entregue a um equipamento em um turno.</summary>
    public sealed class EquipmentSupply
    {
        public string EquipmentId { get; }
        public EquipmentRole Role { get; }
        public bool WasOn { get; }
        public double DemandKwh { get; }
        public double DeliveredKwh { get; }
        public double UnmetKwh { get; }

        /// <summary>Fração atendida (1 quando desligado: não havia demanda a atender).</summary>
        public double SuppliedFraction => DemandKwh > SimMath.EnergyEpsilonKwh ? DeliveredKwh / DemandKwh : 1.0;

        public bool FullySupplied => UnmetKwh <= SimMath.EnergyEpsilonKwh;

        public EquipmentSupply(string equipmentId, EquipmentRole role, bool wasOn, double demandKwh, double deliveredKwh)
        {
            EquipmentId = equipmentId;
            Role = role;
            WasOn = wasOn;
            DemandKwh = demandKwh;
            DeliveredKwh = deliveredKwh;
            UnmetKwh = SimMath.CleanNonNegative(demandKwh - deliveredKwh, $"{equipmentId}.unmet");
        }
    }

    /// <summary>
    /// Resultado completo de um turno. Gerado pelo <see cref="SimulationEngine"/>; também serve como previsão.
    /// Balanços garantidos (testados):
    /// Geração = UsoDireto + EntradaNaBateria + Desperdício
    /// BateriaFinal = BateriaInicial − Descarga + EntradaNaBateria × eficiência
    /// Entregue = UsoDireto + Descarga ; Demanda = Entregue + NãoAtendida
    /// </summary>
    public sealed class TurnReport
    {
        public int TurnIndex { get; internal set; }
        public double DurationHours { get; internal set; }
        public double IlluminationFactor { get; internal set; }

        public double SolarPowerKw { get; internal set; }
        public double GenerationKwh { get; internal set; }
        public double DemandKwh { get; internal set; }
        public double DeliveredKwh { get; internal set; }
        public double UnmetKwh { get; internal set; }

        public double DirectSolarUseKwh { get; internal set; }
        public double BatteryDischargeKwh { get; internal set; }
        public double BatteryChargeInputKwh { get; internal set; }
        public double ChargeLossKwh { get; internal set; }
        public double CurtailedKwh { get; internal set; }

        public double BatteryStartKwh { get; internal set; }
        public double BatteryEndKwh { get; internal set; }

        public double LifeSupportUnmetKwh { get; internal set; }
        public double ResearchHoursGained { get; internal set; }

        public IReadOnlyList<EquipmentSupply> Supplies { get; internal set; }

        /// <summary>Resultado da missão após aplicar este turno.</summary>
        public MissionOutcome OutcomeAfter { get; internal set; }

        public bool LifeSupportDeficit => LifeSupportUnmetKwh > SimMath.EnergyEpsilonKwh;

        /// <summary>Saldo líquido da bateria no turno (kWh; negativo = descarregou).</summary>
        public double BatteryNetKwh => BatteryEndKwh - BatteryStartKwh;
    }
}
