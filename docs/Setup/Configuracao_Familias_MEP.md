# Guia de Configuração — Famílias MEP e Sistemas de Tubulação

> Instruções passo a passo para carregar, configurar e validar todas as famílias hidráulicas necessárias no modelo de teste, garantindo compatibilidade com o plugin de automação.

---

## 📋 Índice

- [Sistemas de Tubulação](#1-sistemas-de-tubulação)
- [Tipos de Tubo e Diâmetros](#2-tipos-de-tubo-e-diâmetros)
- [Famílias de Equipamentos](#3-famílias-de-equipamentos)
- [Conectores MEP](#4-conectores-mep)
- [Parâmetros Obrigatórios](#5-parâmetros-obrigatórios)
- [Carregar Famílias no Modelo](#6-carregar-famílias-no-modelo)
- [Posicionar Equipamentos](#7-posicionar-equipamentos)
- [Testar Conexões](#8-testar-conexões)
- [Configurações do Plugin](#9-configurações-para-o-plugin)
- [Validação Final](#10-validação-final)

---

## 1. Sistemas de Tubulação

### 1.1 Criar sistemas

No Revit, os sistemas de tubulação devem ser configurados antes de inserir qualquer pipe.

1. **Systems** → **Piping** → **Pipe** (ou digite `PI`).
2. No **Properties** → **System Type**, verifique os sistemas disponíveis.
3. Se não existirem, crie via **Manage** → **MEP Settings** → **Piping Systems**.

### 1.2 Sistemas obrigatórios

| # | System Name | System Type | Abreviação | Cor sugerida |
|---|------------|-------------|------------|-------------|
| 1 | **Água Fria** | Supply (Domestic Cold Water) | AF | Azul `#2196F3` |
| 2 | **Água Quente** | Supply (Domestic Hot Water) | AQ | Vermelho `#F44336` |
| 3 | **Esgoto Sanitário** | Sanitary | ES | Marrom `#795548` |
| 4 | **Ventilação** | Vent | VE | Verde `#4CAF50` |
| 5 | **Águas Pluviais** | Storm | AP | Cinza `#9E9E9E` |

### 1.3 Configurar via Manage

1. **Manage** → **MEP Settings** → **Mechanical Settings**.
2. Expanda **Piping**.
3. Em **Piping Systems**, verifique se cada tipo existe.
4. Para cada sistema, configure:

   | Propriedade | Valor |
   |-------------|-------|
   | **System Name** | Nome da tabela acima |
   | **System Abbreviation** | AF, AQ, ES, VE, AP |
   | **Fluid Type** | Water (para AF/AQ) |
   | **Fluid Temperature** | 20°C (AF), 60°C (AQ) |
   | **Graphics Override** | Cor correspondente |

### 1.4 Gráficos por sistema

1. Vá em **Manage** → **MEP Settings** → **Mechanical Settings** → **Piping** → **Piping Systems**.
2. Para cada sistema, configure **Graphics**:

   | Sistema | Padrão de linha | Cor | Espessura |
   |---------|----------------|-----|-----------|
   | AF | Contínua | Azul | 1 |
   | AQ | Tracejada | Vermelho | 1 |
   | ES | Contínua | Marrom | 2 |
   | VE | Tracejada | Verde | 1 |
   | AP | Ponto-traço | Cinza | 1 |

> ✅ **Checkpoint**: 5 sistemas de tubulação criados e configurados.

---

## 2. Tipos de Tubo e Diâmetros

### 2.1 Pipe Types

Crie ou valide os tipos de tubulação para cada material:

1. **Systems** → **Piping** → **Pipe**.
2. No **Properties** → **Type Selector**, verifique os tipos existentes.
3. Se necessário, crie novos via **Edit Type** → **Duplicate**.

### 2.2 Tipos obrigatórios

| # | Pipe Type Name | Material | Uso principal | Método de conexão |
|---|---------------|----------|---------------|-------------------|
| 1 | **PVC Soldável - Água Fria** | PVC | AF | Soldável |
| 2 | **PVC Esgoto Série Normal** | PVC | ES | Junta elástica |
| 3 | **PVC Ventilação** | PVC | VE | Soldável |
| 4 | **CPVC - Água Quente** | CPVC | AQ | Soldável |
| 5 | **PPR - Água** | PPR | AF/AQ | Termofusão |

### 2.3 Configurar cada Pipe Type

Para cada tipo, configure em **Edit Type**:

| Parâmetro | PVC AF | PVC ES | PVC VE | CPVC AQ |
|-----------|--------|--------|--------|---------|
| **Material** | PVC | PVC | PVC | CPVC |
| **Roughness** | 0.0015 mm | 0.0015 mm | 0.0015 mm | 0.0015 mm |
| **Fitting** | PVC Fittings | PVC Fittings | PVC Fittings | CPVC Fittings |
| **Segment** | Appropriate | Appropriate | Appropriate | Appropriate |

### 2.4 Tabela de diâmetros padrão

#### Água Fria (NBR 5626)

| DN (mm) | Diâmetro interno (mm) | Uso típico |
|---------|----------------------|-----------|
| **20** | 17.0 | Ramal de peça (lavatório, chuveiro) |
| **25** | 21.6 | Ramal de distribuição (2-3 peças) |
| **32** | 27.8 | Sub-ramal (4-6 peças) |
| **40** | 35.2 | Coluna de distribuição pequena |
| **50** | 44.0 | Alimentador predial pequeno |
| **60** | 53.4 | Coluna principal |
| **75** | 66.6 | Alimentador predial médio |

#### Esgoto (NBR 8160)

| DN (mm) | Diâmetro interno (mm) | Uso típico |
|---------|----------------------|-----------|
| **40** | 38.0 | Ramal de lavatório, bidê |
| **50** | 47.0 | Ramal de pia, tanque |
| **75** | 71.6 | Ramal coletivo, ralo sifonado |
| **100** | 97.0 | Ramal de vaso sanitário, coluna |
| **150** | 144.0 | Coletor predial |

#### Ventilação (NBR 8160)

| DN (mm) | Uso típico |
|---------|-----------|
| **40** | Ventilação individual |
| **50** | Ventilação de coluna pequena |
| **75** | Ventilação de coluna principal |

### 2.5 Configurar Pipe Sizes

1. **Manage** → **MEP Settings** → **Mechanical Settings** → **Piping** → **Sizes**.
2. Verifique se os diâmetros acima estão habilitados para cada Pipe Type.
3. Se faltarem, adicione manualmente.

> ✅ **Checkpoint**: Pipe Types e diâmetros configurados.

---

## 3. Famílias de Equipamentos

### 3.1 Requisitos de família

Toda família usada pelo plugin **deve ter**:

| Requisito | Obrigatório | Razão |
|-----------|-------------|-------|
| **Categoria = Plumbing Fixtures** | ✅ | Plugin filtra por categoria |
| **Conectores MEP** | ✅ | Conexão automática com pipes |
| **Parâmetro `Type Name` descritivo** | ✅ | Identificação pelo plugin |
| **Geometry visível em planta** | ✅ | Posicionamento no layout |
| **Face-based ou Wall-based** | ⚠️ Preferível | Posicionamento em paredes |

### 3.2 Famílias por equipamento

#### Vaso Sanitário

| Propriedade | Valor |
|-------------|-------|
| **Category** | Plumbing Fixtures |
| **Family Name** | `Toilet_FloorMounted` |
| **Type Names** | `Convencional`, `Caixa Acoplada` |
| **Hosting** | Floor-based |
| **Conectores** | 1× Sanitary (DN100), 1× Cold Water (DN20) |
| **Dimensões aprox.** | 650mm × 370mm × 400mm |

#### Lavatório

| Propriedade | Valor |
|-------------|-------|
| **Category** | Plumbing Fixtures |
| **Family Name** | `Lavatory_WallMounted` |
| **Type Names** | `Padrão`, `Cuba de Embutir` |
| **Hosting** | Wall-based |
| **Conectores** | 1× Cold Water (DN20), 1× Hot Water (DN20), 1× Sanitary (DN40) |
| **Dimensões aprox.** | 550mm × 450mm |
| **Altura** | 850mm (face do piso) |

#### Chuveiro

| Propriedade | Valor |
|-------------|-------|
| **Category** | Plumbing Fixtures |
| **Family Name** | `Shower_WallMounted` |
| **Type Names** | `Elétrico`, `A Gás` |
| **Hosting** | Wall-based |
| **Conectores** | 1× Cold Water (DN25), 1× Hot Water (DN25, opcional), 1× Sanitary (DN40, ralo) |
| **Altura** | 2100mm (ponto de saída) |

#### Pia de Cozinha

| Propriedade | Valor |
|-------------|-------|
| **Category** | Plumbing Fixtures |
| **Family Name** | `KitchenSink_CounterTop` |
| **Type Names** | `Cuba Simples`, `Cuba Dupla` |
| **Hosting** | Face-based ou Free-standing |
| **Conectores** | 1× Cold Water (DN25), 1× Hot Water (DN25), 1× Sanitary (DN50) |
| **Altura** | 850mm (bancada) |

#### Tanque

| Propriedade | Valor |
|-------------|-------|
| **Category** | Plumbing Fixtures |
| **Family Name** | `LaundryTub_FloorMounted` |
| **Type Names** | `Simples`, `Duplo` |
| **Hosting** | Wall-based ou Free-standing |
| **Conectores** | 1× Cold Water (DN25), 1× Sanitary (DN50) |
| **Dimensões aprox.** | 600mm × 500mm |

#### Ralo

| Propriedade | Valor |
|-------------|-------|
| **Category** | Plumbing Fixtures |
| **Family Name** | `FloorDrain_Sifonado` |
| **Type Names** | `DN75`, `DN100` |
| **Hosting** | Floor-based |
| **Conectores** | 1× Sanitary (DN75 ou DN100) |

### 3.3 Família genérica (quando não houver família específica)

Para a **máquina de lavar** e equipamentos sem família dedicada:

| Propriedade | Valor |
|-------------|-------|
| **Category** | Plumbing Fixtures (NÃO Generic Models) |
| **Family Name** | `WashingMachine_Point` |
| **Hosting** | Wall-based |
| **Conectores** | 1× Cold Water (DN25), 1× Sanitary (DN50) |
| **Representação** | Caixa simplificada 600×600×850mm |

> ⚠️ **IMPORTANTE**: Mesmo famílias simplificadas **devem** ter categoria `Plumbing Fixtures` e conectores MEP funcionais.

> ✅ **Checkpoint**: 7 famílias de equipamentos definidas.

---

## 4. Conectores MEP

### 4.1 O que são conectores

Conectores são pontos em famílias MEP que permitem conexão automática com tubulação. Sem conectores, o plugin **não consegue** criar rede automaticamente.

### 4.2 Verificar conectores em família existente

1. Selecione o equipamento no modelo.
2. **Right-click** → **Edit Family**.
3. No **Family Editor**, vá em **Create** → **Connector** → **Pipe Connector**.
4. Verifique se existem conectores.

### 4.3 Criar conector (se não existir)

No **Family Editor**:

1. **Create** → **Connector** → **Pipe Connector**.
2. Clique na face onde a tubulação deve conectar.
3. Configure no **Properties**:

#### Conector de Água Fria

| Parâmetro | Valor |
|-----------|-------|
| **System Type** | Domestic Cold Water |
| **Flow Direction** | In |
| **Radius** | (metade do DN, ex: 10mm para DN20) |
| **Angle** | 0° (horizontal) ou 90° (vertical) |
| **Allow Slope Adjustments** | No |

#### Conector de Esgoto

| Parâmetro | Valor |
|-----------|-------|
| **System Type** | Sanitary |
| **Flow Direction** | Out |
| **Radius** | (metade do DN, ex: 50mm para DN100) |
| **Angle** | 0° (horizontal) |
| **Allow Slope Adjustments** | Yes |

#### Conector de Água Quente

| Parâmetro | Valor |
|-----------|-------|
| **System Type** | Domestic Hot Water |
| **Flow Direction** | In |
| **Radius** | (metade do DN, ex: 10mm para DN20) |
| **Angle** | 0° (horizontal) |

### 4.4 Tabela de conectores por equipamento

| Equipamento | AF In | AQ In | ES Out | VE Out | Total |
|------------|-------|-------|--------|--------|-------|
| Vaso sanitário | 1×DN20 | — | 1×DN100 | — | 2 |
| Lavatório | 1×DN20 | 1×DN20 | 1×DN40 | — | 3 |
| Chuveiro | 1×DN25 | 1×DN25 | 1×DN40 | — | 3 |
| Pia de cozinha | 1×DN25 | 1×DN25 | 1×DN50 | — | 3 |
| Tanque | 1×DN25 | — | 1×DN50 | — | 2 |
| Máq. de lavar | 1×DN25 | — | 1×DN50 | — | 2 |
| Ralo sifonado | — | — | 1×DN75 | — | 1 |

### 4.5 Posição dos conectores

| Equipamento | Conector AF | Conector ES |
|------------|-------------|-------------|
| Vaso sanitário | Parede traseira, h=15cm | Piso, centro traseiro |
| Lavatório | Parede, h=50cm | Abaixo da cuba, h=45cm |
| Chuveiro | Parede, h=210cm | Piso, centro do box |
| Pia de cozinha | Parede, h=55cm | Abaixo da cuba, h=50cm |
| Tanque | Parede, h=85cm | Parede, h=10cm |
| Ralo | — | Piso, centro |

### 4.6 Salvar família editada

1. Após adicionar conectores, **File** → **Save**.
2. **Load into Project** (botão no ribbon).
3. Confirme para substituir a família no modelo.

> ✅ **Checkpoint**: Todos os equipamentos têm conectores MEP funcionais.

---

## 5. Parâmetros Obrigatórios

### 5.1 Parâmetros de instância (que o plugin lê)

O plugin identifica equipamentos usando estes parâmetros:

| Parâmetro | Tipo | Uso pelo plugin |
|-----------|------|-----------------|
| **Family Name** | Built-in (string) | Identificação do tipo de equipamento |
| **Type Name** | Built-in (string) | Variante do equipamento |
| **Room** | Built-in (ElementId) | Ambiente onde está posicionado |
| **Level** | Built-in (ElementId) | Nível de posicionamento |
| **Mark** | Built-in (string) | Identificador único da instância |

### 5.2 Parâmetros compartilhados (criar no projeto)

Para enriquecer a automação, crie estes parâmetros compartilhados:

1. **Manage** → **Shared Parameters** → **Edit**.
2. Crie um arquivo de parâmetros: `HidraulicaRevit_SharedParams.txt`.
3. Adicione os parâmetros:

| Grupo | Parâmetro | Tipo | Categorias | Uso |
|-------|-----------|------|-----------|-----|
| **Hidráulica** | `HID_Sistema` | Text | Plumbing Fixtures | AF, ES, AQ, VE |
| **Hidráulica** | `HID_PesoRelativo` | Number | Plumbing Fixtures | UHC (NBR 5626) |
| **Hidráulica** | `HID_DiametroAF` | Number | Plumbing Fixtures | DN da conexão AF |
| **Hidráulica** | `HID_DiametroES` | Number | Plumbing Fixtures | DN da conexão ES |
| **Hidráulica** | `HID_Processado` | Yes/No | Plumbing Fixtures | Flag de controle |

### 5.3 Vincular parâmetros ao projeto

1. **Manage** → **Project Parameters**.
2. **Add** → selecione parâmetro compartilhado.
3. **Categories**: marque **Plumbing Fixtures**.
4. **Group parameter under**: Mechanical.
5. **Instance** (não Type).
6. Repita para cada parâmetro.

### 5.4 Valores padrão por equipamento

| Equipamento | HID_Sistema | HID_PesoRelativo | HID_DiametroAF | HID_DiametroES |
|-------------|-------------|-------------------|-----------------|-----------------|
| Vaso sanitário | AF,ES | 0.30 | 20 | 100 |
| Lavatório | AF,ES | 0.30 | 20 | 40 |
| Chuveiro | AF,ES | 0.40 | 25 | 40 |
| Pia de cozinha | AF,AQ,ES | 0.70 | 25 | 50 |
| Tanque | AF,ES | 0.70 | 25 | 50 |
| Máq. de lavar | AF,ES | 0.30 | 25 | 50 |
| Ralo sifonado | ES | 0.00 | 0 | 75 |

> ✅ **Checkpoint**: Parâmetros compartilhados criados e vinculados.

---

## 6. Carregar Famílias no Modelo

### 6.1 Fontes de famílias

| Fonte | Prioridade | Caminho |
|-------|-----------|---------|
| **Biblioteca Revit (local)** | 1ª | `C:\ProgramData\Autodesk\RVT 2026\Libraries\` |
| **Revit Family Templates** | 2ª | `C:\ProgramData\Autodesk\RVT 2026\Family Templates\` |
| **BIM Object** | 3ª | [bimobject.com](https://www.bimobject.com) |
| **Fabricante** | 4ª | Sites de fabricantes (Deca, Docol, etc.) |
| **Custom (criada)** | 5ª | `C:\Users\User\Desktop\PluginRevit\Data\Families\` |

### 6.2 Passos para carregar

1. **Insert** → **Load Family** (atalho `LF`).
2. Navegue até a pasta da família.
3. Selecione o arquivo `.rfa` → **Open**.
4. Se pedir para substituir, selecione **Overwrite**.

### 6.3 Buscar na biblioteca US Metric

Se não houver biblioteca BR, use US Metric:

```
C:\ProgramData\Autodesk\RVT 2026\Libraries\US Metric\Plumbing\MEP\Fixtures\
```

| Arquivo da biblioteca | Equipamento correspondente |
|----------------------|---------------------------|
| `Toilet-Commercial-Wall-3D.rfa` | Vaso sanitário |
| `Lavatory-Wall-Oval-3D.rfa` | Lavatório |
| `Shower Stall-3D.rfa` | Chuveiro |
| `Sink-Kitchen-Double-3D.rfa` | Pia de cozinha |
| `Sink-Utility.rfa` | Tanque |
| `Floor Drain-Round.rfa` | Ralo |

### 6.4 Organizar famílias carregadas

No **Project Browser**:

1. Expanda **Families** → **Plumbing Fixtures**.
2. Verifique se todas as 7 famílias estão listadas.
3. Para cada uma, clique com botão direito → **Properties** → confirme o número de conectores.

### 6.5 Criar pasta de famílias custom

```
PluginRevit/Data/Families/
├── Toilet_FloorMounted.rfa
├── Lavatory_WallMounted.rfa
├── Shower_WallMounted.rfa
├── KitchenSink_CounterTop.rfa
├── LaundryTub_FloorMounted.rfa
├── WashingMachine_Point.rfa
└── FloorDrain_Sifonado.rfa
```

> ✅ **Checkpoint**: 7 famílias carregadas no modelo.

---

## 7. Posicionar Equipamentos

### 7.1 Regras gerais de posicionamento

| Regra | Detalhe |
|-------|---------|
| **Sempre dentro do Room** | Equipamento deve pertencer ao Room correto |
| **Encostado na parede hidráulica** | Face traseira tocando a parede |
| **Distância mínima entre peças** | 15 cm |
| **Conector voltado para a parede** | Facilita conexão com tubulação embutida |
| **Nível correto** | Floor-based no Térreo, wall-based na parede do Térreo |

### 7.2 Posicionar no Banheiro Social

1. **Architecture** → **Component** → **Place a Component**.
2. Selecione `Toilet_FloorMounted : Convencional`.
3. Posicione encostado na **parede de fundo** do banheiro.
4. **Rotacione** para que a frente fique voltada para a porta (atalho `Space` para girar).
5. Selecione `Lavatory_WallMounted : Padrão`.
6. Posicione na **parede lateral** do banheiro.
7. Selecione `Shower_WallMounted : Elétrico`.
8. Posicione no **canto oposto** ao vaso.
9. Selecione `FloorDrain_Sifonado : DN75`.
10. Posicione no **piso central** do box.

### 7.3 Posicionar na Cozinha

1. Selecione `KitchenSink_CounterTop : Cuba Simples`.
2. Posicione na **parede com janela** (centralizado na bancada).

### 7.4 Posicionar na Área de Serviço

1. Selecione `LaundryTub_FloorMounted : Simples`.
2. Posicione na **parede hidráulica** (compartilhada com banheiro).
3. Selecione `WashingMachine_Point`.
4. Posicione ao **lado do tanque** (distância: ~10 cm).
5. Selecione `FloorDrain_Sifonado : DN75`.
6. Posicione no **piso** entre tanque e máquina.

### 7.5 Preencher parâmetros

Após posicionar, selecione cada equipamento e preencha:

1. **Mark**: identifique sequencialmente (`VS-01`, `LV-01`, `CH-01`, `PC-01`, `TQ-01`, `ML-01`).
2. Parâmetros HID (se já criados):
   - `HID_Sistema`, `HID_PesoRelativo`, `HID_DiametroAF`, `HID_DiametroES`.

> ✅ **Checkpoint**: Equipamentos posicionados e identificados.

---

## 8. Testar Conexões

### 8.1 Teste manual de pipe

1. **Systems** → **Piping** → **Pipe**.
2. System Type: **Domestic Cold Water**.
3. Diâmetro: **20 mm**.
4. Clique no **conector** do lavatório (deve fazer snap).
5. Trace um pipe até a parede.
6. Se o pipe conectou ao conector automaticamente → ✅.
7. Se não fez snap → a família **não tem conector** → ❌ revisar família.

### 8.2 Verificar system assignments

1. Após conectar o pipe, selecione-o.
2. No **Properties**, verifique:
   - **System Type** = Domestic Cold Water ✅.
   - **System Name** = algo como "Domestic Cold Water 1" ✅.
3. Selecione o equipamento.
4. Verifique se o **System** foi atribuído automaticamente.

### 8.3 Teste rápido por sistema

| Sistema | Equipamento | Conector | Pipe DN | Resultado esperado |
|---------|-------------|----------|---------|-------------------|
| AF | Lavatório | Cold Water In | 20mm | Snap + System assigned |
| AF | Pia cozinha | Cold Water In | 25mm | Snap + System assigned |
| ES | Vaso | Sanitary Out | 100mm | Snap + System assigned |
| ES | Ralo | Sanitary Out | 75mm | Snap + System assigned |

### 8.4 Limpar pipes de teste

Após validar:
1. Selecione todos os pipes de teste.
2. Delete (esses eram apenas para validação).
3. O plugin criará a rede real automaticamente.

> ✅ **Checkpoint**: Conexões testadas e funcionais.

---

## 9. Configurações para o Plugin

### 9.1 Como o plugin identifica equipamentos

O plugin usa a seguinte lógica para encontrar e classificar equipamentos:

```csharp
// Filtro por categoria
var collector = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_PlumbingFixtures)
    .WhereElementIsNotElementType()
    .ToList();

// Para cada elemento, ler:
foreach (var elem in collector)
{
    var familyName = elem.get_Parameter(BuiltInParameter.ELEM_FAMILY_PARAM)
                        .AsValueString();
    var typeName = elem.get_Parameter(BuiltInParameter.ELEM_TYPE_PARAM)
                      .AsValueString();
    var room = elem.Room;  // Room onde está posicionado
    var level = elem.Level; // Nível
    var mark = elem.get_Parameter(BuiltInParameter.ALL_MODEL_MARK)
                   .AsString();
}
```

### 9.2 Mapeamento Family Name → EquipmentType

O plugin usa este mapeamento para identificar o tipo:

| Family Name (contém) | EquipmentType |
|----------------------|---------------|
| `Toilet` | `Toilet` |
| `Lavatory`, `Lav` | `Sink` |
| `Shower` | `Shower` |
| `Bathtub`, `Bath` | `Bathtub` |
| `KitchenSink`, `Kitchen Sink` | `KitchenSink` |
| `LaundryTub`, `Utility Sink` | `LaundryTub` |
| `WashingMachine`, `Washing` | `WashingMachine` |
| `FloorDrain`, `Floor Drain` | `FloorDrain` |
| `Bidet` | `Bidet` |
| `Urinal` | `Urinal` |
| `Dishwasher` | `Dishwasher` |

> ⚠️ **A busca é case-insensitive e por `Contains`**. Use nomes de família que contenham essas palavras-chave.

### 9.3 Mapeamento de conectores

O plugin lê conectores via:

```csharp
var connectorSet = (elem as FamilyInstance)
    .MEPModel.ConnectorManager.Connectors;

foreach (Connector c in connectorSet)
{
    var systemType = c.PipeSystemType; // DomesticColdWater, Sanitary, etc.
    var direction = c.Direction;       // In, Out
    var diameter = c.Radius * 2;       // Diâmetro
    var origin = c.Origin;             // Posição 3D
}
```

### 9.4 JSON de mapeamento de famílias

O plugin carrega um arquivo de configuração para mapear famílias:

```
Data/Config/family_mapping.json
```

```json
{
  "familyMappings": [
    {
      "familyNameContains": "Toilet",
      "equipmentType": "Toilet",
      "expectedConnectors": {
        "coldWater": { "count": 1, "nominalDiameter": 20 },
        "sanitary": { "count": 1, "nominalDiameter": 100 }
      },
      "requiredInRoomTypes": ["Bathroom", "MasterBathroom", "Lavatory"]
    },
    {
      "familyNameContains": "Lavatory",
      "equipmentType": "Sink",
      "expectedConnectors": {
        "coldWater": { "count": 1, "nominalDiameter": 20 },
        "hotWater": { "count": 1, "nominalDiameter": 20 },
        "sanitary": { "count": 1, "nominalDiameter": 40 }
      },
      "requiredInRoomTypes": ["Bathroom", "MasterBathroom", "Lavatory"]
    },
    {
      "familyNameContains": "Shower",
      "equipmentType": "Shower",
      "expectedConnectors": {
        "coldWater": { "count": 1, "nominalDiameter": 25 },
        "sanitary": { "count": 1, "nominalDiameter": 40 }
      },
      "requiredInRoomTypes": ["Bathroom", "MasterBathroom"]
    },
    {
      "familyNameContains": "KitchenSink",
      "equipmentType": "KitchenSink",
      "expectedConnectors": {
        "coldWater": { "count": 1, "nominalDiameter": 25 },
        "hotWater": { "count": 1, "nominalDiameter": 25 },
        "sanitary": { "count": 1, "nominalDiameter": 50 }
      },
      "requiredInRoomTypes": ["Kitchen"]
    },
    {
      "familyNameContains": "LaundryTub",
      "equipmentType": "LaundryTub",
      "expectedConnectors": {
        "coldWater": { "count": 1, "nominalDiameter": 25 },
        "sanitary": { "count": 1, "nominalDiameter": 50 }
      },
      "requiredInRoomTypes": ["Laundry"]
    },
    {
      "familyNameContains": "WashingMachine",
      "equipmentType": "WashingMachine",
      "expectedConnectors": {
        "coldWater": { "count": 1, "nominalDiameter": 25 },
        "sanitary": { "count": 1, "nominalDiameter": 50 }
      },
      "requiredInRoomTypes": ["Laundry"]
    },
    {
      "familyNameContains": "FloorDrain",
      "equipmentType": "FloorDrain",
      "expectedConnectors": {
        "sanitary": { "count": 1, "nominalDiameter": 75 }
      },
      "requiredInRoomTypes": ["Bathroom", "MasterBathroom", "Laundry"]
    }
  ]
}
```

> ✅ **Checkpoint**: Configuração do plugin mapeada para as famílias.

---

## 10. Validação Final

### 10.1 Checklist de famílias

```
FAMÍLIAS CARREGADAS
[ ] Toilet_FloorMounted                (Plumbing Fixtures, 2 conectores)
[ ] Lavatory_WallMounted               (Plumbing Fixtures, 3 conectores)
[ ] Shower_WallMounted                 (Plumbing Fixtures, 2-3 conectores)
[ ] KitchenSink_CounterTop             (Plumbing Fixtures, 3 conectores)
[ ] LaundryTub_FloorMounted            (Plumbing Fixtures, 2 conectores)
[ ] WashingMachine_Point               (Plumbing Fixtures, 2 conectores)
[ ] FloorDrain_Sifonado                (Plumbing Fixtures, 1 conector)
```

### 10.2 Checklist de sistemas

```
SISTEMAS DE TUBULAÇÃO
[ ] Domestic Cold Water (AF) — azul
[ ] Domestic Hot Water (AQ) — vermelho
[ ] Sanitary (ES) — marrom
[ ] Vent (VE) — verde
[ ] Storm (AP) — cinza
```

### 10.3 Checklist de diâmetros

```
DIÂMETROS HABILITADOS
[ ] AF: DN20, DN25, DN32, DN40, DN50, DN60, DN75
[ ] ES: DN40, DN50, DN75, DN100, DN150
[ ] VE: DN40, DN50, DN75
```

### 10.4 Checklist de conectores

```
TESTE DE CONEXÃO (snap ao conector)
[ ] AF → Lavatório: snap ✅
[ ] AF → Pia cozinha: snap ✅
[ ] ES → Vaso sanitário: snap ✅
[ ] ES → Ralo: snap ✅
```

### 10.5 Checklist de parâmetros

```
PARÂMETROS COMPARTILHADOS
[ ] HID_Sistema criado e vinculado
[ ] HID_PesoRelativo criado e vinculado
[ ] HID_DiametroAF criado e vinculado
[ ] HID_DiametroES criado e vinculado
[ ] HID_Processado criado e vinculado
```

### 10.6 Salvar

```
File → Save
```

---

> **Modelo configurado com todas as famílias MEP, sistemas de tubulação, conectores e parâmetros necessários para automação hidráulica.**
