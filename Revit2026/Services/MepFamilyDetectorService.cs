using Autodesk.Revit.DB;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço de detecção de famílias MEP carregadas no modelo.
    /// Identifica famílias e símbolos disponíveis para inserção
    /// automática pelo pipeline hidráulico.
    ///
    /// Categorias suportadas:
    /// - PlumbingFixtures (louças, vasos, pias)
    /// - PipeFittings (conexões de tubulação)
    /// - PipeAccessory (registros, válvulas)
    /// - MechanicalEquipment (bombas, aquecedores)
    /// - SpecialityEquipment (ralos, caixas sifonadas)
    /// - Sprinklers (sprinklers)
    /// </summary>
    public class MepFamilyDetectorService
    {
        private const string ETAPA = "03_Equipamentos";
        private const string COMPONENTE = "FamilyDetector";

        /// <summary>
        /// Conjunto de categorias MEP suportadas pelo pipeline hidráulico.
        /// </summary>
        private static readonly HashSet<BuiltInCategory> CategoriasMep = new()
        {
            BuiltInCategory.OST_PlumbingFixtures,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_PipeAccessory,
            BuiltInCategory.OST_MechanicalEquipment,
            BuiltInCategory.OST_SpecialityEquipment,
            BuiltInCategory.OST_Sprinklers,
            BuiltInCategory.OST_PipeCurves,
        };

        // ══════════════════════════════════════════════════════════
        //  DETECÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Detecta todas as famílias MEP carregadas no modelo.
        /// Retorna catálogo com mapeamento Família → Símbolos.
        /// </summary>
        public MepFamilyCatalog DetectarFamiliasCarregadas(Document doc, ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (log == null) throw new ArgumentNullException(nameof(log));

            log.Info(ETAPA, COMPONENTE,
                "Iniciando detecção de famílias MEP carregadas...");

            var catalogo = new MepFamilyCatalog();

            try
            {
                // ── 1. Coletar todas as famílias ──────────────
                var familias = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .ToList();

                log.Info(ETAPA, COMPONENTE,
                    $"{familias.Count} famílias totais encontradas no modelo.");

                // ── 2. Filtrar e processar famílias MEP ───────
                foreach (var familia in familias)
                {
                    try
                    {
                        if (!EhCategoriaMep(familia))
                            continue;

                        ProcessarFamilia(doc, familia, catalogo, log);
                    }
                    catch (Exception ex)
                    {
                        log.Medio(ETAPA, COMPONENTE,
                            $"Erro ao processar família '{familia.Name}': {ex.Message}",
                            familia.Id.Value);
                    }
                }

                // ── 3. Contabilizar por categoria ─────────────
                ContabilizarPorCategoria(doc, catalogo, log);

                // ── 4. Resumo ─────────────────────────────────
                log.Info(ETAPA, COMPONENTE,
                    $"Detecção concluída: {catalogo}");

                if (catalogo.FamiliasSemSimbolos.Count > 0)
                {
                    var nomes = string.Join(", ",
                        catalogo.FamiliasSemSimbolos.Select(f => $"'{f}'"));
                    log.Medio(ETAPA, COMPONENTE,
                        $"Famílias sem símbolos: {nomes}");
                }

                // Resumo por categoria
                foreach (var kvp in catalogo.FamiliasPorCategoria.OrderByDescending(k => k.Value))
                {
                    log.Info(ETAPA, COMPONENTE,
                        $"  {kvp.Key}: {kvp.Value} família(s)");
                }
            }
            catch (Exception ex)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Erro ao detectar famílias MEP: {ex.Message}",
                    detalhes: ex.StackTrace);
            }

            return catalogo;
        }

        // ══════════════════════════════════════════════════════════
        //  PROCESSAMENTO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Processa uma família: extrai seus símbolos e adiciona ao catálogo.
        /// </summary>
        private static void ProcessarFamilia(
            Document doc, Family familia, MepFamilyCatalog catalogo, ILogService log)
        {
            var nomeFamilia = familia.Name ?? string.Empty;

            // Obter FamilySymbols (tipos) desta família
            var symbolIds = familia.GetFamilySymbolIds();

            if (symbolIds == null || symbolIds.Count == 0)
            {
                catalogo.FamiliasSemSimbolos.Add(nomeFamilia);
                log.Medio(ETAPA, COMPONENTE,
                    $"Família MEP sem símbolos carregados: {nomeFamilia}",
                    familia.Id.Value);
                return;
            }

            var simbolos = new List<string>();

            foreach (var symbolId in symbolIds)
            {
                var symbol = doc.GetElement(symbolId) as FamilySymbol;
                if (symbol != null)
                {
                    simbolos.Add(symbol.Name ?? string.Empty);
                }
            }

            if (simbolos.Count > 0)
            {
                catalogo.FamiliaParaSimbolos[nomeFamilia] = simbolos;
                log.Info(ETAPA, COMPONENTE,
                    $"Família MEP detectada: {nomeFamilia} ({simbolos.Count} símbolos)");
            }
            else
            {
                catalogo.FamiliasSemSimbolos.Add(nomeFamilia);
                log.Medio(ETAPA, COMPONENTE,
                    $"Família MEP sem símbolos carregados: {nomeFamilia}",
                    familia.Id.Value);
            }
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DE CATEGORIA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se a família pertence a uma categoria MEP suportada.
        /// </summary>
        private static bool EhCategoriaMep(Family familia)
        {
            try
            {
                var catId = familia.FamilyCategoryId;
                if (catId == ElementId.InvalidElementId)
                    return false;

                var builtIn = (BuiltInCategory)catId.Value;
                return CategoriasMep.Contains(builtIn);
            }
            catch
            {
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  CONTABILIZAÇÃO POR CATEGORIA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Contabiliza famílias por categoria MEP para o relatório.
        /// </summary>
        private static void ContabilizarPorCategoria(
            Document doc, MepFamilyCatalog catalogo, ILogService log)
        {
            try
            {
                var familias = new FilteredElementCollector(doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(EhCategoriaMep)
                    .ToList();

                var porCategoria = familias
                    .GroupBy(f =>
                    {
                        try
                        {
                            var cat = (BuiltInCategory)f.FamilyCategoryId.Value;
                            return ObterNomeCategoria(cat);
                        }
                        catch
                        {
                            return "Desconhecida";
                        }
                    })
                    .ToDictionary(g => g.Key, g => g.Count());

                catalogo.FamiliasPorCategoria = porCategoria;
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao contabilizar categorias: {ex.Message}");
            }
        }

        /// <summary>
        /// Converte BuiltInCategory para nome legível em PT-BR.
        /// </summary>
        private static string ObterNomeCategoria(BuiltInCategory cat)
        {
            return cat switch
            {
                BuiltInCategory.OST_PlumbingFixtures => "Louças/Aparelhos Sanitários",
                BuiltInCategory.OST_PipeFitting => "Conexões de Tubulação",
                BuiltInCategory.OST_PipeAccessory => "Acessórios de Tubulação",
                BuiltInCategory.OST_MechanicalEquipment => "Equipamentos Mecânicos",
                BuiltInCategory.OST_SpecialityEquipment => "Equipamentos Especiais",
                BuiltInCategory.OST_Sprinklers => "Sprinklers",
                BuiltInCategory.OST_PipeCurves => "Tubulações",
                _ => cat.ToString()
            };
        }
    }
}
