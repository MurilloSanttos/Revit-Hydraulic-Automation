# Regras de Dimensionamento de Água Fria — NBR 5626

> Extração e conversão das regras normativas de dimensionamento de redes de água fria em lógica técnica implementável por software.

---

## 1. Visão Geral do Dimensionamento

### 1.1 Conceitos principais

| Conceito | Definição aplicável |
|----------|-------------------|
| **Vazão de projeto** | Volume de água por unidade de tempo que a tubulação deve ser capaz de conduzir em condições normais de operação |
| **Peso relativo** | Valor adimensional atribuído a cada aparelho sanitário que representa sua probabilidade e magnitude de uso |
| **Vazão provável** | Vazão estimada para um conjunto de aparelhos considerando que nem todos são usados simultaneamente |
| **Pressão estática** | Pressão exercida pela coluna de água em repouso (sem escoamento) |
| **Pressão dinâmica** | Pressão real no ponto de utilização durante escoamento (estática - perdas de carga) |
| **Perda de carga** | Redução de pressão causada pelo atrito da água com as paredes do tubo e perdas em conexões |
| **Caminho crítico** | Trajeto do reservatório ao ponto de utilização mais desfavorável (maior perda de carga) |

### 1.2 Lógica geral

O dimensionamento segue o princípio: **a rede deve fornecer água a todos os pontos com vazão suficiente, velocidade controlada e pressão adequada.**

```
Para cada trecho da rede (de jusante para montante):
  1. Somar pesos dos aparelhos atendidos
  2. Calcular vazão provável
  3. Selecionar diâmetro que atenda V ≤ V_max
  4. Calcular perda de carga no trecho
  5. Acumular perda de carga no caminho crítico
  6. Verificar pressão disponível no ponto final
```

### 1.3 Etapas do cálculo (ordem)

```
1. Numerar trechos da rede (do ponto mais distante ao reservatório)
2. Listar aparelhos atendidos por trecho
3. Somar pesos por trecho (acumulativo)
4. Calcular vazão provável por trecho
5. Selecionar diâmetro comercial por trecho
6. Verificar velocidade em cada trecho
7. Calcular perda de carga por trecho
8. Identificar caminho crítico
9. Somar perdas de carga no caminho crítico
10. Calcular pressão disponível no ponto mais desfavorável
11. Se pressão insuficiente: aumentar diâmetro(s) ou adotar pressurizador
```

---

## 2. Cálculo de Vazão

### 2.1 Pesos relativos por aparelho

A NBR 5626 atribui um **peso relativo (P)** a cada tipo de aparelho sanitário. O peso representa a contribuição do aparelho para o cálculo de vazão.

| Aparelho | Peso (P) | Vazão mínima de projeto (L/s) | DN mínimo sub-ramal (mm) |
|----------|---------|-------------------------------|--------------------------|
| Vaso sanitário — caixa acoplada | 0.3 | 0.15 | 20 |
| Vaso sanitário — válvula de descarga | 32.0 | 1.70 | 50 |
| Lavatório | 0.5 | 0.15 | 20 |
| Bidê | 0.1 | 0.10 | 20 |
| Chuveiro (sem misturador) | 0.5 | 0.20 | 20 |
| Chuveiro (com misturador) | 0.5 | 0.20 | 20 |
| Banheira | 1.0 | 0.30 | 25 |
| Pia de cozinha | 0.7 | 0.25 | 20 |
| Filtro de pressão | 0.1 | 0.10 | 20 |
| Tanque de lavar | 0.7 | 0.25 | 25 |
| Máquina de lavar louça | 1.0 | 0.30 | 25 |
| Máquina de lavar roupa | 1.0 | 0.30 | 25 |
| Torneira de jardim | 0.5 | 0.20 | 20 |
| Torneira de tanque | 0.7 | 0.25 | 25 |

**Nota para o plugin:**
- Padrão residencial brasileiro: **vaso com caixa acoplada** (peso 0.3)
- Se o projeto usar válvula de descarga (peso 32.0), o dimensionamento muda **drasticamente** → o plugin deve solicitar confirmação do tipo de vaso
- Peso é adimensional — serve exclusivamente para o cálculo probabilístico de vazão

### 2.2 Conversão de peso em vazão

#### Fórmula fundamental (método probabilístico)

```
Q = C × √(ΣP)

Onde:
  Q  = vazão provável [L/s]
  C  = coeficiente de descarga (0.30 para aparelhos com caixa acoplada)
  ΣP = soma dos pesos de todos os aparelhos atendidos pelo trecho
```

#### Valores do coeficiente C

| Tipo de aparelhos no trecho | C |
|-----------------------------|---|
| Apenas aparelhos com caixa acoplada (residencial padrão) | **0.30** |
| Trecho com válvula de descarga | Cálculo separado (ver seção 2.3) |

#### Exemplos de cálculo

| Cenário | ΣP | Q (L/s) |
|---------|-----|---------|
| 1 lavatório (0.5) | 0.5 | 0.30 × √0.5 = 0.212 |
| 1 banheiro (vaso 0.3 + lav 0.5 + ch 0.5) | 1.3 | 0.30 × √1.3 = 0.342 |
| 2 banheiros + cozinha (1.3 + 1.3 + 0.7) | 3.3 | 0.30 × √3.3 = 0.545 |
| Casa completa (3 banh + coz + lav + serv) | ~7.0 | 0.30 × √7.0 = 0.794 |

### 2.3 Simultaneidade

#### Conceito

Nem todos os aparelhos são usados ao mesmo tempo. A fórmula Q = 0.3 × √ΣP já incorpora a probabilidade de uso simultâneo. Quanto maior o ΣP, menor a proporção de aparelhos em uso simultâneo.

| ΣP | Q (L/s) | Q/Q_total_individual | Fator de simultaneidade efetivo |
|----|---------|---------------------|-------------------------------|
| 1.0 | 0.300 | — | — |
| 5.0 | 0.671 | — | ~30% |
| 10.0 | 0.949 | — | ~25% |
| 50.0 | 2.121 | — | ~15% |

A fórmula da raiz quadrada é o modelo probabilístico simplificado aceito pela norma para instalações com até ~400 aparelhos (residencial e comercial pequeno).

#### Quando a fórmula NÃO se aplica

| Situação | Tratamento |
|----------|-----------|
| Apenas 1 aparelho no trecho | Usar vazão mínima do aparelho (tabela) |
| Trecho com válvula de descarga | Somar vazão da VD ao resultado da fórmula para os demais |
| Aparelhos industriais ou especiais | Fora do escopo do plugin |

#### Regra para válvula de descarga (quando aplicável)

```
SE trecho contém válvula de descarga:
  Q_trecho = Q_vd + 0.30 × √(ΣP_demais)
  
  Onde:
    Q_vd = 1.70 L/s (vazão da válvula de descarga)
    ΣP_demais = soma dos pesos dos demais aparelhos (excluindo a VD)
```

**No plugin:** O padrão é caixa acoplada. Se o modelo contiver válvula de descarga, o plugin deve alertar e recalcular.

---

## 3. Métodos de Dimensionamento

### 3.1 Método probabilístico (adotado)

| Aspecto | Detalhe |
|---------|---------|
| **Base** | Teoria das probabilidades — nem todos os aparelhos são usados ao mesmo tempo |
| **Fórmula** | Q = 0.30 × √ΣP |
| **Aplicação** | Instalações com ≤ 400 aparelhos (toda a faixa residencial) |
| **Precisão** | Adequada para projetos residenciais |
| **Adotado pelo plugin** | **Sim — este é o método padrão** |

### 3.2 Método empírico/prático (alternativa)

| Aspecto | Detalhe |
|---------|---------|
| **Base** | Tabelas pré-calculadas com vazão por tipo de ambiente |
| **Aplicação** | Estimativas rápidas, pré-dimensionamento |
| **Precisão** | Inferior ao probabilístico |
| **Adotado pelo plugin** | **Não** |

### 3.3 Critério de decisão

```
PARA projetos residenciais (escopo do plugin):
  SEMPRE usar método probabilístico (Q = 0.30 × √ΣP)
  
EXCEÇÃO:
  SE trecho atende apenas 1 aparelho:
    Usar vazão mínima de projeto do aparelho (tabela 2.1)
    (pois a fórmula probabilística não se aplica a 1 aparelho isolado)
```

---

## 4. Dimensionamento de Tubulações

### 4.1 Critério de escolha do diâmetro

O diâmetro é selecionado pelo critério de **velocidade máxima**:

```
Dado Q (vazão provável do trecho):
  Selecionar o MENOR diâmetro comercial onde:
    V = Q / A ≤ V_max

  Onde:
    V = velocidade do escoamento [m/s]
    Q = vazão [m³/s] (converter L/s → m³/s: dividir por 1000)
    A = área interna do tubo [m²] = π × (D_int/2)² 
    V_max = 3.0 m/s
    D_int = diâmetro interno do tubo [m]
```

### 4.2 Tabela de diâmetros comerciais — PVC soldável (água fria)

| DN nominal (mm) | D externo (mm) | D interno (mm) | A interna (m²) | Q máx a 3 m/s (L/s) |
|-----------------|---------------|----------------|----------------|---------------------|
| 20 | 20.0 | 17.0 | 2.270 × 10⁻⁴ | 0.681 |
| 25 | 25.0 | 21.6 | 3.664 × 10⁻⁴ | 1.099 |
| 32 | 32.0 | 27.8 | 6.068 × 10⁻⁴ | 1.820 |
| 40 | 40.0 | 35.2 | 9.733 × 10⁻⁴ | 2.920 |
| 50 | 50.0 | 44.0 | 1.521 × 10⁻³ | 4.562 |
| 60 | 60.0 | 53.4 | 2.240 × 10⁻³ | 6.720 |
| 75 | 75.0 | 66.6 | 3.483 × 10⁻³ | 10.450 |
| 85 | 85.0 | 75.6 | 4.488 × 10⁻³ | 13.464 |
| 110 | 110.0 | 97.8 | 7.509 × 10⁻³ | 22.527 |

### 4.3 Algoritmo de seleção de diâmetro

```python
def selecionar_diametro(Q_Ls):
    """
    Q_Ls: vazão em L/s
    Retorna: (DN_mm, D_int_mm, V_ms)
    """
    Q_m3s = Q_Ls / 1000.0
    V_MAX = 3.0  # m/s
    
    diametros = [
        (20, 17.0), (25, 21.6), (32, 27.8), (40, 35.2),
        (50, 44.0), (60, 53.4), (75, 66.6), (85, 75.6), (110, 97.8)
    ]
    
    for dn, d_int in diametros:
        d_int_m = d_int / 1000.0
        area = math.pi * (d_int_m / 2) ** 2
        velocidade = Q_m3s / area
        
        if velocidade <= V_MAX:
            return (dn, d_int, round(velocidade, 3))
    
    # Nenhum diâmetro atende → erro
    return None
```

### 4.4 Regras adicionais de diâmetro

| Regra | Descrição | Lógica |
|-------|-----------|--------|
| DN mínimo do sub-ramal | Cada aparelho tem DN mínimo obrigatório | `SE DN_calculado < DN_min_aparelho ENTÃO DN = DN_min_aparelho` |
| DN mínimo do ramal | Ramal de distribuição: mínimo DN 20mm | `SE DN_ramal < 20 ENTÃO DN_ramal = 20` |
| DN mínimo do barrilete | Barrilete: mínimo DN 25mm (boa prática) | `SE DN_barrilete < 25 ENTÃO DN_barrilete = 25` |
| DN não aumenta para jusante | No sentido do escoamento (reserv. → ponto), DN pode diminuir, nunca aumentar | `SE DN_jusante > DN_montante ENTÃO erro Médio` |

### 4.5 Velocidade

| Parâmetro | Valor | Significado |
|-----------|-------|------------|
| V máxima absoluta | **3.0 m/s** | Acima: ruído, golpe de aríete, desgaste |
| V recomendada | 1.0 – 2.5 m/s | Faixa ótima |
| V mínima recomendada | 0.5 m/s | Abaixo: risco de sedimentação |

```
Regras:
  SE V > 3.0 m/s → erro Crítico → aumentar diâmetro
  SE V < 0.5 m/s → alerta Leve → diâmetro pode ser excessivo
  SE 0.5 ≤ V ≤ 3.0 → OK
```

---

## 5. Verificação de Pressão

### 5.1 Pressão estática

```
P_est = H_geom = Z_reserv - Z_ponto  [m.c.a.]

Onde:
  Z_reserv = cota do nível d'água no reservatório [m]
  Z_ponto = cota do ponto de utilização [m]
  P_est = pressão estática no ponto [m.c.a.]
```

Conversão: 1 m.c.a. ≈ 10 kPa ≈ 0.1 kgf/cm²

### 5.2 Pressão dinâmica (disponível)

```
P_din = P_est - ΔH_total

Onde:
  ΔH_total = soma de todas as perdas de carga no caminho do reservatório ao ponto
  P_din = pressão real disponível no ponto durante uso [m.c.a.]
```

### 5.3 Limites de pressão

| Parâmetro | Valor | Regra |
|-----------|-------|-------|
| Pressão estática mínima normativa | 0.5 m.c.a. | Mínimo absoluto |
| Pressão dinâmica mínima normativa | 1.0 m.c.a. | Mínimo em qualquer ponto |
| Pressão dinâmica mínima prática | **3.0 m.c.a.** | **Valor adotado pelo plugin** (boa prática) |
| Pressão estática máxima | **40 m.c.a.** | Acima: necessário válvula redutora |

```
Regras:
  SE P_din < 3.0 → erro Crítico → aumentar diâmetros ou elevar reservatório
  SE P_din < 1.0 → erro Crítico → projeto inviável sem pressurizador
  SE P_est > 40.0 → alerta Médio → necessário válvula redutora
  SE P_din ≥ 3.0 E P_est ≤ 40.0 → OK
```

### 5.4 Pressão mínima por tipo de aparelho

| Aparelho | Pressão mínima recomendada (m.c.a.) |
|----------|-------------------------------------|
| Chuveiro (sem pressurizador) | 1.0 (norma) / 3.0 (prática) |
| Torneira de pia | 0.5 (norma) / 2.0 (prática) |
| Válvula de descarga | 1.5 (norma mínima), 3.0 (ideal) |
| Torneira de jardim | 0.5 (norma) |
| Máquina de lavar | 1.0 |

**No plugin:** usar pressão mínima uniforme de 3.0 m.c.a. para todos os pontos (configurável).

### 5.5 Caminho crítico

O caminho crítico é o trajeto do reservatório até o ponto com **menor pressão disponível** (geralmente o ponto mais alto e mais distante).

```
Algoritmo:
  1. Para cada ponto de consumo:
     a. Calcular H_geom = Z_reserv - Z_ponto
     b. Identificar o caminho (sequência de trechos) do reservatório ao ponto
     c. Somar ΔH de cada trecho no caminho
     d. P_din = H_geom - ΣΔH
  2. O ponto com menor P_din é o caminho crítico
  3. SE P_din_min < P_min → rede insuficiente
```

**Simplificação para residencial:** o ponto crítico geralmente é o chuveiro do pavimento mais alto no banheiro mais distante da prumada.

---

## 6. Perda de Carga

### 6.1 Perda de carga distribuída (ao longo do tubo)

#### Fórmula de Fair-Whipple-Hsiao (para tubos de PVC)

```
J = 8.69 × 10⁶ × Q^1.75 / D^4.75

Onde:
  J = perda de carga unitária [m/m] (ou kPa/m)
  Q = vazão [L/s]
  D = diâmetro interno [mm]
  
Perda no trecho:
  ΔH_dist = J × L

  Onde:
    L = comprimento real do trecho [m]
```

#### Tabela de J para referência rápida (PVC)

| Q (L/s) | DN 20 (J m/m) | DN 25 (J m/m) | DN 32 (J m/m) | DN 40 (J m/m) | DN 50 (J m/m) |
|---------|--------------|--------------|--------------|--------------|--------------|
| 0.10 | 0.0099 | 0.0030 | 0.0009 | 0.0003 | 0.0001 |
| 0.20 | 0.0334 | 0.0101 | 0.0029 | 0.0010 | 0.0003 |
| 0.30 | 0.0680 | 0.0206 | 0.0060 | 0.0020 | 0.0006 |
| 0.50 | 0.1628 | 0.0494 | 0.0143 | 0.0048 | 0.0015 |
| 0.70 | 0.2911 | 0.0883 | 0.0257 | 0.0086 | 0.0027 |
| 1.00 | 0.5395 | 0.1636 | 0.0475 | 0.0159 | 0.0050 |

Nota: valores tabelados para referência. O plugin deve calcular J pela fórmula para qualquer Q.

#### Implementação da fórmula

```python
def perda_carga_unitaria(Q_Ls, D_int_mm):
    """
    Fair-Whipple-Hsiao para PVC
    Q_Ls: vazão em L/s
    D_int_mm: diâmetro interno em mm
    Retorna: J em m/m
    """
    return 8.69e6 * (Q_Ls ** 1.75) / (D_int_mm ** 4.75)

def perda_carga_trecho(Q_Ls, D_int_mm, L_m, fator_localizadas=1.20):
    """
    Perda de carga total no trecho (distribuída + localizadas)
    """
    J = perda_carga_unitaria(Q_Ls, D_int_mm)
    return J * L_m * fator_localizadas
```

### 6.2 Perda de carga localizada

Perdas localizadas ocorrem em: tees, curvas, reduções, registros, válvulas.

#### Método 1: Comprimentos equivalentes (tabela)

| Conexão | DN 20 (m) | DN 25 (m) | DN 32 (m) | DN 40 (m) | DN 50 (m) |
|---------|----------|----------|----------|----------|----------|
| Curva 90° | 1.1 | 1.2 | 1.5 | 2.0 | 3.2 |
| Curva 45° | 0.4 | 0.5 | 0.7 | 0.9 | 1.2 |
| Tee passagem direta | 0.7 | 0.8 | 1.0 | 1.5 | 2.2 |
| Tee saída lateral | 2.3 | 2.4 | 3.1 | 4.6 | 7.3 |
| Registro gaveta aberto | 0.2 | 0.3 | 0.4 | 0.5 | 0.7 |
| Registro pressão aberto | 3.5 | 4.3 | 5.0 | 6.0 | 8.1 |
| Válvula de retenção | 2.5 | 3.2 | 4.0 | 5.0 | 6.3 |
| Redução (entrada) | 0.5 | 0.6 | 0.7 | 0.9 | 1.2 |

#### Método 2: Fator percentual (simplificado — adotado pelo plugin)

```
ΔH_total = J × L × K

Onde:
  K = fator de ajuste para perdas localizadas
  K = 1.20 (20% sobre o comprimento real)
```

O fator de 20% é uma **aproximação amplamente aceita** para instalações residenciais. É conservador e simplifica o código significativamente.

```
Regra de decisão:
  PARA projetos residenciais (< 50 aparelhos):
    Usar K = 1.20 (método simplificado)
  
  PARA projetos maiores ou de precisão:
    Usar tabela de comprimentos equivalentes (somar Leq de cada conexão)
    ΔH = J × (L_real + ΣL_eq)
```

**No plugin:** K = 1.20 como padrão, com opção de usar comprimentos equivalentes na configuração avançada.

### 6.3 Perda de carga total no caminho

```
ΔH_caminho = Σ (J_trecho × L_trecho × K)

Para cada trecho no caminho do reservatório ao ponto:
  1. Calcular J com Q e D do trecho
  2. Multiplicar por L (comprimento) e K (fator localizadas)
  3. Somar todas as perdas
```

---

## 7. Regras Convertidas para Lógica

### 7.1 Cálculo de vazão

```
REGRA VZ-01: Vazão provável
  ENTRADA: lista de aparelhos no trecho
  PROCESSAR:
    soma_pesos = SOMA(peso de cada aparelho)
    SE soma_pesos <= 0:
      ERRO("Nenhum aparelho no trecho")
    SENÃO:
      vazao = 0.30 * RAIZ(soma_pesos)
  SAÍDA: vazao [L/s]

REGRA VZ-02: Vazão mínima de aparelho isolado
  SE trecho atende apenas 1 aparelho:
    vazao = MAX(vazao_calculada, vazao_minima_aparelho)
  
REGRA VZ-03: Trecho com válvula de descarga
  SE trecho contém VD:
    soma_pesos_demais = SOMA(pesos excluindo VD)
    vazao = 1.70 + 0.30 * RAIZ(soma_pesos_demais)
```

### 7.2 Seleção de diâmetro

```
REGRA DN-01: Selecionar diâmetro por velocidade
  ENTRADA: vazao [L/s]
  PROCESSAR:
    PARA cada DN em [20, 25, 32, 40, 50, 60, 75, 85, 110]:
      area = PI * (D_int[DN] / 2000)^2  [m²]
      velocidade = (vazao / 1000) / area  [m/s]
      SE velocidade <= 3.0:
        RETORNAR DN, velocidade
    ERRO_CRITICO("Nenhum diâmetro comercial atende V ≤ 3.0 m/s")

REGRA DN-02: Diâmetro mínimo do sub-ramal
  SE DN_calculado < DN_minimo_aparelho:
    DN_final = DN_minimo_aparelho
    
REGRA DN-03: Diâmetro não aumenta para jusante
  SE DN_trecho_jusante > DN_trecho_montante:
    ALERTA_MEDIO("Diâmetro aumenta no sentido do escoamento — verificar")
```

### 7.3 Perda de carga

```
REGRA PC-01: Perda de carga unitária (Fair-Whipple-Hsiao, PVC)
  J = 8.69E6 * Q^1.75 / D_int^4.75  [m/m]

REGRA PC-02: Perda de carga no trecho
  DH_trecho = J * L * K
  ONDE K = 1.20 (padrão) ou 1.0 + soma_Leq/L (avançado)

REGRA PC-03: Perda de carga no caminho crítico
  DH_caminho = SOMA(DH_trecho para cada trecho no caminho)
```

### 7.4 Verificação de pressão

```
REGRA PR-01: Pressão estática
  P_est = Z_reservatorio - Z_ponto  [m.c.a.]
  SE P_est > 40:
    ALERTA_MEDIO("Necessário válvula redutora de pressão")
  SE P_est <= 0:
    ERRO_CRITICO("Ponto acima do reservatório — pressão zero ou negativa")

REGRA PR-02: Pressão disponível
  P_din = P_est - DH_caminho
  SE P_din < 3.0:
    ERRO_CRITICO("Pressão insuficiente: P_din = {P_din} m.c.a. < 3.0")
  SE P_din < 1.0:
    ERRO_CRITICO("Projeto inviável sem pressurizador")
  SE P_din >= 3.0:
    OK

REGRA PR-03: Pressão máxima
  SE P_est > 40:
    ALERTA_MEDIO("Pressão estática > 40 m.c.a. — válvula redutora necessária")
```

### 7.5 Validações adicionais

```
REGRA VA-01: Registro de gaveta por ambiente
  PARA cada ambiente hidráulico:
    SE não existe registro de gaveta na entrada:
      ALERTA_MEDIO("Falta registro na entrada do ambiente")

REGRA VA-02: Registro geral no barrilete
  SE não existe registro na saída do reservatório:
    ALERTA_MEDIO("Falta registro geral no barrilete")

REGRA VA-03: Sub-ramal mínimo
  PARA cada sub-ramal:
    SE DN_sub_ramal < DN_minimo_aparelho:
      ERRO_CRITICO("Sub-ramal abaixo do mínimo normativo")
```

---

## 8. Parâmetros Configuráveis

### 8.1 JSON de parâmetros de dimensionamento AF

```json
{
  "dimensionamento_af": {
    "metodo": "probabilistico",
    "coeficiente_C": 0.30,
    "velocidade_maxima_ms": 3.0,
    "velocidade_minima_ms": 0.5,
    "pressao_minima_mca": 3.0,
    "pressao_maxima_mca": 40.0,
    "fator_perdas_localizadas": 1.20,
    "formula_perda_carga": "Fair-Whipple-Hsiao",
    "material": "PVC_soldavel",
    "tipo_vaso": "caixa_acoplada",
    "altura_reservatorio_m": 6.0
  }
}
```

### 8.2 JSON de aparelhos

```json
{
  "aparelhos_af": [
    { "tipo": "vaso_caixa_acoplada", "peso": 0.3, "vazao_min_Ls": 0.15, "dn_min_mm": 20 },
    { "tipo": "vaso_valvula_descarga", "peso": 32.0, "vazao_min_Ls": 1.70, "dn_min_mm": 50 },
    { "tipo": "lavatorio", "peso": 0.5, "vazao_min_Ls": 0.15, "dn_min_mm": 20 },
    { "tipo": "bide", "peso": 0.1, "vazao_min_Ls": 0.10, "dn_min_mm": 20 },
    { "tipo": "chuveiro", "peso": 0.5, "vazao_min_Ls": 0.20, "dn_min_mm": 20 },
    { "tipo": "banheira", "peso": 1.0, "vazao_min_Ls": 0.30, "dn_min_mm": 25 },
    { "tipo": "pia_cozinha", "peso": 0.7, "vazao_min_Ls": 0.25, "dn_min_mm": 20 },
    { "tipo": "tanque", "peso": 0.7, "vazao_min_Ls": 0.25, "dn_min_mm": 25 },
    { "tipo": "maquina_lavar_roupa", "peso": 1.0, "vazao_min_Ls": 0.30, "dn_min_mm": 25 },
    { "tipo": "maquina_lavar_louca", "peso": 1.0, "vazao_min_Ls": 0.30, "dn_min_mm": 25 },
    { "tipo": "torneira_jardim", "peso": 0.5, "vazao_min_Ls": 0.20, "dn_min_mm": 20 },
    { "tipo": "filtro", "peso": 0.1, "vazao_min_Ls": 0.10, "dn_min_mm": 20 }
  ]
}
```

### 8.3 JSON de diâmetros comerciais

```json
{
  "diametros_pvc_af": [
    { "dn_mm": 20, "d_ext_mm": 20.0, "d_int_mm": 17.0 },
    { "dn_mm": 25, "d_ext_mm": 25.0, "d_int_mm": 21.6 },
    { "dn_mm": 32, "d_ext_mm": 32.0, "d_int_mm": 27.8 },
    { "dn_mm": 40, "d_ext_mm": 40.0, "d_int_mm": 35.2 },
    { "dn_mm": 50, "d_ext_mm": 50.0, "d_int_mm": 44.0 },
    { "dn_mm": 60, "d_ext_mm": 60.0, "d_int_mm": 53.4 },
    { "dn_mm": 75, "d_ext_mm": 75.0, "d_int_mm": 66.6 },
    { "dn_mm": 85, "d_ext_mm": 85.0, "d_int_mm": 75.6 },
    { "dn_mm": 110, "d_ext_mm": 110.0, "d_int_mm": 97.8 }
  ]
}
```

### 8.4 JSON de comprimentos equivalentes (avançado)

```json
{
  "comprimentos_equivalentes_m": {
    "curva_90": { "20": 1.1, "25": 1.2, "32": 1.5, "40": 2.0, "50": 3.2 },
    "curva_45": { "20": 0.4, "25": 0.5, "32": 0.7, "40": 0.9, "50": 1.2 },
    "tee_passagem": { "20": 0.7, "25": 0.8, "32": 1.0, "40": 1.5, "50": 2.2 },
    "tee_saida_lateral": { "20": 2.3, "25": 2.4, "32": 3.1, "40": 4.6, "50": 7.3 },
    "registro_gaveta": { "20": 0.2, "25": 0.3, "32": 0.4, "40": 0.5, "50": 0.7 },
    "registro_pressao": { "20": 3.5, "25": 4.3, "32": 5.0, "40": 6.0, "50": 8.1 },
    "valvula_retencao": { "20": 2.5, "25": 3.2, "32": 4.0, "40": 5.0, "50": 6.3 }
  }
}
```

---

## 9. Fluxo de Dimensionamento (Passo a Passo)

### Fluxo completo implementado pelo plugin

```
INÍCIO

PASSO 1: PREPARAÇÃO
  ├─ Carregar parâmetros de configuração (JSON)
  ├─ Carregar tabela de aparelhos (JSON)
  ├─ Obter cota do reservatório (Z_reserv)
  └─ Obter topologia da rede (trechos, comprimentos, conexões)

PASSO 2: IDENTIFICAÇÃO DE TRECHOS
  ├─ Numerar cada trecho da rede (de jusante para montante)
  ├─ Para cada trecho: listar aparelhos atendidos a jusante
  └─ Identificar caminho do reservatório a cada ponto

PASSO 3: CÁLCULO DE PESOS (por trecho)
  ├─ Para cada trecho:
  │   ├─ Somar pesos dos aparelhos atendidos (acumulativo)
  │   └─ Registrar ΣP do trecho
  └─ Verificar: SE ΣP = 0 para qualquer trecho → erro

PASSO 4: CÁLCULO DE VAZÃO (por trecho)
  ├─ Para cada trecho:
  │   ├─ SE atende apenas 1 aparelho: Q = vazao_minima_aparelho
  │   ├─ SENÃO: Q = 0.30 × √ΣP
  │   └─ Registrar Q do trecho
  └─ Verificar: SE Q > 22 L/s → erro (acima do DN máximo)

PASSO 5: SELEÇÃO DE DIÂMETRO (por trecho)
  ├─ Para cada trecho:
  │   ├─ Selecionar menor DN onde V ≤ 3.0 m/s
  │   ├─ Verificar contra DN mínimo do aparelho
  │   ├─ Calcular V real = Q / A
  │   └─ Registrar DN e V do trecho
  └─ Verificar: nenhum trecho com V > 3.0

PASSO 6: CÁLCULO DE PERDA DE CARGA (por trecho)
  ├─ Para cada trecho:
  │   ├─ J = 8.69E6 × Q^1.75 / D_int^4.75
  │   ├─ ΔH = J × L × K
  │   └─ Registrar J e ΔH do trecho
  └─ Totalizar ΔH_total por caminho

PASSO 7: IDENTIFICAÇÃO DO CAMINHO CRÍTICO
  ├─ Para cada ponto de consumo:
  │   ├─ H_geom = Z_reserv - Z_ponto
  │   ├─ ΣΔH = soma de ΔH dos trechos no caminho
  │   ├─ P_din = H_geom - ΣΔH
  │   └─ Registrar P_din do ponto
  └─ Ponto com menor P_din = caminho crítico

PASSO 8: VERIFICAÇÃO DE PRESSÃO
  ├─ Para cada ponto:
  │   ├─ SE P_din < 3.0 → ERRO CRÍTICO
  │   ├─ SE P_din < 1.0 → ERRO CRÍTICO (pressurizador)
  │   ├─ SE P_est > 40.0 → ALERTA MÉDIO (válvula redutora)
  │   └─ SENÃO → OK
  └─ SE algum ponto falhou: listar todos os pontos com problema

PASSO 9: AJUSTE (se necessário)
  ├─ SE pressão insuficiente:
  │   ├─ Opção 1: aumentar diâmetros no caminho crítico
  │   ├─ Opção 2: elevar reservatório (alterar Z_reserv)
  │   ├─ Opção 3: instalar pressurizador (decisão humana)
  │   └─ Recalcular a partir do PASSO 5
  └─ SE velocidade excedida: já tratado no PASSO 5

PASSO 10: APLICAÇÃO NO MODELO
  ├─ Para cada trecho:
  │   ├─ Atualizar parâmetro de diâmetro do Pipe no Revit
  │   └─ Registrar log de dimensionamento
  └─ Gerar relatório completo

FIM
```

### Estrutura do relatório de dimensionamento

```
| Trecho | Aparelhos | ΣP | Q (L/s) | DN (mm) | V (m/s) | L (m) | J (m/m) | ΔH (m) |
|--------|----------|-----|---------|---------|---------|-------|---------|--------|

Caminho crítico: Reserv → trecho X → trecho Y → ponto Z
H_geométrica: 5.50 m
ΣΔH: 1.82 m
P_disponível: 3.68 m.c.a. ✅ (≥ 3.0)
```

---

## 10. Limitações e Interpretações

### 10.1 Pontos ambíguos da norma

| Ponto | Ambiguidade | Decisão adotada pelo plugin |
|-------|------------|----------------------------|
| Percentual de perdas localizadas | Norma permite 10% a 20% conforme experiência | Adotar 20% (K=1.20) como padrão conservador |
| Pressão mínima no chuveiro | Norma diz 1.0 m.c.a., prática exige ≥ 3.0 | Adotar 3.0 m.c.a. como padrão (configurável) |
| Velocidade mínima | Norma não define explicitamente | Alertar quando V < 0.5 m/s (boa prática) |
| DN do barrilete | Norma não define mínimo explícito | Adotar DN ≥ 25mm como boa prática |
| Diâmetro que não aumenta para jusante | Norma menciona como regra geral | Implementar como alerta Médio (não bloqueia) |

### 10.2 Decisões que dependem de engenharia

| Decisão | Por que não automatizar |
|---------|----------------------|
| Necessidade de pressurizador | Análise de custo-benefício e alternativas construtivas |
| Instalação de válvula redutora | Depende de análise de custo e ponto de instalação |
| Diâmetro acima do calculado (margem) | Decisão comercial (custo futuro de ampliação) |
| Tipo de vaso (caixa vs. válvula) | Decisão do cliente/projetista com impacto em todo o projeto |
| Material alternativo | CPVC, PPR → coeficientes diferentes na fórmula |

### 10.3 Simplificações para automação

| Simplificação | Justificativa | Impacto |
|--------------|--------------|---------|
| K fixo em 1.20 | Evita contagem de cada conexão | ±10% na perda de carga (conservador) |
| Q = 0.30 × √ΣP sempre | Método único simplifica código | Adequado para residencial |
| Pressão uniforme de 3.0 m.c.a. | Simplifica verificação | Mais conservador que norma |
| Apenas PVC soldável | Material único simplifica tabelas | Cobre 95% dos projetos residenciais |
| Apenas caixa acoplada | Evita complexidade da VD | Cobre 90% dos projetos residenciais |

### 10.4 Quando o plugin deve parar e pedir decisão humana

```
1. Pressão < 3.0 m.c.a. em qualquer ponto → parar, apresentar opções
2. Pressão estática > 40 m.c.a. → parar, sugerir válvula redutora
3. Nenhum diâmetro comercial atende V ≤ 3.0 → parar (vazão muito alta)
4. Modelo com válvula de descarga detectada → confirmar tipo
5. Pressão negativa (ponto acima do reservatório) → parar
```
