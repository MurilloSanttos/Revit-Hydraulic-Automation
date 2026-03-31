using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using PluginCore.Interfaces;

namespace Revit2026.Services.Sistemas
{
    /// <summary>
    /// Serviço de criação de PipingSystems para sistemas hidráulicos.
    /// Localiza ou cria PipingSystemType e agrupa elementos MEP
    /// em sistemas lógicos no modelo Revit.
    ///
    /// Sistemas suportados:
    /// - Água Fria (DomesticColdWater)
    /// - Água Quente (DomesticHotWater)
    /// - Esgoto (Sanitary)
    /// - Ventilação (Vent)
    /// </summary>
    public class PipingSystemCreator
    {
        private const string ETAPA = "06_Sistemas";
        private const string COMPONENTE = "PipingSystem";

        // ══════════════════════════════════════════════════════════
        //  ÁGUA FRIA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um sistema de Água Fria agrupando os elementos informados.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public PipingSystem? CriarSistemaAguaFria(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSistema(
                doc,
                elementos,
                MEPSystemClassification.DomesticColdWater,
                "Agua Fria (Auto)",
                log);
        }

        // ══════════════════════════════════════════════════════════
        //  ÁGUA QUENTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um sistema de Água Quente agrupando os elementos informados.
        /// </summary>
        public PipingSystem? CriarSistemaAguaQuente(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSistema(
                doc,
                elementos,
                MEPSystemClassification.DomesticHotWater,
                "Agua Quente (Auto)",
                log);
        }

        // ══════════════════════════════════════════════════════════
        //  ESGOTO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um sistema de Esgoto agrupando os elementos informados.
        /// </summary>
        public PipingSystem? CriarSistemaEsgoto(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSistema(
                doc,
                elementos,
                MEPSystemClassification.Sanitary,
                "Esgoto (Auto)",
                log);
        }

        // ══════════════════════════════════════════════════════════
        //  VENTILAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um sistema de Ventilação agrupando os elementos informados.
        /// </summary>
        public PipingSystem? CriarSistemaVentilacao(
            Document doc,
            IList<ElementId> elementos,
            ILogService log)
        {
            return CriarSistema(
                doc,
                elementos,
                MEPSystemClassification.Vent,
                "Ventilacao (Auto)",
                log);
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO GENÉRICA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um PipingSystem genérico com o tipo de classificação informado.
        /// </summary>
        private PipingSystem? CriarSistema(
            Document doc,
            IList<ElementId> elementos,
            MEPSystemClassification classificacao,
            string nomeDefault,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Validar elementos ──────────────────────────
            if (elementos == null || elementos.Count == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema '{nomeDefault}': " +
                    $"lista de elementos vazia.");
                return null;
            }

            // Filtrar elementos válidos
            var elementosValidos = FiltrarElementosValidos(doc, elementos, log);

            if (elementosValidos.Count < 1)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema '{nomeDefault}': " +
                    $"nenhum elemento MEP válido na lista " +
                    $"({elementos.Count} fornecidos).");
                return null;
            }

            // ── 2. Localizar ou criar SystemType ──────────────
            var systemType = LocalizarOuCriarSystemType(
                doc, classificacao, nomeDefault, log);

            if (systemType == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema '{nomeDefault}': " +
                    $"PipingSystemType não encontrado nem criado.");
                return null;
            }

            // ── 3. Criar PipingSystem ─────────────────────────
            try
            {
                // Obter conectores dos elementos para criar o sistema
                var connectorSet = ObterConnectorSet(doc, elementosValidos);

                if (connectorSet.Size == 0)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao criar sistema '{nomeDefault}': " +
                        $"nenhum conector encontrado nos elementos.");
                    return null;
                }

                // Obter primeiro conector como base
                Connector? baseConnector = null;
                foreach (Connector c in connectorSet)
                {
                    baseConnector = c;
                    break;
                }

                if (baseConnector == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao criar sistema '{nomeDefault}': " +
                        $"conector base não disponível.");
                    return null;
                }

                // Revit 2026: NewPipingSystem uses PipeSystemType enum
                var pipeSystemType = systemType.SystemClassification switch
                {
                    MEPSystemClassification.DomesticColdWater => PipeSystemType.DomesticColdWater,
                    MEPSystemClassification.DomesticHotWater => PipeSystemType.DomesticHotWater,
                    MEPSystemClassification.Sanitary => PipeSystemType.Sanitary,
                    MEPSystemClassification.Vent => PipeSystemType.Vent,
                    _ => PipeSystemType.OtherPipe
                };
                var pipingSystem = doc.Create.NewPipingSystem(
                    baseConnector, connectorSet, pipeSystemType);

                if (pipingSystem == null)
                {
                    log.Critico(ETAPA, COMPONENTE,
                        $"Falha ao criar sistema '{nomeDefault}': " +
                        $"Revit retornou null em NewPipingSystem.");
                    return null;
                }

                log.Info(ETAPA, COMPONENTE,
                    $"Sistema de '{nomeDefault}' criado: Id={pipingSystem.Id.Value}, " +
                    $"{elementosValidos.Count} elementos, " +
                    $"{connectorSet.Size} conectores.",
                    pipingSystem.Id.Value);

                return pipingSystem;
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar sistema '{nomeDefault}': {ex.Message}",
                    detalhes: ex.StackTrace);
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SYSTEM TYPE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Localiza PipingSystemType existente pela classificação,
        /// ou duplica um existente e renomeia.
        /// </summary>
        private static PipingSystemType? LocalizarOuCriarSystemType(
            Document doc,
            MEPSystemClassification classificacao,
            string nomeDefault,
            ILogService log)
        {
            var tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .ToList();

            // Buscar por classificação
            var tipoExistente = tipos.FirstOrDefault(t =>
                t.SystemClassification == classificacao);

            if (tipoExistente != null)
            {
                log.Info(ETAPA, COMPONENTE,
                    $"PipingSystemType encontrado: '{tipoExistente.Name}' " +
                    $"(Id={tipoExistente.Id.Value}).",
                    tipoExistente.Id.Value);
                return tipoExistente;
            }

            // Buscar por nome
            var tipoPorNome = tipos.FirstOrDefault(t =>
                t.Name.Contains(nomeDefault, StringComparison.OrdinalIgnoreCase));

            if (tipoPorNome != null)
                return tipoPorNome;

            // Duplicar primeiro tipo encontrado
            if (tipos.Count > 0)
            {
                try
                {
                    var duplicado = tipos[0].Duplicate(nomeDefault)
                        as PipingSystemType;

                    if (duplicado != null)
                    {
                        log.Info(ETAPA, COMPONENTE,
                            $"PipingSystemType criado por duplicação: " +
                            $"'{nomeDefault}' (Id={duplicado.Id.Value}).",
                            duplicado.Id.Value);
                        return duplicado;
                    }
                }
                catch (Exception ex)
                {
                    log.Medio(ETAPA, COMPONENTE,
                        $"Erro ao duplicar PipingSystemType: {ex.Message}");
                }
            }

            // Fallback: primeiro tipo disponível
            if (tipos.Count > 0)
            {
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
        /// Filtra apenas elementos MEP válidos da lista.
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
                        $"Elemento {id.Value} não encontrado, ignorando.");
                    continue;
                }

                // Aceitar Pipes, Fittings e equipamentos MEP
                if (elem is MEPCurve ||
                    (elem is FamilyInstance fi && fi.MEPModel != null))
                {
                    validos.Add(id);
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
        /// Obtém ConnectorSet de um conjunto de elementos MEP.
        /// Retorna ConnectorSet nativo do Revit.
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
