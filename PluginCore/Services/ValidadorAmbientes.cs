using System.Text;
using PluginCore.Common;
using PluginCore.Domain.Enums;
using PluginCore.Interfaces;
using PluginCore.Logging;
using PluginCore.Models;

namespace PluginCore.Services
{
    /// <summary>
    /// Validador de ambientes — verifica a consistência dos dados detectados
    /// e gera logs apropriados para cada tipo de problema encontrado.
    /// </summary>
    public class ValidadorAmbientes
    {
        private readonly ILogService _log;
        private const string ETAPA = "01_Ambientes";
        private const string ETAPA_CLASS = "02_Classificacao";
        private const string COMPONENTE = "Validador";
        private const double LOW_CONFIDENCE_THRESHOLD = 0.6;
        private const double MIN_AREA = 1.5;
        private const double MAX_AREA = 100.0;

        public ValidadorAmbientes(ILogService log)
        {
            _log = log;
        }

        /// <summary>
        /// Executa todas as validações sobre a lista de ambientes.
        /// </summary>
        /// <returns>true se não há bloqueios (pode avançar), false se há erros críticos.</returns>
        public bool ValidarTodos(List<AmbienteInfo> ambientes)
        {
            if (ambientes == null || ambientes.Count == 0)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    "Nenhum ambiente detectado no modelo. Verifique se existem Rooms definidos.");
                return false;
            }

            _log.Info(ETAPA, COMPONENTE,
                $"Iniciando validação de {ambientes.Count} ambientes.");

            ValidarDuplicatas(ambientes);
            ValidarClassificacao(ambientes);
            ValidarAreas(ambientes);
            ValidarNomes(ambientes);
            ValidarCobertura(ambientes);

            var resumo = GerarResumo(ambientes);
            _log.Info(ETAPA, COMPONENTE, resumo);

            return !_log.TemBloqueio;
        }

        /// <summary>
        /// Verifica se há ambientes duplicados (mesmo número no mesmo nível).
        /// Duplicatas são erros CRÍTICOS — bloqueiam avanço do pipeline.
        /// </summary>
        private void ValidarDuplicatas(List<AmbienteInfo> ambientes)
        {
            var duplicatas = ambientes
                .Where(a => !string.IsNullOrEmpty(a.Numero))
                .GroupBy(a => new { a.Numero, a.Nivel })
                .Where(g => g.Count() > 1);

            foreach (var grupo in duplicatas)
            {
                var ids = string.Join(", ", grupo.Select(a => a.ElementId));

                _log.Critico(ETAPA, COMPONENTE,
                    $"Ambiente duplicado detectado: Número '{grupo.Key.Numero}', " +
                    $"Nível '{grupo.Key.Nivel}' — aparece {grupo.Count()} vezes (IDs: {ids}).");

                // Log individual de cada elemento duplicado
                foreach (var ambiente in grupo)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"  ↳ Duplicata: '{ambiente.NomeOriginal}' (#{ambiente.Numero}) " +
                        $"no nível '{ambiente.Nivel}'.",
                        ambiente.ElementId);
                }
            }
        }

        /// <summary>
        /// Valida a classificação de cada ambiente.
        /// </summary>
        private void ValidarClassificacao(List<AmbienteInfo> ambientes)
        {
            foreach (var ambiente in ambientes)
            {
                if (ambiente.Classificacao.Tipo == TipoAmbiente.NaoIdentificado)
                {
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Ambiente '{ambiente.NomeOriginal}' (#{ambiente.Numero}) não foi classificado. " +
                        $"Será ignorado nas etapas hidráulicas.",
                        ambiente.ElementId);
                }
                else if (ambiente.Classificacao.NecessitaValidacao)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Ambiente '{ambiente.NomeOriginal}' classificado como " +
                        $"'{ambiente.Classificacao.Tipo}' com confiança baixa " +
                        $"({ambiente.Classificacao.Confianca:P0}). Requer validação humana.",
                        ambiente.ElementId);
                }
            }
        }

        /// <summary>
        /// Valida áreas dos ambientes (detecta áreas suspeitas).
        /// </summary>
        private void ValidarAreas(List<AmbienteInfo> ambientes)
        {
            foreach (var ambiente in ambientes.Where(a => a.EhRelevante))
            {
                if (ambiente.AreaM2 < 1.0)
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Ambiente '{ambiente.NomeOriginal}' tem área muito pequena ({ambiente.AreaM2:F2} m²). " +
                        $"Pode indicar Room mal delimitado.",
                        ambiente.ElementId);
                }
                else if (ambiente.AreaM2 > 50.0)
                {
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Ambiente '{ambiente.NomeOriginal}' tem área muito grande ({ambiente.AreaM2:F2} m²). " +
                        $"Verifique se não é um ambiente composto.",
                        ambiente.ElementId);
                }
            }
        }

        /// <summary>
        /// Verifica se há ambientes sem nome.
        /// </summary>
        private void ValidarNomes(List<AmbienteInfo> ambientes)
        {
            var semNome = ambientes.Where(a => string.IsNullOrWhiteSpace(a.NomeOriginal)).ToList();
            
            if (semNome.Count > 0)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"{semNome.Count} ambiente(s) sem nome definido. " +
                    $"IDs: {string.Join(", ", semNome.Select(a => a.ElementId))}.");
            }
        }

        /// <summary>
        /// Verifica se o modelo tem uma cobertura mínima de ambientes hidráulicos.
        /// </summary>
        private void ValidarCobertura(List<AmbienteInfo> ambientes)
        {
            var relevantes = ambientes.Count(a => a.EhRelevante);
            var total = ambientes.Count;

            if (relevantes == 0)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    "Nenhum ambiente hidráulico relevante foi detectado. " +
                    "O modelo pode não conter Rooms nomeados corretamente.");
            }
            else
            {
                _log.Info(ETAPA, COMPONENTE,
                    $"Cobertura: {relevantes}/{total} ambientes são hidraulicamente relevantes " +
                    $"({(double)relevantes / total:P0}).");
            }

            // Verificar se há ao menos um banheiro
            var temBanheiro = ambientes.Any(a =>
                a.Classificacao.Tipo is TipoAmbiente.Banheiro
                                    or TipoAmbiente.Suite
                                    or TipoAmbiente.Lavabo);

            if (!temBanheiro)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    "Nenhum banheiro, suíte ou lavabo foi detectado. " +
                    "Verifique a nomenclatura dos Rooms.");
            }
        }

        /// <summary>
        /// Gera um resumo da detecção.
        /// </summary>
        private string GerarResumo(List<AmbienteInfo> ambientes)
        {
            var porTipo = ambientes
                .Where(a => a.EhRelevante)
                .GroupBy(a => a.Classificacao.Tipo)
                .OrderBy(g => g.Key)
                .Select(g => $"  {g.Key}: {g.Count()}")
                .ToList();

            var naoClassificados = ambientes.Count(a => !a.EhRelevante);

            return $"Resumo da detecção:\n" +
                   string.Join("\n", porTipo) +
                   $"\n  Não classificados: {naoClassificados}";
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE CLASSIFICAÇÃO (ClassificationResult)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida resultados de classificação: detecta não classificados e baixa confiança.
        /// </summary>
        /// <returns>true se pode avançar, false se há bloqueios.</returns>
        public bool ValidateClassificacao(IEnumerable<ClassificationResult> resultados)
        {
            if (resultados == null)
                return true;

            var lista = resultados.ToList();
            var total = lista.Count;
            var naoClassificados = 0;
            var baixaConfianca = 0;
            var classificados = 0;

            _log.Info(ETAPA_CLASS, COMPONENTE,
                $"Validando classificação de {total} ambientes.");

            foreach (var resultado in lista)
            {
                if (resultado == null)
                    continue;

                // Caso 1: Não classificado
                if (!resultado.Found || resultado.Tipo == null)
                {
                    naoClassificados++;
                    _log.Critico(ETAPA_CLASS, COMPONENTE,
                        $"Ambiente não identificado pelo classificador. " +
                        $"Input: '{resultado.InputNormalizado}'. " +
                        $"Estratégia: {resultado.Estrategia}.");
                    continue;
                }

                // Caso 2: Baixa confiança
                if (resultado.Confianca < LOW_CONFIDENCE_THRESHOLD)
                {
                    baixaConfianca++;
                    _log.Medio(ETAPA_CLASS, COMPONENTE,
                        $"Classificação com baixa confiança ({resultado.Confianca:P0}). " +
                        $"Input: '{resultado.InputNormalizado}' → {resultado.Tipo} " +
                        $"(Pattern: '{resultado.PatternMatched}', " +
                        $"Estratégia: {resultado.Estrategia}). Requer validação humana.");
                    continue;
                }

                classificados++;
            }

            // Resumo
            _log.Info(ETAPA_CLASS, COMPONENTE,
                $"Resultado da validação: " +
                $"{classificados}/{total} classificados OK, " +
                $"{baixaConfianca} com baixa confiança, " +
                $"{naoClassificados} não identificados.");

            // Gerar alerta se taxa de classificação for baixa
            if (total > 0)
            {
                var taxa = (double)classificados / total;
                if (taxa < 0.5)
                {
                    _log.Critico(ETAPA_CLASS, COMPONENTE,
                        $"Taxa de classificação muito baixa: {taxa:P0}. " +
                        $"Verifique a nomenclatura dos Rooms no modelo.");
                }
                else if (taxa < 0.8)
                {
                    _log.Medio(ETAPA_CLASS, COMPONENTE,
                        $"Taxa de classificação abaixo do ideal: {taxa:P0}. " +
                        $"Revise os ambientes não classificados.");
                }
            }

            return !_log.TemBloqueio;
        }

        /// <summary>
        /// Valida classificação por lote (BatchResult).
        /// </summary>
        public bool ValidateClassificacaoBatch(BatchClassifier.BatchResult batchResult)
        {
            if (batchResult == null)
                return true;

            _log.Info(ETAPA_CLASS, COMPONENTE,
                $"Validando lote: {batchResult.Total} ambientes, " +
                $"Taxa de acerto: {batchResult.TaxaAcerto:P0}, " +
                $"Confiança média: {batchResult.ConfiancaMedia:P0}.");

            if (batchResult.PorEstrategia.Count > 0)
            {
                var estrategias = string.Join(", ",
                    batchResult.PorEstrategia.Select(kv => $"{kv.Key}: {kv.Value}"));
                _log.Info(ETAPA_CLASS, COMPONENTE,
                    $"Distribuição por estratégia: {estrategias}.");
            }

            return ValidateClassificacao(batchResult.Resultados);
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE ÁREAS (público)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida áreas dos ambientes: detecta áreas zero, muito pequenas e muito grandes.
        /// </summary>
        /// <returns>true se não há bloqueios, false se há erros críticos.</returns>
        public bool ValidateArea(IEnumerable<AmbienteInfo> ambientes)
        {
            if (ambientes == null)
                return true;

            var lista = ambientes.ToList();
            var areaZero = 0;
            var areaPequena = 0;
            var areaGrande = 0;
            var areaOk = 0;

            _log.Info(ETAPA, COMPONENTE,
                $"Validando áreas de {lista.Count} ambientes " +
                $"(Min: {MIN_AREA} m², Max: {MAX_AREA} m²).");

            foreach (var ambiente in lista)
            {
                if (ambiente == null)
                    continue;

                // Caso 0: Área zero ou negativa
                if (ambiente.AreaM2 <= 0)
                {
                    areaZero++;
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Área inválida ({ambiente.AreaM2:F2} m²) para ambiente " +
                        $"'{ambiente.NomeOriginal}' (#{ambiente.Numero}). " +
                        $"Room pode não estar delimitado.",
                        ambiente.ElementId);
                    continue;
                }

                // Caso 1: Área muito pequena
                if (ambiente.AreaM2 < MIN_AREA)
                {
                    areaPequena++;
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Área muito pequena ({ambiente.AreaM2:F2} m²) para ambiente " +
                        $"'{ambiente.NomeOriginal}' (#{ambiente.Numero}, Nível: {ambiente.Nivel}). " +
                        $"Pode indicar Room mal delimitado.",
                        ambiente.ElementId);
                    continue;
                }

                // Caso 2: Área muito grande
                if (ambiente.AreaM2 > MAX_AREA)
                {
                    areaGrande++;
                    _log.Leve(ETAPA, COMPONENTE,
                        $"Área muito grande ({ambiente.AreaM2:F2} m²) para ambiente " +
                        $"'{ambiente.NomeOriginal}' (#{ambiente.Numero}, Nível: {ambiente.Nivel}). " +
                        $"Verifique se não é um ambiente composto.",
                        ambiente.ElementId);
                    continue;
                }

                areaOk++;
            }

            // Resumo
            _log.Info(ETAPA, COMPONENTE,
                $"Validação de áreas: " +
                $"{areaOk} OK, " +
                $"{areaPequena} pequenas, " +
                $"{areaGrande} grandes, " +
                $"{areaZero} inválidas.");

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE NOMES (público)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida nomes dos ambientes: detecta nomes vazios, inválidos e genéricos.
        /// </summary>
        /// <returns>true se não há bloqueios, false se há erros críticos.</returns>
        public bool ValidateNome(IEnumerable<AmbienteInfo> ambientes)
        {
            if (ambientes == null)
                return true;

            var lista = ambientes.ToList();
            var vazios = 0;
            var genericos = 0;
            var validos = 0;

            _log.Info(ETAPA, COMPONENTE,
                $"Validando nomes de {lista.Count} ambientes.");

            foreach (var ambiente in lista)
            {
                if (ambiente == null)
                    continue;

                // Caso 1: Nome vazio ou apenas espaços
                if (string.IsNullOrWhiteSpace(ambiente.NomeOriginal))
                {
                    vazios++;
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Ambiente com nome vazio ou inválido. " +
                        $"Número: '{ambiente.Numero}', Nível: '{ambiente.Nivel}'. " +
                        $"Defina um nome para o Room antes de prosseguir.",
                        ambiente.ElementId);
                    continue;
                }

                // Caso 2: Nome genérico (apenas números, "Room", etc.)
                var normalized = ambiente.NomeOriginal.Trim();
                if (IsNomeGenerico(normalized))
                {
                    genericos++;
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Ambiente com nome genérico: '{normalized}' " +
                        $"(#{ambiente.Numero}, Nível: '{ambiente.Nivel}'). " +
                        $"Pode não ser classificado corretamente.",
                        ambiente.ElementId);
                    continue;
                }

                validos++;
            }

            // Resumo
            _log.Info(ETAPA, COMPONENTE,
                $"Validação de nomes: " +
                $"{validos} válidos, " +
                $"{genericos} genéricos, " +
                $"{vazios} vazios.");

            if (vazios > 0)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    $"{vazios} ambiente(s) sem nome definido. " +
                    $"O classificador não conseguirá identificá-los.");
            }

            return !_log.TemBloqueio;
        }

        /// <summary>
        /// Verifica se o nome é genérico (números, "Room", placeholders).
        /// </summary>
        private static bool IsNomeGenerico(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return true;

            var lower = nome.Trim().ToLowerInvariant();

            // Apenas números
            if (lower.All(c => char.IsDigit(c) || c == ' '))
                return true;

            // Nomes genéricos conhecidos
            var genericos = new HashSet<string>
            {
                "room", "space", "ambiente", "espaco",
                "novo", "novo ambiente", "undefined",
                "a definir", "sem nome", "???", "teste",
                "temp", "temporario", "x", "xx", "xxx",
            };

            return genericos.Contains(lower);
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE COBERTURA MÍNIMA (público)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica presença mínima de ambientes essenciais (banheiro, cozinha).
        /// </summary>
        /// <returns>true se não há bloqueios, false se há erros críticos.</returns>
        public bool ValidateCoberturaMinima(IEnumerable<AmbienteInfo> ambientes)
        {
            if (ambientes == null)
                return true;

            var lista = ambientes.ToList();

            if (lista.Count == 0)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    "Nenhum ambiente fornecido para validação de cobertura.");
                return false;
            }

            _log.Info(ETAPA, COMPONENTE,
                $"Validando cobertura mínima de {lista.Count} ambientes.");

            // ── Verificar banheiro / suíte / lavabo ────────────
            var temBanheiro = lista.Any(a =>
                a.Classificacao.Tipo is TipoAmbiente.Banheiro
                                    or TipoAmbiente.Suite
                                    or TipoAmbiente.Lavabo);

            if (!temBanheiro)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    "Modelo não possui nenhum banheiro, suíte ou lavabo. " +
                    "O pipeline hidráulico requer ao menos 1 ambiente sanitário.");
            }

            // ── Verificar cozinha ──────────────────────────────
            var temCozinha = lista.Any(a =>
                a.Classificacao.Tipo is TipoAmbiente.Cozinha
                                    or TipoAmbiente.CozinhaGourmet);

            if (!temCozinha)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    "Modelo não possui cozinha. " +
                    "Verifique se os Rooms estão nomeados corretamente.");
            }

            // ── Resumo de cobertura ────────────────────────────
            var relevantes = lista.Count(a => a.EhRelevante);
            var total = lista.Count;
            var taxa = total > 0 ? (double)relevantes / total : 0;

            var porTipo = lista
                .Where(a => a.EhRelevante)
                .GroupBy(a => a.Classificacao.Tipo)
                .OrderBy(g => g.Key)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();

            _log.Info(ETAPA, COMPONENTE,
                $"Cobertura: {relevantes}/{total} relevantes ({taxa:P0}). " +
                $"Distribuição: {string.Join(", ", porTipo)}.");

            if (taxa < 0.3)
            {
                _log.Critico(ETAPA, COMPONENTE,
                    $"Cobertura muito baixa: apenas {taxa:P0} dos ambientes são relevantes. " +
                    $"Verifique a nomenclatura dos Rooms.");
            }
            else if (taxa < 0.5)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Cobertura abaixo do ideal: {taxa:P0} dos ambientes são relevantes.");
            }

            return !_log.TemBloqueio;
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO POR TIPO (público)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera resumo consolidado de ambientes por TipoAmbiente.
        /// </summary>
        /// <returns>Resumo textual formatado.</returns>
        public string GenerateResumoPorTipo(IEnumerable<AmbienteInfo> ambientes)
        {
            if (ambientes == null)
                return "Nenhum ambiente fornecido.";

            var lista = ambientes.ToList();

            if (lista.Count == 0)
                return "Nenhum ambiente fornecido.";

            var grupos = lista
                .GroupBy(a => a.Classificacao.Tipo)
                .OrderBy(g => g.Key)
                .ToList();

            var relevantes = lista.Count(a => a.EhRelevante);
            var total = lista.Count;
            var taxa = total > 0 ? (double)relevantes / total : 0;

            var sb = new StringBuilder();
            sb.AppendLine("══════════════════════════════");
            sb.AppendLine("  Resumo de Ambientes por Tipo");
            sb.AppendLine("══════════════════════════════");

            foreach (var grupo in grupos)
            {
                var icon = grupo.Key == TipoAmbiente.NaoIdentificado ? "❓" : "✅";
                sb.AppendLine($"  {icon} {grupo.Key,-20} {grupo.Count(),3}");
            }

            sb.AppendLine("──────────────────────────────");
            sb.AppendLine($"  Total:                {total,5}");
            sb.AppendLine($"  Relevantes:           {relevantes,5} ({taxa:P0})");
            sb.AppendLine($"  Não identificados:    {total - relevantes,5}");
            sb.AppendLine("══════════════════════════════");

            var resumo = sb.ToString();

            _log.Info(ETAPA, COMPONENTE, $"Resumo por tipo:\n{resumo}");

            return resumo;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE EQUIPAMENTOS (público)
        // ══════════════════════════════════════════════════════════

        private const string ETAPA_EQUIP = "03_Equipamentos";

        /// <summary>
        /// Valida equipamentos existentes vs esperados por ambiente.
        /// Detecta faltantes obrigatórios (Critico), opcionais faltantes (Leve) e extras (Leve).
        /// </summary>
        /// <returns>true se não há bloqueios, false se há erros críticos.</returns>
        public bool ValidateEquipamentos(IEnumerable<AmbienteInfo> ambientes)
        {
            if (ambientes == null)
                return true;

            var lista = ambientes.Where(a => a != null && a.EhRelevante).ToList();

            if (lista.Count == 0)
                return true;

            var totalFaltantesObr = 0;
            var totalFaltantesOpc = 0;
            var totalExtras = 0;
            var totalOk = 0;

            _log.Info(ETAPA_EQUIP, COMPONENTE,
                $"Validando equipamentos de {lista.Count} ambientes relevantes.");

            foreach (var ambiente in lista)
            {
                var esperados = EquipamentosPorAmbiente.Get(ambiente.Classificacao.Tipo);

                if (esperados.Count == 0)
                    continue;

                // Normalizar existentes
                var existentesNorm = ambiente.EquipamentosExistentes
                    .Select(e => e.Trim().ToLowerInvariant())
                    .ToHashSet();

                // Normalizar esperados
                var esperadosNorm = esperados
                    .Select(e => (Nome: e.Nome.Trim().ToLowerInvariant(), e.Obrigatorio))
                    .ToList();

                var nomesEsperadosNorm = esperadosNorm
                    .Select(e => e.Nome)
                    .ToHashSet();

                // ── Verificar faltantes ────────────────────────
                foreach (var (nome, obrigatorio) in esperadosNorm)
                {
                    if (!existentesNorm.Contains(nome))
                    {
                        if (obrigatorio)
                        {
                            totalFaltantesObr++;
                            _log.Critico(ETAPA_EQUIP, COMPONENTE,
                                $"Equipamento obrigatório ausente: '{nome}' " +
                                $"no ambiente '{ambiente.NomeOriginal}' " +
                                $"(#{ambiente.Numero}, Nível: '{ambiente.Nivel}').",
                                ambiente.ElementId);
                        }
                        else
                        {
                            totalFaltantesOpc++;
                            _log.Leve(ETAPA_EQUIP, COMPONENTE,
                                $"Equipamento opcional ausente: '{nome}' " +
                                $"no ambiente '{ambiente.NomeOriginal}' " +
                                $"(#{ambiente.Numero}).",
                                ambiente.ElementId);
                        }
                    }
                }

                // ── Verificar extras ───────────────────────────
                foreach (var existente in existentesNorm)
                {
                    if (!nomesEsperadosNorm.Contains(existente))
                    {
                        totalExtras++;
                        _log.Leve(ETAPA_EQUIP, COMPONENTE,
                            $"Equipamento não esperado: '{existente}' " +
                            $"no ambiente '{ambiente.NomeOriginal}' " +
                            $"(#{ambiente.Numero}, Tipo: {ambiente.Classificacao.Tipo}).",
                            ambiente.ElementId);
                    }
                    else
                    {
                        totalOk++;
                    }
                }
            }

            // Resumo
            _log.Info(ETAPA_EQUIP, COMPONENTE,
                $"Validação de equipamentos: " +
                $"{totalOk} OK, " +
                $"{totalFaltantesObr} obrigatórios faltantes, " +
                $"{totalFaltantesOpc} opcionais faltantes, " +
                $"{totalExtras} extras.");

            return !_log.TemBloqueio;
        }
    }
}
