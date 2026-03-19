# Modelo de Domínio — Enums de Classificação

> Catálogo completo de todas as enumerações do PluginCore, unificando as definições dispersas nos modelos de domínio em um documento canônico. Fonte única de verdade para tipagem forte no domínio hidráulico.

---

## 1. Princípios de Classificação

### 1.1 Regras obrigatórias

| Princípio | Regra | Exemplo |
|-----------|-------|---------|
| **Clareza semântica** | O nome do valor deve ser autoexplicativo | `ToiletCoupledTank`, não `Toilet1` |
| **Não ambiguidade** | Um valor, um significado | `Sink` = lavatório. `KitchenSink` = pia |
| **Cobertura completa** | Cobrir 100% do domínio residencial | Todos os aparelhos da NBR |
| **Extensibilidade** | Novos valores não quebram os existentes | Adicionar no final, sem renumerar |
| **Independência** | Sem referência ao Revit | Sem `FamilyType`, `CategoryId` |
| **PascalCase** | Sem abreviações, sem underscores | `WashingMachine`, não `maq_lavar` |

### 1.2 Anti-padrões proibidos

```
❌ Usar string no lugar de enum:
   if (tipo == "banheiro")  →  PROIBIDO

✅ Usar enum tipado:
   if (tipo == RoomType.Bathroom)  →  CORRETO

❌ Enum genérico demais:
   enum Type { A, B, C }  →  PROIBIDO

✅ Enum específico do domínio:
   enum RoomType { Bathroom, Kitchen }  →  CORRETO

❌ Misturar conceitos:
   enum Type { Bathroom, Toilet, ColdWater }  →  PROIBIDO
   (mistura ambiente, equipamento e sistema)

❌ Duplicar valores com nomes diferentes:
   Banheiro = 1, BanheiroSocial = 2  →  PROIBIDO
   (usar o mesmo valor + lógica complementar)
```

---

## 2. Mapa de Enums × Modelos

| Enum | Onde é usado | Decisão que controla |
|------|-------------|---------------------|
| `RoomType` | RoomInfo.ClassifiedType | Quais equipamentos são obrigatórios |
| `EquipmentType` | EquipmentInfo.Type, HydraulicPoint.EquipmentType | Parâmetros hidráulicos, DN, UHC |
| `HydraulicSystem` | HydraulicPoint, PipeSegment, Riser, PipeNetwork | Separação de redes, regras de cálculo |
| `PointType` | HydraulicPoint.Type | Função na rede (alimentação, descarga, dreno) |
| `SegmentType` | PipeSegment.Type | Hierarquia da tubulação (ramal, subcoletor, TQ) |
| `RiserType` | Riser.Type | Papel da prumada (coluna AF, TQ, ventilação) |
| `NetworkType` | PipeNetwork.Type | Tipo de sistema completo |
| `PipeMaterial` | PipeSegment.Material, Riser.Material | Rugosidade, DI, perda de carga |
| `FlushType` | EquipmentInfo.FlushType | Peso, pressão mínima, DN esgoto |
| `FlowDirection` | HydraulicPoint.Direction, ConnectionInfo | Sentido AF (In) ou ES (Out) |
| `FittingType` | FittingInfo.Type | Comprimento equivalente |
| `PointStatus` | HydraulicPoint.Status | Ciclo de vida do ponto |
| `SegmentStatus` | PipeSegment.Status | Ciclo de vida do trecho |
| `RiserStatus` | Riser.Status | Ciclo de vida da prumada |
| `NetworkStatus` | PipeNetwork.Status | Ciclo de vida do sistema |
| `EquipmentStatus` | EquipmentInfo.ValidationStatus | Estado de validação |
| `SegmentOrientation` | PipeSegment.Orientation | Horizontal, vertical, inclinado |
| `SizingContext` | SizingResult.Context | Tipo de elemento dimensionado |
| `CalculationMethod` | SizingResult.Method | Método hidráulico utilizado |
| `WarningLevel` | SizingWarning.Level | Severidade do alerta |
| `ValidationLevel` | ValidationReport | Severidade da validação |

---

## 3. Código C# — Todas as Enums

### 3.1 RoomType — Tipo de Ambiente

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Classificação hidráulica do ambiente.
    /// Define: equipamentos obrigatórios, sistemas necessários, regras normativas.
    /// Fonte: análise functional do ambiente, NÃO o nome do Revit.
    /// </summary>
    public enum RoomType
    {
        // ── Áreas molhadas (requerem sistemas hidráulicos) ──────

        /// <summary>
        /// Banheiro completo: vaso + lavatório + chuveiro + ralo.
        /// Sistemas: AF + ES + VE. É a área molhada mais complexa.
        /// </summary>
        Bathroom = 1,

        /// <summary>
        /// Lavabo: vaso + lavatório (sem chuveiro).
        /// Sistemas: AF + ES + VE.
        /// </summary>
        Lavatory = 2,

        /// <summary>
        /// Cozinha: pia + possível máquina lavar louça.
        /// Sistemas: AF + ES (com caixa de gordura).
        /// </summary>
        Kitchen = 3,

        /// <summary>
        /// Área de serviço: tanque + máquina lavar roupa + ralo.
        /// Sistemas: AF + ES.
        /// </summary>
        ServiceArea = 4,

        /// <summary>
        /// Varanda molhada: ralo + possível torneira.
        /// Sistemas: AF (opcional) + ES.
        /// </summary>
        WetBalcony = 5,

        /// <summary>
        /// Área externa com ponto de água: torneira de jardim + ralo.
        /// Sistemas: AF + ES (ou pluvial).
        /// </summary>
        ExternalArea = 6,

        /// <summary>
        /// Área técnica / shaft: passagem de tubulações, medidores.
        /// Sistemas: passagem, não consumo.
        /// </summary>
        Technical = 7,

        /// <summary>
        /// Banheiro de suíte (funcionalidade igual a Bathroom, destaque para prioridade).
        /// Sistemas: AF + ES + VE.
        /// </summary>
        SuiteBathroom = 8,

        /// <summary>
        /// Copa / kitchenette: pia pequena.
        /// Sistemas: AF + ES.
        /// </summary>
        Kitchenette = 9,

        // ── Áreas secas (sem sistemas hidráulicos) ──────────────

        /// <summary>
        /// Quarto / dormitório: sem pontos hidráulicos.
        /// Uso: referência espacial, não gera demanda.
        /// </summary>
        Bedroom = 100,

        /// <summary>
        /// Sala de estar / jantar: sem pontos hidráulicos.
        /// </summary>
        LivingRoom = 101,

        /// <summary>
        /// Circulação / corredor / hall: sem pontos hidráulicos.
        /// </summary>
        Circulation = 102,

        /// <summary>
        /// Garagem: possível ralo pluvial.
        /// Sistemas: pluvial (opcional).
        /// </summary>
        Garage = 103,

        /// <summary>
        /// Depósito / despensa: sem pontos hidráulicos.
        /// </summary>
        Storage = 104,

        /// <summary>
        /// Escritório / home office: sem pontos hidráulicos.
        /// </summary>
        Office = 105,

        // ── Especiais ───────────────────────────────────────────

        /// <summary>
        /// Ambiente não classificado (requer revisão humana).
        /// Default inicial antes da classificação automática.
        /// </summary>
        Unclassified = 0,

        /// <summary>
        /// Ambiente explicitamente ignorado pelo usuário.
        /// </summary>
        Ignored = 999
    }
}
```

### 3.2 EquipmentType — Tipo de Equipamento

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Classificação funcional do aparelho sanitário.
    /// Define: peso AF, UHC ES, DN mínimo, tipo de descarga.
    /// Referência: NBR 5626 (Tabela A.1), NBR 8160 (Tabela 3).
    /// </summary>
    public enum EquipmentType
    {
        // ── Aparelhos sanitários ─────────────────────────────────

        /// <summary>
        /// Bacia sanitária com caixa acoplada.
        /// AF: peso 0.3, DN sub-ramal 20mm.
        /// ES: UHC 6, DN ramal 100mm, ramal independente.
        /// Requer ventilação.
        /// </summary>
        ToiletCoupledTank = 1,

        /// <summary>
        /// Bacia sanitária com válvula de descarga.
        /// AF: peso 32.0, DN sub-ramal 50mm, pressão mín 1.2 mca.
        /// ES: UHC 6, DN ramal 100mm, ramal independente.
        /// Requer ventilação.
        /// </summary>
        ToiletFlushValve = 2,

        /// <summary>
        /// Lavatório (pia de banheiro).
        /// AF: peso 0.3, DN 20mm.
        /// ES: UHC 2, DN 40mm, via caixa sifonada.
        /// </summary>
        Sink = 3,

        /// <summary>
        /// Chuveiro.
        /// AF: peso 0.5, DN 20mm.
        /// ES: UHC 2, DN 40mm, via caixa sifonada.
        /// </summary>
        Shower = 4,

        /// <summary>
        /// Banheira.
        /// AF: peso 1.0, DN 25mm.
        /// ES: UHC 3, DN 40mm.
        /// </summary>
        Bathtub = 5,

        /// <summary>
        /// Bidê.
        /// AF: peso 0.1, DN 20mm.
        /// ES: UHC 1, DN 40mm, via caixa sifonada.
        /// </summary>
        Bidet = 6,

        /// <summary>
        /// Mictório com válvula de descarga.
        /// AF: peso 0.5, DN 20mm.
        /// ES: UHC 2, DN 40mm.
        /// </summary>
        Urinal = 7,

        // ── Cozinha ─────────────────────────────────────────────

        /// <summary>
        /// Pia de cozinha.
        /// AF: peso 0.7, DN 20mm.
        /// ES: UHC 3, DN 50mm. Requer caixa de gordura.
        /// </summary>
        KitchenSink = 10,

        /// <summary>
        /// Máquina de lavar louça.
        /// AF: peso 0.3, DN 20mm.
        /// ES: UHC 2, DN 50mm.
        /// </summary>
        Dishwasher = 11,

        // ── Área de serviço ─────────────────────────────────────

        /// <summary>
        /// Tanque de lavar roupa.
        /// AF: peso 0.7, DN 25mm.
        /// ES: UHC 3, DN 40mm.
        /// </summary>
        LaundryTub = 20,

        /// <summary>
        /// Máquina de lavar roupa.
        /// AF: peso 0.5, DN 25mm.
        /// ES: UHC 3, DN 50mm.
        /// </summary>
        WashingMachine = 21,

        // ── Pontos externos / genéricos ─────────────────────────

        /// <summary>
        /// Torneira de jardim / uso geral.
        /// AF: peso 0.4, DN 20mm.
        /// ES: sem (água absorvida pelo solo).
        /// </summary>
        GardenFaucet = 30,

        /// <summary>
        /// Torneira de tanque / genérica.
        /// AF: peso 0.4, DN 20mm.
        /// </summary>
        GenericFaucet = 31,

        // ── Drenos e acessórios ─────────────────────────────────

        /// <summary>
        /// Ralo sifonado (com fecho hídrico).
        /// AF: nenhum. ES: UHC 1, DN 40mm, via caixa sifonada.
        /// </summary>
        FloorDrain = 40,

        /// <summary>
        /// Ralo seco (sem fecho hídrico).
        /// AF: nenhum. ES: UHC 1, DN 40mm.
        /// </summary>
        FloorDrainDry = 41,

        /// <summary>
        /// Caixa sifonada (concentrador de ramais).
        /// AF: nenhum. ES: DN saída 75mm (recebe ramais DN 40).
        /// Não gera UHC próprio — soma das entradas.
        /// </summary>
        SiphonBox = 50,

        /// <summary>
        /// Caixa de gordura (obrigatória para cozinha).
        /// AF: nenhum. ES: DN 75mm.
        /// </summary>
        GreaseBox = 51,

        /// <summary>
        /// Caixa de inspeção (acesso para manutenção).
        /// AF: nenhum. ES: passagem.
        /// </summary>
        InspectionBox = 52,

        // ── Especiais ───────────────────────────────────────────

        /// <summary>
        /// Equipamento não classificado (requer revisão).
        /// </summary>
        Unknown = 0
    }
}
```

### 3.3 HydraulicSystem — Sistema Hidráulico

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Sistema hidráulico ao qual um componente pertence.
    /// Cada sistema tem rede, cálculo e norma independentes.
    /// </summary>
    public enum HydraulicSystem
    {
        /// <summary>
        /// Água fria.
        /// Norma: NBR 5626. Cálculo: vazões prováveis (ΣP → Q).
        /// Fluxo: reservatório → aparelhos.
        /// Pressão: sob pressão (tubo cheio).
        /// </summary>
        ColdWater = 1,

        /// <summary>
        /// Esgoto sanitário.
        /// Norma: NBR 8160. Cálculo: UHC → DN.
        /// Fluxo: aparelhos → rede pública.
        /// Pressão: escoamento livre (tubo parcialmente cheio).
        /// </summary>
        Sewer = 2,

        /// <summary>
        /// Ventilação.
        /// Norma: NBR 8160 (complementar). Cálculo: DN = f(DN_esgoto).
        /// Função: impedir perda de fecho hídrico.
        /// </summary>
        Ventilation = 3,

        /// <summary>
        /// Águas pluviais.
        /// Norma: NBR 10844. Cálculo: área de contribuição × intensidade.
        /// (Escopo futuro — estruturado para extensibilidade).
        /// </summary>
        Rainwater = 4
    }
}
```

### 3.4 PointType — Tipo de Ponto Hidráulico

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo funcional do ponto hidráulico na rede.
    /// Define a função do ponto no grafo de rede.
    /// </summary>
    public enum PointType
    {
        /// <summary>
        /// Alimentação de água fria (sub-ramal → aparelho).
        /// Direção: In. Sistema: ColdWater.
        /// </summary>
        WaterSupply = 1,

        /// <summary>
        /// Descarga de esgoto (aparelho → ramal).
        /// Direção: Out. Sistema: Sewer.
        /// </summary>
        SewerDischarge = 2,

        /// <summary>
        /// Ponto de ventilação (ramal → coluna de ventilação).
        /// Direção: Out. Sistema: Ventilation.
        /// </summary>
        Ventilation = 3,

        /// <summary>
        /// Dreno de piso (ralo — sem equipamento associado).
        /// Direção: Out. Sistema: Sewer.
        /// </summary>
        FloorDrain = 4,

        /// <summary>
        /// Acessório concentrador (CX sifonada, CX gordura).
        /// Recebe de vários pontos, envia para subcoletor.
        /// </summary>
        Accessory = 5,

        /// <summary>
        /// Ponto de prumada (conexão com coluna vertical).
        /// Faz a transição horizontal → vertical.
        /// </summary>
        Riser = 6
    }
}
```

### 3.5 SegmentType — Tipo de Trecho

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Classificação hierárquica do trecho na rede.
    /// Define posição na topologia e regras de dimensionamento.
    /// Organizado por sistema com gaps numéricos para extensibilidade.
    /// </summary>
    public enum SegmentType
    {
        // ── Água Fria (1-9) ─────────────────────────────────────
        SubBranch = 1,              // Sub-ramal (aparelho → ramal)
        Branch = 2,                 // Ramal (distribuição horizontal)
        DistributionColumn = 3,     // Coluna de distribuição (vertical)
        BarrelPipe = 4,             // Barrilete (reservatório → colunas)

        // ── Esgoto (10-19) ──────────────────────────────────────
        DischargeBranch = 10,       // Ramal de descarga (aparelho → TQ/sub)
        SecondaryBranch = 11,       // Ramal secundário (aparelho → CX sifonada)
        SubCollector = 12,          // Subcoletor (horizontal, recebe ramais)
        DropPipe = 13,              // Tubo de queda (vertical)
        BuildingCollector = 14,     // Coletor predial (horizontal final)

        // ── Ventilação (20-29) ──────────────────────────────────
        VentBranch = 20,            // Ramal de ventilação
        VentColumn = 21,            // Coluna de ventilação (vertical)
        PrimaryVent = 22            // Ventilação primária (prolongamento TQ)
    }
}
```

### 3.6 RiserType — Tipo de Prumada

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo de coluna vertical (prumada).
    /// Organizado por sistema.
    /// </summary>
    public enum RiserType
    {
        ColdWaterDistribution = 1,  // Coluna AF (barrilete → pavimentos)
        ColdWaterFeed = 2,          // Alimentação (rede → reservatório)

        SewerDropPipe = 10,         // Tubo de queda
        SewerDropPipeExtension = 11,// Prolongamento acima da cobertura

        VentColumn = 20,            // Coluna de ventilação secundária
        VentAuxiliary = 21          // Ventilação auxiliar
    }
}
```

### 3.7 NetworkType — Tipo de Rede

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo detalhado do sistema de rede completo.
    /// </summary>
    public enum NetworkType
    {
        ColdWaterDistribution = 1,  // Rede AF (reservatório → aparelhos)
        ColdWaterFeed = 2,          // Alimentação (rede pública → reservatório)
        SewerSanitary = 10,         // Rede ES (aparelhos → saída predial)
        Ventilation = 20,           // Rede VE (ramais vent → acima da cobertura)
        Rainwater = 30              // Rede pluvial (futuro)
    }
}
```

### 3.8 PipeMaterial — Material de Tubulação

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Material da tubulação.
    /// Impacta: rugosidade (e), DI, coeficiente de Hazen-Williams (C), custo.
    /// </summary>
    public enum PipeMaterial
    {
        /// <summary>PVC soldável. C=140, e=0.01mm. AF residencial padrão.</summary>
        PvcSoldavel = 1,

        /// <summary>PVC roscável. C=140, e=0.01mm. AF com conexões roscáveis.</summary>
        PvcRoscavel = 2,

        /// <summary>PVC série normal. e=0.01mm. ES padrão.</summary>
        PvcSerieNormal = 3,

        /// <summary>PVC série reforçada. e=0.01mm. ES sob carga.</summary>
        PvcSerieReforcada = 4,

        /// <summary>PPR. C=140, e=0.01mm. AF/AQ. Fusão térmica.</summary>
        Ppr = 5,

        /// <summary>Ferro fundido. C=120, e=0.25mm. ES em edifícios altos.</summary>
        FerroFundido = 6,

        /// <summary>CPVC. C=140, e=0.01mm. AQ (água quente).</summary>
        Cpvc = 7
    }
}
```

### 3.9 FlushType — Tipo de Descarga

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo de descarga. Impacta fortemente o dimensionamento AF.
    /// </summary>
    public enum FlushType
    {
        /// <summary>Não aplicável (equipamentos sem descarga).</summary>
        NotApplicable = 0,

        /// <summary>Caixa acoplada. Peso 0.3. Baixa pressão requerida.</summary>
        CoupledTank = 1,

        /// <summary>Válvula de descarga. Peso 32.0. Alta pressão requerida (1.2 mca).</summary>
        FlushValve = 2
    }
}
```

### 3.10 FlowDirection — Direção do Fluxo

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Direção do fluxo em um ponto ou conexão.
    /// </summary>
    public enum FlowDirection
    {
        /// <summary>Entrada — água chegando no aparelho (AF).</summary>
        In = 1,

        /// <summary>Saída — esgoto saindo do aparelho (ES).</summary>
        Out = 2
    }
}
```

### 3.11 SegmentOrientation — Orientação do Trecho

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Orientação geométrica do trecho no espaço.
    /// </summary>
    public enum SegmentOrientation
    {
        Horizontal = 1,     // ΔZ ≈ 0 (ou com declividade controlada)
        Vertical = 2,       // ΔX ≈ 0 e ΔY ≈ 0
        Inclined = 3        // Nem H nem V
    }
}
```

### 3.12 FittingType — Tipo de Conexão/Peça

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>
    /// Tipo de conexão (fitting) dentro de um trecho.
    /// Define comprimento equivalente para perda de carga localizada.
    /// </summary>
    public enum FittingType
    {
        Elbow90 = 1,                // Joelho 90°
        Elbow45 = 2,                // Joelho 45°
        TeeStraight = 3,            // Tê passagem direta
        TeeBranch = 4,              // Tê saída lateral
        TeeBilateral = 5,           // Tê saída bilateral
        ReductionConcentric = 6,    // Redução concêntrica
        ReductionEccentric = 7,     // Redução excêntrica (ES)
        LongSweep90 = 8,            // Curva longa 90° (ES)
        ShortSweep90 = 9,           // Curva curta 90° (ES)
        Wye45 = 10,                 // Junção simples 45° (ES)
        DoubleWye45 = 11,           // Junção dupla 45° (ES)
        CheckValve = 12,            // Válvula de retenção
        GateValve = 13,             // Registro de gaveta
        GlobeValve = 14             // Registro de pressão (globo)
    }
}
```

### 3.13 Status Enums (Ciclo de Vida)

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>Status do ponto hidráulico.</summary>
    public enum PointStatus
    {
        Planned = 0,        // Planejado
        Inserted = 1,       // Equipamento inserido no modelo (pós E04)
        Validated = 2,      // Validado (pós E05)
        Connected = 3,      // Conectado à rede (pós E07/E08)
        Error = 4,          // Com problema
        Skipped = 5         // Ignorado pelo usuário
    }

    /// <summary>Status do trecho de tubulação.</summary>
    public enum SegmentStatus
    {
        Planned = 0,        // Rota definida, DN padrão
        Sized = 1,          // DN calculado
        Validated = 2,      // Atende norma
        Created = 3,        // Criado no Revit
        Error = 4           // DN insuficiente, V fora do limite
    }

    /// <summary>Status da prumada.</summary>
    public enum RiserStatus
    {
        Planned = 0,        // Posição e ambientes definidos
        Sized = 1,          // DN calculado
        Validated = 2,      // Validada
        Created = 3,        // Criada no Revit
        Error = 4           // Sobrecarga, sem ventilação
    }

    /// <summary>Status do sistema (rede completa).</summary>
    public enum NetworkStatus
    {
        Building = 0,       // Em construção
        Assembled = 1,      // Topologia completa
        Sized = 2,          // DNs calculados
        Validated = 3,      // Validada normativamente
        Created = 4,        // MEP System criado no Revit
        Error = 5           // Desconectada, subdimensionada
    }

    /// <summary>Status de validação do equipamento.</summary>
    public enum EquipmentStatus
    {
        Unknown = 0,        // Não verificado
        Valid = 1,          // Válido
        ValidWithRemarks = 2, // Válido com ressalvas
        Invalid = 3,        // Inválido
        Missing = 4,        // Faltando no modelo
        Surplus = 5         // Excedente
    }
}
```

### 3.14 Enums de Cálculo

```csharp
namespace HidraulicoPlugin.Core.Models.Enums
{
    /// <summary>Contexto de dimensionamento.</summary>
    public enum SizingContext
    {
        Segment = 1,        // Trecho
        Riser = 2,          // Prumada
        Network = 3,        // Sistema completo
        Point = 4           // Ponto específico
    }

    /// <summary>Método de cálculo hidráulico.</summary>
    public enum CalculationMethod
    {
        HazenWilliams = 1,      // J = f(Q, C, D). AF (NBR 5626)
        FairWhippleHsiao = 2,   // J = f(Q, D). AF com PVC
        Manning = 3,            // V = f(n, R, i). ES (NBR 8160)
        NormativeTable = 4,     // DN = f(UHC). ES tabela direta
        ProbableFlow = 5        // Q = 0.3 × √ΣP. AF (NBR 5626)
    }

    /// <summary>Nível de severidade de validação.</summary>
    public enum ValidationLevel
    {
        Light = 0,          // Informativo
        Medium = 1,         // Recomendação
        Critical = 2        // Erro normativo
    }

    /// <summary>Nível de severidade de alerta de dimensionamento.</summary>
    public enum WarningLevel
    {
        Info = 0,           // Informativo
        Warning = 1,        // Alerta (margem pequena)
        Error = 2,          // Violação normativa
        Critical = 3        // Projeto inviável
    }
}
```

---

## 4. Tabela de Compatibilidade Enum × Enum

### 4.1 RoomType → EquipmentType (equipamentos obrigatórios)

| RoomType | Equipamentos obrigatórios | Opcionais |
|----------|--------------------------|-----------|
| Bathroom | ToiletCoupledTank, Sink, Shower, FloorDrain, SiphonBox | Bathtub, Bidet |
| SuiteBathroom | ToiletCoupledTank, Sink, Shower, FloorDrain, SiphonBox | Bathtub |
| Lavatory | ToiletCoupledTank, Sink | — |
| Kitchen | KitchenSink, GreaseBox | Dishwasher |
| ServiceArea | LaundryTub, FloorDrain, SiphonBox | WashingMachine |
| WetBalcony | FloorDrain | GardenFaucet |
| ExternalArea | — | GardenFaucet, FloorDrain |
| Kitchenette | KitchenSink | — |
| Technical | — | — |
| Bedroom/LivingRoom/etc. | — (área seca) | — |

### 4.2 EquipmentType → HydraulicSystem

| EquipmentType | ColdWater | Sewer | Ventilation |
|--------------|-----------|-------|-------------|
| ToiletCoupledTank | ✅ | ✅ | ✅ |
| ToiletFlushValve | ✅ | ✅ | ✅ |
| Sink | ✅ | ✅ | — |
| Shower | ✅ | ✅ | — |
| KitchenSink | ✅ | ✅ | — |
| LaundryTub | ✅ | ✅ | — |
| WashingMachine | ✅ | ✅ | — |
| GardenFaucet | ✅ | — | — |
| FloorDrain | — | ✅ | — |
| SiphonBox | — | ✅ | — |
| GreaseBox | — | ✅ | — |

### 4.3 HydraulicSystem → SegmentType (trechos permitidos)

| HydraulicSystem | SegmentTypes permitidos |
|----------------|----------------------|
| ColdWater | SubBranch, Branch, DistributionColumn, BarrelPipe |
| Sewer | DischargeBranch, SecondaryBranch, SubCollector, DropPipe, BuildingCollector |
| Ventilation | VentBranch, VentColumn, PrimaryVent |

---

## 5. Estratégia de Expansão

### 5.1 Regras para adicionar novos valores

```
1. NUNCA renumerar valores existentes
   ❌ ToiletCoupledTank = 1 → ToiletCoupledTank = 5
   ✅ Manter: ToiletCoupledTank = 1 (para sempre)

2. Usar GAPS numéricos por categoria
   AF:  1-9    ES: 10-19    VE: 20-29
   → Permite inserir até 9 valores por categoria antes de precisar reestruturar

3. Adicionar NOVOS valores no final da categoria
   ❌ Adicionar sem critério
   ✅ Adicionar no próximo número disponível do range

4. DOCUMENTAR o novo valor com XML summary completo

5. ATUALIZAR as tabelas de compatibilidade neste documento

6. TESTAR se o novo valor é tratado em todos os switch/case

7. Para valores DEPRECADOS:
   [Obsolete("Use NewValue em vez disto")]
   OldValue = 99,
```

### 5.2 Exemplo de expansão futura

```csharp
// Adicionando água quente (escopo futuro):
public enum HydraulicSystem
{
    ColdWater = 1,
    Sewer = 2,
    Ventilation = 3,
    Rainwater = 4,
    HotWater = 5          // ← NOVO, sem afetar os existentes
}

// Adicionando equipamento:
public enum EquipmentType
{
    // ... existentes ...
    Jacuzzi = 8,           // ← range 1-9 (sanitário), próximo disponível
    PoolFaucet = 32,       // ← range 30-39 (externo), próximo disponível
}
```

---

## 6. Resumo Visual — Todos os Enums

```
Enums do PluginCore (21 enums, 130+ valores)
│
├── Classificação do Domínio
│   ├── RoomType (15 valores — 9 molhadas + 6 secas)
│   ├── EquipmentType (19 valores — 7 sanitários + 4 cozinha/serviço + 3 externos + 5 drenos)
│   └── HydraulicSystem (4 valores — AF, ES, VE, Pluvial)
│
├── Tipagem de Componentes
│   ├── PointType (6 valores — WaterSupply, SewerDischarge, Ventilation, FloorDrain, Accessory, Riser)
│   ├── SegmentType (13 valores — 4 AF + 5 ES + 3 VE)
│   ├── RiserType (6 valores — 2 AF + 2 ES + 2 VE)
│   └── NetworkType (5 valores — 2 AF + 1 ES + 1 VE + 1 Pluvial)
│
├── Propriedades Físicas
│   ├── PipeMaterial (7 valores)
│   ├── FlushType (3 valores)
│   ├── FlowDirection (2 valores)
│   ├── SegmentOrientation (3 valores)
│   └── FittingType (14 valores)
│
├── Ciclo de Vida (Status)
│   ├── PointStatus (6 valores)
│   ├── SegmentStatus (5 valores)
│   ├── RiserStatus (5 valores)
│   ├── NetworkStatus (6 valores)
│   └── EquipmentStatus (6 valores)
│
└── Cálculo e Validação
    ├── SizingContext (4 valores)
    ├── CalculationMethod (5 valores)
    ├── ValidationLevel (3 valores)
    └── WarningLevel (4 valores)
```
