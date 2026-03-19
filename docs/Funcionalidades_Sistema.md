# Funcionalidades do Sistema — Plugin Hidráulico Revit 2026

> Documento completo de especificação funcional do sistema de automação hidráulica residencial integrado ao Autodesk Revit.

---

## 1. Visão Geral do Sistema

O plugin hidráulico é um orquestrador semi-automático que opera dentro do Autodesk Revit 2026, especializado em projetos hidráulicos residenciais brasileiros.

**Papel do sistema:**
- Analisa o modelo arquitetônico (Rooms, Levels, paredes)
- Toma decisões baseadas em regras normativas (NBR 5626, NBR 8160)
- Define parâmetros hidráulicos (vazões, diâmetros, inclinações)
- Dispara automações via Dynamo e unMEP
- Valida resultados e gera diagnósticos

**O sistema NÃO é:**
- Um executor direto de todas as operações MEP
- Um substituto para o projetista (é um assistente de 70–80%)
- Um produto comercial (uso interno)

**Fluxo operacional:**
```
Modelo Arquitetônico → Análise → Decisão → Parametrização → Execução → Validação → Documentação
```

**Modo de operação:** Semi-automático — cada etapa executa, gera logs, aponta erros e aguarda validação humana antes de avançar.

**Normas obrigatórias:**
- NBR 5626 — Instalações prediais de água fria
- NBR 8160 — Sistemas prediais de esgoto sanitário

**Parâmetros base:**
- Sistema com reservatório superior (altura padrão: 6 m)
- Pressão mínima: 3 m.c.a.
- Esgoto com ventilação
- Declividade: 2% (≤ 75mm), 1% (≥ 100mm)

---

## 2. Lista de Funcionalidades (Visão Macro)

| # | Funcionalidade | Categoria |
|---|----------------|-----------|
| F01 | Detecção de Ambientes (Rooms + Spaces) | Análise de Modelo |
| F02 | Classificação Avançada de Ambientes | Análise de Modelo |
| F03 | Identificação de Pontos Hidráulicos | Análise de Requisitos |
| F04 | Inserção Automática de Equipamentos | Execução MEP |
| F05 | Validação de Equipamentos Existentes | Verificação |
| F06 | Criação de Prumadas (Colunas Verticais) | Execução MEP |
| F07 | Geração de Rede de Água Fria | Execução MEP |
| F08 | Geração de Rede de Esgoto Sanitário | Execução MEP |
| F09 | Aplicação de Inclinações | Execução MEP |
| F10 | Criação de Sistemas MEP | Organização |
| F11 | Dimensionamento Hidráulico | Cálculo |
| F12 | Geração de Tabelas Quantitativas | Documentação |
| F13 | Geração de Pranchas | Documentação |
| F14 | Sistema de Logs e Diagnóstico | Infraestrutura |
| F15 | Interface de Controle e Execução | Interface |

---

## 3. Funcionalidades Detalhadas

---

### F01 — Detecção de Ambientes (Rooms + Spaces)

**Descrição:** Lê todos os Rooms do modelo arquitetônico, verifica a existência de Spaces MEP correspondentes e cria os Spaces faltantes para estabelecer a base de trabalho hidráulico.

**Objetivo:** Garantir que cada ambiente relevante do modelo tenha um Space MEP associado, permitindo que as etapas subsequentes trabalhem sobre elementos MEP padronizados.

**Entradas:**
- Modelo Revit com Rooms definidos pela arquitetura
- Levels do modelo
- Spaces MEP existentes (se houver)

**Processamento:**
1. Coleta todos os Rooms via `FilteredElementCollector` (categoria `OST_Rooms`)
2. Filtra Rooms inválidos (sem Location, área zero, redundantes)
3. Extrai metadados: nome, número, nível, área (m²), perímetro (m), ponto central (XYZ)
4. Converte todas as medidas de pés internos do Revit para metros
5. Coleta todos os Spaces existentes (categoria `OST_MEPSpaces`)
6. Executa correspondência Room ↔ Space por proximidade espacial (tolerância: 0.5 m) e nível
7. Identifica Rooms sem Space, Spaces órfãos e correspondências válidas
8. Solicita confirmação do usuário para criar Spaces faltantes
9. Cria Spaces via `Document.Create.NewSpace()` dentro de Transaction

**Saídas:**
- Lista de `AmbienteInfo` com todos os ambientes detectados
- Mapeamento Room → Space com IDs de correspondência
- Indicação de Spaces criados automaticamente
- Log completo com estatísticas

**Regras de Negócio:**
- Rooms sem Location são ignorados (log nível Leve)
- Rooms com área zero são ignorados (log nível Leve)
- Correspondência é feita por proximidade 2D do ponto central + mesmo nível
- Criação de Spaces requer confirmação explícita do usuário
- Spaces criados herdam nome e número do Room correspondente

**Dependências:**
- Modelo deve conter Rooms definidos (caso contrário: erro Crítico)
- Modelo deve ter ao menos 1 Level definido

**Exceções:**
- Nenhum Room encontrado → erro Crítico, bloqueia pipeline
- Falha na criação de Space → erro Crítico por elemento, rollback da Transaction
- Room sem Level associado → erro Médio, Space não é criado

---

### F02 — Classificação Avançada de Ambientes

**Descrição:** Analisa o nome de cada ambiente detectado e o classifica automaticamente em uma das 8 categorias hidráulicas, utilizando motor de NLP com normalização de texto em português brasileiro.

**Objetivo:** Determinar o tipo funcional de cada ambiente para que o sistema possa decidir quais equipamentos hidráulicos, pontos de conexão e redes são necessários.

**Entradas:**
- Lista de `AmbienteInfo` da F01
- Dicionário de padrões de classificação (40+ variações)

**Processamento:**
1. Para cada ambiente, normaliza o nome: lowercase, remoção de acentos (decomposição Unicode), remoção de numeração final, normalização de espaços
2. Executa 3 estratégias de matching em ordem de prioridade:
   - **Match exato:** texto normalizado == padrão → confiança = pesoBase × 1.0
   - **Texto contém padrão:** texto inclui padrão → confiança = pesoBase × 0.85 (prioriza padrão mais longo)
   - **Match parcial por palavras:** ratio de palavras em comum ≥ 50% → confiança = pesoBase × ratio × 0.7
3. Retorna tipo + confiança + padrão utilizado
4. Sinaliza ambientes por faixa de confiança

**Saídas:**
- `ResultadoClassificacao` para cada ambiente (tipo, confiança 0.0–1.0, padrão)
- Relatório estatístico por tipo
- Lista de ambientes que necessitam validação humana

**Classificações suportadas:**

| Tipo | Exemplos de Variações |
|------|----------------------|
| Banheiro | banheiro, wc, bwc, banho, banheiro social, sanitário |
| Lavabo | lavabo, lav, toilette, toilet |
| Suíte | suíte, suite, ste, banheiro suíte |
| Cozinha | cozinha, coz, copa, copa cozinha |
| Cozinha Gourmet | cozinha gourmet, espaço gourmet, gourmet |
| Lavanderia | lavanderia, lav. roupas, lavandaria |
| Área de Serviço | área de serviço, a.s., serviço |
| Área Externa | área externa, quintal, jardim, terraço, varanda, sacada, piscina |

**Regras de Negócio:**
- Confiança ≥ 0.70 → classificação automática aceita
- Confiança 0.50–0.69 → requer validação humana (log Médio)
- Confiança < 0.50 → NaoIdentificado (log Leve)
- Padrões mais específicos têm prioridade (ex: "cozinha gourmet" antes de "cozinha")
- O usuário pode reclassificar manualmente via UI

**Dependências:**
- F01 concluída com sucesso

**Exceções:**
- Nenhum ambiente classificado como relevante → erro Crítico
- Nenhum banheiro/suíte/lavabo detectado → erro Médio (alerta)
- Todos os ambientes com confiança baixa → erro Médio

---

### F03 — Identificação de Pontos Hidráulicos

**Descrição:** Para cada ambiente classificado, determina quais pontos hidráulicos (conexões de água fria e/ou esgoto) são necessários, com base no tipo do ambiente e na tabela normativa de aparelhos sanitários.

**Objetivo:** Gerar a lista completa de pontos de consumo e descarga que a rede hidráulica precisará atender, servindo como entrada para inserção de equipamentos e traçado de redes.

**Entradas:**
- Lista de ambientes classificados (F02)
- Tabela de pontos hidráulicos por tipo de ambiente (JSON)
- Equipamentos já existentes no modelo (se houver)

**Processamento:**
1. Para cada ambiente relevante, consulta tabela de pontos por tipo
2. Lista pontos necessários: tipo de equipamento, conexões (AF/ES/ambos), diâmetros, altura de instalação
3. Calcula vazão por ponto usando peso do aparelho (NBR 5626)
4. Verifica se equipamentos existentes no modelo já atendem algum ponto
5. Gera lista de pontos faltantes por ambiente
6. Totaliza pontos por tipo e por sistema

**Saídas:**
- Lista de `PontoHidraulico` por ambiente (tipo, conexão, diâmetro, vazão, altura)
- Mapa de pontos existentes vs. necessários
- Lista de pontos faltantes para ação
- Totalização por sistema (AF total, ES total)

**Tabela de pontos por tipo de ambiente:**

| Ambiente | Equipamentos Esperados |
|----------|----------------------|
| Banheiro | Vaso sanitário, lavatório, chuveiro, ralo |
| Lavabo | Vaso sanitário, lavatório |
| Suíte | Vaso sanitário, lavatório, chuveiro, ralo |
| Cozinha | Pia de cozinha, ralo |
| Cozinha Gourmet | Pia de cozinha, ralo |
| Lavanderia | Tanque, máquina de lavar, ralo |
| Área de Serviço | Tanque, ralo |
| Área Externa | Torneira de jardim, ralo |

**Regras de Negócio:**
- Cada tipo de aparelho tem diâmetro mínimo de conexão definido por norma
- Cada tipo de aparelho tem peso para cálculo de vazão (NBR 5626)
- Cada tipo de aparelho tem altura de instalação padrão
- Pontos existentes são validados contra critérios (posição, conexões)

**Dependências:**
- F02 concluída
- JSON de parâmetros por aparelho (`room_classification_map.json`)

**Exceções:**
- Ambiente classificado sem tabela de pontos → erro Médio
- Equipamento existente sem connectors válidos → erro Médio

---

### F04 — Inserção Automática de Equipamentos

**Descrição:** Insere automaticamente no modelo os equipamentos (aparelhos sanitários) necessários nos ambientes que não possuem todos os equipamentos esperados, utilizando famílias MEP padronizadas.

**Objetivo:** Completar o modelo com todos os aparelhos sanitários necessários, posicionados de forma adequada considerando layout do ambiente, paredes disponíveis e proximidade de prumadas.

**Entradas:**
- Lista de pontos faltantes por ambiente (F03)
- Famílias MEP carregadas no modelo
- Geometria dos ambientes (paredes, aberturas, dimensões)
- Posição de prumadas (se já definidas)

**Processamento:**
1. Verifica se as famílias MEP necessárias estão carregadas no modelo
2. Para famílias faltantes, tenta carregar da biblioteca padrão
3. Para cada ponto faltante:
   a. Identifica paredes disponíveis no ambiente (exclui paredes com portas/janelas)
   b. Calcula posição ideal: prioriza parede mais próxima da prumada
   c. Aplica offset padrão da parede por tipo de equipamento
   d. Insere `FamilyInstance` via API com posição e rotação calculadas
   e. Valida connectors do equipamento inserido
4. Gera relatório de inserção

**Saídas:**
- Equipamentos inseridos no modelo (FamilyInstances)
- Relatório: inseridos, falhas, validados
- Log detalhado por equipamento

**Regras de Negócio:**
- Vaso sanitário: posicionar a ≥ 15 cm da parede lateral, ≥ 60 cm de espaço frontal
- Lavatório: posicionar centralizado em parede, altura 80 cm
- Chuveiro: posicionar em canto quando possível, distante da porta
- Ralo: posicionar no ponto mais baixo possível do ambiente
- Priorizar proximidade da prumada para reduzir comprimento de ramais
- Inserção requer confirmação do usuário (preview)

**Dependências:**
- F03 concluída
- Famílias MEP disponíveis na biblioteca ou modelo

**Exceções:**
- Família MEP não encontrada → erro Crítico por equipamento
- Ambiente sem parede válida → erro Médio
- Colisão com elemento existente → erro Médio

---

### F05 — Validação de Equipamentos Existentes

**Descrição:** Verifica se os equipamentos já presentes no modelo atendem aos requisitos hidráulicos: posicionamento correto, connectors válidos, família adequada e distâncias normativas.

**Objetivo:** Garantir que equipamentos colocados manualmente ou em etapas anteriores estejam aptos para conexão com a rede hidráulica, evitando problemas nas etapas de traçado.

**Entradas:**
- Lista de equipamentos existentes por ambiente
- Critérios de validação por tipo de equipamento
- Geometria dos ambientes

**Processamento:**
1. Coleta todos os MEP fixtures nos ambientes classificados
2. Para cada equipamento, verifica:
   a. Tipo de família corresponde ao esperado
   b. Connectors de água fria e/ou esgoto existem e estão expostos
   c. Posição está dentro das tolerâncias normativas
   d. Distância da parede está adequada
   e. Não há colisão com outros elementos
3. Classifica cada equipamento: Válido, Com Ressalva, Inválido
4. Gera lista de ações corretivas necessárias

**Saídas:**
- Status de cada equipamento (Válido/Com Ressalva/Inválido)
- Lista de ações corretivas sugeridas
- Log de validação por equipamento

**Regras de Negócio:**
- Equipamento sem connector de AF em ambiente que exige AF → Inválido
- Equipamento sem connector de ES em ambiente que exige ES → Inválido
- Equipamento muito distante de qualquer parede → Com Ressalva
- Equipamento colidindo com outro → Inválido
- Equipamento de família genérica (sem connectors MEP) → Inválido

**Dependências:**
- F03 concluída (pontos identificados)

**Exceções:**
- Mais de 50% dos equipamentos inválidos → erro Crítico
- Equipamento em Room não classificado → ignorado (log Leve)

---

### F06 — Criação de Prumadas (Colunas Verticais)

**Descrição:** Cria as colunas verticais (prumadas) de água fria, esgoto e ventilação, posicionando-as nos eixos hidráulicos ótimos com base no agrupamento de ambientes.

**Objetivo:** Estabelecer a infraestrutura vertical que será o tronco de distribuição/coleta das redes horizontais em cada pavimento.

**Entradas:**
- Ambientes classificados com equipamentos (F04/F05)
- Levels do modelo
- Posição dos shafts (se existirem)

**Processamento:**
1. Agrupa ambientes por proximidade horizontal (identifica clusters/shafts)
2. Para cada cluster, calcula centroide como posição ideal da prumada
3. Define tipo de prumada necessária (AF, ES, VE) por cluster
4. Dimensiona diâmetro por tipo usando carga acumulada
5. Cria Pipe vertical conectando todos os Levels atendidos
6. Cria coluna de ventilação paralela ao tubo de queda
7. Valida que prumada não colide com estrutura

**Saídas:**
- Prumadas criadas no modelo (Pipes verticais)
- Mapeamento: prumada → ambientes atendidos
- Dimensionamento de cada prumada

**Regras de Negócio:**
- Prumada de AF: diâmetro baseado na soma de vazões prováveis dos pavimentos
- Tubo de queda (ES): diâmetro baseado em UHC acumulado (NBR 8160)
- Coluna de ventilação: diâmetro mínimo 2/3 do tubo de queda correspondente
- Distância máxima entre prumada e equipamento mais distante: definida por projeto
- Prumadas de AF e ES devem estar próximas para facilitar instalação

**Dependências:**
- F04 e F05 concluídas
- Levels corretamente definidos no modelo

**Exceções:**
- Nenhum shaft ou posição viável → erro Crítico
- Colisão com estrutura → erro Médio, solicita reposicionamento

---

### F07 — Geração de Rede de Água Fria

**Descrição:** Traça a rede completa de água fria desde o reservatório/barrilete até cada ponto de consumo, incluindo ramais de distribuição, sub-ramais, registros e conexões.

**Objetivo:** Criar a rede de distribuição de água fria completa, dimensionada e conectada, atendendo todos os pontos de consumo.

**Entradas:**
- Prumadas de AF criadas (F06)
- Pontos de consumo com vazão por ambiente (F03)
- Equipamentos posicionados (F04/F05)
- Configuração: altura do reservatório, pressão mínima

**Processamento:**
1. Define ponto de alimentação (reservatório superior)
2. Traça barrilete de distribuição no nível mais alto
3. Conecta barrilete às prumadas de AF
4. Para cada pavimento, traça ramais de distribuição desde a prumada
5. Traça sub-ramais até cada ponto de consumo (connector do equipamento)
6. Otimiza rota para menor comprimento total
7. Insere registros de gaveta (1 por ambiente, nos ramais)
8. Insere registros de pressão onde necessário
9. Dimensiona cada trecho: calcula vazão provável → seleciona diâmetro → verifica velocidade
10. Verifica pressão disponível em cada ponto (altura geométrica - perda de carga ≥ 3 m.c.a.)
11. Atribui todos os elementos ao PipingSystem de AF
12. Conecta todos os segmentos via fittings (tees, curvas, reduções)

**Saídas:**
- Rede de AF completa no modelo (Pipes + Fittings)
- PipingSystem "AF - Água Fria" com todos os elementos
- Relatório de dimensionamento (trecho, diâmetro, vazão, velocidade, pressão)

**Regras de Negócio:**
- Vazão provável: Q = 0.3 × √(ΣPesos) [L/s] (NBR 5626)
- Velocidade máxima: 3.0 m/s
- Diâmetros comerciais: 20, 25, 32, 40, 50, 60, 75, 85, 110 mm
- Pressão mínima em qualquer ponto: 3 m.c.a.
- Pressão máxima em qualquer ponto: 40 m.c.a.
- Perda de carga: Fair-Whipple-Hsiao para tubos de PVC
- Registros de gaveta obrigatórios nos ramais de cada ambiente

**Dependências:**
- F06 concluída (prumadas)
- F04/F05 concluídas (equipamentos posicionados com connectors)

**Exceções:**
- Pressão insuficiente em algum ponto → erro Crítico
- Velocidade excedida → erro Médio (sugere aumento de diâmetro)
- Impossibilidade de rotear sem colisão → erro Crítico

---

### F08 — Geração de Rede de Esgoto Sanitário

**Descrição:** Traça a rede de esgoto desde cada aparelho sanitário até o tubo de queda, incluindo ramais de descarga, ramais de esgoto, caixas sifonadas, caixas de inspeção e subcoletor.

**Objetivo:** Criar a rede de coleta de esgoto completa e dimensionada por UHC, convergindo para os tubos de queda e caixa de inspeção externa.

**Entradas:**
- Pontos de descarga por ambiente (F03)
- Equipamentos posicionados (F04/F05)
- Prumadas de esgoto/tubos de queda (F06)

**Processamento:**
1. Traça ramais de descarga: equipamento → caixa sifonada ou ramal de esgoto
2. Insere caixas sifonadas por ambiente (banheiros: obrigatória)
3. Traça ramais de esgoto: convergência da CX sifonada + vaso → tubo de queda
4. Para pavimento térreo: traça subcoletor até caixa de inspeção externa
5. Insere caixa de gordura na saída da cozinha
6. Insere caixas de inspeção nos pontos de mudança de direção
7. Dimensiona cada trecho por UHC acumulado (NBR 8160)
8. Atribui todos os elementos ao PipingSystem de esgoto

**Saídas:**
- Rede de ES completa no modelo
- PipingSystem "ES - Esgoto Sanitário"
- Relatório de dimensionamento por trecho com UHC

**Regras de Negócio:**
- Ramal de descarga: diâmetro mínimo conforme aparelho (vaso: 100mm, lavatório: 40mm)
- Ramal de esgoto: mínimo 50mm, dimensionado por ΣUHCs
- Tubo de queda: dimensionado por ΣUHCs de todos os pavimentos
- Subcoletor: mínimo 100mm
- Caixa sifonada: obrigatória em banheiros, recomendada em cozinhas
- Caixa de gordura: obrigatória na saída da cozinha (NBR 8160)
- Distância máxima de desconectores conforme norma

**Dependências:**
- F06 concluída
- F04/F05 concluídas

**Exceções:**
- Vaso sanitário sem ramal de 100mm → erro Crítico
- Ambiente sem queda de esgoto acessível → erro Crítico
- Subcoletor com UHC excedido → erro Médio

---

### F09 — Aplicação de Inclinações

**Descrição:** Aplica inclinação (declividade) em todos os trechos horizontais de rede de esgoto conforme norma, ajustando automaticamente a elevação dos tubos e suas conexões.

**Objetivo:** Garantir escoamento gravitacional adequado em toda a rede de esgoto.

**Entradas:**
- Todos os trechos horizontais de esgoto (F08)
- Diâmetro de cada trecho

**Processamento:**
1. Identifica todos os Pipes horizontais do PipingSystem de esgoto
2. Para cada trecho, determina inclinação baseada no diâmetro:
   - Diâmetro ≤ 75mm → 2% (2 cm/m)
   - Diâmetro ≥ 100mm → 1% (1 cm/m)
3. Calcula nova elevação do ponto final: Z_final = Z_inicial - (comprimento × inclinação)
4. Ajusta posição do endpoint do Pipe
5. Ajusta fittings conectados (curvas, tees, reduções)
6. Verifica interferências pós-ajuste (clash detection simplificado)
7. Valida geometricamente que a inclinação foi aplicada corretamente

**Saídas:**
- Tubos com inclinação aplicada no modelo
- Log de cada trecho ajustado (comprimento, desnível, inclinação)
- Lista de interferências detectadas (se houver)

**Regras de Negócio:**
- Inclinação mínima é obrigatória — trecho sem inclinação → erro Crítico
- 2% para DN ≤ 75mm (NBR 8160)
- 1% para DN ≥ 100mm (NBR 8160)
- Máxima inclinação recomendada: 5%
- Fittings devem ser reajustados para manter conexão

**Dependências:**
- F08 concluída (rede de esgoto traçada)

**Exceções:**
- Trecho sem espaço vertical para inclinação → erro Crítico
- Interferência com outra disciplina após ajuste → erro Médio

---

### F10 — Criação de Sistemas MEP

**Descrição:** Cria os sistemas lógicos (PipingSystem) no Revit para cada tipo de rede e atribui todos os elementos aos seus respectivos sistemas, com nomenclatura e cores padronizadas.

**Objetivo:** Organizar logicamente todos os elementos hidráulicos, permitindo filtragem, seleção, quantificação e validação de conectividade por sistema.

**Entradas:**
- Todos os elementos de rede criados (F07, F08)
- Prumadas (F06)
- Equipamentos conectados (F04/F05)

**Processamento:**
1. Cria PipingSystem "AF - Água Fria" (tipo: SupplyHydronic ou DomesticColdWater)
2. Cria PipingSystem "ES - Esgoto Sanitário" (tipo: Sanitary)
3. Cria PipingSystem "VE - Ventilação" (tipo: Vent)
4. Atribui cada Pipe e Fitting ao sistema correto via connectors
5. Verifica que todos os elementos possuem sistema atribuído
6. Valida conectividade: cada sistema deve ser topologicamente contínuo
7. Aplica Override de cor por sistema (AF: azul, ES: marrom, VE: verde)

**Saídas:**
- 3 PipingSystems criados e populados
- Visualização por cores aplicada
- Relatório de conectividade

**Regras de Negócio:**
- Elemento sem sistema → erro Médio
- Sistema desconectado (ilhas) → erro Médio
- Nome do sistema segue padrão: "{SIGLA} - {Descrição}"

**Dependências:**
- F07, F08, F09 concluídas

**Exceções:**
- Pipe Type inexistente → erro Crítico
- Connector incompatível entre elementos → erro Crítico

---

### F11 — Dimensionamento Hidráulico

**Descrição:** Motor de cálculo que dimensiona toda a rede hidráulica: vazões, diâmetros, velocidades, perdas de carga e pressões para água fria; UHCs e diâmetros para esgoto.

**Objetivo:** Garantir que toda a rede está dimensionada conforme NBR 5626 (AF) e NBR 8160 (ES).

**Entradas:**
- Topologia da rede (trechos, conexões, comprimentos)
- Pesos de aparelhos por ponto
- Alturas geométricas
- Parâmetros do sistema (altura reservatório, pressão mínima)

**Processamento — Água Fria:**
1. Soma pesos dos aparelhos por trecho (de jusante para montante)
2. Calcula vazão provável: Q = 0.3 × √(ΣP) [L/s]
3. Seleciona diâmetro comercial: menor diâmetro onde V ≤ 3 m/s
4. Calcula perda de carga unitária (Fair-Whipple-Hsiao)
5. Calcula perda de carga no trecho: J × L × 1.20 (20% para perdas localizadas)
6. Calcula pressão disponível em cada ponto: H_geom - ΣJ
7. Verifica pressão mínima ≥ 3 m.c.a. em todos os pontos

**Processamento — Esgoto:**
1. Soma UHCs por trecho (de montante para jusante)
2. Seleciona diâmetro por tabela UHC (NBR 8160)
3. Verifica diâmetros mínimos por tipo de ramal
4. Aplica regras de diminuição (diâmetro nunca diminui no sentido do escoamento)

**Saídas:**
- `ResultadoDimensionamento` por trecho: diâmetro, vazão, velocidade, perda de carga, pressão
- Dimensionamento completo da rede de AF
- Dimensionamento completo da rede de ES
- Alertas de pontos com pressão insuficiente ou velocidade excedida

**Regras de Negócio:**
- Pesos dos aparelhos conforme Tabela NBR 5626
- UHCs conforme Tabela NBR 8160
- Diâmetro nunca diminui no sentido do escoamento (esgoto)
- Velocidade máxima AF: 3.0 m/s
- Pressão mínima AF: 3 m.c.a.
- Pressão máxima AF: 40 m.c.a.

**Dependências:**
- F07 e F08 concluídas (topologia definida)

**Exceções:**
- Pressão negativa em algum ponto → erro Crítico
- Impossibilidade de atender com diâmetros comerciais → erro Crítico

---

### F12 — Geração de Tabelas Quantitativas

**Descrição:** Cria Schedules (tabelas) dentro do Revit com quantitativos de tubulações, conexões e equipamentos, formatadas para documentação do projeto.

**Objetivo:** Produzir automaticamente todas as tabelas de quantitativo necessárias para o memorial e a prancha do projeto.

**Entradas:**
- Todos os elementos MEP criados (Pipes, Fittings, FamilyInstances)
- Sistemas MEP (F10)

**Processamento:**
1. Cria ViewSchedule para tubulações: agrupa por sistema e diâmetro, soma comprimentos
2. Cria ViewSchedule para conexões: agrupa por tipo e diâmetro, conta unidades
3. Cria ViewSchedule para equipamentos: agrupa por ambiente, lista tipo e quantidade
4. Cria ViewSchedule resumo: 1 linha por ambiente com pontos hidráulicos
5. Configura formatação: cabeçalhos, unidades (m, mm, un), totais, agrupamento

**Saídas:**
- 4 Schedules criadas no modelo
- Exportação opcional para Excel/CSV

**Regras de Negócio:**
- Comprimentos em metros com 2 casas decimais
- Diâmetros em milímetros
- Agrupamento principal por sistema (AF, ES, VE)
- Totalizadores por diâmetro e geral

**Dependências:**
- F10 concluída (sistemas criados)

**Exceções:**
- Schedule com mesmo nome já existe → erro Leve, renomeia com sufixo

---

### F13 — Geração de Pranchas

**Descrição:** Cria automaticamente as pranchas (ViewSheets) do projeto hidráulico com views de planta, tabelas de quantitativo, legendas e notas, prontas para impressão.

**Objetivo:** Produzir pranchas finais padronizadas, reduzindo o trabalho manual de montagem e formatação.

**Entradas:**
- Views de planta do modelo (Floor Plans por pavimento)
- Schedules criadas (F12)
- Titleblock padrão do projeto
- View Templates para hidráulica

**Processamento:**
1. Cria View Templates para AF (filtros: mostra apenas sistema AF) e ES (filtros: mostra apenas sistema ES)
2. Duplica Floor Plan views por pavimento, aplicando View Template por sistema
3. Configura escala (1:50 padrão residencial) e crop region
4. Cria ViewSheets com titleblock padrão
5. Posiciona views nas pranchas (layout automático por tamanho)
6. Adiciona schedules de quantitativo na prancha
7. Adiciona legenda de cores/sistemas
8. Numera pranchas sequencialmente: HID-01, HID-02, etc.

**Saídas:**
- Pranchas completas no modelo (ViewSheets)
- Views configuradas com filtros e templates
- Numeração padronizada

**Regras de Negócio:**
- 1 prancha por sistema por pavimento (quando cabe)
- Escala padrão: 1:50 (ajustável)
- Nomenclatura: "HID-{NN} - {Sistema} - {Pavimento}"
- Titleblock deve conter campos de projeto preenchidos

**Dependências:**
- F12 concluída (schedules para incluir nas pranchas)
- View Templates definidos

**Exceções:**
- Titleblock não encontrado → erro Médio (usa titleblock genérico)
- View não cabe na folha → erro Leve (ajusta escala automaticamente)

---

### F14 — Sistema de Logs e Diagnóstico

**Descrição:** Sistema transversal que registra todas as ações, decisões, erros e métricas de execução de cada etapa do pipeline, com exportação estruturada e visualização em tempo real.

**Objetivo:** Garantir rastreabilidade completa, facilitar depuração e fornecer diagnósticos ao usuário para validação de cada etapa.

**Entradas:**
- Eventos de todos os serviços e módulos (F01–F13)

**Processamento:**
1. Cada serviço registra logs via `ILogService` com: timestamp, nível (Info/Leve/Médio/Crítico), etapa, componente, mensagem, ElementId opcional
2. LogManager acumula entradas em memória
3. Verifica presença de erros Críticos (bloqueio do pipeline)
4. Gera resumo por nível e por etapa
5. Exporta para JSON no diretório `Data/Logs/`
6. Alimenta a UI de diagnóstico em tempo real

**Saídas:**
- Log completo em memória (acessível pela UI)
- Arquivo JSON exportado por execução
- Resumo textual (contadores por nível)
- Flag de bloqueio (`TemBloqueio`)

**Níveis de log e comportamento:**

| Nível | Comportamento | Exemplo |
|-------|--------------|---------|
| Crítico | Bloqueia avanço para próxima etapa | "Nenhum Room encontrado" |
| Médio | Permite continuar com alerta | "Ambiente com confiança baixa" |
| Leve | Apenas informativo | "Room ignorado — área zero" |
| Info | Progresso normal | "15 Rooms lidos com sucesso" |

**Regras de Negócio:**
- Cada módulo DEVE gerar ao menos 1 log Info de início e 1 de conclusão
- Erros Críticos bloqueiam pipeline — nenhuma etapa posterior executa
- Erros Médios geram alerta mas permitem continuidade com aceite do usuário
- ElementId deve ser incluído sempre que o log se refere a um elemento do modelo
- Logs são preservados entre etapas (acumulativos)

**Dependências:**
- Nenhuma (sistema fundamental, disponível desde o início)

**Exceções:**
- Falha na escrita do arquivo JSON → log interno (não bloqueia)
- Diretório de logs inacessível → cria em %TEMP%

---

### F15 — Interface de Controle e Execução (WPF)

**Descrição:** Interface gráfica WPF integrada ao Revit com 3 abas (Configuração, Execução, Diagnóstico) que permite ao usuário controlar todo o pipeline, configurar parâmetros e validar resultados.

**Objetivo:** Fornecer controle total ao usuário sobre a execução semi-automática, com feedback visual em tempo real.

**Entradas:**
- Estado do pipeline (etapas, status)
- Logs acumulados
- Configuração hidráulica
- Dados dos ambientes

**Componentes:**

**Aba de Configuração:**
- Campos editáveis: pressão mínima, altura reservatório, velocidade máxima
- Campos de declividade: 2% e 1% com indicação de diâmetro
- Seleção de norma (NBR 5626, NBR 8160)
- Botões salvar/carregar configuração (JSON)
- Validação em tempo real dos parâmetros

**Aba de Execução:**
- Lista de 11 etapas com status visual (ícone + cor): ⬜ Não iniciada, 🔄 Em execução, ✅ Concluída, ❌ Com erro
- Botão "Executar" por etapa individual
- Botão "Executar Todas" com pausa entre etapas para validação
- Barra de progresso por etapa e geral
- Resumo da etapa concluída (métricas)
- Botões "Aprovar" / "Rejeitar" para validação humana
- Botão "Re-executar" para etapa específica

**Aba de Diagnóstico:**
- DataGrid de logs em tempo real com colunas: hora, nível, etapa, mensagem
- Filtros por nível (checkboxes) e por etapa (combobox)
- Cores de fundo por nível (vermelho Crítico, amarelo Médio, azul Leve, cinza Info)
- Contadores de erro: Crítico (N), Médio (N), Leve (N)
- Botão exportar logs (JSON)
- Clique em log com ElementId → seleciona elemento no Revit

**Saídas:**
- Controle de execução do pipeline
- Configuração persistida em JSON
- Feedback visual ao usuário

**Regras de Negócio:**
- Etapa só pode executar se pré-condições atendidas (etapas anteriores concluídas)
- Etapa com erro Crítico bloqueia botão "Executar" das etapas seguintes
- Validação humana é obrigatória entre etapas (quando há erros Médios)
- Configuração é carregada na abertura e salva automaticamente
- UI não bloqueia durante execução (operações via ExternalEvent)

**Dependências:**
- App.cs registrado na ribbon (F15 é acessada pela ribbon)
- ExternalEventHandlers para operações thread-safe

**Exceções:**
- Janela aberta sem modelo ativo → mensagem de erro e fecha
- Configuração JSON corrompida → carrega valores padrão
- ExternalEvent falha → log Crítico + mensagem ao usuário
