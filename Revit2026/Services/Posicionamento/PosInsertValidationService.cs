using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using PluginCore.Interfaces;

namespace Revit2026.Services.Posicionamento
{
    /// <summary>
    /// Resultado da validação de posicionamento pós-inserção.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>Se todas as validações passaram.</summary>
        public bool Valido { get; set; }

        /// <summary>Motivo da falha (se inválido).</summary>
        public string Motivo { get; set; } = string.Empty;

        /// <summary>Detalhes das validações executadas.</summary>
        public List<string> Detalhes { get; set; } = new();

        /// <summary>Distância mínima encontrada até paredes (metros).</summary>
        public double DistanciaMinParede { get; set; }

        /// <summary>Quantidade de colisões detectadas.</summary>
        public int Colisoes { get; set; }

        public static ValidationResult Ok(string detalhe = "")
        {
            return new ValidationResult
            {
                Valido = true,
                Detalhes = string.IsNullOrEmpty(detalhe)
                    ? new List<string>()
                    : new List<string> { detalhe }
            };
        }

        public static ValidationResult Falha(string motivo)
        {
            return new ValidationResult
            {
                Valido = false,
                Motivo = motivo
            };
        }

        public override string ToString() =>
            Valido ? "✅ Posição válida" : $"❌ {Motivo}";
    }

    /// <summary>
    /// Serviço de validação de posicionamento pós-inserção.
    /// Verifica se o equipamento inserido está:
    /// 1. Dentro do Room correto
    /// 2. A distância mínima das paredes
    /// 3. Sem colisão com outros equipamentos
    /// 4. Sem atravessar paredes
    /// </summary>
    public class PosInsertValidationService
    {
        private const string ETAPA = "04_Insercao";
        private const string COMPONENTE = "PosValidation";

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO PRINCIPAL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Executa todas as validações de posicionamento pós-inserção.
        /// </summary>
        /// <param name="doc">Documento Revit.</param>
        /// <param name="equipamento">FamilyInstance inserida.</param>
        /// <param name="room">Room onde deveria estar.</param>
        /// <param name="offsetMinMm">Distância mínima da parede (mm).</param>
        /// <param name="log">Serviço de log.</param>
        public ValidationResult Validar(
            Document doc,
            FamilyInstance equipamento,
            Room room,
            double offsetMinMm,
            ILogService log)
        {
            if (doc == null) throw new ArgumentNullException(nameof(doc));
            if (equipamento == null) throw new ArgumentNullException(nameof(equipamento));
            if (room == null) throw new ArgumentNullException(nameof(room));
            if (log == null) throw new ArgumentNullException(nameof(log));

            var offsetFeet = UnitUtils.ConvertToInternalUnits(
                offsetMinMm, UnitTypeId.Millimeters);

            var pontoEquip = ObterPontoEquipamento(equipamento);
            if (pontoEquip == null)
            {
                log.Critico(ETAPA, COMPONENTE,
                    $"Equipamento {equipamento.Id.Value} sem ponto de localização.",
                    equipamento.Id.Value);
                return ValidationResult.Falha(
                    "Equipamento sem ponto de localização.");
            }

            // ── V1: Dentro do Room ────────────────────────────
            var v1 = ValidarDentroDoRoom(doc, equipamento, room, pontoEquip, log);
            if (!v1.Valido) return v1;

            // ── V2: Distância mínima a paredes ────────────────
            var v2 = ValidarDistanciaParedes(doc, room, pontoEquip, offsetFeet, log);
            if (!v2.Valido) return v2;

            // ── V3: Colisão com outros equipamentos ───────────
            var v3 = ValidarColisoes(doc, equipamento, room, log);
            if (!v3.Valido) return v3;

            // ── V4: Atravessamento de paredes ─────────────────
            var v4 = ValidarAtravessamentoParedes(doc, equipamento, pontoEquip, log);
            if (!v4.Valido) return v4;

            // ── Sucesso ───────────────────────────────────────
            log.Info(ETAPA, COMPONENTE,
                $"Validação OK: equipamento {equipamento.Id.Value} " +
                $"('{equipamento.Symbol.FamilyName}') posicionado corretamente " +
                $"em Room '{room.Name}'.",
                equipamento.Id.Value);

            var resultado = ValidationResult.Ok();
            resultado.DistanciaMinParede = v2.DistanciaMinParede;
            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  V1: DENTRO DO ROOM
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se o equipamento está dentro do Room esperado.
        /// </summary>
        private static ValidationResult ValidarDentroDoRoom(
            Document doc,
            FamilyInstance equipamento,
            Room room,
            XYZ pontoEquip,
            ILogService log)
        {
            try
            {
                // Verificar via IsPointInRoom
                if (!room.IsPointInRoom(pontoEquip))
                {
                    // Fallback: GetRoomAtPoint
                    var roomNoPonto = doc.GetRoomAtPoint(pontoEquip);
                    if (roomNoPonto == null || roomNoPonto.Id.Value != room.Id.Value)
                    {
                        log.Medio(ETAPA, COMPONENTE,
                            $"Equipamento {equipamento.Id.Value} está fora do " +
                            $"Room '{room.Name}' (Id={room.Id.Value}).",
                            equipamento.Id.Value);

                        return ValidationResult.Falha(
                            $"Equipamento fora do Room '{room.Name}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao verificar IsPointInRoom: {ex.Message}. " +
                    $"Continuando com validação parcial.",
                    equipamento.Id.Value);
            }

            return ValidationResult.Ok("Dentro do Room.");
        }

        // ══════════════════════════════════════════════════════════
        //  V2: DISTÂNCIA MÍNIMA A PAREDES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se o equipamento está a pelo menos offsetFeet de
        /// todas as paredes do Room.
        /// </summary>
        private static ValidationResult ValidarDistanciaParedes(
            Document doc,
            Room room,
            XYZ pontoEquip,
            double offsetFeet,
            ILogService log)
        {
            var resultado = new ValidationResult { Valido = true };
            double menorDistancia = double.MaxValue;

            try
            {
                // Coletar paredes via BoundingBox do Room
                var paredes = ColetarParedesDoRoom(doc, room);

                if (paredes.Count == 0)
                {
                    log.Leve(ETAPA, COMPONENTE,
                        $"Nenhuma parede encontrada para Room '{room.Name}'.",
                        room.Id.Value);
                    resultado.DistanciaMinParede = -1;
                    return resultado;
                }

                foreach (var parede in paredes)
                {
                    try
                    {
                        if (parede.Location is not LocationCurve locationCurve)
                            continue;

                        var curva = locationCurve.Curve;
                        var projecao = curva.Project(pontoEquip);

                        if (projecao == null)
                            continue;

                        var distancia = pontoEquip.DistanceTo(projecao.XYZPoint);

                        // Descontar metade da largura da parede
                        var larguraParede = parede.Width / 2.0;
                        var distanciaEfetiva = distancia - larguraParede;

                        if (distanciaEfetiva < menorDistancia)
                            menorDistancia = distanciaEfetiva;

                        if (distanciaEfetiva < offsetFeet)
                        {
                            var distMm = UnitUtils.ConvertFromInternalUnits(
                                distanciaEfetiva, UnitTypeId.Millimeters);
                            var offsetMm = UnitUtils.ConvertFromInternalUnits(
                                offsetFeet, UnitTypeId.Millimeters);

                            log.Medio(ETAPA, COMPONENTE,
                                $"Equipamento muito próximo da parede " +
                                $"Id={parede.Id.Value}: {distMm:F0} mm " +
                                $"(mínimo: {offsetMm:F0} mm).",
                                room.Id.Value);

                            resultado.Valido = false;
                            resultado.Motivo =
                                $"Distância insuficiente da parede: {distMm:F0} mm " +
                                $"(mínimo: {offsetMm:F0} mm).";
                        }
                    }
                    catch { /* parede individual — silencioso */ }
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao validar distância de paredes: {ex.Message}");
            }

            resultado.DistanciaMinParede = menorDistancia != double.MaxValue
                ? UnitUtils.ConvertFromInternalUnits(menorDistancia, UnitTypeId.Meters)
                : -1;

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  V3: COLISÃO COM OUTROS EQUIPAMENTOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se o equipamento colide com outros FamilyInstance MEP
        /// no mesmo Room.
        /// </summary>
        private static ValidationResult ValidarColisoes(
            Document doc,
            FamilyInstance equipamento,
            Room room,
            ILogService log)
        {
            var resultado = new ValidationResult { Valido = true };

            try
            {
                var bboxEquip = equipamento.get_BoundingBox(null);
                if (bboxEquip == null)
                    return resultado; // Sem bounding box → pular validação

                var outlineEquip = new Outline(bboxEquip.Min, bboxEquip.Max);

                // Coletar FamilyInstances MEP no mesmo Room
                var outros = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .Where(fi => fi.Id.Value != equipamento.Id.Value)
                    .ToList();

                int colisoes = 0;

                foreach (var outro in outros)
                {
                    try
                    {
                        var bboxOutro = outro.get_BoundingBox(null);
                        if (bboxOutro == null)
                            continue;

                        var outlineOutro = new Outline(bboxOutro.Min, bboxOutro.Max);

                        // Verificar se os dois Outlines se interceptam
                        if (outlineEquip.Intersects(outlineOutro, 0))
                        {
                            // Confirmar que estão no mesmo Room
                            var pontoOutro = ObterPontoEquipamento(outro);
                            if (pontoOutro != null)
                            {
                                var roomOutro = doc.GetRoomAtPoint(pontoOutro);
                                if (roomOutro != null &&
                                    roomOutro.Id.Value == room.Id.Value)
                                {
                                    colisoes++;
                                    log.Medio(ETAPA, COMPONENTE,
                                        $"Colisão: equipamento {equipamento.Id.Value} " +
                                        $"↔ {outro.Id.Value} " +
                                        $"('{outro.Symbol.FamilyName}') " +
                                        $"no Room '{room.Name}'.",
                                        equipamento.Id.Value);
                                }
                            }
                        }
                    }
                    catch { /* colisão individual — silencioso */ }
                }

                if (colisoes > 0)
                {
                    resultado.Valido = false;
                    resultado.Colisoes = colisoes;
                    resultado.Motivo =
                        $"Colisão com {colisoes} equipamento(s) no Room '{room.Name}'.";
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao validar colisões: {ex.Message}");
            }

            return resultado;
        }

        // ══════════════════════════════════════════════════════════
        //  V4: ATRAVESSAMENTO DE PAREDES
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se o bounding box do equipamento intersecta paredes.
        /// </summary>
        private static ValidationResult ValidarAtravessamentoParedes(
            Document doc,
            FamilyInstance equipamento,
            XYZ pontoEquip,
            ILogService log)
        {
            try
            {
                var bboxEquip = equipamento.get_BoundingBox(null);
                if (bboxEquip == null)
                    return ValidationResult.Ok();

                // Expandir ligeiramente o bbox para tolerância
                var min = bboxEquip.Min;
                var max = bboxEquip.Max;
                var outline = new Outline(min, max);

                // Buscar paredes que intersectem o bbox do equipamento
                var bbFilter = new BoundingBoxIntersectsFilter(outline);

                var paredesIntersectam = new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WherePasses(bbFilter)
                    .Cast<Wall>()
                    .ToList();

                if (paredesIntersectam.Count > 0)
                {
                    // Verificar se realmente atravessa
                    foreach (var parede in paredesIntersectam)
                    {
                        if (parede.Location is LocationCurve lc)
                        {
                            var proj = lc.Curve.Project(pontoEquip);
                            if (proj != null)
                            {
                                var dist = pontoEquip.DistanceTo(proj.XYZPoint);
                                var meiaPared = parede.Width / 2.0;

                                if (dist < meiaPared)
                                {
                                    log.Leve(ETAPA, COMPONENTE,
                                        $"Equipamento {equipamento.Id.Value} " +
                                        $"pode estar dentro da parede " +
                                        $"Id={parede.Id.Value} " +
                                        $"(dist={dist:F3} < {meiaPared:F3} pés).",
                                        equipamento.Id.Value);

                                    return ValidationResult.Falha(
                                        $"Equipamento atravessa parede " +
                                        $"Id={parede.Id.Value}.");
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Leve(ETAPA, COMPONENTE,
                    $"Erro ao validar atravessamento: {ex.Message}");
            }

            return ValidationResult.Ok();
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Obtém ponto de localização de um FamilyInstance.
        /// </summary>
        private static XYZ? ObterPontoEquipamento(FamilyInstance instancia)
        {
            try
            {
                if (instancia.Location is LocationPoint lp)
                    return lp.Point;

                // Fallback: centroide do BoundingBox
                var bbox = instancia.get_BoundingBox(null);
                if (bbox != null)
                    return (bbox.Min + bbox.Max) / 2.0;
            }
            catch { /* silencioso */ }

            return null;
        }

        /// <summary>
        /// Coleta paredes que intersectem o BoundingBox do Room.
        /// </summary>
        private static List<Wall> ColetarParedesDoRoom(Document doc, Room room)
        {
            try
            {
                var bbox = room.get_BoundingBox(null);
                if (bbox == null)
                    return new List<Wall>();

                var outline = new Outline(bbox.Min, bbox.Max);
                var bbFilter = new BoundingBoxIntersectsFilter(outline);

                return new FilteredElementCollector(doc)
                    .OfClass(typeof(Wall))
                    .WherePasses(bbFilter)
                    .Cast<Wall>()
                    .ToList();
            }
            catch
            {
                return new List<Wall>();
            }
        }
    }
}
