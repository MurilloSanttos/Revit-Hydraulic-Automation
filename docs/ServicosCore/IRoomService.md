# Serviço de Domínio — IRoomService (IAmbienteService)

> Especificação completa da interface de serviço responsável por detecção, classificação, validação e enriquecimento de ambientes, totalmente agnóstica ao Revit, para uso no PluginCore.

---

## 1. Definição da Interface

### 1.1 O que é IRoomService

`IRoomService` é a **interface de serviço de domínio** que centraliza toda a lógica de negócio relacionada a ambientes (Rooms/Spaces). É o ponto de entrada do pipeline — etapas E01 e E02 dependem integralmente deste serviço.

### 1.2 Papel no sistema

```
                    Camada Revit (Infrastructure)
                            │
                   RevitModelReader (adapter)
                            │
                    RoomRawData (DTO bruto)
                            │
                    ╔═══════╧═══════╗
                    ║  IRoomService  ║  ← ESTE SERVIÇO (Core)
                    ╠═══════════════╣
                    ║ DetectRooms()  ║
                    ║ ClassifyRoom() ║
                    ║ ValidateRoom() ║
                    ║ EnrichRoom()   ║
                    ║ GetWetAreas()  ║
                    ╚═══════╤═══════╝
                            │
                    List<RoomInfo> (modelos prontos)
                            │
                    Próximas etapas (E03, E04...)
```

| Etapa | Método usado |
|-------|-------------|
| E01 — Detecção | `DetectRooms()` → converte DTOs brutos em RoomInfo |
| E02 — Classificação | `ClassifyRoom()` → define RoomType de cada ambiente |
| E03 — Identificação | `GetWetAreas()` → lista ambientes com demanda hidráulica |
| E05 — Validação | `ValidateRoom()` → verifica conformidade |
| Pipeline | `EnrichRoom()` → completa dados faltantes |

### 1.3 Por que é independente do Revit

```
PROBLEMA:
  A API do Revit fornece Room com:
    - ElementId, Document, BoundingBox (tipos do Revit)
    - Parâmetros em pés (unidade imperial)
    - Acesso via FilteredElementCollector (Revit API)

  O Core NÃO pode importar Autodesk.Revit.DB.

SOLUÇÃO:
  1. RevitModelReader (Infraestrutura) lê os Rooms do Revit
  2. Converte para RoomRawData (DTO puro, sem tipos Revit)
  3. IRoomService (Core) recebe RoomRawData
  4. Processa, classifica, valida → retorna RoomInfo

BENEFÍCIOS:
  - Core testável sem Revit instalado
  - Mesma lógica funciona com dados mock, JSON, CSV
  - Classificação e validação isoladas para testes unitários
```

---

## 2. Responsabilidades

### 2.1 O que o serviço DEVE fazer

| Ação | Detalhe |
|------|---------|
| Converter DTOs brutos em RoomInfo | Transformar dados crus (pés → metros, string → enum) |
| Classificar ambientes por tipo | Nome + equipamentos → RoomType |
| Validar geometria e dados | Área, perímetro, altura, polygon |
| Enriquecer com dados derivados | IsWetArea, RequiredSystems, PlannedPoints |
| Filtrar áreas molhadas | Retornar apenas ambientes com demanda hidráulica |
| Detectar adjacências | Identificar ambientes vizinhos (para agrupamento de prumadas) |
| Gerar relatório de detecção | Resumo do que foi encontrado e classificado |

### 2.2 O que NÃO DEVE fazer

| Proibição | Quem faz |
|-----------|---------|
| ❌ Acessar API do Revit | RevitModelReader (Infrastructure) |
| ❌ Criar elementos no Revit | RevitElementWriter (Infrastructure) |
| ❌ Inserir equipamentos | IEquipmentService (outro serviço) |
| ❌ Gerar redes de tubulação | INetworkService (outro serviço) |
| ❌ Manipular UI | PluginUI (camada UI) |
| ❌ Ler/gravar arquivos | DataService (camada Data) |

---

## 3. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Serviço de domínio para detecção, classificação, validação
    /// e enriquecimento de ambientes.
    /// Ponto de entrada do pipeline (E01 + E02).
    /// Independente do Revit.
    /// </summary>
    public interface IRoomService
    {
        // ── E01: Detecção ───────────────────────────────────────

        /// <summary>
        /// Converte dados brutos de ambientes em modelos de domínio.
        /// Realiza conversão de unidades (pés → metros), parsing de nomes,
        /// e criação de RoomInfo com dados básicos preenchidos.
        /// </summary>
        /// <param name="rawRooms">Lista de DTOs brutos vindos do adapter Revit.</param>
        /// <returns>Resultado contendo lista de RoomInfo e relatório de detecção.</returns>
        RoomDetectionResult DetectRooms(List<RoomRawData> rawRooms);

        /// <summary>
        /// Converte um único DTO bruto em RoomInfo.
        /// </summary>
        RoomInfo ConvertToRoomInfo(RoomRawData rawRoom);

        // ── E02: Classificação ──────────────────────────────────

        /// <summary>
        /// Classifica um ambiente pelo seu tipo hidráulico.
        /// Usa: nome do ambiente + equipamentos detectados + regras de classificação.
        /// </summary>
        /// <param name="room">Ambiente a ser classificado.</param>
        /// <returns>Resultado com tipo classificado e nível de confiança.</returns>
        ClassificationResult ClassifyRoom(RoomInfo room);

        /// <summary>
        /// Classifica todos os ambientes de uma lista.
        /// Aplica ClassifyRoom() em cada um e atualiza o RoomInfo.
        /// </summary>
        /// <param name="rooms">Lista de ambientes.</param>
        /// <returns>Resultado com estatísticas de classificação.</returns>
        BatchClassificationResult ClassifyAll(List<RoomInfo> rooms);

        // ── Validação ───────────────────────────────────────────

        /// <summary>
        /// Valida um ambiente contra regras de negócio.
        /// Verifica: área mínima, geometria, nome, dados obrigatórios.
        /// </summary>
        /// <param name="room">Ambiente a ser validado.</param>
        /// <returns>Relatório de validação com lista de problemas.</returns>
        RoomValidationResult ValidateRoom(RoomInfo room);

        /// <summary>
        /// Valida todos os ambientes.
        /// </summary>
        BatchValidationResult ValidateAll(List<RoomInfo> rooms);

        // ── Enriquecimento ──────────────────────────────────────

        /// <summary>
        /// Enriquece um ambiente com dados derivados:
        /// - IsWetArea (baseado no RoomType)
        /// - RequiredSystems (AF, ES, VE)
        /// - PlannedPoints (pontos hidráulicos obrigatórios)
        /// - AdjacentRoomIds (adjacências detectadas)
        /// </summary>
        /// <param name="room">Ambiente a ser enriquecido.</param>
        /// <param name="allRooms">Todos os ambientes (para detectar adjacências).</param>
        /// <returns>RoomInfo atualizado com dados derivados.</returns>
        RoomInfo EnrichRoom(RoomInfo room, List<RoomInfo> allRooms);

        /// <summary>
        /// Enriquece todos os ambientes.
        /// </summary>
        List<RoomInfo> EnrichAll(List<RoomInfo> rooms);

        // ── Consultas ───────────────────────────────────────────

        /// <summary>
        /// Retorna apenas ambientes classificados como áreas molhadas.
        /// Áreas molhadas = ambientes que requerem pelo menos 1 sistema hidráulico.
        /// </summary>
        List<RoomInfo> GetWetAreas(List<RoomInfo> rooms);

        /// <summary>
        /// Retorna ambientes agrupados por nível (Level).
        /// </summary>
        Dictionary<string, List<RoomInfo>> GetRoomsByLevel(List<RoomInfo> rooms);

        /// <summary>
        /// Retorna ambientes agrupados por tipo.
        /// </summary>
        Dictionary<RoomType, List<RoomInfo>> GetRoomsByType(List<RoomInfo> rooms);

        /// <summary>
        /// Encontra ambientes adjacentes a um ambiente dado.
        /// Adjacência = bounding boxes com distância ≤ tolerância.
        /// </summary>
        List<RoomInfo> FindAdjacentRooms(
            RoomInfo room, List<RoomInfo> allRooms, double toleranceM = 0.5);

        /// <summary>
        /// Detecta alinhamento vertical entre ambientes de andares diferentes.
        /// Usado para agrupamento de prumadas (E06).
        /// </summary>
        List<List<RoomInfo>> FindVerticallyAlignedRooms(
            List<RoomInfo> rooms, double toleranceM = 1.5);

        // ── Pipeline completo ───────────────────────────────────

        /// <summary>
        /// Executa o pipeline completo: Detect → Classify → Validate → Enrich.
        /// Método de conveniência para rodar E01 + E02 de uma vez.
        /// </summary>
        RoomProcessingResult ProcessAll(List<RoomRawData> rawRooms);
    }
}
```

---

## 4. DTOs de Entrada

### 4.1 RoomRawData — Dados brutos do adapter

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// DTO com dados brutos do ambiente, extraídos pelo adapter Revit.
    /// Unidades: pés (como vêm do Revit). Conversão feita pelo IRoomService.
    /// Não contém NENHUM tipo do Revit.
    /// </summary>
    public class RoomRawData
    {
        // ── Identidade ──────────────────────────────────────────

        /// <summary>ElementId como string.</summary>
        public string RevitId { get; set; }

        /// <summary>Room.Name (pode conter "Banheiro", "Bath", etc.).</summary>
        public string Name { get; set; }

        /// <summary>Room.Number (ex: "102").</summary>
        public string Number { get; set; }

        // ── Level ───────────────────────────────────────────────

        /// <summary>Nome do Level (ex: "Térreo", "1º Pavimento").</summary>
        public string LevelName { get; set; }

        /// <summary>Elevação do Level em PÉS.</summary>
        public double LevelElevationFt { get; set; }

        // ── Geometria (em PÉS) ─────────────────────────────────

        /// <summary>Área em pés².</summary>
        public double AreaSqFt { get; set; }

        /// <summary>Perímetro em pés.</summary>
        public double PerimeterFt { get; set; }

        /// <summary>Altura (pé-direito) em pés.</summary>
        public double HeightFt { get; set; }

        /// <summary>Volume em pés³.</summary>
        public double VolumeCuFt { get; set; }

        // ── Posição (em PÉS) ───────────────────────────────────

        /// <summary>Centro X em pés.</summary>
        public double CenterXFt { get; set; }

        /// <summary>Centro Y em pés.</summary>
        public double CenterYFt { get; set; }

        /// <summary>Centro Z em pés.</summary>
        public double CenterZFt { get; set; }

        // ── BoundingBox (em PÉS) ───────────────────────────────

        /// <summary>BBox mínimo X em pés.</summary>
        public double BBoxMinXFt { get; set; }
        public double BBoxMinYFt { get; set; }
        public double BBoxMinZFt { get; set; }

        /// <summary>BBox máximo X em pés.</summary>
        public double BBoxMaxXFt { get; set; }
        public double BBoxMaxYFt { get; set; }
        public double BBoxMaxZFt { get; set; }

        // ── Boundary (em PÉS) ──────────────────────────────────

        /// <summary>
        /// Polígono de contorno do Room (lista de pontos XY em pés).
        /// Extraído de Room.GetBoundarySegments().
        /// </summary>
        public List<double[]> BoundaryPointsFt { get; set; } = new();

        // ── Equipamentos detectados ─────────────────────────────

        /// <summary>
        /// Nomes de famílias de equipamentos encontrados dentro do Room.
        /// Ex: ["Toilet - Flush Valve", "Lavatory - Round"]
        /// Usado como pista para classificação.
        /// </summary>
        public List<string> DetectedFamilyNames { get; set; } = new();

        /// <summary>
        /// Categorias dos equipamentos detectados.
        /// Ex: ["Plumbing Fixtures", "Mechanical Equipment"]
        /// </summary>
        public List<string> DetectedCategories { get; set; } = new();

        // ── Parâmetros do Revit ─────────────────────────────────

        /// <summary>
        /// Parâmetros customizados do Room como dicionário.
        /// Ex: { "Tipo_Ambiente": "Banheiro", "Area_Molhada": "Sim" }
        /// </summary>
        public Dictionary<string, string> CustomParameters { get; set; } = new();
    }
}
```

---

## 5. Modelos de Saída (Resultados)

### 5.1 RoomDetectionResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da detecção de ambientes (E01).
    /// </summary>
    public class RoomDetectionResult
    {
        /// <summary>Ambientes detectados com sucesso.</summary>
        public List<RoomInfo> Rooms { get; set; } = new();

        /// <summary>DTOs que falharam na conversão.</summary>
        public List<RoomConversionError> Errors { get; set; } = new();

        /// <summary>Total de DTOs recebidos.</summary>
        public int TotalInput { get; set; }

        /// <summary>Total convertido com sucesso.</summary>
        public int TotalConverted => Rooms.Count;

        /// <summary>Total com erro.</summary>
        public int TotalErrors => Errors.Count;

        /// <summary>Se a detecção foi bem-sucedida (≥ 1 ambiente).</summary>
        public bool IsSuccessful => Rooms.Count > 0;

        /// <summary>Tempo de execução.</summary>
        public TimeSpan ExecutionTime { get; set; }
    }

    public class RoomConversionError
    {
        public string RevitId { get; set; }
        public string RoomName { get; set; }
        public string ErrorMessage { get; set; }
    }
}
```

### 5.2 ClassificationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da classificação de um ambiente (E02).
    /// </summary>
    public class ClassificationResult
    {
        /// <summary>ID do ambiente classificado.</summary>
        public string RoomId { get; set; }

        /// <summary>Tipo classificado.</summary>
        public RoomType ClassifiedType { get; set; }

        /// <summary>
        /// Nível de confiança (0.0 a 1.0).
        /// 1.0 = match exato no nome.
        /// 0.5 = match por equipamentos detectados.
        /// 0.0 = não classificado.
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>Método que gerou a classificação.</summary>
        public ClassificationMethod Method { get; set; }

        /// <summary>Justificativa textual.</summary>
        public string Reason { get; set; }

        /// <summary>Classificações alternativas (segundo e terceiro candidatos).</summary>
        public List<(RoomType Type, double Confidence)> Alternatives { get; set; } = new();
    }

    public enum ClassificationMethod
    {
        /// <summary>Match direto pelo nome do Room.</summary>
        NameMatch = 1,

        /// <summary>Match por parâmetro customizado do Revit.</summary>
        ParameterMatch = 2,

        /// <summary>Inferido pelos equipamentos detectados dentro do Room.</summary>
        EquipmentInference = 3,

        /// <summary>Inferido pela geometria (área, proporções).</summary>
        GeometryInference = 4,

        /// <summary>Classificação manual pelo usuário.</summary>
        ManualOverride = 5,

        /// <summary>Não classificado.</summary>
        None = 0
    }
}
```

### 5.3 BatchClassificationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da classificação em lote.
    /// </summary>
    public class BatchClassificationResult
    {
        public List<ClassificationResult> Results { get; set; } = new();

        public int TotalRooms { get; set; }
        public int TotalClassified => Results.Count(r => r.ClassifiedType != RoomType.Unclassified);
        public int TotalUnclassified => Results.Count(r => r.ClassifiedType == RoomType.Unclassified);
        public int TotalWetAreas => Results.Count(r => IsWetArea(r.ClassifiedType));
        public int TotalDryAreas => TotalClassified - TotalWetAreas;
        public double AverageConfidence => Results.Average(r => r.Confidence);
        public TimeSpan ExecutionTime { get; set; }

        /// <summary>Resumo por tipo.</summary>
        public Dictionary<RoomType, int> CountByType =>
            Results.GroupBy(r => r.ClassifiedType)
                   .ToDictionary(g => g.Key, g => g.Count());

        private static bool IsWetArea(RoomType type) =>
            type is RoomType.Bathroom or RoomType.SuiteBathroom or RoomType.Lavatory
                 or RoomType.Kitchen or RoomType.ServiceArea or RoomType.WetBalcony
                 or RoomType.ExternalArea or RoomType.Kitchenette;
    }
}
```

### 5.4 RoomValidationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da validação de um ambiente.
    /// </summary>
    public class RoomValidationResult
    {
        public string RoomId { get; set; }
        public string RoomName { get; set; }
        public bool IsValid { get; set; }
        public List<RoomValidationIssue> Issues { get; set; } = new();
        public int CriticalCount => Issues.Count(i => i.Level == ValidationLevel.Critical);
        public int MediumCount => Issues.Count(i => i.Level == ValidationLevel.Medium);
    }

    public class RoomValidationIssue
    {
        public ValidationLevel Level { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string ActualValue { get; set; }
        public string ExpectedValue { get; set; }
    }

    public class BatchValidationResult
    {
        public List<RoomValidationResult> Results { get; set; } = new();
        public int TotalValid => Results.Count(r => r.IsValid);
        public int TotalInvalid => Results.Count(r => !r.IsValid);
        public int TotalCritical => Results.Sum(r => r.CriticalCount);
        public TimeSpan ExecutionTime { get; set; }
    }
}
```

### 5.5 RoomProcessingResult (Pipeline completo)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado do pipeline completo: Detect → Classify → Validate → Enrich.
    /// </summary>
    public class RoomProcessingResult
    {
        /// <summary>Ambientes processados (detectados + classificados + enriquecidos).</summary>
        public List<RoomInfo> Rooms { get; set; } = new();

        /// <summary>Resultado de detecção.</summary>
        public RoomDetectionResult Detection { get; set; }

        /// <summary>Resultado de classificação.</summary>
        public BatchClassificationResult Classification { get; set; }

        /// <summary>Resultado de validação.</summary>
        public BatchValidationResult Validation { get; set; }

        /// <summary>Se o pipeline rodou sem erros críticos.</summary>
        public bool IsSuccessful =>
            Detection.IsSuccessful && Validation.TotalCritical == 0;

        /// <summary>Apenas áreas molhadas (já filtradas).</summary>
        public List<RoomInfo> WetAreas =>
            Rooms.Where(r => r.IsWetArea).ToList();

        /// <summary>Tempo total de execução (soma das etapas).</summary>
        public TimeSpan TotalExecutionTime { get; set; }

        /// <summary>Resumo textual para log/UI.</summary>
        public string GetSummary()
        {
            return $"Ambientes: {Rooms.Count} detectados, " +
                   $"{Classification.TotalClassified} classificados, " +
                   $"{Classification.TotalWetAreas} áreas molhadas, " +
                   $"{Validation.TotalInvalid} com problemas. " +
                   $"Tempo: {TotalExecutionTime.TotalSeconds:F1}s";
        }
    }
}
```

---

## 6. Regras de Classificação

### 6.1 Classificação por nome (prioridade alta)

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Mapa de classificação: palavras-chave → RoomType.
    /// Carregado de room_classification_map.json (Data layer).
    /// </summary>
    public static class RoomClassificationRules
    {
        /// <summary>
        /// Dicionário: keyword (lowercase) → (RoomType, Confidence).
        /// Ordem importa: match mais específico primeiro.
        /// </summary>
        public static readonly List<(string[] Keywords, RoomType Type, double Confidence)> NameRules = new()
        {
            // ── Matches exatos (confiança alta) ─────────────────
            (new[]{"banheiro social"}, RoomType.Bathroom, 0.95),
            (new[]{"banheiro suíte", "banheiro suite", "wc suíte"}, RoomType.SuiteBathroom, 0.95),
            (new[]{"banheiro", "wc", "bathroom", "bath"}, RoomType.Bathroom, 0.90),
            (new[]{"lavabo", "half bath", "powder room"}, RoomType.Lavatory, 0.95),
            (new[]{"cozinha", "kitchen"}, RoomType.Kitchen, 0.95),
            (new[]{"área de serviço", "area de servico", "lavanderia", "laundry"}, RoomType.ServiceArea, 0.95),
            (new[]{"varanda molhada", "terraço", "balcony"}, RoomType.WetBalcony, 0.85),
            (new[]{"varanda", "sacada"}, RoomType.WetBalcony, 0.70),
            (new[]{"área externa", "jardim", "quintal"}, RoomType.ExternalArea, 0.85),
            (new[]{"copa", "kitchenette"}, RoomType.Kitchenette, 0.90),
            (new[]{"shaft", "duto", "técnica", "técnico"}, RoomType.Technical, 0.90),

            // ── Áreas secas ─────────────────────────────────────
            (new[]{"quarto", "dormitório", "suíte", "bedroom"}, RoomType.Bedroom, 0.90),
            (new[]{"sala", "living", "estar", "jantar"}, RoomType.LivingRoom, 0.90),
            (new[]{"corredor", "hall", "circulação", "circulation"}, RoomType.Circulation, 0.90),
            (new[]{"garagem", "garage"}, RoomType.Garage, 0.90),
            (new[]{"depósito", "despensa", "storage"}, RoomType.Storage, 0.85),
            (new[]{"escritório", "office", "home office"}, RoomType.Office, 0.85),
        };

        /// <summary>
        /// Classifica um ambiente pelo nome.
        /// </summary>
        public static ClassificationResult ClassifyByName(string roomName)
        {
            string normalized = roomName.Trim().ToLowerInvariant();

            foreach (var rule in NameRules)
            {
                foreach (var keyword in rule.Keywords)
                {
                    if (normalized.Contains(keyword))
                    {
                        return new ClassificationResult
                        {
                            ClassifiedType = rule.Type,
                            Confidence = rule.Confidence,
                            Method = ClassificationMethod.NameMatch,
                            Reason = $"Nome '{roomName}' contém keyword '{keyword}'"
                        };
                    }
                }
            }

            return new ClassificationResult
            {
                ClassifiedType = RoomType.Unclassified,
                Confidence = 0,
                Method = ClassificationMethod.None,
                Reason = $"Nome '{roomName}' não reconhecido"
            };
        }
    }
}
```

### 6.2 Classificação por equipamentos detectados (fallback)

```csharp
public static class RoomClassificationRules
{
    // ... NameRules acima ...

    /// <summary>
    /// Classifica pelo tipo de equipamento encontrado dentro do Room.
    /// Usado quando o nome não é conclusivo.
    /// </summary>
    public static ClassificationResult ClassifyByEquipment(List<string> familyNames)
    {
        bool hasToilet = familyNames.Any(f =>
            f.Contains("toilet", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("bacia", StringComparison.OrdinalIgnoreCase));

        bool hasSink = familyNames.Any(f =>
            f.Contains("lavatory", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("lavatório", StringComparison.OrdinalIgnoreCase));

        bool hasShower = familyNames.Any(f =>
            f.Contains("shower", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("chuveiro", StringComparison.OrdinalIgnoreCase));

        bool hasKitchenSink = familyNames.Any(f =>
            f.Contains("kitchen", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("pia", StringComparison.OrdinalIgnoreCase));

        bool hasLaundry = familyNames.Any(f =>
            f.Contains("tanque", StringComparison.OrdinalIgnoreCase) ||
            f.Contains("laundry", StringComparison.OrdinalIgnoreCase));

        // Inferência por combinação
        if (hasToilet && hasSink && hasShower)
            return Result(RoomType.Bathroom, 0.70, "Vaso + lavatório + chuveiro");

        if (hasToilet && hasSink && !hasShower)
            return Result(RoomType.Lavatory, 0.65, "Vaso + lavatório (sem chuveiro)");

        if (hasKitchenSink)
            return Result(RoomType.Kitchen, 0.60, "Pia de cozinha detectada");

        if (hasLaundry)
            return Result(RoomType.ServiceArea, 0.60, "Tanque detectado");

        if (hasToilet)
            return Result(RoomType.Bathroom, 0.50, "Apenas vaso detectado");

        return Result(RoomType.Unclassified, 0, "Nenhum equipamento hidráulico");
    }

    private static ClassificationResult Result(RoomType type, double conf, string reason) =>
        new()
        {
            ClassifiedType = type,
            Confidence = conf,
            Method = ClassificationMethod.EquipmentInference,
            Reason = reason
        };
}
```

---

## 7. Regras de Validação

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Regras de validação para ambientes.
    /// </summary>
    public static class RoomValidationRules
    {
        // ── Limites ─────────────────────────────────────────────
        public const double MinAreaSqM = 0.5;
        public const double MaxAreaSqM = 500.0;
        public const double MinHeightM = 2.0;
        public const double MaxHeightM = 6.0;
        public const double MinPerimeterM = 1.0;

        /// <summary>
        /// Valida um ambiente contra as regras de negócio.
        /// </summary>
        public static RoomValidationResult Validate(RoomInfo room)
        {
            var result = new RoomValidationResult
            {
                RoomId = room.Id,
                RoomName = room.Name
            };

            // ── Identidade ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(room.Id))
                result.Issues.Add(Critical("ID_MISSING", "Ambiente sem ID"));

            if (string.IsNullOrWhiteSpace(room.Name))
                result.Issues.Add(Medium("NAME_MISSING", "Ambiente sem nome"));

            // ── Geometria ───────────────────────────────────────
            if (room.AreaSqM < MinAreaSqM)
                result.Issues.Add(Critical("AREA_MIN",
                    $"Área = {room.AreaSqM:F2}m² (mín: {MinAreaSqM}m²)",
                    $"{room.AreaSqM:F2}", $"≥ {MinAreaSqM}"));

            if (room.AreaSqM > MaxAreaSqM)
                result.Issues.Add(Medium("AREA_MAX",
                    $"Área = {room.AreaSqM:F2}m² muito grande"));

            if (room.HeightM < MinHeightM)
                result.Issues.Add(Medium("HEIGHT_MIN",
                    $"Altura = {room.HeightM:F2}m (mín: {MinHeightM}m)"));

            if (room.PerimeterM < MinPerimeterM)
                result.Issues.Add(Critical("PERIM_MIN",
                    $"Perímetro = {room.PerimeterM:F2}m inválido"));

            // ── Posição ─────────────────────────────────────────
            if (room.CenterX == 0 && room.CenterY == 0)
                result.Issues.Add(Light("POS_ORIGIN",
                    "Ambiente na origem (0,0) — posição suspeita"));

            // ── Classificação ───────────────────────────────────
            if (room.ClassifiedType == RoomType.Unclassified)
                result.Issues.Add(Medium("UNCLASSIFIED",
                    "Ambiente não classificado"));

            if (room.ClassificationConfidence < 0.5 &&
                room.ClassifiedType != RoomType.Unclassified)
                result.Issues.Add(Light("LOW_CONFIDENCE",
                    $"Classificação com confiança baixa ({room.ClassificationConfidence:F2})"));

            // ── Dados de área molhada ───────────────────────────
            if (room.IsWetArea && room.RequiredSystems.Count == 0)
                result.Issues.Add(Medium("WET_NO_SYSTEMS",
                    "Área molhada sem sistemas hidráulicos definidos"));

            // ── Resultados ──────────────────────────────────────
            result.IsValid = result.CriticalCount == 0;
            return result;
        }

        private static RoomValidationIssue Critical(string code, string msg,
            string actual = null, string expected = null) =>
            new() { Level = ValidationLevel.Critical, Code = code,
                    Message = msg, ActualValue = actual, ExpectedValue = expected };

        private static RoomValidationIssue Medium(string code, string msg) =>
            new() { Level = ValidationLevel.Medium, Code = code, Message = msg };

        private static RoomValidationIssue Light(string code, string msg) =>
            new() { Level = ValidationLevel.Light, Code = code, Message = msg };
    }
}
```

---

## 8. Exemplo de Uso (no Orchestrator)

```csharp
// No PipelineOrchestrator, etapas E01 + E02:
public class PipelineOrchestrator
{
    private readonly IRoomService _roomService;

    public PipelineOrchestrator(IRoomService roomService)
    {
        _roomService = roomService;
    }

    public async Task ExecuteE01_E02(List<RoomRawData> rawData)
    {
        // ── OPÇÃO 1: Pipeline completo de uma vez ───────────────
        var result = _roomService.ProcessAll(rawData);

        Console.WriteLine(result.GetSummary());
        // → "Ambientes: 12 detectados, 12 classificados, 5 áreas molhadas, 0 com problemas. Tempo: 0.3s"

        if (!result.IsSuccessful)
        {
            // Tratar erros críticos
            foreach (var issue in result.Validation.Results
                .SelectMany(r => r.Issues)
                .Where(i => i.Level == ValidationLevel.Critical))
            {
                Console.WriteLine($"  ❌ {issue.Code}: {issue.Message}");
            }
            return;
        }

        // Usar apenas áreas molhadas para próximas etapas
        var wetAreas = result.WetAreas;
        // → [ Banheiro Social, Lavabo, Cozinha, Á. Serviço, Banheiro Suíte ]

        // Próxima etapa: E03 (identificação de pontos hidráulicos)
        await ExecuteE03(wetAreas);

        // ── OPÇÃO 2: Passo a passo ──────────────────────────────
        // Útil para debug ou quando precisa de controle granular

        // 1. Detectar
        var detection = _roomService.DetectRooms(rawData);
        // → 12 ambientes convertidos de RoomRawData para RoomInfo

        // 2. Classificar
        var classification = _roomService.ClassifyAll(detection.Rooms);
        // → 12 classificados: 3 Bathroom, 1 Lavatory, 1 Kitchen...

        // 3. Validar
        var validation = _roomService.ValidateAll(detection.Rooms);
        // → 12 válidos, 0 críticos

        // 4. Enriquecer
        var enriched = _roomService.EnrichAll(detection.Rooms);
        // → IsWetArea, RequiredSystems, PlannedPoints preenchidos

        // 5. Filtrar
        var wetOnly = _roomService.GetWetAreas(enriched);
        // → 5 áreas molhadas prontas para E03
    }
}
```

---

## 9. Constantes de Conversão

```csharp
namespace HidraulicoPlugin.Core.Constants
{
    /// <summary>
    /// Fatores de conversão de unidades imperiais (Revit) para métricas (Core).
    /// </summary>
    public static class UnitConversion
    {
        /// <summary>1 pé = 0.3048 metros.</summary>
        public const double FeetToMeters = 0.3048;

        /// <summary>1 pé² = 0.092903 m².</summary>
        public const double SqFeetToSqMeters = 0.092903;

        /// <summary>1 pé³ = 0.0283168 m³.</summary>
        public const double CuFeetToCuMeters = 0.0283168;

        public static double FtToM(double feet) => feet * FeetToMeters;
        public static double SqFtToSqM(double sqFt) => sqFt * SqFeetToSqMeters;
        public static double CuFtToCuM(double cuFt) => cuFt * CuFeetToCuMeters;
    }
}
```

---

## 10. Resumo Visual

```
IRoomService
│
├── Detecção (E01)
│   ├── DetectRooms(List<RoomRawData>) → RoomDetectionResult
│   └── ConvertToRoomInfo(RoomRawData) → RoomInfo
│
├── Classificação (E02)
│   ├── ClassifyRoom(RoomInfo) → ClassificationResult
│   └── ClassifyAll(List<RoomInfo>) → BatchClassificationResult
│
├── Validação
│   ├── ValidateRoom(RoomInfo) → RoomValidationResult
│   └── ValidateAll(List<RoomInfo>) → BatchValidationResult
│
├── Enriquecimento
│   ├── EnrichRoom(RoomInfo, List<RoomInfo>) → RoomInfo
│   └── EnrichAll(List<RoomInfo>) → List<RoomInfo>
│
├── Consultas
│   ├── GetWetAreas(List<RoomInfo>) → List<RoomInfo>
│   ├── GetRoomsByLevel(List<RoomInfo>) → Dict<Level, List>
│   ├── GetRoomsByType(List<RoomInfo>) → Dict<Type, List>
│   ├── FindAdjacentRooms(room, all, tolerance) → List<RoomInfo>
│   └── FindVerticallyAlignedRooms(all, tolerance) → List<List>
│
├── Pipeline Completo
│   └── ProcessAll(List<RoomRawData>) → RoomProcessingResult
│
├── DTOs de Entrada
│   └── RoomRawData (dados brutos em PÉS, sem tipos Revit)
│
├── DTOs de Saída
│   ├── RoomDetectionResult
│   ├── ClassificationResult + BatchClassificationResult
│   ├── RoomValidationResult + BatchValidationResult
│   └── RoomProcessingResult (pipeline completo)
│
├── Regras de Classificação
│   ├── NameRules (17 padrões de nome → RoomType)
│   └── EquipmentInference (equipamentos detectados → RoomType)
│
├── Regras de Validação
│   ├── Área mínima (0.5 m²) / máxima (500 m²)
│   ├── Altura mínima (2.0m)
│   ├── Perímetro mínimo (1.0m)
│   ├── Classificação obrigatória
│   └── Sistemas obrigatórios para áreas molhadas
│
└── Dependências
    ├── UnitConversion (pés → metros)
    └── room_classification_map.json (Data layer)
```
