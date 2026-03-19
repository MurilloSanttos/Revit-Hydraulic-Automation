# Pipeline de Execução por Etapas — Orquestrador do Sistema

> Especificação completa do orquestrador que coordena as 13 etapas do plugin hidráulico, controlando estado, validação humana, integração com Dynamo/unMEP e rollback.

---

## 1. Visão Geral da Pipeline

### 1.1 Conceito

O orquestrador é o **coração operacional** do plugin. Ele coordena a execução de 13 etapas em sequência controlada, onde cada etapa:

- Recebe dados da etapa anterior
- Executa processamento (Core, Dynamo ou unMEP)
- Valida resultado
- Aguarda aprovação humana
- Só então libera a próxima etapa

### 1.2 Tipo de execução: Sequencial controlada com gates

```
E01 → Gate → E02 → Gate → E03 → ... → E13 → ✅ Completo
 │              │              │
 └── Validação  └── Validação  └── Validação
     automática      automática      automática
     + aprovação     + aprovação     + aprovação
     humana          humana          humana
```

Não é automação completa. É **semi-automação**: o plugin faz o trabalho pesado, o engenheiro valida e aprova.

### 1.3 Papel central

```
               ┌────────────────────┐
               │   ORQUESTRADOR     │
               │  (PipelineEngine)  │
               │                    │
       ┌───────┤  Estado global     │
       │       │  Controle de fluxo │
       │       │  Validação/Gates   │
       │       │  Logs              │
       │       └────────┬───────────┘
       │                │
  ┌────┴────┐    ┌──────┴──────┐    ┌─────────────┐
  │  CORE   │    │  REVIT API  │    │ DYNAMO/UNMEP│
  │ Cálcula │    │  Lê/cria    │    │ Executa     │
  │ Decide  │    │  elementos  │    │ em massa    │
  └─────────┘    └─────────────┘    └─────────────┘
```

---

## 2. Modelo de Execução

### 2.1 Execução step-by-step

```
PARA cada etapa E{NN}:

  1. VERIFICAR pré-condições
     → Etapas anteriores completas?
     → Dados de entrada disponíveis?
     → Modelo em estado válido?

  2. EXECUTAR processamento
     → Chamar serviço Core
     → Se necessário: chamar Dynamo ou unMEP via adapter
     → Capturar resultado

  3. VALIDAR resultado automaticamente
     → Critérios objetivos (contagem, proporção, conformidade)
     → Classificar erros por nível

  4. APRESENTAR ao usuário
     → Mostrar resultado na UI
     → Destacar elementos no modelo
     → Listar erros/alertas

  5. AGUARDAR aprovação
     → Usuário pode: Aprovar / Rejeitar / Refazer
     → Se Aprovar → marcar etapa como Completed
     → Se Rejeitar → marcar como Failed, exigir correção
     → Se Refazer → rollback parcial, voltar ao passo 2

  6. LIBERAR próxima etapa
     → Só se estado == Completed
```

### 2.2 Controle de estado

```csharp
public class PipelineState
{
    public Dictionary<string, StageState> Stages { get; set; }
    public string CurrentStageId { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsCompleted => Stages.Values.All(s => s.Status == StageStatus.Completed);
}

public class StageState
{
    public string StageId { get; set; }
    public StageStatus Status { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AttemptCount { get; set; }
    public StageResult LastResult { get; set; }
    public string ApprovalNote { get; set; }
}
```

### 2.3 Persistência de progresso

```
ONDE salvar:
  JSON em %APPDATA%/HidraulicoPlugin/{modelo_nome}/pipeline_state.json

QUANDO salvar:
  - Ao completar cada etapa (Completed)
  - Ao falhar (Failed)
  - Ao iniciar pipeline (criação)

POR QUÊ:
  - Se Revit travar, pode retomar de onde parou
  - Se usuário fechar plugin, estado não se perde
  - Auditoria de execução
```

```json
{
  "modelo": "Residencia_01.rvt",
  "versao_plugin": "0.5.0-alpha",
  "iniciado_em": "2026-03-18T20:00:00",
  "etapa_atual": "E04",
  "etapas": {
    "E01": { "status": "Completed", "tentativas": 1, "completado_em": "2026-03-18T20:01:15" },
    "E02": { "status": "Completed", "tentativas": 1, "completado_em": "2026-03-18T20:02:30" },
    "E03": { "status": "Completed", "tentativas": 2, "completado_em": "2026-03-18T20:05:00" },
    "E04": { "status": "Running", "tentativas": 1 },
    "E05": { "status": "Pending" },
    "E06": { "status": "Pending" },
    "E07": { "status": "Pending" },
    "E08": { "status": "Pending" },
    "E09": { "status": "Pending" },
    "E10": { "status": "Pending" },
    "E11": { "status": "Pending" },
    "E12": { "status": "Pending" },
    "E13": { "status": "Pending" }
  }
}
```

---

## 3. Estrutura de Etapas

### 3.1 Tabela geral

| ID | Nome | Dependências | Ferramenta principal | Aprovação humana |
|----|------|-------------|---------------------|-----------------|
| E01 | Detecção de ambientes | — | Core + Revit API | ✅ |
| E02 | Classificação de ambientes | E01 | Core | ✅ |
| E03 | Identificação de pontos hidráulicos | E02 | Core + Data | ✅ |
| E04 | Inserção de equipamentos | E03 | Dynamo | ✅ |
| E05 | Validação de equipamentos | E04 | Core + Revit API | ✅ |
| E06 | Criação de prumadas | E05 | Revit API | ✅ |
| E07 | Geração de rede AF | E06 | Dynamo / unMEP | ✅ |
| E08 | Geração de rede ES | E06 | Dynamo / unMEP | ✅ |
| E09 | Aplicação de inclinações | E08 | Dynamo | ✅ |
| E10 | Criação de sistemas MEP | E07 + E08 | Revit API | ⬜ (auto) |
| E11 | Dimensionamento hidráulico | E10 | Core | ✅ |
| E12 | Geração de tabelas | E11 | Revit API | ⬜ (auto) |
| E13 | Geração de pranchas | E12 | Dynamo | ✅ |

### 3.2 Detalhamento de cada etapa

---

#### E01 — Detecção de Ambientes

```
ID:           E01
Nome:         DetectionStage
Dependências: nenhuma (etapa inicial)
Ferramenta:   Core (lógica) + Revit API (leitura)

ENTRADA:
  - Document do Revit (via IModelReader)

PROCESSAMENTO:
  1. Core chama IModelReader.GetRooms()
  2. Revit retorna List<RoomInfo> (id, nome, level, área, centroide)
  3. Core filtra: Room com Location != null e Area > 0
  4. Core agrupa por Level

SAÍDA:
  - List<RoomInfo> com todos os ambientes válidos
  - Contagem por Level
  - Log de ambientes descartados

CRITÉRIO DE SUCESSO:
  - ≥ 1 Room válido encontrado
  - 0 erros Críticos

CRITÉRIO DE FALHA:
  - 0 Rooms válidos → Pipeline bloqueado (Crítico)
```

---

#### E02 — Classificação de Ambientes

```
ID:           E02
Nome:         ClassificationStage
Dependências: E01 (Completed)
Ferramenta:   Core (NLP + matching)

ENTRADA:
  - List<RoomInfo> de E01

PROCESSAMENTO:
  1. Core chama RoomClassifier.Classify(roomName) para cada Room
  2. Normaliza nome (remove acentos, maiúsculas, espaços)
  3. Compara com dicionário de sinônimos
  4. Atribui RoomType + Confidence

SAÍDA:
  - List<ClassificationResult> (roomId, type, confidence)
  - Ambientes hidráulicos vs. não-hidráulicos
  - Ambientes com baixa confiança destacados

CRITÉRIO DE SUCESSO:
  - ≥ 1 ambiente hidráulico classificado com confiança ≥ 0.70
  - ≥ 80% dos ambientes classificados (proporção)
  - 0 ambientes Críticos (erro de classificação óbvio)

CRITÉRIO DE FALHA:
  - 0 ambientes hidráulicos → Pipeline bloqueado
```

---

#### E03 — Identificação de Pontos Hidráulicos

```
ID:           E03
Nome:         PointIdentificationStage
Dependências: E02 (Completed)
Ferramenta:   Core + Data (JSON normativo)

ENTRADA:
  - List<ClassificationResult> de E02

PROCESSAMENTO:
  1. Core consulta mapeamento ambiente→aparelhos no JSON
  2. Gera lista de pontos obrigatórios e opcionais por ambiente
  3. Calcula totais (peso AF, UHC ES)

SAÍDA:
  - List<HydraulicPointPlan> por ambiente
  - Total por tipo de aparelho
  - Soma de pesos global

CRITÉRIO DE SUCESSO:
  - Todo ambiente hidráulico tem ≥ 1 ponto obrigatório
  - 0 ambientes sem mapeamento

CRITÉRIO DE FALHA:
  - Ambiente classificado mas sem mapeamento no JSON → Médio
```

---

#### E04 — Inserção de Equipamentos

```
ID:           E04
Nome:         EquipmentInsertionStage
Dependências: E03 (Completed)
Ferramenta:   Dynamo (inserção em batch)

ENTRADA:
  - List<HydraulicPointPlan> de E03
  - Posições calculadas pelo Core (centroide + offset)
  - Famílias MEP configuradas

PROCESSAMENTO:
  1. Core gera instruções de inserção (JSON input)
  2. Plugin escreve JSON e chama Dynamo
  3. Dynamo insere FamilyInstances no modelo
  4. Plugin lê JSON de output

SAÍDA:
  - List<InsertionResult> com ElementIds criados
  - Status por equipamento (inserido/falhou)
  - Elementos não inseridos listados

CRITÉRIO DE SUCESSO:
  - ≥ 80% dos equipamentos inseridos
  - Famílias corretas usadas
  - Equipamentos dentro do Room esperado

CRITÉRIO DE FALHA:
  - < 50% inseridos → Médio (pedir intervenção)
  - Família não encontrada → Crítico
```

---

#### E05 — Validação de Equipamentos

```
ID:           E05
Nome:         EquipmentValidationStage
Dependências: E04 (Completed)
Ferramenta:   Core + Revit API (leitura)

ENTRADA:
  - List<InsertionResult> de E04
  - Equipamentos já existentes no modelo (pré-E04)

PROCESSAMENTO:
  1. Revit lê todos os fixtures MEP por Room via IModelReader
  2. Core compara: existentes vs. necessários (E03)
  3. Core classifica cada equipamento: Valid, ValidWithRemarks, Invalid, Missing

SAÍDA:
  - List<EquipmentValidationResult>
  - Status por equipamento
  - Lista de faltantes/excedentes

CRITÉRIO DE SUCESSO:
  - 0 equipamentos Missing para pontos obrigatórios
  - 0 equipamentos Invalid

CRITÉRIO DE FALHA:
  - Ponto obrigatório Missing → Médio (pedir resolução)
```

---

#### E06 — Criação de Prumadas

```
ID:           E06
Nome:         RiserCreationStage
Dependências: E05 (Completed)
Ferramenta:   Revit API direta

ENTRADA:
  - Ambientes classificados (E02)
  - Equipamentos validados (E05)
  - Configuração de agrupamento de prumadas

PROCESSAMENTO:
  1. Core decide posição e agrupamento de prumadas (shaft/parede)
  2. Revit cria Pipes verticais (AF, ES, VE) entre Levels
  3. Revit atribui parâmetros HID_*

SAÍDA:
  - List<RiserInfo> com ElementIds
  - Agrupamento (AF+ES+VE por posição)
  - DNs iniciais (mínimo normativo, será refinado em E11)

CRITÉRIO DE SUCESSO:
  - ≥ 1 prumada AF e ≥ 1 prumada ES criadas
  - Prumadas visíveis em todos os Levels
  - Connectors livres para conexão

CRITÉRIO DE FALHA:
  - Nenhuma prumada criada → Crítico
```

---

#### E07 — Geração de Rede de Água Fria

```
ID:           E07
Nome:         ColdWaterNetworkStage
Dependências: E06 (Completed)
Ferramenta:   Dynamo (rotas simples) / unMEP (rotas complexas)

ENTRADA:
  - Equipamentos com connectors AF (E05)
  - Prumadas AF (E06)
  - DNs calculados pelo Core

PROCESSAMENTO:
  1. Core calcula vazão e DN para cada trecho
  2. Core define topologia (quem conecta em quem)
  3. Dynamo/unMEP cria Pipes e Fittings no modelo
  4. Plugin valida conectividade

SAÍDA:
  - List<PipeSegment> criados
  - Topologia da rede (grafo)
  - Trechos conectados vs. desconectados

CRITÉRIO DE SUCESSO:
  - 100% dos equipamentos AF conectados à prumada
  - 0 trechos desconectados
  - DNs de acordo com cálculo do Core

CRITÉRIO DE FALHA:
  - < 80% conectados → Médio
  - DN diminui na rede → Crítico
```

---

#### E08 — Geração de Rede de Esgoto

```
ID:           E08
Nome:         SewerNetworkStage
Dependências: E06 (Completed)
Ferramenta:   Dynamo (rotas simples) / unMEP (rotas complexas)

ENTRADA:
  - Equipamentos com connectors ES (E05)
  - Prumadas ES (E06)
  - DNs e UHCs calculados pelo Core
  - Regras de CX sifonada e CX gordura

PROCESSAMENTO:
  1. Core calcula DN e UHC por trecho
  2. Core define ramais independentes (vaso) vs. CX sifonada
  3. Core posiciona CX sifonada/gordura
  4. Dynamo/unMEP cria rede com acessórios

SAÍDA:
  - List<PipeSegment> criados
  - Acessórios inseridos (CX sifonada, CX gordura)
  - Topologia da rede ES

CRITÉRIO DE SUCESSO:
  - 100% dos equipamentos ES conectados
  - Vaso com ramal independente (não passa pela CX sifonada)
  - CX sifonada em banheiros, CX gordura em cozinhas
  - DN vaso ≥ 100mm

CRITÉRIO DE FALHA:
  - Vaso por CX sifonada → Crítico
  - DN vaso < 100mm → Crítico
```

---

#### E09 — Aplicação de Inclinações

```
ID:           E09
Nome:         SlopeApplicationStage
Dependências: E08 (Completed)
Ferramenta:   Dynamo (batch de Z adjustment)

ENTRADA:
  - Pipes horizontais ES de E08
  - Tabela de declividade por DN (do Core)

PROCESSAMENTO:
  1. Core consulta declividade por DN
  2. Core calcula desnível (ΔZ = L × i) para cada trecho
  3. Dynamo ajusta Z dos endpoints de cada Pipe

SAÍDA:
  - Pipes com Z ajustado
  - Verificação de gravidade (Z_saída < Z_entrada)
  - Declividade real vs. esperada

CRITÉRIO DE SUCESSO:
  - 100% dos trechos ES com declividade ≥ mínimo normativo
  - 0 trechos contra gravidade
  - Fittings reconectados após ajuste

CRITÉRIO DE FALHA:
  - Trecho contra gravidade → Crítico
  - Fitting desconectou → Médio
```

---

#### E10 — Criação de Sistemas MEP

```
ID:           E10
Nome:         MepSystemStage
Dependências: E07 (Completed) + E08 (Completed)
Ferramenta:   Revit API direta

ENTRADA:
  - Todos os Pipes e Fittings de E07, E08, E09

PROCESSAMENTO:
  1. Criar PipingSystems: "AF - Água Fria", "ES - Esgoto", "VE - Ventilação"
  2. Atribuir sistema a cada Pipe
  3. Verificar que todos os elementos têm sistema

SAÍDA:
  - 3 PipingSystems criados
  - Todos os elementos atribuídos

CRITÉRIO DE SUCESSO:
  - 3 sistemas ativos
  - 0 pipes sem sistema
  - Aprovação automática (sem gate humano)
```

---

#### E11 — Dimensionamento Hidráulico

```
ID:           E11
Nome:         SizingStage
Dependências: E10 (Completed)
Ferramenta:   Core (cálculos puros)

ENTRADA:
  - Topologia completa da rede (E07 + E08)
  - Pesos por aparelho (Data)
  - Configuração (altura reservatório, coef. C)

PROCESSAMENTO:
  1. Core percorre rede de montante a jusante
  2. Calcula ΣP por trecho
  3. Calcula Q = C × √ΣP
  4. Seleciona DN por velocidade ≤ 3.0 m/s
  5. Calcula perda de carga (FWH)
  6. Verifica pressão: P_din ≥ 3.0 mca em cada ponto
  7. Grava resultados como parâmetros HID_* nos Pipes

SAÍDA:
  - SizingResult por trecho (Q, DN, V, J, P)
  - Relatório de dimensionamento
  - Alertas de pressão insuficiente

CRITÉRIO DE SUCESSO:
  - 100% dos trechos dimensionados
  - Pressão ≥ 3.0 mca no ponto mais desfavorável
  - Velocidade ≤ 3.0 m/s em todos os trechos
  - DNs nunca diminuem no sentido do escoamento

CRITÉRIO DE FALHA:
  - Pressão < 3.0 mca → Crítico (mas não bloqueia — informa)
  - DN diminui → Crítico
```

---

#### E12 — Geração de Tabelas

```
ID:           E12
Nome:         ScheduleStage
Dependências: E11 (Completed)
Ferramenta:   Revit API direta

ENTRADA:
  - Parâmetros HID_* nos elementos (E11)
  - Configuração de tabelas

PROCESSAMENTO:
  1. Criar ViewSchedule para: AF, ES, VE, Equipamentos
  2. Adicionar campos dos parâmetros HID_*
  3. Aplicar filtros por sistema

SAÍDA:
  - 4 ViewSchedules criadas
  - Dados populados automaticamente

CRITÉRIO DE SUCESSO:
  - 4 tabelas com ≥ 1 linha de dados cada
  - Aprovação automática
```

---

#### E13 — Geração de Pranchas

```
ID:           E13
Nome:         SheetStage
Dependências: E12 (Completed)
Ferramenta:   Dynamo (layout)

ENTRADA:
  - ViewSchedules de E12
  - Views de planta por Level
  - Title block padrão

PROCESSAMENTO:
  1. Core define quais views vão em quais sheets
  2. Dynamo cria ViewSheets
  3. Dynamo posiciona views nas sheets

SAÍDA:
  - ViewSheets criadas e populadas
  - Numeração sequencial

CRITÉRIO DE SUCESSO:
  - ≥ 1 prancha criada
  - Todas as tabelas posicionadas
  - Numeração sem duplicata
```

---

## 4. Máquina de Estados

### 4.1 Estados possíveis

```
┌──────────┐    executar    ┌──────────┐    validar    ┌───────────────┐
│ Pending  │───────────────→│ Running  │──────────────→│ WaitingApproval│
└──────────┘                └────┬─────┘               └───────┬───────┘
                                 │                             │
                            erro │                    aprovar  │  rejeitar
                                 ↓                             ↓       ↓
                           ┌──────────┐              ┌──────────┐  ┌──────────┐
                           │  Failed  │              │Completed │  │  Failed  │
                           └────┬─────┘              └──────────┘  └────┬─────┘
                                │                                       │
                           retry│                               retry   │
                                ↓                                       ↓
                           ┌──────────┐                           ┌──────────┐
                           │ Running  │                           │ Running  │
                           └──────────┘                           └──────────┘


Estado especial:
                           ┌────────────┐
                           │ RolledBack │  ← Após undo
                           └──────┬─────┘
                                  │ retry
                                  ↓
                           ┌──────────┐
                           │ Pending  │
                           └──────────┘
```

### 4.2 Enum de estados

```csharp
public enum StageStatus
{
    Pending = 0,        // Ainda não executada
    Running = 1,        // Em execução
    WaitingApproval = 2,// Executou, aguardando aprovação humana
    Completed = 3,      // Aprovada e concluída
    Failed = 4,         // Erro (pode retry)
    Skipped = 5,        // Pulada (com justificativa)
    RolledBack = 6      // Desfeita (undo aplicado)
}
```

### 4.3 Transições permitidas

| De | Para | Trigger |
|----|------|---------|
| Pending → Running | Usuário clica "Executar" e pré-condições OK |
| Running → WaitingApproval | Execução concluiu sem erro crítico bloqueante |
| Running → Failed | Erro crítico durante execução |
| WaitingApproval → Completed | Usuário aprova |
| WaitingApproval → Failed | Usuário rejeita |
| Failed → Running | Usuário clica "Refazer" (retry) |
| Completed → RolledBack | Usuário clica "Desfazer" (raro) |
| RolledBack → Pending | Após undo, etapa volta para início |

### 4.4 Transições PROIBIDAS

```
❌ Pending → Completed     (não pode pular execução)
❌ Pending → WaitingApproval (não pode aprovar sem executar)
❌ Running → Completed     (não pode pular aprovação humana)
❌ Completed → Running     (não pode reexecutar etapa aprovada — só via RollBack)
❌ E{N} Running quando E{N-1} != Completed  (dependência não satisfeita)
```

---

## 5. Controle de Validação Humana

### 5.1 Pontos obrigatórios de aprovação

| Etapa | Requer aprovação | Justificativa |
|-------|-----------------|---------------|
| E01 | ✅ | Usuário confirma Rooms detectados |
| E02 | ✅ | Usuário confirma classificações (especialmente baixa confiança) |
| E03 | ✅ | Usuário confirma pontos mapeados por ambiente |
| E04 | ✅ | Usuário confirma posicionamento dos equipamentos |
| E05 | ✅ | Usuário confirma status de cada equipamento |
| E06 | ✅ | Usuário confirma posição e agrupamento de prumadas |
| E07 | ✅ | Usuário confirma rede AF (visualmente) |
| E08 | ✅ | Usuário confirma rede ES (visualmente) |
| E09 | ✅ | Usuário confirma inclinações |
| E10 | ⬜ Auto | Operação direta, sem ambiguidade |
| E11 | ✅ | Usuário revisa dimensionamento e pressões |
| E12 | ⬜ Auto | Criação de tabelas direta |
| E13 | ✅ | Usuário confirma layout de pranchas |

### 5.2 Como o usuário interage

```
FLUXO NA UI:

1. Painel mostra lista de etapas com ícone de status
   [ ✅ ] E01 Detecção        (completo)
   [ ✅ ] E02 Classificação   (completo)
   [ ▶  ] E03 Pontos          (pronta para executar) ← botão ativo
   [ 🔒 ] E04 Inserção        (bloqueada)
   [ 🔒 ] E05 Validação       (bloqueada)
   ...

2. Usuário clica "▶ Executar" em E03
   → Status muda para Running
   → Barra de progresso aparece

3. Execução termina
   → Status muda para WaitingApproval
   → Resultado aparece (tabela, log, destaque no modelo)
   → Dois botões: "✅ Aprovar" e "❌ Refazer"

4. Se aprovar:
   → Status → Completed
   → E04 desbloqueia (▶ ativo)

5. Se refazer:
   → Rollback se necessário
   → Status → Pending
   → Botão "▶ Executar" reaparece
```

### 5.3 Como bloquear avanço

```csharp
public bool CanExecute(string stageId)
{
    var stage = _state.Stages[stageId];
    
    // Já completou
    if (stage.Status == StageStatus.Completed) return false;
    
    // Está em execução
    if (stage.Status == StageStatus.Running) return false;
    
    // Verificar dependências
    var dependencies = GetDependencies(stageId);
    return dependencies.All(dep => 
        _state.Stages[dep].Status == StageStatus.Completed);
}
```

---

## 6. Tratamento de Erros

### 6.1 Captura de erros por etapa

```csharp
public StageResult ExecuteStage(string stageId)
{
    var stage = ResolveStage(stageId);
    _state.Stages[stageId].Status = StageStatus.Running;
    _state.Stages[stageId].AttemptCount++;
    
    try
    {
        _log.Log(ValidationLevel.Info, $"Iniciando {stageId}...");
        
        var result = stage.Execute();
        
        if (result.HasCriticalBlocker)
        {
            _state.Stages[stageId].Status = StageStatus.Failed;
            _log.Log(ValidationLevel.Critical, $"{stageId}: bloqueio crítico — {result.BlockerMessage}");
            return result;
        }
        
        _state.Stages[stageId].Status = stage.RequiresApproval 
            ? StageStatus.WaitingApproval 
            : StageStatus.Completed;
        
        _state.Stages[stageId].LastResult = result;
        SaveState();
        return result;
    }
    catch (Exception ex)
    {
        _state.Stages[stageId].Status = StageStatus.Failed;
        _log.Log(ValidationLevel.Critical, $"{stageId}: exceção — {ex.Message}");
        SaveState();
        return StageResult.Error(ex);
    }
}
```

### 6.2 Reação por tipo de erro

| Severidade | Ação | Bloqueio |
|-----------|------|---------|
| **Crítico bloqueante** | Aborta etapa, status = Failed | Pipeline para |
| **Crítico não-bloqueante** | Registra, continua | Pipeline continua com alerta |
| **Médio** | Registra, destaca no modelo | Usuário decide na aprovação |
| **Leve** | Registra no log | Invisível ao usuário (log only) |

### 6.3 Retry

```
REGRAS DE RETRY:

1. Máximo 3 tentativas por etapa
2. A cada retry, incrementar AttemptCount
3. Se etapa criou elementos parciais: rollback antes de retry
4. Após 3 falhas: marcar como Failed, NÃO bloquear pipeline
   → Etapas independentes podem continuar (ex: E07 sem E08)
   → Etapas dependentes ficam bloqueadas
5. Log cada tentativa com número: "[Tentativa 2/3]"
```

---

## 7. Estratégia de Rollback

### 7.1 Quando aplicar

```
APLICAR rollback quando:
  1. Usuário clica "Refazer" na etapa
  2. Erro crítico durante execução (auto-rollback)
  3. Validação pós-execução detecta violação grave
  4. Usuário rejeita resultado na aprovação
```

### 7.2 Como desfazer ações no Revit

```csharp
public class RollbackManager
{
    // Estratégia 1: Transaction Group (preferida)
    // Agrupa todas as transactions da etapa em 1 grupo reversível
    
    public void ExecuteWithRollback(Document doc, string stageName, Action<Document> action)
    {
        using var txGroup = new TransactionGroup(doc, $"Pipeline_{stageName}");
        txGroup.Start();
        
        try
        {
            action(doc);
            txGroup.Assimilate(); // Torna permanente
        }
        catch
        {
            txGroup.RollBack(); // Desfaz TUDO da etapa
            throw;
        }
    }
    
    // Estratégia 2: ElementId tracking
    // Para quando TransactionGroup não é viável
    
    private List<ElementId> _createdElements = new();
    
    public void TrackCreated(ElementId id)
    {
        _createdElements.Add(id);
    }
    
    public void UndoCreated(Document doc)
    {
        using var tx = new Transaction(doc, "Rollback");
        tx.Start();
        doc.Delete(_createdElements);
        tx.Commit();
        _createdElements.Clear();
    }
}
```

### 7.3 Rollback por etapa

| Etapa | O que desfazer | Método |
|-------|---------------|--------|
| E01 | Nada (apenas leitura) | — |
| E02 | Nada (apenas classificação em memória) | — |
| E03 | Nada (apenas mapeamento em memória) | — |
| E04 | Deletar FamilyInstances inseridas | TransactionGroup.RollBack ou Delete por ElementId |
| E05 | Nada (apenas validação) | — |
| E06 | Deletar Pipes verticais (prumadas) | TransactionGroup.RollBack |
| E07 | Deletar Pipes e Fittings AF | TransactionGroup.RollBack |
| E08 | Deletar Pipes, Fittings e Acessórios ES | TransactionGroup.RollBack |
| E09 | Restaurar Z original dos Pipes | TransactionGroup.RollBack |
| E10 | Deletar PipingSystems | TransactionGroup.RollBack |
| E11 | Limpar parâmetros HID_* | Overwrite com 0 / null |
| E12 | Deletar ViewSchedules | TransactionGroup.RollBack |
| E13 | Deletar ViewSheets | TransactionGroup.RollBack |

### 7.4 Limitações do rollback

```
NÃO É POSSÍVEL reverter:
  - Ações do Dynamo (já commitadas em Transaction separada)
    → Solução: guardar ElementIds e deletar manualmente
  
  - Ações do unMEP (Transaction própria)
    → Solução: guardar snapshot e deletar novos elementos

  - Undo chain do Revit pode estar poluída
    → Solução: TransactionGroup.RollBack é mais confiável que Ctrl+Z
```

---

## 8. Integração com Dynamo

### 8.1 Etapas que usam Dynamo

| Etapa | Script Dynamo | Quando |
|-------|--------------|--------|
| E04 | `04_InsertEquipment.dyn` | Sempre (inserção em batch) |
| E07 | `07_GenerateColdWaterNetwork.dyn` | Trechos simples |
| E08 | `08_GenerateSewerNetwork.dyn` | Trechos simples + CX |
| E09 | `09_ApplySlopes.dyn` | Sempre (batch Z adjust) |
| E13 | `13_GenerateSheets.dyn` | Sempre (layout) |

### 8.2 Fluxo no orquestrador

```csharp
// Dentro de cada Stage que usa Dynamo:

public StageResult Execute()
{
    // 1. Core decide o quê fazer
    var instructions = _coreService.Plan();
    
    // 2. Serializar para JSON
    string inputJson = _serializer.SerializeInput(instructions);
    
    // 3. Chamar Dynamo via adapter
    var dynamoResult = _dynamoExecutor.Execute(_scriptName, inputJson);
    
    // 4. Tratar resultado
    if (dynamoResult.Status == "failure")
    {
        _log.Log(ValidationLevel.Critical, $"Dynamo falhou: {dynamoResult.Errors.First().Mensagem}");
        return StageResult.Failed("Dynamo execution failed");
    }
    
    if (dynamoResult.Status == "partial")
    {
        _log.Log(ValidationLevel.Medium, $"Dynamo parcial: {dynamoResult.Summary.Falhas} falhas");
    }
    
    // 5. Validar no modelo
    var validation = _validator.ValidateCreatedElements(dynamoResult.CreatedElementIds);
    
    return new StageResult
    {
        Data = dynamoResult,
        Validation = validation,
        HasCriticalBlocker = validation.HasCritical
    };
}
```

---

## 9. Integração com unMEP

### 9.1 Etapas que podem usar unMEP

| Etapa | Quando usar unMEP | Condição |
|-------|-------------------|----------|
| E07 | Rota com obstáculos (pilares, furos) | Dynamo falhou ou rota complexa |
| E08 | Rota com ramificações complexas | Dynamo falhou ou rota complexa |

### 9.2 Fluxo de decisão

```
PARA cada trecho da rede:

  1. TENTAR via Dynamo (padrão)
     SE sucesso → próximo trecho
     SE falha ↓

  2. TENTAR via unMEP (se disponível)
     a. Plugin prepara modelo (seleção, parâmetros)
     b. Plugin captura snapshot
     c. Plugin dispara unMEP
     d. Plugin aguarda (polling 2s, timeout 30s)
     e. Plugin calcula delta (snapshot)
     f. SE delta > 0 → validar resultado
     g. SE delta = 0 → falha
     SE sucesso → próximo trecho
     SE falha ↓

  3. MARCAR para manual
     Destacar no modelo, log Médio
```

---

## 10. Logs e Monitoramento

### 10.1 Registro por etapa

```
CADA etapa registra:

[INÍCIO]  "{stageId}: Iniciando (tentativa {n}/{max})"
[PROGRESSO] "{stageId}: {ação específica}..."
[RESULTADO] "{stageId}: {contagem} elementos processados em {ms}ms"
[VALIDAÇÃO] "{stageId}: {erros} erros, {alertas} alertas"
[STATUS]   "{stageId}: Status → {novoStatus}"
```

### 10.2 Exemplo de log completo de 1 etapa

```
[20:01:00] [INFO]  E01: Iniciando (tentativa 1/3)
[20:01:01] [INFO]  E01: Lendo Rooms do modelo...
[20:01:02] [INFO]  E01: 12 Rooms encontrados
[20:01:02] [INFO]  E01: 4 Rooms descartados (sem Location ou Area=0)
[20:01:02] [INFO]  E01: 8 Rooms válidos em 2 Levels
[20:01:02] [INFO]  E01: Validação automática: 0 erros, 0 alertas
[20:01:02] [INFO]  E01: Status → WaitingApproval
[20:03:15] [INFO]  E01: Aprovado pelo usuário
[20:03:15] [INFO]  E01: Status → Completed (1 tentativa, 2.0s execução)
```

### 10.3 Metricas por etapa

```csharp
public class StageMetrics
{
    public string StageId { get; set; }
    public int AttemptCount { get; set; }
    public long ExecutionMs { get; set; }
    public long WaitingApprovalMs { get; set; }
    public int ElementsProcessed { get; set; }
    public int ElementsCreated { get; set; }
    public int ErrorsCritical { get; set; }
    public int ErrorsMedium { get; set; }
    public int ErrorsLight { get; set; }
    public string Executor { get; set; } // "Core", "Dynamo", "unMEP", "RevitAPI"
}
```

---

## 11. Persistência de Estado

### 11.1 Onde salvar

```
%APPDATA%/HidraulicoPlugin/sessions/{modelo_hash}/
├── pipeline_state.json          ← Estado atual
├── pipeline_state_backup.json   ← Backup anterior
└── logs/
    └── log_{timestamp}.json     ← Logs da sessão
```

### 11.2 Quando salvar

| Evento | Salvar |
|--------|--------|
| Pipeline iniciada | ✅ (criar arquivo) |
| Etapa muda de status | ✅ |
| Etapa concluída | ✅ |
| Erro capturado | ✅ |
| Plugin fechado | ✅ |
| Revit fechado normalmente | ✅ |

### 11.3 Como retomar

```csharp
public void ResumeOrStart(string modelName)
{
    string statePath = GetStatePath(modelName);
    
    if (File.Exists(statePath))
    {
        var savedState = JsonConvert.DeserializeObject<PipelineState>(
            File.ReadAllText(statePath));
        
        // Encontrar última etapa Completed
        var lastCompleted = savedState.Stages
            .Where(s => s.Value.Status == StageStatus.Completed)
            .OrderBy(s => s.Key)
            .LastOrDefault();
        
        _log.Log(ValidationLevel.Info, 
            $"Retomando pipeline. Última etapa: {lastCompleted.Key}");
        
        // Resetar etapas Running/WaitingApproval (interrompidas)
        foreach (var stage in savedState.Stages
            .Where(s => s.Value.Status == StageStatus.Running 
                     || s.Value.Status == StageStatus.WaitingApproval))
        {
            stage.Value.Status = StageStatus.Pending;
            _log.Log(ValidationLevel.Light, 
                $"{stage.Key}: resetado para Pending (interrompido)");
        }
        
        _state = savedState;
    }
    else
    {
        _state = PipelineState.CreateNew(modelName);
    }
    
    SaveState();
}
```

---

## 12. Estrutura Técnica do Orquestrador

### 12.1 Classes principais

```
HidraulicoPlugin.Core/
├── Pipeline/
│   ├── IPipelineEngine.cs           ← Interface do orquestrador
│   ├── PipelineEngine.cs            ← Implementação principal
│   ├── PipelineState.cs             ← Estado global
│   ├── StageState.cs                ← Estado de 1 etapa
│   ├── StageResult.cs               ← Resultado de execução
│   ├── StageMetrics.cs              ← Métricas
│   ├── StageDefinition.cs           ← Metadados estáticos da etapa
│   └── Stages/
│       ├── IStage.cs                ← Interface de etapa
│       ├── DetectionStage.cs        ← E01
│       ├── ClassificationStage.cs   ← E02
│       ├── PointIdentificationStage.cs ← E03
│       ├── EquipmentInsertionStage.cs  ← E04
│       ├── EquipmentValidationStage.cs ← E05
│       ├── RiserCreationStage.cs    ← E06
│       ├── ColdWaterNetworkStage.cs ← E07
│       ├── SewerNetworkStage.cs     ← E08
│       ├── SlopeApplicationStage.cs ← E09
│       ├── MepSystemStage.cs        ← E10
│       ├── SizingStage.cs           ← E11
│       ├── ScheduleStage.cs         ← E12
│       └── SheetStage.cs            ← E13
```

### 12.2 Interfaces

```csharp
public interface IPipelineEngine
{
    PipelineState State { get; }
    bool CanExecute(string stageId);
    StageResult ExecuteStage(string stageId);
    void ApproveStage(string stageId, string note);
    void RejectStage(string stageId, string reason);
    void RollbackStage(string stageId);
    void ResumeOrStart(string modelName);
}

public interface IStage
{
    string Id { get; }
    string Name { get; }
    string[] Dependencies { get; }
    bool RequiresApproval { get; }
    StageResult Execute();
    void Rollback();
}
```

### 12.3 Implementação simplificada do PipelineEngine

```csharp
public class PipelineEngine : IPipelineEngine
{
    private readonly Dictionary<string, IStage> _stages;
    private readonly ILogService _log;
    private PipelineState _state;

    public PipelineEngine(IEnumerable<IStage> stages, ILogService log)
    {
        _stages = stages.ToDictionary(s => s.Id);
        _log = log;
    }

    public bool CanExecute(string stageId)
    {
        var stage = _stages[stageId];
        var state = _state.Stages[stageId];
        
        if (state.Status == StageStatus.Running || 
            state.Status == StageStatus.Completed)
            return false;

        return stage.Dependencies.All(dep => 
            _state.Stages[dep].Status == StageStatus.Completed);
    }

    public StageResult ExecuteStage(string stageId)
    {
        if (!CanExecute(stageId))
            throw new InvalidOperationException($"Etapa {stageId} não pode ser executada");

        var stage = _stages[stageId];
        var stageState = _state.Stages[stageId];
        stageState.Status = StageStatus.Running;
        stageState.StartedAt = DateTime.Now;
        stageState.AttemptCount++;

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var result = stage.Execute();
            sw.Stop();

            stageState.LastResult = result;
            stageState.Status = result.HasCriticalBlocker
                ? StageStatus.Failed
                : stage.RequiresApproval
                    ? StageStatus.WaitingApproval
                    : StageStatus.Completed;

            if (stageState.Status == StageStatus.Completed)
                stageState.CompletedAt = DateTime.Now;

            SaveState();
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            stageState.Status = StageStatus.Failed;
            _log.Log(ValidationLevel.Critical, $"{stageId}: {ex.Message}");
            SaveState();
            return StageResult.Error(ex);
        }
    }

    public void ApproveStage(string stageId, string note)
    {
        var state = _state.Stages[stageId];
        if (state.Status != StageStatus.WaitingApproval)
            throw new InvalidOperationException("Etapa não está aguardando aprovação");

        state.Status = StageStatus.Completed;
        state.CompletedAt = DateTime.Now;
        state.ApprovalNote = note;
        SaveState();

        _log.Log(ValidationLevel.Info, $"{stageId}: Aprovado — {note}");
    }

    public void RejectStage(string stageId, string reason)
    {
        var state = _state.Stages[stageId];
        state.Status = StageStatus.Failed;
        _log.Log(ValidationLevel.Medium, $"{stageId}: Rejeitado — {reason}");
        SaveState();
    }

    public void RollbackStage(string stageId)
    {
        _stages[stageId].Rollback();
        _state.Stages[stageId].Status = StageStatus.RolledBack;
        
        // Também rollback etapas dependentes
        var dependents = _stages.Values
            .Where(s => s.Dependencies.Contains(stageId))
            .Where(s => _state.Stages[s.Id].Status == StageStatus.Completed);
        
        foreach (var dep in dependents)
        {
            dep.Rollback();
            _state.Stages[dep.Id].Status = StageStatus.RolledBack;
            _log.Log(ValidationLevel.Info, $"{dep.Id}: Rollback cascateado de {stageId}");
        }
        
        SaveState();
    }
}
```

---

## 13. Riscos e Limitações

### 13.1 Riscos

| Risco | Probabilidade | Impacto | Mitigação |
|-------|-------------|---------|-----------|
| Revit trava durante etapa | Média | Alto — perda de estado | Persistência a cada transição |
| Dynamo não retorna output | Baixa | Médio — timeout | Fallback + retry |
| unMEP cria elementos indesejados | Média | Médio — poluição do modelo | Snapshot + undo |
| Etapa demorada bloqueia UI | Alta | Médio — frustração | ExternalEvent + indicador de progresso |
| Rollback falha (Transaction perdida) | Baixa | Alto — modelo inconsistente | TransactionGroup; backup manual do .rvt |
| Usuário fecha Revit sem aprovar | Média | Baixo — retomável | Persistência de estado; resume na reabertura |

### 13.2 Limitações

```
1. EXECUÇÃO PARCIAL
   Se E07 falha, E08 pode executar (independentes entre si).
   Mas E09 (declividade) espera E08.
   → Pipeline não é 100% linear — tem dependências paralelas (E07 ∥ E08).

2. ROLLBACK DE DYNAMO
   Dynamo commita transactions internamente.
   → Rollback = deletar ElementIds manualmente (não "undo" nativo).

3. ROLLBACK DE unMEP
   unMEP commita transactions internamente.
   → Rollback = deletar novos ElementIds (via snapshot delta).

4. MULTI-LEVEL
   Algumas etapas se repetem por Level (E04, E07, E08).
   → Orquestrador executa por Level dentro da etapa (loop interno).

5. TEMPO DE APROVAÇÃO
   Usuário pode demorar para aprovar.
   → Plugin salva estado; retomável na próxima sessão.

6. MODELO MODIFICADO EXTERNAMENTE
   Se usuário editar modelo entre etapas, estado pode ficar incoerente.
   → Etapas com "re-scan" opcional (E01 e E05 podem reexecutar).
```
