# Regras de Ventilação Sanitária — NBR 8160

> Documentação técnica completa das regras de ventilação para sistemas de esgoto sanitário predial, convertidas em lógica implementável pelo plugin.

---

## 1. Conceito de Ventilação Sanitária

### 1.1 Definição técnica

Ventilação sanitária é o sistema de tubulações que permite a **entrada e saída de ar** na rede de esgoto predial, mantendo a pressão atmosférica no interior das tubulações e protegendo os desconectores (sifões, caixas sifonadas, vasos sanitários) contra perda do fecho hídrico.

### 1.2 Função da ventilação

| Função | Mecanismo | Consequência se ausente |
|--------|-----------|------------------------|
| **Equalizar pressão** | Ar entra pela ventilação quando água desce pelo TQ, evitando pressão negativa | Sifonamento dos desconectores |
| **Dissipar pressão positiva** | Ar sai pela ventilação quando coluna de água comprime o ar abaixo | Bolhas e gorgolejo nos aparelhos |
| **Remover gases** | Gases do esgoto (metano, H₂S) escapam pelo terminal na cobertura | Gases retornam pelos desconectores para os ambientes |
| **Manter fecho hídrico** | Pressão estável = água do sifão não é aspirada nem empurrada | Perda de barreira contra gases |

### 1.3 Problemas que a ventilação evita

#### Auto-sifonamento

```
Causa: descarga do próprio aparelho gera sucção no sifão
Mecanismo: água descendo pelo ramal cria efeito de aspiração
Prevenção: ventilação permite entrada de ar atrás da coluna de água
Criticidade: ALTA — pode esvaziar sifão em uma única descarga
```

#### Sifonamento induzido

```
Causa: descarga de OUTRO aparelho no mesmo ramal gera sucção
Mecanismo: água no ramal compartilhado cria efeito pistão
Prevenção: ventilação no ramal equaliza pressão
Criticidade: ALTA — afeta aparelhos que não estão em uso
```

#### Pressão positiva (sobrepressão)

```
Causa: coluna de água no TQ comprime ar abaixo
Mecanismo: ar comprimido empurra água dos sifões para dentro dos ambientes
Prevenção: ventilação primária permite que ar escape pelo topo do TQ
Criticidade: MÉDIA — causa gorgolejo e mau cheiro antes de perda real do fecho
```

#### Retorno de gases

```
Causa: perda do fecho hídrico por qualquer mecanismo
Mecanismo: sem barreira de água, gases passam livremente
Gases: metano (CH₄), gás sulfídrico (H₂S), amônia (NH₃)
Riscos: mau cheiro, toxicidade, risco de explosão (metano)
Criticidade: CRÍTICA — risco à saúde
```

---

## 2. Tipos de Ventilação

### 2.1 Ventilação primária

| Campo | Detalhe |
|-------|---------|
| **Definição** | Prolongamento do tubo de queda acima da cobertura, aberto para a atmosfera |
| **Como funciona** | O próprio TQ serve como coluna de ventilação — ar entra/sai pelo topo |
| **Obrigatória** | **Sempre.** Todo TQ deve ter ventilação primária. |
| **Aplicação** | Residências de até 2 pavimentos com banheiros próximos ao TQ |

**Quando é suficiente (sem necessidade de ventilação secundária):**

```
Ventilação primária SOZINHA é suficiente quando TODAS as condições abaixo:
  1. Edifício ≤ 2 pavimentos
  2. Todos os aparelhos estão a ≤ distância máxima do TQ (tabela seção 4)
  3. Ramais de esgoto são curtos e diretos
  4. Não há mais de 4 aparelhos por ramal de esgoto
```

**Na prática residencial:** frequentemente a ventilação primária é suficiente para banheiros alinhados verticalmente com o TQ. Mas se algum banheiro está longe do TQ, ventilação secundária se torna necessária.

### 2.2 Ventilação secundária

| Campo | Detalhe |
|-------|---------|
| **Definição** | Sistema adicional de tubulação dedicado exclusivamente à ventilação |
| **Componentes** | Coluna de ventilação + ramais de ventilação |
| **Como funciona** | Tubulação paralela ao TQ com conexões nos ramais de esgoto |

**Quando é obrigatória:**

```
Ventilação secundária é OBRIGATÓRIA quando QUALQUER condição abaixo:
  1. Edifício > 2 pavimentos
  2. Algum aparelho está a > distância máxima do TQ (tabela seção 4)
  3. Ramal de esgoto atende > 4 aparelhos sem outro ponto de ventilação
  4. Ramal de esgoto tem comprimento > 6m sem ventilação
  5. Subcoletor > 15m sem ventilação
```

**Aplicações típicas em residencial:**

| Cenário | Necessidade |
|---------|-------------|
| Casa térrea, banheiro sobre shaft | Primária suficiente |
| Sobrado, banheiros alinhados | Primária geralmente suficiente |
| Sobrado, banheiro longe do TQ | Secundária necessária |
| 3+ pavimentos | Secundária obrigatória |

### 2.3 Colunas de ventilação

| Campo | Detalhe |
|-------|---------|
| **Definição** | Tubulação vertical paralela ao tubo de queda, dedicada exclusivamente à passagem de ar |
| **Conexão inferior** | Ligada ao subcoletor ou ao ramal de esgoto mais baixo do sistema |
| **Conexão superior** | Ligada ao TQ acima do ramal de esgoto mais alto, OU prolongada independentemente acima da cobertura |
| **Diâmetro** | ≥ 2/3 do DN do TQ correspondente |
| **Material** | PVC esgoto (mesmo da rede) |
| **Orientação** | Vertical (pode ter inclinação ≥ 1% em trechos horizontais de interligação) |

**Relação com tubos de queda:**

```
Cada TQ que necessita ventilação secundária deve ter:
  1. UMA coluna de ventilação paralela
  2. Conexão inferior ao subcoletor (próximo à base do TQ)
  3. Conexão superior ao TQ (acima do ramal mais alto)
     OU terminal independente acima da cobertura
  4. Distância máxima entre TQ e coluna de vent: ~3m (horizontal)
```

### 2.4 Ramais de ventilação (ramais ventiladores)

| Campo | Detalhe |
|-------|---------|
| **Definição** | Trecho de tubulação que conecta um ramal de esgoto (ou aparelho) à coluna de ventilação |
| **Quando usar** | Quando aparelho individual está distante do TQ ou da coluna de ventilação |
| **Orientação** | Sempre ascendente (nunca desce) |
| **Ponto de conexão no esgoto** | A montante do desconector, acima da borda do aparelho mais alto |
| **Ponto de conexão na ventilação** | Na coluna de ventilação, a ≥ 15cm acima do ramal de esgoto |
| **Diâmetro** | ≥ 1/2 do DN do ramal de descarga atendido (mínimo 40mm) |

**Limitações:**

| Limitação | Regra |
|-----------|-------|
| Comprimento máximo | Depende do DN (tabela seção 5) |
| Nunca desce | Z sempre cresce no sentido do ramal → coluna |
| Não pode servir como esgoto | Exclusivamente para passagem de ar |
| Inclinação em trechos horizontais | Mínimo 1% ascendente em direção à coluna |

---

## 3. Regras de Aplicação

### 3.1 Árvore de decisão completa

```
PARA cada tubo de queda no projeto:

  PASSO 1: Ventilação primária (OBRIGATÓRIA)
    → Prolongar TQ acima da cobertura (≥ 30cm)
    → Instalar terminal de ventilação

  PASSO 2: Verificar necessidade de ventilação secundária
    PARA cada aparelho conectado ao TQ (direta ou indiretamente):
      dist = distância horizontal do aparelho ao TQ (pelo ramal)
      dist_max = tabela_distancia_maxima[DN_ramal]
      
      SE dist > dist_max:
        → Necessário ramal de ventilação para este aparelho
        → SE não existe coluna de ventilação: criar
    
    SE edifício > 2 pavimentos:
      → Coluna de ventilação obrigatória
    
    SE ramal de esgoto > 6m sem ponto de ventilação:
      → Ramal de ventilação obrigatório no trecho

  PASSO 3: Dimensionar ventilação
    → DN coluna ≥ 2/3 × DN_TQ
    → DN ramal vent ≥ 1/2 × DN ramal desc (mín 40)
    → Verificar comprimento máximo vs DN

  PASSO 4: Posicionar terminal
    → ≥ 30cm acima da cobertura
    → ≥ 4m de janelas, portas e tomadas de ar
```

### 3.2 Quando apenas ventilação primária basta

| Condição | Valor |
|----------|-------|
| Número máximo de pavimentos | 2 |
| Distância máxima aparelho→TQ | Dentro da tabela (seção 4) |
| Ramais diretos e curtos | Sem curvas complexas |
| Nenhum aparelho em posição crítica | Todos próximos do TQ |

### 3.3 Relação com comprimento do ramal

| Comprimento do ramal de esgoto | Ação |
|-------------------------------|------|
| ≤ distância máxima (tabela) | Ventilação primária é suficiente |
| > distância máxima | Ramal de ventilação obrigatório |
| > 6m sem qualquer ventilação | Ramal de ventilação obrigatório |
| > 10m | Múltiplos pontos de ventilação podem ser necessários |

### 3.4 Relação com número de aparelhos

```
SE ramal de esgoto atende ≤ 4 aparelhos:
  Ventilação primária pode ser suficiente (verificar distância)

SE ramal de esgoto atende > 4 aparelhos:
  Ramal de ventilação obrigatório no trecho
  (múltiplas descargas simultâneas aumentam risco de sifonamento)
```

---

## 4. Distâncias e Limites

### 4.1 Distância máxima sem ventilação individual

| DN do ramal de descarga (mm) | Distância máx. ao TQ ou coluna de vent. (m) |
|------------------------------|---------------------------------------------|
| 40 | **1.00** |
| 50 | **1.20** |
| 75 | **1.80** |
| 100 | **2.40** |

**Nota:** a distância é medida pelo **percurso real da tubulação** (comprimento do ramal), não em linha reta.

### 4.2 Comprimento máximo do ramal de ventilação

| DN do ramal de ventilação (mm) | Comprimento máximo (m) |
|-------------------------------|----------------------|
| 40 | 6.0 |
| 50 | 7.6 |
| 75 | 15.2 |
| 100 | 30.0 |

Se o comprimento necessário excede o máximo, usar DN maior ou inserir ponto intermediário.

### 4.3 Distância entre o subcoletor e a ventilação

```
REGRA: O subcoletor deve ter ventilação (coluna de ventilação conectada)
a cada ~15m de comprimento.

SE comprimento_subcoletor > 15m sem ponto de ventilação:
  ALERTA_MEDIO("Subcoletor > 15m sem ventilação")
```

### 4.4 Distância do terminal de ventilação

| Parâmetro | Valor mínimo |
|-----------|-------------|
| Altura acima da cobertura | **0.30 m** (30 cm) |
| Distância horizontal de janelas | **4.00 m** |
| Distância horizontal de portas | **4.00 m** |
| Distância de tomadas de ar condicionado | **4.00 m** |
| Distância de outros terminais de ventilação | Sem restrição (podem estar próximos) |

---

## 5. Dimensionamento da Ventilação

### 5.1 Coluna de ventilação — DN mínimo

| DN do tubo de queda (mm) | DN mínimo coluna de vent. (mm) | Regra |
|--------------------------|-------------------------------|-------|
| 40 | 40 | 2/3 × 40 = 26.7 → arredondar para 40 (mín absoluto) |
| 50 | 40 | 2/3 × 50 = 33.3 → arredondar para 40 (mín absoluto) |
| 75 | 50 | 2/3 × 75 = 50 |
| 100 | 75 | 2/3 × 100 = 66.7 → arredondar para 75 (DN comercial) |
| 150 | 100 | 2/3 × 150 = 100 |

**Regra geral:**
```
DN_coluna_vent = ARREDONDAR_ACIMA_COMERCIAL(2/3 × DN_TQ)
DN_coluna_vent = MAX(DN_calculado, 40)  // mínimo absoluto 40mm
```

### 5.2 Ramal de ventilação — DN mínimo

| DN do ramal de descarga (mm) | DN mínimo ramal de vent. (mm) | Regra |
|------------------------------|-------------------------------|-------|
| 40 | 40 | 1/2 × 40 = 20 → mínimo absoluto 40 |
| 50 | 40 | 1/2 × 50 = 25 → mínimo absoluto 40 |
| 75 | 40 | 1/2 × 75 = 37.5 → mínimo absoluto 40 |
| 100 | 50 | 1/2 × 100 = 50 |
| 150 | 75 | 1/2 × 150 = 75 |

**Regra geral:**
```
DN_ramal_vent = ARREDONDAR_ACIMA_COMERCIAL(1/2 × DN_ramal_desc)
DN_ramal_vent = MAX(DN_calculado, 40)  // mínimo absoluto 40mm
```

### 5.3 DN comerciais de ventilação disponíveis

| DN (mm) | Uso |
|---------|-----|
| 40 | Ramal de ventilação mínimo |
| 50 | Coluna de ventilação para TQ DN 75 |
| 75 | Coluna de ventilação para TQ DN 100 |
| 100 | Coluna de ventilação para TQ DN 150 |

### 5.4 Tabela de dimensionamento completa

| DN TQ (mm) | ΣUHC máx no TQ | DN coluna vent (mm) | Comprimento máx coluna (m) |
|-----------|----------------|--------------------|-----------------------------|
| 50 | 10 | 40 | 10.7 |
| 75 | 30 | 50 | 19.8 |
| 100 | 240 | 75 | 98.4 |
| 150 | 960 | 100 | 304.8 |

Para residencial (ΣUHC geralmente < 100), os comprimentos máximos não são limitantes.

---

## 6. Integração com Tubos de Queda

### 6.1 Ventilação primária (prolongamento do TQ)

```
REGRAS DE PROLONGAMENTO:

1. O TQ continua com o MESMO diâmetro acima do ramal mais alto
2. Prolonga-se verticalmente até ultrapassar a cobertura
3. Terminal a ≥ 30cm acima do ponto mais alto da cobertura
4. Terminal aberto (sem tampa, cap ou tela fina)
   → Tela grossa anti-pássaro é aceitável (malha ≥ 10mm)
5. O DN do prolongamento = DN do TQ (não reduzir)
```

### 6.2 Conexão da coluna de ventilação ao TQ

```
CONEXÃO SUPERIOR:
  Local: acima do ramal de esgoto mais alto do TQ
  Altura: ≥ 15cm acima do ramal mais alto
  Tipo de fitting: tee 45° ou junção Y
  Orientação: horizontal ou ligeiramente inclinada para cima (em direção ao TQ)

CONEXÃO INFERIOR:
  Local: subcoletor, próximo à base do TQ
  Tipo de fitting: tee 45° ou junção Y
  Orientação: deve permitir drenagem de condensação para o esgoto
  Distância do TQ: ≤ 3m horizontal
```

### 6.3 Ventilação de alívio (edifícios altos)

```
OBRIGATÓRIA quando: edifício > 10 pavimentos
FREQUÊNCIA: a cada 10 pavimentos (máximo)
FUNÇÃO: reconectar coluna de ventilação ao TQ em pavimentos intermediários
DN: mesmo da coluna de ventilação

PARA RESIDENCIAL:
  → Geralmente não se aplica (máximo 3-4 pavimentos)
  → Plugin implementa como regra, mas raramente é ativada
```

### 6.4 Interações críticas

| Interação | Regra | Consequência se violada |
|-----------|-------|------------------------|
| TQ sem ventilação primária | Proibido | Sifonamento generalizado |
| Coluna de vent conectada abaixo do ramal mais baixo | Proibido | Esgoto pode entrar na ventilação |
| Coluna de vent com trecho descendente | Proibido | Condensação bloqueia fluxo de ar |
| Terminal de vent próximo a janela (< 4m) | Proibido | Gases entram no edifício |
| Terminal fechado com cap | Proibido | Ventilação não funciona |

---

## 7. Regras Convertidas para Lógica

### 7.1 Verificação de necessidade

```
REGRA VE-01: Ventilação primária obrigatória
  PARA cada TQ no projeto:
    SE TQ não possui prolongamento acima da cobertura:
      ERRO_CRITICO("TQ sem ventilação primária")

REGRA VE-02: Verificar necessidade de ventilação secundária
  PARA cada aparelho sanitário:
    dist = comprimento_ramal_ate_TQ_ou_coluna_vent(aparelho)
    dn_ramal = DN_ramal_descarga(aparelho)
    dist_max = tabela_dist_max[dn_ramal]
    
    SE dist > dist_max:
      necessita_vent = TRUE
      ALERTA_MEDIO("Aparelho {tipo} a {dist}m — necessita ventilação")

REGRA VE-03: Número de aparelhos no ramal
  PARA cada ramal de esgoto:
    SE num_aparelhos > 4 E ramal não tem ponto de ventilação:
      necessita_vent = TRUE
      ALERTA_MEDIO("Ramal com {num_aparelhos} aparelhos sem ventilação")

REGRA VE-04: Comprimento do ramal de esgoto
  PARA cada ramal de esgoto:
    SE comprimento > 6.0 E ramal não tem ponto de ventilação:
      necessita_vent = TRUE
      ALERTA_MEDIO("Ramal > 6m sem ventilação")

REGRA VE-05: Pavimentos
  SE num_pavimentos > 2:
    coluna_ventilacao_obrigatoria = TRUE
```

### 7.2 Dimensionamento

```
REGRA VE-06: DN da coluna de ventilação
  ENTRADA: DN_TQ [mm]
  PROCESSAR:
    dn_calc = TETO(2/3 × DN_TQ)
    dn_comercial = proximo_dn_comercial_acima(dn_calc, [40, 50, 75, 100])
    DN_coluna = MAX(dn_comercial, 40)
  SAÍDA: DN_coluna [mm]

REGRA VE-07: DN do ramal de ventilação
  ENTRADA: DN_ramal_descarga [mm]
  PROCESSAR:
    dn_calc = TETO(1/2 × DN_ramal_descarga)
    dn_comercial = proximo_dn_comercial_acima(dn_calc, [40, 50, 75, 100])
    DN_ramal_vent = MAX(dn_comercial, 40)
  SAÍDA: DN_ramal_vent [mm]

REGRA VE-08: Comprimento máximo do ramal de ventilação
  ENTRADA: DN_ramal_vent [mm]
  PROCESSAR:
    comp_max = tabela_comp_max[DN_ramal_vent]
    // 40→6m, 50→7.6m, 75→15.2m, 100→30m
  SAÍDA: comp_max [m]
```

### 7.3 Posicionamento

```
REGRA VE-09: Terminal de ventilação
  PARA cada terminal:
    SE cota_terminal < cota_cobertura + 0.30:
      ERRO_MEDIO("Terminal a menos de 30cm da cobertura")
    
    SE distancia_janela < 4.0:
      ERRO_MEDIO("Terminal a menos de 4m de janela/porta")

REGRA VE-10: Ramal de ventilação sempre ascendente
  PARA cada trecho do ramal de ventilação:
    SE Z_final < Z_inicial:
      ERRO_CRITICO("Ramal de ventilação descendente — proibido")

REGRA VE-11: Elevação mínima do ramal de ventilação
  ponto_conexao_esgoto = Z do ramal de esgoto onde conecta
  ponto_minimo = ponto_conexao_esgoto + 0.15
  
  SE Z_saida_vent < ponto_minimo:
    ERRO_MEDIO("Ramal de ventilação abaixo da elevação mínima (15cm acima do ramal)")

REGRA VE-12: Conexão superior da coluna ao TQ
  Z_conexao_superior >= Z_ramal_mais_alto + 0.15
  
  SE Z_conexao_superior < Z_ramal_mais_alto + 0.15:
    ERRO_MEDIO("Conexão superior da coluna de ventilação abaixo do ramal mais alto")

REGRA VE-13: Conexão inferior da coluna
  Z_conexao_inferior = Z do subcoletor ou ramal de esgoto mais baixo
  Distância horizontal TQ→coluna ≤ 3.0m
  
  SE distancia > 3.0:
    ALERTA_LEVE("Coluna de ventilação distante do TQ ({dist}m)")
```

### 7.4 Validação de integridade

```
REGRA VE-14: Verificar continuidade da ventilação
  PARA cada coluna de ventilação:
    SE coluna não conecta ao TQ (superior) E não tem terminal independente:
      ERRO_CRITICO("Coluna de ventilação sem saída para atmosfera")
    SE coluna não conecta ao esgoto (inferior):
      ERRO_CRITICO("Coluna de ventilação sem conexão inferior")

REGRA VE-15: Verificar que ventilação não transporta esgoto
  PARA cada Pipe de ventilação:
    SE Pipe possui declividade descendente em direção ao esgoto:
      OK (condensação drena naturalmente)
    SE Pipe tem ponto baixo (barriga) que acumula líquido:
      ALERTA_MEDIO("Ponto baixo na ventilação — risco de bloqueio")
```

---

## 8. Parâmetros Configuráveis

### 8.1 JSON de configuração de ventilação

```json
{
  "ventilacao": {
    "terminal_altura_minima_acima_cobertura_m": 0.30,
    "terminal_distancia_minima_janela_m": 4.00,
    "conexao_elevacao_minima_acima_ramal_m": 0.15,
    "distancia_maxima_coluna_ao_TQ_m": 3.00,
    "dn_minimo_absoluto_mm": 40,
    "fator_coluna_vs_TQ": 0.667,
    "fator_ramal_vs_descarga": 0.50,
    "max_aparelhos_sem_vent_no_ramal": 4,
    "max_comprimento_ramal_sem_vent_m": 6.0,
    "max_comprimento_subcoletor_sem_vent_m": 15.0,
    "pavimentos_para_vent_secundaria": 3,
    "pavimentos_para_vent_alivio": 10
  }
}
```

### 8.2 JSON de distâncias máximas sem ventilação

```json
{
  "distancia_maxima_sem_ventilacao": [
    { "dn_ramal_descarga_mm": 40, "distancia_max_m": 1.0 },
    { "dn_ramal_descarga_mm": 50, "distancia_max_m": 1.2 },
    { "dn_ramal_descarga_mm": 75, "distancia_max_m": 1.8 },
    { "dn_ramal_descarga_mm": 100, "distancia_max_m": 2.4 }
  ]
}
```

### 8.3 JSON de dimensionamento de ventilação

```json
{
  "dimensionamento_ventilacao": {
    "coluna_por_TQ": [
      { "dn_tq_mm": 40, "dn_coluna_mm": 40, "comp_max_m": 7.6 },
      { "dn_tq_mm": 50, "dn_coluna_mm": 40, "comp_max_m": 10.7 },
      { "dn_tq_mm": 75, "dn_coluna_mm": 50, "comp_max_m": 19.8 },
      { "dn_tq_mm": 100, "dn_coluna_mm": 75, "comp_max_m": 98.4 },
      { "dn_tq_mm": 150, "dn_coluna_mm": 100, "comp_max_m": 304.8 }
    ],
    "ramal_por_ramal_descarga": [
      { "dn_descarga_mm": 40, "dn_ramal_vent_mm": 40 },
      { "dn_descarga_mm": 50, "dn_ramal_vent_mm": 40 },
      { "dn_descarga_mm": 75, "dn_ramal_vent_mm": 40 },
      { "dn_descarga_mm": 100, "dn_ramal_vent_mm": 50 },
      { "dn_descarga_mm": 150, "dn_ramal_vent_mm": 75 }
    ],
    "comprimento_max_ramal_vent": [
      { "dn_ramal_vent_mm": 40, "comp_max_m": 6.0 },
      { "dn_ramal_vent_mm": 50, "comp_max_m": 7.6 },
      { "dn_ramal_vent_mm": 75, "comp_max_m": 15.2 },
      { "dn_ramal_vent_mm": 100, "comp_max_m": 30.0 }
    ],
    "dn_comerciais_vent_mm": [40, 50, 75, 100]
  }
}
```

---

## 9. Aplicação no Modelo BIM (Revit)

### 9.1 Inserção de ventilação primária (prolongamento do TQ)

```
PROCEDIMENTO:
  1. Identificar o TQ (Pipe vertical do PipingSystem ES)
  2. Identificar o Level mais alto que o TQ atende
  3. Identificar cota da cobertura (Level de cobertura ou parâmetro manual)
  4. Criar Pipe vertical com mesmo DN do TQ:
     - Z_inferior = Z_topo do TQ (acima do ramal mais alto)
     - Z_superior = cota_cobertura + 0.30m
  5. Conectar ao topo do TQ via fitting (tee ou redução)
  6. Atribuir ao PipingSystem "VE - Ventilação"
  7. Inserir terminal de ventilação (família especial ou cap aberto)
```

### 9.2 Inserção de coluna de ventilação

```
PROCEDIMENTO:
  1. Posicionar coluna paralela ao TQ (distância ≤ 3m)
  2. Z_inferior = Z do subcoletor (conexão inferior)
  3. Z_superior = Z_ramal_mais_alto + 0.15m (para conexão ao TQ)
     OU Z_cobertura + 0.30m (se terminal independente)
  4. DN = tabela[DN_TQ].dn_coluna_mm
  5. Criar Pipe vertical
  6. Criar ramal horizontal de interligação: coluna → TQ (na parte superior)
  7. Criar ramal horizontal de interligação: coluna → subcoletor (inferior)
  8. Atribuir ao PipingSystem "VE - Ventilação"
```

### 9.3 Inserção de ramal de ventilação

```
PROCEDIMENTO:
  1. Identificar ponto de conexão no ramal de esgoto:
     - A montante do desconector
     - Z = Z_ramal_esgoto + 0.15m (mínimo)
  2. Traçar ramal ascendente até a coluna de ventilação:
     - Trecho vertical de 0.15m (subir acima do ramal)
     - Trecho horizontal inclinado (≥ 1% ascendente) até a coluna
  3. DN = tabela[DN_ramal_descarga].dn_ramal_vent_mm
  4. Verificar comprimento ≤ comprimento máximo para o DN
  5. Conectar à coluna de ventilação
  6. Atribuir ao PipingSystem "VE - Ventilação"
```

### 9.4 Integrações do plugin

| Ator | Responsabilidade |
|------|-----------------|
| **Plugin (C#)** | Decisão de necessidade (regras VE-01 a VE-05), dimensionamento (VE-06 a VE-08), validação (VE-09 a VE-15) |
| **Dynamo** | Traçado de ramais de ventilação (`12_GerarVentilacao.dyn`), posicionamento de terminais |
| **unMEP** | Não atua (ventilação é simples demais para pathfinding) |

### 9.5 Evitar conflitos geométricos

| Conflito | Detecção | Resolução |
|----------|---------|-----------|
| Ramal de vent cruza viga | BoundingBox intersection | Desviar lateralmente antes da viga (decisão humana) |
| Terminal muito próximo de janela | Calcular distância 2D | Mover terminal horizontalmente (ramal horizontal no forro/laje) |
| Coluna de vent no meio do ambiente | Verificar posição vs. Rooms | Posicionar em shaft ou parede |
| Ventilação cruza tubulação de AF | BoundingBox intersection | Ajustar cota ou rota (alerta) |

---

## 10. Validações e Erros

### 10.1 Tabela de validações

| Código | Condição | Nível | Ação |
|--------|---------|-------|------|
| ERR-VE-01 | TQ sem ventilação primária | **Crítico** | Bloqueia pipeline |
| ERR-VE-02 | Aparelho além da distância máxima sem ventilação | **Médio** | Permite com aceite; sugere ramal de vent |
| ERR-VE-03 | Ramal de ventilação descendente | **Crítico** | Bloqueia; traçado incorreto |
| ERR-VE-04 | Terminal < 30cm acima da cobertura | **Médio** | Alerta; ajustar cota |
| ERR-VE-05 | Terminal < 4m de janela/porta | **Médio** | Alerta; mover terminal |
| ERR-VE-06 | DN coluna < 2/3 × DN TQ | **Médio** | Alerta; aumentar DN |
| ERR-VE-07 | DN ramal vent < 40mm | **Crítico** | Abaixo do mínimo absoluto |
| ERR-VE-08 | Coluna de vent sem saída para atmosfera | **Crítico** | Ventilação não funciona |
| ERR-VE-09 | Coluna de vent sem conexão inferior | **Crítico** | Ventilação não funciona |
| ERR-VE-10 | Ramal vent > comprimento máximo | **Médio** | Aumentar DN ou inserir ponto intermediário |
| ERR-VE-11 | Subcoletor > 15m sem ventilação | **Médio** | Alerta; inserir ponto de vent |
| ERR-VE-12 | Ramal de esgoto > 6m sem ventilação | **Médio** | Alerta; inserir ramal de vent |
| ERR-VE-13 | > 4 aparelhos no ramal sem ventilação | **Médio** | Alerta; inserir ramal de vent |
| ERR-VE-14 | Ponto baixo (barriga) na ventilação | **Leve** | Informativo; risco de bloqueio por condensação |

### 10.2 Critérios de bloqueio

```
BLOQUEAR SE:
  - TQ sem ventilação primária (ERR-VE-01)
  - Ramal de ventilação descendente (ERR-VE-03)
  - DN ramal vent < 40mm (ERR-VE-07)
  - Coluna sem saída para atmosfera (ERR-VE-08)
  - Coluna sem conexão inferior (ERR-VE-09)
```

### 10.3 Exceções permitidas

```
EXCEÇÃO:
  - Aparelho a dist > dist_max MAS ventilação primária é considerada suficiente
    pelo projetista (ramal curto, poucos aparelhos)
    → Aceite explícito + log registrado
  
  - Terminal < 4m de janela que é fixa (não abre)
    → Aceite explícito + log registrado
  
  - Edifício < 3 pavimentos sem coluna de ventilação secundária
    quando todos os aparelhos estão dentro da distância máxima
    → Automático (não é exceção, é regra normal)
```

---

## 11. Limitações e Interpretações

### 11.1 Pontos ambíguos da norma

| Ponto | Ambiguidade | Decisão adotada pelo plugin |
|-------|------------|----------------------------|
| Distância medida pelo ramal ou em linha reta | Norma diz "distância do desconector ao tubo de ventilação" sem especificar | Plugin mede pelo **comprimento real do ramal** (mais conservador) |
| Ventilação primária "suficiente" | Norma não define critério absoluto para quando primária basta | Plugin segue: ≤ 2 pav + distâncias dentro da tabela + ≤ 4 aparelhos por ramal |
| Terminal "aberto" | Norma diz aberto; prática usa tela anti-pássaro | Plugin não valida tela (responsabilidade de execução em obra) |
| Coluna de ventilação: conecta ao TQ ou sai independente? | Ambas opções são aceitas pela norma | Plugin prioriza conexão ao TQ (menos tubulação na cobertura) |
| Inclinação mínima do ramal de ventilação | Norma não define explicitamente | Plugin adota 1% ascendente como mínimo (boa prática) |

### 11.2 Simplificações para automação

| Simplificação | Justificativa | Impacto |
|--------------|--------------|---------|
| Tratar toda ventilação como PipingSystem separado ("VE") | Facilita filtragem e visualização | Nenhum impacto negativo |
| DN ventilação = tabela fixa (sem cálculo de Manning) | Tabelas já garantem dimensionamento adequado | Adequado para residencial |
| Posicionar coluna de vent sempre paralela ao TQ | Simplifica o traçado | Pode não ser o caminho mais curto em plantas irregulares |
| Ramal de vent: subir 15cm + horizontal até coluna | Padrão simplificado de traçado | Funciona para 90% dos casos |
| Ventilação de alívio: não implementar inicialmente | Raro em residencial (< 10 pavimentos) | Sem impacto para escopo residencial |

### 11.3 Decisões que exigem engenharia

| Decisão | Por que é humana |
|---------|-----------------|
| Posição do terminal na cobertura | Depende de layout do telhado, estética, acessibilidade |
| Rota do ramal de vent quando há obstáculo | Desviar para cima, para o lado, ou redirecionar — caso a caso |
| Aceitar distância > máxima sem ventilação | Avaliação do projetista sobre risco vs. custo da ventilação extra |
| Compartilhar coluna de ventilação entre TQs | Análise de proximidade e capacidade |
| Ventilação em subsolo (sem acesso direto à cobertura) | Rota complexa, pode exigir ventilação mecânica |
