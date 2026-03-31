namespace PluginCore.Models
{
    /// <summary>
    /// Catálogo de famílias MEP carregadas no modelo Revit.
    /// Mapeia cada família aos seus símbolos (tipos) disponíveis.
    /// </summary>
    public class MepFamilyCatalog
    {
        /// <summary>
        /// Mapa: Nome da Família → Lista de nomes de FamilySymbol carregados.
        /// </summary>
        public Dictionary<string, List<string>> FamiliaParaSimbolos { get; set; } = new();

        /// <summary>
        /// Famílias que não possuem nenhum FamilySymbol carregado.
        /// </summary>
        public List<string> FamiliasSemSimbolos { get; set; } = new();

        /// <summary>Contagem de famílias com símbolos.</summary>
        public int TotalFamilias => FamiliaParaSimbolos.Count;

        /// <summary>Contagem total de símbolos em todas as famílias.</summary>
        public int TotalSimbolos => FamiliaParaSimbolos.Values.Sum(s => s.Count);

        /// <summary>
        /// Detalhes por categoria MEP (para relatórios).
        /// </summary>
        public Dictionary<string, int> FamiliasPorCategoria { get; set; } = new();

        /// <summary>
        /// Verifica se uma família específica está carregada no modelo.
        /// </summary>
        public bool ContainsFamilia(string nomeFamilia)
        {
            return FamiliaParaSimbolos.ContainsKey(nomeFamilia);
        }

        /// <summary>
        /// Verifica se um símbolo específico está disponível em qualquer família.
        /// </summary>
        public bool ContainsSimbolo(string nomeSimbolo)
        {
            return FamiliaParaSimbolos.Values
                .Any(simbolos => simbolos.Contains(nomeSimbolo));
        }

        /// <summary>
        /// Busca famílias cujo nome contém o termo (case-insensitive).
        /// </summary>
        public List<string> BuscarFamilias(string termo)
        {
            var termoLower = termo.ToLowerInvariant();
            return FamiliaParaSimbolos.Keys
                .Where(f => f.ToLowerInvariant().Contains(termoLower))
                .ToList();
        }

        /// <summary>
        /// Obtém os símbolos de uma família pelo nome exato.
        /// Retorna lista vazia se a família não existir.
        /// </summary>
        public List<string> ObterSimbolos(string nomeFamilia)
        {
            return FamiliaParaSimbolos.TryGetValue(nomeFamilia, out var simbolos)
                ? simbolos
                : new List<string>();
        }

        public override string ToString() =>
            $"Catálogo MEP: {TotalFamilias} famílias, {TotalSimbolos} símbolos, " +
            $"{FamiliasSemSimbolos.Count} sem símbolos";
    }
}
