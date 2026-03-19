# Modelo de Domínio — EquipmentInfo (EquipamentoHidráulico)

> Especificação completa do modelo agnóstico ao Revit que representa aparelhos sanitários e pontos hidráulicos para tomada de decisão, dimensionamento e geração de redes.

---

## 1. Definição do Modelo

### 1.1 O que é EquipmentInfo

`EquipmentInfo` é a representação de um equipamento hidráulico (aparelho sanitário, ponto de consumo ou ponto de coleta) dentro do PluginCore. Abstrai o conceito de `FamilyInstance` do Revit em um objeto puro de domínio.

### 1.2 Papel no sistema

| Módulo | Uso do EquipmentInfo |
|--------|---------------------|
| E03 — Pontos | Lista de equipamentos necessários por ambiente |
| E04 — Inserção | Instruções de inserção no modelo |
| E05 — Validação | Comparação existente vs. necessário |
| E07 — Rede AF | Origem dos sub-ramais de água fria |
| E08 — Rede ES | Origem dos ramais de descarga |
| E09 — Inclinações | Referência para cota de saída do ramal |
| E11 — Dimensionamento | Fonte de pesos (ΣP) e UHCs |

### 1.3 Por que é agnóstico ao Revit

```
EquipmentInfo NÃO contém:
  ❌ ElementId (Autodesk.Revit.DB.ElementId)
  ❌ FamilyInstance
  ❌ Connector (Autodesk.Revit.DB.Connector)
  ❌ XYZ (Autodesk.Revit.DB.XYZ)

EquipmentInfo USA:
  ✅ string para IDs (ElementId.ToString())
  ✅ Point3D para coordenadas (struct própria)
  ✅ EquipmentType enum para tipo
  ✅ ConnectionInfo para conexões (classe própria)
```

---

## 2. Estrutura de Dados — Código C# Completo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa um equipamento hidráulico de forma agnóstica ao Revit.
    /// Pode representar tanto um equipamento planejado (a ser inserido)
    /// quanto um equipamento existente (lido do modelo).
    /// </summary>
    public class EquipmentInfo
    {
        // ── Identidade ──────────────────────────────────────────────

        /// <summary>
        /// ID único do equipamento.
        /// Para existentes: ElementId.ToString() do Revit.
        /// Para planejados: GUID gerado pelo Core (ex: "eq_001").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Nome de display do equipamento (ex: "Vaso sanitário c/ caixa acoplada").
        /// Em português, legível pelo usuário.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Tipo do equipamento classificado pelo sistema.
        /// </summary>
        public EquipmentType Type { get; set; }

        /// <summary>
        /// Se o equipamento já existe no modelo (true) ou é planejado (false).
        /// Planejado = será inserido em E04.
        /// Existente = lido do modelo em E05.
        /// </summary>
        public bool ExistsInModel { get; set; }

        /// <summary>
        /// Se o equipamento está ativo no sistema.
        /// False = foi desativado/ignorado pelo usuário na validação.
        /// </summary>
        public bool IsActive { get; set; } = true;

        // ── Localização ─────────────────────────────────────────────

        /// <summary>
        /// ID do ambiente (RoomInfo.Id) ao qual este equipamento pertence.
        /// </summary>
        public string RoomId { get; set; }

        /// <summary>
        /// Nome do Level onde o equipamento está.
        /// </summary>
        public string LevelName { get; set; }

        /// <summary>
        /// Posição do equipamento em metros (centro da família no plano XY, 
        /// Z = elevação do ponto de instalação).
        /// </summary>
        public Point3D Position { get; set; }

        /// <summary>
        /// Rotação do equipamento em graus (0-360, sentido horário).
        /// 0 = frente voltada para +Y.
        /// </summary>
        public double RotationDeg { get; set; }

        // ── Parâmetros Hidráulicos ──────────────────────────────────

        /// <summary>
        /// Peso relativo para dimensionamento de água fria.
        /// Fonte: NBR 5626, Tabela de pesos.
        /// Exemplo: Vaso c/ caixa acoplada = 0.3
        /// </summary>
        public double WeightColdWater { get; set; }

        /// <summary>
        /// Unidades de Hunter de Contribuição para esgoto.
        /// Fonte: NBR 8160, Tabela de UHCs.
        /// Exemplo: Vaso sanitário = 6 UHC
        /// </summary>
        public int ContributionUnitsES { get; set; }

        /// <summary>
        /// Vazão de projeto em L/s (calculada ou pré-definida).
        /// Para aparelho individual: vazão mínima do sub-ramal.
        /// Ex: Chuveiro = 0.20 L/s
        /// </summary>
        public double DesignFlowRateLs { get; set; }

        /// <summary>
        /// Pressão dinâmica mínima requerida no ponto em mca.
        /// Fonte: NBR 5626. Ex: Chuveiro = 1.0 mca, Válvula descarga = 1.2 mca.
        /// </summary>
        public double MinDynamicPressureMca { get; set; }

        /// <summary>
        /// Se o equipamento usa válvula de descarga (fluxo alto) ou caixa acoplada (fluxo baixo).
        /// Impacta diretamente no peso e no dimensionamento.
        /// </summary>
        public FlushType FlushType { get; set; } = FlushType.NotApplicable;

        // ── Diâmetros Normativos ────────────────────────────────────

        /// <summary>
        /// DN mínimo do sub-ramal de água fria em mm.
        /// Fonte: NBR 5626. Ex: Lavatório = DN 20, Chuveiro = DN 20.
        /// </summary>
        public int MinSubBranchDiameterAfMm { get; set; }

        /// <summary>
        /// DN mínimo do ramal de descarga de esgoto em mm.
        /// Fonte: NBR 8160. Ex: Vaso = DN 100, Lavatório = DN 40.
        /// </summary>
        public int MinDischargeBranchDiameterEsMm { get; set; }

        /// <summary>
        /// DN mínimo do ramal de ventilação em mm (se aplicável).
        /// Fonte: NBR 8160.
        /// </summary>
        public int MinVentDiameterMm { get; set; }

        // ── Conexões ────────────────────────────────────────────────

        /// <summary>
        /// Lista de conexões hidráulicas do equipamento.
        /// Cada conexão tem tipo (AF, ES, VE), direção e posição relativa.
        /// </summary>
        public List<ConnectionInfo> Connections { get; set; } = new();

        // ── Família Revit ───────────────────────────────────────────

        /// <summary>
        /// Nome da família no Revit para inserção (ex: "M_Toilet-Commercial-Wall Mounted-3D").
        /// Usado na etapa E04.
        /// </summary>
        public string RevitFamilyName { get; set; }

        /// <summary>
        /// Nome do tipo (FamilySymbol) no Revit.
        /// </summary>
        public string RevitTypeName { get; set; }

        // ── Validação ───────────────────────────────────────────────

        /// <summary>
        /// Status de validação do equipamento (preenchido em E05).
        /// </summary>
        public EquipmentStatus ValidationStatus { get; set; } = EquipmentStatus.Unknown;

        /// <summary>
        /// Observação da validação (motivo de rejeição, alerta, etc.).
        /// </summary>
        public string ValidationNote { get; set; }
    }
}
```

---

## 3. Enum EquipmentType

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipos de equipamentos hidráulicos residenciais conforme NBR 5626 e NBR 8160.
    /// </summary>
    public enum EquipmentType
    {
        /// <summary>Não identificado.</summary>
        Unknown = 0,

        // ── Banheiro ────────────────────────────────────────────

        /// <summary>Vaso sanitário com caixa acoplada (peso 0.3).</summary>
        ToiletCoupledTank = 1,

        /// <summary>Vaso sanitário com válvula de descarga (peso 32).</summary>
        ToiletFlushValve = 2,

        /// <summary>Lavatório / pia de banheiro (peso 0.3).</summary>
        Sink = 3,

        /// <summary>Chuveiro ou ducha (peso 0.4).</summary>
        Shower = 4,

        /// <summary>Banheira (peso 1.0).</summary>
        Bathtub = 5,

        /// <summary>Bidê (peso 0.1).</summary>
        Bidet = 6,

        /// <summary>Mictório com sifão integrado (peso 0.3).</summary>
        UrinalSiphon = 7,

        /// <summary>Mictório com válvula de descarga (peso 0.5).</summary>
        UrinalFlushValve = 8,

        // ── Cozinha ─────────────────────────────────────────────

        /// <summary>Pia de cozinha (peso 0.7).</summary>
        KitchenSink = 9,

        /// <summary>Máquina de lavar louças (peso 1.0).</summary>
        Dishwasher = 10,

        /// <summary>Filtro / purificador (peso 0.1).</summary>
        WaterFilter = 11,

        // ── Área de Serviço ─────────────────────────────────────

        /// <summary>Tanque de lavar roupas (peso 0.7).</summary>
        LaundryTub = 12,

        /// <summary>Máquina de lavar roupas (peso 1.0).</summary>
        WashingMachine = 13,

        // ── Drenos e Ralos ──────────────────────────────────────

        /// <summary>Ralo sifonado (apenas ES, sem AF).</summary>
        FloorDrain = 14,

        /// <summary>Ralo seco (apenas ES, sem AF).</summary>
        FloorDrainDry = 15,

        // ── Acessórios Hidráulicos ──────────────────────────────

        /// <summary>Caixa sifonada (acessório ES do banheiro).</summary>
        SiphonBox = 16,

        /// <summary>Caixa de gordura (acessório ES da cozinha).</summary>
        GreaseBox = 17,

        // ── Externo ─────────────────────────────────────────────

        /// <summary>Torneira de jardim (peso 0.2).</summary>
        GardenFaucet = 18,

        /// <summary>Torneira de tanque externo (peso 0.4).</summary>
        ExternalFaucet = 19
    }
}
```

---

## 4. Enums Auxiliares

### 4.1 FlushType

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo de descarga — impacta diretamente no peso e dimensionamento.
    /// </summary>
    public enum FlushType
    {
        /// <summary>Não aplicável (equipamento sem vaso).</summary>
        NotApplicable = 0,

        /// <summary>Caixa acoplada (peso baixo, mais comum residencial).</summary>
        CoupledTank = 1,

        /// <summary>Válvula de descarga (peso alto, uso comercial).</summary>
        FlushValve = 2
    }
}
```

### 4.2 EquipmentStatus

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Status de validação de um equipamento no modelo.
    /// </summary>
    public enum EquipmentStatus
    {
        /// <summary>Ainda não validado.</summary>
        Unknown = 0,

        /// <summary>Válido — existe no modelo, tipo correto, posição OK.</summary>
        Valid = 1,

        /// <summary>Válido com ressalvas — existe mas com alerta (posição, tipo próximo).</summary>
        ValidWithRemarks = 2,

        /// <summary>Inválido — existe mas está errado (tipo errado, sem connectors).</summary>
        Invalid = 3,

        /// <summary>Ausente — deveria existir e não foi encontrado.</summary>
        Missing = 4,

        /// <summary>Excedente — existe no modelo mas não é necessário.</summary>
        Surplus = 5
    }
}
```

### 4.3 HydraulicSystem

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Sistemas hidráulicos atendidos.
    /// </summary>
    public enum HydraulicSystem
    {
        /// <summary>Água fria — NBR 5626.</summary>
        ColdWater = 1,

        /// <summary>Esgoto sanitário — NBR 8160.</summary>
        Sewer = 2,

        /// <summary>Ventilação de esgoto — NBR 8160.</summary>
        Ventilation = 3
    }
}
```

---

## 5. Conexões — ConnectionInfo

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Representa uma conexão hidráulica de um equipamento.
    /// Cada equipamento pode ter múltiplas conexões (AF entrada, ES saída, etc.).
    /// </summary>
    public class ConnectionInfo
    {
        /// <summary>
        /// ID único da conexão (ex: "conn_af_001").
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Sistema hidráulico desta conexão.
        /// </summary>
        public HydraulicSystem System { get; set; }

        /// <summary>
        /// Direção do fluxo nesta conexão.
        /// </summary>
        public FlowDirection Direction { get; set; }

        /// <summary>
        /// DN da conexão em mm (ex: 20, 25, 40, 100).
        /// </summary>
        public int DiameterMm { get; set; }

        /// <summary>
        /// Posição do connector em metros (coordenada absoluta).
        /// Preenchido pelo adapter quando equipamento existe no modelo.
        /// </summary>
        public Point3D? Position { get; set; }

        /// <summary>
        /// Offset relativo ao centro do equipamento em metros.
        /// Usado para equipamentos planejados (antes de inserir).
        /// </summary>
        public Point3D Offset { get; set; } = Point3D.Zero;

        /// <summary>
        /// Se esta conexão já está conectada a um Pipe.
        /// </summary>
        public bool IsConnected { get; set; }

        /// <summary>
        /// ElementId do Pipe conectado (como string).
        /// Preenchido pós E07/E08.
        /// </summary>
        public string ConnectedPipeId { get; set; }
    }

    /// <summary>
    /// Direção do fluxo na conexão.
    /// </summary>
    public enum FlowDirection
    {
        /// <summary>Entrada de água (AF entrando no equipamento).</summary>
        In = 1,

        /// <summary>Saída de esgoto (ES saindo do equipamento).</summary>
        Out = 2
    }
}
```

---

## 6. Relação com RoomInfo

### 6.1 Compatibilidade Equipamento × Ambiente

```csharp
namespace HidraulicoPlugin.Core.Models
{
    /// <summary>
    /// Regras de compatibilidade entre equipamentos e ambientes.
    /// </summary>
    public static class EquipmentRoomCompatibility
    {
        /// <summary>
        /// Mapa: RoomType → EquipmentTypes permitidos.
        /// Equipamentos obrigatórios são marcados separadamente no JSON normativo.
        /// </summary>
        public static readonly Dictionary<RoomType, HashSet<EquipmentType>> AllowedEquipment = new()
        {
            [RoomType.Bathroom] = new()
            {
                EquipmentType.ToiletCoupledTank,
                EquipmentType.ToiletFlushValve,
                EquipmentType.Sink,
                EquipmentType.Shower,
                EquipmentType.Bathtub,
                EquipmentType.Bidet,
                EquipmentType.FloorDrain,
                EquipmentType.SiphonBox
            },
            [RoomType.Lavatory] = new()
            {
                EquipmentType.ToiletCoupledTank,
                EquipmentType.Sink,
                EquipmentType.FloorDrain
            },
            [RoomType.Kitchen] = new()
            {
                EquipmentType.KitchenSink,
                EquipmentType.Dishwasher,
                EquipmentType.WaterFilter,
                EquipmentType.FloorDrain,
                EquipmentType.GreaseBox
            },
            [RoomType.GourmetKitchen] = new()
            {
                EquipmentType.KitchenSink,
                EquipmentType.WaterFilter,
                EquipmentType.FloorDrain,
                EquipmentType.GreaseBox
            },
            [RoomType.Laundry] = new()
            {
                EquipmentType.LaundryTub,
                EquipmentType.WashingMachine,
                EquipmentType.FloorDrain,
                EquipmentType.SiphonBox
            },
            [RoomType.ServiceArea] = new()
            {
                EquipmentType.LaundryTub,
                EquipmentType.WashingMachine,
                EquipmentType.FloorDrain,
                EquipmentType.SiphonBox
            },
            [RoomType.ExternalArea] = new()
            {
                EquipmentType.GardenFaucet,
                EquipmentType.ExternalFaucet
            }
        };

        /// <summary>
        /// Verifica se um equipamento é compatível com um ambiente.
        /// </summary>
        public static bool IsCompatible(EquipmentType equipment, RoomType room)
        {
            if (!AllowedEquipment.ContainsKey(room)) return false;
            return AllowedEquipment[room].Contains(equipment);
        }
    }
}
```

### 6.2 Equipamentos obrigatórios por ambiente

| Ambiente | Obrigatórios | Opcionais |
|----------|-------------|-----------|
| **Bathroom** | Vaso, Lavatório, Chuveiro, Ralo | Banheira, Bidê |
| **Lavatory** | Vaso, Lavatório | Ralo |
| **Kitchen** | Pia cozinha | Máq. louças, Filtro, Ralo |
| **GourmetKitchen** | Pia cozinha | Filtro, Ralo |
| **Laundry** | Tanque | Máq. lavar, Ralo |
| **ServiceArea** | Tanque | Máq. lavar, Ralo |
| **ExternalArea** | — | Torneira jardim |

---

## 7. Métodos do Modelo

```csharp
public class EquipmentInfo
{
    // ... propriedades acima ...

    // ── Métodos de consulta hidráulica ───────────────────────────

    /// <summary>
    /// Retorna a demanda de água fria (peso) para dimensionamento.
    /// Se inativo, retorna 0.
    /// </summary>
    public double GetWaterDemand()
    {
        return IsActive ? WeightColdWater : 0;
    }

    /// <summary>
    /// Retorna a carga de esgoto (UHC) para dimensionamento.
    /// </summary>
    public int GetSewageLoad()
    {
        return IsActive ? ContributionUnitsES : 0;
    }

    /// <summary>
    /// Verifica se o equipamento precisa de conexão de água fria.
    /// </summary>
    public bool RequiresColdWater()
    {
        return Type != EquipmentType.FloorDrain
            && Type != EquipmentType.FloorDrainDry
            && Type != EquipmentType.SiphonBox
            && Type != EquipmentType.GreaseBox;
    }

    /// <summary>
    /// Verifica se o equipamento precisa de conexão de esgoto.
    /// </summary>
    public bool RequiresSewer()
    {
        return Type != EquipmentType.WaterFilter
            && Type != EquipmentType.GardenFaucet
            && Type != EquipmentType.ExternalFaucet;
    }

    /// <summary>
    /// Verifica se o equipamento requer ventilação individual.
    /// Regra: vaso sanitário SEMPRE requer ventilação do ramal.
    /// </summary>
    public bool RequiresVentilation()
    {
        return Type == EquipmentType.ToiletCoupledTank
            || Type == EquipmentType.ToiletFlushValve;
    }

    /// <summary>
    /// Verifica se o ramal de descarga deste equipamento é independente.
    /// Regra NBR 8160: vaso sanitário NÃO pode passar pela caixa sifonada.
    /// </summary>
    public bool HasIndependentDischargeBranch()
    {
        return Type == EquipmentType.ToiletCoupledTank
            || Type == EquipmentType.ToiletFlushValve;
    }

    /// <summary>
    /// Verifica se o equipamento pode conectar pela caixa sifonada.
    /// Regra: apenas aparelhos com DN ≤ 50mm na descarga.
    /// </summary>
    public bool CanConnectViaSiphonBox()
    {
        return MinDischargeBranchDiameterEsMm <= 50
            && !HasIndependentDischargeBranch();
    }

    /// <summary>
    /// Retorna a conexão de água fria (se existir).
    /// </summary>
    public ConnectionInfo GetColdWaterConnection()
    {
        return Connections.FirstOrDefault(c =>
            c.System == HydraulicSystem.ColdWater && c.Direction == FlowDirection.In);
    }

    /// <summary>
    /// Retorna a conexão de esgoto (se existir).
    /// </summary>
    public ConnectionInfo GetSewerConnection()
    {
        return Connections.FirstOrDefault(c =>
            c.System == HydraulicSystem.Sewer && c.Direction == FlowDirection.Out);
    }

    /// <summary>
    /// Valida se o equipamento é compatível com o ambiente informado.
    /// </summary>
    public bool ValidatePlacement(RoomInfo room)
    {
        if (room == null) return false;
        return EquipmentRoomCompatibility.IsCompatible(Type, room.ClassifiedType);
    }

    // ── Display ─────────────────────────────────────────────────

    /// <summary>
    /// Retorna nome para UI e logs.
    /// </summary>
    public string GetDisplayName()
    {
        string status = ValidationStatus switch
        {
            EquipmentStatus.Valid => "✅",
            EquipmentStatus.ValidWithRemarks => "⚠️",
            EquipmentStatus.Invalid => "❌",
            EquipmentStatus.Missing => "❓",
            EquipmentStatus.Surplus => "➕",
            _ => "⬜"
        };
        return $"{status} {DisplayName} ({LevelName})";
    }

    public override string ToString()
    {
        return $"Equipment[{Id}] {Type} — Room:{RoomId}, Pos:{Position}, Status:{ValidationStatus}";
    }
}
```

---

## 8. Regras de Negócio

### 8.1 Tabela normativa resumida

| Equipamento | Peso AF | UHC ES | DN sub-ramal AF (mm) | DN ramal descarga ES (mm) | Pressão mín (mca) |
|------------|---------|--------|---------------------|--------------------------|-------------------|
| Vaso c/ caixa acoplada | 0.3 | 6 | 20 | 100 | 0.5 |
| Vaso c/ válvula descarga | 32.0 | 6 | 50 | 100 | 1.2 |
| Lavatório | 0.3 | 2 | 20 | 40 | 0.5 |
| Chuveiro | 0.4 | 2 | 20 | 40 | 1.0 |
| Banheira | 1.0 | 2 | 20 | 40 | 0.5 |
| Bidê | 0.1 | 1 | 20 | 30 | 0.5 |
| Pia cozinha | 0.7 | 3 | 20 | 50 | 0.5 |
| Máq. louças | 1.0 | 2 | 20 | 50 | 0.5 |
| Tanque | 0.7 | 3 | 25 | 40 | 0.5 |
| Máq. lavar | 1.0 | 3 | 25 | 50 | 0.5 |
| Ralo sifonado | — | 1 | — | 40 | — |
| Ralo seco | — | 1 | — | 40 | — |
| Torneira jardim | 0.2 | — | 20 | — | 0.5 |

### 8.2 Regras de posicionamento

```
1. Equipamento DEVE estar dentro do BoundingBox do RoomInfo correspondente
   → ValidatePlacement verifica compatibilidade de tipo
   → ContainsPoint verifica posição

2. Vaso sanitário: encostado em parede (distância ≤ 0.05m da parede mais próxima)

3. Lavatório: montado em parede (Z entre 0.50m e 0.90m do piso)

4. Chuveiro: montado em parede ou teto (Z entre 1.80m e 2.20m)

5. Ralo: no piso (Z ≈ 0.00)

6. Pia cozinha: sobre bancada (Z entre 0.80m e 0.90m)

7. CX sifonada: no piso, posição central do banheiro

8. CX gordura: no piso, próxima à pia da cozinha
```

### 8.3 Regras normativas críticas

```
REGRA 1: Ramal do vaso é INDEPENDENTE
  Vaso sanitário → ramal de descarga → diretamente ao subcoletor ou TQ
  NÃO passa pela caixa sifonada
  DN ≥ 100mm

REGRA 2: CX sifonada recebe aparelhos com DN ≤ 50
  Lavatório (DN 40) → CX sifonada ✅
  Chuveiro (DN 40) → CX sifonada ✅
  Vaso (DN 100) → CX sifonada ❌

REGRA 3: CX gordura obrigatória para cozinhas
  Pia cozinha → CX gordura → subcoletor
  Máq. louças → CX gordura → subcoletor

REGRA 4: DN nunca diminui no sentido do escoamento (ES)
  Lavatório (40) → ramal (40) → subcoletor (≥ 40) ✅
  Vaso (100) → ramal (100) → subcoletor (≥ 100) ✅
  Vaso (100) → subcoletor (75) ❌
```

---

## 9. Validações

### 9.1 EquipmentInfoValidator

```csharp
namespace HidraulicoPlugin.Core.Validation
{
    public class EquipmentInfoValidator
    {
        public ValidationReport Validate(EquipmentInfo equipment, RoomInfo room = null)
        {
            var report = new ValidationReport();

            // ── Identidade ──────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(equipment.Id))
                report.Add(ValidationLevel.Critical, "Equipamento sem ID");

            if (equipment.Type == EquipmentType.Unknown)
                report.Add(ValidationLevel.Critical, 
                    $"Equipamento {equipment.Id}: tipo não identificado");

            // ── Localização ─────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(equipment.RoomId))
                report.Add(ValidationLevel.Medium, 
                    $"Equipamento {equipment.Id}: sem ambiente associado");

            if (equipment.Position.X == 0 && equipment.Position.Y == 0 
                && equipment.Position.Z == 0)
                report.Add(ValidationLevel.Light, 
                    $"Equipamento {equipment.Id}: posição na origem (0,0,0)");

            // ── Parâmetros hidráulicos ───────────────────────────────
            if (equipment.RequiresColdWater() && equipment.WeightColdWater <= 0)
                report.Add(ValidationLevel.Medium, 
                    $"Equipamento {equipment.Id}: peso AF = 0");

            if (equipment.RequiresSewer() && equipment.ContributionUnitsES <= 0)
                report.Add(ValidationLevel.Medium, 
                    $"Equipamento {equipment.Id}: UHC = 0");

            if (equipment.RequiresColdWater() && equipment.MinSubBranchDiameterAfMm <= 0)
                report.Add(ValidationLevel.Medium, 
                    $"Equipamento {equipment.Id}: DN sub-ramal AF = 0");

            if (equipment.RequiresSewer() && equipment.MinDischargeBranchDiameterEsMm <= 0)
                report.Add(ValidationLevel.Medium, 
                    $"Equipamento {equipment.Id}: DN ramal descarga ES = 0");

            // ── Conexões ────────────────────────────────────────────
            if (equipment.RequiresColdWater() 
                && !equipment.Connections.Any(c => c.System == HydraulicSystem.ColdWater))
                report.Add(ValidationLevel.Light, 
                    $"Equipamento {equipment.Id}: sem conexão AF definida");

            if (equipment.RequiresSewer() 
                && !equipment.Connections.Any(c => c.System == HydraulicSystem.Sewer))
                report.Add(ValidationLevel.Light, 
                    $"Equipamento {equipment.Id}: sem conexão ES definida");

            // ── Compatibilidade com ambiente ────────────────────────
            if (room != null)
            {
                if (!equipment.ValidatePlacement(room))
                    report.Add(ValidationLevel.Medium, 
                        $"Equipamento {equipment.Id} ({equipment.Type}) incompatível " +
                        $"com {room.ClassifiedType} ({room.Name})");

                if (room.BBoxMin != null && room.BBoxMax != null 
                    && !room.ContainsPoint(equipment.Position))
                    report.Add(ValidationLevel.Medium, 
                        $"Equipamento {equipment.Id}: fora do BBox do ambiente {room.Id}");
            }

            return report;
        }
    }
}
```

---

## 10. Mapeamento Revit → EquipmentInfo

### 10.1 Onde acontece

```
Camada: HidraulicoPlugin.Revit.Adapters.RevitEquipmentReader
Método: ConvertToEquipmentInfo(FamilyInstance fi) → EquipmentInfo
Momento: Etapa E05 (Validação)
```

### 10.2 Código do adapter

```csharp
// HidraulicoPlugin.Revit.Adapters.RevitEquipmentReader

public class RevitEquipmentReader : IEquipmentReader
{
    private readonly Document _doc;
    private readonly EquipmentTypeMapper _typeMapper;

    public List<EquipmentInfo> GetEquipmentInRoom(string roomId)
    {
        var room = _doc.GetElement(new ElementId(int.Parse(roomId))) as Room;
        if (room == null) return new();

        return new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
            .WhereElementIsNotElementType()
            .Cast<FamilyInstance>()
            .Where(fi => IsInRoom(fi, room))
            .Select(fi => ConvertToEquipmentInfo(fi, roomId))
            .ToList();
    }

    private EquipmentInfo ConvertToEquipmentInfo(FamilyInstance fi, string roomId)
    {
        var location = fi.Location as LocationPoint;
        var point = location?.Point ?? XYZ.Zero;

        var equipment = new EquipmentInfo
        {
            Id = fi.Id.ToString(),
            DisplayName = fi.Symbol.FamilyName,
            Type = _typeMapper.InferType(fi),
            ExistsInModel = true,
            IsActive = true,
            RoomId = roomId,
            LevelName = fi.Level?.Name ?? "",
            Position = new Point3D(
                point.X * 0.3048,
                point.Y * 0.3048,
                point.Z * 0.3048),
            RotationDeg = location?.Rotation * (180 / Math.PI) ?? 0,
            RevitFamilyName = fi.Symbol.FamilyName,
            RevitTypeName = fi.Symbol.Name,
            Connections = ExtractConnections(fi)
        };

        // Preencher parâmetros hidráulicos a partir do tipo inferido
        FillHydraulicParameters(equipment);

        return equipment;
    }

    private List<ConnectionInfo> ExtractConnections(FamilyInstance fi)
    {
        var connections = new List<ConnectionInfo>();
        var connectorSet = fi.MEPModel?.ConnectorManager?.Connectors;
        if (connectorSet == null) return connections;

        int index = 0;
        foreach (Connector conn in connectorSet)
        {
            connections.Add(new ConnectionInfo
            {
                Id = $"conn_{fi.Id}_{index++}",
                System = MapSystem(conn),
                Direction = conn.Direction == FlowDirectionType.In
                    ? FlowDirection.In : FlowDirection.Out,
                DiameterMm = (int)(conn.Radius * 2 * 304.8),
                Position = new Point3D(
                    conn.Origin.X * 0.3048,
                    conn.Origin.Y * 0.3048,
                    conn.Origin.Z * 0.3048),
                IsConnected = conn.IsConnected
            });
        }

        return connections;
    }
}
```

---

## 11. Factory — Criação a partir do JSON normativo

```csharp
namespace HidraulicoPlugin.Core.Services
{
    /// <summary>
    /// Cria EquipmentInfo planejados a partir dos dados normativos.
    /// Usado na etapa E03 para gerar a lista de equipamentos necessários.
    /// </summary>
    public class EquipmentFactory
    {
        private readonly INormativeDataProvider _data;
        private int _idCounter = 0;

        public EquipmentFactory(INormativeDataProvider data)
        {
            _data = data;
        }

        /// <summary>
        /// Cria um equipamento planejado para um ambiente.
        /// </summary>
        public EquipmentInfo CreatePlanned(EquipmentType type, string roomId, 
            string levelName, bool isRequired)
        {
            var normData = _data.GetEquipmentData(type);
            string id = $"eq_{++_idCounter:D3}";

            var equipment = new EquipmentInfo
            {
                Id = id,
                DisplayName = normData.DisplayNamePt,
                Type = type,
                ExistsInModel = false,
                IsActive = true,
                RoomId = roomId,
                LevelName = levelName,
                WeightColdWater = normData.WeightColdWater,
                ContributionUnitsES = normData.ContributionUnitsES,
                DesignFlowRateLs = normData.DesignFlowRateLs,
                MinDynamicPressureMca = normData.MinDynamicPressureMca,
                FlushType = normData.FlushType,
                MinSubBranchDiameterAfMm = normData.MinSubBranchDiameterAfMm,
                MinDischargeBranchDiameterEsMm = normData.MinDischargeBranchDiameterEsMm,
                MinVentDiameterMm = normData.MinVentDiameterMm,
                RevitFamilyName = normData.DefaultRevitFamily,
                RevitTypeName = normData.DefaultRevitType,
                ValidationStatus = EquipmentStatus.Unknown
            };

            // Gerar conexões padrão
            if (equipment.RequiresColdWater())
            {
                equipment.Connections.Add(new ConnectionInfo
                {
                    Id = $"{id}_af",
                    System = HydraulicSystem.ColdWater,
                    Direction = FlowDirection.In,
                    DiameterMm = normData.MinSubBranchDiameterAfMm
                });
            }

            if (equipment.RequiresSewer())
            {
                equipment.Connections.Add(new ConnectionInfo
                {
                    Id = $"{id}_es",
                    System = HydraulicSystem.Sewer,
                    Direction = FlowDirection.Out,
                    DiameterMm = normData.MinDischargeBranchDiameterEsMm
                });
            }

            return equipment;
        }
    }
}
```

---

## 12. Exemplo em JSON

```json
{
  "id": "eq_001",
  "display_name": "Vaso sanitário c/ caixa acoplada",
  "type": "ToiletCoupledTank",
  "exists_in_model": false,
  "is_active": true,
  "room_id": "423567",
  "level_name": "Térreo",
  "position": { "x": 5.800, "y": 3.200, "z": 0.000 },
  "rotation_deg": 180,
  "weight_cold_water": 0.3,
  "contribution_units_es": 6,
  "design_flow_rate_ls": 0.15,
  "min_dynamic_pressure_mca": 0.5,
  "flush_type": "CoupledTank",
  "min_sub_branch_diameter_af_mm": 20,
  "min_discharge_branch_diameter_es_mm": 100,
  "min_vent_diameter_mm": 50,
  "revit_family_name": "M_Toilet-Commercial-Wall Mounted-3D",
  "revit_type_name": "Standard",
  "validation_status": "Unknown",
  "validation_note": null,
  "connections": [
    {
      "id": "eq_001_af",
      "system": "ColdWater",
      "direction": "In",
      "diameter_mm": 20,
      "position": null,
      "offset": { "x": -0.15, "y": 0.00, "z": 0.20 },
      "is_connected": false,
      "connected_pipe_id": null
    },
    {
      "id": "eq_001_es",
      "system": "Sewer",
      "direction": "Out",
      "diameter_mm": 100,
      "position": null,
      "offset": { "x": 0.00, "y": -0.15, "z": 0.00 },
      "is_connected": false,
      "connected_pipe_id": null
    }
  ]
}
```

---

## 13. Resumo Visual

```
EquipmentInfo
├── Identidade
│   ├── Id (string)
│   ├── DisplayName (string)
│   ├── Type (EquipmentType enum — 19 valores)
│   ├── ExistsInModel (bool)
│   └── IsActive (bool)
├── Localização
│   ├── RoomId (string → RoomInfo.Id)
│   ├── LevelName (string)
│   ├── Position (Point3D, metros)
│   └── RotationDeg (double)
├── Parâmetros Hidráulicos
│   ├── WeightColdWater (double — peso AF)
│   ├── ContributionUnitsES (int — UHC)
│   ├── DesignFlowRateLs (double — vazão L/s)
│   ├── MinDynamicPressureMca (double — pressão mín)
│   └── FlushType (enum)
├── Diâmetros Normativos
│   ├── MinSubBranchDiameterAfMm (int)
│   ├── MinDischargeBranchDiameterEsMm (int)
│   └── MinVentDiameterMm (int)
├── Conexões
│   └── List<ConnectionInfo>
│       ├── System (ColdWater|Sewer|Ventilation)
│       ├── Direction (In|Out)
│       ├── DiameterMm (int)
│       ├── Position (Point3D?)
│       └── IsConnected (bool)
├── Família Revit
│   ├── RevitFamilyName (string)
│   └── RevitTypeName (string)
├── Validação
│   ├── ValidationStatus (EquipmentStatus — 6 valores)
│   └── ValidationNote (string)
└── Métodos
    ├── GetWaterDemand()
    ├── GetSewageLoad()
    ├── RequiresColdWater()
    ├── RequiresSewer()
    ├── RequiresVentilation()
    ├── HasIndependentDischargeBranch()
    ├── CanConnectViaSiphonBox()
    ├── GetColdWaterConnection()
    ├── GetSewerConnection()
    ├── ValidatePlacement(RoomInfo)
    ├── GetDisplayName()
    └── ToString()
```
