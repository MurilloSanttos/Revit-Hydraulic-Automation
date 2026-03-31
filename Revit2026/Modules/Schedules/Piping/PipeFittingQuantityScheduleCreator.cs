using Autodesk.Revit.DB;

namespace Revit2026.Modules.Schedules.Piping
{
    /// <summary>
    /// Interface para criação de schedule de quantitativos de conexões.
    /// </summary>
    public interface IPipeFittingQuantityScheduleCreator
    {
        /// <summary>
        /// Cria ou retorna schedule existente "Quantitativo - Conexões".
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        ViewSchedule Criar(Document doc);
    }

    /// <summary>
    /// Criador de Schedule de quantitativos de conexões de tubulação.
    ///
    /// Schedule: "Quantitativo - Conexões"
    /// Categoria: OST_PipeFitting
    ///
    /// Campos:
    /// - Sistema (RBS_SYSTEM_CLASSIFICATION_PARAM)
    /// - Família (ALL_MODEL_FAMILY_NAME)
    /// - Tipo da Conexão (ELEM_TYPE_PARAM)
    /// - Diâmetro Nominal (RBS_PIPE_DIAMETER_PARAM)
    /// - Material (MATERIAL_ID_PARAM)
    /// - Nível (LEVEL_PARAM)
    /// - Quantidade (COUNT)
    ///
    /// Filtros: Material não vazio, Diâmetro > 0
    /// Sorting: Sistema → Família → Tipo → Diâmetro
    /// Grouping: Cabeçalhos por grupo, Itemize = false
    /// Formatação: Diâmetro em milímetros
    /// </summary>
    public class PipeFittingQuantityScheduleCreator : IPipeFittingQuantityScheduleCreator
    {
        private const string NOME_SCHEDULE = "Quantitativo - Conexões";

        // Campos a adicionar (exceto Quantidade/COUNT que é tratado à parte)
        private static readonly (BuiltInParameter Param, string Titulo)[] CamposConfig =
        {
            (BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM, "Sistema"),
            (BuiltInParameter.ALL_MODEL_FAMILY_NAME, "Família"),
            (BuiltInParameter.ELEM_TYPE_PARAM, "Tipo da Conexão"),
            (BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, "Diâmetro Nominal"),
            (BuiltInParameter.MATERIAL_ID_PARAM, "Material"),
            (BuiltInParameter.LEVEL_PARAM, "Nível"),
        };

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria o schedule de quantitativos de conexões ou retorna existente.
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
                new ElementId(BuiltInCategory.OST_PipeFitting));

            schedule.Name = NOME_SCHEDULE;

            // ── 3. Adicionar campos ───────────────────────────
            var camposAdicionados = AdicionarCampos(doc, schedule);

            // ── 4. Adicionar campo de contagem ────────────────
            AdicionarCampoContagem(schedule);

            // ── 5. Aplicar filtros ────────────────────────────
            AplicarFiltros(schedule, camposAdicionados);

            // ── 6. Aplicar ordenação e agrupamento ────────────
            AplicarSortingGrouping(schedule, camposAdicionados);

            // ── 7. Aplicar formatação ─────────────────────────
            AplicarFormatacao(schedule, camposAdicionados);

            // ── 8. Configurações gerais ───────────────────────
            ConfigurarSchedule(schedule);

            return schedule;
        }

        // ══════════════════════════════════════════════════════════
        //  BUSCAR EXISTENTE
        // ══════════════════════════════════════════════════════════

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
        /// Adiciona campos parametrizados ao schedule.
        /// </summary>
        private static Dictionary<BuiltInParameter, ScheduleFieldId> AdicionarCampos(
            Document doc,
            ViewSchedule schedule)
        {
            var definition = schedule.Definition;
            var camposAdicionados = new Dictionary<BuiltInParameter, ScheduleFieldId>();
            var camposDisponiveis = definition.GetSchedulableFields();

            foreach (var (bip, titulo) in CamposConfig)
            {
                var schedulableField = EncontrarCampo(camposDisponiveis, bip);
                if (schedulableField == null)
                    continue;

                try
                {
                    var field = definition.AddField(schedulableField);
                    field.ColumnHeading = titulo;
                    camposAdicionados[bip] = field.FieldId;
                }
                catch { /* campo já existe ou não aplicável */ }
            }

            return camposAdicionados;
        }

        /// <summary>
        /// Adiciona campo de contagem automática (COUNT).
        /// </summary>
        private static void AdicionarCampoContagem(ViewSchedule schedule)
        {
            var definition = schedule.Definition;
            var camposDisponiveis = definition.GetSchedulableFields();

            foreach (var campo in camposDisponiveis)
            {
                try
                {
                    if (campo.GetName(schedule.Document) == "Count")
                    {
                        var field = definition.AddField(campo);
                        field.ColumnHeading = "Quantidade";
                        return;
                    }
                }
                catch { /* ignorar */ }
            }

            // Fallback: buscar por ScheduleFieldType
            foreach (var campo in camposDisponiveis)
            {
                try
                {
                    var field = definition.AddField(campo);
                    if (field.GetName() == "Count" ||
                        field.FieldType == ScheduleFieldType.Count)
                    {
                        field.ColumnHeading = "Quantidade";
                        return;
                    }

                    // Não era o campo certo, remover
                    definition.RemoveField(field.FieldId);
                }
                catch { /* ignorar */ }
            }
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
        /// Aplica filtros:
        /// 1. Material não vazio (HasValue)
        /// 2. Diâmetro > 0
        /// </summary>
        private static void AplicarFiltros(
            ViewSchedule schedule,
            Dictionary<BuiltInParameter, ScheduleFieldId> campos)
        {
            var definition = schedule.Definition;

            // ── Filtro 1: Material não vazio ──────────────────
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

            // ── Filtro 2: Diâmetro > 0 ───────────────────────
            if (campos.TryGetValue(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, out var diamId))
            {
                try
                {
                    var filtro = new ScheduleFilter(
                        diamId,
                        ScheduleFilterType.GreaterThan,
                        0.0);
                    definition.AddFilter(filtro);
                }
                catch { /* filtro não aplicável */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  SORTING & GROUPING
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Sorting: Sistema → Família → Tipo da Conexão → Diâmetro
        /// IsItemized = false, Cabeçalhos por grupo.
        /// </summary>
        private static void AplicarSortingGrouping(
            ViewSchedule schedule,
            Dictionary<BuiltInParameter, ScheduleFieldId> campos)
        {
            var definition = schedule.Definition;
            definition.ClearSortGroupFields();

            var ordemSorting = new[]
            {
                BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM,
                BuiltInParameter.ALL_MODEL_FAMILY_NAME,
                BuiltInParameter.ELEM_TYPE_PARAM,
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

                    sortGroup.ShowHeader = true;
                    sortGroup.ShowFooter = true;
                    sortGroup.ShowFooterCount = true;
                    sortGroup.ShowFooterTitle = true;

                    definition.AddSortGroupField(sortGroup);
                }
                catch { /* sorting não aplicável */ }
            }

            // Itemize every instance = false
            definition.IsItemized = false;
        }

        // ══════════════════════════════════════════════════════════
        //  FORMATAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Diâmetro Nominal → milímetros.
        /// </summary>
        private static void AplicarFormatacao(
            ViewSchedule schedule,
            Dictionary<BuiltInParameter, ScheduleFieldId> campos)
        {
            if (!campos.TryGetValue(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM, out var diamId))
                return;

            var definition = schedule.Definition;
            var fieldCount = definition.GetFieldCount();

            for (int i = 0; i < fieldCount; i++)
            {
                var field = definition.GetField(i);

                if (field.FieldId == diamId)
                {
                    try
                    {
                        var formatOptions = new FormatOptions(UnitTypeId.Millimeters);
                        formatOptions.Accuracy = 0.1;
                        formatOptions.UseDefault = false;
                        field.SetFormatOptions(formatOptions);
                    }
                    catch { /* formatação não aplicável */ }

                    break;
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONFIGURAÇÕES GERAIS
        // ══════════════════════════════════════════════════════════

        private static void ConfigurarSchedule(ViewSchedule schedule)
        {
            try
            {
                // Revit 2026: AllowOverrideCellStyle is no longer a settable property
                // Visual configuration handled by default schedule settings
            }
            catch { /* configuração visual não é crítica */ }
        }
    }
}
