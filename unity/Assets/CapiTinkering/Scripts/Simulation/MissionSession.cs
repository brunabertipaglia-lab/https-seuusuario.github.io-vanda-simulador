using System;

namespace CapiTinkering.Simulation
{
    /// <summary>
    /// Ponto único de entrada das ações do jogador sobre uma partida. Mantém o estado atual,
    /// delega as regras ao <see cref="SimulationEngine"/> e avisa a interface por eventos C#.
    /// A sessão sobrevive ao reinício; o <see cref="MissionState"/> é substituído por um novo.
    /// </summary>
    public sealed class MissionSession
    {
        public MissionParameters Parameters { get; }
        public MissionState State { get; private set; }

        /// <summary>Disparado após cada turno aplicado.</summary>
        public event Action<TurnReport> TurnAdvanced;

        /// <summary>Disparado quando a missão termina (vitória, conclusão sem objetivo ou condição crítica).</summary>
        public event Action<MissionOutcome> MissionEnded;

        /// <summary>Disparado após qualquer mudança visível: turno, decisão ou reinício.</summary>
        public event Action StateChanged;

        public MissionSession(MissionParameters parameters)
        {
            Parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            State = MissionState.CreateInitial(parameters);
        }

        public bool CanAdvance => !State.IsFinished;

        public TurnReport AdvanceTurn()
        {
            var report = SimulationEngine.AdvanceTurn(State);
            TurnAdvanced?.Invoke(report);
            if (State.IsFinished) MissionEnded?.Invoke(State.Outcome);
            StateChanged?.Invoke();
            return report;
        }

        /// <summary>Previsão do próximo turno, sem alterar a partida. Nulo se a missão terminou.</summary>
        public TurnReport PreviewNextTurn() => State.IsFinished ? null : SimulationEngine.ComputeTurn(State);

        public bool SetEquipmentOn(string equipmentId, bool on)
        {
            bool changed = State.SetEquipmentOn(equipmentId, on);
            if (changed) StateChanged?.Invoke();
            return changed;
        }

        /// <summary>Recomeça a partir dos mesmos parâmetros, sem reaproveitar nada do estado anterior.</summary>
        public void Restart()
        {
            State = MissionState.CreateInitial(Parameters);
            StateChanged?.Invoke();
        }
    }
}
