# Serviço de Domínio — IEquipmentService (IEquipamentoService)

> Especificação completa da interface de serviço responsável por definição, geração, posicionamento e validação de equipamentos hidráulicos (aparelhos sanitários), totalmente agnóstica ao Revit, para uso no PluginCore.

---

## 1. Definição da Interface

### 1.1 O que é IEquipmentService

`IEquipmentService` é o **serviço de domínio** que centraliza toda a lógica de negócio relacionada a aparelhos sanitários. Ele determina **quais equipamentos** cada ambiente precisa, **gera** instâncias de `EquipmentInfo` com parâmetros hidráulicos corretos, calcula **posições** dentro do ambiente, e **valida** conformidade com regras normativas e espaciais.

### 1.2 Papel no sistema

```
IRoomService (E01/E02)
    │
    └── List<RoomInfo> (ambientes classificados)
            │
            ╔═══════════════════╗
            ║ IEquipmentService ║  ← ESTE SERVIÇO (Core)
            ╠═══════════════════╣
            ║ GetRequired()     ║
            ║ Generate()        ║
            ║ Position()        ║
            ║ Validate()        ║
            ╚═══════╤═══════════╝
                    │
            List<EquipmentInfo> (equipamentos prontos)
                    │
            IHydraulicPointService (E03 — próximo)
```

| Etapa | Método usado |
|-------|-------------|
| E03 — Identificação | `GetRequiredEquipment()` → define o que cada ambiente precisa |
| E04 — Inserção | `GenerateEquipment()` + `PositionEquipment()` → cria e posiciona |
| E05 — Validação | `ValidateEquipment()` → verifica conformidade |
| Pipeline | `ProcessRoom()` → pipeline completo por ambiente |

### 1.3 Por que é independente do Revit

```
ESTE SERVIÇO:
  - Recebe RoomInfo (agnóstico) → não Room (Revit)
  - Gera EquipmentInfo (agnóstico) → não FamilyInstance (Revit)
  - Calcula posições como Point3D (metros) → não XYZ (pés)
  - Valida com regras de domínio → não com Revit Warnings

QUEM USA O REVIT:
  - RevitEquipmentWriter (adapter na camada Infrastructure)
  - Recebe EquipmentInfo do Core
  - Traduz para FamilyInstance no modelo Revit
  - Retorna o ElementId (como string) para o Core guardar

FLUXO:
  Core decide O QUE e ONDE → Revit cria O ELEMENTO
```

---

## 2. Responsabilidades

### 2.1 O que o serviço DEVE fazer

| Ação | Detalhe |
|------|---------|
| Definir equipamentos obrigatórios | RoomType → List\<EquipmentType\> |
| Definir equipamentos opcionais | Complementares ao obrigatório |
| Gerar instâncias de EquipmentInfo | Com todos os parâmetros hidráulicos pré-preenchidos |
| Calcular posição no ambiente | Coordenadas XYZ baseadas em regras de posicionamento |
| Validar posição | Dentro do bounding box, sem sobreposição |
| Validar parâmetros | DN, peso, UHC, pressão conforme norma |
| Detectar equipamentos existentes | Comparar planejado vs. detectado no modelo |
| Ajustar equipamentos | Corrigir posição ou parâmetros com erro |
| Gerar relatório | Resumo de equipamentos por ambiente |

### 2.2 O que NÃO DEVE fazer

| Proibição | Quem faz |
|-----------|---------|
| ❌ Criar FamilyInstance no Revit | RevitEquipmentWriter (Infrastructure) |
| ❌ Ler elementos do Revit | RevitModelReader (Infrastructure) |
| ❌ Gerar pontos hidráulicos | IHydraulicPointService (outro serviço) |
| ❌ Gerar redes de tubulação | INetworkService (outro serviço) |
| ❌ Dimensionar tubulações | ISizingService (outro serviço) |
| ❌ Acessar UI | Camada UI |

---

## 3. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Serviço de domínio para definição, geração, posicionamento
    /// e validação de equipamentos hidráulicos.
    /// Corresponde às etapas E03 (identificação) e E04 (inserção).
    /// Independente do Revit.
    /// </summary>
    public interface IEquipmentService
    {
        // ── E03: Identificação de Equipamentos ──────────────────

        /// <summary>
        /// Retorna os tipos de equipamentos obrigatórios para um ambiente.
        /// Baseado no RoomType e nas regras normativas.
        /// </summary>
        /// <param name="room">Ambiente classificado.</param>
        /// <returns>Lista de equipamentos obrigatórios e opcionais.</returns>
        EquipmentRequirementResult GetRequiredEquipment(RoomInfo room);

        /// <summary>
        /// Retorna os requisitos de equipamentos para todos os ambientes.
        /// </summary>
        BatchEquipmentRequirementResult GetRequiredEquipmentAll(List<RoomInfo> rooms);

        // ── E04: Geração de Equipamentos ────────────────────────

        /// <summary>
        /// Gera instâncias de EquipmentInfo para um ambiente.
        /// Cria todos os equipamentos obrigatórios com parâmetros hidráulicos
        /// preenchidos conforme a norma (peso AF, UHC ES, DN, pressão).
        /// </summary>
        /// <param name="room">Ambiente onde os equipamentos serão gerados.</param>
        /// <returns>Lista de EquipmentInfo prontos para posicionamento.</returns>
        List<EquipmentInfo> GenerateEquipment(RoomInfo room);

        /// <summary>
        /// Gera equipamentos para todos os ambientes molhados.
        /// </summary>
        EquipmentGenerationResult GenerateEquipmentAll(List<RoomInfo> wetAreas);

        /// <summary>
        /// Cria um único EquipmentInfo com parâmetros normativos padrão.
        /// Factory method para criação direta.
        /// </summary>
        /// <param name="type">Tipo do equipamento.</param>
        /// <param name="roomId">ID do ambiente de destino.</param>
        /// <param name="flushType">Tipo de descarga (para vasos).</param>
        /// <returns>EquipmentInfo com parâmetros preenchidos.</returns>
        EquipmentInfo CreateEquipment(EquipmentType type, string roomId,
            FlushType flushType = FlushType.NotApplicable);

        // ── Posicionamento ──────────────────────────────────────

        /// <summary>
        /// Calcula a posição de um equipamento dentro de um ambiente.
        /// Respeita: distância de paredes, altura padrão, sem sobreposição.
        /// </summary>
        /// <param name="equipment">Equipamento a posicionar.</param>
        /// <param name="room">Ambiente de destino.</param>
        /// <param name="existingEquipment">Equipamentos já posicionados (para evitar sobreposição).</param>
        /// <returns>Resultado com posição calculada.</returns>
        PositioningResult PositionEquipment(
            EquipmentInfo equipment,
            RoomInfo room,
            List<EquipmentInfo> existingEquipment = null);

        /// <summary>
        /// Posiciona todos os equipamentos de um ambiente.
        /// Resolve conflitos de sobreposição automaticamente.
        /// </summary>
        BatchPositioningResult PositionAll(
            List<EquipmentInfo> equipment,
            RoomInfo room);

        /// <summary>
        /// Ajusta a posição de um equipamento que falhou na validação.
        /// Tenta encontrar uma posição alternativa válida.
        /// </summary>
        PositioningResult AdjustPosition(
            EquipmentInfo equipment,
            RoomInfo room,
            List<EquipmentInfo> existingEquipment);

        // ── Validação ───────────────────────────────────────────

        /// <summary>
        /// Valida um equipamento: parâmetros hidráulicos + posição + compatibilidade.
        /// </summary>
        EquipmentValidationResult ValidateEquipment(
            EquipmentInfo equipment,
            RoomInfo room);

        /// <summary>
        /// Valida todos os equipamentos de um ambiente.
        /// </summary>
        BatchEquipmentValidationResult ValidateAll(
            List<EquipmentInfo> equipment,
            RoomInfo room);

        // ── Detecção e Comparação ───────────────────────────────

        /// <summary>
        /// Compara equipamentos planejados (gerados pelo Core)
        /// com equipamentos detectados (encontrados no modelo Revit).
        /// Identifica: faltantes, excedentes, compatíveis.
        /// </summary>
        /// <param name="planned">Equipamentos que deveriam existir.</param>
        /// <param name="detected">Equipamentos já presentes no modelo.</param>
        /// <returns>Relatório de comparação.</returns>
        EquipmentComparisonResult CompareEquipment(
            List<EquipmentInfo> planned,
            List<EquipmentInfo> detected);

        // ── Consultas ───────────────────────────────────────────

        /// <summary>
        /// Retorna os parâmetros hidráulicos padrão para um tipo de equipamento.
        /// Usado para consulta rápida sem gerar uma instância completa.
        /// </summary>
        EquipmentDefaults GetDefaults(EquipmentType type,
            FlushType flushType = FlushType.NotApplicable);

        /// <summary>
        /// Retorna o DN mínimo do sub-ramal AF para um equipamento.
        /// </summary>
        int GetMinSubBranchDN(EquipmentType type);

        /// <summary>
        /// Retorna o DN mínimo do ramal de descarga ES para um equipamento.
        /// </summary>
        int GetMinDischargeDN(EquipmentType type);

        // ── Pipeline Completo ───────────────────────────────────

        /// <summary>
        /// Pipeline completo por ambiente: Identify → Generate → Position → Validate.
        /// </summary>
        EquipmentProcessingResult ProcessRoom(RoomInfo room);

        /// <summary>
        /// Pipeline completo para todos os ambientes molhados.
        /// </summary>
        BatchEquipmentProcessingResult ProcessAll(List<RoomInfo> wetAreas);
    }
}
```

---

## 4. DTOs de Resultado

### 4.1 EquipmentRequirementResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Requisitos de equipamento para um ambiente.
    /// </summary>
    public class EquipmentRequirementResult
    {
        public string RoomId { get; set; }
        public RoomType RoomType { get; set; }

        /// <summary>Equipamentos obrigatórios (devem existir).</summary>
        public List<EquipmentRequirement> Required { get; set; } = new();

        /// <summary>Equipamentos opcionais (podem existir).</summary>
        public List<EquipmentRequirement> Optional { get; set; } = new();

        /// <summary>Acessórios hidráulicos obrigatórios (CX sifonada, CX gordura).</summary>
        public List<EquipmentRequirement> Accessories { get; set; } = new();

        /// <summary>Total de equipamentos obrigatórios.</summary>
        public int TotalRequired => Required.Count;

        /// <summary>Sistemas hidráulicos necessários.</summary>
        public List<HydraulicSystem> RequiredSystems { get; set; } = new();
    }

    public class EquipmentRequirement
    {
        /// <summary>Tipo do equipamento.</summary>
        public EquipmentType Type { get; set; }

        /// <summary>Quantidade mínima.</summary>
        public int MinQuantity { get; set; } = 1;

        /// <summary>Quantidade máxima (0 = sem limite).</summary>
        public int MaxQuantity { get; set; } = 1;

        /// <summary>Tipo de descarga (para vasos).</summary>
        public FlushType FlushType { get; set; } = FlushType.NotApplicable;

        /// <summary>Se é obrigatório.</summary>
        public bool IsMandatory { get; set; }

        /// <summary>Justificativa normativa.</summary>
        public string NormReference { get; set; }
    }

    public class BatchEquipmentRequirementResult
    {
        public List<EquipmentRequirementResult> Results { get; set; } = new();
        public int TotalRooms { get; set; }
        public int TotalEquipmentRequired => Results.Sum(r => r.TotalRequired);
        public TimeSpan ExecutionTime { get; set; }
    }
}
```

### 4.2 EquipmentGenerationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da geração de equipamentos.
    /// </summary>
    public class EquipmentGenerationResult
    {
        /// <summary>Todos os equipamentos gerados.</summary>
        public List<EquipmentInfo> Equipment { get; set; } = new();

        /// <summary>Agrupados por ambiente.</summary>
        public Dictionary<string, List<EquipmentInfo>> ByRoom { get; set; } = new();

        /// <summary>Agrupados por tipo.</summary>
        public Dictionary<EquipmentType, int> CountByType =>
            Equipment.GroupBy(e => e.Type)
                     .ToDictionary(g => g.Key, g => g.Count());

        public int TotalGenerated => Equipment.Count;
        public int TotalRooms => ByRoom.Count;
        public TimeSpan ExecutionTime { get; set; }

        public string GetSummary() =>
            $"Gerados: {TotalGenerated} equipamentos em {TotalRooms} ambientes. " +
            $"Tipos: {string.Join(", ", CountByType.Select(kv => $"{kv.Key}={kv.Value}"))}";
    }
}
```

### 4.3 PositioningResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado do posicionamento de um equipamento.
    /// </summary>
    public class PositioningResult
    {
        public string EquipmentId { get; set; }

        /// <summary>Se o posicionamento foi bem-sucedido.</summary>
        public bool IsSuccessful { get; set; }

        /// <summary>Posição calculada (metros).</summary>
        public Point3D Position { get; set; }

        /// <summary>Rotação em graus (0 = orientação padrão).</summary>
        public double RotationDegrees { get; set; }

        /// <summary>Distância da parede mais próxima em metros.</summary>
        public double WallDistanceM { get; set; }

        /// <summary>Se houve ajuste para evitar sobreposição.</summary>
        public bool WasAdjusted { get; set; }

        /// <summary>Mensagem descritiva.</summary>
        public string Message { get; set; }

        /// <summary>Posições alternativas calculadas (para escolha manual).</summary>
        public List<Point3D> AlternativePositions { get; set; } = new();
    }

    public class BatchPositioningResult
    {
        public List<PositioningResult> Results { get; set; } = new();
        public int TotalPositioned => Results.Count(r => r.IsSuccessful);
        public int TotalFailed => Results.Count(r => !r.IsSuccessful);
        public int TotalAdjusted => Results.Count(r => r.WasAdjusted);
        public TimeSpan ExecutionTime { get; set; }
    }
}
```

### 4.4 EquipmentValidationResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado da validação de um equipamento.
    /// </summary>
    public class EquipmentValidationResult
    {
        public string EquipmentId { get; set; }
        public EquipmentType Type { get; set; }
        public bool IsValid { get; set; }
        public List<EquipmentValidationIssue> Issues { get; set; } = new();
        public int CriticalCount => Issues.Count(i => i.Level == ValidationLevel.Critical);
    }

    public class EquipmentValidationIssue
    {
        public ValidationLevel Level { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string ActualValue { get; set; }
        public string ExpectedValue { get; set; }
        public string NormReference { get; set; }
    }

    public class BatchEquipmentValidationResult
    {
        public List<EquipmentValidationResult> Results { get; set; } = new();
        public int TotalValid => Results.Count(r => r.IsValid);
        public int TotalInvalid => Results.Count(r => !r.IsValid);
        public int TotalCritical => Results.Sum(r => r.CriticalCount);
        public TimeSpan ExecutionTime { get; set; }
    }
}
```

### 4.5 EquipmentComparisonResult

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Comparação entre equipamentos planejados e detectados.
    /// </summary>
    public class EquipmentComparisonResult
    {
        /// <summary>Equipamentos planejados que foram encontrados no modelo.</summary>
        public List<EquipmentMatch> Matched { get; set; } = new();

        /// <summary>Equipamentos planejados que NÃO foram encontrados (faltando).</summary>
        public List<EquipmentInfo> Missing { get; set; } = new();

        /// <summary>Equipamentos no modelo que NÃO estavam no planejamento (excedentes).</summary>
        public List<EquipmentInfo> Surplus { get; set; } = new();

        /// <summary>Se tudo está conforme (sem faltantes ou excedentes).</summary>
        public bool IsComplete => Missing.Count == 0 && Surplus.Count == 0;

        public string GetSummary() =>
            $"Matched: {Matched.Count}, Missing: {Missing.Count}, Surplus: {Surplus.Count}";
    }

    public class EquipmentMatch
    {
        public EquipmentInfo Planned { get; set; }
        public EquipmentInfo Detected { get; set; }
        public double PositionDifferenceM { get; set; }
        public bool TypeMatches { get; set; }
    }
}
```

### 4.6 EquipmentProcessingResult (Pipeline completo)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Resultado do pipeline completo: Identify → Generate → Position → Validate.
    /// </summary>
    public class EquipmentProcessingResult
    {
        public string RoomId { get; set; }
        public EquipmentRequirementResult Requirements { get; set; }
        public List<EquipmentInfo> Equipment { get; set; } = new();
        public BatchPositioningResult Positioning { get; set; }
        public BatchEquipmentValidationResult Validation { get; set; }

        public bool IsSuccessful =>
            Equipment.Count > 0 && Validation.TotalCritical == 0;

        public TimeSpan TotalExecutionTime { get; set; }

        public string GetSummary() =>
            $"Room {RoomId}: {Equipment.Count} equipamentos, " +
            $"{Positioning.TotalPositioned} posicionados, " +
            $"{Validation.TotalValid} válidos. " +
            $"Tempo: {TotalExecutionTime.TotalMilliseconds:F0}ms";
    }

    public class BatchEquipmentProcessingResult
    {
        public List<EquipmentProcessingResult> Results { get; set; } = new();
        public int TotalRooms => Results.Count;
        public int TotalEquipment => Results.Sum(r => r.Equipment.Count);
        public int TotalValid => Results.Sum(r => r.Validation.TotalValid);
        public int TotalFailed => Results.Sum(r => r.Validation.TotalInvalid);
        public bool IsSuccessful => Results.All(r => r.IsSuccessful);
        public TimeSpan TotalExecutionTime { get; set; }

        public string GetSummary() =>
            $"Total: {TotalEquipment} equipamentos em {TotalRooms} ambientes, " +
            $"{TotalValid} válidos, {TotalFailed} com problemas. " +
            $"Tempo: {TotalExecutionTime.TotalSeconds:F1}s";
    }
}
```

---

## 5. Regras de Inserção (Equipamentos por Ambiente)

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Regras de quais equipamentos cada tipo de ambiente precisa.
    /// Fonte: tabelas normativas + práticas de projeto.
    /// </summary>
    public static class EquipmentRequirementRules
    {
        /// <summary>
        /// Retorna os requisitos de equipamento para um tipo de ambiente.
        /// </summary>
        public static EquipmentRequirementResult GetRequirements(RoomType roomType)
        {
            var result = new EquipmentRequirementResult();

            switch (roomType)
            {
                // ── Banheiro completo ───────────────────────────
                case RoomType.Bathroom:
                case RoomType.SuiteBathroom:
                    result.Required.AddRange(new[]
                    {
                        Req(EquipmentType.ToiletCoupledTank, true,
                            "NBR 5626 — aparelho obrigatório", FlushType.CoupledTank),
                        Req(EquipmentType.Sink, true, "NBR 5626"),
                        Req(EquipmentType.Shower, true, "NBR 5626"),
                        Req(EquipmentType.FloorDrain, true, "NBR 8160 — ralo sifonado")
                    });
                    result.Optional.AddRange(new[]
                    {
                        Req(EquipmentType.Bathtub, false, "Opcional"),
                        Req(EquipmentType.Bidet, false, "Opcional")
                    });
                    result.Accessories.AddRange(new[]
                    {
                        Req(EquipmentType.SiphonBox, true,
                            "NBR 8160 — CX sifonada obrigatória para banheiro")
                    });
                    result.RequiredSystems.AddRange(new[]
                    {
                        HydraulicSystem.ColdWater,
                        HydraulicSystem.Sewer,
                        HydraulicSystem.Ventilation
                    });
                    break;

                // ── Lavabo ──────────────────────────────────────
                case RoomType.Lavatory:
                    result.Required.AddRange(new[]
                    {
                        Req(EquipmentType.ToiletCoupledTank, true,
                            "NBR 5626", FlushType.CoupledTank),
                        Req(EquipmentType.Sink, true, "NBR 5626")
                    });
                    result.Accessories.AddRange(new[]
                    {
                        Req(EquipmentType.SiphonBox, true, "NBR 8160")
                    });
                    result.RequiredSystems.AddRange(new[]
                    {
                        HydraulicSystem.ColdWater,
                        HydraulicSystem.Sewer,
                        HydraulicSystem.Ventilation
                    });
                    break;

                // ── Cozinha ─────────────────────────────────────
                case RoomType.Kitchen:
                    result.Required.AddRange(new[]
                    {
                        Req(EquipmentType.KitchenSink, true, "NBR 5626")
                    });
                    result.Optional.AddRange(new[]
                    {
                        Req(EquipmentType.Dishwasher, false, "Opcional")
                    });
                    result.Accessories.AddRange(new[]
                    {
                        Req(EquipmentType.GreaseBox, true,
                            "NBR 8160 — CX gordura obrigatória para cozinha")
                    });
                    result.RequiredSystems.AddRange(new[]
                    {
                        HydraulicSystem.ColdWater,
                        HydraulicSystem.Sewer
                    });
                    break;

                // ── Área de serviço ─────────────────────────────
                case RoomType.ServiceArea:
                    result.Required.AddRange(new[]
                    {
                        Req(EquipmentType.LaundryTub, true, "NBR 5626"),
                        Req(EquipmentType.FloorDrain, true, "NBR 8160")
                    });
                    result.Optional.AddRange(new[]
                    {
                        Req(EquipmentType.WashingMachine, false, "Opcional")
                    });
                    result.Accessories.AddRange(new[]
                    {
                        Req(EquipmentType.SiphonBox, true, "NBR 8160")
                    });
                    result.RequiredSystems.AddRange(new[]
                    {
                        HydraulicSystem.ColdWater,
                        HydraulicSystem.Sewer
                    });
                    break;

                // ── Copa / Kitchenette ──────────────────────────
                case RoomType.Kitchenette:
                    result.Required.AddRange(new[]
                    {
                        Req(EquipmentType.KitchenSink, true, "NBR 5626")
                    });
                    result.RequiredSystems.AddRange(new[]
                    {
                        HydraulicSystem.ColdWater,
                        HydraulicSystem.Sewer
                    });
                    break;

                // ── Varanda molhada ─────────────────────────────
                case RoomType.WetBalcony:
                    result.Required.AddRange(new[]
                    {
                        Req(EquipmentType.FloorDrain, true, "NBR 8160")
                    });
                    result.Optional.AddRange(new[]
                    {
                        Req(EquipmentType.GardenFaucet, false, "Opcional")
                    });
                    result.RequiredSystems.Add(HydraulicSystem.Sewer);
                    break;

                // ── Área externa ────────────────────────────────
                case RoomType.ExternalArea:
                    result.Optional.AddRange(new[]
                    {
                        Req(EquipmentType.GardenFaucet, false, "Opcional"),
                        Req(EquipmentType.FloorDrain, false, "Opcional")
                    });
                    break;
            }

            return result;
        }

        private static EquipmentRequirement Req(EquipmentType type, bool mandatory,
            string norm, FlushType flush = FlushType.NotApplicable) =>
            new()
            {
                Type = type,
                IsMandatory = mandatory,
                NormReference = norm,
                FlushType = flush
            };
    }
}
```

---

## 6. Parâmetros Hidráulicos Padrão (EquipmentDefaults)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Parâmetros hidráulicos padrão de um tipo de equipamento.
    /// Fonte: NBR 5626 Tabela A.1, NBR 8160 Tabela 3.
    /// </summary>
    public class EquipmentDefaults
    {
        public EquipmentType Type { get; set; }
        public FlushType FlushType { get; set; }

        // AF
        public double WeightAF { get; set; }
        public int SubBranchDN { get; set; }
        public double MinDynamicPressureMca { get; set; }

        // ES
        public int ContributionUnitsUHC { get; set; }
        public int DischargeDN { get; set; }
        public bool RequiresIndependentBranch { get; set; }
        public bool ConnectsViaSiphonBox { get; set; }
        public bool RequiresVentilation { get; set; }
        public bool RequiresGreaseBox { get; set; }

        // Posicionamento
        public double DefaultHeightM { get; set; }
        public double MinWallDistanceM { get; set; }
        public double OccupancyWidthM { get; set; }
        public double OccupancyDepthM { get; set; }
    }
}
```

### 6.1 Tabela de defaults

```csharp
public static class EquipmentDefaultsTable
{
    private static readonly Dictionary<(EquipmentType, FlushType), EquipmentDefaults> _defaults = new()
    {
        // ── Vasos ───────────────────────────────────────────────
        [(EquipmentType.ToiletCoupledTank, FlushType.CoupledTank)] = new()
        {
            Type = EquipmentType.ToiletCoupledTank,
            FlushType = FlushType.CoupledTank,
            WeightAF = 0.3, SubBranchDN = 20, MinDynamicPressureMca = 0.5,
            ContributionUnitsUHC = 6, DischargeDN = 100,
            RequiresIndependentBranch = true, ConnectsViaSiphonBox = false,
            RequiresVentilation = true, RequiresGreaseBox = false,
            DefaultHeightM = 0.0, MinWallDistanceM = 0.15,
            OccupancyWidthM = 0.40, OccupancyDepthM = 0.70
        },

        [(EquipmentType.ToiletFlushValve, FlushType.FlushValve)] = new()
        {
            Type = EquipmentType.ToiletFlushValve,
            FlushType = FlushType.FlushValve,
            WeightAF = 32.0, SubBranchDN = 50, MinDynamicPressureMca = 1.2,
            ContributionUnitsUHC = 6, DischargeDN = 100,
            RequiresIndependentBranch = true, ConnectsViaSiphonBox = false,
            RequiresVentilation = true, RequiresGreaseBox = false,
            DefaultHeightM = 0.0, MinWallDistanceM = 0.15,
            OccupancyWidthM = 0.40, OccupancyDepthM = 0.70
        },

        // ── Lavatório ───────────────────────────────────────────
        [(EquipmentType.Sink, FlushType.NotApplicable)] = new()
        {
            Type = EquipmentType.Sink,
            WeightAF = 0.3, SubBranchDN = 20, MinDynamicPressureMca = 0.5,
            ContributionUnitsUHC = 2, DischargeDN = 40,
            RequiresIndependentBranch = false, ConnectsViaSiphonBox = true,
            RequiresVentilation = false, RequiresGreaseBox = false,
            DefaultHeightM = 0.60, MinWallDistanceM = 0.05,
            OccupancyWidthM = 0.50, OccupancyDepthM = 0.45
        },

        // ── Chuveiro ────────────────────────────────────────────
        [(EquipmentType.Shower, FlushType.NotApplicable)] = new()
        {
            Type = EquipmentType.Shower,
            WeightAF = 0.5, SubBranchDN = 20, MinDynamicPressureMca = 1.0,
            ContributionUnitsUHC = 2, DischargeDN = 40,
            RequiresIndependentBranch = false, ConnectsViaSiphonBox = true,
            RequiresVentilation = false, RequiresGreaseBox = false,
            DefaultHeightM = 2.20, MinWallDistanceM = 0.0,
            OccupancyWidthM = 0.90, OccupancyDepthM = 0.90
        },

        // ── Pia de cozinha ──────────────────────────────────────
        [(EquipmentType.KitchenSink, FlushType.NotApplicable)] = new()
        {
            Type = EquipmentType.KitchenSink,
            WeightAF = 0.7, SubBranchDN = 20, MinDynamicPressureMca = 0.5,
            ContributionUnitsUHC = 3, DischargeDN = 50,
            RequiresIndependentBranch = false, ConnectsViaSiphonBox = false,
            RequiresVentilation = false, RequiresGreaseBox = true,
            DefaultHeightM = 0.85, MinWallDistanceM = 0.05,
            OccupancyWidthM = 0.80, OccupancyDepthM = 0.55
        },

        // ── Tanque ──────────────────────────────────────────────
        [(EquipmentType.LaundryTub, FlushType.NotApplicable)] = new()
        {
            Type = EquipmentType.LaundryTub,
            WeightAF = 0.7, SubBranchDN = 25, MinDynamicPressureMca = 0.5,
            ContributionUnitsUHC = 3, DischargeDN = 40,
            RequiresIndependentBranch = false, ConnectsViaSiphonBox = true,
            RequiresVentilation = false, RequiresGreaseBox = false,
            DefaultHeightM = 0.85, MinWallDistanceM = 0.05,
            OccupancyWidthM = 0.60, OccupancyDepthM = 0.55
        },

        // ── Máquina de lavar roupa ──────────────────────────────
        [(EquipmentType.WashingMachine, FlushType.NotApplicable)] = new()
        {
            Type = EquipmentType.WashingMachine,
            WeightAF = 0.5, SubBranchDN = 25, MinDynamicPressureMca = 0.5,
            ContributionUnitsUHC = 3, DischargeDN = 50,
            RequiresIndependentBranch = false, ConnectsViaSiphonBox = false,
            RequiresVentilation = false, RequiresGreaseBox = false,
            DefaultHeightM = 0.90, MinWallDistanceM = 0.05,
            OccupancyWidthM = 0.65, OccupancyDepthM = 0.65
        },

        // ── Ralo sifonado ───────────────────────────────────────
        [(EquipmentType.FloorDrain, FlushType.NotApplicable)] = new()
        {
            Type = EquipmentType.FloorDrain,
            WeightAF = 0.0, SubBranchDN = 0, MinDynamicPressureMca = 0.0,
            ContributionUnitsUHC = 1, DischargeDN = 40,
            RequiresIndependentBranch = false, ConnectsViaSiphonBox = true,
            RequiresVentilation = false, RequiresGreaseBox = false,
            DefaultHeightM = 0.0, MinWallDistanceM = 0.20,
            OccupancyWidthM = 0.15, OccupancyDepthM = 0.15
        },
    };

    public static EquipmentDefaults Get(EquipmentType type,
        FlushType flush = FlushType.NotApplicable)
    {
        if (_defaults.TryGetValue((type, flush), out var def)) return def;
        if (_defaults.TryGetValue((type, FlushType.NotApplicable), out def)) return def;
        return null;
    }
}
```

---

## 7. Regras de Posicionamento

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Regras de posicionamento de equipamentos dentro de ambientes.
    /// </summary>
    public static class EquipmentPositioningRules
    {
        // ── Constantes ──────────────────────────────────────────
        public const double MinWallGapM = 0.05;
        public const double MinEquipGapM = 0.10;
        public const double WallSnapToleranceM = 0.30;

        /// <summary>
        /// Calcula a posição de um equipamento no ambiente.
        /// Estratégia: posicionar junto à parede mais próxima,
        /// respeitando distâncias mínimas.
        /// </summary>
        public static PositioningResult CalculatePosition(
            EquipmentInfo equipment,
            RoomInfo room,
            List<EquipmentInfo> existing,
            EquipmentDefaults defaults)
        {
            // 1. Determinar parede preferencial
            //    Vaso: parede de fundo (maior distância da porta)
            //    Lavatório: parede lateral
            //    Chuveiro: canto

            // 2. Calcular coordenada base
            double x = room.BBoxMinX + defaults.MinWallDistanceM + defaults.OccupancyWidthM / 2;
            double y = room.BBoxMinY + defaults.MinWallDistanceM + defaults.OccupancyDepthM / 2;
            double z = room.LevelElevationM + defaults.DefaultHeightM;

            // 3. Aplicar offset para tipo de equipamento
            var offset = GetTypeOffset(equipment.Type, room, existing);
            x += offset.X;
            y += offset.Y;

            // 4. Verificar sobreposição com existentes
            var position = new Point3D(x, y, z);
            bool overlaps = CheckOverlap(position, defaults, existing);

            if (overlaps)
            {
                // Tentar posição alternativa
                position = FindAlternativePosition(position, defaults, room, existing);
                if (position == null)
                {
                    return new PositioningResult
                    {
                        EquipmentId = equipment.Id,
                        IsSuccessful = false,
                        Message = "Não foi possível encontrar posição sem sobreposição"
                    };
                }
            }

            // 5. Verificar se está dentro do ambiente
            bool insideRoom = IsInsideRoom(position, room);
            if (!insideRoom)
            {
                return new PositioningResult
                {
                    EquipmentId = equipment.Id,
                    IsSuccessful = false,
                    Message = "Posição calculada está fora do ambiente"
                };
            }

            return new PositioningResult
            {
                EquipmentId = equipment.Id,
                IsSuccessful = true,
                Position = position,
                RotationDegrees = CalculateRotation(equipment.Type, room),
                WallDistanceM = DistanceToNearestWall(position, room),
                WasAdjusted = overlaps,
                Message = "Posicionado com sucesso"
            };
        }

        private static (double X, double Y) GetTypeOffset(
            EquipmentType type, RoomInfo room, List<EquipmentInfo> existing)
        {
            // Offsets acumulados baseados em equipamentos já posicionados
            double offsetX = existing.Count * 0.8; // espaçamento simplificado
            return (offsetX, 0);
        }

        private static bool CheckOverlap(Point3D pos, EquipmentDefaults def,
            List<EquipmentInfo> existing)
        {
            foreach (var eq in existing)
            {
                double dx = Math.Abs(pos.X - eq.PositionX);
                double dy = Math.Abs(pos.Y - eq.PositionY);
                if (dx < def.OccupancyWidthM && dy < def.OccupancyDepthM)
                    return true;
            }
            return false;
        }

        private static Point3D FindAlternativePosition(Point3D original,
            EquipmentDefaults def, RoomInfo room, List<EquipmentInfo> existing)
        {
            // Tentar em grid dentro do ambiente
            for (double dx = 0; dx < room.WidthM; dx += 0.3)
            {
                for (double dy = 0; dy < room.DepthM; dy += 0.3)
                {
                    var candidate = new Point3D(
                        room.BBoxMinX + def.MinWallDistanceM + dx,
                        room.BBoxMinY + def.MinWallDistanceM + dy,
                        original.Z);

                    if (!CheckOverlap(candidate, def, existing)
                        && IsInsideRoom(candidate, room))
                        return candidate;
                }
            }
            return null;
        }

        private static bool IsInsideRoom(Point3D pos, RoomInfo room)
        {
            return pos.X >= room.BBoxMinX && pos.X <= room.BBoxMaxX
                && pos.Y >= room.BBoxMinY && pos.Y <= room.BBoxMaxY;
        }

        private static double DistanceToNearestWall(Point3D pos, RoomInfo room)
        {
            double dLeft = pos.X - room.BBoxMinX;
            double dRight = room.BBoxMaxX - pos.X;
            double dBottom = pos.Y - room.BBoxMinY;
            double dTop = room.BBoxMaxY - pos.Y;
            return Math.Min(Math.Min(dLeft, dRight), Math.Min(dBottom, dTop));
        }

        private static double CalculateRotation(EquipmentType type, RoomInfo room)
        {
            // Orientar equipamento voltado para o centro do ambiente
            return 0; // Simplificado — em produção, calcular baseado na parede
        }
    }
}
```

---

## 8. Regras de Validação

```csharp
namespace HidraulicoPlugin.Core.Services
{
    public static class EquipmentValidationRules
    {
        public static EquipmentValidationResult Validate(
            EquipmentInfo equipment, RoomInfo room)
        {
            var result = new EquipmentValidationResult
            {
                EquipmentId = equipment.Id,
                Type = equipment.Type
            };

            // ── Identidade ──────────────────────────────────────
            if (string.IsNullOrWhiteSpace(equipment.Id))
                result.Issues.Add(Critical("EQ_NO_ID", "Equipamento sem ID"));

            // ── Posição ─────────────────────────────────────────
            if (equipment.PositionX == 0 && equipment.PositionY == 0)
                result.Issues.Add(Medium("EQ_NO_POS", "Equipamento sem posição definida"));

            if (room != null && !IsInsideRoom(equipment, room))
                result.Issues.Add(Critical("EQ_OUTSIDE",
                    $"Equipamento fora do ambiente {room.Name}",
                    $"({equipment.PositionX:F2}, {equipment.PositionY:F2})",
                    $"Dentro de [{room.BBoxMinX:F2},{room.BBoxMaxX:F2}]×[{room.BBoxMinY:F2},{room.BBoxMaxY:F2}]"));

            // ── Parâmetros hidráulicos AF ────────────────────────
            if (equipment.WeightColdWater < 0)
                result.Issues.Add(Critical("EQ_WEIGHT",
                    "Peso AF negativo", $"{equipment.WeightColdWater}", "≥ 0"));

            if (equipment.SubBranchDiameterMm > 0 && equipment.SubBranchDiameterMm < 20)
                result.Issues.Add(Critical("EQ_DN_MIN",
                    "DN sub-ramal AF < 20mm",
                    $"DN {equipment.SubBranchDiameterMm}", "≥ DN 20",
                    "NBR 5626, Tabela 3"));

            // ── Parâmetros hidráulicos ES ────────────────────────
            if (equipment.ContributionUnitsES < 0)
                result.Issues.Add(Critical("EQ_UHC",
                    "UHC negativo", $"{equipment.ContributionUnitsES}", "≥ 0"));

            if (equipment.DischargeDiameterMm > 0 && equipment.DischargeDiameterMm < 40)
                result.Issues.Add(Critical("EQ_DN_ES_MIN",
                    "DN ramal descarga < 40mm",
                    $"DN {equipment.DischargeDiameterMm}", "≥ DN 40",
                    "NBR 8160, Tabela 3"));

            // ── Compatibilidade com o ambiente ──────────────────
            if (room != null)
            {
                if (equipment.Type == EquipmentType.KitchenSink
                    && room.ClassifiedType != RoomType.Kitchen
                    && room.ClassifiedType != RoomType.Kitchenette)
                {
                    result.Issues.Add(Medium("EQ_INCOMPAT",
                        $"Pia de cozinha em {room.ClassifiedType} (esperado: Kitchen)"));
                }

                if (equipment.Type == EquipmentType.LaundryTub
                    && room.ClassifiedType != RoomType.ServiceArea)
                {
                    result.Issues.Add(Medium("EQ_INCOMPAT",
                        $"Tanque em {room.ClassifiedType} (esperado: ServiceArea)"));
                }
            }

            result.IsValid = result.CriticalCount == 0;
            return result;
        }

        private static bool IsInsideRoom(EquipmentInfo eq, RoomInfo room)
        {
            return eq.PositionX >= room.BBoxMinX && eq.PositionX <= room.BBoxMaxX
                && eq.PositionY >= room.BBoxMinY && eq.PositionY <= room.BBoxMaxY;
        }

        private static EquipmentValidationIssue Critical(string code, string msg,
            string actual = null, string expected = null, string norm = null) =>
            new() { Level = ValidationLevel.Critical, Code = code,
                    Message = msg, ActualValue = actual,
                    ExpectedValue = expected, NormReference = norm };

        private static EquipmentValidationIssue Medium(string code, string msg) =>
            new() { Level = ValidationLevel.Medium, Code = code, Message = msg };
    }
}
```

---

## 9. Exemplo de Uso (no Orchestrator)

```csharp
public class PipelineOrchestrator
{
    private readonly IRoomService _roomService;
    private readonly IEquipmentService _equipmentService;

    public async Task ExecuteE03_E04(List<RoomInfo> wetAreas)
    {
        // ── OPÇÃO 1: Pipeline completo ──────────────────────────
        var result = _equipmentService.ProcessAll(wetAreas);

        Console.WriteLine(result.GetSummary());
        // → "Total: 15 equipamentos em 5 ambientes, 15 válidos, 0 com problemas. Tempo: 0.5s"

        if (!result.IsSuccessful)
        {
            foreach (var roomResult in result.Results.Where(r => !r.IsSuccessful))
                Console.WriteLine($"  ❌ {roomResult.RoomId}: {roomResult.GetSummary()}");
            return;
        }

        // Todos os equipamentos para próxima etapa
        var allEquipment = result.Results.SelectMany(r => r.Equipment).ToList();

        // ── OPÇÃO 2: Passo a passo por ambiente ─────────────────
        foreach (var room in wetAreas)
        {
            // 1. O que este ambiente precisa?
            var requirements = _equipmentService.GetRequiredEquipment(room);
            // → Banheiro: Vaso + Lavatório + Chuveiro + Ralo + CX sifonada

            // 2. Gerar instâncias
            var equipment = _equipmentService.GenerateEquipment(room);
            // → 5 EquipmentInfo com parâmetros hidráulicos preenchidos

            // 3. Posicionar
            var positioning = _equipmentService.PositionAll(equipment, room);
            // → 5 posicionados com coordenadas XYZ

            // 4. Validar
            var validation = _equipmentService.ValidateAll(equipment, room);
            // → 5 válidos, 0 problemas

            // 5. Consultar parâmetros rápidos
            var defaults = _equipmentService.GetDefaults(EquipmentType.ToiletCoupledTank);
            Console.WriteLine($"Vaso CX: peso={defaults.WeightAF}, UHC={defaults.ContributionUnitsUHC}");
        }
    }
}
```

---

## 10. Resumo Visual

```
IEquipmentService
│
├── Identificação (E03)
│   ├── GetRequiredEquipment(RoomInfo) → EquipmentRequirementResult
│   └── GetRequiredEquipmentAll(List<RoomInfo>) → BatchEquipmentRequirementResult
│
├── Geração (E04)
│   ├── GenerateEquipment(RoomInfo) → List<EquipmentInfo>
│   ├── GenerateEquipmentAll(List<RoomInfo>) → EquipmentGenerationResult
│   └── CreateEquipment(type, roomId, flush) → EquipmentInfo
│
├── Posicionamento
│   ├── PositionEquipment(eq, room, existing) → PositioningResult
│   ├── PositionAll(List<eq>, room) → BatchPositioningResult
│   └── AdjustPosition(eq, room, existing) → PositioningResult
│
├── Validação
│   ├── ValidateEquipment(eq, room) → EquipmentValidationResult
│   └── ValidateAll(List<eq>, room) → BatchEquipmentValidationResult
│
├── Comparação
│   └── CompareEquipment(planned, detected) → EquipmentComparisonResult
│
├── Consultas
│   ├── GetDefaults(type, flush) → EquipmentDefaults
│   ├── GetMinSubBranchDN(type) → int
│   └── GetMinDischargeDN(type) → int
│
├── Pipeline
│   ├── ProcessRoom(RoomInfo) → EquipmentProcessingResult
│   └── ProcessAll(List<RoomInfo>) → BatchEquipmentProcessingResult
│
├── Regras (implementação)
│   ├── EquipmentRequirementRules (RoomType → equipamentos)
│   ├── EquipmentDefaultsTable (parâmetros NBR por tipo)
│   ├── EquipmentPositioningRules (algoritmo XYZ)
│   └── EquipmentValidationRules (norma + espacial)
│
└── Dependências
    ├── RoomInfo (entrada — ambiente processado)
    ├── EquipmentInfo (saída — modelo de domínio)
    └── Enums (EquipmentType, FlushType, RoomType)
```
