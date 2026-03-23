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

            var lista = pontos.ToList();

            VerificarPressaoMinima(lista, pressaoReservatorio, perdasTotal);
            VerificarPressaoMaxima(lista, pressaoReservatorio, perdasTotal);

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DE PRESSÃO MÁXIMA
        // ══════════════════════════════════════════════════════════

        /// <summary>Limite preventivo para alerta antecipado (mCA).</summary>
        private const double PRESSAO_ALERTA = 35.0;

        /// <summary>
        /// Verifica pressão máxima (≤ 40.0 mCA) em todos os pontos.
        /// Pressão estática = pressão do reservatório - altura do ponto.
        /// Se as perdas são baixas e o ponto está muito abaixo do reservatório,
        /// a pressão pode exceder o limite.
        /// </summary>
        /// <param name="pontos">Pontos a verificar.</param>
        /// <param name="pressaoReservatorio">Altura do reservatório ou pressão de alimentação (mCA).</param>
        /// <param name="perdasAcumuladas">Perdas de carga acumuladas (mCA).</param>
        /// <returns>true se não há bloqueios.</returns>
        public bool VerificarPressaoMaxima(IEnumerable<PontoHidraulico> pontos,
            double pressaoReservatorio, double perdasAcumuladas = 0)
        {
            if (pontos == null)
                return true;

            var lista = pontos.ToList();

            if (lista.Count == 0)
                return true;

            var excessivos = 0;
            var alertas = 0;
            var ok = 0;

            _log.Info(ETAPA, COMPONENTE,
                $"Verificando pressão máxima de {lista.Count} pontos " +
                $"(limite: {PRESSAO_MAXIMA} mCA).");

            foreach (var ponto in lista)
            {
                if (ponto == null)
                    continue;

                // Apenas pontos de água fria/quente
                if (ponto.Sistema != HydraulicSystem.ColdWater &&
                    ponto.Sistema != HydraulicSystem.HotWater)
                    continue;

                // Pressão estática = Reservatório - Altura do ponto - Perdas
                var pressao = pressaoReservatorio - ponto.PosZ - perdasAcumuladas;

                // Caso 1: Excede máxima (NBR 5626 § 5.4.2.3)
                if (pressao > PRESSAO_MAXIMA)
                {
                    excessivos++;
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Pressão máxima excedida: {pressao:F2} mCA no ponto '{ponto.Id}' " +
                        $"({ponto.TipoEquipamento}, Nível: '{ponto.Nivel}'). " +
                        $"Limite NBR 5626: {PRESSAO_MAXIMA} mCA. " +
                        $"Ação: Instalar válvula redutora de pressão (VRP).",
                        detalhes: $"P_reserv={pressaoReservatorio:F1}, " +
                                  $"Z_ponto={ponto.PosZ:F2}, " +
                                  $"Perdas={perdasAcumuladas:F2}, " +
                                  $"P_result={pressao:F2}");
                }
                // Caso 2: Próximo do limite (alerta preventivo)
                else if (pressao > PRESSAO_ALERTA)
                {
                    alertas++;
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Pressão próxima do limite: {pressao:F2} mCA no ponto '{ponto.Id}' " +
                        $"({ponto.TipoEquipamento}). " +
                        $"Limite: {PRESSAO_MAXIMA} mCA. Considere VRP preventiva.");
                }
                else
                {
                    ok++;
                }
            }

            // Resumo
            _log.Info(ETAPA, COMPONENTE,
                $"Verificação pressão máxima: " +
                $"{ok} OK, {excessivos} excessivos, {alertas} alertas.");

            return !_log.TemBloqueio;
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
            VerificarPressaoMaxima(lista, pressaoReservatorio, perdasAcumuladas);
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
            var excess = pressoes.Count(p => p > PRESSAO_MAXIMA);

            var status = insuf > 0 || excess > 0 ? "❌ REPROVADO" : "✅ APROVADO";

            return $"══ Validação de Pressão ══\n" +
                   $"  Pontos AF/AQ:   {pontosAF.Count}\n" +
                   $"  P reserv.:      {pressaoReservatorio:F1} mCA\n" +
                   $"  Perdas:         {perdasAcumuladas:F2} mCA\n" +
                   $"  P mínima:       {pMin:F2} mCA  {(pMin < PRESSAO_MINIMA_CONFORTO ? "⚠️" : "✅")}\n" +
                   $"  P máxima:       {pMax:F2} mCA  {(pMax > PRESSAO_MAXIMA ? "⚠️" : "✅")}\n" +
                   $"  Insuficientes:  {insuf}\n" +
                   $"  Excessivos:     {excess}\n" +
                   $"  Status:         {status}\n" +
                   $"══════════════════════════";
        }

        // Exemplos:
        // var validator = new PontoValidator(logService);
        //
        // // Verificar pressão mínima
        // validator.VerificarPressaoMinima(pontos, 15.0, 2.5);
        //
        // // Verificar pressão máxima
        // validator.VerificarPressaoMaxima(pontos, 15.0, 0.5);
        //
        // // Verificar com perdas automáticas
        // validator.VerificarPressaoComPerdas(pontos, trechos, 15.0);
        //
        // // Validação completa (mínima + máxima + diâmetros)
        // validator.ValidarTodos(pontos, 15.0, 2.5);
        //
        // Console.WriteLine(validator.GerarResumo(pontos, 15.0, 2.5));
    }
}
