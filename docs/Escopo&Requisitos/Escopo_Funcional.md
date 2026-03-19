# Escopo Funcional do Sistema — Plugin Hidráulico Revit 2026

> Documento de definição de escopo funcional com delimitação precisa de responsabilidades entre Plugin, Dynamo, unMEP e Usuário.

---

## 1. Visão Geral do Escopo

### 1.1 Papel do Plugin

O plugin hidráulico é um **orquestrador semi-automático** que coordena a execução de projetos hidráulicos residenciais dentro do Autodesk Revit 2026. Ele não é um executor monolítico — distribui responsabilidades entre si mesmo (C#/Revit API), Dynamo (automação paramétrica) e unMEP (roteamento de redes).

### 1.2 Definição de "Orquestrador"

O sistema atua como cérebro de decisão:

| Função | Descrição |
|--------|-----------|
| **Analisa** | Lê o modelo, extrai dados, interpreta geometria |
| **Decide** | Aplica regras normativas, classifica, dimensiona |
| **Parametriza** | Define valores de entrada para execução (diâmetros, posições, inclinações) |
| **Dispara** | Aciona Dynamo ou unMEP com parâmetros pré-calculados |
| **Valida** | Verifica resultados, detecta erros, gera diagnósticos |
| **Reporta** | Gera logs estruturados, relatórios e feedback ao usuário |

O plugin **não** tenta fazer tudo sozinho. Ele delega a execução paramétrica pesada para Dynamo e o roteamento avançado de redes para unMEP.

### 1.3 Limites do Sistema

| Dentro do escopo | Fora do escopo |
|-----------------|----------------|
| Água fria com reservatório superior | Água quente |
| Esgoto sanitário com ventilação | Águas pluviais |
| Residencial (até 50 ambientes) | Comercial / Industrial |
| NBR 5626 e NBR 8160 | Normas internacionais |
| Automação de 70–80% do fluxo | 100% de automação |
| Uso interno | Produto comercial com licenciamento |
| Revit 2026 (.NET 8.0) | Versões anteriores do Revit |

---

## 2. Modelo de Responsabilidade

### 2.1 Plugin Core (C# — Revit API)

**Tipo de atuação:** Análise, decisão, validação, coordenação

| Responsabilidades | Detalhamento |
|-------------------|-------------|
| Leitura do modelo | Coleta Rooms, Spaces, Levels, fixtures existentes |
| Classificação de ambientes | Motor de NLP em português (40+ variações) |
| Cálculos hidráulicos | Vazão, diâmetro, perda de carga, pressão, UHC |
| Validação de dados | Duplicatas, áreas, cobertura, connectors, posições |
| Tomada de decisão | Quais equipamentos, onde posicionar, quais diâmetros |
| Gerenciamento de Transactions | Criação controlada de elementos no Revit |
| Geração de logs | Logging estruturado com 4 níveis |
| Interface WPF | Configuração, execução, diagnóstico |
| Orquestração do pipeline | Sequência de 11 etapas com pré-condições |
| Comunicação com Dynamo | Serialização JSON, disparo e leitura de resultados |

**Limitações:**
- Não executa roteamento geométrico avançado (delega ao Dynamo/unMEP)
- Não manipula geometria complexa de famílias
- Operações de escrita limitadas ao contexto de Transaction

---

### 2.2 Dynamo

**Tipo de atuação:** Execução paramétrica, manipulação geométrica em massa

| Responsabilidades | Detalhamento |
|-------------------|-------------|
| Criação em massa de Spaces | Quando quantidade justifica batch |
| Inserção paramétrica de equipamentos | Posicionamento com lógica geométrica |
| Traçado de ramais | Redes horizontais com lógica de rota |
| Aplicação de inclinação em massa | Ajuste de tubulações de esgoto |
| Criação de schedules complexas | Tabelas com formatação avançada |
| Montagem de pranchas | Layout automático de views e schedules |

**Limitações:**
- Não toma decisões — recebe parâmetros pré-calculados pelo plugin
- Não valida regras normativas — apenas executa
- Performance pode ser lenta em modelos grandes
- Roteamento limitado a rotas simples (sem pathfinding avançado)
- Requer que o modelo tenha famílias compatíveis carregadas

---

### 2.3 unMEP

**Tipo de atuação:** Roteamento automático de redes, pathfinding avançado

| Responsabilidades | Detalhamento |
|-------------------|-------------|
| Roteamento de rede de água fria | Traçado otimizado de tubulações com desvio de obstáculos |
| Roteamento de rede de esgoto | Traçado com convergência para tubos de queda |
| Conexão automática de trechos | Inserção de fittings em rotas complexas |
| Otimização de trajeto | Menor comprimento, menor número de curvas |

**Limitações:**
- Não dimensiona — recebe diâmetros do plugin
- Não aplica regras normativas — apenas roteia
- Configuração inicial pode exigir adaptação ao padrão do projeto
- Pode gerar rotas que necessitam ajuste manual
- Dependente de connectors bem configurados nas famílias

---

### 2.4 Usuário

**Tipo de atuação:** Validação, decisão final, correção manual, configuração

| Responsabilidades | Detalhamento |
|-------------------|-------------|
| Preparação do modelo | Rooms nomeados, Levels corretos, famílias carregadas |
| Configuração do plugin | Parâmetros hidráulicos, seleção de norma |
| Validação entre etapas | Aprovar/rejeitar resultado de cada etapa |
| Reclassificação manual | Corrigir classificações com confiança baixa |
| Aprovação de criação de Spaces | Confirmar criação automática |
| Revisão de posicionamento | Validar/ajustar posição de equipamentos |
| Correção de rotas | Ajustar manualmente rotas problemáticas |
| Revisão de dimensionamento | Verificar cálculos contra referência |
| Revisão final de pranchas | Layout, textos, numeração |
| Aprovação para produção | Validar projeto completo antes de emissão |

**Limitações:**
- Necessita conhecimento básico de Revit
- Necessita conhecimento de projeto hidráulico para validação
- Correções manuais no modelo podem exigir refazer etapas do plugin

---

## 3. Escopo Funcional por Módulo

---

### Módulo 01 — Detecção de Ambientes (Rooms + Spaces)

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Leitura de Rooms via API (`FilteredElementCollector`). Filtragem de inválidos (sem Location, área zero). Extração de metadados (nome, número, nível, área, perímetro, ponto central). Conversão de unidades (ft → m). Leitura de Spaces existentes. Correspondência Room↔Space por proximidade (0.5m). Identificação de Rooms sem Space e Spaces órfãos. |
| **Dynamo** | Criação em massa de Spaces quando quantidade > 20 (script `02_CriarSpacesMassivo.dyn`). Validação visual em 3D dos Rooms (script `01_ValidarRooms.dyn`). |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Garantir que Rooms estão definidos e nomeados no modelo. Confirmar criação de Spaces via TaskDialog. Resolver Rooms duplicados ou sobrepostos. |
| **Limitações** | Rooms não colocados são invisíveis ao sistema. Rooms sem nome geram classificação NaoIdentificado. Correspondência por proximidade pode falhar se Room e Space estiverem em posições muito diferentes. |

---

### Módulo 02 — Classificação de Ambientes

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Normalização de texto (lowercase, remoção de acentos, trim, remoção de numeração). Matching contra dicionário de 40+ padrões em 3 estratégias (exato, contém, parcial). Cálculo de confiança (0.0–1.0). Classificação em 8 tipos. Sinalização por faixa de confiança (confiável/validação/não identificado). |
| **Dynamo** | Não atua neste módulo (classificação é lógica pura, sem manipulação do modelo). |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Reclassificar ambientes com confiança < 70% via combobox na UI. Validar ambientes classificados como NaoIdentificado. Confirmar se sala, quarto, corredor realmente não são relevantes. |
| **Limitações** | Dependente da qualidade dos nomes dos Rooms. Nomes em outros idiomas não são reconhecidos. Abreviações não mapeadas no dicionário resultam em baixa confiança. Classificação não analisa conteúdo do Room (apenas nome). |

---

### Módulo 03 — Identificação de Pontos Hidráulicos

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Consulta tabela tipo→equipamentos. Listagem de pontos necessários por ambiente (tipo, conexão AF/ES, diâmetro, peso, altura). Cálculo de vazão por ponto (NBR 5626). Detecção de equipamentos existentes via API. Comparação existentes vs. necessários. Geração de lista de faltantes. |
| **Dynamo** | Não atua neste módulo. |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Validar se a tabela de pontos por ambiente está adequada ao projeto. Adicionar pontos extras não previstos (ex: filtro, torneira de banheira). |
| **Limitações** | Tabela de pontos é fixa por tipo de ambiente — variações dentro do mesmo tipo não são diferenciadas automaticamente. Equipamentos existentes são identificados por categoria do Revit, não por modelo específico. |

---

### Módulo 04 — Inserção de Equipamentos

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Verificação de famílias carregadas. Cálculo de posição ideal (parede disponível, offset, proximidade de prumada). Inserção via API (`Document.Create.NewFamilyInstance`). Rotação conforme orientação da parede. Validação de connectors pós-inserção. |
| **Dynamo** | Inserção paramétrica em massa quando quantidade > 10 (script `03_InserirEquipamentos.dyn`). Posicionamento com lógica geométrica avançada (script `04_ValidarPosicionamento.dyn`). |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Garantir que famílias MEP estão na biblioteca. Validar posição de equipamentos inseridos. Ajustar manualmente posições que colidiram. Confirmar inserção via UI. |
| **Limitações** | Posicionamento automático segue regras simplificadas (parede mais próxima). Ambientes com layout irregular podem exigir ajuste manual. Famílias sem connectors MEP impedem conexão com redes. |

---

### Módulo 05 — Validação de Equipamentos Existentes

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Detecção de tipo de família. Verificação de connectors (AF/ES). Verificação de distância da parede. Detecção de colisão simplificada. Classificação: Válido/Com Ressalva/Inválido. |
| **Dynamo** | Verificação de distâncias entre equipamentos (script `04_ValidarPosicionamento.dyn`). |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Resolver equipamentos classificados como Inválidos. Substituir famílias genéricas por famílias MEP com connectors. Ajustar posições com ressalva. |
| **Limitações** | Clash detection é simplificada (BoundingBox, não geometria exata). Famílias de terceiros podem ter connectors em posições não padronizadas. |

---

### Módulo 06 — Criação de Prumadas

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Clustering de ambientes por proximidade 2D. Cálculo de centroide para posição da prumada. Definição de tipos necessários (AF, ES, VE). Dimensionamento de diâmetro por carga acumulada. Criação de Pipe vertical via API. |
| **Dynamo** | Criação de prumadas complexas com múltiplas derivações (script `08_CriarPrumadas.dyn`). |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Validar posição das prumadas (evitar conflito com estrutura). Definir shafts quando existirem. Aprovar quantidade de prumadas. |
| **Limitações** | Detecção de colisão com estrutura é limitada. Posição ótima depende da qualidade do layout do modelo. Prumadas em shafts pré-definidos requerem indicação do usuário. |

---

### Módulo 07 — Geração de Rede de Água Fria

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Definição da topologia da rede (de onde para onde). Cálculo de vazão por trecho. Dimensionamento de diâmetro (velocidade ≤ 3 m/s). Cálculo de perda de carga. Verificação de pressão (≥ 3 m.c.a., ≤ 40 m.c.a.). Definição de pontos de registro. Atribuição ao PipingSystem. |
| **Dynamo** | Traçado físico de ramais horizontais (script `05_GerarRamalAguaFria.dyn`). Inserção de fittings. Conexão de trechos (script `09_ConectarRede.dyn`). |
| **unMEP** | Roteamento automático otimizado quando Dynamo não consegue resolver rotas complexas (desvio de estrutura, múltiplos pavimentos). |
| **Usuário** | Revisar rotas geradas (especialmente em plantas complexas). Ajustar manualmente trechos com interferência. Validar dimensionamento. |
| **Limitações** | Dynamo tem dificuldade com rotas que exigem desvio de múltiplos obstáculos. unMEP pode gerar rotas com curvas excessivas. Otimização de rota é aproximada, não global. |

---

### Módulo 08 — Geração de Rede de Esgoto

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Definição de topologia (ramais → tubo de queda → subcoletor). Dimensionamento por UHC. Definição de inclinação por diâmetro. Decisão de onde inserir CX sifonada, CX gordura, CX inspeção. Atribuição ao PipingSystem. |
| **Dynamo** | Traçado de ramais de descarga e esgoto (script `06_GerarRamalEsgoto.dyn`). Inserção de acessórios (caixas). Conexão de trechos. |
| **unMEP** | Roteamento do subcoletor com desvio de obstáculos. Roteamento de ramais quando geometria é complexa. |
| **Usuário** | Revisar posição de caixas de inspeção. Ajustar rotas de subcoletor. Validar convergência para tubo de queda. |
| **Limitações** | Traçado gravitacional exige atenção especial a elevações. Caixas de inspeção externas dependem de informação da implantação. |

---

### Módulo 09 — Aplicação de Inclinações

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Identificação de trechos horizontais de esgoto. Determinação de inclinação: 2% (DN ≤ 75mm), 1% (DN ≥ 100mm). Cálculo do novo Z do endpoint. |
| **Dynamo** | Ajuste físico de elevação de Pipes em massa (script `07_AplicarInclinacao.dyn`). Reajuste de fittings afetados. |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Verificar se inclinações não geraram interferência com laje/estrutura. Ajustar trechos onde não há espaço vertical suficiente. |
| **Limitações** | A aplicação de inclinação pode desconectar fittings — requer reconexão. Em lajes com pouca espessura, pode não haver espaço para a inclinação necessária. |

---

### Módulo 10 — Criação de Sistemas MEP

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Criação de 3 PipingSystems (AF, ES, VE). Atribuição de elementos via connectors. Validação de elementos sem sistema. Verificação de conectividade. Aplicação de cores por sistema. Nomenclatura padronizada. |
| **Dynamo** | Não atua neste módulo (criação de sistemas é operação direta da API). |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Verificar se todos os elementos estão atribuídos. Resolver descontinuidades apontadas pelo log. |
| **Limitações** | Elementos sem connectors compatíveis não podem ser atribuídos a sistemas. Pipe Types devem estar pré-configurados no template/modelo. |

---

### Módulo 11 — Dimensionamento Hidráulico

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | **Água Fria:** soma de pesos, fórmula Q = 0.3×√ΣP, seleção de diâmetro por V ≤ 3 m/s, perda de carga (Fair-Whipple-Hsiao), verificação de pressão. **Esgoto:** soma de UHC, tabela UHC→diâmetro (NBR 8160), regra de não diminuição, cálculo de declividade. |
| **Dynamo** | Não atua neste módulo (cálculos são lógica pura, sem manipulação do modelo). |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Validar resultados contra planilha de referência. Aprovar diâmetros selecionados. Verificar pressão em pontos críticos. |
| **Limitações** | Cálculo de perda de carga usa fórmula empírica (precisão ±10%). Perdas localizadas estimadas em 20% das perdas distribuídas (aproximação). Não considera simultaneidade avançada (usa método probabilístico simplificado). |

---

### Módulo 12 — Geração de Tabelas

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Criação de 4 ViewSchedules via API (tubulações, conexões, equipamentos, resumo). Configuração de campos, filtros, agrupamento. Formatação de unidades. |
| **Dynamo** | Criação de schedules com formatação complexa (script `10_GerarTabelas.dyn`). Exportação para Excel/CSV. |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Revisar conteúdo das tabelas. Ajustar formatação se necessário. Validar quantitativos. |
| **Limitações** | Schedules dependem de parâmetros existentes nos elementos. Formatação avançada (merge de células, etc.) é limitada pela API do Revit. |

---

### Módulo 13 — Geração de Pranchas

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Criação de View Templates (filtros por sistema). Duplicação de Floor Plans. Configuração de escala e crop. Criação de ViewSheets. Numeração sequencial (HID-01, HID-02). |
| **Dynamo** | Posicionamento automático de views nas pranchas (script `11_GerarPranchas.dyn`). Inserção de legendas e notas. Layout automático. |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Fornecer titleblock padrão. Revisar layout de cada prancha. Ajustar posição de views/tabelas. Adicionar notas específicas do projeto. Validar escala e legibilidade. |
| **Limitações** | Layout automático é aproximado — pranchas complexas exigem ajuste manual. Titleblock deve estar pré-configurado com campos corretos. Plantas de grande extensão podem exigir divisão manual. |

---

### Módulo 14 — Sistema de Logs e Diagnóstico

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Registro estruturado (timestamp, nível, etapa, componente, mensagem, ElementId). 4 níveis: Info, Leve, Médio, Crítico. Verificação de bloqueio (Crítico → impede avanço). Exportação para JSON. Resumo por nível. Filtragem por etapa. |
| **Dynamo** | Não atua neste módulo. |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Ler e interpretar logs na aba de Diagnóstico. Navegar a elementos com problemas. Decidir se aceita alertas Médios. |
| **Limitações** | Logs são acumulados em memória — modelos muito grandes podem exigir limpeza periódica. ElementId pode ser invalidado se o modelo for editado externamente entre etapas. |

---

### Módulo 15 — Interface do Plugin

| Aspecto | Detalhamento |
|---------|-------------|
| **Plugin automatiza** | Renderização da janela WPF com 3 abas. Binding MVVM com dados em tempo real. Validação de parâmetros com feedback visual. Controle de pipeline (habilitar/desabilitar etapas). Status visual por etapa. Persistência de configuração em JSON. |
| **Dynamo** | Não atua neste módulo. |
| **unMEP** | Não atua neste módulo. |
| **Usuário** | Configurar parâmetros hidráulicos. Executar etapas (individual ou sequencial). Aprovar/rejeitar etapas com alertas. Navegar no diagnóstico. Reclassificar ambientes. |
| **Limitações** | Operações de escrita no modelo exigem ExternalEvent (thread-safe). UI não pode manipular o modelo diretamente (restrição da API do Revit). |

---

## 4. Regras de Delegação

### 4.1 Quando o Plugin resolve sozinho (C# / Revit API)

| Critério | Exemplos |
|----------|---------|
| Operação de leitura do modelo | Ler Rooms, Spaces, Levels, fixtures |
| Lógica de decisão pura | Classificar ambiente, dimensionar, calcular |
| Operações simples de escrita | Criar 1 Space, inserir 1 equipamento |
| Criação de elementos organizacionais | PipingSystems, ViewSchedules |
| Validação e diagnóstico | Verificar connectors, áreas, duplicatas |
| UI e feedback | WPF, logs, relatórios |

### 4.2 Quando delegar ao Dynamo

| Critério | Exemplos |
|----------|---------|
| Operação em massa (batch) | Criar 20+ Spaces, inserir 10+ equipamentos |
| Manipulação geométrica paramétrica | Posicionar equipamentos por regras geométricas |
| Traçado de redes horizontais | Ramais de AF e ES com lógica de rota simples |
| Ajuste em massa de propriedades | Aplicar inclinação em todos os trechos de esgoto |
| Geração de documentação complexa | Pranchas com layout, schedules avançadas |

### 4.3 Quando delegar ao unMEP

| Critério | Exemplos |
|----------|---------|
| Roteamento com desvio de obstáculos | Rota que precisa contornar vigas, pilares |
| Otimização de trajeto | Menor comprimento com menor número de curvas |
| Roteamento entre pavimentos | Conexão vertical com ramificações complexas |
| Quando Dynamo falha no traçado | Rotas que Dynamo não consegue resolver |

### 4.4 Diagrama de Decisão

```
                    ┌─────────────────┐
                    │  Tarefa a       │
                    │  executar       │
                    └────────┬────────┘
                             │
                    ┌────────▼────────┐
                    │  É leitura ou   │── Sim ──▶ Plugin (C#)
                    │  cálculo?       │
                    └────────┬────────┘
                             │ Não
                    ┌────────▼────────┐
                    │  É operação     │── Sim ──▶ Plugin (C#)
                    │  simples (≤5)?  │
                    └────────┬────────┘
                             │ Não
                    ┌────────▼────────┐
                    │  Precisa de     │── Sim ──▶ unMEP
                    │  pathfinding?   │
                    └────────┬────────┘
                             │ Não
                    ┌────────▼────────┐
                    │  É operação     │── Sim ──▶ Dynamo
                    │  em massa ou    │
                    │  paramétrica?   │
                    └────────┬────────┘
                             │ Não
                             ▼
                        Plugin (C#)
```

---

## 5. Limitações Conhecidas

### 5.1 Limitações do Dynamo

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Performance degrada em modelos > 200 MB | Etapas de rede podem levar > 2 minutos | Dividir execução por pavimento |
| Roteamento sem pathfinding avançado | Rotas podem cruzar elementos de estrutura | Delegar ao unMEP quando necessário |
| Não possui lógica de decisão | Não classifica, não dimensiona | Plugin calcula e Dynamo executa |
| Scripts .dyn não são versionáveis como código | Dificuldade de manutenção | Documentar inputs/outputs em JSON |

### 5.2 Limitações do unMEP

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Pode gerar rotas com curvas excessivas | Custo de material e perda de carga aumentam | Validação do plugin + ajuste do usuário |
| Configuração inicial é complexa | Tempo de setup para cada novo projeto | Template de configuração padrão |
| Não respeita regras específicas de norma | Pode rotear sem considerar declividade | Plugin aplica inclinação depois |
| Resultado pode ser inesperado | Rotas não intuitivas | Revisão obrigatória do usuário |

### 5.3 Limitações do modelo

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Rooms sem nome ou mal nomeados | Classificação falha | Validação pré-execução + alerta |
| Famílias sem connectors MEP | Equipamentos não conectáveis | Verificação na etapa de validação |
| Famílias de terceiros não padronizadas | Connectors em posições erradas | Biblioteca de famílias padrão |
| Modelo sem Levels definidos | Impossível criar Spaces e prumadas | Erro Crítico no pré-diagnóstico |
| Paredes não fechadas | Rooms com área zero | Filtragem e log |
| Modelo corrompido ou muito pesado | Lentidão ou crash | Validação prévia |

### 5.4 Limitações de cálculo

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Perdas localizadas estimadas (20%) | Precisão ±10% | Aceito por norma |
| Simultaneidade simplificada | Pode superdimensionar | Conservador — seguro |
| Não considera válvulas redutoras | Pressão alta não tratada automaticamente | Alerta ao usuário |

---

## 6. Intervenções do Usuário

### 6.1 Intervenções Obrigatórias

| Etapa | Intervenção | Momento |
|-------|-------------|---------|
| Pré-execução | Preparar modelo com Rooms nomeados, Levels e famílias | Antes de iniciar |
| Configuração | Definir parâmetros hidráulicos na aba de Configuração | Antes de iniciar |
| Módulo 01 | Confirmar criação de Spaces (TaskDialog) | Durante execução |
| Módulo 02 | Reclassificar ambientes com confiança < 70% | Após classificação |
| Módulo 04 | Validar posição de equipamentos inseridos | Após inserção |
| Módulo 06 | Validar posição de prumadas | Após criação |
| Módulo 07 | Revisar rotas de água fria | Após geração |
| Módulo 08 | Revisar rotas de esgoto | Após geração |
| Módulo 09 | Verificar interferências de inclinação | Após aplicação |
| Módulo 13 | Revisar layout de pranchas | Após geração |
| Todas | Aprovar/rejeitar etapa antes de avançar (quando há alertas Médios) | Entre etapas |

### 6.2 Intervenções Opcionais

| Intervenção | Quando aplicável |
|-------------|-----------------|
| Ajustar rotas manualmente | Quando automação gera rota subótima |
| Adicionar pontos extras | Equipamentos não previstos na tabela padrão |
| Editar classificação | Quando nome do ambiente é ambíguo |
| Exportar logs | Para análise ou documentação externa |
| Re-executar etapa | Quando resultado não é aceito |

### 6.3 Correções Manuais Esperadas

| Cenário | Ação do Usuário |
|---------|----------------|
| Equipamento colide com outro elemento | Mover manualmente no Revit |
| Rota de tubulação cruza estrutura | Ajustar rota ou solicitar re-roteamento |
| Prumada na posição errada | Mover para shaft correto |
| Prancha com layout inadequado | Reposicionar views/schedules |
| Pressão insuficiente em ponto | Verificar altura do reservatório ou aumentar diâmetro |

---

## 7. Fronteiras do Sistema

### 7.1 O que o sistema NÃO faz

| Fronteira | Explicação |
|-----------|-----------|
| **Não substitui o engenheiro** | Todas as decisões de projeto devem ser validadas por profissional habilitado. O sistema é uma ferramenta de apoio. |
| **Não garante 100% de precisão** | Cálculos usam fórmulas empíricas com aproximações. Revisão humana é obrigatória. |
| **Não corrige erros de modelagem** | Rooms mal definidos, famílias sem connectors, Levels errados — o sistema detecta e alerta, mas não conserta. |
| **Não projeta água quente** | Versão atual cobre apenas água fria. Água quente está prevista para futuro. |
| **Não projeta águas pluviais** | Fora do escopo atual. Previsto para extensão futura. |
| **Não projeta incêndio** | Fora do escopo atual. Previsto para extensão futura. |
| **Não funciona sem modelo arquitetônico** | O sistema parte de um modelo com Rooms — não cria o modelo do zero. |
| **Não gera memorial descritivo** | Produz dados e tabelas, mas não redige o memorial. |
| **Não emite ART/RRT** | Responsabilidade exclusiva do profissional. |
| **Não faz interferência entre disciplinas** | Clash detection é simplificada (BoundingBox). Para interferência completa, usar Navisworks. |
| **Não otimiza custos** | Não considera preço de materiais na seleção de diâmetros — segue critério técnico. |
| **Não opera em cloud/rede** | Plugin é 100% local, sem sincronização entre máquinas. |
| **Não funciona em Revit LT** | Requer Revit completo com API habilitada. |
| **Não se auto-atualiza** | Atualizações requerem reinstalação manual da DLL. |

### 7.2 Diagrama de Fronteiras

```
╔══════════════════════════════════════════════════════════╗
║                  DENTRO DO SISTEMA                       ║
║                                                          ║
║  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   ║
║  │ Análise do   │  │ Cálculos     │  │ Geração de   │   ║
║  │ modelo       │  │ hidráulicos  │  │ elementos    │   ║
║  └──────────────┘  └──────────────┘  └──────────────┘   ║
║  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐   ║
║  │ Classificação│  │ Validação    │  │ Documentação │   ║
║  │ de ambientes │  │ e logs       │  │ (tabelas/    │   ║
║  │              │  │              │  │  pranchas)   │   ║
║  └──────────────┘  └──────────────┘  └──────────────┘   ║
╚══════════════════════════════════════════════════════════╝

╔══════════════════════════════════════════════════════════╗
║                  FORA DO SISTEMA                         ║
║                                                          ║
║  • Modelagem arquitetônica    • Memorial descritivo      ║
║  • Água quente                • ART/RRT                  ║
║  • Águas pluviais             • Orçamento                ║
║  • Incêndio                   • Clash detection avançado ║
║  • Correção de modelo         • Aprovação em prefeitura  ║
║  • Otimização de custos       • Compatibilização         ║
╚══════════════════════════════════════════════════════════╝
```
