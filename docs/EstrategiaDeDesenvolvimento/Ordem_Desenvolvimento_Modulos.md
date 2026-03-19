# Ordem de Desenvolvimento dos Módulos — Plugin Hidráulico Revit

> Planejamento técnico detalhado com sequência de desenvolvimento, dependências, validação incremental e estratégia de integração.

---

## 1. Estratégia Geral

### 1.1 Abordagem adotada

**Incremental orientada a núcleo (Inside-Out)**

O desenvolvimento começa pela infraestrutura interna (logs, configuração, interface base) e avança progressivamente em direção aos módulos funcionais, seguindo o fluxo natural do projeto hidráulico:

```
Infraestrutura → Leitura do modelo → Análise → Inserção → Redes → Dimensionamento → Documentação
```

### 1.2 Justificativa técnica

| Razão | Detalhe |
|-------|---------|
| **Validação desde o dia 1** | Com logs e interface funcionando primeiro, cada módulo subsequente já nasce com feedback visual e rastreabilidade |
| **Dependência natural** | O fluxo hidráulico é sequencial: não se pode dimensionar sem ter rede, não se pode ter rede sem equipamentos, não se pode ter equipamentos sem ambientes |
| **Redução de retrabalho** | Módulos base (logs, config) raramente mudam; módulos de alto nível (pranchas, tabelas) dependem de tudo abaixo |
| **Teste contínuo** | Cada fase produz resultado visível e testável dentro do Revit |
| **Risco controlado** | Módulos de maior risco técnico (roteamento, dimensionamento) são desenvolvidos após base estável |

### 1.3 Princípios de desenvolvimento

| # | Princípio | Aplicação |
|---|-----------|-----------|
| 01 | **Sempre testável** | Cada módulo deve ser testável isoladamente antes de integrar |
| 02 | **Falha visível** | Erros devem aparecer no log e na UI imediatamente |
| 03 | **Validação humana** | Cada módulo pausa e aguarda aprovação antes do próximo avançar |
| 04 | **Dados antes de lógica** | Configuração e dados normativos (JSON) prontos antes da lógica que os consome |
| 05 | **API antes de Dynamo** | Funcionalidade base em C# (testável); delegação para Dynamo depois |
| 06 | **Sem dependência circular** | Módulo A nunca depende de módulo B se B depende de A |

---

## 2. Ordem de Desenvolvimento (Fases)

### Visão geral

| Fase | Nome | Módulos | Duração estimada | Objetivo |
|------|------|---------|-----------------|----------|
| **F0** | Fundação | — (config + JSON) | 1 semana | Preparar dados normativos e estrutura de projeto |
| **F1** | Infraestrutura | M14, M15 | 2 semanas | Logs, diagnóstico, interface base funcional |
| **F2** | Leitura do modelo | M01, M02 | 2 semanas | Ler e classificar ambientes do modelo |
| **F3** | Análise hidráulica | M03, M05 | 2 semanas | Identificar pontos necessários e validar existentes |
| **F4** | Inserção | M04 | 2 semanas | Inserir equipamentos faltantes |
| **F5** | Infraestrutura de rede | M06, M10 | 2 semanas | Criar prumadas e sistemas MEP |
| **F6** | Geração de redes | M07, M08 | 3 semanas | Gerar redes AF e ES |
| **F7** | Ajustes de rede | M09, M11 | 2 semanas | Aplicar inclinações e dimensionar |
| **F8** | Documentação | M12, M13 | 2 semanas | Gerar tabelas e pranchas |
| | | **Total** | **~18 semanas** | |

---

### Fase 0 — Fundação (pré-desenvolvimento)

**Objetivo:** Preparar toda a base de dados e configuração antes de escrever código funcional.

| Entregável | Descrição | Status |
|-----------|-----------|--------|
| `referencia_normativa.json` | JSON normativo unificado | ✅ Concluído |
| `config/*.json` | Arquivos de configuração padrão | ✅ Definido |
| Estrutura do projeto C# | Solution, namespaces, referências | A fazer |
| Template de famílias MEP | Famílias de teste no Revit | A fazer |
| Modelo de teste | Arquivo .rvt com Rooms e Levels | A fazer |

**Critério de avanço:** Solution compila, modelo de teste abre com Rooms.

---

### Fase 1 — Infraestrutura (M14 + M15)

**Objetivo:** Ter logs funcionando e interface básica visível no Revit desde o primeiro dia.

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M14 — Logs e Diagnóstico** | 🔴 Primeira | Todo módulo subsequente vai registrar logs. Sem isso, debug é impossível. |
| **M15 — Interface (WPF)** | 🔴 Segunda | Ribbon button, janela com abas, grid de logs. Tudo aparece aqui. |

**Entregáveis da fase:**
- LogService funcional (4 níveis, acumulação em memória, export JSON)
- Janela WPF com 3 abas (Configuração, Execução, Diagnóstico)
- Ribbon button no Revit que abre a janela
- Botões de etapas na aba Execução (desabilitados por enquanto)
- Aba de configuração carregando/salvando JSON
- ExternalEvent implementado (safe threading)

**Validação:**
- [ ] Clicar no ribbon abre a janela
- [ ] Log manual aparece no DataGrid de diagnóstico
- [ ] Configuração salva e recarrega valores
- [ ] Exportar log em JSON funciona

**Critério de avanço:** Janela abre, logs funcionam, config persiste.

---

### Fase 2 — Leitura do Modelo (M01 + M02)

**Objetivo:** Ler o modelo arquitetônico e identificar os ambientes hidráulicos.

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M01 — Detecção de Ambientes** | 🔴 Primeira | Base de TUDO. Sem ambientes, nada funciona. |
| **M02 — Classificação** | 🔴 Segunda | Sem classificação, não se sabe quais pontos cada ambiente precisa. |

**Entregáveis:**
- RoomReader: coleta Rooms, filtra inválidos, converte unidades
- SpaceManager: verifica Spaces, cria faltantes (com confirmação)
- Classificador com NLP para nomes em português
- Lista de AmbienteInfo com tipo, confiança, necessita validação
- Integração com LogService (registrar cada Room lido, cada classificação)
- Botões M01 e M02 habilitados na UI

**Validação:**
- [ ] Modelo com 15 Rooms → 15 lidos e listados no log
- [ ] Rooms sem Location → ignorados e registrados
- [ ] "Banheiro Social" → tipo Banheiro, confiança ≥ 0.85
- [ ] "BWC" → tipo Banheiro, confiança ≥ 0.70
- [ ] Classificação aparece na UI para validação humana

**Critério de avanço:** Ambientes lidos e classificados com ≥ 90% de acerto em corpus de teste.

---

### Fase 3 — Análise Hidráulica (M03 + M05)

**Objetivo:** Saber o que cada ambiente precisa e validar o que já existe no modelo.

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M03 — Identificação de Pontos** | 🔴 Primeira | Mapeia o que cada ambiente precisa (obrigatórios + opcionais) |
| **M05 — Validação de Existentes** | 🟡 Segunda | Verifica se equipamentos no modelo são válidos (connectors, família) |

**Nota:** M05 vem antes de M04 propositalmente. Primeiro validamos o que existe; depois inserimos o que falta.

**Entregáveis:**
- Consulta a `referencia_normativa.json` > `equipamentos.mapeamento_ambiente_aparelhos`
- Lista de PontoHidraulico por ambiente (tipo, sistema, DN, status)
- Comparação existentes vs. necessários
- Classificação de existentes: Válido / Com Ressalva / Inválido
- Totalização de pesos AF e UHCs ES

**Validação:**
- [ ] Banheiro → lista 4 pontos (vaso, lav, ch, ralo) com pesos/UHCs corretos
- [ ] Equipamento MEP com connector → Válido
- [ ] Equipamento de arquitetura sem connector → Inválido
- [ ] Total de pesos e UHCs confere com cálculo manual

**Critério de avanço:** Todos os ambientes com pontos mapeados e existentes validados.

---

### Fase 4 — Inserção (M04)

**Objetivo:** Inserir equipamentos faltantes nos ambientes.

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M04 — Inserção de Equipamentos** | 🟡 Média | Depende da lista de faltantes (M03). Requer famílias MEP. |

**Entregáveis:**
- Algoritmo de posicionamento (parede oposta à porta, offsets)
- Inserção via Revit API (FamilyInstance.Create)
- Validação pós-inserção (connector presente)
- Relatório de inserções (sucesso/falha por equipamento)
- **Primeira integração com Dynamo** (script de inserção em massa)

**Validação:**
- [ ] Banheiro retangular 2.5×3.0m → 4 equipamentos inseridos sem colisão
- [ ] Equipamento sem família disponível → erro Crítico logado
- [ ] Taxa de inserção em retangulares ≥ 90%
- [ ] Todos os inseridos possuem connectors

**Critério de avanço:** ≥ 80% dos equipamentos inseridos em modelo de teste.

---

### Fase 5 — Infraestrutura de Rede (M06 + M10)

**Objetivo:** Criar a estrutura vertical (prumadas) e os sistemas MEP que organizarão a rede.

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M06 — Criação de Prumadas** | 🔴 Alta | Prumadas definem a topologia vertical da rede. Sem elas, ramais não têm destino. |
| **M10 — Criação de Sistemas MEP** | 🟡 Média | PipingSystems (AF, ES, VE) precisam existir antes de gerar redes. |

**Entregáveis:**
- Clustering de ambientes por alinhamento vertical
- Criação de Pipes verticais (TQ AF, TQ ES, coluna VE)
- Dimensionamento preliminar por tabela
- Criação programática de PipingSystemType (AF, ES, VE)
- Atribuição de cores por sistema

**Validação:**
- [ ] 3 banheiros alinhados → 1 cluster, 1 grupo de prumadas
- [ ] 2 clusters separados → 2 grupos independentes
- [ ] PipingSystems criados com nomes e cores corretos
- [ ] Prumadas conectam do Level mais baixo ao mais alto

**Critério de avanço:** Prumadas visíveis no modelo, sistemas MEP criados.

---

### Fase 6 — Geração de Redes (M07 + M08)

**Objetivo:** Gerar as redes horizontais de água fria e esgoto sanitário.

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M07 — Rede de Água Fria** | 🔴 Alta | Conecta pontos de consumo às prumadas AF. |
| **M08 — Rede de Esgoto** | 🔴 Alta | Conecta aparelhos às prumadas ES + acessórios (CX sifonada, CX gordura). |

**Esta é a fase de maior risco técnico.** Roteamento de tubulações em 3D é o problema mais complexo do plugin.

**Entregáveis:**
- Topologia de rede AF (sequência de trechos, fittings, registros)
- Topologia de rede ES (ramais de descarga, ramal de esgoto, CX)
- Inserção de fittings (tees, curvas, registros, válvulas)
- **Integração com unMEP** para caminhos complexos
- Delegação para Dynamo para traçados retos/simples

**Validação:**
- [ ] 10 pontos de consumo → todos conectados à rede AF
- [ ] Banheiro → CX sifonada presente, vaso com ramal independente
- [ ] Cozinha → CX gordura presente
- [ ] DN nunca diminui no escoamento (ES)
- [ ] Registros na entrada de cada ambiente (AF)

**Critério de avanço:** Rede visível no modelo, todos os pontos conectados.

---

### Fase 7 — Ajustes de Rede (M09 + M11)

**Objetivo:** Aplicar inclinações na rede ES e dimensionar toda a rede AF+ES.

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M09 — Aplicação de Inclinações** | 🔴 Alta | Sem declividade, esgoto não funciona. |
| **M11 — Dimensionamento Hidráulico** | 🔴 Alta | Motor de cálculo (vazão, DN, V, J, pressão). |

**Entregáveis:**
- Aplicação de declividade por DN em todos os trechos horizontais ES
- Reconexão de fittings pós-ajuste
- Cálculo de vazão (Q = 0.3×√ΣP)
- Seleção de DN por velocidade
- Cálculo de perda de carga (FWH)
- Verificação de pressão em todos os pontos
- Atualização de DN no modelo
- Relatório de dimensionamento

**Validação:**
- [ ] Todos os trechos ES com declividade ≥ mínima
- [ ] Nenhum trecho contra gravidade
- [ ] Vazão ±1% do cálculo manual
- [ ] Diâmetros conforme tabela normativa
- [ ] Pressão ≥ 3.0 m.c.a. em todos os pontos (ou erro Crítico)
- [ ] Velocidade ≤ 3.0 m/s (ou erro Crítico)

**Critério de avanço:** Dimensionamento completo, modelo atualizado, relatório gerado.

---

### Fase 8 — Documentação (M12 + M13)

**Objetivo:** Gerar a documentação final do projeto (tabelas e pranchas).

| Módulo | Prioridade | Justificativa |
|--------|-----------|---------------|
| **M12 — Geração de Tabelas** | 🟢 Normal | Schedules de tubulações, conexões e equipamentos. |
| **M13 — Geração de Pranchas** | 🟢 Normal | ViewSheets com vistas filtradas + schedules. |

**Entregáveis:**
- 4 Schedules (tubulações, conexões, equipamentos, dimensionamento)
- ViewSheets com numeração HID-01, HID-02...
- Vistas filtradas por sistema (AF, ES, VE)
- Escala 1:50, Crop Region
- Delegação para Dynamo para layout assistido

**Validação:**
- [ ] 4 Schedules criadas com dados corretos
- [ ] Pranchas com view + schedule
- [ ] Filtros por sistema funcionando
- [ ] Numeração sequencial sem lacunas

**Critério de avanço:** Pranchas prontas para plotagem.

---

## 3. Dependências entre Módulos

### 3.1 Tabela de dependências

| Módulo | Depende de | Justificativa |
|--------|-----------|---------------|
| M01 — Detecção | M14 (logs) | Registrar resultados e erros |
| M02 — Classificação | M01 | Precisa da lista de ambientes detectados |
| M03 — Pontos | M02 | Precisa saber o tipo de cada ambiente |
| M04 — Inserção | M03 | Precisa da lista de pontos faltantes |
| M05 — Validação | M03 | Precisa da lista de pontos esperados para comparar |
| M06 — Prumadas | M04, M05 | Precisa saber posição dos equipamentos |
| M07 — Rede AF | M06 | Precisa das prumadas como destino dos ramais |
| M08 — Rede ES | M06 | Precisa das prumadas como destino dos ramais |
| M09 — Inclinações | M08 | Aplica declividade nos trechos de esgoto já criados |
| M10 — Sistemas MEP | M06 | Pode ser criado junto com prumadas, mas aplica-se a toda rede |
| M11 — Dimensionamento | M07, M08, M09 | Precisa da topologia completa com inclinações |
| M12 — Tabelas | M10, M11 | Precisa dos sistemas e diâmetros finais |
| M13 — Pranchas | M12 | Precisa das schedules para colocar nas pranchas |
| M14 — Logs | Nenhum | Módulo fundamental, sem dependência |
| M15 — Interface | M14 | Exibe os logs do módulo 14 |

### 3.2 Grafo de dependências

```
                    M14 (Logs)
                   /    \
                M15      M01
               (UI)       |
                        M02
                         |
                        M03
                       /    \
                     M04    M05
                       \    /
                        M06 ← M10
                       /    \
                     M07    M08
                       \      |
                        \   M09
                         \ /
                         M11
                          |
                         M12
                          |
                         M13
```

### 3.3 Dependências ocultas

| Dependência | Detalhe | Impacto se ignorada |
|------------|---------|---------------------|
| M04 depende de **famílias MEP** carregadas no modelo | Se não existem FamilySymbols, inserção falha | Erro Crítico em 100% dos testes |
| M07/M08 dependem de **Levels corretos** | Pipes precisam de Level para serem criados | API falha silenciosamente |
| M06 depende de **alinhamento vertical real** | Se banheiros não estão alinhados, clustering falha | Prumadas em posição incorreta |
| M09 depende de **espessura da laje** | Se não há espaço, inclinação é impossível | Conflitos geométricos |
| M11 depende de **cota do reservatório** | Sem Z_reserv, pressão não pode ser calculada | Dimensionamento incorreto |
| M13 depende de **Titleblock** carregado | Sem família de folha, pranchas não são criadas | Erro Crítico |

---

## 4. Ordem Recomendada (Sequência Linear)

| # | Módulo | Fase | Justificativa da posição |
|---|--------|------|------------------------|
| 01 | **M14 — Logs e Diagnóstico** | F1 | Todos os módulos dependem dela. Sem logs = sem debug. |
| 02 | **M15 — Interface (WPF)** | F1 | Feedback visual desde o início. Controla todo fluxo. |
| 03 | **M01 — Detecção de Ambientes** | F2 | Primeiro contato com o modelo. Base de tudo. |
| 04 | **M02 — Classificação** | F2 | Transforma Rooms genéricos em ambientes hidráulicos. |
| 05 | **M03 — Identificação de Pontos** | F3 | Mapeia necessidades hidráulicas por ambiente. |
| 06 | **M05 — Validação de Existentes** | F3 | Verifica equipamentos antes de inserir novos. |
| 07 | **M04 — Inserção de Equipamentos** | F4 | Insere o que falta. Primeiro módulo que modifica o modelo. |
| 08 | **M06 — Criação de Prumadas** | F5 | Define topologia vertical. Primeiro elemento de rede. |
| 09 | **M10 — Sistemas MEP** | F5 | Organiza a rede em sistemas antes de gerar ramais. |
| 10 | **M07 — Rede de Água Fria** | F6 | Gera ramais AF. Maior complexidade de roteamento. |
| 11 | **M08 — Rede de Esgoto** | F6 | Gera ramais ES + acessórios (CX, CI). |
| 12 | **M09 — Inclinações** | F7 | Aplica declividade após rede ES existir. |
| 13 | **M11 — Dimensionamento** | F7 | Motor de cálculo. Precisa de toda topologia. |
| 14 | **M12 — Tabelas** | F8 | Extrai dados do modelo dimensionado. |
| 15 | **M13 — Pranchas** | F8 | Último módulo. Depende de tudo. |

---

## 5. Estratégia de Validação

### 5.1 Validação por fase

| Fase | O que testar | Como validar | Critério de avanço |
|------|-------------|-------------|-------------------|
| **F0** | JSON normativos, estrutura do projeto | Deserializar JSON em C#, compilar solution | JSON parseia sem erro; solution compila |
| **F1** | Logs, UI, config | Abrir janela, registrar log, salvar config | Janela abre, log aparece, config persiste |
| **F2** | Leitura de Rooms, classificação | Modelo de teste com 15+ Rooms nomeados | ≥ 90% acerto, 0 Rooms válidos perdidos |
| **F3** | Pontos por ambiente, validação de existentes | Modelo com equipamentos MEP + arquitetura | Pontos corretos, existentes classificados |
| **F4** | Inserção de equipamentos | Modelo de teste com ambientes vazios | ≥ 80% inseridos, todos com connectors |
| **F5** | Prumadas, sistemas MEP | Visual no modelo, systems browser | Prumadas visíveis, 3 sistemas no browser |
| **F6** | Redes AF e ES | Conectividade, todos os pontos ligados | 0 pontos desconectados |
| **F7** | Inclinações, dimensionamento | Comparar com planilha de cálculo manual | Vazão ±1%, DN conforme tabela, pressão OK |
| **F8** | Tabelas, pranchas | Visual + conferência de dados | Dados = modelo, pranchas plotáveis |

### 5.2 Modelos de teste necessários

| Modelo | Descrição | Uso |
|--------|-----------|-----|
| `Teste_01_Basico.rvt` | Casa térrea, 1 banheiro, 1 cozinha, 1 lavanderia | F2–F7 (caso simples) |
| `Teste_02_Sobrado.rvt` | 2 pavimentos, 2 banheiros + lavabo + cozinha + lavanderia | F5–F8 (prumadas multi-pav) |
| `Teste_03_Incompleto.rvt` | Rooms sem nomes padrão, equipamentos de arquitetura | F2–F5 (robustez) |
| `Teste_04_Complexo.rvt` | 3 pavimentos, múltiplos wet cores, banheiros irregulares | F6–F8 (stress test) |

---

## 6. Módulos Críticos

### 6.1 Módulos que bloqueiam o restante

| Módulo | Bloqueados | Impacto |
|--------|-----------|---------|
| **M14 — Logs** | Todos (M01–M13, M15) | Sem logs, nenhum módulo pode registrar resultados |
| **M01 — Detecção** | M02–M13 | Sem ambientes, pipeline inteiro para |
| **M02 — Classificação** | M03–M13 | Sem tipos, não se sabe o que cada ambiente precisa |
| **M06 — Prumadas** | M07–M13 | Sem prumadas, ramais não têm destino |

### 6.2 Módulos de maior risco técnico

| Módulo | Risco | Motivo | Mitigação |
|--------|-------|--------|-----------|
| **M07 — Rede AF** | 🔴 Alto | Roteamento 3D é o problema mais complexo | Usar unMEP para caminhos complexos; simplificar para caminhos retos |
| **M08 — Rede ES** | 🔴 Alto | Idem M07 + regras de acessórios (CX, CI) | Separar lógica de acessórios da lógica de roteamento |
| **M04 — Inserção** | 🟡 Médio | Posicionamento automático em ambientes irregulares | Aceitar inserção manual para irregulares (alerta Médio) |
| **M09 — Inclinações** | 🟡 Médio | Reconexão de fittings, espaço na laje | Backup de posições + rollback se > 30% falha |
| **M02 — Classificação** | 🟡 Médio | NLP para nomes em português com variações | Múltiplas estratégias (exato, parcial, fuzzy) + validação humana |

### 6.3 Módulos que devem ser priorizados

| Prioridade | Módulo | Justificativa |
|-----------|--------|---------------|
| 🥇 1º | M14 (Logs) | Fundação de todo debug e rastreabilidade |
| 🥇 1º | M15 (Interface) | Sem UI, usuário não interage |
| 🥈 2º | M01 (Detecção) | Sem leitura do modelo, nada funciona |
| 🥈 2º | M02 (Classificação) | Sem tipo, não se mapeia necessidades |
| 🥉 3º | M06 (Prumadas) | Define topologia vertical — base das redes |

---

## 7. Estratégia de Integração

### 7.1 Integração com Dynamo

| Quando | Módulo | Script Dynamo | Função |
|--------|--------|--------------|--------|
| **F4** (primeira vez) | M04 | `04_InserirEquipamentos.dyn` | Inserção em massa de fixtures por lista |
| **F6** | M07 | `07_GerarRedeAF.dyn` | Traçado de ramais retos AF |
| **F6** | M08 | `08_GerarRedeES.dyn` | Traçado de ramais ES + inserção de CX |
| **F7** | M09 | `09_AplicarInclinacoes.dyn` | Ajuste de Z em batch |
| **F8** | M13 | `13_GerarPranchas.dyn` | Layout assistido de views em sheets |

**Sequência de integração Dynamo:**
```
1. F4: Primeiro script simples (inserção) — validar comunicação Plugin↔Dynamo
2. F6: Scripts de roteamento — complexidade principal
3. F7: Script de batch update de cotas — simples mas crítico
4. F8: Script de layout — último, menos crítico
```

### 7.2 Integração com unMEP

| Quando | Módulo | Função |
|--------|--------|--------|
| **F6** (somente quando necessário) | M07 | Roteamento alternativo para caminhos que Dynamo não resolve |
| **F6** | M08 | Roteamento de ramais ES com desvio de obstáculos |

**Estratégia:**
```
1. TENTAR roteamento via Dynamo (caminhos retos e simples)
2. SE falha ou caminho tem obstáculo:
   DELEGAR para unMEP
3. SE unMEP também falha:
   MARCAR trecho para roteamento manual
   REGISTRAR log Médio
```

**Ordem de integração:**
```
Dynamo (F4) → Dynamo avançado (F6) → unMEP (F6, quando necessário)
```

### 7.3 Responsabilidades por ferramenta

| Responsabilidade | Plugin C# | Dynamo | unMEP |
|-----------------|-----------|--------|-------|
| Decisão e lógica | ✅ | — | — |
| Leitura do modelo | ✅ | — | — |
| Classificação | ✅ | — | — |
| Inserção em massa | — | ✅ | — |
| Roteamento reto | — | ✅ | — |
| Roteamento complexo | — | — | ✅ |
| Dimensionamento | ✅ | — | — |
| Ajuste de cotas em batch | — | ✅ | — |
| Criação de sistemas | ✅ | — | — |
| Geração de tabelas | ✅ | — | — |
| Layout de pranchas | — | ✅ | — |
| Validação | ✅ | — | — |
| Logs | ✅ | — | — |

---

## 8. Riscos de Sequência

### 8.1 Problemas se a ordem for alterada

| Alteração | Problema | Severidade |
|-----------|---------|-----------|
| Desenvolver M07/M08 antes de M06 | Ramais não têm destino (prumada). Precisaria criar pipes "fantasma". | 🔴 Alto — retrabalho garantido |
| Desenvolver M04 antes de M03 | Não se sabe o que inserir. Código de inserção sem lista de necessidades. | 🔴 Alto — lógica sem propósito |
| Desenvolver M11 antes de M07/M08/M09 | Não existe topologia para dimensionar. Motor de cálculo sem dados. | 🔴 Alto — impossível testar |
| Desenvolver M09 antes de M08 | Não há trechos ES para aplicar inclinação. | 🔴 Alto — impossível executar |
| Desenvolver M13 antes de M12 | Pranchas sem schedules para exibir. | 🟡 Médio — pranchas vazias |
| Desenvolver M01 sem M14 | Detecção funciona, mas sem registro de erros e resultados. | 🟡 Médio — debug difícil |
| Inverter M03/M05 e M04 | Funciona, mas validação de existentes é mais útil ANTES da inserção. | 🟢 Baixo — lógica menos eficiente |

### 8.2 Gargalos técnicos

| Gargalo | Módulo(s) | Causa | Mitigação |
|---------|----------|-------|-----------|
| **Roteamento 3D** | M07, M08 | Problema computacionalmente difícil; Revit API não tem pathfinding nativo | Delegar para unMEP; simplificar para caminhos retos |
| **Famílias MEP** | M04 | Plugin depende de famílias específicas carregadas | Criar kit de famílias padrão; verificar presença antes de executar |
| **Reconexão de fittings** | M09 | Mover endpoint pode desconectar fitting | Algoritmo de reconexão + rollback se falha > 30% |
| **Comunicação Plugin↔Dynamo** | M04+ | Passar dados entre C# e Dynamo | Definir formato de troca (JSON via arquivo temporário) |
| **Performance em modelos grandes** | M07, M08 | Muitos Pipes + Fittings = Revit lento | Usar Transactions em batch, SubTransactions |

### 8.3 Ponto de não retorno

```
Após F6 (redes geradas), o modelo tem elementos significativos criados.
Reverter seria custoso.

RECOMENDAÇÃO: 
  - Validar F1–F5 exaustivamente antes de avançar para F6
  - F6 deve começar em modelo de teste limpo
  - Implementar "undo" completo (deletar elementos criados)
```
