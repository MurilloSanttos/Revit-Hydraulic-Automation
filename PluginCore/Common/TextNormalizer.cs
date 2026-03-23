using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PluginCore.Common
{
    /// <summary>
    /// Utilitário de normalização de texto para o classificador de ambientes.
    /// Remove acentos, converte para minúsculo, padroniza espaços e separadores.
    /// </summary>
    public static class TextNormalizer
    {
        /// <summary>
        /// Normaliza texto: trim, lowercase, sem acentos, espaços simples.
        /// </summary>
        /// <param name="input">Texto de entrada (ex: "Banheiro - Suíte").</param>
        /// <returns>Texto normalizado (ex: "banheiro suite").</returns>
        public static string Normalize(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // 1. Trim + lowercase
            var result = input.Trim().ToLowerInvariant();

            // 2. Remover acentos
            result = RemoverAcentos(result);

            // 3. Remover separadores ( - , / , \ , | )  → espaço
            result = Regex.Replace(result, @"[\-/\\|]", " ");

            // 4. Remover caracteres especiais ( . , ( ) [ ] # )
            result = Regex.Replace(result, @"[.\(\)\[\]#]", "");

            // 5. Colapsar múltiplos espaços em um só
            result = Regex.Replace(result, @"\s+", " ").Trim();

            return result;
        }

        /// <summary>
        /// Remove acentos de uma string via decomposição Unicode.
        /// </summary>
        /// <param name="input">Texto com acentos.</param>
        /// <returns>Texto sem acentos.</returns>
        public static string RemoverAcentos(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);

            foreach (var c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        /// <summary>
        /// Remove números no final da string (ex: "Banheiro 01" → "Banheiro").
        /// Não remove números no meio do texto.
        /// </summary>
        public static string RemoveTrailingNumbers(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove espaços + dígitos apenas no final
            var result = Regex.Replace(input, @"\s*\d+$", "").Trim();

            return result.Length > 0 ? result : input.Trim();
        }

        /// <summary>
        /// Normaliza para classificação: Normalize + remove números finais.
        /// Ideal para o ClassificadorAmbientes.
        /// </summary>
        public static string NormalizeForClassification(string input)
        {
            var normalized = Normalize(input);
            return RemoveTrailingNumbers(normalized);
        }

        /// <summary>
        /// Verifica se o texto normalizado contém a palavra-chave.
        /// </summary>
        public static bool Contains(string input, string keyword)
        {
            var normalizedInput = Normalize(input);
            var normalizedKeyword = Normalize(keyword);

            return normalizedInput.Contains(normalizedKeyword, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifica se o texto normalizado começa com a palavra-chave.
        /// </summary>
        public static bool StartsWith(string input, string keyword)
        {
            var normalizedInput = Normalize(input);
            var normalizedKeyword = Normalize(keyword);

            return normalizedInput.StartsWith(normalizedKeyword, StringComparison.Ordinal);
        }

        /// <summary>
        /// Calcula similaridade simples entre duas strings (0.0 a 1.0).
        /// Baseado em subsequência comum mais longa (para erros de digitação).
        /// </summary>
        public static double Similarity(string a, string b)
        {
            var na = Normalize(a);
            var nb = Normalize(b);

            if (na == nb) return 1.0;
            if (na.Length == 0 || nb.Length == 0) return 0.0;

            var distance = LevenshteinDistance(na, nb);
            var maxLen = Math.Max(na.Length, nb.Length);

            return 1.0 - ((double)distance / maxLen);
        }

        /// <summary>
        /// Distância de Levenshtein entre duas strings.
        /// </summary>
        private static int LevenshteinDistance(string s, string t)
        {
            var n = s.Length;
            var m = t.Length;
            var d = new int[n + 1, m + 1];

            for (var i = 0; i <= n; i++) d[i, 0] = i;
            for (var j = 0; j <= m; j++) d[0, j] = j;

            for (var i = 1; i <= n; i++)
            {
                for (var j = 1; j <= m; j++)
                {
                    var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }

            return d[n, m];
        }

        // Exemplos:
        // TextNormalizer.Normalize("  Banheiro - Suíte  ")           → "banheiro suite"
        // TextNormalizer.Normalize("Área de Serviço")                → "area de servico"
        // TextNormalizer.RemoveTrailingNumbers("Banheiro 01")        → "Banheiro"
        // TextNormalizer.RemoveTrailingNumbers("Quarto 3")            → "Quarto"
        // TextNormalizer.RemoveTrailingNumbers("Sala 101 Norte")      → "Sala 101 Norte"
        // TextNormalizer.NormalizeForClassification("Quarto - 02")   → "quarto"
        // TextNormalizer.NormalizeForClassification("Cozinha")       → "cozinha"
        // TextNormalizer.Similarity("Banhero", "Banheiro")           → 0.875
    }
}
