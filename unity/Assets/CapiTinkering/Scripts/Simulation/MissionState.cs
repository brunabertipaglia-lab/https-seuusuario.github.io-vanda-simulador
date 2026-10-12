using System;
using System.Collections.Generic;
using System.Linq;

namespace CapiTinkering.Simulation
{
    public enum MissionOutcome
    {
        InProgress,
        /// <summary>Todos os turnos concluídos e meta científica atingida.</summary>
        ObjectiveAchieved,
        /// <summary>Tripulação segura até o fim, mas meta científica não atingida.</summary>
        CompletedObjectiveNotMet,
        /// <summary>Encerramento por condição crítica (suporte de vida sem energia por turnos seguidos).</summary>
        CriticalFailure
    }

    /// <summary>Estado mutável de um equipamento durante a partida.</summary>
    public sealed class EquipmentState
    {
        public EquipmentSpec Spec { get; }
        public bool IsOn { get; internal set; }

        internal EquipmentState(EquipmentSpec spec, bool isOn)
        {
            Spec = spec;
            IsOn = isOn;
        }

        internal EquipmentState Clone() => new EquipmentState(Spec, IsOn);
    }

    /// <summary>Decisão do jogador registrada para o relatório e para reprodução da partida.</summary>
    public sealed class DecisionRecord
    {
        /// <summary>Turno em que a decisão foi tomada (antes de avançar esse turno).</summary>
        public int Turn { get; }
        public string EquipmentId { get; }
        public bool TurnedOn { get; }

        public DecisionRecord(int turn, string equipmentId, bool turnedOn)
        {
            Turn = turn;
            EquipmentId = equipmentId;
            TurnedOn = turnedOn;
        }
    }

    /// <summary>
    /// Estado completo de uma partida. Separado das definições: reiniciar = criar outro estado
    /// a partir dos mesmos parâmetros.
    /// </summary>
    public sealed class MissionState
    {
        private readonly List<EquipmentState> _equipment;
        private readonly List<DecisionRecord> _decisions;
        private readonly List<TurnReport> _history;

        public MissionParameters Parameters { get; }

        /// <summary>Turnos já concluídos. O próximo turno a simular tem índice igual a este valor.</summary>
        public int TurnsCompleted { get; internal set; }

        /// <summary>Energia armazenada na bateria, em kWh (nunca negativa, nunca acima da capacidade).</summary>
        public double BatteryStoredKwh { get; internal set; }

        /// <summary>Horas de pesquisa acumuladas (proporcionais à energia recebida).</summary>
        public double ResearchHours { get; internal set; }

        public int ConsecutiveLifeSupportDeficitTurns { get; internal set; }

        public int TotalLifeSupportDeficitTurns { get; internal set; }

        public MissionOutcome Outcome { get; internal set; }

        public IReadOnlyList<EquipmentState> Equipment => _equipment;
        public IReadOnlyList<DecisionRecord> Decisions => _decisions;
        public IReadOnlyList<TurnReport> History => _history;

        public bool IsFinished => Outcome != MissionOutcome.InProgress;

        /// <summary>Tempo simulado decorrido, em horas.</summary>
        public double ElapsedHours => TurnsCompleted * Parameters.TurnDurationHours;

        public double BatteryFraction =>
            Parameters.BatteryCapacityKwh > 0.0 ? BatteryStoredKwh / Parameters.BatteryCapacityKwh : 0.0;

        public static MissionState CreateInitial(MissionParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));
            return new MissionState(
                parameters,
                parameters.Equipment.Select(s => new EquipmentState(s, s.StartsOn)).ToList(),
                new List<DecisionRecord>(),
                new List<TurnReport>())
            {
                BatteryStoredKwh = parameters.BatteryInitialKwh,
                Outcome = MissionOutcome.InProgress
            };
        }

        private MissionState(
            MissionParameters parameters,
            List<EquipmentState> equipment,
            List<DecisionRecord> decisions,
            List<TurnReport> history)
        {
            Parameters = parameters;
            _equipment = equipment;
            _decisions = decisions;
            _history = history;
        }

        /// <summary>Cópia independente, usada para projeções sem alterar a partida real.</summary>
        public MissionState Clone()
        {
            return new MissionState(
                Parameters,
                _equipment.Select(e => e.Clone()).ToList(),
                new List<DecisionRecord>(_decisions),
                new List<TurnReport>(_history))
            {
                TurnsCompleted = TurnsCompleted,
                BatteryStoredKwh = BatteryStoredKwh,
                ResearchHours = ResearchHours,
                ConsecutiveLifeSupportDeficitTurns = ConsecutiveLifeSupportDeficitTurns,
                TotalLifeSupportDeficitTurns = TotalLifeSupportDeficitTurns,
                Outcome = Outcome
            };
        }

        public EquipmentState FindEquipment(string id)
        {
            var found = _equipment.FirstOrDefault(e => e.Spec.Id == id);
            if (found == null) throw new KeyNotFoundException($"Equipamento '{id}' não existe nesta missão.");
            return found;
        }

        /// <summary>
        /// Liga ou desliga um equipamento. Retorna falso (sem alterar nada) se não houver mudança.
        /// Lança exceção para ações proibidas, para que a interface nunca as ofereça.
        /// </summary>
        public bool SetEquipmentOn(string id, bool on)
        {
            if (IsFinished) throw new InvalidOperationException("A missão já terminou.");
            var equipment = FindEquipment(id);
            if (equipment.IsOn == on) return false;
            if (!on && !equipment.Spec.CanBeSwitchedOff)
                throw new InvalidOperationException($"'{id}' não pode ser desligado.");

            equipment.IsOn = on;
            _decisions.Add(new DecisionRecord(TurnsCompleted, id, on));
            return true;
        }

        internal void AddHistory(TurnReport report) => _history.Add(report);
    }
}
