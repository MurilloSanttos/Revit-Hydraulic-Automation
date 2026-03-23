using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Estratégia 1 — Match Exato.
    /// Compara o texto normalizado contra padrões conhecidos via igualdade.
    /// Confiança: 95% quando encontrado.
    /// </summary>
    public static class ExactMatchClassifier
    {
        /// <summary>
        /// Classifica o input por match exato após normalização.
        /// </summary>
        /// <param name="input">Nome do Room (ex: "Banheiro - Social").</param>
        /// <returns>RoomType ou null se não encontrar match exato.</returns>
        public static RoomType? Classify(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var normalized = TextNormalizer.NormalizeForClassification(input);

            if (string.IsNullOrEmpty(normalized))
                return null;

            // Verificar nome genérico primeiro
            if (AmbientePatterns.IsGenericName(normalized))
                return RoomType.Unknown;

            // Buscar match exato no dicionário
            foreach (var (type, patterns) in AmbientePatterns.Patterns)
            {
                if (patterns.Contains(normalized))
                    return type;
            }

            return null;
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

            if (AmbientePatterns.IsGenericName(normalized))
                return new ClassificationResult(RoomType.Unknown, 1.0, normalized, true);

            foreach (var (type, patterns) in AmbientePatterns.Patterns)
            {
                if (patterns.Contains(normalized))
                    return new ClassificationResult(type, 0.95, normalized, true);
            }

            return ClassificationResult.NoMatch;
        }

        /// <summary>Resultado de classificação por match exato.</summary>
        public record ClassificationResult(
            RoomType Type,
            double Confidence,
            string MatchedPattern,
            bool Found
        )
        {
            public static readonly ClassificationResult NoMatch =
                new(RoomType.Unknown, 0.0, "", false);
        }

        // Exemplos:
        // ExactMatchClassifier.Classify("banheiro social")  → Bathroom
        // ExactMatchClassifier.Classify("cozinha")           → Kitchen
        // ExactMatchClassifier.Classify("Quarto - 02")       → DryArea
        // ExactMatchClassifier.Classify("xyz")               → null
        //
        // var r = ExactMatchClassifier.ClassifyDetailed("cozinha");
        // r.Type       → Kitchen
        // r.Confidence → 0.95
        // r.Found      → true
    }
}
