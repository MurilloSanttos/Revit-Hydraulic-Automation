using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Sistemas
{
    /// <summary>
    /// Serviço de criação de PipingSystem para esgoto sanitário.
    /// Localiza ou cria PipingSystemType com classificação Sanitary
    /// e agrupa elementos MEP no sistema.
    ///
    /// Regras NBR 8160:
    /// - Esgoto primário: conectado a vasos e mictórios
    /// - Esgoto secundário: pias, lavatórios, tanques, ralos
    /// - Ventilação: colunas de ventilação
    /// </summary>
    public class WasteSystemCreator
    {
        private const string ETAPA = "06_Sistemas";
        private const string COMPONENTE = "WasteSystem";
        private const string NOME_SISTEMA = "Esgoto Sanitario (Auto)";

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um sistema de Esgoto Sanitário agrupando os elementos.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="elementos">Lista de ElementIds de Pipes/Fittings.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>PipingSystem criado ou null.</returns>
        public PipingSystem? CriarSistemaEsgotoSanitario(
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
                    "Falha ao criar sistema de Esgoto Sanitário: " +
                    "lista de elementos vazia.");
                return null;
            }

            // Filtrar elementos MEP válidos
            var elementosValidos = FiltrarElementosValidos(doc, elementos, log);

            if (elementosValidos.Count < 2)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema de Esgoto Sanitário: " +
                    $"mínimo 2 elementos MEP válidos necessários " +
                    $"({elementosValidos.Count} encontrados de {elementos.Count} fornecidos).");
                return null;
            }

            // ── 2. Localizar ou criar SystemType ──────────────
            var systemType = LocalizarOuCriarSystemType(doc, log);

            if (systemType == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar sistema de Esgoto Sanitário: " +
                    "PipingSystemType Sanitary não encontrado nem criado.");
                return null;
            }

            // ── 3. Obter conectores ───────────────────────────
            var connectorSet = ObterConnectorSet(doc, elementosValidos);

            if (connectorSet.Size == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar sistema de Esgoto Sanitário: " +
                    "nenhum conector encontrado nos elementos.");
                return null;
            }

            // Obter conector base
            Connector? baseConnector = null;
            foreach (Connector c in connectorSet)
            {
                baseConnector = c;
                break;
            }

            if (baseConnector == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    "Falha ao criar sistema de Esgoto Sanitário: " +
                    "conector base não disponível.");
                return null;
            }

            // ── 4. Criar PipingSystem ─────────────────────────
            try
            {
                var pipingSystem = doc.Create.NewPipingSystem(
                    baseConnector, connectorSet, systemType);

                if (pipingSystem == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        "Falha ao criar sistema de Esgoto Sanitário: " +
                        "Revit retornou null em NewPipingSystem.");
                    return null;
                }

                log.Info(ETAPA, COMPONENTE,
                    $"Sistema de Esgoto Sanitário criado com " +
                    $"{elementosValidos.Count} elementos. " +
                    $"Id={pipingSystem.Id.Value}, " +
                    $"{connectorSet.Size} conectores.",
                    pipingSystem.Id.Value);

                return pipingSystem;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema de Esgoto Sanitário: {ex.Message}",
                    detalhes: ex.StackTrace);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO COM SUB-SISTEMAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria sistema de esgoto primário (vasos, mictórios).
        /// </summary>
        public PipingSystem? CriarSistemaEsgotoPrimario(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSubSistema(doc, elementos,
                "Esgoto Primario (Auto)", log);
        }

        /// <summary>
        /// Cria sistema de esgoto secundário (pias, lavatórios, ralos).
        /// </summary>
        public PipingSystem? CriarSistemaEsgotoSecundario(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSubSistema(doc, elementos,
                "Esgoto Secundario (Auto)", log);
        }

        /// <summary>
        /// Cria sub-sistema de esgoto com nome customizado.
        /// </summary>
        private PipingSystem? CriarSubSistema(
            Document doc,
            IList<ElementId> elementos,
            string nome,
            ILogService log)
        {
            var sistema = CriarSistemaEsgotoSanitario(doc, elementos, log);

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
        /// Localiza PipingSystemType Sanitary existente ou duplica um.
        /// </summary>
        private static PipingSystemType? LocalizarOuCriarSystemType(
            Document doc, ILogService log)
        {
            var tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .ToList();

            // Buscar por classificação Sanitary
            var sanitary = tipos.FirstOrDefault(t =>
                t.SystemClassification == MEPSystemClassification.Sanitary);

            if (sanitary != null)
            {
                log.Info(ETAPA, COMPONENTE,
                    $"PipingSystemType Sanitary encontrado: '{sanitary.Name}' " +
                    $"(Id={sanitary.Id.Value}).",
                    sanitary.Id.Value);
                return sanitary;
            }

            // Buscar por nome
            var porNome = tipos.FirstOrDefault(t =>
                t.Name.Contains("Esgoto", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Sanit", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Waste", StringComparison.OrdinalIgnoreCase) ||
                t.Name.Contains("Sewer", StringComparison.OrdinalIgnoreCase));

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
                        $"Elemento {id.Value} não encontrado no documento, ignorando.");
                    continue;
                }

                // Verificar se é MEP com conectores
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
