using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Classificador em lote — executa todas as estratégias e seleciona o melhor resultado.
    /// </summary>
    public static class BatchClassifier
    {
        /// <summary>
        /// Classifica múltiplos inputs utilizando todas as estratégias.
        /// Retorna resultados na mesma ordem dos inputs.
        /// </summary>
        public static IEnumerable<ClassificationResult> ClassificarTodos(
            IEnumerable<string> inputs)
        {
            foreach (var input in inputs)
            {
                yield return ClassificarUm(input);
            }
        }

        /// <summary>
        /// Classifica um único input executando as 3 estratégias e selecionando o melhor.
        /// </summary>
        public static ClassificationResult ClassificarUm(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return new ClassificationResult
                {
                    Tipo = null,
                    Confianca = 0.0,
                    Estrategia = "None",
                    InputNormalizado = input ?? "",
                };
            }

            var normalized = TextNormalizer.NormalizeForClassification(input);

            // ── Estratégia 1: ExactMatch ───────────────────────
            var exactResult = ExactMatchClassifier.ClassifyDetailed(input);
            var exact = exactResult.Found
                ? ClassificationConfidence.FromExact(
                    exactResult.Type, normalized, exactResult.MatchedPattern)
                : ClassificationResult.NoMatch;

            if (exact.Found && exact.Confianca >= 0.95)
                return exact; // Short-circuit — match perfeito

            // ── Estratégia 2: ContainsMatch ────────────────────
            var containsResult = ContainsMatchClassifier.ClassifyDetailed(input);
            var contains = containsResult.Found
                ? ClassificationConfidence.FromContains(
                    containsResult.Type,
                    containsResult.MatchedPattern.Length,
                    normalized.Length,
                    normalized,
                    containsResult.MatchedPattern)
                : ClassificationResult.NoMatch;

            // ── Estratégia 3: PartialMatch ─────────────────────
            var partialResult = PartialMatchClassifier.ClassifyDetailed(input);
            var partial = partialResult.Found
                ? ClassificationConfidence.FromPartial(
                    partialResult.Type,
                    partialResult.WordsMatched,
                    partialResult.TotalWords,
                    normalized,
                    partialResult.MatchedPattern)
                : ClassificationResult.NoMatch;

            // ── Selecionar melhor ──────────────────────────────
            return ClassificationConfidence.Best(exact, contains, partial);
        }

        /// <summary>
        /// Classifica em lote e retorna resumo (total, classificados, falhas).
        /// </summary>
        public static BatchResult ClassificarComResumo(IEnumerable<string> inputs)
        {
            var resultados = ClassificarTodos(inputs).ToList();

            return new BatchResult
            {
                Resultados = resultados,
                Total = resultados.Count,
                Classificados = resultados.Count(r => r.Found),
                NaoClassificados = resultados.Count(r => !r.Found),
                ConfiancaMedia = resultados.Where(r => r.Found)
                    .Select(r => r.Confianca)
                    .DefaultIfEmpty(0.0)
                    .Average(),
                PorEstrategia = resultados
                    .Where(r => r.Found)
                    .GroupBy(r => r.Estrategia)
                    .ToDictionary(g => g.Key, g => g.Count()),
                PorTipo = resultados
                    .Where(r => r.Found && r.Tipo.HasValue)
                    .GroupBy(r => r.Tipo!.Value)
                    .ToDictionary(g => g.Key, g => g.Count()),
            };
        }

        /// <summary>Resultado de classificação em lote.</summary>
        public class BatchResult
        {
            public List<ClassificationResult> Resultados { get; set; } = new();
            public int Total { get; set; }
            public int Classificados { get; set; }
            public int NaoClassificados { get; set; }
            public double ConfiancaMedia { get; set; }
            public Dictionary<string, int> PorEstrategia { get; set; } = new();
            public Dictionary<RoomType, int> PorTipo { get; set; } = new();
            public double TaxaAcerto => Total > 0 ? (double)Classificados / Total : 0;

            public override string ToString()
            {
                var estrategias = string.Join(", ",
                    PorEstrategia.Select(kv => $"{kv.Key}: {kv.Value}"));

                return $"═══ Classificação em Lote ═══\n" +
                       $"  Total:            {Total}\n" +
                       $"  Classificados:    {Classificados}\n" +
                       $"  Não classificados:{NaoClassificados}\n" +
                       $"  Taxa de acerto:   {TaxaAcerto:P1}\n" +
                       $"  Confiança média:  {ConfiancaMedia:P1}\n" +
                       $"  Por estratégia:   {estrategias}\n" +
                       $"═════════════════════════════";
            }
        }

        // Exemplos:
        // var resultados = BatchClassifier.ClassificarTodos(new[]
        // {
        //     "Banheiro 01", "Cozinha Principal", "Sala TV", "xyz"
        // });
        // → [Bathroom(1.0), Kitchen(1.0), DryArea(0.80), NoMatch]
        //
        // var resumo = BatchClassifier.ClassificarComResumo(nomes);
        // Console.WriteLine(resumo);
        // → Total: 4, Classificados: 3, Taxa: 75.0%
    }
}
