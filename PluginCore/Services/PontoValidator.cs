using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Validador de pontos hidráulicos.
    /// Verifica pressão mínima, pressão máxima e conformidade normativa.
    /// </summary>
    public class PontoValidator
    {
        private readonly ILogService _log;

        private const string ETAPA = "05_Pressao";
        private const string COMPONENTE = "PontoValidator";

        /// <summary>Pressão mínima (mCA) — NBR 5626 § 5.4.2.1.</summary>
        private const double PRESSAO_MINIMA = 0.5;

        /// <summary>Pressão mínima recomendada para conforto (mCA).</summary>
        private const double PRESSAO_MINIMA_CONFORTO = 3.0;

        /// <summary>Pressão máxima estática (mCA) — NBR 5626 § 5.4.2.3.</summary>
        private const double PRESSAO_MAXIMA = 40.0;

        public PontoValidator(ILogService log)
        {
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DE PRESSÃO MÍNIMA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica pressão mínima (≥ 3.0 mCA) em todos os pontos.
        /// Pressão = pressão do reservatório - altura do ponto - perdas acumuladas.
        /// </summary>
        /// <param name="pontos">Pontos a verificar.</param>
        /// <param name="pressaoReservatorio">Pressão no reservatório (mCA) ou altura da coluna.</param>
        /// <param name="perdasAcumuladas">Perdas de carga acumuladas até o trecho (mCA).</param>
        /// <returns>true se não há bloqueios.</returns>
        public bool VerificarPressaoMinima(IEnumerable<PontoHidraulico> pontos,
            double pressaoReservatorio, double perdasAcumuladas = 0)
        {
            if (pontos == null)
                return true;

            var lista = pontos.ToList();

            if (lista.Count == 0)
                return true;

            var insuficientes = 0;
            var abaixoConforto = 0;
            var acimMaxima = 0;
            var ok = 0;

            _log.Info(ETAPA, COMPONENTE,
                $"Verificando pressão de {lista.Count} pontos " +
                $"(Reservatório: {pressaoReservatorio:F1} mCA, " +
                $"Perdas acumuladas: {perdasAcumuladas:F2} mCA).");

            foreach (var ponto in lista)
            {
                if (ponto == null)
                    continue;

                // Apenas pontos de água fria/quente
                if (ponto.Sistema != HydraulicSystem.ColdWater &&
                    ponto.Sistema != HydraulicSystem.HotWater)
                    continue;

                // Pressão = Reservatório - Altura do ponto - Perdas
                var pressao = pressaoReservatorio - ponto.PosZ - perdasAcumuladas;

                // Caso 1: Abaixo do mínimo absoluto (NBR 5626)
                if (pressao < PRESSAO_MINIMA)
                {
                    insuficientes++;
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Pressão insuficiente: {pressao:F2} mCA no ponto '{ponto.Id}' " +
                        $"({ponto.TipoEquipamento}, Nível: '{ponto.Nivel}'). " +
                        $"Mínimo NBR 5626: {PRESSAO_MINIMA} mCA. " +
                        $"Ação: Instalar pressurizador ou reposicionar reservatório.");
                    continue;
                }

                // Caso 2: Abaixo do conforto
                if (pressao < PRESSAO_MINIMA_CONFORTO)
                {
                    abaixoConforto++;
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Pressão abaixo do conforto: {pressao:F2} mCA no ponto '{ponto.Id}' " +
                        $"({ponto.TipoEquipamento}). " +
                        $"Recomendado: ≥ {PRESSAO_MINIMA_CONFORTO} mCA.");
                    continue;
                }

                // Caso 3: Acima da máxima
                if (pressao > PRESSAO_MAXIMA)
                {
                    acimMaxima++;
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Pressão excessiva: {pressao:F2} mCA no ponto '{ponto.Id}' " +
                        $"({ponto.TipoEquipamento}). " +
                        $"Máximo: {PRESSAO_MAXIMA} mCA. " +
                        $"Ação: Instalar válvula redutora de pressão.");
                    continue;
                }

                ok++;
            }

            // Resumo
            _log.Info(ETAPA, COMPONENTE,
                $"Verificação de pressão: " +
                $"{ok} OK, " +
                $"{insuficientes} insuficientes, " +
                $"{abaixoConforto} abaixo do conforto, " +
                $"{acimMaxima} excessivas.");

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO COM CÁLCULO AUTOMÁTICO DE PERDAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica pressão considerando perdas de carga calculadas por trecho.
        /// </summary>
        public bool VerificarPressaoComPerdas(IEnumerable<PontoHidraulico> pontos,
            IEnumerable<TrechoTubulacao> trechos, double pressaoReservatorio)
        {
            if (pontos == null || trechos == null)
                return true;

            var perdasTotal = trechos
                .Where(t => t != null)
                .Sum(t => t.PerdaCargaTotal);

            return VerificarPressaoMinima(pontos, pressaoReservatorio, perdasTotal);
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO COMPLETA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa todas as validações de pontos hidráulicos.
        /// </summary>
        public bool ValidarTodos(IEnumerable<PontoHidraulico> pontos,
            double pressaoReservatorio, double perdasAcumuladas = 0)
        {
            if (pontos == null)
                return true;

            var lista = pontos.ToList();

            _log.Info(ETAPA, COMPONENTE,
                $"Iniciando validação completa de {lista.Count} pontos.");

            VerificarPressaoMinima(lista, pressaoReservatorio, perdasAcumuladas);
            VerificarDiametros(lista);

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DE DIÂMETROS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se os diâmetros dos pontos estão definidos.
        /// </summary>
        public bool VerificarDiametros(IEnumerable<PontoHidraulico> pontos)
        {
            if (pontos == null)
                return true;

            foreach (var ponto in pontos)
            {
                if (ponto == null)
                    continue;

                if (ponto.DiametroNominal <= 0)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Ponto '{ponto.Id}' sem diâmetro definido " +
                        $"({ponto.TipoEquipamento}).");
                }
            }

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera resumo textual da validação dos pontos.
        /// </summary>
        public string GerarResumo(IEnumerable<PontoHidraulico> pontos,
            double pressaoReservatorio, double perdasAcumuladas = 0)
        {
            var lista = pontos?.ToList() ?? new();

            if (lista.Count == 0)
                return "Nenhum ponto para validar.";

            var pontosAF = lista
                .Where(p => p.Sistema == HydraulicSystem.ColdWater ||
                            p.Sistema == HydraulicSystem.HotWater)
                .ToList();

            var pressoes = pontosAF
                .Select(p => pressaoReservatorio - p.PosZ - perdasAcumuladas)
                .ToList();

            if (pressoes.Count == 0)
                return "Nenhum ponto de água fria/quente.";

            var pMin = pressoes.Min();
            var pMax = pressoes.Max();
            var insuf = pressoes.Count(p => p < PRESSAO_MINIMA_CONFORTO);

            return $"══ Validação de Pressão ══\n" +
                   $"  Pontos AF/AQ:   {pontosAF.Count}\n" +
                   $"  P reserv.:      {pressaoReservatorio:F1} mCA\n" +
                   $"  Perdas:         {perdasAcumuladas:F2} mCA\n" +
                   $"  P mínima:       {pMin:F2} mCA\n" +
                   $"  P máxima:       {pMax:F2} mCA\n" +
                   $"  Insuficientes:  {insuf}\n" +
                   $"  Status:         {(insuf > 0 ? "⚠️ ATENÇÃO" : "✅ APROVADO")}\n" +
                   $"══════════════════════════";
        }

        // Exemplos:
        // var validator = new PontoValidator(logService);
        //
        // // Reservatório a 15m de altura, perdas de 2.5 mCA
        // validator.VerificarPressaoMinima(pontos, 15.0, 2.5);
        //
        // // Com cálculo automático de perdas
        // validator.VerificarPressaoComPerdas(pontos, trechos, 15.0);
        //
        // // Validação completa
        // validator.ValidarTodos(pontos, 15.0, 2.5);
        //
        // Console.WriteLine(validator.GerarResumo(pontos, 15.0, 2.5));
    }
}
