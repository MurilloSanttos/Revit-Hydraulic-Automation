# Convenções de Nomenclatura e Padronização de Código — Plugin Hidráulico Revit

> Padrões obrigatórios para nomenclatura, organização e estilo de código em todo o projeto.

---

## 1. Princípios Gerais

### 1.1 Princípios fundamentais

| Princípio | Descrição | Exemplo |
|-----------|-----------|---------|
| **Clareza** | O nome deve explicar o que é, sem necessidade de comentário | `pressaoDisponivelMca` em vez de `p` |
| **Consistência** | O mesmo conceito usa sempre o mesmo termo | `Room` nunca alterna com `Ambiente` no código |
| **Intenção explícita** | O nome revela propósito, não implementação | `CalculateAvailablePressure()` em vez de `DoCalc()` |
| **Sem ambiguidade** | Nomes distintos para conceitos distintos | `flowRateLs` (L/s) vs `flowRateM3h` (m³/h) |
| **Previsibilidade** | Conhecendo 1 nome, você adivinha os demais | Se `RoomReader`, então `SpaceReader`, `EquipmentReader` |

### 1.2 Idioma do código

| Elemento | Idioma | Justificativa |
|----------|--------|---------------|
| **Código C# (classes, métodos, variáveis)** | **Inglês** | Padrão da indústria, compatível com API do Revit (em inglês), legível por IA |
| **Comentários XML docs** | **Inglês** | Consistência com o código |
| **Comentários inline explicativos** | **Português** (aceito) | Quando a explicação é normativa/técnica brasileira |
| **Strings de UI (mensagens ao usuário)** | **Português** | Plugin para mercado brasileiro |
| **Nomes de parâmetros Revit** | **Português** com prefixo | Prática local |
| **Logs e mensagens de erro** | **Português** | Leitura pelo projetista |
| **Documentação (.md)** | **Português** | Projeto brasileiro |
| **JSON normativo** | **Português** (snake_case) | Termos técnicos brasileiros (uhc, mca, etc.) |

### 1.3 Regra de ouro

```
SE o elemento é CÓDIGO → Inglês
SE o elemento é INTERFACE COM USUÁRIO → Português
SE o elemento é NORMATIVO → Português
```

---

## 2. Convenções de Namespace

### 2.1 Estrutura

```
HidraulicoPlugin.{Camada}.{Modulo}[.{Subcategoria}]
```

### 2.2 Camadas

| Camada | Namespace | Conteúdo |
|--------|-----------|----------|
| Core | `HidraulicoPlugin.Core` | Lógica de negócio sem dependência do Revit |
| Revit | `HidraulicoPlugin.Revit` | Tudo que usa Autodesk.Revit.DB |
| UI | `HidraulicoPlugin.UI` | WPF, ViewModels, janelas |
| Tests | `HidraulicoPlugin.Tests` | Testes unitários |

### 2.3 Módulos

| Módulo | Namespace Core | Namespace Revit |
|--------|---------------|----------------|
| M01 — Detecção | `Core.Detection` | `Revit.Detection` |
| M02 — Classificação | `Core.Classification` | `Revit.Classification` |
| M03 — Pontos | `Core.HydraulicPoints` | `Revit.HydraulicPoints` |
| M04 — Inserção | `Core.Insertion` | `Revit.Insertion` |
| M05 — Validação equip. | `Core.EquipmentValidation` | `Revit.EquipmentValidation` |
| M06 — Prumadas | `Core.Risers` | `Revit.Risers` |
| M07 — Rede AF | `Core.ColdWaterNetwork` | `Revit.ColdWaterNetwork` |
| M08 — Rede ES | `Core.SewerNetwork` | `Revit.SewerNetwork` |
| M09 — Inclinações | `Core.SlopeApplication` | `Revit.SlopeApplication` |
| M10 — Sistemas MEP | `Core.MepSystems` | `Revit.MepSystems` |
| M11 — Dimensionamento | `Core.Sizing` | `Revit.Sizing` |
| M12 — Tabelas | `Core.Schedules` | `Revit.Schedules` |
| M13 — Pranchas | `Core.Sheets` | `Revit.Sheets` |
| M14 — Logs | `Core.Diagnostics` | — |
| M15 — Interface | — | `UI.Views`, `UI.ViewModels` |
| Compartilhado | `Core.Models`, `Core.Config` | `Revit.Helpers` |

### 2.4 Subcategorias

```csharp
// Modelos de dados
HidraulicoPlugin.Core.Models

// Serviços de negócio
HidraulicoPlugin.Core.Sizing.Services

// Regras normativas
HidraulicoPlugin.Core.Sizing.Rules

// Enums
HidraulicoPlugin.Core.Models.Enums

// Exceções customizadas
HidraulicoPlugin.Core.Exceptions

// Configuração
HidraulicoPlugin.Core.Config

// Helpers Revit
HidraulicoPlugin.Revit.Helpers
```

### 2.5 Regras

```
REGRA 1: Namespace = caminho da pasta
  HidraulicoPlugin.Core.Detection → src/Core/Detection/

REGRA 2: Máximo 4 níveis
  ✅ HidraulicoPlugin.Core.Sizing.Rules
  ❌ HidraulicoPlugin.Core.Sizing.Rules.ColdWater.Pressure

REGRA 3: Sem abreviações no namespace
  ✅ HidraulicoPlugin.Core.Classification
  ❌ HidraulicoPlugin.Core.Classif
```

---

## 3. Convenções de Classes

### 3.1 Regras gerais

| Regra | Comentário |
|-------|-----------|
| PascalCase | Sempre |
| Substantivo ou sintagma nominal | Representa "o quê" |
| 1 classe = 1 arquivo | Arquivo tem o mesmo nome |
| Sem prefixo de tipo | Não usar `clsRoom`, `CRoom` |
| Sufixo por papel | Service, Reader, Builder, Validator, etc. |

### 3.2 Sufixos padrão

| Sufixo | Função | Exemplo |
|--------|--------|---------|
| `Service` | Lógica de negócio, orquestração | `PipeSizingService` |
| `Reader` | Leitura de dados do modelo | `RoomReader` |
| `Writer` | Escrita no modelo | `PipeWriter` |
| `Builder` | Construção complexa de objetos | `NetworkTopologyBuilder` |
| `Validator` | Validação de regras | `EquipmentValidator` |
| `Calculator` | Cálculos matemáticos | `FlowRateCalculator` |
| `Provider` | Fornece dados ou instâncias | `NormativeDataProvider` |
| `Manager` | Gerencia ciclo de vida | `TransactionManager` |
| `Helper` | Métodos utilitários estáticos | `UnitConversionHelper` |
| `Factory` | Criação de objetos | `FittingFactory` |
| `Handler` | Tratamento de eventos | `ExternalEventHandler` |
| `Model` / `Info` / `Data` | Dados (DTO/POCO) | `RoomInfo`, `PipeSegmentData` |
| `Config` | Configuração | `SizingConfig` |
| `Result` | Resultado de operação | `ClassificationResult` |
| `Exception` | Exceção customizada | `InsufficientPressureException` |

### 3.3 Exemplos por módulo

```csharp
// M01 — Detecção
public class RoomReader { }
public class SpaceManager { }
public class RoomInfo { }

// M02 — Classificação
public class RoomClassifier { }
public class ClassificationResult { }
public class NameMatchingService { }

// M07 — Rede AF
public class ColdWaterNetworkBuilder { }
public class PipeRoutingService { }
public class FittingInsertionService { }

// M11 — Dimensionamento
public class FlowRateCalculator { }
public class PipeSizingService { }
public class PressureVerificationService { }
public class HeadLossCalculator { }

// M14 — Logs
public class LogService { }
public class LogEntry { }
public class LogExporter { }
```

---

## 4. Interfaces

### 4.1 Regras

| Regra | Comentário |
|-------|-----------|
| Prefixo `I` | Sempre |
| PascalCase após o `I` | `IRoomReader`, não `Iroomreader` |
| Representam capacidade ou contrato | Verbos ou adjetivos |
| Sem lógica | Apenas assinaturas |

### 4.2 Exemplos

```csharp
// Contratos por módulo
public interface IRoomReader { }
public interface IRoomClassifier { }
public interface IEquipmentValidator { }
public interface INetworkBuilder { }
public interface IPipeSizer { }

// Contratos transversais
public interface ILogService { }
public interface INormativeDataProvider { }
public interface ITransactionExecutor { }
public interface IConfigProvider { }

// Contratos genéricos
public interface IValidator<T> { }
public interface IBuilder<TInput, TOutput> { }
```

### 4.3 Quando criar interface

```
CRIAR interface quando:
  - Classe tem dependências injetadas (DI)
  - Classe pode ter implementações alternativas
  - Classe precisa ser testável com mock
  - Classe é consumida por módulos diferentes

NÃO CRIAR interface quando:
  - Classe é um Model/DTO (RoomInfo, LogEntry)
  - Classe é um Helper estático
  - Classe é um Enum
  - Classe é usada apenas internamente
```

---

## 5. Métodos

### 5.1 Regras

| Regra | Comentário |
|-------|-----------|
| PascalCase | Sempre |
| Começa com verbo | Ação que o método executa |
| Máximo 30 caracteres | Nomes longos demais = método faz demais |
| Sem prefixo de tipo de retorno | Não usar `GetIntCount()` |

### 5.2 Verbos padrão

| Verbo | Quando usar | Exemplo |
|-------|-----------|---------|
| `Get` | Retorna dado sem efeito colateral | `GetRoomsByLevel()` |
| `Set` | Define valor | `SetPipeDiameter()` |
| `Calculate` | Realiza cálculo | `CalculateFlowRate()` |
| `Validate` | Verifica conformidade, retorna bool ou resultado | `ValidatePressure()` |
| `Create` | Cria novo elemento no modelo | `CreatePipe()` |
| `Insert` | Insere elemento existente | `InsertEquipment()` |
| `Delete` | Remove elemento | `DeleteOrphanPipes()` |
| `Find` | Busca, pode retornar null | `FindConnectedPipe()` |
| `Build` | Constrói objeto complexo | `BuildNetworkTopology()` |
| `Apply` | Aplica transformação | `ApplySlope()` |
| `Check` | Verifica condição, retorna bool | `CheckMinimumDiameter()` |
| `Parse` | Converte dados | `ParseNormativeJson()` |
| `Export` | Gera saída externa | `ExportLogToJson()` |
| `Load` | Carrega dados de fonte externa | `LoadConfiguration()` |
| `Save` | Persiste dados | `SaveConfiguration()` |
| `Try` + verbo | Tenta, retorna bool sem exceção | `TryReconnectFitting()` |
| `Is` / `Has` / `Can` | Propriedade booleana | `IsConnected`, `HasVentilation` |

### 5.3 Exemplos

```csharp
// ✅ Correto
public double CalculateFlowRate(double sumOfWeights)
public bool ValidateMinimumPressure(PipeSegment segment)
public List<RoomInfo> GetHydraulicRooms(Document doc)
public bool TryReconnectFitting(Connector source, Connector target)
public void ApplySlopeToSegment(Pipe pipe, double slope)

// ❌ Incorreto
public double Calc(double p)          // Abreviação
public void DoStuff()                  // Sem intenção
public List<RoomInfo> Rooms()          // Sem verbo
public bool Check(Pipe p)             // Genérico
```

---

## 6. Variáveis e Campos

### 6.1 Regras

| Tipo | Convenção | Prefixo | Exemplo |
|------|-----------|---------|---------|
| Variável local | camelCase | nenhum | `flowRate`, `pipeCount` |
| Parâmetro | camelCase | nenhum | `document`, `roomInfo` |
| Campo privado | camelCase | `_` (underscore) | `_logService`, `_config` |
| Campo público | PascalCase | nenhum | `MaxPressure` (evitar; usar propriedade) |
| Propriedade | PascalCase | nenhum | `DiameterMm`, `FlowRateLs` |
| Constante | PascalCase | nenhum | `MinimumPressureMca` |
| Const estática | PascalCase | nenhum | `DefaultCoefficientC` |

### 6.2 Unidades no nome

**Regra obrigatória:** quando a variável tem unidade física, a unidade deve estar no nome como sufixo.

| Unidade | Sufixo | Exemplo |
|---------|--------|---------|
| milímetros | `Mm` | `diameterMm`, `DiameterMm` |
| metros | `M` | `lengthM`, `HeightM` |
| m.c.a. | `Mca` | `pressureMca`, `MinPressureMca` |
| L/s | `Ls` | `flowRateLs`, `FlowRateLs` |
| m/s | `Ms` | `velocityMs`, `MaxVelocityMs` |
| m/m | `Mm_m` (ou adimensional) | `slopeRatio`, `headLossPerMeter` |
| % | `Pct` | `slopePct`, `ConfidencePct` |
| graus | `Deg` | `angleDeg` |
| radianos | `Rad` | `angleRad` |
| segundos | `Sec` | `timeoutSec` |

```csharp
// ✅ Correto — unidade explícita
double flowRateLs = 0.342;
double pressureMca = 3.0;
int diameterMm = 50;
double slopePct = 2.0;
double velocityMs = 1.5;
double lengthM = 4.5;

// ❌ Incorreto — unidade ambígua
double flow = 0.342;      // L/s? m³/h? galões?
double pressure = 3.0;    // mca? kPa? bar?
int diameter = 50;         // mm? polegadas?
```

### 6.3 Nomes descritivos

```csharp
// ✅ Correto
double totalSumOfWeights = 2.5;
int connectedEquipmentCount = 4;
bool hasVentilationColumn = true;
string roomDisplayName = "Banheiro Social";

// ❌ Incorreto
double s = 2.5;
int n = 4;
bool flag = true;
string str = "Banheiro Social";
```

---

## 7. Constantes

### 7.1 Regras

| Regra | Comentário |
|-------|-----------|
| PascalCase | Padrão C# (não usar UPPER_CASE) |
| `const` para literais conhecidos em compilação | Valores imutáveis |
| `static readonly` para valores calculados | Instanciados uma vez |
| Agrupar em classe `Constants` ou na classe que usa | Não espalhar |

### 7.2 Organização

```csharp
namespace HidraulicoPlugin.Core
{
    /// <summary>
    /// Constantes normativas NBR 5626 / NBR 8160.
    /// </summary>
    public static class NormativeConstants
    {
        // Água Fria
        public const double DefaultCoefficientC = 0.30;
        public const double MinPressureMca = 3.0;
        public const double MaxPressureMca = 40.0;
        public const double MaxVelocityMs = 3.0;
        public const double LocalLossFactor = 1.20;

        // Esgoto
        public const int MinDiameterToiletMm = 100;
        public const int MinDiameterSubCollectorMm = 100;
        public const int MinSealHeightMm = 50;

        // Ventilação
        public const int MinVentDiameterMm = 40;
        public const double VentColumnToRiserRatio = 0.667;
        public const double VentBranchToDischargeRatio = 0.50;
        public const double TerminalMinHeightM = 0.30;
        public const double TerminalMinDistanceWindowM = 4.0;

        // Declividade
        public const double MaxRecommendedSlope = 0.05;
        public const double MaxAbsoluteSlope = 0.08;

        // Tolerâncias
        public const double ConnectionToleranceM = 0.001;
        public const double ElevationToleranceM = 0.001;

        // Conversão
        public const double SqFeetToSqMeters = 0.09290304;
        public const double FeetToMeters = 0.3048;
    }
}
```

---

## 8. Enums

### 8.1 Regras

| Regra | Comentário |
|-------|-----------|
| PascalCase no nome | `RoomType`, não `room_type` |
| PascalCase nos valores | `Bathroom`, não `BATHROOM` |
| Singular no nome | `RoomType`, não `RoomTypes` |
| Sem prefixo nos valores | `Bathroom`, não `RoomTypeBathroom` |
| Valor `Unknown = 0` quando faz sentido | Default seguro |

### 8.2 Enums do projeto

```csharp
public enum RoomType
{
    Unknown = 0,
    Bathroom,
    Lavatory,
    Kitchen,
    GourmetKitchen,
    Laundry,
    ServiceArea,
    ExternalArea,
    NonHydraulic
}

public enum HydraulicSystem
{
    Unknown = 0,
    ColdWater,      // AF
    Sewer,           // ES
    Ventilation      // VE
}

public enum EquipmentType
{
    Unknown = 0,
    ToiletCoupledTank,
    ToiletFlushValve,
    Sink,
    Shower,
    Bathtub,
    KitchenSink,
    KitchenSinkWithDisposer,
    WashingMachine,
    Dishwasher,
    LaundryTub,
    FloorDrain,
    DryDrain,
    Bidet,
    PressureFilter,
    GardenFaucet
}

public enum ValidationLevel
{
    Info = 0,
    Light = 1,
    Medium = 2,
    Critical = 3
}

public enum EquipmentStatus
{
    Unknown = 0,
    Valid,
    ValidWithRemarks,
    Invalid,
    Missing
}

public enum PipelineStage
{
    NotStarted = 0,
    Detection,
    Classification,
    PointIdentification,
    EquipmentInsertion,
    EquipmentValidation,
    RiserCreation,
    ColdWaterNetwork,
    SewerNetwork,
    SlopeApplication,
    MepSystems,
    Sizing,
    Schedules,
    Sheets,
    Completed
}

public enum SlopeApplicationMode
{
    Minimum = 0,
    Recommended,
    Custom
}
```

---

## 9. Arquivos e Pastas

### 9.1 Regras de arquivos

| Regra | Comentário |
|-------|-----------|
| Nome do arquivo = nome da classe | `RoomReader.cs` contém `class RoomReader` |
| PascalCase | Sem hifens ou underscores |
| 1 classe principal por arquivo | Exceção: classes aninhadas privadas |
| Extensão padrão | `.cs` para C#, `.xaml` para WPF |

### 9.2 Estrutura de pastas

```
src/
├── HidraulicoPlugin.Core/
│   ├── Config/
│   │   ├── NormativeDataProvider.cs
│   │   ├── ConfigLoader.cs
│   │   └── SizingConfig.cs
│   ├── Models/
│   │   ├── RoomInfo.cs
│   │   ├── HydraulicPoint.cs
│   │   ├── PipeSegment.cs
│   │   ├── NetworkTopology.cs
│   │   └── Enums/
│   │       ├── RoomType.cs
│   │       ├── EquipmentType.cs
│   │       └── HydraulicSystem.cs
│   ├── Detection/
│   │   └── RoomDetectionService.cs
│   ├── Classification/
│   │   ├── RoomClassifier.cs
│   │   ├── NameMatchingService.cs
│   │   └── ClassificationResult.cs
│   ├── HydraulicPoints/
│   │   ├── PointIdentificationService.cs
│   │   └── PointRequirementProvider.cs
│   ├── Sizing/
│   │   ├── Services/
│   │   │   ├── FlowRateCalculator.cs
│   │   │   ├── PipeSizingService.cs
│   │   │   ├── HeadLossCalculator.cs
│   │   │   └── PressureVerificationService.cs
│   │   └── Rules/
│   │       ├── DiameterSelectionRule.cs
│   │       └── VelocityCheckRule.cs
│   ├── Diagnostics/
│   │   ├── LogService.cs
│   │   ├── LogEntry.cs
│   │   └── LogExporter.cs
│   ├── Exceptions/
│   │   ├── InsufficientPressureException.cs
│   │   └── InvalidDiameterException.cs
│   └── NormativeConstants.cs
│
├── HidraulicoPlugin.Revit/
│   ├── Commands/
│   │   ├── OpenPluginCommand.cs
│   │   └── RunPipelineCommand.cs
│   ├── App.cs                            ← IExternalApplication
│   ├── Detection/
│   │   ├── RoomReader.cs
│   │   └── SpaceManager.cs
│   ├── Insertion/
│   │   ├── EquipmentInsertionService.cs
│   │   └── FamilySymbolProvider.cs
│   ├── Networks/
│   │   ├── PipeCreator.cs
│   │   ├── FittingInsertionService.cs
│   │   └── ConnectorHelper.cs
│   ├── SlopeApplication/
│   │   └── SlopeApplicator.cs
│   ├── Helpers/
│   │   ├── UnitConversionHelper.cs
│   │   ├── TransactionHelper.cs
│   │   ├── FilterHelper.cs
│   │   └── ElementSelectionHelper.cs
│   └── ExternalEvents/
│       └── PipelineExternalEventHandler.cs
│
├── HidraulicoPlugin.UI/
│   ├── Views/
│   │   ├── MainWindow.xaml / .xaml.cs
│   │   ├── ConfigTab.xaml / .xaml.cs
│   │   ├── ExecutionTab.xaml / .xaml.cs
│   │   └── DiagnosticsTab.xaml / .xaml.cs
│   ├── ViewModels/
│   │   ├── MainViewModel.cs
│   │   ├── ConfigViewModel.cs
│   │   ├── ExecutionViewModel.cs
│   │   └── DiagnosticsViewModel.cs
│   ├── Converters/
│   │   ├── LevelToColorConverter.cs
│   │   └── BoolToVisibilityConverter.cs
│   └── Resources/
│       ├── Styles.xaml
│       └── Icons/
│
└── HidraulicoPlugin.Tests/
    ├── Core/
    │   ├── ClassificationTests.cs
    │   ├── FlowRateCalculatorTests.cs
    │   └── PipeSizingServiceTests.cs
    └── Revit/
        └── RoomReaderTests.cs
```

---

## 10. Scripts Dynamo

### 10.1 Nomenclatura de arquivos

```
{NN}_{NomeDescritivo}.dyn

Onde:
  NN = número do módulo (02 dígitos)
  NomeDescritivo = PascalCase, sem espaços

Exemplos:
  04_InsertEquipment.dyn
  07_GenerateColdWaterNetwork.dyn
  08_GenerateSewerNetwork.dyn
  09_ApplySlopes.dyn
  13_GenerateSheets.dyn
```

### 10.2 Versionamento no nome

```
Apenas quando necessário manter versão anterior:
  07_GenerateColdWaterNetwork_v1.dyn    ← obsoleto
  07_GenerateColdWaterNetwork_v2.dyn    ← atual

Regra: a versão sem sufixo é sempre a atual.
  07_GenerateColdWaterNetwork.dyn       ← atual (sem _vN)
```

### 10.3 Organização interna do script

```
Cada script .dyn deve conter:

1. Bloco "Note" com:
   - Nome do script
   - Módulo relacionado
   - Versão
   - Data de última alteração
   - Descrição breve
   - Inputs esperados
   - Outputs produzidos

2. Grupos nomeados:
   - "01 - Inputs"
   - "02 - Leitura do Modelo"
   - "03 - Processamento"
   - "04 - Criação de Elementos"
   - "05 - Outputs"

3. Nomes de nós customizados:
   - PascalCase, descritivo
   - Exemplo: "FilterHydraulicRooms", "CalculateSlopeOffset"
```

### 10.4 Comunicação Plugin ↔ Dynamo

```
Formato de troca: JSON via arquivo temporário

Plugin → Dynamo:
  Arquivo: %TEMP%/HidraulicoPlugin/dynamo_input_{timestamp}.json
  Conteúdo: lista de elementos, parâmetros, configuração

Dynamo → Plugin:
  Arquivo: %TEMP%/HidraulicoPlugin/dynamo_output_{timestamp}.json
  Conteúdo: ElementIds criados, status, erros
```

---

## 11. Parâmetros no Revit

### 11.1 Prefixo obrigatório

Todos os parâmetros customizados criados pelo plugin usam o prefixo:

```
HID_
```

### 11.2 Formato

```
HID_{Sistema}_{NomeDescritivo}

Sistema:
  AF  = Água Fria
  ES  = Esgoto
  VE  = Ventilação
  GER = Geral

NomeDescritivo:
  Português, PascalCase, sem acentos
```

### 11.3 Parâmetros padrão do plugin

| Parâmetro | Tipo | Aplicável a | Descrição |
|-----------|------|------------|-----------|
| `HID_GER_Modulo` | String | Pipe, Fitting | Módulo que criou o elemento |
| `HID_GER_Etapa` | String | Pipe, Fitting | Etapa do pipeline |
| `HID_GER_Versao` | String | ProjectInfo | Versão do plugin |
| `HID_AF_Vazao_Ls` | Double | Pipe | Vazão calculada (L/s) |
| `HID_AF_Velocidade_Ms` | Double | Pipe | Velocidade (m/s) |
| `HID_AF_Pressao_Mca` | Double | Pipe, Fixture | Pressão disponível (m.c.a.) |
| `HID_AF_Perda_Carga_M` | Double | Pipe | Perda de carga no trecho (m) |
| `HID_AF_Soma_Pesos` | Double | Pipe | Soma de pesos no trecho |
| `HID_ES_UHC` | Integer | Pipe, Fixture | Unidades de contribuição |
| `HID_ES_Declividade_Pct` | Double | Pipe | Declividade aplicada (%) |
| `HID_ES_Ramal_Independente` | Boolean | Pipe | Se ramal é independente (vaso) |
| `HID_VE_DN_Coluna_Mm` | Integer | Pipe | DN da coluna de ventilação |
| `HID_GER_Status` | String | Fixture | Válido/Inválido/ComRessalva |
| `HID_GER_Classificacao` | String | Room/Space | Tipo do ambiente |
| `HID_GER_Confianca` | Double | Room/Space | Confiança da classificação |

### 11.4 Regras

```
REGRA 1: Nunca alterar parâmetros nativos do Revit
  ✅ Criar HID_AF_Vazao_Ls
  ❌ Alterar "Flow" nativo do Pipe

REGRA 2: Tipo correto
  ✅ HID_ES_UHC como Integer (não é fracionário)
  ❌ HID_ES_UHC como String ("6")

REGRA 3: Unidade no nome
  ✅ HID_AF_Pressao_Mca
  ❌ HID_AF_Pressao (qual unidade?)

REGRA 4: SharedParameter file
  Todos os HID_* definidos em HidraulicoPlugin_SharedParameters.txt
```

---

## 12. Padrões para Integração com IA

### 12.1 Regras para código gerado pela IA

| Regra | Descrição |
|-------|-----------|
| Seguir TODAS as convenções deste documento | Mesmo padrão que código humano |
| Sem placeholders | `// TODO: implement` não é aceito |
| XML docs em métodos públicos | Obrigatório |
| Nomes em inglês no código | Conforme seção 1.2 |
| Tag `[AI-assisted]` no commit | Rastreabilidade |

### 12.2 Como manter consistência

```
ANTES de pedir código à IA:
  1. Fornecer este documento (ou trecho relevante)
  2. Fornecer exemplo de classe existente do projeto
  3. Especificar namespace, classe, métodos esperados
  4. Especificar interfaces a implementar

DEPOIS de receber código da IA:
  1. Verificar naming (este documento)
  2. Verificar namespace (seção 2)
  3. Verificar sufixos de classe (seção 3)
  4. Verificar unidades em variáveis (seção 6)
  5. Verificar XML docs
  6. Compilar
  7. Testar
```

### 12.3 Template para XML docs

```csharp
/// <summary>
/// Calcula a vazão provável no trecho usando método probabilístico.
/// Fórmula: Q = C × √ΣP (NBR 5626).
/// </summary>
/// <param name="sumOfWeights">Soma dos pesos dos aparelhos no trecho.</param>
/// <param name="coefficientC">Coeficiente de descarga (padrão: 0.30).</param>
/// <returns>Vazão em L/s.</returns>
/// <exception cref="ArgumentException">Se sumOfWeights &lt; 0.</exception>
public double CalculateFlowRate(double sumOfWeights, double coefficientC = 0.30)
{
    if (sumOfWeights < 0)
        throw new ArgumentException("Soma de pesos não pode ser negativa.", nameof(sumOfWeights));
    
    return coefficientC * Math.Sqrt(sumOfWeights);
}
```

---

## 13. Anti-padrões (PROIBIDO)

### 13.1 Nomes proibidos

| Proibido | Por quê | Correção |
|----------|---------|----------|
| `data`, `value`, `temp`, `result` | Genérico, sem intenção | Usar nome descritivo |
| `Manager` sem contexto | Vago (manage o quê?) | Usar sufixo específico |
| `DoSomething()` | Sem intenção | Verbo + objeto |
| `x`, `y`, `z` (exceto coordenadas) | Sem significado | Nome descritivo |
| `flag`, `flag2` | Sem propósito | `isConnected`, `hasVentilation` |
| `list1`, `array2` | Tipo no nome | `rooms`, `pipeSegments` |
| `string1`, `int1` | Tipo no nome | Nome descritivo |

### 13.2 Abreviações proibidas

| Proibido | Correção |
|----------|----------|
| `Calc` | `Calculate` |
| `Btn` | `Button` |
| `Lbl` | `Label` |
| `Val` | `Value` / `Validate` (ambíguo) |
| `Mgr` | `Manager` |
| `Svc` | `Service` |
| `Repo` | `Repository` |
| `Info` isolado | Nome completo: `RoomInfo` |

### 13.3 Abreviações PERMITIDAS (exceções)

| Permitido | Significado | Justificativa |
|-----------|-----------|---------------|
| `Id` | Identifier | Universalmente reconhecido |
| `Db` | Database | Universalmente reconhecido |
| `Ui` | User Interface | Universalmente reconhecido |
| `Mep` | Mechanical/Electrical/Plumbing | Termo técnico BIM |
| `Af` | Água Fria | Dentro de prefixos Revit (HID_AF_) |
| `Es` | Esgoto | Dentro de prefixos Revit (HID_ES_) |
| `Ve` | Ventilação | Dentro de prefixos Revit (HID_VE_) |
| `Dn` | Diâmetro Nominal | Termo técnico hidráulico |
| `Tq` | Tubo de Queda | Dentro de JSON/docs (não no código C#) |
| `Uhc` | Unidade Hidráulica de Contribuição | Acrônimo técnico |
| `Ci` | Caixa de Inspeção | Dentro de JSON/docs |

### 13.4 Mistura de idiomas (PROIBIDO no código)

```csharp
// ❌ PROIBIDO — mistura inglês e português
public double CalcularFlowRate(double somaPesos)
public class GerenciadorDeRooms
public void AplicarSlope(Pipe tubo)

// ✅ CORRETO — inglês consistente
public double CalculateFlowRate(double sumOfWeights)
public class RoomManager
public void ApplySlope(Pipe pipe)
```

### 13.5 Outros anti-padrões

```csharp
// ❌ Números mágicos
if (pressure < 3.0)  // O que é 3.0?

// ✅ Usar constante
if (pressure < NormativeConstants.MinPressureMca)

// ❌ Concatenação de string para log
_logService.Log(Level.Critical, "Erro no pipe " + pipe.Id + " com DN " + dn);

// ✅ Interpolação
_logService.Log(Level.Critical, $"Erro no pipe {pipe.Id} com DN {dn}mm");

// ❌ Catch genérico silencioso
try { } catch { }

// ✅ Catch específico com log
try { }
catch (InvalidOperationException ex)
{
    _logService.Log(ValidationLevel.Critical, $"Falha na operação: {ex.Message}");
    throw;
}
```

---

## 14. Resumo Rápido (Cheat Sheet)

| Elemento | Convenção | Exemplo |
|----------|-----------|---------|
| Namespace | PascalCase, por camada/módulo | `HidraulicoPlugin.Core.Sizing` |
| Classe | PascalCase + sufixo | `FlowRateCalculator` |
| Interface | `I` + PascalCase | `IPipeSizer` |
| Método | PascalCase, verbo | `CalculateFlowRate()` |
| Propriedade | PascalCase | `DiameterMm` |
| Variável local | camelCase | `flowRateLs` |
| Campo privado | `_` + camelCase | `_logService` |
| Constante | PascalCase | `MinPressureMca` |
| Enum | PascalCase singular | `RoomType.Bathroom` |
| Arquivo | = nome da classe | `FlowRateCalculator.cs` |
| Parâmetro Revit | `HID_{SIS}_{Nome}` | `HID_AF_Vazao_Ls` |
| Script Dynamo | `{NN}_{Nome}.dyn` | `07_GenerateColdWaterNetwork.dyn` |
| Unidades | Sufixo obrigatório | `Mm`, `Mca`, `Ls`, `Ms`, `Pct` |
| Idioma código | Inglês | — |
| Idioma UI | Português | — |
