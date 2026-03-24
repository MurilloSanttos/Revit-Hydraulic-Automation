using System.Text.Json;
using System.Text.Json.Serialization;
using PluginCore.Interfaces;

namespace PluginCore.Pipeline
{
    /// <summary>
    /// Snapshot serializado do estado de uma etapa.
    /// Apenas dados seguros para serialização (sem Actions/Delegates).
    /// </summary>
    public class EtapaSnapshot
    {
        /// <summary>Nome da etapa.</summary>
        public string Nome { get; set; } = string.Empty;

        /// <summary>Status.</summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StatusEtapa Status { get; set; }

        /// <summary>Tentativa atual.</summary>
        public int TentativaAtual { get; set; }

        /// <summary>Início da execução (UTC).</summary>
        public DateTime? InicioExecucao { get; set; }

        /// <summary>Fim da execução (UTC).</summary>
        public DateTime? FimExecucao { get; set; }

        /// <summary>Mensagem de erro.</summary>
        public string? MensagemErro { get; set; }

        /// <summary>Alertas.</summary>
        public List<string> Alertas { get; set; } = new();
    }

    /// <summary>
    /// Snapshot completo do pipeline.
    /// </summary>
    public class PipelineSnapshot
    {
        /// <summary>Versão do formato.</summary>
        public int Versao { get; set; } = 1;

        /// <summary>Quando foi salvo.</summary>
        public DateTime SalvoEm { get; set; }

        /// <summary>ID da execução.</summary>
        public string ExecucaoId { get; set; } = string.Empty;

        /// <summary>Total de etapas.</summary>
        public int TotalEtapas { get; set; }

        /// <summary>Etapas concluídas.</summary>
        public int Concluidas { get; set; }

        /// <summary>Snapshots das etapas.</summary>
        public List<EtapaSnapshot> Etapas { get; set; } = new();

        /// <summary>Dados do contexto (chave-valor serializável).</summary>
        public Dictionary<string, string> ContextoDados { get; set; } = new();
    }

    /// <summary>
    /// Serviço de persistência do estado do pipeline.
    /// Salva/carrega progresso em JSON para retomar execução após interrupção.
    /// </summary>
    public static class PipelinePersistenceService
    {
        private const string ETAPA = "00_Pipeline";
        private const string COMPONENTE = "Persistence";

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        // ══════════════════════════════════════════════════════════
        //  CAMINHO PADRÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna caminho padrão para o arquivo de estado.
        /// %APPDATA%\HidraulicaRevit\PipelineEstado.json
        /// </summary>
        public static string CaminhoDefault()
        {
            var appData = Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);
            var pasta = Path.Combine(appData, "HidraulicaRevit");
            Directory.CreateDirectory(pasta);
            return Path.Combine(pasta, "PipelineEstado.json");
        }

        // ══════════════════════════════════════════════════════════
        //  SALVAR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Salva o estado atual do pipeline em JSON.
        /// </summary>
        public static bool SalvarEstado(List<EtapaPipeline> etapas,
            string? caminho = null, ILogService? log = null)
        {
            caminho ??= CaminhoDefault();

            try
            {
                var snapshot = CriarSnapshot(etapas);
                var json = JsonSerializer.Serialize(snapshot, _jsonOptions);

                // Garantir diretório
                var dir = Path.GetDirectoryName(caminho);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(caminho, json);

                log?.Info(ETAPA, COMPONENTE,
                    $"Estado salvo: {snapshot.Concluidas}/{snapshot.TotalEtapas} " +
                    $"etapas concluídas → {caminho}");

                return true;
            }
            catch (Exception ex)
            {
                log?.Critico(ETAPA, COMPONENTE,
                    $"Falha ao salvar estado: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Salva estado após cada etapa concluída (para auto-save no orquestrador).
        /// </summary>
        public static bool SalvarAposEtapa(PipelineFila fila,
            EtapaPipeline etapaConcluida, ILogService? log = null)
        {
            if (etapaConcluida.Status != StatusEtapa.Concluida &&
                etapaConcluida.Status != StatusEtapa.ConcluidaComAvisos)
                return false;

            return SalvarEstado(fila.Etapas, null, log);
        }

        // ══════════════════════════════════════════════════════════
        //  CARREGAR
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Carrega estado salvo e atualiza as etapas da fila.
        /// Retorna true se havia estado salvo e foi restaurado.
        /// </summary>
        public static bool CarregarEstado(List<EtapaPipeline> etapas,
            string? caminho = null, ILogService? log = null)
        {
            caminho ??= CaminhoDefault();

            try
            {
                if (!File.Exists(caminho))
                {
                    log?.Info(ETAPA, COMPONENTE,
                        "Nenhum estado salvo encontrado. Iniciando do zero.");
                    return false;
                }

                var json = File.ReadAllText(caminho);
                var snapshot = JsonSerializer.Deserialize<PipelineSnapshot>(json, _jsonOptions);

                if (snapshot == null || snapshot.Etapas.Count == 0)
                {
                    log?.Leve(ETAPA, COMPONENTE,
                        "Arquivo de estado vazio ou corrompido.");
                    return false;
                }

                // Restaurar status das etapas
                var restauradas = 0;
                foreach (var etapa in etapas)
                {
                    var salvo = snapshot.Etapas.Find(e => e.Nome == etapa.Nome);
                    if (salvo == null) continue;

                    // Restaurar apenas etapas concluídas
                    if (salvo.Status == StatusEtapa.Concluida ||
                        salvo.Status == StatusEtapa.ConcluidaComAvisos)
                    {
                        etapa.Status = salvo.Status;
                        etapa.TentativaAtual = salvo.TentativaAtual;
                        etapa.InicioExecucao = salvo.InicioExecucao;
                        etapa.FimExecucao = salvo.FimExecucao;
                        etapa.Alertas = salvo.Alertas;
                        restauradas++;
                    }
                    else if (salvo.Status == StatusEtapa.Falha ||
                             salvo.Status == StatusEtapa.Ignorada)
                    {
                        // Resetar etapas que falharam para tentar novamente
                        etapa.Resetar();
                    }
                }

                log?.Info(ETAPA, COMPONENTE,
                    $"Estado restaurado: {restauradas}/{etapas.Count} etapas " +
                    $"já concluídas (salvo em {snapshot.SalvoEm:HH:mm:ss}).");

                return restauradas > 0;
            }
            catch (Exception ex)
            {
                log?.Critico(ETAPA, COMPONENTE,
                    $"Falha ao carregar estado: {ex.Message}");
                return false;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  LIMPAR
        // ══════════════════════════════════════════════════════════

        /// <summary>Limpa o estado salvo (para nova execução).</summary>
        public static bool LimparEstado(string? caminho = null, ILogService? log = null)
        {
            caminho ??= CaminhoDefault();

            try
            {
                if (File.Exists(caminho))
                {
                    File.Delete(caminho);
                    log?.Info(ETAPA, COMPONENTE, "Estado anterior removido.");
                }
                return true;
            }
            catch (Exception ex)
            {
                log?.Leve(ETAPA, COMPONENTE,
                    $"Falha ao limpar estado: {ex.Message}");
                return false;
            }
        }

        /// <summary>Verifica se existe estado salvo.</summary>
        public static bool ExisteEstado(string? caminho = null)
        {
            caminho ??= CaminhoDefault();
            return File.Exists(caminho);
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Cria snapshot a partir das etapas atuais.</summary>
        private static PipelineSnapshot CriarSnapshot(List<EtapaPipeline> etapas)
        {
            return new PipelineSnapshot
            {
                SalvoEm = DateTime.Now,
                ExecucaoId = Guid.NewGuid().ToString("N")[..8],
                TotalEtapas = etapas.Count,
                Concluidas = etapas.Count(e =>
                    e.Status == StatusEtapa.Concluida ||
                    e.Status == StatusEtapa.ConcluidaComAvisos),
                Etapas = etapas.Select(e => new EtapaSnapshot
                {
                    Nome = e.Nome,
                    Status = e.Status,
                    TentativaAtual = e.TentativaAtual,
                    InicioExecucao = e.InicioExecucao,
                    FimExecucao = e.FimExecucao,
                    MensagemErro = e.MensagemErro,
                    Alertas = e.Alertas,
                }).ToList(),
            };
        }

        /// <summary>Gera resumo do estado salvo.</summary>
        public static string GerarResumo(string? caminho = null)
        {
            caminho ??= CaminhoDefault();

            if (!File.Exists(caminho))
                return "Nenhum estado salvo.";

            try
            {
                var json = File.ReadAllText(caminho);
                var snapshot = JsonSerializer.Deserialize<PipelineSnapshot>(json, _jsonOptions);

                if (snapshot == null)
                    return "Estado corrompido.";

                var lines = new List<string>
                {
                    "══ Estado Salvo do Pipeline ══",
                    $"  Salvo em:    {snapshot.SalvoEm:dd/MM/yyyy HH:mm:ss}",
                    $"  Execução:    {snapshot.ExecucaoId}",
                    $"  Progresso:   {snapshot.Concluidas}/{snapshot.TotalEtapas}",
                    "──────────────────────────────",
                };

                foreach (var e in snapshot.Etapas)
                {
                    var icon = e.Status switch
                    {
                        StatusEtapa.Concluida => "✅",
                        StatusEtapa.ConcluidaComAvisos => "⚠️",
                        StatusEtapa.Falha => "❌",
                        StatusEtapa.Pendente => "⏳",
                        _ => "❓",
                    };
                    lines.Add($"  {icon} {e.Nome}");
                }

                lines.Add("══════════════════════════════");

                return string.Join("\n", lines);
            }
            catch
            {
                return "Erro ao ler estado.";
            }
        }

        // Exemplos:
        // // Salvar
        // PipelinePersistenceService.SalvarEstado(fila.Etapas, log: logService);
        //
        // // Carregar (retomar)
        // var retomou = PipelinePersistenceService.CarregarEstado(fila.Etapas, log: logService);
        //
        // // Limpar (nova execução)
        // PipelinePersistenceService.LimparEstado(log: logService);
        //
        // // Resumo
        // Console.WriteLine(PipelinePersistenceService.GerarResumo());
    }
}
