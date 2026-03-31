using System.Text.Json;
using System.Text.Json.Serialization;
using Autodesk.Revit.DB;

namespace Revit2026.Modules.Core
{
    // ══════════════════════════════════════════════════════════════
    //  CONVERSOR DE UNIDADES — CENTRALIZADO
    //  Revit API opera internamente em FEET (Imperial).
    //  Todo o plugin opera em METROS (SI / NBR).
    // ══════════════════════════════════════════════════════════════

    /// <summary>
    /// Utilitário central para conversão de unidades entre o sistema
    /// interno do Revit (imperial/feet) e o sistema métrico (SI/NBR).
    /// <para>
    /// Todas as constantes e métodos de conversão do pipeline devem
    /// ser acessados exclusivamente por esta classe, eliminando
    /// magic numbers dispersos no código.
    /// </para>
    /// </summary>
    public static class UnitConverter
    {
        // ══════════════════════════════════════════════════════════
        //  CONSTANTES FUNDAMENTAIS
        // ══════════════════════════════════════════════════════════

        /// <summary>1 foot = 0.3048 meters (exato por definição)</summary>
        public const double FeetToMeters = 0.3048;

        /// <summary>1 meter = 3.28083989501... feet</summary>
        public const double MetersToFeet = 1.0 / FeetToMeters;

        /// <summary>1 foot = 304.8 millimeters (exato)</summary>
        public const double FeetToMillimeters = 304.8;

        /// <summary>1 millimeter = 1/304.8 feet</summary>
        public const double MillimetersToFeet = 1.0 / FeetToMillimeters;

        /// <summary>1 sq foot = 0.09290304 sq meters (exato)</summary>
        public const double SqFeetToSqMeters = FeetToMeters * FeetToMeters;

        /// <summary>1 sq meter = 10.7639... sq feet</summary>
        public const double SqMetersToSqFeet = MetersToFeet * MetersToFeet;

        /// <summary>1 cubic foot = 0.028316846592 cubic meters (exato)</summary>
        public const double CuFeetToCuMeters = FeetToMeters * FeetToMeters * FeetToMeters;

        /// <summary>1 cubic meter = 35.3147... cubic feet</summary>
        public const double CuMetersToCuFeet = MetersToFeet * MetersToFeet * MetersToFeet;

        /// <summary>1 inch = 25.4 millimeters (exato)</summary>
        public const double InchesToMillimeters = 25.4;

        /// <summary>1 foot = 12 inches</summary>
        public const double FeetToInches = 12.0;

        /// <summary>Casas decimais padrão para arredondamento</summary>
        public const int DefaultPrecision = 4;

        // ══════════════════════════════════════════════════════════
        //  COMPRIMENTO: FEET ↔ METERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte feet → meters (4 casas decimais)</summary>
        public static double FtToM(double feet) =>
            Math.Round(feet * FeetToMeters, DefaultPrecision);

        /// <summary>Converte meters → feet (4 casas decimais)</summary>
        public static double MToFt(double meters) =>
            Math.Round(meters * MetersToFeet, DefaultPrecision);

        /// <summary>Converte feet → meters com precisão customizada</summary>
        public static double FtToM(double feet, int precision) =>
            Math.Round(feet * FeetToMeters, precision);

        /// <summary>Converte meters → feet com precisão customizada</summary>
        public static double MToFt(double meters, int precision) =>
            Math.Round(meters * MetersToFeet, precision);

        // ══════════════════════════════════════════════════════════
        //  COMPRIMENTO: FEET ↔ MILLIMETERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte feet → millimeters</summary>
        public static double FtToMm(double feet) =>
            Math.Round(feet * FeetToMillimeters, DefaultPrecision);

        /// <summary>Converte millimeters → feet</summary>
        public static double MmToFt(double mm) =>
            Math.Round(mm * MillimetersToFeet, DefaultPrecision);

        /// <summary>Converte millimeters → feet sem arredondamento
        /// (para setar parâmetros no Revit)</summary>
        public static double MmToFtRaw(double mm) =>
            mm * MillimetersToFeet;

        // ══════════════════════════════════════════════════════════
        //  ÁREA: SQ FEET ↔ SQ METERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte sq feet → sq meters</summary>
        public static double SqFtToSqM(double sqFeet) =>
            Math.Round(sqFeet * SqFeetToSqMeters, DefaultPrecision);

        /// <summary>Converte sq meters → sq feet</summary>
        public static double SqMToSqFt(double sqMeters) =>
            Math.Round(sqMeters * SqMetersToSqFeet, DefaultPrecision);

        // ══════════════════════════════════════════════════════════
        //  VOLUME: CU FEET ↔ CU METERS
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte cubic feet → cubic meters</summary>
        public static double CuFtToCuM(double cuFeet) =>
            Math.Round(cuFeet * CuFeetToCuMeters, DefaultPrecision);

        /// <summary>Converte cubic meters → cubic feet</summary>
        public static double CuMToCuFt(double cuMeters) =>
            Math.Round(cuMeters * CuMetersToCuFeet, DefaultPrecision);

        // ══════════════════════════════════════════════════════════
        //  PONTOS E VETORES (XYZ)
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte XYZ de feet → meters (retorna double[])</summary>
        public static double[] XyzToMeters(XYZ point) =>
            new[]
            {
                FtToM(point.X),
                FtToM(point.Y),
                FtToM(point.Z)
            };

        /// <summary>Converte XYZ de feet → meters (retorna XYZ)</summary>
        public static XYZ XyzFtToM(XYZ point) =>
            new(
                point.X * FeetToMeters,
                point.Y * FeetToMeters,
                point.Z * FeetToMeters);

        /// <summary>Converte XYZ de meters → feet (retorna XYZ)</summary>
        public static XYZ XyzMToFt(XYZ point) =>
            new(
                point.X * MetersToFeet,
                point.Y * MetersToFeet,
                point.Z * MetersToFeet);

        /// <summary>Converte um valor em meters para feet e cria XYZ com Z</summary>
        public static double MToFtElevation(double heightM) =>
            heightM * MetersToFeet;

        // ══════════════════════════════════════════════════════════
        //  SLOPE / INCLINAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte slope em porcentagem (%) para fração (0-1)</summary>
        public static double SlopePercentToFraction(double percent) =>
            percent / 100.0;

        /// <summary>Converte slope em fração (0-1) para porcentagem (%)</summary>
        public static double SlopeFractionToPercent(double fraction) =>
            fraction * 100.0;

        /// <summary>Calcula drop (queda) em feet dado comprimento horizontal
        /// (feet) e slope (%)</summary>
        public static double CalculateDrop(double horizontalLenFt,
                                           double slopePercent) =>
            horizontalLenFt * (slopePercent / 100.0);

        // ══════════════════════════════════════════════════════════
        //  DIÂMETRO — ATALHOS MEP
        // ══════════════════════════════════════════════════════════

        /// <summary>Lê o diâmetro de um Pipe e retorna em mm</summary>
        public static double GetPipeDiameterMm(Element pipe)
        {
            var param = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            return FtToMm(param?.AsDouble() ?? 0);
        }

        /// <summary>Define o diâmetro de um Pipe a partir de mm</summary>
        public static bool SetPipeDiameterMm(Element pipe, double diamMm)
        {
            var param = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
            if (param == null || param.IsReadOnly) return false;
            param.Set(MmToFtRaw(diamMm));
            return true;
        }

        /// <summary>Lê o comprimento de um Pipe e retorna em metros</summary>
        public static double GetPipeLengthM(Element pipe)
        {
            var param = pipe.get_Parameter(
                BuiltInParameter.CURVE_ELEM_LENGTH);
            return FtToM(param?.AsDouble() ?? 0);
        }

        /// <summary>Lê o slope de um Pipe e retorna em %</summary>
        public static double GetPipeSlopePercent(Element pipe)
        {
            var param = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_SLOPE);
            return SlopeFractionToPercent(param?.AsDouble() ?? 0);
        }

        /// <summary>Define o slope de um Pipe a partir de %</summary>
        public static bool SetPipeSlopePercent(Element pipe,
                                               double slopePercent)
        {
            var param = pipe.get_Parameter(
                BuiltInParameter.RBS_PIPE_SLOPE);
            if (param == null || param.IsReadOnly) return false;
            param.Set(SlopePercentToFraction(slopePercent));
            return true;
        }

        // ══════════════════════════════════════════════════════════
        //  ROOM — ATALHOS
        // ══════════════════════════════════════════════════════════

        /// <summary>Lê a área de um Room e retorna em m²</summary>
        public static double GetRoomAreaM2(Element room)
        {
            var param = room.get_Parameter(BuiltInParameter.ROOM_AREA);
            return SqFtToSqM(param?.AsDouble() ?? 0);
        }

        /// <summary>Lê o perímetro de um Room e retorna em metros</summary>
        public static double GetRoomPerimeterM(Element room)
        {
            var param = room.get_Parameter(BuiltInParameter.ROOM_PERIMETER);
            return FtToM(param?.AsDouble() ?? 0);
        }

        /// <summary>Lê o volume de um Room e retorna em m³</summary>
        public static double GetRoomVolumeM3(Element room)
        {
            var param = room.get_Parameter(BuiltInParameter.ROOM_VOLUME);
            return CuFtToCuM(param?.AsDouble() ?? 0);
        }

        /// <summary>Lê a altura de um Room e retorna em metros</summary>
        public static double GetRoomHeightM(Element room)
        {
            var param = room.get_Parameter(BuiltInParameter.ROOM_HEIGHT);
            return FtToM(param?.AsDouble() ?? 0);
        }

        /// <summary>Retorna a elevação de um Level em metros</summary>
        public static double GetLevelElevationM(Level level) =>
            FtToM(level.Elevation);

        // ══════════════════════════════════════════════════════════
        //  TOLERÂNCIA
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte tolerância em mm para feet</summary>
        public static double ToleranceMmToFt(double toleranceMm) =>
            MmToFtRaw(toleranceMm);

        // ══════════════════════════════════════════════════════════
        //  CONVERSÃO EM LOTE
        // ══════════════════════════════════════════════════════════

        /// <summary>Converte uma lista de valores de feet → meters</summary>
        public static List<double> BatchFtToM(IEnumerable<double> feetValues) =>
            feetValues.Select(v => FtToM(v)).ToList();

        /// <summary>Converte uma lista de valores de meters → feet</summary>
        public static List<double> BatchMToFt(IEnumerable<double> meterValues) =>
            meterValues.Select(v => MToFt(v)).ToList();
    }

    // ══════════════════════════════════════════════════════════════
    //  RESULTADO DE CONVERSÃO EM LOTE (LOG)
    // ══════════════════════════════════════════════════════════════

    public class ConversionLogEntry
    {
        [JsonPropertyName("field")]
        public string Field { get; set; } = "";

        [JsonPropertyName("elementId")]
        public long ElementId { get; set; }

        [JsonPropertyName("originalValueFt")]
        public double OriginalValueFt { get; set; }

        [JsonPropertyName("convertedValueM")]
        public double ConvertedValueM { get; set; }

        [JsonPropertyName("conversionType")]
        public string ConversionType { get; set; } = "FtToM";
    }

    public class ConversionSummary
    {
        [JsonPropertyName("totalConverted")]
        public int TotalConverted { get; set; }

        [JsonPropertyName("totalErrors")]
        public int TotalErrors { get; set; }

        [JsonPropertyName("entries")]
        public List<ConversionLogEntry> Entries { get; set; } = new();

        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();

        [JsonPropertyName("isConsistent")]
        public bool IsConsistent { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // ══════════════════════════════════════════════════════════════
    //  SERVIÇO DE CONVERSÃO COM LOG
    // ══════════════════════════════════════════════════════════════

    public interface IUnitConversionService
    {
        ConversionSummary ConverterERegistrar(
            Document doc, List<long>? elementIds = null);
    }

    /// <summary>
    /// Serviço que percorre elementos do modelo, converte valores
    /// de feet para metros e registra um log completo de auditoria.
    /// <para>
    /// NOTA: Este serviço NÃO altera o modelo Revit. Ele apenas
    /// lê valores, converte e gera o log. Os serviços individuais
    /// (RoomReader, SewerRouting, etc.) usam UnitConverter para
    /// converter valores em runtime.
    /// </para>
    /// </summary>
    public class UnitConversionService : IUnitConversionService
    {
        public event Action<string>? OnProgress;

        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ConversionSummary ConverterERegistrar(
            Document doc, List<long>? elementIds = null)
        {
            var summary = new ConversionSummary();

            EmitProgress("Verificando unidades do modelo...");

            // ── 1. Rooms ──
            VerificarRooms(doc, summary);

            // ── 2. Levels ──
            VerificarLevels(doc, summary);

            // ── 3. Pipes ──
            VerificarPipes(doc, summary, elementIds);

            // ── 4. Consistência ──
            summary.IsConsistent = summary.TotalErrors == 0;

            EmitProgress($"Verificação concluída: " +
                         $"{summary.TotalConverted} campos verificados, " +
                         $"{summary.TotalErrors} erros. " +
                         $"Consistente: {summary.IsConsistent}");

            // Persistir
            SalvarLog(summary);

            return summary;
        }

        private void VerificarRooms(
            Document doc, ConversionSummary summary)
        {
            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .ToElements();

            foreach (var room in rooms)
            {
                var id = room.Id.Value;

                RegistrarConversao(summary, id, "Room.Area",
                    room.get_Parameter(BuiltInParameter.ROOM_AREA),
                    "SqFtToSqM", UnitConverter.SqFeetToSqMeters);

                RegistrarConversao(summary, id, "Room.Perimeter",
                    room.get_Parameter(BuiltInParameter.ROOM_PERIMETER),
                    "FtToM", UnitConverter.FeetToMeters);

                RegistrarConversao(summary, id, "Room.Volume",
                    room.get_Parameter(BuiltInParameter.ROOM_VOLUME),
                    "CuFtToCuM", UnitConverter.CuFeetToCuMeters);

                RegistrarConversao(summary, id, "Room.Height",
                    room.get_Parameter(BuiltInParameter.ROOM_HEIGHT),
                    "FtToM", UnitConverter.FeetToMeters);
            }
        }

        private void VerificarLevels(
            Document doc, ConversionSummary summary)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .ToList();

            foreach (var level in levels)
            {
                summary.Entries.Add(new ConversionLogEntry
                {
                    Field = "Level.Elevation",
                    ElementId = level.Id.Value,
                    OriginalValueFt = level.Elevation,
                    ConvertedValueM = UnitConverter.FtToM(level.Elevation),
                    ConversionType = "FtToM"
                });
                summary.TotalConverted++;
            }
        }

        private void VerificarPipes(
            Document doc, ConversionSummary summary,
            List<long>? filterIds)
        {
            var collectors = new FilteredElementCollector(doc)
                .OfClass(typeof(Autodesk.Revit.DB.Plumbing.Pipe))
                .ToElements();

            foreach (var elem in collectors)
            {
                if (filterIds != null &&
                    !filterIds.Contains(elem.Id.Value))
                    continue;

                var id = elem.Id.Value;

                RegistrarConversao(summary, id, "Pipe.Diameter",
                    elem.get_Parameter(
                        BuiltInParameter.RBS_PIPE_DIAMETER_PARAM),
                    "FtToMm", UnitConverter.FeetToMillimeters);

                RegistrarConversao(summary, id, "Pipe.Length",
                    elem.get_Parameter(
                        BuiltInParameter.CURVE_ELEM_LENGTH),
                    "FtToM", UnitConverter.FeetToMeters);

                var slopeParam = elem.get_Parameter(
                    BuiltInParameter.RBS_PIPE_SLOPE);
                if (slopeParam != null)
                {
                    summary.Entries.Add(new ConversionLogEntry
                    {
                        Field = "Pipe.Slope",
                        ElementId = id,
                        OriginalValueFt = slopeParam.AsDouble(),
                        ConvertedValueM = UnitConverter
                            .SlopeFractionToPercent(slopeParam.AsDouble()),
                        ConversionType = "FractionToPercent"
                    });
                    summary.TotalConverted++;
                }
            }
        }

        private static void RegistrarConversao(
            ConversionSummary summary,
            long elementId,
            string field,
            Parameter? param,
            string convType,
            double factor)
        {
            if (param == null)
            {
                summary.Errors.Add(
                    $"Elemento {elementId}: parâmetro '{field}' não encontrado");
                summary.TotalErrors++;
                return;
            }

            var rawValue = param.AsDouble();
            var converted = Math.Round(rawValue * factor,
                UnitConverter.DefaultPrecision);

            summary.Entries.Add(new ConversionLogEntry
            {
                Field = field,
                ElementId = elementId,
                OriginalValueFt = rawValue,
                ConvertedValueM = converted,
                ConversionType = convType
            });
            summary.TotalConverted++;
        }

        private void SalvarLog(ConversionSummary summary)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData),
                    "HermesMEP", "Units");

                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var fileName =
                    $"unit_conversion_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                var filePath = Path.Combine(dir, fileName);

                var json = JsonSerializer.Serialize(summary, JsonOpts);
                File.WriteAllText(filePath, json, System.Text.Encoding.UTF8);

                EmitProgress($"Log de conversão salvo: {filePath}");
            }
            catch
            {
                // não quebrar fluxo
            }
        }

        private void EmitProgress(string message)
        {
            OnProgress?.Invoke(message);
        }
    }
}
