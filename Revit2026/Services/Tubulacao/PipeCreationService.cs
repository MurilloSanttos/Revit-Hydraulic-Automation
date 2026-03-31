using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Tubulacao
{
    /// <summary>
    /// Serviço de criação de trechos de tubulação (Pipe) via Revit API.
    /// Cria segmentos entre dois pontos com diâmetro, sistema e tipo definidos.
    ///
    /// Suporta criação individual e em lote (com Transaction interna).
    /// Todas as conversões de unidade usam UnitUtils (ForgeTypeId).
    /// </summary>
    public class PipeCreationService
    {
        private const string ETAPA = "05_Tubulacao";
        private const string COMPONENTE = "PipeCreation";

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um trecho de tubulação entre dois pontos.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="systemTypeId">ElementId do PipingSystemType.</param>
        /// <param name="pipeTypeId">ElementId do PipeType.</param>
        /// <param name="levelId">ElementId do Level.</param>
        /// <param name="pontoInicial">Ponto inicial em coordenadas internas (pés).</param>
        /// <param name="pontoFinal">Ponto final em coordenadas internas (pés).</param>
        /// <param name="diametroMm">Diâmetro nominal em milímetros.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>ElementId do Pipe criado, ou ElementId.InvalidElementId em caso de falha.</returns>
        public ElementId CriarTrecho(
            Document doc,
            ElementId systemTypeId,
            ElementId pipeTypeId,
            ElementId levelId,
            XYZ pontoInicial,
            XYZ pontoFinal,
            double diametroMm,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validações ─────────────────────────────────
            if (systemTypeId == null || systemTypeId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar trecho de tubulação: SystemTypeId inválido.");
                return ElementId.InvalidElementId;
            }

            if (pipeTypeId == null || pipeTypeId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar trecho de tubulação: PipeTypeId inválido.");
                return ElementId.InvalidElementId;
            }

            if (levelId == null || levelId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar trecho de tubulação: LevelId inválido.");
                return ElementId.InvalidElementId;
            }

            if (pontoInicial == null || pontoFinal == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar trecho de tubulação: pontos nulos.");
                return ElementId.InvalidElementId;
            }

            // Verificar comprimento mínimo
            var comprimento = pontoInicial.DistanceTo(pontoFinal);
            var comprMinFeet = UnitUtils.ConvertToInternalUnits(
                10, UnitTypeId.Millimeters); // 10mm mínimo

            if (comprimento < comprMinFeet)
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Trecho muito curto: {ComprimentoMm(comprimento):F1} mm. " +
                    $"Mínimo: 10 mm.");
                return ElementId.InvalidElementId;
            }

            // ── 2. Converter diâmetro mm → pés ───────────────
            var diametroFeet = UnitUtils.ConvertToInternalUnits(
                diametroMm, UnitTypeId.Millimeters);

            // ── 3. Criar Pipe ─────────────────────────────────
            Pipe? pipe = null;

            try
            {
                pipe = Pipe.Create(
                    doc,
                    systemTypeId,
                    pipeTypeId,
                    levelId,
                    pontoInicial,
                    pontoFinal);
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar trecho de tubulação: {ex.Message}",
                    detalhes: ex.StackTrace);
                return ElementId.InvalidElementId;
            }

            if (pipe == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar trecho de tubulação: Revit retornou null.");
                return ElementId.InvalidElementId;
            }

            // ── 4. Aplicar diâmetro ───────────────────────────
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
                    // Fallback por nome
                    var paramNome = pipe.LookupParameter("Diameter");
                    if (paramNome != null && !paramNome.IsReadOnly)
                    {
                        paramNome.Set(diametroFeet);
                    }
                    else
                    {
                        log.Leve(ETAPA, COMPONENTE,
                            $"Parâmetro de diâmetro não encontrado no Pipe {pipe.Id.Value}. " +
                            $"Usando diâmetro padrão do tipo.",
                            pipe.Id.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao definir diâmetro do Pipe {pipe.Id.Value}: {ex.Message}",
                    pipe.Id.Value);
            }

            // ── 5. Log de sucesso ─────────────────────────────
            var comprMm = ComprimentoMm(comprimento);
            log.Info(ETAPA, COMPONENTE,
                $"Trecho de tubulação criado: Id={pipe.Id.Value}, " +
                $"Ø{diametroMm:F0} mm, L={comprMm:F0} mm",
                pipe.Id.Value);

            return pipe.Id;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dados de um trecho a ser criado.
        /// </summary>
        public class TrechoPipe
        {
            public ElementId SystemTypeId { get; set; } = ElementId.InvalidElementId;
            public ElementId PipeTypeId { get; set; } = ElementId.InvalidElementId;
            public ElementId LevelId { get; set; } = ElementId.InvalidElementId;
            public XYZ PontoInicial { get; set; } = XYZ.Zero;
            public XYZ PontoFinal { get; set; } = XYZ.Zero;
            public double DiametroMm { get; set; }
        }

        /// <summary>
        /// Resultado da criação em lote.
        /// </summary>
        public class ResultadoCriacaoLote
        {
            public int Total { get; set; }
            public int Criados { get; set; }
            public int Falhas { get; set; }
            public List<ElementId> PipeIds { get; set; } = new();
            public double ComprimentoTotalMm { get; set; }

            public override string ToString() =>
                $"{Criados}/{Total} trechos criados, " +
                $"L total={ComprimentoTotalMm:F0} mm, {Falhas} falhas";
        }

        /// <summary>
        /// Cria múltiplos trechos dentro de uma Transaction.
        /// </summary>
        public ResultadoCriacaoLote CriarLote(
            Document doc,
            List<TrechoPipe> trechos,
            ILogService log)
        {
            var resultado = new ResultadoCriacaoLote { Total = trechos.Count };

            log.Info(ETAPA, COMPONENTE,
                $"Criando {trechos.Count} trechos de tubulação...");

            using var trans = new Transaction(doc, "Criar Tubulações");

            try
            {
                trans.Start();

                foreach (var trecho in trechos)
                {
                    var pipeId = CriarTrecho(
                        doc,
                        trecho.SystemTypeId,
                        trecho.PipeTypeId,
                        trecho.LevelId,
                        trecho.PontoInicial,
                        trecho.PontoFinal,
                        trecho.DiametroMm,
                        log);

                    if (pipeId != ElementId.InvalidElementId)
                    {
                        resultado.Criados++;
                        resultado.PipeIds.Add(pipeId);
                        resultado.ComprimentoTotalMm += ComprimentoMm(
                            trecho.PontoInicial.DistanceTo(trecho.PontoFinal));
                    }
                    else
                    {
                        resultado.Falhas++;
                    }
                }

                if (resultado.Criados > 0)
                {
                    trans.Commit();
                    log.Info(ETAPA, COMPONENTE,
                        $"Transaction committed: {resultado}");
                }
                else
                {
                    trans.RollBack();
                    log.Medio(ETAPA, COMPONENTE,
                        "Transaction rolled back — nenhum trecho criado.");
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                log.Critico(ETAPA, COMPONENTE,
                    $"Transaction de criação falhou: {ex.Message}",
                    detalhes: ex.StackTrace);

                resultado.Falhas = resultado.Total;
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  BUSCA DE TIPOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca o primeiro PipingSystemType disponível no modelo.
        /// </summary>
        public static ElementId? BuscarSystemType(Document doc, string nomeContem = "")
        {
            var tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .ToList();

            if (!string.IsNullOrEmpty(nomeContem))
            {
                var filtrado = tipos.FirstOrDefault(t =>
                    t.Name.Contains(nomeContem, StringComparison.OrdinalIgnoreCase));
                if (filtrado != null)
                    return filtrado.Id;
            }

            return tipos.FirstOrDefault()?.Id;
        }

        /// <summary>
        /// Busca o primeiro PipeType disponível no modelo.
        /// </summary>
        public static ElementId? BuscarPipeType(Document doc, string nomeContem = "")
        {
            var tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(PipeType))
                .Cast<PipeType>()
                .ToList();

            if (!string.IsNullOrEmpty(nomeContem))
            {
                var filtrado = tipos.FirstOrDefault(t =>
                    t.Name.Contains(nomeContem, StringComparison.OrdinalIgnoreCase));
                if (filtrado != null)
                    return filtrado.Id;
            }

            return tipos.FirstOrDefault()?.Id;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Converte comprimento de pés para milímetros.
        /// </summary>
        private static double ComprimentoMm(double feet)
        {
            return UnitUtils.ConvertFromInternalUnits(feet, UnitTypeId.Millimeters);
        }
    }
}
