using CapiTinkering.Simulation;
using UnityEngine;

namespace CapiTinkering.Game
{
    /// <summary>
    /// Cores da identidade CAPI TINKERING (azul-marinho, branco, turquesa, laranja).
    /// Valores provisórios: substituir pelos códigos oficiais do manual de marca da equipe.
    /// A cor nunca é o único sinal: todo alerta também tem rótulo textual ([!], [!!], [ok]).
    /// </summary>
    public static class BrandPalette
    {
        public static readonly Color Navy = Hex(0x0B, 0x1F, 0x3A);
        public static readonly Color NavyLight = Hex(0x16, 0x34, 0x5C);
        public static readonly Color White = Color.white;
        public static readonly Color Turquoise = Hex(0x1F, 0xC7, 0xB6);
        public static readonly Color Orange = Hex(0xFF, 0x8A, 0x3D);
        public static readonly Color Critical = Hex(0xE5, 0x48, 0x4D);
        public static readonly Color Muted = Hex(0x8A, 0x9B, 0xB0);

        public static Color ForSeverity(CapiSeverity severity)
        {
            switch (severity)
            {
                case CapiSeverity.Warning: return Orange;
                case CapiSeverity.Critical: return Critical;
                case CapiSeverity.Success: return Turquoise;
                default: return NavyLight;
            }
        }

        private static Color Hex(byte r, byte g, byte b) => new Color32(r, g, b, 255);
    }

    /// <summary>Preferências de acessibilidade em memória (persistência local entra em marco posterior).</summary>
    public static class AccessibilityOptions
    {
        public static event System.Action Changed;

        private static bool _reduceMotion;

        public static bool ReduceMotion
        {
            get => _reduceMotion;
            set
            {
                if (_reduceMotion == value) return;
                _reduceMotion = value;
                Changed?.Invoke();
            }
        }
    }
}
