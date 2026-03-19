# Escopo Funcional Detalhado por Módulo — Plugin Hidráulico Revit 2026

> Especificação técnica de implementação dos 15 módulos do sistema, com escopo, processamento, integrações, regras e critérios de conclusão.

---

## Módulo 01 — Detecção de Ambientes (Rooms + Spaces)

### Objetivo
Ler o modelo arquitetônico, extrair todos os ambientes válidos e garantir que cada Room relevante possua um Space MEP associado.

### Escopo
**Incluído:** Leitura de Rooms. Filtragem de inválidos. Conversão de unidades. Leitura de Spaces existentes. Correspondência Room↔Space. Criação de Spaces faltantes. Detecção de Spaces órfãos.

**Não incluído:** Correção de Rooms mal definidos (paredes abertas). Renomeação automática de Rooms. Criação de Rooms. Modificação do modelo arquitetônico.

### Entradas
- Modelo Revit aberto com Rooms definidos
- Levels do modelo
- Spaces MEP existentes (se houver)
- **Dependências:** Nenhuma (primeiro módulo do pipeline)

### Processamento
1. `FilteredElementCollector(OST_Rooms)` → coletar todos os Rooms
2. Para cada Room: verificar `Location != null` e `Area > 0`
3. Rooms inválidos → log Leve com ElementId e motivo, excluir da lista
4. Rooms válidos → extrair: ElementId, Name, Number, Level.Name, Area (ft²→m²), Perimeter (ft→m), LocationPoint (ft→m)
5. `FilteredElementCollector(OST_MEPSpaces)` → coletar Spaces existentes
6. Para cada Room válido: buscar Space no mesmo Level cuja distância 2D do ponto central ≤ 0.5m
7. Classificar: Rooms com Space, Rooms sem Space, Spaces órfãos (sem Room)
8. Se Rooms sem Space > 0: exibir TaskDialog com contagem, solicitar confirmação
9. Se confirmado: para cada Room sem Space, abrir Transaction, criar Space via `doc.Create.NewSpace(level, UV)`, copiar Name/Number, commitar
10. Se erro em qualquer criação: rollback da Transaction, log Crítico

### Saídas
- `List<AmbienteInfo>` com todos os ambientes válidos (Room + Space vinculados)
- Flag `SpaceCriadoAutomaticamente` por ambiente
- Contagem: total lidos, filtrados, com Space, sem Space, Spaces criados, órfãos

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Leitura via API, filtragem, correspondência, criação de Spaces |
| **Dynamo** | Criação em massa quando quantidade > 20 (`02_CriarSpacesMassivo.dyn`) |
| **unMEP** | Não atua |

### Regras de Negócio
- Room sem Location → ignorado (log Leve)
- Room com Area ≤ 0 → ignorado (log Leve)
- Correspondência: distância 2D ≤ 0.5m E mesmo Level
- Se múltiplos Spaces candidatos: selecionar o mais próximo
- Criação de Space requer confirmação explícita do usuário
- Spaces criados herdam Name e Number do Room

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Nenhum Room encontrado no modelo | Crítico (bloqueia pipeline) |
| Room sem Level associado | Médio |
| Falha na criação de Space | Crítico (por elemento) |
| Mais de 30% dos Rooms ignorados | Médio |
| Spaces órfãos detectados | Leve |

### Limitações
- Rooms com paredes abertas podem ter área zero → são ignorados
- Correspondência por proximidade pode falhar em plantas muito compactas (ambientes < 2m²)
- Plugin não corrige o modelo arquitetônico
- NewSpace pode falhar se o Level não tem Phase válida

### Critérios de Conclusão
- ✅ Todos os Rooms válidos possuem `AmbienteInfo` correspondente
- ✅ Cada Room relevante possui Space MEP associado (criado ou pré-existente)
- ✅ Log gerado com estatísticas completas
- ✅ Nenhum erro Crítico pendente

---

## Módulo 02 — Classificação de Ambientes

### Objetivo
Classificar cada ambiente detectado em uma das 8 categorias hidráulicas usando NLP em português brasileiro, calculando confiança e sinalizando casos que requerem validação humana.

### Escopo
**Incluído:** Normalização de texto. Matching em 3 estratégias. Cálculo de confiança. Sinalização por faixa. Suporte a reclassificação manual.

**Não incluído:** Classificação por conteúdo do ambiente (análise de objetos dentro do Room). Classificação por área/formato. Aprendizado de máquina.

### Entradas
- `List<AmbienteInfo>` do Módulo 01
- Dicionário de padrões de classificação (40+ variações em JSON)
- **Dependências:** Módulo 01 concluído

### Processamento
1. Para cada `AmbienteInfo`:
   a. Normalizar nome: `ToLowerInvariant()` → decomposição Unicode FormD → remover NonSpacingMark → recompor FormC → regex `\s*\d+\s*$` (remover numeração) → regex `\s+` → " " → trim
   b. **Estratégia 1 (match exato):** texto normalizado == padrão → confiança = pesoBase × 1.0
   c. **Estratégia 2 (contém):** `texto.Contains(padrão)`, prioridade para padrão mais longo → confiança = pesoBase × 0.85
   d. **Estratégia 3 (parcial):** ratio de palavras em comum ≥ 50% → confiança = pesoBase × ratio × 0.7
   e. Retornar melhor resultado ou (NaoIdentificado, 0.0)
2. Classificar resultado:
   - Confiança ≥ 0.70 → aceito automaticamente
   - 0.50 ≤ confiança < 0.70 → necessita validação humana (log Médio)
   - Confiança < 0.50 → NaoIdentificado (log Leve)
3. Gerar resumo: contagem por tipo, lista de ambientes que necessitam validação

### Saídas
- `ResultadoClassificacao` por ambiente: TipoAmbiente (enum), Confianca (double), PadraoUtilizado (string), EhConfiavel (bool), NecessitaValidacao (bool)
- Resumo estatístico por tipo
- Lista de ambientes pendentes de validação humana

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Motor de NLP completo (normalização, matching, confiança) |
| **Dynamo** | Não atua |
| **unMEP** | Não atua |

### Regras de Negócio
- 8 tipos: Banheiro, Lavabo, Suite, Cozinha, CozinhaGourmet, Lavanderia, AreaServico, AreaExterna
- Padrões mais específicos têm prioridade ("cozinha gourmet" antes de "cozinha")
- Reclassificação manual define confiança = 1.0 e padrão = "manual"
- Ambientes não classificados (sala, quarto, hall) são marcados como NaoIdentificado — não recebem tratamento hidráulico

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Nenhum ambiente classificado como relevante | Crítico |
| Nenhum banheiro/suíte/lavabo encontrado | Médio |
| Mais de 50% dos ambientes com confiança < 0.70 | Médio |
| Nome vazio | Leve (classificação como NaoIdentificado) |

### Limitações
- Depende exclusivamente do nome do Room — não analisa conteúdo
- Nomes em outros idiomas não são reconhecidos
- Abreviações não mapeadas resultam em confiança baixa
- Ambientes com múltiplos usos (ex: "Cozinha/Lavanderia") podem classificar incorretamente

### Critérios de Conclusão
- ✅ Todos os ambientes possuem classificação (inclusive NaoIdentificado)
- ✅ Ambientes que necessitam validação humana foram sinalizados
- ✅ Ao menos 1 ambiente hidráulico identificado
- ✅ Resumo estatístico gerado

---

## Módulo 03 — Identificação de Pontos Hidráulicos

### Objetivo
Determinar, para cada ambiente classificado, quais pontos hidráulicos (equipamentos + conexões) são necessários, com tipo de conexão, diâmetro, peso/UHC e altura de instalação.

### Escopo
**Incluído:** Consulta de tabela tipo→equipamentos. Listagem de pontos por ambiente. Cálculo de vazão por ponto. Detecção de equipamentos existentes. Comparação existentes vs. necessários.

**Não incluído:** Cálculo de posição dos equipamentos (Módulo 04). Dimensionamento da rede (Módulo 11). Inserção física (Módulo 04).

### Entradas
- `List<AmbienteInfo>` classificados (Módulo 02)
- JSON de mapeamento tipo→equipamentos com parâmetros normativos
- Equipamentos MEP existentes no modelo (FilteredElementCollector)
- **Dependências:** Módulo 02 concluído

### Processamento
1. Para cada ambiente com `EhRelevante == true`:
   a. Consultar JSON: tipo → lista de equipamentos esperados
   b. Para cada equipamento: extrair tipo, conexão (AF/ES/ambos), DN mínimo (mm), peso AF (NBR 5626), UHC ES (NBR 8160), altura de instalação (m)
2. Coletar MEP fixtures existentes por ambiente (BoundingBox intersection com Room)
3. Para cada fixture existente: identificar tipo por Family Category
4. Comparar: existentes vs. necessários → gerar lista de faltantes
5. Totalizar: pontos AF totais, pontos ES totais, pesos totais, UHCs totais

### Saídas
- `List<PontoHidraulico>` por ambiente: tipo, conexão, DN, peso, UHC, altura, status (necessário/existente/faltante)
- Lista consolidada de pontos faltantes
- Totalizações por sistema

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Lógica de decisão, consulta JSON, detecção de existentes |
| **Dynamo** | Não atua |
| **unMEP** | Não atua |

### Regras de Negócio
- Tabela de equipamentos definida no JSON (`room_classification_map.json`)
- Cada aparelho tem peso AF (NBR 5626) e UHC (NBR 8160) pré-definidos
- Diâmetro mínimo do sub-ramal conforme tabela normativa
- Equipamentos existentes são identificados por categoria do Revit, não por nome

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Ambiente sem tabela de pontos | Médio |
| Fixture existente sem connectors | Médio |
| Total de pontos = 0 para todo o projeto | Crítico |

### Limitações
- Tabela é fixa por tipo de ambiente — não diferencia variações (banheiro grande vs. pequeno)
- Detecção de fixture existente por BoundingBox pode incluir fixtures de ambientes adjacentes
- Fixtures de arquitetura (sem connectors MEP) são detectados mas sinalizados como inválidos

### Critérios de Conclusão
- ✅ Todos os ambientes relevantes possuem lista de pontos
- ✅ Pontos faltantes identificados
- ✅ Totalizações calculadas

---

## Módulo 04 — Inserção de Equipamentos

### Objetivo
Inserir automaticamente no modelo os equipamentos sanitários faltantes, calculando posição com base em paredes, portas e proximidade de prumada.

### Escopo
**Incluído:** Verificação de famílias. Cálculo de posição em ambientes regulares. Inserção via API. Rotação conforme parede. Validação pós-inserção.

**Não incluído:** Posicionamento em ambientes irregulares (requer intervenção humana). Criação de famílias MEP. Carregamento de famílias de rede.

### Entradas
- Pontos faltantes por ambiente (Módulo 03)
- Famílias MEP disponíveis no modelo
- Geometria dos ambientes (paredes, portas, dimensões)
- Posição de prumadas (se Módulo 06 já executado; caso contrário, usa centroide)
- **Dependências:** Módulo 03. Módulo 06 recomendado mas não obrigatório.

### Processamento
1. Para cada tipo de equipamento necessário: buscar FamilySymbol no modelo
2. Se FamilySymbol não encontrado: tentar carregar da biblioteca padrão. Se falha: log Crítico, pular equipamento.
3. Para cada ponto faltante:
   a. Identificar paredes do ambiente via `Room.GetBoundarySegments()`
   b. Excluir paredes que contêm portas (verificar aberturas)
   c. Selecionar parede conforme regras por tipo de equipamento
   d. Calcular posição: ponto na parede + offset conforme tipo
   e. Calcular rotação: alinhar com normal da parede
   f. Abrir Transaction → `doc.Create.NewFamilyInstance(point, symbol, level, StructuralType.NonStructural)` → rotacionar → commitar
   g. Validar connectors do equipamento inserido
4. Gerar relatório: inseridos, falhas, validados

### Saídas
- FamilyInstances inseridas no modelo
- Relatório por equipamento: ElementId, posição, família, status
- Log detalhado

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Decisão de posição, inserção individual, validação |
| **Dynamo** | Inserção em massa quando > 10 equipamentos (`03_InserirEquipamentos.dyn`) |
| **unMEP** | Não atua |

### Regras de Negócio
- Vaso: parede oposta à porta, ≥ 15cm da lateral, ≥ 60cm frontal livre
- Lavatório: parede adjacente à porta, centralizado
- Chuveiro: canto oposto à porta
- Ralo: centro do ambiente ou canto mais distante
- Pia de cozinha: parede onde há window (se existir) ou parede mais longa
- Inserção requer confirmação do usuário (preview via UI)

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| FamilySymbol não encontrada | Crítico (por equipamento) |
| Ambiente sem parede válida | Médio |
| Connector ausente pós-inserção | Médio |
| Colisão com outro elemento | Médio |

### Limitações
- Algoritmo assume ambientes retangulares — em L/T, posição pode ser incorreta
- Famílias de terceiros sem connectors impedem conexão futura
- Requer que portas estejam modeladas para excluir paredes corretas
- Offset é fixo por tipo — não se adapta a dimensões da família

### Critérios de Conclusão
- ✅ Todos os pontos faltantes com família disponível foram inseridos
- ✅ Todos os equipamentos inseridos possuem connectors válidos
- ✅ Usuário validou posições

---

## Módulo 05 — Validação de Equipamentos Existentes

### Objetivo
Verificar se equipamentos já presentes no modelo atendem requisitos hidráulicos: tipo de família, connectors, posição e ausência de colisão.

### Escopo
**Incluído:** Verificação de família, connectors, posição relativa a parede, clash simplificado. Classificação: Válido/Com Ressalva/Inválido.

**Não incluído:** Substituição automática de famílias. Correção automática de posição. Clash detection preciso.

### Entradas
- Equipamentos existentes por ambiente (detectados no Módulo 03)
- Critérios de validação por tipo
- Geometria dos ambientes
- **Dependências:** Módulo 03 concluído

### Processamento
1. Para cada fixture existente:
   a. Verificar Family Category corresponde ao esperado
   b. Verificar presença de Connector de AF (se ambiente requer AF)
   c. Verificar presença de Connector de ES (se ambiente requer ES)
   d. Calcular distância à parede mais próxima → verificar contra tolerância
   e. Verificar BoundingBox intersection com outros elementos
2. Classificar: Válido (tudo ok), Com Ressalva (posição questionável), Inválido (sem connector ou família errada)
3. Gerar lista de ações corretivas

### Saídas
- Status por equipamento (Válido/Ressalva/Inválido)
- Lista de ações corretivas sugeridas
- Log de validação

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Todas as verificações |
| **Dynamo** | Verificação de distâncias em lote (`04_ValidarPosicionamento.dyn`) |
| **unMEP** | Não atua |

### Regras de Negócio
- Sem connector AF em ambiente que exige AF → Inválido
- Sem connector ES em ambiente que exige ES → Inválido
- Família genérica (arquitetura, sem MEP connectors) → Inválido
- Distância da parede > 2× offset esperado → Com Ressalva

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| > 50% dos equipamentos Inválidos | Crítico |
| Equipamento sem connector necessário | Médio (por equipamento) |

### Limitações
- Clash detection por BoundingBox gera falsos positivos
- Famílias de terceiros com connectors em posições não padronizadas

### Critérios de Conclusão
- ✅ Todos os equipamentos existentes classificados
- ✅ Ações corretivas listadas
- ✅ Usuário informado sobre Inválidos

---

## Módulo 06 — Criação de Prumadas

### Objetivo
Criar colunas verticais de AF, ES e VE nos eixos hidráulicos ótimos com base no agrupamento de ambientes.

### Escopo
**Incluído:** Clustering 2D, cálculo de centroide, definição de tipos, dimensionamento preliminar, criação de Pipes verticais.

**Não incluído:** Detecção de shafts estruturais. Clash com pilares (verificação simplificada apenas). Prumadas para água quente.

### Entradas
- Ambientes com equipamentos (Módulos 04/05)
- Levels do modelo
- Parâmetros normativos (JSON)
- **Dependências:** Módulos 04 e 05 concluídos

### Processamento
1. Projetar pontos centrais de ambientes relevantes no plano XY
2. Cluster por proximidade (threshold configurável, padrão 3m)
3. Para cada cluster: calcular centroide como posição da prumada
4. Definir tipos necessários: AF (sempre), ES (se ambientes com esgoto), VE (se existe TQ)
5. Dimensionar diâmetro:
   - AF: Q_total = 0.3 × √(ΣPesos_todos_pavimentos) → DN
   - ES: ΣUHCs_todos_pavimentos → tabela → DN tubo de queda
   - VE: DN ≥ 2/3 × DN_tq
6. Criar Pipe vertical para cada prumada: do Level mais baixo ao mais alto
7. Validar posição (não colide com parede ou pilar — verificação BoundingBox)

### Saídas
- Prumadas criadas no modelo (Pipes verticais)
- Mapeamento: prumada → ambientes atendidos → Levels
- Dimensionamento de cada prumada

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Clustering, dimensionamento, criação |
| **Dynamo** | Criação de prumadas complexas com derivações (`08_CriarPrumadas.dyn`) |
| **unMEP** | Não atua |

### Regras de Negócio
- 1 grupo de prumadas por cluster de ambientes alinhados verticalmente
- DN do TQ nunca menor que o maior ramal de esgoto (RN-ES-07)
- DN da ventilação ≥ 2/3 DN do TQ (RN-VE-06)
- Prumadas de AF e ES devem estar próximas (< 0.5m)

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Nenhum cluster identificado | Crítico |
| Centroide sobre parede ou pilar | Médio |
| Prumada não conecta todos os Levels | Médio |

### Limitações
- Centroide pode cair sobre elemento estrutural
- Em plantas com layouts muito diferentes entre pavimentos, clustering pode ser impreciso
- Shafts existentes no modelo não são detectados automaticamente

### Critérios de Conclusão
- ✅ Todas as prumadas criadas e dimensionadas
- ✅ Cada ambiente relevante está associado a uma prumada
- ✅ Usuário validou posições

---

## Módulo 07 — Geração de Rede de Água Fria

### Objetivo
Traçar a rede completa de AF desde barrilete até cada ponto de consumo, com ramais dimensionados, registros e fittings.

### Escopo
**Incluído:** Topologia da rede. Traçado de barrilete, ramais e sub-ramais. Inserção de registros. Fittings. Conexão com connectors. Dimensionamento integrado.

**Não incluído:** Posição do reservatório (pré-definida). Instalação de pressurizador. Rede de água quente.

### Entradas
- Prumadas de AF (Módulo 06)
- Pontos de consumo com pesos (Módulo 03)
- Equipamentos posicionados com connectors (Módulos 04/05)
- Configuração: altura reservatório, P_min, V_max
- **Dependências:** Módulos 03, 04/05, 06 concluídos

### Processamento
1. Definir ponto de alimentação: base do barrilete (cota do reservatório)
2. Traçar barrilete horizontal no nível mais alto → conectar às prumadas
3. Para cada pavimento:
   a. Definir ponto de derivação na prumada (tee)
   b. Traçar ramal horizontal até cada ambiente
   c. Inserir registro de gaveta na entrada de cada ambiente
   d. Traçar sub-ramais até connector de cada equipamento
   e. Inserir fittings (tees, curvas 90°, reduções)
4. Dimensionar cada trecho (Módulo 11): peso → vazão → diâmetro → perda de carga → pressão
5. Verificar P_disponível ≥ 3 m.c.a. em todos os pontos
6. Atribuir todos os elementos ao PipingSystem de AF
7. Conectar segmentos via Connector.ConnectTo()

### Saídas
- Rede de AF completa: Pipes + Fittings + Registros
- PipingSystem "AF - Água Fria"
- Relatório de dimensionamento por trecho

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Topologia, dimensionamento, criação de elementos simples |
| **Dynamo** | Traçado de ramais horizontais (`05_GerarRamalAguaFria.dyn`), conexão (`09_ConectarRede.dyn`) |
| **unMEP** | Rotas complexas com desvio de obstáculos |

### Regras de Negócio
- Q = 0.3 × √ΣP (NBR 5626) | V ≤ 3.0 m/s | P_min ≥ 3.0 m.c.a. | P_max ≤ 40 m.c.a.
- Registro de gaveta obrigatório na entrada de cada ambiente (RN-AF-12)
- DN do sub-ramal ≥ DN mínimo do aparelho (RN-AF-13)
- Rota mais curta possível (RN-AF-10)

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Pressão < 3.0 m.c.a. em algum ponto | Crítico |
| Velocidade > 3.0 m/s em algum trecho | Crítico |
| Equipamento não conectado à rede | Crítico |
| Pressão > 40 m.c.a. | Médio |

### Limitações
- Dynamo não desvia de vigas/pilares automaticamente → delegar ao unMEP
- unMEP pode gerar rotas com curvas excessivas
- Rotas entre pavimentos com layouts diferentes são complexas

### Critérios de Conclusão
- ✅ Todos os pontos de consumo conectados
- ✅ Todos os trechos dimensionados
- ✅ Pressão ≥ 3 m.c.a. em todos os pontos
- ✅ Velocidade ≤ 3 m/s em todos os trechos
- ✅ Registros inseridos

---

## Módulo 08 — Geração de Rede de Esgoto

### Objetivo
Traçar a rede de esgoto desde cada aparelho até tubos de queda e subcoletor, incluindo caixas sifonadas, gordura e inspeção.

### Escopo
**Incluído:** Ramais de descarga. Ramais de esgoto. Caixas sifonadas, gordura e inspeção. Subcoletor até CI externa. Dimensionamento por UHC.

**Não incluído:** Coletor predial (após CI externa). Rede externa ao edifício. Sistema pluvial.

### Entradas
- Pontos de descarga (Módulo 03)
- Equipamentos posicionados (Módulos 04/05)
- Prumadas de esgoto/TQ (Módulo 06)
- **Dependências:** Módulos 03, 04/05, 06 concluídos

### Processamento
1. Para cada ambiente:
   a. Traçar ramais de descarga: equipamento → CX sifonada ou ramal de esgoto
   b. Inserir CX sifonada (banheiros: obrigatória)
   c. Vaso sanitário → ramal independente (DN 100mm) direto ao ramal de esgoto (RN-ES-11)
   d. Traçar ramal de esgoto → tubo de queda
2. Cozinha: inserir CX de gordura na saída da pia
3. Térreo: traçar subcoletor do TQ até posição de CI externa
4. Inserir CIs em mudanças de direção > 90° e a cada ~15m
5. Dimensionar por UHC: somar UHCs por trecho → consultar tabela → DN
6. Verificar regra de não-diminuição do diâmetro (RN-ES-04)
7. Atribuir ao PipingSystem de esgoto

### Saídas
- Rede de ES completa: Pipes + Fittings + CXs
- PipingSystem "ES - Esgoto Sanitário"
- Relatório de UHC por trecho

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Topologia, dimensionamento UHC, decisão de CXs |
| **Dynamo** | Traçado de ramais (`06_GerarRamalEsgoto.dyn`) |
| **unMEP** | Roteamento de subcoletor |

### Regras de Negócio
- Vaso: ramal independente DN ≥ 100mm (RN-ES-05/RN-ES-11)
- CX sifonada obrigatória em banheiros (RN-ES-08)
- CX gordura obrigatória em cozinhas (RN-ES-09)
- DN nunca diminui no sentido do escoamento (RN-ES-04)
- Subcoletor mínimo DN 100mm (RN-ES-06)
- Evitar curvas de 90° — usar 2×45° (RN-ES-13)

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Ramal de vaso < 100mm | Crítico |
| DN diminui no escoamento | Crítico |
| Sem CX sifonada em banheiro | Médio |
| Sem CX gordura em cozinha | Médio |
| Equipamento não conectado | Crítico |

### Limitações
- Subcoletor externo depende de informação de implantação (frequentemente indisponível)
- Elevações de esgoto exigem atenção especial (gravitacional)
- Curvas de 45° requerem família de fitting específica

### Critérios de Conclusão
- ✅ Todos os aparelhos conectados ao ramal de esgoto
- ✅ CXs inseridas conforme norma
- ✅ DN dimensionado por UHC e sem diminuição
- ✅ Subcoletor traçado (quando informação disponível)

---

## Módulo 09 — Aplicação de Inclinações

### Objetivo
Aplicar declividade em todos os trechos horizontais de esgoto conforme norma, ajustando elevação dos endpoints e fittings.

### Escopo
**Incluído:** Identificação de trechos horizontais. Cálculo de inclinação por DN. Ajuste de elevação. Reajuste de fittings. Verificação pós-ajuste.

**Não incluído:** Resolução de conflito de espaço (laje fina). Criação de rebaixo de piso. Alteração de rota.

### Entradas
- Rede de esgoto (Módulo 08)
- Diâmetro de cada trecho
- **Dependências:** Módulo 08 concluído

### Processamento
1. Coletar todos os Pipes do PipingSystem de esgoto
2. Filtrar: apenas trechos horizontais (|Z_início - Z_fim| < tolerância)
3. Para cada trecho:
   a. DN ≤ 75mm → inclinação = 0.02 (2%)
   b. DN ≥ 100mm → inclinação = 0.01 (1%)
   c. Desnível = comprimento × inclinação
   d. Z_final_novo = Z_inicial - desnível (escoamento de montante para jusante)
   e. Ajustar endpoint do Pipe
4. Reajustar fittings conectados a endpoints movidos
5. Verificar: nenhum trecho subindo no sentido do escoamento
6. Verificação simplificada de interferência (BoundingBox vs. laje inferior)

### Saídas
- Tubos com inclinação aplicada
- Log por trecho: comprimento, desnível, inclinação
- Lista de interferências detectadas

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Cálculo de inclinação e desnível |
| **Dynamo** | Ajuste físico em massa (`07_AplicarInclinacao.dyn`) |
| **unMEP** | Não atua |

### Regras de Negócio
- 2% para DN ≤ 75mm (RN-ES-01). 1% para DN ≥ 100mm (RN-ES-02)
- Máxima recomendada: 5% (RN-ES-03)
- Escoamento sempre de montante (equipamento) para jusante (TQ)

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Trecho sem inclinação | Crítico |
| Inclinação < mínima | Crítico |
| Inclinação > 5% | Leve |
| Trecho subindo contra escoamento | Crítico |
| Interferência com laje | Médio |

### Limitações
- Fittings podem desconectar e exigir reconexão manual
- Lajes com espessura < 15cm podem não ter espaço
- Ajuste em cascata: mover um trecho pode afetar trechos conectados

### Critérios de Conclusão
- ✅ Todos os trechos horizontais de esgoto com inclinação aplicada
- ✅ Nenhum trecho contra a gravidade
- ✅ Fittings reconectados

---

## Módulo 10 — Criação de Sistemas MEP

### Objetivo
Criar PipingSystems (AF, ES, VE), atribuir todos os elementos e validar conectividade e organização.

### Escopo
**Incluído:** Criação de 3 sistemas. Atribuição de elementos. Validação de conectividade. Cores por sistema. Nomenclatura.

**Não incluído:** Criação de System Types (devem existir no template). Reconexão de elementos desconectados.

### Entradas
- Todos os Pipes e Fittings criados (Módulos 06–09)
- System Types configurados no modelo
- **Dependências:** Módulos 07, 08, 09 concluídos

### Processamento
1. Criar PipingSystem "AF - Água Fria" (tipo DomesticColdWater)
2. Criar PipingSystem "ES - Esgoto Sanitário" (tipo Sanitary)
3. Criar PipingSystem "VE - Ventilação" (tipo Vent)
4. Para cada elemento MEP: atribuir ao sistema correto via Connector.MEPSystem
5. Verificar: elementos sem sistema atribuído
6. Verificar: conectividade topológica por sistema (sem ilhas)
7. Aplicar Override de cor: AF = azul, ES = marrom, VE = verde

### Saídas
- 3 PipingSystems criados e populados
- Relatório de conectividade
- Visualização por cores

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Criação de sistemas, atribuição, validação — tudo via API |
| **Dynamo** | Não atua |
| **unMEP** | Não atua |

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Elemento sem sistema | Médio |
| Sistema desconectado (ilhas) | Médio |
| System Type não existe | Crítico |

### Critérios de Conclusão
- ✅ 3 sistemas criados. ✅ Todos os elementos atribuídos. ✅ Cores aplicadas.

---

## Módulo 11 — Dimensionamento Hidráulico

### Objetivo
Motor de cálculo que dimensiona AF (vazão, diâmetro, velocidade, perda de carga, pressão) e ES (UHC, diâmetro).

### Escopo
**Incluído:** Cálculos completos de AF e ES conforme NBR 5626 e NBR 8160. Aplicação de diâmetros no modelo.

**Não incluído:** Decisão sobre pressurizador. Cálculos de água quente. Dimensionamento de reservatório.

### Entradas
- Topologia da rede (comprimentos, conexões)
- Pesos dos aparelhos por trecho (AF)
- UHCs por trecho (ES)
- Parâmetros de configuração
- **Dependências:** Módulos 07 e 08 concluídos (topologia definida)

### Processamento
**Água Fria:**
1. Percorrer a rede de jusante para montante
2. Por trecho: ΣPesos → Q = 0.3 × √ΣP → selecionar DN onde V ≤ 3 m/s → J (Fair-Whipple-Hsiao) → ΔH = J × L × 1.20
3. Caminho crítico: somar ΔH do reservatório ao ponto mais desfavorável
4. P_disponível = H_geom - ΣΔH → verificar ≥ 3 m.c.a.
5. Aplicar diâmetro no parâmetro do Pipe no modelo

**Esgoto:**
1. Percorrer de montante para jusante
2. Por trecho: ΣUHC → tabela → DN mínimo
3. Verificar não-diminuição de DN
4. Aplicar diâmetro no modelo

### Saídas
- `ResultadoDimensionamento` por trecho: DN, Q, V, J, ΔH, P
- Relatório completo AF + ES
- Diâmetros atualizados no modelo

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Todos os cálculos — lógica pura |
| **Dynamo** | Não atua |
| **unMEP** | Não atua |

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| P < 3 m.c.a. | Crítico |
| V > 3 m/s | Crítico |
| P > 40 m.c.a. | Médio |
| DN diminui em ES | Crítico |

### Critérios de Conclusão
- ✅ Todos os trechos dimensionados. ✅ Pressão verificada. ✅ Diâmetros aplicados no modelo.

---

## Módulo 12 — Geração de Tabelas

### Objetivo
Criar ViewSchedules com quantitativos de tubulações, conexões e equipamentos, formatados para documentação.

### Escopo
**Incluído:** 4 Schedules: tubulações, conexões, equipamentos, resumo. Formatação. Exportação CSV.

**Não incluído:** Tabelas personalizadas non standard. Formatação avançada (merge de células).

### Entradas
- Elementos MEP criados (Pipes, Fittings, Fixtures)
- Sistemas MEP (Módulo 10)
- **Dependências:** Módulo 10 concluído

### Processamento
1. Criar ViewSchedule "Tubulações" via `ViewSchedule.CreateSchedule()`: campos = System, Diameter, Length; agrupa por System+Diameter; soma Length
2. Criar ViewSchedule "Conexões": campos = Family, Diameter, Count; agrupa por Family+Diameter
3. Criar ViewSchedule "Equipamentos": campos = Room, Family, Count
4. Criar ViewSchedule "Resumo": campos = Room, Classification, PointsAF, PointsES
5. Configurar unidades (m, mm), totais, cabeçalhos
6. Exportação opcional para CSV via `ViewSchedule.Export()`

### Saídas
- 4 ViewSchedules no modelo
- Arquivos CSV (se solicitado)

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Criação de Schedules via API |
| **Dynamo** | Exportação Excel (`10_GerarTabelas.dyn`) |
| **unMEP** | Não atua |

### Critérios de Conclusão
- ✅ 4 Schedules criadas com dados corretos. ✅ Formatação aplicada.

---

## Módulo 13 — Geração de Pranchas

### Objetivo
Criar ViewSheets com plantas por sistema/pavimento, schedules, legendas e numeração padronizada.

### Escopo
**Incluído:** View Templates. Duplicação de Floor Plans. ViewSheets. Posicionamento de views. Numeração. Inserção de schedules.

**Não incluído:** Ajuste fino de layout. Notas específicas do projeto. Criação de titleblock.

### Entradas
- Views de planta. Schedules (Módulo 12). Titleblock. View Templates.
- **Dependências:** Módulo 12 concluído

### Processamento
1. Criar/aplicar View Template para AF (filtro: mostra apenas PipingSystem AF)
2. Criar/aplicar View Template para ES (filtro: mostra apenas PipingSystem ES)
3. Duplicar Floor Plan por pavimento, aplicar template por sistema
4. Configurar escala (1:50) e Crop Region
5. Criar ViewSheet com titleblock
6. Posicionar view via `Viewport.Create(sheet, viewId, center)`
7. Inserir schedules na prancha
8. Numerar: HID-01, HID-02...

### Saídas
- Pranchas completas no modelo
- Views configuradas por sistema
- Numeração padronizada

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Criação de Sheets, Views, Viewports |
| **Dynamo** | Layout automático e posicionamento (`11_GerarPranchas.dyn`) |
| **unMEP** | Não atua |

### Critérios de Conclusão
- ✅ Pranchas criadas para todos os pavimentos/sistemas. ✅ Schedules inseridas. ✅ Numeração aplicada.

---

## Módulo 14 — Sistema de Logs e Diagnóstico

### Objetivo
Registrar todas as ações, decisões e erros com rastreabilidade completa, exportação JSON e feedback em tempo real.

### Escopo
**Incluído:** 4 níveis de log. Acumulação em memória. Exportação JSON. Resumo por nível/etapa. Verificação de bloqueio. Filtragem.

**Não incluído:** Persistência em banco de dados. Envio de logs por rede. Analytics.

### Entradas
- Eventos de todos os módulos (F01–F13)
- **Dependências:** Nenhuma (disponível desde o início)

### Processamento
1. Módulos registram via `ILogService.Info/Leve/Medio/Critico(etapa, componente, mensagem, elementId?)`
2. `LogManager` acumula `LogEntry` em `List<LogEntry>` (thread-safe)
3. `TemBloqueio` = `entries.Any(e => e.Level == Critico)`
4. Exportar: `JsonConvert.SerializeObject(entries, Formatting.Indented)` → `Data/Logs/log_{timestamp}.json`
5. Resumo: contar por nível, listar por etapa

### Saídas
- Log acumulado acessível pela UI
- Arquivo JSON exportado
- Flag `TemBloqueio`
- Resumo textual

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Implementação completa do LogManager |
| **Dynamo** | Não atua |
| **unMEP** | Não atua |

### Critérios de Conclusão
- ✅ Módulo disponível para todos os serviços. ✅ Exportação funcional. ✅ Bloqueio funcional.

---

## Módulo 15 — Interface do Plugin (WPF)

### Objetivo
Fornecer janela WPF com 3 abas (Configuração, Execução, Diagnóstico) para controle total do pipeline.

### Escopo
**Incluído:** 3 abas. MVVM. Controle de pipeline. Validação de parâmetros. Status visual por etapa. Logs em tempo real. Navegação ao elemento.

**Não incluído:** UI de posicionamento 3D. Preview gráfico de redes. Integração com Navisworks.

### Entradas
- Estado do pipeline (orquestrador)
- Dados dos ambientes (Módulos 01–02)
- Logs (Módulo 14)
- Configuração (JSON)
- **Dependências:** Módulo 14 para diagnóstico. Demais módulos para execução.

### Processamento
**Configuração:** Campos editáveis com validação. Salvar/carregar JSON. Feedback visual.
**Execução:** Lista de 11 etapas com status (ícone+cor). Botões executar/aprovar/rejeitar/re-executar. Barra de progresso. Operações via ExternalEvent (thread-safe).
**Diagnóstico:** DataGrid com binding a ObservableCollection. Filtros por nível/etapa. Cores por severidade. Contadores. Exportar. Click → selecionar elemento no Revit.

### Saídas
- Controle de execução do pipeline
- Configuração persistida
- Feedback visual ao usuário

### Integrações
| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | WPF, MVVM, ExternalEventHandlers |
| **Dynamo** | Não atua |
| **unMEP** | Não atua |

### Regras de Negócio
- Etapa só executa se pré-condições atendidas
- Erro Crítico bloqueia etapas seguintes
- Validação humana obrigatória entre etapas com erros Médios
- UI não bloqueia durante execução (ExternalEvent)

### Validações
| Validação | Nível se falha |
|-----------|---------------|
| Janela aberta sem modelo ativo | Erro → fecha janela |
| JSON corrompida | Carrega valores padrão |
| ExternalEvent falha | Crítico + mensagem |

### Critérios de Conclusão
- ✅ 3 abas funcionais. ✅ Pipeline controlável. ✅ Logs em tempo real. ✅ Navegação ao elemento funcional.
