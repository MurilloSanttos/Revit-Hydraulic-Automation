# Modelo de Domínio — PipeNetwork (SistemaMEP)

> Especificação completa do modelo agnóstico ao Revit que representa o sistema lógico de redes hidráulicas, agrupando trechos, prumadas, pontos e equipamentos em uma entidade coesa para validação, dimensionamento e geração no Revit.

---

## 1. Definição do Modelo

### 1.1 O que é PipeNetwork

`PipeNetwork` é a representação **lógica** de um sistema hidráulico completo (AF, ES ou VE). Ele agrupa todos os componentes físicos (pontos, equipamentos, trechos, prumadas) em um grafo navegável, permitindo operações globais como dimensionamento, validação de conectividade e geração de MEP Systems no Revit.

### 1.2 Papel no sistema

```
PipeNetwork = O SISTEMA COMPLETO

Contém:
  ├── HydraulicPoints (nós terminais)
  ├── EquipmentInfo (aparelhos atendidos)
  ├── PipeSegments (arestas / trechos)
  ├── Risers (colunas verticais)
  └── Fittings (conexões entre trechos)

Gera no Revit:
  → 1 PipingSystem (MEP System)
  → N Pipes (tubulações)
  → N Pipe Fittings (conexões)
```

| Módulo | Uso do PipeNetwork |
|--------|-------------------|
| E07 — Rede AF | Resultado: PipeNetwork de água fria montada |
| E08 — Rede ES | Resultado: PipeNetwork de esgoto montada |
| E10 — Sistemas | Input: criar MEP PipingSystems no Revit |
| E11 — Dimensionamento | Navegar a rede para calcular acumulados |
| E12 — Tabelas | Listar todos os componentes por sistema |
| E13 — Pranchas | Dados para quadros e quantitativos |

### 1.3 Sistema lógico vs. elementos físicos

```
SISTEMA LÓGICO (PipeNetwork):
  - Existe no Core
  - É um grafo de nós e arestas
  - Pode ser validado, dimensionado, serializado
  - Agnóstico ao Revit

ELEMENTOS FÍSICOS (MEP System no Revit):
  - Existe no modelo Revit
  - É um PipingSystem com Pipes e Fittings reais
  - Tem ElementIds, geometria 3D, tipos de família
  - Criado pelo adapter a partir do PipeNetwork

MAPEAMENTO:
  1 PipeNetwork → 1 PipingSystem no Revit
  1 PipeSegment → 1 Pipe no Revit
  1 FittingInfo → 1 PipeFitting no Revit
  1 Riser → N Pipes verticais no Revit
```

---

## 2. Estrutura de Dados — Código C# Completo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa um sistema hidráulico completo como grafo lógico.
    /// Agrupa todos os componentes: pontos, equipamentos, trechos, prumadas.
    /// Base para dimensionamento, validação e geração no Revit.
    /// </summary>
    public class PipeNetwork
    {
        // ── Identidade ──────────────────────────────────────────────

        /// <summary>
        /// ID único do sistema.
        /// Formato: "net_{sistema}_{seq}" (ex: "net_af_001", "net_es_001").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nome de display do sistema (ex: "Água Fria — Residência").
        /// Usado na UI e nos Systems do Revit.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Sistema hidráulico.
        /// </summary>
        public HydraulicSystem System { get; set; }

        /// <summary>
        /// Tipo detalhado do sistema.
        /// </summary>
        public NetworkType Type { get; set; }

        /// <summary>
        /// Se o sistema está ativo.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ── Componentes ─────────────────────────────────────────────

        /// <summary>
        /// Pontos hidráulicos (nós terminais do grafo).
        /// Indexados por Id para acesso O(1).
        /// </summary>
        public Dictionary<string, HydraulicPoint> Points { get; set; } = new();

        /// <summary>
        /// Equipamentos atendidos pelo sistema.
        /// </summary>
        public Dictionary<string, EquipmentInfo> Equipment { get; set; } = new();

        /// <summary>
        /// Trechos de tubulação (arestas do grafo).
        /// Indexados por Id para acesso O(1).
        /// </summary>
        public Dictionary<string, PipeSegment> Segments { get; set; } = new();

        /// <summary>
        /// Prumadas (colunas verticais).
        /// </summary>
        public Dictionary<string, Riser> Risers { get; set; } = new();

        /// <summary>
        /// Ambientes atendidos pelo sistema.
        /// </summary>
        public List<string> ServedRoomIds { get; set; } = new();

        // ── Topologia ───────────────────────────────────────────────

        /// <summary>
        /// Mapa de adjacência: PointId → lista de SegmentIds que partem/chegam.
        /// Permite navegação rápida no grafo.
        /// </summary>
        public Dictionary<string, List<string>> AdjacencyMap { get; set; } = new();

        /// <summary>
        /// ID do ponto raiz do sistema.
        /// AF: ponto do barrilete/reservatório (fonte).
        /// ES: ponto da saída predial (destino final).
        /// </summary>
        public string RootPointId { get; set; }

        // ── Propriedades Globais ────────────────────────────────────

        /// <summary>
        /// Soma total de pesos (AF) ou UHCs (ES) do sistema inteiro.
        /// </summary>
        public double TotalWeight { get; set; }

        /// <summary>
        /// Total de UHCs (ES).
        /// </summary>
        public int TotalUHC { get; set; }

        /// <summary>
        /// Vazão total de projeto do sistema em L/s.
        /// </summary>
        public double TotalFlowRateLs { get; set; }

        /// <summary>
        /// Pressão disponível na origem do sistema em mca (AF).
        /// Depende da altura do reservatório.
        /// </summary>
        public double SourcePressureMca { get; set; }

        /// <summary>
        /// Pressão no ponto mais desfavorável em mca (AF).
        /// Deve ser ≥ pressão mínima do aparelho crítico.
        /// </summary>
        public double CriticalPointPressureMca { get; set; }

        /// <summary>
        /// Comprimento total de tubulação no sistema em metros.
        /// </summary>
        public double TotalPipeLengthM { get; set; }

        // ── Revit ───────────────────────────────────────────────────

        /// <summary>
        /// Nome do PipingSystemType correspondente no Revit.
        /// Ex: "Domestic Cold Water", "Sanitary", "Vent".
        /// </summary>
        public string RevitSystemTypeName { get; set; }

        /// <summary>
        /// ID do PipingSystem criado no Revit (como string).
        /// Preenchido após E10.
        /// </summary>
        public string RevitSystemId { get; set; }

        // ── Dimensionamento ─────────────────────────────────────────

        /// <summary>
        /// Resultado do dimensionamento do sistema.
        /// Preenchido em E11.
        /// </summary>
        public SizingSummary SizingResult { get; set; }

        // ── Status ──────────────────────────────────────────────────

        /// <summary>
        /// Status do sistema no ciclo de vida.
        /// </summary>
        public NetworkStatus Status { get; set; } = NetworkStatus.Building;
    }
}
```

---

## 3. Tipos Auxiliares

### 3.1 SizingSummary

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Resumo do dimensionamento de todo o sistema.
    /// Gerado na etapa E11.
    /// </summary>
    public class SizingSummary
    {
        /// <summary>Timestamp do cálculo.</summary>
        public DateTime CalculatedAt { get; set; }

        /// <summary>Método de cálculo utilizado.</summary>
        public string CalculationMethod { get; set; }

        /// <summary>Se todos os trechos passaram na verificação.</summary>
        public bool AllSegmentsPassed { get; set; }

        /// <summary>Quantidade de trechos dimensionados.</summary>
        public int SegmentsSized { get; set; }

        /// <summary>Quantidade de trechos com erro.</summary>
        public int SegmentsWithErrors { get; set; }

        /// <summary>DN máximo encontrado na rede.</summary>
        public int MaxDiameterMm { get; set; }

        /// <summary>DN mínimo encontrado na rede.</summary>
        public int MinDiameterMm { get; set; }

        /// <summary>Velocidade máxima encontrada (m/s).</summary>
        public double MaxVelocityMs { get; set; }

        /// <summary>Perda de carga total do caminho crítico (mca).</summary>
        public double CriticalPathHeadLossMca { get; set; }

        /// <summary>Comprimento do caminho crítico (m).</summary>
        public double CriticalPathLengthM { get; set; }

        /// <summary>ID do ponto mais desfavorável (AF).</summary>
        public string CriticalPointId { get; set; }

        /// <summary>Pressão no ponto mais desfavorável (mca).</summary>
        public double CriticalPointPressureMca { get; set; }

        /// <summary>Lista de alertas/problemas encontrados.</summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>Quantitativo de materiais.</summary>
        public List<MaterialQuantity> MaterialQuantities { get; set; } = new();
    }

    /// <summary>
    /// Quantitativo de tubulação por DN e material.
    /// </summary>
    public class MaterialQuantity
    {
        public int DiameterMm { get; set; }
        public PipeMaterial Material { get; set; }
        public double TotalLengthM { get; set; }
        public int FittingCount { get; set; }
    }
}
```

---

## 4. Enums

### 4.1 NetworkType

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo detalhado do sistema de rede.
    /// </summary>
    public enum NetworkType
    {
        /// <summary>
        /// Rede de distribuição de água fria (do reservatório aos aparelhos).
        /// Topologia: árvore. Raiz = barrilete. Folhas = aparelhos.
        /// Fluxo: da raiz para as folhas.
        /// </summary>
        ColdWaterDistribution = 1,

        /// <summary>
        /// Rede de alimentação (da rede pública ao reservatório).
        /// Trecho simples, sem ramificações significativas.
        /// </summary>
        ColdWaterFeed = 2,

        /// <summary>
        /// Rede de esgoto sanitário (dos aparelhos até a saída predial).
        /// Topologia: árvore invertida. Folhas = aparelhos. Raiz = saída.
        /// Fluxo: das folhas para a raiz.
        /// </summary>
        SewerSanitary = 10,

        /// <summary>
        /// Rede de ventilação (dos ramais até acima da cobertura).
        /// Paralela ao esgoto. Permite entrada de ar.
        /// </summary>
        Ventilation = 20
    }
}
```

### 4.2 NetworkStatus

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Status do sistema no ciclo de vida do pipeline.
    /// </summary>
    public enum NetworkStatus
    {
        /// <summary>Rede em construção (adicionando trechos).</summary>
        Building = 0,

        /// <summary>Rede montada (topologia completa).</summary>
        Assembled = 1,

        /// <summary>Rede dimensionada (DNs calculados).</summary>
        Sized = 2,

        /// <summary>Rede validada normativamente.</summary>
        Validated = 3,

        /// <summary>Rede gerada no Revit (MEP System criado).</summary>
        Created = 4,

        /// <summary>Erro na rede (desconectada, DN insuficiente).</summary>
        Error = 5
    }
}
```

---

## 5. Métodos do Modelo

```csharp
public class PipeNetwork
{
    // ... propriedades acima ...

    // ── Montagem do grafo ───────────────────────────────────────

    /// <summary>
    /// Adiciona um ponto ao sistema.
    /// </summary>
    public void AddPoint(HydraulicPoint point)
    {
        Points[point.Id] = point;
        if (!AdjacencyMap.ContainsKey(point.Id))
            AdjacencyMap[point.Id] = new List<string>();
    }

    /// <summary>
    /// Adiciona um trecho ao sistema e atualiza o mapa de adjacência.
    /// </summary>
    public void AddSegment(PipeSegment segment)
    {
        Segments[segment.Id] = segment;

        // Atualizar adjacência
        if (!AdjacencyMap.ContainsKey(segment.StartPointId))
            AdjacencyMap[segment.StartPointId] = new();
        AdjacencyMap[segment.StartPointId].Add(segment.Id);

        if (!AdjacencyMap.ContainsKey(segment.EndPointId))
            AdjacencyMap[segment.EndPointId] = new();
        AdjacencyMap[segment.EndPointId].Add(segment.Id);
    }

    /// <summary>
    /// Adiciona uma prumada e seus segmentos ao sistema.
    /// </summary>
    public void AddRiser(Riser riser)
    {
        Risers[riser.Id] = riser;
    }

    /// <summary>
    /// Reconstrói o mapa de adjacência a partir dos segmentos.
    /// Chamar após carregar de JSON.
    /// </summary>
    public void RebuildAdjacencyMap()
    {
        AdjacencyMap.Clear();
        foreach (var seg in Segments.Values)
        {
            if (!AdjacencyMap.ContainsKey(seg.StartPointId))
                AdjacencyMap[seg.StartPointId] = new();
            AdjacencyMap[seg.StartPointId].Add(seg.Id);

            if (!AdjacencyMap.ContainsKey(seg.EndPointId))
                AdjacencyMap[seg.EndPointId] = new();
            AdjacencyMap[seg.EndPointId].Add(seg.Id);
        }
    }

    // ── Navegação no grafo ──────────────────────────────────────

    /// <summary>
    /// Retorna os segmentos que partem/chegam a um ponto.
    /// </summary>
    public List<PipeSegment> GetSegmentsAt(string pointId)
    {
        if (!AdjacencyMap.ContainsKey(pointId)) return new();
        return AdjacencyMap[pointId]
            .Where(sid => Segments.ContainsKey(sid))
            .Select(sid => Segments[sid])
            .ToList();
    }

    /// <summary>
    /// Retorna os segmentos a montante de um ponto (que terminam nele).
    /// </summary>
    public List<PipeSegment> GetUpstreamSegments(string pointId)
    {
        return GetSegmentsAt(pointId)
            .Where(s => s.EndPointId == pointId)
            .ToList();
    }

    /// <summary>
    /// Retorna os segmentos a jusante de um ponto (que partem dele).
    /// </summary>
    public List<PipeSegment> GetDownstreamSegments(string pointId)
    {
        return GetSegmentsAt(pointId)
            .Where(s => s.StartPointId == pointId)
            .ToList();
    }

    /// <summary>
    /// Encontra o caminho entre dois pontos (BFS).
    /// Retorna lista de SegmentIds na ordem do caminho.
    /// </summary>
    public List<string> FindPath(string fromPointId, string toPointId)
    {
        var visited = new HashSet<string>();
        var queue = new Queue<(string pointId, List<string> path)>();
        queue.Enqueue((fromPointId, new List<string>()));

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();
            if (current == toPointId) return path;
            if (visited.Contains(current)) continue;
            visited.Add(current);

            foreach (var seg in GetSegmentsAt(current))
            {
                string nextPoint = seg.StartPointId == current
                    ? seg.EndPointId : seg.StartPointId;
                var newPath = new List<string>(path) { seg.Id };
                queue.Enqueue((nextPoint, newPath));
            }
        }

        return new(); // sem caminho
    }

    /// <summary>
    /// Encontra o caminho crítico (mais longo) do sistema.
    /// AF: da raiz até o ponto mais distante.
    /// ES: do ponto mais distante até a raiz.
    /// </summary>
    public List<string> FindCriticalPath()
    {
        if (string.IsNullOrEmpty(RootPointId)) return new();

        // BFS para encontrar o ponto mais distante da raiz
        var visited = new HashSet<string>();
        var queue = new Queue<(string pointId, List<string> path, double length)>();
        queue.Enqueue((RootPointId, new(), 0));

        string farthestPoint = RootPointId;
        List<string> longestPath = new();
        double maxLength = 0;

        while (queue.Count > 0)
        {
            var (current, path, length) = queue.Dequeue();
            if (visited.Contains(current)) continue;
            visited.Add(current);

            if (length > maxLength)
            {
                maxLength = length;
                farthestPoint = current;
                longestPath = path;
            }

            foreach (var seg in GetSegmentsAt(current))
            {
                string next = seg.StartPointId == current
                    ? seg.EndPointId : seg.StartPointId;
                if (!visited.Contains(next))
                {
                    var newPath = new List<string>(path) { seg.Id };
                    queue.Enqueue((next, newPath, length + seg.LengthM));
                }
            }
        }

        return longestPath;
    }

    // ── Cálculos globais ────────────────────────────────────────

    /// <summary>
    /// Calcula a vazão total do sistema.
    /// AF: Q = 0.3 × √ΣP.
    /// </summary>
    public double CalculateTotalFlowRate()
    {
        if (System == HydraulicSystem.ColdWater)
        {
            double totalWeight = Points.Values
                .Where(p => p.System == HydraulicSystem.ColdWater)
                .Sum(p => p.WeightColdWater);
            TotalWeight = totalWeight;
            TotalFlowRateLs = 0.3 * Math.Sqrt(totalWeight);
        }
        else
        {
            int totalUHC = Points.Values
                .Where(p => p.System == HydraulicSystem.Sewer)
                .Sum(p => p.ContributionUnitsES);
            TotalUHC = totalUHC;
            TotalFlowRateLs = 0.3 * Math.Sqrt(totalUHC);
        }

        return TotalFlowRateLs;
    }

    /// <summary>
    /// Calcula o comprimento total de tubulação.
    /// </summary>
    public double CalculateTotalPipeLength()
    {
        TotalPipeLengthM = Segments.Values.Sum(s => s.LengthM);
        return TotalPipeLengthM;
    }

    /// <summary>
    /// Gera o quantitativo de materiais por DN.
    /// </summary>
    public List<MaterialQuantity> CalculateMaterialQuantities()
    {
        return Segments.Values
            .GroupBy(s => new { s.DiameterMm, s.Material })
            .Select(g => new MaterialQuantity
            {
                DiameterMm = g.Key.DiameterMm,
                Material = g.Key.Material,
                TotalLengthM = g.Sum(s => s.LengthM),
                FittingCount = g.Sum(s => s.Fittings.Count)
            })
            .OrderBy(q => q.DiameterMm)
            .ToList();
    }

    /// <summary>
    /// Retorna os pontos mais desfavoráveis do sistema (AF).
    /// Pontos com menor pressão disponível.
    /// </summary>
    public List<HydraulicPoint> GetCriticalPoints(int topN = 5)
    {
        if (System != HydraulicSystem.ColdWater)
        {
            // ES: pontos com maior UHC ou mais distantes
            return Points.Values
                .Where(p => p.Type == PointType.SewerDischarge)
                .OrderByDescending(p => p.ContributionUnitsES)
                .Take(topN)
                .ToList();
        }

        // AF: pontos com maior pressão mínima requerida
        // (em produção, seria pela pressão calculada, não requerida)
        return Points.Values
            .Where(p => p.Type == PointType.WaterSupply)
            .OrderByDescending(p => p.MinDynamicPressureMca)
            .Take(topN)
            .ToList();
    }

    // ── Validação de conectividade ──────────────────────────────

    /// <summary>
    /// Verifica se todos os pontos do sistema estão conectados (grafo conexo).
    /// </summary>
    public bool IsConnected()
    {
        if (Points.Count == 0) return false;
        if (Segments.Count == 0 && Points.Count > 1) return false;

        var visited = new HashSet<string>();
        var queue = new Queue<string>();
        string startId = Points.Keys.First();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();
            if (visited.Contains(current)) continue;
            visited.Add(current);

            foreach (var seg in GetSegmentsAt(current))
            {
                string next = seg.StartPointId == current
                    ? seg.EndPointId : seg.StartPointId;
                if (!visited.Contains(next))
                    queue.Enqueue(next);
            }
        }

        // Verificar se todos os pontos foram visitados
        return Points.Keys.All(id => visited.Contains(id));
    }

    /// <summary>
    /// Retorna pontos desconectados (sem nenhum segmento).
    /// </summary>
    public List<HydraulicPoint> GetDisconnectedPoints()
    {
        return Points.Values
            .Where(p => !AdjacencyMap.ContainsKey(p.Id) || AdjacencyMap[p.Id].Count == 0)
            .ToList();
    }

    /// <summary>
    /// Retorna pontos com apenas 1 conexão (folhas/terminais).
    /// </summary>
    public List<HydraulicPoint> GetTerminalPoints()
    {
        return Points.Values
            .Where(p => AdjacencyMap.ContainsKey(p.Id) && AdjacencyMap[p.Id].Count == 1)
            .ToList();
    }

    // ── Contagem ────────────────────────────────────────────────

    /// <summary>Número total de pontos.</summary>
    public int PointCount => Points.Count;

    /// <summary>Número total de trechos.</summary>
    public int SegmentCount => Segments.Count;

    /// <summary>Número total de prumadas.</summary>
    public int RiserCount => Risers.Count;

    /// <summary>Número total de equipamentos.</summary>
    public int EquipmentCount => Equipment.Count;

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

        return $"{sys} — {DisplayName} ({SegmentCount} trechos, " +
               $"{TotalPipeLengthM:F1}m, DN{Segments.Values.Max(s => s.DiameterMm)})";
    }

    public override string ToString()
    {
        return $"Network[{Id}] {System}/{Type} — " +
               $"Pts={PointCount} Segs={SegmentCount} Risers={RiserCount} " +
               $"Q={TotalFlowRateLs:F2}L/s L={TotalPipeLengthM:F1}m " +
               $"Status={Status}";
    }
}
```

---

## 6. Validação

```csharp
namespace HidraulicoPlugin.Core.Validation
{
    public class PipeNetworkValidator
    {
        public ValidationReport Validate(PipeNetwork network)
        {
            var report = new ValidationReport();

            // ── Identidade ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(network.Id))
                report.Add(ValidationLevel.Critical, "Rede sem ID");

            // ── Componentes ─────────────────────────────────────
            if (network.PointCount == 0)
                report.Add(ValidationLevel.Critical,
                    $"Rede {network.Id}: sem pontos");

            if (network.SegmentCount == 0)
                report.Add(ValidationLevel.Critical,
                    $"Rede {network.Id}: sem trechos");

            // ── Conectividade ───────────────────────────────────
            if (!network.IsConnected())
                report.Add(ValidationLevel.Critical,
                    $"Rede {network.Id}: grafo desconectado");

            var disconnected = network.GetDisconnectedPoints();
            foreach (var pt in disconnected)
                report.Add(ValidationLevel.Medium,
                    $"Rede {network.Id}: ponto {pt.Id} desconectado");

            // ── Raiz ────────────────────────────────────────────
            if (string.IsNullOrEmpty(network.RootPointId))
                report.Add(ValidationLevel.Medium,
                    $"Rede {network.Id}: sem ponto raiz definido");

            // ── Referências cruzadas ────────────────────────────
            foreach (var seg in network.Segments.Values)
            {
                if (!network.Points.ContainsKey(seg.StartPointId))
                    report.Add(ValidationLevel.Critical,
                        $"Trecho {seg.Id}: StartPoint {seg.StartPointId} não encontrado");

                if (!network.Points.ContainsKey(seg.EndPointId))
                    report.Add(ValidationLevel.Critical,
                        $"Trecho {seg.Id}: EndPoint {seg.EndPointId} não encontrado");
            }

            // ── Ciclos (rede hidráulica é sempre árvore) ────────
            if (HasCycle(network))
                report.Add(ValidationLevel.Critical,
                    $"Rede {network.Id}: contém ciclo (deve ser árvore)");

            // ── DN não decrescente no sentido do fluxo (ES) ─────
            if (network.System == HydraulicSystem.Sewer)
            {
                foreach (var seg in network.Segments.Values)
                {
                    if (!string.IsNullOrEmpty(seg.DownstreamSegmentId)
                        && network.Segments.ContainsKey(seg.DownstreamSegmentId))
                    {
                        var downstream = network.Segments[seg.DownstreamSegmentId];
                        if (!seg.IsDiameterNonDecreasing(downstream))
                            report.Add(ValidationLevel.Critical,
                                $"Trecho {seg.Id}: DN{seg.DiameterMm} → " +
                                $"DN{downstream.DiameterMm} (DN diminui)");
                    }
                }
            }

            // ── Pressão no ponto crítico (AF) ───────────────────
            if (network.System == HydraulicSystem.ColdWater
                && network.SizingResult != null)
            {
                if (network.SizingResult.CriticalPointPressureMca < 0.5)
                    report.Add(ValidationLevel.Critical,
                        $"Rede {network.Id}: pressão no ponto crítico " +
                        $"({network.SizingResult.CriticalPointPressureMca:F2} mca) " +
                        $"abaixo do mínimo (0.5 mca)");
            }

            return report;
        }

        private bool HasCycle(PipeNetwork network)
        {
            var visited = new HashSet<string>();
            var parent = new Dictionary<string, string>();

            foreach (var pointId in network.Points.Keys)
            {
                if (visited.Contains(pointId)) continue;

                var queue = new Queue<string>();
                queue.Enqueue(pointId);
                parent[pointId] = null;

                while (queue.Count > 0)
                {
                    string current = queue.Dequeue();
                    if (visited.Contains(current)) continue;
                    visited.Add(current);

                    foreach (var seg in network.GetSegmentsAt(current))
                    {
                        string next = seg.StartPointId == current
                            ? seg.EndPointId : seg.StartPointId;

                        if (visited.Contains(next) && parent[current] != next)
                            return true; // ciclo detectado

                        if (!visited.Contains(next))
                        {
                            parent[next] = current;
                            queue.Enqueue(next);
                        }
                    }
                }
            }

            return false;
        }
    }
}
```

---

## 7. Exemplo — Rede ES de um Banheiro

### 7.1 Estrutura

```
PipeNetwork: net_es_001 "Esgoto Sanitário — Residência"
│
├── Points (9):
│   ├── hp_es_001 (Vaso, SewerDischarge, DN 100)
│   ├── hp_es_002 (Lavatório, SewerDischarge, DN 40)
│   ├── hp_es_003 (Chuveiro, SewerDischarge, DN 40)
│   ├── hp_es_004 (Ralo, FloorDrain, DN 40)
│   ├── hp_acc_001 (CX sifonada, Accessory, DN 75)
│   ├── hp_subcol_001 (Junção subcoletor)
│   └── hp_riser_es_001 (TQ — ponto de prumada)
│
├── Segments (6):
│   ├── seg_es_001: hp_es_002 → hp_acc_001 (DN 40, 1.20m)
│   ├── seg_es_002: hp_es_003 → hp_acc_001 (DN 40, 0.80m)
│   ├── seg_es_003: hp_es_004 → hp_acc_001 (DN 40, 0.50m)
│   ├── seg_es_004: hp_acc_001 → hp_subcol_001 (DN 75, 1.50m)
│   ├── seg_es_005: hp_es_001 → hp_subcol_001 (DN 100, 2.00m)
│   └── seg_es_006: hp_subcol_001 → hp_riser_es_001 (DN 100, 3.00m)
│
├── Risers (1):
│   └── riser_es_001 (TQ-01, DN 100)
│
├── RootPointId: "hp_riser_es_001" (saída)
│
└── Topologia:
    Lav ──┐
    Chu ──┼── CX ──┐
    Ralo ─┘        ├── Subcoletor ──→ TQ
    Vaso ──────────┘
```

---

## 8. JSON de Exemplo

```json
{
  "id": "net_es_001",
  "display_name": "Esgoto Sanitário — Residência",
  "system": "Sewer",
  "type": "SewerSanitary",
  "is_active": true,
  "root_point_id": "hp_riser_es_001",
  "total_weight": 0,
  "total_uhc": 11,
  "total_flow_rate_ls": 0.99,
  "total_pipe_length_m": 9.00,
  "revit_system_type_name": "Sanitary",
  "revit_system_id": null,
  "served_room_ids": ["423567"],
  "status": "Assembled",
  "sizing_result": null,
  "point_count": 7,
  "segment_count": 6,
  "riser_count": 1,
  "equipment_count": 4
}
```

---

## 9. Resumo Visual

```
PipeNetwork
├── Identidade
│   ├── Id (string — "net_{sys}_{seq}")
│   ├── DisplayName (string)
│   ├── System (HydraulicSystem)
│   ├── Type (NetworkType — 4 valores)
│   └── IsActive (bool)
├── Componentes (Dictionaries para O(1))
│   ├── Points (Dict<string, HydraulicPoint>)
│   ├── Equipment (Dict<string, EquipmentInfo>)
│   ├── Segments (Dict<string, PipeSegment>)
│   ├── Risers (Dict<string, Riser>)
│   └── ServedRoomIds (List<string>)
├── Topologia
│   ├── AdjacencyMap (Dict<string, List<string>>)
│   └── RootPointId (string)
├── Globais
│   ├── TotalWeight / TotalUHC
│   ├── TotalFlowRateLs
│   ├── SourcePressureMca / CriticalPointPressureMca
│   └── TotalPipeLengthM
├── Revit
│   ├── RevitSystemTypeName
│   └── RevitSystemId
├── Dimensionamento
│   └── SizingSummary (resultado E11)
├── Status
│   └── NetworkStatus (6 valores)
└── Métodos
    ├── AddPoint() / AddSegment() / AddRiser()
    ├── RebuildAdjacencyMap()
    ├── GetSegmentsAt() / GetUpstream() / GetDownstream()
    ├── FindPath(from, to) — BFS
    ├── FindCriticalPath()
    ├── CalculateTotalFlowRate()
    ├── CalculateTotalPipeLength()
    ├── CalculateMaterialQuantities()
    ├── GetCriticalPoints(topN)
    ├── IsConnected() — BFS
    ├── GetDisconnectedPoints()
    ├── GetTerminalPoints()
    ├── GetDisplayName()
    └── ToString()
```
