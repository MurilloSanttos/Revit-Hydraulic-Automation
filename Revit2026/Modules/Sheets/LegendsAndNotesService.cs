using Autodesk.Revit.DB;

namespace Revit2026.Modules.Sheets
{
    /// <summary>
    /// Interface para adição de legendas e notas em pranchas.
    /// </summary>
    public interface ILegendsAndNotesService
    {
        void AdicionarLegendas(
            Document doc, ViewSheet sheet, IList<View> legendViews);

        void AdicionarNotas(
            Document doc, ViewSheet sheet, IList<string> notas);
    }

    /// <summary>
    /// Resultado da adição de legendas e notas.
    /// </summary>
    public class ResultadoLegendasNotas
    {
        public int LegendasAdicionadas { get; set; }
        public int LegendasIgnoradas { get; set; }
        public int NotasAdicionadas { get; set; }
        public List<string> Mensagens { get; set; } = new();

        public override string ToString() =>
            $"{LegendasAdicionadas} legendas, {NotasAdicionadas} notas";
    }

    /// <summary>
    /// Serviço de adição de Legend Views, Key Schedules e TextNotes em pranchas.
    ///
    /// Layout:
    /// - Legendas → lado direito da prancha (empilhadas verticalmente)
    /// - Notas → rodapé da prancha (empilhadas de baixo para cima)
    ///
    /// Funcionalidades:
    /// - Suporte a Legend Views e Key Schedules
    /// - Criação de TextNotes com tipo padrão
    /// - Prevenção de sobreposição via bounding boxes
    /// - Posicionamento automático com espaçamento consistente
    /// </summary>
    public class LegendsAndNotesService : ILegendsAndNotesService
    {
        private const double ESPACAMENTO_M = 0.03;
        private const double MARGEM_M = 0.025;
        private const double NOTA_ALTURA_M = 0.005;

        // ══════════════════════════════════════════════════════════
        //  ADICIONAR LEGENDAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Adiciona Legend Views e Key Schedules no lado direito da prancha.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public void AdicionarLegendas(
            Document doc,
            ViewSheet sheet,
            IList<View> legendViews)
        {
            AdicionarLegendas(doc, sheet, legendViews, out _);
        }

        /// <summary>
        /// Adiciona legendas com resultado detalhado.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ResultadoLegendasNotas AdicionarLegendas(
            Document doc,
            ViewSheet sheet,
            IList<View> legendViews,
            out List<Viewport> viewportsCriados)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (legendViews == null) throw new ArgumentNullException(nameof(legendViews));

            var resultado = new ResultadoLegendasNotas();
            viewportsCriados = new List<Viewport>();

            var sheetSize = ObterTamanhoPrancha(doc, sheet);
            var areaOcupada = ColetarBoundingBoxesExistentes(doc, sheet);
            var margem = UnitUtils.ConvertToInternalUnits(MARGEM_M, UnitTypeId.Meters);
            var espacamento = UnitUtils.ConvertToInternalUnits(ESPACAMENTO_M, UnitTypeId.Meters);

            // Filtrar views válidas
            var viewsValidas = FiltrarLegendasValidas(doc, sheet, legendViews, resultado);

            if (viewsValidas.Count == 0)
                return resultado;

            // Posicionar no lado direito, de cima para baixo
            double yAtual = sheetSize.Y - margem;

            // Calcular X base: lado direito da prancha
            double larguraMaxLegendas = 0;
            var dimensoes = new List<(double Largura, double Altura)>();

            // Criar viewports e medir
            var viewportsTemp = new List<Viewport>();

            foreach (var view in viewsValidas)
            {
                try
                {
                    if (!Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
                    {
                        resultado.LegendasIgnoradas++;
                        resultado.Mensagens.Add(
                            $"'{view.Name}' ignorada: já está em outra prancha.");
                        continue;
                    }

                    var viewport = Viewport.Create(
                        doc, sheet.Id, view.Id, XYZ.Zero);

                    if (viewport == null)
                    {
                        resultado.LegendasIgnoradas++;
                        continue;
                    }

                    viewportsTemp.Add(viewport);

                    var bbox = viewport.get_BoundingBox(sheet);
                    double largura, altura;

                    if (bbox != null)
                    {
                        largura = Math.Max(bbox.Max.X - bbox.Min.X, 0.001);
                        altura = Math.Max(bbox.Max.Y - bbox.Min.Y, 0.001);
                    }
                    else
                    {
                        largura = UnitUtils.ConvertToInternalUnits(0.10, UnitTypeId.Meters);
                        altura = UnitUtils.ConvertToInternalUnits(0.05, UnitTypeId.Meters);
                    }

                    dimensoes.Add((largura, altura));
                    if (largura > larguraMaxLegendas)
                        larguraMaxLegendas = largura;

                    resultado.LegendasAdicionadas++;
                }
                catch (Exception ex)
                {
                    resultado.LegendasIgnoradas++;
                    resultado.Mensagens.Add(
                        $"'{view.Name}' falhou: {ex.Message}");
                }
            }

            // X fixo: alinhado à direita
            double xBase = sheetSize.X - margem - larguraMaxLegendas / 2.0;

            // Ajustar Y inicial para evitar sobreposição
            foreach (var existente in areaOcupada)
            {
                if (existente.Max.X > xBase - larguraMaxLegendas / 2.0)
                {
                    double yExistente = existente.Min.Y;
                    if (yExistente < yAtual)
                        yAtual = yExistente - espacamento;
                }
            }

            // Posicionar viewports
            for (int i = 0; i < viewportsTemp.Count; i++)
            {
                var (largura, altura) = dimensoes[i];

                try
                {
                    var centro = new XYZ(xBase, yAtual - altura / 2.0, 0);
                    viewportsTemp[i].SetBoxCenter(centro);
                    viewportsCriados.Add(viewportsTemp[i]);

                    yAtual -= altura + espacamento;
                }
                catch { /* posicionamento falhou */ }
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  ADICIONAR NOTAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Adiciona TextNotes no rodapé da prancha.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public void AdicionarNotas(
            Document doc,
            ViewSheet sheet,
            IList<string> notas)
        {
            AdicionarNotasComResultado(doc, sheet, notas);
        }

        /// <summary>
        /// Adiciona notas com resultado detalhado.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ResultadoLegendasNotas AdicionarNotasComResultado(
            Document doc,
            ViewSheet sheet,
            IList<string> notas)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (notas == null) throw new ArgumentNullException(nameof(notas));

            var resultado = new ResultadoLegendasNotas();

            var sheetSize = ObterTamanhoPrancha(doc, sheet);
            var margem = UnitUtils.ConvertToInternalUnits(MARGEM_M, UnitTypeId.Meters);
            var espacamento = UnitUtils.ConvertToInternalUnits(ESPACAMENTO_M / 2.0, UnitTypeId.Meters);
            var alturaLinha = UnitUtils.ConvertToInternalUnits(NOTA_ALTURA_M, UnitTypeId.Meters);

            // Obter tipo de texto padrão
            var textNoteType = ObterTextNoteType(doc);
            if (textNoteType == null)
            {
                resultado.Mensagens.Add(
                    "Nenhum TextNoteType encontrado no projeto.");
                return resultado;
            }

            // Obter Y mínimo de elementos existentes
            var areaOcupada = ColetarBoundingBoxesExistentes(doc, sheet);
            double yMinExistente = sheetSize.Y;

            foreach (var bbox in areaOcupada)
            {
                if (bbox.Min.Y < yMinExistente)
                    yMinExistente = bbox.Min.Y;
            }

            // Y inicial: rodapé da prancha
            double yAtual = margem + (notas.Count - 1) * (alturaLinha + espacamento);

            // Garantir que não sobreponha com conteúdo existente
            yAtual = Math.Min(yAtual, yMinExistente - margem);

            // X centralizado
            double xBase = margem;

            // Largura máxima para wrapping
            double larguraMaxima = sheetSize.X - 2 * margem;

            for (int i = 0; i < notas.Count; i++)
            {
                var texto = notas[i];
                if (string.IsNullOrWhiteSpace(texto))
                    continue;

                try
                {
                    // Formatar com número
                    var textoFormatado = $"{i + 1}. {texto}";

                    var options = new TextNoteOptions(textNoteType.Id)
                    {
                        HorizontalAlignment = HorizontalTextAlignment.Left
                    };

                    var posicao = new XYZ(xBase, yAtual, 0);

                    TextNote.Create(doc, sheet.Id, posicao, textoFormatado, options);

                    yAtual -= alturaLinha + espacamento;
                    resultado.NotasAdicionadas++;
                }
                catch (Exception ex)
                {
                    resultado.Mensagens.Add(
                        $"Nota '{texto.Substring(0, Math.Min(30, texto.Length))}...' " +
                        $"falhou: {ex.Message}");
                }
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  ADICIONAR AMBOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Adiciona legendas e notas em uma única operação.
        /// </summary>
        public ResultadoLegendasNotas AdicionarLegendasENotas(
            Document doc,
            ViewSheet sheet,
            IList<View>? legendViews,
            IList<string>? notas)
        {
            var resultado = new ResultadoLegendasNotas();

            if (legendViews != null && legendViews.Count > 0)
            {
                var resLeg = AdicionarLegendas(doc, sheet, legendViews, out _);
                resultado.LegendasAdicionadas = resLeg.LegendasAdicionadas;
                resultado.LegendasIgnoradas = resLeg.LegendasIgnoradas;
                resultado.Mensagens.AddRange(resLeg.Mensagens);
            }

            if (notas != null && notas.Count > 0)
            {
                var resNotas = AdicionarNotasComResultado(doc, sheet, notas);
                resultado.NotasAdicionadas = resNotas.NotasAdicionadas;
                resultado.Mensagens.AddRange(resNotas.Mensagens);
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  NOTAS PADRÃO HIDRÁULICAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna notas técnicas padrão para pranchas hidráulicas.
        /// </summary>
        public static List<string> NotasPadraoHidraulica() => new()
        {
            "Todas as tubulações de água fria em PVC soldável conforme NBR 5648.",
            "Tubulações de esgoto em PVC série normal conforme NBR 5688.",
            "Inclinação mínima para tubulações de esgoto: 2% (1:50).",
            "Ralos sifonados conforme NBR 8160.",
            "Caixas de gordura conforme NBR 8160 — dimensionamento pelo projetista.",
            "Registros de gaveta em todas as derivações de coluna.",
            "Ventilação sanitária conforme NBR 8160.",
            "Cotas em metros, diâmetros em milímetros."
        };

        /// <summary>
        /// Retorna notas gerais padrão para pranchas.
        /// </summary>
        public static List<string> NotasPadraoGeral() => new()
        {
            "Verificar e confirmar todas as cotas em obra antes da execução.",
            "Cotas em metros, salvo indicação em contrário.",
            "Este projeto deve ser lido em conjunto com os projetos complementares.",
            "Quaisquer divergências devem ser comunicadas ao projetista."
        };

        // ══════════════════════════════════════════════════════════
        //  FILTRAR LEGENDAS
        // ══════════════════════════════════════════════════════════

        private static List<View> FiltrarLegendasValidas(
            Document doc,
            ViewSheet sheet,
            IList<View> legendViews,
            ResultadoLegendasNotas resultado)
        {
            var validas = new List<View>();

            foreach (var view in legendViews)
            {
                if (view == null)
                {
                    resultado.LegendasIgnoradas++;
                    continue;
                }

                // Aceitar Legend Views
                if (view.ViewType == ViewType.Legend)
                {
                    validas.Add(view);
                    continue;
                }

                // Aceitar Key Schedules
                if (view.ViewType == ViewType.Schedule &&
                    view is ViewSchedule schedule)
                {
                    try
                    {
                        if (schedule.Definition.IsKeySchedule)
                        {
                            validas.Add(view);
                            continue;
                        }
                    }
                    catch { /* não é key schedule */ }
                }

                // Rejeitar restante
                resultado.LegendasIgnoradas++;
                resultado.Mensagens.Add(
                    $"'{view.Name}' ignorada: tipo {view.ViewType} " +
                    $"não é Legend nem Key Schedule.");
            }

            return validas;
        }

        // ══════════════════════════════════════════════════════════
        //  TEXTNOTE TYPE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o TextNoteType padrão. Prioriza o menor tamanho de texto.
        /// </summary>
        private static TextNoteType? ObterTextNoteType(Document doc)
        {
            var tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(TextNoteType))
                .Cast<TextNoteType>()
                .ToList();

            if (tipos.Count == 0)
                return null;

            // Priorizar tipo com tamanho menor (notas pequenas)
            return tipos
                .OrderBy(t =>
                {
                    try
                    {
                        var param = t.get_Parameter(
                            BuiltInParameter.TEXT_SIZE);
                        return param?.AsDouble() ?? double.MaxValue;
                    }
                    catch { return double.MaxValue; }
                })
                .First();
        }

        // ══════════════════════════════════════════════════════════
        //  BOUNDING BOXES EXISTENTES
        // ══════════════════════════════════════════════════════════

        private static List<BoundingBoxXYZ> ColetarBoundingBoxesExistentes(
            Document doc,
            ViewSheet sheet)
        {
            var bboxes = new List<BoundingBoxXYZ>();

            // Viewports
            try
            {
                var viewports = new FilteredElementCollector(doc, sheet.Id)
                    .OfClass(typeof(Viewport))
                    .Cast<Viewport>();

                foreach (var vp in viewports)
                {
                    try
                    {
                        var bbox = vp.get_BoundingBox(sheet);
                        if (bbox != null) bboxes.Add(bbox);
                    }
                    catch { /* ignorar */ }
                }
            }
            catch { /* ignorar */ }

            // Schedule instances
            try
            {
                var schedules = new FilteredElementCollector(doc, sheet.Id)
                    .OfClass(typeof(ScheduleSheetInstance))
                    .Cast<ScheduleSheetInstance>();

                foreach (var si in schedules)
                {
                    try
                    {
                        var bbox = si.get_BoundingBox(sheet);
                        if (bbox != null) bboxes.Add(bbox);
                    }
                    catch { /* ignorar */ }
                }
            }
            catch { /* ignorar */ }

            // TextNotes
            try
            {
                var textNotes = new FilteredElementCollector(doc, sheet.Id)
                    .OfClass(typeof(TextNote))
                    .Cast<TextNote>();

                foreach (var tn in textNotes)
                {
                    try
                    {
                        var bbox = tn.get_BoundingBox(sheet);
                        if (bbox != null) bboxes.Add(bbox);
                    }
                    catch { /* ignorar */ }
                }
            }
            catch { /* ignorar */ }

            return bboxes;
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
                        return new XYZ(
                            bbox.Max.X - bbox.Min.X,
                            bbox.Max.Y - bbox.Min.Y, 0);

                    var paramW = tb.get_Parameter(BuiltInParameter.SHEET_WIDTH);
                    var paramH = tb.get_Parameter(BuiltInParameter.SHEET_HEIGHT);

                    if (paramW != null && paramH != null)
                        return new XYZ(paramW.AsDouble(), paramH.AsDouble(), 0);
                }
            }
            catch { /* fallback */ }

            return new XYZ(
                UnitUtils.ConvertToInternalUnits(0.841, UnitTypeId.Meters),
                UnitUtils.ConvertToInternalUnits(0.594, UnitTypeId.Meters), 0);
        }
    }
}
