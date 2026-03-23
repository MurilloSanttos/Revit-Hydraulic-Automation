using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Estratégia 2 — Match por Contenção.
    /// Verifica se o texto normalizado contém padrões conhecidos.
    /// Prioriza o padrão mais longo (mais específico).
    /// Confiança: 75-85% quando encontrado.
    /// </summary>
    public static class ContainsMatchClassifier
    {
        /// <summary>
        /// Classifica o input por contenção de padrão (longest match first).
        /// </summary>
        /// <param name="input">Nome do Room.</param>
        /// <returns>RoomType ou null se nenhum padrão for encontrado.</returns>
        public static RoomType? Classify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var normalized = TextNormalizer.NormalizeForClassification(input);

            if (string.IsNullOrEmpty(normalized))
                return null;

            // Flatten + ordenar por comprimento DESC (mais específico primeiro)
            var match = AmbientePatterns.Patterns
                .SelectMany(kvp => kvp.Value.Select(p => (Pattern: p, Type: kvp.Key)))
                .OrderByDescending(x => x.Pattern.Length)
                .FirstOrDefault(x => normalized.Contains(x.Pattern, StringComparison.Ordinal));

            return match.Pattern != null ? match.Type : null;
        }

        /// <summary>
        /// Classifica com resultado detalhado.
        /// </summary>
        public static ClassificationResult ClassifyDetailed(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ClassificationResult.NoMatch;

            var normalized = TextNormalizer.NormalizeForClassification(input);

            if (string.IsNullOrEmpty(normalized))
                return ClassificationResult.NoMatch;

            // Flatten + ordenar por comprimento DESC
            var candidates = AmbientePatterns.Patterns
                .SelectMany(kvp => kvp.Value.Select(p => (Pattern: p, Type: kvp.Key)))
                .OrderByDescending(x => x.Pattern.Length)
                .ToList();

            // StartsWith → maior confiança (85%)
            var startsMatch = candidates
                .FirstOrDefault(x => normalized.StartsWith(x.Pattern, StringComparison.Ordinal));

            if (startsMatch.Pattern != null)
                return new ClassificationResult(startsMatch.Type, 0.85, startsMatch.Pattern,
                    true, "StartsWith");

            // Contains → confiança média (75%)
            var containsMatch = candidates
                .FirstOrDefault(x => normalized.Contains(x.Pattern, StringComparison.Ordinal));

            if (containsMatch.Pattern != null)
                return new ClassificationResult(containsMatch.Type, 0.75, containsMatch.Pattern,
                    true, "Contains");

            return ClassificationResult.NoMatch;
        }

        /// <summary>Resultado de classificação por contenção.</summary>
        public record ClassificationResult(
            RoomType Type,
            double Confidence,
            string MatchedPattern,
            bool Found,
            string Method
        )
        {
            public static readonly ClassificationResult NoMatch =
                new(RoomType.Unknown, 0.0, "", false, "None");
        }

        // Exemplos:
        // ContainsMatchClassifier.Classify("banheiro social grande")   → Bathroom
        // ContainsMatchClassifier.Classify("minha cozinha nova")       → Kitchen
        // ContainsMatchClassifier.Classify("xyz abc")                  → null
        //
        // var r = ContainsMatchClassifier.ClassifyDetailed("banheiro suite master renovado");
        // r.Type       → MasterBathroom
        // r.Confidence → 0.85
        // r.Method     → "StartsWith"
    }
}
