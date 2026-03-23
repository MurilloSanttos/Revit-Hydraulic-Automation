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
        private const string COMPONENTE = "Validador";

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
    }
}
