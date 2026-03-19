# Arquitetura Geral do Sistema — Plugin Hidráulico Revit

> Definição completa da arquitetura em camadas, contratos, fluxo de dados e regras de dependência para o plugin de automação hidráulica.

---

## 1. Visão Geral da Arquitetura

### 1.1 Modelo adotado: Clean Architecture adaptada para plugin Revit

A arquitetura segue os princípios da Clean Architecture com adaptações para o contexto de plugin Revit, onde o host (Revit) é o ponto de entrada e não a aplicação em si.

```
┌──────────────────────────────────────────────────────────────────┐
│                        AUTODESK REVIT (HOST)                     │
│  ┌────────────────────────────────────────────────────────────┐  │
│  │                    HidraulicoPlugin.UI                     │  │
│  │              (WPF Views + ViewModels)                      │  │
│  │  ┌──────────────────────────────────────────────────────┐  │  │
│  │  │              HidraulicoPlugin.Revit                  │  │  │
│  │  │       (Revit API Adapters + Commands)                │  │  │
│  │  │  ┌────────────────────────────────────────────────┐  │  │  │
│  │  │  │           HidraulicoPlugin.Core                │  │  │  │
│  │  │  │    (Regras, Cálculos, Motor de Decisão)        │  │  │  │
│  │  │  │  ┌──────────────────────────────────────────┐  │  │  │  │
│  │  │  │  │       HidraulicoPlugin.Data              │  │  │  │  │
│  │  │  │  │  (JSON, Config, Dados Normativos)        │  │  │  │  │
│  │  │  │  └──────────────────────────────────────────┘  │  │  │  │
│  │  │  └────────────────────────────────────────────────┘  │  │  │
│  │  └──────────────────────────────────────────────────────┘  │  │
│  └────────────────────────────────────────────────────────────┘  │
│                                                                  │
│  ┌────────────────────┐                                          │
│  │   DynamoScripts    │  (executados pelo Dynamo Player)         │
│  └────────────────────┘                                          │
└──────────────────────────────────────────────────────────────────┘
```

### 1.2 Princípios

| Princípio | Aplicação |
|-----------|-----------|
| **Separação de responsabilidades** | Cada camada tem papel único e bem definido |
| **Dependency Rule** | Dependências apontam sempre para dentro (Core não conhece Revit) |
| **Baixo acoplamento** | Camadas se comunicam via interfaces, não implementações |
| **Alta coesão** | Cada classe tem uma responsabilidade clara |
| **Inversão de dependência** | Core define interfaces; Revit implementa |
| **Testabilidade** | Core testável sem Revit; Revit testável com mock |
| **Single Source of Truth** | Dados normativos em 1 JSON; configuração em 1 local |

### 1.3 Projetos da Solution

| Projeto | Tipo | .NET | Referencia Revit API | Referencia Core |
|---------|------|------|---------------------|----------------|
| `HidraulicoPlugin.Data` | Class Library | .NET 4.8 | ❌ | ❌ |
| `HidraulicoPlugin.Core` | Class Library | .NET 4.8 | ❌ | ✅ Data |
| `HidraulicoPlugin.Revit` | Class Library | .NET 4.8 | ✅ | ✅ Core, Data |
| `HidraulicoPlugin.UI` | Class Library (WPF) | .NET 4.8 | ✅ | ✅ Core, Revit |
| `HidraulicoPlugin.Tests` | xUnit Test | .NET 4.8 | ❌ | ✅ Core, Data |
| `DynamoScripts/` | Pasta (não é projeto) | — | — | — |

```
HidraulicoPlugin.sln
├── src/
│   ├── HidraulicoPlugin.Data/
│   ├── HidraulicoPlugin.Core/
│   ├── HidraulicoPlugin.Revit/
│   └── HidraulicoPlugin.UI/
├── tests/
│   └── HidraulicoPlugin.Tests/
└── dynamo/
    └── scripts/
```

---

## 2. Descrição das Camadas

---

### 2.1 HidraulicoPlugin.Data (Camada de Dados)

#### Responsabilidade

Fornecer dados normativos, configuração e constantes. Camada mais interna. Não possui lógica de negócio.

#### O que contém

```
HidraulicoPlugin.Data/
├── Models/
│   ├── NormativeReference.cs         ← POCO que espelha referencia_normativa.json
│   ├── EquipmentData.cs              ← Dados de um aparelho
│   ├── DiameterTable.cs              ← Tabela de diâmetros comerciais
│   ├── SlopeTable.cs                 ← Tabela de declividades por DN
│   ├── VentilationTable.cs           ← Tabela de ventilação
│   ├── ValidationRule.cs             ← Regra de validação (VAL-NNN)
│   └── ConfigData.cs                 ← POCO de configuração
├── Providers/
│   ├── INormativeDataProvider.cs     ← Interface de acesso aos dados
│   ├── NormativeDataProvider.cs      ← Implementação: lê JSON, deserializa
│   ├── IConfigProvider.cs            ← Interface de configuração
│   └── ConfigProvider.cs             ← Implementação: lê config/*.json
├── Constants/
│   └── NormativeConstants.cs         ← Constantes hardcoded (fallback)
└── Resources/
    ├── referencia_normativa.json     ← Embedded resource
    └── config_defaults.json          ← Valores padrão
```

#### O que NÃO pode conter

- ❌ Referência ao Revit API
- ❌ Referência ao Core
- ❌ Lógica de cálculo
- ❌ Lógica de decisão
- ❌ Qualquer `using Autodesk.*`

#### Contratos

```csharp
public interface INormativeDataProvider
{
    NormativeReference LoadNormativeData();
    EquipmentData GetEquipment(string equipmentId);
    List<EquipmentData> GetEquipmentsByRoom(string roomType);
    DiameterTable GetCommercialDiameters(string material);
    SlopeTable GetSlopeByDiameter(int diameterMm);
    VentilationTable GetVentilationData();
    List<ValidationRule> GetValidationRules();
}

public interface IConfigProvider
{
    ConfigData LoadConfig();
    void SaveConfig(ConfigData config);
    T GetValue<T>(string category, string key);
    void SetValue<T>(string category, string key, T value);
}
```

---

### 2.2 HidraulicoPlugin.Core (Camada de Domínio / Negócio)

#### Responsabilidade

Toda a lógica de negócio, cálculos hidráulicos, regras normativas, motor de decisão e orquestração do pipeline. **Coração do sistema.**

#### O que contém

```
HidraulicoPlugin.Core/
├── Models/
│   ├── RoomInfo.cs                   ← Dados de um ambiente
│   ├── HydraulicPoint.cs            ← Ponto hidráulico (AF/ES)
│   ├── PipeSegment.cs               ← Trecho de tubulação
│   ├── NetworkTopology.cs           ← Grafo da rede
│   ├── RiserCluster.cs              ← Grupo de prumadas
│   ├── SizingResult.cs              ← Resultado de dimensionamento
│   ├── ValidationResult.cs          ← Resultado de validação
│   ├── ClassificationResult.cs      ← Resultado de classificação
│   └── Enums/
│       ├── RoomType.cs
│       ├── EquipmentType.cs
│       ├── HydraulicSystem.cs
│       ├── ValidationLevel.cs
│       ├── EquipmentStatus.cs
│       └── PipelineStage.cs
├── Classification/
│   ├── IRoomClassifier.cs
│   ├── RoomClassifier.cs            ← NLP para nomes em português
│   └── NameNormalizer.cs            ← Remove acentos, normaliza
├── HydraulicPoints/
│   ├── IPointIdentificationService.cs
│   ├── PointIdentificationService.cs ← Mapeia pontos por ambiente
│   └── PointRequirementProvider.cs   ← Consulta JSON de mapeamento
├── Sizing/
│   ├── Services/
│   │   ├── IFlowRateCalculator.cs
│   │   ├── FlowRateCalculator.cs    ← Q = C × √ΣP
│   │   ├── IPipeSizingService.cs
│   │   ├── PipeSizingService.cs     ← Seleção de DN
│   │   ├── IHeadLossCalculator.cs
│   │   ├── HeadLossCalculator.cs    ← FWH: J = 8.69e6 × Q^1.75 / D^4.75
│   │   ├── IPressureVerificationService.cs
│   │   └── PressureVerificationService.cs ← P_din = P_est - ΣΔH
│   └── Rules/
│       ├── ISlopeRules.cs
│       ├── SlopeRules.cs            ← Declividade por DN
│       ├── IVentilationRules.cs
│       ├── VentilationRules.cs      ← DN coluna, ramal, obrigatoriedade
│       ├── IDiameterRules.cs
│       └── DiameterRules.cs         ← DN mínimo, nunca diminui
├── Validation/
│   ├── IValidationEngine.cs
│   ├── ValidationEngine.cs          ← Executa todas as regras VAL-NNN
│   └── ValidationResultAggregator.cs ← Consolida resultados
├── Pipeline/
│   ├── IPipelineOrchestrator.cs
│   ├── PipelineOrchestrator.cs      ← Orquestra etapas E01→E13
│   ├── IPipelineStage.cs            ← Interface de cada etapa
│   └── StageResult.cs               ← Resultado de uma etapa
├── Diagnostics/
│   ├── ILogService.cs
│   ├── LogService.cs                ← Singleton, acumula entries
│   ├── LogEntry.cs
│   └── LogExporter.cs               ← Export para JSON
├── Abstractions/
│   ├── IModelReader.cs              ← Contrato: ler modelo
│   ├── IElementCreator.cs           ← Contrato: criar elementos
│   ├── IEquipmentInserter.cs        ← Contrato: inserir equipamentos
│   ├── INetworkBuilder.cs           ← Contrato: gerar rede
│   ├── ISlopeApplicator.cs          ← Contrato: aplicar inclinação
│   ├── IDynamoExecutor.cs           ← Contrato: executar Dynamo
│   └── IScheduleGenerator.cs        ← Contrato: gerar tabelas
└── Exceptions/
    ├── InsufficientPressureException.cs
    ├── InvalidDiameterException.cs
    └── PipelineBlockedException.cs
```

#### O que NÃO pode conter

- ❌ Referência ao Revit API (`Autodesk.Revit.*`)
- ❌ Referência à UI (`System.Windows.*`)
- ❌ Referência ao Dynamo
- ❌ Acesso direto a arquivos (via IConfigProvider)
- ❌ Qualquer `Document`, `Element`, `ElementId`, `XYZ`

#### Princípio fundamental

```
Core define INTERFACES (Abstractions/) para tudo que precisa do mundo externo.
Revit IMPLEMENTA essas interfaces.
Core nunca sabe que Revit existe.
```

#### Exemplo de fluxo no Core

```csharp
public class PipelineOrchestrator : IPipelineOrchestrator
{
    private readonly IModelReader _modelReader;
    private readonly IRoomClassifier _classifier;
    private readonly IPointIdentificationService _pointService;
    private readonly IEquipmentInserter _inserter;
    private readonly IFlowRateCalculator _flowCalc;
    private readonly IPipeSizingService _sizer;
    private readonly ILogService _log;

    // Injeção de dependência — Core não sabe de onde vêm
    public PipelineOrchestrator(
        IModelReader modelReader,
        IRoomClassifier classifier,
        IPointIdentificationService pointService,
        IEquipmentInserter inserter,
        IFlowRateCalculator flowCalc,
        IPipeSizingService sizer,
        ILogService log)
    {
        _modelReader = modelReader;
        _classifier = classifier;
        _pointService = pointService;
        _inserter = inserter;
        _flowCalc = flowCalc;
        _sizer = sizer;
        _log = log;
    }

    public StageResult ExecuteStage(PipelineStage stage)
    {
        return stage switch
        {
            PipelineStage.Detection => ExecuteDetection(),
            PipelineStage.Classification => ExecuteClassification(),
            // ...
            _ => throw new ArgumentException($"Stage {stage} not implemented")
        };
    }

    private StageResult ExecuteDetection()
    {
        _log.Log(ValidationLevel.Info, "Iniciando detecção de ambientes...");
        
        // Core usa IModelReader — não sabe que é Revit
        var rooms = _modelReader.GetRooms();
        
        if (!rooms.Any())
        {
            _log.Log(ValidationLevel.Critical, "Nenhum Room encontrado no modelo");
            return StageResult.Blocked("Modelo sem Rooms");
        }

        _log.Log(ValidationLevel.Info, $"{rooms.Count} Rooms detectados");
        return StageResult.Success(rooms);
    }
}
```

---

### 2.3 HidraulicoPlugin.Revit (Camada de Infraestrutura)

#### Responsabilidade

Implementar as interfaces definidas no Core usando a API do Autodesk Revit. Traduzir conceitos do domínio (RoomInfo, PipeSegment) para elementos Revit (Room, Pipe, FamilyInstance).

#### O que contém

```
HidraulicoPlugin.Revit/
├── App.cs                            ← IExternalApplication (entry point)
├── Commands/
│   ├── OpenPluginCommand.cs          ← IExternalCommand (ribbon button)
│   └── RunStageCommand.cs
├── Adapters/
│   ├── RevitModelReader.cs           ← Implementa IModelReader
│   ├── RevitElementCreator.cs        ← Implementa IElementCreator
│   ├── RevitEquipmentInserter.cs     ← Implementa IEquipmentInserter
│   ├── RevitNetworkBuilder.cs        ← Implementa INetworkBuilder
│   ├── RevitSlopeApplicator.cs       ← Implementa ISlopeApplicator
│   ├── RevitScheduleGenerator.cs     ← Implementa IScheduleGenerator
│   └── DynamoExecutorAdapter.cs      ← Implementa IDynamoExecutor
├── Detection/
│   ├── RoomReader.cs                 ← FilteredElementCollector para Rooms
│   └── SpaceManager.cs               ← Cria/verifica Spaces
├── Insertion/
│   ├── FamilySymbolProvider.cs       ← Localiza FamilySymbols no modelo
│   └── EquipmentPositionCalculator.cs ← XYZ de posicionamento
├── Networks/
│   ├── PipeCreator.cs                ← Cria Pipes via Revit API
│   ├── FittingInsertionService.cs    ← Insere Tees, Curvas, Registros
│   └── ConnectorHelper.cs           ← Gerencia Connectors
├── Helpers/
│   ├── TransactionHelper.cs          ← Gerencia Transactions de forma segura
│   ├── UnitConversionHelper.cs       ← ft↔m, ft²↔m²
│   ├── FilterHelper.cs               ← FilteredElementCollector helpers
│   ├── ParameterHelper.cs            ← Lê/escreve parâmetros HID_*
│   └── ElementSelectionHelper.cs     ← Seleciona/destaca elementos
├── ExternalEvents/
│   └── PipelineExternalEventHandler.cs ← Safe thread para UI ↔ Revit
└── Registration/
    ├── ServiceRegistry.cs            ← Registra implementações (DI manual)
    └── RibbonBuilder.cs              ← Cria ribbon tab e botões
```

#### Tradução de conceitos

| Core (domínio) | Revit (infraestrutura) |
|----------------|----------------------|
| `RoomInfo` | `Room` (Autodesk.Revit.DB.Architecture) |
| `HydraulicPoint` | `FamilyInstance` + `Connector` |
| `PipeSegment` | `Pipe` (Autodesk.Revit.DB.Plumbing) |
| `NetworkTopology` | Conjunto de `Pipe` + `FittingInstance` |
| `RiserCluster` | Conjunto de `Pipe` verticais |
| `ElementReference` (string Id) | `ElementId` |

#### Exemplo de adapter

```csharp
namespace HidraulicoPlugin.Revit.Adapters
{
    /// <summary>
    /// Implementa IModelReader usando Revit API.
    /// Core não sabe que esta classe existe.
    /// </summary>
    public class RevitModelReader : IModelReader
    {
        private readonly Document _doc;
        
        public RevitModelReader(Document doc)
        {
            _doc = doc;
        }

        public List<RoomInfo> GetRooms()
        {
            return new FilteredElementCollector(_doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Location != null && r.Area > 0)
                .Select(r => new RoomInfo
                {
                    Id = r.Id.ToString(),
                    Name = r.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
                    Number = r.Number,
                    LevelName = r.Level?.Name ?? "",
                    AreaSqM = UnitConversionHelper.SqFeetToSqMeters(r.Area),
                    CenterX = ((LocationPoint)r.Location).Point.X * 0.3048,
                    CenterY = ((LocationPoint)r.Location).Point.Y * 0.3048,
                    CenterZ = ((LocationPoint)r.Location).Point.Z * 0.3048
                })
                .ToList();
        }

        public List<string> GetLevelNames()
        {
            return new FilteredElementCollector(_doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => l.Name)
                .ToList();
        }
    }
}
```

#### O que NÃO pode conter

- ❌ Lógica de cálculo (isso é do Core)
- ❌ Regras normativas (isso é do Core)
- ❌ Decisões hidráulicas (isso é do Core)
- ❌ Acesso direto a JSON normativo (isso é do Data)

---

### 2.4 HidraulicoPlugin.UI (Camada de Apresentação)

#### Responsabilidade

Interface visual com o usuário. Exibe dados, recebe inputs, controla o fluxo semi-automático.

#### O que contém

```
HidraulicoPlugin.UI/
├── Views/
│   ├── MainWindow.xaml                ← Janela principal
│   ├── MainWindow.xaml.cs
│   ├── ConfigTab.xaml                 ← Aba de configuração
│   ├── ExecutionTab.xaml              ← Aba de execução (botões de etapa)
│   └── DiagnosticsTab.xaml            ← Aba de logs/diagnóstico
├── ViewModels/
│   ├── MainViewModel.cs              ← Orquestra ViewModels das abas
│   ├── ConfigViewModel.cs            ← Binding com ConfigData
│   ├── ExecutionViewModel.cs         ← Estado das etapas, botões
│   └── DiagnosticsViewModel.cs       ← LogEntries, filtros, export
├── Converters/
│   ├── ValidationLevelToColorConverter.cs
│   ├── BoolToVisibilityConverter.cs
│   └── StageStatusToIconConverter.cs
└── Resources/
    ├── Styles.xaml
    ├── Colors.xaml
    └── Icons/
        ├── icon_16.png
        └── icon_32.png
```

#### Padrão: MVVM

```
View (XAML) ←binding→ ViewModel (C#) →commands→ Core (via interfaces)
```

- **View** nunca acessa Core ou Revit diretamente
- **ViewModel** orquestra via `IPipelineOrchestrator`
- **Commands** executados via `ExternalEvent` (thread safety)

#### O que NÃO pode conter

- ❌ Lógica de cálculo
- ❌ Acesso direto à Revit API (exceto via ExternalEvent)
- ❌ Acesso direto a JSON
- ❌ Regras normativas

---

### 2.5 DynamoScripts (Camada de Execução Externa)

#### Papel

Scripts Dynamo são **executores delegados**. O plugin (Core + Revit) decide o quê fazer; o Dynamo executa operações específicas que são mais eficientes ou possíveis apenas via Dynamo.

#### Tipos de automação delegados

| Script | Módulo | Função |
|--------|--------|--------|
| `04_InsertEquipment.dyn` | M04 | Inserção em massa de fixtures |
| `07_GenerateColdWaterNetwork.dyn` | M07 | Traçado de ramais AF |
| `08_GenerateSewerNetwork.dyn` | M08 | Traçado de ramais ES + acessórios |
| `09_ApplySlopes.dyn` | M09 | Ajuste de Z em batch |
| `13_GenerateSheets.dyn` | M13 | Layout de views em sheets |

#### Como são chamados

```
1. Core decide que precisa executar Dynamo
2. Core chama IDynamoExecutor.Execute(scriptName, inputJson)
3. Revit implementa DynamoExecutorAdapter:
   a. Escreve inputJson em arquivo temporário
   b. Invoca Dynamo.Applications.DynamoRevit.RunScript(path)
   c. Aguarda conclusão
   d. Lê outputJson do arquivo temporário
   e. Retorna resultado para Core
4. Core valida resultado
```

#### Comunicação Plugin ↔ Dynamo

```
Plugin → Dynamo:
  %TEMP%/HidraulicoPlugin/dynamo_input_{scriptId}_{timestamp}.json
  Conteúdo: { "elements": [...], "parameters": {...}, "config": {...} }

Dynamo → Plugin:
  %TEMP%/HidraulicoPlugin/dynamo_output_{scriptId}_{timestamp}.json
  Conteúdo: { "created": [...], "errors": [...], "status": "success"|"partial"|"failure" }
```

#### Limitações do Dynamo

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Não tem retorno programático nativo | Plugin não sabe se script terminou | Polling de arquivo output + timeout |
| Sem tratamento de erro robusto | Script pode "terminar" sem fazer nada | Validar delta de elementos pós-execução |
| Performance variável | Scripts complexos podem demorar | Timeout configurável (60s padrão) |
| Compatibilidade de versão | Dynamo 2.x vs. 3.x | Fixar versão e testar antes de release |

---

### 2.6 HidraulicoPlugin.Data (já detalhado em 2.1)

#### Estrutura de dados

```
data/
├── referencia_normativa.json    ← Núcleo normativo (NBR 5626 + 8160)
├── config/
│   ├── config_geral.json
│   ├── config_agua_fria.json
│   ├── config_esgoto.json
│   ├── config_ventilacao.json
│   ├── config_declividade.json
│   ├── config_dimensionamento.json
│   ├── config_automacao.json
│   ├── config_interface.json
│   └── config_validacao.json
└── logs/
    └── log_{timestamp}.json
```

#### Persistência

| Dado | Armazenamento | Quando persiste |
|------|--------------|----------------|
| Dados normativos | `referencia_normativa.json` (embedded) | Build time (nunca muda em runtime) |
| Configuração do projeto | `config/*.json` na pasta do projeto | Ao salvar config na UI |
| Logs | `logs/log_{timestamp}.json` | Ao exportar ou ao final do pipeline |
| Estado do pipeline | Memória (não persiste) | — |
| Resultados de dimensionamento | Parâmetros HID_* nos elementos Revit | Ao executar dimensionamento |

---

## 3. Fluxo de Comunicação

### 3.1 Fluxo principal (pipeline completo)

```
USUÁRIO                UI                    CORE                  REVIT                DYNAMO
   │                   │                      │                     │                    │
   │ clica "Detectar"  │                      │                     │                    │
   │──────────────────→│                      │                     │                    │
   │                   │ ExternalEvent.Raise() │                     │                    │
   │                   │──────────────────────→│                     │                    │
   │                   │                      │ orchestrator         │                    │
   │                   │                      │ .ExecuteStage(E01)   │                    │
   │                   │                      │                     │                    │
   │                   │                      │ IModelReader         │                    │
   │                   │                      │ .GetRooms()         │                    │
   │                   │                      │────────────────────→│                    │
   │                   │                      │                     │ FilteredElement    │
   │                   │                      │                     │ Collector          │
   │                   │                      │     List<RoomInfo>  │                    │
   │                   │                      │←────────────────────│                    │
   │                   │                      │                     │                    │
   │                   │                      │ classifier          │                    │
   │                   │                      │ .Classify(rooms)    │                    │
   │                   │                      │ (lógica interna)    │                    │
   │                   │                      │                     │                    │
   │                   │                      │ IDynamoExecutor     │                    │
   │                   │                      │ .Execute("07_AF")   │                    │
   │                   │                      │────────────────────→│                    │
   │                   │                      │                     │ write input.json   │
   │                   │                      │                     │───────────────────→│
   │                   │                      │                     │                    │ executa
   │                   │                      │                     │   output.json      │ script
   │                   │                      │                     │←───────────────────│
   │                   │                      │   DynamoResult      │                    │
   │                   │                      │←────────────────────│                    │
   │                   │                      │                     │                    │
   │                   │   StageResult        │                     │                    │
   │                   │←─────────────────────│                     │                    │
   │   atualiza UI     │                      │                     │                    │
   │←──────────────────│                      │                     │                    │
   │                   │                      │                     │                    │
   │ clica "Aprovar"   │                      │                     │                    │
   │──────────────────→│ próxima etapa...     │                     │                    │
```

### 3.2 Direção das dependências

```
        ┌────────────────────────────────────┐
        │                                    │
        │    Data ← Core ← Revit ← UI       │
        │                    ↑               │
        │                    │               │
        │              DynamoScripts         │
        │       (via arquivo JSON)           │
        │                                    │
        └────────────────────────────────────┘

Seta ← significa "é referenciado por"
Seta → significa "depende de"

UI → Revit → Core → Data
     Revit → DynamoScripts (via filesystem)
```

### 3.3 Quem chama quem

| Chamador | Chamado | Via |
|----------|---------|-----|
| UI (ViewModel) | Core (Orchestrator) | Interface `IPipelineOrchestrator` via `ExternalEvent` |
| Core (Orchestrator) | Core (Services) | Interface (DI) |
| Core (Orchestrator) | Revit (Adapters) | Interface do Core (ex: `IModelReader`) |
| Core (Services) | Data (Providers) | Interface `INormativeDataProvider` |
| Revit (Adapter) | Revit API | Diretamente (`Document`, `FilteredElementCollector`) |
| Revit (DynamoAdapter) | DynamoScripts | Via filesystem (JSON) + DynamoRevit.RunScript |
| **Core NUNCA chama** | Revit diretamente | — |
| **Core NUNCA chama** | UI diretamente | — |
| **Data NUNCA chama** | ninguém | — |

---

## 4. Regras de Dependência

### 4.1 Matriz de dependências permitidas

| Projeto | Data | Core | Revit | UI | Dynamo |
|---------|------|------|-------|----|--------|
| **Data** | — | ❌ | ❌ | ❌ | ❌ |
| **Core** | ✅ | — | ❌ | ❌ | ❌ |
| **Revit** | ✅ | ✅ | — | ❌ | ❌ (filesystem) |
| **UI** | ✅ | ✅ | ✅ | — | ❌ |
| **Tests** | ✅ | ✅ | ❌* | ❌ | ❌ |

*Tests de integração podem referenciar Revit via RevitTestFramework.

### 4.2 Dependências PROIBIDAS

```
❌ Core → Revit         (Core não deve saber que Revit existe)
❌ Core → UI            (Core não deve saber que UI existe)
❌ Data → Core          (Data é passivo, não chama lógica)
❌ Data → Revit         (Data é puro dados)
❌ UI → Revit API direta (UI vai via ExternalEvent)
❌ Qualquer → DynamoScripts (comunicação via arquivo)
```

### 4.3 Verificação estática

```csharp
// No arquivo .csproj do Core: NENHUMA referência a Revit
// Se um using Autodesk.* aparecer em Core: ERRO DE BUILD

// Adicionar ao Core.csproj:
// <ItemGroup>
//   <PackageReference Include="Microsoft.CodeAnalysis.BannedApiAnalyzers" />
// </ItemGroup>
// Com BannedSymbols.txt:
// N:Autodesk;Este projeto não pode referenciar Autodesk
```

---

## 5. Interfaces entre Camadas

### 5.1 Contratos Core → Revit (Abstractions)

```csharp
// Todas definidas em HidraulicoPlugin.Core.Abstractions

public interface IModelReader
{
    List<RoomInfo> GetRooms();
    List<string> GetLevelNames();
    List<HydraulicPoint> GetExistingEquipment(string roomId);
    bool HasSpaceForRoom(string roomId);
}

public interface IElementCreator
{
    string CreatePipe(PipeCreationRequest request);
    string CreateFitting(FittingCreationRequest request);
    bool ConnectElements(string elementId1, string elementId2);
    bool SetParameter(string elementId, string paramName, object value);
}

public interface IEquipmentInserter
{
    InsertionResult InsertEquipment(EquipmentInsertionRequest request);
    bool IsEquipmentFamilyAvailable(string equipmentTypeId);
    List<string> GetAvailableFamilies();
}

public interface INetworkBuilder
{
    NetworkBuildResult BuildColdWaterNetwork(NetworkBuildRequest request);
    NetworkBuildResult BuildSewerNetwork(NetworkBuildRequest request);
}

public interface ISlopeApplicator
{
    SlopeApplicationResult ApplySlopes(List<PipeSegment> segments);
    bool TryReconnectFitting(string fittingId);
    void RollbackAll(List<string> elementIds);
}

public interface IDynamoExecutor
{
    DynamoResult Execute(string scriptName, string inputJson);
    bool IsScriptAvailable(string scriptName);
}

public interface IScheduleGenerator
{
    string CreateSchedule(ScheduleRequest request);
    string CreateSheet(SheetRequest request);
}

public interface ITransactionScope : IDisposable
{
    void Commit();
    void Rollback();
}

public interface ITransactionManager
{
    ITransactionScope BeginTransaction(string name);
}
```

### 5.2 DTOs (Data Transfer Objects)

```csharp
// Todos em HidraulicoPlugin.Core.Models — sem dependência de Revit

public class RoomInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Number { get; set; }
    public string LevelName { get; set; }
    public double AreaSqM { get; set; }
    public double CenterX { get; set; }
    public double CenterY { get; set; }
    public double CenterZ { get; set; }
    public RoomType ClassifiedType { get; set; }
    public double ClassificationConfidence { get; set; }
}

public class PipeCreationRequest
{
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double StartZ { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public double EndZ { get; set; }
    public int DiameterMm { get; set; }
    public HydraulicSystem System { get; set; }
    public string LevelName { get; set; }
}

public class SizingResult
{
    public string SegmentId { get; set; }
    public double FlowRateLs { get; set; }
    public int DiameterMm { get; set; }
    public double VelocityMs { get; set; }
    public double HeadLossPerMeter { get; set; }
    public double TotalHeadLossM { get; set; }
    public double AvailablePressureMca { get; set; }
    public bool IsAdequate { get; set; }
}
```

### 5.3 Formato de dados Plugin ↔ Dynamo

```json
// Input (plugin → dynamo)
{
  "script": "07_GenerateColdWaterNetwork",
  "version": "1.0",
  "timestamp": "2026-03-18T20:00:00",
  "data": {
    "segments": [
      {
        "start": { "x": 1.0, "y": 2.0, "z": 3.0 },
        "end": { "x": 4.0, "y": 2.0, "z": 3.0 },
        "diameter_mm": 25,
        "system": "ColdWater",
        "level_name": "Térreo"
      }
    ],
    "config": {
      "material": "pvc_soldavel"
    }
  }
}

// Output (dynamo → plugin)
{
  "script": "07_GenerateColdWaterNetwork",
  "status": "success",
  "created_element_ids": [12345, 12346, 12347],
  "errors": [],
  "warnings": ["Trecho 3: desvio de pilar automático"],
  "duration_ms": 2500
}
```

---

## 6. Estratégia de Isolamento

### 6.1 Como Core não depende do Revit

```
MECANISMO: Inversão de Dependência (DIP)

1. Core DEFINE interfaces em Core.Abstractions/
   (IModelReader, IElementCreator, etc.)

2. Revit IMPLEMENTA essas interfaces em Revit.Adapters/
   (RevitModelReader, RevitElementCreator, etc.)

3. Na inicialização (App.cs), Revit REGISTRA implementações:
   ServiceRegistry.Register<IModelReader>(new RevitModelReader(doc));
   ServiceRegistry.Register<IElementCreator>(new RevitElementCreator(doc));

4. Core recebe implementações via construtor (DI):
   new PipelineOrchestrator(modelReader, classifier, ...)
   Core nunca faz "new RevitModelReader()" — nem sabe que existe.
```

### 6.2 Registro de serviços (DI manual)

```csharp
// HidraulicoPlugin.Revit.Registration.ServiceRegistry

public static class ServiceRegistry
{
    private static readonly Dictionary<Type, object> _services = new();

    public static void Register<TInterface>(TInterface implementation)
    {
        _services[typeof(TInterface)] = implementation;
    }

    public static TInterface Resolve<TInterface>()
    {
        return (TInterface)_services[typeof(TInterface)];
    }

    /// <summary>
    /// Registra todas as implementações. Chamado no OnStartup do App.cs.
    /// </summary>
    public static void RegisterAll(Document doc)
    {
        // Data
        Register<INormativeDataProvider>(new NormativeDataProvider());
        Register<IConfigProvider>(new ConfigProvider());

        // Core (lógica pura)
        var dataProvider = Resolve<INormativeDataProvider>();
        Register<IRoomClassifier>(new RoomClassifier());
        Register<IFlowRateCalculator>(new FlowRateCalculator());
        Register<IPipeSizingService>(new PipeSizingService(dataProvider));
        Register<IHeadLossCalculator>(new HeadLossCalculator());
        Register<ISlopeRules>(new SlopeRules(dataProvider));
        Register<IVentilationRules>(new VentilationRules(dataProvider));
        Register<ILogService>(LogService.Instance);

        // Revit (adapters)
        Register<IModelReader>(new RevitModelReader(doc));
        Register<IElementCreator>(new RevitElementCreator(doc));
        Register<IEquipmentInserter>(new RevitEquipmentInserter(doc));
        Register<ISlopeApplicator>(new RevitSlopeApplicator(doc));
        Register<IDynamoExecutor>(new DynamoExecutorAdapter());
        Register<ITransactionManager>(new RevitTransactionManager(doc));

        // Orchestrator
        Register<IPipelineOrchestrator>(new PipelineOrchestrator(
            Resolve<IModelReader>(),
            Resolve<IRoomClassifier>(),
            Resolve<IPointIdentificationService>(),
            Resolve<IEquipmentInserter>(),
            Resolve<IFlowRateCalculator>(),
            Resolve<IPipeSizingService>(),
            Resolve<ILogService>()
        ));
    }
}
```

### 6.3 Testabilidade resultante

```csharp
// Teste unitário: Core sem Revit

[Fact]
public void Detection_WithNoRooms_ReturnsCritical()
{
    // Mock — sem Revit envolvido
    var mockReader = new Mock<IModelReader>();
    mockReader.Setup(r => r.GetRooms()).Returns(new List<RoomInfo>());
    
    var mockLog = new Mock<ILogService>();
    
    var orchestrator = new PipelineOrchestrator(
        mockReader.Object,
        new RoomClassifier(),
        /* ... mais mocks ... */
        mockLog.Object);
    
    var result = orchestrator.ExecuteStage(PipelineStage.Detection);
    
    result.IsBlocked.Should().BeTrue();
    mockLog.Verify(l => l.Log(ValidationLevel.Critical, It.IsAny<string>()), Times.Once);
}
```

---

## 7. Estratégia de Evolução

### 7.1 Suporte a novas versões do Revit

```
Cenário: Revit 2025 muda para .NET 8

Solução: Multi-target build

HidraulicoPlugin.sln
├── HidraulicoPlugin.Data/          ← NÃO muda (sem Revit)
├── HidraulicoPlugin.Core/          ← NÃO muda (sem Revit)
├── HidraulicoPlugin.Revit2024/     ← .NET 4.8, Revit API 2024
├── HidraulicoPlugin.Revit2025/     ← .NET 8, Revit API 2025
├── HidraulicoPlugin.UI/            ← Shared (ou duplicado para .NET 8)
└── HidraulicoPlugin.Tests/         ← Testa Core (independente)

Impacto:
  Core e Data: ZERO mudanças (isolados)
  Revit: novo projeto com mesmo conjunto de Adapters
  UI: possível recompilação
  Dynamo: verificar compatibilidade
```

### 7.2 Adição de novos módulos

```
PARA adicionar módulo M16 (ex: "Cálculo de Reservatório"):

1. Core:
   - Criar namespace Core.ReservoirSizing/
   - Criar IReservoirCalculator, ReservoirCalculator
   - Adicionar PipelineStage.ReservoirSizing ao enum
   - Adicionar case no PipelineOrchestrator

2. Data:
   - Adicionar dados de reservatório no JSON (se necessário)
   - Criar ReservoirData model

3. Revit:
   - Implementar adapter se necessário (ex: ler elemento reservatório)
   - Registrar no ServiceRegistry

4. UI:
   - Adicionar botão na ExecutionTab
   - Adicionar ao ViewModel

5. Tests:
   - Criar ReservoirCalculatorTests

NÃO É NECESSÁRIO alterar módulos existentes (Open/Closed Principle).
```

### 7.3 Extensão para outros sistemas

```
PARA adicionar Água Quente (AQ) no futuro:

1. Data: adicionar seção "agua_quente" no JSON
2. Core: criar namespace Core.HotWaterSizing/
3. Core: adicionar HydraulicSystem.HotWater ao enum
4. Revit: reutilizar mesmos adapters (PipeCreator serve para AQ)
5. Config: adicionar config_agua_quente.json

A arquitetura suporta sem reestruturação.
```

---

## 8. Riscos Arquiteturais

### 8.1 Acoplamentos indevidos

| Risco | Probabilidade | Impacto | Prevenção |
|-------|-------------|---------|-----------|
| Core referenciar Revit API "por conveniência" | Alta | Crítico — quebra testabilidade | BannedApiAnalyzers no .csproj do Core |
| ViewModel acessar Document diretamente | Média | Alto — thread safety | ExternalEvent obrigatório; code review |
| Dynamo script hardcoded com caminhos | Média | Médio — quebra em outro PC | Usar caminhos relativos e variáveis |
| JSON normativo editado manualmente e corrompido | Baixa | Alto — cálculos errados | Validação de schema no startup |
| Classe "God Object" no Orchestrator | Média | Médio — manutenção difícil | Cada etapa é um IPipelineStage separado |

### 8.2 Problemas comuns em plugins Revit

| Problema | Causa | Solução na arquitetura |
|----------|-------|----------------------|
| **Cross-thread exception** | UI thread vs. Revit thread | ExternalEvent para toda operação Revit |
| **Transaction não commitada** | Exceção dentro de Transaction | TransactionHelper com try/catch/rollback |
| **Elementos lixo no modelo** | Teste ou execução falha no meio | RollBack explícito; `ITransactionScope` |
| **Famílias não encontradas** | FamilySymbol não carregado | FamilySymbolProvider verifica antes de inserir |
| **Performance** | FilteredElementCollector sem filtro | FilterHelper com filtros sempre específicos |

### 8.3 Pontos críticos

| Ponto | Módulo | Risco | Mitigação |
|-------|--------|-------|-----------|
| `PipelineOrchestrator` | Core | Pode virar God Object | Delegar para IPipelineStage por etapa |
| `ServiceRegistry` | Revit | DI manual pode ficar complexo | Manter registro centralizado e documentado |
| `DynamoExecutorAdapter` | Revit | Comunicação instável | Timeout + retry + validação pós-execução |
| `TransactionHelper` | Revit | Pode mascarar erros | Sempre registrar no log antes de rollback |
| `referencia_normativa.json` | Data | Single point of failure | Validar schema no startup; fallback para constantes |

---

## 9. Diagrama de Componentes — Resumo

```
┌─────────────────────────────────────────────────────────────────────────┐
│                              REVIT HOST                                │
│                                                                        │
│  ┌─────────────┐    ┌──────────────────────────────────────────────┐   │
│  │     UI      │    │              HidraulicoPlugin.Revit          │   │
│  │  (WPF/MVVM) │    │                                              │   │
│  │             │    │  App.cs ← Entry Point                        │   │
│  │ MainWindow  │    │  ServiceRegistry ← DI Registration           │   │
│  │ ViewModels ─┼───→│  Commands/ ← IExternalCommand                │   │
│  │             │    │  Adapters/ ← IModelReader, IElementCreator   │   │
│  │  ExternalEvent   │  Helpers/ ← Transaction, UnitConversion      │   │
│  │     ↓       │    │  ExternalEvents/ ← Thread-safe bridge        │   │
│  └─────────────┘    │                                              │   │
│                      │         ↓ implementa interfaces de ↓         │   │
│                      └──────────────────────────────────────────────┘   │
│                                         │                              │
│                      ┌──────────────────┴─────────────────────────┐   │
│                      │          HidraulicoPlugin.Core             │   │
│                      │                                            │   │
│                      │  Abstractions/ ← Interfaces (contratos)    │   │
│                      │  Pipeline/ ← Orchestrator + Stages         │   │
│                      │  Classification/ ← NLP, matching           │   │
│                      │  Sizing/ ← Q, DN, J, V, P cálculos        │   │
│                      │  Validation/ ← Regras VAL-NNN              │   │
│                      │  Diagnostics/ ← LogService                 │   │
│                      │  Models/ ← DTOs, Enums                     │   │
│                      │                                            │   │
│                      │         ↓ consome dados de ↓               │   │
│                      └──────────────────┬─────────────────────────┘   │
│                                         │                              │
│                      ┌──────────────────┴─────────────────────────┐   │
│                      │          HidraulicoPlugin.Data             │   │
│                      │                                            │   │
│                      │  Providers/ ← INormativeDataProvider       │   │
│                      │  Models/ ← POCOs (NormativeReference)      │   │
│                      │  Constants/ ← Fallback hardcoded           │   │
│                      │  Resources/ ← JSON normativos              │   │
│                      └────────────────────────────────────────────┘   │
│                                                                        │
│  ┌────────────────────┐                                                │
│  │   DynamoScripts/   │  ← Executados via DynamoRevit.RunScript       │
│  │   *.dyn            │  ← Comunicação via JSON files                 │
│  └────────────────────┘                                                │
│                                                                        │
└─────────────────────────────────────────────────────────────────────────┘
```
