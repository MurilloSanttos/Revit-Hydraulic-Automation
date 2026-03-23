# Guia de Criação — Modelo de Teste com Inconsistências

> Instruções para criar um modelo Revit propositalmente problemático, simulando erros reais de projeto para validar o sistema de detecção, validação e tratamento de erros do plugin.

---

## 📋 Índice

- [Visão Geral](#visão-geral)
- [Ambientes com Erros](#1-ambientes-roomsspaces)
- [Classificação com Erros](#2-classificação-de-ambientes)
- [Equipamentos com Erros](#3-equipamentos-hidráulicos)
- [Pontos Hidráulicos com Erros](#4-pontos-hidráulicos)
- [Prumadas com Erros](#5-prumadas)
- [Rede AF com Erros](#6-rede-de-água-fria)
- [Rede ES com Erros](#7-rede-de-esgoto)
- [Ventilação com Erros](#8-ventilação)
- [Dimensionamento com Erros](#9-dimensionamento)
- [Organização com Erros](#10-documentação-e-organização)
- [Tabela Consolidada](#11-tabela-consolidada-de-erros)
- [Checklist Final](#12-checklist-final)

---

## Visão Geral

### Arquivo

```
Modelo_Teste_Erros.rvt
```

### Base

Partir de uma **cópia** do modelo funcional (`Modelo_Teste_Hidraulico.rvt`) e introduzir as inconsistências descritas abaixo.

### Classificação de erros

| Severidade | Código | Ação no pipeline | Cor |
|-----------|--------|------------------|-----|
| **Crítico** | 🔴 C | Bloqueia execução — pipeline para | Vermelho |
| **Médio** | 🟡 M | Pausa para aprovação — usuário decide | Amarelo |
| **Leve** | 🟢 L | Log de aviso — pipeline continua | Verde |

### Categorias de erro

| Código | Categoria |
|--------|-----------|
| `AMB` | Ambientes (Rooms/Spaces) |
| `CLS` | Classificação |
| `EQP` | Equipamentos |
| `PNT` | Pontos hidráulicos |
| `PRU` | Prumadas |
| `RAF` | Rede de água fria |
| `RES` | Rede de esgoto |
| `VEN` | Ventilação |
| `DIM` | Dimensionamento |
| `DOC` | Documentação |

---

## 1. Ambientes (Rooms/Spaces)

### AMB-01 — Room sem nome

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `AMB-01` |
| **Categoria** | Validação de dados |
| **Impacto** | Impossível classificar ambiente |

**Como criar no Revit:**

1. Vá para a vista **Térreo**.
2. **Architecture** → **Room** → clique em um espaço válido.
3. Quando o cursor solicitar o nome, pressione **Esc** ou deixe em branco.
4. No **Properties**, apague o campo **Name** (deixe vazio).
5. O Room deve ter: Name = `""`, Number = `07`.

---

### AMB-02 — Room com nome genérico

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `AMB-02` |
| **Categoria** | Classificação |
| **Impacto** | Classificador retorna `Unknown` |

**Como criar no Revit:**

1. Crie um Room adicional em uma área livre.
2. Nomeie como `Ambiente 01`.
3. Crie outro e nomeie como `Espaço`.
4. Crie outro e nomeie como `Room 1`.

---

### AMB-03 — Room "Not Enclosed"

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `AMB-03` |
| **Categoria** | Geometria |
| **Impacto** | Área = 0, impossível processar |

**Como criar no Revit:**

1. Selecione uma parede interna que delimita um ambiente.
2. Delete apenas **um segmento** da parede (deixe um gap de ~50 cm).
3. O Room dentro perde o boundary → vira "Not Enclosed".
4. Confirme no Properties: **Area = Not Enclosed**.

---

### AMB-04 — Room duplicado (mesmo nome)

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `AMB-04` |
| **Categoria** | Duplicação |
| **Impacto** | Ambiguidade na classificação |

**Como criar no Revit:**

1. Crie um Room e nomeie como `Banheiro - Social`.
2. Crie outro Room (em outro local) e nomeie também como `Banheiro - Social`.
3. Ambos com o mesmo nome mas Number diferente.

---

### AMB-05 — Room sem Space correspondente

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `AMB-05` |
| **Categoria** | Dados MEP |
| **Impacto** | Plugin não consegue criar Spaces automaticamente |

**Como criar no Revit:**

1. Crie um Room chamado `Banheiro - Teste` em um novo espaço.
2. **NÃO crie** o Space MEP correspondente.
3. O Room existe mas sem Space MEP.

---

### AMB-06 — Space sem Room correspondente

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `AMB-06` |
| **Categoria** | Dados MEP |
| **Impacto** | Space órfão — pode ser ignorado |

**Como criar no Revit:**

1. Vá para a vista MEP.
2. Crie um Space em uma área sem Room.
3. Nomeie como `Space Órfão`.

---

### AMB-07 — Room com área absurda

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `AMB-07` |
| **Categoria** | Validação |
| **Impacto** | Classificação aceita mas área está fora do esperado |

**Como criar no Revit:**

1. Crie uma parede formando um espaço de **30 m × 30 m** (900 m²).
2. Crie um Room e nomeie como `Banheiro - Gigante`.
3. Área de 900 m² é absurda para um banheiro (esperado: 2-12 m²).

---

## 2. Classificação de Ambientes

### CLS-01 — Nome trocado (banheiro como quarto)

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `CLS-01` |
| **Categoria** | Classificação incorreta |
| **Impacto** | Equipamentos errados serão definidos |

**Como criar no Revit:**

1. Localize o Room do banheiro existente.
2. Renomeie de `Banheiro - Social` para `Quarto - 03`.
3. O ambiente tem equipamentos hidráulicos mas será classificado como `DryArea`.

---

### CLS-02 — Nome ambíguo multi-tipo

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `CLS-02` |
| **Categoria** | Classificação ambígua |
| **Impacto** | Classificador pode escolher errado |

**Como criar no Revit:**

1. Crie um Room e nomeie como `Sala / Cozinha`.
2. Crie outro e nomeie como `Banheiro Cozinha`.
3. Ambos contêm palavras-chave de múltiplos tipos.

---

### CLS-03 — Nome em idioma misto

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `CLS-03` |
| **Categoria** | Classificação |
| **Impacto** | Pode falhar se classificador não suportar inglês |

**Como criar no Revit:**

1. Crie Rooms com nomes:
   - `Bathroom - Social`
   - `Kitchen`
   - `Living Room`

---

### CLS-04 — Ambiente seco com equipamento hidráulico

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `CLS-04` |
| **Categoria** | Inconsistência lógica |
| **Impacto** | Classificação diz "seco" mas tem equipamentos |

**Como criar no Revit:**

1. No Room `Sala de Estar` (classificado como `DryArea`):
2. Insira um vaso sanitário no centro da sala.
3. O classificador dirá "sem hidráulica" mas existe equipamento.

---

## 3. Equipamentos Hidráulicos

### EQP-01 — Banheiro sem vaso sanitário

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `EQP-01` |
| **Categoria** | Equipamento obrigatório ausente |
| **Impacto** | Rede de esgoto incompleta |

**Como criar no Revit:**

1. Em um banheiro, **delete o vaso sanitário**.
2. Mantenha lavatório e chuveiro.
3. O banheiro fica sem peça obrigatória.

---

### EQP-02 — Equipamento duplicado

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `EQP-02` |
| **Categoria** | Duplicação |
| **Impacto** | Cálculo de vazão fica incorreto |

**Como criar no Revit:**

1. No banheiro, insira **2 vasos sanitários** (um sobre o outro ou lado a lado).
2. Ou insira **2 lavatórios** sobrepostos no mesmo ponto.

---

### EQP-03 — Equipamento em ambiente errado

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `EQP-03` |
| **Categoria** | Posicionamento |
| **Impacto** | Equipamento não pertence ao tipo de ambiente |

**Como criar no Revit:**

1. Insira um **vaso sanitário** dentro do Room `Sala de Estar`.
2. Insira um **chuveiro** dentro do Room `Cozinha`.
3. Equipamentos válidos mas em ambientes incompatíveis.

---

### EQP-04 — Equipamento fora de qualquer Room

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `EQP-04` |
| **Categoria** | Posicionamento |
| **Impacto** | Equipamento sem associação com ambiente |

**Como criar no Revit:**

1. Insira um lavatório **fora das paredes** (na área externa, sem Room).
2. O equipamento existe no modelo mas não pertence a nenhum ambiente.

---

### EQP-05 — Equipamento flutuando (sem parede)

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `EQP-05` |
| **Categoria** | Posicionamento |
| **Impacto** | Impossível criar conexão com tubulação |

**Como criar no Revit:**

1. Selecione um lavatório (wall-mounted).
2. Mova-o para o **centro do ambiente**, longe das paredes.
3. O equipamento está no Room correto mas sem suporte físico.

---

### EQP-06 — Equipamento sem conector

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `EQP-06` |
| **Categoria** | Família |
| **Impacto** | Rede não pode ser conectada automaticamente |

**Como criar no Revit:**

1. Insira um **Generic Model** no formato de pia.
2. Use uma família sem Plumbing Connectors.
3. O objeto parece uma pia mas não é reconhecido pelo MEP.

---

## 4. Pontos Hidráulicos

### PNT-01 — Ponto sem equipamento associado

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `PNT-01` |
| **Categoria** | Dados |
| **Impacto** | Ponto hidráulico sem origem |

**Como criar no Revit:**

1. Delete um equipamento que já tinha ponto hidráulico definido.
2. O ponto fica órfão (referência a equipamento que não existe).

---

### PNT-02 — Ponto duplicado no mesmo equipamento

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `PNT-02` |
| **Categoria** | Duplicação |
| **Impacto** | Vazão calculada em dobro |

**Simulação lógica** (via dados do plugin):

1. No JSON de pontos, insira dois registros com o mesmo `equipmentId`.
2. Ambos com `system: "ColdWater"`.

---

### PNT-03 — Ponto em posição inacessível

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `PNT-03` |
| **Categoria** | Posicionamento |
| **Impacto** | Rede não consegue rotear até o ponto |

**Como criar no Revit:**

1. Mova um equipamento para dentro de uma parede (parcialmente embeddado).
2. O conector fica inacessível para roteamento.

---

## 5. Prumadas

### PRU-01 — Ambiente molhado sem prumada

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `PRU-01` |
| **Categoria** | Rede |
| **Impacto** | Impossível conectar equipamentos à rede vertical |

**Simulação:**

1. Ao executar o plugin, pule a etapa E06 (Criar Prumadas).
2. Tente executar E07-E09 (redes) sem prumadas criadas.
3. O pipeline deve detectar dependência não atendida.

---

### PRU-02 — Prumada desalinhada

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `PRU-02` |
| **Categoria** | Geometria |
| **Impacto** | Conexões com offset excessivo |

**Como criar no Revit:**

1. Crie um pipe vertical (prumada manual).
2. Posicione a **3 metros** do equipamento mais próximo.
3. Distância máxima aceitável: 1.5m da parede hidráulica.

---

### PRU-03 — Prumada com diâmetro fixo

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `PRU-03` |
| **Categoria** | Dimensionamento |
| **Impacto** | Diâmetro pode estar sub/superdimensionado |

**Como criar no Revit:**

1. Crie prumada com diâmetro fixo de **25 mm** para todos os sistemas.
2. Esgoto deveria ser ≥75mm, água fria depende da vazão.

---

## 6. Rede de Água Fria

### RAF-01 — Trecho desconectado

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `RAF-01` |
| **Categoria** | Conectividade |
| **Impacto** | Equipamento sem abastecimento |

**Como criar no Revit:**

1. Desenhe uma rede de pipes nos ambientes.
2. Delete **um segmento intermediário** (deixe um gap de 20cm).
3. A rede fica desconexa — parte do sistema sem alimentação.

---

### RAF-02 — Loop na rede

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `RAF-02` |
| **Categoria** | Topologia |
| **Impacto** | Cálculo de perda de carga fica indeterminado |

**Como criar no Revit:**

1. Crie a rede de AF com um trecho que **retorna ao ponto de origem**.
2. Forme um "anel" — a rede não deve ter loops em residencial.

---

### RAF-03 — Diâmetro incompatível

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `RAF-03` |
| **Categoria** | Dimensionamento |
| **Impacto** | Velocidade excessiva ou pressão insuficiente |

**Como criar no Revit:**

1. Use um pipe de **15 mm** para alimentar 6 equipamentos.
2. A vazão demandada exige no mínimo 25mm.

---

### RAF-04 — Sem conexão ao reservatório

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `RAF-04` |
| **Categoria** | Conectividade |
| **Impacto** | Sistema sem fonte de água |

**Simulação:**

1. Desenhe a rede de AF sem conectar ao ponto de alimentação.
2. A rede existe mas "começa do nada".

---

### RAF-05 — Redução invertida

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `RAF-05` |
| **Categoria** | Dimensionamento |
| **Impacto** | Diâmetro aumenta no sentido do fluxo (inverso do correto) |

**Como criar no Revit:**

1. Crie trecho com diâmetro 25mm → 32mm → 40mm no sentido do fluxo.
2. O correto seria diminuir: 40mm → 32mm → 25mm.

---

## 7. Rede de Esgoto

### RES-01 — Sem declividade

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `RES-01` |
| **Categoria** | Hidráulica |
| **Impacto** | Esgoto não escoa — acúmulo de dejetos |

**Como criar no Revit:**

1. Desenhe pipes de esgoto horizontais com **slope = 0%**.
2. O esgoto exige mínimo de 1% (NBR 8160).

---

### RES-02 — Declividade invertida

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `RES-02` |
| **Categoria** | Hidráulica |
| **Impacto** | Esgoto escoa na direção errada |

**Como criar no Revit:**

1. Crie pipe com slope negativo (caindo para o lado errado).
2. Ou crie pipe que sobe para o ponto de destino.

---

### RES-03 — Vaso conectado a tubo de 40 mm

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `RES-03` |
| **Categoria** | Dimensionamento |
| **Impacto** | NBR 8160 exige mínimo de 100mm para vaso sanitário |

**Como criar no Revit:**

1. Conecte a saída do vaso sanitário a um pipe de **40 mm**.
2. O mínimo normativo é 100mm.

---

### RES-04 — Junção sem caixa de inspeção

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `RES-04` |
| **Categoria** | Norma |
| **Impacto** | Manutenção impossível — viola norma |

**Simulação:**

1. Crie junção de 3+ ramais sem inserir caixa sifonada/inspeção.
2. Junções com mais de 2 entradas exigem inspeção.

---

### RES-05 — Saída para rede pública ausente

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `RES-05` |
| **Categoria** | Conectividade |
| **Impacto** | Esgoto "termina no nada" |

**Simulação:**

1. A rede de esgoto termina dentro do lote sem conexão externa.

---

## 8. Ventilação

### VEN-01 — Coluna de esgoto sem ventilação

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `VEN-01` |
| **Categoria** | Norma |
| **Impacto** | Sifonamento dos aparelhos (mau cheiro) |

**Simulação:**

1. Crie rede de esgoto completa.
2. **NÃO adicione** tubo de ventilação.
3. NBR 8160 exige ventilação em toda coluna de esgoto.

---

### VEN-02 — Ventilação que não chega à cobertura

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `VEN-02` |
| **Categoria** | Norma |
| **Impacto** | Ventilação ineficaz |

**Como criar no Revit:**

1. Crie tubo de ventilação que termina **dentro do forro** (nível Forro).
2. Deveria ultrapassar a cobertura em 30 cm mínimo.

---

### VEN-03 — Ventilação conectada a retorno de esgoto

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `VEN-03` |
| **Categoria** | Conexão |
| **Impacto** | Gás de esgoto retorna pelo sistema |

**Simulação:**

1. Conecte o tubo de ventilação ao ramal de descarga em vez do ramal de ventilação.
2. Conexão deve ser feita acima do nível de transbordamento.

---

## 9. Dimensionamento

### DIM-01 — Velocidade excessiva na rede

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟡 Médio |
| **ErrorCode** | `DIM-01` |
| **Categoria** | Cálculo |
| **Impacto** | Ruído e desgaste da tubulação |

**Simulação (via dados):**

1. Diâmetro 20mm com vazão que resulta em v > 3.0 m/s.
2. NBR 5626 limita em 3.0 m/s.

---

### DIM-02 — Pressão disponível insuficiente

| Campo | Valor |
|-------|-------|
| **Severidade** | 🔴 Crítico |
| **ErrorCode** | `DIM-02` |
| **Categoria** | Cálculo |
| **Impacto** | Equipamento mais desfavorável sem pressão mínima |

**Simulação (via dados):**

1. Configure pressão de alimentação = 5 mca.
2. Percurso com ΣhF > 5 mca → pressão residual negativa.

---

### DIM-03 — Peso da peça (UHC) errado

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `DIM-03` |
| **Categoria** | Dados |
| **Impacto** | Vazão calculada incorretamente |

**Simulação (via dados):**

1. Atribua peso 5.0 UHC a um lavatório (correto: 0.3).
2. Vazão provável fica muito inflada.

---

## 10. Documentação e Organização

### DOC-01 — Vista sem nome padrão

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `DOC-01` |
| **Categoria** | Organização |
| **Impacto** | Pranchas geradas com nomes confusos |

**Como criar no Revit:**

1. Renomeie a planta baixa para `View 1`.
2. Renomeie a vista 3D para `{3D} - Copy`.

---

### DOC-02 — Parâmetros de projeto incompletos

| Campo | Valor |
|-------|-------|
| **Severidade** | 🟢 Leve |
| **ErrorCode** | `DOC-02` |
| **Categoria** | Dados |
| **Impacto** | Cabeçalhos de prancha vazios |

**Como criar no Revit:**

1. **Manage** → **Project Information**.
2. Deixe `Project Name`, `Client Name` e `Author` em branco.

---

## 11. Tabela Consolidada de Erros

### Por severidade

| Severidade | Qtd | ErrorCodes |
|-----------|-----|------------|
| 🔴 **Crítico** | **13** | AMB-01, AMB-03, EQP-01, EQP-04, PRU-01, RAF-01, RAF-04, RES-01, RES-02, RES-03, VEN-01, VEN-03, DIM-02 |
| 🟡 **Médio** | **16** | AMB-02, AMB-04, AMB-05, AMB-07, CLS-01, CLS-02, CLS-04, EQP-02, EQP-03, EQP-06, PNT-01, PNT-03, PRU-02, RAF-02, RAF-03, RAF-05, RES-04, RES-05, VEN-02, DIM-01 |
| 🟢 **Leve** | **8** | AMB-06, CLS-03, EQP-05, PNT-02, PRU-03, DIM-03, DOC-01, DOC-02 |
| **TOTAL** | **37** | |

### Por categoria

| Categoria | Qtd | Erros |
|-----------|-----|-------|
| **Ambientes** | 7 | AMB-01 a AMB-07 |
| **Classificação** | 4 | CLS-01 a CLS-04 |
| **Equipamentos** | 6 | EQP-01 a EQP-06 |
| **Pontos** | 3 | PNT-01 a PNT-03 |
| **Prumadas** | 3 | PRU-01 a PRU-03 |
| **Rede AF** | 5 | RAF-01 a RAF-05 |
| **Rede ES** | 5 | RES-01 a RES-05 |
| **Ventilação** | 3 | VEN-01 a VEN-03 |
| **Dimensionamento** | 3 | DIM-01 a DIM-03 |
| **Documentação** | 2 | DOC-01 a DOC-02 |

### Por etapa do pipeline impactada

| Etapa | Erros que devem ser detectados |
|-------|-------------------------------|
| **E01** Detectar Ambientes | AMB-01, AMB-03, AMB-06 |
| **E02** Classificar | AMB-02, AMB-04, AMB-07, CLS-01, CLS-02, CLS-03 |
| **E03** Identificar Equipamentos | CLS-04, EQP-01, EQP-02, EQP-03, EQP-04, EQP-06 |
| **E04** Inserir Equipamentos | EQP-05 |
| **E05** Validar Modelo | AMB-05, PNT-01, PNT-02, PNT-03 |
| **E06** Criar Prumadas | PRU-01, PRU-02, PRU-03 |
| **E07** Rede AF | RAF-01, RAF-02, RAF-03, RAF-04, RAF-05 |
| **E08** Rede ES | RES-01, RES-02, RES-03, RES-04, RES-05 |
| **E09** Ventilação | VEN-01, VEN-02, VEN-03 |
| **E11** Dimensionar | DIM-01, DIM-02, DIM-03 |
| **E12** Tabelas/Pranchas | DOC-01, DOC-02 |

---

## 12. Checklist Final

### Modelo de erros criado

```
AMBIENTES
[ ] AMB-01: Room sem nome criado
[ ] AMB-02: Rooms com nomes genéricos (3 unidades)
[ ] AMB-03: Room "Not Enclosed" (gap na parede)
[ ] AMB-04: Rooms com nomes duplicados
[ ] AMB-05: Room sem Space MEP
[ ] AMB-06: Space sem Room
[ ] AMB-07: Banheiro com área absurda (900 m²)

CLASSIFICAÇÃO
[ ] CLS-01: Banheiro renomeado como "Quarto - 03"
[ ] CLS-02: Rooms com nomes ambíguos multi-tipo
[ ] CLS-03: Rooms com nomes em inglês
[ ] CLS-04: Equipamento hidráulico em ambiente seco

EQUIPAMENTOS
[ ] EQP-01: Banheiro sem vaso sanitário
[ ] EQP-02: Equipamentos duplicados (2 vasos)
[ ] EQP-03: Vaso na sala, chuveiro na cozinha
[ ] EQP-04: Lavatório fora de qualquer Room
[ ] EQP-05: Lavatório flutuando no centro do ambiente
[ ] EQP-06: Generic Model sem connector no lugar de pia

PONTOS
[ ] PNT-01: Equipamento deletado deixando ponto órfão
[ ] PNT-02: Ponto duplicado no JSON
[ ] PNT-03: Equipamento dentro de parede

PRUMADAS
[ ] PRU-01: Dependência de etapa não atendida
[ ] PRU-02: Prumada a 3m do equipamento
[ ] PRU-03: Prumada de 25mm para esgoto

REDE AF
[ ] RAF-01: Gap de 20cm na rede
[ ] RAF-02: Loop na rede
[ ] RAF-03: Pipe de 15mm para 6 equipamentos
[ ] RAF-04: Rede sem ponto de alimentação
[ ] RAF-05: Diâmetros crescentes no sentido do fluxo

REDE ES
[ ] RES-01: Pipes com slope = 0%
[ ] RES-02: Slope negativo (direção errada)
[ ] RES-03: Vaso conectado a tubo de 40mm
[ ] RES-04: Junção sem caixa de inspeção
[ ] RES-05: Rede sem saída para rede pública

VENTILAÇÃO
[ ] VEN-01: Coluna sem ventilação
[ ] VEN-02: Ventilação termina no forro
[ ] VEN-03: Ventilação conectada ao ramal de descarga

DIMENSIONAMENTO
[ ] DIM-01: v > 3.0 m/s no trecho
[ ] DIM-02: Pressão residual negativa
[ ] DIM-03: Peso UHC errado (5.0 em lavatório)

DOCUMENTAÇÃO
[ ] DOC-01: Vistas com nomes genéricos
[ ] DOC-02: Informações do projeto em branco
```

### Salvar

```
C:\Users\User\Desktop\PluginRevit\Data\Modelo_Teste_Erros.rvt
```

---

> **37 erros controlados prontos para validação completa do sistema de detecção e tratamento de erros do plugin.**
