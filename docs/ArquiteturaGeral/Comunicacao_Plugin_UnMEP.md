# Modelo de Comunicação Plugin ↔ unMEP — Integração Indireta

> Estratégia realista de integração entre o plugin C# e o unMEP para criação automática de redes hidráulicas, considerando ausência de API oficial e desenvolvimento solo.

---

## 1. Estratégia Geral de Integração

### 1.1 Tipo de integração: Indireta Orquestrada

O unMEP **não possui API programática documentada**. A integração é feita de forma indireta:

```
Plugin prepara modelo → Plugin dispara unMEP → unMEP executa → Plugin valida resultado
```

O plugin atua como **orquestrador**: configura o modelo antes, dispara o unMEP e valida depois. Não controla o que acontece durante.

### 1.2 Papel de cada componente

| Componente | Papel | Analogia |
|-----------|-------|---------|
| **Plugin (C#)** | Orquestrador. Prepara, dispara, valida. | Diretor de obra |
| **unMEP** | Executor especializado de roteamento. Cria pipes e fittings. | Empreiteiro especializado |
| **Dynamo** | Executor de propósito geral. Inserção, batch, layout. | Equipe própria |

### 1.3 Quando usar unMEP vs. Dynamo

| Situação | Ferramenta | Justificativa |
|----------|-----------|---------------|
| Roteamento entre 2 pontos com obstáculos | **unMEP** | unMEP resolve pathfinding 3D automaticamente |
| Roteamento reto sem obstáculo | **Dynamo** | Mais controlável, script simples |
| Inserção de equipamentos | **Dynamo** | unMEP não é feito para inserção |
| Ajuste de Z em batch (declividade) | **Dynamo** | Operação simples de set parameter |
| Rede completa com desvios e ramificações | **unMEP** | Complexidade que justifica ferramenta especializada |
| Layout de pranchas | **Dynamo** | unMEP não faz isso |
| Criação de sistemas MEP | **Plugin direto** | API Revit simples, não precisa de ferramenta |
| Conexão de 2 fixtures a 1 ramal | **unMEP** | Pathfinding com merge de rotas |

### 1.4 Regra de decisão

```
O PLUGIN TENTA na seguinte ordem:

1. REVIT API DIRETA (se operação é simples)
   → Criar 1 pipe reto, 1 fitting
   
2. DYNAMO (se operação é em batch ou moderadamente complexa)
   → Inserir 10 fixtures, ajustar Z de 20 pipes

3. unMEP (se operação envolve roteamento com obstáculos)
   → Conectar fixture a prumada com desvio de pilar

4. MANUAL (se tudo falha)
   → Destacar no modelo, aguardar intervenção humana
```

---

## 2. Modelo de Comunicação

### 2.1 Mecanismo de comunicação

Como o unMEP não tem API, a comunicação é feita via **estado do modelo Revit**:

```
┌──────────┐                        ┌──────────┐
│  Plugin   │                        │  unMEP   │
│  (C#)     │                        │          │
│           │  1. Prepara modelo     │          │
│           │  (seleciona elementos) │          │
│           │──────────────────────→ │          │
│           │                        │          │
│           │  2. Dispara comando    │          │
│           │  (PostableCommand ou   │          │
│           │   simulação de click)  │          │
│           │──────────────────────→ │          │
│           │                        │ executa  │
│           │                        │ routing  │
│           │                        │          │
│           │  3. Valida resultado   │          │
│           │  (lê novos elementos)  │          │
│           │←──────────────────────│          │
└──────────┘                        └──────────┘
```

### 2.2 As 4 fases da comunicação

| Fase | Quem faz | O que faz |
|------|---------|-----------|
| **Preparação** | Plugin | Posiciona equipamentos, preenche parâmetros, seleciona connectors de origem/destino |
| **Disparo** | Plugin | Invoca comando do unMEP (via PostableCommand, ribbon automation ou UI Automation) |
| **Execução** | unMEP | Cria pipes, fittings, conecta elementos (plugin não interfere) |
| **Validação** | Plugin | Verifica elementos criados, conectividade, DNs, conformidade |

### 2.3 Snapshot antes/depois

```csharp
// Estratégia de detecção de resultado: comparar estado do modelo antes e depois

public class ModelSnapshot
{
    public HashSet<int> PipeIds { get; set; }
    public HashSet<int> FittingIds { get; set; }
    public HashSet<int> AccessoryIds { get; set; }
    public int TotalPipes { get; set; }
    public int TotalFittings { get; set; }
    
    public static ModelSnapshot Take(Document doc)
    {
        return new ModelSnapshot
        {
            PipeIds = new FilteredElementCollector(doc)
                .OfClass(typeof(Pipe))
                .Select(e => e.Id.IntegerValue)
                .ToHashSet(),
            FittingIds = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeFitting)
                .WhereElementIsNotElementType()
                .Select(e => e.Id.IntegerValue)
                .ToHashSet(),
            AccessoryIds = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_PipeAccessory)
                .WhereElementIsNotElementType()
                .Select(e => e.Id.IntegerValue)
                .ToHashSet()
        };
    }
    
    public ModelDelta CompareTo(ModelSnapshot after)
    {
        return new ModelDelta
        {
            NewPipeIds = after.PipeIds.Except(this.PipeIds).ToList(),
            NewFittingIds = after.FittingIds.Except(this.FittingIds).ToList(),
            NewAccessoryIds = after.AccessoryIds.Except(this.AccessoryIds).ToList(),
            RemovedPipeIds = this.PipeIds.Except(after.PipeIds).ToList()
        };
    }
}
```

---

## 3. Estratégia de Controle

### 3.1 Pré-condições checadas pelo plugin

Antes de chamar o unMEP, o plugin verifica:

```
□ Equipamentos de origem estão posicionados e têm connectors
□ Prumadas de destino existem e estão visíveis
□ PipingSystemType correto está criado (AF, ES ou VE)
□ PipeType (material) está carregado no modelo
□ Level ativo corresponde ao pavimento de trabalho
□ Nenhuma Transaction pendente
□ Vista ativa é planta (não 3D) — se unMEP exigir
□ Snapshot do modelo foi capturado (estado antes)
```

### 3.2 Como garantir execução correta

| Controle | Implementação |
|----------|--------------|
| **Seleção pré-configurada** | Plugin seleciona connectors de origem e destino via `uidoc.Selection.SetElementIds()` |
| **Parâmetros pré-preenchidos** | Plugin configura PipingSystemType, PipeType e preferências antes de chamar |
| **Vista correta** | Plugin ativa a vista/level correto antes de disparar |
| **Timeout** | Plugin monitora por N segundos; se nada muda, assume falha |
| **Snapshot** | Comparar modelo antes/depois para detectar o que mudou |

### 3.3 Como evitar execuções incorretas

```
ANTES de chamar unMEP:

1. Verificar que não há Transaction aberta
   → Se houver, commitar ou reverter antes

2. Verificar que os connectors de origem são válidos
   → connector.IsConnected == false (livre para conectar)

3. Verificar que o PipingSystem correto está ativo
   → Se não: configurar antes

4. Salvar estado do modelo (Ctrl+S programático)
   → Se unMEP criar algo errado, pode reverter via Undo

5. Registrar no log o que está prestes a ser feito
   → Rastreabilidade de ação
```

### 3.4 Repetição de execução

```
SE resultado do unMEP insatisfatório:

1. Undo (doc.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.Undo)))
2. Ajustar parâmetros (mudar rota, reposicionar equipamento)
3. Refazer snapshot
4. Re-disparar unMEP
5. Revalidar

LIMITE: máximo 2 tentativas por trecho
  Após 2 falhas → marcar como "roteamento manual"
```

---

## 4. Preparação do Modelo

### 4.1 Checklist de preparação por módulo

#### M07 — Rede de Água Fria

```
ANTES de chamar unMEP para rota AF:

□ Fixture com connector AF livre
□ Prumada AF existente no pavimento
□ PipingSystemType "AF - Água Fria" criado
□ PipeType PVC Soldável carregado
□ DN pré-definido pelo Core (ex: DN 25)
□ Registro de gaveta na entrada do ambiente (já inserido)
□ Vista de planta ativa no Level correto
```

#### M08 — Rede de Esgoto

```
ANTES de chamar unMEP para rota ES:

□ Fixture com connector ES livre
□ CX sifonada posicionada (banheiro) ou CX gordura (cozinha)
□ Prumada ES existente no pavimento
□ PipingSystemType "ES - Esgoto" criado
□ PipeType PVC Esgoto carregado
□ DN pré-definido pelo Core (ex: DN 100 para vaso)
□ Ramal do vaso NÃO passa pela CX sifonada (verificar rota)
□ Vista de planta ativa no Level correto
```

### 4.2 Configuração de parâmetros

```csharp
// O plugin prepara o ambiente antes de chamar o unMEP

public void PrepareForUnMep(Document doc, UnMepRequest request)
{
    // 1. Garantir PipingSystemType
    EnsurePipingSystem(doc, request.System);
    
    // 2. Garantir PipeType (material)
    EnsurePipeType(doc, request.Material);
    
    // 3. Selecionar connectors de origem
    var connectorIds = request.SourceConnectorIds
        .Select(id => new ElementId(id))
        .ToList();
    _uidoc.Selection.SetElementIds(connectorIds);
    
    // 4. Ativar vista correta
    ActivateFloorPlanView(doc, request.LevelName);
    
    // 5. Registrar no log
    _log.Log(ValidationLevel.Info, 
        $"Modelo preparado para unMEP: {request.System}, Level={request.LevelName}");
}
```

### 4.3 O que deve estar resolvido ANTES (pelo Core)

| Decisão | Responsável | Valor |
|---------|------------|-------|
| Qual DN usar | Core (PipeSizingService) | DN 25, DN 100, etc. |
| Qual sistema | Core (PipelineOrchestrator) | ColdWater, Sewer |
| Qual material | Core (ConfigProvider) | PVC Soldável, PVC Esgoto |
| Pontos de origem/destino | Core (PointIdentificationService) | Connectors das fixtures |
| Prumada de destino | Core (RiserCluster) | ConnectorId da prumada |

---

## 5. Coleta de Resultados

### 5.1 Como identificar elementos criados pelo unMEP

```csharp
public class UnMepResultCollector
{
    public UnMepResult CollectResults(Document doc, ModelSnapshot before, int timeoutSec = 30)
    {
        // Esperar o unMEP terminar (polling)
        var deadline = DateTime.Now.AddSeconds(timeoutSec);
        ModelSnapshot current;
        ModelDelta delta;
        
        do
        {
            Thread.Sleep(2000); // Polling a cada 2s
            current = ModelSnapshot.Take(doc);
            delta = before.CompareTo(current);
            
            // Se criou algo e parou de mudar, assumir que terminou
            if (delta.NewPipeIds.Any() && !IsStillChanging(doc, current))
                break;
                
        } while (DateTime.Now < deadline);
        
        if (!delta.NewPipeIds.Any() && !delta.NewFittingIds.Any())
        {
            return UnMepResult.NothingCreated();
        }
        
        return new UnMepResult
        {
            Status = delta.NewPipeIds.Any() ? "success" : "failure",
            NewPipes = CollectPipeInfo(doc, delta.NewPipeIds),
            NewFittings = CollectFittingInfo(doc, delta.NewFittingIds),
            NewAccessories = CollectAccessoryInfo(doc, delta.NewAccessoryIds)
        };
    }
    
    private bool IsStillChanging(Document doc, ModelSnapshot lastSnapshot)
    {
        Thread.Sleep(3000); // Aguardar 3s
        var newSnapshot = ModelSnapshot.Take(doc);
        return !lastSnapshot.PipeIds.SetEquals(newSnapshot.PipeIds);
    }
    
    private List<PipeInfo> CollectPipeInfo(Document doc, List<int> pipeIds)
    {
        return pipeIds.Select(id =>
        {
            var pipe = doc.GetElement(new ElementId(id)) as Pipe;
            if (pipe == null) return null;
            
            return new PipeInfo
            {
                ElementId = id,
                DiameterMm = (int)(pipe.Diameter * 304.8),
                LengthM = pipe.get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble() * 0.3048,
                SystemName = pipe.MEPSystem?.Name ?? "Sem sistema",
                IsConnected = HasConnectedEnds(pipe)
            };
        })
        .Where(p => p != null)
        .ToList();
    }
}
```

### 5.2 Mapeamento de resultados

| O que verificar | Como verificar | Se falhar |
|----------------|---------------|-----------|
| Pipes criados | Delta de ElementIds (classe Pipe) | Resultado vazio → failure |
| Fittings criados | Delta de ElementIds (categoria PipeFitting) | Pode estar ok (rota reta) |
| Conectividade | `connector.IsConnected` nos endpoints | Médio: trecho desconectado |
| DN correto | `pipe.Diameter * 304.8 == DN_esperado` | Médio: DN errado |
| Sistema correto | `pipe.MEPSystem.Name == sistema_esperado` | Leve: reatribuir |
| Comprimento razoável | `length < comprimento_máximo_esperado` | Leve: rota ineficiente |

---

## 6. Validação Pós-Execução

### 6.1 Validações obrigatórias

```csharp
public class UnMepValidator
{
    public ValidationReport Validate(Document doc, UnMepResult result, UnMepRequest request)
    {
        var report = new ValidationReport();
        
        // 1. Algo foi criado?
        if (!result.NewPipes.Any())
        {
            report.Add(ValidationLevel.Critical, 
                "unMEP não criou nenhum elemento");
            return report;
        }
        
        // 2. Conectividade: origem → destino
        bool originated = CheckConnectivity(doc, 
            request.SourceConnectorId, result.NewPipes);
        if (!originated)
            report.Add(ValidationLevel.Critical, 
                "Rede não conecta à fixture de origem");
        
        bool terminated = CheckConnectivity(doc, 
            request.DestinationConnectorId, result.NewPipes);
        if (!terminated)
            report.Add(ValidationLevel.Critical, 
                "Rede não conecta à prumada de destino");
        
        // 3. DNs corretos
        foreach (var pipe in result.NewPipes)
        {
            if (pipe.DiameterMm != request.ExpectedDiameterMm)
                report.Add(ValidationLevel.Medium, 
                    $"Pipe {pipe.ElementId}: DN {pipe.DiameterMm}mm ≠ esperado {request.ExpectedDiameterMm}mm");
        }
        
        // 4. DN nunca diminui no sentido do escoamento (ES)
        if (request.System == HydraulicSystem.Sewer)
        {
            if (HasDecreasingDiameter(doc, result.NewPipes))
                report.Add(ValidationLevel.Critical, 
                    "DN diminui no sentido do escoamento");
        }
        
        // 5. Sistema atribuído
        foreach (var pipe in result.NewPipes.Where(p => p.SystemName == "Sem sistema"))
        {
            report.Add(ValidationLevel.Light, 
                $"Pipe {pipe.ElementId}: sem sistema atribuído — será corrigido na etapa M10");
        }
        
        // 6. Comprimento razoável
        double totalLengthM = result.NewPipes.Sum(p => p.LengthM);
        double straightLineM = CalculateStraightLine(request.SourcePosition, request.DestinationPosition);
        
        if (totalLengthM > straightLineM * 3.0)
            report.Add(ValidationLevel.Light, 
                $"Rota muito longa: {totalLengthM:F1}m (reta seria {straightLineM:F1}m)");
        
        return report;
    }
}
```

### 6.2 Checklist de validação pós-unMEP

```
PARA cada execução do unMEP:

□ Elementos criados? (pipes > 0)
□ Conectividade origem → destino?
□ DN correto em todos os pipes?
□ DN nunca diminui (ES)?
□ Sistema MEP atribuído?
□ Fittings nos pontos de mudança de direção?
□ Sem colisão com outros sistemas?
□ Comprimento razoável (< 3× a reta)?
□ Sem trechos contra gravidade (ES)?
```

### 6.3 Validação normativa pós-unMEP

| Regra | Verificação | Se violada |
|-------|------------|-----------|
| DN vaso ≥ 100mm | Todos os pipes do ramal do vaso ≥ DN 100 | Crítico |
| DN nunca diminui | Percorrer do aparelho à prumada | Crítico |
| Ramal vaso independente da CX sifonada | Traceroute do vaso NÃO passa pela CX sifonada | Médio |
| CX sifonada recebe apenas aparelhos compatíveis | Verificar connectors da CX | Médio |

---

## 7. Tratamento de Erros

### 7.1 Tipos de falha

| Código | Tipo | Descrição | Frequência esperada |
|--------|------|-----------|-------------------|
| `UNMEP-EXE-001` | Execução | unMEP não respondeu (timeout) | Baixa |
| `UNMEP-EXE-002` | Execução | unMEP não está instalado/ativo | Rara (config) |
| `UNMEP-EXE-003` | Execução | Comando não disponível no ribbon | Rara (versão) |
| `UNMEP-RES-001` | Resultado | Nenhum elemento criado | Média |
| `UNMEP-RES-002` | Resultado | Rota parcial (desconectada) | Média |
| `UNMEP-RES-003` | Resultado | DN incorreto aplicado | Baixa |
| `UNMEP-GEO-001` | Geométrica | Colisão com estrutura | Média |
| `UNMEP-GEO-002` | Geométrica | Rota impossível (sem caminho) | Baixa |
| `UNMEP-GEO-003` | Geométrica | Fittings não conectaram | Média |
| `UNMEP-VAL-001` | Validação | DN diminui no escoamento | Baixa |
| `UNMEP-VAL-002` | Validação | Ramal do vaso passa pela CX sifonada | Baixa |

### 7.2 Reação do plugin por tipo

| Tipo | Ação imediata | Próximo passo |
|------|-------------|---------------|
| **Timeout** | Log Crítico | Undo + tentar Dynamo como fallback |
| **Nada criado** | Log Crítico | Tentar Dynamo; se falha → manual |
| **Rota parcial** | Log Médio | Validar o que foi criado; completar via Dynamo |
| **DN incorreto** | Log Médio | Corrigir DN via Revit API (pipe.Diameter = correto) |
| **Colisão** | Log Leve | Aceitar rota; alertar usuário |
| **Rota impossível** | Log Crítico | Marcar trecho para roteamento manual |
| **DN diminui** | Log Crítico | Undo + corrigir trechos + redesenhar |

### 7.3 Fluxo de tratamento

```
unMEP executa
  │
  ├── Nada criado?
  │     └── UNDO → FALLBACK Dynamo → FALLBACK Manual
  │
  ├── Parcialmente criado?
  │     ├── Conectividade OK? → Validar normativamente → Continuar
  │     └── Conectividade NOK? → Completar via Dynamo → Continuar
  │
  ├── Totalmente criado?
  │     ├── Validação OK? → ✅ Sucesso
  │     └── Validação NOK?
  │           ├── DN errado? → Corrigir DN via API → ✅
  │           ├── DN diminui? → UNDO → Reconfigurar → Tentar novamente
  │           └── Ramal vaso por CX? → UNDO → Rota alternativa → Tentar
  │
  └── Timeout?
        └── UNDO → FALLBACK Dynamo → FALLBACK Manual
```

---

## 8. Logs e Rastreabilidade

### 8.1 O que registrar

| Momento | Log | Nível | Conteúdo |
|---------|-----|-------|----------|
| Pré-execução | Preparação | Info | "Preparando modelo para unMEP: sistema={sys}, origem={id1}, destino={id2}" |
| Pré-execução | Snapshot | Info | "Snapshot capturado: {N} pipes, {M} fittings existentes" |
| Disparo | Comando | Info | "Disparando unMEP para roteamento {sistema}" |
| Pós-execução | Delta | Info | "unMEP criou {P} pipes, {F} fittings em {T}ms" |
| Pós-execução | Validação OK | Info | "Rota validada: DN correto, conectividade OK" |
| Pós-execução | Validação NOK | Médio/Crítico | "Falha: {código} — {mensagem}" |
| Fallback | Transição | Info | "Fallback para Dynamo: trecho {id}" |
| Fallback | Manual | Médio | "Trecho {id} marcado para roteamento manual" |

### 8.2 Formato de log

```csharp
// Todos os logs do unMEP são prefixados com [unMEP]

_log.Log(ValidationLevel.Info, 
    $"[unMEP] Preparando: {system}, origem={sourceId}, destino={destId}");

_log.Log(ValidationLevel.Info, 
    $"[unMEP] Delta: +{delta.NewPipeIds.Count} pipes, +{delta.NewFittingIds.Count} fittings ({elapsedMs}ms)");

_log.Log(ValidationLevel.Critical, 
    $"[unMEP] UNMEP-RES-001: Nenhum elemento criado após {timeoutSec}s");
```

### 8.3 Auditoria

```
PARA cada chamada ao unMEP, registrar:

{
  "timestamp": "ISO 8601",
  "modulo": "M07 ou M08",
  "sistema": "ColdWater ou Sewer",
  "origem_id": 12345,
  "destino_id": 67890,
  "dn_esperado_mm": 25,
  "elementos_antes": { "pipes": 45, "fittings": 30 },
  "elementos_depois": { "pipes": 48, "fittings": 33 },
  "delta": { "pipes": 3, "fittings": 3 },
  "duracao_ms": 8500,
  "status": "success | partial | failure",
  "erros": [],
  "fallback": null
}
```

---

## 9. Estratégia de Fallback

### 9.1 Cascata de execução

```
PARA cada trecho de rede a criar:

NÍVEL 1 — Revit API direta
  Condição: trecho reto, sem obstáculo, < 2 mudanças de direção
  Método: PipeCreator + FittingInsertionService
  Tempo: < 2s
  Taxa de sucesso esperada: ~95%

NÍVEL 2 — Dynamo
  Condição: múltiplos trechos, batch, caminhos simples
  Método: Script .dyn via DynamoExecutorAdapter
  Tempo: < 30s
  Taxa de sucesso esperada: ~80%

NÍVEL 3 — unMEP
  Condição: roteamento com obstáculos, caminhos complexos
  Método: Preparar modelo + disparar unMEP + validar
  Tempo: < 60s
  Taxa de sucesso esperada: ~70%

NÍVEL 4 — Manual
  Condição: todos os anteriores falharam
  Método: Destacar trecho no modelo, log Médio, aguardar usuário
  Tempo: variável
  Taxa de sucesso: 100% (humano resolve)
```

### 9.2 Quando NÃO usar unMEP

| Cenário | Por quê | Alternativa |
|---------|---------|------------|
| Trecho reto entre 2 pontos | Revit API é mais rápido e controlável | Revit API direta |
| Inserção de equipamentos | unMEP não é projetado para inserção | Dynamo |
| Ajuste de elevação (declividade) | Operação simples de set parameter | Dynamo |
| Criação de PipingSystem | Revit API nativa | Plugin direto |
| Layout de pranchas | unMEP não faz isso | Dynamo |
| Modelo sem fixtures posicionadas | unMEP precisa de origem/destino definidos | Não chamar — executar M04 antes |

### 9.3 Quando exigir intervenção humana

```
MARCAR PARA MANUAL quando:

1. unMEP falhou 2× no mesmo trecho
2. Dynamo falhou como fallback
3. Revit API falhou como fallback
4. Conflito geométrico irresolvível (pilar no caminho sem desvio possível)
5. Ramal impossível (fixture em nível diferente sem espaço para rampa)
6. DN necessário não disponível como PipeType

COMO MARCAR:
  - Log Médio com ElementIds dos endpoints
  - Destacar endpoints no modelo (override vermelho)
  - Adicionar à lista "Pendências Manuais" na UI
  - NÃO bloquear o pipeline (outros trechos continuam)
```

---

## 10. Limitações

### 10.1 Limitações do unMEP

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| **Sem API oficial** | Não há garantia de controle programático | Comunicação indireta via estado do modelo |
| **Sem retorno estruturado** | Plugin não sabe se unMEP terminou com sucesso | Snapshot antes/depois + polling |
| **Versão pode mudar** | Atualizações do unMEP podem quebrar integração | Fixar versão; testar antes de atualizar |
| **Depende de UI do Revit** | Pode exigir vista ativa, ribbon visível | Ativar vista/ribbon programaticamente |
| **Controle de DN limitado** | unMEP pode aplicar DN diferente do esperado | Validar e corrigir DN pós-execução via API |
| **Não garante conformidade normativa** | unMEP não sabe das regras NBR | Validação normativa é SEMPRE feita pelo plugin |
| **Performance imprevisível** | Pode demorar muito em modelos grandes | Timeout + fallback |

### 10.2 Falta de controle fino

```
O PLUGIN NÃO CONTROLA:
  - Rota escolhida pelo unMEP (pode ser ineficiente)
  - Ordem de criação de elementos
  - Tipo exato de fitting usado
  - Se o unMEP tentou mas falhou silenciosamente
  - Se o unMEP alterou elementos existentes

O PLUGIN CONTROLA:
  - O que existe ANTES (preparação)
  - O que existe DEPOIS (validação)
  - Se o resultado é aceitável (conformidade)
  - Undo se resultado não serve
```

### 10.3 Dependência de interface

```
RISCO: se o unMEP mudar seu ribbon, botões ou fluxo de UI,
       a automação do plugin pode quebrar.

MITIGAÇÃO:
  1. Isolar toda integração em UnMepAdapter (1 classe)
  2. Se quebrar, alterar APENAS essa classe
  3. Fallback automático para Dynamo se unMEP não responder
  4. Nunca depender do unMEP para funções críticas
     → unMEP é COMPLEMENTAR, não essencial
```

---

## 11. Boas Práticas

### 11.1 Evitar dependência excessiva

```
REGRA 1: unMEP é OPCIONAL
  O plugin deve funcionar sem unMEP instalado.
  Se unMEP não está disponível → pular nível 3 do fallback.

REGRA 2: unMEP é COMPLEMENTAR
  Funcionalidade principal via Revit API e Dynamo.
  unMEP é "bônus" para caminhos complexos.

REGRA 3: NUNCA confiar cegamente no resultado
  Sempre validar o que o unMEP criou.
  Tratar como "caixa preta": input → output → validar.

REGRA 4: SEMPRE ter plano B
  Para cada trecho que o unMEP vai criar,
  ter alternativa (Dynamo ou manual) mapeada.
```

### 11.2 Manter controle do sistema

```
1. TODA decisão hidráulica é do Core
   unMEP NÃO decide DN, declividade, sistema ou material
   Esses valores são definidos pelo Core e aplicados antes/depois

2. TODA validação normativa é do Core
   unMEP NÃO valida conformidade com NBR
   Plugin valida tudo após unMEP executar

3. TODO log passa pelo LogService do plugin
   Registrar antes, durante (polling) e depois
   Nenhuma ação do unMEP pode ficar sem registro
```

### 11.3 Garantir previsibilidade

```
PARA cada chamada ao unMEP:

  1. Snapshot ANTES
  2. Timeout definido (30s padrão)
  3. Delta calculado DEPOIS
  4. Se delta = 0 → falha registrada, fallback acionado
  5. Se delta > 0 → validar, aceitar ou undoar
  6. Limpeza de estado (desseleção, reset de vista)
  7. Log completo para auditoria
```

### 11.4 Encapsulamento da integração

```csharp
// TODA a integração do unMEP vive em UMA CLASSE

namespace HidraulicoPlugin.Revit.Adapters
{
    /// <summary>
    /// Adaptador para integração indireta com unMEP.
    /// Se o unMEP mudar, apenas esta classe é alterada.
    /// </summary>
    public class UnMepAdapter
    {
        // Preparar modelo
        public void PrepareModel(Document doc, UnMepRequest request) { }
        
        // Disparar unMEP
        public void TriggerUnMep(UIDocument uidoc, UnMepRequest request) { }
        
        // Coletar resultados
        public UnMepResult CollectResults(Document doc, ModelSnapshot before) { }
        
        // Validar pós-execução
        public ValidationReport ValidateResults(Document doc, UnMepResult result) { }
        
        // Verificar se unMEP está disponível
        public bool IsAvailable() { }
        
        // Fallback se unMEP falhar
        public void HandleFailure(UnMepRequest request, string errorCode) { }
    }
}
```

---

## 12. Resumo: Plugin ↔ unMEP vs. Plugin ↔ Dynamo

| Aspecto | Dynamo | unMEP |
|---------|--------|-------|
| **Comunicação** | JSON file (estruturada) | Estado do modelo (indireta) |
| **Controle** | Alto (input/output definidos) | Baixo (caixa preta) |
| **Detecção de resultado** | Output JSON explícito | Snapshot antes/depois |
| **Erro handling** | Códigos de erro no JSON | Inferência por delta |
| **Reexecução** | Novo request com instruções filtradas | Undo + retentar |
| **Fallback** | Revit API direta | Dynamo ou manual |
| **Quando usar** | Batch, inserção, ajuste Z | Roteamento complexo |
| **Essencial** | Sim | Não (complementar) |
| **Encapsulamento** | DynamoExecutorAdapter | UnMepAdapter |
