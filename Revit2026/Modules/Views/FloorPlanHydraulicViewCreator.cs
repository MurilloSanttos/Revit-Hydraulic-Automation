using Autodesk.Revit.DB;

namespace Revit2026.Modules.Views
{
    /// <summary>
    /// Interface para criação de Floor Plan views hidráulicas.
    /// </summary>
    public interface IFloorPlanHydraulicViewCreator
    {
        /// <summary>
        /// Cria Floor Plan filtrada para disciplina hidráulica.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        ViewPlan Criar(Document doc, Level level);
    }

    /// <summary>
    /// Resultado da criação de Floor Plan hidráulica.
    /// </summary>
    public class ResultadoCriacaoView
    {
        public bool Sucesso { get; set; }
        public ViewPlan? View { get; set; }
        public string Nome { get; set; } = "";
        public string Mensagem { get; set; } = "";
        public int FiltrosAplicados { get; set; }
        public int CategoriasOcultas { get; set; }
    }

    /// <summary>
    /// Criador de Floor Plan Views para disciplina hidráulica.
    ///
    /// Funcionalidades:
    /// - Cria ViewPlan baseada em Level
    /// - Aplica disciplina Mechanical (hidráulica)
    /// - Oculta categorias irrelevantes (Estrutural, Arquitetura, Elétrica)
    /// - Destaca elementos hidráulicos com cor azul
    /// - Escala 1:50, DetailLevel Fine
    /// - Nomenclatura: "HID - {Level}" com sufixo incremental
    ///
    /// Uso:
    ///   var creator = new FloorPlanHydraulicViewCreator();
    ///   var view = creator.Criar(doc, level);
    /// </summary>
    public class FloorPlanHydraulicViewCreator : IFloorPlanHydraulicViewCreator
    {
        private const string PREFIXO = "HID";
        private static readonly Color COR_HIDRAULICA = new(0, 80, 255);

        // Categorias a ocultar (não hidráulicas)
        private static readonly BuiltInCategory[] CategoriasOcultar =
        {
            BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_StructuralFoundation,
            BuiltInCategory.OST_Rebar,
            BuiltInCategory.OST_ElectricalEquipment,
            BuiltInCategory.OST_ElectricalFixtures,
            BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_CableTrayFitting,
            BuiltInCategory.OST_Conduit,
            BuiltInCategory.OST_ConduitFitting,
            BuiltInCategory.OST_LightingFixtures,
            BuiltInCategory.OST_LightingDevices,
            BuiltInCategory.OST_CommunicationDevices,
            BuiltInCategory.OST_FireAlarmDevices,
            BuiltInCategory.OST_DataDevices,
            BuiltInCategory.OST_Topography,
            BuiltInCategory.OST_Furniture,
            BuiltInCategory.OST_FurnitureSystems,
            BuiltInCategory.OST_Casework,
            BuiltInCategory.OST_Entourage,
            BuiltInCategory.OST_Planting,
            BuiltInCategory.OST_Site,
            BuiltInCategory.OST_Ramps,
            BuiltInCategory.OST_StairsRailing,
            BuiltInCategory.OST_Parking,
        };

        // Categorias hidráulicas a destacar com cor
        private static readonly BuiltInCategory[] CategoriasHidraulicas =
        {
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_PipeAccessory,
            BuiltInCategory.OST_PipeInsulations,
            BuiltInCategory.OST_PlumbingFixtures,
            BuiltInCategory.OST_Sprinklers,
            BuiltInCategory.OST_FlexPipeCurves,
            BuiltInCategory.OST_MechanicalEquipment,
            BuiltInCategory.OST_FabricationPipework,
        };

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Floor Plan filtrada para disciplina hidráulica.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ViewPlan Criar(Document doc, Level level)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (level == null)
                throw new ArgumentNullException(nameof(level));

            // ── 1. Obter ViewFamilyType ───────────────────────
            var viewFamilyType = ObterViewFamilyType(doc);

            // ── 2. Criar ViewPlan ─────────────────────────────
            var view = ViewPlan.Create(doc, viewFamilyType.Id, level.Id);

            if (view == null)
                throw new InvalidOperationException(
                    $"Revit retornou null ao criar ViewPlan para Level '{level.Name}'.");

            // ── 3. Nomear ─────────────────────────────────────
            view.Name = GerarNomeUnico(doc, level.Name);

            // ── 4. Configurar disciplina ──────────────────────
            ConfigurarDisciplina(view);

            // ── 5. Configurar escala e detalhe ────────────────
            ConfigurarEscalaDetalhe(view);

            // ── 6. Ocultar categorias irrelevantes ────────────
            OcultarCategoriasIrrelevantes(doc, view);

            // ── 7. Destacar categorias hidráulicas ────────────
            DestacarCategoriasHidraulicas(doc, view);

            // ── 8. Aplicar filtros paramétricos ───────────────
            AplicarFiltrosHidraulicos(doc, view);

            return view;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Floor Plans hidráulicas para todos os Levels do projeto.
        /// </summary>
        public List<ResultadoCriacaoView> CriarParaTodosLevels(Document doc)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .ToList();

            var resultados = new List<ResultadoCriacaoView>();

            using var trans = new Transaction(doc, "Criar Floor Plans Hidráulicas");
            trans.Start();

            try
            {
                foreach (var level in levels)
                {
                    try
                    {
                        var view = Criar(doc, level);
                        resultados.Add(new ResultadoCriacaoView
                        {
                            Sucesso = true,
                            View = view,
                            Nome = view.Name,
                            Mensagem = "View criada com sucesso."
                        });
                    }
                    catch (Exception ex)
                    {
                        resultados.Add(new ResultadoCriacaoView
                        {
                            Nome = $"{PREFIXO} - {level.Name}",
                            Mensagem = $"Falha: {ex.Message}"
                        });
                    }
                }

                if (resultados.Any(r => r.Sucesso))
                    trans.Commit();
                else
                    trans.RollBack();
            }
            catch
            {
                if (trans.HasStarted())
                    trans.RollBack();
            }

            return resultados;
        }

        /// <summary>
        /// Cria Floor Plans hidráulicas para Levels específicos.
        /// </summary>
        public List<ResultadoCriacaoView> CriarParaLevels(
            Document doc,
            IList<Level> levels)
        {
            var resultados = new List<ResultadoCriacaoView>();

            using var trans = new Transaction(doc, "Criar Floor Plans Hidráulicas");
            trans.Start();

            try
            {
                foreach (var level in levels)
                {
                    try
                    {
                        var view = Criar(doc, level);
                        resultados.Add(new ResultadoCriacaoView
                        {
                            Sucesso = true,
                            View = view,
                            Nome = view.Name
                        });
                    }
                    catch (Exception ex)
                    {
                        resultados.Add(new ResultadoCriacaoView
                        {
                            Nome = $"{PREFIXO} - {level.Name}",
                            Mensagem = ex.Message
                        });
                    }
                }

                if (resultados.Any(r => r.Sucesso))
                    trans.Commit();
                else
                    trans.RollBack();
            }
            catch
            {
                if (trans.HasStarted())
                    trans.RollBack();
            }

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  VIEWFAMILYTYPE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o ViewFamilyType para FloorPlan.
        /// </summary>
        private static ViewFamilyType ObterViewFamilyType(Document doc)
        {
            var tipos = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewFamilyType))
                .Cast<ViewFamilyType>()
                .Where(t => t.ViewFamily == ViewFamily.FloorPlan)
                .ToList();

            if (tipos.Count == 0)
                throw new InvalidOperationException(
                    "Nenhum ViewFamilyType de FloorPlan encontrado no projeto.");

            return tipos.First();
        }

        // ══════════════════════════════════════════════════════════
        //  DISCIPLINA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Configura disciplina Mechanical e subdisciplina Hidráulica.
        /// </summary>
        private static void ConfigurarDisciplina(ViewPlan view)
        {
            try
            {
                var paramDisciplina = view.get_Parameter(
                    BuiltInParameter.VIEW_DISCIPLINE);

                if (paramDisciplina != null && !paramDisciplina.IsReadOnly)
                    paramDisciplina.Set((int)ViewDiscipline.Mechanical);
            }
            catch { /* disciplina não aplicável */ }

            // SubDisciplina "Hidráulica" (parâmetro compartilhado)
            try
            {
                var paramSub = view.LookupParameter("SubDisciplina");
                if (paramSub != null && !paramSub.IsReadOnly &&
                    paramSub.StorageType == StorageType.String)
                {
                    paramSub.Set("Hidráulica");
                }
            }
            catch { /* parâmetro não existe — silencioso */ }

            // Comments — identificar como hidráulica
            try
            {
                var paramComments = view.get_Parameter(
                    BuiltInParameter.VIEW_DESCRIPTION);
                if (paramComments != null && !paramComments.IsReadOnly)
                    paramComments.Set("Planta Hidráulica — gerada automaticamente");
            }
            catch { /* silencioso */ }
        }

        // ══════════════════════════════════════════════════════════
        //  ESCALA E DETALHE
        // ══════════════════════════════════════════════════════════

        private static void ConfigurarEscalaDetalhe(ViewPlan view)
        {
            try { view.Scale = 50; }
            catch { /* escala não configurável */ }

            try { view.DetailLevel = ViewDetailLevel.Fine; }
            catch { /* detalhamento não configurável */ }

            try
            {
                // Revit 2026: VIEW_UNDERLAY_ID removed; use ViewPlan properties
                view.SetUnderlayBaseLevel(ElementId.InvalidElementId);
            }
            catch { /* underlay não configurável */ }
        }

        // ══════════════════════════════════════════════════════════
        //  OCULTAR CATEGORIAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Oculta categorias não hidráulicas na view.
        /// </summary>
        private static void OcultarCategoriasIrrelevantes(Document doc, ViewPlan view)
        {
            foreach (var bic in CategoriasOcultar)
            {
                try
                {
                    var catId = new ElementId(bic);
                    var category = Category.GetCategory(doc, bic);

                    if (category == null)
                        continue;

                    if (view.CanCategoryBeHidden(catId))
                        view.SetCategoryHidden(catId, true);
                }
                catch { /* categoria não existe ou não pode ser ocultada */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  DESTACAR CATEGORIAS HIDRÁULICAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Aplica cor azul às categorias hidráulicas.
        /// </summary>
        private static void DestacarCategoriasHidraulicas(Document doc, ViewPlan view)
        {
            var ogs = new OverrideGraphicSettings();
            ogs.SetProjectionLineColor(COR_HIDRAULICA);
            ogs.SetProjectionLineWeight(3);

            foreach (var bic in CategoriasHidraulicas)
            {
                try
                {
                    var catId = new ElementId(bic);
                    var category = Category.GetCategory(doc, bic);

                    if (category == null)
                        continue;

                    view.SetCategoryOverrides(catId, ogs);
                }
                catch { /* categoria não existe — silencioso */ }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  FILTROS PARAMÉTRICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria e aplica ParameterFilterElement para elementos hidráulicos.
        /// </summary>
        private static void AplicarFiltrosHidraulicos(Document doc, ViewPlan view)
        {
            // Filtro: Pipes por classificação de sistema
            CriarFiltroSistema(doc, view,
                "HID_Agua_Fria",
                BuiltInCategory.OST_PipeCurves,
                "Domestic Cold Water",
                new Color(0, 100, 255));    // Azul

            CriarFiltroSistema(doc, view,
                "HID_Agua_Quente",
                BuiltInCategory.OST_PipeCurves,
                "Domestic Hot Water",
                new Color(255, 60, 60));    // Vermelho

            CriarFiltroSistema(doc, view,
                "HID_Esgoto",
                BuiltInCategory.OST_PipeCurves,
                "Sanitary",
                new Color(139, 90, 43));    // Marrom

            CriarFiltroSistema(doc, view,
                "HID_Ventilacao",
                BuiltInCategory.OST_PipeCurves,
                "Vent",
                new Color(128, 128, 128));  // Cinza

            CriarFiltroSistema(doc, view,
                "HID_Agua_Pluvial",
                BuiltInCategory.OST_PipeCurves,
                "Storm",
                new Color(0, 180, 0));      // Verde
        }

        /// <summary>
        /// Cria ParameterFilterElement por classificação de sistema.
        /// </summary>
        private static void CriarFiltroSistema(
            Document doc,
            ViewPlan view,
            string nomeFiltro,
            BuiltInCategory categoria,
            string valorSistema,
            Color cor)
        {
            try
            {
                // Verificar se o filtro já existe
                var filtroExistente = new FilteredElementCollector(doc)
                    .OfClass(typeof(ParameterFilterElement))
                    .Cast<ParameterFilterElement>()
                    .FirstOrDefault(f => f.Name == nomeFiltro);

                ParameterFilterElement filtro;

                if (filtroExistente != null)
                {
                    filtro = filtroExistente;
                }
                else
                {
                    // Criar regra de filtro
                    var paramId = new ElementId(
                        BuiltInParameter.RBS_SYSTEM_CLASSIFICATION_PARAM);

                    var rule = ParameterFilterRuleFactory.CreateContainsRule(
                        paramId, valorSistema);

                    var elementFilter = new ElementParameterFilter(rule);

                    var categoryIds = new List<ElementId>
                    {
                        new ElementId(categoria)
                    };

                    filtro = ParameterFilterElement.Create(
                        doc, nomeFiltro, categoryIds, elementFilter);
                }

                // Aplicar à view
                if (!view.IsFilterApplied(filtro.Id))
                    view.AddFilter(filtro.Id);

                // Override gráfico
                var ogs = new OverrideGraphicSettings();
                ogs.SetProjectionLineColor(cor);
                ogs.SetProjectionLineWeight(4);
                ogs.SetSurfaceForegroundPatternColor(cor);

                view.SetFilterOverrides(filtro.Id, ogs);
            }
            catch { /* filtro não pôde ser criado — silencioso */ }
        }

        // ══════════════════════════════════════════════════════════
        //  NOMENCLATURA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera nome único: "HID - {Level}" com sufixo incremental.
        /// </summary>
        private static string GerarNomeUnico(Document doc, string nomeLevel)
        {
            var nomeBase = $"{PREFIXO} - {nomeLevel}";
            var nomesExistentes = ObterNomesViewsExistentes(doc);

            if (!nomesExistentes.Contains(nomeBase))
                return nomeBase;

            for (int i = 2; i <= 999; i++)
            {
                var candidato = $"{nomeBase} ({i})";
                if (!nomesExistentes.Contains(candidato))
                    return candidato;
            }

            return $"{nomeBase} ({DateTime.Now:HHmmss})";
        }

        /// <summary>
        /// Obtém nomes de todas as views existentes.
        /// </summary>
        private static HashSet<string> ObterNomesViewsExistentes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => !v.IsTemplate)
                .Select(v => v.Name)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
