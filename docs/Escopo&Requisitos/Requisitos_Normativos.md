# Requisitos Normativos Obrigatórios — Plugin Hidráulico Revit 2026

> Tradução técnica das normas NBR 5626 e NBR 8160 em regras implementáveis por software para automação de projetos hidráulicos residenciais.

---

## 1. Visão Geral das Normas

### 1.1 NBR 5626 — Instalações Prediais de Água Fria

| Campo | Descrição |
|-------|-----------|
| **Objetivo** | Estabelecer requisitos para projeto, execução e manutenção de instalações prediais de água fria |
| **Escopo** | Ramais prediais, reservatórios, redes de distribuição interna, sub-ramais e pontos de utilização |
| **Aplicação no plugin** | Dimensionamento de vazão, seleção de diâmetro, verificação de pressão, velocidade máxima, perda de carga |

### 1.2 NBR 8160 — Sistemas Prediais de Esgoto Sanitário

| Campo | Descrição |
|-------|-----------|
| **Objetivo** | Estabelecer requisitos para projeto, execução e manutenção de sistemas de esgoto sanitário predial |
| **Escopo** | Ramais de descarga, ramais de esgoto, tubos de queda, subcoletores, ventilação, caixas e dispositivos |
| **Aplicação no plugin** | Dimensionamento por UHC, declividades, diâmetros mínimos, regras de ventilação, desconectores |

### 1.3 Relação entre as normas

As normas são complementares e atuam sobre o mesmo edifício:

- NBR 5626 rege o **abastecimento** (água entra)
- NBR 8160 rege a **coleta** (água sai)
- O ponto de conexão é o **aparelho sanitário** (recebe AF, gera ES)
- Ambas exigem que o projeto considere a outra disciplina (coordenação)

O plugin deve aplicar ambas simultaneamente sobre o mesmo modelo.

---

## 2. Requisitos Normativos — Água Fria (NBR 5626)

### 2.1 Sistema de Abastecimento

#### Tipos de sistema

| Tipo | Descrição | Uso residencial |
|------|-----------|----------------|
| **Direto** | Alimentação direta da rede pública | Raro em residencial brasileiro |
| **Indireto** | Alimentação via reservatório (inferior + superior ou apenas superior) | **Padrão adotado pelo plugin** |
| **Misto** | Parte direta, parte via reservatório | Ocasional |

#### Regras para sistema indireto com reservatório superior

| Regra | Descrição | Parâmetro |
|-------|-----------|-----------|
| RN-AF-01 | O reservatório superior deve estar acima de todos os pontos de utilização | Cota do fundo do reservatório > cota de qualquer ponto de consumo |
| RN-AF-02 | A alimentação dos pontos ocorre por gravidade (pressão estática) | Pressão = diferença de altura × peso específico da água |
| RN-AF-03 | O reservatório deve ter capacidade para consumo diário + reserva | Volume = população × consumo diário per capita |
| RN-AF-04 | Deve haver registro de gaveta na saída do reservatório (barrilete) | 1 registro geral na saída |
| RN-AF-05 | Deve haver registro de gaveta na entrada de cada ambiente | 1 registro por cômodo molhado |

#### Consumo diário per capita (residencial)

| Tipo de edificação | Consumo (L/pessoa/dia) |
|-------------------|----------------------|
| Residência (padrão médio/alto) | 150 |
| Apartamento (padrão econômico) | 120 |
| Casa popular | 100 |

**Padrão do plugin:** 150 L/pessoa/dia (ajustável na configuração).

### 2.2 Pressão

#### Pressões de referência

| Parâmetro | Valor | Regra |
|-----------|-------|-------|
| Pressão estática mínima em qualquer ponto | 0.5 m.c.a. | Mínimo absoluto (norma) |
| Pressão dinâmica mínima em ponto de utilização | **1.0 m.c.a.** | Mínimo para aparelhos comuns |
| Pressão dinâmica mínima no chuveiro | **1.0 m.c.a.** | Mínimo normativo |
| Pressão mínima recomendada (prática) | **3.0 m.c.a.** | Valor prático para bom funcionamento |
| Pressão estática máxima em qualquer ponto | **40 m.c.a.** | Acima disso: exige válvula redutora |

**Padrão do plugin:** pressão mínima = 3.0 m.c.a. (conservador, boa prática).

#### Cálculo de pressão disponível

```
P_disponível = H_geométrica - ΔH_total

Onde:
  H_geométrica = cota do nível d'água do reservatório - cota do ponto de utilização [m]
  ΔH_total = soma de todas as perdas de carga no caminho (distribuídas + localizadas) [m]
  P_disponível deve ser ≥ P_mínima (3.0 m.c.a.)
```

#### Pressão estática máxima

```
P_estática = H_geométrica (sem considerar perdas)

SE P_estática > 40 m.c.a. em qualquer ponto:
  → Necessário instalar válvula redutora de pressão
  → Alerta Médio no plugin (não automatiza a válvula)
```

### 2.3 Dimensionamento

#### Pesos relativos dos aparelhos sanitários

| Aparelho | Peso (P) | Vazão de projeto (L/s) | Diâmetro mínimo do sub-ramal (mm) |
|----------|---------|----------------------|----------------------------------|
| Vaso sanitário com caixa acoplada | 0.3 | 0.15 | 20 |
| Vaso sanitário com válvula de descarga | 32.0 | 1.70 | 50 |
| Lavatório | 0.5 | 0.15 | 20 |
| Bidê | 0.1 | 0.10 | 20 |
| Chuveiro | 0.5 | 0.20 | 20 |
| Banheira | 1.0 | 0.30 | 25 |
| Pia de cozinha | 0.7 | 0.25 | 20 |
| Tanque de lavar | 0.7 | 0.25 | 25 |
| Máquina de lavar roupa | 1.0 | 0.30 | 25 |
| Torneira de jardim | 0.5 | 0.20 | 20 |

**Padrão do plugin:** vaso com caixa acoplada (peso 0.3) — residencial brasileiro.

#### Vazão provável (método probabilístico)

```
Q = 0.3 × √(ΣP)  [L/s]

Onde:
  ΣP = soma dos pesos dos aparelhos no trecho (de jusante para montante)
  Q = vazão provável no trecho
```

Essa fórmula considera que nem todos os aparelhos são usados simultaneamente. É a fórmula padrão da NBR 5626 para instalações com até 400 aparelhos.

#### Seleção de diâmetro

```
Critério: V ≤ V_máx (3.0 m/s)

V = Q / A = Q / (π × D² / 4)

Portanto:
  D_mínimo = √(4 × Q / (π × V_máx))

Selecionar o menor diâmetro comercial ≥ D_mínimo.
```

#### Diâmetros comerciais (PVC soldável — água fria)

| DN (mm) | Diâmetro externo (mm) | Diâmetro interno (mm) | Área interna (mm²) |
|---------|----------------------|----------------------|-------------------|
| 20 | 20 | 17.0 | 227 |
| 25 | 25 | 21.6 | 366 |
| 32 | 32 | 27.8 | 607 |
| 40 | 40 | 35.2 | 973 |
| 50 | 50 | 44.0 | 1521 |
| 60 | 60 | 53.4 | 2240 |
| 75 | 75 | 66.6 | 3483 |
| 85 | 85 | 75.6 | 4488 |
| 110 | 110 | 97.8 | 7509 |

#### Velocidade máxima

| Parâmetro | Valor |
|-----------|-------|
| Velocidade máxima na tubulação | **3.0 m/s** |
| Velocidade recomendada | 1.0 — 2.5 m/s |
| Abaixo de 0.5 m/s | Risco de sedimentação (alerta Leve) |

#### Perda de carga

**Fórmula de Fair-Whipple-Hsiao (para PVC):**

```
J = 8.69 × 10⁶ × Q^1.75 / D^4.75  [m/m]

Onde:
  J = perda de carga unitária [m por metro de tubulação]
  Q = vazão [L/s]
  D = diâmetro interno [mm]
```

**Perdas localizadas (método dos comprimentos equivalentes):**

```
ΔH_total = J × L × 1.20

Onde:
  1.20 = fator que adiciona 20% ao comprimento real para compensar perdas localizadas
  L = comprimento real do trecho [m]
```

Nota: O fator de 20% é uma aproximação aceita pela prática. Para maior precisão, usar tabela de comprimentos equivalentes por tipo de conexão.

### 2.4 Materiais e Tubulações

| Regra | Descrição |
|-------|-----------|
| RN-AF-06 | Material padrão residencial: PVC soldável (marrom) |
| RN-AF-07 | Tubulação de água fria deve ser diferenciada visualmente de água quente (quando houver) |
| RN-AF-08 | Todas as conexões devem ser do mesmo material da tubulação |
| RN-AF-09 | Registros: gaveta para bloqueio, pressão para regulagem de vazão |

**No plugin:** Pipe Type configurado como "PVC Soldável - Água Fria" com diâmetros da tabela acima.

### 2.5 Disposição e Traçado

| Regra | Descrição | Impacto no plugin |
|-------|-----------|-------------------|
| RN-AF-10 | Ramais devem seguir percurso mais curto possível | Algoritmo de otimização de rota |
| RN-AF-11 | Tubulações devem ser acessíveis para manutenção | Não embutir em locais inacessíveis |
| RN-AF-12 | Registro de gaveta obrigatório na entrada de cada ambiente | Plugin insere automaticamente |
| RN-AF-13 | Sub-ramais devem ter diâmetro mínimo conforme tabela de aparelhos | Validação automática |
| RN-AF-14 | Diâmetro nunca deve aumentar no sentido do escoamento (montante → jusante) | Validação automática |
| RN-AF-15 | Deve-se evitar cruzamento com redes de esgoto | Verificação de interferência |

---

## 3. Requisitos Normativos — Esgoto (NBR 8160)

### 3.1 Tipos de Sistema

#### Sistema separador absoluto

| Princípio | Descrição |
|-----------|-----------|
| **Separador absoluto** | Esgoto sanitário e águas pluviais em sistemas TOTALMENTE separados |
| **Obrigatório** | No Brasil, é o sistema adotado por norma. O plugin trata apenas esgoto sanitário. |

#### Organização do sistema de esgoto

| Componente | Função | Exemplo |
|-----------|--------|---------|
| **Ramal de descarga** | Trecho entre o aparelho e o ramal de esgoto | Vaso → ramal |
| **Ramal de esgoto** | Trecho que recebe ramais de descarga e conduz ao tubo de queda | Ramais do banheiro → TQ |
| **Tubo de queda (TQ)** | Coluna vertical que recebe ramais de esgoto de todos os pavimentos | TQ do banheiro |
| **Subcoletor** | Trecho horizontal que recebe TQ e conduz à caixa de inspeção externa | TQ → CI externa |
| **Coletor predial** | Trecho entre a última CI e a rede pública | CI → rede |

### 3.2 Declividades

#### Valores obrigatórios

| Diâmetro nominal | Declividade mínima | Declividade máxima recomendada |
|-----------------|-------------------|-------------------------------|
| DN ≤ 75 mm | **2% (2 cm/m)** | 5% |
| DN ≥ 100 mm | **1% (1 cm/m)** | 5% |

```
Regra lógica:
SE DN <= 75:
  declividade_min = 0.02
SENÃO:
  declividade_min = 0.01

Declividade máxima (qualquer DN) = 0.05

Desnível = comprimento_trecho × declividade
```

| Regra | Descrição |
|-------|-----------|
| RN-ES-01 | Todos os trechos horizontais de esgoto devem ter declividade ≥ mínima |
| RN-ES-02 | Declividade > 5% deve gerar alerta (escoamento turbulento excessivo) |
| RN-ES-03 | Trechos verticais (tubos de queda) não possuem inclinação |

### 3.3 Ventilação

#### Tipos de ventilação

| Tipo | Descrição | Quando usar |
|------|-----------|-------------|
| **Ventilação primária** | Prolongamento do tubo de queda acima da cobertura | Obrigatório em todo TQ |
| **Ventilação secundária** | Coluna de ventilação dedicada, paralela ao TQ | Quando ramal de esgoto é longo |
| **Ventilação individual** | Ramal de ventilação de um único aparelho | Quando aparelho está distante do TQ |
| **Ventilação de alívio** | Ligação entre coluna de ventilação e TQ em pavimentos intermediários | Edifícios altos (> 4 pavimentos) |

#### Regras obrigatórias

| Regra | Descrição | Parâmetro |
|-------|-----------|-----------|
| RN-VE-01 | Todo tubo de queda deve ter ventilação primária (prolongamento até acima da cobertura) | TQ termina 30 cm acima da cobertura |
| RN-VE-02 | Terminal de ventilação deve estar a ≥ 30 cm acima da cobertura | Cota mínima |
| RN-VE-03 | Terminal de ventilação deve ficar a ≥ 4 m de janelas, portas e entradas de ar | Distância mínima |
| RN-VE-04 | Ramal de ventilação deve ser ligado acima da borda do aparelho mais alto do pavimento | Elevação mínima |
| RN-VE-05 | Ramal de ventilação deve ter inclinação ascendente em direção à coluna de ventilação | Nunca desce |
| RN-VE-06 | Diâmetro da coluna de ventilação: mínimo 2/3 do diâmetro do TQ correspondente | D_vent ≥ (2/3) × D_tq |

#### Dimensionamento da coluna de ventilação

| DN do tubo de queda (mm) | DN mínimo da ventilação (mm) |
|--------------------------|----------------------------|
| 50 | 40 |
| 75 | 50 |
| 100 | 75 |
| 150 | 100 |

#### Distância máxima entre aparelho e ventilação

| DN do ramal de descarga | Distância máxima sem ventilação individual (m) |
|------------------------|-----------------------------------------------|
| 40 | 1.0 |
| 50 | 1.2 |
| 75 | 1.8 |
| 100 | 2.4 |

```
Regra lógica:
SE distancia_aparelho_ao_TQ > distancia_maxima_para_DN:
  → Necessário ramal de ventilação individual
  → Plugin gera alerta Médio e sugere inserção
```

### 3.4 Dimensionamento

#### Unidades Hunter de Contribuição (UHC)

| Aparelho | UHC |
|----------|-----|
| Vaso sanitário (caixa acoplada) | 6 |
| Vaso sanitário (válvula) | 6 |
| Lavatório | 1 |
| Bidê | 1 |
| Chuveiro | 2 |
| Banheira | 2 |
| Pia de cozinha | 3 |
| Tanque de lavar | 3 |
| Máquina de lavar roupa | 3 |
| Ralo sifonado (150mm) | 1 |
| Ralo sifonado (100mm) | 1 |

#### Diâmetros mínimos de ramais de descarga

| Aparelho | DN mínimo (mm) |
|----------|---------------|
| Vaso sanitário | **100** |
| Lavatório | 40 |
| Bidê | 40 |
| Chuveiro | 40 |
| Banheira | 40 |
| Pia de cozinha | 50 |
| Tanque de lavar | 40 |
| Máquina de lavar | 50 |
| Ralo sifonado | 40 |

#### Dimensionamento de ramais de esgoto (pelo somatório de UHC)

| Σ UHC | DN mínimo (mm) | Declividade mínima |
|-------|---------------|-------------------|
| ≤ 3 | 40 | 2% |
| ≤ 6 | 50 | 2% |
| ≤ 20 | 75 | 2% |
| ≤ 160 | 100 | 1% |
| ≤ 620 | 150 | 1% |

#### Dimensionamento de tubos de queda

| Σ UHC (todos os pav.) | DN mínimo (mm) |
|-----------------------|---------------|
| ≤ 2 | 40 |
| ≤ 10 | 50 |
| ≤ 30 | 75 |
| ≤ 240 | 100 |
| ≤ 960 | 150 |

#### Dimensionamento de subcoletores

| Σ UHC | DN mínimo (mm) | Declividade |
|-------|---------------|------------|
| ≤ 21 | 100 | 1% |
| ≤ 180 | 150 | 1% |

#### Regras de dimensionamento

| Regra | Descrição |
|-------|-----------|
| RN-ES-04 | Diâmetro NUNCA diminui no sentido do escoamento |
| RN-ES-05 | Ramal de descarga do vaso sanitário: mínimo 100mm (obrigatório) |
| RN-ES-06 | Subcoletor: diâmetro mínimo absoluto é 100mm |
| RN-ES-07 | Tubo de queda: diâmetro não pode ser menor que o maior ramal de esgoto que descarrega nele |

### 3.5 Disposição e Traçado

| Regra | Descrição | Impacto no plugin |
|-------|-----------|-------------------|
| RN-ES-08 | Caixa sifonada obrigatória em banheiros para coleta de água do piso | Plugin insere automaticamente |
| RN-ES-09 | Caixa de gordura obrigatória na saída da pia de cozinha | Plugin insere automaticamente |
| RN-ES-10 | Caixa de inspeção em mudança de direção > 90° e a cada ~15m | Plugin sugere posição |
| RN-ES-11 | Vaso sanitário deve ter ramal de descarga independente (não passa pela CX sifonada) | Topologia separada |
| RN-ES-12 | Ramais de esgoto devem convergir para o tubo de queda | Rota em direção ao TQ |
| RN-ES-13 | Curvas de 90° devem ser evitadas — usar 2 curvas de 45° | Validação de fittings |
| RN-ES-14 | Máximo 2 vasos sanitários por ramal de esgoto antes de tubo de queda | Limitação de UHC |

---

## 4. Regras Convertidas para Software

### 4.1 Água Fria

| ID | Descrição normativa | Regra lógica | Parâmetros | Validação |
|----|---------------------|-------------|-----------|-----------|
| SW-AF-01 | Pressão mínima em ponto de utilização | `SE P_disponivel < 3.0 ENTÃO erro Crítico` | P_min = 3.0 m.c.a. | Calcular P_disponível para cada ponto. Se < 3.0 → bloqueia. |
| SW-AF-02 | Pressão máxima em qualquer ponto | `SE P_estatica > 40 ENTÃO alerta Médio` | P_max = 40 m.c.a. | Calcular P_estática para cada ponto. Se > 40 → alerta. |
| SW-AF-03 | Velocidade máxima | `SE V > 3.0 ENTÃO aumentar diâmetro` | V_max = 3.0 m/s | Para cada trecho, calcular V = Q/A. |
| SW-AF-04 | Vazão provável | `Q = 0.3 × sqrt(soma_pesos)` | Pesos por aparelho (tabela) | Somar pesos de jusante para montante. |
| SW-AF-05 | Diâmetro mínimo do sub-ramal | `SE D < D_min_aparelho ENTÃO erro Crítico` | D_min por aparelho (tabela) | Verificar cada sub-ramal. |
| SW-AF-06 | Diâmetro não aumenta no sentido do escoamento | `SE D_jusante > D_montante ENTÃO erro Médio` | D de cada trecho | Percorrer rede de montante para jusante. |
| SW-AF-07 | Registro na entrada do ambiente | `SE ambiente_nao_tem_registro ENTÃO alerta Médio` | 1 registro por ambiente | Verificar presença de registro. |
| SW-AF-08 | Perda de carga total | `DH = J × L × 1.20` | J (Fair-Whipple-Hsiao), L | Calcular por trecho, somar no caminho crítico. |

### 4.2 Esgoto

| ID | Descrição normativa | Regra lógica | Parâmetros | Validação |
|----|---------------------|-------------|-----------|-----------|
| SW-ES-01 | Declividade mínima DN ≤ 75 | `SE DN <= 75 E decliv < 0.02 ENTÃO erro Crítico` | decliv_min = 2% | Verificar cada trecho horizontal. |
| SW-ES-02 | Declividade mínima DN ≥ 100 | `SE DN >= 100 E decliv < 0.01 ENTÃO erro Crítico` | decliv_min = 1% | Verificar cada trecho horizontal. |
| SW-ES-03 | Declividade máxima | `SE decliv > 0.05 ENTÃO alerta Leve` | decliv_max = 5% | Verificar cada trecho. |
| SW-ES-04 | Diâmetro não diminui no escoamento | `SE DN_jusante < DN_montante ENTÃO erro Crítico` | DN de cada trecho | Percorrer rede de montante para jusante. |
| SW-ES-05 | Vaso sanitário: ramal de descarga mínimo | `SE DN_ramal_vaso < 100 ENTÃO erro Crítico` | DN_min_vaso = 100mm | Verificar cada ramal de vaso. |
| SW-ES-06 | Subcoletor: diâmetro mínimo | `SE DN_subcoletor < 100 ENTÃO erro Crítico` | DN_min_sub = 100mm | Verificar subcoletores. |
| SW-ES-07 | Dimensionamento por UHC | `DN = tabela_UHC_DN(soma_UHC)` | Tabela UHC→DN | Para cada trecho, somar UHCs e consultar tabela. |
| SW-ES-08 | Caixa sifonada em banheiro | `SE banheiro_sem_CX_sifonada ENTÃO alerta Médio` | 1 CX por banheiro | Verificar presença por ambiente. |
| SW-ES-09 | Caixa de gordura na cozinha | `SE cozinha_sem_CX_gordura ENTÃO erro Médio` | 1 CX por cozinha | Verificar presença. |
| SW-ES-10 | Curva de 90° em esgoto | `SE fitting_90_em_esgoto ENTÃO alerta Leve` | Verificar tipo de fitting | Sugerir 2×45°. |

### 4.3 Ventilação

| ID | Descrição normativa | Regra lógica | Parâmetros | Validação |
|----|---------------------|-------------|-----------|-----------|
| SW-VE-01 | TQ deve ter ventilação primária | `SE TQ_sem_prolongamento ENTÃO erro Crítico` | Para cada TQ | Verificar existência de prolongamento. |
| SW-VE-02 | Terminal ≥ 30cm acima da cobertura | `SE cota_terminal < cota_cobertura + 0.30 ENTÃO erro Médio` | H_min = 0.30m | Verificar cota. |
| SW-VE-03 | DN ventilação ≥ 2/3 DN do TQ | `SE DN_vent < (2/3 × DN_tq) ENTÃO erro Médio` | Relação 2/3 | Verificar diâmetros. |
| SW-VE-04 | Ramal de ventilação sempre sobe | `SE trecho_vent_desce ENTÃO erro Crítico` | Elevação | Verificar Z_final > Z_inicial em cada trecho. |
| SW-VE-05 | Distância máxima sem ventilação individual | `SE dist > dist_max_para_DN ENTÃO necessita_vent_individual` | Tabela distâncias | Calcular distância ao TQ. |

---

## 5. Parâmetros Técnicos Padronizados

### 5.1 JSON de configuração normativa

```json
{
  "normas": {
    "agua_fria": "NBR 5626",
    "esgoto": "NBR 8160"
  },

  "agua_fria": {
    "sistema": "reservatorio_superior",
    "pressao_minima_mca": 3.0,
    "pressao_maxima_mca": 40.0,
    "velocidade_maxima_ms": 3.0,
    "altura_reservatorio_m": 6.0,
    "consumo_per_capita_L_dia": 150,
    "fator_perdas_localizadas": 1.20,
    "material": "PVC_soldavel",
    "formula_vazao": "Q = 0.3 * sqrt(soma_pesos)",
    "formula_perda_carga": "Fair-Whipple-Hsiao"
  },

  "esgoto": {
    "sistema": "separador_absoluto",
    "com_ventilacao": true,
    "declividade_ate_75mm": 0.02,
    "declividade_100mm_ou_mais": 0.01,
    "declividade_maxima": 0.05,
    "diametro_minimo_subcoletor_mm": 100,
    "diametro_minimo_ramal_vaso_mm": 100,
    "material": "PVC_esgoto"
  },

  "ventilacao": {
    "altura_terminal_acima_cobertura_m": 0.30,
    "distancia_terminal_janela_m": 4.0,
    "fator_diametro_vent_vs_tq": 0.667
  }
}
```

### 5.2 JSON de pesos de aparelhos (AF)

```json
{
  "pesos_aparelhos": {
    "vaso_caixa_acoplada": { "peso": 0.3, "vazao_Ls": 0.15, "dn_min_mm": 20 },
    "lavatorio": { "peso": 0.5, "vazao_Ls": 0.15, "dn_min_mm": 20 },
    "bide": { "peso": 0.1, "vazao_Ls": 0.10, "dn_min_mm": 20 },
    "chuveiro": { "peso": 0.5, "vazao_Ls": 0.20, "dn_min_mm": 20 },
    "banheira": { "peso": 1.0, "vazao_Ls": 0.30, "dn_min_mm": 25 },
    "pia_cozinha": { "peso": 0.7, "vazao_Ls": 0.25, "dn_min_mm": 20 },
    "tanque": { "peso": 0.7, "vazao_Ls": 0.25, "dn_min_mm": 25 },
    "maquina_lavar": { "peso": 1.0, "vazao_Ls": 0.30, "dn_min_mm": 25 },
    "torneira_jardim": { "peso": 0.5, "vazao_Ls": 0.20, "dn_min_mm": 20 }
  }
}
```

### 5.3 JSON de UHC (ES)

```json
{
  "uhc_aparelhos": {
    "vaso_caixa_acoplada": { "uhc": 6, "dn_min_ramal_mm": 100 },
    "lavatorio": { "uhc": 1, "dn_min_ramal_mm": 40 },
    "bide": { "uhc": 1, "dn_min_ramal_mm": 40 },
    "chuveiro": { "uhc": 2, "dn_min_ramal_mm": 40 },
    "banheira": { "uhc": 2, "dn_min_ramal_mm": 40 },
    "pia_cozinha": { "uhc": 3, "dn_min_ramal_mm": 50 },
    "tanque": { "uhc": 3, "dn_min_ramal_mm": 40 },
    "maquina_lavar": { "uhc": 3, "dn_min_ramal_mm": 50 },
    "ralo_sifonado": { "uhc": 1, "dn_min_ramal_mm": 40 }
  }
}
```

### 5.4 JSON de diâmetros comerciais

```json
{
  "diametros_comerciais_af_mm": [20, 25, 32, 40, 50, 60, 75, 85, 110],
  "diametros_comerciais_es_mm": [40, 50, 75, 100, 150],

  "tabela_uhc_para_dn_ramal": [
    { "uhc_max": 3, "dn_mm": 40 },
    { "uhc_max": 6, "dn_mm": 50 },
    { "uhc_max": 20, "dn_mm": 75 },
    { "uhc_max": 160, "dn_mm": 100 },
    { "uhc_max": 620, "dn_mm": 150 }
  ],

  "tabela_uhc_para_dn_tq": [
    { "uhc_max": 2, "dn_mm": 40 },
    { "uhc_max": 10, "dn_mm": 50 },
    { "uhc_max": 30, "dn_mm": 75 },
    { "uhc_max": 240, "dn_mm": 100 },
    { "uhc_max": 960, "dn_mm": 150 }
  ],

  "tabela_uhc_para_dn_subcoletor": [
    { "uhc_max": 21, "dn_mm": 100 },
    { "uhc_max": 180, "dn_mm": 150 }
  ]
}
```

### 5.5 JSON de alturas de instalação

```json
{
  "alturas_instalacao_m": {
    "vaso_sanitario": { "af": 0.20, "es": 0.00 },
    "lavatorio": { "af": 0.60, "es": 0.55 },
    "chuveiro": { "af": 2.00, "es": 0.00 },
    "pia_cozinha": { "af": 1.00, "es": 0.55 },
    "tanque": { "af": 1.00, "es": 0.55 },
    "maquina_lavar": { "af": 0.75, "es": 0.10 },
    "torneira_jardim": { "af": 0.60, "es": null },
    "registro_gaveta": { "af": 1.50, "es": null },
    "registro_pressao": { "af": 1.50, "es": null }
  }
}
```

---

## 6. Regras de Validação

### 6.1 Validações automáticas obrigatórias

| ID | Validação | Momento | Nível se falha |
|----|----------|---------|---------------|
| VAL-01 | Pressão ≥ 3 m.c.a. em todos os pontos de AF | Após dimensionamento | Crítico |
| VAL-02 | Pressão ≤ 40 m.c.a. em todos os pontos de AF | Após dimensionamento | Médio |
| VAL-03 | Velocidade ≤ 3 m/s em todos os trechos AF | Após dimensionamento | Crítico |
| VAL-04 | Declividade ≥ mínima em todos os trechos ES | Após aplicação de inclinação | Crítico |
| VAL-05 | Declividade ≤ 5% em todos os trechos ES | Após aplicação de inclinação | Leve |
| VAL-06 | Diâmetro não diminui no sentido do escoamento (ES) | Após dimensionamento ES | Crítico |
| VAL-07 | Ramal de vaso ≥ 100mm | Após dimensionamento ES | Crítico |
| VAL-08 | Subcoletor ≥ 100mm | Após dimensionamento ES | Crítico |
| VAL-09 | CX sifonada em cada banheiro | Após traçado ES | Médio |
| VAL-10 | CX gordura na cozinha | Após traçado ES | Médio |
| VAL-11 | Ventilação primária em cada TQ | Após traçado de ventilação | Crítico |
| VAL-12 | DN ventilação ≥ 2/3 DN do TQ | Após dimensionamento | Médio |
| VAL-13 | Trecho de ventilação sempre ascendente | Após traçado de ventilação | Crítico |
| VAL-14 | Registro de gaveta na entrada de cada ambiente | Após traçado AF | Médio |
| VAL-15 | Todos os equipamentos conectados à rede | Após criação de sistemas | Crítico |
| VAL-16 | Todos os elementos atribuídos a um sistema MEP | Após criação de sistemas | Médio |

### 6.2 Classificação de não conformidade

| Nível | Comportamento | Exemplos |
|-------|--------------|---------|
| **Crítico** | Pipeline bloqueado. Não avança sem correção. | Pressão < 3 m.c.a., vaso sem 100mm, sem ventilação primária |
| **Médio** | Permite continuar com aceite explícito do usuário. | Pressão > 40 m.c.a. (recomenda válvula), sem CX gordura |
| **Leve** | Apenas informativo, não impede avanço. | Declividade > 5%, curva 90° em esgoto |

---

## 7. Limitações de Interpretação

### 7.1 Pontos que exigem interpretação humana

| Ponto normativo | Por que o software não deve decidir sozinho |
|-----------------|---------------------------------------------|
| Necessidade de pressurizador | Envolve análise de custo-benefício e alternativas (aumentar reservatório, usar sistema misto) |
| Posição do reservatório | Depende de projeto arquitetônico e estrutural (carga sobre laje) |
| Sistema misto (direto + indireto) | Decisão de engenharia com múltiplas variáveis |
| Posição da caixa de inspeção externa | Depende de implantação, terreno, acesso, legislação municipal |
| Necessidade de ventilação de alívio | Cálculo envolve altura do edifício e análise de pressão pneumática |
| Material alternativo (CPVC, PPR, cobre) | Decisão de custo, disponibilidade, preferência do cliente |
| Exceção de declividade impossível | Quando não há espaço: opções incluem bomba, rebaixo de piso, mudança de rota |
| Curva de 90° em esgoto (quando aceitável) | Em ramais de descarga curtos (< 1m), norma permite com ressalvas |
| Dimensionamento com válvula de descarga | Peso 32.0 altera drasticamente o dimensionamento — decisão de projeto |
| Compartilhamento de caixa sifonada entre ambientes | Depende de layout e análise específica |

### 7.2 Regra geral para o plugin

```
QUANDO situação exigir interpretação normativa:
  1. Plugin identifica a situação
  2. Gera log Médio com descrição clara
  3. NÃO toma decisão automática
  4. Apresenta opções ao usuário (quando possível)
  5. Aguarda decisão humana antes de prosseguir
```

O plugin NUNCA deve:
- Violar um requisito Crítico da norma silenciosamente
- Escolher alternativas com implicação de custo sem consultar o usuário
- Assumir que uma exceção normativa se aplica sem confirmação
