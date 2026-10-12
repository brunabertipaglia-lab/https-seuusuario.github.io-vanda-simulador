using System.Collections.Generic;
using System.Globalization;

namespace CapiTinkering.Simulation
{
    public enum GameLanguage
    {
        PortugueseBrazil,
        English
    }

    /// <summary>
    /// Tabela de textos mínima (pt-BR e en). Mantida no núcleo para que testes verifiquem
    /// que toda chave existe nos dois idiomas. Pode ser trocada pelo pacote Unity Localization
    /// quando o volume de texto justificar.
    /// </summary>
    public static class TextTable
    {
        private static readonly Dictionary<string, string> Pt = new Dictionary<string, string>
        {
            ["equip.life_support"] = "Suporte de vida",
            ["equip.communication"] = "Comunicação",
            ["equip.research"] = "Módulo de pesquisa",

            ["status.on"] = "LIGADO",
            ["status.off"] = "DESLIGADO",
            ["status.partial"] = "ENERGIA PARCIAL",
            ["status.unpowered"] = "SEM ENERGIA",
            ["status.locked"] = "(não pode ser desligado)",

            ["severity.info"] = "[i] Informação",
            ["severity.warning"] = "[!] Atenção",
            ["severity.critical"] = "[!!] Crítico",
            ["severity.success"] = "[ok] Sucesso",

            ["outcome.in_progress"] = "Missão em andamento",
            ["outcome.objective_achieved"] = "Missão concluída: objetivo científico atingido",
            ["outcome.completed_not_met"] = "Missão concluída: objetivo científico NÃO atingido",
            ["outcome.critical"] = "Missão encerrada: condição crítica",

            ["capi.intro"] =
                "Olá! Sou o Capi. Cada turno representa {0} h. Potência (kW) indica o ritmo de uso; energia (kWh) indica a quantidade. " +
                "Exemplo: {1} kW durante {0} h = {2} kWh. Observe a previsão antes de avançar.",
            ["capi.ls_deficit"] =
                "O suporte de vida recebeu apenas {0}% da energia necessária neste turno. Mais {1} turno(s) assim encerram a missão. " +
                "Quais sistemas podem esperar?",
            ["capi.projected_ls_deficit"] =
                "Mantendo a configuração atual, minha projeção indica falta de energia para o suporte de vida no turno {0}. " +
                "O que acontece com a bateria quando o Sol some?",
            ["capi.projected_ls_deficit_hint"] =
                "Dica: a prioridade protege o suporte de vida DENTRO de um turno, mas não guarda energia para os turnos seguintes. " +
                "Compare a demanda total com o que a bateria pode entregar até o Sol voltar (turno {0}).",
            ["capi.curtailed"] =
                "Bateria cheia: {0} kWh de energia solar foram desperdiçados neste turno. Algum sistema desligado poderia usar essa energia?",
            ["capi.research_unreachable"] =
                "Mesmo com pesquisa em potência total em todos os turnos restantes, faltariam {0} h para a meta. " +
                "Registre o que aprendeu e compare com uma nova estratégia no reinício.",
            ["capi.research_behind"] =
                "Pesquisa acumulada: {0} h de {1} h. Restam {2} turno(s); são necessárias {3} h por turno, de no máximo {4} h.",
            ["capi.balance_positive"] =
                "Balanço do último turno: geração {0} kWh, consumo atendido {1} kWh. A bateria ganhou {2} kWh.",
            ["capi.balance_negative"] =
                "Balanço do último turno: geração {0} kWh, consumo atendido {1} kWh. A bateria perdeu {2} kWh.",
            ["capi.end.achieved"] =
                "Missão cumprida! A tripulação ficou segura e a pesquisa somou {0} h (meta {1} h). Que escolha fez mais diferença?",
            ["capi.end.achieved_with_risk"] =
                "A pesquisa somou {0} h e a meta foi atingida, mas o suporte de vida ficou sem energia completa em {1} turno(s). " +
                "A tripulação correu risco. Existe uma estratégia que cumpra a meta sem esse risco?",
            ["capi.reserve_for_shadow"] =
                "A bateria está cheia e {0} kWh foram desperdiçados, mas a sombra se aproxima: religar tudo agora levaria a falta de energia adiante. " +
                "Quanto da reserva você quer guardar para os turnos sem Sol?",
            ["capi.end.not_met"] =
                "A tripulação ficou segura, mas a pesquisa somou {0} h de {1} h. Proteger demais também tem custo. " +
                "Em que turnos a pesquisa poderia ter ficado ligada?",
            ["capi.end.critical"] =
                "O suporte de vida ficou sem energia suficiente por {0} turnos seguidos e a missão foi encerrada. " +
                "Isso não é um fracasso seu: é informação. Olhe o histórico e tente outra estratégia.",

            ["hud.turn"] = "Turno {0}/{1} · tempo simulado: dia {2}, {3} h (turno = {4} h)",
            ["hud.objective"] = "Objetivo: {0} h de pesquisa · progresso: {1} h ({2}%)",
            ["hud.illumination"] = "Iluminação solar neste turno: {0}% → {1} kW",
            ["hud.battery"] = "Bateria: {0} / {1} kWh ({2}%) · reserva mínima protegida: {3} kWh",
            ["hud.flow"] = "Último turno: geração {0} kWh · demanda {1} kWh · atendido {2} kWh · não atendido {3} kWh · desperdiçado {4} kWh",
            ["hud.flow_none"] = "Nenhum turno simulado ainda.",
            ["hud.system_line"] = "{0}: {1} · {2} kW → {3} kWh/turno",
            ["hud.forecast"] =
                "Previsão do próximo turno (iluminação {0}%): geração {1} kWh, demanda {2} kWh, bateria → {3} kWh{4}",
            ["hud.forecast_ls_risk"] = " · RISCO: suporte de vida sem energia completa",
            ["hud.forecast_note"] = "Previsão determinística com o perfil de iluminação conhecido; eventos aleatórios ainda não modelados.",
            ["hud.button_advance"] = "Avançar turno",
            ["hud.button_restart"] = "Reiniciar missão",
            ["hud.button_turn_off"] = "Desligar {0}",
            ["hud.button_turn_on"] = "Ligar {0}",
            ["hud.button_language"] = "Idioma: Português",
            ["hud.button_motion_reduced"] = "Movimento reduzido: SIM",
            ["hud.button_motion_full"] = "Movimento reduzido: NÃO",
            ["hud.capi_name"] = "Capi",
            ["hud.title"] = "{0} · Missão Lunar — Etapa 1: Energia"
        };

        private static readonly Dictionary<string, string> En = new Dictionary<string, string>
        {
            ["equip.life_support"] = "Life support",
            ["equip.communication"] = "Communication",
            ["equip.research"] = "Research module",

            ["status.on"] = "ON",
            ["status.off"] = "OFF",
            ["status.partial"] = "PARTIAL POWER",
            ["status.unpowered"] = "NO POWER",
            ["status.locked"] = "(cannot be switched off)",

            ["severity.info"] = "[i] Info",
            ["severity.warning"] = "[!] Warning",
            ["severity.critical"] = "[!!] Critical",
            ["severity.success"] = "[ok] Success",

            ["outcome.in_progress"] = "Mission in progress",
            ["outcome.objective_achieved"] = "Mission complete: science objective achieved",
            ["outcome.completed_not_met"] = "Mission complete: science objective NOT achieved",
            ["outcome.critical"] = "Mission ended: critical condition",

            ["capi.intro"] =
                "Hi! I'm Capi. Each turn represents {0} h. Power (kW) is the rate of use; energy (kWh) is the amount. " +
                "Example: {1} kW for {0} h = {2} kWh. Check the forecast before advancing.",
            ["capi.ls_deficit"] =
                "Life support received only {0}% of the energy it needed this turn. {1} more turn(s) like this will end the mission. " +
                "Which systems can wait?",
            ["capi.projected_ls_deficit"] =
                "With the current setup, my projection shows life support running short of energy on turn {0}. " +
                "What happens to the battery when the Sun is gone?",
            ["capi.projected_ls_deficit_hint"] =
                "Hint: priority protects life support WITHIN a turn, but it does not save energy for later turns. " +
                "Compare total demand with what the battery can deliver until sunlight returns (turn {0}).",
            ["capi.curtailed"] =
                "Battery full: {0} kWh of solar energy were wasted this turn. Could a switched-off system use that energy?",
            ["capi.research_unreachable"] =
                "Even with full research power on every remaining turn, you would be {0} h short of the goal. " +
                "Note what you learned and compare with a new strategy after restarting.",
            ["capi.research_behind"] =
                "Research so far: {0} h of {1} h. {2} turn(s) left; you need {3} h per turn, out of at most {4} h.",
            ["capi.balance_positive"] =
                "Last turn balance: generation {0} kWh, consumption supplied {1} kWh. The battery gained {2} kWh.",
            ["capi.balance_negative"] =
                "Last turn balance: generation {0} kWh, consumption supplied {1} kWh. The battery lost {2} kWh.",
            ["capi.end.achieved"] =
                "Mission accomplished! The crew stayed safe and research reached {0} h (goal {1} h). Which choice mattered most?",
            ["capi.end.achieved_with_risk"] =
                "Research reached {0} h and the goal was met, but life support was not fully powered on {1} turn(s). " +
                "The crew was at risk. Is there a strategy that meets the goal without that risk?",
            ["capi.reserve_for_shadow"] =
                "The battery is full and {0} kWh were wasted, but the shadow is coming: switching everything back on now would cause a shortage later. " +
                "How much reserve do you want to keep for the turns without sunlight?",
            ["capi.end.not_met"] =
                "The crew stayed safe, but research reached {0} h of {1} h. Over-protecting also has a cost. " +
                "On which turns could research have stayed on?",
            ["capi.end.critical"] =
                "Life support lacked enough energy for {0} turns in a row and the mission was ended. " +
                "That is not a failure on your part: it is information. Check the history and try another strategy.",

            ["hud.turn"] = "Turn {0}/{1} · simulated time: day {2}, {3} h (turn = {4} h)",
            ["hud.objective"] = "Objective: {0} h of research · progress: {1} h ({2}%)",
            ["hud.illumination"] = "Sunlight this turn: {0}% → {1} kW",
            ["hud.battery"] = "Battery: {0} / {1} kWh ({2}%) · protected minimum reserve: {3} kWh",
            ["hud.flow"] = "Last turn: generation {0} kWh · demand {1} kWh · supplied {2} kWh · unmet {3} kWh · wasted {4} kWh",
            ["hud.flow_none"] = "No turn simulated yet.",
            ["hud.system_line"] = "{0}: {1} · {2} kW → {3} kWh/turn",
            ["hud.forecast"] =
                "Next turn forecast (sunlight {0}%): generation {1} kWh, demand {2} kWh, battery → {3} kWh{4}",
            ["hud.forecast_ls_risk"] = " · RISK: life support not fully powered",
            ["hud.forecast_note"] = "Deterministic forecast using the known sunlight profile; random events are not modeled yet.",
            ["hud.button_advance"] = "Advance turn",
            ["hud.button_restart"] = "Restart mission",
            ["hud.button_turn_off"] = "Turn off {0}",
            ["hud.button_turn_on"] = "Turn on {0}",
            ["hud.button_language"] = "Language: English",
            ["hud.button_motion_reduced"] = "Reduced motion: YES",
            ["hud.button_motion_full"] = "Reduced motion: NO",
            ["hud.capi_name"] = "Capi",
            ["hud.title"] = "{0} · Lunar Mission — Stage 1: Power"
        };

        /// <summary>
        /// Nome da equipe exibido no jogo. ÚNICO ponto de troca: o logo oficial grafa "CAPY TINKERING",
        /// enquanto os textos de planejamento usam "CAPI TINKERING" (decisão pendente da equipe).
        /// </summary>
        public const string TeamName = "CAPI TINKERING";

        public static GameLanguage Language { get; set; } = GameLanguage.PortugueseBrazil;

        public static IEnumerable<string> PortugueseKeys => Pt.Keys;
        public static IEnumerable<string> EnglishKeys => En.Keys;

        public static CultureInfo Culture =>
            Language == GameLanguage.English ? SafeCulture("en-US") : SafeCulture("pt-BR");

        // Algumas plataformas (ex.: builds com globalização invariante) não trazem dados de cultura.
        private static CultureInfo SafeCulture(string name)
        {
            try { return CultureInfo.GetCultureInfo(name); }
            catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
        }

        public static bool Has(string key, GameLanguage language) =>
            (language == GameLanguage.English ? En : Pt).ContainsKey(key);

        /// <summary>Retorna o texto no idioma atual; chave ausente aparece entre colchetes para ser notada nos testes de jogo.</summary>
        public static string Get(string key, params object[] args)
        {
            var table = Language == GameLanguage.English ? En : Pt;
            if (!table.TryGetValue(key, out var template)) return $"[{key}]";
            return args == null || args.Length == 0 ? template : string.Format(Culture, template, args);
        }

        /// <summary>Formata número com casas decimais fixas na cultura atual.</summary>
        public static string Num(double value, int decimals = 1) =>
            value.ToString("F" + decimals, Culture);
    }
}
