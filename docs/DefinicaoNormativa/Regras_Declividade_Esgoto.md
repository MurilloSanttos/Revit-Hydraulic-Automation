# Regras de Declividade para Esgoto Sanitário — NBR 8160

> Documentação técnica completa das regras de declividade aplicáveis a sistemas de esgoto sanitário predial, convertidas em lógica implementável pelo plugin.

---

## 1. Conceito de Declividade

### 1.1 Definição técnica

Declividade (i) é a relação entre o desnível vertical (Δh) e o comprimento horizontal (L) de um trecho de tubulação:

```
i = Δh / L  (adimensional)

Expresso em porcentagem:
i% = (Δh / L) × 100

Exemplo: i = 2% significa 2 cm de desnível para cada 1 m de comprimento horizontal.
```

Em esgoto sanitário predial, a declividade é **sempre descendente** no sentido do escoamento (do aparelho em direção ao tubo de queda ou caixa de inspeção).

### 1.2 Importância no escoamento

A declividade controla diretamente três fenômenos hidráulicos:

| Fenômeno | Declividade baixa demais | Declividade adequada | Declividade alta demais |
|----------|------------------------|---------------------|------------------------|
| **Velocidade** | Muito baixa (< 0.6 m/s) | Adequada (0.6 – 2.5 m/s) | Muito alta (> 2.5 m/s) |
| **Transporte de sólidos** | Sedimentação e entupimento | Arraste eficiente | Água escoa rápido e sólidos ficam para trás |
| **Lâmina d'água** | Alta (tubo cheio demais) | Parcial (máx. 2/3 do diâmetro) | Muito baixa (escoamento raso) |

### 1.3 Relação com velocidade e autolimpeza

O esgoto sanitário transporta sólidos em suspensão. Para que o escoamento tenha capacidade de **autolimpeza** (arrasta sólidos sem depositar), a velocidade deve estar dentro de uma faixa:

| Parâmetro | Valor | Significado |
|-----------|-------|------------|
| Velocidade mínima de autolimpeza | **0.60 m/s** | Abaixo: sólidos sedimentam → entupimento |
| Velocidade máxima recomendada | **2.50 m/s** | Acima: separação sólido-líquido → sólidos grudam na parede |
| Velocidade crítica | **> 4.0 m/s** | Erosão da tubulação e conexões |

A declividade é o fator principal que determina a velocidade em escoamento gravitacional (sem pressão). Portanto, controlar a declividade = controlar a velocidade = garantir autolimpeza.

---

## 2. Tabela de Declividades

### 2.1 Tabela principal — Declividade por diâmetro nominal

| DN (mm) | Declividade mínima (%) | Declividade mínima (m/m) | Declividade recomendada (%) | Declividade máxima recomendada (%) | Desnível por metro (cm/m) |
|---------|----------------------|-------------------------|---------------------------|-----------------------------------|--------------------------|
| 40 | **2.0** | 0.020 | 2.5 – 3.0 | 5.0 | 2.0 cm/m (mín) |
| 50 | **2.0** | 0.020 | 2.5 – 3.0 | 5.0 | 2.0 cm/m (mín) |
| 75 | **2.0** | 0.020 | 2.0 – 2.5 | 5.0 | 2.0 cm/m (mín) |
| 100 | **1.0** | 0.010 | 1.5 – 2.0 | 5.0 | 1.0 cm/m (mín) |
| 150 | **0.65** | 0.0065 | 1.0 – 1.5 | 5.0 | 0.65 cm/m (mín) |
| 200 | **0.50** | 0.005 | 0.7 – 1.0 | 5.0 | 0.50 cm/m (mín) |

### 2.2 Regra simplificada (para memorização e implementação rápida)

```
DN ≤ 75mm  → declividade mínima = 2%
DN = 100mm → declividade mínima = 1%
DN = 150mm → declividade mínima = 0.65%
DN ≥ 200mm → declividade mínima = 0.5%
```

### 2.3 Declividade máxima

A NBR 8160 não define um valor máximo explícito obrigatório. Porém, a prática de engenharia estabelece:

| Limite | Valor | Justificativa |
|--------|-------|---------------|
| Máximo recomendado | **5%** | Acima: separação sólido-líquido significativa |
| Máximo aceitável com ressalvas | **8%** | Em trechos muito curtos (< 1m) |
| Proibido | **> 10%** | Erosão, sifonamento, comportamento imprevisível |

**No plugin:** declividade máxima = 5% como padrão. Acima de 5%: alerta Leve. Acima de 8%: alerta Médio.

---

## 3. Regras por Tipo de Tubulação

### 3.1 Ramal de descarga

| Parâmetro | Valor |
|-----------|-------|
| **Definição** | Trecho entre saída do aparelho (ou desconector) e ramal de esgoto |
| **Orientação** | Horizontal com declividade |
| **Declividade** | Conforme tabela da seção 2 |
| **Comprimento típico** | 0.5 – 3.0 m |

**Condições especiais:**

| Condição | Regra |
|----------|-------|
| Ramal de descarga do vaso sanitário | DN 100mm, declividade mínima 1% |
| Ramal curto (< 0.5m) | Manter declividade mínima mesmo assim |
| Ramal longo (> 2.5m para DN ≤ 75) | Verificar necessidade de ventilação individual |
| Ramal do vaso: independente | NÃO passa pela caixa sifonada |
| Ramal de lavatório/chuveiro | Pode ir para caixa sifonada; DN 40mm, decliv. 2% |

**Cálculo de desnível:**
```
Exemplo: ramal de descarga do lavatório
  DN = 40mm → declividade = 2%
  Comprimento = 1.5m
  Desnível = 1.5 × 0.02 = 0.03m = 3.0 cm
  Z_final = Z_inicial - 0.03
```

### 3.2 Ramal de esgoto

| Parâmetro | Valor |
|-----------|-------|
| **Definição** | Trecho que recebe ramais de descarga do pavimento e conduz ao tubo de queda |
| **Orientação** | Horizontal com declividade |
| **DN típico** | 50 – 100mm (residencial) |
| **Comprimento típico** | 1.0 – 6.0 m |

**Regras práticas:**

| Regra | Descrição |
|-------|-----------|
| Declividade conforme DN | Tabela seção 2 |
| Se recebe vaso: DN ≥ 100 | Declividade mínima 1% |
| Se recebe apenas CX sifonada: DN ≥ 50 | Declividade mínima 2% |
| Convergência para TQ | Todos os ramais devem convergir para o tubo de queda com declividade contínua |
| Sem pontos baixos (barriga) | Toda a extensão deve descer continuamente |

**Limitações:**

| Limitação | Impacto | Mitigação |
|-----------|---------|-----------|
| Espaço entre lajes insuficiente | Não há altura para aplicar declividade | Reduzir comprimento ou alterar rota |
| Viga no caminho | Tubulação precisa desviar | Desviar antes da viga (decisão humana) |
| Múltiplos ramais convergindo | Cada ramal contribui com desnível; ponto de encontro deve ter cota compatível | Calcular cotas de chegada |

### 3.3 Subcoletores e coletores

| Parâmetro | Valor |
|-----------|-------|
| **Definição** | Subcoletor: trecho horizontal enterrado do TQ à CI. Coletor: trecho da CI à rede pública. |
| **DN mínimo** | 100mm (subcoletor), 100mm (coletor) |
| **Comprimento típico** | 3.0 – 20.0 m |

**Critérios normativos:**

| DN (mm) | Declividade mínima subcoletor/coletor |
|---------|--------------------------------------|
| 100 | 1% |
| 150 | 0.65% |
| 200 | 0.50% |

**Ajustes de projeto:**

| Ajuste | Descrição |
|--------|-----------|
| CI em mudança de direção | Caixa de inspeção obrigatória em mudança > 90° |
| CI a cada ~15m | Máximo 15m entre CIs consecutivas |
| Profundidade mínima de enterramento | 30cm de cobertura sobre o tubo (proteção mecânica) |
| Declividade: manter constante no trecho | Evitar variações de decliv. entre CIs |
| Trecho entre CIs: reta | Sem curvas entre caixas de inspeção |

---

## 4. Influência Hidráulica

### 4.1 Relação declividade × velocidade

Para escoamento em conduto livre (gravitacional), a velocidade depende de:

```
V = f(i, D, n, y)

Onde:
  i = declividade
  D = diâmetro
  n = coeficiente de rugosidade (Manning)
  y = lâmina d'água

Simplificação (Manning para seção circular parcialmente cheia):
  V ≈ (1/n) × Rh^(2/3) × i^(1/2)

  Onde:
    Rh = raio hidráulico [m]
    n = 0.010 (PVC liso)
    i = declividade [m/m]
```

**Na prática do plugin:** a fórmula de Manning não é usada diretamente para dimensionamento residencial. As declividades tabeladas já garantem velocidades adequadas para cada DN. O plugin usa a tabela, não o cálculo de Manning.

### 4.2 Riscos por faixa de declividade

| Faixa de declividade | Velocidade estimada | Risco principal | Consequência |
|---------------------|--------------------|-----------------|-|
| **< mínima (ex: < 1% para DN 100)** | < 0.60 m/s | **Assoreamento** | Sólidos depositam no fundo do tubo. Redução progressiva da seção útil. Entupimento. |
| **= mínima** | 0.60 – 0.80 m/s | Nenhum (limiar) | Funciona, mas sem margem de segurança. Qualquer irregularidade pode causar acúmulo. |
| **= recomendada** | 0.80 – 1.50 m/s | **Nenhum** | Faixa ideal. Arraste eficiente. Autolimpeza. |
| **Entre 3% e 5%** | 1.50 – 2.50 m/s | Baixo | Velocidade alta mas ainda dentro do aceitável. Ruído pode ser perceptível. |
| **> 5%** | > 2.50 m/s | **Separação sólido-líquido** | Água escoa rápido e sólidos ficam aderidos à parede. Acúmulo localizado. |
| **> 8%** | > 3.50 m/s | **Erosão e turbulência** | Desgaste de conexões. Ruído excessivo. Possível sifonamento de desconectores. |

### 4.3 Lâmina d'água máxima

| Componente | Lâmina máxima | Justificativa |
|-----------|--------------|---------------|
| Ramal de descarga | 1/2 do diâmetro (50%) | Permitir ventilação sobre a lâmina |
| Ramal de esgoto | 2/3 do diâmetro (67%) | Folga para picos de vazão |
| Subcoletor | 3/4 do diâmetro (75%) | Maior capacidade, mas com folga |

A lâmina é controlada pela relação entre vazão e capacidade do tubo (que depende do diâmetro e da declividade). As declividades tabeladas já consideram esses limites.

---

## 5. Regras Convertidas para Lógica

### 5.1 Atribuição de declividade

```
REGRA DEC-01: Declividade mínima por DN
  ENTRADA: DN do trecho [mm]
  PROCESSAR:
    SE DN <= 75:
      decliv_min = 0.02
    SE DN == 100:
      decliv_min = 0.01
    SE DN == 150:
      decliv_min = 0.0065
    SE DN >= 200:
      decliv_min = 0.005
  SAÍDA: decliv_min [adimensional]

REGRA DEC-02: Declividade recomendada por DN
  ENTRADA: DN do trecho [mm]
  PROCESSAR:
    SE DN <= 50:
      decliv_rec = 0.025
    SE DN == 75:
      decliv_rec = 0.02
    SE DN == 100:
      decliv_rec = 0.015
    SE DN == 150:
      decliv_rec = 0.01
    SE DN >= 200:
      decliv_rec = 0.007
  SAÍDA: decliv_rec [adimensional]

REGRA DEC-03: Declividade máxima
  CONSTANTE: decliv_max = 0.05
  CONSTANTE: decliv_max_absoluto = 0.08
```

### 5.2 Cálculo de desnível

```
REGRA DEC-04: Calcular desnível do trecho
  ENTRADA: comprimento [m], declividade [adimensional]
  PROCESSAR:
    desnivel = comprimento × declividade
  SAÍDA: desnivel [m]

REGRA DEC-05: Calcular novo Z do endpoint
  ENTRADA: Z_inicial [m], desnivel [m], sentido_escoamento
  PROCESSAR:
    SE sentido == montante_para_jusante:
      Z_final = Z_inicial - desnivel
    SENÃO:
      ERRO("Sentido de escoamento inválido")
  SAÍDA: Z_final [m]
```

### 5.3 Aplicação de declividade

```
REGRA DEC-06: Aplicar declividade em trecho
  ENTRADA: Pipe (elemento do Revit), DN, sentido_escoamento
  PROCESSAR:
    1. Obter Z_inicial (montante) e Z_final (jusante) atuais
    2. Obter comprimento horizontal do trecho
    3. decliv_alvo = DEC-02(DN) // usar recomendada como padrão
    4. desnivel = DEC-04(comprimento, decliv_alvo)
    5. Z_final_novo = DEC-05(Z_inicial, desnivel)
    6. Ajustar endpoint do Pipe para Z_final_novo
    7. Reconectar fittings nos endpoints
  SAÍDA: Pipe com declividade aplicada

REGRA DEC-07: Aplicar em cascata (múltiplos trechos)
  ENTRADA: lista de Pipes no sentido montante→jusante
  PROCESSAR:
    Z_corrente = Z_inicial_do_primeiro_trecho
    PARA cada Pipe na lista:
      desnivel = comprimento × decliv_alvo
      Z_final = Z_corrente - desnivel
      Ajustar endpoint para Z_final
      Z_corrente = Z_final  // proximo trecho começa onde este terminou
  SAÍDA: todos os trechos com declividade contínua
```

### 5.4 Validação de declividade

```
REGRA DEC-08: Validar declividade aplicada
  ENTRADA: Z_inicial, Z_final, comprimento, DN
  PROCESSAR:
    SE comprimento <= 0:
      ERRO_CRITICO("Comprimento inválido")
    
    decliv_aplicada = (Z_inicial - Z_final) / comprimento
    decliv_min = DEC-01(DN)
    
    SE Z_final >= Z_inicial:
      ERRO_CRITICO("Trecho subindo — contra gravidade")
      RETORNAR FALHA
    
    SE decliv_aplicada < decliv_min:
      ERRO_CRITICO("Declividade {decliv_aplicada*100}% < mínima {decliv_min*100}%")
      RETORNAR FALHA
    
    SE decliv_aplicada > 0.08:
      ALERTA_MEDIO("Declividade {decliv_aplicada*100}% > 8% — risco alto")
    SENAO SE decliv_aplicada > 0.05:
      ALERTA_LEVE("Declividade {decliv_aplicada*100}% > 5% — risco de separação")
    
    RETORNAR OK

REGRA DEC-09: Verificar espaço disponível
  ENTRADA: comprimento, decliv, espessura_laje [m], DN_externo [m]
  PROCESSAR:
    desnivel = comprimento × decliv
    espaco_necessario = desnivel + DN_externo
    
    SE espaco_necessario > espessura_laje:
      ALERTA_MEDIO("Espaço insuficiente: necessário {espaco_necessario*100}cm, 
                     disponível {espessura_laje*100}cm")
      // Opções:
      //   1. Reduzir comprimento do trecho (dividir em 2)
      //   2. Alterar rota
      //   3. Decisão humana
  SAÍDA: status

REGRA DEC-10: Verificar continuidade
  ENTRADA: lista de trechos consecutivos
  PROCESSAR:
    PARA cada par (trecho_A, trecho_B) consecutivo:
      SE Z_final_A != Z_inicial_B (tolerância ± 1mm):
        ALERTA_MEDIO("Descontinuidade de cota entre trechos")
      SE decliv_B < decliv_A × 0.5:
        LOG_INFO("Redução brusca de declividade — verificar")
```

---

## 6. Parâmetros Configuráveis

### 6.1 JSON de declividades

```json
{
  "declividades": {
    "DN_40": {
      "minima": 0.02,
      "recomendada": 0.025,
      "maxima_recomendada": 0.05,
      "maxima_absoluta": 0.08,
      "unidade": "m/m"
    },
    "DN_50": {
      "minima": 0.02,
      "recomendada": 0.025,
      "maxima_recomendada": 0.05,
      "maxima_absoluta": 0.08,
      "unidade": "m/m"
    },
    "DN_75": {
      "minima": 0.02,
      "recomendada": 0.02,
      "maxima_recomendada": 0.05,
      "maxima_absoluta": 0.08,
      "unidade": "m/m"
    },
    "DN_100": {
      "minima": 0.01,
      "recomendada": 0.015,
      "maxima_recomendada": 0.05,
      "maxima_absoluta": 0.08,
      "unidade": "m/m"
    },
    "DN_150": {
      "minima": 0.0065,
      "recomendada": 0.01,
      "maxima_recomendada": 0.05,
      "maxima_absoluta": 0.08,
      "unidade": "m/m"
    },
    "DN_200": {
      "minima": 0.005,
      "recomendada": 0.007,
      "maxima_recomendada": 0.05,
      "maxima_absoluta": 0.08,
      "unidade": "m/m"
    }
  }
}
```

### 6.2 JSON de configuração de aplicação

```json
{
  "configuracao_declividade": {
    "modo_aplicacao": "recomendada",
    "tolerancia_cota_mm": 1,
    "tolerancia_decliv_percentual": 5,
    "aplicar_em_cascata": true,
    "reconectar_fittings": true,
    "verificar_espaco_laje": true,
    "espessura_laje_padrao_m": 0.15,
    "alertar_acima_de_5_porcento": true,
    "bloquear_acima_de_8_porcento": false
  }
}
```

### 6.3 JSON de desnível por cenário típico

```json
{
  "exemplos_desnivel": {
    "ramal_descarga_lavatorio": {
      "dn_mm": 40,
      "comprimento_m": 1.5,
      "declividade": 0.02,
      "desnivel_m": 0.030,
      "desnivel_cm": 3.0
    },
    "ramal_descarga_vaso": {
      "dn_mm": 100,
      "comprimento_m": 2.0,
      "declividade": 0.01,
      "desnivel_m": 0.020,
      "desnivel_cm": 2.0
    },
    "ramal_esgoto_banheiro": {
      "dn_mm": 100,
      "comprimento_m": 4.0,
      "declividade": 0.015,
      "desnivel_m": 0.060,
      "desnivel_cm": 6.0
    },
    "subcoletor_terreo": {
      "dn_mm": 100,
      "comprimento_m": 8.0,
      "declividade": 0.01,
      "desnivel_m": 0.080,
      "desnivel_cm": 8.0
    },
    "subcoletor_longo": {
      "dn_mm": 150,
      "comprimento_m": 15.0,
      "declividade": 0.0065,
      "desnivel_m": 0.098,
      "desnivel_cm": 9.8
    }
  }
}
```

---

## 7. Aplicação no Modelo BIM (Revit)

### 7.1 Como aplicar inclinação em tubulações no Revit

A aplicação de declividade no Revit é feita **ajustando a coordenada Z dos endpoints dos Pipes**. Não existe um parâmetro nativo "slope" para Pipes de esgoto gravitacional no Revit MEP.

**Processo via API:**

```
1. Obter o Pipe via FilteredElementCollector
2. Obter LocationCurve do Pipe
3. Obter pontos Start e End (XYZ)
4. Determinar qual é montante e qual é jusante
5. Calcular Z_final = Z_montante - (comprimento × declividade)
6. Criar novo XYZ com Z ajustado
7. Abrir Transaction
8. Mover endpoint via LocationCurve.SetEndPoint()
9. Commitar Transaction
10. Verificar se fittings conectados seguem o ajuste
```

**Processo via Dynamo:**

```
1. Script recebe lista de ElementIds + declividade por DN (JSON)
2. Para cada Pipe:
   a. Ler endpoints via Element.GetParameterValueByName
   b. Calcular novo Z
   c. Mover endpoint via SetParameterByName ou Geometry.Translate
3. Executar em batch por pavimento
```

### 7.2 Como validar automaticamente

```
PROCEDIMENTO: Validar declividade de toda a rede ES

1. Coletar todos os Pipes do PipingSystem de esgoto
2. Filtrar: apenas trechos horizontais (|ΔZ| > 0 E ΔZ_horizontal > 0)
3. Para cada Pipe:
   a. Obter Z_montante, Z_jusante, comprimento, DN
   b. Calcular decliv_real = (Z_montante - Z_jusante) / comprimento
   c. Obter decliv_min = tabela[DN]
   d. Aplicar REGRA DEC-08
4. Gerar relatório: trechos OK, trechos com erro, trechos com alerta
```

### 7.3 Como tratar conflitos geométricos

| Conflito | Detecção | Resolução |
|----------|---------|-----------|
| **Tubo fura laje inferior** | Z_final < Z_nivel_inferior + espessura_laje | Alerta Médio → decisão humana |
| **Fitting desconecta** | Conector do Pipe não conectado após ajuste | Reconectar via Connector.ConnectTo() |
| **Trechos consecutivos desalinhados** | Z_final_A ≠ Z_inicial_B (> 1mm) | Ajustar Z_inicial_B = Z_final_A |
| **Decliv. impossível (distância curta, desnível grande)** | desnivel > espaco_disponível | Reduzir comprimento, alterar rota, ou aceitar decliv. > recomendada |
| **Tubo cruza outro elemento** | BoundingBox intersection pós-ajuste | Alerta Médio → ajuste manual |

**Estratégia de aplicação segura:**

```
1. Salvar posições originais de todos os endpoints (backup)
2. Aplicar declividade em sequência (montante → jusante)
3. Para cada ajuste:
   a. Verificar se Z_final > Z_nivel_inferior
   b. Verificar se fitting reconecta
   c. Se falha: reverter este trecho, registrar erro
4. Verificar continuidade entre trechos
5. Gerar relatório de ajustes e falhas
6. Se taxa de falha > 30%: reverter tudo, solicitar intervenção humana
```

---

## 8. Validações e Erros

### 8.1 Classificação de erros

| Código | Condição | Nível | Ação |
|--------|---------|-------|------|
| ERR-DEC-01 | Trecho subindo (contra gravidade) | **Crítico** | Bloqueia avanço. Trecho invertido. |
| ERR-DEC-02 | Declividade < mínima para o DN | **Crítico** | Bloqueia avanço. Trecho subdimensionado. |
| ERR-DEC-03 | Declividade zero (trecho horizontal puro) | **Crítico** | Bloqueia avanço. Sem escoamento. |
| ERR-DEC-04 | Descontinuidade de cota entre trechos | **Médio** | Permite com aceite. Pode ser degrau intencional. |
| ERR-DEC-05 | Espaço insuficiente para declividade | **Médio** | Permite com aceite. Requer solução alternativa. |
| ERR-DEC-06 | Declividade > 5% | **Leve** | Informativo. Risco de separação sólido-líquido. |
| ERR-DEC-07 | Declividade > 8% | **Médio** | Permite com aceite. Risco significativo. |
| ERR-DEC-08 | Fitting desconectou após ajuste | **Médio** | Tentativa de reconexão automática. |
| ERR-DEC-09 | Tubo fura laje após ajuste | **Médio** | Reverter ajuste. Solicitar decisão humana. |
| ERR-DEC-10 | Taxa de falha > 30% dos trechos | **Crítico** | Reverter todos os ajustes. Pipeline bloqueado. |

### 8.2 Quando bloquear o avanço

```
BLOQUEAR SE:
  - Qualquer trecho subindo (ERR-DEC-01)
  - Qualquer trecho com decliv < mínima não corrigido (ERR-DEC-02)
  - Qualquer trecho com decliv = 0 (ERR-DEC-03)
  - Taxa de falha > 30% (ERR-DEC-10)
```

### 8.3 Quando gerar alerta (mas permitir avanço)

```
ALERTAR SE:
  - Declividade > 5% (ERR-DEC-06) → informativo
  - Declividade > 8% (ERR-DEC-07) → aceite explícito
  - Espaço insuficiente (ERR-DEC-05) → aceite + decisão
  - Fitting desconectou (ERR-DEC-08) → tentativa automática
  - Descontinuidade de cota (ERR-DEC-04) → aceite
```

### 8.4 Quando permitir exceção

```
EXCEÇÃO PERMITIDA:
  - Declividade > 5% em trecho < 1.0m (curva fechada, transição)
  - Declividade < mínima em trecho < 0.3m (conexão direta)
  - Degrau intencional entre trechos (queda vertical em CI)
  
  Todas exceções requerem aceite explícito do usuário + log registrado.
```

---

## 9. Limitações e Interpretações

### 9.1 Pontos ambíguos da norma

| Ponto | Ambiguidade | Decisão adotada pelo plugin |
|-------|------------|----------------------------|
| Declividade máxima | Norma não define valor obrigatório | Plugin adota 5% como máximo recomendado e 8% como máximo absoluto |
| Declividade de subcoletor DN 150 | Valor 0.65% é pouco usual e gera confusão | Plugin implementa exatamente 0.0065 (sem arredondamento) |
| Declividade em trecho muito curto (< 0.3m) | Aplicar 2% em 0.2m = 4mm de desnível (irrelevante) | Plugin aplica mesmo assim para consistência, mas aceita tolerância de ±1mm |
| Declividade em ramal de descarga vs. ramal de esgoto | Mesma regra (por DN), mas contextos diferentes | Plugin aplica a mesma tabela, diferencia apenas pela nomenclatura do trecho |
| Ponto de ligação TQ → subcoletor | Transição vertical para horizontal — declividade começa a partir daqui | Plugin inicia cálculo de declividade no primeiro trecho horizontal após o TQ |

### 9.2 Simplificações para automação

| Simplificação | Justificativa | Impacto |
|--------------|--------------|---------|
| Usar declividade recomendada como padrão (não mínima) | Margem de segurança sem custo adicional | Desníveis ligeiramente maiores (verificar espaço) |
| Tolerância de ±1mm na cota | Precisão do Revit e da obra | Evita falsos positivos |
| Decliv. máxima genérica 5% | Simplifica validação | Conservador |
| Não calcular Manning | Tabelas tabeladas já garantem velocidade | Sem impacto para residencial |
| Aplicar em batch por pavimento | Performance e organização | Plugin mais rápido |

### 9.3 Decisões que exigem engenharia

| Decisão | Por que é humana |
|---------|-----------------|
| Alterar rota quando não há espaço para declividade | Múltiplas alternativas; depende de contexto e construtibilidade |
| Aceitar declividade > 5% | Avaliação caso a caso do risco vs. viabilidade |
| Inserir degrau (queda) em caixa de inspeção | Técnica construtiva que resolve diferença de cota; depende de análise |
| Redução de espessura de laje para acomodar tubulação | Decisão interdisciplinar (estrutura + hidráulica) |
| Usar bomba de esgoto | Quando gravitacional não é possível (subsolo); decisão de projeto |
