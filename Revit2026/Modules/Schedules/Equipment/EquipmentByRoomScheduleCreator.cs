using Autodesk.Revit.DB;

namespace Revit2026.Modules.Schedules.Equipment
{
    /// <summary>
    /// Interface para criação de schedule de equipamentos por ambiente.
    /// </summary>
    public interface IEquipmentByRoomScheduleCreator
    {
        /// <summary>
        /// Cria ou retorna schedule existente "Equipamentos por Ambiente".
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        ViewSchedule Criar(Document doc);
    }

    /// <summary>
    /// Criador de Schedule de equipamentos MEP por ambiente (Room/Space).
    ///
    /// Schedule: "Equipamentos por Ambiente"
    /// Categoria: OST_PlumbingFixtures
    ///
    /// Campos:
    /// - Ambiente (ROOM_NAME)
    /// - Número do Ambiente (ROOM_NUMBER)
    /// - Sistema (RBS_SYSTEM_CLASSIFICATION_PARAM)
    /// - Família (ALL_MODEL_FAMILY_NAME)
    /// - Tipo (ELEM_TYPE_PARAM)
    /// - Nível (LEVEL_PARAM)
    /// - Quantidade (COUNT)
    ///
    /// Filtros: Ambiente não vazio, Sistema não vazio
    /// Sorting: Ambiente → Número → Sistema → Família → Tipo
    /// Grouping: Cabeçalhos por grupo, Itemize = false
    /// </summary>
    public class EquipmentByRoomScheduleCreator : IEquipmentByRoomScheduleCreator
    {
        private const string NOME_SCHEDULE = "Equipamentos por Ambiente";

        // Campos parametrizados (exceto COUNT)
        private static readonly (BuiltInParameter Param, string Titulo)[] CamposConfig =
        {
            (BuiltInParameter.ROOM_NAME, "Ambiente"),
            (BuiltInParameter.ROOM_NUMBER, "Número do Ambiente"),
            (BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM, "Sistema"),
            (BuiltInParameter.ALL_MODEL_FAMILY_NAME, "Família"),
            (BuiltInParameter.ELEM_TYPE_PARAM, "Tipo"),
            (BuiltInParameter.LEVEL_PARAM, "Nível"),
        };

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria o schedule de equipamentos por ambiente ou retorna existente.
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
                new ElementId(BuiltInCategory.OST_PlumbingFixtures));

            schedule.Name = NOME_SCHEDULE;

            // ── 3. Adicionar campos ───────────────────────────
            var camposAdicionados = AdicionarCampos(schedule);

            // ── 4. Adicionar campo de contagem ────────────────
            AdicionarCampoContagem(schedule);

            // ── 5. Aplicar filtros ────────────────────────────
            AplicarFiltros(schedule, camposAdicionados);

            // ── 6. Aplicar ordenação e agrupamento ────────────
            AplicarSortingGrouping(schedule, camposAdicionados);

            // ── 7. Configurações gerais ───────────────────────
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
        /// Adiciona todos os campos parametrizados ao schedule.
        /// </summary>
        private static Dictionary<BuiltInParameter, ScheduleFieldId> AdicionarCampos(
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

            // Buscar campo COUNT pelo nome
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

            // Fallback: buscar por FieldType
            foreach (var campo in camposDisponiveis)
            {
                try
                {
                    var field = definition.AddField(campo);
                    if (field.FieldType == ScheduleFieldType.Count)
                    {
                        field.ColumnHeading = "Quantidade";
                        return;
                    }
                    definition.RemoveField(field.FieldId);
                }
                catch { /* ignorar */ }
            }
        }

        /// <summary>
        /// Encontra SchedulableField pelo BuiltInParameter.
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
        /// Filtros:
        /// 1. Ambiente (ROOM_NAME) não vazio → remove equipamentos sem Room
        /// 2. Sistema não vazio → remove sem classificação
        /// </summary>
        private static void AplicarFiltros(
            ViewSchedule schedule,
            Dictionary<BuiltInParameter, ScheduleFieldId> campos)
        {
            var definition = schedule.Definition;

            // ── Filtro 1: Room Name não vazio ─────────────────
            if (campos.TryGetValue(BuiltInParameter.ROOM_NAME, out var roomNameId))
            {
                try
                {
                    var filtro = new ScheduleFilter(
                        roomNameId,
                        ScheduleFilterType.HasValue);
                    definition.AddFilter(filtro);
                }
                catch { /* filtro não aplicável */ }
            }

            // ── Filtro 2: Sistema não vazio ───────────────────
            if (campos.TryGetValue(BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM, out var sistemaId))
            {
                try
                {
                    var filtro = new ScheduleFilter(
                        sistemaId,
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
        /// Sorting: Ambiente → Número → Sistema → Família → Tipo
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
                BuiltInParameter.ROOM_NAME,
                BuiltInParameter.ROOM_NUMBER,
                BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM,
                BuiltInParameter.ALL_MODEL_FAMILY_NAME,
                BuiltInParameter.ELEM_TYPE_PARAM
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

                    // Cabeçalhos de grupo apenas para Ambiente e Sistema
                    if (bip == BuiltInParameter.ROOM_NAME ||
                        bip == BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM)
                    {
                        sortGroup.ShowHeader = true;
                        sortGroup.ShowFooter = true;
                        sortGroup.ShowFooterCount = true;
                        sortGroup.ShowFooterTitle = true;
                    }
                    else
                    {
                        sortGroup.ShowHeader = false;
                        sortGroup.ShowFooter = false;
                    }

                    definition.AddSortGroupField(sortGroup);
                }
                catch { /* sorting não aplicável */ }
            }

            // Itemize every instance = false
            definition.IsItemized = false;
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
