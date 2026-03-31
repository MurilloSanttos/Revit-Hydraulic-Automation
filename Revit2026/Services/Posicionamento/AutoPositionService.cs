using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using PluginCore.Interfaces;

namespace Revit2026.Services.Posicionamento
{
    /// <summary>
    /// Serviço de posicionamento automático de equipamentos hidráulicos.
    /// Determina o ponto de inserção junto à parede mais próxima
    /// do centro do Room, aplicando offset para dentro do ambiente.
    ///
    /// Algoritmo:
    /// 1. Obtém ponto central do Room (LocationPoint ou BoundingBox)
    /// 2. Projeta o centro em cada parede (LocationCurve)
    /// 3. Seleciona a parede mais próxima
    /// 4. Calcula normal apontando para dentro do ambiente
    /// 5. Aplica offset na direção da normal
    /// </summary>
    public class AutoPositionService
    {
        private const string ETAPA = "04_Insercao";
        private const string COMPONENTE = "AutoPosition";

        // ══════════════════════════════════════════════════════════
        //  CÁLCULO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula ponto de inserção para equipamento junto à parede
        /// mais próxima do centro do Room.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="room">Room onde o equipamento será inserido.</param>
        /// <param name="paredes">Lista de paredes do ambiente.</param>
        /// <param name="offsetMm">Distância da parede em milímetros.</param>
        /// <param name="log">Serviço de log.</param>
        /// <returns>Ponto XYZ em coordenadas internas (pés) ou null.</returns>
        public XYZ? CalcularPontoDeInsercao(
            Document doc,
            Room room,
            IList<Wall> paredes,
            double offsetMm,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (log == null) throw new ArgumentNullException(nameof(log));

            // ── 1. Converter offset para pés ──────────────────
            var offsetFeet = UnitUtils.ConvertToInternalUnits(
                offsetMm, UnitTypeId.Millimeters);

            // ── 2. Obter ponto central ────────────────────────
            var pontoCentral = ObterPontoCentral(room, log);
            if (pontoCentral == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Impossível calcular posição: Room '{room.Name}' " +
                    $"(Id={room.Id.Value}) sem ponto central.",
                    room.Id.Value);
                return null;
            }

            // ── 3. Validar paredes ────────────────────────────
            if (paredes == null || paredes.Count == 0)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Impossível calcular posição: nenhuma parede fornecida " +
                    $"para Room '{room.Name}' (Id={room.Id.Value}).",
                    room.Id.Value);
                return null;
            }

            // ── 4. Encontrar parede mais próxima ──────────────
            var resultado = EncontrarParedeMaisProxima(
                pontoCentral, paredes, log);

            if (resultado == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Nenhuma parede válida encontrada para Room '{room.Name}' " +
                    $"(Id={room.Id.Value}). {paredes.Count} paredes analisadas.",
                    room.Id.Value);
                return null;
            }

            // ── 5. Calcular normal para dentro do ambiente ────
            var normal = CalcularNormalParaDentro(
                resultado.Curva, resultado.PontoProjetado, pontoCentral);

            // ── 6. Aplicar offset ─────────────────────────────
            var pontoInsercao = resultado.PontoProjetado + normal * offsetFeet;

            // Manter Z do ponto central (chão do ambiente)
            pontoInsercao = new XYZ(
                pontoInsercao.X,
                pontoInsercao.Y,
                pontoCentral.Z);

            // ── 7. Validar se o ponto está dentro do Room ─────
            if (!ValidarPontoDentroDoRoom(doc, room, pontoInsercao))
            {
                log.Medio(ETAPA, COMPONENTE,
                    $"Ponto calculado pode estar fora do Room '{room.Name}'. " +
                    $"Tentando fallback com offset reduzido.",
                    room.Id.Value);

                // Fallback: reduzir offset pela metade
                pontoInsercao = resultado.PontoProjetado + normal * (offsetFeet * 0.5);
                pontoInsercao = new XYZ(pontoInsercao.X, pontoInsercao.Y, pontoCentral.Z);
            }

            // ── 8. Log de sucesso ─────────────────────────────
            var distMetros = UnitUtils.ConvertFromInternalUnits(
                resultado.Distancia, UnitTypeId.Meters);
            var offMetros = UnitUtils.ConvertFromInternalUnits(
                offsetFeet, UnitTypeId.Meters);

            log.Info(ETAPA, COMPONENTE,
                $"Posicionamento automático: parede={resultado.ParedeId}, " +
                $"distância={distMetros:F3} m, offset={offMetros:F3} m, " +
                $"ponto=({pontoInsercao.X:F3}, {pontoInsercao.Y:F3}, {pontoInsercao.Z:F3}) pés. " +
                $"Room='{room.Name}'",
                room.Id.Value);

            return pontoInsercao;
        }

        // ══════════════════════════════════════════════════════════
        //  POSICIONAMENTO COM PREFERÊNCIA DE PAREDE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula ponto de inserção preferencialmente em uma parede específica.
        /// Se a parede não for válida, usa a mais próxima como fallback.
        /// </summary>
        public XYZ? CalcularPontoEmParede(
            Document doc,
            Room room,
            Wall paredePreferida,
            double offsetMm,
            ILogService log)
        {
            var pontoCentral = ObterPontoCentral(room, log);
            if (pontoCentral == null) return null;

            var offsetFeet = UnitUtils.ConvertToInternalUnits(
                offsetMm, UnitTypeId.Millimeters);

            // Tentar posicionar na parede preferida
            if (paredePreferida.Location is LocationCurve locationCurve)
            {
                var curva = locationCurve.Curve;
                var projecao = curva.Project(pontoCentral);

                if (projecao != null)
                {
                    var normal = CalcularNormalParaDentro(
                        curva, projecao.XYZPoint, pontoCentral);

                    var pontoInsercao = projecao.XYZPoint + normal * offsetFeet;
                    pontoInsercao = new XYZ(
                        pontoInsercao.X, pontoInsercao.Y, pontoCentral.Z);

                    log.Info(ETAPA, COMPONENTE,
                        $"Posicionamento na parede preferida: " +
                        $"Id={paredePreferida.Id.Value}",
                        room.Id.Value);

                    return pontoInsercao;
                }
            }

            // Fallback: parede mais próxima
            log.Leve(ETAPA, COMPONENTE,
                $"Parede preferida inválida, usando fallback.",
                room.Id.Value);

            return CalcularPontoDeInsercao(
                doc, room, new List<Wall> { paredePreferida }, offsetMm, log);
        }

        // ══════════════════════════════════════════════════════════
        //  PONTO CENTRAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém ponto central do Room.
        /// Estratégia 1: LocationPoint.
        /// Estratégia 2: BoundingBox centroide.
        /// </summary>
        private static XYZ? ObterPontoCentral(Room room, ILogService log)
        {
            // Estratégia 1: LocationPoint
            try
            {
                if (room.Location is LocationPoint locationPoint)
                    return locationPoint.Point;
            }
            catch { /* fallback */ }

            // Estratégia 2: BoundingBox centroide
            try
            {
                var bbox = room.get_BoundingBox(null);
                if (bbox != null)
                {
                    var centroide = (bbox.Min + bbox.Max) / 2.0;
                    log.Leve(ETAPA, COMPONENTE,
                        $"Ponto central via BoundingBox: Room '{room.Name}'.",
                        room.Id.Value);
                    return centroide;
                }
            }
            catch { /* sem fallback restante */ }

            return null;
        }

        // ══════════════════════════════════════════════════════════
        //  PAREDE MAIS PRÓXIMA
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Resultado da busca por parede mais próxima.
        /// </summary>
        private class ResultadoProximidade
        {
            public long ParedeId { get; set; }
            public XYZ PontoProjetado { get; set; } = XYZ.Zero;
            public Curve Curva { get; set; } = null!;
            public double Distancia { get; set; } = double.MaxValue;
        }

        /// <summary>
        /// Encontra a parede mais próxima do ponto central.
        /// Projeta o ponto em cada LocationCurve e seleciona a menor distância.
        /// </summary>
        private static ResultadoProximidade? EncontrarParedeMaisProxima(
            XYZ pontoCentral, IList<Wall> paredes, ILogService log)
        {
            ResultadoProximidade? melhor = null;

            foreach (var parede in paredes)
            {
                try
                {
                    if (parede.Location is not LocationCurve locationCurve)
                        continue;

                    var curva = locationCurve.Curve;
                    var projecao = curva.Project(pontoCentral);

                    if (projecao == null)
                        continue;

                    var distancia = pontoCentral.DistanceTo(projecao.XYZPoint);

                    if (melhor == null || distancia < melhor.Distancia)
                    {
                        melhor = new ResultadoProximidade
                        {
                            ParedeId = parede.Id.Value,
                            PontoProjetado = projecao.XYZPoint,
                            Curva = curva,
                            Distancia = distancia
                        };
                    }
                }
                catch (Exception ex)
                {
                    log.Leve(ETAPA, COMPONENTE,
                        $"Erro ao projetar em parede Id={parede.Id.Value}: {ex.Message}");
                }
            }

            return melhor;
        }

        // ══════════════════════════════════════════════════════════
        //  NORMAL DA PAREDE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Calcula o vetor normal da parede que aponta para dentro do ambiente.
        /// O sentido é verificado comparando com a direção do ponto central.
        /// </summary>
        private static XYZ CalcularNormalParaDentro(
            Curve curva, XYZ pontoProjetado, XYZ pontoCentral)
        {
            // Vetor da parede
            var start = curva.GetEndPoint(0);
            var end = curva.GetEndPoint(1);
            var direcaoParede = (end - start).Normalize();

            // Normal perpendicular (rotação 90° no plano XY)
            var normal = new XYZ(-direcaoParede.Y, direcaoParede.X, 0).Normalize();

            // Vetor do ponto projetado até o centro do ambiente
            var paraCentro = (pontoCentral - pontoProjetado).Normalize();

            // Verificar se a normal aponta na mesma direção do centro
            var dotProduct = normal.DotProduct(paraCentro);

            // Se negativo, a normal aponta para fora → inverter
            if (dotProduct < 0)
                normal = normal.Negate();

            return normal;
        }

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO DE PONTO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se o ponto está dentro do Room usando doc.GetRoomAtPoint.
        /// </summary>
        private static bool ValidarPontoDentroDoRoom(
            Document doc, Room room, XYZ ponto)
        {
            try
            {
                var roomNoPonto = doc.GetRoomAtPoint(ponto);
                return roomNoPonto != null &&
                       roomNoPonto.Id.Value == room.Id.Value;
            }
            catch
            {
                return false;
            }
        }
    }
}
