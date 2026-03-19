namespace PluginCore.Models
{
    /// <summary>
    /// Classificação de tipo de ambiente hidráulico.
    /// Cada tipo determina quais equipamentos e conexões são esperados.
    /// </summary>
    public enum TipoAmbiente
    {
        NaoIdentificado = 0,
        Banheiro = 1,
        Lavabo = 2,
        Suite = 3,
        Cozinha = 4,
        CozinhaGourmet = 5,
        Lavanderia = 6,
        AreaDeServico = 7,
        AreaExterna = 8
    }

    /// <summary>
    /// Resultado da classificação de um ambiente pelo motor de NLP.
    /// </summary>
    public class ResultadoClassificacao
    {
        /// <summary>
        /// Tipo de ambiente identificado.
        /// </summary>
        public TipoAmbiente Tipo { get; set; } = TipoAmbiente.NaoIdentificado;

        /// <summary>
        /// Nível de confiança da classificação (0.0 a 1.0).
        /// Valores abaixo de 0.5 são considerados incertos.
        /// </summary>
        public double Confianca { get; set; } = 0.0;

        /// <summary>
        /// Padrão que foi utilizado para fazer o match.
        /// </summary>
        public string PadraoUtilizado { get; set; } = string.Empty;

        /// <summary>
        /// Indica se a classificação é confiável (confiança >= 0.7).
        /// </summary>
        public bool EhConfiavel => Confianca >= 0.7;

        /// <summary>
        /// Indica se a classificação necessita de validação humana (confiança entre 0.5 e 0.7).
        /// </summary>
        public bool NecessitaValidacao => Confianca >= 0.5 && Confianca < 0.7;
    }
}
