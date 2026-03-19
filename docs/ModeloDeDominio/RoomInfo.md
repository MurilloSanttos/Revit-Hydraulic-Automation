# Modelo de Domínio — RoomInfo (AmbienteInfo)

> Especificação completa do modelo agnóstico ao Revit que representa ambientes de projeto para tomada de decisão hidráulica.

---

## 1. Definição do Modelo

### 1.1 O que é RoomInfo

`RoomInfo` é a representação de um ambiente de projeto dentro do PluginCore. Ele abstrai os conceitos de `Room` e `Space` do Revit em um objeto puro de domínio, sem nenhuma dependência da API do Revit.

### 1.2 Papel no sistema

| Módulo | Uso do RoomInfo |
|--------|----------------|
| E01 — Detecção | Resultado da leitura do modelo |
| E02 — Classificação | Input para `RoomClassifier` |
| E03 — Pontos | Base para mapear aparelhos obrigatórios |
| E04 — Inserção | Define onde inserir equipamentos |
| E05 — Validação | Referência para validar existentes |
| E06 — Prumadas | Agrupamento por posição/adjacência |
| E11 — Dimensionamento | Contexto de cálculo (altura, área) |

### 1.3 Por que é agnóstico ao Revit

```
O Core NÃO pode referenciar Autodesk.Revit.DB.

Se RoomInfo usasse Room, ElementId ou XYZ:
  → Core não compila sem DLLs do Revit
  → Core não é testável com xUnit puro
  → Core fica acoplado a uma versão específica do Revit

Com RoomInfo agnóstico:
  → Core compila sozinho
  → 100% testável sem Revit
  → Funciona com Revit 2022, 2024, 2025, 2026
  → Pode ser serializado para JSON
```

---

## 2. Estrutura de Dados — Código C# Completo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa um ambiente de projeto de forma agnóstica ao Revit.
    /// Criado pelo adapter RevitModelReader a partir de Room/Space.
    /// Usado por todo o Core para decisões hidráulicas.
    /// </summary>
    public class RoomInfo
    {
        // ── Identidade ──────────────────────────────────────────────

        /// <summary>
        /// ID único do ambiente. Corresponde ao ElementId.ToString() do Revit.
        /// Usado como chave de referência em todo o sistema.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nome original do ambiente como aparece no Revit (ex: "Banheiro Social").
        /// Usado pelo RoomClassifier para determinar o tipo.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Número do ambiente no Revit (ex: "101", "201").
        /// Usado para display e identificação pelo usuário.
        /// </summary>
        public string Number { get; set; }

        // ── Classificação ───────────────────────────────────────────

        /// <summary>
        /// Tipo classificado do ambiente (Bathroom, Kitchen, etc.).
        /// Preenchido pelo RoomClassifier na etapa E02.
        /// Antes da classificação: RoomType.Unknown.
        /// </summary>
        public RoomType ClassifiedType { get; set; } = RoomType.Unknown;

        /// <summary>
        /// Confiança da classificação (0.0 a 1.0).
        /// 1.0 = match exato. 0.7 = limiar mínimo aceitável.
        /// Preenchido pelo RoomClassifier na etapa E02.
        /// </summary>
        public double ClassificationConfidence { get; set; }

        // ── Localização ─────────────────────────────────────────────

        /// <summary>
        /// Nome do Level/andar (ex: "Térreo", "1º Pavimento").
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// Elevação do Level em metros.
        /// </summary>
        public double LevelElevationM { get; set; }

        /// <summary>
        /// Centroide do ambiente em metros (coordenada X).
        /// </summary>
        public double CenterX { get; set; }

        /// <summary>
        /// Centroide do ambiente em metros (coordenada Y).
        /// </summary>
        public double CenterY { get; set; }

        /// <summary>
        /// Centroide do ambiente em metros (coordenada Z = elevação do piso).
        /// </summary>
        public double CenterZ { get; set; }

        // ── Geometria ───────────────────────────────────────────────

        /// <summary>
        /// Área do ambiente em metros quadrados (m²).
        /// </summary>
        public double AreaSqM { get; set; }

        /// <summary>
        /// Perímetro do ambiente em metros (m).
        /// </summary>
        public double PerimeterM { get; set; }

        /// <summary>
        /// Altura do piso ao teto em metros (m).
        /// Obtido do parâmetro Unbounded Height no Revit.
        /// </summary>
        public double HeightM { get; set; }

        /// <summary>
        /// Volume do ambiente em metros cúbicos (m³).
        /// Calculado: AreaSqM × HeightM (simplificado) ou lido do Revit.
        /// </summary>
        public double VolumeCbM { get; set; }

        /// <summary>
        /// Bounding box simplificada: menor coordenada (canto inferior esquerdo).
        /// Em metros.
        /// </summary>
        public Point3D BBoxMin { get; set; }

        /// <summary>
        /// Bounding box simplificada: maior coordenada (canto superior direito).
        /// Em metros.
        /// </summary>
        public Point3D BBoxMax { get; set; }

        /// <summary>
        /// Contorno 2D do ambiente como lista de pontos (polígono simplificado).
        /// Opcional. Útil para posicionamento preciso de equipamentos.
        /// Em metros, no plano XY.
        /// </summary>
        public List<Point2D> BoundaryPolygon { get; set; } = new();

        // ── Relações ────────────────────────────────────────────────

        /// <summary>
        /// IDs dos ambientes adjacentes (compartilham parede).
        /// Preenchido opcionalmente na etapa E01.
        /// </summary>
        public List<string> AdjacentRoomIds { get; set; } = new();

        /// <summary>
        /// ID do ambiente acima (no andar superior), se houver.
        /// Usado para alinhamento de prumadas.
        /// </summary>
        public string AboveRoomId { get; set; }

        /// <summary>
        /// ID do ambiente abaixo (no andar inferior), se houver.
        /// Usado para alinhamento de prumadas.
        /// </summary>
        public string BelowRoomId { get; set; }

        // ── Metadados Hidráulicos ───────────────────────────────────

        /// <summary>
        /// Indica se o ambiente é uma área molhada (banheiro, cozinha, lavanderia, etc.).
        /// Calculado a partir de ClassifiedType.
        /// </summary>
        public bool IsWetArea => ClassifiedType switch
        {
            RoomType.Bathroom => true,
            RoomType.Lavatory => true,
            RoomType.Kitchen => true,
            RoomType.GourmetKitchen => true,
            RoomType.Laundry => true,
            RoomType.ServiceArea => true,
            RoomType.ExternalArea => true,
            _ => false
        };

        /// <summary>
        /// Sistemas hidráulicos necessários neste ambiente.
        /// Calculado a partir de ClassifiedType.
        /// </summary>
        public List<HydraulicSystem> RequiredSystems => ClassifiedType switch
        {
            RoomType.Bathroom => new() { HydraulicSystem.ColdWater, HydraulicSystem.Sewer, HydraulicSystem.Ventilation },
            RoomType.Lavatory => new() { HydraulicSystem.ColdWater, HydraulicSystem.Sewer },
            RoomType.Kitchen => new() { HydraulicSystem.ColdWater, HydraulicSystem.Sewer },
            RoomType.GourmetKitchen => new() { HydraulicSystem.ColdWater, HydraulicSystem.Sewer },
            RoomType.Laundry => new() { HydraulicSystem.ColdWater, HydraulicSystem.Sewer },
            RoomType.ServiceArea => new() { HydraulicSystem.ColdWater, HydraulicSystem.Sewer },
            RoomType.ExternalArea => new() { HydraulicSystem.ColdWater },
            _ => new()
        };

        /// <summary>
        /// Lista de pontos hidráulicos planejados para este ambiente.
        /// Preenchido na etapa E03.
        /// </summary>
        public List<HydraulicPoint> PlannedPoints { get; set; } = new();

        /// <summary>
        /// Lista de equipamentos já existentes no modelo para este ambiente.
        /// Preenchido na etapa E05.
        /// </summary>
        public List<EquipmentInfo> ExistingEquipment { get; set; } = new();
    }
}
```

---

## 3. Tipos Auxiliares

### 3.1 Point3D

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Ponto 3D em metros. Substitui Autodesk.Revit.DB.XYZ.
    /// Imutável. Usado para coordenadas, centroides, bounding box.
    /// </summary>
    public readonly struct Point3D
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public Point3D(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double DistanceTo(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            double dz = Z - other.Z;
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public double DistanceTo2D(Point3D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => $"({X:F3}, {Y:F3}, {Z:F3})";

        public static Point3D Zero => new(0, 0, 0);
    }
}
```

### 3.2 Point2D

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Ponto 2D em metros. Usado para polígonos de contorno.
    /// </summary>
    public readonly struct Point2D
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double DistanceTo(Point2D other)
        {
            double dx = X - other.X;
            double dy = Y - other.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => $"({X:F3}, {Y:F3})";
    }
}
```

### 3.3 Enum RoomType

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Classificação funcional dos ambientes para decisão hidráulica.
    /// Valores baseados nos ambientes residenciais típicos brasileiros.
    /// </summary>
    public enum RoomType
    {
        /// <summary>Não classificado (pré-E02).</summary>
        Unknown = 0,

        /// <summary>Banheiro (com vaso, chuveiro, lavatório, ralo).</summary>
        Bathroom = 1,

        /// <summary>Lavabo (com vaso e lavatório, sem chuveiro).</summary>
        Lavatory = 2,

        /// <summary>Cozinha padrão (com pia, filtro, máq. louças).</summary>
        Kitchen = 3,

        /// <summary>Cozinha gourmet / espaço gourmet.</summary>
        GourmetKitchen = 4,

        /// <summary>Lavanderia / área de serviço com tanque e máquina.</summary>
        Laundry = 5,

        /// <summary>Área de serviço (similar a Laundry).</summary>
        ServiceArea = 6,

        /// <summary>Área externa com ponto de água (jardim, piscina).</summary>
        ExternalArea = 7,

        /// <summary>Área técnica (shaft, casa de máquinas).</summary>
        TechnicalArea = 8,

        /// <summary>Ambiente sem necessidade hidráulica (sala, quarto, corredor).</summary>
        NonHydraulic = 99
    }
}
```

### 3.4 HydraulicPoint

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Ponto hidráulico planejado ou existente dentro de um ambiente.
    /// </summary>
    public class HydraulicPoint
    {
        /// <summary>ID único do ponto.</summary>
        public string Id { get; set; }

        /// <summary>Tipo do equipamento (vaso, lavatório, chuveiro, etc.).</summary>
        public EquipmentType EquipmentType { get; set; }

        /// <summary>Se o ponto é obrigatório para o tipo de ambiente.</summary>
        public bool IsRequired { get; set; }

        /// <summary>Sistemas atendidos por este ponto.</summary>
        public List<HydraulicSystem> Systems { get; set; } = new();

        /// <summary>Peso para água fria (unidade de consumo).</summary>
        public double WeightColdWater { get; set; }

        /// <summary>Unidades de contribuição para esgoto (UHC).</summary>
        public int ContributionUnitsES { get; set; }

        /// <summary>DN mínimo do ramal de descarga para esgoto (mm).</summary>
        public int MinDischargeDiameterMm { get; set; }

        /// <summary>DN mínimo do sub-ramal de água fria (mm).</summary>
        public int MinColdWaterDiameterMm { get; set; }

        /// <summary>Posição planejada em metros (preenchida em E04).</summary>
        public Point3D? PlannedPosition { get; set; }

        /// <summary>ElementId do equipamento inserido (preenchido após E04).</summary>
        public string InsertedElementId { get; set; }

        /// <summary>Status de validação (preenchido em E05).</summary>
        public EquipmentStatus ValidationStatus { get; set; } = EquipmentStatus.Unknown;
    }
}
```

### 3.5 EquipmentInfo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Equipamento já existente no modelo Revit, lido pelo adapter.
    /// </summary>
    public class EquipmentInfo
    {
        /// <summary>ElementId.ToString() do FamilyInstance.</summary>
        public string ElementId { get; set; }

        /// <summary>Nome da família no Revit.</summary>
        public string FamilyName { get; set; }

        /// <summary>Tipo do equipamento inferido pela família.</summary>
        public EquipmentType InferredType { get; set; }

        /// <summary>Posição em metros.</summary>
        public Point3D Position { get; set; }

        /// <summary>Tem connector de água fria?</summary>
        public bool HasColdWaterConnector { get; set; }

        /// <summary>Tem connector de esgoto?</summary>
        public bool HasSewerConnector { get; set; }

        /// <summary>ID do Room que contém este equipamento.</summary>
        public string ContainingRoomId { get; set; }
    }
}
```

---

## 4. Geometria Simplificada

### 4.1 BoundingBox

```
BBoxMin e BBoxMax definem um retângulo alinhado aos eixos:

  BBoxMax ──────────────────┐
  │                         │
  │     (CenterX, CenterY)  │
  │           ×              │
  │                         │
  └──────────────────BBoxMin

Unidade: metros
Coordenada Z: elevação do piso (BBoxMin.Z) até teto (BBoxMax.Z)
```

### 4.2 BoundaryPolygon

```
Lista ordenada de Point2D formando o contorno do ambiente (sentido horário).
Último ponto fecha no primeiro (implícito).

Exemplo — banheiro retangular 2.0m × 3.0m:
  [
    (0.0, 0.0),
    (2.0, 0.0),
    (2.0, 3.0),
    (0.0, 3.0)
  ]

USO:
  - Verificar se ponto está dentro do ambiente
  - Calcular distância à parede mais próxima
  - Posicionar equipamentos com offset da parede

QUANDO PREENCHER:
  - Opcional em E01
  - Obrigatório se E04 usar posicionamento automático avançado
```

### 4.3 Unidades

| Propriedade | Unidade | Conversão (do Revit) |
|------------|---------|---------------------|
| CenterX/Y/Z | metros | × 0.3048 (de feet) |
| AreaSqM | m² | × 0.09290304 (de sq feet) |
| PerimeterM | m | × 0.3048 (de feet) |
| HeightM | m | × 0.3048 (de feet) |
| VolumeCbM | m³ | × 0.028316846592 (de cubic feet) |
| BBoxMin/Max | metros | × 0.3048 (de feet) |
| BoundaryPolygon | metros | × 0.3048 (de feet) |

---

## 5. Relações Espaciais

### 5.1 Adjacência

```csharp
// Dois ambientes são adjacentes se compartilham parede

public bool IsAdjacentTo(RoomInfo other)
{
    return AdjacentRoomIds.Contains(other.Id);
}

// Uso: agrupar banheiros adjacentes para compartilhar prumada
public List<RoomInfo> GetAdjacentWetAreas(List<RoomInfo> allRooms)
{
    return allRooms
        .Where(r => AdjacentRoomIds.Contains(r.Id) && r.IsWetArea)
        .ToList();
}
```

### 5.2 Alinhamento vertical

```csharp
// Ambientes no mesmo eixo vertical (para prumadas)

public bool IsVerticallyAlignedWith(RoomInfo other, double toleranceM = 1.0)
{
    return Math.Abs(CenterX - other.CenterX) < toleranceM
        && Math.Abs(CenterY - other.CenterY) < toleranceM
        && LevelName != other.LevelName;
}

// Uso: verificar se banheiro do térreo está abaixo do banheiro do 1º andar
// → Compartilham a mesma prumada
```

### 5.3 Distância entre ambientes

```csharp
public double DistanceTo(RoomInfo other)
{
    return new Point3D(CenterX, CenterY, CenterZ)
        .DistanceTo2D(new Point3D(other.CenterX, other.CenterY, other.CenterZ));
}

// Uso: calcular comprimento estimado de ramal entre cozinha e prumada
```

---

## 6. Métodos do Modelo

```csharp
public class RoomInfo
{
    // ... propriedades acima ...

    // ── Métodos de consulta ─────────────────────────────────────

    /// <summary>
    /// Retorna true se o ambiente é uma área molhada (precisa de hidráulica).
    /// </summary>
    public bool IsWetArea => /* definido acima via switch */;

    /// <summary>
    /// Retorna true se a classificação foi feita e tem confiança suficiente.
    /// </summary>
    public bool IsClassified => ClassifiedType != RoomType.Unknown
                             && ClassificationConfidence >= 0.70;

    /// <summary>
    /// Retorna true se o ambiente tem todos os pontos obrigatórios preenchidos.
    /// </summary>
    public bool HasAllRequiredPoints =>
        PlannedPoints
            .Where(p => p.IsRequired)
            .All(p => p.ValidationStatus == EquipmentStatus.Valid
                   || p.ValidationStatus == EquipmentStatus.ValidWithRemarks);

    /// <summary>
    /// Retorna o centroide como Point3D.
    /// </summary>
    public Point3D GetCentroid() => new(CenterX, CenterY, CenterZ);

    /// <summary>
    /// Calcula a soma de pesos AF de todos os pontos planejados.
    /// </summary>
    public double GetTotalWeightColdWater() =>
        PlannedPoints.Sum(p => p.WeightColdWater);

    /// <summary>
    /// Calcula a soma de UHCs de todos os pontos planejados.
    /// </summary>
    public int GetTotalContributionUnitsES() =>
        PlannedPoints.Sum(p => p.ContributionUnitsES);

    /// <summary>
    /// Retorna pontos obrigatórios que ainda estão Missing.
    /// </summary>
    public List<HydraulicPoint> GetMissingRequiredPoints() =>
        PlannedPoints
            .Where(p => p.IsRequired && p.ValidationStatus == EquipmentStatus.Missing)
            .ToList();

    /// <summary>
    /// Retorna a largura do ambiente (BBox.X).
    /// </summary>
    public double GetWidthM() =>
        BBoxMax != null && BBoxMin != null
            ? BBoxMax.X - BBoxMin.X
            : 0;

    /// <summary>
    /// Retorna a profundidade do ambiente (BBox.Y).
    /// </summary>
    public double GetDepthM() =>
        BBoxMax != null && BBoxMin != null
            ? BBoxMax.Y - BBoxMin.Y
            : 0;

    /// <summary>
    /// Verifica se um ponto está dentro do bounding box do ambiente.
    /// </summary>
    public bool ContainsPoint(Point3D point)
    {
        if (BBoxMin == null || BBoxMax == null) return false;
        return point.X >= BBoxMin.X && point.X <= BBoxMax.X
            && point.Y >= BBoxMin.Y && point.Y <= BBoxMax.Y
            && point.Z >= BBoxMin.Z && point.Z <= BBoxMax.Z;
    }

    /// <summary>
    /// Retorna string de display para UI e logs.
    /// </summary>
    public string GetDisplayName() =>
        $"{Name} ({Number}) — {LevelName}";

    /// <summary>
    /// Retorna resumo para log.
    /// </summary>
    public override string ToString() =>
        $"Room[{Id}] {Name} ({ClassifiedType}, {AreaSqM:F1}m², {LevelName})";
}
```

---

## 7. Validações

### 7.1 Regras de validade

```csharp
public class RoomInfoValidator
{
    /// <summary>
    /// Valida se um RoomInfo está completo e consistente.
    /// </summary>
    public ValidationReport Validate(RoomInfo room)
    {
        var report = new ValidationReport();

        // Identidade
        if (string.IsNullOrWhiteSpace(room.Id))
            report.Add(ValidationLevel.Critical, "Room sem ID");

        if (string.IsNullOrWhiteSpace(room.Name))
            report.Add(ValidationLevel.Medium, $"Room {room.Id}: sem nome");

        // Geometria
        if (room.AreaSqM <= 0)
            report.Add(ValidationLevel.Critical, $"Room {room.Id}: área ≤ 0");

        if (room.AreaSqM < 1.0)
            report.Add(ValidationLevel.Light, $"Room {room.Id}: área muito pequena ({room.AreaSqM:F1}m²)");

        if (room.HeightM <= 0)
            report.Add(ValidationLevel.Light, $"Room {room.Id}: altura não definida");

        // Localização
        if (string.IsNullOrWhiteSpace(room.LevelName))
            report.Add(ValidationLevel.Medium, $"Room {room.Id}: sem Level");

        if (room.CenterX == 0 && room.CenterY == 0)
            report.Add(ValidationLevel.Light, $"Room {room.Id}: centroide na origem (0,0)");

        // Classificação (pós-E02)
        if (room.ClassifiedType != RoomType.Unknown && room.ClassificationConfidence < 0.70)
            report.Add(ValidationLevel.Medium, 
                $"Room {room.Id}: confiança baixa ({room.ClassificationConfidence:P0})");

        // BBox
        if (room.BBoxMin != null && room.BBoxMax != null)
        {
            if (room.BBoxMin.X >= room.BBoxMax.X || room.BBoxMin.Y >= room.BBoxMax.Y)
                report.Add(ValidationLevel.Medium, $"Room {room.Id}: BBox invertida");
        }

        return report;
    }
}
```

### 7.2 Tratamento de dados incompletos

| Dado ausente | Nível | Ação |
|-------------|-------|------|
| Id | Crítico | Room descartado |
| Name | Médio | Aceitar, classificar como Unknown |
| AreaSqM ≤ 0 | Crítico | Room descartado |
| HeightM = 0 | Leve | Usar default 2.80m |
| PerimeterM = 0 | Leve | Calcular a partir do BBox |
| BoundaryPolygon vazio | Leve | Usar BBox como aproximação |
| AdjacentRoomIds vazio | Leve | Sem agrupamento, prumada independente |
| LevelName vazio | Médio | Aceitar, log de alerta |

---

## 8. Regras de Criação (Mapeamento Revit → RoomInfo)

### 8.1 Onde acontece

```
Camada: HidraulicoPlugin.Revit.Adapters.RevitModelReader
Método: ConvertToRoomInfo(Room room) → RoomInfo
Momento: Etapa E01 (Detecção)
```

### 8.2 Mapeamento campo a campo

| RoomInfo | Fonte Revit | Conversão |
|----------|-----------|-----------|
| `Id` | `room.Id.ToString()` | direto |
| `Name` | `room.get_Parameter(ROOM_NAME).AsString()` | direto |
| `Number` | `room.Number` | direto |
| `LevelName` | `room.Level.Name` | direto |
| `LevelElevationM` | `room.Level.Elevation` | × 0.3048 |
| `CenterX` | `((LocationPoint)room.Location).Point.X` | × 0.3048 |
| `CenterY` | `((LocationPoint)room.Location).Point.Y` | × 0.3048 |
| `CenterZ` | `((LocationPoint)room.Location).Point.Z` | × 0.3048 |
| `AreaSqM` | `room.Area` | × 0.09290304 |
| `PerimeterM` | `room.Perimeter` | × 0.3048 |
| `HeightM` | `room.UnboundedHeight` | × 0.3048 |
| `VolumeCbM` | `room.Volume` | × 0.028316846592 |
| `BBoxMin` | `room.get_BoundingBox(null).Min` | × 0.3048 cada eixo |
| `BBoxMax` | `room.get_BoundingBox(null).Max` | × 0.3048 cada eixo |
| `BoundaryPolygon` | `room.GetBoundarySegments()` | × 0.3048 cada ponto |
| `ClassifiedType` | — | Preenchido em E02 |
| `PlannedPoints` | — | Preenchido em E03 |

### 8.3 Código do adapter

```csharp
// HidraulicoPlugin.Revit.Adapters.RevitModelReader

private RoomInfo ConvertToRoomInfo(Room room)
{
    var location = (LocationPoint)room.Location;
    var point = location.Point;
    var bbox = room.get_BoundingBox(null);

    return new RoomInfo
    {
        Id = room.Id.ToString(),
        Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
        Number = room.Number ?? "",
        LevelName = room.Level?.Name ?? "",
        LevelElevationM = (room.Level?.Elevation ?? 0) * 0.3048,
        CenterX = point.X * 0.3048,
        CenterY = point.Y * 0.3048,
        CenterZ = point.Z * 0.3048,
        AreaSqM = room.Area * 0.09290304,
        PerimeterM = room.Perimeter * 0.3048,
        HeightM = room.UnboundedHeight * 0.3048,
        VolumeCbM = room.Volume * 0.028316846592,
        BBoxMin = bbox != null
            ? new Point3D(bbox.Min.X * 0.3048, bbox.Min.Y * 0.3048, bbox.Min.Z * 0.3048)
            : Point3D.Zero,
        BBoxMax = bbox != null
            ? new Point3D(bbox.Max.X * 0.3048, bbox.Max.Y * 0.3048, bbox.Max.Z * 0.3048)
            : Point3D.Zero,
        BoundaryPolygon = ExtractBoundary(room)
    };
}

private List<Point2D> ExtractBoundary(Room room)
{
    var options = new SpatialElementBoundaryOptions();
    var segments = room.GetBoundarySegments(options);
    if (segments == null || !segments.Any()) return new();

    return segments[0] // primeiro loop (contorno externo)
        .Select(seg => seg.GetCurve().GetEndPoint(0))
        .Select(pt => new Point2D(pt.X * 0.3048, pt.Y * 0.3048))
        .ToList();
}
```

---

## 9. Exemplo em JSON

```json
{
  "id": "423567",
  "name": "Banheiro Social",
  "number": "102",
  "classified_type": "Bathroom",
  "classification_confidence": 0.95,
  "level_name": "Térreo",
  "level_elevation_m": 0.0,
  "center_x": 5.200,
  "center_y": 3.800,
  "center_z": 0.000,
  "area_sq_m": 4.50,
  "perimeter_m": 8.60,
  "height_m": 2.80,
  "volume_cb_m": 12.60,
  "bbox_min": { "x": 4.200, "y": 2.800, "z": 0.000 },
  "bbox_max": { "x": 6.200, "y": 4.800, "z": 2.800 },
  "boundary_polygon": [
    { "x": 4.200, "y": 2.800 },
    { "x": 6.200, "y": 2.800 },
    { "x": 6.200, "y": 4.800 },
    { "x": 4.200, "y": 4.800 }
  ],
  "adjacent_room_ids": ["423568", "423572"],
  "above_room_id": "425001",
  "below_room_id": null,
  "is_wet_area": true,
  "required_systems": ["ColdWater", "Sewer", "Ventilation"],
  "planned_points": [
    {
      "id": "pt_001",
      "equipment_type": "ToiletCoupledTank",
      "is_required": true,
      "systems": ["ColdWater", "Sewer"],
      "weight_cold_water": 0.3,
      "contribution_units_es": 6,
      "min_discharge_diameter_mm": 100,
      "min_cold_water_diameter_mm": 20,
      "planned_position": { "x": 5.800, "y": 3.200, "z": 0.000 },
      "inserted_element_id": null,
      "validation_status": "Unknown"
    },
    {
      "id": "pt_002",
      "equipment_type": "Sink",
      "is_required": true,
      "systems": ["ColdWater", "Sewer"],
      "weight_cold_water": 0.3,
      "contribution_units_es": 2,
      "min_discharge_diameter_mm": 40,
      "min_cold_water_diameter_mm": 20,
      "planned_position": { "x": 4.600, "y": 4.500, "z": 0.600 },
      "inserted_element_id": null,
      "validation_status": "Unknown"
    },
    {
      "id": "pt_003",
      "equipment_type": "Shower",
      "is_required": true,
      "systems": ["ColdWater", "Sewer"],
      "weight_cold_water": 0.4,
      "contribution_units_es": 2,
      "min_discharge_diameter_mm": 40,
      "min_cold_water_diameter_mm": 20,
      "planned_position": { "x": 4.600, "y": 3.200, "z": 2.000 },
      "inserted_element_id": null,
      "validation_status": "Unknown"
    },
    {
      "id": "pt_004",
      "equipment_type": "FloorDrain",
      "is_required": true,
      "systems": ["Sewer"],
      "weight_cold_water": 0.0,
      "contribution_units_es": 1,
      "min_discharge_diameter_mm": 40,
      "min_cold_water_diameter_mm": 0,
      "planned_position": { "x": 5.200, "y": 3.800, "z": 0.000 },
      "inserted_element_id": null,
      "validation_status": "Unknown"
    }
  ],
  "existing_equipment": []
}
```

---

## 10. Resumo Visual

```
RoomInfo
├── Identidade
│   ├── Id (string)
│   ├── Name (string)
│   └── Number (string)
├── Classificação
│   ├── ClassifiedType (RoomType enum)
│   └── ClassificationConfidence (double 0-1)
├── Localização
│   ├── LevelName (string)
│   ├── LevelElevationM (double)
│   └── Center X/Y/Z (double, metros)
├── Geometria
│   ├── AreaSqM (double)
│   ├── PerimeterM (double)
│   ├── HeightM (double)
│   ├── VolumeCbM (double)
│   ├── BBoxMin/Max (Point3D)
│   └── BoundaryPolygon (List<Point2D>)
├── Relações
│   ├── AdjacentRoomIds (List<string>)
│   ├── AboveRoomId (string?)
│   └── BelowRoomId (string?)
├── Hidráulica
│   ├── IsWetArea (bool, calculado)
│   ├── RequiredSystems (List<HydraulicSystem>, calculado)
│   ├── PlannedPoints (List<HydraulicPoint>)
│   └── ExistingEquipment (List<EquipmentInfo>)
└── Métodos
    ├── IsClassified
    ├── HasAllRequiredPoints
    ├── GetTotalWeightColdWater()
    ├── GetTotalContributionUnitsES()
    ├── GetMissingRequiredPoints()
    ├── ContainsPoint(Point3D)
    ├── GetWidthM() / GetDepthM()
    ├── DistanceTo(RoomInfo)
    ├── IsAdjacentTo(RoomInfo)
    ├── IsVerticallyAlignedWith(RoomInfo)
    ├── GetDisplayName()
    └── ToString()
```
