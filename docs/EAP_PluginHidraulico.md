# EAP — Plugin Hidráulico Revit 2026

> Estrutura Analítica do Projeto completa para desenvolvimento do sistema de automação hidráulica residencial integrado ao Autodesk Revit.
> 
> 14 Fases | 66 Subfases | 409 Tarefas Executáveis

---

## 1. PLANEJAMENTO E DEFINIÇÃO

### 1.1 Escopo e Requisitos
- 1.1.1 Definir escopo funcional completo do plugin (o que automatiza × o que delega)
- 1.1.2 Mapear fluxo manual atual de projeto hidráulico residencial (etapas do projetista)
- 1.1.3 Identificar pontos de automação viáveis (70–80% do fluxo)
- 1.1.4 Documentar requisitos normativos obrigatórios (NBR 5626 e NBR 8160)
- 1.1.5 Definir escopo de cada módulo funcional (11 módulos)
- 1.1.6 Estabelecer critérios de aceitação por módulo

### 1.2 Definição Normativa
- 1.2.1 Extrair regras de dimensionamento de água fria da NBR 5626
- 1.2.2 Extrair regras de dimensionamento de esgoto da NBR 8160
- 1.2.3 Tabular pesos de aparelhos sanitários por tipo
- 1.2.4 Tabular diâmetros mínimos por tipo de aparelho
- 1.2.5 Documentar regras de declividade (2% ≤ 75mm, 1% ≥ 100mm)
- 1.2.6 Documentar regras de ventilação de esgoto
- 1.2.7 Definir parâmetros padrão do sistema (reservatório superior, 3 m.c.a., 6 m altura)
- 1.2.8 Criar JSON de referência normativa para consumo pelo plugin

### 1.3 Estratégia de Desenvolvimento
- 1.3.1 Definir ordem de desenvolvimento dos módulos (dependências)
- 1.3.2 Estabelecer critérios de validação por etapa
- 1.3.3 Definir fluxo de versionamento e controle de código
- 1.3.4 Estabelecer convenções de nomenclatura (código, namespaces, variáveis)
- 1.3.5 Definir estratégia de testes (unitários no Core, integração no Revit)

---

## 2. ARQUITETURA DO SISTEMA

### 2.1 Arquitetura Geral
- 2.1.1 Definir separação de camadas (PluginCore × Revit2026 × DynamoScripts × Data)
- 2.1.2 Documentar responsabilidade de cada camada
- 2.1.3 Definir modelo de comunicação plugin ↔ Dynamo (via JSON)
- 2.1.4 Definir modelo de comunicação plugin ↔ unMEP
- 2.1.5 Projetar pipeline de execução por etapas (orquestrador)
- 2.1.6 Criar diagrama de arquitetura geral

### 2.2 Modelos de Domínio
- 2.2.1 Definir modelo `AmbienteInfo` (Room/Space agnóstico ao Revit)
- 2.2.2 Definir modelo `EquipamentoHidraulico` (aparelhos sanitários)
- 2.2.3 Definir modelo `PontoHidraulico` (conexão ambiente ↔ equipamento)
- 2.2.4 Definir modelo `TrechoTubulacao` (segmento de rede)
- 2.2.5 Definir modelo `Prumada` (coluna vertical)
- 2.2.6 Definir modelo `SistemaMEP` (sistema lógico do Revit)
- 2.2.7 Definir modelo `ResultadoDimensionamento` (vazão, diâmetro, pressão)
- 2.2.8 Definir enums de classificação (`TipoAmbiente`, `TipoEquipamento`, `TipoRede`)

### 2.3 Interfaces e Contratos
- 2.3.1 Definir `IAmbienteService` (detecção e classificação)
- 2.3.2 Definir `IEquipamentoService` (inserção e validação)
- 2.3.3 Definir `IRedeService` (geração de redes)
- 2.3.4 Definir `IDimensionamentoService` (cálculos hidráulicos)
- 2.3.5 Definir `ILogService` (logging estruturado)
- 2.3.6 Definir `IOrquestradorService` (pipeline de etapas)
- 2.3.7 Definir `IDynamoIntegration` (disparo de scripts)
- 2.3.8 Definir `IExportService` (tabelas e pranchas)

### 2.4 Padrões de Projeto
- 2.4.1 Implementar padrão Pipeline/Chain para fluxo de etapas
- 2.4.2 Implementar padrão Strategy para decisão por tipo de ambiente
- 2.4.3 Implementar padrão Observer para logs e eventos de progresso
- 2.4.4 Definir padrão de erro (Crítico/Médio/Leve) com regras de bloqueio

---

## 3. SETUP DO AMBIENTE

### 3.1 Ambiente de Desenvolvimento
- 3.1.1 Instalar Revit 2026 com SDK
- 3.1.2 Configurar Visual Studio com .NET 8.0 e WPF
- 3.1.3 Criar Solution (`PluginRevit.sln`) com projetos PluginCore e Revit2026
- 3.1.4 Configurar referências da API do Revit (`RevitAPI.dll`, `RevitAPIUI.dll`)
- 3.1.5 Configurar NuGet (Newtonsoft.Json)
- 3.1.6 Configurar `.gitignore` para projeto C#/Revit

### 3.2 Ambiente de Teste
- 3.2.1 Criar modelo Revit de teste com planta residencial completa
- 3.2.2 Definir Rooms nomeados no modelo de teste (todos os tipos de ambiente)
- 3.2.3 Incluir variações de nomenclatura para teste do classificador
- 3.2.4 Criar modelo com inconsistências propositais (para testar validações)
- 3.2.5 Configurar modelo com famílias MEP necessárias

### 3.3 Estrutura de Diretórios
- 3.3.1 Criar estrutura `PluginCore/` (Models, Services, Interfaces, Logging)
- 3.3.2 Criar estrutura `Revit2026/` (Commands, Services, Events, UI)
- 3.3.3 Criar estrutura `DynamoScripts/` (organizada por etapa)
- 3.3.4 Criar estrutura `Data/` (Config, Mappings, Logs)
- 3.3.5 Criar estrutura `docs/` (documentação por etapa)

### 3.4 Registro do Plugin
- 3.4.1 Criar arquivo `.addin` para Revit 2026
- 3.4.2 Implementar `IExternalApplication` (App.cs)
- 3.4.3 Registrar aba "Hidráulica" na ribbon do Revit
- 3.4.4 Registrar painel "Automação" com botões por etapa
- 3.4.5 Validar carregamento correto do plugin no Revit

---

## 4. DESENVOLVIMENTO DO CORE (PluginCore)

### 4.1 Sistema de Logging
- 4.1.1 Implementar enum `LogLevel` (Crítico, Médio, Leve, Info)
- 4.1.2 Implementar modelo `LogEntry` (timestamp, nível, etapa, componente, elementId)
- 4.1.3 Implementar `LogManager` com acumulação em memória
- 4.1.4 Implementar exportação de logs para JSON
- 4.1.5 Implementar resumo textual de logs (contagem por nível)
- 4.1.6 Implementar filtragem de logs por etapa e por nível
- 4.1.7 Implementar verificação de bloqueio (presença de erro crítico)

### 4.2 Motor de Classificação de Ambientes
- 4.2.1 Implementar normalização de texto (remoção de acentos, lowercase, trim)
- 4.2.2 Implementar remoção de numeração final (ex: "Banheiro 01" → "Banheiro")
- 4.2.3 Criar dicionário de padrões por tipo de ambiente (40+ variações)
- 4.2.4 Implementar estratégia 1: match exato
- 4.2.5 Implementar estratégia 2: texto contém padrão (prioridade pelo mais longo)
- 4.2.6 Implementar estratégia 3: match parcial por palavras
- 4.2.7 Implementar cálculo de confiança por estratégia
- 4.2.8 Implementar classificação em lote (`ClassificarTodos`)
- 4.2.9 Validar cobertura do classificador com corpus de nomes reais

### 4.3 Validador de Ambientes
- 4.3.1 Implementar detecção de ambientes duplicados (mesmo número + nível)
- 4.3.2 Implementar validação de classificação (confiança baixa, não identificados)
- 4.3.3 Implementar validação de áreas (muito pequenas ou muito grandes)
- 4.3.4 Implementar validação de nomes vazios
- 4.3.5 Implementar verificação de cobertura mínima (ao menos 1 banheiro)
- 4.3.6 Implementar geração de resumo da detecção por tipo

### 4.4 Motor de Decisão por Ambiente
- 4.4.1 Definir tabela de equipamentos esperados por tipo de ambiente
- 4.4.2 Implementar lógica de decisão: quais pontos hidráulicos cada ambiente exige
- 4.4.3 Implementar regras de posicionamento relativo (vaso próx. à prumada, etc.)
- 4.4.4 Implementar validação de equipamentos existentes vs. esperados
- 4.4.5 Implementar geração de lista de ação (inserir, validar, ajustar)

### 4.5 Motor de Cálculo Hidráulico — Água Fria
- 4.5.1 Implementar tabela de pesos de aparelhos sanitários (NBR 5626)
- 4.5.2 Implementar cálculo de vazão provável (fórmula da raiz quadrada)
- 4.5.3 Implementar dimensionamento de diâmetro por trecho
- 4.5.4 Implementar verificação de velocidade máxima (≤ 3 m/s)
- 4.5.5 Implementar cálculo de perda de carga (Hazen-Williams ou Fair-Whipple-Hsiao)
- 4.5.6 Implementar verificação de pressão mínima nos pontos (≥ 3 m.c.a.)
- 4.5.7 Implementar verificação de pressão máxima (≤ 40 m.c.a.)
- 4.5.8 Implementar dimensionamento de coluna (prumada) de água fria

### 4.6 Motor de Cálculo Hidráulico — Esgoto
- 4.6.1 Implementar tabela de UHC (Unidades Hunter de Contribuição) por aparelho
- 4.6.2 Implementar dimensionamento de ramais de descarga
- 4.6.3 Implementar dimensionamento de ramais de esgoto
- 4.6.4 Implementar dimensionamento de tubos de queda
- 4.6.5 Implementar dimensionamento de subcoletores
- 4.6.6 Implementar cálculo de declividade (2% ≤ 75mm, 1% ≥ 100mm)
- 4.6.7 Implementar verificação de ventilação por trecho
- 4.6.8 Implementar dimensionamento de coluna de ventilação

### 4.7 Orquestrador de Pipeline
- 4.7.1 Implementar modelo `EtapaPipeline` (nome, status, dependências, ação)
- 4.7.2 Implementar fila de execução com 11 etapas
- 4.7.3 Implementar verificação de pré-condições por etapa
- 4.7.4 Implementar execução controlada com pausa entre etapas
- 4.7.5 Implementar persistência de estado do pipeline (retomar execução)
- 4.7.6 Implementar eventos de progresso (para atualizar UI)

---

## 5. INTEGRAÇÃO COM REVIT (Revit2026)

### 5.1 Leitura do Modelo
- 5.1.1 Implementar `RoomReaderService` — leitura de Rooms com conversão de unidades
- 5.1.2 Implementar filtragem de Rooms inválidos (sem Location, área zero)
- 5.1.3 Implementar extração de dados geométricos (área, perímetro, ponto central)
- 5.1.4 Implementar leitura de Levels do modelo
- 5.1.5 Implementar leitura de paredes e aberturas por Room (para posicionamento)
- 5.1.6 Implementar leitura de MEP fixtures existentes (equipamentos já colocados)

### 5.2 Gerenciamento de Spaces MEP
- 5.2.1 Implementar `SpaceManagerService` — leitura de Spaces existentes
- 5.2.2 Implementar correspondência Room ↔ Space por proximidade espacial
- 5.2.3 Implementar criação automática de Spaces para Rooms sem Space
- 5.2.4 Implementar detecção de Spaces órfãos
- 5.2.5 Implementar transferência de classificação Room → Space

### 5.3 Manipulação de Equipamentos
- 5.3.1 Implementar detecção de famílias MEP carregadas no modelo
- 5.3.2 Implementar verificação de tipo de família por equipamento
- 5.3.3 Implementar inserção de equipamentos via API (FamilyInstance)
- 5.3.4 Implementar posicionamento automático (parede mais próxima, offset da parede)
- 5.3.5 Implementar validação de posicionamento pós-inserção
- 5.3.6 Implementar rotação de equipamento conforme orientação da parede

### 5.4 Criação de Tubulações
- 5.4.1 Implementar criação de Pipe segments via API
- 5.4.2 Implementar criação de Pipe fittings (conexões, curvas, tees)
- 5.4.3 Implementar criação de tubulação vertical (prumadas)
- 5.4.4 Implementar criação de tubulação horizontal (ramais)
- 5.4.5 Implementar aplicação de inclinação em tubulações de esgoto
- 5.4.6 Implementar conexão automática entre trechos (connectors)

### 5.5 Sistemas MEP
- 5.5.1 Implementar criação de PipingSystem para água fria
- 5.5.2 Implementar criação de PipingSystem para esgoto sanitário
- 5.5.3 Implementar criação de PipingSystem para ventilação
- 5.5.4 Implementar atribuição de elementos aos sistemas
- 5.5.5 Implementar validação de conectividade dos sistemas
- 5.5.6 Implementar nomenclatura padronizada de sistemas

### 5.6 External Events
- 5.6.1 Implementar `ExternalEventHandler` base para operações thread-safe
- 5.6.2 Implementar event handler para criação de Spaces
- 5.6.3 Implementar event handler para inserção de equipamentos
- 5.6.4 Implementar event handler para criação de redes
- 5.6.5 Implementar fila de eventos com callback de conclusão

### 5.7 Geração de Tabelas
- 5.7.1 Implementar criação de Schedule para quantitativos de tubulação
- 5.7.2 Implementar criação de Schedule para quantitativos de conexões
- 5.7.3 Implementar criação de Schedule para equipamentos por ambiente
- 5.7.4 Configurar campos, filtros e formatação das tabelas
- 5.7.5 Implementar exportação de tabelas para Excel/CSV

### 5.8 Geração de Pranchas
- 5.8.1 Implementar criação de ViewSheet a partir de titleblock padrão
- 5.8.2 Implementar criação de Floor Plan views filtradas (hidráulica)
- 5.8.3 Implementar posicionamento automático de views na prancha
- 5.8.4 Implementar adição de tabelas à prancha
- 5.8.5 Implementar adição de legendas e notas
- 5.8.6 Implementar numeração sequencial de pranchas
- 5.8.7 Configurar View Templates para plantas hidráulicas

---

## 6. INTEGRAÇÃO COM DYNAMO

### 6.1 Infraestrutura de Integração
- 6.1.1 Definir formato JSON de comunicação plugin → Dynamo
- 6.1.2 Implementar serialização de parâmetros de entrada por script
- 6.1.3 Implementar leitura de resultados do Dynamo (JSON de saída)
- 6.1.4 Implementar disparo de scripts Dynamo via API (DynamoRevit)
- 6.1.5 Implementar timeout e tratamento de erros na execução Dynamo
- 6.1.6 Implementar log de execução Dynamo (entrada, saída, tempo)

### 6.2 Scripts de Ambientes
- 6.2.1 Criar script `01_ValidarRooms.dyn` — validação visual de Rooms
- 6.2.2 Criar script `02_CriarSpacesMassivo.dyn` — criação em massa de Spaces

### 6.3 Scripts de Equipamentos
- 6.3.1 Criar script `03_InserirEquipamentos.dyn` — inserção paramétrica de fixtures
- 6.3.2 Criar script `04_ValidarPosicionamento.dyn` — verificação de distâncias

### 6.4 Scripts de Redes
- 6.4.1 Criar script `05_GerarRamalAguaFria.dyn` — traçado de rede de água fria
- 6.4.2 Criar script `06_GerarRamalEsgoto.dyn` — traçado de rede de esgoto
- 6.4.3 Criar script `07_AplicarInclinacao.dyn` — ajuste de declividade
- 6.4.4 Criar script `08_CriarPrumadas.dyn` — criação de colunas verticais
- 6.4.5 Criar script `09_ConectarRede.dyn` — interconexão de trechos

### 6.5 Scripts de Documentação
- 6.5.1 Criar script `10_GerarTabelas.dyn` — criação de schedules
- 6.5.2 Criar script `11_GerarPranchas.dyn` — montagem automática de pranchas

---

## 7. INTEGRAÇÃO COM unMEP

### 7.1 Configuração
- 7.1.1 Documentar capacidades e limitações do unMEP
- 7.1.2 Definir quais tarefas serão delegadas ao unMEP vs. Dynamo
- 7.1.3 Configurar unMEP para o padrão do projeto

### 7.2 Roteirização de Redes
- 7.2.1 Implementar disparo de roteamento automático de água fria via unMEP
- 7.2.2 Implementar disparo de roteamento automático de esgoto via unMEP
- 7.2.3 Implementar validação de rotas geradas pelo unMEP
- 7.2.4 Implementar ajuste manual de rotas (feedback ao unMEP)

---

## 8. MÓDULOS FUNCIONAIS

### 8.1 Módulo 01 — Detecção de Ambientes (Rooms + Spaces)
- 8.1.1 Ler todos os Rooms válidos do modelo arquitetônico
- 8.1.2 Filtrar Rooms inválidos (sem Location, área zero, redundantes)
- 8.1.3 Extrair metadados de cada Room (nome, número, nível, área, perímetro)
- 8.1.4 Converter unidades de pés para metros
- 8.1.5 Ler Spaces MEP existentes no modelo
- 8.1.6 Correlacionar Rooms com Spaces por proximidade espacial
- 8.1.7 Identificar Rooms sem Space correspondente
- 8.1.8 Identificar Spaces órfãos (sem Room)
- 8.1.9 Criar Spaces automaticamente para Rooms sem correspondência
- 8.1.10 Gerar log completo da detecção

### 8.2 Módulo 02 — Classificação de Ambientes
- 8.2.1 Normalizar nomes de ambientes (acentos, case, numeração)
- 8.2.2 Classificar cada ambiente por tipo (8 categorias)
- 8.2.3 Calcular nível de confiança da classificação
- 8.2.4 Sinalizar ambientes que necessitam validação humana (confiança < 70%)
- 8.2.5 Sinalizar ambientes não classificados
- 8.2.6 Permitir reclassificação manual pelo usuário via UI
- 8.2.7 Persistir classificações finais em parâmetro do Space
- 8.2.8 Gerar relatório de classificação com estatísticas

### 8.3 Módulo 03 — Identificação de Pontos Hidráulicos
- 8.3.1 Definir tabela de pontos hidráulicos por tipo de ambiente
- 8.3.2 Para cada ambiente classificado, listar pontos necessários
- 8.3.3 Definir tipo de conexão por ponto (água fria, esgoto, ambos)
- 8.3.4 Definir diâmetro de conexão por ponto
- 8.3.5 Calcular vazão por ponto (peso do aparelho × fórmula NBR 5626)
- 8.3.6 Definir altura de instalação por tipo de ponto
- 8.3.7 Mapear pontos existentes no modelo vs. pontos necessários
- 8.3.8 Gerar lista de pontos faltantes por ambiente
- 8.3.9 Gerar log de pontos identificados

### 8.4 Módulo 04 — Inserção e Validação de Equipamentos
- 8.4.1 Verificar famílias MEP disponíveis no modelo
- 8.4.2 Carregar famílias faltantes de biblioteca padrão
- 8.4.3 Detectar equipamentos já existentes nos ambientes
- 8.4.4 Validar posicionamento de equipamentos existentes
- 8.4.5 Calcular posição ideal para novos equipamentos (distância da parede, prumada)
- 8.4.6 Inserir vaso sanitário com posicionamento automático
- 8.4.7 Inserir lavatório com posicionamento automático
- 8.4.8 Inserir chuveiro com posicionamento automático
- 8.4.9 Inserir ralos com posicionamento automático
- 8.4.10 Inserir pia de cozinha com posicionamento automático
- 8.4.11 Inserir tanque com posicionamento automático
- 8.4.12 Inserir torneira de jardim com posicionamento automático
- 8.4.13 Validar connectors de cada equipamento inserido
- 8.4.14 Gerar relatório de inserção (inseridos, validados, com problemas)

### 8.5 Módulo 05 — Criação de Prumadas (Colunas Verticais)
- 8.5.1 Identificar posição ideal para prumadas (eixos hidráulicos)
- 8.5.2 Agrupar ambientes por proximidade horizontal (shafts)
- 8.5.3 Definir quantidade de prumadas necessárias
- 8.5.4 Criar prumada de água fria (vertical, sem inclinação)
- 8.5.5 Criar prumada de esgoto (tubo de queda)
- 8.5.6 Criar coluna de ventilação
- 8.5.7 Dimensionar diâmetro de cada prumada
- 8.5.8 Conectar prumadas aos níveis correspondentes
- 8.5.9 Validar posicionamento (não colidir com estrutura)
- 8.5.10 Gerar log de prumadas criadas

### 8.6 Módulo 06 — Geração de Rede de Água Fria
- 8.6.1 Definir ponto de alimentação (reservatório / barrilete)
- 8.6.2 Traçar barrilete de distribuição
- 8.6.3 Conectar barrilete às prumadas
- 8.6.4 Traçar ramais de distribuição por pavimento
- 8.6.5 Traçar sub-ramais até cada ponto de consumo
- 8.6.6 Definir rota com menor comprimento (otimização)
- 8.6.7 Evitar cruzamentos com estrutura e outras disciplinas
- 8.6.8 Inserir registros de gaveta por ambiente
- 8.6.9 Inserir registros de pressão onde necessário
- 8.6.10 Dimensionar todos os trechos (vazão → diâmetro)
- 8.6.11 Verificar pressão disponível em cada ponto
- 8.6.12 Atribuir todos os elementos ao PipingSystem de água fria
- 8.6.13 Gerar isométrico simplificado (se possível)
- 8.6.14 Gerar log da rede com comprimentos e diâmetros

### 8.7 Módulo 07 — Geração de Rede de Esgoto Sanitário
- 8.7.1 Traçar ramais de descarga por aparelho
- 8.7.2 Traçar ramais de esgoto por ambiente
- 8.7.3 Definir rota com convergência para tubo de queda
- 8.7.4 Inserir caixas sifonadas por ambiente (quando necessário)
- 8.7.5 Inserir caixas de inspeção/gordura
- 8.7.6 Traçar subcoletor até caixa de inspeção externa
- 8.7.7 Dimensionar todos os trechos por UHC
- 8.7.8 Atribuir todos os elementos ao PipingSystem de esgoto
- 8.7.9 Verificar conectividade completa
- 8.7.10 Gerar log da rede com UHCs acumulados e diâmetros

### 8.8 Módulo 08 — Aplicação de Inclinações
- 8.8.1 Identificar todos os trechos horizontais de esgoto
- 8.8.2 Aplicar inclinação de 2% para diâmetros ≤ 75mm
- 8.8.3 Aplicar inclinação de 1% para diâmetros ≥ 100mm
- 8.8.4 Ajustar elevação de tubos automaticamente
- 8.8.5 Verificar interferências após aplicação de inclinação
- 8.8.6 Ajustar conexões e fittings afetados
- 8.8.7 Validar que inclinação foi aplicada corretamente (verificação geométrica)
- 8.8.8 Gerar log de inclinações aplicadas

### 8.9 Módulo 09 — Criação de Sistemas MEP
- 8.9.1 Criar PipingSystem "AF - Água Fria" com configurações padronizadas
- 8.9.2 Criar PipingSystem "ES - Esgoto Sanitário" com configurações
- 8.9.3 Criar PipingSystem "VE - Ventilação" com configurações
- 8.9.4 Atribuir cada tubulação ao sistema correto
- 8.9.5 Atribuir cada conexão ao sistema correto
- 8.9.6 Validar que não há elementos sem sistema
- 8.9.7 Validar conectividade de cada sistema
- 8.9.8 Aplicar cores por sistema (azul AF, marrom ES, verde VE)
- 8.9.9 Gerar log de sistemas criados

### 8.10 Módulo 10 — Geração de Tabelas Quantitativas
- 8.10.1 Criar Schedule de tubulações por sistema (comprimento × diâmetro)
- 8.10.2 Criar Schedule de conexões por sistema (tipo × diâmetro × quantidade)
- 8.10.3 Criar Schedule de equipamentos por ambiente
- 8.10.4 Criar Schedule resumo de ambientes com pontos hidráulicos
- 8.10.5 Configurar formatação (cabeçalhos, unidades, agrupamento)
- 8.10.6 Implementar exportação para Excel/CSV
- 8.10.7 Gerar log de tabelas criadas

### 8.11 Módulo 11 — Geração de Pranchas
- 8.11.1 Criar View Template para planta de água fria (filtros, visibilidade)
- 8.11.2 Criar View Template para planta de esgoto (filtros, visibilidade)
- 8.11.3 Criar Floor Plan views por pavimento por sistema
- 8.11.4 Configurar escala e recorte de cada view
- 8.11.5 Criar ViewSheets com titleblock padrão
- 8.11.6 Posicionar views nas pranchas (layout automático)
- 8.11.7 Adicionar tabelas de quantitativo à prancha
- 8.11.8 Adicionar legendas e notas padrão
- 8.11.9 Numerar pranchas sequencialmente (HID-01, HID-02, etc.)
- 8.11.10 Gerar log de pranchas criadas

---

## 9. INTERFACE DO USUÁRIO (WPF)

### 9.1 Janela Principal
- 9.1.1 Projetar layout da janela principal (abas: Configuração, Execução, Diagnóstico)
- 9.1.2 Implementar shell MVVM (MainWindow + MainViewModel)
- 9.1.3 Implementar navegação entre abas
- 9.1.4 Implementar estilo visual coerente (paleta de cores, fontes)
- 9.1.5 Implementar responsividade da janela (redimensionamento)

### 9.2 Aba de Configuração
- 9.2.1 Implementar seção de parâmetros de água fria (pressão, altura reservatório)
- 9.2.2 Implementar seção de parâmetros de esgoto (declividades, ventilação)
- 9.2.3 Implementar seleção de norma aplicada
- 9.2.4 Implementar configuração de tipos de sistema MEP
- 9.2.5 Implementar botão salvar/carregar configuração (JSON)
- 9.2.6 Implementar validação de parâmetros com feedback visual

### 9.3 Aba de Execução
- 9.3.1 Implementar lista de etapas com status visual (não iniciada, em execução, concluída, com erro)
- 9.3.2 Implementar botão "Executar Etapa" por etapa individual
- 9.3.3 Implementar botão "Executar Todas" com pausa entre etapas
- 9.3.4 Implementar barra de progresso por etapa e geral
- 9.3.5 Implementar exibição de resumo por etapa concluída
- 9.3.6 Implementar botão de validação humana (aprovar/rejeitar etapa)
- 9.3.7 Implementar possibilidade de re-executar etapa específica

### 9.4 Aba de Diagnóstico
- 9.4.1 Implementar visualização de logs em tempo real (DataGrid)
- 9.4.2 Implementar filtros de log por nível (Crítico, Médio, Leve, Info)
- 9.4.3 Implementar filtros de log por etapa
- 9.4.4 Implementar destaque visual por nível (cores de fundo)
- 9.4.5 Implementar botão de exportar logs (JSON)
- 9.4.6 Implementar contadores de erro por nível
- 9.4.7 Implementar clique em log para selecionar elemento no Revit

### 9.5 Visualização de Ambientes
- 9.5.1 Implementar DataGrid de ambientes detectados
- 9.5.2 Implementar colunas: nome, tipo, confiança, área, nível, status
- 9.5.3 Implementar edição inline de classificação (combobox)
- 9.5.4 Implementar destaque de ambientes com confiança baixa
- 9.5.5 Implementar botão "Ir Para" que navega ao ambiente no modelo

---

## 10. SISTEMA DE LOGS E DIAGNÓSTICO

### 10.1 Logs Estruturados
- 10.1.1 Garantir que cada serviço gera logs adequados
- 10.1.2 Padronizar formato de mensagens (etapa/componente/mensagem)
- 10.1.3 Garantir rastreabilidade por ElementId
- 10.1.4 Implementar log de tempo de execução por etapa

### 10.2 Diagnóstico de Modelo
- 10.2.1 Implementar verificação de modelo antes da execução (pré-diagnóstico)
- 10.2.2 Verificar se existem Rooms definidos
- 10.2.3 Verificar se existem Levels definidos
- 10.2.4 Verificar se existem famílias MEP necessárias
- 10.2.5 Verificar se existem Pipe Types configurados
- 10.2.6 Gerar relatório de prontidão do modelo

### 10.3 Relatórios
- 10.3.1 Implementar relatório de execução completa (todas as etapas)
- 10.3.2 Implementar relatório de dimensionamento (água fria + esgoto)
- 10.3.3 Implementar relatório de inconsistências encontradas
- 10.3.4 Implementar exportação de relatórios para PDF ou HTML

---

## 11. TESTES E VALIDAÇÃO

### 11.1 Testes Unitários (PluginCore)
- 11.1.1 Testar `ClassificadorAmbientes` com corpus de 50+ variações de nomes
- 11.1.2 Testar `ValidadorAmbientes` com cenários de duplicatas, áreas inválidas
- 11.1.3 Testar cálculos de vazão provável (água fria)
- 11.1.4 Testar cálculos de UHC e dimensionamento (esgoto)
- 11.1.5 Testar cálculos de perda de carga
- 11.1.6 Testar `LogManager` (acumulação, exportação, bloqueio)
- 11.1.7 Testar `Orquestrador` (sequência, pré-condições, pausa)

### 11.2 Testes de Integração (Revit2026)
- 11.2.1 Testar leitura de Rooms em modelo real
- 11.2.2 Testar criação de Spaces em modelo real
- 11.2.3 Testar inserção de equipamentos em modelo real
- 11.2.4 Testar criação de tubulações em modelo real
- 11.2.5 Testar criação de sistemas MEP em modelo real
- 11.2.6 Testar geração de schedules em modelo real
- 11.2.7 Testar geração de pranchas em modelo real

### 11.3 Testes de Fluxo Completo
- 11.3.1 Executar fluxo completo em planta de 2 dormitórios
- 11.3.2 Executar fluxo completo em planta de 3 dormitórios com suíte
- 11.3.3 Executar fluxo completo em planta de 2 pavimentos
- 11.3.4 Validar resultados contra projeto manual de referência
- 11.3.5 Medir percentual de automação atingido (meta: 70–80%)

### 11.4 Validação Normativa
- 11.4.1 Validar dimensionamentos de água fria contra planilha de cálculo manual
- 11.4.2 Validar dimensionamentos de esgoto contra planilha de cálculo manual
- 11.4.3 Validar inclinações aplicadas contra norma
- 11.4.4 Validar configuração de ventilação contra norma
- 11.4.5 Obter revisão de engenheiro hidráulico

---

## 12. PADRONIZAÇÃO (Templates + Famílias)

### 12.1 Famílias MEP
- 12.1.1 Criar/adaptar família de vaso sanitário com connectors corretos
- 12.1.2 Criar/adaptar família de lavatório com connectors corretos
- 12.1.3 Criar/adaptar família de chuveiro com connectors corretos
- 12.1.4 Criar/adaptar família de ralo com connectors corretos
- 12.1.5 Criar/adaptar família de pia de cozinha com connectors corretos
- 12.1.6 Criar/adaptar família de tanque com connectors corretos
- 12.1.7 Criar/adaptar família de caixa sifonada
- 12.1.8 Criar/adaptar família de caixa de gordura
- 12.1.9 Criar/adaptar família de registro de gaveta
- 12.1.10 Validar que todas as famílias possuem connectors de água fria e/ou esgoto

### 12.2 Templates de Projeto
- 12.2.1 Criar Pipe Types padronizados (PVC água fria, PVC esgoto, PVC ventilação)
- 12.2.2 Criar View Templates para planta de água fria
- 12.2.3 Criar View Templates para planta de esgoto
- 12.2.4 Criar View Templates para isométrico
- 12.2.5 Criar Titleblock padrão para pranchas hidráulicas
- 12.2.6 Criar Filters para diferenciação visual por sistema
- 12.2.7 Criar Color Schemes para sistemas hidráulicos

### 12.3 Biblioteca de Dados
- 12.3.1 Consolidar JSON de parâmetros hidráulicos por tipo de aparelho
- 12.3.2 Consolidar JSON de pesos para cálculo de vazão (NBR 5626)
- 12.3.3 Consolidar JSON de UHC por aparelho (NBR 8160)
- 12.3.4 Consolidar JSON de diâmetros mínimos por tipo de ramal
- 12.3.5 Consolidar JSON de alturas de instalação por equipamento

---

## 13. DOCUMENTAÇÃO

### 13.1 Documentação Técnica
- 13.1.1 Documentar arquitetura geral do sistema (diagrama + descrição)
- 13.1.2 Documentar cada serviço do PluginCore (propósito, métodos, exemplos)
- 13.1.3 Documentar cada serviço do Revit2026 (integração API)
- 13.1.4 Documentar formato JSON de comunicação plugin ↔ Dynamo
- 13.1.5 Documentar pipeline de execução (etapas, dependências, pré-condições)
- 13.1.6 Documentar regras normativas implementadas

### 13.2 Documentação de Uso
- 13.2.1 Criar guia de instalação do plugin
- 13.2.2 Criar guia de configuração inicial (parâmetros hidráulicos)
- 13.2.3 Criar guia de preparação do modelo (Rooms, Levels, Famílias)
- 13.2.4 Criar guia de execução passo a passo (cada etapa)
- 13.2.5 Criar FAQ com problemas comuns e soluções
- 13.2.6 Criar checklist de pré-requisitos do modelo

### 13.3 Documentação por Etapa
- 13.3.1 Documentar Etapa 01 — Ambientes
- 13.3.2 Documentar Etapa 02 — Classificação
- 13.3.3 Documentar Etapa 03 — Pontos hidráulicos
- 13.3.4 Documentar Etapa 04 — Equipamentos
- 13.3.5 Documentar Etapa 05 — Prumadas
- 13.3.6 Documentar Etapa 06 — Rede de água fria
- 13.3.7 Documentar Etapa 07 — Rede de esgoto
- 13.3.8 Documentar Etapa 08 — Inclinações
- 13.3.9 Documentar Etapa 09 — Sistemas MEP
- 13.3.10 Documentar Etapa 10 — Tabelas
- 13.3.11 Documentar Etapa 11 — Pranchas

---

## 14. OTIMIZAÇÃO E REFINO

### 14.1 Performance
- 14.1.1 Implementar batch processing para operações em massa (transactions agrupadas)
- 14.1.2 Otimizar FilteredElementCollector (filtros rápidos vs. lentos)
- 14.1.3 Implementar cache de dados de modelo (evitar releituras)
- 14.1.4 Medir e otimizar tempo de execução por etapa
- 14.1.5 Implementar progresso assíncrono na UI durante operações longas

### 14.2 Robustez
- 14.2.1 Implementar tratamento de exceções em todas as camadas
- 14.2.2 Implementar undo/rollback por etapa (transaction groups)
- 14.2.3 Implementar recuperação de estado após crash
- 14.2.4 Implementar validação de modelo antes de cada etapa
- 14.2.5 Implementar detecção de modelo corrompido ou incompleto

### 14.3 Usabilidade
- 14.3.1 Refinar mensagens de erro para serem acionáveis pelo usuário
- 14.3.2 Adicionar tooltips explicativos na UI
- 14.3.3 Implementar atalhos de teclado para operações frequentes
- 14.3.4 Implementar highlight visual de elementos com problemas no modelo
- 14.3.5 Implementar preview antes de executar alterações no modelo

### 14.4 Extensibilidade
- 14.4.1 Refatorar classificador para suportar dicionário externo (JSON editável)
- 14.4.2 Refatorar regras de posicionamento para suportar customização
- 14.4.3 Preparar arquitetura para futuro suporte a água quente
- 14.4.4 Preparar arquitetura para futuro suporte a incêndio
- 14.4.5 Preparar arquitetura para futuro suporte a pluvial

---

## MAPA DE DEPENDÊNCIAS ENTRE FASES

```
1. Planejamento ──────────────────────────┐
2. Arquitetura ───────────────────────────┤
3. Setup ─────────────────────────────────┤
                                          ▼
4. Core ──────────┬── 5. Integração Revit ┬── 6. Dynamo
                  │                       │
                  │                       ├── 7. unMEP
                  │                       │
                  └───────────────────────┼── 8. Módulos Funcionais
                                          │       (8.1 → 8.2 → 8.3 → 8.4 →
                                          │        8.5 → 8.6/8.7 → 8.8 →
                                          │        8.9 → 8.10 → 8.11)
                                          │
                                    9. Interface UI
                                    10. Logs/Diagnóstico
                                          │
                                    11. Testes ──── 12. Padronização
                                          │
                                    13. Documentação
                                          │
                                    14. Otimização
```

---

## RESUMO QUANTITATIVO

| Fase | Subfases | Tarefas |
|------|----------|---------|
| 1. Planejamento | 3 | 18 |
| 2. Arquitetura | 4 | 24 |
| 3. Setup | 4 | 18 |
| 4. Core | 7 | 49 |
| 5. Integração Revit | 8 | 46 |
| 6. Dynamo | 5 | 15 |
| 7. unMEP | 2 | 7 |
| 8. Módulos Funcionais | 11 | 103 |
| 9. Interface UI | 5 | 29 |
| 10. Logs/Diagnóstico | 3 | 14 |
| 11. Testes | 4 | 23 |
| 12. Padronização | 3 | 24 |
| 13. Documentação | 3 | 19 |
| 14. Otimização | 4 | 20 |
| **TOTAL** | **66** | **409** |
