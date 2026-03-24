using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Mechanical;
using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Resultado da criação automática de Spaces.
    /// </summary>
    public class ResultadoCriacaoSpaces
    {
        /// <summary>Total de Rooms processadas.</summary>
        public int TotalRooms { get; set; }

        /// <summary>Rooms que já tinham Space.</summary>
        public int JaPossuiam { get; set; }

        /// <summary>Spaces criados com sucesso.</summary>
        public int Criados { get; set; }

        /// <summary>Falhas na criação.</summary>
        public int Falhas { get; set; }

        /// <summary>Rooms ignoradas (sem nível ou centro).</summary>
        public int Ignorados { get; set; }

        /// <summary>IDs dos Spaces criados.</summary>
        public List<long> SpaceIdsCriados { get; set; } = new();

        /// <summary>Erros encontrados.</summary>
        public List<string> Erros { get; set; } = new();

        public bool Sucesso => Falhas == 0 && Erros.Count == 0;

        public override string ToString() =>
            $"Spaces: {Criados} criados, {JaPossuiam} existentes, " +
            $"{Ignorados} ignorados, {Falhas} falhas (de {TotalRooms} Rooms)";
    }

    /// <summary>
    /// Serviço responsável pela criação automática de MEP Spaces
    /// para Rooms que não possuem Space correspondente.
    /// Deve ser executado dentro de uma Transaction do Revit.
    /// </summary>
    public class SpaceCreatorService
    {
        private readonly Document _doc;
        private readonly ILogService _log;

        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "SpaceCreator";
        private const double FEET_TO_METERS = 0.3048;

        public SpaceCreatorService(Document doc, ILogService log)
        {
            _doc = doc;
            _log = log;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria Spaces para todas as Rooms sem Space associado.
        /// IMPORTANTE: Deve ser chamado dentro de uma Transaction ativa.
        /// </summary>
        public ResultadoCriacaoSpaces CriarSpacesParaRoomsSemSpace(List<AmbienteInfo> ambientes)
        {
            var resultado = new ResultadoCriacaoSpaces
            {
                TotalRooms = ambientes.Count,
            };

            _log.Info(ETAPA, COMPONENTE,
                $"Iniciando criação de Spaces para {ambientes.Count} ambientes...");

            // Cache de Rooms do modelo para lookup por ElementId
            var roomsCache = BuildRoomsCache();

            foreach (var ambiente in ambientes)
            {
                // Pular se já possui Space
                if (ambiente.SpaceIdCorrespondente > 0)
                {
                    resultado.JaPossuiam++;
                    continue;
                }

                // Pular Spaces (só processar Rooms)
                if (ambiente.TipoElemento != TipoElemento.Room)
                    continue;

                try
                {
                    var sucesso = CriarSpaceParaAmbiente(ambiente, roomsCache, resultado);
                    if (sucesso)
                        resultado.Criados++;
                    else
                        resultado.Ignorados++;
                }
                catch (Exception ex)
                {
                    resultado.Falhas++;
                    resultado.Erros.Add($"{ambiente.NomeOriginal}: {ex.Message}");

                    _log.Critico(ETAPA, COMPONENTE,
                        $"Erro ao criar Space para '{ambiente.NomeOriginal}': {ex.Message}",
                        ambiente.ElementId, ex.StackTrace);
                }
            }

            _log.Info(ETAPA, COMPONENTE, $"Criação concluída: {resultado}");

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  CRIAÇÃO INDIVIDUAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria um Space para um ambiente individual.
        /// Retorna true se criou, false se ignorou.
        /// </summary>
        private bool CriarSpaceParaAmbiente(
            AmbienteInfo ambiente,
            Dictionary<long, Room> roomsCache,
            ResultadoCriacaoSpaces resultado)
        {
            // 1. Encontrar Room no modelo
            if (!roomsCache.TryGetValue(ambiente.ElementId, out var room))
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Room não encontrada no modelo para ElementId {ambiente.ElementId}.",
                    ambiente.ElementId);
                return false;
            }

            // 2. Verificar Location
            if (room.Location == null)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Room '{ambiente.NomeOriginal}' sem Location — não colocada.",
                    ambiente.ElementId);
                return false;
            }

            // 3. Obter Level
            var level = _doc.GetElement(room.LevelId) as Level;
            if (level == null)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Room '{ambiente.NomeOriginal}' sem Level definido.",
                    ambiente.ElementId);
                return false;
            }

            // 4. Obter centro geométrico
            var centro = ObterCentro(room);
            if (centro == null)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Não foi possível obter centro da Room '{ambiente.NomeOriginal}'.",
                    ambiente.ElementId);
                return false;
            }

            // 5. Criar Space
            var uv = new UV(centro.X, centro.Y);
            var space = _doc.Create.NewSpace(level, uv);

            if (space == null)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    $"Revit retornou null ao criar Space para '{ambiente.NomeOriginal}'.",
                    ambiente.ElementId);
                resultado.Falhas++;
                return false;
            }

            // 6. Configurar propriedades
            space.Name = ambiente.NomeOriginal;
            space.Number = ambiente.Numero;

            // 7. Atualizar AmbienteInfo
            ambiente.SpaceIdCorrespondente = space.Id.Value;
            ambiente.SpaceCriadoAutomaticamente = true;

            resultado.SpaceIdsCriados.Add(space.Id.Value);

            _log.Info(ETAPA, COMPONENTE,
                $"✅ Space criado: '{ambiente.NomeOriginal}' (#{ambiente.Numero}) " +
                $"→ SpaceId={space.Id.Value}, Level={level.Name}",
                ambiente.ElementId);

            return true;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Monta cache de Rooms por ElementId para lookup rápido.
        /// </summary>
        private Dictionary<long, Room> BuildRoomsCache()
        {
            var cache = new Dictionary<long, Room>();

            var collector = new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType();

            foreach (var element in collector)
            {
                if (element is Room room)
                    cache[room.Id.Value] = room;
            }

            _log.Info(ETAPA, COMPONENTE,
                $"Cache de Rooms criado: {cache.Count} elementos.");

            return cache;
        }

        /// <summary>
        /// Obtém o centro da Room (LocationPoint ou centroide do Solid).
        /// </summary>
        private XYZ? ObterCentro(Room room)
        {
            // Tentar LocationPoint primeiro
            if (room.Location is LocationPoint locationPoint)
                return locationPoint.Point;

            // Fallback: centroide geométrico
            try
            {
                var options = new Options
                {
                    ComputeReferences = true,
                    IncludeNonVisibleObjects = false,
                    DetailLevel = ViewDetailLevel.Fine,
                };

                var geom = room.get_Geometry(options);
                if (geom != null)
                {
                    foreach (var obj in geom)
                    {
                        if (obj is Solid solid && solid.Volume > 0)
                            return solid.ComputeCentroid();
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Leve(ETAPA, COMPONENTE,
                    $"Fallback geométrico falhou para Room '{room.Name}': {ex.Message}",
                    room.Id.Value);
            }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  EXECUÇÃO COM TRANSACTION
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa criação de Spaces dentro de sua própria Transaction.
        /// Conveniência para uso direto sem gerenciar Transaction externamente.
        /// </summary>
        public ResultadoCriacaoSpaces ExecutarComTransaction(List<AmbienteInfo> ambientes)
        {
            using var trans = new Transaction(_doc, "Criar MEP Spaces ausentes");

            try
            {
                trans.Start();

                var resultado = CriarSpacesParaRoomsSemSpace(ambientes);

                if (resultado.Sucesso || resultado.Criados > 0)
                {
                    trans.Commit();
                    _log.Info(ETAPA, COMPONENTE,
                        $"Transaction committed: {resultado.Criados} Spaces criados.");
                }
                else
                {
                    trans.RollBack();
                    _log.Medio(ETAPA, COMPONENTE,
                        "Transaction rolled back — nenhum Space criado.");
                }

                return resultado;
            }
            catch (Exception ex)
            {
                if (trans.HasStarted())
                    trans.RollBack();

                _log.Critico(ETAPA, COMPONENTE,
                    $"Transaction falhou: {ex.Message}",
                    stackTrace: ex.StackTrace);

                return new ResultadoCriacaoSpaces
                {
                    TotalRooms = ambientes.Count,
                    Falhas = 1,
                    Erros = { ex.Message },
                };
            }
        }

        // Exemplos:
        // var creator = new SpaceCreatorService(doc, logService);
        //
        // // Opção 1: Dentro de Transaction existente
        // using var trans = new Transaction(doc, "Criar Spaces");
        // trans.Start();
        // var resultado = creator.CriarSpacesParaRoomsSemSpace(ambientes);
        // trans.Commit();
        //
        // // Opção 2: Transaction gerenciada
        // var resultado = creator.ExecutarComTransaction(ambientes);
        //
        // Console.WriteLine(resultado);
        // → "Spaces: 5 criados, 8 existentes, 1 ignorados, 0 falhas (de 14 Rooms)"
    }
}
