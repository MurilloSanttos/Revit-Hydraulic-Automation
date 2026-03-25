using PluginCore.Interfaces;
using PluginCore.Models;

namespace Revit2026.Services
{
    /// <summary>
    /// Serviço de correspondência espacial Room ↔ Space.
    /// Utiliza proximidade geométrica dos pontos centrais e matching
    /// por nível para criar um mapa 1:1 (RoomId → SpaceId).
    ///
    /// Garante que cada Space seja atribuído a no máximo um Room,
    /// priorizando menor distância e mesmo nível.
    /// </summary>
    public class RoomSpaceMatcherService
    {
        private readonly ILogService _log;

        private const string ETAPA = "01_Ambientes";
        private const string COMPONENTE = "RoomSpaceMatcher";

        public RoomSpaceMatcherService(ILogService log)
        {
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        // ══════════════════════════════════════════════════════════
        //  MAPEAMENTO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria mapa de correspondência RoomId → SpaceId baseado em
        /// proximidade geométrica e prioridade por nível.
        /// Garante correspondência 1:1 (cada Space atribuído a no máximo 1 Room).
        /// </summary>
        /// <param name="rooms">Lista de ambientes tipo Room.</param>
        /// <param name="spaces">Lista de ambientes tipo Space.</param>
        /// <param name="toleranciaMetros">Distância máxima para considerar match (default 1.5m).</param>
        /// <returns>Dicionário RoomId → SpaceId. Rooms sem match não aparecem.</returns>
        public Dictionary<long, long> MapearRoomsParaSpaces(
            List<AmbienteInfo> rooms,
            List<AmbienteInfo> spaces,
            double toleranciaMetros = 1.5)
        {
            if (rooms == null) throw new ArgumentNullException(nameof(rooms));
            if (spaces == null) throw new ArgumentNullException(nameof(spaces));

            _log.Info(ETAPA, COMPONENTE,
                $"Iniciando correspondência: {rooms.Count} Rooms × {spaces.Count} Spaces " +
                $"(tolerância={toleranciaMetros:F2} m)...");

            if (spaces.Count == 0)
            {
                _log.Medio(ETAPA, COMPONENTE,
                    "Nenhum Space disponível para correspondência.");
                return new Dictionary<long, long>();
            }

            // ── 1. Calcular todas as distâncias candidatas ────
            var candidatos = CalcularCandidatos(rooms, spaces, toleranciaMetros);

            _log.Info(ETAPA, COMPONENTE,
                $"{candidatos.Count} pares candidatos encontrados dentro da tolerância.");

            // ── 2. Resolver correspondência 1:1 (guloso) ──────
            var mapa = ResolverCorrespondencia(candidatos, rooms, spaces);

            // ── 3. Atualizar AmbienteInfo dos Rooms ───────────
            AtualizarRooms(mapa, rooms);

            // ── 4. Resumo ─────────────────────────────────────
            var semMatch = rooms.Count - mapa.Count;
            _log.Info(ETAPA, COMPONENTE,
                $"Correspondência concluída: {mapa.Count} pares criados, " +
                $"{semMatch} Rooms sem Space.");

            return mapa;
        }

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO DE CANDIDATOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula todos os pares (Room, Space) com distância dentro da tolerância.
        /// Cada par inclui: RoomId, SpaceId, distância e se estão no mesmo nível.
        /// </summary>
        private List<Candidato> CalcularCandidatos(
            List<AmbienteInfo> rooms,
            List<AmbienteInfo> spaces,
            double tolerancia)
        {
            var candidatos = new List<Candidato>();

            foreach (var room in rooms)
            {
                if (room.PontoCentral == null)
                {
                    _log.Critico(ETAPA, COMPONENTE,
                        $"Room '{room.NomeOriginal}' (Id={room.ElementId}) " +
                        $"sem PontoCentral. Impossível calcular correspondência.",
                        room.ElementId);
                    continue;
                }

                foreach (var space in spaces)
                {
                    if (space.PontoCentral == null)
                        continue;

                    var dist = Distancia(room.PontoCentral, space.PontoCentral);

                    if (dist <= tolerancia)
                    {
                        candidatos.Add(new Candidato
                        {
                            RoomId = room.ElementId,
                            SpaceId = space.ElementId,
                            RoomNome = room.NomeOriginal,
                            SpaceNome = space.NomeOriginal,
                            Distancia = dist,
                            MesmoNivel = string.Equals(
                                room.Nivel, space.Nivel,
                                StringComparison.OrdinalIgnoreCase)
                        });
                    }
                }
            }

            return candidatos;
        }

        // ══════════════════════════════════════════════════════════
        //  RESOLUÇÃO DE CORRESPONDÊNCIA 1:1
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resolve correspondência 1:1 usando algoritmo guloso:
        /// 1. Ordena candidatos por prioridade (mesmo nível primeiro, depois menor distância)
        /// 2. Atribui cada par garantindo que Room e Space sejam usados no máximo uma vez
        /// 3. Loga Rooms sem correspondência
        /// </summary>
        private Dictionary<long, long> ResolverCorrespondencia(
            List<Candidato> candidatos,
            List<AmbienteInfo> rooms,
            List<AmbienteInfo> spaces)
        {
            var mapa = new Dictionary<long, long>();
            var roomsUsados = new HashSet<long>();
            var spacesUsados = new HashSet<long>();

            // Ordenar: mesmo nível primeiro, depois menor distância
            var ordenados = candidatos
                .OrderByDescending(c => c.MesmoNivel)
                .ThenBy(c => c.Distancia)
                .ToList();

            foreach (var candidato in ordenados)
            {
                // Pular se Room ou Space já foram atribuídos
                if (roomsUsados.Contains(candidato.RoomId))
                    continue;
                if (spacesUsados.Contains(candidato.SpaceId))
                    continue;

                // Atribuir correspondência
                mapa[candidato.RoomId] = candidato.SpaceId;
                roomsUsados.Add(candidato.RoomId);
                spacesUsados.Add(candidato.SpaceId);

                _log.Info(ETAPA, COMPONENTE,
                    $"Match: Room '{candidato.RoomNome}' (Id={candidato.RoomId}) " +
                    $"↔ Space '{candidato.SpaceNome}' (Id={candidato.SpaceId}) " +
                    $"dist={candidato.Distancia:F3} m" +
                    $"{(candidato.MesmoNivel ? " [mesmo nível]" : " [nível diferente]")}");
            }

            // Logar Rooms sem correspondência
            foreach (var room in rooms)
            {
                if (!roomsUsados.Contains(room.ElementId))
                {
                    _log.Medio(ETAPA, COMPONENTE,
                        $"Room sem Space correspondente: '{room.NomeOriginal}' " +
                        $"(Id={room.ElementId}, Nível='{room.Nivel}')",
                        room.ElementId);
                }
            }

            // Logar Spaces órfãos
            var spacesOrfaos = spaces
                .Where(s => !spacesUsados.Contains(s.ElementId))
                .ToList();

            if (spacesOrfaos.Count > 0)
            {
                var nomes = string.Join(", ",
                    spacesOrfaos.Select(s => $"'{s.NomeOriginal}'"));
                _log.Leve(ETAPA, COMPONENTE,
                    $"{spacesOrfaos.Count} Spaces órfãos (sem Room): {nomes}");
            }

            return mapa;
        }

        // ══════════════════════════════════════════════════════════
        //  ATUALIZAÇÃO DE AMBIENTES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Atualiza AmbienteInfo.SpaceIdCorrespondente dos Rooms com match.
        /// </summary>
        private static void AtualizarRooms(
            Dictionary<long, long> mapa, List<AmbienteInfo> rooms)
        {
            foreach (var room in rooms)
            {
                if (mapa.TryGetValue(room.ElementId, out var spaceId))
                {
                    room.SpaceIdCorrespondente = spaceId;
                }
            }
        }

        // ══════════════════════════════════════════════════════════
        //  ANÁLISE DE QUALIDADE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera relatório de qualidade da correspondência.
        /// Retorna métricas úteis para diagnóstico.
        /// </summary>
        public RelatorioCorrespondencia GerarRelatorio(
            Dictionary<long, long> mapa,
            List<AmbienteInfo> rooms,
            List<AmbienteInfo> spaces)
        {
            var relatorio = new RelatorioCorrespondencia
            {
                TotalRooms = rooms.Count,
                TotalSpaces = spaces.Count,
                TotalMatches = mapa.Count,
                RoomsSemMatch = rooms.Count - mapa.Count,
                SpacesOrfaos = spaces.Count - mapa.Count,
                PercentualCobertura = rooms.Count > 0
                    ? (double)mapa.Count / rooms.Count * 100.0
                    : 0
            };

            // Calcular distância média dos matches
            var spacesDict = spaces.ToDictionary(s => s.ElementId);
            var distancias = new List<double>();

            foreach (var room in rooms)
            {
                if (mapa.TryGetValue(room.ElementId, out var spaceId) &&
                    spacesDict.TryGetValue(spaceId, out var space))
                {
                    distancias.Add(Distancia(room.PontoCentral, space.PontoCentral));
                }
            }

            if (distancias.Count > 0)
            {
                relatorio.DistanciaMedia = distancias.Average();
                relatorio.DistanciaMaxima = distancias.Max();
                relatorio.DistanciaMinima = distancias.Min();
            }

            _log.Info(ETAPA, COMPONENTE,
                $"Relatório: {relatorio.TotalMatches}/{relatorio.TotalRooms} Rooms pareados " +
                $"({relatorio.PercentualCobertura:F1}%), " +
                $"distância média={relatorio.DistanciaMedia:F3} m, " +
                $"máx={relatorio.DistanciaMaxima:F3} m");

            return relatorio;
        }

        // ══════════════════════════════════════════════════════════
        //  UTILIDADES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula a distância euclidiana 2D entre dois pontos (ignora Z).
        /// Pontos já devem estar em metros (PontoXYZ do PluginCore).
        /// </summary>
        private static double Distancia(PontoXYZ a, PontoXYZ b)
        {
            var dx = a.X - b.X;
            var dy = a.Y - b.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        // ══════════════════════════════════════════════════════════
        //  MODELOS INTERNOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Par candidato Room ↔ Space com metadados de distância.
        /// </summary>
        private class Candidato
        {
            public long RoomId { get; set; }
            public long SpaceId { get; set; }
            public string RoomNome { get; set; } = string.Empty;
            public string SpaceNome { get; set; } = string.Empty;
            public double Distancia { get; set; }
            public bool MesmoNivel { get; set; }
        }
    }

    /// <summary>
    /// Relatório de qualidade da correspondência Room ↔ Space.
    /// </summary>
    public class RelatorioCorrespondencia
    {
        public int TotalRooms { get; set; }
        public int TotalSpaces { get; set; }
        public int TotalMatches { get; set; }
        public int RoomsSemMatch { get; set; }
        public int SpacesOrfaos { get; set; }
        public double PercentualCobertura { get; set; }
        public double DistanciaMedia { get; set; }
        public double DistanciaMaxima { get; set; }
        public double DistanciaMinima { get; set; }

        public override string ToString()
        {
            return $"Matches: {TotalMatches}/{TotalRooms} ({PercentualCobertura:F1}%) | " +
                   $"Dist média: {DistanciaMedia:F3} m | " +
                   $"Órfãos: {SpacesOrfaos}";
        }
    }
}
