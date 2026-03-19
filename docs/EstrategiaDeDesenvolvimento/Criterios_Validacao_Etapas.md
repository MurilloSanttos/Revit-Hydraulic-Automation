# Critérios de Validação por Etapa do Fluxo — Plugin Hidráulico Revit

> Documento de QA com critérios objetivos e mensuráveis para validação incremental de cada etapa do pipeline de automação hidráulica.

---

## 1. Estratégia de Validação

### 1.1 Abordagem geral

**Validação incremental com portão de aprovação (gate) por etapa.**

Cada etapa do fluxo funciona como um portão: o sistema executa, valida automaticamente, apresenta resultado ao usuário e aguarda aprovação antes de avançar. Nenhuma etapa subsequente executa sem que a anterior tenha sido aprovada.

```
Execução → Validação automática → Feedback visual → Validação humana → Aprovação/Rejeição → Próxima etapa
```

### 1.2 Papel do sistema vs. usuário

| Responsabilidade | Sistema | Usuário |
|-----------------|---------|---------|
| Executar lógica | ✅ | — |
| Verificar regras normativas | ✅ | — |
| Identificar erros e alertas | ✅ | — |
| Classificar severidade | ✅ | — |
| Decidir se avança com alerta Médio | — | ✅ |
| Corrigir posicionamento de equipamento | — | ✅ |
| Aceitar classificação de baixa confiança | — | ✅ |
| Resolver conflitos geométricos | — | ✅ |
| Validar conformidade final | — | ✅ |

### 1.3 Tipos de validação

| Tipo | Quem faz | Quando | Exemplo |
|------|---------|--------|---------|
| **Automática** | Sistema, sem intervenção | Sempre, ao final de cada etapa | DN nunca diminui no escoamento |
| **Assistida** | Sistema sugere, usuário confirma | Quando há ambiguidade ou alerta | Ambiente classificado com confiança 0.65 |
| **Manual** | Usuário inspeciona visualmente | Quando posicionamento ou rota requer julgamento | Equipamento inserido em local aceitável |

### 1.4 Fluxo de decisão por nível de erro

```
CRÍTICO  → Sistema bloqueia automaticamente. Usuário NÃO pode ignorar.
MÉDIO   → Sistema pausa e exibe. Usuário escolhe: Corrigir ou Aceitar com ressalva.
LEVE    → Sistema registra. Fluxo continua. Usuário pode revisar depois.
INFO    → Sistema registra. Sem pausa. Para auditoria.
```

---

## 2. Critérios por Etapa

---

### Etapa 01 — Detecção de Ambientes

#### Objetivo da validação
Confirmar que todos os Rooms válidos do modelo foram lidos, Rooms inválidos foram corretamente ignorados e Spaces foram criados ou mapeados.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-01-01 | **Dado** um modelo com N Rooms nomeados e com Location válida, **quando** executar detecção, **então** N Rooms aparecem na lista com nome, número, nível, área (m²) e ponto central. |
| VE-01-02 | **Dado** um Room sem Location (não colocado), **quando** executar detecção, **então** Room é excluído da lista e registrado no log como Leve. |
| VE-01-03 | **Dado** um Room com área = 0, **quando** executar, **então** excluído e registrado como Leve. |
| VE-01-04 | **Dado** 0 Rooms válidos no modelo, **quando** executar, **então** erro Crítico e pipeline bloqueado. |
| VE-01-05 | **Dado** Rooms sem Spaces correspondentes e usuário aprova criação, **quando** criar Spaces, **então** cada Space herda Name e Number do Room. |

#### Validações automáticas
- Contagem de Rooms lidos vs. total no modelo
- Filtragem de Rooms sem Location ou com área ≤ 0
- Verificação de correspondência Room↔Space por nome e Level
- Conversão de área ft² → m² (fator: 0.0929)

#### Validações assistidas
- Listar Rooms ignorados para revisão do usuário
- Confirmar criação de Spaces faltantes

#### Validação manual
- Usuário verifica se algum Room relevante foi incorretamente ignorado

#### Critérios de aprovação
- ✅ ≥ 1 Room válido detectado
- ✅ Todos os Rooms com Location e área > 0 estão na lista
- ✅ Spaces criados (se solicitado) correspondem aos Rooms

#### Critérios de reprovação
- ❌ 0 Rooms válidos → Crítico, bloqueio total
- ❌ Erro na Transaction de criação de Space → Crítico por elemento

#### Tipos de erro
| Erro | Nível | Mensagem |
|------|-------|----------|
| Nenhum Room no modelo | Crítico | "Modelo sem Rooms — impossível continuar" |
| Room sem Location | Leve | "Room '{nome}' (ID: {id}) ignorado — sem Location" |
| Room com área zero | Leve | "Room '{nome}' (ID: {id}) ignorado — área = 0" |
| Falha ao criar Space | Crítico | "Falha ao criar Space para Room '{nome}'" |

#### Métricas
| Métrica | Valor |
|---------|-------|
| Taxa de leitura de Rooms válidos | 100% |
| Tempo máximo (50 Rooms) | ≤ 8s |
| Tolerância de área mínima | > 0.5 m² |

---

### Etapa 02 — Classificação de Ambientes

#### Objetivo da validação
Confirmar que cada ambiente recebeu um tipo hidráulico correto com confiança mensurável.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-02-01 | **Dado** ambiente "Banheiro Social", **quando** classificar, **então** tipo = Banheiro, confiança ≥ 0.85. |
| VE-02-02 | **Dado** ambiente "BWC", **quando** classificar, **então** tipo = Banheiro, confiança ≥ 0.70. |
| VE-02-03 | **Dado** ambiente "Sala de Estar", **quando** classificar, **então** tipo = NaoHidraulico, excluído do pipeline. |
| VE-02-04 | **Dado** ambiente com confiança < 0.70, **quando** classificar, **então** marcado para validação humana. |
| VE-02-05 | **Dado** 0 ambientes hidráulicos classificados, **quando** finalizar, **então** Crítico. |

#### Validações automáticas
- Normalização de texto (lowercase, sem acentos, sem números)
- Matching exato → parcial → fuzzy
- Cálculo de confiança por estratégia usada
- Filtragem de ambientes não-hidráulicos (sala, quarto, corredor)

#### Validações assistidas
- Listar ambientes com confiança < 0.70 para reclassificação
- Sugerir tipo mais provável para ambientes ambíguos

#### Validação manual
- Usuário confirma ou corrige classificações de baixa confiança
- Usuário pode reclassificar qualquer ambiente

#### Critérios de aprovação
- ✅ ≥ 1 ambiente hidráulico classificado
- ✅ Todos os ambientes com confiança < 0.70 foram validados pelo usuário
- ✅ Taxa de acerto ≥ 90% em corpus de nomes comuns

#### Critérios de reprovação
- ❌ 0 ambientes hidráulicos → Crítico

#### Tipos de erro
| Erro | Nível | Mensagem |
|------|-------|----------|
| 0 ambientes relevantes | Crítico | "Nenhum ambiente hidráulico identificado" |
| Confiança < 0.70 | Médio | "Ambiente '{nome}' classificado como '{tipo}' (confiança: {conf}) — validar" |
| Ambiente não reconhecido | Leve | "Ambiente '{nome}' não classificado — tipo NaoIdentificado" |

#### Métricas
| Métrica | Valor |
|---------|-------|
| Taxa de acerto (corpus 50+ nomes) | ≥ 90% |
| Confiança média | ≥ 0.80 |
| Tempo (50 ambientes) | ≤ 1s |

---

### Etapa 03 — Identificação de Pontos Hidráulicos

#### Objetivo da validação
Confirmar que todos os pontos hidráulicos necessários foram mapeados e existentes foram detectados.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-03-01 | **Dado** banheiro classificado, **quando** identificar pontos, **então** lista contém vaso (AF+ES), lavatório (AF+ES), chuveiro (AF+ES), ralo (ES). |
| VE-03-02 | **Dado** equipamento MEP existente com connector, **quando** comparar, **então** marcado como "existente válido". |
| VE-03-03 | **Dado** equipamento de arquitetura sem connector, **quando** comparar, **então** marcado como "existente inválido" e ponto permanece como faltante. |
| VE-03-04 | **Dado** todos os ambientes, **quando** totalizar, **então** ΣPesos AF e ΣUHCs ES conferem com tabela normativa. |

#### Validações automáticas
- Consulta ao JSON normativo: `equipamentos.mapeamento_ambiente_aparelhos`
- Detecção de fixtures existentes por categoria e connector
- Cálculo de pesos AF e UHCs ES por ambiente e total
- Verificação de DN mínimo por aparelho

#### Validações assistidas
- Listar equipamentos existentes classificados como "inválido" para revisão
- Mostrar resumo de pontos faltantes por ambiente

#### Validação manual
- Usuário confirma se equipamentos existentes estão corretamente classificados

#### Critérios de aprovação
- ✅ Todos os ambientes com pontos mapeados (obrigatórios listados)
- ✅ Total de pontos > 0
- ✅ Pesos e UHCs conferem com tabela

#### Critérios de reprovação
- ❌ 0 pontos identificados → Crítico
- ❌ Ambiente hidráulico sem nenhum ponto → Médio

#### Métricas
| Métrica | Valor |
|---------|-------|
| Cobertura de pontos obrigatórios | 100% |
| Precisão de detecção de existentes | ≥ 90% |
| Tempo (20 ambientes) | ≤ 2s |

---

### Etapa 04 — Inserção de Equipamentos

#### Objetivo da validação
Confirmar que equipamentos faltantes foram inseridos com posição aceitável e connectors válidos.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-04-01 | **Dado** equipamento inserido, **quando** verificar, **então** possui ao menos 1 connector AF e/ou ES conforme tipo. |
| VE-04-02 | **Dado** família não encontrada no modelo, **quando** tentar inserir, **então** erro Crítico com nome da família. |
| VE-04-03 | **Dado** ambiente irregular, **quando** inserir, **então** posicionar com alerta Médio para ajuste manual. |
| VE-04-04 | **Dado** inserção concluída, **quando** gerar relatório, **então** relatório mostra inseridos, falhos e motivos. |

#### Validações automáticas
- Verificar presença de FamilySymbol antes de inserir
- Verificar connector pós-inserção
- Detectar colisão com elementos existentes (BoundingBox)
- Calcular distância de paredes para posicionamento

#### Validações assistidas
- Mostrar equipamentos inseridos com posição calculada para aprovação
- Destacar ambientes onde inserção falhou

#### Validação manual
- Usuário verifica posição visual de cada equipamento inserido
- Usuário pode mover equipamentos antes de aprovar

#### Critérios de aprovação
- ✅ ≥ 50% dos equipamentos inseridos com sucesso
- ✅ Todos os inseridos possuem connectors válidos
- ✅ Usuário aprovou posições

#### Critérios de reprovação
- ❌ 0 famílias disponíveis → Crítico
- ❌ > 50% das inserções falharam → Crítico

#### Métricas
| Métrica | Valor |
|---------|-------|
| Taxa de inserção (ambientes retangulares) | ≥ 90% |
| Taxa de inserção (ambientes irregulares) | ≥ 50% |
| Connectors válidos pós-inserção | 100% |
| Tempo (10 equipamentos) | ≤ 10s |

---

### Etapa 05 — Validação de Equipamentos Existentes

#### Objetivo da validação
Confirmar que equipamentos existentes estão classificados corretamente como Válido, Com Ressalva ou Inválido.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-05-01 | **Dado** equipamento MEP com connector correto e posição adequada, **quando** validar, **então** status = Válido. |
| VE-05-02 | **Dado** equipamento de arquitetura sem connectors MEP, **quando** validar, **então** status = Inválido. |
| VE-05-03 | **Dado** > 50% dos equipamentos Inválidos, **quando** avaliar, **então** Crítico. |

#### Validações automáticas
- Tipo de família (MEP vs. arquitetura)
- Presença e tipo de connectors (AF, ES)
- Distância à parede mais próxima

#### Métricas
| Métrica | Valor |
|---------|-------|
| Taxa de classificação correta | ≥ 95% |
| Tempo (20 equipamentos) | ≤ 5s |

---

### Etapa 06 — Criação de Prumadas

#### Objetivo da validação
Confirmar que prumadas foram criadas nas posições ótimas com diâmetros preliminares corretos.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-06-01 | **Dado** 3 banheiros alinhados verticalmente, **quando** criar prumadas, **então** 1 grupo (AF+ES+VE) no centroide. |
| VE-06-02 | **Dado** 2 clusters separados por > 5m, **quando** criar, **então** 2 grupos independentes. |
| VE-06-03 | **Dado** DN tubo de queda ES com ΣUHC pelos pavimentos, **quando** dimensionar, **então** DN conforme tabela normativa. |
| VE-06-04 | **Dado** prumada criada, **quando** verificar geometria, **então** Pipe conecta Level mais baixo ao mais alto. |

#### Validações automáticas
- Algoritmo de clustering (centroide, distância entre ambientes)
- Verificação de DN vs. tabela de UHC (tubo de queda)
- Verificação de ventilação: DN ≥ 2/3 × DN_TQ
- Verificação de Z: prumada conecta entre Levels

#### Validações assistidas
- Mostrar posição proposta das prumadas no modelo
- Sugerir ajuste se prumada colide com pilar

#### Validação manual
- Usuário verifica posição visual das prumadas
- Usuário pode ajustar posição antes de aprovar

#### Critérios de aprovação
- ✅ ≥ 1 cluster identificado
- ✅ Prumadas visíveis do Level mais baixo ao mais alto
- ✅ DNs conforme tabela

#### Critérios de reprovação
- ❌ 0 clusters → Crítico
- ❌ Prumada com Z incorreto (não conecta Levels) → Crítico

#### Métricas
| Métrica | Valor |
|---------|-------|
| Clusters corretos | ≥ 90% |
| DNs conforme tabela | 100% |
| Tempo (3 pav, 2 clusters) | ≤ 10s |

---

### Etapa 07 — Geração de Rede de Água Fria

#### Objetivo da validação
Confirmar que todos os pontos de consumo estão conectados à rede AF com fittings e registros.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-07-01 | **Dado** N pontos de consumo, **quando** gerar rede, **então** todos os N connectors estão conectados. |
| VE-07-02 | **Dado** rede completa, **quando** verificar, **então** registro de gaveta na entrada de cada ambiente hidráulico. |
| VE-07-03 | **Dado** rede completa, **quando** verificar sistema, **então** todos os Pipes no PipingSystem "AF - Água Fria". |
| VE-07-04 | **Dado** ponto desconectado, **quando** detectar, **então** Crítico com ElementId. |

#### Validações automáticas
- Connectivity check: todos os connectors de fixtures ligados a Pipes
- Presença de registro por ambiente
- Atribuição ao PipingSystem correto
- Verificação de ilhas (elementos desconectados)

#### Validações assistidas
- Destacar pontos desconectados no modelo (colorir em vermelho)
- Listar trechos sem sistema atribuído

#### Validação manual
- Usuário verifica visualmente rotas geradas
- Usuário aprova ou ajusta manualmente trechos complexos

#### Critérios de aprovação
- ✅ 100% dos pontos conectados
- ✅ Registros em todos os ambientes
- ✅ Todos os elementos no PipingSystem AF

#### Critérios de reprovação
- ❌ Qualquer ponto desconectado → Crítico
- ❌ > 20% dos trechos sem rota gerada → Crítico

#### Métricas
| Métrica | Valor |
|---------|-------|
| Pontos conectados | 100% |
| Registros por ambiente | 100% |
| Tempo (15 pontos, 3 pav) | ≤ 30s |

---

### Etapa 08 — Geração de Rede de Esgoto

#### Objetivo da validação
Confirmar que todos os aparelhos estão conectados à rede ES com dimensionamento por UHC e acessórios normativos.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-08-01 | **Dado** banheiro com ΣUHC, **quando** dimensionar ramal, **então** DN conforme tabela e ≥ DN de todos os ramais conectados. |
| VE-08-02 | **Dado** ramal do vaso, **quando** verificar, **então** DN ≥ 100mm e independente da CX sifonada. |
| VE-08-03 | **Dado** banheiro, **quando** verificar, **então** CX sifonada presente. |
| VE-08-04 | **Dado** cozinha, **quando** verificar, **então** CX de gordura presente. |
| VE-08-05 | **Dado** DN a jusante < DN a montante, **quando** verificar, **então** Crítico. |

#### Validações automáticas
- ΣUHC por trecho e DN pela tabela
- DN nunca diminui no escoamento
- CX sifonada em cada banheiro
- CX gordura em cada cozinha
- Ramal do vaso DN ≥ 100 e independente

#### Critérios de aprovação
- ✅ 100% dos aparelhos conectados
- ✅ DN nunca diminui
- ✅ Ramal do vaso ≥ 100mm
- ✅ CX sifonada em banheiros, CX gordura em cozinhas

#### Critérios de reprovação
- ❌ DN diminui no escoamento → Crítico
- ❌ Ramal do vaso < 100mm → Crítico
- ❌ Aparelho não conectado → Crítico

#### Métricas
| Métrica | Valor |
|---------|-------|
| Aparelhos conectados | 100% |
| CX sifonada em banheiros | 100% |
| CX gordura em cozinhas | 100% |
| DN conforme tabela | 100% |
| Tempo (15 aparelhos, 3 pav) | ≤ 30s |

---

### Etapa 09 — Aplicação de Inclinações

#### Objetivo da validação
Confirmar que declividade correta foi aplicada em todos os trechos horizontais de esgoto.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-09-01 | **Dado** trecho DN 50mm com L=2.0m, **quando** aplicar inclinação 2%, **então** desnível = 4cm, Z_final = Z_inicial - 0.04. |
| VE-09-02 | **Dado** trecho DN 100mm com L=3.0m, **quando** aplicar 1%, **então** desnível = 3cm. |
| VE-09-03 | **Dado** todos os trechos, **quando** verificar, **então** nenhum trecho com Z_final ≥ Z_inicial. |
| VE-09-04 | **Dado** trecho com declividade > 5%, **quando** verificar, **então** alerta Leve. |
| VE-09-05 | **Dado** fitting desconectado após ajuste, **quando** reconectar, **então** reconexão automática. Se falha: Médio. |

#### Validações automáticas
- Declividade aplicada ≥ mínima por DN
- Z_final < Z_inicial em todos os trechos
- Declividade ≤ 5% (alerta se excede)
- Reconexão de fittings nos endpoints ajustados
- Verificação de espaço na laje

#### Critérios de aprovação
- ✅ 100% dos trechos com declividade ≥ mínima
- ✅ 0 trechos contra gravidade
- ✅ ≥ 95% dos fittings reconectados

#### Critérios de reprovação
- ❌ Trecho contra gravidade → Crítico
- ❌ Trecho sem declividade → Crítico
- ❌ Taxa de falha > 30% → Crítico + rollback

#### Métricas
| Métrica | Valor |
|---------|-------|
| Trechos com declividade aplicada | 100% |
| Trechos na faixa normativa | 100% |
| Fittings reconectados | ≥ 95% |
| Tempo (20 trechos) | ≤ 10s |

---

### Etapa 10 — Criação de Sistemas MEP

#### Objetivo da validação
Confirmar que os 3 PipingSystems existem, estão populados e com conectividade validada.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-10-01 | **Dado** rede completa, **quando** criar sistemas, **então** 3 PipingSystems: AF, ES, VE. |
| VE-10-02 | **Dado** todos os Pipes e Fittings, **quando** atribuir, **então** 0 elementos sem sistema. |
| VE-10-03 | **Dado** sistemas criados, **quando** verificar cores, **então** AF=azul, ES=marrom, VE=verde. |
| VE-10-04 | **Dado** elemento desconectado (ilha), **quando** verificar, **então** Médio. |

#### Validações automáticas
- Contagem de elementos por sistema
- Verificação de elementos sem sistema
- Verificação de ilhas (grupos desconectados)

#### Critérios de aprovação
- ✅ 3 sistemas criados
- ✅ 100% dos elementos com sistema atribuído
- ✅ ≥ 95% conectividade (sem ilhas)

#### Critérios de reprovação
- ❌ PipingSystemType não existe → Crítico

#### Métricas
| Métrica | Valor |
|---------|-------|
| Elementos com sistema | 100% |
| Sistemas sem ilhas | ≥ 95% |
| Tempo | ≤ 5s |

---

### Etapa 11 — Dimensionamento Hidráulico

#### Objetivo da validação
Confirmar que cálculos de vazão, diâmetro, velocidade, perda de carga e pressão estão corretos e modelo atualizado.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-11-01 | **Dado** trecho com ΣPesos = 2.0, **quando** calcular, **então** Q = 0.3 × √2.0 = 0.424 L/s (±0.01). |
| VE-11-02 | **Dado** Q calculado, **quando** selecionar DN, **então** V ≤ 3.0 m/s no DN selecionado. |
| VE-11-03 | **Dado** caminho crítico calculado, **quando** verificar pressão, **então** P_din ≥ 3.0 m.c.a. em todos os pontos. |
| VE-11-04 | **Dado** trecho ES com ΣUHC, **quando** dimensionar, **então** DN conforme tabela normativa. |
| VE-11-05 | **Dado** dimensionamento completo, **quando** verificar modelo, **então** todos os Pipes com DN atualizado. |
| VE-11-06 | **Dado** pressão < 3.0 m.c.a., **quando** detectar, **então** Crítico com ElementId e P_din calculado. |

#### Validações automáticas
- Vazão vs. fórmula (Q = 0.3×√ΣP, ±1%)
- Velocidade V ≤ 3.0 m/s em todos os trechos
- Perda de carga FWH por trecho
- Pressão disponível em cada ponto
- DN atualizado no modelo = DN calculado
- DN ES nunca diminui

#### Validações assistidas
- Relatório de dimensionamento (tabela trecho × vazão × DN × V × J × ΔH)
- Destacar pontos com pressão < 3.0
- Sugerir opções: aumentar DN, elevar reservatório

#### Validação manual
- Usuário confere relatório vs. planilha de referência
- Usuário decide ação para pontos com pressão insuficiente

#### Critérios de aprovação
- ✅ Vazão ±1% do cálculo manual
- ✅ V ≤ 3.0 m/s em 100% dos trechos
- ✅ P_din ≥ 3.0 m.c.a. em 100% dos pontos (ou aprovação explícita)
- ✅ DNs conforme tabela normativa

#### Critérios de reprovação
- ❌ Pressão < 3.0 m.c.a. não resolvida → Crítico
- ❌ Velocidade > 3.0 m/s não resolvida → Crítico
- ❌ DN diminui no escoamento (ES) → Crítico

#### Métricas
| Métrica | Valor |
|---------|-------|
| Precisão de vazão | ±1% |
| Precisão de perda de carga | ±10% |
| DNs conformes | 100% |
| Pressão em todos os pontos | ≥ 3.0 m.c.a. |
| Tempo (30 trechos AF + 20 ES) | ≤ 5s |

---

### Etapa 12 — Geração de Tabelas

#### Objetivo da validação
Confirmar que Schedules foram criadas com dados corretos extraídos do modelo.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-12-01 | **Dado** rede com 50m DN25 AF e 30m DN32 AF, **quando** gerar schedule, **então** dados conferem exatamente. |
| VE-12-02 | **Dado** 15 tees e 8 curvas no sistema AF, **quando** gerar schedule de conexões, **então** quantidades conferem. |
| VE-12-03 | **Dado** schedule existente com mesmo nome, **quando** criar, **então** nome incrementado e log Leve. |

#### Validações automáticas
- Dados da schedule vs. dados do modelo (FilteredElementCollector)
- Presença de cabeçalho, unidades, totalização
- Agrupamento por sistema

#### Critérios de aprovação
- ✅ 4 Schedules criadas
- ✅ Dados conferem com modelo

#### Métricas
| Métrica | Valor |
|---------|-------|
| Schedules criadas | 4 |
| Dados = modelo | 100% |
| Tempo | ≤ 10s |

---

### Etapa 13 — Geração de Pranchas

#### Objetivo da validação
Confirmar que pranchas foram criadas com views filtradas, schedules posicionadas e numeração correta.

#### Critérios de validação

| ID | Critério |
|----|---------|
| VE-13-01 | **Dado** 2 pavimentos e 2 sistemas, **quando** gerar, **então** ≥ 4 ViewSheets. |
| VE-13-02 | **Dado** prancha AF Térreo, **quando** verificar view, **então** apenas elementos AF visíveis. |
| VE-13-03 | **Dado** pranchas, **quando** verificar numeração, **então** HID-01, HID-02... sem lacunas. |
| VE-13-04 | **Dado** prancha, **quando** verificar, **então** ≥ 1 view de planta + ≥ 1 schedule. |

#### Validações automáticas
- Contagem de ViewSheets vs. esperado
- Filtro aplicado na view (VG override por sistema)
- Sequência de numeração
- Presença de view + schedule em cada prancha

#### Critérios de aprovação
- ✅ Pranchas com view + schedule
- ✅ Filtros corretos
- ✅ Numeração sequencial

#### Métricas
| Métrica | Valor |
|---------|-------|
| Pranchas completas | 100% |
| Filtros corretos | 100% |
| Tempo (4 pranchas) | ≤ 20s |

---

## 3. Regras de Bloqueio Global

### 3.1 Bloqueios absolutos (sistema não pode avançar)

| Condição | Etapa afetada | Consequência |
|----------|-------------|-------------|
| 0 Rooms no modelo | E01 | Pipeline bloqueado. Nenhuma etapa executa. |
| 0 ambientes hidráulicos | E02 | Pipeline bloqueado após E02. |
| 0 pontos identificados | E03 | Pipeline bloqueado após E03. |
| 0 famílias MEP disponíveis | E04 | E04 não executa. |
| Pressão < 1.0 m.c.a. (inviável) | E11 | Crítico irreversível sem pressurizador. |
| DN diminui no escoamento | E08/E11 | Crítico. Modelo inconsistente. |
| Trecho contra gravidade | E09 | Crítico. Esgoto não funciona. |
| TQ sem ventilação primária | E06 | Crítico. Violação normativa obrigatória. |

### 3.2 Dependências entre etapas (gates)

```
E01 ✅ → E02
E02 ✅ → E03
E03 ✅ → E04, E05
E04 ✅ + E05 ✅ → E06
E06 ✅ → E07, E08
E07 ✅ + E08 ✅ → E09
E09 ✅ → E10, E11
E10 ✅ + E11 ✅ → E12
E12 ✅ → E13
```

Nenhuma etapa pode ser executada se a anterior não obteve aprovação (✅).

---

## 4. Estratégia de Feedback ao Usuário

### 4.1 Exibição de erros no Revit

| Nível | Indicador visual | Ação na UI |
|-------|-----------------|-----------|
| **Crítico** | ❌ Vermelho (#F44336) | Botão da etapa fica vermelho. Botões seguintes desabilitados. Mensagem modal. |
| **Médio** | ⚠️ Laranja (#FF9800) | Botão da etapa fica laranja. Botões "Aprovar" e "Rejeitar" aparecem. Próxima etapa desabilitada até decisão. |
| **Leve** | ℹ️ Azul (#2196F3) | Linha no log. Etapa pode avançar automaticamente. |
| **Info** | 📝 Cinza (#9E9E9E) | Apenas log. Sem indicação visual na barra de etapas. |

### 4.2 Apresentação de logs

| Componente | Localização | Conteúdo |
|-----------|------------|---------|
| **DataGrid de diagnóstico** | Aba "Diagnóstico" da janela WPF | Todas as entries com filtro por nível |
| **Resumo por etapa** | Aba "Execução", abaixo de cada botão | Contagem: X críticos, Y médios, Z leves |
| **Tooltip no botão** | Hover sobre botão da etapa | Resumo do resultado |
| **Exportação** | Botão "Exportar Log" | JSON em `Data/Logs/log_{timestamp}.json` |

### 4.3 Destaque de problemas no modelo

| Funcionalidade | Implementação |
|---------------|--------------|
| **Selecionar elemento** | Clicar no log → `uidoc.Selection.SetElementIds({elementId})` |
| **Zoom no elemento** | Clicar no log → `uidoc.ShowElements({elementId})` |
| **Colorir elemento** | Override temporário via OverrideGraphicSettings (vermelho para erro) |
| **Criar filtro de vista** | Para sistemas: VG Override filtra por PipingSystem |

---

## 5. Integração com Ferramentas

### 5.1 Validação de resultados do Dynamo

| Aspecto | Validação |
|---------|-----------|
| **Script executou?** | Verificar retorno do DynamoRevit.RunScript (success/failure) |
| **Elementos criados?** | Contar elementos do tipo esperado antes e depois da execução |
| **Connectors válidos?** | Para cada Pipe/Fitting criado: verificar HasConnectedConnector |
| **Posição correta?** | Para equipamentos: verificar XYZ dentro dos limites do Room |
| **Tempo de execução** | Se > timeout (60s padrão): abortar e registrar Crítico |

```
PROCEDIMENTO pós-Dynamo:
  1. Contar elementos criados (delta vs. antes)
  2. Verificar se delta > 0 (se não: Crítico "Dynamo não criou elementos")
  3. Para cada elemento criado:
     a. Verificar connectors
     b. Verificar posição vs. Room
     c. Verificar atribuição de sistema
  4. Registrar resultado no log
```

### 5.2 Validação de resultados do unMEP

| Aspecto | Validação |
|---------|-----------|
| **Rota gerada?** | Verificar se Pipe(s) foram criados entre pontos A e B |
| **Rota contínua?** | Verificar conectividade extremo a extremo |
| **DN correto?** | Verificar DN do Pipe vs. DN calculado pelo plugin |
| **Fittings inseridos?** | Verificar presença de curvas/tees nos pontos de mudança |
| **Colisão?** | Verificar BoundingBox intersect com estrutura |

```
PROCEDIMENTO pós-unMEP:
  1. Identificar Pipes criados no trecho delegado
  2. Verificar continuidade (connector A → ... → connector B)
  3. Se descontinuidade: Médio "trecho com falha de roteamento"
  4. Verificar DN de cada Pipe = DN requerido
  5. Verificar fittings em mudanças de direção
  6. Registrar resultado
```

### 5.3 Fallback quando ferramenta externa falha

```
SE Dynamo falha em roteamento:
  TENTAR unMEP
  SE unMEP também falha:
    MARCAR trecho como "roteamento manual"
    REGISTRAR Médio
    MOSTRAR trecho destacado na vista para o usuário resolver

SE Dynamo falha em inserção:
  TENTAR inserção direta via Revit API
  SE API falha:
    REGISTRAR Crítico com ElementId e motivo
```

---

## Resumo Quantitativo

| Métrica | Total |
|---------|-------|
| Etapas com critérios | 13 |
| Critérios de validação (DADO/QUANDO/ENTÃO) | 52 |
| Regras de bloqueio global | 8 |
| Validações automáticas detalhadas | 48 |
| Erros tipados (com nível e mensagem) | 34 |
| Métricas mensuráveis | 32 |
