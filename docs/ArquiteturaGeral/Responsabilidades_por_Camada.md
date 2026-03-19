# Responsabilidades por Camada — Plugin Hidráulico Revit

> Definição precisa do que cada camada faz e NÃO faz, simplificada para desenvolvimento solo com apoio de IA.

---

## 1. Princípios de Responsabilidade

### 1.1 Regra de ouro

```
Cada camada faz UMA coisa bem feita.
Se você está em dúvida sobre onde colocar código, está na camada errada.
```

### 1.2 Princípios aplicados

| Princípio | Aplicação prática |
|-----------|------------------|
| **Responsabilidade única** | Core calcula. Revit2026 executa no modelo. Data fornece dados. Dynamo automatiza traçado. |
| **Baixo acoplamento** | Core não sabe que Revit existe. Se você deletar Revit2026, Core compila normalmente. |
| **Alta coesão** | Tudo sobre cálculo de pressão está junto em `Core/Sizing/`. Não espalhado em 3 camadas. |
| **Simplicidade** | Sem framework de DI. Sem EventAggregator. Sem MediatR. Registro manual, direto. |

### 1.3 Simplicidade para desenvolvimento solo

```
O que este projeto NÃO precisa:
  ❌ Container de IoC (Autofac, Unity, etc.)
  ❌ CQRS
  ❌ Event Sourcing
  ❌ Repository pattern com Unit of Work
  ❌ Generic host
  ❌ Microserviços internos

O que este projeto USA:
  ✅ Interfaces simples
  ✅ Registro manual de dependências (dicionário)
  ✅ Pastas organizadas por módulo
  ✅ DTOs simples (POCOs)
  ✅ Singleton para LogService
```

---

## 2. Responsabilidade por Camada

### Visão geral em 1 frase

| Camada | Faz o quê |
|--------|-----------|
| **Data** | Guarda dados. Não pensa. |
| **Core** | Pensa e decide. Não toca no Revit. |
| **Revit2026** | Toca no Revit. Não decide. |
| **DynamoScripts** | Executa criação em massa. Não decide nem calcula. |

---

### 2.1 PluginCore

#### Responsabilidade principal

**Toda a inteligência do sistema.** Cálculos hidráulicos, regras normativas, decisões de dimensionamento, classificação de ambientes, validação de conformidade e orquestração do pipeline.

#### O que DEVE fazer

| Responsabilidade | Exemplo concreto |
|-----------------|-----------------|
| Calcular vazão | `Q = 0.30 × √ΣP` |
| Selecionar diâmetro | Iterar DNs comerciais até V ≤ 3.0 m/s |
| Calcular perda de carga | Fórmula FWH: `J = 8.69e6 × Q^1.75 / D^4.75` |
| Verificar pressão | `P_din = P_est - ΣΔH ≥ 3.0 mca` |
| Aplicar regras de declividade | DN ≤ 75 → mín 2%, DN 100 → mín 1% |
| Aplicar regras de ventilação | DN_vent ≥ 2/3 × DN_TQ |
| Classificar ambientes | "Banheiro Social" → Bathroom, confiança 0.95 |
| Mapear pontos hidráulicos | Banheiro → vaso + lavatório + chuveiro + ralo |
| Decidir o que precisa ser inserido | Comparar existentes vs. necessários |
| Validar conformidade normativa | Executar todas as regras VAL-001 a VAL-026 |
| Orquestrar o pipeline | Executar E01 → E13 em sequência |
| Registrar logs | Todos os eventos com nível, mensagem e ElementId |
| Gerar relatórios de dimensionamento | Tabela trecho × Q × DN × V × J × P |

#### O que NÃO PODE fazer

| Proibição | Por quê |
|-----------|---------|
| ❌ Usar `Autodesk.Revit.DB` | Se Core importar isso, não pode ser testado sem Revit |
| ❌ Criar Pipes, Fittings, FamilyInstances | Isso é trabalho do Revit2026 |
| ❌ Abrir Transactions | Transaction é conceito do Revit |
| ❌ Usar FilteredElementCollector | Isso é API do Revit |
| ❌ Acessar Document, UIDocument | Conceitos do Revit |
| ❌ Manipular XYZ (Autodesk.Revit.DB.XYZ) | Usar double x, y, z nos DTOs |
| ❌ Ler/escrever arquivos diretamente | Delegar para IConfigProvider |
| ❌ Exibir janelas ou dialogs | Isso é da UI |

#### Exemplos de código CORRETO no Core

```csharp
// ✅ CORRETO — Lógica pura, sem Revit
public class FlowRateCalculator : IFlowRateCalculator
{
    public double CalculateFlowRate(double sumOfWeights, double coefficientC = 0.30)
    {
        if (sumOfWeights < 0)
            throw new ArgumentException("Soma de pesos não pode ser negativa.");
        if (sumOfWeights == 0) return 0;
        return coefficientC * Math.Sqrt(sumOfWeights);
    }
}

// ✅ CORRETO — Usa interface, não implementação Revit
public class DetectionStage
{
    private readonly IModelReader _reader;
    private readonly ILogService _log;

    public DetectionStage(IModelReader reader, ILogService log)
    {
        _reader = reader;
        _log = log;
    }

    public StageResult Execute()
    {
        var rooms = _reader.GetRooms(); // Core não sabe que é Revit
        if (!rooms.Any())
        {
            _log.Log(ValidationLevel.Critical, "Modelo sem Rooms");
            return StageResult.Blocked();
        }
        return StageResult.Success(rooms);
    }
}
```

#### Exemplos de VIOLAÇÃO no Core

```csharp
// ❌ VIOLAÇÃO — Core usando Revit API
public class DetectionStage
{
    public List<RoomInfo> Execute(Document doc)  // ← ERRADO: Document é Revit
    {
        var collector = new FilteredElementCollector(doc)  // ← ERRADO: API Revit
            .OfCategory(BuiltInCategory.OST_Rooms);        // ← ERRADO: Enum Revit
        // ...
    }
}

// ❌ VIOLAÇÃO — Core abrindo Transaction
public void ApplySlopes(Document doc)
{
    using var tx = new Transaction(doc, "Slopes");  // ← ERRADO
    tx.Start();
    // ...
}

// ❌ VIOLAÇÃO — Core lendo arquivo
public void LoadConfig()
{
    string json = File.ReadAllText("config.json");  // ← ERRADO: usar IConfigProvider
}
```

#### Teste rápido: "Está no Core certo?"

```
PERGUNTA: Este código compila sem as DLLs do Revit?
  SIM → Pode estar no Core ✅
  NÃO → Não pertence ao Core ❌
```

---

### 2.2 Revit2026 (Infraestrutura)

#### Responsabilidade principal

**Traduzir ordens do Core em ações no modelo Revit.** É o "braço" que toca no modelo. Lê elementos, cria elementos, modifica elementos — sempre sob comando do Core.

#### O que DEVE fazer

| Responsabilidade | Exemplo concreto |
|-----------------|-----------------|
| Ler Rooms do modelo | `FilteredElementCollector` → converter para `RoomInfo` |
| Ler Levels | Listar Levels e devolver como `List<string>` |
| Detectar equipamentos existentes | Buscar FamilyInstances por categoria |
| Criar Pipes | `Pipe.Create(doc, pipingSystemTypeId, pipeTypeId, ...)` |
| Criar Fittings | Inserir Tees, Curvas, Registros |
| Inserir equipamentos | `doc.Create.NewFamilyInstance(...)` |
| Conectar Connectors | `connector1.ConnectTo(connector2)` |
| Gerenciar Transactions | Abrir, commitar ou reverter |
| Converter unidades | ft ↔ m, ft² ↔ m² |
| Ler/escrever parâmetros HID_* | `element.get_Parameter().Set(value)` |
| Selecionar/destacar elementos | `uidoc.Selection.SetElementIds()` |
| Executar scripts Dynamo | Escrever JSON, chamar DynamoRevit.RunScript |
| Criar Schedules e Sheets | Usar ViewSchedule.CreateSchedule, ViewSheet.Create |

#### O que NÃO PODE fazer

| Proibição | Por quê |
|-----------|---------|
| ❌ Calcular vazão | Isso é lógica de negócio → Core |
| ❌ Decidir qual DN usar | Isso é regra normativa → Core |
| ❌ Decidir se ambiente é Banheiro ou Cozinha | Classificação → Core |
| ❌ Validar conformidade normativa | Regras VAL-NNN → Core |
| ❌ Decidir declividade por DN | Tabela normativa → Core |
| ❌ Decidir quais pontos um ambiente precisa | Mapeamento → Core |
| ❌ Guardar dados normativos | Isso é da camada Data |

#### A regra do "garçom"

```
Revit2026 é um garçom:
  - Não cozinha (não calcula)
  - Não decide o cardápio (não aplica regras)
  - Entrega o que o Core pedir
  - Traz de volta o que está no modelo
```

#### Exemplos de código CORRETO no Revit2026

```csharp
// ✅ CORRETO — Apenas lê do modelo e converte para DTO do Core
public class RevitModelReader : IModelReader
{
    private readonly Document _doc;

    public RevitModelReader(Document doc) => _doc = doc;

    public List<RoomInfo> GetRooms()
    {
        return new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>()
            .Where(r => r.Location != null && r.Area > 0)
            .Select(ConvertToRoomInfo)
            .ToList();
    }

    private RoomInfo ConvertToRoomInfo(Room room)
    {
        var point = ((LocationPoint)room.Location).Point;
        return new RoomInfo
        {
            Id = room.Id.ToString(),
            Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "",
            LevelName = room.Level?.Name ?? "",
            AreaSqM = room.Area * 0.09290304, // ft² → m²
            CenterX = point.X * 0.3048,
            CenterY = point.Y * 0.3048,
            CenterZ = point.Z * 0.3048
        };
    }
}

// ✅ CORRETO — Cria elemento conforme instrução do Core
public class RevitPipeCreator : IElementCreator
{
    private readonly Document _doc;

    public string CreatePipe(PipeCreationRequest request)
    {
        // Apenas executa — não decide DN, nem sistema
        var startPt = new XYZ(
            request.StartX / 0.3048,
            request.StartY / 0.3048,
            request.StartZ / 0.3048);
        // ... cria o Pipe com os parâmetros recebidos
    }
}
```

#### Exemplos de VIOLAÇÃO no Revit2026

```csharp
// ❌ VIOLAÇÃO — Revit2026 decidindo lógica de negócio
public class RevitPipeCreator
{
    public void CreatePipeWithSizing(Document doc, double sumWeights)
    {
        double flow = 0.30 * Math.Sqrt(sumWeights);  // ← ERRADO: calcular é do Core
        int dn = flow < 0.3 ? 20 : 25;               // ← ERRADO: decidir DN é do Core
        // ...
    }
}

// ❌ VIOLAÇÃO — Revit2026 aplicando regra normativa
public class RevitSlopeApplicator
{
    public void ApplySlope(Pipe pipe)
    {
        int dn = (int)(pipe.Diameter * 304.8);
        double slope = dn <= 75 ? 0.02 : 0.01;  // ← ERRADO: regra normativa é do Core
        // ...
    }
}
```

#### Teste rápido: "Está no Revit2026 certo?"

```
PERGUNTA: Este código toma alguma decisão de engenharia hidráulica?
  SIM → Não pertence ao Revit2026 ❌ → Mover para Core
  NÃO → Pode estar no Revit2026 ✅
```

---

### 2.3 DynamoScripts

#### Papel dentro do sistema

**Executor em massa.** Quando o plugin precisa criar muitos elementos de uma vez (Pipes, Fittings, Fixtures), e a operação é mais eficiente ou prática via Dynamo, o script é chamado.

#### O que DEVE fazer

| Responsabilidade | Exemplo concreto |
|-----------------|-----------------|
| Receber lista de instruções via JSON | Ler `dynamo_input_{id}.json` |
| Criar elementos em batch | Inserir 10 fixtures de uma vez |
| Traçar caminhos de pipes | Usando nós de roteamento (ex: MEPover) |
| Reportar resultado via JSON | Escrever `dynamo_output_{id}.json` |

#### O que NÃO PODE fazer

| Proibição | Por quê |
|-----------|---------|
| ❌ Calcular vazão | Lógica de negócio → Core |
| ❌ Decidir diâmetro | Regra normativa → Core |
| ❌ Decidir declividade | Regra normativa → Core |
| ❌ Classificar ambientes | Lógica de classificação → Core |
| ❌ Validar regras normativas | Motor de validação → Core |
| ❌ Ler dados do `referencia_normativa.json` | Dados normativos → Data |
| ❌ Tomar decisões que não estão no JSON de input | Toda decisão vem do plugin |

#### A regra do "pedreiro"

```
Script Dynamo é um pedreiro:
  - Recebe a planta pronta (JSON de input)
  - Executa conforme especificado
  - Não redesenha a planta
  - Reporta o que fez (JSON de output)
```

#### Quando usar Dynamo vs. Revit API direta

| Situação | Usar |
|----------|------|
| Criar 1 Pipe | Revit API direta (mais rápido) |
| Criar 20 Pipes com roteamento | Dynamo (mais prático) |
| Inserir 1 fixture | Revit API direta |
| Inserir 15 fixtures em massa | Dynamo |
| Ajustar Z de 30 pipes | Dynamo (batch) |
| Ler parâmetro de 1 elemento | Revit API direta |
| Layout de 6 views em sheets | Dynamo |

#### Limitações aceitas

```
1. Dynamo não reporta erros de forma estruturada
   → Plugin SEMPRE valida resultado pós-execução

2. Dynamo pode travar em modelos grandes
   → Timeout de 60s; fallback para Revit API

3. Scripts .dyn são binários no Git
   → Sem merge automático; 1 branch por script

4. Dynamo exige versão específica
   → Fixar versão; testar antes de cada release
```

---

### 2.4 Data

#### Responsabilidade principal

**Armazenar e fornecer dados.** Não processa, não calcula, não decide. Apenas guarda e devolve quando perguntado.

#### O que DEVE conter

| Conteúdo | Arquivo | Exemplo |
|----------|---------|---------|
| Dados normativos | `referencia_normativa.json` | Tabela de pesos, UHCs, diâmetros |
| Configuração do sistema | `config/*.json` | Pressão mínima, material padrão |
| Constantes de fallback | `NormativeConstants.cs` | Valores hardcoded caso JSON falhe |
| Modelos de dados (POCOs) | `Models/*.cs` | Classes que espelham o JSON |
| Providers de dados | `Providers/*.cs` | Deserialização de JSON |

#### O que NÃO PODE conter

| Proibição | Por quê |
|-----------|---------|
| ❌ Cálculos | Isso é Core |
| ❌ Regras condicionais de engenharia | Isso é Core |
| ❌ Referências ao Revit | Isso é Revit2026 |
| ❌ Lógica de classificação | Isso é Core |
| ❌ Validações normativas | Isso é Core |

#### A regra da "biblioteca"

```
Data é uma biblioteca:
  - Guarda os livros (dados)
  - Empresta quando pedem (providers)
  - Não lê os livros para você (não interpreta)
  - Não escreve novos livros (não gera dados)
```

#### Exemplo CORRETO

```csharp
// ✅ CORRETO — Apenas fornece dados, não interpreta
public class NormativeDataProvider : INormativeDataProvider
{
    private NormativeReference _data;

    public NormativeReference LoadNormativeData()
    {
        if (_data == null)
        {
            string json = ReadEmbeddedResource("referencia_normativa.json");
            _data = JsonConvert.DeserializeObject<NormativeReference>(json);
        }
        return _data;
    }

    public EquipmentData GetEquipment(string equipmentId)
    {
        return _data.AguaFria.UnidadesConsumo
            .FirstOrDefault(e => e.Id == equipmentId);
    }
}
```

#### Exemplo de VIOLAÇÃO

```csharp
// ❌ VIOLAÇÃO — Data calculando vazão
public class NormativeDataProvider
{
    public double GetFlowRate(string equipmentId)
    {
        var equip = GetEquipment(equipmentId);
        return 0.30 * Math.Sqrt(equip.Peso);  // ← ERRADO: cálculo é do Core
    }
}
```

---

## 3. Limites entre Camadas

### 3.1 Mapa de fronteiras

```
┌─────────┐          ┌─────────┐          ┌─────────────┐
│  DATA   │  dados   │  CORE   │  ordens  │  REVIT2026  │
│         │────────→ │         │────────→ │             │
│ JSON    │          │ Calcula │          │ Executa no  │
│ Config  │          │ Decide  │          │ modelo      │
│ POCOs   │          │ Valida  │ result.  │             │
│         │          │         │←──────── │ Lê e cria   │
└─────────┘          └─────────┘          └──────┬──────┘
                                                  │
                                           JSON   │  JSON
                                           input  ↓  output
                                          ┌───────────────┐
                                          │ DYNAMO SCRIPTS│
                                          │ Cria em massa │
                                          └───────────────┘
```

### 3.2 Onde cada coisa vive — tabela definitiva

| Ação | Camada | Justificativa |
|------|--------|---------------|
| `Q = 0.30 × √ΣP` | **Core** | Cálculo de engenharia |
| `FilteredElementCollector` | **Revit2026** | API do Revit |
| Pesos e UHCs dos aparelhos | **Data** | Dados normativos fixos |
| "BWC" → Banheiro? | **Core** | Decisão de classificação |
| `Pipe.Create(doc, ...)` | **Revit2026** | Criação via API |
| DN 100 → declividade mín 1% | **Core** | Regra normativa |
| Ajustar Z de 20 pipes de uma vez | **Dynamo** | Batch operation |
| `referencia_normativa.json` | **Data** | Armazenamento de dados |
| Pressão ≥ 3.0 mca? | **Core** | Validação normativa |
| Destacar elemento com erro no modelo | **Revit2026** | Interação visual Revit |
| Exibir log no DataGrid | **UI** | Apresentação |
| Decidir se pipeline pode avançar | **Core** | Decisão de negócio |
| Abrir Transaction | **Revit2026** | Operação do Revit |
| Definir que banheiro precisa de 4 aparelhos | **Core** (consultando **Data**) | Regra + dados |

### 3.3 Erros comuns de fronteira

| Erro | Onde colocou | Onde deveria estar | Como corrigir |
|------|-------------|-------------------|---------------|
| Calcular vazão dentro do Adapter | Revit2026 | Core | Extrair para `FlowRateCalculator` |
| Hardcoded `MIN_PRESSURE = 3.0` no Revit | Revit2026 | Data/Core | Usar `NormativeConstants.MinPressureMca` |
| Classificar ambiente dentro do RoomReader | Revit2026 | Core | RoomReader retorna nome cru; `RoomClassifier` classifica |
| Decidir DN dentro do PipeCreator | Revit2026 | Core | PipeCreator recebe DN pronto no request |
| Script Dynamo com lógica `if DN < 100 then...` | Dynamo | Core | Toda lógica no JSON de input |
| `File.ReadAllText()` dentro do Core | Core | Data | Usar `IConfigProvider.LoadConfig()` |

---

## 4. Regras de Comunicação

### 4.1 Quem chama quem

```
✅ PERMITIDO:
  Core chama Data (via interface INormativeDataProvider)
  Core chama suas próprias classes (FlowRateCalculator, PipeSizingService)
  Revit2026 chama Core (via interfaces do Core)
  Revit2026 chama Data (para registro de DI)
  Revit2026 chama Revit API (diretamente)
  Revit2026 chama Dynamo (via filesystem)
  UI chama Core (via IPipelineOrchestrator + ExternalEvent)

❌ PROIBIDO:
  Core chamar Revit2026
  Core chamar Dynamo
  Core chamar UI
  Data chamar qualquer camada
  Dynamo chamar Core
  Dynamo chamar Revit2026 (exceto via nós nativos)
```

### 4.2 Fluxo de uma etapa típica

```
1. UI: Usuário clica "Executar E01"
2. UI: ViewModel dispara ExternalEvent
3. Revit2026: ExternalEventHandler recebe
4. Revit2026: Resolve IPipelineOrchestrator do ServiceRegistry
5. Core: Orchestrator executa DetectionStage
6. Core: DetectionStage chama IModelReader.GetRooms()
7. Revit2026: RevitModelReader faz FilteredElementCollector
8. Revit2026: Converte Rooms para List<RoomInfo>
9. Core: Recebe List<RoomInfo>, processa, valida
10. Core: Retorna StageResult
11. UI: ViewModel recebe resultado, atualiza tela
```

### 4.3 Formato de comunicação entre camadas

| De → Para | Formato |
|-----------|---------|
| Data → Core | POCOs deserializados de JSON |
| Core → Revit2026 | DTOs simples (RoomInfo, PipeCreationRequest) |
| Revit2026 → Core | DTOs simples (List<RoomInfo>, InsertionResult) |
| Core → UI | StageResult, List<LogEntry> |
| Revit2026 → Dynamo | JSON file (`dynamo_input_{id}.json`) |
| Dynamo → Revit2026 | JSON file (`dynamo_output_{id}.json`) |

### 4.4 Interfaces entre camadas — lista completa

```csharp
// Core DEFINE, Revit2026 IMPLEMENTA:

IModelReader          → RevitModelReader          // Ler modelo
IElementCreator       → RevitElementCreator       // Criar pipes/fittings
IEquipmentInserter    → RevitEquipmentInserter    // Inserir fixtures
ISlopeApplicator      → RevitSlopeApplicator      // Ajustar Z dos pipes
IDynamoExecutor       → DynamoExecutorAdapter     // Executar scripts
IScheduleGenerator    → RevitScheduleGenerator    // Criar tabelas/pranchas
ITransactionManager   → RevitTransactionManager   // Gerenciar transactions

// Core DEFINE, Data IMPLEMENTA:

INormativeDataProvider → NormativeDataProvider     // Dados normativos
IConfigProvider        → ConfigProvider            // Configuração
```

---

## 5. Anti-Padrões (PROIBIDO)

### 5.1 Os 10 pecados capitais

| # | Anti-padrão | Exemplo | Consequência |
|---|------------|---------|-------------|
| 1 | **Lógica de negócio no Revit2026** | Calcular Q dentro do RoomReader | Core não testável para esse cálculo |
| 2 | **Revit API no Core** | `using Autodesk.Revit.DB` no Core | Core não compila sem DLLs do Revit |
| 3 | **Regra normativa no Dynamo** | Nó code block com `if DN < 100` | Regra duplicada, impossível de manter |
| 4 | **Dependência circular** | Core referencia Revit2026 que referencia Core | Compilação impossível |
| 5 | **God Class** | `HydraulicManager` com 3000 linhas | Impossível de manter e testar |
| 6 | **Dados normativos hardcoded no código** | `double minPressure = 3.0;` espalhado | Mudar valor = buscar em 50 arquivos |
| 7 | **Transaction fora do Revit2026** | Core abrindo Transaction | Core acoplado ao Revit |
| 8 | **Decisão no Dynamo** | Script decide qual DN usar | Lógica fora do controle do plugin |
| 9 | **UI acessando Document direto** | ViewModel fazendo FilteredElementCollector | Thread crash garantido |
| 10 | **Core lendo arquivos direto** | `File.ReadAllText("config.json")` no Core | Core acoplado ao filesystem |

### 5.2 Como detectar violações

```
SINAIS DE ALERTA no Core:
  🚨 using Autodesk.Revit.DB
  🚨 using System.IO (exceto em Data)
  🚨 new Transaction(
  🚨 new FilteredElementCollector(
  🚨 parâmetro do tipo Document
  🚨 parâmetro do tipo ElementId
  🚨 parâmetro do tipo XYZ

SINAIS DE ALERTA no Revit2026:
  🚨 Math.Sqrt( (provavelmente é cálculo de vazão)
  🚨 if (dn <= 75) slope = 0.02 (regra normativa)
  🚨 Constantes mágicas de engenharia (3.0, 0.30, 40.0)
  🚨 Classificação de string ("Banheiro", "BWC")

SINAIS DE ALERTA no Dynamo:
  🚨 Code block com fórmulas de engenharia
  🚨 Code block com if/else baseado em norma
  🚨 Leitura direta de JSON normativo
```

---

## 6. Estratégia para Desenvolvimento Solo

### 6.1 Como manter organização sozinho

| Prática | Frequência | Como fazer |
|---------|-----------|-----------|
| **Revisar camada antes de commitar** | A cada commit | Usar o checklist da seção 7 |
| **Teste de compilação do Core isolado** | A cada 3 commits | Build apenas `HidraulicoPlugin.Core.csproj` |
| **Grep por violações** | Semanal | Buscar `Autodesk.Revit` no Core |
| **Nomear arquivos pela camada** | Sempre | Se está em `Core/`, não pode ter Revit |
| **Pedir revisão à IA** | A cada módulo completo | Colar a classe e perguntar "está na camada certa?" |

### 6.2 Sequência de desenvolvimento por funcionalidade

```
PARA cada funcionalidade (ex: "Detecção de ambientes"):

1. PRIMEIRO → Data
   Verificar se os dados necessários estão no JSON
   Se não, adicionar ao JSON ou Data/Models

2. SEGUNDO → Core
   Implementar a lógica (classificar, calcular, validar)
   Testar com xUnit (sem Revit)
   Confirmar que compila sem Revit

3. TERCEIRO → Revit2026
   Implementar o Adapter (RevitModelReader)
   Registrar no ServiceRegistry
   Testar dentro do Revit

4. QUARTO → Dynamo (se aplicável)
   Criar script .dyn
   Definir JSON de input/output
   Testar no Dynamo Player

5. QUINTO → UI
   Adicionar botão/estado na janela
   Conectar ao ViewModel
```

### 6.3 Quando a IA gera código

```
PROCEDIMENTO:

1. Especificar CAMADA no prompt:
   "Crie a classe FlowRateCalculator no CORE"
   "Crie o adapter RevitModelReader no REVIT2026"

2. Verificar que a IA não violou camadas:
   □ Core sem using Autodesk?
   □ Revit2026 sem cálculos de engenharia?
   □ Data sem lógica?

3. Se violou → pedir correção antes de aceitar
```

### 6.4 Atalho mental para decidir camada

```
QUANDO ESCREVENDO CÓDIGO, PERGUNTE:

"Isso é um DADO (tabela, valor, constante)?"
  → Data

"Isso é uma DECISÃO ou CÁLCULO?"
  → Core

"Isso TOCA no modelo Revit?"
  → Revit2026

"Isso CRIA MUITOS ELEMENTOS de uma vez?"
  → Dynamo (via Revit2026)

"Isso MOSTRA ALGO na tela?"
  → UI
```

---

## 7. Checklist de Validação Arquitetural

### 7.1 Antes de cada commit

```
CORE:
  □ Nenhum "using Autodesk" no arquivo
  □ Nenhum parâmetro do tipo Document, ElementId ou XYZ
  □ Nenhum new Transaction(
  □ Nenhum File.ReadAllText( ou File.WriteAllText(
  □ Nenhuma constante mágica de engenharia sem usar NormativeConstants
  □ Projeto Core compila sozinho (sem Revit DLLs)

REVIT2026:
  □ Nenhum cálculo de vazão, pressão, DN ou declividade
  □ Nenhuma regra normativa (if dn <= 75 then slope = 0.02)
  □ Toda decisão vem como parâmetro, não é calculada internamente
  □ Conversão de unidades feita ao receber/enviar dados (fronteira)

DATA:
  □ Nenhuma lógica de cálculo
  □ Nenhuma referência ao Revit
  □ Apenas POCOs e providers de leitura

DYNAMO:
  □ Nenhuma lógica normativa no script
  □ Toda decisão vem do JSON de input
  □ Output reporta o que foi feito (element IDs, status)
```

### 7.2 Antes de cada PR/merge

```
GERAL:
  □ Solution compila sem erros
  □ Core.csproj não referencia Revit DLLs
  □ Testes unitários passam (Core testado sem Revit)
  □ Nenhum TODO sem issue correspondente
  □ Convenções de nomenclatura seguidas
  □ XML docs em métodos públicos
```

### 7.3 Verificação rápida por busca (grep)

```bash
# Executar na raiz do projeto periodicamente:

# 1. Core não pode ter Revit
grep -r "Autodesk.Revit" src/HidraulicoPlugin.Core/
# Esperado: 0 resultados

# 2. Core não pode ter File I/O direto
grep -r "File.ReadAllText\|File.WriteAllText\|StreamReader\|StreamWriter" src/HidraulicoPlugin.Core/
# Esperado: 0 resultados

# 3. Revit não pode ter cálculos de engenharia
grep -r "Math.Sqrt\|Math.Pow" src/HidraulicoPlugin.Revit/
# Esperado: 0 resultados (ou apenas em UnitConversionHelper)

# 4. Constantes mágicas no Revit
grep -rn "= 3\.0\|= 0\.30\|= 40\.0\|= 0\.02\|= 0\.01" src/HidraulicoPlugin.Revit/
# Esperado: 0 resultados (devem estar em NormativeConstants ou Config)
```

---

## 8. Resumo Visual

```
┌─────────────────────────────────────────────────────────────┐
│                                                             │
│   DATA          CORE           REVIT2026       DYNAMO       │
│   ─────         ─────          ──────────      ──────       │
│   Guarda        Pensa          Executa         Executa      │
│   Fornece       Decide         Traduz          em massa     │
│   Não pensa     Não toca       Não decide      Não decide   │
│                 no Revit                                    │
│                                                             │
│   Biblioteca    Engenheiro     Garçom          Pedreiro     │
│                                                             │
│   JSON → DTO    DTO → Decisão  Decisão → API  JSON → elem. │
│                                                             │
│   Testável:     Testável:      Testável:       Testável:    │
│   trivial       xUnit+Mock     RevitTestFw     manual       │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```
