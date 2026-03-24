using PluginCore.Interfaces;
using PluginCore.Models;
using PluginCore.Services;

namespace PluginCore.Pipeline
{
    /// <summary>
    /// Fila de execução do pipeline de dimensionamento hidráulico.
    /// 11 etapas sequenciais com dependências, ações e integração com logs.
    /// </summary>
    public class PipelineFila
    {
        /// <summary>Etapas do pipeline.</summary>
        public List<EtapaPipeline> Etapas { get; private set; } = new();

        /// <summary>Contexto compartilhado entre etapas.</summary>
        public PipelineContext Contexto { get; } = new();

        private readonly ILogService _log;

        private const string ETAPA_LOG = "00_Pipeline";
        private const string COMPONENTE = "PipelineFila";

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTORES
        // ══════════════════════════════════════════════════════════

        public PipelineFila(ILogService log)
        {
            _log = log;
            InicializarEtapas();
        }

        // ══════════════════════════════════════════════════════════
        //  INICIALIZAÇÃO DAS 11 ETAPAS
        // ══════════════════════════════════════════════════════════

        private void InicializarEtapas()
        {
            // ── 01. Carregar Modelo ───────────────────────────
            Etapas.Add(new EtapaPipeline("CarregarModelo", (ctx) =>
            {
                _log.Info("01_Modelo", "CarregarModelo",
                    "Carregando modelo Revit e extraindo elementos MEP.");

                // O Revit2026 preenche SistemaMEP e envia via contexto
                // ctx.Set("sistemaMEP", sistema);
            })
            {
                Descricao = "Carregar modelo Revit e extrair elementos MEP",
                Componente = "RevitModelLoader",
                Ordem = 1,
                Obrigatoria = true,
            });

            // ── 02. Detecção de Ambientes ─────────────────────
            Etapas.Add(new EtapaPipeline("DeteccaoAmbientes", (ctx) =>
            {
                _log.Info("02_Ambientes", "DeteccaoAmbientes",
                    "Detectando e classificando ambientes do modelo.");

                // var sistema = ctx.Get<SistemaMEP>("sistemaMEP");
                // AmbienteService.ClassificarTodos(sistema.Ambientes);
            },
            new List<string> { "CarregarModelo" })
            {
                Descricao = "Detectar e classificar ambientes (cozinha, banheiro, etc.)",
                Componente = "AmbienteService",
                Ordem = 2,
            });

            // ── 03. Validar Ambientes ─────────────────────────
            Etapas.Add(new EtapaPipeline("ValidarAmbientes", (ctx) =>
            {
                _log.Info("02_Ambientes", "ValidarAmbientes",
                    "Validando ambientes: duplicados, áreas e nomes.");

                // AmbienteValidator.ValidarTodos(sistema.Ambientes);
            },
            new List<string> { "DeteccaoAmbientes" })
            {
                Descricao = "Validar duplicados, áreas mínimas e nomenclatura",
                Componente = "AmbienteValidator",
                Ordem = 3,
            });

            // ── 04. Decidir Equipamentos ──────────────────────
            Etapas.Add(new EtapaPipeline("DecidirEquipamentos", (ctx) =>
            {
                _log.Info("03_Equipamentos", "DecidirEquipamentos",
                    "Definindo pontos hidráulicos por ambiente.");

                // EquipamentoService.DistribuirPontos(sistema);
                // ctx.Set("pontos", sistema.Pontos);
                // ctx.Set("trechos", sistema.Trechos);
            },
            new List<string> { "ValidarAmbientes" })
            {
                Descricao = "Distribuir pontos hidráulicos por ambiente",
                Componente = "EquipamentoService",
                Ordem = 4,
            });

            // ── 05. Dimensionar Água Fria ─────────────────────
            Etapas.Add(new EtapaPipeline("DimensionarAguaFria", (ctx) =>
            {
                _log.Info("04_AguaFria", "DimensionarAguaFria",
                    "Iniciando dimensionamento de água fria.");

                var dimensionamento = new DimensionamentoService();
                var perdaCarga = new PerdaCargaService(_log);
                var trechoValidator = new TrechoValidator(_log);
                var pontoValidator = new PontoValidator(_log);
                var prumadaService = new PrumadaService(_log, dimensionamento);

                // var sistema = ctx.Get<SistemaMEP>("sistemaMEP");
                // dimensionamento.Dimensionar(sistema);
                // trechoValidator.ValidarTodos(sistema.Trechos);
                // pontoValidator.ValidarTodos(sistema.Pontos, pressaoReservatorio, perdas);
                // prumadaService.DimensionarTodas(sistema.Prumadas);

                _log.Info("04_AguaFria", "DimensionarAguaFria",
                    "Dimensionamento de água fria concluído.");
            },
            new List<string> { "DecidirEquipamentos" })
            {
                Descricao = "Calcular vazão, diâmetros, velocidade e perda de carga (AF)",
                Componente = "DimensionamentoService",
                Ordem = 5,
                Obrigatoria = true,
            });

            // ── 06. Dimensionar Esgoto ────────────────────────
            Etapas.Add(new EtapaPipeline("DimensionarEsgoto", (ctx) =>
            {
                _log.Info("07_Esgoto", "DimensionarEsgoto",
                    "Iniciando dimensionamento de esgoto.");

                var ramaisDescarga = new RamaisDescargaService(_log);
                var ramaisEsgoto = new RamaisEsgotoService(_log);
                var prumadasEsgoto = new PrumadasEsgotoService(_log);
                var subcoletores = new SubcoletoresService(_log);

                // 1. Ramais de descarga
                // 2. Ramais de esgoto
                // 3. Prumadas de esgoto
                // 4. Subcoletores
                // 5. Coletor predial

                _log.Info("07_Esgoto", "DimensionarEsgoto",
                    "Dimensionamento de esgoto concluído.");
            },
            new List<string> { "DecidirEquipamentos" })
            {
                Descricao = "Dimensionar ramais, prumadas, subcoletores e coletor (ES)",
                Componente = "RamaisEsgotoService",
                Ordem = 6,
                Obrigatoria = true,
            });

            // ── 07. Dimensionar Ventilação ────────────────────
            Etapas.Add(new EtapaPipeline("DimensionarVentilacao", (ctx) =>
            {
                _log.Info("08_Ventilacao", "DimensionarVentilacao",
                    "Iniciando dimensionamento de ventilação.");

                var ventilacao = new VentilacaoService(_log);
                var colunas = new ColunasVentilacaoService(_log);

                // ventilacao.VerificarTodos(sistema.Trechos);
                // colunas.DimensionarTodas(sistema.Prumadas);

                _log.Info("08_Ventilacao", "DimensionarVentilacao",
                    "Dimensionamento de ventilação concluído.");
            },
            new List<string> { "DimensionarEsgoto" })
            {
                Descricao = "Verificar e dimensionar ventilação de esgoto",
                Componente = "VentilacaoService",
                Ordem = 7,
            });

            // ── 08. Gerar Tabelas ─────────────────────────────
            Etapas.Add(new EtapaPipeline("GerarTabelas", (ctx) =>
            {
                _log.Info("09_Tabelas", "GerarTabelas",
                    "Gerando tabelas de dimensionamento.");

                // TabelaService.GerarTabelaAguaFria(sistema);
                // TabelaService.GerarTabelaEsgoto(sistema);
                // TabelaService.GerarTabelaVentilacao(sistema);
            },
            new List<string> { "DimensionarAguaFria", "DimensionarEsgoto", "DimensionarVentilacao" })
            {
                Descricao = "Gerar tabelas de dimensionamento (AF + ES + Vent)",
                Componente = "TabelaService",
                Ordem = 8,
            });

            // ── 09. Gerar Pranchas ────────────────────────────
            Etapas.Add(new EtapaPipeline("GerarPranchas", (ctx) =>
            {
                _log.Info("10_Pranchas", "GerarPranchas",
                    "Gerando pranchas no Revit.");

                // PranchaService.GerarPranchas(sistema);
            },
            new List<string> { "GerarTabelas" })
            {
                Descricao = "Gerar pranchas e folhas no modelo Revit",
                Componente = "PranchaService",
                Ordem = 9,
                Obrigatoria = false,
            });

            // ── 10. Exportar Dados ────────────────────────────
            Etapas.Add(new EtapaPipeline("ExportarDados", (ctx) =>
            {
                _log.Info("11_Exportacao", "ExportarDados",
                    "Exportando dados para JSON e relatórios.");

                // ExportService.ExportarJSON(sistema);
                // ExportService.ExportarRelatorio(sistema);
            },
            new List<string> { "GerarTabelas" })
            {
                Descricao = "Exportar resultados para JSON, CSV e relatórios",
                Componente = "ExportService",
                Ordem = 10,
                Obrigatoria = false,
            });

            // ── 11. Finalizar Pipeline ────────────────────────
            Etapas.Add(new EtapaPipeline("FinalizarPipeline", (ctx) =>
            {
                var duracao = ctx.Duracao;

                _log.Info(ETAPA_LOG, COMPONENTE,
                    $"Pipeline finalizado em {duracao.TotalSeconds:F1}s.");

                var concluidas = ctx.StatusEtapas.Count(s =>
                    s.Value == StatusEtapa.Concluida ||
                    s.Value == StatusEtapa.ConcluidaComAvisos);
                var falhas = ctx.StatusEtapas.Count(s => s.Value == StatusEtapa.Falha);

                _log.Info(ETAPA_LOG, COMPONENTE,
                    $"Resultado: {concluidas} concluídas, {falhas} falhas, " +
                    $"tempo total: {duracao.TotalSeconds:F1}s.");
            },
            new List<string> { "ExportarDados", "GerarPranchas" })
            {
                Descricao = "Finalizar pipeline e gerar relatório de execução",
                Componente = "PipelineFila",
                Ordem = 11,
            });

            _log.Info(ETAPA_LOG, COMPONENTE,
                $"Pipeline inicializado com {Etapas.Count} etapas.");
        }

        // ══════════════════════════════════════════════════════════
        //  CONSULTAS
        // ══════════════════════════════════════════════════════════

        /// <summary>Retorna etapa pelo nome.</summary>
        public EtapaPipeline? ObterEtapa(string nome)
        {
            return Etapas.FirstOrDefault(e => e.Nome == nome);
        }

        /// <summary>Retorna etapas prontas para execução.</summary>
        public List<EtapaPipeline> ObterProntas()
        {
            var statusMap = Etapas.ToDictionary(e => e.Nome, e => e.Status);
            return Etapas.Where(e => e.PodeExecutar(statusMap)).ToList();
        }

        /// <summary>Retorna etapas pendentes.</summary>
        public List<EtapaPipeline> ObterPendentes()
        {
            return Etapas.Where(e =>
                e.Status == StatusEtapa.Pendente ||
                e.Status == StatusEtapa.AguardandoDependencias).ToList();
        }

        /// <summary>Verifica se o pipeline está completo.</summary>
        public bool Completo => Etapas.All(e =>
            e.Status == StatusEtapa.Concluida ||
            e.Status == StatusEtapa.ConcluidaComAvisos ||
            e.Status == StatusEtapa.Ignorada);

        /// <summary>Verifica se há falhas bloqueantes.</summary>
        public bool TemFalha => Etapas.Any(e =>
            e.Status == StatusEtapa.Falha && e.Obrigatoria);

        // ══════════════════════════════════════════════════════════
        //  RESUMO
        // ══════════════════════════════════════════════════════════

        /// <summary>Gera resumo textual do pipeline.</summary>
        public string GerarResumo()
        {
            var lines = new List<string>
            {
                "══ Pipeline de Dimensionamento Hidráulico ══",
                $"  Etapas:     {Etapas.Count}",
                $"  Status:     {(Completo ? "✅ Completo" : TemFalha ? "❌ Falha" : "🔄 Em andamento")}",
                "─────────────────────────────────────────────",
            };

            foreach (var e in Etapas.OrderBy(e => e.Ordem))
            {
                var deps = e.Dependencias.Count > 0
                    ? $" ← [{string.Join(", ", e.Dependencias)}]"
                    : "";
                lines.Add($"  {e}{deps}");
            }

            lines.Add("═════════════════════════════════════════════");

            return string.Join("\n", lines);
        }

        // Exemplos:
        // var fila = new PipelineFila(logService);
        //
        // // Ver etapas prontas
        // var prontas = fila.ObterProntas();
        //
        // // Executar manualmente
        // foreach (var etapa in fila.Etapas)
        // {
        //     etapa.Status = StatusEtapa.EmExecucao;
        //     etapa.AcaoComContexto?.Invoke(fila.Contexto);
        //     etapa.Status = StatusEtapa.Concluida;
        // }
        //
        // Console.WriteLine(fila.GerarResumo());
    }
}
