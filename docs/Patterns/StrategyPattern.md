# Padrão de Decisão — Strategy por Tipo de Ambiente

> Especificação completa do padrão Strategy aplicado a decisões baseadas em `RoomType`, eliminando condicionais complexas e permitindo expansão futura, para uso no PluginCore.

---

## 1. Definição do Padrão

### 1.1 O que é Strategy

O padrão **Strategy** (GoF) encapsula uma família de algoritmos em classes separadas, permitindo que sejam intercambiáveis. O cliente (serviço) delega a decisão para a estratégia correta sem saber qual implementação está sendo usada.

### 1.2 Por que usar neste contexto

O sistema hidráulico toma decisões radicalmente diferentes conforme o ambiente:

```
Banheiro → vaso + lavatório + chuveiro + ralo (4 equipamentos, DN 100, ventilação obrigatória)
Cozinha  → pia + lava-louça (2 equipamentos, caixa de gordura, DN 50)
Lavabo   → vaso + lavatório (2 equipamentos, DN 100, compacto)
Sala     → nada (0 equipamentos, skip)
```

Sem Strategy, o código vira uma cascata de `if/switch` que cresce a cada novo ambiente.

### 1.3 Benefícios

| Benefício | Sem Strategy | Com Strategy |
|-----------|-------------|-------------|
| Novo ambiente | Editar 5+ métodos em 3 serviços | Criar 1 classe |
| Testes | Testar todos os branches de cada if | 1 teste por strategy |
| Leitura | 200+ linhas de switch | Cada classe tem 30-50 linhas |
| Manutenção | Risco de quebrar outro ambiente ao editar | Isolamento total |
| Regras de negócio | Espalhadas pelo código | Centralizadas na strategy |

---

## 2. O Problema (Antes)

### 2.1 Código com condicionais

```csharp
// ❌ ANTES: if/switch espalhados por múltiplos serviços

// No IEquipmentService:
public List<EquipmentInfo> GetRequiredEquipment(RoomInfo room)
{
    var equipment = new List<EquipmentInfo>();

    switch (room.RoomType)
    {
        case RoomType.Bathroom:
            equipment.Add(CreateEquipment(EquipmentType.ToiletCoupledTank));
            equipment.Add(CreateEquipment(EquipmentType.Sink));
            equipment.Add(CreateEquipment(EquipmentType.Shower));
            equipment.Add(CreateEquipment(EquipmentType.FloorDrain));
            break;
        case RoomType.Kitchen:
            equipment.Add(CreateEquipment(EquipmentType.KitchenSink));
            // precisa de caixa de gordura?
            if (room.AreaSqM > 6.0) equipment.Add(CreateEquipment(EquipmentType.Dishwasher));
            break;
        case RoomType.Laundry:
            equipment.Add(CreateEquipment(EquipmentType.LaundryTub));
            equipment.Add(CreateEquipment(EquipmentType.WashingMachine));
            break;
        // ... mais 10 cases
    }

    return equipment;
}

// No INetworkService: OUTRO switch igual
public bool NeedsVentilation(RoomInfo room)
{
    switch (room.RoomType)
    {
        case RoomType.Bathroom: return true;   // ← duplicação
        case RoomType.Lavatory: return true;
        case RoomType.Kitchen: return false;
        // ... mesmos cases de novo
    }
}
```

### 2.2 Problemas

```
❌ Duplicação: mesmo switch em 5+ métodos
❌ Fragilidade: adicionar "Banheiro Suíte" exige editar todos os switches
❌ Testabilidade: precisa testar todos os branches juntos
❌ Legibilidade: 200+ linhas de switch por método
❌ Violação OCP: para estender, precisa modificar código existente
```

---

## 3. A Solução (Strategy)

```
╔══════════════════════════════════════════════════════════════════╗
║                    IEquipmentService                             ║
║  GetRequiredEquipment(room)                                      ║
╠══════════════════════════════════════════════════════════════════╣
║                           │                                      ║
║              ┌────────────┴────────────┐                         ║
║              ▼                         ▼                         ║
║   RoomStrategyFactory          IRoomStrategy                     ║
║   GetStrategy(room.Type)       ┌──────────────────┐             ║
║              │                 │ GetEquipment()    │             ║
║              ▼                 │ GetHydPoints()    │             ║
║   ┌──────────────────────┐     │ GetNetworkRules() │             ║
║   │ Dictionary<Type,Str> │     │ Validate()        │             ║
║   │ Bathroom → BathStr   │     └──────────────────┘             ║
║   │ Kitchen  → KitchStr  │            ▲                          ║
║   │ Laundry  → LaundStr  │     ┌──────┼──────┐                  ║
║   │ DryArea  → DryStr    │     │      │      │                   ║
║   └──────────────────────┘     ▼      ▼      ▼                  ║
║                          Bathroom Kitchen Laundry                ║
║                          Strategy Strategy Strategy              ║
╚══════════════════════════════════════════════════════════════════╝
```

---

## 4. Interface IRoomStrategy

```csharp
namespace HidraulicoPlugin.Core.Strategies
{
    /// <summary>
    /// Estratégia de decisão por tipo de ambiente.
    /// Encapsula TODAS as regras específicas de um tipo de ambiente.
    /// </summary>
    public interface IRoomStrategy
    {
        // ══════════════════════════════════════════════════════════
        //  IDENTIDADE
        // ══════════════════════════════════════════════════════════

        /// <summary>Tipo de ambiente que esta estratégia atende.</summary>
        RoomType RoomType { get; }

        /// <summary>Nome legível (ex: "Banheiro").</summary>
        string DisplayName { get; }

        /// <summary>Se o ambiente é considerado "molhado".</summary>
        bool IsWetArea { get; }

        // ══════════════════════════════════════════════════════════
        //  EQUIPAMENTOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna a lista de equipamentos obrigatórios para este tipo de ambiente.
        /// </summary>
        List<EquipmentRequirement> GetRequiredEquipment();

        /// <summary>
        /// Retorna a lista de equipamentos opcionais
        /// (dependem de área, configuração do cliente, etc.).
        /// </summary>
        List<EquipmentRequirement> GetOptionalEquipment();

        // ══════════════════════════════════════════════════════════
        //  PONTOS HIDRÁULICOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna os pontos hidráulicos necessários para este ambiente.
        /// Inclui: tipo (AF, ES, VE), DN, posição relativa.
        /// </summary>
        List<HydraulicPointRequirement> GetHydraulicPoints();

        // ══════════════════════════════════════════════════════════
        //  REGRAS DE REDE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna as regras específicas de rede para este ambiente.
        /// Ex: banheiro precisa de caixa sifonada, cozinha de caixa de gordura.
        /// </summary>
        RoomNetworkRules GetNetworkRules();

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida se o ambiente atende aos requisitos mínimos.
        /// Ex: área mínima, equipamentos presentes, etc.
        /// </summary>
        RoomStrategyValidationResult Validate(RoomInfo room);

        /// <summary>
        /// Valida se os equipamentos presentes estão corretos.
        /// </summary>
        RoomStrategyValidationResult ValidateEquipment(
            RoomInfo room, List<EquipmentInfo> equipment);

        // ══════════════════════════════════════════════════════════
        //  POSICIONAMENTO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Retorna regras de posicionamento para os equipamentos do ambiente.
        /// Ex: vaso a 0.30m da parede, chuveiro centralizado, etc.
        /// </summary>
        List<PositioningRule> GetPositioningRules();
    }
}
```

---

## 5. DTOs de Suporte

```csharp
namespace HidraulicoPlugin.Core.Strategies
{
    /// <summary>
    /// Requisito de equipamento por ambiente.
    /// </summary>
    public class EquipmentRequirement
    {
        public EquipmentType Type { get; set; }
        public string DisplayName { get; set; }
        public int Quantity { get; set; } = 1;
        public bool IsMandatory { get; set; } = true;

        /// <summary>Condição para incluir (ex: área > 6m²).</summary>
        public Func<RoomInfo, bool> Condition { get; set; }

        /// <summary>Peso AF (NBR 5626).</summary>
        public double WeightAF { get; set; }

        /// <summary>UHC esgoto (NBR 8160).</summary>
        public int UHC { get; set; }

        /// <summary>DN do sub-ramal AF (mm).</summary>
        public int SubBranchDN { get; set; }

        /// <summary>DN do ramal de descarga ES (mm).</summary>
        public int DischargeDN { get; set; }
    }

    /// <summary>
    /// Requisito de ponto hidráulico por ambiente.
    /// </summary>
    public class HydraulicPointRequirement
    {
        public HydraulicSystem System { get; set; }
        public EquipmentType AssociatedEquipment { get; set; }
        public PointFunction Function { get; set; }
        public int DiameterMm { get; set; }
        public double HeightFromFloorM { get; set; }
    }

    /// <summary>
    /// Regras de rede específicas do ambiente.
    /// </summary>
    public class RoomNetworkRules
    {
        /// <summary>Precisa de caixa sifonada?</summary>
        public bool RequiresSiphonBox { get; set; }

        /// <summary>Precisa de caixa de gordura?</summary>
        public bool RequiresGreaseBox { get; set; }

        /// <summary>Precisa de ramal de ventilação?</summary>
        public bool RequiresVentilation { get; set; }

        /// <summary>Precisa de ralo de piso?</summary>
        public bool RequiresFloorDrain { get; set; }

        /// <summary>DN mínimo do ramal do ambiente.</summary>
        public int MinBranchDN { get; set; } = 40;

        /// <summary>Tem aparelho com descarga direta (vaso)?</summary>
        public bool HasDirectDischarge { get; set; }

        /// <summary>Pode agrupar ramais em caixa sifonada?</summary>
        public bool CanGroupInSiphonBox { get; set; }
    }

    /// <summary>
    /// Regra de posicionamento.
    /// </summary>
    public class PositioningRule
    {
        public EquipmentType EquipmentType { get; set; }
        public double MinDistanceFromWallM { get; set; }
        public double PreferredDistanceFromWallM { get; set; }
        public double MinSpaceBetweenEquipmentM { get; set; }
        public WallPreference WallPreference { get; set; }
        public double HeightFromFloorM { get; set; }
    }

    public enum WallPreference
    {
        AnyWall,
        LongestWall,
        ShortestWall,
        OppositeToEntrance,
        NearPlumbing
    }

    /// <summary>
    /// Resultado da validação de estratégia.
    /// </summary>
    public class RoomStrategyValidationResult
    {
        public bool IsValid { get; set; }
        public List<RoomStrategyIssue> Issues { get; set; } = new();

        public static RoomStrategyValidationResult Valid() =>
            new() { IsValid = true };

        public static RoomStrategyValidationResult Invalid(params RoomStrategyIssue[] issues) =>
            new() { IsValid = false, Issues = issues.ToList() };
    }

    public class RoomStrategyIssue
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public bool IsCritical { get; set; }

        public RoomStrategyIssue(string code, string msg, bool critical = false)
        {
            Code = code; Message = msg; IsCritical = critical;
        }
    }
}
```

---

## 6. Classe Base

```csharp
namespace HidraulicoPlugin.Core.Strategies
{
    /// <summary>
    /// Classe base para estratégias. Fornece defaults e helpers.
    /// </summary>
    public abstract class RoomStrategyBase : IRoomStrategy
    {
        public abstract RoomType RoomType { get; }
        public abstract string DisplayName { get; }
        public abstract bool IsWetArea { get; }

        public abstract List<EquipmentRequirement> GetRequiredEquipment();

        public virtual List<EquipmentRequirement> GetOptionalEquipment() => new();

        public abstract List<HydraulicPointRequirement> GetHydraulicPoints();

        public abstract RoomNetworkRules GetNetworkRules();

        public abstract List<PositioningRule> GetPositioningRules();

        // ── Validação padrão ────────────────────────────────────

        public virtual RoomStrategyValidationResult Validate(RoomInfo room)
        {
            var issues = new List<RoomStrategyIssue>();

            if (room.AreaSqM <= 0)
                issues.Add(new("AREA_ZERO", "Área deve ser maior que zero", true));

            if (room.HeightM < 2.2)
                issues.Add(new("HEIGHT_LOW", $"Pé-direito {room.HeightM:F2}m < 2.20m mínimo"));

            return issues.Count > 0
                ? RoomStrategyValidationResult.Invalid(issues.ToArray())
                : RoomStrategyValidationResult.Valid();
        }

        public virtual RoomStrategyValidationResult ValidateEquipment(
            RoomInfo room, List<EquipmentInfo> equipment)
        {
            var issues = new List<RoomStrategyIssue>();
            var required = GetRequiredEquipment();

            foreach (var req in required.Where(r => r.IsMandatory))
            {
                int found = equipment.Count(e => e.EquipmentType == req.Type);
                if (found < req.Quantity)
                    issues.Add(new("MISSING_EQUIPMENT",
                        $"Falta {req.DisplayName}: esperado {req.Quantity}, encontrado {found}",
                        true));
            }

            return issues.Count > 0
                ? RoomStrategyValidationResult.Invalid(issues.ToArray())
                : RoomStrategyValidationResult.Valid();
        }

        // ── Helper ──────────────────────────────────────────────

        protected EquipmentRequirement Req(EquipmentType type, string name,
            double weightAF, int uhc, int subDN, int dischDN,
            bool mandatory = true)
        {
            return new EquipmentRequirement
            {
                Type = type, DisplayName = name,
                WeightAF = weightAF, UHC = uhc,
                SubBranchDN = subDN, DischargeDN = dischDN,
                IsMandatory = mandatory
            };
        }

        protected HydraulicPointRequirement Point(HydraulicSystem sys,
            EquipmentType equip, PointFunction func, int dn, double height)
        {
            return new HydraulicPointRequirement
            {
                System = sys, AssociatedEquipment = equip,
                Function = func, DiameterMm = dn,
                HeightFromFloorM = height
            };
        }
    }
}
```

---

## 7. Estratégias Concretas

### 7.1 BathroomStrategy (Banheiro Social)

```csharp
namespace HidraulicoPlugin.Core.Strategies.Rooms
{
    /// <summary>
    /// Estratégia para banheiro social / suíte.
    /// Equipamentos: vaso, lavatório, chuveiro, ralo.
    /// Ventilação obrigatória. Caixa sifonada obrigatória.
    /// </summary>
    public class BathroomStrategy : RoomStrategyBase
    {
        public override RoomType RoomType => RoomType.Bathroom;
        public override string DisplayName => "Banheiro";
        public override bool IsWetArea => true;

        public override List<EquipmentRequirement> GetRequiredEquipment() => new()
        {
            Req(EquipmentType.ToiletCoupledTank, "Vaso sanitário CX acoplada",
                weightAF: 0.3, uhc: 6, subDN: 20, dischDN: 100),
            Req(EquipmentType.Sink, "Lavatório",
                weightAF: 0.3, uhc: 2, subDN: 20, dischDN: 40),
            Req(EquipmentType.Shower, "Chuveiro",
                weightAF: 0.5, uhc: 2, subDN: 20, dischDN: 40),
            Req(EquipmentType.FloorDrain, "Ralo sifonado",
                weightAF: 0.0, uhc: 1, subDN: 0, dischDN: 40)
        };

        public override List<EquipmentRequirement> GetOptionalEquipment() => new()
        {
            new EquipmentRequirement
            {
                Type = EquipmentType.Bidet, DisplayName = "Bidê",
                WeightAF = 0.1, UHC = 1, SubBranchDN = 20, DischargeDN = 40,
                IsMandatory = false,
                Condition = room => room.AreaSqM >= 5.0
            }
        };

        public override List<HydraulicPointRequirement> GetHydraulicPoints() => new()
        {
            // Água fria
            Point(HydraulicSystem.ColdWater, EquipmentType.ToiletCoupledTank,
                PointFunction.Supply, 20, 0.30),
            Point(HydraulicSystem.ColdWater, EquipmentType.Sink,
                PointFunction.Supply, 20, 0.60),
            Point(HydraulicSystem.ColdWater, EquipmentType.Shower,
                PointFunction.Supply, 20, 2.10),

            // Esgoto
            Point(HydraulicSystem.Sewer, EquipmentType.ToiletCoupledTank,
                PointFunction.Discharge, 100, 0.00),
            Point(HydraulicSystem.Sewer, EquipmentType.Sink,
                PointFunction.Discharge, 40, 0.00),
            Point(HydraulicSystem.Sewer, EquipmentType.Shower,
                PointFunction.Discharge, 40, 0.00),
            Point(HydraulicSystem.Sewer, EquipmentType.FloorDrain,
                PointFunction.Discharge, 40, 0.00),

            // Ventilação
            Point(HydraulicSystem.Ventilation, EquipmentType.ToiletCoupledTank,
                PointFunction.Ventilation, 50, 0.00)
        };

        public override RoomNetworkRules GetNetworkRules() => new()
        {
            RequiresSiphonBox = true,
            RequiresGreaseBox = false,
            RequiresVentilation = true,
            RequiresFloorDrain = true,
            MinBranchDN = 50,
            HasDirectDischarge = true,  // vaso liga direto no subcoletor
            CanGroupInSiphonBox = true  // lav + chuv + ralo → CX sifonada
        };

        public override List<PositioningRule> GetPositioningRules() => new()
        {
            new() {
                EquipmentType = EquipmentType.ToiletCoupledTank,
                MinDistanceFromWallM = 0.20,
                PreferredDistanceFromWallM = 0.30,
                MinSpaceBetweenEquipmentM = 0.40,
                WallPreference = WallPreference.ShortestWall,
                HeightFromFloorM = 0.0
            },
            new() {
                EquipmentType = EquipmentType.Sink,
                MinDistanceFromWallM = 0.0,
                PreferredDistanceFromWallM = 0.0,
                MinSpaceBetweenEquipmentM = 0.30,
                WallPreference = WallPreference.AnyWall,
                HeightFromFloorM = 0.80
            },
            new() {
                EquipmentType = EquipmentType.Shower,
                MinDistanceFromWallM = 0.05,
                PreferredDistanceFromWallM = 0.10,
                MinSpaceBetweenEquipmentM = 0.0,
                WallPreference = WallPreference.OppositeToEntrance,
                HeightFromFloorM = 0.0
            }
        };

        public override RoomStrategyValidationResult Validate(RoomInfo room)
        {
            var issues = new List<RoomStrategyIssue>();

            // Herdar validações padrão
            var baseResult = base.Validate(room);
            if (!baseResult.IsValid) issues.AddRange(baseResult.Issues);

            // Regras específicas de banheiro
            if (room.AreaSqM < 2.0)
                issues.Add(new("BATH_AREA_SMALL",
                    $"Banheiro com {room.AreaSqM:F1}m² < 2.0m² mínimo", true));

            if (room.AreaSqM > 20.0)
                issues.Add(new("BATH_AREA_LARGE",
                    $"Banheiro com {room.AreaSqM:F1}m² > 20m² — verificar classificação"));

            return issues.Count > 0
                ? RoomStrategyValidationResult.Invalid(issues.ToArray())
                : RoomStrategyValidationResult.Valid();
        }
    }
}
```

### 7.2 KitchenStrategy (Cozinha)

```csharp
namespace HidraulicoPlugin.Core.Strategies.Rooms
{
    public class KitchenStrategy : RoomStrategyBase
    {
        public override RoomType RoomType => RoomType.Kitchen;
        public override string DisplayName => "Cozinha";
        public override bool IsWetArea => true;

        public override List<EquipmentRequirement> GetRequiredEquipment() => new()
        {
            Req(EquipmentType.KitchenSink, "Pia de cozinha",
                weightAF: 0.7, uhc: 3, subDN: 20, dischDN: 50)
        };

        public override List<EquipmentRequirement> GetOptionalEquipment() => new()
        {
            new()
            {
                Type = EquipmentType.Dishwasher, DisplayName = "Lava-louça",
                WeightAF = 0.3, UHC = 2, SubBranchDN = 20, DischargeDN = 50,
                IsMandatory = false,
                Condition = room => room.AreaSqM >= 8.0
            }
        };

        public override List<HydraulicPointRequirement> GetHydraulicPoints() => new()
        {
            Point(HydraulicSystem.ColdWater, EquipmentType.KitchenSink,
                PointFunction.Supply, 20, 1.00),
            Point(HydraulicSystem.Sewer, EquipmentType.KitchenSink,
                PointFunction.Discharge, 50, 0.00)
        };

        public override RoomNetworkRules GetNetworkRules() => new()
        {
            RequiresSiphonBox = false,
            RequiresGreaseBox = true,
            RequiresVentilation = false,
            RequiresFloorDrain = false,
            MinBranchDN = 50,
            HasDirectDischarge = false,
            CanGroupInSiphonBox = false
        };

        public override List<PositioningRule> GetPositioningRules() => new()
        {
            new() {
                EquipmentType = EquipmentType.KitchenSink,
                MinDistanceFromWallM = 0.0,
                PreferredDistanceFromWallM = 0.0,
                MinSpaceBetweenEquipmentM = 0.60,
                WallPreference = WallPreference.LongestWall,
                HeightFromFloorM = 0.85
            }
        };
    }
}
```

### 7.3 LaundryStrategy (Área de Serviço)

```csharp
namespace HidraulicoPlugin.Core.Strategies.Rooms
{
    public class LaundryStrategy : RoomStrategyBase
    {
        public override RoomType RoomType => RoomType.Laundry;
        public override string DisplayName => "Área de Serviço";
        public override bool IsWetArea => true;

        public override List<EquipmentRequirement> GetRequiredEquipment() => new()
        {
            Req(EquipmentType.LaundryTub, "Tanque",
                weightAF: 0.7, uhc: 3, subDN: 25, dischDN: 40),
            Req(EquipmentType.WashingMachine, "Máquina de lavar",
                weightAF: 0.5, uhc: 3, subDN: 25, dischDN: 50)
        };

        public override List<EquipmentRequirement> GetOptionalEquipment() => new()
        {
            new()
            {
                Type = EquipmentType.FloorDrain, DisplayName = "Ralo",
                WeightAF = 0.0, UHC = 1, SubBranchDN = 0, DischargeDN = 40,
                IsMandatory = false,
                Condition = room => room.AreaSqM >= 4.0
            }
        };

        public override List<HydraulicPointRequirement> GetHydraulicPoints() => new()
        {
            Point(HydraulicSystem.ColdWater, EquipmentType.LaundryTub,
                PointFunction.Supply, 25, 1.00),
            Point(HydraulicSystem.ColdWater, EquipmentType.WashingMachine,
                PointFunction.Supply, 25, 0.80),
            Point(HydraulicSystem.Sewer, EquipmentType.LaundryTub,
                PointFunction.Discharge, 40, 0.00),
            Point(HydraulicSystem.Sewer, EquipmentType.WashingMachine,
                PointFunction.Discharge, 50, 0.00)
        };

        public override RoomNetworkRules GetNetworkRules() => new()
        {
            RequiresSiphonBox = true,
            RequiresGreaseBox = false,
            RequiresVentilation = false,
            RequiresFloorDrain = false,
            MinBranchDN = 40,
            HasDirectDischarge = false,
            CanGroupInSiphonBox = true
        };

        public override List<PositioningRule> GetPositioningRules() => new()
        {
            new() {
                EquipmentType = EquipmentType.LaundryTub,
                MinDistanceFromWallM = 0.0,
                PreferredDistanceFromWallM = 0.0,
                MinSpaceBetweenEquipmentM = 0.50,
                WallPreference = WallPreference.NearPlumbing,
                HeightFromFloorM = 0.0
            },
            new() {
                EquipmentType = EquipmentType.WashingMachine,
                MinDistanceFromWallM = 0.05,
                PreferredDistanceFromWallM = 0.10,
                MinSpaceBetweenEquipmentM = 0.10,
                WallPreference = WallPreference.NearPlumbing,
                HeightFromFloorM = 0.0
            }
        };
    }
}
```

### 7.4 LavatoryStrategy (Lavabo)

```csharp
namespace HidraulicoPlugin.Core.Strategies.Rooms
{
    public class LavatoryStrategy : RoomStrategyBase
    {
        public override RoomType RoomType => RoomType.Lavatory;
        public override string DisplayName => "Lavabo";
        public override bool IsWetArea => true;

        public override List<EquipmentRequirement> GetRequiredEquipment() => new()
        {
            Req(EquipmentType.ToiletCoupledTank, "Vaso sanitário CX acoplada",
                weightAF: 0.3, uhc: 6, subDN: 20, dischDN: 100),
            Req(EquipmentType.Sink, "Lavatório",
                weightAF: 0.3, uhc: 2, subDN: 20, dischDN: 40)
        };

        public override List<HydraulicPointRequirement> GetHydraulicPoints() => new()
        {
            Point(HydraulicSystem.ColdWater, EquipmentType.ToiletCoupledTank,
                PointFunction.Supply, 20, 0.30),
            Point(HydraulicSystem.ColdWater, EquipmentType.Sink,
                PointFunction.Supply, 20, 0.60),
            Point(HydraulicSystem.Sewer, EquipmentType.ToiletCoupledTank,
                PointFunction.Discharge, 100, 0.00),
            Point(HydraulicSystem.Sewer, EquipmentType.Sink,
                PointFunction.Discharge, 40, 0.00),
            Point(HydraulicSystem.Ventilation, EquipmentType.ToiletCoupledTank,
                PointFunction.Ventilation, 50, 0.00)
        };

        public override RoomNetworkRules GetNetworkRules() => new()
        {
            RequiresSiphonBox = false,
            RequiresGreaseBox = false,
            RequiresVentilation = true,
            RequiresFloorDrain = false,
            MinBranchDN = 40,
            HasDirectDischarge = true,
            CanGroupInSiphonBox = false
        };

        public override List<PositioningRule> GetPositioningRules() => new()
        {
            new() {
                EquipmentType = EquipmentType.ToiletCoupledTank,
                MinDistanceFromWallM = 0.15,
                PreferredDistanceFromWallM = 0.20,
                MinSpaceBetweenEquipmentM = 0.30,
                WallPreference = WallPreference.ShortestWall,
                HeightFromFloorM = 0.0
            },
            new() {
                EquipmentType = EquipmentType.Sink,
                MinDistanceFromWallM = 0.0,
                PreferredDistanceFromWallM = 0.0,
                MinSpaceBetweenEquipmentM = 0.25,
                WallPreference = WallPreference.AnyWall,
                HeightFromFloorM = 0.80
            }
        };

        public override RoomStrategyValidationResult Validate(RoomInfo room)
        {
            var issues = new List<RoomStrategyIssue>();
            var baseResult = base.Validate(room);
            if (!baseResult.IsValid) issues.AddRange(baseResult.Issues);

            if (room.AreaSqM < 1.2)
                issues.Add(new("LAV_AREA_SMALL",
                    $"Lavabo com {room.AreaSqM:F1}m² < 1.2m² mínimo", true));

            return issues.Count > 0
                ? RoomStrategyValidationResult.Invalid(issues.ToArray())
                : RoomStrategyValidationResult.Valid();
        }
    }
}
```

### 7.5 DryAreaStrategy (Ambiente Seco)

```csharp
namespace HidraulicoPlugin.Core.Strategies.Rooms
{
    /// <summary>
    /// Estratégia para ambientes secos (sala, quarto, corredor, etc.).
    /// Sem equipamentos, sem rede hidráulica.
    /// </summary>
    public class DryAreaStrategy : RoomStrategyBase
    {
        public override RoomType RoomType => RoomType.Bedroom; // Genérico para secos
        public override string DisplayName => "Ambiente Seco";
        public override bool IsWetArea => false;

        public override List<EquipmentRequirement> GetRequiredEquipment() => new();
        public override List<HydraulicPointRequirement> GetHydraulicPoints() => new();
        public override List<PositioningRule> GetPositioningRules() => new();

        public override RoomNetworkRules GetNetworkRules() => new()
        {
            RequiresSiphonBox = false,
            RequiresGreaseBox = false,
            RequiresVentilation = false,
            RequiresFloorDrain = false,
            MinBranchDN = 0,
            HasDirectDischarge = false,
            CanGroupInSiphonBox = false
        };
    }
}
```

---

## 8. Factory de Estratégias

```csharp
namespace HidraulicoPlugin.Core.Strategies
{
    /// <summary>
    /// Fábrica de estratégias por tipo de ambiente.
    /// Ponto central de resolução: RoomType → IRoomStrategy.
    /// </summary>
    public class RoomStrategyFactory
    {
        private readonly Dictionary<RoomType, IRoomStrategy> _strategies;
        private readonly IRoomStrategy _defaultStrategy;

        public RoomStrategyFactory()
        {
            var bathroom = new BathroomStrategy();
            _defaultStrategy = new DryAreaStrategy();

            _strategies = new Dictionary<RoomType, IRoomStrategy>
            {
                [RoomType.Bathroom] = bathroom,
                [RoomType.MasterBathroom] = bathroom,    // mesma strategy
                [RoomType.Lavatory] = new LavatoryStrategy(),
                [RoomType.Kitchen] = new KitchenStrategy(),
                [RoomType.Laundry] = new LaundryStrategy(),

                // Ambientes secos → todos usam DryAreaStrategy
                [RoomType.Bedroom] = _defaultStrategy,
                [RoomType.LivingRoom] = _defaultStrategy,
                [RoomType.DiningRoom] = _defaultStrategy,
                [RoomType.Office] = _defaultStrategy,
                [RoomType.Hallway] = _defaultStrategy,
                [RoomType.Garage] = _defaultStrategy,
                [RoomType.Balcony] = _defaultStrategy,
            };
        }

        /// <summary>
        /// Retorna a estratégia para um tipo de ambiente.
        /// Se o tipo não estiver mapeado, retorna DryAreaStrategy.
        /// </summary>
        public IRoomStrategy GetStrategy(RoomType roomType)
        {
            return _strategies.TryGetValue(roomType, out var strategy)
                ? strategy
                : _defaultStrategy;
        }

        /// <summary>
        /// Retorna a estratégia para um RoomInfo.
        /// </summary>
        public IRoomStrategy GetStrategy(RoomInfo room)
        {
            return GetStrategy(room.RoomType);
        }

        /// <summary>
        /// Verifica se um tipo tem estratégia registrada.
        /// </summary>
        public bool HasStrategy(RoomType roomType)
        {
            return _strategies.ContainsKey(roomType);
        }

        /// <summary>
        /// Registra uma nova estratégia (extensibilidade).
        /// </summary>
        public void RegisterStrategy(RoomType roomType, IRoomStrategy strategy)
        {
            _strategies[roomType] = strategy;
        }

        /// <summary>
        /// Retorna todos os tipos que são áreas molhadas.
        /// </summary>
        public List<RoomType> GetWetAreaTypes()
        {
            return _strategies
                .Where(kv => kv.Value.IsWetArea)
                .Select(kv => kv.Key)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Retorna resumo de todas as estratégias registradas.
        /// </summary>
        public List<StrategySummary> GetAllStrategies()
        {
            return _strategies
                .GroupBy(kv => kv.Value)
                .Select(g => new StrategySummary
                {
                    StrategyName = g.First().Value.DisplayName,
                    IsWetArea = g.First().Value.IsWetArea,
                    RoomTypes = g.Select(kv => kv.Key).ToList(),
                    RequiredEquipmentCount = g.First().Value.GetRequiredEquipment().Count,
                    RequiresVentilation = g.First().Value.GetNetworkRules().RequiresVentilation
                })
                .ToList();
        }
    }

    public class StrategySummary
    {
        public string StrategyName { get; set; }
        public bool IsWetArea { get; set; }
        public List<RoomType> RoomTypes { get; set; }
        public int RequiredEquipmentCount { get; set; }
        public bool RequiresVentilation { get; set; }
    }
}
```

---

## 9. Integração com Serviços

### 9.1 No IEquipmentService (Antes vs. Depois)

```csharp
// ❌ ANTES (com switch):
public List<EquipmentInfo> GetRequiredEquipment(RoomInfo room)
{
    switch (room.RoomType)
    {
        case RoomType.Bathroom: // 10 linhas
        case RoomType.Kitchen:  // 8 linhas
        case RoomType.Laundry:  // 7 linhas
        // ... 50+ linhas de switch
    }
}

// ✅ DEPOIS (com Strategy):
public class EquipmentService : IEquipmentService
{
    private readonly RoomStrategyFactory _strategyFactory;

    public List<EquipmentInfo> GetRequiredEquipment(RoomInfo room)
    {
        var strategy = _strategyFactory.GetStrategy(room);
        var requirements = strategy.GetRequiredEquipment();

        // Adicionar opcionais se condição atendida
        var optionals = strategy.GetOptionalEquipment()
            .Where(opt => opt.Condition?.Invoke(room) ?? false);

        requirements.AddRange(optionals);

        return requirements.Select(req => CreateEquipmentFromRequirement(req, room))
            .ToList();
    }
}
```

### 9.2 No INetworkService

```csharp
public class NetworkService : INetworkService
{
    private readonly RoomStrategyFactory _strategyFactory;

    public bool NeedsVentilation(RoomInfo room)
    {
        // ✅ Uma linha, zero condicionais
        return _strategyFactory.GetStrategy(room)
            .GetNetworkRules().RequiresVentilation;
    }

    public bool NeedsGreaseBox(RoomInfo room)
    {
        return _strategyFactory.GetStrategy(room)
            .GetNetworkRules().RequiresGreaseBox;
    }

    public int GetMinBranchDN(RoomInfo room)
    {
        return _strategyFactory.GetStrategy(room)
            .GetNetworkRules().MinBranchDN;
    }
}
```

### 9.3 No Pipeline (E03)

```csharp
public class IdentifyEquipmentStep : ProcessingStepBase
{
    private readonly RoomStrategyFactory _strategyFactory;

    protected override StepResult ExecuteCore(PipelineContext context)
    {
        foreach (var room in context.WetAreas)
        {
            var strategy = _strategyFactory.GetStrategy(room);

            // Validar ambiente
            var validation = strategy.Validate(room);
            if (!validation.IsValid) { /* tratar */ }

            // Obter equipamentos
            var required = strategy.GetRequiredEquipment();
            var optional = strategy.GetOptionalEquipment()
                .Where(o => o.Condition?.Invoke(room) ?? false);

            // Obter pontos
            var points = strategy.GetHydraulicPoints();

            // Obter regras de rede
            var rules = strategy.GetNetworkRules();

            context.RequiredEquipmentByRoom[room.Id] =
                required.Concat(optional).ToList();
        }

        return StepResult.Success(StepId, "Equipamentos identificados");
    }
}
```

---

## 10. Tabela Resumo por Estratégia

```
┌─────────────────┬────────────┬──────────────┬───────────┬──────┬──────┬───────┐
│ Estratégia       │ IsWetArea  │ Equipamentos │ Pontos AF │ Vent │ CX S │ CX G  │
├─────────────────┼────────────┼──────────────┼───────────┼──────┼──────┼───────┤
│ Bathroom         │ ✅         │ 4 obrig      │ 3         │ ✅   │ ✅   │ ❌    │
│ Lavatory         │ ✅         │ 2 obrig      │ 2         │ ✅   │ ❌   │ ❌    │
│ Kitchen          │ ✅         │ 1 obrig      │ 1         │ ❌   │ ❌   │ ✅    │
│ Laundry          │ ✅         │ 2 obrig      │ 2         │ ❌   │ ✅   │ ❌    │
│ DryArea          │ ❌         │ 0            │ 0         │ ❌   │ ❌   │ ❌    │
└─────────────────┴────────────┴──────────────┴───────────┴──────┴──────┴───────┘
CX S = Caixa Sifonada, CX G = Caixa de Gordura, Vent = Ventilação
```

---

## 11. Extensibilidade

### Para adicionar novo tipo de ambiente (ex: Piscina):

```csharp
// 1. Criar a Strategy (1 classe)
public class PoolStrategy : RoomStrategyBase
{
    public override RoomType RoomType => RoomType.Pool;
    public override string DisplayName => "Piscina";
    public override bool IsWetArea => true;

    public override List<EquipmentRequirement> GetRequiredEquipment() => new()
    {
        Req(EquipmentType.FloorDrain, "Ralo de fundo",
            weightAF: 0.0, uhc: 5, subDN: 0, dischDN: 75,
            mandatory: true),
        Req(EquipmentType.FloorDrain, "Ralo lateral",
            weightAF: 0.0, uhc: 3, subDN: 0, dischDN: 50,
            mandatory: true)
    };

    // ... implementar outros métodos
}

// 2. Registrar na factory (1 linha)
_strategyFactory.RegisterStrategy(RoomType.Pool, new PoolStrategy());

// 3. Pronto — todos os serviços já usam a nova strategy
//    IEquipmentService, INetworkService, Pipeline, etc.
//    NENHUM switch precisa ser editado.
```

---

## 12. Resumo Visual

```
Strategy Pattern — Decisões por Tipo de Ambiente
│
├── IRoomStrategy (Interface)
│   ├── RoomType, DisplayName, IsWetArea
│   ├── GetRequiredEquipment() → List<EquipmentRequirement>
│   ├── GetOptionalEquipment() → List<EquipmentRequirement>
│   ├── GetHydraulicPoints() → List<HydraulicPointRequirement>
│   ├── GetNetworkRules() → RoomNetworkRules
│   ├── Validate(room) → RoomStrategyValidationResult
│   ├── ValidateEquipment(room, equip) → RoomStrategyValidationResult
│   └── GetPositioningRules() → List<PositioningRule>
│
├── RoomStrategyBase (Classe base)
│   ├── Validação padrão (área, pé-direito)
│   ├── ValidateEquipment (verifica obrigatórios)
│   └── Helpers: Req(), Point()
│
├── Estratégias Concretas (5)
│   ├── BathroomStrategy  → 4 equip, ventilação, CX sifonada
│   ├── LavatoryStrategy  → 2 equip, ventilação, compacto
│   ├── KitchenStrategy   → 1 equip, caixa de gordura
│   ├── LaundryStrategy   → 2 equip, CX sifonada
│   └── DryAreaStrategy   → 0 equip, skip
│
├── RoomStrategyFactory
│   ├── GetStrategy(RoomType) → IRoomStrategy
│   ├── RegisterStrategy(type, strategy)
│   ├── GetWetAreaTypes() → List<RoomType>
│   └── Dictionary: 12 RoomTypes → 5 strategies
│
├── Integração
│   ├── IEquipmentService.GetRequired → strategy.GetRequiredEquipment()
│   ├── INetworkService.NeedsVent → strategy.GetNetworkRules().RequiresVent
│   └── Pipeline.E03 → strategy para cada wetArea
│
└── Extensibilidade
    └── Novo ambiente = 1 classe + 1 RegisterStrategy()
```
