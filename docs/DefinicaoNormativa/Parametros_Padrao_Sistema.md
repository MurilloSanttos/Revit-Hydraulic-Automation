# Parâmetros Padrão do Sistema — Plugin Hidráulico Revit

> Base de configuração completa com parâmetros técnicos, valores padrão, limites, interdependências e regras de override para o plugin de automação hidráulica.

---

## 1. Organização Geral

### 1.1 Categorias de parâmetros

| Categoria | Prefixo | Arquivo JSON | Nº de parâmetros |
|-----------|---------|-------------|-----------------|
| Geral | `GER_` | `config_geral.json` | 8 |
| Água Fria | `AF_` | `config_agua_fria.json` | 14 |
| Esgoto | `ES_` | `config_esgoto.json` | 12 |
| Ventilação | `VE_` | `config_ventilacao.json` | 10 |
| Declividade | `DEC_` | `config_declividade.json` | 8 |
| Dimensionamento | `DIM_` | `config_dimensionamento.json` | 10 |
| Automação | `AUT_` | `config_automacao.json` | 9 |
| Interface | `UI_` | `config_interface.json` | 8 |
| Validação | `VAL_` | `config_validacao.json` | 10 |
| **TOTAL** | | | **89** |

### 1.2 Hierarquia de configuração

```
1. Valores padrão (hardcoded no plugin — fallback final)
2. config/*.json (arquivo de configuração do projeto)
3. Override do usuário via UI (sessão atual)

Prioridade: Override > JSON > Padrão hardcoded
```

### 1.3 Localização dos arquivos

```
{RevitProjectFolder}/HidraulicoPlugin/
├── config/
│   ├── config_geral.json
│   ├── config_agua_fria.json
│   ├── config_esgoto.json
│   ├── config_ventilacao.json
│   ├── config_declividade.json
│   ├── config_dimensionamento.json
│   ├── config_automacao.json
│   ├── config_interface.json
│   └── config_validacao.json
├── data/
│   ├── aparelhos.json
│   ├── diametros_comerciais.json
│   └── tabelas_dimensionamento.json
└── logs/
    └── log_{timestamp}.json
```

---

## 2. Parâmetros por Categoria

### 2.1 Geral

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `GER_versao_norma_af` | Versão NBR 5626 | string | "2020" | — | Versão da norma de água fria utilizada | Define tabelas e regras aplicáveis |
| 02 | `GER_versao_norma_es` | Versão NBR 8160 | string | "1999" | — | Versão da norma de esgoto utilizada | Define tabelas e regras aplicáveis |
| 03 | `GER_tipo_edificacao` | Tipo de edificação | enum | "residencial" | — | Valores: residencial, comercial, misto | Altera pesos, UHCs e regras de simultaneidade |
| 04 | `GER_num_pavimentos` | Número de pavimentos | int | 2 | — | Número de pavimentos do projeto | Influencia ventilação e dimensionamento de prumadas |
| 05 | `GER_material_af` | Material AF | enum | "pvc_soldavel" | — | Valores: pvc_soldavel, cpvc, ppr, cobre | Altera coeficientes de perda de carga e diâmetros internos |
| 06 | `GER_material_es` | Material ES | enum | "pvc_esgoto" | — | Valores: pvc_esgoto, ferro_fundido | Altera DN comerciais e rugosidade |
| 07 | `GER_tipo_abastecimento` | Tipo de abastecimento | enum | "reservatorio_superior" | — | Valores: reservatorio_superior, direto_rede, pressurizador | Define cálculo de pressão (gravitacional vs. pressurizado) |
| 08 | `GER_unidade_pressao` | Unidade de pressão | enum | "mca" | — | Valores: mca, kpa, kgf_cm2 | Unidade de exibição (internamente usa m.c.a.) |

### 2.2 Água Fria

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `AF_pressao_minima` | Pressão mínima | float | **3.0** | m.c.a. | Pressão dinâmica mínima em qualquer ponto de consumo | Valores abaixo geram erro Crítico. Norma: 1.0; prática: 3.0. |
| 02 | `AF_pressao_maxima` | Pressão máxima | float | **40.0** | m.c.a. | Pressão estática máxima em qualquer ponto | Acima: necessário válvula redutora. Alerta Médio. |
| 03 | `AF_velocidade_maxima` | Velocidade máxima | float | **3.0** | m/s | Velocidade máxima do escoamento em tubulações AF | Acima: ruído, golpe de aríete, desgaste. Erro Crítico. |
| 04 | `AF_velocidade_minima` | Velocidade mínima | float | **0.5** | m/s | Velocidade mínima recomendada | Abaixo: risco de sedimentação. Alerta Leve. |
| 05 | `AF_coeficiente_C` | Coeficiente de descarga | float | **0.30** | — | Coeficiente C da fórmula Q = C × √ΣP | Fixo para aparelhos com caixa acoplada. |
| 06 | `AF_tipo_vaso` | Tipo de vaso padrão | enum | "caixa_acoplada" | — | Valores: caixa_acoplada, valvula_descarga | Altera peso (0.3 vs. 32.0) e DN mínimo (20 vs. 50). |
| 07 | `AF_altura_reservatorio` | Altura do reservatório | float | **6.0** | m | Cota do nível d'água do reservatório superior | Base para cálculo de pressão estática. |
| 08 | `AF_fator_perdas_localizadas` | Fator de perdas localizadas | float | **1.20** | — | Fator K multiplicador para perdas localizadas (método simplificado) | 1.20 = 20% sobre comprimento real. Válido para residencial. |
| 09 | `AF_formula_perda_carga` | Fórmula de perda de carga | enum | "fair_whipple_hsiao" | — | Valores: fair_whipple_hsiao, hazen_williams | FWH para PVC; HW para metálicos. |
| 10 | `AF_metodo_perdas_localizadas` | Método de perdas localizadas | enum | "fator_percentual" | — | Valores: fator_percentual, comprimentos_equivalentes | Fator = simplificado; Comprimentos = mais preciso. |
| 11 | `AF_dn_minimo_subramal` | DN mínimo sub-ramal | int | **20** | mm | Menor diâmetro permitido para sub-ramal AF | Padrão PVC soldável. |
| 12 | `AF_dn_minimo_barrilete` | DN mínimo barrilete | int | **25** | mm | Menor diâmetro para barrilete | Boa prática. |
| 13 | `AF_registro_por_ambiente` | Registro por ambiente | bool | **true** | — | Exigir registro de gaveta na entrada de cada ambiente | Boa prática. Se false, não valida. |
| 14 | `AF_registro_geral` | Registro geral obrigatório | bool | **true** | — | Exigir registro na saída do reservatório | Normativo. |

### 2.3 Esgoto

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `ES_dn_minimo_ramal_vaso` | DN mínimo ramal vaso | int | **100** | mm | DN obrigatório para ramal de descarga do vaso sanitário | Inviolável. Não pode ser alterado abaixo de 100. |
| 02 | `ES_dn_minimo_subcoletor` | DN mínimo subcoletor | int | **100** | mm | DN mínimo para subcoletores | Inviolável. |
| 03 | `ES_dn_minimo_ramal_descarga` | DN mínimo ramal descarga geral | int | **40** | mm | Menor DN para ramal de descarga (exceto vaso) | PVC esgoto padrão. |
| 04 | `ES_fecho_hidrico_minimo` | Fecho hídrico mínimo | int | **50** | mm | Altura mínima da coluna de água no desconector | Normativo. Garante barreira contra gases. |
| 05 | `ES_cx_sifonada_banheiro` | CX sifonada em banheiro | bool | **true** | — | Exigir caixa sifonada em cada banheiro | Normativo. |
| 06 | `ES_cx_gordura_cozinha` | CX gordura em cozinha | bool | **true** | — | Exigir caixa de gordura na saída da pia | Normativo. |
| 07 | `ES_ramal_vaso_independente` | Ramal do vaso independente | bool | **true** | — | Vaso não passa pela CX sifonada | Normativo. Não deve ser desativado. |
| 08 | `ES_distancia_max_CI` | Distância máx entre CIs | float | **15.0** | m | Máximo comprimento de subcoletor sem caixa de inspeção | Boa prática. |
| 09 | `ES_curva_90_horizontal` | Permitir curva 90° horizontal | enum | "alerta" | — | Valores: bloquear, alerta, permitir | "alerta": gera Leve. "bloquear": Crítico. |
| 10 | `ES_dn_nunca_diminui` | DN nunca diminui no escoamento | bool | **true** | — | Validar que DN não diminui a jusante | Normativo. Sempre true. |
| 11 | `ES_fitting_juncao_preferido` | Fitting de junção preferido | enum | "tee_45" | — | Valores: tee_45, juncao_y, tee_90 | tee_45 para ramais horizontais. |
| 12 | `ES_profundidade_minima_enterramento` | Profundidade mínima | float | **0.30** | m | Cobertura mínima de solo sobre subcoletor enterrado | Proteção mecânica. |

### 2.4 Ventilação

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `VE_dn_minimo_absoluto` | DN mínimo ventilação | int | **40** | mm | Menor DN para qualquer tubo de ventilação | Normativo. |
| 02 | `VE_fator_coluna_vs_TQ` | Fator coluna/TQ | float | **0.667** | — | DN_coluna ≥ fator × DN_TQ | 2/3, conforme norma. |
| 03 | `VE_fator_ramal_vs_descarga` | Fator ramal vent/descarga | float | **0.50** | — | DN_ramal_vent ≥ fator × DN_ramal_desc | 1/2, conforme norma. |
| 04 | `VE_terminal_altura_minima` | Altura mín terminal | float | **0.30** | m | Acima da cobertura | Normativo. |
| 05 | `VE_terminal_distancia_janela` | Distância mín de janela | float | **4.00** | m | Distância horizontal de aberturas | Normativo. |
| 06 | `VE_conexao_elevacao_minima` | Elevação mín conexão | float | **0.15** | m | Ramal de vent acima do ramal de esgoto | Boa prática. |
| 07 | `VE_max_aparelhos_sem_vent` | Máx aparelhos sem vent | int | **4** | — | No ramal de esgoto, sem ventilação secundária | Boa prática. |
| 08 | `VE_max_comprimento_ramal_sem_vent` | Comprimento máx sem vent | float | **6.0** | m | Ramal de esgoto sem ponto de ventilação | Boa prática. |
| 09 | `VE_pavimentos_para_secundaria` | Pavimentos p/ vent secundária | int | **3** | — | A partir de quantos pav obrigar coluna de vent | Normativo. |
| 10 | `VE_inclinacao_minima_ramal` | Decliv mín ramal vent | float | **0.01** | m/m | Inclinação ascendente mínima do ramal de vent | 1% para drenagem de condensação. |

### 2.5 Declividade

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `DEC_modo_aplicacao` | Modo de aplicação | enum | "recomendada" | — | Valores: minima, recomendada, personalizada | Define qual decliv é aplicada automaticamente. |
| 02 | `DEC_tolerancia_cota` | Tolerância de cota | float | **0.001** | m | Tolerância de ±1mm na verificação de cotas | Evita falsos positivos por imprecisão. |
| 03 | `DEC_maxima_recomendada` | Declividade máx recomendada | float | **0.05** | m/m | 5% — acima gera alerta Leve | Boa prática. |
| 04 | `DEC_maxima_absoluta` | Declividade máx absoluta | float | **0.08** | m/m | 8% — acima gera alerta Médio | Conservador. |
| 05 | `DEC_verificar_espaco_laje` | Verificar espaço laje | bool | **true** | — | Verificar se desnível cabe na espessura da laje | Evita conflito geométrico. |
| 06 | `DEC_espessura_laje_padrao` | Espessura da laje | float | **0.15** | m | Valor padrão quando não obtido do modelo | 15cm = laje padrão residencial. |
| 07 | `DEC_aplicar_em_cascata` | Aplicar em cascata | bool | **true** | — | Ajustar trechos consecutivos sequencialmente | Garante continuidade de cotas. |
| 08 | `DEC_reconectar_fittings` | Reconectar fittings | bool | **true** | — | Após ajuste de cota, reconectar conexões | Evita desconexão de elementos. |

### 2.6 Dimensionamento

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `DIM_metodo_af` | Método AF | enum | "probabilistico" | — | Valores: probabilistico, empirico | Probabilístico: Q = C × √ΣP. |
| 02 | `DIM_metodo_es` | Método ES | enum | "uhc_tabela" | — | Valores: uhc_tabela | UHC com consulta a tabela normativa. |
| 03 | `DIM_margem_seguranca_af` | Margem de segurança AF | float | **1.0** | — | Multiplicador sobre vazão calculada (1.0 = sem margem) | 1.10 = 10% de margem. Aumenta diâmetros. |
| 04 | `DIM_arredondar_dn_acima` | Arredondar DN acima | bool | **true** | — | Se DN calculado não é comercial, arredondar para cima | Padrão. False = erro. |
| 05 | `DIM_verificar_velocidade` | Verificar velocidade | bool | **true** | — | Verificar V ≤ V_max após seleção de DN | Normativo. |
| 06 | `DIM_verificar_pressao` | Verificar pressão | bool | **true** | — | Calcular e verificar pressão em todos os pontos | Normativo. |
| 07 | `DIM_atualizar_modelo` | Atualizar diâmetros no modelo | bool | **true** | — | Após dimensionamento, atualizar DN dos Pipes no Revit | Se false: apenas relatório. |
| 08 | `DIM_gerar_relatorio` | Gerar relatório | bool | **true** | — | Exportar tabela de dimensionamento (CSV ou JSON) | Para documentação. |
| 09 | `DIM_precisao_vazao` | Precisão de vazão | int | **3** | casas decimais | Casas decimais para vazão nos cálculos | 0.342 L/s (3 casas). |
| 10 | `DIM_precisao_perda_carga` | Precisão perda de carga | int | **4** | casas decimais | Casas decimais para J nos cálculos | 0.0206 m/m (4 casas). |

### 2.7 Automação

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `AUT_modo` | Modo de automação | enum | "semi_automatico" | — | Valores: semi_automatico, automatico_total | Semi: valida cada etapa. Total: executa tudo sem pausa. |
| 02 | `AUT_aprovacao_por_etapa` | Aprovação por etapa | bool | **true** | — | Usuário deve aprovar cada módulo antes de avançar | Base do fluxo semi-automático. |
| 03 | `AUT_pausar_em_erro_medio` | Pausar em erro Médio | bool | **true** | — | Pausar e solicitar aprovação quando erro Médio ocorre | Se false: registra e continua. |
| 04 | `AUT_bloquear_em_erro_critico` | Bloquear em erro Crítico | bool | **true** | — | Impedir avanço quando erro Crítico | Não deve ser desativado. |
| 05 | `AUT_criar_spaces_automatico` | Criar Spaces automaticamente | bool | **false** | — | Criar Spaces sem pedir confirmação | false = pede confirmação (mais seguro). |
| 06 | `AUT_inserir_equipamentos_auto` | Inserir equipamentos auto | bool | **false** | — | Inserir equipamentos sem validação | false = lista e pede confirmação. |
| 07 | `AUT_roteamento_ferramenta` | Ferramenta de roteamento | enum | "dynamo" | — | Valores: dynamo, unmep, manual | Principal executor de roteamento. |
| 08 | `AUT_max_tentativas_reconexao` | Máx tentativas reconexão | int | **3** | — | Tentativas de reconectar fitting após ajuste | Após 3 falhas: registra erro Médio. |
| 09 | `AUT_taxa_falha_max_rollback` | Taxa falha para rollback | float | **0.30** | — | Se > 30% dos elementos falham: reverter tudo | Proteção contra danos ao modelo. |

### 2.8 Interface

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `UI_idioma` | Idioma | enum | "pt_BR" | — | Valores: pt_BR | Todas mensagens em português. |
| 02 | `UI_nivel_log` | Nível de log na UI | enum | "medio" | — | Valores: critico, medio, leve, info | Nível mínimo exibido na aba de diagnóstico. |
| 03 | `UI_mostrar_elementid` | Mostrar ElementId | bool | **true** | — | Exibir ElementId nos logs | Para debug e navegação ao elemento. |
| 04 | `UI_selecionar_ao_clicar` | Selecionar ao clicar log | bool | **true** | — | Clicar no log seleciona o elemento no modelo | Facilitador de revisão. |
| 05 | `UI_cor_status_ok` | Cor status OK | string | "#4CAF50" | hex | Verde para etapas concluídas | Visual. |
| 06 | `UI_cor_status_erro` | Cor status erro | string | "#F44336" | hex | Vermelho para erros | Visual. |
| 07 | `UI_cor_status_alerta` | Cor status alerta | string | "#FF9800" | hex | Laranja para alertas | Visual. |
| 08 | `UI_exportar_log_json` | Exportar log em JSON | bool | **true** | — | Ao final, exportar log completo | Para auditoria. |

### 2.9 Validação

| # | Nome técnico | Nome amigável | Tipo | Valor padrão | Unidade | Descrição | Impacto |
|---|-------------|--------------|------|-------------|---------|-----------|---------|
| 01 | `VAL_tolerancia_conexao` | Tolerância de conexão | float | **0.001** | m | Distância máx entre connectors para considerar conectado | 1mm = padrão Revit. |
| 02 | `VAL_tolerancia_cota` | Tolerância de cota | float | **0.001** | m | Diferença de Z aceitável para continuidade | ±1mm. |
| 03 | `VAL_confianca_minima_classificacao` | Confiança mín classificação | float | **0.70** | — | Abaixo: solicitar validação humana | 70% = balanceado. |
| 04 | `VAL_taxa_insercao_minima` | Taxa inserção mínima | float | **0.50** | — | Se < 50% dos equipamentos inseridos: Crítico | Proteção contra modelos difíceis. |
| 05 | `VAL_validar_dn_nunca_diminui` | Validar DN escoamento | bool | **true** | — | Verificar que DN ES nunca diminui a jusante | Normativo. |
| 06 | `VAL_validar_decliv_contra_grav` | Validar contra gravidade | bool | **true** | — | Verificar que nenhum trecho ES sobe | Normativo. |
| 07 | `VAL_validar_pressao_todos_pontos` | Validar pressão AF | bool | **true** | — | Calcular pressão em todos os pontos de consumo | Normativo. |
| 08 | `VAL_validar_velocidade_af` | Validar velocidade AF | bool | **true** | — | Verificar V ≤ V_max em todos os trechos | Normativo. |
| 09 | `VAL_validar_ventilacao_primaria` | Validar vent primária | bool | **true** | — | Verificar ventilação primária em todos os TQs | Normativo. |
| 10 | `VAL_nivel_bloqueio` | Nível de bloqueio | enum | "critico" | — | Valores: critico, medio | "critico": só bloqueia em Crítico. "medio": bloqueia também em Médio. |

---

## 3. Estrutura JSON Completa

```json
{
  "versao_config": "1.0.0",
  "data_criacao": "2026-03-18",
  
  "geral": {
    "versao_norma_af": "2020",
    "versao_norma_es": "1999",
    "tipo_edificacao": "residencial",
    "num_pavimentos": 2,
    "material_af": "pvc_soldavel",
    "material_es": "pvc_esgoto",
    "tipo_abastecimento": "reservatorio_superior",
    "unidade_pressao": "mca"
  },

  "agua_fria": {
    "pressao_minima_mca": 3.0,
    "pressao_maxima_mca": 40.0,
    "velocidade_maxima_ms": 3.0,
    "velocidade_minima_ms": 0.5,
    "coeficiente_C": 0.30,
    "tipo_vaso": "caixa_acoplada",
    "altura_reservatorio_m": 6.0,
    "fator_perdas_localizadas": 1.20,
    "formula_perda_carga": "fair_whipple_hsiao",
    "metodo_perdas_localizadas": "fator_percentual",
    "dn_minimo_subramal_mm": 20,
    "dn_minimo_barrilete_mm": 25,
    "registro_por_ambiente": true,
    "registro_geral": true
  },

  "esgoto": {
    "dn_minimo_ramal_vaso_mm": 100,
    "dn_minimo_subcoletor_mm": 100,
    "dn_minimo_ramal_descarga_mm": 40,
    "fecho_hidrico_minimo_mm": 50,
    "cx_sifonada_banheiro": true,
    "cx_gordura_cozinha": true,
    "ramal_vaso_independente": true,
    "distancia_max_CI_m": 15.0,
    "curva_90_horizontal": "alerta",
    "dn_nunca_diminui": true,
    "fitting_juncao_preferido": "tee_45",
    "profundidade_minima_enterramento_m": 0.30
  },

  "ventilacao": {
    "dn_minimo_absoluto_mm": 40,
    "fator_coluna_vs_TQ": 0.667,
    "fator_ramal_vs_descarga": 0.50,
    "terminal_altura_minima_m": 0.30,
    "terminal_distancia_janela_m": 4.00,
    "conexao_elevacao_minima_m": 0.15,
    "max_aparelhos_sem_vent": 4,
    "max_comprimento_ramal_sem_vent_m": 6.0,
    "pavimentos_para_secundaria": 3,
    "inclinacao_minima_ramal": 0.01
  },

  "declividade": {
    "modo_aplicacao": "recomendada",
    "tolerancia_cota_m": 0.001,
    "maxima_recomendada": 0.05,
    "maxima_absoluta": 0.08,
    "verificar_espaco_laje": true,
    "espessura_laje_padrao_m": 0.15,
    "aplicar_em_cascata": true,
    "reconectar_fittings": true,
    "por_diametro": {
      "DN_40":  { "minima": 0.020, "recomendada": 0.025 },
      "DN_50":  { "minima": 0.020, "recomendada": 0.025 },
      "DN_75":  { "minima": 0.020, "recomendada": 0.020 },
      "DN_100": { "minima": 0.010, "recomendada": 0.015 },
      "DN_150": { "minima": 0.0065, "recomendada": 0.010 },
      "DN_200": { "minima": 0.005, "recomendada": 0.007 }
    }
  },

  "dimensionamento": {
    "metodo_af": "probabilistico",
    "metodo_es": "uhc_tabela",
    "margem_seguranca_af": 1.0,
    "arredondar_dn_acima": true,
    "verificar_velocidade": true,
    "verificar_pressao": true,
    "atualizar_modelo": true,
    "gerar_relatorio": true,
    "precisao_vazao": 3,
    "precisao_perda_carga": 4
  },

  "automacao": {
    "modo": "semi_automatico",
    "aprovacao_por_etapa": true,
    "pausar_em_erro_medio": true,
    "bloquear_em_erro_critico": true,
    "criar_spaces_automatico": false,
    "inserir_equipamentos_auto": false,
    "roteamento_ferramenta": "dynamo",
    "max_tentativas_reconexao": 3,
    "taxa_falha_max_rollback": 0.30
  },

  "interface": {
    "idioma": "pt_BR",
    "nivel_log": "medio",
    "mostrar_elementid": true,
    "selecionar_ao_clicar": true,
    "cor_status_ok": "#4CAF50",
    "cor_status_erro": "#F44336",
    "cor_status_alerta": "#FF9800",
    "exportar_log_json": true
  },

  "validacao": {
    "tolerancia_conexao_m": 0.001,
    "tolerancia_cota_m": 0.001,
    "confianca_minima_classificacao": 0.70,
    "taxa_insercao_minima": 0.50,
    "validar_dn_nunca_diminui": true,
    "validar_decliv_contra_grav": true,
    "validar_pressao_todos_pontos": true,
    "validar_velocidade_af": true,
    "validar_ventilacao_primaria": true,
    "nivel_bloqueio": "critico"
  }
}
```

---

## 4. Regras de Interdependência

### 4.1 Interdependências AF

```
SE AF_tipo_vaso == "valvula_descarga":
  ENTÃO peso do vaso muda de 0.3 para 32.0
  ENTÃO AF_dn_minimo_subramal do vaso muda de 20 para 50mm
  ENTÃO dimensionamento da rede AF muda drasticamente
  → EXIGIR confirmação do usuário antes de aplicar

SE AF_velocidade_maxima ↓ (ex: 2.5 → 2.0):
  ENTÃO diâmetros calculados tendem a ↑ (para reduzir V)
  ENTÃO custo de material ↑

SE AF_fator_perdas_localizadas ↑ (ex: 1.20 → 1.30):
  ENTÃO perdas de carga calculadas ↑
  ENTÃO pressão disponível ↓
  ENTÃO mais pontos podem ficar com pressão insuficiente

SE AF_pressao_minima ↑ (ex: 3.0 → 5.0):
  ENTÃO mais trechos falham na verificação de pressão
  ENTÃO diâmetros precisam ser maiores OU reservatório mais alto

SE AF_altura_reservatorio ↓:
  ENTÃO pressão estática ↓ em todos os pontos
  ENTÃO mais pontos com pressão insuficiente
```

### 4.2 Interdependências ES

```
SE ES_dn_nunca_diminui == false (PERIGOSO):
  ENTÃO validação de DN no escoamento é desativada
  ENTÃO risco de dimensionamento incorreto
  → NÃO DEVE ser desativado

SE DEC_modo_aplicacao == "minima":
  ENTÃO desníveis menores
  ENTÃO mais espaço disponível sob a laje
  ENTÃO menor margem de autolimpeza

SE DEC_modo_aplicacao == "recomendada":
  ENTÃO desníveis maiores
  ENTÃO menor espaço disponível
  ENTÃO melhor autolimpeza
```

### 4.3 Interdependências de ventilação

```
SE VE_max_aparelhos_sem_vent ↑ (ex: 4 → 6):
  ENTÃO menos pontos de ventilação necessários
  ENTÃO menor custo, maior risco de sifonamento

SE VE_pavimentos_para_secundaria ↑ (ex: 3 → 5):
  ENTÃO menos projetos exigem coluna de ventilação
  ENTÃO risco de sifonamento em edifícios de 3-4 pavimentos

SE GER_num_pavimentos ≥ VE_pavimentos_para_secundaria:
  ENTÃO coluna de ventilação é obrigatória
```

### 4.4 Diagrama de dependências

```
AF_altura_reservatorio → Pressão estática → Pressão disponível
AF_velocidade_maxima → Seleção de DN → Perda de carga → Pressão disponível
AF_fator_perdas_localizadas → Perda de carga → Pressão disponível

ES_dn_minimo_ramal_vaso → DN ramal esgoto → DN TQ → DN subcoletor
DEC_modo_aplicacao → Desnível → Espaço na laje

VE_pavimentos_para_secundaria + GER_num_pavimentos → Necessidade de vent. secundária
VE_fator_coluna_vs_TQ + DN_TQ → DN da coluna de ventilação
```

---

## 5. Regras de Validação dos Parâmetros

### 5.1 Limites para cada parâmetro

| Parâmetro | Mínimo | Máximo | Justificativa |
|-----------|--------|--------|---------------|
| `AF_pressao_minima` | 0.5 | 10.0 | Norma: mín 0.5; acima de 10 é irreal |
| `AF_pressao_maxima` | 20.0 | 60.0 | 20 = edifício pequeno; 60 = limite de tubulação |
| `AF_velocidade_maxima` | 1.5 | 4.0 | 1.5 = muito conservador; 4.0 = limite extremo |
| `AF_velocidade_minima` | 0.1 | 1.0 | 0.1 = quase parado; 1.0 = normal |
| `AF_coeficiente_C` | 0.20 | 0.40 | Variação por tipo de aparelhos |
| `AF_altura_reservatorio` | 1.0 | 30.0 | 1m = mín; 30m = edifício alto |
| `AF_fator_perdas_localizadas` | 1.0 | 1.50 | 1.0 = sem localizadas (irreal); 1.5 = 50% (conservador extremo) |
| `ES_dn_minimo_ramal_vaso` | 100 | 100 | Fixo. Não pode ser alterado. |
| `ES_dn_minimo_subcoletor` | 100 | 200 | 100 = mínimo normativo |
| `ES_distancia_max_CI` | 5.0 | 25.0 | 5 = muito conservador; 25 = limite |
| `DEC_maxima_recomendada` | 0.03 | 0.10 | 3% a 10% |
| `DEC_tolerancia_cota` | 0.0005 | 0.005 | 0.5mm a 5mm |
| `VE_terminal_altura_minima` | 0.20 | 1.00 | 20cm a 1m |
| `VE_terminal_distancia_janela` | 2.0 | 6.0 | 2m (mín) a 6m (conservador) |
| `VE_max_aparelhos_sem_vent` | 2 | 6 | 2 = muito conservador |
| `VAL_confianca_minima_classificacao` | 0.50 | 0.95 | 50% = aceita tudo; 95% = muito restritivo |
| `AUT_taxa_falha_max_rollback` | 0.10 | 0.50 | 10% a 50% |

### 5.2 Validação ao salvar configuração

```
PROCEDIMENTO: Validar configuração antes de salvar

PARA cada parâmetro alterado:
  1. Verificar tipo (int, float, bool, enum, string)
  2. Verificar se valor está dentro de [mín, máx]
  3. SE fora dos limites:
     REJEITAR e exibir mensagem: "Valor {valor} fora do intervalo [{min}, {max}]"
  4. SE parâmetro é bloqueado (ex: ES_dn_minimo_ramal_vaso):
     REJEITAR e exibir: "Parâmetro normativo — não pode ser alterado"
  5. SE parâmetro é crítico e valor mudou significativamente:
     EXIGIR confirmação: "Alterar {nome} de {antigo} para {novo}? Impacto: {desc}"

SE todas validações OK:
  Salvar JSON
  Registrar log: "Configuração alterada: {parâmetro} = {valor_novo}"
```

---

## 6. Parâmetros Críticos

### 6.1 Parâmetros que impactam diretamente o dimensionamento

| Parâmetro | Impacto | Risco se alterado incorretamente |
|-----------|---------|----------------------------------|
| `AF_pressao_minima` | Define se pontos passam na verificação | Valor baixo demais = chuveiro sem água |
| `AF_velocidade_maxima` | Define DN mínimo dos trechos AF | Valor alto = tubos menores = ruído e desgaste |
| `AF_coeficiente_C` | Altera TODA a vazão calculada | Valor incorreto = rede sub/superdimensionada |
| `AF_altura_reservatorio` | Base de TODA a pressão do sistema AF | Valor errado = dimensionamento incorreto |
| `AF_tipo_vaso` | Peso muda de 0.3 para 32.0 | Erro = rede completamente inadequada |
| `ES_dn_minimo_ramal_vaso` | DN 100 obrigatório | Alterar = violação normativa |
| `DEC_modo_aplicacao` | Declividade aplicada automaticamente | "minima" pode ser insuficiente na prática |

### 6.2 Parâmetros que podem gerar erro grave

| Parâmetro | Erro possível | Proteção |
|-----------|--------------|----------|
| `AF_tipo_vaso` = "valvula_descarga" sem confirmar | Peso 32.0 aplicado → rede superdimensionada | Exigir confirmação |
| `ES_dn_nunca_diminui` = false | DN diminui no escoamento → entupimento | Bloquear alteração |
| `ES_ramal_vaso_independente` = false | Vaso na CX sifonada → sifonamento | Bloquear alteração |
| `AUT_bloquear_em_erro_critico` = false | Pipeline avança com erro crítico → danos | Bloquear alteração |
| `VAL_validar_pressao_todos_pontos` = false | Pressão não verificada → chuveiros sem água | Exigir confirmação |

---

## 7. Estratégia de Override

### 7.1 Classificação de parâmetros

| Classificação | Comportamento | Exemplos |
|--------------|--------------|----------|
| **🔒 Bloqueado** | Não pode ser alterado pelo usuário. Valor fixo normativo. | `ES_dn_minimo_ramal_vaso`, `ES_ramal_vaso_independente`, `ES_dn_nunca_diminui`, `AUT_bloquear_em_erro_critico` |
| **⚠️ Confirmação** | Pode ser alterado, mas exige confirmação explícita com aviso de impacto. | `AF_tipo_vaso`, `AF_pressao_minima`, `AF_velocidade_maxima`, `AF_coeficiente_C`, `AF_altura_reservatorio`, `DEC_modo_aplicacao` |
| **🔓 Livre** | Pode ser alterado livremente dentro dos limites. | `UI_*`, `GER_num_pavimentos`, `DEC_espessura_laje_padrao`, `VE_max_aparelhos_sem_vent` |

### 7.2 Tabela completa de override

| Parâmetro | Override | Nível |
|-----------|---------|-------|
| `GER_versao_norma_af` | 🔓 Livre | — |
| `GER_versao_norma_es` | 🔓 Livre | — |
| `GER_tipo_edificacao` | ⚠️ Confirmação | Altera pesos e UHCs |
| `GER_num_pavimentos` | 🔓 Livre | — |
| `GER_material_af` | ⚠️ Confirmação | Altera perda de carga |
| `GER_material_es` | ⚠️ Confirmação | Altera DNs |
| `AF_pressao_minima` | ⚠️ Confirmação | Altera verificação de pressão |
| `AF_pressao_maxima` | 🔓 Livre | — |
| `AF_velocidade_maxima` | ⚠️ Confirmação | Altera seleção de DN |
| `AF_coeficiente_C` | ⚠️ Confirmação | Altera toda a vazão |
| `AF_tipo_vaso` | ⚠️ Confirmação | Impacto drástico |
| `AF_altura_reservatorio` | ⚠️ Confirmação | Base da pressão |
| `AF_fator_perdas_localizadas` | 🔓 Livre | — |
| `ES_dn_minimo_ramal_vaso` | 🔒 Bloqueado | Normativo |
| `ES_dn_minimo_subcoletor` | 🔒 Bloqueado | Normativo |
| `ES_dn_nunca_diminui` | 🔒 Bloqueado | Normativo |
| `ES_ramal_vaso_independente` | 🔒 Bloqueado | Normativo |
| `ES_cx_sifonada_banheiro` | ⚠️ Confirmação | Normativo |
| `ES_cx_gordura_cozinha` | ⚠️ Confirmação | Normativo |
| `DEC_modo_aplicacao` | ⚠️ Confirmação | Altera toda declividade |
| `DEC_*` (demais) | 🔓 Livre | — |
| `VE_*` (todos) | 🔓 Livre | — |
| `DIM_*` (todos) | 🔓 Livre | — |
| `AUT_bloquear_em_erro_critico` | 🔒 Bloqueado | Segurança |
| `AUT_*` (demais) | 🔓 Livre | — |
| `UI_*` (todos) | 🔓 Livre | — |
| `VAL_validar_dn_nunca_diminui` | 🔒 Bloqueado | Normativo |
| `VAL_validar_decliv_contra_grav` | 🔒 Bloqueado | Normativo |
| `VAL_*` (demais) | ⚠️ Confirmação | Desativar validação = risco |

### 7.3 Mensagens de confirmação

```json
{
  "mensagens_confirmacao": {
    "AF_tipo_vaso": "Alterar tipo de vaso para '{valor}'? O peso AF muda de 0.3 para 32.0, impactando TODO o dimensionamento da rede.",
    "AF_pressao_minima": "Alterar pressão mínima para {valor} m.c.a.? Isso pode aprovar/reprovar pontos que estavam no limite.",
    "AF_velocidade_maxima": "Alterar velocidade máxima para {valor} m/s? Isso pode alterar diâmetros selecionados em toda a rede.",
    "AF_coeficiente_C": "Alterar coeficiente C para {valor}? Isso altera TODA a vazão calculada no projeto.",
    "AF_altura_reservatorio": "Alterar altura do reservatório para {valor}m? Isso recalcula TODA a pressão disponível.",
    "GER_material_af": "Alterar material para '{valor}'? Coeficientes de perda de carga e diâmetros internos serão recalculados.",
    "DEC_modo_aplicacao": "Alterar modo de declividade para '{valor}'? Todos os desníveis serão recalculados."
  }
}
```
