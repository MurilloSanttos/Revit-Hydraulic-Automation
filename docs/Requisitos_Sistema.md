# Requisitos Funcionais e Não Funcionais — Plugin Hidráulico Revit 2026

> Documento de requisitos completo para o sistema de automação hidráulica residencial integrado ao Autodesk Revit.

---

## 1. Visão Geral

**Sistema:** Plugin local de automação hidráulica para Autodesk Revit 2026
**Domínio:** Projetos hidráulicos residenciais no Brasil
**Normas:** NBR 5626 (água fria) e NBR 8160 (esgoto sanitário)
**Modo de operação:** Semi-automático com validação humana por etapa
**Motores de execução:** API do Revit (C#), Dynamo (.dyn), unMEP
**Uso:** Interno (não comercial)

**Escopo de automação:** 70–80% do fluxo completo de projeto hidráulico, cobrindo desde a leitura do modelo arquitetônico até a geração final de pranchas.

**Pipeline de execução (11 etapas sequenciais):**
```
Detecção → Classificação → Pontos Hidráulicos → Equipamentos → Prumadas →
Rede AF → Rede ES → Inclinações → Sistemas MEP → Tabelas → Pranchas
```

---

## 2. Requisitos Funcionais

---

### 2.1 Detecção de Ambientes

#### RF-001 — Leitura de Rooms do modelo arquitetônico

| Campo | Descrição |
|-------|-----------|
| **Nome** | Leitura de Rooms |
| **Descrição** | O sistema deve coletar todos os Rooms válidos do modelo Revit ativo, extraindo nome, número, nível, área, perímetro e ponto central. |
| **Entrada** | Modelo Revit aberto com Rooms definidos pela disciplina de arquitetura. |
| **Processamento** | 1. Executar `FilteredElementCollector` na categoria `OST_Rooms`. 2. Filtrar Rooms sem `Location` (não colocados). 3. Filtrar Rooms com área ≤ 0. 4. Converter área de ft² para m² (fator: 0.0929). 5. Converter perímetro de ft para m (fator: 0.3048). 6. Extrair ponto central via `LocationPoint`. 7. Obter nome do Level associado. |
| **Saída** | Lista de objetos `AmbienteInfo` contendo: ElementId, NomeOriginal, Numero, Nivel, AreaM2, PerimetroM, PontoCentral (XYZ em metros). |
| **Prioridade** | Alta |

#### RF-002 — Filtragem de Rooms inválidos

| Campo | Descrição |
|-------|-----------|
| **Nome** | Filtragem de Rooms inválidos |
| **Descrição** | O sistema deve ignorar Rooms que não possuem localização física ou que possuem área zero/negativa, registrando cada caso no log. |
| **Entrada** | Coleção bruta de elementos da categoria `OST_Rooms`. |
| **Processamento** | 1. Verificar `room.Location != null`. 2. Verificar `room.Area > 0`. 3. Para cada Room ignorado, registrar log nível Leve com ElementId e motivo. |
| **Saída** | Lista filtrada de Rooms válidos. Log com contagem de ignorados e motivo individual. |
| **Prioridade** | Alta |

#### RF-003 — Leitura de Spaces MEP existentes

| Campo | Descrição |
|-------|-----------|
| **Nome** | Leitura de Spaces MEP |
| **Descrição** | O sistema deve coletar todos os Spaces MEP já existentes no modelo, com os mesmos metadados extraídos dos Rooms. |
| **Entrada** | Modelo Revit com possíveis Spaces na categoria `OST_MEPSpaces`. |
| **Processamento** | 1. Executar `FilteredElementCollector` na categoria `OST_MEPSpaces`. 2. Filtrar Spaces sem Location ou área zero. 3. Extrair metadados (mesmos campos de `AmbienteInfo`). 4. Marcar `TipoElemento = Space`. |
| **Saída** | Lista de `AmbienteInfo` com `TipoElemento = Space`. |
| **Prioridade** | Alta |

#### RF-004 — Correspondência Room ↔ Space

| Campo | Descrição |
|-------|-----------|
| **Nome** | Correspondência Room–Space |
| **Descrição** | O sistema deve correlacionar cada Room com seu Space MEP correspondente, utilizando proximidade espacial e nível como critérios. |
| **Entrada** | Lista de Rooms (RF-001) e lista de Spaces (RF-003). |
| **Processamento** | 1. Para cada Room, buscar Spaces no mesmo nível. 2. Calcular distância euclidiana 2D entre pontos centrais. 3. Considerar correspondente se distância ≤ 0.5 m. 4. Priorizar Space mais próximo. 5. Registrar Rooms sem Space e Spaces órfãos. |
| **Saída** | Lista de pares (Room, Space), lista de Rooms sem Space, lista de Spaces órfãos. |
| **Prioridade** | Alta |

#### RF-005 — Criação automática de Spaces MEP

| Campo | Descrição |
|-------|-----------|
| **Nome** | Criação de Spaces |
| **Descrição** | O sistema deve criar Spaces MEP para Rooms relevantes que não possuem Space correspondente, mediante confirmação do usuário. |
| **Entrada** | Lista de Rooms sem Space (RF-004). Confirmação do usuário via TaskDialog. |
| **Processamento** | 1. Filtrar apenas Rooms classificados como relevantes (hidráulicos). 2. Exibir TaskDialog com contagem e solicitar confirmação. 3. Para cada Room aprovado: abrir Transaction, criar Space via `Document.Create.NewSpace(level, UV)`, copiar nome e número do Room, commitar Transaction. 4. Em caso de erro, executar rollback. |
| **Saída** | Spaces criados no modelo Revit com `SpaceCriadoAutomaticamente = true`. Log por Space criado. |
| **Prioridade** | Alta |

---

### 2.2 Classificação de Ambientes

#### RF-006 — Normalização de nomes de ambientes

| Campo | Descrição |
|-------|-----------|
| **Nome** | Normalização de texto |
| **Descrição** | O sistema deve normalizar o nome do ambiente para matching: converter para lowercase, remover acentos via decomposição Unicode, remover numeração final e normalizar espaços. |
| **Entrada** | String com nome original do ambiente (ex: "Banheiro Social 01"). |
| **Processamento** | 1. `ToLowerInvariant()`. 2. Decompor em `FormD`, remover `NonSpacingMark`, recompor em `FormC`. 3. Regex `\s*\d+\s*$` para remover numeração final. 4. Regex `\s+` → " " e trim. |
| **Saída** | String normalizada (ex: "banheiro social"). |
| **Prioridade** | Alta |

#### RF-007 — Classificação automática por tipo

| Campo | Descrição |
|-------|-----------|
| **Nome** | Classificação de ambiente |
| **Descrição** | O sistema deve classificar cada ambiente em uma das 8 categorias hidráulicas usando 3 estratégias de matching progressivas sobre o nome normalizado. |
| **Entrada** | Nome normalizado do ambiente (RF-006). Dicionário de 40+ padrões com tipo e peso base. |
| **Processamento** | 1. **Match exato**: texto == padrão → confiança = pesoBase. 2. **Texto contém padrão**: `texto.Contains(padrão)` → confiança = pesoBase × 0.85, prioriza padrão mais longo. 3. **Match parcial**: ratio de palavras em comum ≥ 50% → confiança = pesoBase × ratio × 0.7. 4. Retornar melhor resultado ou `NaoIdentificado` com confiança 0. |
| **Saída** | `ResultadoClassificacao`: TipoAmbiente (enum), Confianca (0.0–1.0), PadraoUtilizado (string). |
| **Prioridade** | Alta |

#### RF-008 — Sinalização por faixa de confiança

| Campo | Descrição |
|-------|-----------|
| **Nome** | Sinalização de confiança |
| **Descrição** | O sistema deve sinalizar cada classificação conforme a confiança: ≥ 0.70 aceita automaticamente, 0.50–0.69 requer validação humana, < 0.50 marcado como não identificado. |
| **Entrada** | `ResultadoClassificacao` (RF-007). |
| **Processamento** | 1. Se `Confianca >= 0.70`: `EhConfiavel = true`. 2. Se `0.50 <= Confianca < 0.70`: `NecessitaValidacao = true`, log Médio. 3. Se `Confianca < 0.50`: `Tipo = NaoIdentificado`, log Leve. |
| **Saída** | Flags booleanas no `ResultadoClassificacao`. Logs por ambiente sinalizado. |
| **Prioridade** | Alta |

#### RF-009 — Reclassificação manual pelo usuário

| Campo | Descrição |
|-------|-----------|
| **Nome** | Reclassificação manual |
| **Descrição** | O sistema deve permitir que o usuário altere a classificação de qualquer ambiente via combobox na interface, sobrescrevendo a classificação automática. |
| **Entrada** | Seleção do usuário na UI (tipo de ambiente via combobox). |
| **Processamento** | 1. Atualizar `Classificacao.Tipo` do `AmbienteInfo` selecionado. 2. Definir `Confianca = 1.0`. 3. Definir `PadraoUtilizado = "manual"`. 4. Registrar log Info com a alteração. |
| **Saída** | `AmbienteInfo` atualizado. Log de reclassificação. |
| **Prioridade** | Média |

---

### 2.3 Validação de Ambientes

#### RF-010 — Detecção de ambientes duplicados

| Campo | Descrição |
|-------|-----------|
| **Nome** | Detecção de duplicatas |
| **Descrição** | O sistema deve identificar ambientes com mesmo número no mesmo nível, registrando como erro Médio. |
| **Entrada** | Lista de `AmbienteInfo` (RF-001). |
| **Processamento** | Agrupar por (Numero, Nivel). Identificar grupos com count > 1. Para cada duplicata, registrar log Médio com IDs dos elementos. |
| **Saída** | Log com detalhes de cada duplicata encontrada. |
| **Prioridade** | Média |

#### RF-011 — Validação de áreas suspeitas

| Campo | Descrição |
|-------|-----------|
| **Nome** | Validação de áreas |
| **Descrição** | O sistema deve alertar sobre ambientes relevantes com área < 1.0 m² (possível erro de modelo) ou > 50.0 m² (possível ambiente composto). |
| **Entrada** | Lista de `AmbienteInfo` classificados como relevantes. |
| **Processamento** | Para cada ambiente com `EhRelevante == true`: se `AreaM2 < 1.0` → log Médio; se `AreaM2 > 50.0` → log Leve. |
| **Saída** | Logs de alertas com ElementId e área do ambiente. |
| **Prioridade** | Média |

#### RF-012 — Verificação de cobertura mínima

| Campo | Descrição |
|-------|-----------|
| **Nome** | Cobertura mínima |
| **Descrição** | O sistema deve verificar que ao menos 1 ambiente hidráulico foi detectado. Se nenhum relevante for encontrado, registrar erro Crítico. Se nenhum banheiro/suíte/lavabo for encontrado, registrar erro Médio. |
| **Entrada** | Lista de `AmbienteInfo` classificados. |
| **Processamento** | 1. Contar ambientes com `EhRelevante == true`. Se 0 → Crítico. 2. Verificar presença de Banheiro, Suite ou Lavabo. Se ausente → Médio. |
| **Saída** | Logs de cobertura. Flag de bloqueio se Crítico. |
| **Prioridade** | Alta |

---

### 2.4 Identificação de Pontos Hidráulicos

#### RF-013 — Mapeamento de pontos por tipo de ambiente

| Campo | Descrição |
|-------|-----------|
| **Nome** | Pontos por ambiente |
| **Descrição** | O sistema deve determinar, para cada ambiente classificado, a lista de pontos hidráulicos necessários com base na tabela de equipamentos por tipo. |
| **Entrada** | Lista de ambientes classificados (RF-007). JSON de mapeamento tipo → equipamentos. |
| **Processamento** | 1. Para cada ambiente relevante, consultar tabela. 2. Listar equipamentos esperados. 3. Para cada equipamento, definir: tipo de conexão (AF/ES/ambos), diâmetro mínimo, peso para cálculo de vazão, altura de instalação. |
| **Saída** | Lista de `PontoHidraulico` por ambiente com todos os parâmetros. |
| **Prioridade** | Alta |

#### RF-014 — Comparação com pontos existentes

| Campo | Descrição |
|-------|-----------|
| **Nome** | Comparação pontos existentes vs. necessários |
| **Descrição** | O sistema deve comparar os equipamentos já presentes no modelo com os pontos necessários, identificando pontos faltantes e pontos excedentes. |
| **Entrada** | Pontos necessários (RF-013). Equipamentos existentes no modelo por ambiente. |
| **Processamento** | 1. Coletar MEP fixtures por ambiente (FilteredElementCollector + BoundingBox intersect). 2. Para cada fixture, identificar tipo. 3. Marcar pontos necessários como atendidos/faltantes. |
| **Saída** | Lista de pontos faltantes por ambiente. Lista de pontos já atendidos. |
| **Prioridade** | Alta |

---

### 2.5 Inserção de Equipamentos

#### RF-015 — Verificação de famílias MEP

| Campo | Descrição |
|-------|-----------|
| **Nome** | Verificação de famílias |
| **Descrição** | O sistema deve verificar se as famílias MEP necessárias para cada tipo de equipamento estão carregadas no modelo. |
| **Entrada** | Lista de tipos de equipamento necessários. Modelo Revit ativo. |
| **Processamento** | 1. Para cada tipo necessário, buscar FamilySymbol correspondente. 2. Se não encontrada, registrar erro e tentar carregar da biblioteca padrão. |
| **Saída** | Mapa de tipo → FamilySymbol disponível. Log de famílias faltantes. |
| **Prioridade** | Alta |

#### RF-016 — Inserção automática de equipamentos

| Campo | Descrição |
|-------|-----------|
| **Nome** | Inserção de equipamentos |
| **Descrição** | O sistema deve inserir automaticamente cada equipamento faltante no ambiente, calculando posição baseada em paredes disponíveis, offset padrão e proximidade da prumada. |
| **Entrada** | Pontos faltantes (RF-014). Famílias disponíveis (RF-015). Geometria do ambiente. |
| **Processamento** | 1. Identificar paredes do ambiente (excluir paredes com portas). 2. Selecionar parede mais próxima da prumada. 3. Calcular posição com offset por tipo. 4. Inserir FamilyInstance via API dentro de Transaction. 5. Aplicar rotação conforme orientação da parede. 6. Validar connectors do equipamento. |
| **Saída** | FamilyInstances inseridas no modelo. Log por equipamento: posição, família, status. |
| **Prioridade** | Alta |

#### RF-017 — Validação de equipamentos existentes

| Campo | Descrição |
|-------|-----------|
| **Nome** | Validação de equipamentos |
| **Descrição** | O sistema deve validar cada equipamento existente: tipo de família, presença de connectors AF/ES, posição relativa às paredes e ausência de colisões. |
| **Entrada** | Equipamentos existentes por ambiente. Critérios de validação por tipo. |
| **Processamento** | 1. Verificar família corresponde ao esperado. 2. Verificar connectors necessários (AF e/ou ES). 3. Verificar distância da parede. 4. Verificar colisão com outros elementos. 5. Classificar: Válido, Com Ressalva, Inválido. |
| **Saída** | Status por equipamento. Lista de ações corretivas. Log de validação. |
| **Prioridade** | Média |

---

### 2.6 Criação de Prumadas

#### RF-018 — Agrupamento de ambientes por proximidade

| Campo | Descrição |
|-------|-----------|
| **Nome** | Clustering de ambientes |
| **Descrição** | O sistema deve agrupar ambientes por proximidade horizontal para definir os eixos de prumadas (shafts hidráulicos). |
| **Entrada** | Ambientes com equipamentos e pontos centrais. |
| **Processamento** | 1. Projetar pontos centrais no plano XY. 2. Agrupar por proximidade (threshold configurável). 3. Calcular centroide de cada cluster. |
| **Saída** | Lista de clusters com centroide e ambientes associados. |
| **Prioridade** | Alta |

#### RF-019 — Criação de prumadas verticais

| Campo | Descrição |
|-------|-----------|
| **Nome** | Criação de prumadas |
| **Descrição** | O sistema deve criar Pipes verticais (AF, ES, VE) em cada eixo de prumada, conectando todos os níveis atendidos. |
| **Entrada** | Clusters (RF-018). Levels do modelo. Dimensionamento (F11). |
| **Processamento** | 1. Para cada cluster, criar Pipe vertical de AF. 2. Criar tubo de queda (ES). 3. Criar coluna de ventilação (VE). 4. Dimensionar diâmetros. 5. Validar contra estrutura. |
| **Saída** | Prumadas no modelo. Mapeamento prumada → ambientes. |
| **Prioridade** | Alta |

---

### 2.7 Geração de Redes

#### RF-020 — Traçado de rede de água fria

| Campo | Descrição |
|-------|-----------|
| **Nome** | Rede de água fria |
| **Descrição** | O sistema deve traçar a rede completa de AF desde barrilete até cada ponto de consumo, com ramais dimensionados, registros e conexões. |
| **Entrada** | Prumadas AF (RF-019). Pontos de consumo com vazão. Configuração (altura reservatório, pressão mínima). |
| **Processamento** | 1. Traçar barrilete no nível mais alto. 2. Conectar às prumadas. 3. Traçar ramais por pavimento. 4. Traçar sub-ramais até connectors dos equipamentos. 5. Inserir registros de gaveta. 6. Dimensionar cada trecho. 7. Verificar pressão em todos os pontos. |
| **Saída** | Rede AF completa (Pipes + Fittings). Relatório de dimensionamento. |
| **Prioridade** | Alta |

#### RF-021 — Traçado de rede de esgoto sanitário

| Campo | Descrição |
|-------|-----------|
| **Nome** | Rede de esgoto |
| **Descrição** | O sistema deve traçar a rede de ES desde cada aparelho até o tubo de queda e subcoletor, incluindo caixas sifonadas, caixas de gordura e caixas de inspeção. |
| **Entrada** | Prumadas ES (RF-019). Pontos de descarga por equipamento. |
| **Processamento** | 1. Traçar ramais de descarga. 2. Inserir caixas sifonadas. 3. Traçar ramais de esgoto. 4. Inserir caixa de gordura (cozinha). 5. Traçar subcoletor (térreo). 6. Dimensionar por UHC. |
| **Saída** | Rede ES completa (Pipes + Fittings + Acessórios). Relatório de UHC por trecho. |
| **Prioridade** | Alta |

#### RF-022 — Aplicação de inclinação em esgoto

| Campo | Descrição |
|-------|-----------|
| **Nome** | Inclinação de esgoto |
| **Descrição** | O sistema deve aplicar declividade em todos os trechos horizontais de esgoto: 2% para DN ≤ 75mm e 1% para DN ≥ 100mm, ajustando elevação e fittings. |
| **Entrada** | Rede ES traçada (RF-021). Diâmetro de cada trecho. |
| **Processamento** | 1. Identificar trechos horizontais de esgoto. 2. Determinar inclinação pelo diâmetro. 3. Calcular novo Z do endpoint. 4. Reposicionar extremidade do Pipe. 5. Ajustar fittings conectados. 6. Verificar interferências. |
| **Saída** | Tubos com inclinação aplicada. Log de cada ajuste. |
| **Prioridade** | Alta |

---

### 2.8 Sistemas MEP

#### RF-023 — Criação de PipingSystems

| Campo | Descrição |
|-------|-----------|
| **Nome** | Sistemas MEP |
| **Descrição** | O sistema deve criar 3 PipingSystems (AF, ES, VE), atribuir todos os elementos e validar conectividade. |
| **Entrada** | Todos os Pipes e Fittings criados (RF-019, RF-020, RF-021). |
| **Processamento** | 1. Criar system para cada tipo. 2. Atribuir elementos via connectors. 3. Validar que não há elementos sem sistema. 4. Verificar continuidade topológica. 5. Aplicar cores: AF azul, ES marrom, VE verde. |
| **Saída** | 3 PipingSystems criados e populados. Relatório de conectividade. |
| **Prioridade** | Alta |

---

### 2.9 Dimensionamento Hidráulico

#### RF-024 — Cálculo de vazão provável (AF)

| Campo | Descrição |
|-------|-----------|
| **Nome** | Vazão provável |
| **Descrição** | O sistema deve calcular a vazão provável de cada trecho de AF usando a fórmula Q = 0.3 × √(ΣPesos), conforme NBR 5626. |
| **Entrada** | Soma de pesos dos aparelhos por trecho. |
| **Processamento** | 1. Somar pesos de jusante para montante. 2. Aplicar fórmula. 3. Arredondar para 2 casas decimais. |
| **Saída** | Vazão em L/s por trecho. |
| **Prioridade** | Alta |

#### RF-025 — Dimensionamento de diâmetro (AF)

| Campo | Descrição |
|-------|-----------|
| **Nome** | Diâmetro AF |
| **Descrição** | O sistema deve selecionar o menor diâmetro comercial em que a velocidade não exceda 3.0 m/s para a vazão calculada. |
| **Entrada** | Vazão por trecho (RF-024). Tabela de diâmetros comerciais. |
| **Processamento** | 1. Para cada diâmetro (20, 25, 32, 40, 50, 60, 75mm), calcular V = Q / A. 2. Selecionar menor diâmetro onde V ≤ 3.0 m/s. |
| **Saída** | Diâmetro selecionado em mm. Velocidade resultante em m/s. |
| **Prioridade** | Alta |

#### RF-026 — Verificação de pressão (AF)

| Campo | Descrição |
|-------|-----------|
| **Nome** | Verificação de pressão |
| **Descrição** | O sistema deve verificar que a pressão disponível em cada ponto de consumo é ≥ 3 m.c.a. e ≤ 40 m.c.a. |
| **Entrada** | Altura geométrica (reservatório → ponto). Perda de carga acumulada por trecho. |
| **Processamento** | 1. P_disponivel = H_geométrica - ΣJ (perda de carga). 2. Se P < 3 m.c.a. → erro Crítico. 3. Se P > 40 m.c.a. → erro Médio. |
| **Saída** | Pressão disponível em cada ponto. Alertas conforme limites. |
| **Prioridade** | Alta |

#### RF-027 — Dimensionamento por UHC (ES)

| Campo | Descrição |
|-------|-----------|
| **Nome** | Dimensionamento de esgoto por UHC |
| **Descrição** | O sistema deve dimensionar cada trecho de esgoto usando a soma de Unidades Hunter de Contribuição, conforme tabelas da NBR 8160. |
| **Entrada** | UHC por aparelho. Topologia da rede ES. |
| **Processamento** | 1. Somar UHCs de montante para jusante. 2. Consultar tabela UHC → diâmetro mínimo. 3. Respeitar regra: diâmetro nunca diminui no sentido do escoamento. |
| **Saída** | Diâmetro dimensionado por trecho. UHC acumulado por trecho. |
| **Prioridade** | Alta |

---

### 2.10 Geração de Documentação

#### RF-028 — Criação de schedules (tabelas)

| Campo | Descrição |
|-------|-----------|
| **Nome** | Tabelas quantitativas |
| **Descrição** | O sistema deve criar 4 ViewSchedules: tubulações por sistema/diâmetro, conexões por tipo/diâmetro, equipamentos por ambiente, e resumo de ambientes. |
| **Entrada** | Elementos MEP criados. Sistemas MEP (RF-023). |
| **Processamento** | 1. Criar ViewSchedule via API para cada tipo. 2. Configurar campos, filtros e agrupamento. 3. Configurar formatação (unidades, totais). |
| **Saída** | 4 Schedules no modelo Revit. Exportação opcional para CSV/Excel. |
| **Prioridade** | Média |

#### RF-029 — Criação de pranchas

| Campo | Descrição |
|-------|-----------|
| **Nome** | Pranchas automáticas |
| **Descrição** | O sistema deve criar ViewSheets com plantas por sistema/pavimento, schedules, legendas e numeração padronizada (HID-01, HID-02). |
| **Entrada** | Views de planta. Schedules (RF-028). Titleblock padrão. |
| **Processamento** | 1. Criar View Templates para AF e ES. 2. Duplicar Floor Plans com filtros. 3. Criar ViewSheets. 4. Posicionar views e schedules. 5. Numerar sequencialmente. |
| **Saída** | Pranchas completas no modelo, prontas para impressão. |
| **Prioridade** | Média |

---

### 2.11 Sistema de Logs

#### RF-030 — Registro de logs estruturados

| Campo | Descrição |
|-------|-----------|
| **Nome** | Logging estruturado |
| **Descrição** | O sistema deve registrar cada ação relevante com: timestamp, nível (Info/Leve/Médio/Crítico), etapa, componente, mensagem e ElementId opcional. |
| **Entrada** | Eventos de todos os serviços. |
| **Processamento** | Acumular `LogEntry` em memória via `LogManager`. |
| **Saída** | Lista de entries acessível pela UI e exportável para JSON. |
| **Prioridade** | Alta |

#### RF-031 — Bloqueio por erro Crítico

| Campo | Descrição |
|-------|-----------|
| **Nome** | Bloqueio de pipeline |
| **Descrição** | O sistema deve impedir o avanço para a próxima etapa quando há pelo menos 1 log de nível Crítico na etapa atual. |
| **Entrada** | Lista de logs da etapa atual. |
| **Processamento** | Verificar `_entries.Any(e => e.Level == LogLevel.Critico)`. |
| **Saída** | Flag `TemBloqueio`. Botão de próxima etapa desabilitado na UI. |
| **Prioridade** | Alta |

#### RF-032 — Exportação de logs para JSON

| Campo | Descrição |
|-------|-----------|
| **Nome** | Exportação de logs |
| **Descrição** | O sistema deve exportar todos os logs acumulados para arquivo JSON no diretório `Data/Logs/` com nome baseado em timestamp. |
| **Entrada** | Lista de `LogEntry` em memória. |
| **Processamento** | Serializar com Newtonsoft.Json (Formatting.Indented). Salvar em `log_{yyyyMMdd_HHmmss}.json`. |
| **Saída** | Arquivo JSON no disco. Caminho do arquivo retornado. |
| **Prioridade** | Média |

---

### 2.12 Interface do Usuário

#### RF-033 — Aba de Configuração

| Campo | Descrição |
|-------|-----------|
| **Nome** | Configuração de parâmetros |
| **Descrição** | A UI deve permitir edição de: pressão mínima (m.c.a.), altura do reservatório (m), declividades (%), velocidade máxima (m/s), tipo de norma. Com validação em tempo real e persistência em JSON. |
| **Entrada** | Valores editados pelo usuário. |
| **Processamento** | Validar faixas. Salvar em `hydraulic_config.json`. Carregar ao abrir a janela. |
| **Saída** | Configuração persistida. Feedback visual de validação. |
| **Prioridade** | Média |

#### RF-034 — Aba de Execução por etapas

| Campo | Descrição |
|-------|-----------|
| **Nome** | Execução por etapas |
| **Descrição** | A UI deve exibir as 11 etapas com status visual, permitindo executar individualmente ou em sequência, com validação humana entre etapas. |
| **Entrada** | Clique do usuário em botão por etapa ou "Executar Todas". |
| **Processamento** | 1. Verificar pré-condições. 2. Executar via ExternalEvent. 3. Atualizar status visual. 4. Se há alertas, aguardar aprovação. |
| **Saída** | Pipeline controlado com feedback visual em tempo real. |
| **Prioridade** | Alta |

#### RF-035 — Aba de Diagnóstico

| Campo | Descrição |
|-------|-----------|
| **Nome** | Diagnóstico com logs |
| **Descrição** | A UI deve exibir DataGrid de logs em tempo real, com filtros por nível e etapa, cores por severidade, contadores e botão de exportar. |
| **Entrada** | Lista de logs do `LogManager`. |
| **Processamento** | Binding MVVM em ObservableCollection. Filtros via CollectionView. |
| **Saída** | Visualização em tempo real. Navegação para elemento no Revit ao clicar em log com ElementId. |
| **Prioridade** | Média |

#### RF-036 — Validação humana entre etapas

| Campo | Descrição |
|-------|-----------|
| **Nome** | Validação humana |
| **Descrição** | Após cada etapa com alertas (erros Médios), o sistema deve pausar e exibir botões "Aprovar" / "Rejeitar" para que o usuário valide antes de avançar. |
| **Entrada** | Resultado da etapa com logs Médios. |
| **Processamento** | Exibir resumo da etapa. Aguardar ação do usuário. Se aprovado: habilitar próxima etapa. Se rejeitado: permitir re-execução. |
| **Saída** | Pipeline avança ou pausado. Log de decisão do usuário. |
| **Prioridade** | Alta |

---

### 2.13 Orquestração

#### RF-037 — Pipeline de execução sequencial

| Campo | Descrição |
|-------|-----------|
| **Nome** | Orquestrador de pipeline |
| **Descrição** | O sistema deve gerenciar a execução sequencial das 11 etapas, verificando pré-condições, persistindo estado e emitindo eventos de progresso. |
| **Entrada** | Configuração. Modelo Revit. Ação do usuário (executar etapa). |
| **Processamento** | 1. Verificar dependências da etapa. 2. Executar ação correspondente. 3. Coletar resultado e logs. 4. Atualizar estado do pipeline. 5. Emitir evento de progresso para UI. |
| **Saída** | Estado do pipeline (etapa atual, status por etapa). Eventos para atualização da UI. |
| **Prioridade** | Alta |

#### RF-038 — Integração com Dynamo

| Campo | Descrição |
|-------|-----------|
| **Nome** | Disparo de scripts Dynamo |
| **Descrição** | O sistema deve ser capaz de disparar scripts Dynamo (.dyn), passando parâmetros via JSON e lendo resultados. |
| **Entrada** | Caminho do script .dyn. JSON de parâmetros. |
| **Processamento** | 1. Serializar parâmetros para JSON. 2. Disparar script via DynamoRevit API. 3. Aguardar conclusão com timeout. 4. Ler JSON de saída. 5. Registrar log de execução. |
| **Saída** | Resultado do script. Log de tempo de execução. |
| **Prioridade** | Alta |

---

## 3. Requisitos Não Funcionais

---

### RNF-001 — Performance de etapas individuais

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Performance |
| **Descrição** | Cada etapa do pipeline deve completar sua execução em tempo aceitável para modelos residenciais típicos (até 20 ambientes). |
| **Critério** | Etapas 1–2 (análise): ≤ 5 segundos. Etapas 3–5 (equipamentos/prumadas): ≤ 15 segundos. Etapas 6–9 (redes): ≤ 30 segundos. Etapas 10–11 (documentação): ≤ 20 segundos. |

### RNF-002 — Performance de modelo grande

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Performance |
| **Descrição** | O sistema deve suportar modelos com até 50 ambientes sem degradação superior a 3× o tempo base. |
| **Critério** | Tempo total de execução completa ≤ 5 minutos para modelos com 50 ambientes. |

### RNF-003 — Confiabilidade de transações

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Confiabilidade |
| **Descrição** | Toda operação de escrita no modelo deve ser executada dentro de Transaction com rollback automático em caso de erro. Nenhuma alteração parcial deve persistir em caso de falha. |
| **Critério** | 100% das operações de escrita utilizam Transaction. Zero modificações persistidas em caso de exceção. |

### RNF-004 — Confiabilidade do classificador

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Confiabilidade |
| **Descrição** | O classificador de ambientes deve atingir taxa de acerto ≥ 90% em corpus representativo de nomes de ambientes residenciais brasileiros. |
| **Critério** | Taxa de acerto ≥ 90% em corpus de teste com ≥ 50 variações de nomes. Confiança média ≥ 0.80 para ambientes corretamente classificados. |

### RNF-005 — Escalabilidade modular

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Escalabilidade |
| **Descrição** | O sistema deve ser projetado para que novos módulos (ex: água quente, incêndio, pluvial) possam ser adicionados sem alteração dos módulos existentes. |
| **Critério** | Adição de novo módulo requer zero alterações em classes existentes do PluginCore. Nova etapa adicionada ao pipeline com ≤ 3 arquivos novos. |

### RNF-006 — Usabilidade da interface

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Usabilidade |
| **Descrição** | A interface deve ser operável por projetista com conhecimento básico de Revit, sem treinamento adicional. Todas as ações devem ter feedback visual imediato. |
| **Critério** | Status visual de cada etapa atualizado em ≤ 1 segundo. Mensagens de erro em português claro. Tooltips em todos os campos de configuração. |

### RNF-007 — Usabilidade de logs

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Usabilidade |
| **Descrição** | O sistema de logs deve permitir que o usuário identifique e localize qualquer problema em ≤ 2 cliques. |
| **Critério** | Filtro por nível: 1 clique. Navegação ao elemento no modelo: 1 clique adicional. Cores diferenciadas por nível (vermelho, amarelo, azul, cinza). |

### RNF-008 — Manutenibilidade do código

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Manutenibilidade |
| **Descrição** | O código deve seguir separação limpa entre lógica (PluginCore) e integração (Revit2026). PluginCore não deve ter nenhuma referência a assemblies do Revit. |
| **Critério** | Zero referências a `Autodesk.Revit.*` no projeto PluginCore. Cobertura de XML docs ≥ 90% em classes e métodos públicos. |

### RNF-009 — Manutenibilidade de regras normativas

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Manutenibilidade |
| **Descrição** | Todos os parâmetros normativos (pesos, UHCs, diâmetros, declividades) devem ser externalizados em arquivos JSON editáveis, não hardcoded. |
| **Critério** | Alteração de qualquer parâmetro normativo requer edição de JSON apenas — zero alteração de código C#. |

### RNF-010 — Compatibilidade com Revit

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Compatibilidade |
| **Descrição** | O plugin deve ser compatível com Autodesk Revit 2026 (.NET 8.0). A arquitetura deve permitir adaptação para versões futuras com mínimo de alterações. |
| **Critério** | Funcional no Revit 2026. Referências à API do Revit concentradas exclusivamente no projeto Revit2026. Adaptação para Revit 2027 requer ≤ 10% das classes alteradas. |

### RNF-011 — Robustez contra modelos imperfeitos

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Robustez |
| **Descrição** | O sistema deve operar de forma resiliente em modelos com problemas comuns: Rooms sem nome, Rooms sobrepostos, famílias sem connectors, Levels duplicados. Nenhum cenário deve causar crash do Revit. |
| **Critério** | Zero exceções não tratadas propagadas ao Revit. Modelo com 30% de erros intencionais: sistema completa diagnóstico sem crash. Todo erro registrado no log com ElementId. |

### RNF-012 — Robustez contra interrupção

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Robustez |
| **Descrição** | O sistema deve suportar interrupção pelo usuário em qualquer ponto, sem deixar o modelo em estado inconsistente. |
| **Critério** | Transaction rollback automático em caso de cancelamento. Estado do pipeline persistido — re-execução retoma do ponto correto. |

### RNF-013 — Segurança de dados

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Segurança |
| **Descrição** | Todos os dados do plugin (configuração, logs, mapeamentos) devem ser armazenados localmente. Nenhuma informação é enviada para servidores externos. |
| **Critério** | Zero requisições HTTP no código. Dados armazenados em `%LOCALAPPDATA%/HidraulicaRevit/` e na pasta `Data/` do projeto. |

### RNF-014 — Rastreabilidade completa

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Rastreabilidade |
| **Descrição** | Toda decisão do sistema (classificação, dimensionamento, posicionamento) deve ser rastreável: registrada em log com timestamp, critério e ElementId. |
| **Critério** | Cada ambiente: log de classificação com padrão e confiança. Cada trecho: log de dimensionamento com vazão, diâmetro e pressão. Cada equipamento: log de inserção com posição e família. |

### RNF-015 — Tempo de startup do plugin

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Performance |
| **Descrição** | O carregamento do plugin no Revit (OnStartup) deve ser rápido e não impactar o tempo de abertura do Revit. |
| **Critério** | `OnStartup` completa em ≤ 500 ms. Nenhum carregamento pesado durante inicialização. |

### RNF-016 — Consumo de memória

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Performance |
| **Descrição** | O plugin deve ter consumo de memória controlado, sem acumular dados desnecessários em RAM. |
| **Critério** | Incremento de memória do processo Revit ≤ 100 MB durante execução completa. Logs exportados após cada etapa para liberar memória quando acima de 10.000 entries. |

### RNF-017 — Independência de conexão de rede

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Disponibilidade |
| **Descrição** | O plugin deve funcionar 100% offline, sem necessidade de conexão com internet ou servidores de licença próprios. |
| **Critério** | Todas as funcionalidades operáveis em máquina sem acesso à internet. Zero dependências de CDN, APIs externas ou licenciamento online. |

### RNF-018 — Idioma da interface

| Campo | Descrição |
|-------|-----------|
| **Categoria** | Usabilidade |
| **Descrição** | Toda a interface, mensagens de log, relatórios e documentação devem estar em português brasileiro. |
| **Critério** | 100% dos textos visíveis ao usuário em pt-BR. Termos técnicos conforme normas brasileiras (m.c.a., UHC, etc.). |
