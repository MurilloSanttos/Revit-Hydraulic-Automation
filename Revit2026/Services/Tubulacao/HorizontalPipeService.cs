using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Tubulacao
{
    /// <summary>
    /// Serviço de criação de tubulação horizontal (ramais).
    /// Cria segmentos horizontais entre dois pontos no mesmo plano Z,
    /// associando ao Level mais próximo.
    ///
    /// Valida horizontalidade (ΔZ ≤ 1cm) e comprimento mínimo (10mm).
    /// Suporta criação individual, em sequência (rota) e em lote.
    /// </summary>
    public class HorizontalPipeService
    {
        private const string ETAPA = "05_Tubulacao";
        private const string COMPONENTE = "HorizontalPipe";

        // Tolerância vertical: 1 cm em pés
        private static readonly double ToleranciaVertical =
            UnitUtils.ConvertToInternalUnits(10, UnitTypeId.Millimeters);

        // Comprimento mínimo: 10 mm em pés
        private static readonly double ComprimentoMinimo =
            UnitUtils.ConvertToInternalUnits(10, UnitTypeId.Millimeters);

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um ramal (tubulação horizontal) entre dois pontos.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="systemTypeId">ElementId do PipingSystemType.</param>
        /// <param name="pipeTypeId">ElementId do PipeType.</param>
        /// <param name="pontoInicial">Ponto inicial em coordenadas internas (pés).</param>
        /// <param name="pontoFinal">Ponto final em coordenadas internas (pés).</param>
        /// <param name="diametroMm">Diâmetro nominal em milímetros.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>ElementId do Pipe criado, ou InvalidElementId.</returns>
        public ElementId CriarRamal(
            Document doc,
            ElementId systemTypeId,
            ElementId pipeTypeId,
            XYZ pontoInicial,
            XYZ pontoFinal,
            double diametroMm,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar pontos ─────────────────────────────
            if (pontoInicial == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar ramal: pontoInicial é nulo.");
                return ElementId.InvalidElementId;
            }

            if (pontoFinal == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar ramal: pontoFinal é nulo.");
                return ElementId.InvalidElementId;
            }

            // ── 2. Validar pontos distintos ───────────────────
            var comprimento = pontoInicial.DistanceTo(pontoFinal);
            if (comprimento < ComprimentoMinimo)
            {
                var comprMm = UnitUtils.ConvertFromInternalUnits(
                    comprimento, UnitTypeId.Millimeters);
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar ramal: comprimento insuficiente " +
                    $"({comprMm:F1} mm). Mínimo: 10 mm.");
                return ElementId.InvalidElementId;
            }

            // ── 3. Validar horizontalidade ────────────────────
            var deltaZ = Math.Abs(pontoInicial.Z - pontoFinal.Z);
            if (deltaZ > ToleranciaVertical)
            {
                var deltaZMm = UnitUtils.ConvertFromInternalUnits(
                    deltaZ, UnitTypeId.Millimeters);
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar ramal: tubulação não é horizontal. " +
                    $"ΔZ = {deltaZMm:F1} mm (tolerância: 10 mm). " +
                    $"Use VerticalPipeService para prumadas.");
                return ElementId.InvalidElementId;
            }

            // Nivelar Z para garantir horizontalidade perfeita
            var zMedio = (pontoInicial.Z + pontoFinal.Z) / 2.0;
            var p1 = new XYZ(pontoInicial.X, pontoInicial.Y, zMedio);
            var p2 = new XYZ(pontoFinal.X, pontoFinal.Y, zMedio);

            // ── 4. Validar IDs ────────────────────────────────
            if (systemTypeId == null || systemTypeId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar ramal: SystemTypeId inválido.");
                return ElementId.InvalidElementId;
            }

            if (pipeTypeId == null || pipeTypeId == ElementId.InvalidElementId)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar ramal: PipeTypeId inválido.");
                return ElementId.InvalidElementId;
            }

            // ── 5. Buscar Level ───────────────────────────────
            var level = BuscarLevelMaisProximo(doc, zMedio);
            if (level == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar ramal: nenhum Level encontrado no modelo.");
                return ElementId.InvalidElementId;
            }

            // ── 6. Converter diâmetro ─────────────────────────
            var diametroFeet = UnitUtils.ConvertToInternalUnits(
                diametroMm, UnitTypeId.Millimeters);

            // ── 7. Criar Pipe ─────────────────────────────────
            Pipe? pipe = null;

            try
            {
                pipe = Pipe.Create(doc, systemTypeId, pipeTypeId, level.Id, p1, p2);
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar ramal: {ex.Message}",
                    detalhes: ex.StackTrace);
                return ElementId.InvalidElementId;
            }

            if (pipe == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar ramal: Revit retornou null.");
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
                    $"Erro ao definir diâmetro do ramal: {ex.Message}",
                    pipe.Id.Value);
            }

            // ── 9. Log de sucesso ─────────────────────────────
            var comprMmFinal = UnitUtils.ConvertFromInternalUnits(
                comprimento, UnitTypeId.Millimeters);

            log.Info(ETAPA, COMPONENTE,
                $"Ramal criado: Id={pipe.Id.Value}, " +
                $"Ø{diametroMm:F0} mm, L={comprMmFinal:F0} mm, " +
                $"Level='{level.Name}'",
                pipe.Id.Value);

            return pipe.Id;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM ROTA (SEQUÊNCIA DE PONTOS)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria uma sequência de ramais conectando uma lista de pontos.
        /// Cada par consecutivo gera um trecho horizontal.
        /// </summary>
        public List<ElementId> CriarRota(
            Document doc,
            ElementId systemTypeId,
            ElementId pipeTypeId,
            List<XYZ> pontos,
            double diametroMm,
            ILogService log)
        {
            var resultado = new List<ElementId>();

            if (pontos == null || pontos.Count < 2)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar rota: mínimo 2 pontos necessários.");
                return resultado;
            }

            log.Info(ETAPA, COMPONENTE,
                $"Criando rota horizontal: {pontos.Count} pontos, " +
                $"{pontos.Count - 1} trechos, Ø{diametroMm:F0} mm.");

            for (int i = 0; i < pontos.Count - 1; i++)
            {
                var id = CriarRamal(doc, systemTypeId, pipeTypeId,
                    pontos[i], pontos[i + 1], diametroMm, log);

                if (id != ElementId.InvalidElementId)
                    resultado.Add(id);
            }

            log.Info(ETAPA, COMPONENTE,
                $"Rota concluída: {resultado.Count}/{pontos.Count - 1} trechos criados.");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Dados de um ramal a ser criado.
        /// </summary>
        public class TrechoRamal
        {
            public ElementId SystemTypeId { get; set; } = ElementId.InvalidElementId;
            public ElementId PipeTypeId { get; set; } = ElementId.InvalidElementId;
            public XYZ PontoInicial { get; set; } = XYZ.Zero;
            public XYZ PontoFinal { get; set; } = XYZ.Zero;
            public double DiametroMm { get; set; }
        }

        /// <summary>
        /// Cria múltiplos ramais dentro de uma Transaction.
        /// </summary>
        public List<ElementId> CriarLote(
            Document doc,
            List<TrechoRamal> trechos,
            ILogService log)
        {
            var resultado = new List<ElementId>();

            if (trechos == null || trechos.Count == 0)
                return resultado;

            log.Info(ETAPA, COMPONENTE,
                $"Criando {trechos.Count} ramais em lote...");

            using var trans = new Transaction(doc, "Criar Ramais");

            try
            {
                trans.Start();

                foreach (var trecho in trechos)
                {
                    var id = CriarRamal(doc,
                        trecho.SystemTypeId,
                        trecho.PipeTypeId,
                        trecho.PontoInicial,
                        trecho.PontoFinal,
                        trecho.DiametroMm,
                        log);

                    if (id != ElementId.InvalidElementId)
                        resultado.Add(id);
                }

                if (resultado.Count > 0)
                {
                    trans.Commit();
                    log.Info(ETAPA, COMPONENTE,
                        $"Transaction committed: {resultado.Count}/{trechos.Count} ramais criados.");
                }
                else
                {
                    trans.RollBack();
                    log.Medio(ETAPA, COMPONENTE,
                        "Transaction rolled back — nenhum ramal criado.");
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                log.Critico(ETAPA, COMPONENTE,
                    $"Transaction de ramais falhou: {ex.Message}",
                    detalhes: ex.StackTrace);
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  BUSCA DE LEVEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca o Level cuja elevação é mais próxima do valor alvo.
        /// </summary>
        private static Level? BuscarLevelMaisProximo(Document doc, double elevacao)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => Math.Abs(l.Elevation - elevacao))
                .ToList();

            return levels.FirstOrDefault();
        }
    }
}
