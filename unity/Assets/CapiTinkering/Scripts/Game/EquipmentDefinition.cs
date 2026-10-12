using CapiTinkering.Simulation;
using UnityEngine;

namespace CapiTinkering.Game
{
    /// <summary>
    /// Definição editável de um equipamento (asset). Somente leitura durante a partida:
    /// a simulação recebe uma cópia imutável via <see cref="ToSpec"/>.
    /// Massa e custo entram no marco M2 (planejamento de carga).
    /// </summary>
    [CreateAssetMenu(fileName = "Equipment_", menuName = "Capi Tinkering/Equipamento", order = 10)]
    public sealed class EquipmentDefinition : ScriptableObject
    {
        [Tooltip("Identificador estável, sem espaços. Ex.: life_support, communication, research.")]
        [SerializeField] private string id = "new_equipment";

        [Tooltip("Chave de texto do nome exibido (TextTable). Ex.: equip.research")]
        [SerializeField] private string nameKey = "equip.research";

        [SerializeField] private EquipmentRole role = EquipmentRole.Other;

        [Tooltip("Potência demandada quando ligado, em kW. PARÂMETRO DIDÁTICO/FICTÍCIO.")]
        [Min(0f)] [SerializeField] private float powerKw = 1f;

        [Tooltip("Prioridade de alimentação: 0 = atendido primeiro.")]
        [Min(0)] [SerializeField] private int priority = 1;

        [SerializeField] private bool canBeSwitchedOff = true;
        [SerializeField] private bool startsOn = true;

        public string Id => id;

        public EquipmentSpec ToSpec() =>
            new EquipmentSpec(id, nameKey, role, System.Math.Round((double)powerKw, 6), priority, canBeSwitchedOff, startsOn);

        /// <summary>Usado pelo menu do Editor para criar os dados padrão.</summary>
        public void Configure(string newId, string newNameKey, EquipmentRole newRole, float newPowerKw,
            int newPriority, bool newCanBeSwitchedOff, bool newStartsOn)
        {
            id = newId;
            nameKey = newNameKey;
            role = newRole;
            powerKw = newPowerKw;
            priority = newPriority;
            canBeSwitchedOff = newCanBeSwitchedOff;
            startsOn = newStartsOn;
        }

        private void OnValidate()
        {
            if (role == EquipmentRole.LifeSupport)
            {
                canBeSwitchedOff = false;
                startsOn = true;
            }
        }
    }
}
