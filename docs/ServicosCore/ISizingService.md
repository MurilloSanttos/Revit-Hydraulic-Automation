# Serviço de Domínio — ISizingService (IDimensionamentoService)

> Especificação completa da interface de serviço responsável por todos os cálculos hidráulicos do sistema (vazão, diâmetro, pressão, velocidade, perda de carga, declividade), totalmente agnóstica ao Revit, para uso no PluginCore.

---

## 1. Definição da Interface

### 1.1 O que é ISizingService

`ISizingService` é o **motor de cálculo** do PluginCore. Ele recebe componentes da rede (trechos, prumadas, sistemas), aplica fórmulas normativas (NBR 5626, NBR 8160), e retorna `SizingResult` com valores calculados, conformidade verificada e alertas gerados.

### 1.2 Papel no sistema

```
INetworkService (E06-E09)
    │
    └── PipeNetwork (grafo montado, sem DN)
            │
            ╔══════════════════╗
            ║  ISizingService  ║  ← ESTE SERVIÇO (Core)
            ╠══════════════════╣
            ║ SizeSegment()    ║  → DN + V + J + ΔP
            ║ SizeRiser()      ║  → DN da coluna
            ║ SizeNetwork()    ║  → Rede dimensionada
            ║ CheckPressure()  ║  → P ≥ P_min?
            ║ CheckSlope()     ║  → i ≥ i_min?
            ╚═══════╤══════════╝
                    │
            PipeNetwork (com SizingResult em cada trecho)
                    │
            └── ExportForRevit (E10) / Tabelas (E12)
```

| Etapa | Método usado |
|-------|-------------|
| E11 — Dimensionamento | `SizeNetwork()` → dimensiona rede inteira |
| E11a — Trechos | `SizeSegment()` → DN + V + J por trecho |
| E11b — Prumadas | `SizeRiser()` → DN da coluna vertical |
| E05 — Validação | `CheckPressure()`, `CheckSlope()` → conformidade |
| E12 — Tabelas | Resultados usados para preenchimento de tabelas |
| Pipeline | `ProcessNetwork()` → pipeline Size→Check→Report |

### 1.3 Por que é independente do Revit

```
ESTE SERVIÇO:
  - Recebe PipeSegment, Riser, PipeNetwork (agnósticos)
  - Aplica fórmulas matemáticas puras
  - Retorna SizingResult (agnóstico)
  - ZERO dependência de qualquer API externa

TESTABILIDADE MÁXIMA:
  - Entrada: valores numéricos (ΣP, ΣUHC, L, C, D)
  - Saída: valores numéricos (Q, DN, V, J, ΔP)
  - Pode ser testado com dados mock em 100% dos cenários
  - Cada fórmula pode ter test unitário isolado
```

---

## 2. Responsabilidades

### 2.1 O que o serviço DEVE fazer

| Ação | Fórmula/Critério |
|------|-----------------|
| Calcular vazão provável AF | Q = 0.3 × √ΣP (NBR 5626) |
| Calcular vazão ES | Tabela UHC → Q (NBR 8160) |
| Determinar DN teórico | Fair-Whipple-Hsiao invertida |
| Arredondar para DN comercial | Próximo DN ≥ DN_teórico |
| Calcular velocidade | V = Q / A |
| Calcular perda de carga unitária | Hazen-Williams: J = f(Q, C, D) |
| Calcular perda de carga total | ΔP = J × (L + Leq) |
| Calcular pressão disponível | P_fim = P_início - ΔP + ΔZ |
| Verificar velocidade | 0.5 ≤ V ≤ 3.0 m/s (AF) |
| Verificar pressão | P_fim ≥ P_min_aparelho (AF) |
| Verificar declividade | i ≥ 1% (DN≥100) ou 2% (DN≤75) (ES) |
| Verificar ocupação | Lâmina ≤ 75% (ES) |
| Gerar alertas | Warnings com código + referência NBR |
| Acumular pesos/UHCs | Top-down na árvore da rede |
| Encontrar caminho crítico | Ponto mais desfavorável (AF) |

### 2.2 O que NÃO DEVE fazer

| Proibição | Quem faz |
|-----------|---------|
| ❌ Montar a topologia da rede | INetworkService |
| ❌ Gerar equipamentos | IEquipmentService |
| ❌ Classificar ambientes | IRoomService |
| ❌ Criar Pipes no Revit | RevitNetworkWriter (Infrastructure) |
| ❌ Armazenar resultados em disco | DataService (Data) |
| ❌ Manipular UI | Camada UI |

---

## 3. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Serviço de domínio para cálculos hidráulicos.
    /// Motor normativo do sistema — aplica NBR 5626 e NBR 8160.
    /// Corresponde à etapa E11.
    /// Independente do Revit.
    /// </summary>
    public interface ISizingService
    {
        // ══════════════════════════════════════════════════════════
        //  DIMENSIONAMENTO POR COMPONENTE
        // ══════════════════════════════════════════════════════════

        // ── Trecho ──────────────────────────────────────────────

        /// <summary>
        /// Dimensiona um trecho de água fria.
        /// Calcula: Q → DN → V → J → ΔP → P_fim.
        /// </summary>
        /// <param name="segment">Trecho a dimensionar.</param>
        /// <param name="pressureStartMca">Pressão disponível no início do trecho (mca).</param>
        /// <returns>Resultado completo do dimensionamento.</returns>
        SizingResult SizeSegmentColdWater(PipeSegment segment, double pressureStartMca);

        /// <summary>
        /// Dimensiona um trecho de esgoto sanitário.
        /// Calcula: ΣUHC → DN (tabela) → V (Manning) → i% → ocupação.
        /// </summary>
        SizingResult SizeSegmentSewer(PipeSegment segment);

        /// <summary>
        /// Dimensiona um trecho de ventilação.
        /// DN = f(DN_esgoto associado). Mínimo 40mm.
        /// </summary>
        SizingResult SizeSegmentVentilation(PipeSegment segment, int associatedSewerDN);

        /// <summary>
        /// Dimensiona um trecho (auto-detecta o sistema).
        /// </summary>
        SizingResult SizeSegment(PipeSegment segment, SizingContext context);

        // ── Prumada ─────────────────────────────────────────────

        /// <summary>
        /// Dimensiona uma prumada completa.
        /// Recalcula acumulados por andar, define DN de cada trecho vertical.
        /// AF: ΣP acumulado → DN.
        /// ES: ΣUHC acumulado → DN (tabela TQ).
        /// </summary>
        RiserSizingResult SizeRiser(Riser riser);

        // ── Rede completa ───────────────────────────────────────

        /// <summary>
        /// Dimensiona uma rede completa.
        /// Percorre o grafo da raiz às folhas (AF) ou das folhas à raiz (ES),
        /// acumulando pesos/UHCs e dimensionando cada trecho.
        /// </summary>
        NetworkSizingResult SizeNetwork(PipeNetwork network);

        // ══════════════════════════════════════════════════════════
        //  CÁLCULOS INDIVIDUAIS (BUILDING BLOCKS)
        // ══════════════════════════════════════════════════════════

        // ── Vazão ───────────────────────────────────────────────

        /// <summary>
        /// Calcula a vazão provável de AF.
        /// Q = 0.3 × √ΣP (L/s).
        /// </summary>
        /// <param name="totalWeight">Soma dos pesos dos aparelhos (ΣP).</param>
        /// <returns>Vazão em L/s.</returns>
        double CalculateProbableFlow(double totalWeight);

        /// <summary>
        /// Calcula a vazão de ES a partir de UHCs.
        /// Simplificação: Q = 0.3 × √ΣUHC (L/s).
        /// Em produção: tabela NBR 8160.
        /// </summary>
        double CalculateSewerFlow(int totalUHC);

        /// <summary>
        /// Calcula a vazão máxima que um DN suporta a uma velocidade limite.
        /// Q_max = V_max × A = V_max × π × (DI/2)².
        /// </summary>
        double CalculateMaxFlow(int diameterMm, PipeMaterial material,
            double maxVelocityMs = 3.0);

        // ── Diâmetro ────────────────────────────────────────────

        /// <summary>
        /// Calcula o DN teórico para uma vazão AF.
        /// Fórmula invertida de Fair-Whipple-Hsiao:
        /// D = (8.69e6 × Q^1.75 / J_max)^(1/4.75)
        /// </summary>
        /// <param name="flowRateLs">Vazão em L/s.</param>
        /// <param name="maxUnitaryHeadLoss">J máxima admissível em m/m (padrão: 0.08).</param>
        /// <returns>Diâmetro teórico em mm.</returns>
        double CalculateTheoreticalDiameter(double flowRateLs,
            double maxUnitaryHeadLoss = 0.08);

        /// <summary>
        /// Arredonda DN teórico para o próximo DN comercial.
        /// </summary>
        int GetCommercialDiameter(double theoreticalDiameterMm,
            HydraulicSystem system);

        /// <summary>
        /// Retorna o DI (diâmetro interno) de um DN comercial.
        /// Depende do material.
        /// </summary>
        double GetInternalDiameter(int nominalDN, PipeMaterial material);

        /// <summary>
        /// Dimensiona DN por tabela de UHC (esgoto).
        /// </summary>
        int GetDiameterByUHC(int totalUHC, SegmentType segmentType);

        /// <summary>
        /// Retorna o DN do ramal de ventilação.
        /// DN_vent ≥ 2/3 × DN_descarga. Mínimo 40mm.
        /// </summary>
        int GetVentilationDiameter(int dischargeDN);

        // ── Velocidade ──────────────────────────────────────────

        /// <summary>
        /// Calcula a velocidade.
        /// V = Q / A = Q / (π × (DI/2)²).
        /// </summary>
        /// <param name="flowRateLs">Vazão em L/s.</param>
        /// <param name="internalDiameterMm">DI em mm.</param>
        /// <returns>Velocidade em m/s.</returns>
        double CalculateVelocity(double flowRateLs, double internalDiameterMm);

        /// <summary>
        /// Calcula a velocidade por Manning (ES — escoamento livre).
        /// V = (1/n) × R^(2/3) × i^(1/2).
        /// </summary>
        /// <param name="internalDiameterMm">DI em mm.</param>
        /// <param name="slopePercent">Declividade em %.</param>
        /// <param name="manningN">Coeficiente de Manning (PVC = 0.010).</param>
        /// <returns>Velocidade em m/s.</returns>
        double CalculateManningVelocity(double internalDiameterMm,
            double slopePercent, double manningN = 0.010);

        // ── Perda de carga ──────────────────────────────────────

        /// <summary>
        /// Calcula a perda de carga unitária por Hazen-Williams.
        /// J = 10.643 × Q^1.85 / (C^1.85 × D^4.87) (m/m).
        /// </summary>
        /// <param name="flowRateM3s">Vazão em m³/s.</param>
        /// <param name="hazenC">Coeficiente C de Hazen-Williams.</param>
        /// <param name="internalDiameterM">DI em metros.</param>
        /// <returns>Perda de carga unitária em m/m.</returns>
        double CalculateHazenWilliams(double flowRateM3s,
            double hazenC, double internalDiameterM);

        /// <summary>
        /// Calcula a perda de carga unitária por Fair-Whipple-Hsiao.
        /// J = 8.69 × 10^6 × Q^1.75 / D^4.75 (m/m).
        /// Válida para tubos lisos (PVC).
        /// </summary>
        double CalculateFairWhippleHsiao(double flowRateM3s,
            double internalDiameterM);

        /// <summary>
        /// Calcula a perda de carga total no trecho.
        /// ΔP = J × (L_real + L_equivalente).
        /// </summary>
        double CalculateTotalHeadLoss(double unitaryHeadLoss,
            double realLengthM, double equivalentLengthM);

        /// <summary>
        /// Calcula o comprimento equivalente total dos fittings de um trecho.
        /// Soma dos Leq de cada fitting.
        /// </summary>
        double CalculateEquivalentLength(List<FittingInfo> fittings);

        // ── Pressão ─────────────────────────────────────────────

        /// <summary>
        /// Calcula a pressão disponível no final de um trecho AF.
        /// P_fim = P_início - ΔP_perda + ΔZ.
        /// ΔZ positivo = fluxo descendente (a favor da gravidade).
        /// </summary>
        /// <param name="pressureStartMca">Pressão no início em mca.</param>
        /// <param name="headLossMca">Perda de carga total em mca.</param>
        /// <param name="elevationDiffM">Desnível Z (positivo = desce).</param>
        /// <returns>Pressão disponível no fim em mca.</returns>
        double CalculateEndPressure(double pressureStartMca,
            double headLossMca, double elevationDiffM);

        /// <summary>
        /// Calcula a pressão estática no sistema AF.
        /// P_estática = (Z_reservatório + nível_água) - Z_ponto.
        /// </summary>
        double CalculateStaticPressure(double reservoirElevationM,
            double waterLevelM, double pointElevationM);

        // ── Declividade ─────────────────────────────────────────

        /// <summary>
        /// Calcula a declividade real de um trecho ES.
        /// i% = (ΔZ / L_horizontal) × 100.
        /// </summary>
        double CalculateSlope(double startElevationM, double endElevationM,
            double horizontalLengthM);

        /// <summary>
        /// Retorna a declividade mínima por DN (NBR 8160).
        /// DN ≤ 75 → 2%. DN ≥ 100 → 1%.
        /// </summary>
        double GetMinimumSlope(int diameterMm);

        // ══════════════════════════════════════════════════════════
        //  VERIFICAÇÕES DE CONFORMIDADE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Verifica se a pressão final atende ao mínimo requerido.
        /// </summary>
        PressureCheckResult CheckPressure(double pressureEndMca,
            double minRequiredMca, EquipmentType equipmentType);

        /// <summary>
        /// Verifica se a velocidade está dentro dos limites (AF).
        /// 0.5 ≤ V ≤ 3.0 m/s.
        /// </summary>
        VelocityCheckResult CheckVelocity(double velocityMs,
            HydraulicSystem system);

        /// <summary>
        /// Verifica se a declividade atende ao mínimo (ES).
        /// </summary>
        SlopeCheckResult CheckSlope(double slopePercent, int diameterMm);

        /// <summary>
        /// Verifica se o DN atende ao mínimo normativo por tipo de trecho.
        /// </summary>
        DiameterCheckResult CheckDiameter(int adoptedDN,
            SegmentType segmentType, EquipmentType equipmentType);

        // ══════════════════════════════════════════════════════════
        //  TABELAS NORMATIVAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna o coeficiente de Hazen-Williams para um material.
        /// </summary>
        double GetHazenWilliamsC(PipeMaterial material);

        /// <summary>
        /// Retorna o coeficiente de Manning para um material.
        /// </summary>
        double GetManningN(PipeMaterial material);

        /// <summary>
        /// Retorna os DNs comerciais disponíveis para um sistema.
        /// </summary>
        int[] GetCommercialDiameters(HydraulicSystem system);

        /// <summary>
        /// Retorna o comprimento equivalente de um fitting por DN.
        /// </summary>
        double GetFittingEquivalentLength(FittingType fittingType, int diameterMm);

        // ══════════════════════════════════════════════════════════
        //  ACUMULADO NA REDE (TRAVERSAL)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Percorre a rede AF de cima para baixo (da raiz para as folhas),
        /// acumulando pesos e dimensionando cada trecho.
        /// </summary>
        void AccumulateWeightsTopDown(PipeNetwork network);

        /// <summary>
        /// Percorre a rede ES de cima para baixo (das folhas para a raiz),
        /// acumulando UHCs e dimensionando cada trecho.
        /// </summary>
        void AccumulateUHCBottomUp(PipeNetwork network);

        /// <summary>
        /// Calcula a pressão em cada ponto da rede AF (da raiz para as folhas).
        /// </summary>
        PressureTraversalResult CalculatePressureTraversal(
            PipeNetwork network, double sourcePresMca);

        /// <summary>
        /// Encontra o ponto mais desfavorável (menor pressão) da rede AF.
        /// </summary>
        CriticalPointResult FindMostUnfavorablePoint(PipeNetwork network);

        // ══════════════════════════════════════════════════════════
        //  PIPELINE COMPLETO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Pipeline completo: Accumulate → Size → Check → Report.
        /// </summary>
        SizingProcessingResult ProcessNetwork(PipeNetwork network,
            SizingOptions options = null);
    }
}
```

---

## 4. DTOs de Resultado

### 4.1 Verificações individuais

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>Resultado da verificação de pressão.</summary>
    public class PressureCheckResult
    {
        public bool Passes { get; set; }
        public double AvailableMca { get; set; }
        public double RequiredMca { get; set; }
        public double MarginMca { get; set; }
        public string Message { get; set; }
        public string NormReference { get; set; } = "NBR 5626, §5.4.2.1";
    }

    /// <summary>Resultado da verificação de velocidade.</summary>
    public class VelocityCheckResult
    {
        public bool Passes { get; set; }
        public double ActualMs { get; set; }
        public double MinMs { get; set; }
        public double MaxMs { get; set; }
        public double MarginMs { get; set; }
        public string Message { get; set; }
        public string NormReference { get; set; } = "NBR 5626, §5.4.2.3";
    }

    /// <summary>Resultado da verificação de declividade.</summary>
    public class SlopeCheckResult
    {
        public bool Passes { get; set; }
        public double ActualPercent { get; set; }
        public double MinPercent { get; set; }
        public string Message { get; set; }
        public string NormReference { get; set; } = "NBR 8160, §5.1.4";
    }

    /// <summary>Resultado da verificação de diâmetro.</summary>
    public class DiameterCheckResult
    {
        public bool Passes { get; set; }
        public int AdoptedDN { get; set; }
        public int MinDN { get; set; }
        public string Message { get; set; }
        public string NormReference { get; set; }
    }
}
```

### 4.2 Resultados de dimensionamento por componente

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado do dimensionamento de uma prumada.
    /// </summary>
    public class RiserSizingResult
    {
        public string RiserId { get; set; }
        public HydraulicSystem System { get; set; }

        /// <summary>DN adotado para a coluna principal.</summary>
        public int MainDiameterMm { get; set; }

        /// <summary>SizingResult para cada trecho vertical.</summary>
        public List<SizingResult> SegmentResults { get; set; } = new();

        /// <summary>SizingResult para cada derivação por andar.</summary>
        public List<FloorSizingResult> FloorResults { get; set; } = new();

        /// <summary>Se todos os trechos passaram.</summary>
        public bool IsCompliant => SegmentResults.All(sr => sr.IsCompliant);

        /// <summary>Total de alertas.</summary>
        public int TotalWarnings => SegmentResults.Sum(sr => sr.Warnings.Count);

        public TimeSpan ExecutionTime { get; set; }
    }

    /// <summary>
    /// Dimensionamento por andar de uma prumada.
    /// </summary>
    public class FloorSizingResult
    {
        public string LevelName { get; set; }
        public double LevelElevationM { get; set; }
        public double AccumulatedWeight { get; set; }
        public int AccumulatedUHC { get; set; }
        public double FlowRateLs { get; set; }
        public int ColumnDiameterMm { get; set; }
        public int BranchDiameterMm { get; set; }
        public double AvailablePressureMca { get; set; }
    }
}
```

### 4.3 Resultado do dimensionamento de rede

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado do dimensionamento de uma rede completa.
    /// </summary>
    public class NetworkSizingResult
    {
        public string NetworkId { get; set; }
        public HydraulicSystem System { get; set; }

        /// <summary>SizingResult de cada trecho indexado por SegmentId.</summary>
        public Dictionary<string, SizingResult> SegmentResults { get; set; } = new();

        /// <summary>RiserSizingResult de cada prumada.</summary>
        public List<RiserSizingResult> RiserResults { get; set; } = new();

        /// <summary>SizingSummary global (resumo).</summary>
        public SizingSummary Summary { get; set; }

        /// <summary>Se todos os componentes passaram.</summary>
        public bool IsCompliant =>
            SegmentResults.Values.All(sr => sr.IsCompliant);

        /// <summary>Quantidade de trechos dimensionados.</summary>
        public int TotalSegmentsSized => SegmentResults.Count;

        /// <summary>Quantidade com erro.</summary>
        public int TotalSegmentsWithErrors =>
            SegmentResults.Values.Count(sr => !sr.IsCompliant);

        /// <summary>Caminho crítico (AF).</summary>
        public CriticalPointResult CriticalPoint { get; set; }

        /// <summary>Quantitativos de material.</summary>
        public List<MaterialQuantity> Quantities { get; set; } = new();

        public TimeSpan ExecutionTime { get; set; }

        public string GetSummary() =>
            $"{System}: {TotalSegmentsSized} trechos dimensionados, " +
            $"{TotalSegmentsWithErrors} com erro. " +
            $"DN máx={Summary?.MaxDiameterMm ?? 0} DN mín={Summary?.MinDiameterMm ?? 0}. " +
            $"{(IsCompliant ? "✅ Aprovado" : "❌ Reprovado")}";
    }
}
```

### 4.4 Traversal e ponto crítico

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado do percurso de pressão na rede AF.
    /// </summary>
    public class PressureTraversalResult
    {
        /// <summary>Pressão em cada ponto da rede (PointId → pressão mca).</summary>
        public Dictionary<string, double> PressureByPoint { get; set; } = new();

        /// <summary>Perda de carga acumulada até cada ponto.</summary>
        public Dictionary<string, double> HeadLossByPoint { get; set; } = new();

        /// <summary>ID do ponto com menor pressão.</summary>
        public string MostUnfavorablePointId { get; set; }

        /// <summary>Menor pressão encontrada em mca.</summary>
        public double MinPressureMca { get; set; }

        /// <summary>Se todos os pontos atendem à pressão mínima.</summary>
        public bool AllPointsMeetMinPressure { get; set; }

        /// <summary>Pontos que NÃO atendem à pressão mínima.</summary>
        public List<string> FailingPointIds { get; set; } = new();
    }

    /// <summary>
    /// Informações do ponto mais desfavorável.
    /// </summary>
    public class CriticalPointResult
    {
        public string PointId { get; set; }
        public string EquipmentType { get; set; }
        public string RoomName { get; set; }
        public string LevelName { get; set; }

        /// <summary>Pressão estática (sem perda de carga) em mca.</summary>
        public double StaticPressureMca { get; set; }

        /// <summary>Perda de carga total até este ponto em mca.</summary>
        public double TotalHeadLossMca { get; set; }

        /// <summary>Pressão dinâmica disponível em mca.</summary>
        public double DynamicPressureMca { get; set; }

        /// <summary>Pressão mínima requerida pelo aparelho em mca.</summary>
        public double MinRequiredMca { get; set; }

        /// <summary>Margem (dinâmica - requerida).</summary>
        public double MarginMca { get; set; }

        /// <summary>Se atende ao mínimo.</summary>
        public bool Passes { get; set; }

        /// <summary>Comprimento do caminho da raiz até este ponto em metros.</summary>
        public double PathLengthM { get; set; }

        /// <summary>IDs dos segmentos no caminho.</summary>
        public List<string> PathSegmentIds { get; set; } = new();
    }
}
```

### 4.5 Pipeline

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Opções de dimensionamento.
    /// </summary>
    public class SizingOptions
    {
        /// <summary>Método de cálculo AF preferido.</summary>
        public CalculationMethod ColdWaterMethod { get; set; } = CalculationMethod.ProbableFlow;

        /// <summary>J máxima admissível para AF (m/m).</summary>
        public double MaxUnitaryHeadLoss { get; set; } = 0.08;

        /// <summary>Pressão na origem do sistema AF (mca).</summary>
        public double SourcePressureMca { get; set; }

        /// <summary>Se deve tentar redimensionar trechos com erro.</summary>
        public bool AutoResize { get; set; } = true;

        /// <summary>Número máximo de iterações de auto-resize.</summary>
        public int MaxResizeIterations { get; set; } = 3;
    }

    /// <summary>
    /// Resultado do pipeline completo de dimensionamento.
    /// </summary>
    public class SizingProcessingResult
    {
        public NetworkSizingResult Sizing { get; set; }
        public PressureTraversalResult PressureTraversal { get; set; }
        public CriticalPointResult CriticalPoint { get; set; }

        /// <summary>Se houve auto-resize.</summary>
        public bool WasAutoResized { get; set; }
        public int ResizeIterations { get; set; }

        /// <summary>Trechos que foram redimensionados automaticamente.</summary>
        public List<string> ResizedSegmentIds { get; set; } = new();

        public bool IsSuccessful => Sizing?.IsCompliant ?? false;
        public TimeSpan TotalExecutionTime { get; set; }

        public string GetSummary() =>
            $"{Sizing?.GetSummary()} " +
            $"Ponto crítico: {CriticalPoint?.PointId} " +
            $"({CriticalPoint?.DynamicPressureMca:F2} mca" +
            $"{(CriticalPoint?.Passes == true ? " ✅" : " ❌")})";
    }
}
```

---

## 5. Tabelas Normativas (Constantes)

```csharp
namespace HidraulicoPlugin.Core.Constants
{
    /// <summary>
    /// Tabelas normativas para dimensionamento hidráulico.
    /// Fonte: NBR 5626 e NBR 8160.
    /// </summary>
    public static class HydraulicTables
    {
        // ══════════════════════════════════════════════════════════
        //  COEFICIENTES
        // ══════════════════════════════════════════════════════════

        /// <summary>Hazen-Williams C por material.</summary>
        public static double GetHazenC(PipeMaterial material) => material switch
        {
            PipeMaterial.PvcSoldavel => 140,
            PipeMaterial.PvcRoscavel => 140,
            PipeMaterial.Ppr => 140,
            PipeMaterial.Cpvc => 140,
            PipeMaterial.FerroFundido => 120,
            PipeMaterial.PvcSerieNormal => 140,
            PipeMaterial.PvcSerieReforcada => 140,
            _ => 140
        };

        /// <summary>Manning n por material.</summary>
        public static double GetManningN(PipeMaterial material) => material switch
        {
            PipeMaterial.PvcSerieNormal => 0.010,
            PipeMaterial.PvcSerieReforcada => 0.010,
            PipeMaterial.FerroFundido => 0.012,
            _ => 0.010
        };

        // ══════════════════════════════════════════════════════════
        //  DNs COMERCIAIS
        // ══════════════════════════════════════════════════════════

        /// <summary>DNs comerciais PVC soldável (AF).</summary>
        public static readonly int[] ColdWaterDNs = { 20, 25, 32, 40, 50, 60, 75, 100 };

        /// <summary>DNs comerciais PVC esgoto (ES).</summary>
        public static readonly int[] SewerDNs = { 40, 50, 75, 100, 150 };

        /// <summary>DI (mm) por DN e material (PVC soldável).</summary>
        public static double GetInternalDiameter(int dn, PipeMaterial mat) => dn switch
        {
            20 => 17.0,  25 => 21.6,  32 => 27.8,
            40 => mat == PipeMaterial.PvcSoldavel ? 35.2 : 37.0,
            50 => mat == PipeMaterial.PvcSoldavel ? 44.0 : 47.0,
            60 => 53.0,
            75 => mat == PipeMaterial.PvcSoldavel ? 66.6 : 72.0,
            100 => 97.0,
            150 => 146.0,
            _ => dn * 0.88
        };

        // ══════════════════════════════════════════════════════════
        //  UHC → DN (ESGOTO)
        // ══════════════════════════════════════════════════════════

        /// <summary>Tabela UHC → DN para ramais de descarga (NBR 8160).</summary>
        public static int GetDischargeDN(int uhc)
        {
            if (uhc <= 3) return 40;
            if (uhc <= 6) return 50;
            if (uhc <= 10) return 75;
            if (uhc <= 20) return 75;
            return 100;
        }

        /// <summary>Tabela UHC → DN para subcoletores/coletores (NBR 8160).</summary>
        public static int GetCollectorDN(int uhc)
        {
            if (uhc <= 6) return 50;
            if (uhc <= 20) return 75;
            if (uhc <= 80) return 100;
            if (uhc <= 300) return 150;
            return 150;
        }

        /// <summary>Tabela UHC → DN para tubos de queda (NBR 8160).</summary>
        public static int GetDropPipeDN(int uhc, bool hasVentilation)
        {
            if (hasVentilation)
            {
                if (uhc <= 5) return 40;
                if (uhc <= 10) return 50;
                if (uhc <= 30) return 75;
                if (uhc <= 180) return 100;
                if (uhc <= 600) return 150;
            }
            else
            {
                if (uhc <= 3) return 40;
                if (uhc <= 6) return 50;
                if (uhc <= 15) return 75;
                if (uhc <= 80) return 100;
                if (uhc <= 200) return 150;
            }
            return 150;
        }

        // ══════════════════════════════════════════════════════════
        //  COMPRIMENTOS EQUIVALENTES (FITTINGS)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Comprimento equivalente por tipo de fitting e DN (metros).
        /// Fonte: tabelas de fabricantes + NBR.
        /// </summary>
        public static double GetEquivalentLength(FittingType type, int dn)
        {
            // Tabela simplificada — valores para PVC soldável
            return (type, dn) switch
            {
                (FittingType.Elbow90, 20) => 1.1,
                (FittingType.Elbow90, 25) => 1.2,
                (FittingType.Elbow90, 32) => 1.5,
                (FittingType.Elbow90, 40) => 2.0,
                (FittingType.Elbow90, 50) => 3.2,
                (FittingType.Elbow90, 75) => 3.4,
                (FittingType.Elbow90, 100) => 4.3,

                (FittingType.Elbow45, 20) => 0.4,
                (FittingType.Elbow45, 25) => 0.5,
                (FittingType.Elbow45, 32) => 0.7,
                (FittingType.Elbow45, 40) => 0.9,
                (FittingType.Elbow45, 50) => 1.3,
                (FittingType.Elbow45, 75) => 1.5,
                (FittingType.Elbow45, 100) => 1.6,

                (FittingType.TeeStraight, 20) => 0.7,
                (FittingType.TeeStraight, 25) => 0.8,
                (FittingType.TeeStraight, 32) => 1.0,
                (FittingType.TeeStraight, 40) => 1.5,
                (FittingType.TeeStraight, 50) => 2.2,
                (FittingType.TeeStraight, 75) => 2.3,
                (FittingType.TeeStraight, 100) => 2.4,

                (FittingType.TeeBranch, 20) => 2.3,
                (FittingType.TeeBranch, 25) => 2.4,
                (FittingType.TeeBranch, 32) => 3.1,
                (FittingType.TeeBranch, 40) => 4.6,
                (FittingType.TeeBranch, 50) => 7.3,
                (FittingType.TeeBranch, 75) => 7.6,
                (FittingType.TeeBranch, 100) => 7.8,

                (FittingType.GateValve, 20) => 0.2,
                (FittingType.GateValve, 25) => 0.3,
                (FittingType.GateValve, 32) => 0.3,
                (FittingType.GateValve, 40) => 0.4,
                (FittingType.GateValve, 50) => 0.7,
                (FittingType.GateValve, 75) => 0.7,
                (FittingType.GateValve, 100) => 0.8,

                (FittingType.GlobeValve, 20) => 8.1,
                (FittingType.GlobeValve, 25) => 9.5,
                (FittingType.GlobeValve, 32) => 13.3,
                (FittingType.GlobeValve, 40) => 17.4,
                (FittingType.GlobeValve, 50) => 25.0,

                _ => 1.0 // fallback conservador
            };
        }
    }
}
```

---

## 6. Exemplo de Uso (no Orchestrator)

```csharp
public class PipelineOrchestrator
{
    private readonly ISizingService _sizingService;
    private readonly INetworkService _networkService;

    public async Task ExecuteE11(PipeNetwork networkAF, PipeNetwork networkES)
    {
        // ══════════════════════════════════════════════════════════
        //  OPÇÃO 1: Pipeline completo
        // ══════════════════════════════════════════════════════════

        var options = new SizingOptions
        {
            ColdWaterMethod = CalculationMethod.ProbableFlow,
            SourcePressureMca = 10.0,   // 10 mca do reservatório
            MaxUnitaryHeadLoss = 0.08,  // 8% (critério econômico)
            AutoResize = true
        };

        var resultAF = _sizingService.ProcessNetwork(networkAF, options);
        Console.WriteLine(resultAF.GetSummary());
        // → "AF: 15 trechos dimensionados, 0 com erro. DN máx=32 DN mín=20 ✅
        //    Ponto crítico: hp_af_chuveiro_suite (7.2 mca ✅)"

        var resultES = _sizingService.ProcessNetwork(networkES, null);
        Console.WriteLine(resultES.Sizing.GetSummary());
        // → "ES: 8 trechos dimensionados, 0 com erro. DN máx=100 DN mín=40 ✅"

        // ══════════════════════════════════════════════════════════
        //  OPÇÃO 2: Trecho a trecho (debug / controle fino)
        // ══════════════════════════════════════════════════════════

        double P = 10.0; // pressão inicial (reservatório)

        foreach (var seg in networkAF.Segments.Values)
        {
            var sr = _sizingService.SizeSegmentColdWater(seg, P);
            Console.WriteLine(sr.GetDisplayName());
            // → "✅ AF Segment: DN25 Q=0.285L/s V=1.25m/s [Aprovado]"

            P = sr.PressureEndMca; // pressão cascateia
        }

        // ══════════════════════════════════════════════════════════
        //  OPÇÃO 3: Cálculos individuais (testes unitários)
        // ══════════════════════════════════════════════════════════

        // Vazão provável
        double Q = _sizingService.CalculateProbableFlow(0.9); // ΣP = 0.9
        // → 0.285 L/s

        // DN teórico
        double DN_calc = _sizingService.CalculateTheoreticalDiameter(Q);
        // → 14.1 mm

        // DN comercial
        int DN = _sizingService.GetCommercialDiameter(DN_calc, HydraulicSystem.ColdWater);
        // → 20 mm

        // DI
        double DI = _sizingService.GetInternalDiameter(DN, PipeMaterial.PvcSoldavel);
        // → 17.0 mm

        // Velocidade
        double V = _sizingService.CalculateVelocity(Q, DI);
        // → 1.25 m/s

        // Verificação
        var velCheck = _sizingService.CheckVelocity(V, HydraulicSystem.ColdWater);
        // → Passes: true, Margin: 1.75 m/s

        // Perda de carga (Hazen-Williams)
        double Q_m3s = Q / 1000.0;
        double DI_m = DI / 1000.0;
        double J = _sizingService.CalculateHazenWilliams(Q_m3s, 140, DI_m);
        // → 0.1010 m/m

        // Comprimento equivalente
        var fittings = new List<FittingInfo>
        {
            new() { Type = FittingType.Elbow90, DiameterMm = 20 },
            new() { Type = FittingType.TeeBranch, DiameterMm = 20 }
        };
        double Leq = _sizingService.CalculateEquivalentLength(fittings);
        // → 1.1 + 2.3 = 3.4 m

        // Perda total
        double L_real = 2.5; // metros
        double deltaP = _sizingService.CalculateTotalHeadLoss(J, L_real, Leq);
        // → 0.1010 × (2.5 + 3.4) = 0.596 mca

        // Pressão final
        double P_fim = _sizingService.CalculateEndPressure(10.0, deltaP, 0.0);
        // → 10.0 - 0.596 + 0.0 = 9.404 mca
    }
}
```

---

## 7. Resumo Visual

```
ISizingService
│
├── Dimensionamento por Componente
│   ├── SizeSegmentColdWater(seg, P) → SizingResult
│   ├── SizeSegmentSewer(seg) → SizingResult
│   ├── SizeSegmentVentilation(seg, DN_es) → SizingResult
│   ├── SizeSegment(seg, context) → SizingResult
│   ├── SizeRiser(riser) → RiserSizingResult
│   └── SizeNetwork(network) → NetworkSizingResult
│
├── Cálculos Individuais (Building Blocks)
│   ├── Vazão
│   │   ├── CalculateProbableFlow(ΣP) → Q      [Q = 0.3√ΣP]
│   │   ├── CalculateSewerFlow(ΣUHC) → Q        [tabela NBR]
│   │   └── CalculateMaxFlow(DN, mat, Vmax) → Q [Q = V×A]
│   ├── Diâmetro
│   │   ├── CalculateTheoreticalDiameter(Q, Jmax) → DN_calc
│   │   ├── GetCommercialDiameter(DN_calc, sys) → DN
│   │   ├── GetInternalDiameter(DN, mat) → DI
│   │   ├── GetDiameterByUHC(ΣUHC, type) → DN   [tabela ES]
│   │   └── GetVentilationDiameter(DN_es) → DN   [≥ 2/3 DN_es]
│   ├── Velocidade
│   │   ├── CalculateVelocity(Q, DI) → V         [V = Q/A]
│   │   └── CalculateManningVelocity(DI, i, n) → V [Manning]
│   ├── Perda de Carga
│   │   ├── CalculateHazenWilliams(Q, C, D) → J  [m/m]
│   │   ├── CalculateFairWhippleHsiao(Q, D) → J  [m/m]
│   │   ├── CalculateTotalHeadLoss(J, L, Leq) → ΔP [mca]
│   │   └── CalculateEquivalentLength(fittings) → Leq
│   ├── Pressão
│   │   ├── CalculateEndPressure(P_ini, ΔP, ΔZ) → P_fim
│   │   └── CalculateStaticPressure(Z_res, h, Z_pt) → P_est
│   └── Declividade
│       ├── CalculateSlope(Z1, Z2, L) → i%
│       └── GetMinimumSlope(DN) → i_min%
│
├── Verificações de Conformidade
│   ├── CheckPressure(P, P_min, type) → PressureCheckResult
│   ├── CheckVelocity(V, sys) → VelocityCheckResult
│   ├── CheckSlope(i%, DN) → SlopeCheckResult
│   └── CheckDiameter(DN, segType, eqType) → DiameterCheckResult
│
├── Tabelas Normativas
│   ├── GetHazenWilliamsC(material) → C
│   ├── GetManningN(material) → n
│   ├── GetCommercialDiameters(system) → int[]
│   └── GetFittingEquivalentLength(type, DN) → Leq
│
├── Traversal na Rede
│   ├── AccumulateWeightsTopDown(network)
│   ├── AccumulateUHCBottomUp(network)
│   ├── CalculatePressureTraversal(net, P_source) → PressureTraversalResult
│   └── FindMostUnfavorablePoint(network) → CriticalPointResult
│
├── Pipeline
│   └── ProcessNetwork(net, options) → SizingProcessingResult
│
└── Dependências
    ├── PipeSegment, Riser, PipeNetwork (modelos)
    ├── SizingResult (modelo de saída)
    ├── HydraulicTables (constantes normativas)
    └── INetworkService (fornece a rede montada)
```
