using Autodesk.Revit.DB;

namespace Revit2026.Modules.Schedules.Piping
{
    /// <summary>
    /// Interface para criação de schedule de quantitativos de tubulação.
    /// </summary>
    public interface IPipeQuantityScheduleCreator
    {
        /// <summary>
        /// Cria ou retorna schedule existente "Quantitativo - Tubulação".
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        ViewSchedule Criar(Document doc);
    }

    /// <summary>
    /// Criador de Schedule de quantitativos de tubulação.
    ///
    /// Schedule: "Quantitativo - Tubulação"
    /// Categoria: OST_PipeCurves
    ///
    /// Campos:
    /// - Sistema (RBS_SYSTEM_CLASSIFICATION_PARAM)
    /// - Material (MATERIAL_ID_PARAM)
    /// - Tipo de Tubo (ELEM_TYPE_PARAM)
    /// - Diâmetro (RBS_PIPE_DIAMETER_PARAM)
    /// - Comprimento (CURVE_ELEM_LENGTH)
    /// - Nível (LEVEL_PARAM)
    ///
    /// Filtros:
    /// - Comprimento > 0
    /// - Material não vazio
    ///
    /// Sorting: Sistema → Material → Diâmetro
    /// Grouping: Cabeçalhos por grupo, Itemize = false
    ///
    /// Formatação: Comprimento em metros, Diâmetro em milímetros
    /// </summary>
    public class PipeQuantityScheduleCreator : IPipeQuantityScheduleCreator
    {
        private const string NOME_SCHEDULE = "Quantitativo - Tubulação";

        // Mapeamento de campos
        private static readonly (BuiltInParameter Param, string Titulo, bool IsTotal)[] CamposConfig =
        {
            (BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM, "Sistema", false),
            (BuiltInParameter.MATERIAL_ID_PARAM, "Material", false),
            (BuiltInParameter.ELEM_TYPE_PARAM, "Tipo de Tubo", false),
            (BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, "Diâmetro", false),
            (BuiltInParameter.CURVE_ELEM_LENGTH, "Comprimento", true),
            (BuiltInParameter.LEVEL_PARAM, "Nível", false),
        };

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria o schedule de quantitativos ou retorna existente.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ViewSchedule Criar(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            // ── 1. Verificar existente ────────────────────────
            var existente = BuscarScheduleExistente(doc);
            if (existente != null)
                return existente;

            // ── 2. Criar novo Schedule ────────────────────────
            var schedule = ViewSchedule.CreateSchedule(
                doc,
                new ElementId(BuiltInCategory.OST_PipeCurves));

            schedule.Name = NOME_SCHEDULE;

            // ── 3. Adicionar campos ───────────────────────────
            var camposAdicionados = AdicionarCampos(doc, schedule);

            // ── 4. Aplicar filtros ────────────────────────────
            AplicarFiltros(schedule, camposAdicionados);

            // ── 5. Aplicar ordenação e agrupamento ────────────
            AplicarSortingGrouping(schedule, camposAdicionados);

            // ── 6. Aplicar formatação ─────────────────────────
            AplicarFormatacao(schedule, camposAdicionados);

            // ── 7. Configurações gerais ───────────────────────
            ConfigurarSchedule(schedule);

            return schedule;
        }

        // ══════════════════════════════════════════════════════════
        //  BUSCAR EXISTENTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca schedule com o mesmo nome no documento.
        /// </summary>
        private static ViewSchedule? BuscarScheduleExistente(Document doc)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>();

            foreach (var vs in collector)
            {
                if (vs.Name == NOME_SCHEDULE && !vs.IsTitleblockRevisionSchedule)
                    return vs;
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  CAMPOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Adiciona todos os campos ao schedule.
        /// Retorna dicionário de BuiltInParameter → ScheduleFieldId.
        /// </summary>
        private static Dictionary<BuiltInParameter, ScheduleFieldId> AdicionarCampos(
            Document doc,
            ViewSchedule schedule)
        {
            var definition = schedule.Definition;
            var camposAdicionados = new Dictionary<BuiltInParameter, ScheduleFieldId>();

            // Obter campos disponíveis
            var camposDisponiveis = definition.GetSchedulableFields();

            foreach (var (bip, titulo, isTotal) in CamposConfig)
            {
                var schedulableField = EncontrarCampo(camposDisponiveis, bip);

                if (schedulableField == null)
                    continue;

                try
                {
                    var field = definition.AddField(schedulableField);

                    // Definir título da coluna
                    field.ColumnHeading = titulo;

                    // Configurar totalização para Comprimento
                    if (isTotal)
                    {
                        field.DisplayType = ScheduleFieldDisplayType.Totals;
                    }

                    camposAdicionados[bip] = field.FieldId;
                }
                catch
                {
                    // Campo já existe ou não pode ser adicionado
                }
            }

            return camposAdicionados;
        }

        /// <summary>
        /// Encontra um SchedulableField pelo BuiltInParameter.
        /// </summary>
        private static SchedulableField? EncontrarCampo(
            IList<SchedulableField> campos,
            BuiltInParameter bip)
        {
            var paramId = new ElementId(bip);

            foreach (var campo in campos)
            {
                if (campo.ParameterId == paramId)
                    return campo;
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  FILTROS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica filtros automáticos:
        /// 1. Comprimento > 0
        /// 2. Material não vazio
        /// </summary>
        private static void AplicarFiltros(
            ViewSchedule schedule,
            Dictionary<BuiltInParameter, ScheduleFieldId> campos)
        {
            var definition = schedule.Definition;

            // ── Filtro 1: Comprimento > 0 ─────────────────────
            if (campos.TryGetValue(BuiltInParameter.CURVE_ELEM_LENGTH, out var comprimentoId))
            {
                try
                {
                    var filtro = new ScheduleFilter(
                        comprimentoId,
                        ScheduleFilterType.GreaterThan,
                        0.0);

                    definition.AddFilter(filtro);
                }
                catch { /* filtro não aplicável */ }
            }

            // ── Filtro 2: Material não vazio ──────────────────
            if (campos.TryGetValue(BuiltInParameter.MATERIAL_ID_PARAM, out var materialId))
            {
                try
                {
                    var filtro = new ScheduleFilter(
                        materialId,
                        ScheduleFilterType.HasValue);

                    definition.AddFilter(filtro);
                }
                catch { /* filtro não aplicável */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SORTING & GROUPING
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica ordenação: Sistema → Material → Diâmetro
        /// Com cabeçalhos de grupo e Itemize = false.
        /// </summary>
        private static void AplicarSortingGrouping(
            ViewSchedule schedule,
            Dictionary<BuiltInParameter, ScheduleFieldId> campos)
        {
            var definition = schedule.Definition;

            // Limpar ordenações existentes
            definition.ClearSortGroupFields();

            // Ordem de sorting
            var ordemSorting = new[]
            {
                BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM,
                BuiltInParameter.MATERIAL_ID_PARAM,
                BuiltInParameter.RBS_PIPE_DIAMETER_PARAM
            };

            foreach (var bip in ordemSorting)
            {
                if (!campos.TryGetValue(bip, out var fieldId))
                    continue;

                try
                {
                    var sortGroup = new ScheduleSortGroupField(
                        fieldId,
                        ScheduleSortOrder.Ascending);

                    // Cabeçalho em cada grupo
                    sortGroup.ShowHeader = true;
                    sortGroup.ShowFooter = true;

                    // Desativar contagem de totais no footer
                    sortGroup.ShowFooterCount = false;
                    sortGroup.ShowFooterTitle = true;

                    definition.AddSortGroupField(sortGroup);
                }
                catch { /* sorting não aplicável */ }
            }

            // Itemize every instance = false (agrupar)
            definition.IsItemized = false;
        }

        // ══════════════════════════════════════════════════════════
        //  FORMATAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica formatação de unidades:
        /// - Comprimento → metros
        /// - Diâmetro → milímetros
        /// </summary>
        private static void AplicarFormatacao(
            ViewSchedule schedule,
            Dictionary<BuiltInParameter, ScheduleFieldId> campos)
        {
            var definition = schedule.Definition;
            var fieldCount = definition.GetFieldCount();

            for (int i = 0; i < fieldCount; i++)
            {
                var field = definition.GetField(i);

                // ── Comprimento → metros ──────────────────────
                if (campos.TryGetValue(BuiltInParameter.CURVE_ELEM_LENGTH, out var compId) &&
                    field.FieldId == compId)
                {
                    try
                    {
                        var formatOptions = new FormatOptions(UnitTypeId.Meters);
                        formatOptions.Accuracy = 0.001; // 3 casas decimais
                        formatOptions.UseDefault = false;
                        field.SetFormatOptions(formatOptions);
                    }
                    catch { /* formatação não aplicável */ }
                }

                // ── Diâmetro → milímetros ─────────────────────
                if (campos.TryGetValue(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, out var diamId) &&
                    field.FieldId == diamId)
                {
                    try
                    {
                        var formatOptions = new FormatOptions(UnitTypeId.Millimeters);
                        formatOptions.Accuracy = 0.1; // 1 casa decimal
                        formatOptions.UseDefault = false;
                        field.SetFormatOptions(formatOptions);
                    }
                    catch { /* formatação não aplicável */ }
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONFIGURAÇÕES GERAIS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Configurações adicionais do schedule.
        /// </summary>
        private static void ConfigurarSchedule(ViewSchedule schedule)
        {
            try
            {
                // Configurar aparência
                var bodyData = schedule.GetTableData()?.GetSectionData(
                    SectionType.Body);

                // Revit 2026: AllowOverrideCellStyle is no longer a settable property
                // Visual configuration handled by default schedule settings

                var headerData = schedule.GetTableData()?.GetSectionData(
                    SectionType.Header);
            }
            catch { /* configuração visual não é crítica */ }
        }
    }
}
