# Modelo de Domínio — Riser (Prumada)

> Especificação completa do modelo agnóstico ao Revit que representa colunas verticais de instalações hidráulicas, conectando pavimentos e servindo como eixo estrutural das redes.

---

## 1. Definição do Modelo

### 1.1 O que é Riser

`Riser` é a representação de uma **coluna vertical** que atravessa um ou mais pavimentos, servindo como eixo de distribuição (AF) ou coleta (ES/VE). É o elemento que conecta as redes horizontais de cada andar entre si e com a infraestrutura geral (reservatório, rede pública).

### 1.2 Papel no sistema

```
Reservatório / Barrilete
        │
        └── Riser (coluna AF)          ← ESTE MODELO
                │
                ├── Ramal 1º Pav
                ├── Ramal Térreo
                └── (terminação)

Aparelhos
        │
        └── Ramais horizontais
                │
                └── Riser (TQ esgoto)  ← ESTE MODELO
                        │
                        └── Subcoletor → Rede pública
```

| Módulo | Uso do Riser |
|--------|-------------|
| E06 — Prumadas | Gerado: define posição e DN das colunas verticais |
| E07 — Rede AF | Destino dos ramais (cada ramal conecta à coluna AF) |
| E08 — Rede ES | Destino dos subcoletores (cada subcoletor conecta ao TQ) |
| E10 — Sistemas | Associação ao MEP PipingSystem |
| E11 — Dimensionamento | DN calculado pela soma dos pesos/UHCs de todos os andares |
| E12 — Tabelas | Listagem de prumadas com propriedades |

### 1.3 Prumada vs. PipeSegment

```
PipeSegment = UM trecho de tubo entre dois pontos (qualquer direção)
  - Pode ser horizontal, vertical ou inclinado
  - É a unidade de cálculo
  - Não sabe que faz parte de uma coluna

Riser = UMA COLUNA VERTICAL inteira (conceito lógico)
  - Agrupa N PipeSegments verticais alinhados
  - Tem posição XY fixa
  - Conecta N pavimentos
  - Gerencia as derivações por andar

ANALOGIA:
  PipeSegment = 1 peça de tubo (o tijolo)
  Riser = a coluna inteira do térreo ao último andar (a parede)
```

---

## 2. Estrutura de Dados — Código C# Completo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa uma coluna vertical (prumada) que conecta pavimentos.
    /// Agrupa PipeSegments verticais e gerencia derivações por andar.
    /// Agnóstico ao Revit.
    /// </summary>
    public class Riser
    {
        // ── Identidade ──────────────────────────────────────────────

        /// <summary>
        /// ID único da prumada.
        /// Formato: "riser_{sistema}_{seq}" (ex: "riser_af_001", "riser_es_002").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nome de display da prumada (ex: "Coluna AF-01", "TQ-02").
        /// Em português, para documentação e UI.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Sistema hidráulico desta prumada.
        /// </summary>
        public HydraulicSystem System { get; set; }

        /// <summary>
        /// Tipo de prumada (define função na rede).
        /// </summary>
        public RiserType Type { get; set; }

        /// <summary>
        /// Se a prumada está ativa no projeto.
        /// False = removida ou substituída.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ── Posição ─────────────────────────────────────────────────

        /// <summary>
        /// Coordenada X da prumada em metros (fixa em toda a altura).
        /// Posição no plano do edifício.
        /// </summary>
        public double PositionX { get; set; }

        /// <summary>
        /// Coordenada Y da prumada em metros (fixa em toda a altura).
        /// </summary>
        public double PositionY { get; set; }

        /// <summary>
        /// Elevação da base da prumada em metros (cota Z inferior).
        /// Normalmente = elevação do subsolo ou térreo.
        /// </summary>
        public double BaseElevationM { get; set; }

        /// <summary>
        /// Elevação do topo da prumada em metros (cota Z superior).
        /// AF: pode ser a laje do reservatório.
        /// ES/VE: acima da cobertura.
        /// </summary>
        public double TopElevationM { get; set; }

        /// <summary>
        /// Altura total da prumada em metros.
        /// Calculado: TopElevationM - BaseElevationM.
        /// </summary>
        public double TotalHeightM => TopElevationM - BaseElevationM;

        // ── Propriedades Físicas ────────────────────────────────────

        /// <summary>
        /// DN principal da prumada em mm.
        /// Pode variar por trecho (ex: redução em andares superiores).
        /// Este é o DN predominante ou o maior.
        /// </summary>
        public int MainDiameterMm { get; set; }

        /// <summary>
        /// Material da tubulação.
        /// </summary>
        public PipeMaterial Material { get; set; }

        // ── Pavimentos ──────────────────────────────────────────────

        /// <summary>
        /// Lista de conexões por pavimento (derivações).
        /// Ordenada de baixo para cima.
        /// </summary>
        public List<RiserFloorConnection> FloorConnections { get; set; } = new();

        // ── Segmentos ───────────────────────────────────────────────

        /// <summary>
        /// IDs dos PipeSegments verticais que compõem esta prumada.
        /// Um segmento por trecho entre pavimentos.
        /// </summary>
        public List<string> SegmentIds { get; set; } = new();

        // ── Hidráulica ──────────────────────────────────────────────

        /// <summary>
        /// Soma total dos pesos AF de todos os andares (para dimensionamento).
        /// Apenas para prumadas AF.
        /// </summary>
        public double TotalAccumulatedWeightAF { get; set; }

        /// <summary>
        /// Soma total das UHCs de todos os andares (para dimensionamento).
        /// Apenas para prumadas ES.
        /// </summary>
        public int TotalAccumulatedUHC { get; set; }

        /// <summary>
        /// Vazão total de projeto em L/s.
        /// AF: Q = 0.3 × √ΣP. ES: tabela UHC→Q.
        /// </summary>
        public double TotalFlowRateLs { get; set; }

        /// <summary>
        /// Pressão disponível na base da prumada em mca (AF).
        /// Depende da altura do reservatório e perdas de carga.
        /// </summary>
        public double BasePressureMca { get; set; }

        /// <summary>
        /// Pressão disponível no topo da prumada em mca (AF).
        /// Deve ser ≥ pressão mínima do aparelho mais desfavorável.
        /// </summary>
        public double TopPressureMca { get; set; }

        // ── Ventilação (para prumadas ES) ───────────────────────────

        /// <summary>
        /// Se a prumada de esgoto tem ventilação primária (prolongamento acima do telhado).
        /// Obrigatório para TQ.
        /// </summary>
        public bool HasPrimaryVentilation { get; set; }

        /// <summary>
        /// ID da prumada de ventilação associada (se houver ventilação secundária).
        /// </summary>
        public string AssociatedVentRiserId { get; set; }

        // ── Ambientes ───────────────────────────────────────────────

        /// <summary>
        /// IDs dos ambientes atendidos por esta prumada (todos os andares).
        /// Preenchido na fase de agrupamento (E06).
        /// </summary>
        public List<string> ServedRoomIds { get; set; } = new();

        // ── Revit ───────────────────────────────────────────────────

        /// <summary>
        /// ElementIds dos Pipes verticais no Revit (como strings).
        /// Preenchido após geração.
        /// </summary>
        public List<string> RevitElementIds { get; set; } = new();

        // ── Status ──────────────────────────────────────────────────

        /// <summary>
        /// Status da prumada no ciclo de vida.
        /// </summary>
        public RiserStatus Status { get; set; } = RiserStatus.Planned;
    }
}
```

---

## 3. RiserFloorConnection (Derivação por Pavimento)

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa a conexão da prumada em um pavimento específico.
    /// Cada derivação é o ponto onde os ramais horizontais do andar
    /// se conectam à coluna vertical.
    /// </summary>
    public class RiserFloorConnection
    {
        /// <summary>
        /// Nome do Level (ex: "Térreo", "1º Pavimento").
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// Elevação do Level em metros.
        /// </summary>
        public double LevelElevationM { get; set; }

        /// <summary>
        /// Posição 3D do ponto de derivação.
        /// X/Y = posição da prumada. Z = elevação do Level.
        /// </summary>
        public Point3D ConnectionPosition { get; set; }

        /// <summary>
        /// DN da derivação (tê de saída) em mm.
        /// Pode ser diferente do DN da coluna principal.
        /// </summary>
        public int BranchDiameterMm { get; set; }

        /// <summary>
        /// DN da coluna neste trecho (trecho acima da derivação).
        /// Pode variar: colunas AF podem ter redução nos andares superiores.
        /// </summary>
        public int ColumnDiameterMm { get; set; }

        /// <summary>
        /// Tipo de conexão no encontro (tê, junção, etc.).
        /// </summary>
        public FittingType ConnectionFittingType { get; set; }

        /// <summary>
        /// IDs dos trechos horizontais que partem desta derivação.
        /// (PipeSegment.Id dos ramais que conectam aqui).
        /// </summary>
        public List<string> ConnectedSegmentIds { get; set; } = new();

        /// <summary>
        /// IDs dos HydraulicPoints atendidos neste andar via esta derivação.
        /// </summary>
        public List<string> ServedPointIds { get; set; } = new();

        /// <summary>
        /// IDs dos ambientes atendidos neste andar.
        /// </summary>
        public List<string> ServedRoomIds { get; set; } = new();

        /// <summary>
        /// Soma de pesos AF neste andar (para cálculo acumulado).
        /// </summary>
        public double FloorWeightAF { get; set; }

        /// <summary>
        /// Soma de UHCs neste andar.
        /// </summary>
        public int FloorUHC { get; set; }

        /// <summary>
        /// Peso/UHC acumulado até este andar (soma deste + andares acima/abaixo).
        /// AF: acumulado de cima para baixo.
        /// ES: acumulado de cima para baixo (UHCs dos andares superiores).
        /// </summary>
        public double AccumulatedWeight { get; set; }

        /// <summary>
        /// UHC acumulado até este andar.
        /// </summary>
        public int AccumulatedUHC { get; set; }

        /// <summary>
        /// Pressão disponível nesta derivação em mca (AF).
        /// </summary>
        public double AvailablePressureMca { get; set; }

        /// <summary>
        /// Se a derivação está ativa.
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
```

---

## 4. Enums

### 4.1 RiserType

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo de prumada (define função na rede).
    /// </summary>
    public enum RiserType
    {
        // ── Água Fria ───────────────────────────────────────────

        /// <summary>
        /// Coluna de distribuição de água fria.
        /// Do barrilete até o pavimento mais baixo atendido.
        /// </summary>
        ColdWaterDistribution = 1,

        /// <summary>
        /// Coluna de alimentação direta (do medidor até reservatório superior).
        /// Subida de água. Pode ter pressurizador.
        /// </summary>
        ColdWaterFeed = 2,

        // ── Esgoto ──────────────────────────────────────────────

        /// <summary>
        /// Tubo de queda — coluna vertical de esgoto.
        /// Recebe subcoletores de cada andar.
        /// </summary>
        SewerDropPipe = 10,

        /// <summary>
        /// Prolongamento do tubo de queda acima da cobertura (ventilação primária).
        /// Mesmo DN do TQ. Saída aberta ao ar.
        /// </summary>
        SewerDropPipeExtension = 11,

        // ── Ventilação ──────────────────────────────────────────

        /// <summary>
        /// Coluna de ventilação secundária.
        /// Paralela ao TQ. Recebe ramais de ventilação.
        /// </summary>
        VentColumn = 20,

        /// <summary>
        /// Coluna de ventilação auxiliar (quando a secundária não é suficiente).
        /// </summary>
        VentAuxiliary = 21
    }
}
```

### 4.2 RiserStatus

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Status da prumada no ciclo de vida do pipeline.
    /// </summary>
    public enum RiserStatus
    {
        /// <summary>Prumada planejada (posição e ambientes definidos).</summary>
        Planned = 0,

        /// <summary>DN(s) calculado(s) por dimensionamento.</summary>
        Sized = 1,

        /// <summary>Validada normativamente.</summary>
        Validated = 2,

        /// <summary>Criada no Revit (segmentos verticais + tês).</summary>
        Created = 3,

        /// <summary>Erro no dimensionamento ou validação.</summary>
        Error = 4
    }
}
```

---

## 5. Métodos do Modelo

```csharp
public class Riser
{
    // ... propriedades acima ...

    // ── Consultas de pavimentos ─────────────────────────────────

    /// <summary>
    /// Retorna a lista de Levels atendidos (nomes em ordem de elevação).
    /// </summary>
    public List<string> GetConnectedFloors()
    {
        return FloorConnections
            .OrderBy(fc => fc.LevelElevationM)
            .Select(fc => fc.LevelName)
            .ToList();
    }

    /// <summary>
    /// Retorna o número de pavimentos atendidos.
    /// </summary>
    public int GetFloorCount() => FloorConnections.Count;

    /// <summary>
    /// Retorna a derivação de um pavimento específico.
    /// </summary>
    public RiserFloorConnection GetFloorConnection(string levelName)
    {
        return FloorConnections.FirstOrDefault(fc => fc.LevelName == levelName);
    }

    /// <summary>
    /// Verifica se a prumada atende um pavimento.
    /// </summary>
    public bool ServesFloor(string levelName)
    {
        return FloorConnections.Any(fc => fc.LevelName == levelName);
    }

    // ── Cálculos hidráulicos ────────────────────────────────────

    /// <summary>
    /// Calcula o ΣP total (soma de pesos AF de todos os andares).
    /// </summary>
    public double CalculateTotalWeightAF()
    {
        return FloorConnections.Sum(fc => fc.FloorWeightAF);
    }

    /// <summary>
    /// Calcula o ΣUHC total (soma de UHCs de todos os andares).
    /// </summary>
    public int CalculateTotalUHC()
    {
        return FloorConnections.Sum(fc => fc.FloorUHC);
    }

    /// <summary>
    /// Calcula a vazão total de projeto.
    /// AF: Q = 0.3 × √ΣP (L/s).
    /// ES: tabela UHC→Q (simplificado aqui como raiz).
    /// </summary>
    public double CalculateTotalFlowRate()
    {
        if (System == HydraulicSystem.ColdWater)
        {
            double totalWeight = CalculateTotalWeightAF();
            return 0.3 * Math.Sqrt(totalWeight);
        }
        else
        {
            // Simplificação — em produção, usar tabela NBR 8160
            int totalUHC = CalculateTotalUHC();
            return 0.3 * Math.Sqrt(totalUHC);
        }
    }

    /// <summary>
    /// Recalcula os acumulados por andar (de cima para baixo).
    /// AF: cada andar acumula os pesos dos andares superiores.
    /// ES: cada andar acumula as UHCs dos andares superiores.
    /// </summary>
    public void RecalculateAccumulated()
    {
        var ordered = FloorConnections.OrderByDescending(fc => fc.LevelElevationM).ToList();

        double accWeight = 0;
        int accUHC = 0;

        foreach (var floor in ordered)
        {
            accWeight += floor.FloorWeightAF;
            accUHC += floor.FloorUHC;
            floor.AccumulatedWeight = accWeight;
            floor.AccumulatedUHC = accUHC;
        }
    }

    /// <summary>
    /// Verifica se o DN é suficiente para a vazão total.
    /// Simplificado: compara DN com tabela de capacidade.
    /// </summary>
    public bool IsOverloaded()
    {
        // Capacidade máxima de UHC por DN (TQ esgoto — NBR 8160)
        int maxUHC = MainDiameterMm switch
        {
            40 => 3,
            50 => 6,
            75 => 20,
            100 => 80,
            150 => 300,
            _ => 0
        };

        if (System == HydraulicSystem.Sewer)
            return TotalAccumulatedUHC > maxUHC;

        // AF: capacidade por peso
        double maxWeight = MainDiameterMm switch
        {
            20 => 4,
            25 => 8,
            32 => 15,
            40 => 25,
            50 => 50,
            60 => 90,
            75 => 150,
            _ => 0
        };

        return TotalAccumulatedWeightAF > maxWeight;
    }

    // ── Validações ──────────────────────────────────────────────

    /// <summary>
    /// Verifica se todas as derivações têm pelo menos um segmento conectado.
    /// </summary>
    public bool ValidateConnections()
    {
        return FloorConnections.All(fc =>
            fc.ConnectedSegmentIds.Count > 0 || !fc.IsActive);
    }

    /// <summary>
    /// Verifica se a prumada tem segmentos verticais definidos.
    /// </summary>
    public bool HasSegments() => SegmentIds.Count > 0;

    /// <summary>
    /// Verifica se a prumada de esgoto tem ventilação adequada.
    /// </summary>
    public bool HasAdequateVentilation()
    {
        if (System != HydraulicSystem.Sewer) return true;
        return HasPrimaryVentilation;
    }

    /// <summary>
    /// Verifica consistência: prumada ES com ≥ 3 andares precisa de ventilação secundária.
    /// </summary>
    public bool NeedsSecondaryVentilation()
    {
        if (System != HydraulicSystem.Sewer) return false;
        return GetFloorCount() >= 3 && string.IsNullOrEmpty(AssociatedVentRiserId);
    }

    /// <summary>
    /// Retorna a posição como Point3D na base.
    /// </summary>
    public Point3D GetBasePosition() => new(PositionX, PositionY, BaseElevationM);

    /// <summary>
    /// Retorna a posição como Point3D no topo.
    /// </summary>
    public Point3D GetTopPosition() => new(PositionX, PositionY, TopElevationM);

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

        return $"{sys} {DisplayName} DN{MainDiameterMm} " +
               $"({GetFloorCount()} pav, {TotalHeightM:F1}m)";
    }

    public override string ToString()
    {
        return $"Riser[{Id}] {System}/{Type} DN{MainDiameterMm} " +
               $"H={TotalHeightM:F1}m " +
               $"ΣP={TotalAccumulatedWeightAF:F1} ΣUHC={TotalAccumulatedUHC} " +
               $"Andares={GetFloorCount()} Status={Status}";
    }
}
```

---

## 6. Posicionamento de Prumadas

### 6.1 Regras de agrupamento (E06)

```
REGRA 1: Áreas molhadas adjacentes compartilham prumada
  Banheiro Social + Lavabo (mesma parede) → 1 prumada ES

REGRA 2: Áreas molhadas alinhadas verticalmente compartilham prumada
  Banheiro 1º andar (X=5.2, Y=3.8) ×
  Banheiro Térreo (X=5.3, Y=3.9)   → tolerância 1.0m → mesma prumada

REGRA 3: Cozinha tem prumada separada (caixa de gordura)
  Cozinha → prumada ES própria (com CX gordura)

REGRA 4: Máximo de UHC por prumada define se precisa de outra
  TQ DN 100 → máx 80 UHC. Se ΣUH > 80 → criar segunda prumada

REGRA 5: Cada prumada ES (TQ) precisa de ventilação primária
  O TQ é prolongado acima da cobertura (mesmo DN)

REGRA 6: Prumada AF alimenta todos os andares abaixo do reservatório
  Do barrilete até o térreo
```

### 6.2 Algoritmo de posicionamento

```csharp
public static class RiserPlacementService
{
    /// <summary>
    /// Agrupa ambientes em prumadas com base na posição XY.
    /// Ambientes com centroide dentro da tolerância compartilham prumada.
    /// </summary>
    public static List<Riser> GroupIntoRisers(
        List<RoomInfo> wetAreas,
        HydraulicSystem system,
        double toleranceM = 1.5)
    {
        var risers = new List<Riser>();
        var assigned = new HashSet<string>();
        int seq = 0;

        // Agrupar por proximidade XY entre andares
        foreach (var room in wetAreas.Where(r => r.IsWetArea).OrderBy(r => r.CenterX))
        {
            if (assigned.Contains(room.Id)) continue;

            // Encontrar ambientes alinhados verticalmente
            var aligned = wetAreas
                .Where(r => !assigned.Contains(r.Id)
                    && Math.Abs(r.CenterX - room.CenterX) < toleranceM
                    && Math.Abs(r.CenterY - room.CenterY) < toleranceM)
                .ToList();

            string prefix = system switch
            {
                HydraulicSystem.ColdWater => "af",
                HydraulicSystem.Sewer => "es",
                HydraulicSystem.Ventilation => "ve",
                _ => "xx"
            };

            var riser = new Riser
            {
                Id = $"riser_{prefix}_{++seq:D3}",
                DisplayName = $"{prefix.ToUpper()}-{seq:D2}",
                System = system,
                Type = system switch
                {
                    HydraulicSystem.ColdWater => RiserType.ColdWaterDistribution,
                    HydraulicSystem.Sewer => RiserType.SewerDropPipe,
                    HydraulicSystem.Ventilation => RiserType.VentColumn,
                    _ => RiserType.SewerDropPipe
                },
                PositionX = aligned.Average(r => r.CenterX),
                PositionY = aligned.Average(r => r.CenterY),
                Material = system == HydraulicSystem.ColdWater
                    ? PipeMaterial.PvcSoldavel
                    : PipeMaterial.PvcSerieNormal,
                HasPrimaryVentilation = system == HydraulicSystem.Sewer
            };

            // Criar derivações por andar
            var byLevel = aligned.GroupBy(r => r.LevelName);
            foreach (var levelGroup in byLevel)
            {
                var firstRoom = levelGroup.First();
                riser.FloorConnections.Add(new RiserFloorConnection
                {
                    LevelName = levelGroup.Key,
                    LevelElevationM = firstRoom.LevelElevationM,
                    ConnectionPosition = new Point3D(
                        riser.PositionX, riser.PositionY, firstRoom.LevelElevationM),
                    ServedRoomIds = levelGroup.Select(r => r.Id).ToList()
                });

                riser.ServedRoomIds.AddRange(levelGroup.Select(r => r.Id));
            }

            // Definir elevações
            riser.BaseElevationM = riser.FloorConnections.Min(fc => fc.LevelElevationM);
            riser.TopElevationM = riser.FloorConnections.Max(fc => fc.LevelElevationM) + 3.0;

            foreach (var room in aligned)
                assigned.Add(room.Id);

            risers.Add(riser);
        }

        return risers;
    }
}
```

---

## 7. Validação

```csharp
namespace HidraulicoPlugin.Core.Validation
{
    public class RiserValidator
    {
        public ValidationReport Validate(Riser riser)
        {
            var report = new ValidationReport();

            // ── Identidade ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(riser.Id))
                report.Add(ValidationLevel.Critical, "Prumada sem ID");

            // ── Geometria ───────────────────────────────────────
            if (riser.TotalHeightM <= 0)
                report.Add(ValidationLevel.Critical,
                    $"Prumada {riser.Id}: altura ≤ 0");

            if (riser.TotalHeightM > 50)
                report.Add(ValidationLevel.Light,
                    $"Prumada {riser.Id}: altura muito grande ({riser.TotalHeightM:F1}m)");

            // ── Pavimentos ──────────────────────────────────────
            if (riser.FloorConnections.Count == 0)
                report.Add(ValidationLevel.Critical,
                    $"Prumada {riser.Id}: sem pavimentos conectados");

            // ── DN ──────────────────────────────────────────────
            if (riser.MainDiameterMm <= 0)
                report.Add(ValidationLevel.Critical,
                    $"Prumada {riser.Id}: DN = 0");

            // ── Sobrecarga ──────────────────────────────────────
            if (riser.IsOverloaded())
                report.Add(ValidationLevel.Critical,
                    $"Prumada {riser.Id}: DN{riser.MainDiameterMm} sobrecarregada " +
                    $"(ΣP={riser.TotalAccumulatedWeightAF:F1}, ΣUHC={riser.TotalAccumulatedUHC})");

            // ── Ventilação ES ───────────────────────────────────
            if (riser.System == HydraulicSystem.Sewer)
            {
                if (!riser.HasPrimaryVentilation)
                    report.Add(ValidationLevel.Critical,
                        $"Prumada {riser.Id}: TQ sem ventilação primária");

                if (riser.NeedsSecondaryVentilation())
                    report.Add(ValidationLevel.Medium,
                        $"Prumada {riser.Id}: ≥ 3 andares sem ventilação secundária");
            }

            // ── Ambientes ───────────────────────────────────────
            if (riser.ServedRoomIds.Count == 0)
                report.Add(ValidationLevel.Medium,
                    $"Prumada {riser.Id}: sem ambientes associados");

            return report;
        }
    }
}
```

---

## 8. Tabelas Normativas

### 8.1 Capacidade de tubos de queda (ES — NBR 8160)

| DN TQ (mm) | Máx UHC (sem ventilação) | Máx UHC (com ventilação) |
|-----------|--------------------------|--------------------------|
| 40 | 3 | 5 |
| 50 | 6 | 10 |
| 75 | 15 | 30 |
| 100 | 80 | 180 |
| 150 | 200 | 600 |

### 8.2 Capacidade de colunas AF (NBR 5626)

| DN Coluna (mm) | ΣP máximo | Vazão máx (L/s) |
|---------------|----------|-----------------|
| 20 | 4 | 0.60 |
| 25 | 8 | 0.85 |
| 32 | 15 | 1.16 |
| 40 | 25 | 1.50 |
| 50 | 50 | 2.12 |
| 60 | 90 | 2.85 |
| 75 | 150 | 3.67 |

---

## 9. Exemplo — Prédio 2 andares

### 9.1 Prumadas geradas

```
Edifício residencial — 2 pavimentos (Térreo + 1º Andar)

PRUMADAS DE ESGOTO:
  riser_es_001 — TQ-01 (Banheiros)
    Posição: X=5.2m, Y=3.8m
    DN: 100mm (PVC série normal)
    Térreo: Banheiro Social (6 UHC) + Lavabo (4 UHC) = 10 UHC
    1º Pav: Banheiro Suíte (6 UHC) + Banheiro Social (6 UHC) = 12 UHC
    Total: 22 UHC → DN 100 OK (máx 80)
    Ventilação primária: SIM (prolongamento acima)

  riser_es_002 — TQ-02 (Cozinha)
    Posição: X=8.5m, Y=6.0m
    DN: 75mm (PVC série normal)
    Térreo: Cozinha (3 UHC) com CX gordura
    Total: 3 UHC → DN 75 OK

  riser_es_003 — TQ-03 (Serviço)
    Posição: X=2.0m, Y=1.5m
    DN: 75mm (PVC série normal)
    Térreo: Área serviço (3 UHC)
    Total: 3 UHC → DN 75 OK

PRUMADAS DE ÁGUA FRIA:
  riser_af_001 — CAF-01 (Principal)
    Posição: X=5.0m, Y=4.0m
    DN: 32mm (PVC soldável)
    Barrilete → 1º Pav: ΣP = 1.4
    1º Pav → Térreo: ΣP = 2.8 (acumulado)
    Total: ΣP = 2.8 → Q = 0.50 L/s → DN 25 OK

  riser_af_002 — CAF-02 (Cozinha/Serviço)
    Posição: X=6.0m, Y=5.5m
    DN: 25mm (PVC soldável)
    Total: ΣP = 2.4 → Q = 0.46 L/s → DN 25 OK
```

---

## 10. JSON de Exemplo

```json
{
  "id": "riser_es_001",
  "display_name": "TQ-01",
  "system": "Sewer",
  "type": "SewerDropPipe",
  "is_active": true,
  "position_x": 5.200,
  "position_y": 3.800,
  "base_elevation_m": 0.000,
  "top_elevation_m": 9.500,
  "main_diameter_mm": 100,
  "material": "PvcSerieNormal",
  "floor_connections": [
    {
      "level_name": "Térreo",
      "level_elevation_m": 0.000,
      "connection_position": { "x": 5.200, "y": 3.800, "z": 0.000 },
      "branch_diameter_mm": 100,
      "column_diameter_mm": 100,
      "connection_fitting_type": "Wye45",
      "connected_segment_ids": ["seg_es_006"],
      "served_point_ids": ["hp_es_001", "hp_es_002", "hp_es_003", "hp_es_004"],
      "served_room_ids": ["423567", "423568"],
      "floor_weight_af": 0,
      "floor_uhc": 10,
      "accumulated_weight": 0,
      "accumulated_uhc": 22,
      "available_pressure_mca": 0,
      "is_active": true
    },
    {
      "level_name": "1º Pavimento",
      "level_elevation_m": 3.000,
      "connection_position": { "x": 5.200, "y": 3.800, "z": 3.000 },
      "branch_diameter_mm": 100,
      "column_diameter_mm": 100,
      "connection_fitting_type": "Wye45",
      "connected_segment_ids": ["seg_es_020", "seg_es_021"],
      "served_point_ids": ["hp_es_010", "hp_es_011", "hp_es_012"],
      "served_room_ids": ["425001", "425002"],
      "floor_weight_af": 0,
      "floor_uhc": 12,
      "accumulated_weight": 0,
      "accumulated_uhc": 12,
      "available_pressure_mca": 0,
      "is_active": true
    }
  ],
  "segment_ids": ["seg_es_v001", "seg_es_v002"],
  "total_accumulated_weight_af": 0,
  "total_accumulated_uhc": 22,
  "total_flow_rate_ls": 1.41,
  "base_pressure_mca": 0,
  "top_pressure_mca": 0,
  "has_primary_ventilation": true,
  "associated_vent_riser_id": null,
  "served_room_ids": ["423567", "423568", "425001", "425002"],
  "revit_element_ids": [],
  "status": "Planned"
}
```

---

## 11. Resumo Visual

```
Riser
├── Identidade
│   ├── Id (string — "riser_{sys}_{seq}")
│   ├── DisplayName (string — "TQ-01", "CAF-02")
│   ├── System (HydraulicSystem — AF|ES|VE)
│   ├── Type (RiserType — 6 valores)
│   └── IsActive (bool)
├── Posição
│   ├── PositionX / PositionY (double, metros — fixos)
│   ├── BaseElevationM / TopElevationM (double)
│   └── TotalHeightM (computed)
├── Físicas
│   ├── MainDiameterMm (int)
│   └── Material (PipeMaterial)
├── Pavimentos
│   └── List<RiserFloorConnection>
│       ├── LevelName, LevelElevationM
│       ├── ConnectionPosition (Point3D)
│       ├── BranchDiameterMm / ColumnDiameterMm
│       ├── ConnectionFittingType
│       ├── ConnectedSegmentIds / ServedPointIds / ServedRoomIds
│       ├── FloorWeightAF / FloorUHC
│       ├── AccumulatedWeight / AccumulatedUHC
│       └── AvailablePressureMca
├── Segmentos
│   └── SegmentIds (List<string> → PipeSegment)
├── Hidráulica
│   ├── TotalAccumulatedWeightAF / TotalAccumulatedUHC
│   ├── TotalFlowRateLs
│   ├── BasePressureMca / TopPressureMca
│   ├── HasPrimaryVentilation
│   └── AssociatedVentRiserId
├── Ambientes
│   └── ServedRoomIds (List<string> → RoomInfo)
├── Revit
│   └── RevitElementIds (List<string>)
├── Status
│   └── RiserStatus (5 valores)
└── Métodos
    ├── GetConnectedFloors()
    ├── GetFloorCount()
    ├── GetFloorConnection(level)
    ├── ServesFloor(level)
    ├── CalculateTotalWeightAF()
    ├── CalculateTotalUHC()
    ├── CalculateTotalFlowRate()
    ├── RecalculateAccumulated()
    ├── IsOverloaded()
    ├── ValidateConnections()
    ├── HasSegments()
    ├── HasAdequateVentilation()
    ├── NeedsSecondaryVentilation()
    ├── GetBasePosition() / GetTopPosition()
    ├── GetDisplayName()
    └── ToString()
```
