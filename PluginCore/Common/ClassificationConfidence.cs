using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Calcula confiança por estratégia de classificação.
    /// Fábrica de ClassificationResult padronizados.
    /// </summary>
    public static class ClassificationConfidence
    {
        /// <summary>
        /// Resultado para match exato. Confiança = 1.0 se encontrado.
        /// </summary>
        public static ClassificationResult FromExact(RoomType? tipo,
            string input = "", string pattern = "")
        {
            if (tipo == null)
                return new ClassificationResult
                {
                    Tipo = null,
                    Confianca = 0.0,
                    Estrategia = "ExactMatch",
                    InputNormalizado = input,
                };

            return new ClassificationResult
            {
                Tipo = tipo,
                Confianca = 1.0,
                Estrategia = "ExactMatch",
                PatternMatched = pattern,
                InputNormalizado = input,
            };
        }

        /// <summary>
        /// Resultado para match por contenção.
        /// Confiança proporcional ao comprimento do padrão vs input (max 0.95).
        /// </summary>
        public static ClassificationResult FromContains(RoomType? tipo,
            int patternLength, int inputLength,
            string input = "", string pattern = "")
        {
            if (tipo == null || inputLength == 0)
                return new ClassificationResult
                {
                    Tipo = null,
                    Confianca = 0.0,
                    Estrategia = "ContainsMatch",
                    InputNormalizado = input,
                };

            var confidence = (double)patternLength / Math.Max(1, inputLength);
            confidence = Math.Clamp(confidence, 0.0, 0.95);

            return new ClassificationResult
            {
                Tipo = tipo,
                Confianca = confidence,
                Estrategia = "ContainsMatch",
                PatternMatched = pattern,
                InputNormalizado = input,
            };
        }

        /// <summary>
        /// Resultado para match parcial por palavras.
        /// Confiança = palavras matched / total (max 0.80).
        /// </summary>
        public static ClassificationResult FromPartial(RoomType? tipo,
            int matchedWords, int totalWords,
            string input = "", string pattern = "")
        {
            if (tipo == null || totalWords == 0 || matchedWords == 0)
                return new ClassificationResult
                {
                    Tipo = null,
                    Confianca = 0.0,
                    Estrategia = "PartialMatch",
                    InputNormalizado = input,
                };

            var confidence = (double)matchedWords / Math.Max(1, totalWords);
            confidence = Math.Clamp(confidence, 0.0, 0.80);

            return new ClassificationResult
            {
                Tipo = tipo,
                Confianca = confidence,
                Estrategia = "PartialMatch",
                PatternMatched = pattern,
                InputNormalizado = input,
            };
        }

        /// <summary>
        /// Resultado para match por similaridade (Levenshtein).
        /// Confiança = score de similaridade (max 0.70).
        /// </summary>
        public static ClassificationResult FromSimilarity(RoomType? tipo,
            double similarity,
            string input = "", string pattern = "")
        {
            if (tipo == null || similarity <= 0)
                return new ClassificationResult
                {
                    Tipo = null,
                    Confianca = 0.0,
                    Estrategia = "Similarity",
                    InputNormalizado = input,
                };

            var confidence = Math.Clamp(similarity, 0.0, 0.70);

            return new ClassificationResult
            {
                Tipo = tipo,
                Confianca = confidence,
                Estrategia = "Similarity",
                PatternMatched = pattern,
                InputNormalizado = input,
            };
        }

        /// <summary>
        /// Seleciona o melhor resultado entre vários candidatos.
        /// Prioriza maior confiança.
        /// </summary>
        public static ClassificationResult Best(params ClassificationResult[] candidates)
        {
            return candidates
                .Where(c => c.Found)
                .OrderByDescending(c => c.Confianca)
                .FirstOrDefault() ?? ClassificationResult.NoMatch;
        }

        // Exemplos:
        // var r1 = ClassificationConfidence.FromExact(RoomType.Bathroom, "banheiro", "banheiro");
        // r1.Confianca → 1.0
        //
        // var r2 = ClassificationConfidence.FromContains(RoomType.Kitchen, 7, 15, "cozinha gourmet nova", "cozinha");
        // r2.Confianca → 0.47
        //
        // var r3 = ClassificationConfidence.FromPartial(RoomType.DryArea, 2, 3, "suite casal master", "suite casal");
        // r3.Confianca → 0.67
        //
        // var best = ClassificationConfidence.Best(r1, r2, r3);
        // best → r1 (confiança 1.0)
    }
}
