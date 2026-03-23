namespace PluginCore.Models
{
    /// <summary>
    /// Representa as informações de um ambiente detectado no modelo.
    /// Este modelo é agnóstico ao Revit — não possui dependências da API.
    /// </summary>
    public class AmbienteInfo
    {
        /// <summary>
        /// ID único do elemento no Revit (ElementId.Value).
        /// </summary>
        public long ElementId { get; set; }

        /// <summary>
        /// Nome original do ambiente conforme o modelo.
        /// </summary>
        public string NomeOriginal { get; set; } = string.Empty;

        /// <summary>
        /// Número do ambiente no modelo.
        /// </summary>
        public string Numero { get; set; } = string.Empty;

        /// <summary>
        /// Nome do nível (andar) onde o ambiente está localizado.
        /// </summary>
        public string Nivel { get; set; } = string.Empty;

        /// <summary>
        /// Área do ambiente em metros quadrados.
        /// </summary>
        public double AreaM2 { get; set; }

        /// <summary>
        /// Perímetro do ambiente em metros.
        /// </summary>
        public double PerimetroM { get; set; }

        /// <summary>
        /// Resultado da classificação automática.
        /// </summary>
        public ResultadoClassificacao Classificacao { get; set; } = new();

        /// <summary>
        /// Indica se o ambiente é um Room (arquitetura) ou um Space (MEP).
        /// </summary>
        public TipoElemento TipoElemento { get; set; } = TipoElemento.Room;

        /// <summary>
        /// ID do Space MEP correspondente (se existir).
        /// -1 indica que não há Space associado.
        /// </summary>
        public long SpaceIdCorrespondente { get; set; } = -1;

        /// <summary>
        /// Indica se o Space já existia ou foi criado pela automação.
        /// </summary>
        public bool SpaceCriadoAutomaticamente { get; set; }

        /// <summary>
        /// Coordenadas do ponto central do ambiente (X, Y, Z) em metros.
        /// </summary>
        public PontoXYZ PontoCentral { get; set; } = new();

        /// <summary>
        /// Lista de nomes dos equipamentos existentes no ambiente (modelados no Revit).
        /// </summary>
        public List<string> EquipamentosExistentes { get; set; } = new();

        /// <summary>
        /// Indica se o ambiente possui relevância hidráulica.
        /// Ambientes como salas, quartos, corredores não são relevantes.
        /// </summary>
        public bool EhRelevante => Classificacao.Tipo != TipoAmbiente.NaoIdentificado;

        /// <summary>
        /// Retorna uma descrição formatada do ambiente.
        /// </summary>
        public override string ToString()
        {
            return $"[{Numero}] {NomeOriginal} → {Classificacao.Tipo} " +
                   $"(Confiança: {Classificacao.Confianca:P0}) | Nível: {Nivel} | " +
                   $"Área: {AreaM2:F2} m²";
        }
    }

    /// <summary>
    /// Tipo de elemento de origem no Revit.
    /// </summary>
    public enum TipoElemento
    {
        Room,
        Space
    }

    /// <summary>
    /// Ponto 3D simples, agnóstico ao Revit.
    /// </summary>
    public class PontoXYZ
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public PontoXYZ() { }

        public PontoXYZ(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }
    }
}
