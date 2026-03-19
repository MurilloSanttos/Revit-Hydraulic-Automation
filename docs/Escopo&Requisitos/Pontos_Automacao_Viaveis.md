# Pontos de Automação Viáveis — Plugin Hidráulico Revit 2026

> Análise técnica e realista das oportunidades de automação no fluxo de projeto hidráulico residencial, com mapeamento de viabilidade, limites, riscos e estratégia de implementação.

---

## 1. Visão Geral da Automação

### 1.1 Percentual estimado alcançável

**Automação total estimada: 72–78%** do fluxo de projeto hidráulico residencial.

Justificativa:

| Faixa | Razão |
|-------|-------|
| Piso (72%) | Modelo arquitetônico com qualidade média, famílias parcialmente padronizadas, rotas com interferências que exigem ajuste manual |
| Teto (78%) | Modelo bem preparado, famílias padronizadas, planta regular sem interferências complexas |
| Barreira dos 80% | Ultrapassar 78% exigiria automação de decisões que dependem de experiência contextual e julgamento do projetista (layout irregular, interferências complexas, exceções normativas) |

**Distribuição da automação por natureza da tarefa:**

| Natureza | Automação | Justificativa |
|----------|-----------|---------------|
| Leitura e análise de dados | 95% | API do Revit permite leitura completa |
| Cálculos normativos | 95% | Regras determinísticas, tabelas fixas |
| Classificação e decisão | 85% | NLP funciona bem para nomes em português; 15% exige validação humana |
| Criação de elementos simples | 90% | API suporta criação de Pipes, Fittings, Spaces |
| Traçado de rotas | 60% | Rotas simples via Dynamo/unMEP; rotas complexas exigem ajuste |
| Posicionamento de equipamentos | 55% | Regras simplificadas funcionam para layouts regulares; irregulares exigem humano |
| Documentação (tabelas/pranchas) | 80% | Schedules via API; pranchas precisam ajuste de layout |
| Revisão e compatibilização | 30% | Clash detection básico via API; resolução é humana |

### 1.2 Estratégia geral

O plugin atua em 3 modos:

1. **Executor direto** — operações determinísticas que não requerem julgamento (leitura, cálculos, criação de sistemas)
2. **Assistente inteligente** — propõe soluções com base em regras, aguarda aprovação (posicionamento, traçado)
3. **Validador** — verifica trabalho feito (manual ou automático) e aponta erros

**Princípio fundamental:** automatizar o que é repetitivo e determinístico; assistir o que exige julgamento; nunca substituir a responsabilidade do projetista.

### 1.3 Papel do usuário

| Momento | Ação do usuário |
|---------|----------------|
| Antes de iniciar | Preparar modelo (Rooms nomeados, famílias carregadas) |
| Configuração | Definir parâmetros hidráulicos |
| Entre cada etapa | Validar resultado (aprovar/rejeitar) |
| Classificação | Corrigir ambientes com confiança baixa |
| Equipamentos | Validar posições, ajustar manualmente se necessário |
| Redes | Revisar rotas, ajustar trechos com interferência |
| Pranchas | Ajustar layout, adicionar notas específicas |
| Final | Revisão completa antes de entrega |

---

## 2. Classificação das Etapas

| # | Etapa | Classificação | Automação |
|---|-------|--------------|-----------|
| 01 | Análise do modelo | Totalmente automatizável | 95% |
| 02 | Definição de parâmetros | Totalmente automatizável | 90% |
| 03 | Locação de equipamentos | Parcialmente automatizável | 55% |
| 04 | Definição de prumadas | Parcialmente automatizável | 65% |
| 05 | Rede de água fria | Parcialmente automatizável | 65% |
| 06 | Rede de esgoto | Parcialmente automatizável | 65% |
| 07 | Inclinações | Totalmente automatizável | 95% |
| 08 | Ventilação | Parcialmente automatizável | 60% |
| 09 | Dimensionamento | Totalmente automatizável | 95% |
| 10 | Sistemas MEP | Totalmente automatizável | 90% |
| 11 | Compatibilização | Parcialmente automatizável | 35% |
| 12 | Tabelas | Totalmente automatizável | 90% |
| 13 | Pranchas | Parcialmente automatizável | 70% |
| 14 | Memorial e entrega | Não automatizável | 5% |

**Média ponderada (por tempo gasto):** ~74%

---

## 3. Mapeamento Detalhado de Automação

---

### Etapa 01 — Análise do Modelo Arquitetônico

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 95% |
| **O que automatiza** | Leitura de Rooms (nome, número, nível, área, perímetro, ponto central). Filtragem de Rooms inválidos. Leitura de Levels. Leitura de Spaces existentes. Correspondência Room↔Space. Criação de Spaces faltantes. Verificação de qualidade do modelo (paredes abertas, Rooms sem nome). Classificação de ambientes por nome (NLP). |
| **O que NÃO automatiza** | Interpretação de ambientes sem nome ou com nome ambíguo. Correção de paredes não fechadas. Decisão sobre Rooms sobrepostos (qual é correto). Comunicação com o arquiteto para solicitar correções. |
| **Ferramenta** | Plugin (C#) — leitura via `FilteredElementCollector`, classificação via `ClassificadorAmbientes` |
| **Complexidade** | Baixa |
| **Riscos técnicos** | Rooms com nomes fora do dicionário não são classificados. Correspondência Room↔Space por proximidade pode falhar em plantas muito compactas. |
| **Dependência do modelo** | **Alta** — exige Rooms definidos, Levels corretos, paredes fechadas |

---

### Etapa 02 — Definição de Parâmetros

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 90% |
| **O que automatiza** | Carregamento de configuração padrão (JSON). Cálculo de consumo diário (população × 150 L). Definição de material padrão (PVC). Aplicação automática de normas (NBR 5626, NBR 8160). Persistência de configuração entre sessões. |
| **O que NÃO automatiza** | Decisão sobre tipo de sistema quando há exceções (pressurizador, alimentação direta). Preferências específicas do cliente (marca, modelo). Informações externas (pressão da rede pública local). |
| **Ferramenta** | Plugin (C#) — leitura e gravação de JSON |
| **Complexidade** | Baixa |
| **Riscos técnicos** | Nenhum significativo |
| **Dependência do modelo** | Baixa — depende apenas de Levels para calcular alturas |

---

### Etapa 03 — Locação de Equipamentos

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 55% |
| **O que automatiza** | Determinação de quais equipamentos cada ambiente precisa (tabela tipo→equipamentos). Detecção de equipamentos já existentes. Validação de equipamentos existentes (connectors, posição). Cálculo de posição ideal em ambientes regulares (retangulares): parede mais distante da porta para vaso, centralizado para lavatório, canto para chuveiro. Inserção via API com rotação automática. |
| **O que NÃO automatiza** | Posicionamento em ambientes irregulares (L, T, triangulares). Resolução de conflitos com mobiliário (definido pelo arquiteto). Decisão quando há múltiplas paredes viáveis e nenhuma é claramente melhor. Posicionamento de banheira (variações de modelo muito grandes). Ajuste estético (alinhamento com azulejo, centralização visual). |
| **Ferramenta** | Plugin (C#) para decisão + Dynamo para inserção em massa |
| **Complexidade** | **Alta** |
| **Riscos técnicos** | Posicionamento automático em planta irregular pode colocar equipamento em posição inviável. Famílias sem connectors impedem conexão posterior. Famílias com dimensões diferentes das esperadas podem colidir. |
| **Dependência do modelo** | **Muito alta** — exige paredes fechadas, portas posicionadas, famílias MEP com connectors. Layout irregular reduz automação para ~30%. |

---

### Etapa 04 — Definição de Prumadas

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 65% |
| **O que automatiza** | Clustering de ambientes por proximidade 2D (identificação de eixos hidráulicos). Cálculo de centroide de cada cluster para posição ideal. Definição de tipos necessários (AF, ES, VE) por cluster. Criação de Pipe vertical conectando Levels. Dimensionamento preliminar por carga acumulada. |
| **O que NÃO automatiza** | Decisão quando shaft está pré-definido pelo arquiteto mas não está em posição ótima. Resolução de colisão prumada vs. estrutura (pilares, vigas). Decisão sobre prumadas compartilhadas (pavimentos com layouts muito diferentes). Prumada em parede vs. shaft embutido (decisão construtiva). |
| **Ferramenta** | Plugin (C#) para clustering e decisão + Plugin para criação de Pipes |
| **Complexidade** | Média |
| **Riscos técnicos** | Centroide automático pode cair sobre um pilar. Em edificações com plantas desalinhadas entre pavimentos, clustering pode ser incorreto. |
| **Dependência do modelo** | Alta — exige Levels corretos. Presença de modelo estrutural melhora qualidade (detecção de pilares). |

---

### Etapa 05 — Rede de Água Fria

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 65% |
| **O que automatiza** | Definição da topologia da rede (reservatório → barrilete → prumada → ramal → sub-ramal → equipamento). Cálculo de comprimento de cada trecho. Dimensionamento completo (vazão, diâmetro, velocidade, perda de carga, pressão). Definição de pontos de registro. Criação de Pipes e Fittings para rotas retilíneas (sem obstáculo). Atribuição ao PipingSystem. |
| **O que NÃO automatiza** | Traçado de rotas complexas (desvio de vigas, pilares, eletrodutos). Decisão sobre caminho quando há múltiplas opções equivalentes. Rota em ambientes com geometria irregular. Posição exata do barrilete (depende de cobertura/telhado). Resolução de interferência pós-traçado. |
| **Ferramenta** | Plugin (C#) para dimensionamento e topologia. Dynamo para traçado de ramais simples. unMEP para rotas com obstáculos. |
| **Complexidade** | **Alta** |
| **Riscos técnicos** | Dynamo tem dificuldade com pathfinding em ambientes com muitos obstáculos. unMEP pode gerar rotas com curvas excessivas. Desconexão entre trechos gerados por ferramentas diferentes. |
| **Dependência do modelo** | Muito alta — exige prumadas definidas, equipamentos com connectors, modelo limpo. Modelo estrutural linkado melhora desvio de obstáculos significativamente. |

---

### Etapa 06 — Rede de Esgoto

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 65% |
| **O que automatiza** | Definição da topologia (equipamento → ramal de descarga → CX sifonada → ramal de esgoto → tubo de queda → subcoletor). Dimensionamento por UHC. Decisão de onde inserir CX sifonada (por ambiente), CX gordura (cozinha), CX inspeção (mudanças de direção). Criação de Pipes gravitacionais simples. Atribuição ao PipingSystem. |
| **O que NÃO automatiza** | Traçado de subcoletor externo (depende de informação de implantação). Rota dentro do piso quando há vigas invertidas ou rebaixos. Posição exata da CX de inspeção externa (depende de terreno). Resolução de conflitos de elevação (não há espaço entre lajes para inclinação). |
| **Ferramenta** | Plugin (C#) para topologia e dimensionamento. Dynamo para traçado de ramais. unMEP para subcoletor. |
| **Complexidade** | **Alta** |
| **Riscos técnicos** | Esgoto é gravitacional — qualquer erro de elevação compromete o escoamento. Rota de subcoletor depende de informação frequentemente indisponível (implantação). |
| **Dependência do modelo** | Muito alta — mesmas dependências da AF + informação de implantação para subcoletor |

---

### Etapa 07 — Aplicação de Inclinações

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 95% |
| **O que automatiza** | Identificação de todos os trechos horizontais de esgoto. Determinação de inclinação por diâmetro (2% ou 1%). Cálculo de novo Z do endpoint. Ajuste de elevação. Reajuste de fittings conectados. Verificação de interferência pós-ajuste (simplificada). Relatório de cada trecho ajustado. |
| **O que NÃO automatiza** | Resolução quando não há espaço vertical (laje muito fina). Decisão de quebrar trecho em 2 para viabilizar inclinação. Ajuste fino quando fitting desconecta e Revit não reconecta automaticamente. |
| **Ferramenta** | Dynamo para ajuste em massa de elevações |
| **Complexidade** | Média |
| **Riscos técnicos** | Fittings podem desconectar e exigir reconexão manual no Revit. Em lajes com pouca espessura (< 15cm), pode não haver espaço. |
| **Dependência do modelo** | Média — depende de rede de esgoto traçada. Informação de espessura de laje melhora validação. |

---

### Etapa 08 — Ventilação

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 60% |
| **O que automatiza** | Identificação de pontos que necessitam ventilação (distância do tubo de queda > limite). Dimensionamento da coluna de ventilação. Criação de Pipe vertical de ventilação. Conexão ramal de ventilação ao ramal de esgoto. |
| **O que NÃO automatiza** | Rota de ramais de ventilação em ambientes com forro ou espaço limitado. Passagem de ventilação através de laje/cobertura (depende de projeto arquitetônico). Posição do terminal de ventilação na cobertura. |
| **Ferramenta** | Plugin (C#) para decisão + Dynamo para traçado |
| **Complexidade** | Média |
| **Riscos técnicos** | Rota de ventilação deve sempre subir — Dynamo pode traçar trecho descendente. |
| **Dependência do modelo** | Alta — exige modelo de cobertura para posicionar terminal |

---

### Etapa 09 — Dimensionamento Hidráulico

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 95% |
| **O que automatiza** | **AF:** Soma de pesos por trecho. Cálculo Q = 0.3 × √ΣP. Seleção de diâmetro por V ≤ 3 m/s. Cálculo de perda de carga (Fair-Whipple-Hsiao). Verificação de pressão (≥ 3 m.c.a., ≤ 40 m.c.a.). Identificação do caminho crítico. **ES:** Soma de UHC por trecho. Dimensionamento por tabela NBR 8160. Verificação de não-diminuição de diâmetro. Aplicação automática de diâmetros no modelo. |
| **O que NÃO automatiza** | Decisão sobre pressurizador quando pressão é insuficiente (envolve análise de custo). Escolha de material alternativo quando PVC não é adequado (raro em residencial). |
| **Ferramenta** | Plugin (C#) — cálculo puro, sem manipulação do modelo exceto atualizar parâmetro de diâmetro |
| **Complexidade** | Média |
| **Riscos técnicos** | Precisão de ±10% nas perdas localizadas (estimativa de 20%). Aceitável por norma. |
| **Dependência do modelo** | Média — precisa de topologia da rede (comprimentos). Não depende de geometria complexa. |

---

### Etapa 10 — Sistemas MEP

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 90% |
| **O que automatiza** | Criação de PipingSystems (AF, ES, VE). Atribuição de cada elemento ao sistema correto (por tipo de Pipe e localização). Validação de elementos sem sistema. Verificação de conectividade. Aplicação de cores por sistema. |
| **O que NÃO automatiza** | Resolução de elementos ambíguos (tee na fronteira entre sistemas). Reconexão de elementos desconectados. |
| **Ferramenta** | Plugin (C#) — operações diretas da API |
| **Complexidade** | Baixa |
| **Riscos técnicos** | Elementos sem connectors compatíveis não podem ser atribuídos. |
| **Dependência do modelo** | Baixa — depende apenas dos elementos MEP já criados |

---

### Etapa 11 — Compatibilização

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 35% |
| **O que automatiza** | Clash detection simplificado (BoundingBox intersection). Verificação de conectividade (elementos desconectados). Verificação de diâmetros vs. dimensionamento. Verificação de inclinação aplicada. Relatório de problemas encontrados. |
| **O que NÃO automatiza** | Clash detection preciso (geometria real — requer Navisworks). Resolução de interferências (decisão humana + edição manual). Coordenação com outras disciplinas (elétrica, estrutura). Análise de construtibilidade (acessibilidade em obra). |
| **Ferramenta** | Plugin (C#) para verificações. Navisworks para clash avançado (fora do escopo). |
| **Complexidade** | Alta |
| **Riscos técnicos** | BoundingBox intersection pode gerar falsos positivos em elementos próximos mas sem colisão real. |
| **Dependência do modelo** | Muito alta — depende de modelo multidisciplinar (estrutura, elétrica) para ser efetivo |

---

### Etapa 12 — Tabelas

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 90% |
| **O que automatiza** | Criação de ViewSchedules via API. Configuração de campos, filtros, agrupamento. Formatação de unidades e totais. 4 schedules: tubulações, conexões, equipamentos, resumo por ambiente. |
| **O que NÃO automatiza** | Formatação visual muito específica (cores de cabeçalho, merge de células — limitação da API). Tabelas personalizadas não previstas no template. |
| **Ferramenta** | Plugin (C#) para Schedules padrão. Dynamo para exportação Excel. |
| **Complexidade** | Baixa |
| **Riscos técnicos** | API do Revit tem limitações em formatação de Schedule. |
| **Dependência do modelo** | Baixa — depende apenas de elementos criados com parâmetros corretos |

---

### Etapa 13 — Pranchas

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 70% |
| **O que automatiza** | Criação de View Templates com filtros por sistema. Duplicação de Floor Plans com template aplicado. Configuração de escala e Crop Region. Criação de ViewSheets com titleblock. Posicionamento inicial de views (layout por algoritmo). Numeração sequencial (HID-01...). Inserção de Schedules na prancha. |
| **O que NÃO automatiza** | Ajuste fino de layout (overlap de views, espaçamento estético). Adição de notas e textos específicos do projeto. Legenda personalizada (varia por escritório). Ajuste de escala quando planta não cabe na folha. Revisão visual de qualidade de impressão. |
| **Ferramenta** | Plugin (C#) para criação de Sheets e Views. Dynamo para posicionamento e layout. |
| **Complexidade** | Média |
| **Riscos técnicos** | Algoritmo de layout pode sobrepor views em pranchas densas. Titleblock pode não ter campos esperados. |
| **Dependência do modelo** | Média — exige titleblock padrão e View Templates |

---

### Etapa 14 — Memorial e Entrega

| Campo | Detalhe |
|-------|---------|
| **Automação possível** | 5% |
| **O que automatiza** | Exportação de dados de dimensionamento para JSON (para uso em template de memorial). Geração de PDF das pranchas via API (PrintManager). |
| **O que NÃO automatiza** | Redação do memorial descritivo (texto livre). Compilação da documentação. Comunicação com cliente. Iterações de revisão. |
| **Ferramenta** | Plugin (C#) para exportação de dados |
| **Complexidade** | Baixa (para a parte automatizável) |
| **Riscos técnicos** | Nenhum |
| **Dependência do modelo** | Nenhuma |

---

## 4. Estratégia de Automação por Módulo

### Matriz consolidada

| Módulo | Plugin (C#) | Dynamo | unMEP | Usuário | Automação |
|--------|------------|--------|-------|---------|-----------|
| Detecção de ambientes | Leitura, filtragem, correspondência | Criação em massa de Spaces | — | Validar, corrigir nomes | 95% |
| Classificação | Motor NLP completo | — | — | Reclassificar confiança baixa | 85% |
| Pontos hidráulicos | Tabela tipo→equipamentos, vazão | — | — | Adicionar pontos extras | 90% |
| Inserção de equipamentos | Decisão de posição | Inserção paramétrica | — | Validar posições | 55% |
| Validação de equipamentos | Verificação completa | Distâncias | — | Corrigir inválidos | 80% |
| Prumadas | Clustering, dimensionamento | Criação em massa | — | Validar posição | 65% |
| Rede de água fria | Dimensionamento, topologia | Traçado simples | Rotas complexas | Revisar rotas | 65% |
| Rede de esgoto | Dimensionamento, topologia | Traçado simples | Subcoletor | Revisar rotas | 65% |
| Inclinações | Cálculo | Ajuste em massa | — | Verificar espaço | 95% |
| Sistemas MEP | Criação e atribuição total | — | — | Verificar | 90% |
| Dimensionamento | Cálculo completo AF+ES | — | — | Validar | 95% |
| Tabelas | Criação de Schedules | Exportação Excel | — | Revisar | 90% |
| Pranchas | Sheets, Views, Templates | Layout | — | Ajustar layout | 70% |

### Cálculo de automação total ponderada por tempo

| Módulo | Peso (% do tempo total) | Automação (%) | Contribuição |
|--------|------------------------|---------------|-------------|
| Detecção + Classificação | 8% | 90% | 7.2% |
| Pontos hidráulicos | 3% | 90% | 2.7% |
| Equipamentos (inserção + validação) | 10% | 60% | 6.0% |
| Prumadas | 5% | 65% | 3.3% |
| Redes (AF + ES) | 25% | 65% | 16.3% |
| Inclinações | 8% | 95% | 7.6% |
| Ventilação | 5% | 60% | 3.0% |
| Dimensionamento | 12% | 95% | 11.4% |
| Sistemas MEP | 4% | 90% | 3.6% |
| Compatibilização | 5% | 35% | 1.8% |
| Tabelas | 5% | 90% | 4.5% |
| Pranchas | 8% | 70% | 5.6% |
| Memorial | 2% | 5% | 0.1% |
| **TOTAL** | **100%** | — | **73.1%** |

**Automação total ponderada: ~73%** (dentro da meta de 70–80%).

---

## 5. Limites da Automação

### 5.1 Onde a automação falha

| Cenário | Por que falha | O que acontece |
|---------|--------------|----------------|
| Planta com geometria muito irregular (L, T, circular) | Algoritmo de posicionamento assume retângulo | Equipamentos em posição inviável |
| Rooms sem nome ou com nome em outro idioma | Classificador depende de dicionário pt-BR | Classificação `NaoIdentificado` |
| Modelo sem Rooms definidos | Sem Rooms = sem base para análise | Erro Crítico, pipeline não inicia |
| Viga invertida no caminho da tubulação | Dynamo não tem clash detection | Tubulação atravessa a viga |
| Subcoletor externo | Depende de implantação (não modelada) | Rota incompleta |
| Laje com espessura insuficiente para inclinação | Plugin calcula mas não resolve | Alerta ao usuário |
| Famílias de terceiros sem connectors | Equipamento fica "solto" | Não conectável à rede |

### 5.2 Decisões humanas indispensáveis

| Decisão | Por que é humana |
|---------|-----------------|
| Aceitar posição de equipamento | Questão de ergonomia e estética — contexto visual |
| Escolher rota entre alternativas equivalentes | Envolve preferência construtiva e experiência |
| Resolver interferência | Cada caso é único — subir, desviar, redirecionar |
| Validar dimensionamento | Responsabilidade técnica do engenheiro |
| Definir necessidade de pressurizador | Análise de custo-benefício |
| Aprovar pranchas | Qualidade de apresentação é subjetiva |
| Layout irregular | Julgamento sobre funcionalidade vs. estética |

### 5.3 Pontos de parada obrigatória (sistema DEVE pausar)

| Ponto de parada | Condição |
|-----------------|----------|
| Após classificação | Ambientes com confiança < 70% existem |
| Antes de criar Spaces | Confirmação explícita do usuário |
| Após posicionar equipamentos | Validação visual obrigatória |
| Após traçar rede AF | Verificar rotas antes de dimensionar |
| Após traçar rede ES | Verificar convergência para tubos de queda |
| Após aplicar inclinação | Verificar interferências de elevação |
| Pressão insuficiente detectada | Decisão sobre pressurizador |
| Após gerar pranchas | Revisão de layout |
| Qualquer erro Crítico | Bloqueio imediato do pipeline |

---

## 6. Dependências Críticas

### 6.1 Qualidade do modelo arquitetônico

| Requisito | Impacto se ausente | Severidade |
|-----------|-------------------|-----------|
| Rooms definidos e nomeados | Pipeline não inicia | Crítico |
| Rooms com nomes em português | Classificação falha | Alto |
| Paredes fechadas (Rooms com área > 0) | Rooms ignorados | Alto |
| Portas posicionadas | Posicionamento de equipamentos incorreto | Médio |
| Levels corretos | Alturas e pressão erradas | Crítico |
| Modelo atualizado | Projeto hidráulico sobre planta errada | Crítico |

### 6.2 Padronização de famílias MEP

| Requisito | Impacto se ausente | Severidade |
|-----------|-------------------|-----------|
| Families com connectors de AF (DomesticColdWater) | Não conecta à rede AF | Crítico |
| Families com connectors de ES (Sanitary) | Não conecta à rede ES | Crítico |
| Connectors na posição correta | Pipe conecta no lugar errado | Alto |
| Connector com diâmetro configurado | Diâmetro do sub-ramal incorreto | Médio |
| Dimensões da família compatíveis com modelo | Colisão visual | Baixo |

### 6.3 Configuração do template MEP

| Requisito | Impacto se ausente | Severidade |
|-----------|-------------------|-----------|
| Pipe Types definidos (PVC AF, PVC ES, PVC VE) | Criação de Pipes falha | Crítico |
| Pipe Sizes com diâmetros comerciais brasileiros | Diâmetros não disponíveis | Crítico |
| Fitting families carregadas (tee, curva, redução) | Conexões sem fitting | Alto |
| System Types configurados | Atribuição a sistema falha | Alto |

### 6.4 Consistência de nomenclatura

| Requisito | Impacto se ausente | Severidade |
|-----------|-------------------|-----------|
| Nomes de Rooms em português | Classificador cobre pt-BR | Crítico |
| Nomes padronizados no escritório | Classificação mais precisa | Médio |
| Sem caracteres especiais no nome | Normalização pode falhar em casos extremos | Baixo |

---

## 7. Riscos e Restrições Técnicas

### 7.1 Limitações do Dynamo

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Sem pathfinding avançado | Não desvia de obstáculos automaticamente | Delegar ao unMEP quando necessário |
| Performance em modelos > 200 MB | Scripts lentos (> 2 min) | Dividir execução por pavimento |
| Scripts .dyn não são versionáveis como código | Difícil manutenção e debug | Documentar I/O em JSON |
| Execução é single-thread | Bloqueia UI do Revit durante execução | Progress bar no Dynamo |
| API do Dynamo muda entre versões | Scripts podem quebrar em atualização | Testar em cada versão |

### 7.2 Limitações do unMEP

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Curvas excessivas em rotas complexas | Maior custo e perda de carga | Validação pós-roteamento |
| Configuração inicial complexa | Tempo de setup por projeto | Template de configuração |
| Não respeita declividade de esgoto | Rota pode ser impossível com inclinação | Plugin aplica inclinação depois |
| Resultado imprevisível em alguns layouts | Rotas não intuitivas | Revisão obrigatória |

### 7.3 Limitações da API do Revit

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Operações de escrita exigem Transaction | Complexidade de gerenciamento | Transaction wrapper no plugin |
| UI não pode modificar modelo (thread-safety) | Necessidade de ExternalEvent | Implementar fila de eventos |
| `NewSpace()` pode retornar null | Criação de Space pode falhar | Tratamento de erro + log |
| Schedule formatting limitado | Tabelas sem merge de células | Exportação para Excel |
| Connector mismatch pode impedir conexão | Pipes não conectam | Validação prévia de connectors |
| `Pipe.Create()` exige System Type válido | Criação falha se type não existe | Verificação pré-criação |

---

## 8. Estratégia de Implementação

### 8.1 Ordem recomendada de desenvolvimento

A implementação deve seguir a **cadeia de dependências** — cada módulo depende dos anteriores.

```
FASE 1 — Fundação (sem manipulação de modelo)
├── 1.1 Sistema de Logs (base para todos)
├── 1.2 Detecção de Ambientes (Rooms + Spaces)
├── 1.3 Classificação de Ambientes
└── 1.4 Interface WPF básica (diagnóstico + logs)

FASE 2 — Análise e Decisão (leitura do modelo)
├── 2.1 Identificação de Pontos Hidráulicos
├── 2.2 Motor de Dimensionamento AF (cálculos puros)
├── 2.3 Motor de Dimensionamento ES (cálculos puros)
└── 2.4 Validação de Equipamentos Existentes

FASE 3 — Criação de Elementos (manipulação do modelo)
├── 3.1 Inserção de Equipamentos
├── 3.2 Criação de Prumadas
├── 3.3 Integração com Dynamo (infraestrutura JSON)
└── 3.4 Interface WPF completa (execução por etapas)

FASE 4 — Redes (maior complexidade)
├── 4.1 Traçado de Rede AF (Dynamo + unMEP)
├── 4.2 Traçado de Rede ES (Dynamo + unMEP)
├── 4.3 Aplicação de Inclinações (Dynamo)
├── 4.4 Ventilação
└── 4.5 Criação de Sistemas MEP

FASE 5 — Documentação
├── 5.1 Geração de Tabelas
├── 5.2 Geração de Pranchas
└── 5.3 Exportação de dados

FASE 6 — Refinamento
├── 6.1 Otimização de performance
├── 6.2 Robustez e tratamento de erros
├── 6.3 Testes completos
└── 6.4 Documentação final
```

### 8.2 O que desenvolver primeiro

**Prioridade máxima (Fase 1):** Logs + Detecção + Classificação + UI básica

Justificativa:
- Estabelece a fundação sobre a qual tudo funciona
- É de baixa complexidade e alto retorno imediato
- Permite validar a arquitetura antes de investir em módulos complexos
- Já foi implementado na Etapa 1 do projeto atual

### 8.3 Dependências entre módulos

```
Logs ──────────────────────────────────────────────────────────┐
                                                                │
Detecção ──▶ Classificação ──▶ Pontos Hidráulicos ──┐          │
                                                      │          │
                                     Validação Equip ─┤          │
                                                      ▼          │
                                     Inserção Equip ──▶ Prumadas │
                                                          │       │
                                                          ▼       │
                                    Dimensionamento AF ◀──┤       │
                                    Dimensionamento ES ◀──┤       │
                                                          │       │
                                             Rede AF ◀────┤       │
                                             Rede ES ◀────┤       │
                                                          │       │
                                         Inclinações ◀────┘       │
                                                │                 │
                                         Ventilação               │
                                                │                 │
                                       Sistemas MEP ◀────────────┘
                                                │
                                            Tabelas
                                                │
                                           Pranchas
```

Regra: **nenhum módulo pode ser desenvolvido antes de seus predecessores estarem implementados e testados**.
