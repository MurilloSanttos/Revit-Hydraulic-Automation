using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Sistemas
{
    /// <summary>
    /// Serviço de criação de PipingSystem para ventilação sanitária.
    /// Localiza ou cria PipingSystemType com classificação Vent
    /// e agrupa tubulações de ventilação no sistema.
    ///
    /// NBR 8160 — Ventilação:
    /// - Coluna de ventilação: prolonga acima da cobertura
    /// - Ventilação primária: extensão do tubo de queda
    /// - Ventilação secundária: ramal individual de ventilação
    /// </summary>
    public class VentSystemCreator
    {
        private const string ETAPA = "06_Sistemas";
        private const string COMPONENTE = "VentSystem";
        private const string NOME_SISTEMA = "Ventilacao Sanitaria (Auto)";

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um sistema de Ventilação Sanitária agrupando os elementos.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="elementos">Lista de ElementIds de Pipes/Fittings de ventilação.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>PipingSystem criado ou null.</returns>
        public PipingSystem? CriarSistemaDeVentilacaoSanitaria(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar elementos ──────────────────────────
            if (elementos == null || elementos.Count == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar sistema de Ventilação Sanitária: " +
                    "lista de elementos vazia.");
                return null;
            }

            var elementosValidos = FiltrarElementosValidos(doc, elementos, log);

            if (elementosValidos.Count < 2)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema de Ventilação Sanitária: " +
                    $"mínimo 2 elementos MEP válidos necessários " +
                    $"({elementosValidos.Count} encontrados de {elementos.Count} fornecidos).");
                return null;
            }

            // ── 2. Localizar ou criar SystemType ──────────────
            var systemType = LocalizarOuCriarSystemType(doc, log);

            if (systemType == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar sistema de Ventilação Sanitária: " +
                    "PipingSystemType Vent não encontrado nem criado.");
                return null;
            }

            // ── 3. Obter conectores ───────────────────────────
            var connectorSet = ObterConnectorSet(doc, elementosValidos);

            if (connectorSet.Size == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar sistema de Ventilação Sanitária: " +
                    "nenhum conector encontrado nos elementos.");
                return null;
            }

            Connector? baseConnector = null;
            foreach (Connector c in connectorSet)
            {
                baseConnector = c;
                break;
            }

            if (baseConnector == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar sistema de Ventilação Sanitária: " +
                    "conector base não disponível.");
                return null;
            }

            // ── 4. Criar PipingSystem ─────────────────────────
            try
            {
                var pipeSystemType = systemType.SystemClassification switch
                {
                    MEPSystemClassification.Vent => PipeSystemType.Vent,
                    _ => PipeSystemType.OtherPipe
                };
                var pipingSystem = doc.Create.NewPipingSystem(
                    baseConnector, connectorSet, pipeSystemType);

                if (pipingSystem == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        "Falha ao criar sistema de Ventilação Sanitária: " +
                        "Revit retornou null em NewPipingSystem.");
                    return null;
                }

                log.Info(ETAPA, COMPONENTE,
                    $"Sistema de Ventilação Sanitária criado com " +
                    $"{elementosValidos.Count} elementos. " +
                    $"Id={pipingSystem.Id.Value}, " +
                    $"{connectorSet.Size} conectores.",
                    pipingSystem.Id.Value);

                return pipingSystem;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema de Ventilação Sanitária: {ex.Message}",
                    detalhes: ex.StackTrace);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO COM SUB-TIPOS DE VENTILAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria sistema de ventilação primária (extensão do tubo de queda).
        /// </summary>
        public PipingSystem? CriarVentilacaoPrimaria(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSubSistema(doc, elementos,
                "Ventilacao Primaria (Auto)", log);
        }

        /// <summary>
        /// Cria sistema de ventilação secundária (ramais individuais).
        /// </summary>
        public PipingSystem? CriarVentilacaoSecundaria(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSubSistema(doc, elementos,
                "Ventilacao Secundaria (Auto)", log);
        }

        /// <summary>
        /// Cria sistema de coluna de ventilação.
        /// </summary>
        public PipingSystem? CriarColunaVentilacao(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSubSistema(doc, elementos,
                "Coluna Ventilacao (Auto)", log);
        }

        /// <summary>
        /// Cria sub-sistema com nome customizado via Comments.
        /// </summary>
        private PipingSystem? CriarSubSistema(
            Document doc,
            IList<ElementId> elementos,
            string nome,
            ILogService log)
        {
            var sistema = CriarSistemaDeVentilacaoSanitaria(doc, elementos, log);

            if (sistema != null)
            {
                try
                {
                    var comments = sistema.get_Parameter(
                        BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                    if (comments != null && !comments.IsReadOnly)
                        comments.Set(nome);
                }
                catch { /* comments não é crítico */ }
            }

            return sistema;
        }

        // ══════════════════════════════════════════════════════════
        //  SYSTEM TYPE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Localiza PipingSystemType Vent existente ou duplica um.
        /// </summary>
        private static PipingSystemType? LocalizarOuCriarSystemType(
            Document doc, ILogService log)
        {
            var tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .ToList();

            // Buscar por classificação Vent
            var vent = tipos.FirstOrDefault(t =>
                t.SystemClassification == MEPSystemClassification.Vent);

            if (vent != null)
            {
                log.Info(ETAPA, COMPONENTE,
                    $"PipingSystemType Vent encontrado: '{vent.Name}' " +
                    $"(Id={vent.Id.Value}).",
                    vent.Id.Value);
                return vent;
            }

            // Buscar por nome
            var porNome = tipos.FirstOrDefault(t =>
                t.Name.Contains("Vent", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Ventila", StringComparison.OrdinalIgnoreCase));

            if (porNome != null)
            {
                log.Info(ETAPA, COMPONENTE,
                    $"PipingSystemType por nome: '{porNome.Name}'.",
                    porNome.Id.Value);
                return porNome;
            }

            // Duplicar primeiro tipo existente
            if (tipos.Count > 0)
            {
                try
                {
                    var duplicado = tipos[0].Duplicate(NOME_SISTEMA)
                        as PipingSystemType;

                    if (duplicado != null)
                    {
                        log.Info(ETAPA, COMPONENTE,
                            $"PipingSystemType criado por duplicação: " +
                            $"'{NOME_SISTEMA}' (Id={duplicado.Id.Value}).",
                            duplicado.Id.Value);
                        return duplicado;
                    }
                }
                catch (Exception ex)
                {
                    log.Medio(ETAPA, COMPONENTE,
                        $"Erro ao duplicar PipingSystemType: {ex.Message}");
                }

                // Fallback
                log.Leve(ETAPA, COMPONENTE,
                    $"Usando PipingSystemType genérico: '{tipos[0].Name}'.");
                return tipos[0];
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  FILTRAR ELEMENTOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Filtra apenas elementos MEP válidos com conectores.
        /// </summary>
        private static List<ElementId> FiltrarElementosValidos(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            var validos = new List<ElementId>();

            foreach (var id in elementos)
            {
                if (id == null || id == ElementId.InvalidElementId)
                    continue;

                var elem = doc.GetElement(id);
                if (elem == null)
                {
                    log.Medio(ETAPA, COMPONENTE,
                        $"Elemento {id.Value} não pertence ao documento, ignorando.");
                    continue;
                }

                if (elem is MEPCurve mep)
                {
                    if (mep.ConnectorManager?.Connectors?.Size > 0)
                        validos.Add(id);
                    else
                        log.Medio(ETAPA, COMPONENTE,
                            $"MEPCurve {id.Value} sem conectores, ignorando.",
                            id.Value);
                }
                else if (elem is FamilyInstance fi && fi.MEPModel != null)
                {
                    if (fi.MEPModel.ConnectorManager?.Connectors?.Size > 0)
                        validos.Add(id);
                    else
                        log.Medio(ETAPA, COMPONENTE,
                            $"FamilyInstance {id.Value} sem conectores MEP, ignorando.",
                            id.Value);
                }
                else
                {
                    log.Medio(ETAPA, COMPONENTE,
                        $"Elemento {id.Value} ('{elem.Name}') não é MEP, ignorando.",
                        id.Value);
                }
            }

            return validos;
        }

        // ══════════════════════════════════════════════════════════
        //  CONECTORES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém ConnectorSet nativo de um conjunto de elementos.
        /// </summary>
        private static ConnectorSet ObterConnectorSet(
            Document doc,
            List<ElementId> elementIds)
        {
            var resultado = new ConnectorSet();

            foreach (var id in elementIds)
            {
                var elem = doc.GetElement(id);
                if (elem == null) continue;

                ConnectorSet? connSet = null;

                if (elem is MEPCurve mep)
                    connSet = mep.ConnectorManager?.Connectors;
                else if (elem is FamilyInstance fi)
                    connSet = fi.MEPModel?.ConnectorManager?.Connectors;

                if (connSet == null) continue;

                foreach (Connector conn in connSet)
                {
                    if (conn.ConnectorType == ConnectorType.End ||
                        conn.ConnectorType == ConnectorType.Curve)
                    {
                        resultado.Insert(conn);
                    }
                }
            }

            return resultado;
        }
    }
}
