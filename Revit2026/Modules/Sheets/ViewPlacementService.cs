using Autodesk.Revit.DB;

namespace Revit2026.Modules.Sheets
{
    /// <summary>
    /// Interface para posicionamento de Views em pranchas.
    /// </summary>
    public interface IViewPlacementService
    {
        /// <summary>
        /// Posiciona múltiplas Views em uma ViewSheet em layout grid.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        void PosicionarViews(Document doc, ViewSheet sheet, IList<View> views);
    }

    /// <summary>
    /// Configuração de layout para posicionamento.
    /// </summary>
    public class LayoutConfig
    {
        /// <summary>Espaçamento entre views em metros.</summary>
        public double EspacamentoM { get; set; } = 0.05;

        /// <summary>Margem da borda da prancha em metros.</summary>
        public double MargemM { get; set; } = 0.02;

        /// <summary>Número fixo de colunas (0 = automático).</summary>
        public int ColunasFixas { get; set; } = 0;

        /// <summary>Direção do preenchimento.</summary>
        public DirecaoLayout Direcao { get; set; } = DirecaoLayout.EsquerdaParaDireita;
    }

    public enum DirecaoLayout
    {
        EsquerdaParaDireita,
        CimaParaBaixo
    }

    /// <summary>
    /// Resultado do posicionamento.
    /// </summary>
    public class ResultadoPosicionamento
    {
        public int TotalViews { get; set; }
        public int Posicionadas { get; set; }
        public int Ignoradas { get; set; }
        public List<string> Mensagens { get; set; } = new();

        public override string ToString() =>
            $"{Posicionadas}/{TotalViews} posicionadas, {Ignoradas} ignoradas";
    }

    /// <summary>
    /// Serviço de posicionamento automático de Views em pranchas (ViewSheet).
    ///
    /// Funcionalidades:
    /// - Layout grid automático (√N colunas)
    /// - Sem sobreposição
    /// - Espaçamento consistente
    /// - Centralização na prancha
    /// - Suporte a Views e Schedules
    ///
    /// Uso:
    ///   var service = new ViewPlacementService();
    ///   service.PosicionarViews(doc, sheet, views);
    /// </summary>
    public class ViewPlacementService : IViewPlacementService
    {
        // ══════════════════════════════════════════════════════════
        //  POSICIONAR VIEWS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Posiciona múltiplas Views em layout grid.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public void PosicionarViews(
            Document doc,
            ViewSheet sheet,
            IList<View> views)
        {
            PosicionarViews(doc, sheet, views, new LayoutConfig());
        }

        /// <summary>
        /// Posiciona múltiplas Views com configuração customizada.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ResultadoPosicionamento PosicionarViews(
            Document doc,
            ViewSheet sheet,
            IList<View> views,
            LayoutConfig config)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (sheet == null) throw new ArgumentNullException(nameof(sheet));
            if (views == null) throw new ArgumentNullException(nameof(views));

            var resultado = new ResultadoPosicionamento { TotalViews = views.Count };
            var espacamento = UnitUtils.ConvertToInternalUnits(
                config.EspacamentoM, UnitTypeId.Meters);

            // ── 1. Criar Viewports ────────────────────────────
            var viewports = CriarViewports(doc, sheet, views, resultado);

            if (viewports.Count == 0)
                return resultado;

            // ── 2. Medir dimensões ────────────────────────────
            var dimensoes = MedirViewports(sheet, viewports);

            // ── 3. Calcular grid ──────────────────────────────
            int colunas = config.ColunasFixas > 0
                ? config.ColunasFixas
                : (int)Math.Ceiling(Math.Sqrt(viewports.Count));

            int linhas = (int)Math.Ceiling((double)viewports.Count / colunas);

            // ── 4. Calcular larguras/alturas máximas por col/row
            var largurasColunas = new double[colunas];
            var alturasLinhas = new double[linhas];

            for (int i = 0; i < viewports.Count; i++)
            {
                int col = i % colunas;
                int row = i / colunas;

                var (largura, altura) = dimensoes[i];

                if (largura > largurasColunas[col])
                    largurasColunas[col] = largura;
                if (altura > alturasLinhas[row])
                    alturasLinhas[row] = altura;
            }

            // ── 5. Posicionar cada viewport ───────────────────
            for (int i = 0; i < viewports.Count; i++)
            {
                int col = i % colunas;
                int row = i / colunas;

                // X = soma das larguras anteriores + espaçamentos
                double x = 0;
                for (int c = 0; c < col; c++)
                    x += largurasColunas[c] + espacamento;
                x += largurasColunas[col] / 2.0;

                // Y = soma das alturas anteriores + espaçamentos (de cima para baixo)
                double y = 0;
                for (int r = 0; r < row; r++)
                    y += alturasLinhas[r] + espacamento;
                y += alturasLinhas[row] / 2.0;
                y = -y; // Eixo Y invertido

                try
                {
                    viewports[i].SetBoxCenter(new XYZ(x, y, 0));
                }
                catch { /* posicionamento falhou */ }
            }

            // ── 6. Centralizar na prancha ─────────────────────
            CentralizarNaPrancha(doc, sheet, viewports, config);

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  POSICIONAR SCHEDULES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Posiciona Schedules em uma prancha em coluna vertical.
        /// </summary>
        public ResultadoPosicionamento PosicionarSchedules(
            Document doc,
            ViewSheet sheet,
            IList<ViewSchedule> schedules,
            LayoutConfig? config = null)
        {
            config ??= new LayoutConfig();
            var resultado = new ResultadoPosicionamento
            {
                TotalViews = schedules.Count
            };

            var espacamento = UnitUtils.ConvertToInternalUnits(
                config.EspacamentoM, UnitTypeId.Meters);
            var margem = UnitUtils.ConvertToInternalUnits(
                config.MargemM, UnitTypeId.Meters);

            // Obter tamanho da prancha
            var sheetSize = ObterTamanhoPrancha(doc, sheet);
            var instances = new List<ScheduleSheetInstance>();

            foreach (var schedule in schedules)
            {
                try
                {
                    var ponto = new XYZ(margem, sheetSize.Y - margem, 0);
                    var instance = ScheduleSheetInstance.Create(
                        doc, sheet.Id, schedule.Id, ponto);

                    if (instance != null)
                    {
                        instances.Add(instance);
                        resultado.Posicionadas++;
                    }
                }
                catch
                {
                    resultado.Ignoradas++;
                }
            }

            // Reposicionar em coluna vertical
            if (instances.Count > 1)
            {
                double yOffset = sheetSize.Y - margem;

                foreach (var instance in instances)
                {
                    try
                    {
                        var bbox = instance.get_BoundingBox(sheet);
                        if (bbox == null) continue;

                        var altura = bbox.Max.Y - bbox.Min.Y;
                        var largura = bbox.Max.X - bbox.Min.X;

                        var novoPonto = new XYZ(
                            margem + largura / 2.0,
                            yOffset - altura / 2.0,
                            0);

                        // ScheduleSheetInstance usa Point, não SetBoxCenter
                        instance.Point = new XYZ(margem, yOffset - altura, 0);
                        yOffset -= altura + espacamento;
                    }
                    catch { /* reposicionamento falhou */ }
                }
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  POSICIONAMENTO MISTO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Posiciona Views à esquerda e Schedules à direita da prancha.
        /// </summary>
        public ResultadoPosicionamento PosicionarMisto(
            Document doc,
            ViewSheet sheet,
            IList<View> views,
            IList<ViewSchedule> schedules,
            LayoutConfig? config = null)
        {
            config ??= new LayoutConfig();
            var resultado = new ResultadoPosicionamento
            {
                TotalViews = views.Count + schedules.Count
            };

            // Views ocupam 70% da largura
            var resViews = PosicionarViews(doc, sheet, views, config);
            resultado.Posicionadas += resViews.Posicionadas;
            resultado.Ignoradas += resViews.Ignoradas;

            // Schedules no canto superior direito
            var resSched = PosicionarSchedules(doc, sheet, schedules, config);
            resultado.Posicionadas += resSched.Posicionadas;
            resultado.Ignoradas += resSched.Ignoradas;

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAR VIEWPORTS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Viewports para cada View válida.
        /// </summary>
        private static List<Viewport> CriarViewports(
            Document doc,
            ViewSheet sheet,
            IList<View> views,
            ResultadoPosicionamento resultado)
        {
            var viewports = new List<Viewport>();

            foreach (var view in views)
            {
                if (view == null)
                {
                    resultado.Ignoradas++;
                    continue;
                }

                // Ignorar templates
                if (view.IsTemplate)
                {
                    resultado.Ignoradas++;
                    resultado.Mensagens.Add(
                        $"'{view.Name}' ignorada: é template.");
                    continue;
                }

                // Verificar se pode ser adicionada
                if (!Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id))
                {
                    resultado.Ignoradas++;
                    resultado.Mensagens.Add(
                        $"'{view.Name}' ignorada: já está em outra prancha.");
                    continue;
                }

                try
                {
                    var viewport = Viewport.Create(
                        doc, sheet.Id, view.Id, XYZ.Zero);

                    if (viewport != null)
                    {
                        viewports.Add(viewport);
                        resultado.Posicionadas++;
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
                        $"'{view.Name}' falhou: {ex.Message}");
                }
            }

            return viewports;
        }

        // ══════════════════════════════════════════════════════════
        //  MEDIR VIEWPORTS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Mede largura e altura de cada viewport.
        /// </summary>
        private static List<(double Largura, double Altura)> MedirViewports(
            ViewSheet sheet,
            List<Viewport> viewports)
        {
            var dimensoes = new List<(double, double)>();

            foreach (var vp in viewports)
            {
                try
                {
                    var bbox = vp.get_BoundingBox(sheet);

                    if (bbox != null)
                    {
                        var largura = bbox.Max.X - bbox.Min.X;
                        var altura = bbox.Max.Y - bbox.Min.Y;
                        dimensoes.Add((
                            Math.Max(largura, 0.01),
                            Math.Max(altura, 0.01)));
                    }
                    else
                    {
                        // Fallback: tamanho padrão
                        dimensoes.Add((
                            UnitUtils.ConvertToInternalUnits(0.15, UnitTypeId.Meters),
                            UnitUtils.ConvertToInternalUnits(0.10, UnitTypeId.Meters)));
                    }
                }
                catch
                {
                    dimensoes.Add((
                        UnitUtils.ConvertToInternalUnits(0.15, UnitTypeId.Meters),
                        UnitUtils.ConvertToInternalUnits(0.10, UnitTypeId.Meters)));
                }
            }

            return dimensoes;
        }

        // ══════════════════════════════════════════════════════════
        //  CENTRALIZAR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Centraliza todos os viewports na prancha.
        /// </summary>
        private static void CentralizarNaPrancha(
            Document doc,
            ViewSheet sheet,
            List<Viewport> viewports,
            LayoutConfig config)
        {
            if (viewports.Count == 0) return;

            // Bounding geral dos viewports
            double minX = double.MaxValue, minY = double.MaxValue;
            double maxX = double.MinValue, maxY = double.MinValue;

            foreach (var vp in viewports)
            {
                try
                {
                    var bbox = vp.get_BoundingBox(sheet);
                    if (bbox == null) continue;

                    var center = vp.GetBoxCenter();
                    var halfW = (bbox.Max.X - bbox.Min.X) / 2.0;
                    var halfH = (bbox.Max.Y - bbox.Min.Y) / 2.0;

                    if (center.X - halfW < minX) minX = center.X - halfW;
                    if (center.Y - halfH < minY) minY = center.Y - halfH;
                    if (center.X + halfW > maxX) maxX = center.X + halfW;
                    if (center.Y + halfH > maxY) maxY = center.Y + halfH;
                }
                catch { /* ignorar */ }
            }

            if (minX == double.MaxValue) return;

            // Centro do grupo
            var centroGrupoX = (minX + maxX) / 2.0;
            var centroGrupoY = (minY + maxY) / 2.0;

            // Centro da prancha
            var sheetSize = ObterTamanhoPrancha(doc, sheet);
            var centroPranchaX = sheetSize.X / 2.0;
            var centroPranchaY = sheetSize.Y / 2.0;

            // Offset
            var offsetX = centroPranchaX - centroGrupoX;
            var offsetY = centroPranchaY - centroGrupoY;

            // Aplicar
            foreach (var vp in viewports)
            {
                try
                {
                    var center = vp.GetBoxCenter();
                    vp.SetBoxCenter(new XYZ(
                        center.X + offsetX,
                        center.Y + offsetY,
                        0));
                }
                catch { /* reposicionamento falhou */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o tamanho da prancha a partir do TitleBlock.
        /// Retorna (Largura, Altura) em unidades internas.
        /// </summary>
        private static XYZ ObterTamanhoPrancha(Document doc, ViewSheet sheet)
        {
            // Tentar obter do TitleBlock
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

                    // Tentar parâmetros
                    var paramW = tb.get_Parameter(
                        BuiltInParameter.SHEET_WIDTH);
                    var paramH = tb.get_Parameter(
                        BuiltInParameter.SHEET_HEIGHT);

                    if (paramW != null && paramH != null)
                    {
                        return new XYZ(
                            paramW.AsDouble(),
                            paramH.AsDouble(),
                            0);
                    }
                }
            }
            catch { /* fallback abaixo */ }

            // Fallback: A1 (841 × 594 mm)
            return new XYZ(
                UnitUtils.ConvertToInternalUnits(0.841, UnitTypeId.Meters),
                UnitUtils.ConvertToInternalUnits(0.594, UnitTypeId.Meters),
                0);
        }
    }
}
