using PluginCore.Data;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Resultado da verificação de ventilação de um trecho/prumada.
    /// </summary>
    public class ResultadoVentilacao
    {
        /// <summary>ID do elemento verificado.</summary>
        public string ElementoId { get; set; } = string.Empty;

        /// <summary>Tipo (Trecho, Prumada, Ramal).</summary>
        public string TipoElemento { get; set; } = string.Empty;

        /// <summary>Diâmetro do elemento (mm).</summary>
        public int DiametroElemento { get; set; }

        /// <summary>Possui ventilação.</summary>
        public bool PossuiVentilacao { get; set; }

        /// <summary>Diâmetro da ventilação existente (mm).</summary>
        public int DiametroVentilacao { get; set; }

        /// <summary>Diâmetro mínimo exigido para ventilação (mm).</summary>
        public int DiametroVentilacaoMinimo { get; set; }

        /// <summary>Ventilação adequada.</summary>
        public bool Aprovado { get; set; }

        /// <summary>Motivo da reprovação.</summary>
        public string Motivo { get; set; } = string.Empty;

        public override string ToString()
        {
            var status = Aprovado ? "✅" : "❌";
            var vent = PossuiVentilacao
                ? $"Vent DN{DiametroVentilacao} (min: DN{DiametroVentilacaoMinimo})"
                : "SEM VENTILAÇÃO";
            return $"{status} {TipoElemento} '{ElementoId}': DN{DiametroElemento} | {vent}";
        }
    }

    /// <summary>
    /// Serviço de verificação e dimensionamento de ventilação de esgoto.
    /// Conforme NBR 8160:1999 — § 5.1.2 e Tabela 7.
    /// </summary>
    public class VentilacaoService
    {
        private readonly ILogService? _log;

        private const string ETAPA = "08_Ventilacao";
        private const string COMPONENTE = "VentilacaoService";

        // ══════════════════════════════════════════════════════════
        //  TABELA NBR 8160 — VENTILAÇÃO (Tab. 7)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Diâmetro mínimo do tubo ventilador por DN do tubo de esgoto.
        /// </summary>
        private static readonly Dictionary<int, int> _ventilacaoPorDN = new()
        {
            [40] = 40,
            [50] = 40,
            [75] = 50,
            [100] = 75,
            [150] = 100,
            [200] = 100,
            [250] = 150,
            [300] = 150,
        };

        /// <summary>
        /// Comprimento máximo do ramal de ventilação (m) por DN ventilador.
        /// NBR 8160 — Tabela 8.
        /// </summary>
        private static readonly Dictionary<int, double> _comprimentoMaxVent = new()
        {
            [40] = 1.0,
            [50] = 1.5,
            [75] = 3.0,
            [100] = 6.0,
            [150] = 9.0,
        };

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public VentilacaoService() { }

        public VentilacaoService(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  DIÂMETRO MÍNIMO DE VENTILAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna diâmetro mínimo do ventilador para um DN de esgoto.
        /// </summary>
        public static int ObterDiametroVentilacao(int diametroEsgotoMm)
        {
            if (diametroEsgotoMm <= 0)
                return 40;

            // Busca exata
            if (_ventilacaoPorDN.TryGetValue(diametroEsgotoMm, out var dnVent))
                return dnVent;

            // Busca pelo próximo maior
            foreach (var kv in _ventilacaoPorDN.OrderBy(k => k.Key))
            {
                if (kv.Key >= diametroEsgotoMm)
                    return kv.Value;
            }

            return 150; // Maior disponível
        }

        /// <summary>
        /// Retorna comprimento máximo do ramal de ventilação (m).
        /// </summary>
        public static double ObterComprimentoMaximo(int diametroVentilacaoMm)
        {
            return _comprimentoMaxVent.TryGetValue(diametroVentilacaoMm, out var comp)
                ? comp : 6.0;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se um trecho de esgoto possui ventilação adequada.
        /// </summary>
        public ResultadoVentilacao VerificarVentilacao(TrechoTubulacao trecho)
        {
            var resultado = new ResultadoVentilacao
            {
                TipoElemento = "Trecho",
            };

            if (trecho == null)
                return resultado;

            resultado.ElementoId = trecho.Id;
            resultado.DiametroElemento = trecho.DiametroNominal;

            // Apenas trechos de esgoto
            if (trecho.Sistema != HydraulicSystem.Sewer)
            {
                resultado.Aprovado = true;
                resultado.Motivo = "Não é esgoto.";
                return resultado;
            }

            var dnVentMin = ObterDiametroVentilacao(trecho.DiametroNominal);
            resultado.DiametroVentilacaoMinimo = dnVentMin;

            // Verificar se o trecho precisa de ventilação individual
            var uhc = trecho.SomaPesos;
            var precisaVent = NecessitaVentilacaoIndividual(trecho.DiametroNominal, uhc);

            if (!precisaVent)
            {
                resultado.Aprovado = true;
                resultado.Motivo = "Ventilação individual não exigida.";
                return resultado;
            }

            // Verificar se possui ventilação
            if (!trecho.PossuiVentilacao)
            {
                resultado.PossuiVentilacao = false;
                resultado.Aprovado = false;
                resultado.Motivo = "Trecho sem ventilação — exigida pela NBR 8160.";

                _log?.Critico(ETAPA, COMPONENTE,
                    $"Trecho '{trecho.Id}' (DN{trecho.DiametroNominal}): " +
                    $"SEM VENTILAÇÃO. Exigido: DN{dnVentMin}.");
                return resultado;
            }

            resultado.PossuiVentilacao = true;
            resultado.DiametroVentilacao = trecho.DiametroVentilacao;

            // Verificar diâmetro
            if (trecho.DiametroVentilacao < dnVentMin)
            {
                resultado.Aprovado = false;
                resultado.Motivo = $"Ventilação DN{trecho.DiametroVentilacao} " +
                                   $"< mínimo DN{dnVentMin}.";

                _log?.Medio(ETAPA, COMPONENTE,
                    $"Trecho '{trecho.Id}': ventilação DN{trecho.DiametroVentilacao} " +
                    $"insuficiente (mín: DN{dnVentMin}).");
                return resultado;
            }

            resultado.Aprovado = true;
            return resultado;
        }

        /// <summary>
        /// Verifica ventilação de uma prumada de esgoto.
        /// </summary>
        public ResultadoVentilacao VerificarPrumada(Prumada prumada)
        {
            var resultado = new ResultadoVentilacao
            {
                TipoElemento = "Prumada",
            };

            if (prumada == null)
                return resultado;

            resultado.ElementoId = prumada.Id;
            resultado.DiametroElemento = prumada.DiametroNominal;

            if (prumada.Sistema != HydraulicSystem.Sewer)
            {
                resultado.Aprovado = true;
                return resultado;
            }

            var dnVentMin = ObterDiametroVentilacao(prumada.DiametroNominal);
            resultado.DiametroVentilacaoMinimo = dnVentMin;

            // Prumadas de esgoto sempre necessitam ventilação
            // (prolongamento ou coluna de ventilação paralela)
            resultado.PossuiVentilacao = true; // assumir prolongamento
            resultado.DiametroVentilacao = prumada.DiametroNominal; // prolongamento = mesmo DN
            resultado.Aprovado = true;

            _log?.Info(ETAPA, COMPONENTE,
                $"Prumada '{prumada.Id}' (DN{prumada.DiametroNominal}): " +
                $"ventilação por prolongamento.");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica ventilação de todos os trechos de esgoto.
        /// </summary>
        public List<ResultadoVentilacao> VerificarTodos(
            IEnumerable<TrechoTubulacao> trechos)
        {
            if (trechos == null)
                return new List<ResultadoVentilacao>();

            var lista = trechos
                .Where(t => t != null && t.Sistema == HydraulicSystem.Sewer)
                .ToList();

            _log?.Info(ETAPA, COMPONENTE,
                $"Verificando ventilação de {lista.Count} trechos de esgoto.");

            var resultados = lista.Select(VerificarVentilacao).ToList();

            var aprovados = resultados.Count(r => r.Aprovado);
            var semVent = resultados.Count(r => !r.PossuiVentilacao && !r.Aprovado);
            var ventInsuf = resultados.Count(r => r.PossuiVentilacao && !r.Aprovado);

            _log?.Info(ETAPA, COMPONENTE,
                $"Ventilação: {aprovados} OK, " +
                $"{semVent} sem ventilação, {ventInsuf} insuficientes.");

            return resultados;
        }

        /// <summary>
        /// Verifica ventilação de um sistema completo (trechos + prumadas).
        /// </summary>
        public List<ResultadoVentilacao> VerificarSistema(SistemaMEP sistema)
        {
            if (sistema == null)
                return new List<ResultadoVentilacao>();

            var resultados = new List<ResultadoVentilacao>();

            // Trechos
            resultados.AddRange(VerificarTodos(sistema.Trechos));

            // Prumadas
            var prumadasEsgoto = sistema.Prumadas
                .Where(p => p.Sistema == HydraulicSystem.Sewer)
                .Select(VerificarPrumada);
            resultados.AddRange(prumadasEsgoto);

            return resultados;
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual.</summary>
        public static string GerarResumo(List<ResultadoVentilacao> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhum elemento verificado.";

            var aprovados = resultados.Count(r => r.Aprovado);
            var semVent = resultados.Count(r => !r.PossuiVentilacao && !r.Aprovado);

            var lines = new List<string>
            {
                "══ Verificação de Ventilação (NBR 8160) ══",
                $"  Total:           {resultados.Count}",
                $"  Aprovados:       {aprovados}",
                $"  Sem ventilação:  {semVent}",
                $"  Insuficientes:   {resultados.Count - aprovados - semVent}",
                "───────────────────────────────────────────",
                "   DN Esgoto → DN Ventilação mínimo",
                "   40-50     → DN 40",
                "   75        → DN 50",
                "   100       → DN 75",
                "   150-200   → DN 100",
                "   250-300   → DN 150",
                "───────────────────────────────────────────",
            };

            foreach (var r in resultados.Where(r => !r.Aprovado))
            {
                lines.Add($"  {r}");
            }

            lines.Add("═══════════════════════════════════════════");

            return string.Join("\n", lines);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Determina se o trecho necessita ventilação individual.
        /// Aparelhos com UHC ≥ 6 (vaso sanitário) ou DN ≥ 100 sempre exigem.
        /// </summary>
        private static bool NecessitaVentilacaoIndividual(int diametroMm, double uhc)
        {
            // Vaso sanitário (UHC ≥ 6)
            if (uhc >= 6.0)
                return true;

            // DN ≥ 100 (ramais de esgoto principais)
            if (diametroMm >= 100)
                return true;

            // Distância > 1.5m do ponto de ventilação mais próximo
            // (verificação simplificada)
            return false;
        }

        // Exemplos:
        // var service = new VentilacaoService(logService);
        //
        // var r = service.VerificarVentilacao(trecho);
        // r.Aprovado → true/false
        //
        // var todos = service.VerificarTodos(trechos);
        // Console.WriteLine(VentilacaoService.GerarResumo(todos));
        //
        // VentilacaoService.ObterDiametroVentilacao(100) → 75
        // VentilacaoService.ObterDiametroVentilacao(150) → 100
        // VentilacaoService.ObterComprimentoMaximo(75)   → 3.0 m
    }
}
