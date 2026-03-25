namespace PluginCore.Models
{
    /// <summary>
    /// Representa um Level (nível/pavimento) do modelo, agnóstico ao Revit.
    /// Contém dados essenciais para agrupamento vertical de ambientes,
    /// geração de prumadas e cálculos de pressão.
    /// </summary>
    public class LevelInfo
    {
        /// <summary>
        /// ID único do elemento no Revit (ElementId.Value).
        /// </summary>
        public long ElementId { get; set; }

        /// <summary>
        /// Nome do nível conforme definido no modelo.
        /// </summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>
        /// Elevação do nível em metros (convertida de pés).
        /// Referência: nível 0.00 do projeto.
        /// </summary>
        public double Elevacao { get; set; }

        /// <summary>
        /// Indica se este é o nível de referência do projeto (elevation = 0).
        /// </summary>
        public bool EhNivelBase => Math.Abs(Elevacao) < 0.01;

        /// <summary>
        /// Quantidade de ambientes detectados neste nível.
        /// Preenchido em etapas posteriores do pipeline.
        /// </summary>
        public int QuantidadeAmbientes { get; set; }

        /// <summary>
        /// Diferença de elevação para o próximo nível acima (em metros).
        /// -1 indica que não há nível superior (último pavimento).
        /// Preenchido pelo LevelReaderService após ordenação.
        /// </summary>
        public double PeDireitoAteProximo { get; set; } = -1;

        /// <summary>
        /// Indica se o nível é estrutural (Building Story).
        /// </summary>
        public bool EhPavimento { get; set; }

        public override string ToString()
        {
            return $"{Nome} (Elevação: {Elevacao:F2} m" +
                   $"{(EhPavimento ? " | Pavimento" : "")}" +
                   $"{(PeDireitoAteProximo > 0 ? $" | Pé-direito: {PeDireitoAteProximo:F2} m" : "")})";
        }
    }
}
