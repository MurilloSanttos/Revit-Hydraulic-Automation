using PluginCore.Domain.Enums;

namespace PluginCore.Common
{
    /// <summary>
    /// Resultado unificado de classificação de ambiente.
    /// Padroniza a saída de todas as estratégias.
    /// </summary>
    public class ClassificationResult
    {
        /// <summary>Tipo de ambiente classificado.</summary>
        public RoomType? Tipo { get; set; }

        /// <summary>Confiança do resultado (0.0 a 1.0).</summary>
        public double Confianca { get; set; }

        /// <summary>Nome da estratégia utilizada.</summary>
        public string Estrategia { get; set; } = string.Empty;

        /// <summary>Padrão que fez match.</summary>
        public string PatternMatched { get; set; } = string.Empty;

        /// <summary>Input original normalizado.</summary>
        public string InputNormalizado { get; set; } = string.Empty;

        /// <summary>Se o resultado foi encontrado.</summary>
        public bool Found => Tipo.HasValue && Tipo != RoomType.Unknown && Confianca > 0;

        /// <summary>Resultado sem match.</summary>
        public static ClassificationResult NoMatch => new()
        {
            Tipo = null,
            Confianca = 0.0,
            Estrategia = "None",
        };

        public override string ToString()
        {
            if (!Found)
                return $"[SEM MATCH] Input: \"{InputNormalizado}\"";

            return $"[{Estrategia}] {Tipo} ({Confianca:P0}) " +
                   $"Pattern: \"{PatternMatched}\" Input: \"{InputNormalizado}\"";
        }
    }
}
