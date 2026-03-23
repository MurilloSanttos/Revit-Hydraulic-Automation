using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Validador de trechos de tubulação.
    /// Verifica velocidade, diâmetro, perda de carga e conformidade normativa.
    /// </summary>
    public class TrechoValidator
    {
        private readonly ILogService _log;
        private readonly DimensionamentoService _dimensionamento;

        private const string ETAPA = "04_Dimensionamento";
        private const string COMPONENTE = "TrechoValidator";

        /// <summary>Velocidade máxima permitida (m/s) — NBR 5626.</summary>
        private const double VELOCIDADE_MAXIMA = 3.0;

        /// <summary>Velocidade mínima recomendada (m/s) — evitar sedimentação.</summary>
        private const double VELOCIDADE_MINIMA = 0.5;

        /// <summary>Pressão mínima (mca) — NBR 5626.</summary>
        private const double PRESSAO_MINIMA_MCA = 0.5;

        /// <summary>Pressão máxima estática (mca) — NBR 5626.</summary>
        private const double PRESSAO_MAXIMA_MCA = 40.0;

        public TrechoValidator(ILogService log)
        {
            _log = log;
            _dimensionamento = new DimensionamentoService();
        }

        public TrechoValidator(ILogService log, DimensionamentoService dimensionamento)
        {
            _log = log;
            _dimensionamento = dimensionamento;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DE VELOCIDADE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica velocidade máxima (≤ 3.0 m/s) em todos os trechos.
        /// </summary>
        /// <returns>true se não há bloqueios.</returns>
        public bool VerificarVelocidadeMaxima(IEnumerable<TrechoTubulacao> trechos)
        {
            if (trechos == null)
                return true;

            var lista = trechos.ToList();
            var violacoes = 0;
            var avisos = 0;

            _log.Info(ETAPA, COMPONENTE,
                $"Verificando velocidade de {lista.Count} trechos " +
                $"(limite: {VELOCIDADE_MAXIMA} m/s).");

            foreach (var trecho in lista)
            {
                if (trecho == null)
                    continue;

                var velocidade = CalcularVelocidade(trecho);

                // Velocidade excede o máximo
                if (velocidade > VELOCIDADE_MAXIMA)
                {
                    violacoes++;
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Velocidade máxima excedida: {velocidade:F2} m/s " +
                        $"no trecho '{trecho.Id}' " +
                        $"(DN{trecho.DiametroNominal}, Q={trecho.Vazao:F3} L/s). " +
                        $"Limite: {VELOCIDADE_MAXIMA} m/s. " +
                        $"Ação: Aumentar diâmetro.");
                }
                // Velocidade muito baixa (risco de sedimentação)
                else if (velocidade > 0 && velocidade < VELOCIDADE_MINIMA)
                {
                    avisos++;
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Velocidade muito baixa: {velocidade:F2} m/s " +
                        $"no trecho '{trecho.Id}' " +
                        $"(DN{trecho.DiametroNominal}). " +
                        $"Risco de sedimentação. Considere reduzir diâmetro.");
                }
            }

            // Resumo
            var ok = lista.Count - violacoes - avisos;
            _log.Info(ETAPA, COMPONENTE,
                $"Verificação de velocidade: " +
                $"{ok} OK, {violacoes} acima do limite, {avisos} abaixo do mínimo.");

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO COMPLETA DE TRECHOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa todas as validações em uma lista de trechos.
        /// </summary>
        public bool ValidarTodos(IEnumerable<TrechoTubulacao> trechos)
        {
            if (trechos == null)
                return true;

            var lista = trechos.ToList();

            _log.Info(ETAPA, COMPONENTE,
                $"Iniciando validação completa de {lista.Count} trechos.");

            VerificarVelocidadeMaxima(lista);
            VerificarDiametros(lista);
            VerificarPerdaCarga(lista);

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DE DIÂMETROS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se os diâmetros estão definidos e são válidos.
        /// </summary>
        public bool VerificarDiametros(IEnumerable<TrechoTubulacao> trechos)
        {
            if (trechos == null)
                return true;

            var lista = trechos.ToList();
            var erros = 0;

            foreach (var trecho in lista)
            {
                if (trecho == null)
                    continue;

                if (trecho.DiametroNominal <= 0)
                {
                    erros++;
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Trecho '{trecho.Id}' sem diâmetro definido. " +
                        $"Execute o dimensionamento antes da validação.");
                }
                else if (trecho.DiametroInterno <= 0)
                {
                    erros++;
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Trecho '{trecho.Id}' sem diâmetro interno (DN={trecho.DiametroNominal}). " +
                        $"Verifique a tabela de materiais.");
                }
            }

            if (erros == 0 && lista.Count > 0)
            {
                _log.Info(ETAPA, COMPONENTE,
                    $"Diâmetros: {lista.Count} trechos OK.");
            }

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DE PERDA DE CARGA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se a perda de carga unitária é aceitável (≤ 0.08 m/m).
        /// </summary>
        public bool VerificarPerdaCarga(IEnumerable<TrechoTubulacao> trechos)
        {
            if (trechos == null)
                return true;

            var lista = trechos.ToList();
            const double PERDA_MAXIMA = 0.08; // m/m

            foreach (var trecho in lista)
            {
                if (trecho == null)
                    continue;

                if (trecho.PerdaCargaUnitaria > PERDA_MAXIMA)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Perda de carga alta no trecho '{trecho.Id}': " +
                        $"{trecho.PerdaCargaUnitaria:F4} m/m " +
                        $"(limite: {PERDA_MAXIMA} m/m). " +
                        $"Considere aumentar o diâmetro.");
                }
            }

            var perdaTotal = lista.Sum(t => t.PerdaCargaTotal);
            _log.Info(ETAPA, COMPONENTE,
                $"Perda de carga total do percurso: {perdaTotal:F3} m.");

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  GERAÇÃO DE RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera resumo textual da validação dos trechos.
        /// </summary>
        public string GerarResumo(IEnumerable<TrechoTubulacao> trechos)
        {
            var lista = trechos?.ToList() ?? new();

            if (lista.Count == 0)
                return "Nenhum trecho para validar.";

            var vMax = lista.Max(t => t.Velocidade);
            var vMin = lista.Where(t => t.Velocidade > 0)
                .Select(t => t.Velocidade)
                .DefaultIfEmpty(0)
                .Min();
            var perdaTotal = lista.Sum(t => t.PerdaCargaTotal);
            var excedidos = lista.Count(t => t.Velocidade > VELOCIDADE_MAXIMA);

            return $"══ Validação de Trechos ══\n" +
                   $"  Trechos:        {lista.Count}\n" +
                   $"  V máx:          {vMax:F2} m/s\n" +
                   $"  V mín:          {vMin:F2} m/s\n" +
                   $"  Excedidos:      {excedidos}\n" +
                   $"  Perda total:    {perdaTotal:F3} m\n" +
                   $"  Status:         {(excedidos > 0 ? "❌ REPROVADO" : "✅ APROVADO")}\n" +
                   $"══════════════════════════";
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula velocidade do fluido no trecho.
        /// V = Q / A = (Q/1000) / (π/4 × (D/1000)²)
        /// </summary>
        private double CalcularVelocidade(TrechoTubulacao trecho)
        {
            // Se já calculada, usar o valor
            if (trecho.Velocidade > 0)
                return trecho.Velocidade;

            if (trecho.Vazao <= 0 || trecho.DiametroInterno <= 0)
                return 0.0;

            return _dimensionamento.CalcularVelocidade(trecho.Vazao, trecho.DiametroInterno);
        }

        // Exemplos:
        // var validator = new TrechoValidator(logService);
        // validator.VerificarVelocidadeMaxima(trechos);
        // validator.ValidarTodos(trechos);
        //
        // Console.WriteLine(validator.GerarResumo(trechos));
        // ══ Validação de Trechos ══
        //   Trechos:        12
        //   V máx:          2.85 m/s
        //   V mín:          0.62 m/s
        //   Excedidos:      0
        //   Perda total:    1.245 m
        //   Status:         ✅ APROVADO
        // ══════════════════════════
    }
}
