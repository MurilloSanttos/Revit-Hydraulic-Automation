using System.Text;
using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Validador do classificador de ambientes.
    /// Executa corpus de 100+ nomes reais e gera relatório de acurácia.
    /// </summary>
    public class ClassifierValidator
    {
        // ══════════════════════════════════════════════════════════
        //  CORPUS DE TESTE (100+ exemplos reais)
        // ══════════════════════════════════════════════════════════

        private static readonly List<(string Input, RoomType Expected)> _corpus = new()
        {
            // ── BATHROOM (15) ──────────────────────────────────
            ("Banheiro", RoomType.Bathroom),
            ("Banheiro Social", RoomType.Bathroom),
            ("Banheiro 01", RoomType.Bathroom),
            ("Banheiro 02", RoomType.Bathroom),
            ("Banheiro de Serviço", RoomType.Bathroom),
            ("Banheiro Empregada", RoomType.Bathroom),
            ("WC", RoomType.Bathroom),
            ("BWC Social", RoomType.Bathroom),
            ("Sanitário", RoomType.Bathroom),
            ("Banho", RoomType.Bathroom),
            ("Banhero Social", RoomType.Bathroom),       // typo
            ("Banh", RoomType.Bathroom),                  // abreviação
            ("banheiro reversivel", RoomType.Bathroom),
            ("Banheiro Externo", RoomType.Bathroom),
            ("banheiro comum", RoomType.Bathroom),

            // ── MASTER BATHROOM (12) ───────────────────────────
            ("Banheiro Suíte", RoomType.MasterBathroom),
            ("Banheiro da Suíte", RoomType.MasterBathroom),
            ("Banheiro Suite Master", RoomType.MasterBathroom),
            ("Banheiro Suite Casal", RoomType.MasterBathroom),
            ("Banheiro Suite 01", RoomType.MasterBathroom),
            ("Banheiro Suite 02", RoomType.MasterBathroom),
            ("Banheiro Master", RoomType.MasterBathroom),
            ("Banh Suite", RoomType.MasterBathroom),      // abreviação
            ("WC Suite", RoomType.MasterBathroom),
            ("BWC Suite", RoomType.MasterBathroom),
            ("Banhero Suite", RoomType.MasterBathroom),   // typo
            ("Master Bathroom", RoomType.MasterBathroom),

            // ── LAVATORY (10) ──────────────────────────────────
            ("Lavabo", RoomType.Lavatory),
            ("Lavabo Social", RoomType.Lavatory),
            ("Lavabo 01", RoomType.Lavatory),
            ("Lavabo Íntimo", RoomType.Lavatory),
            ("Lavabo Visitas", RoomType.Lavatory),
            ("Toilette", RoomType.Lavatory),
            ("Toalete", RoomType.Lavatory),
            ("Lav", RoomType.Lavatory),                   // abreviação
            ("Lav Social", RoomType.Lavatory),
            ("Lavbo", RoomType.Lavatory),                 // typo

            // ── KITCHEN (12) ──────────────────────────────────
            ("Cozinha", RoomType.Kitchen),
            ("Cozinha Principal", RoomType.Kitchen),
            ("Cozinha Gourmet", RoomType.Kitchen),
            ("Cozinha Americana", RoomType.Kitchen),
            ("Cozinha 01", RoomType.Kitchen),
            ("Copa", RoomType.Kitchen),
            ("Copa Cozinha", RoomType.Kitchen),
            ("Copa e Cozinha", RoomType.Kitchen),
            ("Cosinha", RoomType.Kitchen),                // typo
            ("Cozihna", RoomType.Kitchen),                // typo
            ("Coz", RoomType.Kitchen),                    // abreviação
            ("Kitchen", RoomType.Kitchen),

            // ── LAUNDRY (12) ───────────────────────────────────
            ("Área de Serviço", RoomType.Laundry),
            ("Area de Servico", RoomType.Laundry),
            ("Área de Serviço 01", RoomType.Laundry),
            ("A. Serv.", RoomType.Laundry),
            ("Lavanderia", RoomType.Laundry),
            ("Serviço", RoomType.Laundry),
            ("Area Servico", RoomType.Laundry),
            ("Área de Serviço Coberta", RoomType.Laundry),
            ("Área de Serviço Externa", RoomType.Laundry),
            ("A Servico", RoomType.Laundry),
            ("Laundry", RoomType.Laundry),
            ("Area de Serv", RoomType.Laundry),

            // ── BALCONY (8) ────────────────────────────────────
            ("Varanda", RoomType.Balcony),
            ("Varanda Gourmet", RoomType.Balcony),
            ("Varanda Social", RoomType.Balcony),
            ("Sacada", RoomType.Balcony),
            ("Sacada Suite", RoomType.Balcony),
            ("Terraço", RoomType.Balcony),
            ("Terraço Descoberto", RoomType.Balcony),
            ("Varanda Coberta", RoomType.Balcony),

            // ── GARAGE (8) ─────────────────────────────────────
            ("Garagem", RoomType.Garage),
            ("Garagem Coberta", RoomType.Garage),
            ("Garagem 01", RoomType.Garage),
            ("Garagem 02", RoomType.Garage),
            ("Vaga", RoomType.Garage),
            ("Vaga 01", RoomType.Garage),
            ("Estacionamento", RoomType.Garage),
            ("Garagem Dupla", RoomType.Garage),

            // ── SERVICE AREA (8) ───────────────────────────────
            ("Área Externa", RoomType.ServiceArea),
            ("Piscina", RoomType.ServiceArea),
            ("Churrasqueira", RoomType.ServiceArea),
            ("Área de Lazer", RoomType.ServiceArea),
            ("Espaço Gourmet", RoomType.ServiceArea),
            ("Quintal", RoomType.ServiceArea),
            ("Jardim", RoomType.ServiceArea),
            ("Área Descoberta", RoomType.ServiceArea),

            // ── DRY AREA (20) ──────────────────────────────────
            ("Sala", RoomType.DryArea),
            ("Sala de Estar", RoomType.DryArea),
            ("Sala de Jantar", RoomType.DryArea),
            ("Sala de TV", RoomType.DryArea),
            ("Sala Íntima", RoomType.DryArea),
            ("Living", RoomType.DryArea),
            ("Quarto", RoomType.DryArea),
            ("Quarto Casal", RoomType.DryArea),
            ("Quarto Solteiro", RoomType.DryArea),
            ("Quarto Hóspedes", RoomType.DryArea),
            ("Quarto 01", RoomType.DryArea),
            ("Dormitório", RoomType.DryArea),
            ("Suíte", RoomType.DryArea),
            ("Suite Master", RoomType.DryArea),
            ("Corredor", RoomType.DryArea),
            ("Corredor Íntimo", RoomType.DryArea),
            ("Hall", RoomType.DryArea),
            ("Hall de Entrada", RoomType.DryArea),
            ("Escritório", RoomType.DryArea),
            ("Closet", RoomType.DryArea),
        };

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>Executa validação e retorna relatório textual.</summary>
        public string Validate()
        {
            var resultados = new List<ValidationEntry>();

            foreach (var (input, expected) in _corpus)
            {
                var result = BatchClassifier.ClassificarUm(input);

                var obtido = result.Tipo ?? RoomType.Unknown;
                var acertou = obtido == expected;

                resultados.Add(new ValidationEntry
                {
                    Input = input,
                    Expected = expected,
                    Obtained = obtido,
                    Confidence = result.Confianca,
                    Strategy = result.Estrategia,
                    Correct = acertou,
                });
            }

            return GerarRelatorio(resultados);
        }

        /// <summary>Executa validação e retorna dados estruturados.</summary>
        public ValidationReport ValidateStructured()
        {
            var entries = new List<ValidationEntry>();

            foreach (var (input, expected) in _corpus)
            {
                var result = BatchClassifier.ClassificarUm(input);
                var obtido = result.Tipo ?? RoomType.Unknown;

                entries.Add(new ValidationEntry
                {
                    Input = input,
                    Expected = expected,
                    Obtained = obtido,
                    Confidence = result.Confianca,
                    Strategy = result.Estrategia,
                    Correct = obtido == expected,
                });
            }

            var total = entries.Count;
            var acertos = entries.Count(e => e.Correct);
            var erros = entries.Where(e => !e.Correct).ToList();

            var porTipo = entries
                .GroupBy(e => e.Expected)
                .ToDictionary(
                    g => g.Key,
                    g => new TypeMetrics
                    {
                        Total = g.Count(),
                        Acertos = g.Count(e => e.Correct),
                    });

            return new ValidationReport
            {
                Total = total,
                Acertos = acertos,
                Erros = erros,
                Acuracia = total > 0 ? (double)acertos / total : 0,
                PorTipo = porTipo,
                ConfiancaMedia = entries
                    .Where(e => e.Correct)
                    .Select(e => e.Confidence)
                    .DefaultIfEmpty(0)
                    .Average(),
            };
        }

        // ══════════════════════════════════════════════════════════
        //  RELATÓRIO TEXTUAL
        // ══════════════════════════════════════════════════════════

        private string GerarRelatorio(List<ValidationEntry> entries)
        {
            var sb = new StringBuilder();
            var total = entries.Count;
            var acertos = entries.Count(e => e.Correct);
            var errList = entries.Where(e => !e.Correct).ToList();
            var acuracia = total > 0 ? (double)acertos / total : 0;

            sb.AppendLine("╔══════════════════════════════════════════════╗");
            sb.AppendLine("║     RELATÓRIO DE VALIDAÇÃO DO CLASSIFICADOR ║");
            sb.AppendLine("╠══════════════════════════════════════════════╣");
            sb.AppendLine($"║  Total:     {total,5}                         ║");
            sb.AppendLine($"║  Acertos:   {acertos,5}                         ║");
            sb.AppendLine($"║  Erros:     {errList.Count,5}                         ║");
            sb.AppendLine($"║  Acurácia:  {acuracia,6:P1}                       ║");
            sb.AppendLine("╠══════════════════════════════════════════════╣");
            sb.AppendLine("║  ACURÁCIA POR TIPO                          ║");
            sb.AppendLine("╠══════════════════════════════════════════════╣");

            var porTipo = entries
                .GroupBy(e => e.Expected)
                .OrderBy(g => g.Key.ToString());

            foreach (var grupo in porTipo)
            {
                var t = grupo.Count();
                var a = grupo.Count(e => e.Correct);
                var pct = t > 0 ? (double)a / t : 0;
                var icon = pct >= 0.9 ? "✅" : pct >= 0.7 ? "⚠️" : "❌";

                sb.AppendLine($"║  {icon} {grupo.Key,-18} {a,3}/{t,-3} ({pct:P0})       ║");
            }

            if (errList.Count > 0)
            {
                sb.AppendLine("╠══════════════════════════════════════════════╣");
                sb.AppendLine("║  ERROS DE CLASSIFICAÇÃO                     ║");
                sb.AppendLine("╠══════════════════════════════════════════════╣");

                foreach (var err in errList)
                {
                    sb.AppendLine($"║  Input:    \"{err.Input}\"");
                    sb.AppendLine($"║  Esperado: {err.Expected}");
                    sb.AppendLine($"║  Obtido:   {err.Obtained} ({err.Strategy}, {err.Confidence:P0})");
                    sb.AppendLine("║  ──────────────────────────────────────────");
                }
            }

            sb.AppendLine("╚══════════════════════════════════════════════╝");

            return sb.ToString();
        }

        // ══════════════════════════════════════════════════════════
        //  MODELOS
        // ══════════════════════════════════════════════════════════

        public class ValidationEntry
        {
            public string Input { get; set; } = "";
            public RoomType Expected { get; set; }
            public RoomType Obtained { get; set; }
            public double Confidence { get; set; }
            public string Strategy { get; set; } = "";
            public bool Correct { get; set; }
        }

        public class TypeMetrics
        {
            public int Total { get; set; }
            public int Acertos { get; set; }
            public double Acuracia => Total > 0 ? (double)Acertos / Total : 0;
        }

        public class ValidationReport
        {
            public int Total { get; set; }
            public int Acertos { get; set; }
            public List<ValidationEntry> Erros { get; set; } = new();
            public double Acuracia { get; set; }
            public double ConfiancaMedia { get; set; }
            public Dictionary<RoomType, TypeMetrics> PorTipo { get; set; } = new();
        }

        // Exemplo de uso:
        // var validator = new ClassifierValidator();
        // Console.WriteLine(validator.Validate());
        //
        // var report = validator.ValidateStructured();
        // Console.WriteLine($"Acurácia: {report.Acuracia:P1}");
        // Console.WriteLine($"Erros: {report.Erros.Count}");
    }
}
