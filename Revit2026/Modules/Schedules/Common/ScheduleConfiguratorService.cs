using Autodesk.Revit.DB;

namespace Revit2026.Modules.Schedules.Common
{
    /// <summary>
    /// Interface para configuração centralizada de Schedules.
    /// </summary>
    public interface IScheduleConfiguratorService
    {
        IList<ScheduleFieldId> ConfigurarCampos(
            ViewSchedule schedule,
            IList<BuiltInParameter> parametros);

        void ConfigurarFiltros(
            ViewSchedule schedule,
            IList<(BuiltInParameter param, ScheduleFilterType tipo, object valor)> filtros);

        void ConfigurarSorting(
            ViewSchedule schedule,
            IList<BuiltInParameter> ordem);

        void ConfigurarFormatacao(
            ViewSchedule schedule,
            IDictionary<BuiltInParameter, (string titulo, ForgeTypeId? unidade)> configuracoes);
    }

    /// <summary>
    /// Serviço centralizado para configuração de campos, filtros,
    /// ordenação e formatação de ViewSchedules.
    ///
    /// Reutilizável por todos os criadores de schedules do projeto.
    /// Cada método é independente e pode ser usado separadamente.
    ///
    /// Padrão de uso:
    ///   var config = new ScheduleConfiguratorService();
    ///   var campos = config.ConfigurarCampos(schedule, parametros);
    ///   config.ConfigurarFiltros(schedule, filtros);
    ///   config.ConfigurarSorting(schedule, ordem);
    ///   config.ConfigurarFormatacao(schedule, formatacoes);
    /// </summary>
    public class ScheduleConfiguratorService : IScheduleConfiguratorService
    {
        // ══════════════════════════════════════════════════════════
        //  1. CONFIGURAR CAMPOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Limpa campos existentes e adiciona novos na ordem recebida.
        /// Retorna lista de ScheduleFieldId dos campos adicionados.
        /// Parâmetros inexistentes são ignorados silenciosamente.
        /// </summary>
        public IList<ScheduleFieldId> ConfigurarCampos(
            ViewSchedule schedule,
            IList<BuiltInParameter> parametros)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (parametros == null)
                throw new ArgumentNullException(nameof(parametros));

            var definition = schedule.Definition;
            var resultado = new List<ScheduleFieldId>();

            // Limpar campos existentes
            LimparCampos(definition);

            // Obter campos disponíveis
            var camposDisponiveis = definition.GetSchedulableFields();

            foreach (var bip in parametros)
            {
                var schedulable = EncontrarCampo(camposDisponiveis, bip);
                if (schedulable == null)
                    continue;

                try
                {
                    var field = definition.AddField(schedulable);
                    resultado.Add(field.FieldId);
                }
                catch { /* campo não pôde ser adicionado */ }
            }

            return resultado;
        }

        /// <summary>
        /// Adiciona campos sem limpar os existentes.
        /// </summary>
        public IList<ScheduleFieldId> AdicionarCampos(
            ViewSchedule schedule,
            IList<BuiltInParameter> parametros)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            var definition = schedule.Definition;
            var resultado = new List<ScheduleFieldId>();
            var camposDisponiveis = definition.GetSchedulableFields();

            foreach (var bip in parametros)
            {
                var schedulable = EncontrarCampo(camposDisponiveis, bip);
                if (schedulable == null)
                    continue;

                try
                {
                    var field = definition.AddField(schedulable);
                    resultado.Add(field.FieldId);
                }
                catch { /* campo já existe ou não aplicável */ }
            }

            return resultado;
        }

        /// <summary>
        /// Adiciona campo de contagem (COUNT) ao schedule.
        /// </summary>
        public ScheduleFieldId? AdicionarCampoContagem(
            ViewSchedule schedule,
            string titulo = "Quantidade")
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            var definition = schedule.Definition;
            var camposDisponiveis = definition.GetSchedulableFields();

            // Buscar por nome "Count"
            foreach (var campo in camposDisponiveis)
            {
                try
                {
                    if (campo.GetName(schedule.Document) == "Count")
                    {
                        var field = definition.AddField(campo);
                        field.ColumnHeading = titulo;
                        return field.FieldId;
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
                        field.ColumnHeading = titulo;
                        return field.FieldId;
                    }
                    definition.RemoveField(field.FieldId);
                }
                catch { /* ignorar */ }
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  2. CONFIGURAR FILTROS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Limpa filtros existentes e aplica novos.
        /// Suporta: Equals, NotEquals, GreaterThan, LessThan,
        ///          GreaterThanOrEqual, LessThanOrEqual,
        ///          HasValue, NotHasValue, Contains, NotContains.
        ///
        /// Para HasValue/NotHasValue o valor é ignorado.
        /// Para filtros numéricos o valor deve ser double.
        /// Para filtros de texto o valor deve ser string.
        /// </summary>
        public void ConfigurarFiltros(
            ViewSchedule schedule,
            IList<(BuiltInParameter param, ScheduleFilterType tipo, object valor)> filtros)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (filtros == null)
                throw new ArgumentNullException(nameof(filtros));

            var definition = schedule.Definition;

            // Limpar filtros existentes
            LimparFiltros(definition);

            // Mapear campos existentes
            var mapaFieldIds = MapearCamposExistentes(definition);

            foreach (var (bip, tipo, valor) in filtros)
            {
                var paramId = new ElementId(bip);

                if (!mapaFieldIds.TryGetValue(paramId.Value, out var fieldId))
                    continue;

                try
                {
                    ScheduleFilter filtro;

                    // Filtros sem valor
                    if (tipo == ScheduleFilterType.HasValue ||
                        tipo == ScheduleFilterType.HasNoValue)
                    {
                        filtro = new ScheduleFilter(fieldId, tipo);
                    }
                    // Filtros com valor double
                    else if (valor is double d)
                    {
                        filtro = new ScheduleFilter(fieldId, tipo, d);
                    }
                    // Filtros com valor int
                    else if (valor is int i)
                    {
                        filtro = new ScheduleFilter(fieldId, tipo, i);
                    }
                    // Filtros com valor string
                    else if (valor is string s)
                    {
                        filtro = new ScheduleFilter(fieldId, tipo, s);
                    }
                    // Filtros com ElementId
                    else if (valor is ElementId eid)
                    {
                        filtro = new ScheduleFilter(fieldId, tipo, eid);
                    }
                    // Fallback: tentar como double 0
                    else
                    {
                        filtro = new ScheduleFilter(fieldId, tipo, 0.0);
                    }

                    definition.AddFilter(filtro);
                }
                catch { /* filtro não aplicável */ }
            }
        }

        /// <summary>
        /// Adiciona um único filtro ao schedule (sem limpar existentes).
        /// </summary>
        public bool AdicionarFiltro(
            ViewSchedule schedule,
            BuiltInParameter param,
            ScheduleFilterType tipo,
            object? valor = null)
        {
            if (schedule == null) return false;

            var definition = schedule.Definition;
            var mapaFieldIds = MapearCamposExistentes(definition);
            var paramId = new ElementId(param);

            if (!mapaFieldIds.TryGetValue(paramId.Value, out var fieldId))
                return false;

            try
            {
                ScheduleFilter filtro;

                if (tipo == ScheduleFilterType.HasValue ||
                    tipo == ScheduleFilterType.HasNoValue)
                {
                    filtro = new ScheduleFilter(fieldId, tipo);
                }
                else if (valor is double d)
                {
                    filtro = new ScheduleFilter(fieldId, tipo, d);
                }
                else if (valor is int i)
                {
                    filtro = new ScheduleFilter(fieldId, tipo, i);
                }
                else if (valor is string s)
                {
                    filtro = new ScheduleFilter(fieldId, tipo, s);
                }
                else
                {
                    filtro = new ScheduleFilter(fieldId, tipo, 0.0);
                }

                definition.AddFilter(filtro);
                return true;
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  3. CONFIGURAR SORTING
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Limpa sorting existente e aplica nova ordenação.
        /// ShowHeaders = true, ShowBlankLine = false,
        /// ShowFooter = false, IsItemized = false.
        /// </summary>
        public void ConfigurarSorting(
            ViewSchedule schedule,
            IList<BuiltInParameter> ordem)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (ordem == null)
                throw new ArgumentNullException(nameof(ordem));

            var definition = schedule.Definition;

            // Limpar sorting existente
            definition.ClearSortGroupFields();

            // Mapear campos
            var mapaFieldIds = MapearCamposExistentes(definition);

            foreach (var bip in ordem)
            {
                var paramId = new ElementId(bip);

                if (!mapaFieldIds.TryGetValue(paramId.Value, out var fieldId))
                    continue;

                try
                {
                    var sortGroup = new ScheduleSortGroupField(
                        fieldId,
                        ScheduleSortOrder.Ascending);

                    sortGroup.ShowHeader = true;
                    sortGroup.ShowBlankLine = false;
                    sortGroup.ShowFooter = false;
                    sortGroup.ShowFooterCount = false;
                    sortGroup.ShowFooterTitle = false;

                    definition.AddSortGroupField(sortGroup);
                }
                catch { /* sorting não aplicável */ }
            }

            // Desativar itemização
            definition.IsItemized = false;
        }

        /// <summary>
        /// Configuração avançada de sorting com opções individuais por campo.
        /// </summary>
        public void ConfigurarSortingAvancado(
            ViewSchedule schedule,
            IList<SortingConfig> configs)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (configs == null)
                throw new ArgumentNullException(nameof(configs));

            var definition = schedule.Definition;
            definition.ClearSortGroupFields();

            var mapaFieldIds = MapearCamposExistentes(definition);

            foreach (var config in configs)
            {
                var paramId = new ElementId(config.Parametro);

                if (!mapaFieldIds.TryGetValue(paramId.Value, out var fieldId))
                    continue;

                try
                {
                    var sortGroup = new ScheduleSortGroupField(
                        fieldId,
                        config.Ordem);

                    sortGroup.ShowHeader = config.MostrarCabecalho;
                    sortGroup.ShowBlankLine = config.LinhaEmBranco;
                    sortGroup.ShowFooter = config.MostrarRodape;
                    sortGroup.ShowFooterCount = config.MostrarContagem;
                    sortGroup.ShowFooterTitle = config.MostrarTituloRodape;

                    definition.AddSortGroupField(sortGroup);
                }
                catch { /* sorting não aplicável */ }
            }

            definition.IsItemized = false;
        }

        // ══════════════════════════════════════════════════════════
        //  4. CONFIGURAR FORMATAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica títulos e unidades aos campos.
        /// Unidade null = manter default do Revit.
        /// </summary>
        public void ConfigurarFormatacao(
            ViewSchedule schedule,
            IDictionary<BuiltInParameter, (string titulo, ForgeTypeId? unidade)> configuracoes)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (configuracoes == null)
                throw new ArgumentNullException(nameof(configuracoes));

            var definition = schedule.Definition;
            var fieldCount = definition.GetFieldCount();

            // Mapear BuiltInParameter → ScheduleField index
            var mapaParamToIndex = new Dictionary<long, int>();

            for (int i = 0; i < fieldCount; i++)
            {
                var field = definition.GetField(i);
                var paramId = field.ParameterId;
                if (paramId != null && paramId != ElementId.InvalidElementId)
                    mapaParamToIndex[paramId.Value] = i;
            }

            foreach (var (bip, (titulo, unidade)) in configuracoes)
            {
                var paramId = new ElementId(bip);

                if (!mapaParamToIndex.TryGetValue(paramId.Value, out var index))
                    continue;

                try
                {
                    var field = definition.GetField(index);

                    // Título
                    if (!string.IsNullOrEmpty(titulo))
                        field.ColumnHeading = titulo;

                    // Unidade
                    if (unidade != null)
                    {
                        var formatOptions = new FormatOptions(unidade);
                        formatOptions.UseDefault = false;

                        // Precisão baseada na unidade
                        if (unidade == UnitTypeId.Meters)
                            formatOptions.Accuracy = 0.001;
                        else if (unidade == UnitTypeId.Millimeters)
                            formatOptions.Accuracy = 0.1;
                        else if (unidade == UnitTypeId.Centimeters)
                            formatOptions.Accuracy = 0.01;

                        field.SetFormatOptions(formatOptions);
                    }
                }
                catch { /* formatação não aplicável */ }
            }
        }

        /// <summary>
        /// Aplica formatação com precisão customizada.
        /// </summary>
        public void ConfigurarFormatacaoAvancada(
            ViewSchedule schedule,
            IDictionary<BuiltInParameter, FormatacaoConfig> configuracoes)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));

            var definition = schedule.Definition;
            var fieldCount = definition.GetFieldCount();

            var mapaParamToIndex = new Dictionary<long, int>();
            for (int i = 0; i < fieldCount; i++)
            {
                var field = definition.GetField(i);
                var paramId = field.ParameterId;
                if (paramId != null && paramId != ElementId.InvalidElementId)
                    mapaParamToIndex[paramId.Value] = i;
            }

            foreach (var (bip, config) in configuracoes)
            {
                var paramId = new ElementId(bip);

                if (!mapaParamToIndex.TryGetValue(paramId.Value, out var index))
                    continue;

                try
                {
                    var field = definition.GetField(index);

                    if (!string.IsNullOrEmpty(config.Titulo))
                        field.ColumnHeading = config.Titulo;

                    if (config.Oculto.HasValue)
                        field.IsHidden = config.Oculto.Value;

                    if (config.DisplayType.HasValue)
                        field.DisplayType = config.DisplayType.Value;

                    if (config.Unidade != null)
                    {
                        var formatOptions = new FormatOptions(config.Unidade);
                        formatOptions.UseDefault = false;

                        if (config.Precisao.HasValue)
                            formatOptions.Accuracy = config.Precisao.Value;

                        field.SetFormatOptions(formatOptions);
                    }
                }
                catch { /* formatação não aplicável */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  UTILITÁRIOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca schedule existente por nome.
        /// </summary>
        public static ViewSchedule? BuscarPorNome(Document doc, string nome)
        {
            var collector = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>();

            foreach (var vs in collector)
            {
                if (vs.Name == nome && !vs.IsTitleblockRevisionSchedule)
                    return vs;
            }

            return null;
        }

        /// <summary>
        /// Cria um novo schedule para uma categoria.
        /// </summary>
        public static ViewSchedule CriarSchedule(
            Document doc,
            BuiltInCategory categoria,
            string nome)
        {
            var schedule = ViewSchedule.CreateSchedule(
                doc,
                new ElementId(categoria));

            schedule.Name = nome;
            return schedule;
        }

        /// <summary>
        /// Obtém ou cria schedule (verifica existência pelo nome).
        /// </summary>
        public static (ViewSchedule schedule, bool jáExistia) ObterOuCriar(
            Document doc,
            BuiltInCategory categoria,
            string nome)
        {
            var existente = BuscarPorNome(doc, nome);
            if (existente != null)
                return (existente, true);

            return (CriarSchedule(doc, categoria, nome), false);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS INTERNOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Limpa todos os campos de um schedule.
        /// </summary>
        private static void LimparCampos(ScheduleDefinition definition)
        {
            while (definition.GetFieldCount() > 0)
            {
                try
                {
                    var field = definition.GetField(0);
                    definition.RemoveField(field.FieldId);
                }
                catch { break; }
            }
        }

        /// <summary>
        /// Limpa todos os filtros de um schedule.
        /// </summary>
        private static void LimparFiltros(ScheduleDefinition definition)
        {
            while (definition.GetFilterCount() > 0)
            {
                try
                {
                    definition.RemoveFilter(0);
                }
                catch { break; }
            }
        }

        /// <summary>
        /// Cria mapa de ParameterId.Value → ScheduleFieldId.
        /// </summary>
        private static Dictionary<long, ScheduleFieldId> MapearCamposExistentes(
            ScheduleDefinition definition)
        {
            var mapa = new Dictionary<long, ScheduleFieldId>();
            var count = definition.GetFieldCount();

            for (int i = 0; i < count; i++)
            {
                try
                {
                    var field = definition.GetField(i);
                    var paramId = field.ParameterId;

                    if (paramId != null && paramId != ElementId.InvalidElementId)
                    {
                        mapa[paramId.Value] = field.FieldId;
                    }
                }
                catch { /* ignorar */ }
            }

            return mapa;
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
    }

    // ══════════════════════════════════════════════════════════════
    //  MODELOS DE CONFIGURAÇÃO
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Configuração avançada de sorting para um campo.
    /// </summary>
    public class SortingConfig
    {
        public BuiltInParameter Parametro { get; set; }
        public ScheduleSortOrder Ordem { get; set; } = ScheduleSortOrder.Ascending;
        public bool MostrarCabecalho { get; set; } = true;
        public bool LinhaEmBranco { get; set; } = false;
        public bool MostrarRodape { get; set; } = false;
        public bool MostrarContagem { get; set; } = false;
        public bool MostrarTituloRodape { get; set; } = false;
    }

    /// <summary>
    /// Configuração avançada de formatação para um campo.
    /// </summary>
    public class FormatacaoConfig
    {
        public string? Titulo { get; set; }
        public ForgeTypeId? Unidade { get; set; }
        public double? Precisao { get; set; }
        public bool? Oculto { get; set; }
        public ScheduleFieldDisplayType? DisplayType { get; set; }
    }
}
