using PluginCore.Common;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Tipo de rede hidráulica para um ponto.
    /// </summary>
    public enum TipoRede
    {
        AguaFria,
        AguaQuente,
        Esgoto,
        Ventilacao,
        PluvialD
    }

    /// <summary>
    /// Definição de um ponto hidráulico derivado de um equipamento.
    /// </summary>
    public class PontoHidraulicoDefinicao
    {
        /// <summary>Nome do ponto (ex: "Vaso Sanitário - AF").</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Nome do equipamento de origem.</summary>
        public string Equipamento { get; set; } = string.Empty;

        /// <summary>Se é obrigatório no ambiente.</summary>
        public bool Obrigatorio { get; set; }

        /// <summary>Tipo de rede (AguaFria, Esgoto, etc.).</summary>
        public TipoRede Rede { get; set; }

        /// <summary>Diâmetro em mm.</summary>
        public int DiametroMm { get; set; }

        /// <summary>Peso UHC do ponto.</summary>
        public double PesoUHC { get; set; }

        public override string ToString()
        {
            var tipo = Obrigatorio ? "OBR" : "OPC";
            return $"[{tipo}] {Nome} | {Rede} Ø{DiametroMm}mm UHC:{PesoUHC}";
        }
    }

    /// <summary>
    /// Serviço que determina os pontos hidráulicos necessários por ambiente.
    /// Mapeia equipamentos → pontos de água fria e esgoto.
    /// </summary>
    public class PontosHidraulicosService
    {
        // ══════════════════════════════════════════════════════════
        //  MAPEAMENTO EQUIPAMENTO → PONTOS
        // ══════════════════════════════════════════════════════════

        private static readonly Dictionary<string, Func<EquipamentoRegra, List<PontoHidraulicoDefinicao>>>
            _mapeamento = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Vaso Sanitário"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.3),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.7),
            },
            ["Lavatório"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Chuveiro"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Banheira"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Bidê"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Pia de Cozinha"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Tanque"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Máquina de Lavar"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Lava-louças"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC * 0.5),
                Ponto(eq, TipoRede.Esgoto,   eq.DiametroSaidaMm,   eq.PesoUHC * 0.5),
            },
            ["Ralo Sifonado"] = eq => new()
            {
                Ponto(eq, TipoRede.Esgoto, eq.DiametroSaidaMm, eq.PesoUHC),
            },
            ["Ralo Seco"] = eq => new()
            {
                Ponto(eq, TipoRede.Esgoto, eq.DiametroSaidaMm, eq.PesoUHC),
            },
            ["Filtro"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC),
            },
            ["Torneira de Jardim"] = eq => new()
            {
                Ponto(eq, TipoRede.AguaFria, eq.DiametroEntradaMm, eq.PesoUHC),
            },
        };

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS PÚBLICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna todos os pontos hidráulicos necessários para um ambiente.
        /// </summary>
        public IEnumerable<PontoHidraulicoDefinicao> GetPontos(AmbienteInfo ambiente)
        {
            if (ambiente == null)
                return Enumerable.Empty<PontoHidraulicoDefinicao>();

            return GetPontosPorTipo(ambiente.Classificacao.Tipo);
        }

        /// <summary>
        /// Retorna todos os pontos hidráulicos para um tipo de ambiente.
        /// </summary>
        public IEnumerable<PontoHidraulicoDefinicao> GetPontosPorTipo(TipoAmbiente tipo)
        {
            var equipamentos = EquipamentosPorAmbiente.Get(tipo);

            if (equipamentos.Count == 0)
                return Enumerable.Empty<PontoHidraulicoDefinicao>();

            var pontos = new List<PontoHidraulicoDefinicao>();

            foreach (var equip in equipamentos)
            {
                if (_mapeamento.TryGetValue(equip.Nome, out var gerador))
                {
                    pontos.AddRange(gerador(equip));
                }
                else
                {
                    // Equipamento sem mapeamento → gerar pontos padrão
                    if (equip.DiametroEntradaMm > 0)
                        pontos.Add(Ponto(equip, TipoRede.AguaFria, equip.DiametroEntradaMm, equip.PesoUHC * 0.5));
                    if (equip.DiametroSaidaMm > 0)
                        pontos.Add(Ponto(equip, TipoRede.Esgoto, equip.DiametroSaidaMm, equip.PesoUHC * 0.5));
                }
            }

            return pontos;
        }

        /// <summary>Retorna apenas pontos obrigatórios.</summary>
        public IEnumerable<PontoHidraulicoDefinicao> GetPontosObrigatorios(AmbienteInfo ambiente)
        {
            return GetPontos(ambiente).Where(p => p.Obrigatorio);
        }

        /// <summary>Retorna apenas pontos de água fria.</summary>
        public IEnumerable<PontoHidraulicoDefinicao> GetPontosAguaFria(AmbienteInfo ambiente)
        {
            return GetPontos(ambiente).Where(p => p.Rede == TipoRede.AguaFria);
        }

        /// <summary>Retorna apenas pontos de esgoto.</summary>
        public IEnumerable<PontoHidraulicoDefinicao> GetPontosEsgoto(AmbienteInfo ambiente)
        {
            return GetPontos(ambiente).Where(p => p.Rede == TipoRede.Esgoto);
        }

        /// <summary>Soma UHC de todos os pontos de um ambiente.</summary>
        public double SomaUHC(AmbienteInfo ambiente)
        {
            return GetPontos(ambiente).Sum(p => p.PesoUHC);
        }

        /// <summary>Gera resumo textual dos pontos de um ambiente.</summary>
        public string GerarResumo(AmbienteInfo ambiente)
        {
            var pontos = GetPontos(ambiente).ToList();

            if (pontos.Count == 0)
                return $"[{ambiente.NomeOriginal}] Sem pontos hidráulicos.";

            var af = pontos.Count(p => p.Rede == TipoRede.AguaFria);
            var es = pontos.Count(p => p.Rede == TipoRede.Esgoto);
            var obr = pontos.Count(p => p.Obrigatorio);
            var uhc = pontos.Sum(p => p.PesoUHC);

            return $"[{ambiente.NomeOriginal}] {pontos.Count} pontos " +
                   $"(AF:{af} ES:{es} | Obr:{obr}) UHC:{uhc:F1}";
        }

        // ══════════════════════════════════════════════════════════
        //  HELPER
        // ══════════════════════════════════════════════════════════

        private static PontoHidraulicoDefinicao Ponto(
            EquipamentoRegra equip, TipoRede rede, int diametro, double uhc)
        {
            var sufixo = rede switch
            {
                TipoRede.AguaFria => "AF",
                TipoRede.AguaQuente => "AQ",
                TipoRede.Esgoto => "ES",
                TipoRede.Ventilacao => "VE",
                _ => rede.ToString()
            };

            return new PontoHidraulicoDefinicao
            {
                Nome = $"{equip.Nome} - {sufixo}",
                Equipamento = equip.Nome,
                Obrigatorio = equip.Obrigatorio,
                Rede = rede,
                DiametroMm = diametro,
                PesoUHC = uhc,
            };
        }

        // Exemplos:
        // var service = new PontosHidraulicosService();
        // var pontos = service.GetPontos(ambiente);
        // → [Vaso Sanitário - AF, Vaso Sanitário - ES, Lavatório - AF, ...]
        //
        // var af = service.GetPontosAguaFria(ambiente);
        // var uhc = service.SomaUHC(ambiente);
        // var resumo = service.GerarResumo(ambiente);
        // → "[Banheiro] 9 pontos (AF:4 ES:5 | Obr:6) UHC:8.0"
    }
}
