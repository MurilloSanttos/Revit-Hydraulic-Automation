# Diagrama de Arquitetura Geral — Plugin Hidráulico Revit

> Representação visual completa da arquitetura do sistema em Mermaid.

---

## 1. Arquitetura Geral do Sistema

```mermaid
graph TD
    subgraph USUARIO["👤 Usuário / Engenheiro"]
        U_INPUT["Configurar + Executar + Aprovar"]
    end

    subgraph REVIT_HOST["Autodesk Revit 2026"]

        subgraph UI_LAYER["Camada UI — HidraulicoPlugin.UI"]
            UI_MAIN["MainWindow"]
            UI_CONFIG["ConfigTab"]
            UI_EXEC["ExecutionTab"]
            UI_DIAG["DiagnosticsTab"]
            UI_VM["ViewModels MVVM"]
        end

        subgraph ORCHESTRATOR["Orquestrador — PipelineEngine"]
            ORC_ENGINE["PipelineEngine"]
            ORC_STATE["PipelineState"]
            ORC_STAGES["13 Stages E01-E13"]
            ORC_ROLLBACK["RollbackManager"]
            ORC_GATE["Validation Gates"]
        end

        subgraph CORE_LAYER["Camada Core — HidraulicoPlugin.Core"]

            subgraph CORE_CLASS["Classificação"]
                CL_CLASSIFIER["RoomClassifier"]
                CL_NORMALIZER["NameNormalizer"]
            end

            subgraph CORE_POINTS["Pontos Hidráulicos"]
                PT_SERVICE["PointIdentificationService"]
                PT_PROVIDER["PointRequirementProvider"]
            end

            subgraph CORE_SIZING["Motor de Dimensionamento"]
                SZ_FLOW["FlowRateCalculator"]
                SZ_PIPE["PipeSizingService"]
                SZ_LOSS["HeadLossCalculator"]
                SZ_PRESS["PressureVerificationService"]
            end

            subgraph CORE_RULES["Motor Normativo"]
                RL_SLOPE["SlopeRules"]
                RL_VENT["VentilationRules"]
                RL_DIAM["DiameterRules"]
            end

            subgraph CORE_VALID["Validador"]
                VD_ENGINE["ValidationEngine"]
                VD_RULES["26 Regras VAL-NNN"]
                VD_AGGREG["ResultAggregator"]
            end

            subgraph CORE_LOG["Diagnóstico"]
                LOG_SVC["LogService"]
                LOG_EXPORT["LogExporter"]
            end

            subgraph CORE_ABS["Abstrações / Interfaces"]
                ABS_READER["IModelReader"]
                ABS_CREATOR["IElementCreator"]
                ABS_INSERTER["IEquipmentInserter"]
                ABS_DYNAMO["IDynamoExecutor"]
                ABS_SLOPE["ISlopeApplicator"]
                ABS_TX["ITransactionManager"]
            end
        end

        subgraph REVIT_LAYER["Camada Revit — HidraulicoPlugin.Revit"]

            subgraph REVIT_ADAPT["Adapters"]
                RA_READER["RevitModelReader"]
                RA_CREATOR["RevitElementCreator"]
                RA_INSERTER["RevitEquipmentInserter"]
                RA_SLOPE["RevitSlopeApplicator"]
                RA_SCHEDULE["RevitScheduleGenerator"]
                RA_TX["RevitTransactionManager"]
            end

            subgraph REVIT_DYN["Executor Dynamo"]
                DYN_ADAPTER["DynamoExecutorAdapter"]
                DYN_SERIAL["JSON Serializer"]
                DYN_POLL["Output Polling"]
            end

            subgraph REVIT_UNMEP["Executor unMEP"]
                UNMEP_ADAPTER["UnMepAdapter"]
                UNMEP_SNAP["ModelSnapshot"]
                UNMEP_VALID["UnMepValidator"]
            end

            subgraph REVIT_HELPERS["Helpers"]
                HLP_UNIT["UnitConversionHelper"]
                HLP_FILTER["FilterHelper"]
                HLP_PARAM["ParameterHelper"]
            end

            REVIT_CMD["Commands / ExternalEvents"]
            REVIT_REG["ServiceRegistry"]
        end

        subgraph DATA_LAYER["Camada Data — HidraulicoPlugin.Data"]
            DATA_JSON["referencia_normativa.json"]
            DATA_CONFIG["config/*.json"]
            DATA_PROVIDER["NormativeDataProvider"]
            DATA_CFGPROV["ConfigProvider"]
            DATA_CONST["NormativeConstants"]
            DATA_MODELS["Models / POCOs"]
        end
    end

    subgraph EXTERNAL["Ferramentas Externas"]
        EXT_DYNAMO["Dynamo Player"]
        EXT_UNMEP["unMEP"]
        EXT_REVITAPI["Revit API"]
    end

    subgraph DYNAMO_SCRIPTS["DynamoScripts"]
        DYN_04["04_InsertEquipment.dyn"]
        DYN_07["07_GenerateColdWaterNetwork.dyn"]
        DYN_08["08_GenerateSewerNetwork.dyn"]
        DYN_09["09_ApplySlopes.dyn"]
        DYN_13["13_GenerateSheets.dyn"]
    end

    subgraph FILESYSTEM["Sistema de Arquivos"]
        FS_INPUT["dynamo_input.json"]
        FS_OUTPUT["dynamo_output.json"]
        FS_STATE["pipeline_state.json"]
        FS_LOGS["logs/*.json"]
    end

    %% Fluxo do Usuário
    U_INPUT --> UI_MAIN
    UI_MAIN --> UI_VM
    UI_VM --> REVIT_CMD

    %% UI → Orquestrador
    REVIT_CMD --> ORC_ENGINE
    ORC_ENGINE --> ORC_STATE
    ORC_ENGINE --> ORC_STAGES
    ORC_ENGINE --> ORC_GATE
    ORC_ENGINE --> ORC_ROLLBACK
    ORC_GATE --> UI_VM

    %% Orquestrador → Core
    ORC_STAGES --> CL_CLASSIFIER
    ORC_STAGES --> PT_SERVICE
    ORC_STAGES --> SZ_FLOW
    ORC_STAGES --> SZ_PIPE
    ORC_STAGES --> VD_ENGINE
    ORC_STAGES --> LOG_SVC

    %% Core → Abstrações
    CL_CLASSIFIER --> ABS_READER
    PT_SERVICE --> DATA_PROVIDER
    SZ_FLOW --> DATA_PROVIDER
    SZ_PIPE --> DATA_PROVIDER
    RL_SLOPE --> DATA_PROVIDER
    RL_VENT --> DATA_PROVIDER
    VD_ENGINE --> VD_RULES

    %% Abstrações → Adapters (DIP)
    ABS_READER -.->|implementa| RA_READER
    ABS_CREATOR -.->|implementa| RA_CREATOR
    ABS_INSERTER -.->|implementa| RA_INSERTER
    ABS_DYNAMO -.->|implementa| DYN_ADAPTER
    ABS_SLOPE -.->|implementa| RA_SLOPE
    ABS_TX -.->|implementa| RA_TX

    %% Adapters → Revit API
    RA_READER --> EXT_REVITAPI
    RA_CREATOR --> EXT_REVITAPI
    RA_INSERTER --> EXT_REVITAPI
    RA_SLOPE --> EXT_REVITAPI
    RA_SCHEDULE --> EXT_REVITAPI
    RA_TX --> EXT_REVITAPI

    %% Dynamo Adapter → Scripts
    DYN_ADAPTER --> DYN_SERIAL
    DYN_SERIAL --> FS_INPUT
    FS_INPUT --> EXT_DYNAMO
    EXT_DYNAMO --> DYN_04
    EXT_DYNAMO --> DYN_07
    EXT_DYNAMO --> DYN_08
    EXT_DYNAMO --> DYN_09
    EXT_DYNAMO --> DYN_13
    EXT_DYNAMO --> FS_OUTPUT
    FS_OUTPUT --> DYN_POLL
    DYN_POLL --> DYN_ADAPTER

    %% unMEP Adapter
    UNMEP_ADAPTER --> UNMEP_SNAP
    UNMEP_ADAPTER --> EXT_UNMEP
    UNMEP_ADAPTER --> UNMEP_VALID

    %% Data
    DATA_PROVIDER --> DATA_JSON
    DATA_CFGPROV --> DATA_CONFIG
    DATA_PROVIDER --> DATA_MODELS

    %% Persistência
    ORC_STATE --> FS_STATE
    LOG_SVC --> FS_LOGS
    LOG_SVC --> LOG_EXPORT

    %% Registry
    REVIT_REG --> RA_READER
    REVIT_REG --> RA_CREATOR
    REVIT_REG --> DYN_ADAPTER
    REVIT_REG --> UNMEP_ADAPTER

    %% Helpers
    RA_READER --> HLP_UNIT
    RA_READER --> HLP_FILTER
    RA_CREATOR --> HLP_PARAM
```

---

## 2. Fluxo de Execução da Pipeline

```mermaid
flowchart LR
    E01["E01\nDetecção"] --> E02["E02\nClassificação"]
    E02 --> E03["E03\nPontos"]
    E03 --> E04["E04\nInserção"]
    E04 --> E05["E05\nValidação Equip."]
    E05 --> E06["E06\nPrumadas"]
    E06 --> E07["E07\nRede AF"]
    E06 --> E08["E08\nRede ES"]
    E08 --> E09["E09\nInclinações"]
    E07 --> E10["E10\nSistemas MEP"]
    E09 --> E10
    E10 --> E11["E11\nDimensionamento"]
    E11 --> E12["E12\nTabelas"]
    E12 --> E13["E13\nPranchas"]

    style E01 fill:#4CAF50,color:#fff
    style E02 fill:#4CAF50,color:#fff
    style E03 fill:#4CAF50,color:#fff
    style E04 fill:#FF9800,color:#fff
    style E05 fill:#4CAF50,color:#fff
    style E06 fill:#2196F3,color:#fff
    style E07 fill:#FF9800,color:#fff
    style E08 fill:#FF9800,color:#fff
    style E09 fill:#FF9800,color:#fff
    style E10 fill:#2196F3,color:#fff
    style E11 fill:#4CAF50,color:#fff
    style E12 fill:#2196F3,color:#fff
    style E13 fill:#FF9800,color:#fff
```

**Legenda:**
- 🟢 Verde = Core (lógica pura)
- 🟠 Laranja = Dynamo / unMEP (execução externa)
- 🔵 Azul = Revit API direta

---

## 3. Máquina de Estados por Etapa

```mermaid
stateDiagram-v2
    [*] --> Pending
    Pending --> Running: executar
    Running --> WaitingApproval: sucesso
    Running --> Failed: erro
    WaitingApproval --> Completed: aprovar
    WaitingApproval --> Failed: rejeitar
    Failed --> Running: retry
    Completed --> RolledBack: desfazer
    RolledBack --> Pending: reset
    
    note right of Completed
        Libera próxima etapa
    end note
    
    note right of Failed
        Máx 3 tentativas
    end note
    
    note right of WaitingApproval
        Usuário decide
    end note
```

---

## 4. Fluxo de Comunicação Plugin ↔ Dynamo

```mermaid
sequenceDiagram
    participant Core as PluginCore
    participant Adapter as DynamoExecutorAdapter
    participant FS as FileSystem
    participant Dynamo as Dynamo Player
    participant Model as Revit Model

    Core->>Core: Calcular instruções
    Core->>Adapter: Execute(scriptName, instructions)
    Adapter->>Adapter: Serializar para JSON
    Adapter->>FS: Escrever input.json
    Adapter->>Dynamo: RunScript(path.dyn)
    Dynamo->>FS: Ler input.json
    Dynamo->>Model: Criar elementos
    Dynamo->>FS: Escrever output.json
    
    loop Polling (1s interval, 60s timeout)
        Adapter->>FS: output.json existe?
    end
    
    Adapter->>FS: Ler output.json
    Adapter->>Adapter: Deserializar resultado
    Adapter->>Core: DynamoResult
    Core->>Core: Validar elementos criados
    Adapter->>FS: Cleanup (deletar JSONs)
```

---

## 5. Fluxo de Comunicação Plugin ↔ unMEP

```mermaid
sequenceDiagram
    participant Core as PluginCore
    participant Adapter as UnMepAdapter
    participant Model as Revit Model
    participant UnMEP as unMEP

    Core->>Adapter: PrepareAndExecute(request)
    Adapter->>Model: Snapshot ANTES
    Adapter->>Model: Selecionar connectors
    Adapter->>Model: Configurar PipingSystem
    Adapter->>Model: Ativar vista correta
    Adapter->>UnMEP: Disparar comando
    UnMEP->>Model: Criar pipes e fittings
    
    loop Polling (2s interval, 30s timeout)
        Adapter->>Model: Snapshot ATUAL
        Adapter->>Adapter: Comparar com ANTES
    end
    
    Adapter->>Adapter: Calcular delta
    Adapter->>Core: UnMepResult (novos ElementIds)
    Core->>Core: Validar conectividade
    Core->>Core: Validar DNs
    Core->>Core: Validar conformidade normativa
```

---

## 6. Fluxo de Dependências entre Projetos

```mermaid
graph BT
    DATA["HidraulicoPlugin.Data\n(JSON, POCOs, Providers)"]
    CORE["HidraulicoPlugin.Core\n(Lógica, Cálculos, Regras)"]
    REVIT["HidraulicoPlugin.Revit\n(Adapters, Commands, Helpers)"]
    UI["HidraulicoPlugin.UI\n(Views, ViewModels, WPF)"]
    TESTS["HidraulicoPlugin.Tests\n(xUnit, Mocks)"]
    DYNAMO["DynamoScripts/\n(.dyn files)"]

    CORE -->|ref| DATA
    REVIT -->|ref| CORE
    REVIT -->|ref| DATA
    UI -->|ref| CORE
    UI -->|ref| REVIT
    TESTS -->|ref| CORE
    TESTS -->|ref| DATA
    DYNAMO -.->|JSON via filesystem| REVIT

    style DATA fill:#E8F5E9,stroke:#4CAF50,color:#000
    style CORE fill:#E3F2FD,stroke:#2196F3,color:#000
    style REVIT fill:#FFF3E0,stroke:#FF9800,color:#000
    style UI fill:#F3E5F5,stroke:#9C27B0,color:#000
    style TESTS fill:#ECEFF1,stroke:#607D8B,color:#000
    style DYNAMO fill:#FFF9C4,stroke:#FFC107,color:#000
```

---

## 7. Cascata de Fallback para Criação de Redes

```mermaid
flowchart TD
    START["Trecho a criar"] --> CHECK{"Rota simples?\n(reta, sem obstáculo)"}
    
    CHECK -->|Sim| API["Nível 1: Revit API Direta\n< 2s"]
    CHECK -->|Não| DYN{"Rota moderada?\n(batch, sem pathfinding)"}
    
    API -->|Sucesso| OK["✅ Trecho criado"]
    API -->|Falha| DYN
    
    DYN -->|Sim| DYNAMO["Nível 2: Dynamo Script\n< 30s"]
    DYN -->|Não| UNMEP_CHECK{"unMEP disponível?"}
    
    DYNAMO -->|Sucesso| OK
    DYNAMO -->|Falha| UNMEP_CHECK
    
    UNMEP_CHECK -->|Sim| UNMEP["Nível 3: unMEP\n< 60s"]
    UNMEP_CHECK -->|Não| MANUAL
    
    UNMEP -->|Sucesso| OK
    UNMEP -->|Falha| MANUAL["Nível 4: Manual\n⚠️ Destacar no modelo"]

    style START fill:#37474F,color:#fff
    style OK fill:#4CAF50,color:#fff
    style MANUAL fill:#F44336,color:#fff
    style API fill:#2196F3,color:#fff
    style DYNAMO fill:#FF9800,color:#fff
    style UNMEP fill:#9C27B0,color:#fff
```

---

## 8. Visão por Camada — Responsabilidades

```mermaid
graph TD
    subgraph UI["UI — Apresentação"]
        UI1["Exibe dados"]
        UI2["Recebe input do usuário"]
        UI3["Controla fluxo semi-auto"]
    end

    subgraph ORCH["Orquestrador — Coordenação"]
        O1["Executa etapas em sequência"]
        O2["Controla estado e gates"]
        O3["Gerencia rollback"]
    end

    subgraph CORE["Core — Inteligência"]
        C1["Calcula: Q, DN, V, J, P"]
        C2["Decide: classificação, pontos"]
        C3["Valida: conformidade normativa"]
    end

    subgraph REVIT["Revit — Execução"]
        R1["Lê modelo"]
        R2["Cria elementos"]
        R3["Gerencia transactions"]
    end

    subgraph DATA["Data — Armazenamento"]
        D1["JSON normativo"]
        D2["Configuração"]
        D3["Constantes"]
    end

    subgraph EXT["Externos — Ferramentas"]
        E1["Dynamo: batch"]
        E2["unMEP: roteamento"]
    end

    UI --> ORCH
    ORCH --> CORE
    CORE --> DATA
    ORCH --> REVIT
    REVIT --> EXT

    style UI fill:#F3E5F5,stroke:#9C27B0
    style ORCH fill:#FFEBEE,stroke:#F44336
    style CORE fill:#E3F2FD,stroke:#2196F3
    style REVIT fill:#FFF3E0,stroke:#FF9800
    style DATA fill:#E8F5E9,stroke:#4CAF50
    style EXT fill:#FFF9C4,stroke:#FFC107
```
