using PluginCore.Common;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Referência espacial para posicionamento de ponto hidráulico.
    /// </summary>
    public enum ReferenciaEspacial
    {
        Prumada,
        Parede,
        CentroBaixo,
        CentroAmbiente,
        ProximoEquipamento
    }

    /// <summary>
    /// Regra de posicionamento relativo para um ponto hidráulico.
    /// </summary>
    public class RegraPosicionamento
    {
        /// <summary>Nome do ponto/equipamento.</summary>
        public string NomePonto { get; set; } = string.Empty;

        /// <summary>Descrição da regra.</summary>
        public string Regra { get; set; } = string.Empty;

        /// <summary>Distância máxima em metros.</summary>
        public double DistanciaMaxima { get; set; }

        /// <summary>Referência espacial.</summary>
        public ReferenciaEspacial Referencia { get; set; }

        /// <summary>Altura do ponto em relação ao piso (metros).</summary>
        public double AlturaM { get; set; }

        /// <summary>Prioridade de posicionamento (1 = mais prioritário).</summary>
        public int Prioridade { get; set; } = 5;

        /// <summary>Se o equipamento deve ficar encostado na referência.</summary>
        public bool Encostado { get; set; }

        public override string ToString()
        {
            return $"[P{Prioridade}] {NomePonto}: {Regra} " +
                   $"(max {DistanciaMaxima:F1}m da {Referencia}, h={AlturaM:F2}m)";
        }
    }

    /// <summary>
    /// Serviço de regras de posicionamento relativo para pontos hidráulicos.
    /// Define onde cada equipamento deve ser posicionado dentro do ambiente.
    /// </summary>
    public class PosicionamentoService
    {
        // ══════════════════════════════════════════════════════════
        //  REGRAS POR EQUIPAMENTO
        // ══════════════════════════════════════════════════════════

        private static readonly Dictionary<string, RegraPosicionamento> _regras = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Vaso Sanitário"] = new()
            {
                NomePonto = "Vaso Sanitário",
                Regra = "Próximo à prumada de esgoto, encostado na parede",
                DistanciaMaxima = 1.5,
                Referencia = ReferenciaEspacial.Prumada,
                AlturaM = 0.00,
                Prioridade = 1,
                Encostado = true,
            },
            ["Lavatório"] = new()
            {
                NomePonto = "Lavatório",
                Regra = "Fixado na parede, próximo ao vaso sanitário",
                DistanciaMaxima = 0.5,
                Referencia = ReferenciaEspacial.Parede,
                AlturaM = 0.60,
                Prioridade = 2,
                Encostado = true,
            },
            ["Chuveiro"] = new()
            {
                NomePonto = "Chuveiro",
                Regra = "No canto do box, fixado na parede",
                DistanciaMaxima = 0.7,
                Referencia = ReferenciaEspacial.Parede,
                AlturaM = 2.10,
                Prioridade = 3,
                Encostado = true,
            },
            ["Banheira"] = new()
            {
                NomePonto = "Banheira",
                Regra = "Encostada na parede, afastada do vaso sanitário",
                DistanciaMaxima = 0.3,
                Referencia = ReferenciaEspacial.Parede,
                AlturaM = 0.00,
                Prioridade = 3,
                Encostado = true,
            },
            ["Bidê"] = new()
            {
                NomePonto = "Bidê",
                Regra = "Adjacente ao vaso sanitário",
                DistanciaMaxima = 0.8,
                Referencia = ReferenciaEspacial.ProximoEquipamento,
                AlturaM = 0.00,
                Prioridade = 4,
                Encostado = true,
            },
            ["Pia de Cozinha"] = new()
            {
                NomePonto = "Pia de Cozinha",
                Regra = "Sob a bancada, encostada na parede com janela",
                DistanciaMaxima = 0.5,
                Referencia = ReferenciaEspacial.Parede,
                AlturaM = 0.85,
                Prioridade = 1,
                Encostado = true,
            },
            ["Tanque"] = new()
            {
                NomePonto = "Tanque",
                Regra = "Encostado na parede, próximo à prumada",
                DistanciaMaxima = 0.5,
                Referencia = ReferenciaEspacial.Parede,
                AlturaM = 0.85,
                Prioridade = 1,
                Encostado = true,
            },
            ["Máquina de Lavar"] = new()
            {
                NomePonto = "Máquina de Lavar",
                Regra = "Próxima ao tanque, encostada na parede",
                DistanciaMaxima = 0.6,
                Referencia = ReferenciaEspacial.ProximoEquipamento,
                AlturaM = 0.00,
                Prioridade = 2,
                Encostado = false,
            },
            ["Lava-louças"] = new()
            {
                NomePonto = "Lava-louças",
                Regra = "Sob a bancada, próxima à pia",
                DistanciaMaxima = 0.8,
                Referencia = ReferenciaEspacial.ProximoEquipamento,
                AlturaM = 0.00,
                Prioridade = 3,
                Encostado = false,
            },
            ["Ralo Sifonado"] = new()
            {
                NomePonto = "Ralo Sifonado",
                Regra = "Ponto mais baixo do piso, próximo ao chuveiro ou centro",
                DistanciaMaxima = 2.0,
                Referencia = ReferenciaEspacial.CentroBaixo,
                AlturaM = 0.00,
                Prioridade = 5,
                Encostado = false,
            },
            ["Ralo Seco"] = new()
            {
                NomePonto = "Ralo Seco",
                Regra = "Próximo à porta ou ponto de escoamento natural",
                DistanciaMaxima = 1.5,
                Referencia = ReferenciaEspacial.CentroBaixo,
                AlturaM = 0.00,
                Prioridade = 6,
                Encostado = false,
            },
            ["Filtro"] = new()
            {
                NomePonto = "Filtro",
                Regra = "Na parede acima da pia",
                DistanciaMaxima = 0.3,
                Referencia = ReferenciaEspacial.Parede,
                AlturaM = 1.50,
                Prioridade = 4,
                Encostado = true,
            },
            ["Torneira de Jardim"] = new()
            {
                NomePonto = "Torneira de Jardim",
                Regra = "Na parede externa, acessível",
                DistanciaMaxima = 0.3,
                Referencia = ReferenciaEspacial.Parede,
                AlturaM = 0.60,
                Prioridade = 2,
                Encostado = true,
            },
        };

        // ══════════════════════════════════════════════════════════
        //  MÉTODOS PÚBLICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna regras de posicionamento para todos os equipamentos de um ambiente.
        /// Ordenadas por prioridade.
        /// </summary>
        public IEnumerable<RegraPosicionamento> GetRegras(AmbienteInfo ambiente)
        {
            if (ambiente == null)
                return Enumerable.Empty<RegraPosicionamento>();

            return GetRegrasPorTipo(ambiente.Classificacao.Tipo);
        }

        /// <summary>
        /// Retorna regras de posicionamento por tipo de ambiente.
        /// </summary>
        public IEnumerable<RegraPosicionamento> GetRegrasPorTipo(TipoAmbiente tipo)
        {
            var equipamentos = EquipamentosPorAmbiente.Get(tipo);

            if (equipamentos.Count == 0)
                return Enumerable.Empty<RegraPosicionamento>();

            return equipamentos
                .Where(eq => _regras.ContainsKey(eq.Nome))
                .Select(eq => _regras[eq.Nome])
                .OrderBy(r => r.Prioridade);
        }

        /// <summary>
        /// Retorna a regra de posicionamento de um equipamento específico.
        /// </summary>
        public RegraPosicionamento? GetRegra(string nomeEquipamento)
        {
            return _regras.TryGetValue(nomeEquipamento, out var regra) ? regra : null;
        }

        /// <summary>
        /// Retorna apenas regras de equipamentos encostados na parede.
        /// </summary>
        public IEnumerable<RegraPosicionamento> GetRegrasParede(AmbienteInfo ambiente)
        {
            return GetRegras(ambiente)
                .Where(r => r.Referencia == ReferenciaEspacial.Parede);
        }

        /// <summary>
        /// Retorna apenas regras de equipamentos próximos à prumada.
        /// </summary>
        public IEnumerable<RegraPosicionamento> GetRegrasPrumada(AmbienteInfo ambiente)
        {
            return GetRegras(ambiente)
                .Where(r => r.Referencia == ReferenciaEspacial.Prumada);
        }

        /// <summary>
        /// Gera resumo textual das regras de um ambiente.
        /// </summary>
        public string GerarResumo(AmbienteInfo ambiente)
        {
            var regras = GetRegras(ambiente).ToList();

            if (regras.Count == 0)
                return $"[{ambiente.NomeOriginal}] Sem regras de posicionamento.";

            var linhas = regras.Select(r => $"  {r}");

            return $"[{ambiente.NomeOriginal}] {regras.Count} regras:\n" +
                   string.Join("\n", linhas);
        }

        // Exemplos:
        // var service = new PosicionamentoService();
        // var regras = service.GetRegras(ambiente);
        // → [P1] Vaso Sanitário: Próximo à prumada (max 1.5m)
        // → [P2] Lavatório: Fixado na parede (max 0.5m, h=0.60m)
        // → [P3] Chuveiro: No canto do box (max 0.7m, h=2.10m)
        // → [P5] Ralo Sifonado: Ponto mais baixo (max 2.0m)
    }
}
