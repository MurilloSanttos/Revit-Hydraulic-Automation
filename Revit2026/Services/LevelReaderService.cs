using Autodesk.Revit.DB;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço responsável pela leitura de Levels (níveis/pavimentos) do modelo.
    /// Extrai elevação, nome e metadados de cada Level, filtra inválidos,
    /// calcula pé-direito entre pavimentos e retorna lista ordenada por elevação.
    /// </summary>
    public class LevelReaderService
    {
        private readonly ILogService _log;

        private const string ETAPA = "LevelReader";
        private const string COMPONENTE = "Leitura";
        private const string FILTRO = "FiltroLevel";

        // Elevação mínima aceitável (em metros)
        private const double ELEVACAO_MINIMA = -100.0;

        // Elevação máxima aceitável (em metros)
        private const double ELEVACAO_MAXIMA = 500.0;

        public LevelReaderService(ILogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  LEITURA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Lê todos os Levels válidos do modelo.
        /// Retorna lista ordenada por elevação crescente com pé-direito calculado.
        /// </summary>
        public List<LevelInfo> LerLevels(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            _log.Info(ETAPA, COMPONENTE, "Iniciando leitura de Levels do modelo...");

            // ── 1. Coletar todos os Levels ────────────────────
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .WhereElementIsNotElementType();

            var todosElementos = collector.ToList();

            _log.Info(ETAPA, COMPONENTE,
                $"Collector retornou {todosElementos.Count} elementos Level.");

            // ── 2. Filtrar e converter ────────────────────────
            var levels = new List<LevelInfo>();
            var nomesProcessados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int descartados = 0;

            foreach (var element in todosElementos)
            {
                if (element is not Level level)
                    continue;

                // Validar Level
                var motivo = ValidarLevel(level, nomesProcessados);
                if (motivo != null)
                {
                    descartados++;
                    _log.Medio(ETAPA, FILTRO,
                        $"Level descartado: {motivo} " +
                        $"('{level.Name}', Id={level.Id.Value})");
                    continue;
                }

                // Converter e adicionar
                var levelInfo = ConverterParaLevelInfo(level);
                levels.Add(levelInfo);
                nomesProcessados.Add(level.Name);

                _log.Info(ETAPA, COMPONENTE,
                    $"Level carregado: '{levelInfo.Nome}' " +
                    $"(Elevação={levelInfo.Elevacao:F2} m, " +
                    $"Pavimento={levelInfo.EhPavimento}, " +
                    $"Id={levelInfo.ElementId})");
            }

            // ── 3. Ordenar por elevação crescente ─────────────
            levels.Sort((a, b) => a.Elevacao.CompareTo(b.Elevacao));

            // ── 4. Calcular pé-direito entre pavimentos ───────
            CalcularPeDireito(levels);

            // ── 5. Resumo ─────────────────────────────────────
            var pavimentos = levels.Count(l => l.EhPavimento);
            var elevacaoMin = levels.Count > 0 ? levels.First().Elevacao : 0;
            var elevacaoMax = levels.Count > 0 ? levels.Last().Elevacao : 0;

            _log.Info(ETAPA, COMPONENTE,
                $"Leitura concluída: {levels.Count} Levels válidos " +
                $"({pavimentos} pavimentos), {descartados} descartados. " +
                $"Faixa de elevação: {elevacaoMin:F2} m a {elevacaoMax:F2} m.");

            return levels;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida se um Level é utilizável pelo pipeline.
        /// Retorna null se válido, ou o motivo da rejeição.
        /// </summary>
        private string? ValidarLevel(Level level, HashSet<string> nomesJaProcessados)
        {
            // V1: Nome vazio
            if (string.IsNullOrWhiteSpace(level.Name))
                return "nome vazio.";

            // V2: Nome duplicado
            if (nomesJaProcessados.Contains(level.Name))
                return $"duplicado (nome '{level.Name}' já processado).";

            // V3: Elevação extrema negativa
            var elevacaoM = ConverterComprimento(level.Elevation);
            if (elevacaoM < ELEVACAO_MINIMA)
                return $"elevação negativa extrema ({elevacaoM:F2} m).";

            // V4: Elevação extrema positiva
            if (elevacaoM > ELEVACAO_MAXIMA)
                return $"elevação positiva extrema ({elevacaoM:F2} m).";

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  CONVERSÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Converte um Level do Revit para LevelInfo do PluginCore.
        /// </summary>
        private static LevelInfo ConverterParaLevelInfo(Level level)
        {
            // Verificar se é Building Story (pavimento real)
            bool ehPavimento = false;
            try
            {
                var param = level.get_Parameter(BuiltInParameter.LEVEL_IS_BUILDING_STORY);
                if (param != null)
                    ehPavimento = param.AsInteger() == 1;
            }
            catch { /* fallback: não é pavimento */ }

            return new LevelInfo
            {
                ElementId = level.Id.Value,
                Nome = level.Name ?? string.Empty,
                Elevacao = ConverterComprimento(level.Elevation),
                EhPavimento = ehPavimento,
            };
        }

        /// <summary>
        /// Converte comprimento de unidades internas (pés) para metros.
        /// </summary>
        private static double ConverterComprimento(double valorInterno)
        {
            return UnitUtils.ConvertFromInternalUnits(valorInterno, UnitTypeId.Meters);
        }

        // ══════════════════════════════════════════════════════════
        //  PÉ-DIREITO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula o pé-direito (distância vertical) entre cada par de Levels consecutivos.
        /// O último Level recebe PeDireitoAteProximo = -1.
        /// </summary>
        private void CalcularPeDireito(List<LevelInfo> levels)
        {
            for (int i = 0; i < levels.Count - 1; i++)
            {
                var atual = levels[i];
                var proximo = levels[i + 1];
                atual.PeDireitoAteProximo = proximo.Elevacao - atual.Elevacao;

                _log.Info(ETAPA, COMPONENTE,
                    $"Pé-direito '{atual.Nome}' → '{proximo.Nome}': " +
                    $"{atual.PeDireitoAteProximo:F2} m");
            }

            // Último nível não tem próximo
            if (levels.Count > 0)
                levels[^1].PeDireitoAteProximo = -1;
        }
    }
}
