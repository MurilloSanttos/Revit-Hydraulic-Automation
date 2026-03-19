# Serviço de Domínio — INetworkService (IRedeService)

> Especificação completa da interface de serviço responsável por geração, organização, validação e otimização de redes hidráulicas (AF, ES, VE), totalmente agnóstica ao Revit, para uso no PluginCore.

---

## 1. Definição da Interface

### 1.1 O que é INetworkService

`INetworkService` é o **serviço de domínio** que constrói redes hidráulicas completas como grafos lógicos (`PipeNetwork`). Ele recebe pontos hidráulicos e equipamentos, determina a topologia das conexões, gera trechos de tubulação (`PipeSegment`), organiza prumadas (`Riser`), e valida a conectividade e conformidade do sistema resultante.

### 1.2 Papel no sistema

```
IEquipmentService (E03/E04)
    │
    └── List<EquipmentInfo> + List<HydraulicPoint>
            │
            ╔═════════════════╗
            ║ INetworkService ║  ← ESTE SERVIÇO (Core)
            ╠═════════════════╣
            ║ BuildColdWater()║
            ║ BuildSewer()    ║
            ║ BuildVent()     ║
            ║ Validate()      ║
            ║ Optimize()      ║
            ╚═══════╤═════════╝
                    │
            PipeNetwork (grafo lógico completo)
                    │
            ├── ISizingService (E11 — dimensionamento)
            └── RevitNetworkWriter (Infrastructure — criação no Revit)
```

| Etapa | Método usado |
|-------|-------------|
| E06 — Prumadas | `BuildRisers()` → define colunas verticais |
| E07 — Rede AF | `BuildColdWaterNetwork()` → grafo AF completo |
| E08 — Rede ES | `BuildSewerNetwork()` → grafo ES completo |
| E08b — Ventilação | `BuildVentilationNetwork()` → grafo VE |
| E05 — Validação | `ValidateNetwork()` → conectividade + conformidade |
| E09 — Otimização | `OptimizeNetwork()` → remoção de redundâncias |
| E10 — Sistemas | `ExportForRevit()` → dados para criação no Revit |

### 1.3 Por que é independente do Revit

```
ESTE SERVIÇO (Core):
  - Recebe HydraulicPoint + EquipmentInfo (agnósticos)
  - Monta PipeNetwork como grafo (nós + arestas)
  - Gera PipeSegment com geometria em metros (Point3D)
  - Valida conectividade com BFS/DFS
  - Retorna PipeNetwork pronto para materialização

QUEM USA O REVIT (Infrastructure):
  - RevitNetworkWriter → traduz PipeNetwork para Pipes/Fittings/Systems
  - DynamoScriptExecutor → roda scripts .dyn para criar tubulações
  - UnMepExecutor → usa unMEP para routing automático

FLUXO:
  Core decide a TOPOLOGIA (o que conecta com o quê)
  Revit/Dynamo/unMEP cria a GEOMETRIA FÍSICA (Pipes no modelo)
```

---

## 2. Responsabilidades

### 2.1 O que o serviço DEVE fazer

| Ação | Detalhe |
|------|---------|
| Montar topologia AF | Árvore: reservatório → barrilete → colunas → ramais → sub-ramais |
| Montar topologia ES | Árvore invertida: aparelhos → ramais → subcoletores → TQ → coletor |
| Montar topologia VE | Ramais de ventilação → coluna VE → acima da cobertura |
| Gerar PipeSegments | Cada aresta do grafo = 1 PipeSegment com Start/End |
| Gerar Risers | Agrupar pontos alinhados verticalmente em prumadas |
| Calcular rotas | Determinar caminho entre pontos (menor distância + regras) |
| Montar mapa de adjacência | AdjacencyMap para navegação rápida |
| Validar conectividade | BFS para verificar grafo conexo |
| Detectar ciclos | Rede hidráulica deve ser árvore (sem loops) |
| Otimizar | Eliminar trechos redundantes, consolidar ramais |
| Exportar dados | Formato para Dynamo/unMEP/Revit Writer |

### 2.2 O que NÃO DEVE fazer

| Proibição | Quem faz |
|-----------|---------|
| ❌ Criar Pipes no Revit | RevitNetworkWriter (Infrastructure) |
| ❌ Executar scripts Dynamo | DynamoScriptExecutor (Infrastructure) |
| ❌ Usar unMEP | UnMepExecutor (Infrastructure) |
| ❌ Dimensionar (calcular DN) | ISizingService |
| ❌ Classificar ambientes | IRoomService |
| ❌ Gerar equipamentos | IEquipmentService |
| ❌ Manipular UI | Camada UI |

---

## 3. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Serviço de domínio para geração, organização, validação e otimização
    /// de redes hidráulicas (AF, ES, VE).
    /// Corresponde às etapas E06, E07, E08, E09.
    /// Independente do Revit.
    /// </summary>
    public interface INetworkService
    {
        // ── E06: Prumadas ───────────────────────────────────────

        /// <summary>
        /// Gera prumadas agrupando ambientes alinhados verticalmente.
        /// Cada prumada recebe um sistema, posição XY e derivações por andar.
        /// </summary>
        /// <param name="rooms">Ambientes molhados classificados.</param>
        /// <param name="system">Sistema hidráulico (AF, ES, VE).</param>
        /// <param name="toleranceM">Tolerância de alinhamento vertical em metros.</param>
        /// <returns>Resultado com lista de Risers gerados.</returns>
        RiserGenerationResult BuildRisers(
            List<RoomInfo> rooms,
            HydraulicSystem system,
            double toleranceM = 1.5);

        /// <summary>
        /// Gera todas as prumadas (AF + ES + VE) de uma vez.
        /// </summary>
        BatchRiserGenerationResult BuildAllRisers(List<RoomInfo> rooms);

        // ── E07: Rede de Água Fria ──────────────────────────────

        /// <summary>
        /// Constrói a rede de água fria completa.
        /// Topologia: árvore. Raiz = reservatório. Folhas = aparelhos.
        /// Etapas internas:
        ///   1. Criar ponto raiz (reservatório/barrilete)
        ///   2. Criar trechos de barrilete → colunas
        ///   3. Criar trechos de coluna → ramais
        ///   4. Criar trechos de ramal → sub-ramais
        ///   5. Montar adjacência
        /// </summary>
        /// <param name="input">Dados de entrada para geração AF.</param>
        /// <returns>PipeNetwork de água fria montado.</returns>
        NetworkBuildResult BuildColdWaterNetwork(ColdWaterNetworkInput input);

        // ── E08: Rede de Esgoto ─────────────────────────────────

        /// <summary>
        /// Constrói a rede de esgoto sanitário completa.
        /// Topologia: árvore invertida. Folhas = aparelhos. Raiz = saída predial.
        /// Etapas internas:
        ///   1. Criar ponto raiz (saída predial / caixa de inspeção)
        ///   2. Criar ramais de descarga (aparelho → CX sifonada / subcoletor)
        ///   3. Criar subcoletores (horizontais)
        ///   4. Criar tubos de queda (TQ — verticais)
        ///   5. Criar coletor predial
        ///   6. Montar adjacência
        /// </summary>
        NetworkBuildResult BuildSewerNetwork(SewerNetworkInput input);

        // ── E08b: Rede de Ventilação ────────────────────────────

        /// <summary>
        /// Constrói a rede de ventilação.
        /// Conecta pontos que requerem ventilação à coluna VE ou
        /// ao prolongamento do TQ acima da cobertura.
        /// </summary>
        NetworkBuildResult BuildVentilationNetwork(VentilationNetworkInput input);

        // ── Geração de trechos ──────────────────────────────────

        /// <summary>
        /// Gera trechos de tubulação entre uma lista de pontos.
        /// Determina rotas e cria PipeSegments com geometria.
        /// </summary>
        /// <param name="points">Pontos a conectar.</param>
        /// <param name="system">Sistema hidráulico.</param>
        /// <param name="routingStrategy">Estratégia de roteamento.</param>
        /// <returns>Lista de PipeSegments gerados.</returns>
        SegmentGenerationResult GenerateSegments(
            List<HydraulicPoint> points,
            HydraulicSystem system,
            RoutingStrategy routingStrategy = RoutingStrategy.ManhattanShortest);

        /// <summary>
        /// Cria um único PipeSegment entre dois pontos.
        /// </summary>
        PipeSegment CreateSegment(
            HydraulicPoint startPoint,
            HydraulicPoint endPoint,
            HydraulicSystem system,
            SegmentType type);

        /// <summary>
        /// Conecta uma lista de pontos em sequência (cadeia linear).
        /// Usado para ramais simples.
        /// </summary>
        List<PipeSegment> CreateSegmentChain(
            List<HydraulicPoint> orderedPoints,
            HydraulicSystem system,
            SegmentType type);

        // ── Validação ───────────────────────────────────────────

        /// <summary>
        /// Valida uma rede completa: conectividade, ciclos,
        /// referências cruzadas, regras por sistema.
        /// </summary>
        NetworkValidationResult ValidateNetwork(PipeNetwork network);

        /// <summary>
        /// Verifica se a rede está totalmente conectada (grafo conexo).
        /// </summary>
        bool IsFullyConnected(PipeNetwork network);

        /// <summary>
        /// Retorna pontos desconectados (sem trechos).
        /// </summary>
        List<HydraulicPoint> GetDisconnectedPoints(PipeNetwork network);

        /// <summary>
        /// Detecta ciclos na rede (não permitido em redes hidráulicas).
        /// </summary>
        bool HasCycles(PipeNetwork network);

        // ── Otimização ──────────────────────────────────────────

        /// <summary>
        /// Otimiza a rede: remove trechos redundantes, consolida ramais,
        /// reduz fittings desnecessários.
        /// </summary>
        NetworkOptimizationResult OptimizeNetwork(PipeNetwork network);

        // ── Consultas ───────────────────────────────────────────

        /// <summary>
        /// Encontra o caminho crítico (mais longo) da raiz até as folhas.
        /// AF: caminho com mais perda de carga.
        /// ES: caminho com mais UHC acumulado.
        /// </summary>
        CriticalPathResult FindCriticalPath(PipeNetwork network);

        /// <summary>
        /// Calcula o comprimento total de tubulação por DN.
        /// Para quantitativos.
        /// </summary>
        List<MaterialQuantity> CalculateQuantities(PipeNetwork network);

        /// <summary>
        /// Retorna estatísticas da rede.
        /// </summary>
        NetworkStatistics GetStatistics(PipeNetwork network);

        // ── Exportação ──────────────────────────────────────────

        /// <summary>
        /// Exporta a rede para formato de criação no Revit.
        /// Gera dados estruturados para RevitNetworkWriter, Dynamo ou unMEP.
        /// </summary>
        NetworkExportData ExportForRevit(PipeNetwork network);

        /// <summary>
        /// Serializa a rede como JSON (para debug, logs, persistência).
        /// </summary>
        string SerializeToJson(PipeNetwork network);

        /// <summary>
        /// Deserializa a rede de um JSON.
        /// </summary>
        PipeNetwork DeserializeFromJson(string json);

        // ── Pipeline Completo ───────────────────────────────────

        /// <summary>
        /// Pipeline completo para um sistema:
        /// BuildRisers → Build Network → Validate → Optimize.
        /// </summary>
        NetworkProcessingResult ProcessSystem(
            HydraulicSystem system,
            NetworkInputBase input);

        /// <summary>
        /// Pipeline completo para todos os sistemas (AF + ES + VE).
        /// </summary>
        FullNetworkProcessingResult ProcessAll(FullNetworkInput input);
    }
}
```

---

## 4. DTOs de Entrada

### 4.1 Inputs por sistema

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Base para inputs de geração de rede.
    /// </summary>
    public abstract class NetworkInputBase
    {
        /// <summary>Pontos hidráulicos a conectar.</summary>
        public List<HydraulicPoint> Points { get; set; } = new();

        /// <summary>Equipamentos associados.</summary>
        public List<EquipmentInfo> Equipment { get; set; } = new();

        /// <summary>Ambientes mapeados.</summary>
        public List<RoomInfo> Rooms { get; set; } = new();

        /// <summary>Prumadas já geradas (E06).</summary>
        public List<Riser> Risers { get; set; } = new();
    }

    /// <summary>
    /// Input para geração da rede de água fria.
    /// </summary>
    public class ColdWaterNetworkInput : NetworkInputBase
    {
        /// <summary>
        /// Elevação do reservatório em metros.
        /// Define a pressão disponível no sistema.
        /// </summary>
        public double ReservoirElevationM { get; set; }

        /// <summary>
        /// Nível d'água no reservatório em metros (acima da elevação base).
        /// </summary>
        public double WaterLevelM { get; set; } = 1.0;

        /// <summary>
        /// Se o sistema tem pressurizador.
        /// </summary>
        public bool HasPressureBooster { get; set; }

        /// <summary>
        /// Pressão adicional do pressurizador em mca.
        /// </summary>
        public double BoosterPressureMca { get; set; }

        /// <summary>
        /// Posição do barrilete (X, Y, Z) em metros.
        /// Ponto de partida da rede.
        /// </summary>
        public Point3D BarrelPosition { get; set; }

        /// <summary>
        /// Estratégia de roteamento.
        /// </summary>
        public RoutingStrategy Strategy { get; set; } = RoutingStrategy.ManhattanShortest;
    }

    /// <summary>
    /// Input para geração da rede de esgoto.
    /// </summary>
    public class SewerNetworkInput : NetworkInputBase
    {
        /// <summary>
        /// Posição da saída predial (caixa de inspeção final).
        /// Ponto de destino da rede (raiz do grafo).
        /// </summary>
        public Point3D BuildingOutletPosition { get; set; }

        /// <summary>
        /// Elevação da saída predial em metros.
        /// </summary>
        public double OutletElevationM { get; set; }

        /// <summary>
        /// Declividade mínima padrão para trechos horizontais em %.
        /// DN ≤ 75 → 2%, DN ≥ 100 → 1%.
        /// </summary>
        public double DefaultMinSlopePercent { get; set; } = 1.0;

        /// <summary>
        /// Se deve criar caixa de gordura para cozinha.
        /// </summary>
        public bool RequiresGreaseBox { get; set; } = true;
    }

    /// <summary>
    /// Input para geração da rede de ventilação.
    /// </summary>
    public class VentilationNetworkInput : NetworkInputBase
    {
        /// <summary>
        /// Rede de esgoto associada (para identificar pontos que precisam de ventilação).
        /// </summary>
        public PipeNetwork SewerNetwork { get; set; }

        /// <summary>
        /// Elevação mínima do topo das colunas de ventilação (acima da cobertura).
        /// </summary>
        public double MinVentTopElevationM { get; set; }

        /// <summary>
        /// DN mínimo para ramais de ventilação.
        /// </summary>
        public int MinVentDiameterMm { get; set; } = 40;
    }

    /// <summary>
    /// Input para processamento completo (todos os sistemas).
    /// </summary>
    public class FullNetworkInput
    {
        public ColdWaterNetworkInput ColdWater { get; set; }
        public SewerNetworkInput Sewer { get; set; }
        public VentilationNetworkInput Ventilation { get; set; }
    }
}
```

### 4.2 Enums auxiliares

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Estratégia de roteamento para geração de trechos.
    /// </summary>
    public enum RoutingStrategy
    {
        /// <summary>
        /// Caminho mais curto em Manhattan (ortogonal: só X, Y, Z).
        /// Mais realista para tubulações.
        /// </summary>
        ManhattanShortest = 1,

        /// <summary>
        /// Caminho mais curto euclidiano.
        /// Pode gerar trechos inclinados.
        /// </summary>
        EuclideanShortest = 2,

        /// <summary>
        /// Sempre seguir paredes (X ou Y alinhado com as bordas do ambiente).
        /// Mais limpo visualmente.
        /// </summary>
        WallFollowing = 3,

        /// <summary>
        /// Agrupamento central: coletar tudo em um ponto central do ambiente.
        /// Bom para CX sifonada.
        /// </summary>
        CentralCollection = 4
    }
}
```

---

## 5. DTOs de Resultado

### 5.1 NetworkBuildResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da construção de uma rede.
    /// </summary>
    public class NetworkBuildResult
    {
        /// <summary>Rede gerada.</summary>
        public PipeNetwork Network { get; set; }

        /// <summary>Se foi bem-sucedido.</summary>
        public bool IsSuccessful { get; set; }

        /// <summary>Erros durante a construção.</summary>
        public List<NetworkBuildError> Errors { get; set; } = new();

        /// <summary>Avisos (não impeditivos).</summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>Estatísticas da rede gerada.</summary>
        public NetworkStatistics Statistics { get; set; }

        /// <summary>Tempo de execução.</summary>
        public TimeSpan ExecutionTime { get; set; }

        public string GetSummary() =>
            $"{Network?.System}: {Network?.SegmentCount ?? 0} trechos, " +
            $"{Network?.RiserCount ?? 0} prumadas, " +
            $"{Network?.TotalPipeLengthM:F1}m total. " +
            $"{(IsSuccessful ? "✅" : "❌")} Tempo: {ExecutionTime.TotalMilliseconds:F0}ms";
    }

    public class NetworkBuildError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public string PointId { get; set; }
        public string SegmentId { get; set; }
    }
}
```

### 5.2 RiserGenerationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class RiserGenerationResult
    {
        public List<Riser> Risers { get; set; } = new();
        public HydraulicSystem System { get; set; }
        public int TotalRisers => Risers.Count;
        public int TotalFloors => Risers.Sum(r => r.GetFloorCount());
        public bool IsSuccessful => Risers.Count > 0;
        public TimeSpan ExecutionTime { get; set; }
    }

    public class BatchRiserGenerationResult
    {
        public RiserGenerationResult ColdWater { get; set; }
        public RiserGenerationResult Sewer { get; set; }
        public RiserGenerationResult Ventilation { get; set; }
        public int TotalRisers =>
            (ColdWater?.TotalRisers ?? 0) +
            (Sewer?.TotalRisers ?? 0) +
            (Ventilation?.TotalRisers ?? 0);
        public TimeSpan TotalExecutionTime { get; set; }
    }
}
```

### 5.3 SegmentGenerationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class SegmentGenerationResult
    {
        public List<PipeSegment> Segments { get; set; } = new();
        public int TotalGenerated => Segments.Count;
        public double TotalLengthM => Segments.Sum(s => s.LengthM);
        public List<string> Errors { get; set; } = new();
        public bool IsSuccessful => Errors.Count == 0;
        public TimeSpan ExecutionTime { get; set; }
    }
}
```

### 5.4 NetworkValidationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da validação de uma rede.
    /// </summary>
    public class NetworkValidationResult
    {
        public string NetworkId { get; set; }
        public HydraulicSystem System { get; set; }
        public bool IsValid { get; set; }

        /// <summary>A rede é um grafo conexo?</summary>
        public bool IsConnected { get; set; }

        /// <summary>A rede tem ciclos? (deve ser false).</summary>
        public bool HasCycles { get; set; }

        /// <summary>Há pontos desconectados?</summary>
        public int DisconnectedPointCount { get; set; }

        /// <summary>Há referências cruzadas inválidas?</summary>
        public int BrokenReferenceCount { get; set; }

        /// <summary>Lista de problemas encontrados.</summary>
        public List<NetworkValidationIssue> Issues { get; set; } = new();

        public int CriticalCount => Issues.Count(i => i.Level == ValidationLevel.Critical);
        public int MediumCount => Issues.Count(i => i.Level == ValidationLevel.Medium);
    }

    public class NetworkValidationIssue
    {
        public ValidationLevel Level { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string ElementId { get; set; }
        public string NormReference { get; set; }
    }
}
```

### 5.5 NetworkOptimizationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class NetworkOptimizationResult
    {
        public PipeNetwork OptimizedNetwork { get; set; }
        public int SegmentsRemoved { get; set; }
        public int SegmentsMerged { get; set; }
        public int FittingsRemoved { get; set; }
        public double LengthSavedM { get; set; }
        public string GetSummary() =>
            $"Otimização: -{SegmentsRemoved} trechos, -{FittingsRemoved} fittings, " +
            $"-{LengthSavedM:F1}m";
    }
}
```

### 5.6 CriticalPathResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class CriticalPathResult
    {
        /// <summary>IDs dos segmentos no caminho crítico (em ordem).</summary>
        public List<string> SegmentIds { get; set; } = new();

        /// <summary>IDs dos pontos no caminho (em ordem).</summary>
        public List<string> PointIds { get; set; } = new();

        /// <summary>Comprimento total do caminho em metros.</summary>
        public double TotalLengthM { get; set; }

        /// <summary>Perda de carga acumulada no caminho em mca.</summary>
        public double TotalHeadLossMca { get; set; }

        /// <summary>ID do ponto mais desfavorável (final do caminho).</summary>
        public string CriticalPointId { get; set; }

        /// <summary>Pressão disponível no ponto crítico em mca (AF).</summary>
        public double CriticalPressureMca { get; set; }

        /// <summary>Soma de pesos/UHCs no caminho.</summary>
        public double AccumulatedWeight { get; set; }
    }
}
```

### 5.7 NetworkStatistics

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class NetworkStatistics
    {
        public string NetworkId { get; set; }
        public HydraulicSystem System { get; set; }
        public int PointCount { get; set; }
        public int SegmentCount { get; set; }
        public int RiserCount { get; set; }
        public int FittingCount { get; set; }
        public int FloorCount { get; set; }
        public double TotalPipeLengthM { get; set; }
        public double TotalWeightAF { get; set; }
        public int TotalUHC { get; set; }
        public double MaxElevationM { get; set; }
        public double MinElevationM { get; set; }
        public int MaxDiameterMm { get; set; }
        public int MinDiameterMm { get; set; }

        public Dictionary<SegmentType, int> CountBySegmentType { get; set; } = new();
        public Dictionary<int, double> LengthByDiameterMm { get; set; } = new();

        public override string ToString() =>
            $"{System}: {PointCount} pts, {SegmentCount} segs, " +
            $"{RiserCount} risers, {TotalPipeLengthM:F1}m total";
    }
}
```

### 5.8 NetworkExportData

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Dados estruturados para criar a rede no Revit.
    /// Consumido por RevitNetworkWriter, DynamoScriptExecutor ou UnMepExecutor.
    /// </summary>
    public class NetworkExportData
    {
        public string NetworkId { get; set; }
        public HydraulicSystem System { get; set; }

        /// <summary>Nome do PipingSystemType no Revit.</summary>
        public string RevitSystemTypeName { get; set; }

        /// <summary>Nome do PipeType no Revit.</summary>
        public string RevitPipeTypeName { get; set; }

        /// <summary>
        /// Segmentos a criar (cada um vira 1 Pipe no Revit).
        /// Contém: StartXYZ, EndXYZ, DN, Level.
        /// </summary>
        public List<SegmentExportData> Segments { get; set; } = new();

        /// <summary>
        /// Fittings a criar (cada um vira 1 PipeFitting no Revit).
        /// </summary>
        public List<FittingExportData> Fittings { get; set; } = new();

        /// <summary>Método de execução recomendado.</summary>
        public ExecutionMethod RecommendedMethod { get; set; }
    }

    public class SegmentExportData
    {
        public string SegmentId { get; set; }
        public Point3D StartPosition { get; set; }
        public Point3D EndPosition { get; set; }
        public int DiameterMm { get; set; }
        public string LevelName { get; set; }
        public double SlopePercent { get; set; }
        public SegmentType Type { get; set; }
    }

    public class FittingExportData
    {
        public FittingType Type { get; set; }
        public Point3D Position { get; set; }
        public int DiameterMm { get; set; }
        public double AngleDegrees { get; set; }
        public string LevelName { get; set; }
    }

    public enum ExecutionMethod
    {
        /// <summary>Criação direta via Revit API (RevitNetworkWriter).</summary>
        DirectApi = 1,

        /// <summary>Via Dynamo script (.dyn).</summary>
        DynamoScript = 2,

        /// <summary>Via unMEP routing.</summary>
        UnMepRouting = 3
    }
}
```

### 5.9 Pipeline Results

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class NetworkProcessingResult
    {
        public HydraulicSystem System { get; set; }
        public RiserGenerationResult Risers { get; set; }
        public NetworkBuildResult Build { get; set; }
        public NetworkValidationResult Validation { get; set; }
        public NetworkOptimizationResult Optimization { get; set; }

        public bool IsSuccessful =>
            Build.IsSuccessful && Validation.IsValid;

        public PipeNetwork Network => Optimization?.OptimizedNetwork ?? Build?.Network;
        public TimeSpan TotalExecutionTime { get; set; }

        public string GetSummary() =>
            $"{System}: {Build?.GetSummary()} " +
            $"Validação: {(Validation?.IsValid == true ? "✅" : "❌")} " +
            $"Otimização: {Optimization?.GetSummary() ?? "N/A"}";
    }

    public class FullNetworkProcessingResult
    {
        public NetworkProcessingResult ColdWater { get; set; }
        public NetworkProcessingResult Sewer { get; set; }
        public NetworkProcessingResult Ventilation { get; set; }

        public bool IsSuccessful =>
            (ColdWater?.IsSuccessful ?? true) &&
            (Sewer?.IsSuccessful ?? true) &&
            (Ventilation?.IsSuccessful ?? true);

        public TimeSpan TotalExecutionTime { get; set; }

        public string GetSummary() =>
            $"AF: {ColdWater?.GetSummary()}\n" +
            $"ES: {Sewer?.GetSummary()}\n" +
            $"VE: {Ventilation?.GetSummary()}\n" +
            $"Total: {TotalExecutionTime.TotalSeconds:F1}s";
    }
}
```

---

## 6. Regras de Geração por Sistema

### 6.1 Água Fria — Topologia

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Regras de construção da rede AF.
    /// Topologia: árvore do reservatório para os aparelhos.
    /// </summary>
    public static class ColdWaterNetworkRules
    {
        /// <summary>
        /// Hierarquia de conexão AF (de cima para baixo):
        ///
        /// Reservatório
        ///    └── Barrilete (BarrelPipe)
        ///            ├── Coluna AF-01 (DistributionColumn / Riser)
        ///            │       ├── Ramal 1º Pav (Branch)
        ///            │       │       ├── Sub-ramal → Lavatório (SubBranch)
        ///            │       │       ├── Sub-ramal → Chuveiro (SubBranch)
        ///            │       │       └── Sub-ramal → Vaso (SubBranch)
        ///            │       └── Ramal Térreo (Branch)
        ///            │               └── ...
        ///            └── Coluna AF-02 (DistributionColumn / Riser)
        ///                    └── Ramal Coz/Serv (Branch)
        ///                            └── Sub-ramal → Pia (SubBranch)
        /// </summary>
        public static SegmentType GetSegmentType(string fromPointType, string toPointType)
        {
            // Reservatório → Barrilete
            // Barrilete → Coluna = BarrelPipe
            // Coluna → Derivação = DistributionColumn (dentro do Riser)
            // Derivação → Ramal = Branch
            // Ramal → Aparelho = SubBranch
            return SegmentType.SubBranch; // simplificado
        }

        /// <summary>
        /// Pressão disponível no topo do reservatório em mca.
        /// P = (elevação reservatório + nível d'água) - elevação do ponto.
        /// </summary>
        public static double CalculateStaticPressure(
            double reservoirElevationM, double waterLevelM, double pointElevationM)
        {
            return (reservoirElevationM + waterLevelM) - pointElevationM;
        }

        /// <summary>
        /// Pressão mínima por tipo de aparelho (mca).
        /// </summary>
        public static double GetMinPressure(EquipmentType type, FlushType flush)
        {
            if (type == EquipmentType.ToiletFlushValve) return 1.2;
            if (type == EquipmentType.Shower) return 1.0;
            return 0.5; // padrão
        }

        /// <summary>
        /// Velocidade máxima AF = 3.0 m/s (NBR 5626 §5.4.2.3).
        /// </summary>
        public const double MaxVelocityMs = 3.0;

        /// <summary>
        /// Velocidade mínima AF = 0.5 m/s (prevenir sedimentação).
        /// </summary>
        public const double MinVelocityMs = 0.5;
    }
}
```

### 6.2 Esgoto — Topologia

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Regras de construção da rede ES.
    /// Topologia: árvore invertida dos aparelhos para a saída predial.
    /// </summary>
    public static class SewerNetworkRules
    {
        /// <summary>
        /// Hierarquia de conexão ES (de cima para baixo no grafo):
        ///
        /// Lavatório → ramal DN 40 ─┐
        /// Chuveiro  → ramal DN 40 ─┼── CX Sifonada → subcoletor DN 75
        /// Ralo      → ramal DN 40 ─┘                         │
        /// Vaso      → ramal DN 100 ──────────────────── junção
        ///                                                     │
        ///                                             tubo de queda (TQ) DN 100
        ///                                                     │
        ///                                             subcoletor DN 100
        ///                                                     │
        ///                                             coletor predial DN 100/150
        ///                                                     │
        ///                                             saída predial
        /// </summary>

        /// <summary>
        /// Declividade mínima por DN (NBR 8160 §5.1.4).
        /// </summary>
        public static double GetMinSlope(int diameterMm)
        {
            return diameterMm >= 100 ? 1.0 : 2.0;
        }

        /// <summary>
        /// DN mínimo do ramal de descarga por equipamento (NBR 8160 Tab.3).
        /// </summary>
        public static int GetMinDischargeDN(EquipmentType type)
        {
            return type switch
            {
                EquipmentType.ToiletCoupledTank => 100,
                EquipmentType.ToiletFlushValve => 100,
                EquipmentType.KitchenSink => 50,
                EquipmentType.WashingMachine => 50,
                EquipmentType.Dishwasher => 50,
                _ => 40
            };
        }

        /// <summary>
        /// Equipamentos que devem se conectar via caixa sifonada (não direto no TQ).
        /// </summary>
        public static bool ConnectsViaSiphonBox(EquipmentType type)
        {
            return type is EquipmentType.Sink or EquipmentType.Shower
                        or EquipmentType.FloorDrain or EquipmentType.Bidet
                        or EquipmentType.Bathtub or EquipmentType.LaundryTub;
        }

        /// <summary>
        /// Equipamentos com ramal de descarga independente (direto no subcoletor/TQ).
        /// </summary>
        public static bool RequiresIndependentBranch(EquipmentType type)
        {
            return type is EquipmentType.ToiletCoupledTank
                        or EquipmentType.ToiletFlushValve;
        }

        /// <summary>
        /// DN do coletor predial: NUNCA menor que o maior TQ.
        /// </summary>
        public const int MinBuildingCollectorDN = 100;

        /// <summary>
        /// Taxa máxima de ocupação = 75% (NBR 8160 §5.1.3).
        /// </summary>
        public const double MaxOccupancyPercent = 75.0;
    }
}
```

### 6.3 Ventilação — Regras

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Regras de ventilação (NBR 8160).
    /// </summary>
    public static class VentilationNetworkRules
    {
        /// <summary>
        /// Pontos que obrigatoriamente precisam de ventilação.
        /// </summary>
        public static bool RequiresVentilation(EquipmentType type)
        {
            return type is EquipmentType.ToiletCoupledTank
                        or EquipmentType.ToiletFlushValve;
        }

        /// <summary>
        /// DN mínimo do ramal de ventilação = 2/3 do DN do ramal de descarga.
        /// Mínimo absoluto = 40mm.
        /// </summary>
        public static int GetMinVentDN(int dischargeDN)
        {
            int calculated = (int)(dischargeDN * 2.0 / 3.0);
            int[] commercial = { 40, 50, 75, 100 };
            foreach (int dn in commercial)
                if (dn >= calculated) return dn;
            return 40;
        }

        /// <summary>
        /// Ventilação primária: prolongamento do TQ acima da cobertura.
        /// DN = mesmo do TQ. Obrigatório.
        /// </summary>
        public static int GetPrimaryVentDN(int dropPipeDN) => dropPipeDN;

        /// <summary>
        /// Ventilação secundária necessária quando:
        /// TQ atende ≥ 3 pavimentos OU comprimento do ramal > 2.4m.
        /// </summary>
        public static bool NeedsSecondaryVent(int floorCount, double branchLengthM)
        {
            return floorCount >= 3 || branchLengthM > 2.4;
        }

        /// <summary>
        /// Distância máxima entre o ponto de ventilação e o sifão: 2.4m (NBR 8160).
        /// </summary>
        public const double MaxVentDistanceM = 2.4;

        /// <summary>
        /// Altura mínima da ventilação primária acima do telhado: 0.30m.
        /// </summary>
        public const double MinVentAboveRoofM = 0.30;
    }
}
```

---

## 7. Exemplo de Uso (no Orchestrator)

```csharp
public class PipelineOrchestrator
{
    private readonly IRoomService _roomService;
    private readonly IEquipmentService _equipmentService;
    private readonly INetworkService _networkService;

    public async Task ExecuteE06_E08(
        List<RoomInfo> wetAreas,
        List<EquipmentInfo> equipment,
        List<HydraulicPoint> points)
    {
        // ── OPÇÃO 1: Pipeline completo ──────────────────────────
        var input = new FullNetworkInput
        {
            ColdWater = new ColdWaterNetworkInput
            {
                Points = points.Where(p => p.System == HydraulicSystem.ColdWater).ToList(),
                Equipment = equipment,
                Rooms = wetAreas,
                ReservoirElevationM = 9.0,
                WaterLevelM = 1.0,
                BarrelPosition = new Point3D(5.0, 4.0, 9.5)
            },
            Sewer = new SewerNetworkInput
            {
                Points = points.Where(p => p.System == HydraulicSystem.Sewer).ToList(),
                Equipment = equipment,
                Rooms = wetAreas,
                BuildingOutletPosition = new Point3D(0.0, 0.0, -0.5),
                OutletElevationM = -0.5
            },
            Ventilation = new VentilationNetworkInput
            {
                Points = points.Where(p => p.System == HydraulicSystem.Ventilation).ToList(),
                MinVentTopElevationM = 10.0
            }
        };

        var result = _networkService.ProcessAll(input);
        Console.WriteLine(result.GetSummary());

        if (!result.IsSuccessful)
        {
            Console.WriteLine("❌ Falha na geração de redes");
            return;
        }

        // Redes prontas para dimensionamento (E11)
        var networkAF = result.ColdWater.Network;
        var networkES = result.Sewer.Network;
        var networkVE = result.Ventilation.Network;

        // Quantitativos
        var qtdAF = _networkService.CalculateQuantities(networkAF);
        foreach (var item in qtdAF)
            Console.WriteLine($"  AF DN{item.DiameterMm}: {item.TotalLengthM:F1}m");

        // Exportar para Revit
        var exportAF = _networkService.ExportForRevit(networkAF);
        // → enviar para RevitNetworkWriter / DynamoScriptExecutor
    }
}
```

---

## 8. Resumo Visual

```
INetworkService
│
├── Prumadas (E06)
│   ├── BuildRisers(rooms, system, tolerance) → RiserGenerationResult
│   └── BuildAllRisers(rooms) → BatchRiserGenerationResult
│
├── Redes (E07/E08)
│   ├── BuildColdWaterNetwork(input) → NetworkBuildResult
│   ├── BuildSewerNetwork(input) → NetworkBuildResult
│   └── BuildVentilationNetwork(input) → NetworkBuildResult
│
├── Trechos
│   ├── GenerateSegments(points, system, strategy) → SegmentGenerationResult
│   ├── CreateSegment(start, end, system, type) → PipeSegment
│   └── CreateSegmentChain(orderedPoints, system, type) → List<PipeSegment>
│
├── Validação
│   ├── ValidateNetwork(network) → NetworkValidationResult
│   ├── IsFullyConnected(network) → bool
│   ├── GetDisconnectedPoints(network) → List<HydraulicPoint>
│   └── HasCycles(network) → bool
│
├── Otimização
│   └── OptimizeNetwork(network) → NetworkOptimizationResult
│
├── Consultas
│   ├── FindCriticalPath(network) → CriticalPathResult
│   ├── CalculateQuantities(network) → List<MaterialQuantity>
│   └── GetStatistics(network) → NetworkStatistics
│
├── Exportação
│   ├── ExportForRevit(network) → NetworkExportData
│   ├── SerializeToJson(network) → string
│   └── DeserializeFromJson(json) → PipeNetwork
│
├── Pipeline
│   ├── ProcessSystem(system, input) → NetworkProcessingResult
│   └── ProcessAll(fullInput) → FullNetworkProcessingResult
│
├── Inputs
│   ├── ColdWaterNetworkInput (reservatório, barrilete, pressurizador)
│   ├── SewerNetworkInput (saída predial, declividade, CX gordura)
│   ├── VentilationNetworkInput (rede ES, elevação topo, DN mín)
│   └── FullNetworkInput (todos)
│
├── Regras
│   ├── ColdWaterNetworkRules (pressão, velocidade, hierarquia AF)
│   ├── SewerNetworkRules (declividade, DN, CX sifonada, TQ)
│   └── VentilationNetworkRules (DN vent, distância, secundária)
│
└── Dependências
    ├── HydraulicPoint, PipeSegment, Riser, PipeNetwork (modelos)
    ├── EquipmentInfo, RoomInfo (inputs)
    └── ISizingService (próximo — dimensiona a rede gerada)
```
