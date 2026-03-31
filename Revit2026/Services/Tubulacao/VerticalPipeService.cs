using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Tubulacao
{
    /// <summary>
    /// Serviço de criação de tubulação vertical (prumadas).
    /// Cria segmentos verticais entre dois níveis do edifício,
    /// associando ao Level mais próximo do ponto base.
    ///
    /// Suporta criação individual e em lote com Transaction interna.
    /// </summary>
    public class VerticalPipeService
    {
        private const string ETAPA = "05_Tubulacao";
        private const string COMPONENTE = "VerticalPipe";

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria uma prumada (tubulação vertical) entre pontoBase e pontoTopo.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="systemTypeId">ElementId do PipingSystemType.</param>
        /// <param name="pipeTypeId">ElementId do PipeType.</param>
        /// <param name="pontoBase">Ponto inferior da prumada (pés).</param>
        /// <param name="pontoTopo">Ponto superior da prumada (pés).</param>
        /// <param name="diametroMm">Diâmetro nominal em milímetros.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>ElementId do Pipe criado, ou InvalidElementId.</returns>
        public ElementId CriarPrumada(
            Document doc,
            ElementId systemTypeId,
            ElementId pipeTypeId,
            XYZ pontoBase,
            XYZ pontoTopo,
            double diametroMm,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar pontos ─────────────────────────────
            if (pontoBase == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar prumada: pontoBase é nulo.");
                return ElementId.InvalidElementId;
            }

            if (pontoTopo == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar prumada: pontoTopo é nulo.");
                return ElementId.InvalidElementId;
            }

            // ── 2. Validar verticalidade ──────────────────────
            if (pontoBase.Z >= pontoTopo.Z)
            {
                var alturaM = UnitUtils.ConvertFromInternalUnits(
                    pontoTopo.Z - pontoBase.Z, UnitTypeId.Meters);
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar prumada: pontoBase.Z ({pontoBase.Z:F3}) " +
                    $">= pontoTopo.Z ({pontoTopo.Z:F3}). " +
                    $"ΔZ = {alturaM:F3} m. Prumada deve ser vertical para cima.");
                return ElementId.InvalidElementId;
            }

            // Garantir que X e Y sejam idênticos (prumada vertical pura)
            var pontoBaseFinal = pontoBase;
            var pontoTopoFinal = new XYZ(pontoBase.X, pontoBase.Y, pontoTopo.Z);

            if (pontoBase.X != pontoTopo.X || pontoBase.Y != pontoTopo.Y)
            {
                log.Leve(ETAPA, COMPONENTE,
                    "Pontos base e topo não estão alinhados em X/Y. " +
                    "Usando X/Y do ponto base para garantir verticalidade.");
            }

            // ── 3. Validar IDs ────────────────────────────────
            if (systemTypeId == null || systemTypeId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar prumada: SystemTypeId inválido.");
                return ElementId.InvalidElementId;
            }

            if (pipeTypeId == null || pipeTypeId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar prumada: PipeTypeId inválido.");
                return ElementId.InvalidElementId;
            }

            // ── 4. Buscar Level ───────────────────────────────
            var level = BuscarLevelMaisProximo(doc, pontoBaseFinal.Z);

            if (level == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar prumada: nenhum Level encontrado no modelo.");
                return ElementId.InvalidElementId;
            }

            // ── 5. Converter diâmetro ─────────────────────────
            var diametroFeet = UnitUtils.ConvertToInternalUnits(
                diametroMm, UnitTypeId.Millimeters);

            // ── 6. Calcular altura ────────────────────────────
            var alturaFeet = pontoTopoFinal.Z - pontoBaseFinal.Z;
            var alturaMetros = UnitUtils.ConvertFromInternalUnits(
                alturaFeet, UnitTypeId.Meters);

            // ── 7. Criar Pipe ─────────────────────────────────
            Pipe? pipe = null;

            try
            {
                pipe = Pipe.Create(
                    doc,
                    systemTypeId,
                    pipeTypeId,
                    level.Id,
                    pontoBaseFinal,
                    pontoTopoFinal);
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar prumada: {ex.Message}",
                    detalhes: ex.StackTrace);
                return ElementId.InvalidElementId;
            }

            if (pipe == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar prumada: Revit retornou null.");
                return ElementId.InvalidElementId;
            }

            // ── 8. Aplicar diâmetro ───────────────────────────
            try
            {
                var paramDiam = pipe.get_Parameter(
                    BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);

                if (paramDiam != null && !paramDiam.IsReadOnly)
                {
                    paramDiam.Set(diametroFeet);
                }
                else
                {
                    var paramNome = pipe.LookupParameter("Diameter");
                    if (paramNome != null && !paramNome.IsReadOnly)
                        paramNome.Set(diametroFeet);
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao definir diâmetro da prumada: {ex.Message}",
                    pipe.Id.Value);
            }

            // ── 9. Marcar como prumada via Comments ───────────
            try
            {
                var comments = pipe.get_Parameter(
                    BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (comments != null && !comments.IsReadOnly)
                {
                    comments.Set($"Prumada | Ø{diametroMm:F0}mm | " +
                                 $"H={alturaMetros:F2}m | Level={level.Name}");
                }
            }
            catch { /* comments não é crítico */ }

            // ── 10. Log de sucesso ────────────────────────────
            log.Info(ETAPA, COMPONENTE,
                $"Prumada criada: Id={pipe.Id.Value}, " +
                $"Ø{diametroMm:F0} mm, H={alturaMetros:F2} m, " +
                $"Level='{level.Name}' " +
                $"({pontoBaseFinal.Z:F3} → {pontoTopoFinal.Z:F3} pés)",
                pipe.Id.Value);

            return pipe.Id;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO MULTI-LEVEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria prumada atravessando múltiplos níveis, segmentada por Level.
        /// Retorna lista de ElementIds dos trechos criados.
        /// </summary>
        public List<ElementId> CriarPrumadaMultiLevel(
            Document doc,
            ElementId systemTypeId,
            ElementId pipeTypeId,
            XYZ pontoBase,
            double alturaTotal,
            double diametroMm,
            ILogService log)
        {
            var resultado = new List<ElementId>();

            if (doc == null || pontoBase == null || log == null)
                return resultado;

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            if (levels.Count == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Nenhum Level encontrado para prumada multi-level.");
                return resultado;
            }

            var zBase = pontoBase.Z;
            var zTopo = zBase + UnitUtils.ConvertToInternalUnits(
                alturaTotal, UnitTypeId.Meters);

            // Filtrar levels dentro do range
            var levelsNoRange = levels
                .Where(l => l.Elevation >= zBase && l.Elevation <= zTopo)
                .ToList();

            if (levelsNoRange.Count == 0)
            {
                // Criar trecho único
                var pontoTopo = new XYZ(pontoBase.X, pontoBase.Y, zTopo);
                var id = CriarPrumada(doc, systemTypeId, pipeTypeId,
                    pontoBase, pontoTopo, diametroMm, log);
                if (id != ElementId.InvalidElementId)
                    resultado.Add(id);
                return resultado;
            }

            // Criar trechos entre levels
            var pontosZ = new List<double> { zBase };
            pontosZ.AddRange(levelsNoRange.Select(l => l.Elevation));
            pontosZ.Add(zTopo);
            pontosZ = pontosZ.Distinct().OrderBy(z => z).ToList();

            log.Info(ETAPA, COMPONENTE,
                $"Criando prumada multi-level: {pontosZ.Count - 1} trechos, " +
                $"H total={alturaTotal:F2} m.");

            for (int i = 0; i < pontosZ.Count - 1; i++)
            {
                var pBase = new XYZ(pontoBase.X, pontoBase.Y, pontosZ[i]);
                var pTopo = new XYZ(pontoBase.X, pontoBase.Y, pontosZ[i + 1]);

                if (pBase.Z >= pTopo.Z)
                    continue;

                var id = CriarPrumada(doc, systemTypeId, pipeTypeId,
                    pBase, pTopo, diametroMm, log);

                if (id != ElementId.InvalidElementId)
                    resultado.Add(id);
            }

            log.Info(ETAPA, COMPONENTE,
                $"Prumada multi-level concluída: {resultado.Count} trechos criados.");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  BUSCA DE LEVEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca o Level cuja elevação é ≤ elevação alvo e mais próxima.
        /// Fallback: primeiro Level encontrado.
        /// </summary>
        private static Level? BuscarLevelMaisProximo(Document doc, double elevacao)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderByDescending(l => l.Elevation)
                .ToList();

            // Level com elevação ≤ alvo (mais próximo abaixo)
            var levelAbaixo = levels.FirstOrDefault(l => l.Elevation <= elevacao);

            if (levelAbaixo != null)
                return levelAbaixo;

            // Fallback: Level mais próximo (qualquer direção)
            return levels
                .OrderBy(l => Math.Abs(l.Elevation - elevacao))
                .FirstOrDefault();
        }
    }
}
