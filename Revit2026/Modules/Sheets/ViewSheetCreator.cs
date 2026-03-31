using Autodesk.Revit.DB;

namespace Revit2026.Modules.Sheets
{
    /// <summary>
    /// Interface para criação de pranchas (ViewSheet).
    /// </summary>
    public interface IViewSheetCreator
    {
        /// <summary>
        /// Cria uma nova ViewSheet com TitleBlock padrão.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        ViewSheet Criar(Document doc, string nome, string numero);

        /// <summary>
        /// Cria uma nova ViewSheet com TitleBlock específico.
        /// </summary>
        ViewSheet Criar(Document doc, string nome, string numero, ElementId titleBlockId);
    }

    /// <summary>
    /// Resultado da criação de prancha.
    /// </summary>
    public class ResultadoCriacaoPrancha
    {
        public bool Sucesso { get; set; }
        public ViewSheet? Prancha { get; set; }
        public string Numero { get; set; } = "";
        public string Nome { get; set; } = "";
        public string Mensagem { get; set; } = "";

        public override string ToString() =>
            $"{Numero} - {Nome}: {(Sucesso ? "OK" : "FALHA")}";
    }

    /// <summary>
    /// Serviço de criação de pranchas (ViewSheet) com TitleBlock padrão.
    ///
    /// Funcionalidades:
    /// - Busca automática de TitleBlock no projeto
    /// - Validação de número duplicado com sufixo incremental
    /// - Ativação automática de FamilySymbol
    /// - Criação individual e em lote
    ///
    /// Uso:
    ///   var creator = new ViewSheetCreator();
    ///   var sheet = creator.Criar(doc, "Planta Hidráulica", "H-001");
    /// </summary>
    public class ViewSheetCreator : IViewSheetCreator
    {
        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO COM TITLEBLOCK AUTOMÁTICO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria ViewSheet com TitleBlock padrão do projeto.
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ViewSheet Criar(Document doc, string nome, string numero)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var titleBlock = BuscarTitleBlockPadrao(doc);

            if (titleBlock == null)
                throw new InvalidOperationException(
                    "Nenhum TitleBlock encontrado no projeto. " +
                    "Carregue uma família de Folha de Desenho antes de criar pranchas.");

            return CriarPrancha(doc, nome, numero, titleBlock);
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO COM TITLEBLOCK ESPECÍFICO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria ViewSheet com TitleBlock específico (por ElementId).
        /// Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ViewSheet Criar(Document doc, string nome, string numero, ElementId titleBlockId)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));
            if (titleBlockId == null || titleBlockId == ElementId.InvalidElementId)
                throw new ArgumentException("TitleBlock Id inválido.", nameof(titleBlockId));

            var titleBlock = doc.GetElement(titleBlockId) as FamilySymbol;

            if (titleBlock == null)
                throw new InvalidOperationException(
                    $"TitleBlock não encontrado (Id={titleBlockId.Value}).");

            return CriarPrancha(doc, nome, numero, titleBlock);
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria múltiplas pranchas dentro de uma Transaction.
        /// </summary>
        public List<ResultadoCriacaoPrancha> CriarLote(
            Document doc,
            IList<(string Nome, string Numero)> pranchas)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var resultados = new List<ResultadoCriacaoPrancha>();
            var titleBlock = BuscarTitleBlockPadrao(doc);

            if (titleBlock == null)
            {
                resultados.Add(new ResultadoCriacaoPrancha
                {
                    Mensagem = "Nenhum TitleBlock encontrado no projeto."
                });
                return resultados;
            }

            using var trans = new Transaction(doc, "Criar Pranchas em Lote");
            trans.Start();

            try
            {
                foreach (var (nome, numero) in pranchas)
                {
                    try
                    {
                        var sheet = CriarPrancha(doc, nome, numero, titleBlock);
                        resultados.Add(new ResultadoCriacaoPrancha
                        {
                            Sucesso = true,
                            Prancha = sheet,
                            Numero = sheet.SheetNumber,
                            Nome = sheet.Name,
                            Mensagem = "Prancha criada com sucesso."
                        });
                    }
                    catch (Exception ex)
                    {
                        resultados.Add(new ResultadoCriacaoPrancha
                        {
                            Nome = nome,
                            Numero = numero,
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

        // ══════════════════════════════════════════════════════════
        //  ADICIONAR VIEWS À PRANCHA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Posiciona um ViewSchedule na prancha.
        /// </summary>
        public static ScheduleSheetInstance? AdicionarSchedule(
            Document doc,
            ViewSheet sheet,
            ViewSchedule schedule,
            XYZ? posicao = null)
        {
            if (sheet == null || schedule == null)
                return null;

            try
            {
                var ponto = posicao ?? new XYZ(
                    UnitUtils.ConvertToInternalUnits(0.15, UnitTypeId.Meters),
                    UnitUtils.ConvertToInternalUnits(0.15, UnitTypeId.Meters),
                    0);

                return ScheduleSheetInstance.Create(doc, sheet.Id, schedule.Id, ponto);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Posiciona uma View (planta, corte, etc.) na prancha.
        /// </summary>
        public static Viewport? AdicionarView(
            Document doc,
            ViewSheet sheet,
            View view,
            XYZ? posicao = null)
        {
            if (sheet == null || view == null)
                return null;

            // Verificar se a view já está em alguma prancha
            if (Viewport.CanAddViewToSheet(doc, sheet.Id, view.Id) == false)
                return null;

            try
            {
                var ponto = posicao ?? new XYZ(
                    UnitUtils.ConvertToInternalUnits(0.21, UnitTypeId.Meters),
                    UnitUtils.ConvertToInternalUnits(0.148, UnitTypeId.Meters),
                    0);

                return Viewport.Create(doc, sheet.Id, view.Id, ponto);
            }
            catch
            {
                return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  LÓGICA DE CRIAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria prancha com validações e configuração.
        /// </summary>
        private static ViewSheet CriarPrancha(
            Document doc,
            string nome,
            string numero,
            FamilySymbol titleBlock)
        {
            // ── 1. Validar inputs ─────────────────────────────
            if (string.IsNullOrWhiteSpace(nome))
                nome = "Prancha Sem Nome";

            if (string.IsNullOrWhiteSpace(numero))
                numero = GerarNumeroAutomatico(doc);

            // ── 2. Resolver número duplicado ──────────────────
            numero = ResolverNumeroDuplicado(doc, numero);

            // ── 3. Ativar TitleBlock ──────────────────────────
            if (!titleBlock.IsActive)
            {
                titleBlock.Activate();
                doc.Regenerate();
            }

            // ── 4. Criar ViewSheet ────────────────────────────
            var sheet = ViewSheet.Create(doc, titleBlock.Id);

            if (sheet == null)
                throw new InvalidOperationException(
                    $"Revit retornou null ao criar ViewSheet " +
                    $"(TitleBlock='{titleBlock.Name}').");

            // ── 5. Configurar propriedades ────────────────────
            sheet.SheetNumber = numero;
            sheet.Name = nome;

            return sheet;
        }

        // ══════════════════════════════════════════════════════════
        //  TITLEBLOCK
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Busca o TitleBlock padrão do projeto.
        /// Prioridade: primeiro ativo > qualquer disponível.
        /// </summary>
        private static FamilySymbol? BuscarTitleBlockPadrao(Document doc)
        {
            var titleBlocks = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .ToList();

            if (titleBlocks.Count == 0)
                return null;

            // Priorizar ativo
            var ativo = titleBlocks.FirstOrDefault(tb => tb.IsActive);
            if (ativo != null)
                return ativo;

            return titleBlocks.First();
        }

        /// <summary>
        /// Busca TitleBlock por nome de família.
        /// </summary>
        public static FamilySymbol? BuscarTitleBlockPorNome(
            Document doc,
            string nomeFamilia)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .FirstOrDefault(tb =>
                    tb.FamilyName.IndexOf(nomeFamilia,
                        StringComparison.OrdinalIgnoreCase) >= 0);
        }

        /// <summary>
        /// Lista todos os TitleBlocks disponíveis no projeto.
        /// </summary>
        public static List<(ElementId Id, string Familia, string Tipo)> ListarTitleBlocks(
            Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_TitleBlocks)
                .OfClass(typeof(FamilySymbol))
                .Cast<FamilySymbol>()
                .Select(tb => (tb.Id, tb.FamilyName, tb.Name))
                .ToList();
        }

        // ══════════════════════════════════════════════════════════
        //  NUMERAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resolve número duplicado adicionando sufixo incremental.
        /// Ex: "H-001" já existe → "H-001-1" → "H-001-2" ...
        /// </summary>
        private static string ResolverNumeroDuplicado(Document doc, string numero)
        {
            var numerosExistentes = ObterNumerosExistentes(doc);

            if (!numerosExistentes.Contains(numero))
                return numero;

            // Tentar sufixos incrementais
            for (int i = 1; i <= 999; i++)
            {
                var candidato = $"{numero}-{i}";
                if (!numerosExistentes.Contains(candidato))
                    return candidato;
            }

            // Fallback com timestamp
            return $"{numero}-{DateTime.Now:HHmmss}";
        }

        /// <summary>
        /// Gera número automático para prancha.
        /// Formato: AUTO-001, AUTO-002...
        /// </summary>
        private static string GerarNumeroAutomatico(Document doc)
        {
            var numerosExistentes = ObterNumerosExistentes(doc);

            for (int i = 1; i <= 9999; i++)
            {
                var candidato = $"AUTO-{i:D3}";
                if (!numerosExistentes.Contains(candidato))
                    return candidato;
            }

            return $"AUTO-{DateTime.Now:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Obtém todos os números de prancha existentes no projeto.
        /// </summary>
        private static HashSet<string> ObterNumerosExistentes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Select(s => s.SheetNumber)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }
    }
}
