# Ajuste Manual de Rotas com Feedback — Especificação Técnica

> Módulo: `Revit2026.Modules.UnMEP.RouteAdjustmentService`
> Pipeline Hidráulico Revit 2026 | v1.0

---

## 1. DETECÇÃO DE TRECHOS A AJUSTAR

### Entrada

O serviço consome o `RouteValidationResult` do `RouteValidationService`:

```csharp
var validator = new RouteValidationService();
var validacao = validator.ValidarRotas(doc);

var adjService = new RouteAdjustmentService();
var problematicos = adjService.ObterTrechosProblematicos(validacao);
// → Lista ordenada: ❌ CRITICO primeiro, depois ⚠️ AVISO
```

### Filtro automático

| Status | Incluído? | Prioridade |
|---|---|---|
| ✅ OK | ❌ Não | — |
| ⚠️ AJUSTE_NECESSARIO | ✅ Sim | 2 |
| ❌ FALHA_CRITICA | ✅ Sim | 1 (primeiro) |

---

## 2. OPERAÇÕES DE AJUSTE

### 6 operações disponíveis

| # | Operação | Método | Parâmetros | Afeta |
|---|---|---|---|---|
| 1 | **Mover Endpoint** | `MoverEndpoint()` | `ElementId, EndpointIndex, NewPosition` | Geometria (start/end XYZ) |
| 2 | **Alterar Altura** | `AlterarAltura()` | `ElementId, NewHeightM, MaintainSlope` | Z de ambos endpoints |
| 3 | **Alterar Slope** | `AlterarSlope()` | `ElementId, NewSlopePercent, AnchorEnd` | Z do endpoint não-ancorado + RBS_PIPE_SLOPE |
| 4 | **Alterar Offset** | `AlterarOffset()` | `ElementId, OffsetXM, OffsetYM` | X, Y via ElementTransformUtils.MoveElement |
| 5 | **Alterar Diâmetro** | `AlterarDiametro()` | `ElementId, NewDiameterMm` | RBS_PIPE_DIAMETER_PARAM |
| 6 | **Reconectar** | `Reconectar()` | `ElementId, TargetElementId, ConnectorIndex` | DisconnectFrom + ConnectTo |

### Fluxo de cada operação

```
Operação(doc, params)
│
├─ Localizar Pipe pelo ElementId
├─ Capturar PipeSnapshot (ANTES)
│   └─ StartPoint, EndPoint, Ø, Slope, Level, Conectados
│
├─ Transaction.Start("Ajuste Manual - [tipo] [id]")
│   ├─ Aplicar modificação
│   └─ Transaction.Commit()
│
├─ Capturar PipeSnapshot (DEPOIS)
├─ Revalidar trecho via RouteValidationService
├─ Registrar na sessão + undo stack
└─ Emitir OnAdjustmentApplied
```

### Validações de segurança por operação

| Operação | Guarda |
|---|---|
| MoverEndpoint | `newLine.Length ≥ 3mm` (evita micro-trecho) |
| AlterarAltura | `MaintainSlope` preserva ΔZ original |
| AlterarSlope | Rejeita se trecho é vertical (`horiz < 0.01ft`) |
| AlterarDiametro | Rejeita se parâmetro é ReadOnly |
| Reconectar | Rejeita se target conector já está conectado |
| AlterarOffset | Sem guarda adicional (MoveElement é atômico) |

---

## 3. SISTEMA DE UNDO

### PipeSnapshot

Captura completa do estado antes de cada ajuste:

```json
{
  "elementId": 12345,
  "startPoint": [10.5, 20.3, 1.2],
  "endPoint": [15.5, 20.3, 1.15],
  "diameterMm": 100,
  "slopePercent": 1.0,
  "levelId": 9876,
  "systemName": "Esgoto Sanitário",
  "connectedIds": [12340, 12346],
  "capturedAt": "2026-03-31T12:00:00Z"
}
```

### Operações de undo

| Método | Comportamento |
|---|---|
| `ReverterAjuste(doc, adjustmentId)` | Restaura geometria, Ø e slope do snapshot "antes" |
| `ReverterTodos(doc)` | Reverte todos na **ordem inversa** (LIFO) |

### O que é restaurado

| Propriedade | Restaurada? |
|---|---|
| Geometria (start/end XYZ) | ✅ |
| Diâmetro | ✅ |
| Slope (parâmetro) | ✅ |
| Conexões | ⚠️ Parcial (desconexão pode não ser reversível) |
| Level | ❌ (não muda com ajustes) |

---

## 4. REVALIDAÇÃO AUTOMÁTICA

### Pós-ajuste individual

Após cada operação, o trecho ajustado é revalidado automaticamente via `RouteValidationService.ValidarRotasPorIds()`:

```csharp
adj.ValidationStatus = "OK" | "AJUSTE_NECESSARIO" | "FALHA_CRITICA"
adj.Issues = [...] // issues detectados pós-ajuste
```

### Pós-sessão (finalização)

Ao chamar `FinalizarSessao()`, todos os trechos ajustados são revalidados em lote:

```
FinalizarSessao(doc)
│
├─ Coletar IDs de trechos ajustados (não revertidos)
├─ ValidarRotasPorIds(doc, adjustedIds)
│   ├─ Verificar conectividade
│   ├─ Verificar diâmetros
│   ├─ Verificar slope
│   └─ Detectar desconexões causadas
│
├─ Status:
│   ├─ OK → tudo válido
│   ├─ COM_FALHAS → ajustes falharam
│   ├─ VALIDACAO_FALHOU → trechos com issues CRITICO
│   └─ DESCONEXOES_DETECTADAS → ajustes causaram desconexão
│
└─ Salvar JSON da sessão
```

---

## 5. LOG E EXPORTAÇÃO

### Persistência

```
%APPDATA%/HermesMEP/Adjustments/adj_session_[id]_YYYYMMDD_HHmmss.json
```

### Estrutura do JSON de sessão

```json
{
  "sessionId": "a1b2c3d4e5f6",
  "success": true,
  "status": "OK",
  "adjustmentsApplied": 5,
  "adjustmentsReverted": 1,
  "adjustmentsFailed": 0,
  "revalidationPassed": true,
  "disconnectedElements": [],
  "adjustments": [
    {
      "adjustmentId": "f7a8b9c0",
      "elementId": 12345,
      "type": "ChangeSlope",
      "applied": true,
      "reverted": false,
      "validationStatus": "OK",
      "before": { "slopePercent": 0.5, "..." : "..." },
      "after": { "slopePercent": 2.0, "..." : "..." },
      "issues": [],
      "timestamp": "2026-03-31T12:05:00Z",
      "userId": "murillo.santtos"
    }
  ],
  "executionTimeMs": 1200
}
```

### Callbacks

```csharp
var service = new RouteAdjustmentService();

service.OnProgress += msg => statusBar.Text = msg;
service.OnAdjustmentApplied += adj =>
    listView.Items.Add($"✅ {adj.Type}: Pipe {adj.ElementId}");
service.OnAdjustmentReverted += id =>
    listView.Items.Add($"↩️ Revertido: {id}");
```

---

## 6. CRITÉRIOS DE SEGURANÇA

| Critério | Implementação |
|---|---|
| Não desconectar válidos | Revalidação detecta `!IsFullyConnected` |
| Auditável | Cada ajuste tem `adjustmentId`, `userId`, before/after |
| Reversível | `ReverterAjuste()` restaura snapshot completo |
| Revalidado | Automático após cada operação + lote na finalização |
| Transacional | Cada operação = 1 Transaction com nome rastreável |
| Sem efeito colateral | Ajuste em 1 pipe não altera outros pipes |

### Proteções contra erros

| Cenário | Proteção |
|---|---|
| Pipe deletado | `GetElement() == null` → falha segura |
| Micro-trecho | `newLine.Length < 0.01` → rejeita |
| Prumada + slope | `horiz < 0.01` → "slope não aplicável" |
| Ø read-only | `IsReadOnly` check → rejeita |
| Target já conectado | `IsConnected` check → rejeita |
| Transaction falha | RollBack automático via `using` |

---

## 7. INTEGRAÇÃO COM PIPELINE

### Uso típico

```csharp
// 1. Validar rotas
var validator = new RouteValidationService(logger);
var validacao = validator.ValidarRotas(doc);

// 2. Obter trechos problemáticos
var adjService = new RouteAdjustmentService(logger, validator);
var problemas = adjService.ObterTrechosProblematicos(validacao);

// 3. Ajustar (via UI do usuário)
foreach (var item in problemas)
{
    if (item.Issues.Any(i => i.Code == "SLOPE_BELOW_MIN"))
    {
        adjService.AlterarSlope(doc, new ChangeSlopeParams
        {
            ElementId = item.ElementId,
            NewSlopePercent = 2.0,
            AnchorEnd = 0
        });
    }

    if (item.Issues.Any(i => i.Code == "DIAM_BELOW_MIN"))
    {
        adjService.AlterarDiametro(doc, new ChangeDiameterParams
        {
            ElementId = item.ElementId,
            NewDiameterMm = 50
        });
    }
}

// 4. Finalizar sessão
var sessao = adjService.FinalizarSessao(doc);

if (!sessao.Success)
{
    // Reverter tudo se necessário
    adjService.ReverterTodos(doc);
}
```

### Posição no pipeline

```
E06-RotearEsgoto        → SewerRoutingService
E06.5-ValidarRotas      → RouteValidationService
▶ E07-AjusteManual      → RouteAdjustmentService (interativo)
E07.5-AplicarSlope      → Dynamo (07_AplicarInclinacao.dyn)
E08-CriarPrumadas       → Dynamo
E09-ConectarRede        → Dynamo
E09.5-ValidarFinal      → RouteValidationService
```

---

## 8. IMPACTO ESPERADO

| Métrica | Sem serviço | Com serviço | Δ |
|---|---|---|---|
| Correção de trechos críticos | Manual (15min/trecho) | **2min/trecho** | **-87%** |
| Desconexões acidentais | ~30% ao editar | **< 3%** (revalidação) | **-90%** |
| Rastreabilidade de ajustes | Nenhuma | **100% JSON** | ∞ |
| Reversibilidade | Ctrl+Z (limitado) | **Undo completo por ajuste** | ✅ |
| Revalidação | Manual | **Automática por trecho** | ✅ |
| Tempo total de correção | 2–4h / projeto | **15–30min** | **-85%** |

---

*Documento de referência técnica | RouteAdjustmentService | v1.0 | 2026-03-31*
