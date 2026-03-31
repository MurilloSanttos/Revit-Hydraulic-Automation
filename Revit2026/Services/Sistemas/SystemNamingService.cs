using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Sistemas
{
    /// <summary>
    /// Serviço de nomenclatura padronizada de sistemas MEP.
    /// Aplica padrão: MEP-[TIPO]-[NÍVEL]-[XX]
    ///
    /// Exemplos:
    /// - MEP-AF-Terreo-01
    /// - MEP-ES-PavTipo-02
    /// - MEP-VT-Cobertura-01
    /// - MEP-AQ-Terreo-03
    /// </summary>
    public class SystemNamingService
    {
        private const string ETAPA = "06_Sistemas";
        private const string COMPONENTE = "SystemNaming";
        private const string PREFIXO = "MEP";

        // Mapeamento de classificação → sigla e nome legível
        private static readonly Dictionary<MEPSystemClassification, (string Sigla, string Nome)>
            MapaTipos = new()
            {
                { MEPSystemClassification.DomesticColdWater, ("AF", "AguaFria") },
                { MEPSystemClassification.DomesticHotWater, ("AQ", "AguaQuente") },
                { MEPSystemClassification.Sanitary, ("ES", "Esgoto") },
                { MEPSystemClassification.Vent, ("VT", "Ventilacao") },
                { MEPSystemClassification.Storm, ("AP", "AguaPluvial") },
                { MEPSystemClassification.FireProtectWet, ("IC", "Incendio") },
            };

        // ══════════════════════════════════════════════════════════
        //  NOMENCLATURA EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica nomenclatura padronizada a uma lista de sistemas MEP.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public void AplicarNomenclaturaPadrao(
            Document doc,
            IList<MEPSystem> sistemas,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            if (sistemas == null || sistemas.Count == 0)
            {
                log.Medio(ETAPA, COMPONENTE,
                    "Nenhum sistema fornecido para nomenclatura.");
                return;
            }

            var contadorPorTipo = new Dictionary<string, int>();
            int nomeados = 0;
            int ignorados = 0;

            log.Info(ETAPA, COMPONENTE,
                $"Aplicando nomenclatura padronizada a {sistemas.Count} sistemas...");

            foreach (var sistema in sistemas)
            {
                if (sistema == null)
                {
                    ignorados++;
                    continue;
                }

                var resultado = AplicarNome(doc, sistema, contadorPorTipo, log);

                if (resultado)
                    nomeados++;
                else
                    ignorados++;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Nomenclatura concluída: {nomeados} nomeados, " +
                $"{ignorados} ignorados.");
        }

        // ══════════════════════════════════════════════════════════
        //  NOMENCLATURA PARA PIPINGSYSTEMS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica nomenclatura padronizada a todos os PipingSystems do modelo.
        /// </summary>
        public int AplicarNomenclaturaGlobal(
            Document doc,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));

            var sistemas = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystem))
                .Cast<PipingSystem>()
                .ToList();

            if (sistemas.Count == 0)
            {
                log.Medio(ETAPA, COMPONENTE,
                    "Nenhum PipingSystem encontrado no modelo.");
                return 0;
            }

            var contadorPorTipo = new Dictionary<string, int>();
            int nomeados = 0;

            log.Info(ETAPA, COMPONENTE,
                $"Nomenclatura global: {sistemas.Count} PipingSystems...");

            // Ordenar por tipo e depois por Level para consistência
            var ordenados = sistemas
                .OrderBy(s => ObterSigla(s))
                .ThenBy(s => ObterNomeLevel(doc, s))
                .ToList();

            foreach (var sistema in ordenados)
            {
                if (AplicarNome(doc, sistema, contadorPorTipo, log))
                    nomeados++;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Nomenclatura global concluída: {nomeados}/{sistemas.Count} nomeados.");

            return nomeados;
        }

        // ══════════════════════════════════════════════════════════
        //  LÓGICA DE NOMENCLATURA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica nome padronizado a um sistema individual.
        /// </summary>
        private bool AplicarNome(
            Document doc,
            MEPSystem sistema,
            Dictionary<string, int> contadorPorTipo,
            ILogService log)
        {
            try
            {
                // ── 1. Identificar tipo ───────────────────────
                var sigla = ObterSigla(sistema);

                if (string.IsNullOrEmpty(sigla))
                {
                    log.Info(ETAPA, COMPONENTE,
                        $"Sistema '{sistema.Name}' (Id={sistema.Id.Value}) " +
                        $"ignorado: tipo não mapeado.",
                        sistema.Id.Value);
                    return false;
                }

                // ── 2. Obter Level ────────────────────────────
                var nomeLevel = ObterNomeLevel(doc, sistema);

                if (nomeLevel == "SemNivel")
                {
                    log.Leve(ETAPA, COMPONENTE,
                        $"Sistema '{sistema.Name}' sem Level identificável. " +
                        $"Usando 'SemNivel'.",
                        sistema.Id.Value);
                }

                // ── 3. Incrementar índice ─────────────────────
                var chaveTipo = $"{sigla}-{nomeLevel}";

                if (!contadorPorTipo.ContainsKey(chaveTipo))
                    contadorPorTipo[chaveTipo] = 0;

                contadorPorTipo[chaveTipo]++;
                var indice = contadorPorTipo[chaveTipo];

                // ── 4. Compor nome ────────────────────────────
                var nomeFinal = $"{PREFIXO}-{sigla}-{SanitizarNome(nomeLevel)}-{indice:D2}";

                // ── 5. Aplicar ────────────────────────────────
                var nomeAnterior = sistema.Name;

                var paramName = sistema.get_Parameter(
                    BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (paramName != null && !paramName.IsReadOnly)
                {
                    paramName.Set(nomeFinal);
                }

                // Tentar via RBS_SYSTEM_NAME_PARAM
                var paramSysName = sistema.get_Parameter(
                    BuiltInParameter.RBS_SYSTEM_NAME_PARAM);
                if (paramSysName != null && !paramSysName.IsReadOnly)
                {
                    paramSysName.Set(nomeFinal);
                }

                // Tentar via RBS_SYSTEM_ABBREVIATION_PARAM
                var paramAbrev = sistema.get_Parameter(
                    BuiltInParameter.RBS_SYSTEM_ABBREVIATION_PARAM);
                if (paramAbrev != null && !paramAbrev.IsReadOnly)
                {
                    paramAbrev.Set(sigla);
                }

                log.Info(ETAPA, COMPONENTE,
                    $"Nome aplicado ao sistema {sistema.Id.Value}: " +
                    $"'{nomeFinal}' (anterior: '{nomeAnterior}')",
                    sistema.Id.Value);

                return true;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao nomear sistema {sistema.Id.Value}: {ex.Message}",
                    sistema.Id.Value,
                    detalhes: ex.StackTrace);
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  MAPEAMENTO DE TIPO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém a sigla do tipo de sistema.
        /// </summary>
        private static string ObterSigla(MEPSystem sistema)
        {
            try
            {
                if (sistema is PipingSystem ps)
                {
                    var systemType = ps.Document.GetElement(ps.GetTypeId())
                        as PipingSystemType;

                    if (systemType != null &&
                        MapaTipos.TryGetValue(
                            systemType.SystemClassification, out var info))
                    {
                        return info.Sigla;
                    }

                    // Fallback por nome do sistema
                    return ClassificarPorNome(sistema.Name);
                }
            }
            catch { /* fallback abaixo */ }

            return ClassificarPorNome(sistema.Name);
        }

        /// <summary>
        /// Classificação por nome quando SystemClassification não está disponível.
        /// </summary>
        private static string ClassificarPorNome(string nome)
        {
            if (string.IsNullOrEmpty(nome)) return "";

            var lower = nome.ToLowerInvariant();

            if (lower.Contains("fria") || lower.Contains("cold"))
                return "AF";
            if (lower.Contains("quente") || lower.Contains("hot"))
                return "AQ";
            if (lower.Contains("esgoto") || lower.Contains("sanit") ||
                lower.Contains("waste") || lower.Contains("sewer"))
                return "ES";
            if (lower.Contains("vent"))
                return "VT";
            if (lower.Contains("pluv") || lower.Contains("rain") ||
                lower.Contains("storm"))
                return "AP";
            if (lower.Contains("incendio") || lower.Contains("fire"))
                return "IC";

            return "";
        }

        // ══════════════════════════════════════════════════════════
        //  LEVEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o nome do Level principal do sistema.
        /// </summary>
        private static string ObterNomeLevel(Document doc, MEPSystem sistema)
        {
            try
            {
                var elements = sistema.Elements;
                if (elements == null) return "SemNivel";

                // Contar ocorrências de cada Level
                var levelCount = new Dictionary<ElementId, int>();

                foreach (Element elem in elements)
                {
                    if (elem == null) continue;

                    var levelId = ObterLevelId(elem);
                    if (levelId == null || levelId == ElementId.InvalidElementId)
                        continue;

                    if (!levelCount.ContainsKey(levelId))
                        levelCount[levelId] = 0;
                    levelCount[levelId]++;
                }

                if (levelCount.Count == 0)
                    return "SemNivel";

                // Level mais frequente
                var levelPrincipal = levelCount
                    .OrderByDescending(kv => kv.Value)
                    .First().Key;

                var level = doc.GetElement(levelPrincipal) as Level;
                return level?.Name ?? "SemNivel";
            }
            catch
            {
                return "SemNivel";
            }
        }

        /// <summary>
        /// Obtém LevelId de um elemento.
        /// </summary>
        private static ElementId? ObterLevelId(Element elem)
        {
            try
            {
                // Propriedade direta
                var levelId = elem.LevelId;
                if (levelId != null && levelId != ElementId.InvalidElementId)
                    return levelId;

                // Parâmetro INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM
                var paramLevel = elem.get_Parameter(
                    BuiltInParameter.INSTANCE_SCHEDULE_ONLY_LEVEL_PARAM);
                if (paramLevel != null)
                {
                    var id = paramLevel.AsElementId();
                    if (id != null && id != ElementId.InvalidElementId)
                        return id;
                }

                // Parâmetro RBS_START_LEVEL_PARAM (Pipes)
                var paramStartLevel = elem.get_Parameter(
                    BuiltInParameter.RBS_START_LEVEL_PARAM);
                if (paramStartLevel != null)
                {
                    var id = paramStartLevel.AsElementId();
                    if (id != null && id != ElementId.InvalidElementId)
                        return id;
                }
            }
            catch { /* silencioso */ }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Remove caracteres especiais e espaços do nome para uso em nomenclatura.
        /// </summary>
        private static string SanitizarNome(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return "SemNivel";

            // Substituir espaços e caracteres especiais
            var sanitizado = nome
                .Replace(" ", "")
                .Replace(".", "")
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace("(", "")
                .Replace(")", "")
                .Replace("[", "")
                .Replace("]", "")
                .Replace("{", "")
                .Replace("}", "")
                .Replace(":", "")
                .Replace(";", "")
                .Replace(",", "")
                .Replace("'", "")
                .Replace("\"", "")
                .Trim();

            // Limitar comprimento
            if (sanitizado.Length > 20)
                sanitizado = sanitizado.Substring(0, 20);

            return string.IsNullOrEmpty(sanitizado) ? "SemNivel" : sanitizado;
        }
    }
}
