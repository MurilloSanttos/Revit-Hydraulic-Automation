using PluginCore.Interfaces;

namespace PluginCore.Pipeline
{
    /// <summary>
    /// Resultado da verificação de pré-condições.
    /// </summary>
    public class ResultadoPreCondicao
    {
        /// <summary>Nome da etapa verificada.</summary>
        public string EtapaNome { get; set; } = string.Empty;

        /// <summary>Pode executar.</summary>
        public bool PodeExecutar { get; set; }

        /// <summary>Dependências pendentes.</summary>
        public List<string> Pendentes { get; set; } = new();

        /// <summary>Dependências concluídas.</summary>
        public List<string> Concluidas { get; set; } = new();

        /// <summary>Dependências com falha.</summary>
        public List<string> Falhas { get; set; } = new();

        /// <summary>Dependências não encontradas.</summary>
        public List<string> NaoEncontradas { get; set; } = new();

        /// <summary>Motivo do bloqueio (se houver).</summary>
        public string Motivo { get; set; } = string.Empty;

        public override string ToString()
        {
            if (PodeExecutar)
                return $"✅ '{EtapaNome}': pronta ({Concluidas.Count} deps OK)";

            return $"❌ '{EtapaNome}': bloqueada — {Motivo}";
        }
    }

    /// <summary>
    /// Serviço de verificação de pré-condições do pipeline.
    /// Garante que cada etapa só execute quando suas dependências estiverem concluídas.
    /// </summary>
    public static class PreCondicoesService
    {
        private const string ETAPA = "00_Pipeline";
        private const string COMPONENTE = "PreCondicoes";

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO SIMPLES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se uma etapa pode executar (todas as dependências concluídas).
        /// </summary>
        public static bool Verificar(EtapaPipeline etapa, List<EtapaPipeline> todasEtapas,
            ILogService? log = null)
        {
            if (etapa == null)
                return false;

            if (etapa.Dependencias.Count == 0)
                return true;

            foreach (var dep in etapa.Dependencias)
            {
                var etapaDep = todasEtapas.FirstOrDefault(e => e.Nome == dep);

                // Dependência não encontrada
                if (etapaDep == null)
                {
                    log?.Critico(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}': dependência '{dep}' não encontrada no pipeline.");
                    return false;
                }

                // Dependência falhou
                if (etapaDep.Status == StatusEtapa.Falha)
                {
                    log?.Critico(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}': dependência '{dep}' falhou — " +
                        $"execução bloqueada.");
                    return false;
                }

                // Dependência cancelada
                if (etapaDep.Status == StatusEtapa.Cancelada)
                {
                    log?.Medio(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}': dependência '{dep}' foi cancelada.");
                    return false;
                }

                // Dependência ainda não concluída
                if (etapaDep.Status != StatusEtapa.Concluida &&
                    etapaDep.Status != StatusEtapa.ConcluidaComAvisos)
                {
                    log?.Info(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}': aguardando '{dep}' " +
                        $"(status: {etapaDep.Status}).");
                    return false;
                }
            }

            return true;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO DETALHADA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica pré-condições e retorna resultado detalhado.
        /// </summary>
        public static ResultadoPreCondicao VerificarDetalhado(
            EtapaPipeline etapa, List<EtapaPipeline> todasEtapas, ILogService? log = null)
        {
            var resultado = new ResultadoPreCondicao
            {
                EtapaNome = etapa?.Nome ?? "",
            };

            if (etapa == null)
            {
                resultado.Motivo = "Etapa nula.";
                return resultado;
            }

            // Sem dependências → pode executar
            if (etapa.Dependencias.Count == 0)
            {
                resultado.PodeExecutar = true;
                return resultado;
            }

            // Etapa já executada ou em execução
            if (etapa.Status != StatusEtapa.Pendente &&
                etapa.Status != StatusEtapa.AguardandoDependencias)
            {
                resultado.Motivo = $"Status atual: {etapa.Status}.";
                return resultado;
            }

            foreach (var dep in etapa.Dependencias)
            {
                var etapaDep = todasEtapas.FirstOrDefault(e => e.Nome == dep);

                if (etapaDep == null)
                {
                    resultado.NaoEncontradas.Add(dep);
                    continue;
                }

                switch (etapaDep.Status)
                {
                    case StatusEtapa.Concluida:
                    case StatusEtapa.ConcluidaComAvisos:
                        resultado.Concluidas.Add(dep);
                        break;

                    case StatusEtapa.Falha:
                    case StatusEtapa.Cancelada:
                        resultado.Falhas.Add(dep);
                        break;

                    default:
                        resultado.Pendentes.Add(dep);
                        break;
                }
            }

            // Avaliar resultado
            if (resultado.NaoEncontradas.Count > 0)
            {
                resultado.Motivo = $"Dependências não encontradas: " +
                                   string.Join(", ", resultado.NaoEncontradas);
                log?.Critico(ETAPA, COMPONENTE,
                    $"Etapa '{etapa.Nome}': {resultado.Motivo}");
            }
            else if (resultado.Falhas.Count > 0)
            {
                resultado.Motivo = $"Dependências com falha: " +
                                   string.Join(", ", resultado.Falhas);

                // Se a etapa é obrigatória, é crítico
                if (etapa.Obrigatoria)
                {
                    log?.Critico(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}': {resultado.Motivo}");
                }
                else
                {
                    log?.Medio(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}': {resultado.Motivo} (etapa opcional).");
                }
            }
            else if (resultado.Pendentes.Count > 0)
            {
                resultado.Motivo = $"Aguardando: " +
                                   string.Join(", ", resultado.Pendentes);
            }
            else
            {
                // Todas concluídas
                resultado.PodeExecutar = true;
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica pré-condições de todas as etapas pendentes.
        /// Retorna quais estão prontas para execução.
        /// </summary>
        public static List<ResultadoPreCondicao> VerificarTodas(
            List<EtapaPipeline> etapas, ILogService? log = null)
        {
            return etapas
                .Where(e => e.Status == StatusEtapa.Pendente ||
                            e.Status == StatusEtapa.AguardandoDependencias)
                .Select(e => VerificarDetalhado(e, etapas, log))
                .ToList();
        }

        /// <summary>
        /// Retorna apenas as etapas prontas para execução.
        /// </summary>
        public static List<EtapaPipeline> ObterProntas(
            List<EtapaPipeline> etapas, ILogService? log = null)
        {
            return etapas
                .Where(e => (e.Status == StatusEtapa.Pendente ||
                             e.Status == StatusEtapa.AguardandoDependencias) &&
                            Verificar(e, etapas, log))
                .OrderBy(e => e.Ordem)
                .ToList();
        }

        /// <summary>
        /// Atualiza status de etapas que devem ser ignoradas (dependência falhou).
        /// </summary>
        public static int MarcarIgnoradas(List<EtapaPipeline> etapas, ILogService? log = null)
        {
            var ignoradas = 0;

            foreach (var etapa in etapas)
            {
                if (etapa.Status != StatusEtapa.Pendente &&
                    etapa.Status != StatusEtapa.AguardandoDependencias)
                    continue;

                var temFalha = etapa.Dependencias.Any(dep =>
                {
                    var d = etapas.FirstOrDefault(e => e.Nome == dep);
                    return d != null && (d.Status == StatusEtapa.Falha ||
                                         d.Status == StatusEtapa.Ignorada);
                });

                if (temFalha && etapa.Obrigatoria)
                {
                    etapa.Status = StatusEtapa.Ignorada;
                    etapa.MensagemErro = "Dependência falhou.";
                    ignoradas++;

                    log?.Medio(ETAPA, COMPONENTE,
                        $"Etapa '{etapa.Nome}' ignorada — dependência falhou.");
                }
            }

            return ignoradas;
        }

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual.</summary>
        public static string GerarResumo(List<ResultadoPreCondicao> resultados)
        {
            if (resultados == null || resultados.Count == 0)
                return "Nenhuma etapa verificada.";

            var prontas = resultados.Count(r => r.PodeExecutar);

            var lines = new List<string>
            {
                "══ Pré-Condições do Pipeline ══",
                $"  Verificadas: {resultados.Count}",
                $"  Prontas:     {prontas}",
                $"  Bloqueadas:  {resultados.Count - prontas}",
                "───────────────────────────────",
            };

            foreach (var r in resultados)
            {
                lines.Add($"  {r}");
            }

            lines.Add("═══════════════════════════════");

            return string.Join("\n", lines);
        }

        // Exemplos:
        // // Verificação simples
        // bool ok = PreCondicoesService.Verificar(etapa, fila.Etapas, logService);
        // if (ok) etapa.AcaoComContexto?.Invoke(ctx);
        //
        // // Verificação detalhada
        // var resultado = PreCondicoesService.VerificarDetalhado(etapa, fila.Etapas);
        // resultado.PodeExecutar  → true/false
        // resultado.Pendentes     → ["DimensionarEsgoto"]
        // resultado.Falhas        → []
        //
        // // Obter prontas
        // var prontas = PreCondicoesService.ObterProntas(fila.Etapas);
        //
        // // Marcar ignoradas (cascade de falhas)
        // PreCondicoesService.MarcarIgnoradas(fila.Etapas, logService);
    }
}
