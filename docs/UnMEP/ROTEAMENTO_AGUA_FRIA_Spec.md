# Roteamento Automático de Água Fria — Especificação Técnica

> Módulo: `Revit2026.Modules.UnMEP.ColdWaterRoutingService`
> Pipeline Hidráulico Revit 2026 | v1.0

---

## 1. INICIALIZAÇÃO DO ROTEAMENTO

### Pré-condições verificadas automaticamente

| # | Validação | Critério de Aprovação | Ação se Falhar |
|---|---|---|---|
| 1 | Rooms presentes | `Count > 0` | ❌ Aborta pipeline |
| 2 | Rooms com nome | `Name ≠ null && Name ≠ "Room"` | ⚠️ Warning (continua) |
| 3 | Spaces MEP | `Count > 0` | ⚠️ Warning — recomenda executar `02_CriarSpacesMassivo.dyn` |
| 4 | Fixtures plumbingAF | `HasColdWaterConnector == true` | ❌ Aborta se nenhum |
| 5 | Fixtures posicionados | `LocationPoint ≠ null` | ⚠️ Warning (ignora fixture) |
| 6 | Levels presentes | `Count > 0` | ❌ Aborta pipeline |
| 7 | PipeType carregado | `PipeType existe no modelo` | ❌ Aborta pipeline |
| 8 | PipingSystemType AF | `SystemClassification == DomesticColdWater` | ❌ Aborta pipeline |

### Log de estado inicial

```json
{
  "scriptPath": "ColdWaterRoutingService",
  "startTime": "2026-03-31T12:00:00Z",
  "inputJson": "{ config completo }"
}
```

---

## 2. CONFIGURAÇÃO DE PARÂMETROS

### Configuração via `ColdWaterRoutingConfig`

| Parâmetro | Tipo | Default | Descrição |
|---|---|---|---|
| `sistema` | string | `"Água Fria"` | Identificador do sistema |
| `diametroPadraoMm` | double | `25` | Diâmetro fallback |
| `alturaRamalM` | double | `0.60` | Altura base do ramal (m) |
| `alturaColunaM` | double | `2.80` | Altura da coluna vertical (m) |
| `offsetParedeM` | double | `0.05` | Offset mínimo da parede (m) |
| `offsetMinimoEntreEixosM` | double | `0.30` | Distância mínima entre eixos (m) |
| `slopePercent` | double | `0` | Inclinação (0 para AF) |
| `connectToExisting` | bool | `true` | Conectar a pipes existentes |
| `toleranceMm` | double | `5` | Tolerância de conexão (mm) |
| `maxRetries` | int | `2` | Tentativas de reconexão |
| `prefixoNomenclatura` | string | `"AF"` | Prefixo para logs e nomes |

### Diâmetros por aparelho (NBR 5626:2020)

| Aparelho | Diâmetro (mm) | Altura sub-ramal (m) |
|---|---|---|
| Lavatório | 20 | 0.60 |
| Vaso Sanitário | 25 | 0.30 |
| Chuveiro | 25 | 2.20 |
| Pia Cozinha | 25 | 1.20 |
| Tanque | 25 | 1.20 |
| Máquina de Lavar | 25 | 0.80 |
| Bidê | 20 | 0.30 |
| Banheira | 25 | 0.50 |
| Torneira Jardim | 20 | 0.60 |

---

## 3. DISPARO DO ROTEAMENTO AUTOMÁTICO

### Fluxo de execução

```
ExecutarPipelineCompleto(doc, config)
│
├─ FASE 1: ValidarPreCondicoes(doc)
│   ├─ Rooms ✓
│   ├─ Spaces ✓
│   ├─ Fixtures ✓ (com conector AF)
│   ├─ Levels ✓
│   ├─ PipeType ✓
│   └─ SystemType DomesticColdWater ✓
│
├─ FASE 2: ExecutarRoteamento(doc, config)
│   ├─ CollectColdWaterFixtures()
│   │   └─ Filtra OST_PlumbingFixtures com Domain.DomainPiping
│   │
│   ├─ ResolvePipeType() + ResolveColdWaterSystemType()
│   │
│   ├─ Agrupar fixtures por Level (ordenado por elevation)
│   │
│   ├─ Para cada fixture:
│   │   ├─ Resolver diâmetro (tabela aparelho)
│   │   ├─ Resolver altura sub-ramal (tabela aparelho)
│   │   ├─ Pipe.Create(systemType, pipeType, level, fixPt, ramalPt)
│   │   ├─ Aplicar diâmetro via RBS_PIPE_DIAMETER_PARAM
│   │   ├─ TryConnectPipeToFixture() via ConnectorManager
│   │   └─ Se falhou → retry (maxRetries vezes)
│   │       └─ Se falhou de novo → ManualReviewItem
│   │
│   └─ ConectarTrechosAdjacentes() — pipe↔pipe (tolerância 5mm)
│
└─ FASE 3: ValidarPosRoteamento(doc, resultado)
    ├─ Verificar conectividade (conectores não conectados)
    ├─ Verificar diâmetros (15mm ≤ Ø ≤ 200mm)
    ├─ Verificar comprimento mínimo (≥ 3mm)
    └─ SalvarResultadoJson()
```

### Seleção de equipamentos

```csharp
// Critério: OST_PlumbingFixtures + Domain.DomainPiping + PipeSystemType cold/hot/undefined
var collector = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
    .WhereElementIsNotElementType();
```

### Criação de pipe por fixture

```csharp
var pipe = Pipe.Create(doc, systemType.Id, pipeType.Id, level.Id, fixPt, ramalPt);
pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM).Set(diamFt);
```

### Conexão automática

```csharp
// Fixture → Pipe: menor distância, tolerância 5mm, ConnectTo()
// Pipe → Pipe: O(n²) comparação, menor distância, ConnectTo()
```

---

## 4. VALIDAÇÃO PÓS-ROTEAMENTO

### Checagens automáticas

| # | Verificação | Critério | Gravidade |
|---|---|---|---|
| 1 | Pipe existe no modelo | `doc.GetElement(id) ≠ null` | 🔴 ALTA |
| 2 | Conectores conectados | `Connector.IsConnected == true` | 🟡 MÉDIA |
| 3 | Diâmetro válido | `15mm ≤ Ø ≤ 200mm` | 🔴 ALTA |
| 4 | Comprimento mínimo | `Length ≥ 3mm` | 🟡 MÉDIA |

### Classificação de status

| Status | Condição |
|---|---|
| `OK` | 0 falhas, pipes criados > 0 |
| `PARCIAL` | Fixtures conectados > 0 E fixtures falhados > 0 |
| `FALHA_TOTAL` | 0 pipes criados |
| `FALHA_PRE_VALIDACAO` | Pré-condições não atendidas |
| `FALHA_TIPO_NAO_ENCONTRADO` | PipeType ou SystemType ausente |
| `VALIDACAO_COM_CRITICOS` | Pós-validação encontrou trechos com gravidade ALTA |
| `ERRO_FATAL` | Exception não tratada |

---

## 5. LOG E CALLBACK

### Persistência JSON

Resultado salvo automaticamente em:

```
%APPDATA%/HermesMEP/Routing/AF_routing_YYYYMMDD_HHmmss.json
```

### Estrutura do JSON de saída

```json
{
  "success": true,
  "status": "OK",
  "fixturesProcessed": 24,
  "fixturesConnected": 22,
  "fixturesFailed": 2,
  "pipesCreated": [12345, 12346, ...],
  "fittingsCreated": [],
  "trechosGerados": 22,
  "conexoesRealizadas": 18,
  "trechosCriticos": [
    { "pipeId": 12350, "tipo": "DESCONECTADO", "motivo": "...", "gravidade": "MEDIA" }
  ],
  "manualReviewRequired": [
    { "fixtureId": 9999, "fixtureName": "Chuveiro: CH-01", "reason": "...", "retryCount": 2 }
  ],
  "errors": [],
  "executionTimeMs": 3450,
  "timestamp": "2026-03-31T12:05:00Z"
}
```

### Callbacks para UI

| Evento | Tipo | Dados |
|---|---|---|
| `OnProgress` | `Action<string>` | Mensagem textual de progresso |
| `OnFixtureProgress` | `Action<string, int, int>` | `(familyName, current, total)` |

### Exemplo de uso com callback

```csharp
var service = new ColdWaterRoutingService();

service.OnProgress += msg => TaskDialog.Show("AF", msg);
service.OnFixtureProgress += (name, cur, tot) =>
    statusBar.Text = $"[{cur}/{tot}] {name}";

var resultado = service.ExecutarPipelineCompleto(doc);
```

---

## 6. CRITÉRIOS DE FALHA E FALLBACK

### Política de retry

```
Para cada fixture com falha:
  ├─ Tentativa 1: RotearFixture() original
  ├─ Tentativa 2: retry automático (mesmos parâmetros)
  ├─ Tentativa 3: retry automático (mesmos parâmetros)
  └─ Após 3 falhas → ManualReviewItem
      └─ Roteamento dos demais fixtures CONTINUA
```

### Isolamento de falhas

- Falha em 1 fixture **não** aborta o pipeline
- Falha é registrada em `RoutingResult.Errors` e `ManualReview`
- Status final = `PARCIAL` (permite revisão seletiva)

### Cenários de fallback

| Cenário | Ação automática | Ação manual necessária |
|---|---|---|
| Fixture sem LocationPoint | Ignorado + warning | Reposicionar fixture |
| Fixture sem conector piping | Ignorado + warning | Verificar família |
| Pipe.Create falha (geometria) | Retry com offset Z | Rever posição |
| ConnectTo falha (incompatível) | Registra erro | Conectar manualmente |
| SystemType não encontrado | Aborta pipeline | Carregar template AF |

---

## 7. IMPACTO ESPERADO

| Métrica | Manual | Com serviço | Δ |
|---|---|---|---|
| Tempo de roteamento (24 fixtures) | 2h | **12min** | **-90%** |
| Erros de conexão | ~15% | **< 3%** | **-80%** |
| Fixtures sem ramal | ~8% | **< 2%** | **-75%** |
| Rastreabilidade | Nenhuma | **100% JSON** | ∞ |
| Repetibilidade | Variável | **Determinística** | ✅ |
| Callback para UI | N/A | **Tempo real** | ✅ |

---

## 8. INTEGRAÇÃO COM PIPELINE

### Posição no HydraulicOrchestrator

```
E01-ValidarRooms       → Dynamo (01_ValidarRooms.dyn)
E02-CriarSpaces        → Dynamo (02_CriarSpacesMassivo.dyn)
E03-InserirEquipamentos → Dynamo (03_InserirEquipamentos.dyn)
E04-ValidarPosicao     → Dynamo (04_ValidarPosicionamento.dyn)
▶ E05-RotearAguaFria   → ColdWaterRoutingService.ExecutarPipelineCompleto()
E06-RotearEsgoto       → (próxima implementação)
E07-AplicarSlope       → Dynamo (07_AplicarInclinacao.dyn)
E08-CriarPrumadas      → Dynamo (08_CriarPrumadas.dyn)
E09-ConectarRede       → Dynamo (09_ConectarRede.dyn)
E10-Schedules          → Dynamo (10_GerarTabelas.dyn)
E11-Pranchas           → Dynamo (11_GerarPranchas.dyn)
E12-Export             → HydraulicOrchestrator
```

### Chamada no Orchestrator

```csharp
var etapaAF = ExecutarEtapaInternal(doc, "E05-RotearAguaFria",
    "Roteamento Água Fria",
    () =>
    {
        var service = new ColdWaterRoutingService(_logger);
        return service.ExecutarPipelineCompleto(doc, coldWaterConfig);
    });
```

---

*Documento de referência técnica | ColdWaterRoutingService | v1.0 | 2026-03-31*
