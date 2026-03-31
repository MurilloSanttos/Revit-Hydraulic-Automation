using System.Text.RegularExpressions;
using Autodesk.Revit.DB;

namespace Revit2026.Modules.Sheets
{
    public interface ISheetNumberingService
    {
        string GerarNumeroPrancha(Document doc, string prefixo, string sufixo);

        void ReordenarNumeracao(Document doc, string prefixo, string sufixo);
    }

    public class ConfiguracaoNumeracao
    {
        public string Prefixo { get; set; } = "H-";
        public string Sufixo { get; set; } = "";
        public int Digitos { get; set; } = 3;
        public int Inicio { get; set; } = 1;
    }

    public class ResultadoReordenacao
    {
        public int PranchasReordenadas { get; set; }
        public int PranchasIgnoradas { get; set; }
        public List<(string Anterior, string Novo)> Alteracoes { get; set; } = new();
        public List<string> Mensagens { get; set; } = new();

        public override string ToString() =>
            $"{PranchasReordenadas} reordenadas, {PranchasIgnoradas} ignoradas";
    }

    public class SheetNumberingService : ISheetNumberingService
    {
        // ══════════════════════════════════════════════════════════
        //  GERAR NÚMERO
        // ══════════════════════════════════════════════════════════

        public string GerarNumeroPrancha(
            Document doc,
            string prefixo,
            string sufixo)
        {
            return GerarNumeroPrancha(doc, new ConfiguracaoNumeracao
            {
                Prefixo = prefixo ?? "",
                Sufixo = sufixo ?? ""
            });
        }

        public string GerarNumeroPrancha(
            Document doc,
            ConfiguracaoNumeracao config)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var prefixo = config.Prefixo ?? "";
            var sufixo = config.Sufixo ?? "";
            var digitos = Math.Max(1, config.Digitos);

            var numerosExistentes = ObterNumerosExistentes(doc);
            var sequenciasUsadas = ExtrairSequencias(
                numerosExistentes, prefixo, sufixo);

            int proximaSequencia = config.Inicio;

            if (sequenciasUsadas.Count > 0)
                proximaSequencia = sequenciasUsadas.Max() + 1;

            var formatStr = new string('0', digitos);
            var numero = $"{prefixo}{proximaSequencia.ToString(formatStr)}{sufixo}";

            while (numerosExistentes.Contains(numero))
            {
                proximaSequencia++;
                numero = $"{prefixo}{proximaSequencia.ToString(formatStr)}{sufixo}";

                if (proximaSequencia > 99999)
                {
                    numero = $"{prefixo}{DateTime.Now:yyyyMMddHHmmss}{sufixo}";
                    break;
                }
            }

            return numero;
        }

        public List<string> GerarMultiplosNumeros(
            Document doc,
            int quantidade,
            ConfiguracaoNumeracao? config = null)
        {
            config ??= new ConfiguracaoNumeracao();
            var numeros = new List<string>();
            var prefixo = config.Prefixo ?? "";
            var sufixo = config.Sufixo ?? "";
            var digitos = Math.Max(1, config.Digitos);
            var formatStr = new string('0', digitos);

            var numerosExistentes = ObterNumerosExistentes(doc);
            var sequenciasUsadas = ExtrairSequencias(
                numerosExistentes, prefixo, sufixo);

            int sequencia = config.Inicio;
            if (sequenciasUsadas.Count > 0)
                sequencia = sequenciasUsadas.Max() + 1;

            for (int i = 0; i < quantidade; i++)
            {
                var numero = $"{prefixo}{sequencia.ToString(formatStr)}{sufixo}";

                while (numerosExistentes.Contains(numero) ||
                       numeros.Contains(numero))
                {
                    sequencia++;
                    numero = $"{prefixo}{sequencia.ToString(formatStr)}{sufixo}";
                }

                numeros.Add(numero);
                sequencia++;
            }

            return numeros;
        }

        // ══════════════════════════════════════════════════════════
        //  REORDENAR
        // ══════════════════════════════════════════════════════════

        public void ReordenarNumeracao(
            Document doc,
            string prefixo,
            string sufixo)
        {
            ReordenarNumeracao(doc, new ConfiguracaoNumeracao
            {
                Prefixo = prefixo ?? "",
                Sufixo = sufixo ?? ""
            });
        }

        public ResultadoReordenacao ReordenarNumeracao(
            Document doc,
            ConfiguracaoNumeracao config)
        {
            if (doc == null)
                throw new ArgumentNullException(nameof(doc));

            var resultado = new ResultadoReordenacao();
            var prefixo = config.Prefixo ?? "";
            var sufixo = config.Sufixo ?? "";
            var digitos = Math.Max(1, config.Digitos);
            var formatStr = new string('0', digitos);

            var regex = CriarRegex(prefixo, sufixo);

            var sheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsPlaceholder)
                .ToList();

            // Separar: pranchas com padrão vs sem padrão
            var comPadrao = new List<(ViewSheet Sheet, int Sequencia)>();

            foreach (var sheet in sheets)
            {
                var match = regex.Match(sheet.SheetNumber);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var seq))
                {
                    comPadrao.Add((sheet, seq));
                }
                else
                {
                    resultado.PranchasIgnoradas++;
                }
            }

            if (comPadrao.Count == 0)
            {
                resultado.Mensagens.Add("Nenhuma prancha com o padrão encontrada.");
                return resultado;
            }

            // Ordenar por sequência original
            comPadrao = comPadrao.OrderBy(x => x.Sequencia).ToList();

            // Fase 1: Mover todas para números temporários
            using (var trans = new Transaction(doc, "Reordenar Numeração - Fase 1"))
            {
                trans.Start();

                try
                {
                    var tempPrefix = $"__TEMP_{Guid.NewGuid().ToString("N")[..6]}_";
                    for (int i = 0; i < comPadrao.Count; i++)
                    {
                        var sheet = comPadrao[i].Sheet;
                        sheet.SheetNumber = $"{tempPrefix}{i:D4}";
                    }

                    trans.Commit();
                }
                catch
                {
                    if (trans.HasStarted()) trans.RollBack();
                    resultado.Mensagens.Add("Falha na fase 1 da reordenação.");
                    return resultado;
                }
            }

            // Fase 2: Aplicar numeração sequencial
            using (var trans = new Transaction(doc, "Reordenar Numeração - Fase 2"))
            {
                trans.Start();

                try
                {
                    int sequencia = config.Inicio;

                    for (int i = 0; i < comPadrao.Count; i++)
                    {
                        var sheet = comPadrao[i].Sheet;
                        var numeroAnterior = $"{prefixo}{comPadrao[i].Sequencia.ToString(formatStr)}{sufixo}";
                        var novoNumero = $"{prefixo}{sequencia.ToString(formatStr)}{sufixo}";

                        sheet.SheetNumber = novoNumero;

                        resultado.Alteracoes.Add((numeroAnterior, novoNumero));
                        resultado.PranchasReordenadas++;
                        sequencia++;
                    }

                    trans.Commit();
                }
                catch
                {
                    if (trans.HasStarted()) trans.RollBack();
                    resultado.Mensagens.Add("Falha na fase 2 da reordenação.");
                    return resultado;
                }
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAR CONFLITOS
        // ══════════════════════════════════════════════════════════

        public List<(ViewSheet Sheet1, ViewSheet Sheet2, string Numero)> DetectarDuplicatas(
            Document doc)
        {
            var duplicatas = new List<(ViewSheet, ViewSheet, string)>();

            var sheets = new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsPlaceholder)
                .GroupBy(s => s.SheetNumber)
                .Where(g => g.Count() > 1);

            foreach (var grupo in sheets)
            {
                var lista = grupo.ToList();
                for (int i = 0; i < lista.Count - 1; i++)
                {
                    duplicatas.Add((lista[i], lista[i + 1], grupo.Key));
                }
            }

            return duplicatas;
        }

        public bool NumeroDisponivel(Document doc, string numero)
        {
            return !ObterNumerosExistentes(doc).Contains(numero);
        }

        // ══════════════════════════════════════════════════════════
        //  PRÓXIMO DISPONÍVEL
        // ══════════════════════════════════════════════════════════

        public string ProximoDisponivel(
            Document doc,
            string prefixo,
            int inicio = 1,
            int digitos = 3,
            string sufixo = "")
        {
            var numerosExistentes = ObterNumerosExistentes(doc);
            var formatStr = new string('0', digitos);

            for (int seq = inicio; seq <= 99999; seq++)
            {
                var candidato = $"{prefixo}{seq.ToString(formatStr)}{sufixo}";
                if (!numerosExistentes.Contains(candidato))
                    return candidato;
            }

            return $"{prefixo}{DateTime.Now:yyyyMMddHHmmss}{sufixo}";
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        private static HashSet<string> ObterNumerosExistentes(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(ViewSheet))
                .Cast<ViewSheet>()
                .Where(s => !s.IsPlaceholder)
                .Select(s => s.SheetNumber)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static HashSet<int> ExtrairSequencias(
            HashSet<string> numeros,
            string prefixo,
            string sufixo)
        {
            var regex = CriarRegex(prefixo, sufixo);
            var sequencias = new HashSet<int>();

            foreach (var numero in numeros)
            {
                var match = regex.Match(numero);
                if (match.Success && int.TryParse(match.Groups[1].Value, out var seq))
                    sequencias.Add(seq);
            }

            return sequencias;
        }

        private static Regex CriarRegex(string prefixo, string sufixo)
        {
            var prefixoEscaped = Regex.Escape(prefixo ?? "");
            var sufixoEscaped = Regex.Escape(sufixo ?? "");
            return new Regex(
                $"^{prefixoEscaped}(\\d+){sufixoEscaped}$",
                RegexOptions.IgnoreCase);
        }
    }
}
