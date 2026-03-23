using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Estratégia 3 — Match Parcial por Palavras.
    /// Tokeniza input e padrões, conta interseção de palavras.
    /// Prioriza o tipo com maior score (mais palavras em comum).
    /// Confiança: 50-70% quando encontrado.
    /// </summary>
    public static class PartialMatchClassifier
    {
        /// <summary>
        /// Classifica o input por interseção de palavras com padrões.
        /// </summary>
        /// <param name="input">Nome do Room.</param>
        /// <returns>RoomType ou null se score &lt; 1.</returns>
        public static RoomType? Classify(string input)
        {
            var result = ClassifyDetailed(input);
            return result.Found ? result.Type : null;
        }

        /// <summary>
        /// Classifica com resultado detalhado (tipo, score, confiança, padrão).
        /// </summary>
        public static ClassificationResult ClassifyDetailed(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return ClassificationResult.NoMatch;

            var normalized = TextNormalizer.NormalizeForClassification(input);

            if (string.IsNullOrEmpty(normalized))
                return ClassificationResult.NoMatch;

            // Tokenizar input
            var inputWords = Tokenize(normalized);

            if (inputWords.Length == 0)
                return ClassificationResult.NoMatch;

            var bestType = RoomType.Unknown;
            var bestScore = 0;
            var bestPattern = "";
            var bestTotal = 0;

            foreach (var (type, patterns) in AmbientePatterns.Patterns)
            {
                foreach (var pattern in patterns)
                {
                    var patternWords = Tokenize(pattern);

                    // Contar palavras em comum
                    var commonWords = inputWords
                        .Intersect(patternWords, StringComparer.Ordinal)
                        .Count();

                    // Priorizar: mais palavras em comum → melhor score
                    // Em empate: padrão com mais palavras no total (mais específico)
                    if (commonWords > bestScore ||
                        (commonWords == bestScore && patternWords.Length > bestTotal))
                    {
                        bestScore = commonWords;
                        bestType = type;
                        bestPattern = pattern;
                        bestTotal = patternWords.Length;
                    }
                }
            }

            // Threshold mínimo: pelo menos 1 palavra em comum
            if (bestScore < 1)
                return ClassificationResult.NoMatch;

            // Calcular confiança baseada na cobertura
            var inputCoverage = (double)bestScore / inputWords.Length;
            var patternCoverage = (double)bestScore / Math.Max(1, Tokenize(bestPattern).Length);
            var confidence = (inputCoverage + patternCoverage) / 2.0;

            // Limitar entre 0.50 e 0.70
            confidence = Math.Clamp(confidence * 0.70, 0.50, 0.70);

            return new ClassificationResult(bestType, confidence, bestPattern,
                true, bestScore, inputWords.Length);
        }

        /// <summary>
        /// Tokeniza uma string em palavras (split por espaço, remove vazios).
        /// </summary>
        private static string[] Tokenize(string text)
        {
            return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        /// <summary>Resultado de classificação por match parcial.</summary>
        public record ClassificationResult(
            RoomType Type,
            double Confidence,
            string MatchedPattern,
            bool Found,
            int WordsMatched,
            int TotalWords
        )
        {
            public static readonly ClassificationResult NoMatch =
                new(RoomType.Unknown, 0.0, "", false, 0, 0);
        }

        // Exemplos:
        // PartialMatchClassifier.Classify("suite casal banheiro")  → MasterBathroom
        //   input words:   [suite, casal, banheiro]
        //   best pattern:  "banheiro suite casal" → 3 palavras em comum
        //
        // PartialMatchClassifier.Classify("social wc visitas")     → Bathroom
        //   input words:   [social, wc, visitas]
        //   best pattern:  "wc social" → 2 palavras em comum
        //
        // var r = PartialMatchClassifier.ClassifyDetailed("coz gourmet");
        // r.Type         → Kitchen
        // r.Confidence   → 0.70
        // r.WordsMatched → 1
        // r.Found        → true
    }
}
