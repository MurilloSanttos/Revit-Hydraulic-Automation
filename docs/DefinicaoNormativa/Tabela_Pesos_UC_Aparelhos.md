# Tabela de Pesos e Unidades de Contribuição — Aparelhos Sanitários

> Base de dados consolidada para cálculo de vazão (NBR 5626) e dimensionamento de esgoto (NBR 8160), estruturada para consumo direto pelo plugin.

---

## 1. Tabela Principal — Água Fria (NBR 5626)

| # | Aparelho | Peso (P) | Vazão mín. projeto (L/s) | DN mín. sub-ramal (mm) | Altura instalação AF (m) | Observações |
|---|----------|---------|------------------------|------------------------|-------------------------|-------------|
| 01 | Vaso sanitário — caixa acoplada | 0.3 | 0.15 | 20 | 0.20 | Padrão residencial brasileiro. Alimentação lateral ou inferior. |
| 02 | Vaso sanitário — válvula de descarga | 32.0 | 1.70 | 50 | 0.20 | Peso muito alto; altera drasticamente o dimensionamento. Raro em residencial novo. |
| 03 | Lavatório — torneira simples | 0.5 | 0.15 | 20 | 0.60 | Torneira de pressão ou estándar. |
| 04 | Lavatório — torneira misturadora | 0.5 | 0.15 | 20 | 0.60 | Mesmo peso da simples para AF. |
| 05 | Bidê | 0.1 | 0.10 | 20 | 0.20 | Uso reduzido em projetos novos. |
| 06 | Chuveiro — sem misturador | 0.5 | 0.20 | 20 | 2.00 | Alimentação por cima (AF). |
| 07 | Chuveiro — com misturador | 0.5 | 0.20 | 20 | 2.00 | Mesmo peso AF. Misturador não altera o cálculo de AF isolada. |
| 08 | Banheira — torneira simples | 1.0 | 0.30 | 25 | 0.50 | Rara em residencial popular. |
| 09 | Banheira — torneira misturadora | 1.0 | 0.30 | 25 | 0.50 | Mesmo peso AF. |
| 10 | Pia de cozinha — torneira simples | 0.7 | 0.25 | 20 | 1.00 | Torneira de bica alta ou baixa. |
| 11 | Pia de cozinha — torneira misturadora | 0.7 | 0.25 | 20 | 1.00 | Mesmo peso AF. |
| 12 | Filtro de pressão de parede | 0.1 | 0.10 | 20 | 1.80 | Instalado na parede, DN reduzido. |
| 13 | Tanque de lavar roupa | 0.7 | 0.25 | 25 | 1.00 | Torneira de jardim/tanque. |
| 14 | Máquina de lavar roupa | 1.0 | 0.30 | 25 | 0.75 | Torneira com engate rápido. Ciclo intermitente. |
| 15 | Máquina de lavar louça | 1.0 | 0.30 | 25 | 0.75 | Alimentação sob bancada. |
| 16 | Torneira de jardim / lavagem | 0.5 | 0.20 | 20 | 0.60 | Uso externo. Ponto simples. |
| 17 | Torneira de tanque (com engate) | 0.7 | 0.25 | 25 | 1.00 | Mesmo padrão de tanque. |

**Fórmula de vazão:**
```
Q = 0.30 × √(ΣP)  [L/s]

Exceção: trecho com apenas 1 aparelho → usar vazão mínima de projeto da tabela.
Exceção: trecho com válvula de descarga → Q = 1.70 + 0.30 × √(ΣP_demais)
```

---

## 2. Tabela Principal — Esgoto (NBR 8160)

| # | Aparelho | UHC | DN mín. ramal descarga (mm) | Altura instalação ES (m) | Desconector | Observações |
|---|----------|-----|---------------------------|-------------------------|-------------|-------------|
| 01 | Vaso sanitário — caixa acoplada | 6 | **100** | 0.00 (nível do piso) | Integrado (sifão do vaso) | Ramal independente. NUNCA passa pela CX sifonada. |
| 02 | Vaso sanitário — válvula de descarga | 6 | **100** | 0.00 | Integrado | Mesmo UHC da caixa acoplada. |
| 03 | Lavatório | 1 | 40 | 0.55 | Sifão externo | Descarga na CX sifonada (banheiro) ou sifão individual. |
| 04 | Bidê | 1 | 40 | 0.00 | Sifão externo | Descarga na CX sifonada. |
| 05 | Chuveiro | 2 | 40 | 0.00 (nível do piso) | CX sifonada do banheiro | Ralo do chuveiro → CX sifonada. |
| 06 | Banheira | 2 | 40 | 0.00 | Sifão externo | Descarga direta ou na CX sifonada. |
| 07 | Pia de cozinha — sem triturador | 3 | **50** | 0.55 | Sifão externo | Obrigatório CX de gordura na saída. |
| 08 | Pia de cozinha — com triturador | 3 | **75** | 0.55 | Sifão externo | DN maior por causa dos sólidos triturados. Raro em residencial. |
| 09 | Tanque de lavar roupa | 3 | 40 | 0.55 | Sifão externo | Pode descarregar na CX sifonada da lavanderia ou sifão individual. |
| 10 | Máquina de lavar roupa | 3 | **50** | 0.10 | Sifão/ralo sifonado | Vazão alta intermitente. DN 50 obrigatório. |
| 11 | Máquina de lavar louça | 2 | **50** | 0.10 | Sifão externo | Descarga sob bancada. |
| 12 | Mictório com válvula automática | 6 | **75** | 0.00 | Integrado | Em residencial: praticamente inexistente. |
| 13 | Mictório sem válvula | 2 | 40 | 0.00 | Integrado | Idem acima. |
| 14 | Ralo sifonado DN 100 | 1 | 40 | 0.00 | Integrado (sifonado) | Coleta de piso do banheiro → CX sifonada. |
| 15 | Ralo seco | 1 | 40 | 0.00 | Nenhum (seco) | Apenas coleta de piso, sem fecho hídrico. |
| 16 | Caixa sifonada 150×150×50 | (*) | 50 | 0.00 | Integrada | UHC = soma dos aparelhos conectados. Não adiciona UHC própria. |
| 17 | Caixa sifonada 100×100×50 | (*) | 40 | 0.00 | Integrada | Idem. Para uso em ambientes pequenos. |

(*) A caixa sifonada não possui UHC própria. Seu dimensionamento é pela soma dos UHCs dos aparelhos que descarregam nela.

**Dimensionamento:**
```
Para cada trecho: ΣUHC → consultar tabela de DN mínimo
Regra absoluta: DN nunca diminui no sentido do escoamento
Vaso sanitário: SEMPRE DN ≥ 100mm
```

---

## 3. Tabela Consolidada — AF + ES

| # | Aparelho | Peso AF | Vazão AF (L/s) | DN mín AF (mm) | UHC ES | DN mín ES (mm) | Sistemas |
|---|----------|---------|---------------|---------------|--------|---------------|----------|
| 01 | Vaso — caixa acoplada | 0.3 | 0.15 | 20 | 6 | 100 | AF + ES |
| 02 | Vaso — válvula descarga | 32.0 | 1.70 | 50 | 6 | 100 | AF + ES |
| 03 | Lavatório | 0.5 | 0.15 | 20 | 1 | 40 | AF + ES |
| 04 | Bidê | 0.1 | 0.10 | 20 | 1 | 40 | AF + ES |
| 05 | Chuveiro | 0.5 | 0.20 | 20 | 2 | 40 | AF + ES |
| 06 | Banheira | 1.0 | 0.30 | 25 | 2 | 40 | AF + ES |
| 07 | Pia de cozinha | 0.7 | 0.25 | 20 | 3 | 50 | AF + ES |
| 08 | Filtro de pressão | 0.1 | 0.10 | 20 | — | — | AF |
| 09 | Tanque de lavar | 0.7 | 0.25 | 25 | 3 | 40 | AF + ES |
| 10 | Máq. lavar roupa | 1.0 | 0.30 | 25 | 3 | 50 | AF + ES |
| 11 | Máq. lavar louça | 1.0 | 0.30 | 25 | 2 | 50 | AF + ES |
| 12 | Torneira jardim | 0.5 | 0.20 | 20 | — | — | AF |
| 13 | Ralo sifonado | — | — | — | 1 | 40 | ES |
| 14 | Ralo seco | — | — | — | 1 | 40 | ES |

---

## 4. Variações e Condições Especiais

### 4.1 Variações por modelo de aparelho

| Aparelho | Variação | Impacto AF | Impacto ES | Ação no plugin |
|----------|----------|-----------|-----------|---------------|
| Vaso sanitário | Caixa acoplada vs. Válvula de descarga | Peso AF muda de 0.3 para 32.0 | UHC não muda (6) | Solicitar confirmação do tipo; padrão = caixa acoplada |
| Chuveiro | Com/sem misturador | Peso AF não muda (0.5) | UHC não muda (2) | Sem impacto no cálculo |
| Pia cozinha | Com/sem triturador | Peso AF não muda (0.7) | DN muda: 50→75mm | Verificar família; padrão = sem triturador |
| Lavatório | Torneira simples/misturadora | Peso não muda | UHC não muda | Sem impacto |
| Mictório | Com/sem válvula | (raro em residencial) | UHC muda: 6→2 | Identificar na família |

### 4.2 Restrições normativas críticas

| Restrição | Norma | Regra |
|-----------|-------|-------|
| Vaso: DN ramal descarga ≥ 100mm | NBR 8160 | Inviolável. Não existe exceção. |
| Pia cozinha: CX gordura obrigatória | NBR 8160 | Antes da conexão com ramal de esgoto. |
| Vaso: ramal independente da CX sifonada | NBR 8160 | Volume de descarga pode sifonar CX. |
| Pressão mínima no chuveiro: 1 m.c.a. (norma) / 3 m.c.a. (prática) | NBR 5626 | Plugin usa 3.0 m.c.a. como padrão. |
| Velocidade máx. AF: 3.0 m/s | NBR 5626 | Aplicável a todos os trechos. |
| Diâmetro nunca diminui no escoamento (ES) | NBR 8160 | Validação automática. |

### 4.3 Aparelhos residenciais mais frequentes

| Frequência | Aparelhos | % de projetos onde aparece |
|-----------|-----------|--------------------------|
| **Sempre** | Vaso (caixa), lavatório, chuveiro, pia cozinha, ralo sifonado | 100% |
| **Muito frequente** | Tanque, máq. lavar roupa | 90%+ |
| **Frequente** | Torneira jardim, filtro | 70%+ |
| **Ocasional** | Máq. lavar louça, bidê | 30–50% |
| **Raro** | Banheira, mictório, vaso com válvula | < 10% |

---

## 5. Mapeamento Aparelho → Ambiente

| Tipo de ambiente | Aparelhos obrigatórios | Aparelhos opcionais |
|-----------------|----------------------|-------------------|
| **Banheiro** | Vaso, lavatório, chuveiro, ralo sifonado, CX sifonada | Bidê |
| **Lavabo** | Vaso, lavatório | — |
| **Suíte** (banheiro) | Vaso, lavatório, chuveiro, ralo sifonado, CX sifonada | Bidê, banheira |
| **Cozinha** | Pia, CX gordura | Máq. lavar louça, filtro |
| **Cozinha Gourmet** | Pia, CX gordura | — |
| **Lavanderia** | Tanque, máq. lavar roupa, ralo sifonado | — |
| **Área de Serviço** | Tanque, ralo sifonado | Máq. lavar roupa |
| **Área Externa** | Torneira jardim | Ralo seco |

---

## 6. JSON Estruturado — Base de Dados do Plugin

### 6.1 JSON completo de aparelhos

```json
{
  "aparelhos": {
    "vaso_caixa_acoplada": {
      "nome_display": "Vaso Sanitário (Caixa Acoplada)",
      "agua_fria": {
        "peso": 0.3,
        "vazao_min_Ls": 0.15,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 0.20
      },
      "esgoto": {
        "uhc": 6,
        "dn_min_ramal_mm": 100,
        "altura_instalacao_m": 0.00,
        "desconector": "integrado",
        "ramal_independente": true
      },
      "frequencia": "sempre",
      "observacao": "Padrão residencial. Ramal ES independente da CX sifonada."
    },

    "vaso_valvula_descarga": {
      "nome_display": "Vaso Sanitário (Válvula de Descarga)",
      "agua_fria": {
        "peso": 32.0,
        "vazao_min_Ls": 1.70,
        "dn_min_subramal_mm": 50,
        "altura_instalacao_m": 0.20
      },
      "esgoto": {
        "uhc": 6,
        "dn_min_ramal_mm": 100,
        "altura_instalacao_m": 0.00,
        "desconector": "integrado",
        "ramal_independente": true
      },
      "frequencia": "raro",
      "observacao": "Peso AF 100× maior que caixa acoplada. Usar apenas se confirmado."
    },

    "lavatorio": {
      "nome_display": "Lavatório",
      "agua_fria": {
        "peso": 0.5,
        "vazao_min_Ls": 0.15,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 0.60
      },
      "esgoto": {
        "uhc": 1,
        "dn_min_ramal_mm": 40,
        "altura_instalacao_m": 0.55,
        "desconector": "sifao_externo",
        "ramal_independente": false
      },
      "frequencia": "sempre",
      "observacao": "Descarga na CX sifonada do banheiro."
    },

    "bide": {
      "nome_display": "Bidê",
      "agua_fria": {
        "peso": 0.1,
        "vazao_min_Ls": 0.10,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 0.20
      },
      "esgoto": {
        "uhc": 1,
        "dn_min_ramal_mm": 40,
        "altura_instalacao_m": 0.00,
        "desconector": "sifao_externo",
        "ramal_independente": false
      },
      "frequencia": "ocasional",
      "observacao": "Descarga na CX sifonada."
    },

    "chuveiro": {
      "nome_display": "Chuveiro",
      "agua_fria": {
        "peso": 0.5,
        "vazao_min_Ls": 0.20,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 2.00
      },
      "esgoto": {
        "uhc": 2,
        "dn_min_ramal_mm": 40,
        "altura_instalacao_m": 0.00,
        "desconector": "caixa_sifonada",
        "ramal_independente": false
      },
      "frequencia": "sempre",
      "observacao": "Ralo do box → CX sifonada do banheiro."
    },

    "banheira": {
      "nome_display": "Banheira",
      "agua_fria": {
        "peso": 1.0,
        "vazao_min_Ls": 0.30,
        "dn_min_subramal_mm": 25,
        "altura_instalacao_m": 0.50
      },
      "esgoto": {
        "uhc": 2,
        "dn_min_ramal_mm": 40,
        "altura_instalacao_m": 0.00,
        "desconector": "sifao_externo",
        "ramal_independente": false
      },
      "frequencia": "raro",
      "observacao": "Rara em residencial popular."
    },

    "pia_cozinha": {
      "nome_display": "Pia de Cozinha",
      "agua_fria": {
        "peso": 0.7,
        "vazao_min_Ls": 0.25,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 1.00
      },
      "esgoto": {
        "uhc": 3,
        "dn_min_ramal_mm": 50,
        "altura_instalacao_m": 0.55,
        "desconector": "sifao_externo",
        "ramal_independente": true,
        "cx_gordura_obrigatoria": true
      },
      "frequencia": "sempre",
      "observacao": "CX de gordura obrigatória antes do ramal de esgoto."
    },

    "pia_cozinha_triturador": {
      "nome_display": "Pia de Cozinha (com Triturador)",
      "agua_fria": {
        "peso": 0.7,
        "vazao_min_Ls": 0.25,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 1.00
      },
      "esgoto": {
        "uhc": 3,
        "dn_min_ramal_mm": 75,
        "altura_instalacao_m": 0.55,
        "desconector": "sifao_externo",
        "ramal_independente": true,
        "cx_gordura_obrigatoria": true
      },
      "frequencia": "raro",
      "observacao": "DN ES mínimo 75mm por causa de sólidos triturados."
    },

    "filtro_pressao": {
      "nome_display": "Filtro de Pressão",
      "agua_fria": {
        "peso": 0.1,
        "vazao_min_Ls": 0.10,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 1.80
      },
      "esgoto": null,
      "frequencia": "frequente",
      "observacao": "Apenas AF. Sem descarga de esgoto."
    },

    "tanque": {
      "nome_display": "Tanque de Lavar Roupa",
      "agua_fria": {
        "peso": 0.7,
        "vazao_min_Ls": 0.25,
        "dn_min_subramal_mm": 25,
        "altura_instalacao_m": 1.00
      },
      "esgoto": {
        "uhc": 3,
        "dn_min_ramal_mm": 40,
        "altura_instalacao_m": 0.55,
        "desconector": "sifao_externo",
        "ramal_independente": false
      },
      "frequencia": "muito_frequente",
      "observacao": "Pode descarregar na CX sifonada da lavanderia."
    },

    "maquina_lavar_roupa": {
      "nome_display": "Máquina de Lavar Roupa",
      "agua_fria": {
        "peso": 1.0,
        "vazao_min_Ls": 0.30,
        "dn_min_subramal_mm": 25,
        "altura_instalacao_m": 0.75
      },
      "esgoto": {
        "uhc": 3,
        "dn_min_ramal_mm": 50,
        "altura_instalacao_m": 0.10,
        "desconector": "ralo_sifonado",
        "ramal_independente": false
      },
      "frequencia": "muito_frequente",
      "observacao": "Descarga intermitente. DN ES 50 obrigatório."
    },

    "maquina_lavar_louca": {
      "nome_display": "Máquina de Lavar Louça",
      "agua_fria": {
        "peso": 1.0,
        "vazao_min_Ls": 0.30,
        "dn_min_subramal_mm": 25,
        "altura_instalacao_m": 0.75
      },
      "esgoto": {
        "uhc": 2,
        "dn_min_ramal_mm": 50,
        "altura_instalacao_m": 0.10,
        "desconector": "sifao_externo",
        "ramal_independente": false
      },
      "frequencia": "ocasional",
      "observacao": "Instalação sob bancada da cozinha."
    },

    "torneira_jardim": {
      "nome_display": "Torneira de Jardim/Lavagem",
      "agua_fria": {
        "peso": 0.5,
        "vazao_min_Ls": 0.20,
        "dn_min_subramal_mm": 20,
        "altura_instalacao_m": 0.60
      },
      "esgoto": null,
      "frequencia": "frequente",
      "observacao": "Apenas AF. Ponto externo."
    },

    "ralo_sifonado": {
      "nome_display": "Ralo Sifonado",
      "agua_fria": null,
      "esgoto": {
        "uhc": 1,
        "dn_min_ramal_mm": 40,
        "altura_instalacao_m": 0.00,
        "desconector": "integrado",
        "ramal_independente": false
      },
      "frequencia": "sempre",
      "observacao": "Apenas ES. Coleta de piso do banheiro/lavanderia."
    },

    "ralo_seco": {
      "nome_display": "Ralo Seco",
      "agua_fria": null,
      "esgoto": {
        "uhc": 1,
        "dn_min_ramal_mm": 40,
        "altura_instalacao_m": 0.00,
        "desconector": "nenhum",
        "ramal_independente": false
      },
      "frequencia": "frequente",
      "observacao": "Sem fecho hídrico. Usar em área externa ou coberta."
    }
  }
}
```

### 6.2 JSON de mapeamento ambiente → aparelhos

```json
{
  "mapeamento_ambiente_aparelhos": {
    "Banheiro": {
      "obrigatorios": [
        "vaso_caixa_acoplada",
        "lavatorio",
        "chuveiro",
        "ralo_sifonado"
      ],
      "opcionais": ["bide"],
      "acessorios": ["caixa_sifonada_150"]
    },
    "Lavabo": {
      "obrigatorios": [
        "vaso_caixa_acoplada",
        "lavatorio"
      ],
      "opcionais": [],
      "acessorios": []
    },
    "Suite": {
      "obrigatorios": [
        "vaso_caixa_acoplada",
        "lavatorio",
        "chuveiro",
        "ralo_sifonado"
      ],
      "opcionais": ["bide", "banheira"],
      "acessorios": ["caixa_sifonada_150"]
    },
    "Cozinha": {
      "obrigatorios": [
        "pia_cozinha"
      ],
      "opcionais": ["maquina_lavar_louca", "filtro_pressao"],
      "acessorios": ["caixa_gordura"]
    },
    "CozinhaGourmet": {
      "obrigatorios": [
        "pia_cozinha"
      ],
      "opcionais": [],
      "acessorios": ["caixa_gordura"]
    },
    "Lavanderia": {
      "obrigatorios": [
        "tanque",
        "ralo_sifonado"
      ],
      "opcionais": ["maquina_lavar_roupa"],
      "acessorios": []
    },
    "AreaServico": {
      "obrigatorios": [
        "tanque",
        "ralo_sifonado"
      ],
      "opcionais": ["maquina_lavar_roupa"],
      "acessorios": []
    },
    "AreaExterna": {
      "obrigatorios": [
        "torneira_jardim"
      ],
      "opcionais": ["ralo_seco"],
      "acessorios": []
    }
  }
}
```

### 6.3 JSON de totalização — exemplos de projeto

```json
{
  "exemplos_totalizacao": {
    "banheiro_completo": {
      "aparelhos": {
        "vaso_caixa_acoplada": 1,
        "lavatorio": 1,
        "chuveiro": 1,
        "ralo_sifonado": 1
      },
      "total_peso_AF": 1.3,
      "total_UHC_ES": 10,
      "vazao_provavel_Ls": 0.342,
      "calculo": "Q = 0.30 × √1.3 = 0.342 L/s"
    },
    "lavabo": {
      "aparelhos": {
        "vaso_caixa_acoplada": 1,
        "lavatorio": 1
      },
      "total_peso_AF": 0.8,
      "total_UHC_ES": 7,
      "vazao_provavel_Ls": 0.268,
      "calculo": "Q = 0.30 × √0.8 = 0.268 L/s"
    },
    "cozinha": {
      "aparelhos": {
        "pia_cozinha": 1
      },
      "total_peso_AF": 0.7,
      "total_UHC_ES": 3,
      "vazao_provavel_Ls": 0.251,
      "calculo": "Q = 0.30 × √0.7 = 0.251 L/s"
    },
    "lavanderia_completa": {
      "aparelhos": {
        "tanque": 1,
        "maquina_lavar_roupa": 1,
        "ralo_sifonado": 1
      },
      "total_peso_AF": 1.7,
      "total_UHC_ES": 7,
      "vazao_provavel_Ls": 0.391,
      "calculo": "Q = 0.30 × √1.7 = 0.391 L/s"
    },
    "casa_2pav_completa": {
      "aparelhos": {
        "vaso_caixa_acoplada": 3,
        "lavatorio": 3,
        "chuveiro": 3,
        "ralo_sifonado": 3,
        "pia_cozinha": 1,
        "tanque": 1,
        "maquina_lavar_roupa": 1,
        "torneira_jardim": 1
      },
      "total_peso_AF": 7.3,
      "total_UHC_ES": 43,
      "vazao_provavel_Ls": 0.811,
      "calculo": "Q = 0.30 × √7.3 = 0.811 L/s"
    }
  }
}
```

---

## 7. Resolução de Inconsistências

| Inconsistência | Fonte | Resolução | Justificativa |
|---------------|-------|-----------|---------------|
| UHC do vaso não muda entre caixa e válvula | NBR 8160 | Mantido 6 para ambos | Esgoto depende de volume de descarga, não de pressão de alimentação |
| Peso AF do chuveiro com/sem misturador | NBR 5626 | Mantido 0.5 para ambos | Misturador não altera vazão AF isolada (AQ é outra rede) |
| CX sifonada: UHC própria ou soma? | NBR 8160 | Soma dos aparelhos conectados | CX é acessório, não aparelho. Seu dimensionamento é pelo que recebe. |
| Pia com triturador: DN 50 ou 75? | NBR 8160 | DN 75 com triturador, DN 50 sem | Sólidos triturados exigem DN maior |
| Filtro de pressão: possui esgoto? | Prática | Não possui | Água filtrada é consumida, não descartada |
| Torneira de jardim: possui esgoto? | Prática | Não possui | Água irrigada infiltra no solo |

---

## 8. Notas de Implementação

### 8.1 Como o plugin deve usar esta tabela

```
1. Classificar ambiente (Módulo 02)
2. Consultar mapeamento ambiente → aparelhos (JSON 6.2)
3. Para cada aparelho:
   a. Consultar dados AF: peso, vazão mín, DN mín (JSON 6.1)
   b. Consultar dados ES: UHC, DN mín ramal (JSON 6.1)
4. Totalizar pesos AF por trecho
5. Totalizar UHCs ES por trecho
6. Usar nas fórmulas de dimensionamento:
   AF: Q = 0.30 × √ΣP → DN (velocidade)
   ES: ΣUHC → tabela → DN
```

### 8.2 Aparelhos que o plugin deve tratar como padrão

| Decisão padrão | Valor | Justificativa |
|---------------|-------|---------------|
| Tipo de vaso | Caixa acoplada | 90%+ dos projetos residenciais novos |
| Pia com triturador | Sem triturador | Padrão brasileiro |
| CX sifonada | 150×150 | Tamanho padrão para banheiro |
| Máq. lavar roupa | Presente na lavanderia | 90%+ dos projetos |
| Bidê | Não incluir por padrão | Opcional, < 30% dos projetos |
| Banheira | Não incluir por padrão | Raro, < 10% dos projetos |
