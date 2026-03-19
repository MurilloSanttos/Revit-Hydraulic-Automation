# Modelo de Domínio — PipeSegment (TrechoTubulação)

> Especificação completa do modelo agnóstico ao Revit que representa segmentos de tubulação hidráulica, servindo como base para dimensionamento, validação normativa e geração de redes no Revit.

---

## 1. Definição do Modelo

### 1.1 O que é PipeSegment

`PipeSegment` é a representação de um **trecho individual de tubulação** entre dois pontos. É o bloco fundamental da rede hidráulica — qualquer rede completa (AF, ES, VE) é uma coleção de `PipeSegment` conectados.

### 1.2 Papel no sistema

```
HydraulicPoint (ponto A)
        │
        └── PipeSegment (trecho)    ← ESTE MODELO
                │
                └── HydraulicPoint (ponto B)
```

| Módulo | Uso do PipeSegment |
|--------|-------------------|
| E06 — Prumadas | Segmentos verticais (colunas AF, tubos de queda ES) |
| E07 — Rede AF | Sub-ramais, ramais, colunas de distribuição |
| E08 — Rede ES | Ramais de descarga, subcoletores, coletores |
| E09 — Inclinações | Aplicação de declividade nos segmentos ES |
| E10 — Sistemas | Associação dos segmentos aos MEP Systems |
| E11 — Dimensionamento | Cálculo de DN, V, J, P em cada trecho |
| E12 — Tabelas | Listagem de trechos com propriedades |

### 1.3 Trecho vs. Rede completa

```
PipeSegment = UM trecho de tubo entre dois pontos
  - Tem início e fim
  - Tem DN, comprimento, material
  - É a unidade de cálculo

PipeNetwork  = COLEÇÃO de PipeSegments formando uma rede
  - É um grafo (nós = pontos, arestas = trechos)
  - Tem topologia (árvore, não ciclo)
  - É o sistema completo (AF ou ES ou VE)

ANALOGIA:
  PipeSegment = 1 peça de tubo cortada
  PipeNetwork = a instalação inteira montada
```

---

## 2. Estrutura de Dados — Código C# Completo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa um trecho individual de tubulação entre dois pontos.
    /// Unidade fundamental de cálculo e geração de rede.
    /// Agnóstico ao Revit — não depende de Pipe, Connector ou ElementId.
    /// </summary>
    public class PipeSegment
    {
        // ── Identidade ──────────────────────────────────────────────

        /// <summary>
        /// ID único do trecho.
        /// Formato: "seg_{sistema}_{seq}" (ex: "seg_af_001", "seg_es_012").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Sistema hidráulico ao qual este trecho pertence.
        /// </summary>
        public HydraulicSystem System { get; set; }

        /// <summary>
        /// Classificação hierárquica do trecho na rede.
        /// </summary>
        public SegmentType Type { get; set; }

        // ── Conectividade ───────────────────────────────────────────

        /// <summary>
        /// ID do ponto de origem (upstream — de onde vem o fluxo).
        /// Para AF: da coluna/ramal em direção ao aparelho.
        /// Para ES: do aparelho em direção ao subcoletor.
        /// </summary>
        public string StartPointId { get; set; }

        /// <summary>
        /// ID do ponto de destino (downstream — para onde vai o fluxo).
        /// </summary>
        public string EndPointId { get; set; }

        /// <summary>
        /// IDs dos trechos a montante (que alimentam este trecho).
        /// Preenchido na montagem da rede.
        /// </summary>
        public List<string> UpstreamSegmentIds { get; set; } = new();

        /// <summary>
        /// ID do trecho a jusante (para onde este trecho deságua).
        /// NULL se é o último trecho (chega na prumada ou saída).
        /// </summary>
        public string DownstreamSegmentId { get; set; }

        // ── Geometria ───────────────────────────────────────────────

        /// <summary>
        /// Posição do início do trecho em metros (coordenada absoluta).
        /// </summary>
        public Point3D StartPosition { get; set; }

        /// <summary>
        /// Posição do fim do trecho em metros (coordenada absoluta).
        /// </summary>
        public Point3D EndPosition { get; set; }

        /// <summary>
        /// Comprimento do trecho em metros.
        /// Calculado: distância 3D entre StartPosition e EndPosition.
        /// </summary>
        public double LengthM { get; set; }

        /// <summary>
        /// Orientação do trecho.
        /// </summary>
        public SegmentOrientation Orientation { get; set; }

        /// <summary>
        /// Nome do Level onde o trecho está.
        /// </summary>
        public string LevelName { get; set; }

        // ── Propriedades Físicas ────────────────────────────────────

        /// <summary>
        /// Diâmetro nominal em mm (ex: 20, 25, 32, 40, 50, 75, 100).
        /// </summary>
        public int DiameterMm { get; set; }

        /// <summary>
        /// Diâmetro interno efetivo em mm (depende do material).
        /// Usado para cálculos hidráulicos.
        /// PVC soldável DN 25 → DI 21.6mm.
        /// </summary>
        public double InternalDiameterMm { get; set; }

        /// <summary>
        /// Material da tubulação.
        /// </summary>
        public PipeMaterial Material { get; set; }

        /// <summary>
        /// Rugosidade absoluta do material em mm.
        /// PVC = 0.01mm. Ferro fundido = 0.25mm.
        /// Usado no cálculo de perda de carga.
        /// </summary>
        public double RoughnessMm { get; set; }

        // ── Propriedades Hidráulicas ────────────────────────────────

        /// <summary>
        /// Soma dos pesos a montante deste trecho (para AF).
        /// ΣP de todos os pontos alimentados por este trecho.
        /// </summary>
        public double AccumulatedWeightAF { get; set; }

        /// <summary>
        /// Soma das UHC a montante deste trecho (para ES).
        /// ΣUHC de todos os pontos que descarregam neste trecho.
        /// </summary>
        public int AccumulatedUHC { get; set; }

        /// <summary>
        /// Vazão de projeto em L/s.
        /// AF: calculada pela fórmula Q = 0.3 × √ΣP (NBR 5626).
        /// ES: lida da tabela UHC→Q (NBR 8160).
        /// </summary>
        public double FlowRateLs { get; set; }

        /// <summary>
        /// Velocidade do fluxo em m/s.
        /// V = Q / A, onde A = π × (DI/2)².
        /// Limite AF: 0.5 ≤ V ≤ 3.0 m/s.
        /// </summary>
        public double VelocityMs { get; set; }

        /// <summary>
        /// Declividade do trecho em % (apenas ES e VE).
        /// Valores típicos: 1% (DN ≥ 75), 2% (DN ≤ 50).
        /// AF: 0 (horizontal ou vertical, sem declividade).
        /// </summary>
        public double SlopePercent { get; set; }

        /// <summary>
        /// Perda de carga unitária em m/m (J).
        /// Calculada por Fair-Whipple-Hsiao ou Hazen-Williams.
        /// </summary>
        public double HeadLossUnitaryMm { get; set; }

        /// <summary>
        /// Perda de carga total no trecho em mca.
        /// J × L (perda unitária × comprimento).
        /// </summary>
        public double HeadLossTotalMca { get; set; }

        /// <summary>
        /// Pressão disponível no início do trecho em mca (apenas AF).
        /// Calculada de montante para jusante.
        /// </summary>
        public double PressureStartMca { get; set; }

        /// <summary>
        /// Pressão disponível no fim do trecho em mca (apenas AF).
        /// PressureEnd = PressureStart - HeadLossTotal ± desnível.
        /// </summary>
        public double PressureEndMca { get; set; }

        // ── Conexões (Fittings) ─────────────────────────────────────

        /// <summary>
        /// Lista de conexões (joelhos, tês, reduções) neste trecho.
        /// Usadas para cálculo de comprimento equivalente.
        /// </summary>
        public List<FittingInfo> Fittings { get; set; } = new();

        /// <summary>
        /// Comprimento equivalente total dos fittings em metros.
        /// Depende do DN e tipo de conexão.
        /// </summary>
        public double FittingsEquivalentLengthM { get; set; }

        /// <summary>
        /// Comprimento total para cálculo (real + equivalente).
        /// LengthM + FittingsEquivalentLengthM.
        /// </summary>
        public double TotalCalculationLengthM =>
            LengthM + FittingsEquivalentLengthM;

        // ── Revit ───────────────────────────────────────────────────

        /// <summary>
        /// ElementId do Pipe criado no Revit (como string).
        /// Preenchido após geração em E07/E08.
        /// </summary>
        public string RevitElementId { get; set; }

        /// <summary>
        /// Nome do PipingSystemType no Revit.
        /// Ex: "Domestic Cold Water", "Sanitary".
        /// </summary>
        public string RevitSystemTypeName { get; set; }

        /// <summary>
        /// Nome do PipeType no Revit.
        /// Ex: "PVC - Schedule 40".
        /// </summary>
        public string RevitPipeTypeName { get; set; }

        // ── Status ──────────────────────────────────────────────────

        /// <summary>
        /// Status do trecho no ciclo de vida.
        /// </summary>
        public SegmentStatus Status { get; set; } = SegmentStatus.Planned;

        /// <summary>
        /// Se o trecho foi dimensionado (DN calculado, não default).
        /// </summary>
        public bool IsSized { get; set; }

        /// <summary>
        /// Se o trecho foi verificado normativamente.
        /// </summary>
        public bool IsValidated { get; set; }
    }
}
```

---

## 3. Enums

### 3.1 SegmentType

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Classificação hierárquica do trecho na rede hidráulica.
    /// Define a posição do trecho na topologia da instalação.
    /// </summary>
    public enum SegmentType
    {
        // ── Água Fria ───────────────────────────────────────────

        /// <summary>
        /// Sub-ramal: trecho entre a coluna/ramal e o aparelho.
        /// O mais fino da rede AF. DN típico: 20-25mm.
        /// </summary>
        SubBranch = 1,

        /// <summary>
        /// Ramal: trecho que alimenta vários sub-ramais.
        /// Distribui água no pavimento. DN típico: 25-32mm.
        /// </summary>
        Branch = 2,

        /// <summary>
        /// Coluna de distribuição: trecho vertical entre pavimentos.
        /// Alimenta os ramais de cada andar. DN típico: 32-50mm.
        /// </summary>
        DistributionColumn = 3,

        /// <summary>
        /// Barrilete: trecho horizontal no topo, saindo do reservatório.
        /// Alimenta as colunas. DN típico: 40-75mm.
        /// </summary>
        BarrelPipe = 4,

        // ── Esgoto ──────────────────────────────────────────────

        /// <summary>
        /// Ramal de descarga: do aparelho até o TQ (vertical) ou subcoletor.
        /// DN mínimo depende do aparelho (40-100mm).
        /// </summary>
        DischargeBranch = 10,

        /// <summary>
        /// Ramal secundário: liga aparelhos à CX sifonada.
        /// DN típico: 40mm.
        /// </summary>
        SecondaryBranch = 11,

        /// <summary>
        /// Subcoletor: horizontal, recebe ramais de descarga.
        /// Liga ramais ao tubo de queda. DN típico: 75-100mm.
        /// </summary>
        SubCollector = 12,

        /// <summary>
        /// Tubo de queda: vertical, recebe subcoletores de cada andar.
        /// DN típico: 75-100mm.
        /// </summary>
        DropPipe = 13,

        /// <summary>
        /// Coletor predial: horizontal, do TQ até a rede pública.
        /// Trecho final. DN típico: 100-150mm.
        /// </summary>
        BuildingCollector = 14,

        // ── Ventilação ──────────────────────────────────────────

        /// <summary>
        /// Ramal de ventilação: do ramal de descarga ao tubo de ventilação.
        /// DN mínimo: metade do ramal de descarga.
        /// </summary>
        VentBranch = 20,

        /// <summary>
        /// Coluna de ventilação: vertical, recebe ramais de ventilação.
        /// Sobe até acima da cobertura.
        /// </summary>
        VentColumn = 21,

        /// <summary>
        /// Ventilação primária: prolongamento do tubo de queda acima da cobertura.
        /// Mesmo DN do TQ.
        /// </summary>
        PrimaryVent = 22
    }
}
```

### 3.2 SegmentOrientation

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Orientação do trecho no espaço.
    /// </summary>
    public enum SegmentOrientation
    {
        /// <summary>Horizontal (ΔZ ≈ 0 ou com declividade controlada).</summary>
        Horizontal = 1,

        /// <summary>Vertical (ΔX ≈ 0 e ΔY ≈ 0).</summary>
        Vertical = 2,

        /// <summary>Inclinado (nem horizontal nem vertical).</summary>
        Inclined = 3
    }
}
```

### 3.3 PipeMaterial

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Material da tubulação. Impacta rugosidade, DI e perda de carga.
    /// </summary>
    public enum PipeMaterial
    {
        /// <summary>PVC soldável (água fria). C = 140. e = 0.01mm.</summary>
        PvcSoldavel = 1,

        /// <summary>PVC roscável (água fria). C = 140. e = 0.01mm.</summary>
        PvcRoscavel = 2,

        /// <summary>PVC série normal (esgoto). e = 0.01mm.</summary>
        PvcSerieNormal = 3,

        /// <summary>PVC série reforçada (esgoto sob carga). e = 0.01mm.</summary>
        PvcSerieReforcada = 4,

        /// <summary>PPR (água fria/quente). C = 140. e = 0.01mm.</summary>
        Ppr = 5,

        /// <summary>Ferro fundido (esgoto primário). C = 120. e = 0.25mm.</summary>
        FerroFundido = 6,

        /// <summary>CPVC (água quente). C = 140. e = 0.01mm.</summary>
        Cpvc = 7
    }
}
```

### 3.4 SegmentStatus

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Status do trecho no ciclo de vida do pipeline.
    /// </summary>
    public enum SegmentStatus
    {
        /// <summary>Trecho planejado (rota definida, DN padrão).</summary>
        Planned = 0,

        /// <summary>Trecho dimensionado (DN calculado, V e J verificados).</summary>
        Sized = 1,

        /// <summary>Trecho validado (atende norma).</summary>
        Validated = 2,

        /// <summary>Trecho criado no Revit (ElementId preenchido).</summary>
        Created = 3,

        /// <summary>Trecho com erro (DN insuficiente, V fora do limite).</summary>
        Error = 4
    }
}
```

---

## 4. FittingInfo (Conexões/Peças)

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa uma conexão (peça) dentro de um trecho.
    /// Joelhos, tês, reduções — contribuem para o comprimento equivalente.
    /// </summary>
    public class FittingInfo
    {
        /// <summary>
        /// Tipo de conexão.
        /// </summary>
        public FittingType Type { get; set; }

        /// <summary>
        /// DN da conexão em mm.
        /// </summary>
        public int DiameterMm { get; set; }

        /// <summary>
        /// Comprimento equivalente em metros.
        /// Fonte: tabelas de comprimento equivalente por DN e tipo.
        /// </summary>
        public double EquivalentLengthM { get; set; }

        /// <summary>
        /// Posição da conexão em metros (para geração no Revit).
        /// </summary>
        public Point3D Position { get; set; }

        /// <summary>
        /// ElementId do fitting no Revit (como string, pós-criação).
        /// </summary>
        public string RevitElementId { get; set; }
    }

    /// <summary>
    /// Tipos de conexão (fitting).
    /// </summary>
    public enum FittingType
    {
        /// <summary>Joelho 90° (cotovelo).</summary>
        Elbow90 = 1,

        /// <summary>Joelho 45°.</summary>
        Elbow45 = 2,

        /// <summary>Tê de passagem direta.</summary>
        TeeStraight = 3,

        /// <summary>Tê de saída lateral.</summary>
        TeeBranch = 4,

        /// <summary>Tê de saída bilateral.</summary>
        TeeBilateral = 5,

        /// <summary>Redução concêntrica.</summary>
        ReductionConcentric = 6,

        /// <summary>Redução excêntrica (esgoto).</summary>
        ReductionEccentric = 7,

        /// <summary>Curva longa 90° (esgoto).</summary>
        LongSweep90 = 8,

        /// <summary>Curva curta 90° (esgoto).</summary>
        ShortSweep90 = 9,

        /// <summary>Junção simples 45° (esgoto).</summary>
        Wye45 = 10,

        /// <summary>Junção dupla 45° (esgoto).</summary>
        DoubleWye45 = 11,

        /// <summary>Válvula de retenção.</summary>
        CheckValve = 12,

        /// <summary>Registro de gaveta.</summary>
        GateValve = 13,

        /// <summary>Registro de pressão (globo).</summary>
        GlobeValve = 14
    }
}
```

---

## 5. Métodos do Modelo

```csharp
public class PipeSegment
{
    // ... propriedades acima ...

    // ── Cálculos geométricos ────────────────────────────────────

    /// <summary>
    /// Calcula comprimento 3D entre os pontos.
    /// </summary>
    public double CalculateLength()
    {
        return StartPosition.DistanceTo(EndPosition);
    }

    /// <summary>
    /// Calcula a declividade real do trecho em %.
    /// Positivo = desce no sentido do fluxo (correto para ES).
    /// </summary>
    public double CalculateActualSlope()
    {
        double horizontalDist = StartPosition.DistanceTo2D(EndPosition);
        if (horizontalDist < 0.001) return 0; // vertical
        double dz = StartPosition.Z - EndPosition.Z; // positivo = descendo
        return (dz / horizontalDist) * 100;
    }

    /// <summary>
    /// Determina a orientação do trecho.
    /// </summary>
    public SegmentOrientation DetermineOrientation()
    {
        double dx = Math.Abs(StartPosition.X - EndPosition.X);
        double dy = Math.Abs(StartPosition.Y - EndPosition.Y);
        double dz = Math.Abs(StartPosition.Z - EndPosition.Z);

        double horizontal = Math.Sqrt(dx * dx + dy * dy);

        if (horizontal < 0.05 && dz > 0.10) return SegmentOrientation.Vertical;
        if (dz < 0.05) return SegmentOrientation.Horizontal;
        return SegmentOrientation.Inclined;
    }

    /// <summary>
    /// Calcula desnível (diferença de cota) em metros.
    /// Positivo = Start mais alto que End.
    /// </summary>
    public double GetElevationDifference()
    {
        return StartPosition.Z - EndPosition.Z;
    }

    // ── Cálculos hidráulicos ────────────────────────────────────

    /// <summary>
    /// Calcula a área da seção transversal em m².
    /// A = π × (DI / 2)²
    /// </summary>
    public double GetCrossSectionAreaM2()
    {
        double radiusM = (InternalDiameterMm / 1000.0) / 2.0;
        return Math.PI * radiusM * radiusM;
    }

    /// <summary>
    /// Calcula velocidade: V = Q / A.
    /// Q em L/s → converter para m³/s.
    /// </summary>
    public double CalculateVelocity()
    {
        double area = GetCrossSectionAreaM2();
        if (area < 0.000001) return 0;
        double flowM3s = FlowRateLs / 1000.0;
        return flowM3s / area;
    }

    /// <summary>
    /// Calcula perda de carga unitária por Fair-Whipple-Hsiao.
    /// J = (10.643 × Q^1.85) / (C^1.85 × D^4.87)
    /// Q em m³/s, D em m.
    /// </summary>
    public double CalculateHeadLossHazenWilliams(double hazenC = 140)
    {
        double D = InternalDiameterMm / 1000.0;
        double Q = FlowRateLs / 1000.0;

        if (D < 0.001 || Q < 0.00001) return 0;

        return (10.643 * Math.Pow(Q, 1.85)) /
               (Math.Pow(hazenC, 1.85) * Math.Pow(D, 4.87));
    }

    /// <summary>
    /// Calcula perda de carga total no trecho.
    /// headLoss = J × (L + Leq)
    /// </summary>
    public double CalculateTotalHeadLoss()
    {
        return HeadLossUnitaryMm * TotalCalculationLengthM;
    }

    // ── Validações rápidas ──────────────────────────────────────

    /// <summary>
    /// Verifica se a velocidade está dentro dos limites normativos.
    /// AF: 0.5 ≤ V ≤ 3.0 m/s (NBR 5626).
    /// </summary>
    public bool IsVelocityWithinLimits()
    {
        if (System == HydraulicSystem.ColdWater)
            return VelocityMs >= 0.5 && VelocityMs <= 3.0;

        return true; // ES e VE não têm limite de velocidade por tubo cheio
    }

    /// <summary>
    /// Verifica se a declividade atende a NBR 8160.
    /// DN ≤ 75mm → i ≥ 2%. DN ≥ 100mm → i ≥ 1%.
    /// </summary>
    public bool IsSlopeWithinLimits()
    {
        if (System != HydraulicSystem.Sewer) return true;
        if (Orientation == SegmentOrientation.Vertical) return true;

        double minSlope = DiameterMm >= 100 ? 1.0 : 2.0;
        return SlopePercent >= minSlope;
    }

    /// <summary>
    /// Verifica se o DN nunca diminui no sentido do fluxo (ES).
    /// Precisa receber o DN do trecho downstream.
    /// </summary>
    public bool IsDiameterNonDecreasing(PipeSegment downstream)
    {
        if (downstream == null) return true;
        return downstream.DiameterMm >= DiameterMm;
    }

    /// <summary>
    /// Verifica se o trecho está conectado (tem pontos nas duas pontas).
    /// </summary>
    public bool IsConnected()
    {
        return !string.IsNullOrEmpty(StartPointId)
            && !string.IsNullOrEmpty(EndPointId);
    }

    /// <summary>
    /// Verifica se o trecho já foi criado no Revit.
    /// </summary>
    public bool ExistsInRevit() => !string.IsNullOrEmpty(RevitElementId);

    // ── Display ─────────────────────────────────────────────────

    public string GetDisplayName()
    {
        string sys = System switch
        {
            HydraulicSystem.ColdWater => "AF",
            HydraulicSystem.Sewer => "ES",
            HydraulicSystem.Ventilation => "VE",
            _ => "??"
        };

        return $"{sys} {Type} DN{DiameterMm} — {LengthM:F2}m";
    }

    public override string ToString()
    {
        return $"Seg[{Id}] {System}/{Type} DN{DiameterMm} " +
               $"L={LengthM:F2}m Q={FlowRateLs:F3}L/s V={VelocityMs:F2}m/s " +
               $"i={SlopePercent:F1}% J={HeadLossUnitaryMm:F4}m/m " +
               $"({StartPointId}→{EndPointId})";
    }
}
```

---

## 6. Tabelas de Referência

### 6.1 DNs comerciais (PVC soldável — AF)

| DN (mm) | DI (mm) | Área (cm²) | Uso típico |
|---------|---------|-----------|-----------|
| 20 | 17.0 | 2.27 | Sub-ramais |
| 25 | 21.6 | 3.66 | Ramais, sub-ramais |
| 32 | 27.8 | 6.07 | Ramais |
| 40 | 35.2 | 9.73 | Ramais, colunas |
| 50 | 44.0 | 15.21 | Colunas |
| 60 | 53.0 | 22.06 | Colunas, barrilete |
| 75 | 66.6 | 34.84 | Barrilete |

### 6.2 DNs comerciais (PVC série normal — ES)

| DN (mm) | DI (mm) | Declividade mín (%) | Uso típico |
|---------|---------|-------------------|-----------|
| 40 | 37.0 | 2% | Ramais descarga (lav, chuveiro) |
| 50 | 47.0 | 2% | Ramais descarga (pia) |
| 75 | 72.0 | 2% | Subcoletores, CX sifonada |
| 100 | 97.0 | 1% | Vaso sanitário, TQ, subcoletor |
| 150 | 146.0 | 1% | Coletor predial |

### 6.3 Comprimentos equivalentes (PVC)

| Peça | DN 20 | DN 25 | DN 32 | DN 40 | DN 50 | DN 75 | DN 100 |
|------|-------|-------|-------|-------|-------|-------|--------|
| Joelho 90° | 1.1 | 1.2 | 1.5 | 2.0 | 3.2 | 3.4 | 3.5 |
| Joelho 45° | 0.4 | 0.5 | 0.7 | 0.9 | 1.0 | 1.3 | 1.5 |
| Tê passagem | 0.7 | 0.8 | 1.0 | 1.5 | 2.2 | 2.4 | 2.5 |
| Tê saída lat. | 2.3 | 2.4 | 3.1 | 4.6 | 7.3 | 7.5 | 7.8 |
| Reg. gaveta | 0.2 | 0.3 | 0.4 | 0.5 | 0.6 | 0.7 | 0.8 |
| Reg. globo | 8.1 | 11.1 | 13.6 | 17.4 | 21.3 | 22.0 | 23.0 |

---

## 7. Validação

### 7.1 PipeSegmentValidator

```csharp
namespace HidraulicoPlugin.Core.Validation
{
    public class PipeSegmentValidator
    {
        public ValidationReport Validate(PipeSegment segment)
        {
            var report = new ValidationReport();

            // ── Identidade ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(segment.Id))
                report.Add(ValidationLevel.Critical, "Trecho sem ID");

            // ── Conectividade ───────────────────────────────────
            if (!segment.IsConnected())
                report.Add(ValidationLevel.Critical,
                    $"Trecho {segment.Id}: sem pontos de conexão");

            // ── Geometria ───────────────────────────────────────
            if (segment.LengthM <= 0)
                report.Add(ValidationLevel.Critical,
                    $"Trecho {segment.Id}: comprimento ≤ 0");

            if (segment.LengthM > 50)
                report.Add(ValidationLevel.Light,
                    $"Trecho {segment.Id}: comprimento muito longo ({segment.LengthM:F1}m)");

            // ── DN ──────────────────────────────────────────────
            if (segment.DiameterMm <= 0)
                report.Add(ValidationLevel.Critical,
                    $"Trecho {segment.Id}: DN = 0");

            int[] dnsValidos = { 20, 25, 32, 40, 50, 60, 75, 100, 150 };
            if (!dnsValidos.Contains(segment.DiameterMm))
                report.Add(ValidationLevel.Medium,
                    $"Trecho {segment.Id}: DN {segment.DiameterMm} não é comercial");

            // ── Hidráulica (se dimensionado) ────────────────────
            if (segment.IsSized)
            {
                if (segment.FlowRateLs <= 0)
                    report.Add(ValidationLevel.Medium,
                        $"Trecho {segment.Id}: vazão = 0 (dimensionado)");

                if (!segment.IsVelocityWithinLimits())
                    report.Add(ValidationLevel.Medium,
                        $"Trecho {segment.Id}: V={segment.VelocityMs:F2} m/s fora do limite");

                if (!segment.IsSlopeWithinLimits())
                    report.Add(ValidationLevel.Medium,
                        $"Trecho {segment.Id}: i={segment.SlopePercent:F1}% abaixo do mínimo");
            }

            // ── Declividade ES ──────────────────────────────────
            if (segment.System == HydraulicSystem.Sewer
                && segment.Orientation == SegmentOrientation.Horizontal)
            {
                if (segment.SlopePercent <= 0)
                    report.Add(ValidationLevel.Critical,
                        $"Trecho {segment.Id}: ES horizontal sem declividade");
            }

            return report;
        }
    }
}
```

---

## 8. Exemplo — Banheiro social (rede ES)

### 8.1 Trechos gerados

```
Banheiro Social — Rede de Esgoto:

Trecho 1: Lavatório → CX sifonada
  seg_es_001 | SecondaryBranch | DN 40 | L 1.20m | 2 UHC

Trecho 2: Chuveiro → CX sifonada
  seg_es_002 | SecondaryBranch | DN 40 | L 0.80m | 2 UHC

Trecho 3: Ralo → CX sifonada
  seg_es_003 | SecondaryBranch | DN 40 | L 0.50m | 1 UHC

Trecho 4: CX sifonada → Subcoletor
  seg_es_004 | DischargeBranch | DN 75 | L 1.50m | 5 UHC (acumulado)

Trecho 5: Vaso → Subcoletor (INDEPENDENTE)
  seg_es_005 | DischargeBranch | DN 100 | L 2.00m | 6 UHC

Trecho 6: Subcoletor → Tubo de queda
  seg_es_006 | SubCollector | DN 100 | L 3.00m | 11 UHC (total banheiro)
```

### 8.2 Topologia

```
Lavatório ──seg_es_001──┐
Chuveiro  ──seg_es_002──┼── CX sif ──seg_es_004──┐
Ralo      ──seg_es_003──┘                         ├── Subcoletor ──seg_es_006──→ TQ
Vaso      ──────────────seg_es_005────────────────┘
```

---

## 9. JSON de Exemplo

```json
[
  {
    "id": "seg_es_001",
    "system": "Sewer",
    "type": "SecondaryBranch",
    "start_point_id": "hp_es_002",
    "end_point_id": "hp_acc_001",
    "upstream_segment_ids": [],
    "downstream_segment_id": "seg_es_004",
    "start_position": { "x": 4.600, "y": 4.500, "z": 0.500 },
    "end_position": { "x": 5.200, "y": 3.800, "z": 0.000 },
    "length_m": 1.20,
    "orientation": "Inclined",
    "level_name": "Térreo",
    "diameter_mm": 40,
    "internal_diameter_mm": 37.0,
    "material": "PvcSerieNormal",
    "roughness_mm": 0.01,
    "accumulated_weight_af": 0,
    "accumulated_uhc": 2,
    "flow_rate_ls": 0.0,
    "velocity_ms": 0.0,
    "slope_percent": 2.0,
    "head_loss_unitary_mm": 0.0,
    "head_loss_total_mca": 0.0,
    "fittings": [
      {
        "type": "Elbow90",
        "diameter_mm": 40,
        "equivalent_length_m": 2.0,
        "position": { "x": 5.200, "y": 4.500, "z": 0.250 }
      }
    ],
    "fittings_equivalent_length_m": 2.0,
    "revit_element_id": null,
    "revit_system_type_name": "Sanitary",
    "revit_pipe_type_name": "PVC - Serie Normal",
    "status": "Planned",
    "is_sized": false,
    "is_validated": false
  },
  {
    "id": "seg_es_005",
    "system": "Sewer",
    "type": "DischargeBranch",
    "start_point_id": "hp_es_001",
    "end_point_id": "hp_riser_es_001",
    "upstream_segment_ids": [],
    "downstream_segment_id": "seg_es_006",
    "start_position": { "x": 5.800, "y": 3.050, "z": 0.000 },
    "end_position": { "x": 5.200, "y": 1.500, "z": -0.030 },
    "length_m": 2.00,
    "orientation": "Horizontal",
    "level_name": "Térreo",
    "diameter_mm": 100,
    "internal_diameter_mm": 97.0,
    "material": "PvcSerieNormal",
    "roughness_mm": 0.01,
    "accumulated_weight_af": 0,
    "accumulated_uhc": 6,
    "flow_rate_ls": 0.0,
    "velocity_ms": 0.0,
    "slope_percent": 1.0,
    "head_loss_unitary_mm": 0.0,
    "head_loss_total_mca": 0.0,
    "fittings": [],
    "fittings_equivalent_length_m": 0.0,
    "revit_element_id": null,
    "revit_system_type_name": "Sanitary",
    "revit_pipe_type_name": "PVC - Serie Normal",
    "status": "Planned",
    "is_sized": false,
    "is_validated": false
  }
]
```

---

## 10. Resumo Visual

```
PipeSegment
├── Identidade
│   ├── Id (string — "seg_{sys}_{seq}")
│   ├── System (HydraulicSystem — AF|ES|VE)
│   └── Type (SegmentType — 13 valores)
├── Conectividade
│   ├── StartPointId (string → HydraulicPoint.Id)
│   ├── EndPointId (string → HydraulicPoint.Id)
│   ├── UpstreamSegmentIds (List<string>)
│   └── DownstreamSegmentId (string?)
├── Geometria
│   ├── StartPosition (Point3D)
│   ├── EndPosition (Point3D)
│   ├── LengthM (double)
│   └── Orientation (Horizontal|Vertical|Inclined)
├── Propriedades Físicas
│   ├── DiameterMm (int — DN comercial)
│   ├── InternalDiameterMm (double — DI efetivo)
│   ├── Material (PipeMaterial — 7 tipos)
│   └── RoughnessMm (double)
├── Hidráulica
│   ├── AccumulatedWeightAF / AccumulatedUHC
│   ├── FlowRateLs (double — vazão)
│   ├── VelocityMs (double — velocidade)
│   ├── SlopePercent (double — declividade)
│   ├── HeadLossUnitaryMm (double — J)
│   ├── HeadLossTotalMca (double)
│   └── PressureStart/EndMca (double — AF)
├── Fittings
│   ├── List<FittingInfo> (joelhos, tês, etc.)
│   ├── FittingsEquivalentLengthM (double)
│   └── TotalCalculationLengthM (computed)
├── Revit
│   ├── RevitElementId (string?)
│   ├── RevitSystemTypeName (string)
│   └── RevitPipeTypeName (string)
├── Status
│   ├── Status (SegmentStatus — 5 estados)
│   ├── IsSized (bool)
│   └── IsValidated (bool)
└── Métodos
    ├── CalculateLength()
    ├── CalculateActualSlope()
    ├── DetermineOrientation()
    ├── GetCrossSectionAreaM2()
    ├── CalculateVelocity()
    ├── CalculateHeadLossHazenWilliams()
    ├── CalculateTotalHeadLoss()
    ├── IsVelocityWithinLimits()
    ├── IsSlopeWithinLimits()
    ├── IsDiameterNonDecreasing()
    ├── IsConnected()
    ├── ExistsInRevit()
    ├── GetDisplayName()
    └── ToString()
```
