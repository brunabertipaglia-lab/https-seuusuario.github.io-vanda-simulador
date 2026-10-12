using System;

namespace CapiTinkering.Simulation
{
    /// <summary>Papel funcional de um equipamento. Define regras especiais (ex.: suporte de vida).</summary>
    public enum EquipmentRole
    {
        LifeSupport,
        Communication,
        Research,
        Other
    }

    /// <summary>
    /// Definição imutável de um consumidor elétrico, já convertida dos ScriptableObjects.
    /// Não guarda estado da partida.
    /// </summary>
    public sealed class EquipmentSpec
    {
        /// <summary>Identificador estável (usado em decisões, testes e localização).</summary>
        public string Id { get; }

        /// <summary>Chave de texto para o nome exibido (ver <see cref="TextTable"/>).</summary>
        public string NameKey { get; }

        public EquipmentRole Role { get; }

        /// <summary>Potência demandada quando ligado, em kW.</summary>
        public double PowerKw { get; }

        /// <summary>Prioridade de alimentação: menor número = atendido primeiro.</summary>
        public int Priority { get; }

        /// <summary>Se o jogador pode desligar o equipamento.</summary>
        public bool CanBeSwitchedOff { get; }

        /// <summary>Estado inicial ao começar a missão.</summary>
        public bool StartsOn { get; }

        public EquipmentSpec(
            string id,
            string nameKey,
            EquipmentRole role,
            double powerKw,
            int priority,
            bool canBeSwitchedOff,
            bool startsOn)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("O id do equipamento é obrigatório.", nameof(id));
            if (!SimMath.IsFiniteNonNegative(powerKw))
                throw new ArgumentOutOfRangeException(nameof(powerKw), powerKw,
                    $"Potência de '{id}' deve ser finita e >= 0 kW.");
            if (priority < 0)
                throw new ArgumentOutOfRangeException(nameof(priority), priority,
                    $"Prioridade de '{id}' deve ser >= 0.");
            if (role == EquipmentRole.LifeSupport && canBeSwitchedOff)
                throw new ArgumentException(
                    $"'{id}' é suporte de vida e não pode ser desligável no protótipo.", nameof(canBeSwitchedOff));
            if (!canBeSwitchedOff && !startsOn)
                throw new ArgumentException(
                    $"'{id}' não é desligável, portanto precisa começar ligado.", nameof(startsOn));

            Id = id;
            NameKey = string.IsNullOrWhiteSpace(nameKey) ? id : nameKey;
            Role = role;
            PowerKw = powerKw;
            Priority = priority;
            CanBeSwitchedOff = canBeSwitchedOff;
            StartsOn = startsOn;
        }
    }
}
