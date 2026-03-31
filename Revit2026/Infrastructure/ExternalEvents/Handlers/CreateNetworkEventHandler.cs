using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using PluginCore.Interfaces;
using Revit2026.Services.Sistemas;

namespace Revit2026.Infrastructure.ExternalEvents.Handlers
{
    /// <summary>
    /// Tipos de rede MEP suportados.
    /// </summary>
    public enum TipoRede
    {
        AguaFria,
        AguaQuente,
        EsgotoSanitario,
        EsgotoPrimario,
        EsgotoSecundario,
        Ventilacao,
        VentilacaoPrimaria,
        VentilacaoSecundaria,
        ColunaVentilacao
    }

    /// <summary>
    /// Parâmetros para criação de uma rede MEP.
    /// </summary>
    public class CriarRedeParametros
    {
        public TipoRede Tipo { get; set; }
        public IList<ElementId> Elementos { get; set; } = new List<ElementId>();
        public string? NomeCustomizado { get; set; }
        public bool ValidarConectividade { get; set; } = true;
        public bool AplicarNomenclatura { get; set; } = true;
    }

    /// <summary>
    /// Resultado da criação de uma rede MEP.
    /// </summary>
    public class ResultadoCriacaoRede
    {
        public bool Sucesso { get; set; }
        public TipoRede Tipo { get; set; }
        public ElementId? SistemaId { get; set; }
        public int ElementosAdicionados { get; set; }
        public ConnectivityReport? Conectividade { get; set; }
        public string Mensagem { get; set; } = "";

        public override string ToString() =>
            $"{Tipo}: {(Sucesso ? "OK" : "FALHA")} | " +
            $"{ElementosAdicionados} elementos" +
            (SistemaId != null ? $" | SysId={SistemaId.Value}" : "");
    }

    /// <summary>
    /// ExternalEventHandler para criação automática de redes MEP (PipingSystem).
    /// Orquestra PipingSystemCreator, WasteSystemCreator, VentSystemCreator,
    /// SystemNamingService e SystemConnectivityValidator.
    ///
    /// Todos os serviços são instanciados no thread do Revit.
    /// Ações são enfileiradas via BaseExternalEventHandler.
    /// </summary>
    public class CreateNetworkEventHandler : BaseExternalEventHandler
    {
        private readonly ILogService _log;

        private const string ETAPA = "06_Sistemas";
        private const string COMPONENTE = "CreateNetworkHandler";

        // ══════════════════════════════════════════════════════════
        //  CONSTRUTOR
        // ══════════════════════════════════════════════════════════

        public CreateNetworkEventHandler(ILogService logService)
            : base(logService)
        {
            _log = logService ?? throw new ArgumentNullException(nameof(logService));
        }

        // ══════════════════════════════════════════════════════════
        //  NOME DO HANDLER
        // ══════════════════════════════════════════════════════════

        public override string GetName() => "CreateNetworkEventHandler";

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Enfileira a criação de uma rede MEP individual.
        /// </summary>
        public void CriarRede(
            CriarRedeParametros parametros,
            Action<ResultadoCriacaoRede>? callback = null)
        {
            if (parametros == null)
                throw new ArgumentNullException(nameof(parametros));

            EnfileirarAcao(app =>
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        "Nenhum documento ativo no Revit.");
                    callback?.Invoke(new ResultadoCriacaoRede
                    {
                        Tipo = parametros.Tipo,
                        Mensagem = "Documento não disponível."
                    });
                    return;
                }

                var resultado = ExecutarCriacaoRede(doc, parametros);
                callback?.Invoke(resultado);
            });
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado da criação em lote.
        /// </summary>
        public class ResultadoCriacaoLote
        {
            public int Total { get; set; }
            public int Sucesso { get; set; }
            public int Falhas { get; set; }
            public List<ResultadoCriacaoRede> Resultados { get; set; } = new();

            public override string ToString() =>
                $"{Sucesso}/{Total} redes criadas, {Falhas} falhas";
        }

        /// <summary>
        /// Enfileira a criação de múltiplas redes MEP.
        /// </summary>
        public void CriarRedesEmLote(
            List<CriarRedeParametros> listaParametros,
            Action<ResultadoCriacaoLote>? callback = null)
        {
            EnfileirarAcao(app =>
            {
                var doc = app.ActiveUIDocument?.Document;
                if (doc == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        "Nenhum documento ativo no Revit.");
                    callback?.Invoke(null);
                    return;
                }

                var resultado = new ResultadoCriacaoLote
                {
                    Total = listaParametros.Count
                };

                _log.Info(ETAPA, COMPONENTE,
                    $"Criação em lote: {listaParametros.Count} redes...");

                foreach (var parametros in listaParametros)
                {
                    var res = ExecutarCriacaoRede(doc, parametros);
                    resultado.Resultados.Add(res);

                    if (res.Sucesso)
                        resultado.Sucesso++;
                    else
                        resultado.Falhas++;
                }

                // Log resumo
                _log.Info(ETAPA, COMPONENTE,
                    $"═══ Resumo Criação de Redes ═══\n" +
                    $"  Total solicitados: {resultado.Total}\n" +
                    $"  Criados:           {resultado.Sucesso}\n" +
                    $"  Falhas:            {resultado.Falhas}");

                callback?.Invoke(resultado);
            });
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO RÁPIDA POR TIPO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Atalho: Cria rede de Água Fria.
        /// </summary>
        public void CriarRedeAguaFria(
            IList<ElementId> elementos,
            Action<ResultadoCriacaoRede>? callback = null)
        {
            CriarRede(new CriarRedeParametros
            {
                Tipo = TipoRede.AguaFria,
                Elementos = elementos
            }, callback);
        }

        /// <summary>
        /// Atalho: Cria rede de Esgoto Sanitário.
        /// </summary>
        public void CriarRedeEsgoto(
            IList<ElementId> elementos,
            Action<ResultadoCriacaoRede>? callback = null)
        {
            CriarRede(new CriarRedeParametros
            {
                Tipo = TipoRede.EsgotoSanitario,
                Elementos = elementos
            }, callback);
        }

        /// <summary>
        /// Atalho: Cria rede de Ventilação.
        /// </summary>
        public void CriarRedeVentilacao(
            IList<ElementId> elementos,
            Action<ResultadoCriacaoRede>? callback = null)
        {
            CriarRede(new CriarRedeParametros
            {
                Tipo = TipoRede.Ventilacao,
                Elementos = elementos
            }, callback);
        }

        // ══════════════════════════════════════════════════════════
        //  LÓGICA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa a criação de uma rede no thread do Revit.
        /// </summary>
        private ResultadoCriacaoRede ExecutarCriacaoRede(
            Document doc,
            CriarRedeParametros parametros)
        {
            var resultado = new ResultadoCriacaoRede
            {
                Tipo = parametros.Tipo
            };

            _log.Info(ETAPA, COMPONENTE,
                $"Criando rede {parametros.Tipo} com " +
                $"{parametros.Elementos.Count} elementos...");

            // Validar elementos
            if (parametros.Elementos.Count == 0)
            {
                resultado.Mensagem = "Lista de elementos vazia.";
                _log.Critico(ETAPA, COMPONENTE,
                    $"Falha ao criar rede {parametros.Tipo}: " +
                    $"lista de elementos vazia.");
                return resultado;
            }

            using var trans = new Transaction(doc,
                $"Criar Rede {parametros.Tipo}");

            try
            {
                trans.Start();

                // ── 1. Criar PipingSystem ─────────────────────
                PipingSystem? sistema = CriarSistemaPorTipo(
                    doc, parametros.Tipo, parametros.Elementos);

                if (sistema == null)
                {
                    trans.RollBack();
                    resultado.Mensagem =
                        $"Falha ao criar PipingSystem ({parametros.Tipo}).";
                    _log.Critico(ETAPA, COMPONENTE, resultado.Mensagem);
                    return resultado;
                }

                resultado.SistemaId = sistema.Id;
                resultado.ElementosAdicionados = parametros.Elementos.Count;

                // ── 2. Nomenclatura ───────────────────────────
                if (parametros.AplicarNomenclatura)
                {
                    try
                    {
                        var naming = new SystemNamingService();
                        naming.AplicarNomenclaturaPadrao(doc,
                            new List<MEPSystem> { sistema }, _log);
                    }
                    catch (Exception ex)
                    {
                        _log.Leve(ETAPA, COMPONENTE,
                            $"Nomenclatura não aplicada: {ex.Message}");
                    }
                }

                trans.Commit();

                // ── 3. Validar conectividade (pós-commit) ─────
                if (parametros.ValidarConectividade)
                {
                    try
                    {
                        var validator = new SystemConnectivityValidator();
                        resultado.Conectividade =
                            validator.ValidarConectividade(doc, sistema, _log);
                    }
                    catch (Exception ex)
                    {
                        _log.Leve(ETAPA, COMPONENTE,
                            $"Validação de conectividade falhou: {ex.Message}");
                    }
                }

                // ── 4. Resultado ──────────────────────────────
                resultado.Sucesso = true;
                resultado.Mensagem =
                    $"Rede {parametros.Tipo} criada: " +
                    $"Id={sistema.Id.Value}, " +
                    $"{parametros.Elementos.Count} elementos.";

                _log.Info(ETAPA, COMPONENTE, resultado.Mensagem,
                    sistema.Id.Value);

                return resultado;
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                resultado.Mensagem =
                    $"Falha ao criar rede {parametros.Tipo}: {ex.Message}";
                _log.Critico(ETAPA, COMPONENTE, resultado.Mensagem,
                    detalhes: ex.StackTrace);
                return resultado;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  DISPATCH POR TIPO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Delega a criação ao serviço correto baseado no tipo de rede.
        /// </summary>
        private PipingSystem? CriarSistemaPorTipo(
            Document doc,
            TipoRede tipo,
            IList<ElementId> elementos)
        {
            switch (tipo)
            {
                // ── Água ──────────────────────────────────────
                case TipoRede.AguaFria:
                {
                    var creator = new PipingSystemCreator();
                    return creator.CriarSistemaAguaFria(doc, elementos, _log);
                }
                case TipoRede.AguaQuente:
                {
                    var creator = new PipingSystemCreator();
                    return creator.CriarSistemaAguaQuente(doc, elementos, _log);
                }

                // ── Esgoto ────────────────────────────────────
                case TipoRede.EsgotoSanitario:
                {
                    var creator = new WasteSystemCreator();
                    return creator.CriarSistemaEsgotoSanitario(doc, elementos, _log);
                }
                case TipoRede.EsgotoPrimario:
                {
                    var creator = new WasteSystemCreator();
                    return creator.CriarSistemaEsgotoPrimario(doc, elementos, _log);
                }
                case TipoRede.EsgotoSecundario:
                {
                    var creator = new WasteSystemCreator();
                    return creator.CriarSistemaEsgotoSecundario(doc, elementos, _log);
                }

                // ── Ventilação ────────────────────────────────
                case TipoRede.Ventilacao:
                {
                    var creator = new VentSystemCreator();
                    return creator.CriarSistemaDeVentilacaoSanitaria(
                        doc, elementos, _log);
                }
                case TipoRede.VentilacaoPrimaria:
                {
                    var creator = new VentSystemCreator();
                    return creator.CriarVentilacaoPrimaria(doc, elementos, _log);
                }
                case TipoRede.VentilacaoSecundaria:
                {
                    var creator = new VentSystemCreator();
                    return creator.CriarVentilacaoSecundaria(doc, elementos, _log);
                }
                case TipoRede.ColunaVentilacao:
                {
                    var creator = new VentSystemCreator();
                    return creator.CriarColunaVentilacao(doc, elementos, _log);
                }

                default:
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Tipo de rede não suportado: {tipo}");
                    return null;
            }
        }
    }
}
