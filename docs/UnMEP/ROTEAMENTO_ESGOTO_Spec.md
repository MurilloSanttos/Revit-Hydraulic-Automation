# Roteamento Automático de Esgoto Sanitário — Especificação Técnica

> Módulo: `Revit2026.Modules.UnMEP.SewerRoutingService`
> Pipeline Hidráulico Revit 2026 | v1.0 | NBR 8160

---

## 1. INICIALIZAÇÃO DO ROTEAMENTO

### Pré-condições verificadas automaticamente

| # | Validação | Critério | Ação se Falhar |
|---|---|---|---|
| 1 | Rooms presentes | `Count > 0` | ❌ Aborta |
| 2 | Rooms com nome | `Name ≠ null && Name ≠ "Room"` | ⚠️ Warning |
| 3 | Spaces MEP | `Count > 0` | ⚠️ Warning |
| 4 | Fixtures com drain | `HasDrainConnector == true` | ❌ Aborta se nenhum |
| 5 | Fixtures posicionados | `LocationPoint ≠ null` | ⚠️ Warning |
| 6 | Levels presentes | `Count > 0` | ❌ Aborta |
| 7 | PipeType carregado | `PipeType existe` | ❌ Aborta |
| 8 | SystemType Sanitary | `Sanitary ou OtherPipe` | ❌ Aborta |

---

## 2. CONFIGURAÇÃO DE PARÂMETROS

### `SewerRoutingConfig` — parâmetros gerais

| Parâmetro | Tipo | Default | Descrição |
|---|---|---|---|
| `sistema` | string | `"Esgoto Sanitário"` | Identificador |
| `diametroPadraoMm` | double | `50` | Fallback |
| `alturaRamalM` | double | `0.10` | Altura base sub-ramal |
| `offsetLajeFundoM` | double | `0.15` | Z do subcoletor abaixo da laje |
| `offsetParedeM` | double | `0.05` | Offset mínimo da parede |
| `offsetMinimoEntreEixosM` | double | `0.30` | Distância mínima entre eixos |
| `connectToExisting` | bool | `true` | Conectar a pipes existentes |
| `toleranceMm` | double | `5` | Tolerância de conexão |
| `maxRetries` | int | `2` | Tentativas de reconexão |
| `prefixoNomenclatura` | string | `"ES"` | Prefixo de logs e nomes |
| `criarSubcoletores` | bool | `true` | Criar subcoletor por nível |
| `comprimentoMaximoRamalM` | double | `5.0` | Limite de ramal horizontal |

### NBR 8160 — Diâmetros mínimos (mm) e UHC por aparelho

| Aparelho | Ø min (mm) | UHC | Saída Z (m) |
|---|---|---|---|
| Lavatório | 40 | 1 | -0.05 |
| Vaso Sanitário | 100 | 6 | -0.10 |
| Chuveiro | 40 | 2 | -0.05 |
| Pia Cozinha | 50 | 3 | -0.10 |
| Tanque | 50 | 3 | -0.10 |
| Máquina de Lavar | 50 | 3 | -0.05 |
| Bidê | 40 | 1 | -0.05 |
| Banheira | 50 | 3 | -0.05 |
| Ralo Sifonado | 50 | 2 | -0.05 |
| Ralo Seco | 40 | 1 | -0.05 |
| Mictório | 50 | 2 | -0.10 |

### NBR 8160 — Declividade mínima por diâmetro

| Faixa de diâmetro | Slope mínimo |
|---|---|
| Ø ≤ 75mm | **2%** |
| Ø ≥ 100mm | **1%** |

### NBR 8160 — Diâmetro do subcoletor por UHC acumulado

| UHC acumulado | Ø subcoletor (mm) |
|---|---|
| ≤ 3 | 50 |
| ≤ 6 | 75 |
| ≤ 20 | 100 |
| ≤ 160 | 150 |
| > 160 | 200 |

---

## 3. DISPARO DO ROTEAMENTO AUTOMÁTICO

### Fluxo de execução

```
ExecutarPipelineCompleto(doc, config)
│
├─ FASE 1: ValidarPreCondicoes(doc)
│   ├─ Rooms ✓
│   ├─ Spaces ✓
│   ├─ Fixtures com DrainConnector ✓
│   ├─ Levels ✓
│   ├─ PipeType ✓
│   └─ SystemType Sanitary/OtherPipe ✓
│
├─ FASE 2: ExecutarRoteamento(doc, config)
│   ├─ CollectSewerFixtures()
│   │   └─ OST_PlumbingFixtures + Domain.DomainPiping
│   │       + PipeSystemType (Sanitary/OtherPipe/Undefined)
│   │
│   ├─ ResolvePipeType() + ResolveSanitarySystemType()
│   │
│   ├─ Agrupar por Level (ordenado por elevation)
│   │
│   ├─ Para cada fixture:
│   │   ├─ Resolver Ø por aparelho (NBR 8160)
│   │   ├─ Resolver UHC por aparelho
│   │   ├─ Calcular slope (2% ou 1% conforme Ø)
│   │   ├─ Calcular drop (slope × comprimento)
│   │   ├─ Pipe.Create(fixPt → ramalPt inclinado)
│   │   ├─ Aplicar Ø via RBS_PIPE_DIAMETER_PARAM
│   │   ├─ Aplicar slope via RBS_PIPE_SLOPE
│   │   ├─ TryConnectPipeToFixture() via ConnectorManager
│   │   └─ Se falhou → retry (maxRetries vezes)
│   │
│   ├─ CriarSubcoletor() por nível:
│   │   ├─ Somar UHC de todos fixtures do nível
│   │   ├─ Resolver Ø do subcoletor via GetDiametroPorUhcAcumulado()
│   │   ├─ Calcular bounding box (minX → maxX, avgY)
│   │   ├─ Criar pipe horizontal com slope aplicado
│   │   └─ Registrar SlopeDetail
│   │
│   └─ ConectarTrechosAdjacentes() (tolerância 5mm)
│
└─ FASE 3: ValidarPosRoteamento(doc, resultado)
    ├─ Verificar conectividade
    ├─ Verificar diâmetros (40mm ≤ Ø ≤ 200mm)
    ├─ Verificar comprimento mínimo (≥ 3mm)
    ├─ Verificar slope mínimo (NBR 8160, 20% tolerância)
    └─ SalvarResultadoJson()
```

### Diferenças-chave vs. Água Fria

| Aspecto | Água Fria | Esgoto |
|---|---|---|
| Diâmetro mínimo | 20mm | 40mm |
| Diâmetro máximo | 200mm | 200mm |
| Slope | 0% | 1–2% (NBR 8160) |
| UHC | N/A | Calculado por aparelho |
| Subcoletor | N/A | Criado automaticamente |
| SystemType | DomesticColdWater | Sanitary/OtherPipe |
| Conector | ColdWater | Drain/Sanitary |
| Saída Z | Acima do piso | Abaixo do piso |

---

## 4. VALIDAÇÃO PÓS-ROTEAMENTO

### Checagens automáticas

| # | Verificação | Critério | Gravidade |
|---|---|---|---|
| 1 | Pipe existe | `doc.GetElement(id) ≠ null` | 🔴 ALTA |
| 2 | Conectividade | `Connector.IsConnected == true` | 🟡 MÉDIA |
| 3 | Diâmetro | `40mm ≤ Ø ≤ 200mm` | 🔴 ALTA |
| 4 | Comprimento mín. | `Length ≥ 3mm` | 🟡 MÉDIA |
| 5 | **Slope mínimo** | `slope ≥ 80% do mínimo NBR` | 🔴 ALTA |

### Status possíveis

| Status | Condição |
|---|---|
| `OK` | 0 falhas, pipes > 0 |
| `PARCIAL` | Conectados > 0 E falhados > 0 |
| `FALHA_TOTAL` | 0 pipes |
| `FALHA_PRE_VALIDACAO` | Pré-condições não atendidas |
| `FALHA_TIPO_NAO_ENCONTRADO` | PipeType ou SystemType ausente |
| `VALIDACAO_COM_CRITICOS` | Trechos com gravidade ALTA |
| `ERRO_FATAL` | Exception global |

---

## 5. LOG E CALLBACK

### Persistência JSON

```
%APPDATA%/HermesMEP/Routing/ES_routing_YYYYMMDD_HHmmss.json
```

### Estrutura do JSON de saída

```json
{
  "success": true,
  "status": "OK",
  "fixturesProcessed": 18,
  "fixturesConnected": 17,
  "fixturesFailed": 1,
  "uhcTotal": 42,
  "pipesCreated": [23456, 23457, ...],
  "trechosGerados": 20,
  "conexoesRealizadas": 15,
  "subcoletoresCriados": 3,
  "slopeApplied": [
    { "pipeId": 23456, "diameterMm": 100, "slopePercent": 1.0, "lengthM": 2.5, "dropM": 0.025 },
    { "pipeId": 23457, "diameterMm": 50, "slopePercent": 2.0, "lengthM": 1.2, "dropM": 0.024 }
  ],
  "trechosCriticos": [
    { "pipeId": 23460, "tipo": "SLOPE_INSUFICIENTE", "motivo": "Slope 0.5% < mínimo 1%", "gravidade": "ALTA" }
  ],
  "manualReviewRequired": [
    { "fixtureId": 8888, "fixtureName": "Ralo: RS-01", "reason": "Falha após tentativas", "retryCount": 2 }
  ],
  "errors": [],
  "executionTimeMs": 4200,
  "timestamp": "2026-03-31T12:10:00Z"
}
```

### Callbacks

```csharp
var service = new SewerRoutingService();

service.OnProgress += msg => TaskDialog.Show("ES", msg);
service.OnFixtureProgress += (name, cur, tot) =>
    statusBar.Text = $"Esgoto [{cur}/{tot}] {name}";

var resultado = service.ExecutarPipelineCompleto(doc);
```

---

## 6. CRITÉRIOS DE FALHA E FALLBACK

### Política de retry

```
Para cada fixture com falha:
  ├─ Tentativa 1: RotearFixtureEsgoto() original
  ├─ Tentativa 2: retry (mesmos parâmetros)
  ├─ Tentativa 3: retry (mesmos parâmetros)
  └─ Após 3 falhas → ManualReviewItem
      └─ Roteamento dos demais fixtures CONTINUA
```

### Isolamento

- Falha em 1 fixture **não** interrompe pipeline
- Subcoletor é criado mesmo com fixtures falhados
- Status final = `PARCIAL` (revisão seletiva)

### Cenários de fallback

| Cenário | Ação automática | Ação manual |
|---|---|---|
| Fixture sem DrainConnector | Ignorado + warning | Verificar família |
| Pipe.Create falha (geometria) | Retry com offset | Rever posição |
| Slope < mínimo NBR | Registra SLOPE_INSUFICIENTE | Ajustar manualmente ou via 07_AplicarInclinacao.dyn |
| UHC > 160 → Ø 200mm | Aplicado automaticamente | Validar viabilidade |
| ConnectTo falha | Registra erro | Conectar manualmente |
| Subcoletor cruza com outro | Não detecta | Clash detection (Navisworks) |

---

## 7. IMPACTO ESPERADO

| Métrica | Manual | Com serviço | Δ |
|---|---|---|---|
| Tempo de roteamento (18 fixtures ES) | 3h | **15min** | **-92%** |
| Erros de slope | ~20% | **< 3%** | **-85%** |
| Erros de diâmetro | ~12% | **< 1%** | **-92%** |
| Fixtures sem ramal | ~10% | **< 2%** | **-80%** |
| Subcoletores criados | Manual (30min/nível) | **Automático** | **-95%** |
| Rastreabilidade | Nenhuma | **100% JSON + SlopeDetails** | ∞ |

---

## 8. INTEGRAÇÃO COM PIPELINE

### Posição no HydraulicOrchestrator

```
E01-ValidarRooms        → Dynamo
E02-CriarSpaces         → Dynamo
E03-InserirEquipamentos → Dynamo
E04-ValidarPosicao      → Dynamo
E05-RotearAguaFria      → ColdWaterRoutingService
▶ E06-RotearEsgoto      → SewerRoutingService.ExecutarPipelineCompleto()
E07-AplicarSlope        → Dynamo (ajuste fino pós-unMEP)
E08-CriarPrumadas       → Dynamo
E09-ConectarRede        → Dynamo
E10-Schedules           → Dynamo
E11-Pranchas            → Dynamo
E12-Export              → HydraulicOrchestrator
```

### Chamada no Orchestrator

```csharp
var etapaES = ExecutarEtapaInternal(doc, "E06-RotearEsgoto",
    "Roteamento Esgoto Sanitário",
    () =>
    {
        var service = new SewerRoutingService(_logger);
        service.OnProgress += msg => EtapaIniciada?.Invoke(msg);
        return service.ExecutarPipelineCompleto(doc, sewerConfig);
    });
```

---

*Documento de referência técnica | SewerRoutingService | NBR 8160 | v1.0 | 2026-03-31*
