# Validação de Rotas MEP — Especificação Técnica

> Módulo: `Revit2026.Modules.UnMEP.RouteValidationService`
> Pipeline Hidráulico Revit 2026 | v1.0 | NBR 5626 + NBR 8160

---

## 1. COLETA DE DADOS

### Elementos coletados automaticamente

| Tipo | Collector | Dados extraídos |
|---|---|---|
| **Pipes** | `FilteredElementCollector.OfClass(typeof(Pipe))` | Ø, comprimento, slope, Z, Level, sistema, conectores |
| **Fittings** | `OfCategory(OST_PipeFitting)` | Conectores, Ø (via radius), Level, posição |

### Parâmetros extraídos por pipe

| Parâmetro | BuiltInParameter | Unidade |
|---|---|---|
| Diâmetro | `RBS_PIPE_DIAMETER_PARAM` | ft → mm |
| Comprimento | `CURVE_ELEM_LENGTH` | ft → m |
| Slope | `RBS_PIPE_SLOPE` | fração → % |
| Level | `RBS_START_LEVEL_PARAM` | ElementId |
| Sistema | `RBS_PIPING_SYSTEM_TYPE_PARAM` | SystemClassification |
| Geometria | `LocationCurve` | Line endpoints (Z) |

### Parâmetros extraídos por fitting

| Parâmetro | Fonte | Unidade |
|---|---|---|
| Diâmetro | `Connector.Radius * 2` | ft → mm |
| Conectores | `MEPModel.ConnectorManager` | contagem |
| Posição | `LocationPoint.Z` | ft → m |
| Level | `FAMILY_LEVEL_PARAM` | ElementId |

---

## 2. REGRAS DE VALIDAÇÃO

### V1 — Conectividade

| Critério | Lógica | Severidade |
|---|---|---|
| Todos conectores conectados | `ConnectorsConnected == ConnectorsTotal` | ✅ OK |
| Parcialmente desconectado | `0 < Connected < Total` | ⚠️ AVISO |
| Totalmente desconectado | `Connected == 0` | ❌ CRITICO |
| Fitting sem conectores | `ConnectorsTotal == 0` | ❌ CRITICO |

### V2 — Diâmetro (NBR 5626 + NBR 8160)

| Sistema | Ø mínimo | Ø máximo | Referência |
|---|---|---|---|
| Água Fria | 15mm | 200mm | NBR 5626:2020 |
| Esgoto | 40mm | 200mm | NBR 8160:1999 |

| Critério | Severidade |
|---|---|
| `Ø < mínimo` | ❌ CRITICO |
| `Ø > máximo` | ⚠️ AVISO |
| `Ø == 0` | ❌ CRITICO |

### V3 — Slope (somente esgoto sanitário)

| Faixa | Slope mínimo | Tolerância | Slope máximo |
|---|---|---|---|
| Ø ≤ 75mm | 2.0% | -20% (aceita ≥ 1.6%) | 5.0% |
| Ø ≥ 100mm | 1.0% | -20% (aceita ≥ 0.8%) | 5.0% |

| Critério | Severidade |
|---|---|
| `slope < 50% do mínimo` | ❌ CRITICO |
| `slope < mínimo (com tolerância)` | ⚠️ AVISO |
| `slope > 5%` | ⚠️ AVISO |
| Prumada (vertical) | 🔇 Skip |

**Detecção de prumada:** se `vertical/horizontal > 2.0`, o pipe é considerado prumada e a validação de slope é ignorada.

### V4 — Comprimento

| Critério | Default | Severidade |
|---|---|---|
| `< 3mm` | Micro-trecho | ⚠️ AVISO |
| `> 15m` | Requer caixa de inspeção | ⚠️ AVISO |

### V5 — Altura (Z relativo)

| Critério | Default | Severidade |
|---|---|---|
| `Z < -0.50m` | Abaixo do aceitável | ⚠️ AVISO |
| `Z > 3.50m` | Acima do aceitável | ⚠️ AVISO |

---

## 3. GERAÇÃO DE ALERTAS

### Classificação por status

| Status | Ícone | Significado |
|---|---|---|
| `OK` | ✅ | Nenhum problema detectado |
| `AJUSTE_NECESSARIO` | ⚠️ | Issues de severidade AVISO |
| `FALHA_CRITICA` | ❌ | Issues de severidade CRITICO |

### Códigos de issue

| Código | Tipo | Descrição |
|---|---|---|
| `CONN_INCOMPLETE` | Conectividade | Conectores não conectados |
| `DIAM_BELOW_MIN` | Diâmetro | Ø abaixo do mínimo normativo |
| `DIAM_ABOVE_MAX` | Diâmetro | Ø acima do máximo |
| `DIAM_ZERO` | Diâmetro | Sem diâmetro atribuído |
| `SLOPE_BELOW_MIN` | Inclinação | Slope insuficiente (NBR 8160) |
| `SLOPE_ABOVE_MAX` | Inclinação | Slope excessivo (> 5%) |
| `LEN_BELOW_MIN` | Comprimento | Micro-trecho (< 3mm) |
| `LEN_ABOVE_MAX` | Comprimento | Trecho > 15m sem CX |
| `HEIGHT_BELOW_MIN` | Altura | Z muito baixo |
| `HEIGHT_ABOVE_MAX` | Altura | Z muito alto |
| `FIT_CONN_INCOMPLETE` | Fitting | Fitting desconectado |
| `FIT_NO_CONNECTORS` | Fitting | Família sem conectores |
| `ELEM_NOT_FOUND` | Integridade | Elemento deletado |

### Cada issue contém

```json
{
  "code": "SLOPE_BELOW_MIN",
  "severity": "CRITICO",
  "message": "Slope 0.5% < mínimo NBR 8160: 1% (Ø100mm)",
  "suggestedAction": "Executar 07_AplicarInclinacao.dyn ou ajustar manualmente",
  "currentValue": "0.50%",
  "expectedValue": "≥ 1%"
}
```

---

## 4. RESUMO DE VALIDAÇÃO

### Métricas globais

```json
{
  "totalPipes": 85,
  "totalFittings": 42,
  "totalElements": 127,
  "okCount": 110,
  "warningCount": 12,
  "criticalCount": 5,
  "connectivityRate": 92.3,
  "diameterConformityRate": 98.8,
  "slopeConformityRate": 88.5,
  "overallConformityRate": 86.6,
  "conflicts": 3
}
```

### Resumo por sistema

```json
{
  "summaryBySystem": {
    "Esgoto Sanitário": {
      "pipeCount": 45,
      "fittingCount": 22,
      "totalLengthM": 78.5,
      "okCount": 55,
      "warningCount": 8,
      "criticalCount": 4,
      "conformityRate": 82.1
    },
    "Água Fria": {
      "pipeCount": 40,
      "fittingCount": 20,
      "totalLengthM": 62.3,
      "okCount": 55,
      "warningCount": 4,
      "criticalCount": 1,
      "conformityRate": 91.7
    }
  }
}
```

### Detecção de conflitos

| Tipo | Descrição |
|---|---|
| `SOBREPOSICAO` | Pipes com distância < 1mm |
| `PROXIMIDADE_MESMO_SISTEMA` | Pipes do mesmo sistema < 25mm |
| `INTERFERENCIA_CRUZADA` | Pipes de sistemas diferentes < 25mm |

---

## 5. LOG E EXPORTAÇÃO

### Persistência

```
%APPDATA%/HermesMEP/Validation/route_validation_YYYYMMDD_HHmmss.json
```

### Callbacks

```csharp
var service = new RouteValidationService();

service.OnProgress += msg => TaskDialog.Show("Validação", msg);
service.OnItemProgress += (current, total) =>
    progressBar.Value = (int)(current * 100.0 / total);

// Validar TUDO
var resultado = service.ValidarRotas(doc);

// Validar apenas IDs pós-roteamento
var parcial = service.ValidarRotasPorIds(doc, pipeIds);
```

### Status finais do resultado

| Status | Condição |
|---|---|
| `OK` | 0 criticos, 0 warnings |
| `AJUSTES_NECESSARIOS` | 0 criticos, warnings > 0 |
| `CRITICO` | criticos > 0 |
| `VAZIO` | 0 pipes e 0 fittings |
| `ERRO_FATAL` | Exception global |

---

## 6. INTEGRAÇÃO COM PIPELINE

### Posição no HydraulicOrchestrator

```
E05-RotearAguaFria      → ColdWaterRoutingService
E06-RotearEsgoto        → SewerRoutingService
▶ E06.5-ValidarRotas    → RouteValidationService.ValidarRotas()
E07-AplicarSlope        → Dynamo (07_AplicarInclinacao.dyn)
E08-CriarPrumadas       → Dynamo (08_CriarPrumadas.dyn)
E09-ConectarRede        → Dynamo (09_ConectarRede.dyn)
▶ E09.5-ValidarFinal    → RouteValidationService.ValidarRotas()
E10-Schedules           → Dynamo
E11-Pranchas            → Dynamo
E12-Export              → HydraulicOrchestrator
```

### Chamada no Orchestrator

```csharp
// Validação pós-roteamento
var etapaVal1 = ExecutarEtapaInternal(doc, "E06.5-ValidarRotas",
    "Validar Rotas pós-Roteamento",
    () =>
    {
        var service = new RouteValidationService(_logger);
        return service.ValidarRotas(doc);
    });

// Validação final pré-documentação
var etapaValFinal = ExecutarEtapaInternal(doc, "E09.5-ValidarFinal",
    "Validação Final da Rede",
    () =>
    {
        var service = new RouteValidationService(_logger);
        var resultado = service.ValidarRotas(doc);

        if (resultado.CriticalCount > 0 && config.PararNaFalha)
            throw new InvalidOperationException(
                $"Validação falhou: {resultado.CriticalCount} trechos críticos");

        return resultado;
    });
```

### 2 pontos de validação no pipeline

| Ponto | Quando | Propósito |
|---|---|---|
| **E06.5** | Após roteamento AF + ES | Detectar falhas de rota antes de ajuste fino |
| **E09.5** | Após conectar rede | Garantir integridade antes de documentação |

---

*Documento de referência técnica | RouteValidationService | v1.0 | 2026-03-31*
