using Autodesk.Revit.DB;

namespace Revit2026.Modules.Sheets
{
    /// <summary>
    /// Interface para adição de Schedules em pranchas.
    /// </summary>
    public interface ISchedulePlacementService
    {
        /// <summary>
        /// Adiciona múltiplos ViewSchedules a uma ViewSheet.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        void AdicionarTabelas(
            Document doc,
            ViewSheet sheet,
            IList<ViewSchedule> schedules);
    }

    /// <summary>
    /// Configuração de posicionamento de Schedules.
    /// </summary>
    public class ScheduleLayoutConfig
    {
        /// <summary>Espaçamento vertical entre tabelas em metros.</summary>
        public double EspacamentoVerticalM { get; set; } = 0.04;

        /// <summary>Espaçamento horizontal entre colunas em metros.</summary>
        public double EspacamentoHorizontalM { get; set; } = 0.04;

        /// <summary>Margem da borda da prancha em metros.</summary>
        public double MargemM { get; set; } = 0.025;

        /// <summary>Modo de layout.</summary>
        public ScheduleLayoutMode Modo { get; set; } = ScheduleLayoutMode.ColunaUnica;

        /// <summary>Posição inicial na prancha.</summary>
        public ScheduleAnchor Ancora { get; set; } = ScheduleAnchor.SuperiorEsquerdo;
    }

    public enum ScheduleLayoutMode
    {
        /// <summary>Empilhamento vertical em coluna única.</summary>
        ColunaUnica,

        /// <summary>Duas colunas lado a lado.</summary>
        DuasColunas,

        /// <summary>Distribuição automática baseada no espaço.</summary>
        Automatico
    }

    public enum ScheduleAnchor
    {
        SuperiorEsquerdo,
        SuperiorDireito,
        InferiorEsquerdo,
        InferiorDireito,
        Centro
    }

    /// <summary>
    /// Resultado da adição de Schedules.
    /// </summary>
    public class ResultadoSchedulePlacement
    {
        public int Total { get; set; }
        public int Adicionadas { get; set; }
        public int Ignoradas { get; set; }
        public List<ScheduleSheetInstance> Instancias { get; set; } = new();
        public List<string> Mensagens { get; set; } = new();

        public override string ToString() =>
            $"{Adicionadas}/{Total} tabelas adicionadas, {Ignoradas} ignoradas";
    }

    /// <summary>
    /// Serviço de adição e posicionamento automático de Schedules em pranchas.
    ///
    /// Layouts:
    /// - Coluna única: empilhamento vertical
    /// - Duas colunas: lado a lado com balanceamento
    /// - Automático: escolhe baseado no número de schedules
    ///
    /// Funcionalidades:
    /// - Evita sobreposição
    /// - Centralização horizontal
    /// - Detecção de schedules já existentes
    /// - Detecção de tamanho da prancha
    ///
    /// Uso:
    ///   var service = new SchedulePlacementService();
    ///   service.AdicionarTabelas(doc, sheet, schedules);
    /// </summary>
    public class SchedulePlacementService : ISchedulePlacementService
    {
        // ══════════════════════════════════════════════════════════
        //  ADICIONAR TABELAS (INTERFACE)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Adiciona Schedules com layout padrão (coluna única, superior esquerdo).
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public void AdicionarTabelas(
            Document doc,
            ViewSheet sheet,
            IList<ViewSchedule> schedules)
        {
            AdicionarTabelas(doc, sheet, schedules, new ScheduleLayoutConfig());
        }

        /// <summary>
        /// Adiciona Schedules com configuração customizada.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ResultadoSchedulePlacement AdicionarTabelas(
            Document doc,
            ViewSheet sheet,
            IList<ViewSchedule> schedules,
            ScheduleLayoutConfig config)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (schedules == null) throw new ArgumentNullException(nameof(schedules));

            var resultado = new ResultadoSchedulePlacement { Total = schedules.Count };

            // ── 1. Filtrar schedules válidos ──────────────────
            var schedulesValidos = FiltrarValidos(doc, sheet, schedules, resultado);

            if (schedulesValidos.Count == 0)
                return resultado;

            // ── 2. Obter dimensões da prancha ─────────────────
            var sheetSize = ObterTamanhoPrancha(doc, sheet);

            // ── 3. Obter área ocupada existente ───────────────
            var areaOcupada = ObterAreaOcupada(doc, sheet);

            // ── 4. Criar instances ────────────────────────────
            var instances = CriarInstances(doc, sheet, schedulesValidos, resultado);

            if (instances.Count == 0)
                return resultado;

            // ── 5. Medir dimensões ────────────────────────────
            var dimensoes = MedirInstances(sheet, instances);

            // ── 6. Escolher modo de layout ────────────────────
            var modo = config.Modo;
            if (modo == ScheduleLayoutMode.Automatico)
                modo = instances.Count <= 3
                    ? ScheduleLayoutMode.ColunaUnica
                    : ScheduleLayoutMode.DuasColunas;

            // ── 7. Posicionar ─────────────────────────────────
            switch (modo)
            {
                case ScheduleLayoutMode.ColunaUnica:
                    PosicionarColunaUnica(
                        instances, dimensoes, sheetSize, areaOcupada, config);
                    break;

                case ScheduleLayoutMode.DuasColunas:
                    PosicionarDuasColunas(
                        instances, dimensoes, sheetSize, areaOcupada, config);
                    break;
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  FILTRAR VÁLIDOS
        // ══════════════════════════════════════════════════════════

        private static List<ViewSchedule> FiltrarValidos(
            Document doc,
            ViewSheet sheet,
            IList<ViewSchedule> schedules,
            ResultadoSchedulePlacement resultado)
        {
            var validos = new List<ViewSchedule>();

            // Schedules já na prancha
            var schedulesExistentes = new FilteredElementCollector(doc, sheet.Id)
                .OfClass(typeof(ScheduleSheetInstance))
                .Cast<ScheduleSheetInstance>()
                .Select(s => s.ScheduleId.Value)
                .ToHashSet();

            foreach (var schedule in schedules)
            {
                if (schedule == null)
                {
                    resultado.Ignoradas++;
                    continue;
                }

                // Já existe na prancha
                if (schedulesExistentes.Contains(schedule.Id.Value))
                {
                    resultado.Ignoradas++;
                    resultado.Mensagens.Add(
                        $"'{schedule.Name}' ignorada: já existe na prancha.");
                    continue;
                }

                // Validar compatibilidade
                try
                {
                    if (!ScheduleSheetInstance.IsValidScheduleForSheet(
                        doc, sheet.Id, schedule.Id))
                    {
                        resultado.Ignoradas++;
                        resultado.Mensagens.Add(
                            $"'{schedule.Name}' ignorada: incompatível com a prancha.");
                        continue;
                    }
                }
                catch
                {
                    resultado.Ignoradas++;
                    continue;
                }

                validos.Add(schedule);
            }

            return validos;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR INSTANCES
        // ══════════════════════════════════════════════════════════

        private static List<ScheduleSheetInstance> CriarInstances(
            Document doc,
            ViewSheet sheet,
            List<ViewSchedule> schedules,
            ResultadoSchedulePlacement resultado)
        {
            var instances = new List<ScheduleSheetInstance>();

            foreach (var schedule in schedules)
            {
                try
                {
                    var instance = ScheduleSheetInstance.Create(
                        doc, sheet.Id, schedule.Id, XYZ.Zero);

                    if (instance != null)
                    {
                        instances.Add(instance);
                        resultado.Instancias.Add(instance);
                        resultado.Adicionadas++;
                    }
                    else
                    {
                        resultado.Ignoradas++;
                    }
                }
                catch (Exception ex)
                {
                    resultado.Ignoradas++;
                    resultado.Mensagens.Add(
                        $"'{schedule.Name}' falhou: {ex.Message}");
                }
            }

            return instances;
        }

        // ══════════════════════════════════════════════════════════
        //  MEDIR
        // ══════════════════════════════════════════════════════════

        private static List<(double Largura, double Altura)> MedirInstances(
            ViewSheet sheet,
            List<ScheduleSheetInstance> instances)
        {
            var dimensoes = new List<(double, double)>();
            var fallbackW = UnitUtils.ConvertToInternalUnits(0.20, UnitTypeId.Meters);
            var fallbackH = UnitUtils.ConvertToInternalUnits(0.05, UnitTypeId.Meters);

            foreach (var instance in instances)
            {
                try
                {
                    var bbox = instance.get_BoundingBox(sheet);
                    if (bbox != null)
                    {
                        dimensoes.Add((
                            Math.Max(bbox.Max.X - bbox.Min.X, 0.001),
                            Math.Max(bbox.Max.Y - bbox.Min.Y, 0.001)));
                    }
                    else
                    {
                        dimensoes.Add((fallbackW, fallbackH));
                    }
                }
                catch
                {
                    dimensoes.Add((fallbackW, fallbackH));
                }
            }

            return dimensoes;
        }

        // ══════════════════════════════════════════════════════════
        //  LAYOUT: COLUNA ÚNICA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Empilha tabelas verticalmente de cima para baixo.
        /// </summary>
        private static void PosicionarColunaUnica(
            List<ScheduleSheetInstance> instances,
            List<(double Largura, double Altura)> dimensoes,
            XYZ sheetSize,
            double yOcupado,
            ScheduleLayoutConfig config)
        {
            var margem = UnitUtils.ConvertToInternalUnits(
                config.MargemM, UnitTypeId.Meters);
            var espacamento = UnitUtils.ConvertToInternalUnits(
                config.EspacamentoVerticalM, UnitTypeId.Meters);

            // Largura máxima das tabelas
            double larguraMax = dimensoes.Max(d => d.Largura);

            // Ponto inicial baseado na âncora
            double xBase;
            double yAtual;

            switch (config.Ancora)
            {
                case ScheduleAnchor.SuperiorDireito:
                    xBase = sheetSize.X - margem - larguraMax;
                    yAtual = sheetSize.Y - margem;
                    break;

                case ScheduleAnchor.InferiorEsquerdo:
                    xBase = margem;
                    yAtual = margem + dimensoes.Sum(d => d.Altura) +
                             espacamento * (dimensoes.Count - 1);
                    break;

                case ScheduleAnchor.Centro:
                    xBase = (sheetSize.X - larguraMax) / 2.0;
                    var alturaTotal = dimensoes.Sum(d => d.Altura) +
                                     espacamento * (dimensoes.Count - 1);
                    yAtual = (sheetSize.Y + alturaTotal) / 2.0;
                    break;

                case ScheduleAnchor.SuperiorEsquerdo:
                default:
                    xBase = margem;
                    yAtual = sheetSize.Y - margem;
                    break;
            }

            // Ajustar se há conteúdo existente
            if (yOcupado > 0)
                yAtual = Math.Min(yAtual, yOcupado - espacamento);

            // Posicionar cada tabela
            for (int i = 0; i < instances.Count; i++)
            {
                var (largura, altura) = dimensoes[i];

                try
                {
                    // ScheduleSheetInstance.Point = canto superior esquerdo
                    var ponto = new XYZ(xBase, yAtual - altura, 0);
                    instances[i].Point = ponto;

                    yAtual -= altura + espacamento;
                }
                catch { /* posicionamento falhou */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  LAYOUT: DUAS COLUNAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Distribui tabelas em duas colunas balanceadas por altura.
        /// </summary>
        private static void PosicionarDuasColunas(
            List<ScheduleSheetInstance> instances,
            List<(double Largura, double Altura)> dimensoes,
            XYZ sheetSize,
            double yOcupado,
            ScheduleLayoutConfig config)
        {
            var margem = UnitUtils.ConvertToInternalUnits(
                config.MargemM, UnitTypeId.Meters);
            var espV = UnitUtils.ConvertToInternalUnits(
                config.EspacamentoVerticalM, UnitTypeId.Meters);
            var espH = UnitUtils.ConvertToInternalUnits(
                config.EspacamentoHorizontalM, UnitTypeId.Meters);

            // Balancear por altura total
            var coluna1 = new List<int>();
            var coluna2 = new List<int>();
            double alturaCol1 = 0, alturaCol2 = 0;

            for (int i = 0; i < instances.Count; i++)
            {
                if (alturaCol1 <= alturaCol2)
                {
                    coluna1.Add(i);
                    alturaCol1 += dimensoes[i].Altura + espV;
                }
                else
                {
                    coluna2.Add(i);
                    alturaCol2 += dimensoes[i].Altura + espV;
                }
            }

            // Largura máxima por coluna
            double largCol1 = coluna1.Count > 0
                ? coluna1.Max(i => dimensoes[i].Largura) : 0;
            double largCol2 = coluna2.Count > 0
                ? coluna2.Max(i => dimensoes[i].Largura) : 0;

            // X base de cada coluna
            double x1 = margem;
            double x2 = margem + largCol1 + espH;

            // Y inicial
            double yInicial = sheetSize.Y - margem;
            if (yOcupado > 0)
                yInicial = Math.Min(yInicial, yOcupado - espV);

            // Posicionar coluna 1
            PosicionarColuna(instances, dimensoes, coluna1, x1, yInicial, espV);

            // Posicionar coluna 2
            PosicionarColuna(instances, dimensoes, coluna2, x2, yInicial, espV);
        }

        /// <summary>
        /// Posiciona uma coluna de tabelas.
        /// </summary>
        private static void PosicionarColuna(
            List<ScheduleSheetInstance> instances,
            List<(double Largura, double Altura)> dimensoes,
            List<int> indices,
            double xBase,
            double yInicial,
            double espacamento)
        {
            double yAtual = yInicial;

            foreach (var idx in indices)
            {
                var (_, altura) = dimensoes[idx];

                try
                {
                    instances[idx].Point = new XYZ(xBase, yAtual - altura, 0);
                    yAtual -= altura + espacamento;
                }
                catch { /* posicionamento falhou */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ÁREA OCUPADA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o Y mínimo de elementos já posicionados na prancha.
        /// Retorna 0 se não houver nada.
        /// </summary>
        private static double ObterAreaOcupada(Document doc, ViewSheet sheet)
        {
            double yMin = double.MaxValue;
            bool temElemento = false;

            // Viewports
            var viewports = new FilteredElementCollector(doc, sheet.Id)
                .OfClass(typeof(Viewport))
                .Cast<Viewport>()
                .ToList();

            foreach (var vp in viewports)
            {
                try
                {
                    var bbox = vp.get_BoundingBox(sheet);
                    if (bbox != null && bbox.Min.Y < yMin)
                    {
                        yMin = bbox.Min.Y;
                        temElemento = true;
                    }
                }
                catch { /* ignorar */ }
            }

            // ScheduleSheetInstances existentes
            var scheduleInstances = new FilteredElementCollector(doc, sheet.Id)
                .OfClass(typeof(ScheduleSheetInstance))
                .Cast<ScheduleSheetInstance>()
                .ToList();

            foreach (var si in scheduleInstances)
            {
                try
                {
                    var bbox = si.get_BoundingBox(sheet);
                    if (bbox != null && bbox.Min.Y < yMin)
                    {
                        yMin = bbox.Min.Y;
                        temElemento = true;
                    }
                }
                catch { /* ignorar */ }
            }

            return temElemento ? yMin : 0;
        }

        // ══════════════════════════════════════════════════════════
        //  TAMANHO DA PRANCHA
        // ══════════════════════════════════════════════════════════

        private static XYZ ObterTamanhoPrancha(Document doc, ViewSheet sheet)
        {
            try
            {
                var titleBlocks = new FilteredElementCollector(doc, sheet.Id)
                    .OfCategory(BuiltInCategory.OST_TitleBlocks)
                    .ToList();

                if (titleBlocks.Count > 0)
                {
                    var tb = titleBlocks.First();
                    var bbox = tb.get_BoundingBox(sheet);

                    if (bbox != null)
                    {
                        return new XYZ(
                            bbox.Max.X - bbox.Min.X,
                            bbox.Max.Y - bbox.Min.Y,
                            0);
                    }

                    var paramW = tb.get_Parameter(BuiltInParameter.SHEET_WIDTH);
                    var paramH = tb.get_Parameter(BuiltInParameter.SHEET_HEIGHT);

                    if (paramW != null && paramH != null)
                        return new XYZ(paramW.AsDouble(), paramH.AsDouble(), 0);
                }
            }
            catch { /* fallback */ }

            // A1
            return new XYZ(
                UnitUtils.ConvertToInternalUnits(0.841, UnitTypeId.Meters),
                UnitUtils.ConvertToInternalUnits(0.594, UnitTypeId.Meters),
                0);
        }
    }
}
