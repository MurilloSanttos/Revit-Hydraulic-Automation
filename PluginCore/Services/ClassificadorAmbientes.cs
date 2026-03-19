using System.Globalization;
using System.Text;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Motor de classificação de ambientes por análise de nome.
    /// Utiliza dicionário de variações em português brasileiro para
    /// identificar o tipo de cada ambiente com nível de confiança.
    /// 
    /// Estratégia de matching (em ordem de prioridade):
    /// 1. Match exato (confiança 1.0)
    /// 2. Texto contém padrão (confiança 0.85)
    /// 3. Padrão contido no texto (confiança 0.7)
    /// 4. Match parcial por palavras (confiança 0.5-0.6)
    /// </summary>
    public class ClassificadorAmbientes
    {
        /// <summary>
        /// Dicionário interno de padrões: chave normalizada → tipo de ambiente.
        /// Organizado por prioridade (padrões mais específicos primeiro).
        /// </summary>
        private static readonly List<(string Padrao, TipoAmbiente Tipo, double PesoBase)> _padroes = new()
        {
            // === Cozinha Gourmet (mais específico, antes de "cozinha") ===
            ("cozinha gourmet", TipoAmbiente.CozinhaGourmet, 1.0),
            ("coz gourmet", TipoAmbiente.CozinhaGourmet, 1.0),
            ("coz. gourmet", TipoAmbiente.CozinhaGourmet, 1.0),
            ("espaco gourmet", TipoAmbiente.CozinhaGourmet, 0.9),
            ("gourmet", TipoAmbiente.CozinhaGourmet, 0.75),

            // === Suíte (antes de "banheiro" para capturar "banheiro suíte") ===
            ("suite", TipoAmbiente.Suite, 1.0),
            ("ste", TipoAmbiente.Suite, 0.85),
            ("banheiro suite", TipoAmbiente.Suite, 1.0),
            ("banho suite", TipoAmbiente.Suite, 1.0),
            ("wc suite", TipoAmbiente.Suite, 1.0),
            ("bwc suite", TipoAmbiente.Suite, 1.0),

            // === Lavabo (antes de "banheiro") ===
            ("lavabo", TipoAmbiente.Lavabo, 1.0),
            ("lav", TipoAmbiente.Lavabo, 0.7),
            ("toilette", TipoAmbiente.Lavabo, 0.95),
            ("toilet", TipoAmbiente.Lavabo, 0.9),

            // === Banheiro ===
            ("banheiro", TipoAmbiente.Banheiro, 1.0),
            ("wc", TipoAmbiente.Banheiro, 1.0),
            ("bwc", TipoAmbiente.Banheiro, 1.0),
            ("banho", TipoAmbiente.Banheiro, 0.9),
            ("banheiro social", TipoAmbiente.Banheiro, 1.0),
            ("banheiro empregada", TipoAmbiente.Banheiro, 1.0),
            ("banheiro servico", TipoAmbiente.Banheiro, 0.95),
            ("sanitario", TipoAmbiente.Banheiro, 0.9),

            // === Lavanderia ===
            ("lavanderia", TipoAmbiente.Lavanderia, 1.0),
            ("lav. roupas", TipoAmbiente.Lavanderia, 1.0),
            ("lav roupas", TipoAmbiente.Lavanderia, 1.0),
            ("lavandaria", TipoAmbiente.Lavanderia, 0.95),

            // === Área de Serviço ===
            ("area de servico", TipoAmbiente.AreaDeServico, 1.0),
            ("area servico", TipoAmbiente.AreaDeServico, 0.95),
            ("a.s.", TipoAmbiente.AreaDeServico, 0.85),
            ("a.s", TipoAmbiente.AreaDeServico, 0.85),
            ("servico", TipoAmbiente.AreaDeServico, 0.6),

            // === Cozinha (genérica) ===
            ("cozinha", TipoAmbiente.Cozinha, 1.0),
            ("coz", TipoAmbiente.Cozinha, 0.85),
            ("coz.", TipoAmbiente.Cozinha, 0.85),
            ("copa", TipoAmbiente.Cozinha, 0.8),
            ("copa cozinha", TipoAmbiente.Cozinha, 0.95),

            // === Área Externa ===
            ("area externa", TipoAmbiente.AreaExterna, 1.0),
            ("quintal", TipoAmbiente.AreaExterna, 1.0),
            ("jardim", TipoAmbiente.AreaExterna, 0.9),
            ("terraco", TipoAmbiente.AreaExterna, 0.85),
            ("varanda", TipoAmbiente.AreaExterna, 0.8),
            ("sacada", TipoAmbiente.AreaExterna, 0.8),
            ("pergolado", TipoAmbiente.AreaExterna, 0.75),
            ("churrasqueira", TipoAmbiente.AreaExterna, 0.8),
            ("piscina", TipoAmbiente.AreaExterna, 0.9),
        };

        /// <summary>
        /// Classifica um ambiente a partir do seu nome.
        /// </summary>
        /// <param name="nomeAmbiente">Nome do ambiente como aparece no modelo.</param>
        /// <returns>Resultado com tipo, confiança e padrão utilizado.</returns>
        public ResultadoClassificacao Classificar(string nomeAmbiente)
        {
            if (string.IsNullOrWhiteSpace(nomeAmbiente))
            {
                return new ResultadoClassificacao
                {
                    Tipo = TipoAmbiente.NaoIdentificado,
                    Confianca = 0.0,
                    PadraoUtilizado = string.Empty
                };
            }

            var textoNormalizado = Normalizar(nomeAmbiente);

            // Estratégia 1: Match exato
            foreach (var (padrao, tipo, pesoBase) in _padroes)
            {
                if (textoNormalizado == padrao)
                {
                    return new ResultadoClassificacao
                    {
                        Tipo = tipo,
                        Confianca = Math.Min(pesoBase * 1.0, 1.0),
                        PadraoUtilizado = padrao
                    };
                }
            }

            // Estratégia 2: Texto contém o padrão (ex: "banheiro social 01" contém "banheiro social")
            ResultadoClassificacao? melhorResultado = null;
            int maiorTamanhoPadrao = 0;

            foreach (var (padrao, tipo, pesoBase) in _padroes)
            {
                if (textoNormalizado.Contains(padrao) && padrao.Length > maiorTamanhoPadrao)
                {
                    maiorTamanhoPadrao = padrao.Length;
                    melhorResultado = new ResultadoClassificacao
                    {
                        Tipo = tipo,
                        Confianca = Math.Min(pesoBase * 0.85, 1.0),
                        PadraoUtilizado = padrao
                    };
                }
            }

            if (melhorResultado != null)
                return melhorResultado;

            // Estratégia 3: Padrão contido no texto com palavras parciais
            foreach (var (padrao, tipo, pesoBase) in _padroes)
            {
                var palavrasPadrao = padrao.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                var palavrasTexto = textoNormalizado.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                var matches = palavrasPadrao.Count(pp =>
                    palavrasTexto.Any(pt => pt.Contains(pp) || pp.Contains(pt)));

                if (matches > 0)
                {
                    var ratio = (double)matches / palavrasPadrao.Length;
                    if (ratio >= 0.5)
                    {
                        var confianca = pesoBase * ratio * 0.7;
                        if (melhorResultado == null || confianca > melhorResultado.Confianca)
                        {
                            melhorResultado = new ResultadoClassificacao
                            {
                                Tipo = tipo,
                                Confianca = Math.Min(confianca, 1.0),
                                PadraoUtilizado = padrao
                            };
                        }
                    }
                }
            }

            return melhorResultado ?? new ResultadoClassificacao
            {
                Tipo = TipoAmbiente.NaoIdentificado,
                Confianca = 0.0,
                PadraoUtilizado = string.Empty
            };
        }

        /// <summary>
        /// Classifica uma lista de ambientes.
        /// </summary>
        public void ClassificarTodos(List<AmbienteInfo> ambientes)
        {
            foreach (var ambiente in ambientes)
            {
                ambiente.Classificacao = Classificar(ambiente.NomeOriginal);
            }
        }

        /// <summary>
        /// Normaliza o texto: remove acentos, converte para lowercase, remove caracteres especiais.
        /// </summary>
        internal static string Normalizar(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return string.Empty;

            // Lowercase
            texto = texto.ToLowerInvariant();

            // Remove acentos
            texto = RemoverAcentos(texto);

            // Remove numeração final (ex: "banheiro 01" → "banheiro")
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\s*\d+\s*$", "");

            // Normaliza espaços
            texto = System.Text.RegularExpressions.Regex.Replace(texto, @"\s+", " ").Trim();

            return texto;
        }

        /// <summary>
        /// Remove acentos de um texto usando decomposição Unicode.
        /// </summary>
        private static string RemoverAcentos(string texto)
        {
            var normalizado = texto.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();

            foreach (var c in normalizado)
            {
                var categoria = CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
