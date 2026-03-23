# Dataset de Teste — Variações de Nomenclatura de Rooms

> Conjunto de dados realistas para validação do classificador automático de ambientes. Simula nomenclaturas reais encontradas em projetos de diferentes escritórios.

---

## 📋 Índice

- [Banheiro](#1-banheiro)
- [Cozinha](#2-cozinha)
- [Área de Serviço](#3-área-de-serviço)
- [Lavabo](#4-lavabo)
- [Quarto](#5-quarto)
- [Sala](#6-sala)
- [Corredor / Circulação](#7-corredor--circulação)
- [Garagem](#8-garagem)
- [Varanda / Sacada](#9-varanda--sacada)
- [Casos Críticos](#10-casos-críticos-e-ambíguos)
- [Uso no Sistema](#11-uso-no-sistema)
- [Estatísticas](#12-estatísticas)

---

## Legenda

| Coluna | Significado |
|--------|------------|
| **Input** | Nome do Room como vem do modelo Revit |
| **Esperado** | `RoomType` que o classificador deve retornar |
| **Tipo** | `C` = Correto, `A` = Abreviação, `E` = Erro digitação, `S` = Sinônimo, `M` = Misto, `X` = Ambíguo |
| **Confiança** | Nível esperado: Alta (≥90%), Média (60-89%), Baixa (30-59%) |

---

## 1. Banheiro

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 1 | `Banheiro` | `Bathroom` | C | Alta |
| 2 | `Banheiro Social` | `Bathroom` | C | Alta |
| 3 | `Banheiro - Social` | `Bathroom` | C | Alta |
| 4 | `Banheiro - Serviço` | `Bathroom` | C | Alta |
| 5 | `Banheiro Suíte` | `MasterBathroom` | C | Alta |
| 6 | `Banheiro - Suíte` | `MasterBathroom` | C | Alta |
| 7 | `Banheiro da Suíte` | `MasterBathroom` | C | Alta |
| 8 | `Banheiro Suite` | `MasterBathroom` | C | Alta |
| 9 | `Banheiro Suíte Master` | `MasterBathroom` | C | Alta |
| 10 | `Banh.` | `Bathroom` | A | Média |
| 11 | `Banh. Social` | `Bathroom` | A | Média |
| 12 | `Banh Social` | `Bathroom` | A | Média |
| 13 | `Ban. Suíte` | `MasterBathroom` | A | Média |
| 14 | `BWC` | `Bathroom` | S | Média |
| 15 | `BWC Social` | `Bathroom` | S | Média |
| 16 | `WC` | `Bathroom` | S | Média |
| 17 | `W.C.` | `Bathroom` | S | Média |
| 18 | `Sanitário` | `Bathroom` | S | Média |
| 19 | `Banho` | `Bathroom` | S | Média |
| 20 | `Banhero` | `Bathroom` | E | Média |
| 21 | `Banhero Social` | `Bathroom` | E | Média |
| 22 | `Bnaherio` | `Bathroom` | E | Baixa |
| 23 | `Banheiro 01` | `Bathroom` | M | Alta |
| 24 | `Banheiro 02` | `Bathroom` | M | Alta |
| 25 | `Banheiro - 1` | `Bathroom` | M | Alta |
| 26 | `BANHEIRO` | `Bathroom` | M | Alta |
| 27 | `banheiro social` | `Bathroom` | M | Alta |
| 28 | `BANHEIRO SOCIAL` | `Bathroom` | M | Alta |
| 29 | `Banheiro - Empregada` | `Bathroom` | M | Alta |
| 30 | `Banheiro Reversível` | `Bathroom` | M | Alta |

---

## 2. Cozinha

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 31 | `Cozinha` | `Kitchen` | C | Alta |
| 32 | `Cozinha - Gourmet` | `Kitchen` | C | Alta |
| 33 | `Cozinha Gourmet` | `Kitchen` | C | Alta |
| 34 | `Cozinha - Principal` | `Kitchen` | C | Alta |
| 35 | `Cozinha Americana` | `Kitchen` | C | Alta |
| 36 | `Coz.` | `Kitchen` | A | Média |
| 37 | `Coz` | `Kitchen` | A | Média |
| 38 | `Coz. Gourmet` | `Kitchen` | A | Média |
| 39 | `Copa` | `Kitchen` | S | Média |
| 40 | `Copa-Cozinha` | `Kitchen` | S | Alta |
| 41 | `Copa / Cozinha` | `Kitchen` | S | Alta |
| 42 | `Copa e Cozinha` | `Kitchen` | S | Alta |
| 43 | `Kitchen` | `Kitchen` | S | Média |
| 44 | `Cozihna` | `Kitchen` | E | Média |
| 45 | `Cosinha` | `Kitchen` | E | Média |
| 46 | `Cozina` | `Kitchen` | E | Média |
| 47 | `COZINHA` | `Kitchen` | M | Alta |
| 48 | `cozinha` | `Kitchen` | M | Alta |
| 49 | `Cozinha 01` | `Kitchen` | M | Alta |
| 50 | `Cozinha/Copa` | `Kitchen` | M | Alta |

---

## 3. Área de Serviço

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 51 | `Área de Serviço` | `Laundry` | C | Alta |
| 52 | `Area de Serviço` | `Laundry` | C | Alta |
| 53 | `Área de Servico` | `Laundry` | C | Alta |
| 54 | `Area de Servico` | `Laundry` | C | Alta |
| 55 | `Área de Serviço - 01` | `Laundry` | C | Alta |
| 56 | `Lavanderia` | `Laundry` | S | Alta |
| 57 | `Serviço` | `Laundry` | S | Média |
| 58 | `Á. Serviço` | `Laundry` | A | Média |
| 59 | `A. Serviço` | `Laundry` | A | Média |
| 60 | `Á. Serv.` | `Laundry` | A | Média |
| 61 | `A.S.` | `Laundry` | A | Baixa |
| 62 | `AS` | `Laundry` | A | Baixa |
| 63 | `Área Serv.` | `Laundry` | A | Média |
| 64 | `Area Serv` | `Laundry` | A | Média |
| 65 | `Área Serviço` | `Laundry` | M | Alta |
| 66 | `ÁREA DE SERVIÇO` | `Laundry` | M | Alta |
| 67 | `area de servico` | `Laundry` | M | Alta |
| 68 | `Área de Serv.` | `Laundry` | A | Média |
| 69 | `Laundry` | `Laundry` | S | Média |
| 70 | `Área Servço` | `Laundry` | E | Média |

---

## 4. Lavabo

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 71 | `Lavabo` | `Lavatory` | C | Alta |
| 72 | `Lavabo - Social` | `Lavatory` | C | Alta |
| 73 | `Lavabo Social` | `Lavatory` | C | Alta |
| 74 | `Lavabo - 01` | `Lavatory` | C | Alta |
| 75 | `Toilette` | `Lavatory` | S | Alta |
| 76 | `Toalete` | `Lavatory` | S | Alta |
| 77 | `Toilet` | `Lavatory` | S | Média |
| 78 | `Lav.` | `Lavatory` | A | Baixa |
| 79 | `Lavbo` | `Lavatory` | E | Média |
| 80 | `Labavo` | `Lavatory` | E | Baixa |
| 81 | `LAVABO` | `Lavatory` | M | Alta |
| 82 | `lavabo` | `Lavatory` | M | Alta |
| 83 | `Lavabo Visitas` | `Lavatory` | M | Alta |
| 84 | `Lavabo - Íntimo` | `Lavatory` | M | Alta |
| 85 | `Lavabo Externo` | `Lavatory` | M | Alta |

---

## 5. Quarto

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 86 | `Quarto` | `DryArea` | C | Alta |
| 87 | `Quarto - Casal` | `DryArea` | C | Alta |
| 88 | `Quarto Casal` | `DryArea` | C | Alta |
| 89 | `Quarto - Solteiro` | `DryArea` | C | Alta |
| 90 | `Quarto - 01` | `DryArea` | C | Alta |
| 91 | `Quarto - 02` | `DryArea` | C | Alta |
| 92 | `Quarto - 03` | `DryArea` | C | Alta |
| 93 | `Quarto - Hóspedes` | `DryArea` | C | Alta |
| 94 | `Quarto de Hóspedes` | `DryArea` | C | Alta |
| 95 | `Quarto Hospedes` | `DryArea` | C | Alta |
| 96 | `Quarto - Empregada` | `DryArea` | C | Alta |
| 97 | `Dormitório` | `DryArea` | S | Alta |
| 98 | `Dormitório Casal` | `DryArea` | S | Alta |
| 99 | `Dorm.` | `DryArea` | A | Média |
| 100 | `Dorm. 01` | `DryArea` | A | Média |
| 101 | `Qto.` | `DryArea` | A | Média |
| 102 | `Qto` | `DryArea` | A | Média |
| 103 | `Qto. Casal` | `DryArea` | A | Média |
| 104 | `Qto 01` | `DryArea` | A | Média |
| 105 | `Quarto Reversível` | `DryArea` | M | Alta |
| 106 | `Quarot` | `DryArea` | E | Média |
| 107 | `QUARTO` | `DryArea` | M | Alta |
| 108 | `quarto casal` | `DryArea` | M | Alta |
| 109 | `Suíte` | `DryArea` | M | Alta |
| 110 | `Suíte - Master` | `DryArea` | M | Alta |
| 111 | `Suíte - 01` | `DryArea` | M | Alta |
| 112 | `Suite Casal` | `DryArea` | M | Alta |
| 113 | `Suite Master` | `DryArea` | M | Alta |
| 114 | `Bedroom` | `DryArea` | S | Média |
| 115 | `Closet` | `DryArea` | C | Alta |

---

## 6. Sala

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 116 | `Sala de Estar` | `DryArea` | C | Alta |
| 117 | `Sala Estar` | `DryArea` | C | Alta |
| 118 | `Sala de Jantar` | `DryArea` | C | Alta |
| 119 | `Sala - Estar` | `DryArea` | C | Alta |
| 120 | `Sala - Jantar` | `DryArea` | C | Alta |
| 121 | `Sala de TV` | `DryArea` | C | Alta |
| 122 | `Sala TV` | `DryArea` | C | Alta |
| 123 | `Sala - TV` | `DryArea` | C | Alta |
| 124 | `Sala Íntima` | `DryArea` | C | Alta |
| 125 | `Sala` | `DryArea` | C | Alta |
| 126 | `Living` | `DryArea` | S | Média |
| 127 | `Living Room` | `DryArea` | S | Média |
| 128 | `Estar` | `DryArea` | S | Média |
| 129 | `Estar / Jantar` | `DryArea` | S | Média |
| 130 | `Sala Estar / Jantar` | `DryArea` | M | Alta |
| 131 | `Sala / Cozinha` | `DryArea` | X | Média |
| 132 | `Sala e Cozinha` | `DryArea` | X | Média |
| 133 | `SALA DE ESTAR` | `DryArea` | M | Alta |
| 134 | `sala` | `DryArea` | M | Alta |
| 135 | `Escritório` | `DryArea` | C | Alta |
| 136 | `Home Office` | `DryArea` | C | Alta |
| 137 | `Home-Office` | `DryArea` | C | Alta |
| 138 | `Estúdio` | `DryArea` | S | Média |
| 139 | `Biblioteca` | `DryArea` | S | Média |
| 140 | `Ateliê` | `DryArea` | S | Média |

---

## 7. Corredor / Circulação

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 141 | `Corredor` | `DryArea` | C | Alta |
| 142 | `Corredor - 01` | `DryArea` | C | Alta |
| 143 | `Corredor - Íntimo` | `DryArea` | C | Alta |
| 144 | `Circulação` | `DryArea` | C | Alta |
| 145 | `Circulacao` | `DryArea` | C | Alta |
| 146 | `Circ.` | `DryArea` | A | Média |
| 147 | `Hall` | `DryArea` | C | Alta |
| 148 | `Hall de Entrada` | `DryArea` | C | Alta |
| 149 | `Hall Social` | `DryArea` | C | Alta |
| 150 | `Hall - Íntimo` | `DryArea` | C | Alta |
| 151 | `Escada` | `DryArea` | C | Alta |
| 152 | `Escadaria` | `DryArea` | S | Média |
| 153 | `Depósito` | `DryArea` | C | Alta |
| 154 | `Deposito` | `DryArea` | C | Alta |
| 155 | `Despensa` | `DryArea` | C | Alta |
| 156 | `Desp.` | `DryArea` | A | Média |
| 157 | `Rouparia` | `DryArea` | S | Média |
| 158 | `Adega` | `DryArea` | S | Média |
| 159 | `CORREDOR` | `DryArea` | M | Alta |
| 160 | `Vest.` | `DryArea` | A | Baixa |
| 161 | `Vestíbulo` | `DryArea` | S | Média |

---

## 8. Garagem

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 162 | `Garagem` | `Garage` | C | Alta |
| 163 | `Garagem - 01` | `Garage` | C | Alta |
| 164 | `Garagem - 02` | `Garage` | C | Alta |
| 165 | `Garagem Coberta` | `Garage` | C | Alta |
| 166 | `Garagem Descoberta` | `Garage` | C | Alta |
| 167 | `Vaga` | `Garage` | S | Média |
| 168 | `Vaga - 01` | `Garage` | S | Média |
| 169 | `Estacionamento` | `Garage` | S | Alta |
| 170 | `Garage` | `Garage` | S | Média |
| 171 | `Garagm` | `Garage` | E | Média |
| 172 | `Gar.` | `Garage` | A | Média |
| 173 | `GARAGEM` | `Garage` | M | Alta |
| 174 | `garagem` | `Garage` | M | Alta |
| 175 | `Parking` | `Garage` | S | Baixa |

---

## 9. Varanda / Sacada

| # | Input | Esperado | Tipo | Confiança |
|---|-------|----------|------|-----------|
| 176 | `Varanda` | `Balcony` | C | Alta |
| 177 | `Varanda - Gourmet` | `Balcony` | C | Alta |
| 178 | `Varanda Gourmet` | `Balcony` | C | Alta |
| 179 | `Varanda - Social` | `Balcony` | C | Alta |
| 180 | `Varanda - Íntima` | `Balcony` | C | Alta |
| 181 | `Sacada` | `Balcony` | S | Alta |
| 182 | `Sacada - Suíte` | `Balcony` | S | Alta |
| 183 | `Terraço` | `Balcony` | S | Alta |
| 184 | `Terraco` | `Balcony` | S | Alta |
| 185 | `Terraço - Descoberto` | `Balcony` | S | Alta |
| 186 | `Balcão` | `Balcony` | S | Média |
| 187 | `Balcony` | `Balcony` | S | Média |
| 188 | `Var.` | `Balcony` | A | Média |
| 189 | `Var. Gourmet` | `Balcony` | A | Média |
| 190 | `VARANDA` | `Balcony` | M | Alta |

---

## 10. Casos Críticos e Ambíguos

Entradas que exigem atenção especial do classificador:

### 10.1 Ambiguidade entre tipos

| # | Input | Esperado | Risco | Detalhe |
|---|-------|----------|-------|---------|
| 191 | `Suíte` | `DryArea` | ⚠️ Alto | Pode confundir com `MasterBathroom` (é o quarto, não o banheiro) |
| 192 | `Suíte Master` | `DryArea` | ⚠️ Alto | Idem — suíte = quarto com banheiro privativo |
| 193 | `Área` | `Unknown` | ⚠️ Alto | Muito genérico — pode ser serviço, externa, etc. |
| 194 | `Espaço` | `Unknown` | ⚠️ Alto | Completamente genérico |
| 195 | `Sala / Cozinha` | `DryArea` | ⚠️ Médio | Ambiente integrado — priorizar o nome principal |
| 196 | `Copa / Cozinha` | `Kitchen` | ⚠️ Médio | Contém "cozinha" — classificar como Kitchen |
| 197 | `Varanda Gourmet` | `Balcony` | ⚠️ Médio | "Gourmet" pode confundir com Kitchen |
| 198 | `Banheiro Suíte` | `MasterBathroom` | ⚠️ Baixo | Deve ser Master, não Bathroom simples |
| 199 | `Lav.` | `Lavatory` | ⚠️ Médio | Pode ser "Lavabo" ou "Lavatório" — ambos vão para Lavatory |
| 200 | `Área Gourmet` | `Balcony` | ⚠️ Médio | Pode ser varanda gourmet ou espaço gourmet |

### 10.2 Nomes que NÃO devem classificar (→ Unknown)

| # | Input | Esperado | Razão |
|---|-------|----------|-------|
| 201 | ` ` (espaço) | `Unknown` | Nome vazio efetivo |
| 202 | `Room` | `Unknown` | Nome genérico em inglês |
| 203 | `Room 1` | `Unknown` | Sem significado funcional |
| 204 | `Ambiente` | `Unknown` | Genérico demais |
| 205 | `Ambiente 01` | `Unknown` | Genérico com número |
| 206 | `Espaço 1` | `Unknown` | Genérico |
| 207 | `Undefined` | `Unknown` | Template não preenchido |
| 208 | `???` | `Unknown` | Placeholder |
| 209 | `Novo Ambiente` | `Unknown` | Nome provisório |
| 210 | `A definir` | `Unknown` | Não classificado |

### 10.3 Nomes em inglês (projetos internacionais)

| # | Input | Esperado | Confiança |
|---|-------|----------|-----------|
| 211 | `Bathroom` | `Bathroom` | Média |
| 212 | `Master Bathroom` | `MasterBathroom` | Média |
| 213 | `Kitchen` | `Kitchen` | Média |
| 214 | `Laundry` | `Laundry` | Média |
| 215 | `Living Room` | `DryArea` | Média |
| 216 | `Bedroom` | `DryArea` | Média |
| 217 | `Bedroom - 01` | `DryArea` | Média |
| 218 | `Garage` | `Garage` | Média |
| 219 | `Balcony` | `Balcony` | Média |
| 220 | `Hallway` | `DryArea` | Baixa |

### 10.4 Nomes com caracteres especiais

| # | Input | Esperado | Confiança |
|---|-------|----------|-----------|
| 221 | `Banheiro (Social)` | `Bathroom` | Alta |
| 222 | `Cozinha [Principal]` | `Kitchen` | Alta |
| 223 | `Quarto #1` | `DryArea` | Média |
| 224 | `Sala_Estar` | `DryArea` | Média |
| 225 | `Banheiro/Social` | `Bathroom` | Média |

---

## 11. Uso no Sistema

### 11.1 Localização do dataset

```
Data/TestData/room_name_variations.json
```

### 11.2 Formato JSON recomendado

```json
{
  "testCases": [
    {
      "id": 1,
      "input": "Banheiro - Social",
      "expectedType": "Bathroom",
      "variationType": "Correct",
      "expectedConfidence": "High"
    },
    {
      "id": 20,
      "input": "Banhero",
      "expectedType": "Bathroom",
      "variationType": "Typo",
      "expectedConfidence": "Medium"
    }
  ]
}
```

### 11.3 Usos

| Uso | Detalhe |
|-----|---------|
| **Teste unitário** | Validar cada entrada contra o resultado esperado |
| **Teste de regressão** | Garantir que correções não quebram classificações anteriores |
| **Métricas** | Calcular taxa de acerto por tipo e por nível de confiança |
| **Benchmark** | Medir performance do classificador com 225 entradas |

### 11.4 Exemplo de teste unitário

```csharp
[Theory]
[InlineData("Banheiro", RoomType.Bathroom)]
[InlineData("Banheiro - Social", RoomType.Bathroom)]
[InlineData("Banheiro Suíte", RoomType.MasterBathroom)]
[InlineData("BWC", RoomType.Bathroom)]
[InlineData("Banhero", RoomType.Bathroom)]
[InlineData("Cozinha", RoomType.Kitchen)]
[InlineData("Copa", RoomType.Kitchen)]
[InlineData("Cosinha", RoomType.Kitchen)]
[InlineData("Área de Serviço", RoomType.Laundry)]
[InlineData("Lavanderia", RoomType.Laundry)]
[InlineData("Lavabo", RoomType.Lavatory)]
[InlineData("Quarto - Casal", RoomType.DryArea)]
[InlineData("Sala de Estar", RoomType.DryArea)]
[InlineData("Garagem", RoomType.Garage)]
[InlineData("Varanda", RoomType.Balcony)]
[InlineData("Room 1", RoomType.Unknown)]
public void Classificar_DeveRetornarTipoCorreto(string input, RoomType expected)
{
    var classificador = new ClassificadorAmbientes();
    var result = classificador.Classificar(input);
    Assert.Equal(expected, result.Tipo);
}
```

---

## 12. Estatísticas

### 12.1 Total por tipo

| RoomType | Total entradas | Corretas | Abreviações | Erros | Sinônimos | Mistos | Ambíguos |
|----------|---------------|----------|-------------|-------|-----------|--------|----------|
| `Bathroom` | 20 | 6 | 4 | 3 | 5 | 8 | 0 |
| `MasterBathroom` | 10 | 5 | 1 | 0 | 0 | 1 | 0 |
| `Kitchen` | 20 | 5 | 3 | 3 | 4 | 4 | 0 |
| `Laundry` | 20 | 5 | 7 | 1 | 3 | 3 | 0 |
| `Lavatory` | 15 | 4 | 1 | 2 | 3 | 4 | 0 |
| `DryArea (Quarto)` | 30 | 11 | 5 | 1 | 3 | 7 | 0 |
| `DryArea (Sala)` | 25 | 9 | 0 | 0 | 5 | 4 | 2 |
| `DryArea (Circulação)` | 21 | 10 | 3 | 0 | 4 | 1 | 0 |
| `Garage` | 14 | 5 | 1 | 1 | 4 | 2 | 0 |
| `Balcony` | 15 | 5 | 2 | 0 | 5 | 1 | 0 |
| `Unknown` | 10 | — | — | — | — | — | 10 |
| **Inglês** | 10 | — | — | — | 10 | — | — |
| **Especiais** | 5 | — | — | — | — | 5 | — |
| **TOTAL** | **225** | | | | | | |

### 12.2 Métricas alvo

| Métrica | Meta |
|---------|------|
| **Acurácia geral** | ≥ 92% |
| **Acurácia (Corretos)** | 100% |
| **Acurácia (Abreviações)** | ≥ 85% |
| **Acurácia (Erros digitação)** | ≥ 70% |
| **Acurácia (Sinônimos)** | ≥ 90% |
| **Acurácia (Mistos)** | ≥ 90% |
| **Taxa de falso positivo** | ≤ 3% |
| **Taxa de Unknown correto** | 100% (nomes genéricos DEVEM retornar Unknown) |

---

> **225 casos de teste prontos para validação do classificador automático de ambientes.**
