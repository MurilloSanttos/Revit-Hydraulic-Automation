# Critérios de Aceitação por Módulo — Plugin Hidráulico Revit 2026

> Documento de QA com critérios testáveis e mensuráveis para validação de cada módulo do sistema de automação hidráulica.

---

## Módulo 01 — Detecção de Ambientes (Rooms + Spaces)

### Objetivo do Teste
Verificar que o sistema lê corretamente todos os Rooms do modelo, filtra inválidos, identifica correspondência com Spaces e cria Spaces faltantes.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-01-01 | **Dado** um modelo com 15 Rooms nomeados e válidos, **quando** executar detecção, **então** os 15 Rooms são lidos e aparecem na lista de ambientes com nome, número, nível, área (m²), perímetro (m) e ponto central. |
| CA-01-02 | **Dado** um modelo com 3 Rooms sem Location (não colocados), **quando** executar detecção, **então** os 3 são ignorados e registrados no log como nível Leve com ElementId e motivo "sem Location". |
| CA-01-03 | **Dado** um modelo com 2 Rooms com área zero, **quando** executar detecção, **então** os 2 são ignorados e registrados no log como nível Leve. |
| CA-01-04 | **Dado** um modelo onde 5 Rooms não possuem Space correspondente, **quando** executar detecção e o usuário confirmar, **então** 5 Spaces são criados com mesmo Name e Number do Room. |
| CA-01-05 | **Dado** um modelo onde o usuário rejeita criação de Spaces, **quando** executar detecção, **então** nenhum Space é criado e log Info registra "criação rejeitada pelo usuário". |
| CA-01-06 | **Dado** um modelo com 2 Spaces sem Room correspondente (órfãos), **quando** executar detecção, **então** os 2 são listados como órfãos no log nível Leve. |
| CA-01-07 | **Dado** um modelo sem nenhum Room, **quando** executar detecção, **então** erro Crítico é gerado e pipeline é bloqueado. |
| CA-01-08 | **Dado** que a criação de 1 Space falha (exceção da API), **quando** executar criação, **então** a Transaction é revertida, log Crítico é gerado com ElementId do Room afetado, e os demais Spaces já criados persistem (Transactions independentes). |

### Casos de Sucesso
- Modelo com 20 Rooms, 15 com Space existente, 5 sem → 5 Spaces criados com sucesso
- Modelo com todos os Rooms tendo Spaces → nenhuma criação necessária, log Info "todos os Rooms já possuem Space"
- Conversão de área: Room com 100 ft² → AmbienteInfo com 9.29 m²

### Casos de Falha
- 0 Rooms → Crítico, pipeline bloqueado
- Falha na Transaction → Crítico por elemento, rollback
- Room sem Level → Médio, Space não é criado para este Room

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Taxa de leitura de Rooms válidos | 100% |
| Taxa de correspondência Room↔Space (modelo preparado) | ≥ 95% |
| Tempo de execução (20 Rooms) | ≤ 3 segundos |
| Tempo de execução (50 Rooms) | ≤ 8 segundos |

### Dependências
- Modelo com Rooms definidos
- Ao menos 1 Level no modelo

### Critérios de Bloqueio
- ❌ 0 Rooms encontrados → **pipeline bloqueado**
- ❌ Erro Crítico na criação de Space → **módulo finaliza com status de erro**

---

## Módulo 02 — Classificação de Ambientes

### Objetivo do Teste
Verificar que o classificador identifica corretamente o tipo de cada ambiente com confiança mensurável.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-02-01 | **Dado** um ambiente chamado "Banheiro Social", **quando** classificar, **então** tipo = Banheiro, confiança ≥ 0.85. |
| CA-02-02 | **Dado** um ambiente chamado "BWC", **quando** classificar, **então** tipo = Banheiro, confiança ≥ 0.70. |
| CA-02-03 | **Dado** um ambiente chamado "Cozinha Gourmet", **quando** classificar, **então** tipo = CozinhaGourmet (não Cozinha), confiança ≥ 0.85. |
| CA-02-04 | **Dado** um ambiente chamado "Sala de Estar", **quando** classificar, **então** tipo = NaoIdentificado, confiança < 0.50. |
| CA-02-05 | **Dado** um ambiente chamado "Banheiro 01" (com numeração), **quando** normalizar, **então** texto normalizado = "banheiro" (sem número). |
| CA-02-06 | **Dado** um ambiente chamado "SUÍTE MÁSTER" (com acentos e caps), **quando** normalizar, **então** texto normalizado = "suite master". |
| CA-02-07 | **Dado** 20 ambientes classificados com 3 com confiança < 0.70, **quando** classificar todos, **então** os 3 são sinalizados com `NecessitaValidacao = true` e log Médio. |
| CA-02-08 | **Dado** que o usuário reclassifica um ambiente de NaoIdentificado para Lavanderia, **quando** reclassificar, **então** confiança = 1.0 e padrão = "manual". |
| CA-02-09 | **Dado** que nenhum ambiente é classificado como relevante, **quando** classificar todos, **então** erro Crítico e pipeline bloqueado. |

### Casos de Sucesso
- Corpus de 50 nomes reais → ≥ 45 classificados corretamente (≥ 90%)
- "Área de Serviço", "A.S.", "Lavanderia", "Lav. Roupas" → todos classificados corretamente

### Casos de Falha
- "Storage Room" (inglês) → NaoIdentificado
- 0 ambientes relevantes → Crítico
- Todos com confiança < 0.70 → Médio

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Taxa de acerto em corpus de 50+ nomes | ≥ 90% |
| Confiança média (ambientes corretos) | ≥ 0.80 |
| Taxa de falsos positivos | ≤ 5% |
| Tempo de classificação (50 ambientes) | ≤ 1 segundo |

### Dependências
- Módulo 01 concluído

### Critérios de Bloqueio
- ❌ 0 ambientes relevantes → **pipeline bloqueado**

---

## Módulo 03 — Identificação de Pontos Hidráulicos

### Objetivo do Teste
Verificar que o sistema identifica corretamente todos os pontos hidráulicos necessários por ambiente.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-03-01 | **Dado** um banheiro classificado, **quando** identificar pontos, **então** lista contém: vaso (AF+ES, DN100), lavatório (AF+ES, DN40), chuveiro (AF+ES, DN40), ralo (ES, DN40). |
| CA-03-02 | **Dado** uma cozinha classificada, **quando** identificar pontos, **então** lista contém: pia (AF+ES, DN50), ralo (ES, DN40). |
| CA-03-03 | **Dado** um banheiro com lavatório já existente no modelo (fixture com connector AF), **quando** comparar existentes vs. necessários, **então** lavatório é marcado como "existente" e não aparece na lista de faltantes. |
| CA-03-04 | **Dado** um banheiro com fixture de arquitetura (sem connector MEP), **quando** comparar, **então** fixture é marcado como "existente mas inválido" e ponto permanece como faltante. |
| CA-03-05 | **Dado** 5 ambientes relevantes, **quando** identificar todos os pontos, **então** totalização de pesos AF e UHCs ES é calculada corretamente. |

### Casos de Sucesso
- Banheiro completo (4 pontos) + cozinha (2 pontos) + lavanderia (3 pontos) = 9 pontos totais
- Todos os pesos e UHCs conforme tabela normativa

### Casos de Falha
- Ambiente classificado sem tabela → Médio
- 0 pontos para projeto inteiro → Crítico

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Cobertura de pontos por ambiente | 100% dos tipos esperados listados |
| Precisão de detecção de existentes | ≥ 90% |
| Tempo (20 ambientes) | ≤ 2 segundos |

### Dependências
- Módulo 02 concluído

### Critérios de Bloqueio
- ❌ Total de pontos = 0 → **pipeline bloqueado**

---

## Módulo 04 — Inserção de Equipamentos

### Objetivo do Teste
Verificar que equipamentos são inseridos na posição correta com connectors válidos.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-04-01 | **Dado** um banheiro retangular com vaso faltante, **quando** inserir, **então** vaso é inserido na parede oposta à porta, a ≥ 15cm da parede lateral, com connector de ES DN100. |
| CA-04-02 | **Dado** que a família "Vaso_Sanitario_MEP" não existe no modelo, **quando** verificar famílias, **então** log Crítico é gerado com "FamilySymbol não encontrada: Vaso_Sanitario_MEP". |
| CA-04-03 | **Dado** 3 equipamentos inseridos com sucesso e 1 com falha, **quando** gerar relatório, **então** relatório mostra 3 inseridos e 1 falha com motivo. |
| CA-04-04 | **Dado** um equipamento inserido, **quando** validar pós-inserção, **então** equipamento possui ao menos 1 connector de AF e/ou ES conforme tipo. |
| CA-04-05 | **Dado** que o usuário não confirma inserção, **quando** solicitar confirmação, **então** nenhum equipamento é inserido e log Info gerado. |

### Casos de Sucesso
- Banheiro retangular 2.5×3.0m: vaso, lavatório, chuveiro, ralo inseridos sem colisão
- Todos com connectors validados pós-inserção

### Casos de Falha
- Família não encontrada → Crítico por equipamento
- Ambiente sem parede válida (todas com porta) → Médio
- Colisão com elemento existente → Médio

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Taxa de inserção em ambientes retangulares | ≥ 90% |
| Taxa de inserção em ambientes irregulares | ≥ 50% |
| Equipamentos com connectors válidos pós-inserção | 100% |
| Tempo (10 equipamentos) | ≤ 10 segundos |

### Dependências
- Módulo 03 concluído. Famílias MEP na biblioteca.

### Critérios de Bloqueio
- ❌ 0 famílias disponíveis → **módulo não executa**
- ❌ > 50% das inserções falharam → **erro Crítico**

---

## Módulo 05 — Validação de Equipamentos Existentes

### Objetivo do Teste
Verificar que equipamentos existentes são corretamente classificados como Válido, Com Ressalva ou Inválido.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-05-01 | **Dado** um vaso sanitário MEP com connector ES DN100 posicionado a 20cm da parede, **quando** validar, **então** status = Válido. |
| CA-05-02 | **Dado** um lavatório de arquitetura (sem connectors MEP), **quando** validar, **então** status = Inválido, motivo = "sem connectors MEP". |
| CA-05-03 | **Dado** um chuveiro a 1.5m da parede (distância excessiva), **quando** validar, **então** status = Com Ressalva, motivo = "distância da parede acima do esperado". |
| CA-05-04 | **Dado** 10 equipamentos onde 6 são Inválidos, **quando** validar todos, **então** erro Crítico ("mais de 50% inválidos"). |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Taxa de classificação correta | ≥ 95% |
| Tempo (20 equipamentos) | ≤ 5 segundos |

### Dependências
- Módulo 03 concluído

### Critérios de Bloqueio
- ❌ > 50% dos equipamentos Inválidos → **erro Crítico**

---

## Módulo 06 — Criação de Prumadas

### Objetivo do Teste
Verificar que prumadas são criadas nas posições ótimas com diâmetros corretos.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-06-01 | **Dado** 3 banheiros alinhados verticalmente (1 por pavimento), **quando** criar prumadas, **então** 1 grupo de prumadas é criado (AF+ES+VE) na posição do centroide dos 3. |
| CA-06-02 | **Dado** 2 clusters de ambientes separados por > 5m, **quando** criar prumadas, **então** 2 grupos independentes de prumadas são criados. |
| CA-06-03 | **Dado** prumada de esgoto atendendo 3 pavimentos com ΣUHCs = 24, **quando** dimensionar, **então** DN tubo de queda = 75mm (≤ 30 UHC na tabela). |
| CA-06-04 | **Dado** DN do tubo de queda = 100mm, **quando** dimensionar ventilação, **então** DN ventilação ≥ 75mm (2/3 × 100). |
| CA-06-05 | **Dado** prumada criada, **quando** verificar, **então** Pipe conecta Level mais baixo ao Level mais alto com Z correto. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Clusters identificados corretamente | ≥ 90% |
| Diâmetros conforme tabela normativa | 100% |
| Tempo (3 pavimentos, 2 clusters) | ≤ 10 segundos |

### Dependências
- Módulos 04/05 concluídos. Levels corretos.

### Critérios de Bloqueio
- ❌ 0 clusters identificados → **pipeline bloqueado**
- ❌ Prumada colide com pilar (quando modelo estrutural presente) → **Médio, solicita reposicionamento**

---

## Módulo 07 — Geração de Rede de Água Fria

### Objetivo do Teste
Verificar que a rede AF conecta todos os pontos de consumo com dimensionamento normativo correto.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-07-01 | **Dado** 10 pontos de consumo em 3 ambientes, **quando** gerar rede, **então** todos os 10 connectors estão conectados a Pipes da rede AF. |
| CA-07-02 | **Dado** um banheiro com vaso (P=0.3), lavatório (P=0.5), chuveiro (P=0.5), **quando** dimensionar ramal do banheiro, **então** Q = 0.3 × √1.3 = 0.342 L/s, DN ≥ 25mm. |
| CA-07-03 | **Dado** reservatório a 6m e ponto mais desfavorável a 3m de altura, **quando** verificar pressão, **então** P_disponível = (6-3) - ΣJ ≥ 3 m.c.a. |
| CA-07-04 | **Dado** que a pressão é 2.5 m.c.a. em um ponto, **quando** verificar, **então** erro Crítico "pressão insuficiente" com ElementId do ponto. |
| CA-07-05 | **Dado** um ramal com V = 3.5 m/s, **quando** verificar, **então** erro Crítico "velocidade excedida" e sugestão de aumentar diâmetro. |
| CA-07-06 | **Dado** banheiro com rede AF, **quando** verificar registros, **então** registro de gaveta presente na entrada do ambiente. |
| CA-07-07 | **Dado** rede completa, **quando** verificar sistema, **então** todos os Pipes estão atribuídos ao PipingSystem "AF - Água Fria". |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Pontos de consumo conectados | 100% |
| Pressão ≥ 3 m.c.a. em todos os pontos | 100% (ou erro Crítico) |
| Velocidade ≤ 3 m/s em todos os trechos | 100% (ou erro Crítico) |
| Registros na entrada de cada ambiente | 100% |
| Tempo (15 pontos, 3 pavimentos) | ≤ 30 segundos |

### Dependências
- Módulos 03, 04/05, 06 concluídos

### Critérios de Bloqueio
- ❌ Pressão < 3 m.c.a. em qualquer ponto → **Crítico**
- ❌ Equipamento não conectado → **Crítico**
- ❌ Velocidade > 3 m/s não resolvida → **Crítico**

---

## Módulo 08 — Geração de Rede de Esgoto

### Objetivo do Teste
Verificar que a rede ES coleta todos os efluentes com dimensionamento por UHC e acessórios normativos.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-08-01 | **Dado** banheiro com vaso (UHC=6), lavatório (1), chuveiro (2), ralo (1), **quando** dimensionar ramal de esgoto, **então** ΣUHC=10, DN ≥ 75mm (tabela: ≤20 UHC → 75mm). |
| CA-08-02 | **Dado** ramal de descarga do vaso, **quando** verificar, **então** DN ≥ 100mm. |
| CA-08-03 | **Dado** banheiro, **quando** verificar, **então** caixa sifonada presente no ambiente. |
| CA-08-04 | **Dado** cozinha, **quando** verificar, **então** caixa de gordura presente na saída da pia. |
| CA-08-05 | **Dado** ramal de esgoto DN 75mm conectando a subcoletor DN 50mm, **quando** verificar, **então** erro Crítico "diâmetro diminui no sentido do escoamento". |
| CA-08-06 | **Dado** vaso sanitário, **quando** verificar ramal, **então** ramal é independente (não passa pela CX sifonada). |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Aparelhos conectados ao sistema de esgoto | 100% |
| CX sifonada em banheiros | 100% |
| CX gordura em cozinhas | 100% |
| DN nunca diminui no escoamento | 100% |
| Ramal de vaso ≥ 100mm | 100% |
| Tempo (15 aparelhos, 3 pavimentos) | ≤ 30 segundos |

### Dependências
- Módulos 03, 04/05, 06 concluídos

### Critérios de Bloqueio
- ❌ DN diminui no escoamento → **Crítico**
- ❌ Ramal de vaso < 100mm → **Crítico**
- ❌ Aparelho não conectado → **Crítico**

---

## Módulo 09 — Aplicação de Inclinações

### Objetivo do Teste
Verificar que declividade correta é aplicada em todos os trechos horizontais de esgoto.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-09-01 | **Dado** trecho horizontal de esgoto DN 50mm com comprimento 2.0m, **quando** aplicar inclinação, **então** desnível = 2.0 × 0.02 = 0.04m (4cm), Z_final = Z_inicial - 0.04. |
| CA-09-02 | **Dado** trecho DN 100mm com comprimento 3.0m, **quando** aplicar, **então** desnível = 3.0 × 0.01 = 0.03m. |
| CA-09-03 | **Dado** todos os trechos com inclinação aplicada, **quando** verificar, **então** nenhum trecho horizontal de esgoto tem Z_final ≥ Z_inicial (nenhum "sobe"). |
| CA-09-04 | **Dado** trecho com inclinação aplicada de 7%, **quando** verificar, **então** alerta Leve "inclinação acima do recomendado (5%)". |
| CA-09-05 | **Dado** 10 trechos de esgoto, **quando** aplicar inclinação, **então** todos os 10 possuem inclinação dentro da faixa [1%–5%]. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Trechos com inclinação aplicada | 100% |
| Trechos dentro da faixa normativa | 100% (ou erro) |
| Fittings reconectados após ajuste | ≥ 95% |
| Tempo (20 trechos) | ≤ 10 segundos |

### Dependências
- Módulo 08 concluído

### Critérios de Bloqueio
- ❌ Trecho sem inclinação → **Crítico**
- ❌ Trecho contra gravidade → **Crítico**

---

## Módulo 10 — Criação de Sistemas MEP

### Objetivo do Teste
Verificar que os 3 PipingSystems são criados, populados e com conectividade validada.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-10-01 | **Dado** rede completa, **quando** criar sistemas, **então** 3 PipingSystems existem: "AF - Água Fria", "ES - Esgoto Sanitário", "VE - Ventilação". |
| CA-10-02 | **Dado** 30 Pipes + 15 Fittings no modelo, **quando** atribuir, **então** todos os 45 elementos possuem sistema atribuído (0 sem sistema). |
| CA-10-03 | **Dado** sistemas criados, **quando** aplicar cores, **então** AF = azul, ES = marrom, VE = verde visíveis na vista. |
| CA-10-04 | **Dado** elemento desconectado (ilha), **quando** verificar, **então** alerta Médio com ElementId do elemento isolado. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Elementos com sistema atribuído | 100% |
| Sistemas sem ilhas (conectividade completa) | ≥ 95% |
| Tempo | ≤ 5 segundos |

### Dependências
- Módulos 07, 08, 09 concluídos

### Critérios de Bloqueio
- ❌ System Type não existe no modelo → **Crítico**

---

## Módulo 11 — Dimensionamento Hidráulico

### Objetivo do Teste
Verificar que cálculos de vazão, diâmetro, velocidade, perda de carga e pressão estão corretos.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-11-01 | **Dado** trecho com ΣPesos = 2.0, **quando** calcular vazão, **então** Q = 0.3 × √2.0 = 0.424 L/s (±0.01). |
| CA-11-02 | **Dado** Q = 0.424 L/s, **quando** selecionar diâmetro, **então** DN 25mm (V = Q/A = 0.424/0.000366 = 1.16 m/s ≤ 3.0). |
| CA-11-03 | **Dado** trecho DN 25mm, Q = 0.424 L/s, L = 5m, **quando** calcular perda de carga, **então** J (FWH) calculado e ΔH = J × 5 × 1.20 dentro de ±10% do valor de referência. |
| CA-11-04 | **Dado** H_geom = 5m e ΣΔH = 1.5m, **quando** verificar pressão, **então** P = 3.5 m.c.a. ≥ 3.0 → OK. |
| CA-11-05 | **Dado** trecho ES com ΣUHC = 8, **quando** dimensionar, **então** DN = 50mm (tabela: ≤6→50mm, ≤20→75mm → 8 UHC → 75mm). Correção: ΣUHC=8 > 6, portanto DN = 75mm. |
| CA-11-06 | **Dado** ramal ES DN 100mm seguido por trecho DN 75mm (diminuição), **quando** verificar, **então** erro Crítico. |
| CA-11-07 | **Dado** dimensionamento completo, **quando** verificar diâmetros no modelo, **então** todos os Pipes possuem DN atualizado conforme cálculo. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Precisão da vazão vs. cálculo manual | ±1% |
| Precisão da perda de carga vs. planilha | ±10% |
| Diâmetros conformes com tabela | 100% |
| Pressão verificada em todos os pontos | 100% |
| Tempo (30 trechos AF + 20 trechos ES) | ≤ 5 segundos |

### Dependências
- Módulos 07 e 08 (topologia definida)

### Critérios de Bloqueio
- ❌ Pressão < 3 m.c.a. → **Crítico**
- ❌ Velocidade > 3 m/s → **Crítico**
- ❌ DN diminui em ES → **Crítico**

---

## Módulo 12 — Geração de Tabelas

### Objetivo do Teste
Verificar que Schedules são criadas com dados corretos e formatação adequada.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-12-01 | **Dado** rede completa com 50m de tubulação DN25 AF e 30m DN32 AF, **quando** gerar schedule de tubulações, **então** schedule mostra: AF/DN25/50.00m e AF/DN32/30.00m. |
| CA-12-02 | **Dado** 15 tees e 8 curvas 90° no sistema AF, **quando** gerar schedule de conexões, **então** schedule mostra: Tee/15un e Curva90/8un com diâmetros. |
| CA-12-03 | **Dado** 4 Schedules criadas, **quando** verificar, **então** todas possuem cabeçalho, unidades (m, mm), totalização e agrupamento por sistema. |
| CA-12-04 | **Dado** que já existe Schedule com nome "Tubulações", **quando** criar, **então** nova Schedule é nomeada "Tubulações (2)" e log Leve registrado. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Schedules criadas | 4 |
| Dados conferem com modelo | 100% |
| Tempo | ≤ 10 segundos |

### Dependências
- Módulo 10 concluído

### Critérios de Bloqueio
- Nenhum bloqueio crítico (módulo de documentação)

---

## Módulo 13 — Geração de Pranchas

### Objetivo do Teste
Verificar que pranchas são criadas com views filtradas, schedules e numeração correta.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-13-01 | **Dado** modelo com 2 pavimentos e 2 sistemas (AF, ES), **quando** gerar pranchas, **então** ≥ 4 ViewSheets criadas (1 por pavimento × sistema). |
| CA-13-02 | **Dado** prancha de AF do Térreo, **quando** verificar view, **então** apenas elementos do PipingSystem AF são visíveis. |
| CA-13-03 | **Dado** pranchas criadas, **quando** verificar numeração, **então** sequência HID-01, HID-02, HID-03... sem lacunas. |
| CA-13-04 | **Dado** pranchas criadas, **quando** verificar, **então** cada prancha contém ao menos 1 view de planta e 1 schedule. |
| CA-13-05 | **Dado** view de planta na prancha, **quando** verificar, **então** escala = 1:50, Crop Region ativo. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Pranchas com view + schedule | 100% |
| Views com filtro correto | 100% |
| Numeração sequencial sem lacunas | 100% |
| Tempo (4 pranchas) | ≤ 20 segundos |

### Dependências
- Módulo 12 concluído. Titleblock disponível.

### Critérios de Bloqueio
- ❌ Titleblock não encontrado → **Médio** (usa genérico)

---

## Módulo 14 — Sistema de Logs e Diagnóstico

### Objetivo do Teste
Verificar que logs são registrados, acumulados, filtrados, exportados e bloqueiam pipeline quando necessário.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-14-01 | **Dado** um serviço que chama `logService.Critico("Etapa01", "RoomReader", "Nenhum Room encontrado")`, **quando** registrar, **então** entry acumulada com timestamp, nível Crítico, etapa, componente e mensagem. |
| CA-14-02 | **Dado** 100 log entries acumulados, **quando** filtrar por nível Médio, **então** apenas entries de nível Médio são retornadas. |
| CA-14-03 | **Dado** log com 1 entry Crítico, **quando** verificar bloqueio, **então** `TemBloqueio == true`. |
| CA-14-04 | **Dado** log sem entry Crítico, **quando** verificar bloqueio, **então** `TemBloqueio == false`. |
| CA-14-05 | **Dado** 50 entries acumulados, **quando** exportar JSON, **então** arquivo criado em `Data/Logs/log_{timestamp}.json` com 50 entries serializadas. |
| CA-14-06 | **Dado** log entries de 3 etapas diferentes, **quando** gerar resumo, **então** resumo contém contagem por nível e por etapa. |
| CA-14-07 | **Dado** entry com ElementId = 12345, **quando** exibir na UI, **então** clique no entry seleciona elemento 12345 no modelo Revit. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Perda de log entries | 0% |
| Tempo de registro (1 entry) | ≤ 1 ms |
| Tempo de exportação (1000 entries) | ≤ 500 ms |
| Arquivo JSON válido após exportação | 100% |

### Dependências
- Nenhuma (módulo fundamental)

### Critérios de Bloqueio
- ❌ Entry Crítico → **pipeline bloqueado**

---

## Módulo 15 — Interface do Plugin (WPF)

### Objetivo do Teste
Verificar que a interface permite controlar todas as etapas, configurar parâmetros e visualizar diagnósticos.

### Critérios de Aceitação

| ID | Critério |
|----|---------|
| CA-15-01 | **Dado** plugin carregado no Revit, **quando** clicar no botão "Detectar Ambientes" na ribbon, **então** janela WPF abre com 3 abas visíveis. |
| CA-15-02 | **Dado** aba de Configuração, **quando** alterar pressão mínima para 4.0 e salvar, **então** `hydraulic_config.json` contém `"pressao_minima_mca": 4.0`. |
| CA-15-03 | **Dado** aba de Execução, **quando** etapa 01 concluída com sucesso, **então** status visual muda para ✅ (verde) e botão da etapa 02 é habilitado. |
| CA-15-04 | **Dado** aba de Execução, **quando** etapa gera erro Crítico, **então** status muda para ❌ (vermelho) e botões de etapas seguintes ficam desabilitados. |
| CA-15-05 | **Dado** aba de Execução com alertas Médios, **quando** etapa conclui, **então** botões "Aprovar" e "Rejeitar" aparecem e próxima etapa fica desabilitada até ação do usuário. |
| CA-15-06 | **Dado** aba de Diagnóstico com 50 logs, **quando** filtrar por nível "Crítico", **então** apenas logs Críticos são exibidos no DataGrid. |
| CA-15-07 | **Dado** log com ElementId, **quando** clicar no log, **então** elemento correspondente é selecionado e enquadrado na vista ativa do Revit. |
| CA-15-08 | **Dado** aba de Configuração, **quando** inserir valor negativo em pressão, **então** campo mostra borda vermelha e botão salvar fica desabilitado. |
| CA-15-09 | **Dado** execução de etapa em andamento, **quando** etapa executa, **então** UI não bloqueia (operação via ExternalEvent) e barra de progresso atualiza. |
| CA-15-10 | **Dado** janela aberta sem modelo ativo no Revit, **quando** abrir, **então** mensagem de erro e janela fecha automaticamente. |

### Métricas de Validação

| Métrica | Valor esperado |
|---------|---------------|
| Tempo de abertura da janela | ≤ 500 ms |
| Atualização de status visual | ≤ 1 segundo após conclusão |
| UI responsiva durante execução | 100% (sem congelamento) |
| Persistência de configuração | Valores salvos = valores carregados na reabertura |

### Dependências
- Módulo 14 para diagnóstico
- Todos os módulos para execução

### Critérios de Bloqueio
- ❌ Sem modelo ativo → **janela não abre**
- ❌ ExternalEvent falha → **Crítico + mensagem**

---

## Resumo de Critérios

| Módulo | Critérios | Bloqueios |
|--------|----------|-----------|
| 01 — Detecção | 8 | 2 |
| 02 — Classificação | 9 | 1 |
| 03 — Pontos | 5 | 1 |
| 04 — Inserção | 5 | 2 |
| 05 — Validação | 4 | 1 |
| 06 — Prumadas | 5 | 2 |
| 07 — Rede AF | 7 | 3 |
| 08 — Rede ES | 6 | 3 |
| 09 — Inclinações | 5 | 2 |
| 10 — Sistemas | 4 | 1 |
| 11 — Dimensionamento | 7 | 3 |
| 12 — Tabelas | 4 | 0 |
| 13 — Pranchas | 5 | 1 |
| 14 — Logs | 7 | 1 |
| 15 — Interface | 10 | 2 |
| **TOTAL** | **96** | **25** |
