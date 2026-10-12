using CapiTinkering.Simulation;
using UnityEngine;

namespace CapiTinkering.Game
{
    /// <summary>
    /// Cores da identidade CAPI TINKERING, extraídas do logo oficial (art/reference) por mediana
    /// de pixels por faixa de matiz, em 12/10/2026. Contrastes (WCAG 2.x) verificados:
    /// branco/Navy 16,6:1 · Cream/Navy 14,5:1 · TurquoiseGlow/Navy 12,1:1 · Orange/Navy 6,2:1
    /// · Navy/Orange 6,2:1 · branco/Critical 5,4:1. Branco sobre laranja = 2,7:1 → PROIBIDO para texto.
    /// A cor nunca é o único sinal: todo alerta também tem rótulo textual ([!], [!!], [ok]).
    /// </summary>
    public static class BrandPalette
    {
        public static readonly Color NavyDeep = Hex(0x00, 0x0E, 0x26);     // fundo do espaço no logo
        public static readonly Color Navy = Hex(0x00, 0x1D, 0x45);         // painéis
        public static readonly Color NavyLight = Hex(0x0E, 0x3B, 0x66);    // botões e destaques sobre Navy
        public static readonly Color White = Color.white;
        public static readonly Color Cream = Hex(0xFB, 0xEE, 0xD5);        // letras do logo
        public static readonly Color Turquoise = Hex(0x00, 0xF4, 0xFA);    // anel luminoso do logo
        public static readonly Color Orange = Hex(0xF2, 0x7F, 0x28);       // detalhes do traje e da ferramenta
        public static readonly Color Critical = Hex(0xC0, 0x39, 0x2B);     // não está no logo: reservado a alertas críticos
        public static readonly Color Muted = Hex(0x8F, 0xA3, 0xBF);

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

        /// <summary>Cor de texto legível sobre o fundo de cada severidade (contraste ≥ 4,5:1).</summary>
        public static Color TextOnSeverity(CapiSeverity severity)
        {
            switch (severity)
            {
                case CapiSeverity.Warning:
                case CapiSeverity.Success:
                    return Navy;
                default:
                    return White;
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
