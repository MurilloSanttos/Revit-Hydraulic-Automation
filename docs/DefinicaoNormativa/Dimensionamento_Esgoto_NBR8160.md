# Regras de Dimensionamento de Esgoto Sanitário — NBR 8160

> Extração e conversão das regras normativas de dimensionamento de sistemas de esgoto sanitário em lógica técnica implementável por software.

---

## 1. Visão Geral do Sistema de Esgoto

### 1.1 Conceitos principais

| Conceito | Definição aplicável |
|----------|-------------------|
| **Sistema separador absoluto** | Esgoto sanitário e águas pluviais são coletados por redes totalmente independentes. Obrigatório no Brasil. |
| **Escoamento gravitacional** | Todo esgoto sanitário predial escoa por gravidade (sem bombeamento em condições normais). |
| **Desconector** | Dispositivo hidráulico que impede a passagem de gases do esgoto para o ambiente (fecho hídrico). Exemplos: caixa sifonada, sifão, vaso sanitário. |
| **Fecho hídrico** | Camada de água retida no desconector que funciona como barreira contra gases. Mínimo 50mm. |
| **Unidade Hunter de Contribuição (UHC)** | Fator probabilístico que expressa a frequência de uso e a vazão de descarga de cada aparelho sanitário. |
| **Ventilação** | Sistema de tubulação que permite entrada de ar na rede, equalizando pressão e impedindo sifonamento dos desconectores. |

### 1.2 Componentes do sistema (hierarquia de escoamento)

```
Aparelho Sanitário
    │
    ▼
Ramal de Descarga (individual por aparelho)
    │
    ▼
Ramal de Esgoto (coleta vários ramais de descarga no pavimento)
    │
    ▼
Tubo de Queda (coluna vertical, recebe ramais de todos os pavimentos)
    │
    ▼
Subcoletor (trecho horizontal enterrado, conecta TQ à CI)
    │
    ▼
Caixa de Inspeção (acesso para manutenção)
    │
    ▼
Coletor Predial (trecho final até a rede pública)
```

### 1.3 Definição de cada componente

| Componente | Definição | Orientação |
|-----------|-----------|-----------|
| **Ramal de descarga** | Trecho entre a saída do aparelho (ou desconector) e o ramal de esgoto | Horizontal com declividade |
| **Ramal de esgoto** | Trecho que recebe ramais de descarga do mesmo pavimento e conduz ao tubo de queda | Horizontal com declividade |
| **Tubo de queda (TQ)** | Tubulação vertical que recebe os ramais de esgoto de todos os pavimentos | Vertical |
| **Subcoletor** | Tubulação horizontal que recebe o TQ e conduz à caixa de inspeção | Horizontal com declividade |
| **Coletor predial** | Tubulação entre a última CI interna e o ponto de conexão com a rede pública | Horizontal com declividade |
| **Coluna de ventilação** | Tubulação vertical paralela ao TQ para equalização de pressão | Vertical |

### 1.4 Lógica geral de dimensionamento

O dimensionamento de esgoto é baseado em **Unidades Hunter de Contribuição (UHC)**, não em vazão direta.

```
Para cada trecho:
  1. Listar aparelhos atendidos
  2. Somar UHCs dos aparelhos
  3. Consultar tabela ΣUHC → DN mínimo
  4. Verificar diâmetro mínimo absoluto por tipo de componente
  5. Verificar que diâmetro nunca diminui no sentido do escoamento
  6. Aplicar declividade conforme DN
  7. Verificar necessidade de ventilação
```

---

## 2. Unidades de Contribuição (UHC)

### 2.1 Tabela de UHC por aparelho sanitário

| Aparelho | UHC | DN mínimo ramal de descarga (mm) |
|----------|-----|--------------------------------|
| Vaso sanitário — caixa acoplada | **6** | **100** |
| Vaso sanitário — válvula de descarga | **6** | **100** |
| Lavatório | 1 | 40 |
| Bidê | 1 | 40 |
| Chuveiro | 2 | 40 |
| Banheira | 2 | 40 |
| Pia de cozinha | 3 | 50 |
| Pia de cozinha (com triturador) | 3 | 75 |
| Tanque de lavar | 3 | 40 |
| Máquina de lavar roupa | 3 | 50 |
| Máquina de lavar louça | 2 | 50 |
| Mictório com válvula | 6 | 75 |
| Mictório sem válvula | 2 | 40 |
| Ralo sifonado DN 100 | 1 | 40 |
| Ralo sifonado DN 75 | 1 | 40 |
| Ralo seco | 1 | 40 |
| Caixa sifonada 150×150 | 2 | 50 |
| Caixa sifonada 100×100 | 1 | 40 |

**Notas para o plugin:**
- Vaso sanitário: sempre 6 UHC e DN mínimo 100mm, independente do tipo (caixa ou válvula)
- O DN mínimo do ramal de descarga é **obrigatório** — não pode ser menor que o tabelado
- Caixa sifonada não é um aparelho, mas contribui com UHC quando recebe mais de 1 ramal

### 2.2 Regras de soma de UHC

```
REGRA UHC-01: Soma acumulativa
  Para cada trecho: ΣUHC = soma de todos os UHCs dos aparelhos 
  a jusante (no sentido do escoamento)

REGRA UHC-02: Soma entre pavimentos (tubo de queda)
  ΣUHC_TQ = soma de todos os UHCs de todos os pavimentos 
  que descarregam no TQ

REGRA UHC-03: Vaso sanitário sempre separado
  O ramal de descarga do vaso NÃO passa pela caixa sifonada.
  Conecta diretamente ao ramal de esgoto (DN 100mm independente).

REGRA UHC-04: Aparelhos na caixa sifonada
  Lavatório, bidê, chuveiro → descarregam na caixa sifonada
  Caixa sifonada → conecta ao ramal de esgoto
  UHC da CX = soma dos UHCs dos aparelhos que descarregam nela
```

---

## 3. Dimensionamento por Trecho

### 3.1 Ramal de descarga

| Regra | Descrição |
|-------|-----------|
| DN mínimo | Conforme tabela 2.1 por aparelho (obrigatório) |
| Comprimento máximo | Sem limite explícito, mas ramal longo exige ventilação |
| Declividade | Obrigatória conforme seção 4 |
| Material | PVC esgoto (branco), DN normalizado |

**Diâmetros obrigatórios por aparelho (não negociáveis):**

| Aparelho | DN mínimo (mm) | Justificativa |
|----------|---------------|---------------|
| Vaso sanitário | **100** | Volume de descarga + sólidos |
| Pia de cozinha | **50** | Gordura + resíduos sólidos |
| Máquina de lavar | **50** | Vazão elevada |
| Lavatório | 40 | Vazão baixa |
| Chuveiro | 40 | Vazão baixa |
| Bidê | 40 | Vazão baixa |
| Tanque | 40 | Vazão moderada |
| Ralo sifonado | 40 | Coleta de piso |

```
REGRA RD-01: DN do ramal de descarga
  DN_ramal = MAX(DN_minimo_aparelho, DN_calculado_por_UHC)
  
  Na prática residencial:
    Vaso → sempre DN 100
    Pia cozinha → sempre DN 50
    Demais → DN 40 (maioria dos casos)
```

### 3.2 Ramal de esgoto

O ramal de esgoto recebe os ramais de descarga de um ou mais aparelhos do mesmo pavimento e conduz ao tubo de queda.

#### Tabela de dimensionamento: ΣUHC → DN ramal de esgoto

| ΣUHC máximo | DN mínimo (mm) | Declividade mínima |
|-------------|---------------|-------------------|
| 3 | 40 | 2% |
| 6 | 50 | 2% |
| 10 | 50 | 2% |
| 20 | 75 | 2% |
| 35 | 75 | 2% |
| 100 | 100 | 1% |
| 160 | 100 | 1% |
| 350 | 150 | 1% |
| 620 | 150 | 1% |

```
REGRA RE-01: DN do ramal de esgoto
  ENTRADA: ΣUHC dos aparelhos no ramal
  PROCESSAR:
    Percorrer tabela de ΣUHC → DN
    Selecionar menor DN onde ΣUHC_max ≥ ΣUHC_ramal
  SAÍDA: DN do ramal

REGRA RE-02: DN mínimo absoluto do ramal de esgoto
  SE ramal recebe vaso sanitário (direta ou indiretamente):
    DN_ramal ≥ 100mm (obrigatório)
    
REGRA RE-03: DN nunca diminui
  SE DN_trecho_jusante < DN_trecho_montante:
    ERRO_CRITICO("DN diminui no sentido do escoamento")
```

#### Exemplo prático — banheiro completo

| Aparelho | UHC | Ramal individual |
|----------|-----|-----------------|
| Lavatório | 1 | → CX sifonada |
| Chuveiro | 2 | → CX sifonada |
| Ralo sifonado | 1 | → CX sifonada |
| CX sifonada | (4) | → Ramal de esgoto |
| Vaso sanitário | 6 | → Ramal de esgoto (independente) |

```
ΣUHC no ramal de esgoto do banheiro = 4 (CX) + 6 (vaso) = 10
DN pela tabela: ΣUHC 10 → DN 50mm
MAS vaso exige DN ≥ 100mm
PORTANTO: DN_ramal = 100mm
```

### 3.3 Tubo de queda

O tubo de queda recebe ramais de esgoto de todos os pavimentos e transporta o efluente verticalmente.

#### Tabela de dimensionamento de tubos de queda

| ΣUHC máximo (todos os pav.) | DN mínimo TQ (mm) |
|-----------------------------|-------------------|
| 2 | 40 |
| 4 | 50 |
| 10 | 50 |
| 30 | 75 |
| 70 | 75 |
| 240 | 100 |
| 500 | 100 |
| 960 | 150 |
| 2200 | 150 |

```
REGRA TQ-01: DN do tubo de queda
  ΣUHC_TQ = soma de UHCs de TODOS os pavimentos que descarregam no TQ
  DN_TQ = consulta_tabela(ΣUHC_TQ)

REGRA TQ-02: DN mínimo obrigatório
  SE algum ramal de esgoto que descarrega no TQ tem DN ≥ 100mm:
    DN_TQ ≥ 100mm (obrigatório)
  NOTA: como vaso exige DN 100, todo TQ que recebe vaso: DN ≥ 100

REGRA TQ-03: DN do TQ ≥ maior DN dos ramais conectados
  DN_TQ = MAX(DN_tabela, MAX(DN_ramais_conectados))
```

#### Exemplo — residência 2 pavimentos

```
Térreo: 1 banheiro (ΣUHC = 10) + 1 cozinha (ΣUHC = 3) = 13
Superior: 2 banheiros (ΣUHC = 10 + 10) = 20

ΣUHC_TQ = 13 + 20 = 33
DN pela tabela: 33 > 30, ≤ 70 → DN 75mm
MAS vaso exige ramal DN 100 → TQ ≥ 100mm
PORTANTO: DN_TQ = 100mm
```

### 3.4 Subcoletores e coletores

Subcoletores são trechos horizontais enterrados que recebem tubos de queda e conduzem às caixas de inspeção.

#### Tabela de dimensionamento de subcoletores

| ΣUHC máximo | DN mínimo (mm) | Declividade mínima |
|-------------|---------------|-------------------|
| 21 | 100 | 1% |
| 60 | 100 | 1% |
| 180 | 150 | 0.65% |
| 700 | 200 | 0.50% |

```
REGRA SC-01: DN do subcoletor
  ΣUHC_SC = soma de UHCs de todos os TQs que descarregam no subcoletor
  DN_SC = consulta_tabela_subcoletor(ΣUHC_SC)

REGRA SC-02: DN mínimo absoluto
  DN_SC ≥ 100mm SEMPRE (mesmo que ΣUHC seja baixo)

REGRA SC-03: DN_SC ≥ DN_TQ
  DN do subcoletor NUNCA menor que DN do tubo de queda conectado

REGRA SC-04: Coletor predial
  DN_coletor ≥ DN_subcoletor
  DN_coletor ≥ 100mm
  Declividade: mesma regra do subcoletor
```

---

## 4. Declividades

### 4.1 Regra geral

| Diâmetro nominal | Declividade mínima | Declividade máxima recomendada |
|-----------------|-------------------|-------------------------------|
| DN ≤ 75 mm | **2% (2 cm/m)** | 5% |
| DN 100 mm | **1% (1 cm/m)** | 5% |
| DN 150 mm | **0.65% (0.65 cm/m)** | 5% |
| DN 200 mm | **0.50% (0.50 cm/m)** | 5% |

### 4.2 Lógica de aplicação

```
REGRA DEC-01: Atribuição de declividade
  SE DN <= 75:
    decliv_min = 0.02
  SE DN == 100:
    decliv_min = 0.01
  SE DN == 150:
    decliv_min = 0.0065
  SE DN >= 200:
    decliv_min = 0.005

REGRA DEC-02: Cálculo de desnível
  desnivel = comprimento_trecho × declividade
  Z_final = Z_inicial - desnivel
  (sempre desce no sentido do escoamento)

REGRA DEC-03: Declividade máxima
  SE declividade > 0.05 (5%):
    ALERTA_LEVE("Declividade acima de 5% - risco de escoamento 
    turbulento e separação sólido-líquido")

REGRA DEC-04: Declividade zero ou negativa
  SE Z_final >= Z_inicial em trecho horizontal:
    ERRO_CRITICO("Trecho sem declividade ou contra gravidade")
```

### 4.3 Casos especiais

| Caso | Regra |
|------|-------|
| Trechos verticais (TQ) | Sem declividade — escoamento vertical livre |
| Subcoletor DN 150 | Declividade mínima 0.65% (diferente dos demais) |
| Subcoletor DN 200 | Declividade mínima 0.50% |
| Ramal de descarga curto (< 1m) | Manter declividade mínima mesmo assim |
| Trecho sob laje estreita (< 15cm) | Pode não haver espaço → alerta humano |

### 4.4 Cálculo de espaço necessário

```
REGRA DEC-05: Verificação de espaço entre lajes
  espaco_necessario = comprimento × declividade + DN_externo / 1000
  
  SE espaco_necessario > espaco_entre_lajes:
    ALERTA_MEDIO("Espaço insuficiente para inclinação do trecho")
    opcoes:
      - Reduzir comprimento (dividir trecho)
      - Alterar rota
      - Decisão humana
```

---

## 5. Ventilação Sanitária

### 5.1 Conceito e função

A ventilação tem 3 funções:
1. **Impedir sifonamento** dos desconectores (perda do fecho hídrico)
2. **Impedir retorno de gases** do esgoto para os ambientes
3. **Permitir escoamento adequado** pela entrada de ar atrás da coluna de água

### 5.2 Tipos de ventilação

| Tipo | Descrição | Quando usar |
|------|-----------|-------------|
| **Ventilação primária** | Prolongamento do tubo de queda acima da cobertura | **Sempre obrigatória** para cada TQ |
| **Coluna de ventilação (secundária)** | Tubulação vertical paralela ao TQ, conectada em cima e embaixo | Quando altura do TQ > 2 pavimentos ou ramais longos |
| **Ramal de ventilação** | Tubulação que conecta um aparelho ou ramal à coluna de ventilação | Quando aparelho está distante do TQ |
| **Ventilação de alívio** | Conexão intermediária entre coluna de ventilação e TQ a cada 10 pavimentos | Edifícios altos (> 10 pav.) |
| **Terminal de ventilação** | Extremidade aberta da ventilação acima da cobertura | Em toda saída de ventilação |

### 5.3 Regras obrigatórias de ventilação

```
REGRA VE-01: Ventilação primária (obrigatória)
  TODO tubo de queda DEVE ter ventilação primária:
    TQ é prolongado acima da cobertura
    Terminal a ≥ 0.30m acima do telhado
    Terminal a ≥ 4.0m de janelas, portas ou entradas de ar
    Terminal NUNCA fechado (aberto para atmosfera)

REGRA VE-02: Coluna de ventilação
  OBRIGATÓRIA quando:
    - TQ atende ≥ 3 pavimentos
    - OU qualquer ramal de esgoto > distância máxima sem ventilação
  CONEXÃO:
    - Inferior: ligada ao subcoletor ou ramal de esgoto mais baixo
    - Superior: ligada ao TQ acima do mais alto ramal de esgoto
      OU prolongada independentemente acima da cobertura

REGRA VE-03: Ramal de ventilação
  OBRIGATÓRIO quando:
    distância do aparelho (desconector) ao TQ > distância_máxima
  POSIÇÃO:
    Conectado a montante do desconector
    Deve subir verticalmente antes de ir na horizontal
    Nunca desce no sentido da coluna de ventilação
```

### 5.4 Distâncias máximas sem ventilação individual

| DN do ramal de descarga (mm) | Distância máx. ao TQ sem ventilação (m) |
|------------------------------|----------------------------------------|
| 40 | 1.00 |
| 50 | 1.20 |
| 75 | 1.80 |
| 100 | 2.40 |

```
REGRA VE-04: Necessidade de ventilação individual
  PARA cada aparelho:
    dist = distancia_horizontal_ao_TQ_ou_coluna_ventilacao
    dist_max = tabela_distancia_maxima[DN_ramal]
    
    SE dist > dist_max:
      NECESSITA ventilação individual
      ALERTA_MEDIO("Aparelho {tipo} a {dist}m do TQ - necessita ramal de ventilação")
```

### 5.5 Dimensionamento da coluna de ventilação

| DN do tubo de queda (mm) | DN mínimo ventilação (mm) | Comprimento máx. ventilação (m) |
|--------------------------|--------------------------|-------------------------------|
| 40 | 40 | 7.6 |
| 50 | 40 | 10.7 |
| 75 | 50 | 19.8 |
| 100 | 50 | 30.5 |
| 100 | 75 | 98.4 |
| 150 | 75 | 61.0 |
| 150 | 100 | 304.8 |

**Regra simplificada para residencial:**

```
REGRA VE-05: DN da coluna de ventilação (simplificado)
  DN_vent ≥ (2/3) × DN_TQ
  
  Mínimo absoluto: DN 40mm
  
  Tabela simplificada:
    TQ DN 50  → Ventilação ≥ DN 40
    TQ DN 75  → Ventilação ≥ DN 50
    TQ DN 100 → Ventilação ≥ DN 75
    TQ DN 150 → Ventilação ≥ DN 100
```

### 5.6 Dimensionamento do ramal de ventilação

```
REGRA VE-06: DN do ramal de ventilação
  DN_ramal_vent ≥ (1/2) × DN_ramal_descarga_atendido
  Mínimo absoluto: DN 40mm
  
  Prática:
    Ramal descarga DN 40 → Vent DN 40
    Ramal descarga DN 50 → Vent DN 40
    Ramal descarga DN 75 → Vent DN 40
    Ramal descarga DN 100 → Vent DN 50

REGRA VE-07: Ramal de ventilação sempre sobe
  Z de cada ponto do ramal de ventilação deve ser ≥ Z do ponto anterior
  (no sentido aparelho → coluna de ventilação)
  SE qualquer trecho desce:
    ERRO_CRITICO("Ramal de ventilação não pode descer")

REGRA VE-08: Elevação mínima do ramal de ventilação
  O ramal de ventilação deve conectar ao ramal de esgoto a uma 
  elevação ≥ borda superior do aparelho mais alto do pavimento
  Simplificação: conectar a ≥ 0.15m acima do ramal de esgoto
```

---

## 6. Condições de Funcionamento

### 6.1 Proteção contra sifonamento

| Condição | Causa | Prevenção |
|----------|-------|-----------|
| **Auto-sifonamento** | Descarga do próprio aparelho sifona o desconector | Manter fecho hídrico ≥ 50mm; ventilação adequada |
| **Sifonamento induzido** | Descarga de outro aparelho no mesmo ramal causa sucção | Ventilação no ramal; limitar comprimento sem ventilação |
| **Pressão positiva** | Coluna de água no TQ comprime ar, empurrando água dos desconectores | Ventilação primária obrigatória |

```
REGRA FH-01: Fecho hídrico mínimo
  Todos os desconectores devem manter fecho hídrico ≥ 50mm
  (garantido pelo projeto correto de ventilação)

REGRA FH-02: Caixa sifonada obrigatória
  Todo banheiro DEVE ter caixa sifonada para:
    - Coletar água do piso
    - Servir como desconector
    - Receber ramais de lavatório, chuveiro, bidê e ralo

REGRA FH-03: Caixa de gordura
  Obrigatória na saída da pia de cozinha
  Impede acúmulo de gordura na rede
  Posicionar antes da conexão com o ramal de esgoto
```

### 6.2 Regras de traçado para escoamento adequado

```
REGRA TR-01: Evitar curvas de 90°
  Em trechos horizontais de esgoto, EVITAR curvas de 90°
  Usar 2 curvas de 45° (90° em duas etapas)
  Exceção: ramal de descarga curto (< 1m) aceita 90° com ressalva

REGRA TR-02: Mudança de direção com caixa de inspeção
  Em mudança de direção > 90° em trechos horizontais:
    Inserir caixa de inspeção no ponto de mudança

REGRA TR-03: Distância máxima entre CIs
  Máximo ~15m entre caixas de inspeção consecutivas em subcoletores
  
REGRA TR-04: Junção de ramais
  Ramais de descarga devem conectar ao ramal de esgoto com junção (tee 45° ou junção Y)
  Nunca usar tee 90° na horizontal

REGRA TR-05: Vaso sanitário — ramal independente
  O ramal do vaso NUNCA passa pela caixa sifonada
  Conecta diretamente ao ramal de esgoto
  Justificativa: volume de descarga pode sifonar a CX
```

### 6.3 Verificações automáticas de funcionamento

```
REGRA FN-01: Todos os aparelhos com desconector
  PARA cada aparelho sanitário:
    SE tipo == vaso: desconector é integrado (OK)
    SE tipo == pia/lav/tanque: verificar presença de sifão (no conector)
    SE tipo == chuveiro/ralo: verificar presença de CX sifonada no caminho

REGRA FN-02: Rede contínua até caixa de inspeção
  Todos os ramais de esgoto devem convergir para o tubo de queda
  Todos os subcoletores devem convergir para a caixa de inspeção
  Nenhum trecho pode terminar em "ponto morto" (sem saída)

REGRA FN-03: Compatibilidade de conexões
  DN_montante ≤ DN_jusante SEMPRE
  Tipo de fitting compatível com orientação (horizontal ↔ vertical)
```

---

## 7. Regras Convertidas para Lógica

### 7.1 Dimensionamento

```
REGRA SW-ES-01: Dimensionar ramal de descarga
  ENTRADA: tipo_aparelho
  PROCESSAR:
    DN = tabela_dn_minimo_aparelho[tipo_aparelho]
  SAÍDA: DN [mm]

REGRA SW-ES-02: Dimensionar ramal de esgoto
  ENTRADA: lista de aparelhos no ramal
  PROCESSAR:
    soma_uhc = SOMA(uhc de cada aparelho)
    DN_tabela = consulta_tabela_ramal(soma_uhc)
    DN_minimo = MAX(DN dos ramais de descarga conectados)
    DN_final = MAX(DN_tabela, DN_minimo)
  SAÍDA: DN_final [mm]

REGRA SW-ES-03: Dimensionar tubo de queda
  ENTRADA: lista de ramais de esgoto de todos os pavimentos
  PROCESSAR:
    soma_uhc_total = SOMA(uhc de todos os aparelhos de todos os pav.)
    DN_tabela = consulta_tabela_TQ(soma_uhc_total)
    DN_maxramal = MAX(DN de todos os ramais conectados)
    DN_final = MAX(DN_tabela, DN_maxramal)
  SAÍDA: DN_final [mm]

REGRA SW-ES-04: Dimensionar subcoletor
  ENTRADA: lista de TQs que descarregam no subcoletor
  PROCESSAR:
    soma_uhc_total = SOMA(uhc de todos os TQs)
    DN_tabela = consulta_tabela_subcoletor(soma_uhc_total)
    DN_maxTQ = MAX(DN de todos os TQs conectados)
    DN_final = MAX(DN_tabela, DN_maxTQ, 100)  // mínimo absoluto 100
  SAÍDA: DN_final [mm]
```

### 7.2 Declividade

```
REGRA SW-ES-05: Atribuir declividade
  ENTRADA: DN do trecho
  PROCESSAR:
    SE DN <= 75: decliv = 0.02
    SE DN == 100: decliv = 0.01
    SE DN == 150: decliv = 0.0065
    SE DN >= 200: decliv = 0.005
  SAÍDA: declividade [adimensional]

REGRA SW-ES-06: Calcular desnível
  ENTRADA: comprimento, declividade
  PROCESSAR:
    desnivel = comprimento × declividade
    Z_final = Z_inicial - desnivel
  SAÍDA: Z_final [m]

REGRA SW-ES-07: Validar declividade aplicada
  ENTRADA: Z_inicial, Z_final, comprimento, DN
  PROCESSAR:
    decliv_aplicada = (Z_inicial - Z_final) / comprimento
    decliv_min = regra_SW-ES-05(DN)
    SE decliv_aplicada < decliv_min: ERRO_CRITICO
    SE decliv_aplicada > 0.05: ALERTA_LEVE
    SE Z_final >= Z_inicial: ERRO_CRITICO("contra gravidade")
  SAÍDA: status
```

### 7.3 Ventilação

```
REGRA SW-ES-08: Verificar ventilação primária
  PARA cada TQ:
    SE TQ não possui prolongamento acima da cobertura:
      ERRO_CRITICO("TQ sem ventilação primária")

REGRA SW-ES-09: Verificar necessidade de ventilação individual
  PARA cada aparelho:
    dist = distância horizontal ao TQ ou coluna de ventilação
    dist_max = tabela_distancia_max[DN_ramal]
    SE dist > dist_max:
      ALERTA_MEDIO("Necessita ramal de ventilação")
      SUGERIR inserção de ramal de ventilação

REGRA SW-ES-10: Dimensionar coluna de ventilação
  ENTRADA: DN do TQ
  PROCESSAR:
    DN_vent = MAX(ARREDONDAR_ACIMA(2/3 × DN_TQ), 40)
    // Arredondar para DN comercial acima
    Tabela direta:
      TQ 50 → Vent 40
      TQ 75 → Vent 50
      TQ 100 → Vent 75
      TQ 150 → Vent 100
  SAÍDA: DN_vent [mm]

REGRA SW-ES-11: Validar traçado da ventilação
  PARA cada trecho do ramal de ventilação:
    SE Z_ponto_final < Z_ponto_inicial:
      ERRO_CRITICO("Ventilação descendo — não permitido")
```

### 7.4 Validações de integridade

```
REGRA SW-ES-12: Diâmetro nunca diminui
  PARA cada par de trechos consecutivos (montante→jusante):
    SE DN_jusante < DN_montante:
      ERRO_CRITICO("DN diminui de {DN_montante} para {DN_jusante}")

REGRA SW-ES-13: Vaso — ramal independente
  PARA cada vaso sanitário:
    caminho = traçar ramal do vaso até o ramal de esgoto
    SE caminho passa por caixa sifonada:
      ERRO_MEDIO("Ramal do vaso passando por CX sifonada — corrigir topologia")

REGRA SW-ES-14: CX sifonada em banheiro
  PARA cada banheiro:
    SE não existe CX sifonada no ambiente:
      ALERTA_MEDIO("Banheiro sem caixa sifonada")

REGRA SW-ES-15: CX gordura em cozinha
  PARA cada cozinha:
    SE não existe CX de gordura na saída da pia:
      ALERTA_MEDIO("Cozinha sem caixa de gordura")

REGRA SW-ES-16: Fitting de 90° em horizontal
  PARA cada fitting em trecho horizontal de esgoto:
    SE angulo == 90:
      ALERTA_LEVE("Curva de 90° em trecho horizontal — usar 2×45°")
```

---

## 8. Parâmetros Configuráveis

### 8.1 JSON de parâmetros de esgoto

```json
{
  "dimensionamento_es": {
    "sistema": "separador_absoluto",
    "material": "PVC_esgoto",
    "dn_minimo_subcoletor_mm": 100,
    "dn_minimo_ramal_vaso_mm": 100,
    "declividade_maxima": 0.05,
    "distancia_maxima_entre_CI_m": 15,
    "fecho_hidrico_minimo_mm": 50
  },
  "declividades_minimas": {
    "DN_40": 0.02,
    "DN_50": 0.02,
    "DN_75": 0.02,
    "DN_100": 0.01,
    "DN_150": 0.0065,
    "DN_200": 0.005
  }
}
```

### 8.2 JSON de UHC por aparelho

```json
{
  "uhc_aparelhos": [
    { "tipo": "vaso_caixa_acoplada", "uhc": 6, "dn_min_ramal_mm": 100 },
    { "tipo": "vaso_valvula_descarga", "uhc": 6, "dn_min_ramal_mm": 100 },
    { "tipo": "lavatorio", "uhc": 1, "dn_min_ramal_mm": 40 },
    { "tipo": "bide", "uhc": 1, "dn_min_ramal_mm": 40 },
    { "tipo": "chuveiro", "uhc": 2, "dn_min_ramal_mm": 40 },
    { "tipo": "banheira", "uhc": 2, "dn_min_ramal_mm": 40 },
    { "tipo": "pia_cozinha", "uhc": 3, "dn_min_ramal_mm": 50 },
    { "tipo": "pia_cozinha_triturador", "uhc": 3, "dn_min_ramal_mm": 75 },
    { "tipo": "tanque", "uhc": 3, "dn_min_ramal_mm": 40 },
    { "tipo": "maquina_lavar_roupa", "uhc": 3, "dn_min_ramal_mm": 50 },
    { "tipo": "maquina_lavar_louca", "uhc": 2, "dn_min_ramal_mm": 50 },
    { "tipo": "ralo_sifonado", "uhc": 1, "dn_min_ramal_mm": 40 },
    { "tipo": "caixa_sifonada_150", "uhc": 2, "dn_min_ramal_mm": 50 },
    { "tipo": "caixa_sifonada_100", "uhc": 1, "dn_min_ramal_mm": 40 }
  ]
}
```

### 8.3 JSON de tabelas de dimensionamento

```json
{
  "tabela_ramal_esgoto": [
    { "uhc_max": 3, "dn_mm": 40 },
    { "uhc_max": 6, "dn_mm": 50 },
    { "uhc_max": 10, "dn_mm": 50 },
    { "uhc_max": 20, "dn_mm": 75 },
    { "uhc_max": 35, "dn_mm": 75 },
    { "uhc_max": 100, "dn_mm": 100 },
    { "uhc_max": 160, "dn_mm": 100 },
    { "uhc_max": 350, "dn_mm": 150 },
    { "uhc_max": 620, "dn_mm": 150 }
  ],
  "tabela_tubo_queda": [
    { "uhc_max": 2, "dn_mm": 40 },
    { "uhc_max": 4, "dn_mm": 50 },
    { "uhc_max": 10, "dn_mm": 50 },
    { "uhc_max": 30, "dn_mm": 75 },
    { "uhc_max": 70, "dn_mm": 75 },
    { "uhc_max": 240, "dn_mm": 100 },
    { "uhc_max": 500, "dn_mm": 100 },
    { "uhc_max": 960, "dn_mm": 150 },
    { "uhc_max": 2200, "dn_mm": 150 }
  ],
  "tabela_subcoletor": [
    { "uhc_max": 21, "dn_mm": 100 },
    { "uhc_max": 60, "dn_mm": 100 },
    { "uhc_max": 180, "dn_mm": 150 },
    { "uhc_max": 700, "dn_mm": 200 }
  ],
  "tabela_ventilacao": [
    { "dn_tq_mm": 50, "dn_vent_mm": 40 },
    { "dn_tq_mm": 75, "dn_vent_mm": 50 },
    { "dn_tq_mm": 100, "dn_vent_mm": 75 },
    { "dn_tq_mm": 150, "dn_vent_mm": 100 }
  ],
  "tabela_distancia_max_sem_ventilacao": [
    { "dn_ramal_mm": 40, "dist_max_m": 1.0 },
    { "dn_ramal_mm": 50, "dist_max_m": 1.2 },
    { "dn_ramal_mm": 75, "dist_max_m": 1.8 },
    { "dn_ramal_mm": 100, "dist_max_m": 2.4 }
  ],
  "diametros_comerciais_es_mm": [40, 50, 75, 100, 150, 200]
}
```

---

## 9. Fluxo de Dimensionamento (Passo a Passo)

```
INÍCIO

PASSO 1: PREPARAÇÃO
  ├─ Carregar JSON de UHC por aparelho
  ├─ Carregar tabelas de dimensionamento
  ├─ Obter topologia da rede de esgoto (trechos, conexões)
  └─ Obter lista de aparelhos por ambiente

PASSO 2: ATRIBUIR UHC A CADA APARELHO
  ├─ Para cada aparelho no modelo:
  │   ├─ Identificar tipo
  │   ├─ Consultar JSON → UHC
  │   └─ Registrar UHC
  └─ Totalizar UHC por ambiente

PASSO 3: DIMENSIONAR RAMAIS DE DESCARGA
  ├─ Para cada aparelho:
  │   ├─ DN = DN_minimo_aparelho (tabela)
  │   └─ Registrar DN do ramal de descarga
  └─ Verificar: vaso = DN 100 sempre

PASSO 4: DIMENSIONAR RAMAIS DE ESGOTO
  ├─ Para cada ramal de esgoto:
  │   ├─ ΣUHC = soma dos aparelhos conectados
  │   ├─ DN_tabela = consulta_tabela_ramal(ΣUHC)
  │   ├─ DN_min = MAX(DN dos ramais de descarga conectados)
  │   ├─ DN_final = MAX(DN_tabela, DN_min)
  │   └─ Registrar
  └─ Verificar: DN nunca diminui no escoamento

PASSO 5: DIMENSIONAR TUBOS DE QUEDA
  ├─ Para cada TQ:
  │   ├─ ΣUHC = soma de todos os pavimentos
  │   ├─ DN_tabela = consulta_tabela_TQ(ΣUHC)
  │   ├─ DN_maxramal = MAX(DN ramais conectados)
  │   ├─ DN_final = MAX(DN_tabela, DN_maxramal)
  │   └─ Registrar
  └─ Verificar: DN_TQ ≥ 100 se recebe vaso

PASSO 6: DIMENSIONAR SUBCOLETORES
  ├─ Para cada subcoletor:
  │   ├─ ΣUHC = soma de todos os TQs conectados
  │   ├─ DN = MAX(consulta_tabela_SC(ΣUHC), DN_maxTQ, 100)
  │   └─ Registrar
  └─ Verificar: DN_SC ≥ 100 sempre

PASSO 7: APLICAR DECLIVIDADES
  ├─ Para cada trecho horizontal:
  │   ├─ decliv = regra por DN
  │   ├─ desnível = comprimento × decliv
  │   ├─ Z_final = Z_inicial - desnível
  │   └─ Ajustar elevação do endpoint
  ├─ Reconectar fittings afetados
  └─ Verificar: nenhum trecho sobe

PASSO 8: VERIFICAR VENTILAÇÃO
  ├─ Para cada TQ:
  │   ├─ Verificar ventilação primária (prolongamento)
  │   └─ Se ausente: ERRO_CRITICO
  ├─ Para cada aparelho:
  │   ├─ Calcular distância ao TQ/coluna de ventilação
  │   ├─ Se dist > dist_max: sinalizar necessidade de ventilação
  │   └─ Registrar
  ├─ Dimensionar coluna de ventilação: DN ≥ 2/3 × DN_TQ
  └─ Verificar: ventilação sempre sobe

PASSO 9: VERIFICAR ACESSÓRIOS
  ├─ CX sifonada em cada banheiro
  ├─ CX gordura em cada cozinha
  ├─ CI em mudanças de direção
  ├─ CI a cada ~15m de subcoletor
  └─ Sinalizar ausências

PASSO 10: VALIDAÇÃO FINAL
  ├─ DN nunca diminui no escoamento (todos os trechos)
  ├─ Todos os aparelhos conectados à rede
  ├─ Todos os trechos com declividade aplicada
  ├─ Ventilação primária em cada TQ
  ├─ Sem curvas de 90° (ou alertadas)
  └─ Gerar relatório completo

PASSO 11: APLICAR NO MODELO
  ├─ Atualizar DN de cada Pipe no Revit
  ├─ Atualizar Z de endpoints ajustados
  └─ Gerar log de dimensionamento

FIM
```

### Estrutura do relatório de dimensionamento ES

```
| Trecho | Tipo | Aparelhos | ΣUHC | DN (mm) | Decliv. | Comp. (m) | Desnível (m) |
|--------|------|----------|------|---------|---------|-----------|-------------|

Ventilação:
| TQ | DN_TQ | Vent. Primária | DN_Vent | Status |
|----|-------|---------------|---------|--------|

Acessórios:
| Ambiente | CX Sifonada | CX Gordura | Status |
|----------|------------|------------|--------|
```

---

## 10. Limitações e Interpretações

### 10.1 Pontos ambíguos da norma

| Ponto | Ambiguidade | Decisão adotada pelo plugin |
|-------|------------|----------------------------|
| UHC da caixa sifonada | Norma trata como acessório, não como aparelho. UHC varia conforme aparelhos conectados. | Plugin soma UHCs dos aparelhos conectados à CX, não adiciona UHC separado para a CX. |
| Distância máxima sem ventilação | Norma mede "distância do desconector ao tubo de ventilação". Em layout real, medir distância horizontal pelo percurso da tubulação. | Plugin mede distância pelo comprimento real do ramal (via Pipes), não em linha reta. |
| Curva de 90° em ramal de descarga | Norma permite com ressalvas em trechos curtos. | Plugin permite em ramal de descarga < 1m com alerta Leve. Bloqueia em ramal de esgoto. |
| UHC de pia com triturador | Não é universal no Brasil. | Plugin considera como aparelho separado no JSON (pia_cozinha_triturador). |
| Subcoletor com decliv. 0.65% | Valor intermediário, menos usual. | Plugin implementa exatamente conforme tabela. |

### 10.2 Decisões que exigem engenharia

| Decisão | Por que não automatizar |
|---------|----------------------|
| Posição da caixa de inspeção externa | Depende de implantação, terreno, acesso, legislação municipal |
| Compartilhamento de caixa sifonada entre ambientes | Depende de proximidade, layout, preferência construtiva |
| Uso de bomba de esgoto | Quando não há espaço para gravitacional (subsolo) — decisão de projeto |
| Desvio de subcoletor por obstáculo | Caso a caso: fundação, tubulação existente |
| Ventilação de alívio (> 10 pavimentos) | Fora do escopo residencial; requer análise caso a caso |

### 10.3 Simplificações para automação

| Simplificação | Justificativa | Impacto |
|--------------|--------------|---------|
| Vaso sempre caixa acoplada (UHC 6, DN 100) | Cobre 90%+ dos projetos residenciais | Nenhum para residencial padrão |
| Apenas PVC esgoto (branco) | Material mais comum, DN padronizado | Cobre 95% dos projetos |
| Decliv. máxima genérica 5% | Simplifica validação | Conservador |
| CX sifonada 1 por banheiro | Padrão mais comum | Exceções em banheiros grandes são raras |
| Tabela de ventilação simplificada (2/3) | Preciso o suficiente para residencial | ± suficiente |

### 10.4 Quando o plugin deve parar

```
1. DN diminui no sentido do escoamento → ERRO CRÍTICO obrigatório
2. Trecho sem declividade ou contra gravidade → ERRO CRÍTICO
3. TQ sem ventilação primária → ERRO CRÍTICO
4. Nenhum espaço para declividade entre lajes → ALERTA MÉDIO + decisão humana
5. Subcoletor sem CI por > 15m → ALERTA MÉDIO
6. Aparelho não conectado à rede → ERRO CRÍTICO
```
