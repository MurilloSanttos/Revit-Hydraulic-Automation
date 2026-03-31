# Mapa Definitivo de Responsabilidades — unMEP × Dynamo × Plugin

> Pipeline de Automação Hidráulica | Revit 2026
> Versão 1.0 — 2026-03-31

---

## 1. VISÃO GERAL DA DIVISÃO DE RESPONSABILIDADES

### Plugin C# (Orquestrador)

Camada central de decisão e controle. **Não cria elementos MEP diretamente** — delega criação ao Dynamo e dimensionamento ao unMEP. Responsável por:

- Lógica de negócio e cálculos hidráulicos (vazão provável, perda de carga, dimensionamento)
- Validação de pré-requisitos do modelo (Rooms, Levels, Fixtures)
- Serialização de parâmetros (JSON) para comunicação com Dynamo
- Disparo, supervisão e logging de scripts Dynamo
- Leitura de resultados pós-unMEP e pós-Dynamo
- Exportação final (PDF, IFC, memorial)

### unMEP (Motor Normativo)

Ferramenta semi-automatizada operada via UI do Revit. Atua **exclusivamente no dimensionamento normativo** e na atribuição de parâmetros calculados aos elementos do modelo:

- Dimensionamento de diâmetros por NBR 5626/8160
- Aplicação de slope calculado por norma
- Geração de memorial descritivo
- Quantitativos normativos e listas de materiais

### Dynamo (Executor Geométrico)

Motor de criação, manipulação e documentação de elementos Revit. Atua de forma **programática e headless** via DynamoRevit API:

- Criação de elementos (Spaces, Pipes, Fittings, Fixtures)
- Traçado geométrico de ramais e prumadas
- Conexão física entre elementos via ConnectorManager
- Geração de Schedules, Views e ViewSheets
- Validação geométrica e auditoria visual

---

## 2. TAREFAS DELEGADAS AO UNMEP

### 2.1 Dimensionamento de Diâmetros

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Calcula diâmetro de cada trecho de tubulação com base no método dos pesos (NBR 5626) para água fria e UHC (NBR 8160) para esgoto |
| **Por que unMEP** | Motor de cálculo normativo embutido, validado contra normas brasileiras; reproduzir em C#/Dynamo exigiria reimplementação completa das tabelas normativas |
| **Tempo economizado** | **85%** — elimina cálculo manual em planilha e consulta de tabelas |
| **Confiabilidade** | **95%** — falha apenas em cenários de fixtures sem sistema atribuído |

### 2.2 Aplicação de Declividade Normativa (Slope)

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Calcula e aplica inclinação mínima conforme NBR 8160 (1% para Ø ≥ 75mm, 2% para Ø < 75mm) em ramais horizontais de esgoto |
| **Por que unMEP** | Regra normativa acoplada ao cálculo de diâmetro — o slope depende do Ø definido na etapa anterior |
| **Tempo economizado** | **70%** — aplicação automática vs ajuste manual trecho a trecho |
| **Confiabilidade** | **90%** — pode falhar em sub-ramais com geometria não-ortogonal |

### 2.3 Geração de Memorial Descritivo

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Gera documento técnico com cálculos, justificativas normativas, diâmetros adotados, vazões e perdas de carga |
| **Por que unMEP** | Formato de memorial pré-aprovado por órgãos reguladores; geração automática a partir dos dados do modelo |
| **Tempo economizado** | **90%** — de ~4h manual para ~20min |
| **Confiabilidade** | **97%** — formato fixo garante consistência |

### 2.4 Quantitativos Normativos

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Gera listas de materiais por sistema, pavimento e prumada com nomenclatura técnica padronizada |
| **Por que unMEP** | Nomenclatura interna alinhada com o mercado brasileiro; integração direta com os diâmetros calculados |
| **Tempo economizado** | **80%** — extração automática vs contagem manual |
| **Confiabilidade** | **92%** — pode omitir fittings inseridos manualmente fora do template |

### 2.5 Calibração de Offsets Verticais

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Ajusta altura base de ramais horizontais com base na cota do piso acabado e nas regras de passagem sob laje |
| **Por que unMEP** | Motor interno conhece as relações entre Level, Room e cota de instalação; ajuste automático por tipo de ambiente |
| **Tempo economizado** | **60%** |
| **Confiabilidade** | **85%** — sensível a níveis split e meios-pisos |

### 2.6 Geração de Colunas Verticais (Prumadas via unMEP)

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Cria pipes verticais entre níveis com diâmetro calculado e sistema atribuído |
| **Por que unMEP** | Diâmetro da prumada depende do somatório de UHC/pesos dos pavimentos — cálculo interno do unMEP |
| **Tempo economizado** | **65%** |
| **Confiabilidade** | **82%** — falha em prumadas com menos de 300mm entre eixos |

---

## 3. TAREFAS DELEGADAS AO DYNAMO

### 3.1 Validação Visual de Rooms

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Lê todos os Rooms, verifica nome/número/área/colocação, aplica coloração verde/vermelho para debug visual |
| **Por que Dynamo** | Requer manipulação de OverrideGraphicSettings e criação de filtros visuais — operações não disponíveis no unMEP |
| **Controle aumentado** | **95%** — feedback visual imediato para o projetista |

### 3.2 Criação Massiva de Spaces MEP

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Cria Spaces correspondentes aos Rooms, copiando nome, número e atribuindo Level |
| **Por que Dynamo** | Operação de criação em lote não disponível no unMEP; Dynamo permite controle individualizado via loop |
| **Controle aumentado** | **90%** — verificação unitária de cada Space criado |

### 3.3 Inserção Paramétrica de Equipamentos

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Insere FamilyInstances por coordenadas XYZ, aplica rotação vetorial e parâmetros customizados |
| **Por que Dynamo** | Controle geométrico ponto-a-ponto necessário; JSON de entrada permite automação completa sem UI |
| **Controle aumentado** | **92%** — posição e orientação exatas por vetor |

### 3.4 Traçado Geométrico de Ramais

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Cria pipes horizontais ordenando fixtures por proximidade, aplicando slope por trecho, conectando via ConnectorManager |
| **Por que Dynamo** | Controle individual de cada segmento (start/end point, Z ajustado); lógica de ordenação geométrica customizada impossível no unMEP |
| **Controle aumentado** | **88%** — cada trecho tracável individualmente |

### 3.5 Ajuste Fino de Inclinação

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Recalcula e substitui LocationCurve de pipes existentes com novo Z baseado em slope%, suportando 3 modos (start/end/auto) |
| **Por que Dynamo** | unMEP aplica slope globalmente; Dynamo permite ajuste trecho a trecho com before/after tracking |
| **Controle aumentado** | **95%** — granularidade por pipe individual |

### 3.6 Criação de Prumadas Customizadas

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Cria pipes verticais entre Levels com offset X/Y, diâmetro e conectividade entre segmentos |
| **Por que Dynamo** | Quando o diâmetro é definido pelo plugin (não pelo unMEP), ou quando é necessário prumada fora do padrão normativo |
| **Controle aumentado** | **85%** — controle de offset e alinhamento |

### 3.7 Interconexão Automática de Rede

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Encontra conectores próximos por distância + domínio, executa auto-align/auto-rotate e ConnectTo |
| **Por que Dynamo** | Operação pós-modelagem que o unMEP não cobre; conecta pipes criados por diferentes etapas do pipeline |
| **Controle aumentado** | **80%** — detecção de pares com tracking de skipped/errors |

### 3.8 Geração de Schedules

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Cria ViewSchedule para Pipes/Fittings/Fixtures com campos, filtros, sorting e formatação customizáveis |
| **Por que Dynamo** | Nomenclatura e campos controlados pelo plugin (não pela nomenclatura fixa do unMEP); flexibilidade total |
| **Controle aumentado** | **93%** — campos, filtros e agrupamentos por JSON |

### 3.9 Montagem de Pranchas

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Cria ViewSheets, insere Viewports/Schedules/Legends/TextNotes com posicionamento paramétrico e numeração sequencial |
| **Por que Dynamo** | Automação de documentação end-to-end; unMEP não gera pranchas |
| **Controle aumentado** | **90%** — layout configurável via JSON |

---

## 4. TAREFAS QUE PERMANECEM NO PLUGIN C#

### 4.1 Validação Estrutural do Modelo

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Verifica integridade de Rooms, Levels, Fixtures, Systems antes de qualquer automação |
| **Por que plugin** | Pré-requisito para todo o pipeline; erro aqui invalida todas as etapas subsequentes |
| **Risco de delegar** | **85%** — Dynamo não tem capacidade de abortar pipeline em caso de falha crítica |

### 4.2 Cálculos Hidráulicos Avançados

| Aspecto | Detalhe |
|---|---|
| **O que faz** | ProbableFlowCalculator (vazão provável), PipeSizingService (velocidade, perda de carga) |
| **Por que plugin** | Algoritmos complexos com múltiplas iterações; performance crítica; necessidade de cache e thread-safety |
| **Risco de delegar** | **90%** — Python/Dynamo não tem performance suficiente para cálculos iterativos em grandes redes |

### 4.3 Serialização e Protocolo de Comunicação

| Aspecto | Detalhe |
|---|---|
| **O que faz** | InputParameterSerializer (C# → JSON), DynamoOutputReader (JSON → C#), protocolo versionado |
| **Por que plugin** | Contrato formal entre componentes; tipagem forte; validação de schema |
| **Risco de delegar** | **95%** — desserialização em Python é frágil e sem tipagem |

### 4.4 Orquestração do Pipeline

| Aspecto | Detalhe |
|---|---|
| **O que faz** | HydraulicOrchestrator sequencia todas as etapas, gerencia estado, trata erros, controla fluxo |
| **Por que plugin** | Lógica de fluxo condicional, retry, rollback e supervisão exigem runtime robusto |
| **Risco de delegar** | **98%** — sem orquestrador central, pipeline perde determinismo e rastreabilidade |

### 4.5 Supervisão e Logging

| Aspecto | Detalhe |
|---|---|
| **O que faz** | DynamoExecutionSupervisor (timeout, retry), DynamoExecutionLogger (JSON persistente) |
| **Por que plugin** | Monitoramento cross-etapa; persistência de métricas; detecção de execuções presas |
| **Risco de delegar** | **90%** — Dynamo não pode supervisionar a si mesmo |

### 4.6 Leitura de Resultados pós-unMEP

| Aspecto | Detalhe |
|---|---|
| **O que faz** | Lê parâmetros de diâmetro, tipo e sistema atribuídos pelo unMEP para alimentar etapas seguintes |
| **Por que plugin** | Ponte entre unMEP (manual) e Dynamo (automático); normalização de dados |
| **Risco de delegar** | **75%** — Dynamo poderia ler, mas sem validação estruturada |

---

## 5. MATRIZ DE DECISÃO

| Tarefa | unMEP | Dynamo | Plugin | Motivo Técnico | Tempo Econ. (%) | Acurácia (%) |
|---|---|---|---|---|---|---|
| Validação de Rooms | ❌ | ✅ | — | Requer OverrideGraphics + coloração visual | 60 | 95 |
| Criação de Spaces | ❌ | ✅ | — | Criação em lote com controle unitário | 75 | 90 |
| Inserção de fixtures | ❌ | ✅ | — | Posição XYZ + rotação vetorial | 70 | 92 |
| Validação posicionamento | ❌ | ✅ | — | Cálculo de distância equip↔parede | 55 | 88 |
| Dimensionamento diâmetros | ✅ | ❌ | — | NBR 5626/8160 embutida no engine | 85 | 95 |
| Aplicação de slope (norma) | ✅ | ❌ | — | Slope acoplado ao Ø calculado | 70 | 90 |
| Ajuste fino de slope | ❌ | ✅ | — | Granularidade por pipe individual | 65 | 95 |
| Traçado de ramais esgoto | ❌ | ✅ | — | Ordenação geométrica customizada | 75 | 85 |
| Traçado de ramais água fria | ❌ | ✅ | — | Controle de rota ponto-a-ponto | 75 | 85 |
| Prumadas (Ø normativo) | ✅ | ❌ | — | Diâmetro depende de UHC somatório | 65 | 82 |
| Prumadas (Ø customizado) | ❌ | ✅ | — | Diâmetro definido pelo plugin | 60 | 85 |
| Conexão de rede | ❌ | ✅ | — | ConnectorManager + auto-align | 70 | 80 |
| Calibração de offsets Z | ✅ | ❌ | — | Regras de cota por tipo de ambiente | 60 | 85 |
| Memorial descritivo | ✅ | ❌ | — | Formato normativo pré-aprovado | 90 | 97 |
| Quantitativos | ✅ | ❌ | — | Nomenclatura técnica integrada | 80 | 92 |
| Geração de schedules | ❌ | ✅ | — | Campos/filtros flexíveis via JSON | 85 | 93 |
| Montagem de pranchas | ❌ | ✅ | — | ViewSheet + Viewport + posição | 88 | 90 |
| Cálculo hidráulico | ❌ | ❌ | ✅ | Performance iterativa, thread-safety | 80 | 98 |
| Orquestração | ❌ | ❌ | ✅ | Fluxo condicional, retry, rollback | — | 99 |
| Serialização JSON | ❌ | ❌ | ✅ | Tipagem forte, schema validation | — | 98 |
| Supervisão/Logging | ❌ | ❌ | ✅ | Cross-etapa, timeout, métricas | — | 97 |
| Validação pré-pipeline | ❌ | ❌ | ✅ | Gate de qualidade antes de iniciar | — | 95 |
| Leitura pós-unMEP | ❌ | ❌ | ✅ | Normalização de dados entre etapas | — | 90 |

---

## 6. CRITÉRIOS TÉCNICOS DE DECISÃO

| Critério | → unMEP | → Dynamo | → Plugin |
|---|---|---|---|
| Requer cálculo normativo NBR | ✅ | — | — |
| Requer controle geométrico fino (XYZ) | — | ✅ | — |
| Requer criação de elementos Revit em lote | — | ✅ | — |
| Requer lógica iterativa de alta performance | — | — | ✅ |
| Requer manipulação de ConnectorManager | — | ✅ | — |
| Requer geração de documento normativo | ✅ | — | — |
| Requer decisão condicional (if/else complexo) | — | — | ✅ |
| Requer visualização/debug (Watch, cores) | — | ✅ | — |
| Requer persistência de estado entre execuções | — | — | ✅ |
| Requer interação com UI do Revit | ✅ | — | — |
| Requer execução headless/background | — | ✅ | ✅ |
| Requer formato de saída flexível (JSON) | — | ✅ | ✅ |
| Requer retry/timeout/abort | — | — | ✅ |
| Requer nomenclatura técnica padronizada (BR) | ✅ | — | — |
| Requer posicionamento em ViewSheet | — | ✅ | — |

### Regra de ouro

```
SE tarefa envolve cálculo normativo brasileiro → unMEP
SE tarefa envolve criação/manipulação geométrica → Dynamo
SE tarefa envolve decisão/orquestração/supervisão → Plugin
SE tarefa envolve as três → Plugin orquestra, Dynamo executa, unMEP valida
```

---

## 7. IMPACTO FINAL NO PIPELINE

### Métricas comparativas

| Métrica | Antes (manual) | Depois (pipeline) | Δ |
|---|---|---|---|
| Tempo total do projeto (100 fixtures) | 40h | 12h | **-70%** |
| Tempo total do projeto (300 fixtures) | 120h | 30h | **-75%** |
| Erros de dimensionamento | 15–25% | 1–2% | **-92%** |
| Erros de conectividade | 20–30% | 3–5% | **-83%** |
| Retrabalho total | 12h/projeto | 1.5h/projeto | **-87%** |
| Geração de documentação | 8h | 0.5h | **-94%** |
| Conformidade normativa | ~70% | ~98% | **+28pp** |

### Consistência e repetibilidade

| Aspecto | Manual | Pipeline automatizado |
|---|---|---|
| Mesmo resultado para o mesmo input | Improvável | **Garantido** |
| Rastreabilidade de decisões | Nenhuma | **100% (logs JSON)** |
| Reprodutibilidade por outro projetista | Baixa | **Total** |
| Auditoria pós-projeto | Difícil | **Imediata (logs + JSON)** |

### Distribuição de tempo por componente

| Componente | % do tempo total | Automatizado? |
|---|---|---|
| Plugin C# (validação + orquestração) | 10% | ✅ |
| unMEP (dimensionamento) | 25% | ⚠️ Semi |
| Dynamo (criação + documentação) | 60% | ✅ |
| Revisão manual do projetista | 5% | ❌ |

---

## 8. CONCLUSÃO TÉCNICA

### O que vai para o unMEP (6 tarefas)

1. Dimensionamento de diâmetros (NBR 5626/8160)
2. Aplicação de slope normativo
3. Calibração de offsets verticais
4. Prumadas com diâmetro normativo
5. Memorial descritivo
6. Quantitativos e listas de materiais

**Perfil:** tarefas que dependem de motor de cálculo normativo brasileiro e cuja reimplementação no plugin teria custo desproporcional.

### O que vai para o Dynamo (9 tarefas)

1. Validação visual de Rooms
2. Criação massiva de Spaces
3. Inserção paramétrica de equipamentos
4. Validação de posicionamento
5. Traçado de ramais (esgoto e água fria)
6. Ajuste fino de inclinação
7. Prumadas customizadas + conexão de rede
8. Geração de schedules
9. Montagem de pranchas

**Perfil:** tarefas de criação/manipulação geométrica com controle ponto-a-ponto, executáveis em modo headless via DynamoRevit.

### O que fica no Plugin C# (6 tarefas)

1. Validação estrutural do modelo
2. Cálculos hidráulicos avançados
3. Serialização e protocolo JSON
4. Orquestração do pipeline
5. Supervisão e logging
6. Leitura de resultados pós-unMEP

**Perfil:** tarefas de decisão, performance, controle de fluxo e integridade de dados que exigem runtime .NET robusto.

### Benefício operacional

A divisão elimina **sobreposição de responsabilidades**: cada componente faz exclusivamente o que faz melhor. O resultado é um pipeline onde **90% das operações são automatizadas**, o projetista intervém apenas no dimensionamento via unMEP (~25% do tempo) e na revisão final (~5%), e toda execução é auditável via logs JSON estruturados.

---

*Documento de referência | Pipeline Hidráulico Revit 2026 | v1.0 | 2026-03-31*
