using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço responsável pela transferência de classificação Room → Space.
    /// Garante coerência semântica: cada Space recebe o TipoAmbiente,
    /// confiança e metadados derivados do Room correspondente.
    ///
    /// Grava parâmetros DNX_ nos Spaces para uso no pipeline MEP.
    /// </summary>
    public class SpaceClassificationTransferService
    {
        private readonly ILogService _log;

        private const string ETAPA = "02_Classificacao";
        private const string COMPONENTE = "TransferService";

        // Nomes dos parâmetros compartilhados (Shared Parameters)
        private const string PARAM_TIPO = "DNX_TipoAmbiente";
        private const string PARAM_GRUPO = "DNX_GrupoAmbiente";
        private const string PARAM_CONFIANCA = "DNX_ConfiancaClassificacao";
        private const string PARAM_NOME_NORM = "DNX_NomeNormalizado";
        private const string PARAM_FONTE = "DNX_ClassificacaoFonte";

        public SpaceClassificationTransferService(ILogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  TRANSFERÊNCIA PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Transfere a classificação de cada Room para seu Space correspondente.
        /// Grava parâmetros DNX_ nos Spaces via transação única.
        /// </summary>
        /// <param name="doc">Documento Revit ativo.</param>
        /// <param name="rooms">Lista de AmbienteInfo (Rooms classificados).</param>
        /// <param name="mapaRoomSpace">Mapa RoomId → SpaceId.</param>
        /// <param name="logService">Serviço de log (opcional, usa o do construtor).</param>
        public ResultadoTransferencia TransferirClassificacaoRoomParaSpace(
            Document doc,
            List<AmbienteInfo> rooms,
            Dictionary<long, long> mapaRoomSpace,
            ILogService? logService = null)
        {
            var log = logService ?? _log;

            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (mapaRoomSpace == null) throw new ArgumentNullException(nameof(mapaRoomSpace));

            log.Info(ETAPA, COMPONENTE,
                $"Iniciando transferência de classificação: " +
                $"{rooms.Count} Rooms, {mapaRoomSpace.Count} correspondências.");

            var resultado = new ResultadoTransferencia { TotalRooms = rooms.Count };

            // Filtrar Rooms com classificação válida
            var roomsClassificados = rooms
                .Where(r => r.Classificacao.Tipo != TipoAmbiente.NaoIdentificado)
                .ToList();

            log.Info(ETAPA, COMPONENTE,
                $"{roomsClassificados.Count} Rooms com classificação válida.");

            using var trans = new Transaction(doc, "Transferir Classificação Room→Space");

            try
            {
                trans.Start();

                foreach (var room in roomsClassificados)
                {
                    // Verificar se Room tem Space correspondente
                    if (!mapaRoomSpace.TryGetValue(room.ElementId, out var spaceId))
                    {
                        resultado.SemSpace++;
                        log.Medio(ETAPA, COMPONENTE,
                            $"Room {room.ElementId} ('{room.NomeOriginal}') " +
                            $"não possui Space correspondente para transferência.",
                            room.ElementId);
                        continue;
                    }

                    // Obter o elemento Space do documento
                    var spaceElement = doc.GetElement(new ElementId(spaceId));
                    if (spaceElement is not Space space)
                    {
                        resultado.Falhas++;
                        log.Critico(ETAPA, COMPONENTE,
                            $"SpaceId={spaceId} não é um Space válido no documento. " +
                            $"(Room '{room.NomeOriginal}', Id={room.ElementId})",
                            room.ElementId);
                        continue;
                    }

                    // Transferir classificação
                    try
                    {
                        var sucesso = TransferirParaSpace(space, room, log);
                        if (sucesso)
                        {
                            resultado.Transferidos++;
                            log.Info(ETAPA, COMPONENTE,
                                $"Classificação transferida Room→Space | " +
                                $"RoomId={room.ElementId} | SpaceId={spaceId} | " +
                                $"Tipo={room.Classificacao.Tipo} | " +
                                $"Confiança={room.Classificacao.Confianca:P0}",
                                room.ElementId);
                        }
                        else
                        {
                            resultado.SemParametros++;
                            log.Medio(ETAPA, COMPONENTE,
                                $"Space {spaceId} não possui parâmetros DNX. " +
                                $"Classificação transferida via Name apenas.",
                                spaceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        resultado.Falhas++;
                        resultado.Erros.Add(
                            $"Room={room.ElementId} → Space={spaceId}: {ex.Message}");
                        log.Critico(ETAPA, COMPONENTE,
                            $"Falha ao transferir classificação para Space {spaceId}: {ex.Message}",
                            spaceId, ex.StackTrace);
                    }
                }

                // Commit ou Rollback
                if (resultado.Transferidos > 0 || resultado.SemParametros > 0)
                {
                    trans.Commit();
                    log.Info(ETAPA, COMPONENTE,
                        $"Transaction committed: {resultado.Transferidos} transferências.");
                }
                else
                {
                    trans.RollBack();
                    log.Medio(ETAPA, COMPONENTE,
                        "Transaction rolled back — nenhuma transferência realizada.");
                }
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                log.Critico(ETAPA, COMPONENTE,
                    $"Transaction de transferência falhou: {ex.Message}",
                    detalhes: ex.StackTrace);

                resultado.Falhas++;
                resultado.Erros.Add($"Transaction: {ex.Message}");
            }

            // Resumo
            log.Info(ETAPA, COMPONENTE,
                $"Transferência concluída: {resultado}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  TRANSFERÊNCIA INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Transfere classificação de um Room para seu Space.
        /// Tenta gravar parâmetros DNX_. Se não existirem, grava via Name.
        /// Retorna true se parâmetros DNX_ foram gravados, false se fallback.
        /// </summary>
        private bool TransferirParaSpace(Space space, AmbienteInfo room, ILogService log)
        {
            var classificacao = room.Classificacao;
            var grupo = ObterGrupo(classificacao.Tipo);
            var nomeNorm = NormalizarNome(room.NomeOriginal);

            // Tentar gravar parâmetros DNX_
            bool temParametros = true;

            temParametros &= SetParameterString(space, PARAM_TIPO,
                classificacao.Tipo.ToString());
            temParametros &= SetParameterString(space, PARAM_GRUPO, grupo);
            temParametros &= SetParameterDouble(space, PARAM_CONFIANCA,
                classificacao.Confianca);
            temParametros &= SetParameterString(space, PARAM_NOME_NORM, nomeNorm);
            temParametros &= SetParameterString(space, PARAM_FONTE, "Room");

            // Fallback: gravar no Name do Space (sempre funciona)
            try
            {
                // Manter o nome original, adicionar prefixo de classificação
                var prefixo = $"[{classificacao.Tipo}] ";
                if (!space.Name.StartsWith("["))
                {
                    space.Name = $"{prefixo}{room.NomeOriginal}";
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao atualizar Name do Space: {ex.Message}",
                    space.Id.Value);
            }

            // Gravar Comments como backup
            try
            {
                var commentsParam = space.get_Parameter(BuiltInParameter.ALL_MODEL_INSTANCE_COMMENTS);
                if (commentsParam != null && !commentsParam.IsReadOnly)
                {
                    commentsParam.Set(
                        $"Tipo={classificacao.Tipo} | Confiança={classificacao.Confianca:P0} | " +
                        $"Grupo={grupo} | Fonte=Room | Padrão={classificacao.PadraoUtilizado}");
                }
            }
            catch { /* Comments não é crítico */ }

            return temParametros;
        }

        // ══════════════════════════════════════════════════════════
        //  SET PARAMETERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Define um parâmetro String em um elemento.
        /// Retorna true se o parâmetro existe e foi gravado.
        /// </summary>
        private static bool SetParameterString(Element element, string nome, string valor)
        {
            try
            {
                var param = element.LookupParameter(nome);
                if (param != null && !param.IsReadOnly &&
                    param.StorageType == StorageType.String)
                {
                    param.Set(valor);
                    return true;
                }
            }
            catch { /* parâmetro não disponível */ }

            return false;
        }

        /// <summary>
        /// Define um parâmetro Double em um elemento.
        /// Retorna true se o parâmetro existe e foi gravado.
        /// </summary>
        private static bool SetParameterDouble(Element element, string nome, double valor)
        {
            try
            {
                var param = element.LookupParameter(nome);
                if (param != null && !param.IsReadOnly &&
                    param.StorageType == StorageType.Double)
                {
                    param.Set(valor);
                    return true;
                }

                // Tentar como String se o parâmetro for String
                if (param != null && !param.IsReadOnly &&
                    param.StorageType == StorageType.String)
                {
                    param.Set(valor.ToString("F4"));
                    return true;
                }
            }
            catch { /* parâmetro não disponível */ }

            return false;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém o grupo funcional do tipo de ambiente.
        /// </summary>
        private static string ObterGrupo(TipoAmbiente tipo)
        {
            return tipo switch
            {
                TipoAmbiente.Banheiro => "Sanitário",
                TipoAmbiente.Lavabo => "Sanitário",
                TipoAmbiente.Suite => "Sanitário",
                TipoAmbiente.Cozinha => "Alimentação",
                TipoAmbiente.CozinhaGourmet => "Alimentação",
                TipoAmbiente.Lavanderia => "Serviço",
                TipoAmbiente.AreaDeServico => "Serviço",
                TipoAmbiente.AreaExterna => "Externo",
                _ => "NaoClassificado"
            };
        }

        /// <summary>
        /// Normaliza o nome do ambiente para padronização.
        /// </summary>
        private static string NormalizarNome(string nome)
        {
            if (string.IsNullOrWhiteSpace(nome))
                return string.Empty;

            return nome.Trim().ToLowerInvariant()
                .Replace("á", "a").Replace("à", "a").Replace("ã", "a").Replace("â", "a")
                .Replace("é", "e").Replace("ê", "e")
                .Replace("í", "i")
                .Replace("ó", "o").Replace("ô", "o").Replace("õ", "o")
                .Replace("ú", "u").Replace("ü", "u")
                .Replace("ç", "c");
        }
    }

    /// <summary>
    /// Resultado da transferência de classificação Room → Space.
    /// </summary>
    public class ResultadoTransferencia
    {
        /// <summary>Total de Rooms processados.</summary>
        public int TotalRooms { get; set; }

        /// <summary>Transferências bem-sucedidas (com parâmetros DNX).</summary>
        public int Transferidos { get; set; }

        /// <summary>Rooms sem Space correspondente.</summary>
        public int SemSpace { get; set; }

        /// <summary>Spaces sem parâmetros DNX (fallback via Name).</summary>
        public int SemParametros { get; set; }

        /// <summary>Falhas na transferência.</summary>
        public int Falhas { get; set; }

        /// <summary>Detalhes dos erros.</summary>
        public List<string> Erros { get; set; } = new();

        public bool Sucesso => Falhas == 0;

        public override string ToString() =>
            $"{Transferidos} transferidos, {SemParametros} via fallback, " +
            $"{SemSpace} sem Space, {Falhas} falhas (de {TotalRooms} Rooms)";
    }
}
