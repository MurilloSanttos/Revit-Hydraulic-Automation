# Padrão de Nomenclatura de Ambientes (Rooms)

> Definição completa de nomes, classificações e regras para todos os ambientes residenciais suportados pelo plugin de automação hidráulica.

---

## 📋 Índice

- [Formato de Nomenclatura](#1-formato-de-nomenclatura)
- [Tabela Completa de Ambientes](#2-tabela-completa-de-ambientes)
- [Palavras-Chave para Classificação](#3-palavras-chave-para-classificação-automática)
- [Equipamentos por Ambiente](#4-equipamentos-por-ambiente)
- [Regras de Validação](#5-regras-de-validação)
- [Regras de Automação](#6-regras-de-automação)
- [Mapeamento Strategy](#7-mapeamento-strategy)
- [Modelo de Teste](#8-aplicação-no-modelo-de-teste)
- [Extensibilidade](#9-extensibilidade)

---

## 1. Formato de Nomenclatura

### 1.1 Padrão

```
[TipoAmbiente] - [Complemento]
```

| Parte | Obrigatório | Exemplo |
|-------|-------------|---------|
| **TipoAmbiente** | ✅ Sim | `Banheiro`, `Cozinha`, `Quarto` |
| **Complemento** | ❌ Não | `Social`, `Suíte`, `Casal`, `01` |

### 1.2 Regras

| Regra | Detalhe |
|-------|---------|
| **Idioma** | Português (Brasil) |
| **Case** | Primeira letra maiúscula (Title Case) |
| **Separador** | ` - ` (espaço-traço-espaço) |
| **Acentos** | Permitidos e esperados |
| **Números** | Permitidos no complemento |

### 1.3 Exemplos válidos

```
Banheiro - Social
Banheiro - Suíte
Quarto - Casal
Quarto - 01
Cozinha
Sala de Estar
Área de Serviço
```

### 1.4 Exemplos inválidos

```
❌ banheiro social        → sem separador, lowercase
❌ COZINHA                → uppercase
❌ Bath - Social          → idioma incorreto
❌ Amb. 01                → nome genérico
❌ Room 1                 → nome genérico em inglês
❌                        → nome vazio
```

---

## 2. Tabela Completa de Ambientes

### 2.1 Ambientes Molhados (com hidráulica)

| # | Nome Padrão | Enum `RoomType` | Categoria | Hidráulica | Prioridade |
|---|------------|-----------------|-----------|------------|------------|
| 1 | **Banheiro - Social** | `Bathroom` | Molhado | ✅ Sim | Alta |
| 2 | **Banheiro - Suíte** | `MasterBathroom` | Molhado | ✅ Sim | Alta |
| 3 | **Banheiro - Serviço** | `Bathroom` | Molhado | ✅ Sim | Alta |
| 4 | **Lavabo** | `Lavatory` | Molhado | ✅ Sim | Alta |
| 5 | **Cozinha** | `Kitchen` | Molhado | ✅ Sim | Alta |
| 6 | **Cozinha - Gourmet** | `Kitchen` | Molhado | ✅ Sim | Alta |
| 7 | **Área de Serviço** | `Laundry` | Molhado | ✅ Sim | Alta |
| 8 | **Lavanderia** | `Laundry` | Molhado | ✅ Sim | Alta |

### 2.2 Ambientes Secos (sem hidráulica)

| # | Nome Padrão | Enum `RoomType` | Categoria | Hidráulica | Prioridade |
|---|------------|-----------------|-----------|------------|------------|
| 9 | **Sala de Estar** | `DryArea` | Seco | ❌ Não | Baixa |
| 10 | **Sala de Jantar** | `DryArea` | Seco | ❌ Não | Baixa |
| 11 | **Sala de TV** | `DryArea` | Seco | ❌ Não | Baixa |
| 12 | **Quarto - Casal** | `DryArea` | Seco | ❌ Não | Baixa |
| 13 | **Quarto - Solteiro** | `DryArea` | Seco | ❌ Não | Baixa |
| 14 | **Quarto - 01** | `DryArea` | Seco | ❌ Não | Baixa |
| 15 | **Quarto - 02** | `DryArea` | Seco | ❌ Não | Baixa |
| 16 | **Quarto - Hóspedes** | `DryArea` | Seco | ❌ Não | Baixa |
| 17 | **Escritório** | `DryArea` | Seco | ❌ Não | Baixa |
| 18 | **Home Office** | `DryArea` | Seco | ❌ Não | Baixa |
| 19 | **Closet** | `DryArea` | Seco | ❌ Não | Baixa |

### 2.3 Ambientes Técnicos / Circulação

| # | Nome Padrão | Enum `RoomType` | Categoria | Hidráulica | Prioridade |
|---|------------|-----------------|-----------|------------|------------|
| 20 | **Garagem** | `Garage` | Técnico | ❌ Não | Nenhuma |
| 21 | **Corredor** | `DryArea` | Circulação | ❌ Não | Nenhuma |
| 22 | **Hall** | `DryArea` | Circulação | ❌ Não | Nenhuma |
| 23 | **Hall de Entrada** | `DryArea` | Circulação | ❌ Não | Nenhuma |
| 24 | **Circulação** | `DryArea` | Circulação | ❌ Não | Nenhuma |
| 25 | **Escada** | `DryArea` | Circulação | ❌ Não | Nenhuma |
| 26 | **Depósito** | `DryArea` | Técnico | ❌ Não | Nenhuma |
| 27 | **Despensa** | `DryArea` | Técnico | ❌ Não | Nenhuma |

### 2.4 Ambientes Especiais (semi-molhados)

| # | Nome Padrão | Enum `RoomType` | Categoria | Hidráulica | Prioridade |
|---|------------|-----------------|-----------|------------|------------|
| 28 | **Varanda** | `Balcony` | Semi | ⚠️ Opcional | Baixa |
| 29 | **Varanda - Gourmet** | `Balcony` | Semi | ⚠️ Opcional | Média |
| 30 | **Sacada** | `Balcony` | Semi | ⚠️ Opcional | Baixa |
| 31 | **Terraço** | `Balcony` | Semi | ⚠️ Opcional | Baixa |
| 32 | **Área Externa** | `ServiceArea` | Semi | ⚠️ Opcional | Baixa |
| 33 | **Piscina** | `ServiceArea` | Semi | ⚠️ Opcional | Média |

---

## 3. Palavras-Chave para Classificação Automática

O classificador (`ClassificadorAmbientes`) usa essas palavras-chave para determinar o `RoomType`:

### 3.1 Tabela de palavras-chave

| RoomType | Palavras-Chave (case-insensitive) | Prioridade |
|----------|-----------------------------------|------------|
| `Bathroom` | `banheiro`, `banho`, `wc`, `bwc`, `sanitário` | 1 |
| `MasterBathroom` | `suíte`, `suite`, `master` (combinado com `banheiro`) | 1 |
| `Lavatory` | `lavabo`, `toilette`, `toalete` | 2 |
| `Kitchen` | `cozinha`, `kitchen`, `copa`, `gourmet` (sem `varanda`) | 3 |
| `Laundry` | `serviço`, `servico`, `lavanderia`, `área de serviço` | 4 |
| `Balcony` | `varanda`, `sacada`, `terraço`, `terraco`, `balcão` | 5 |
| `Garage` | `garagem`, `garage`, `estacionamento`, `vaga` | 6 |
| `ServiceArea` | `área externa`, `piscina`, `churrasqueira` | 7 |
| `DryArea` | `sala`, `quarto`, `escritório`, `closet`, `corredor`, `hall`, `circulação`, `escada`, `depósito`, `despensa`, `home office` | 8 |

### 3.2 Algoritmo de classificação

```
1. Normalizar nome do Room (lowercase, trim, remover acentos para comparação)
2. Buscar match nas palavras-chave (por prioridade)
3. Se match encontrado → atribuir RoomType
4. Se "suíte" presente E "banheiro" presente → MasterBathroom
5. Se nenhum match → RoomType.Unknown
6. Registrar confiança:
   - Match exato no início do nome → Confiança Alta (90%+)
   - Match parcial (contém palavra) → Confiança Média (60-89%)
   - Match por similaridade → Confiança Baixa (30-59%)
   - Sem match → Confiança Nenhuma (0%)
```

### 3.3 Normalização de texto

| Original | Normalizado |
|----------|------------|
| `Banheiro - Social` | `banheiro social` |
| `Área de Serviço` | `area de servico` |
| `Banheiro - Suíte` | `banheiro suite` |
| `Quarto - 01` | `quarto 01` |

---

## 4. Equipamentos por Ambiente

### 4.1 Banheiro Social / Serviço

| Equipamento | Enum `EquipmentType` | Qtd | Peso NBR (UHC) | Sistemas |
|-------------|---------------------|-----|-----------------|----------|
| Vaso sanitário | `Toilet` | 1 | 0.30 | ES, AF |
| Lavatório | `Sink` | 1 | 0.30 | AF, ES |
| Chuveiro | `Shower` | 1 | 0.40 | AF (AQ), ES |
| Ralo seco | `FloorDrain` | 1 | — | ES |

### 4.2 Banheiro Suíte (Master)

| Equipamento | Enum `EquipmentType` | Qtd | Peso NBR (UHC) | Sistemas |
|-------------|---------------------|-----|-----------------|----------|
| Vaso sanitário | `Toilet` | 1 | 0.30 | ES, AF |
| Lavatório | `Sink` | 1 | 0.30 | AF, ES |
| Chuveiro | `Shower` | 1 | 0.40 | AF (AQ), ES |
| Banheira (opcional) | `Bathtub` | 0-1 | 0.30 | AF (AQ), ES |
| Bidê (opcional) | `Bidet` | 0-1 | 0.10 | AF, ES |
| Ralo seco | `FloorDrain` | 1 | — | ES |

### 4.3 Lavabo

| Equipamento | Enum `EquipmentType` | Qtd | Peso NBR (UHC) | Sistemas |
|-------------|---------------------|-----|-----------------|----------|
| Vaso sanitário | `Toilet` | 1 | 0.30 | ES, AF |
| Lavatório | `Sink` | 1 | 0.30 | AF, ES |

### 4.4 Cozinha

| Equipamento | Enum `EquipmentType` | Qtd | Peso NBR (UHC) | Sistemas |
|-------------|---------------------|-----|-----------------|----------|
| Pia de cozinha | `KitchenSink` | 1 | 0.70 | AF (AQ), ES |
| Máquina de lavar louça (opc.) | `Dishwasher` | 0-1 | 0.30 | AF, ES |

### 4.5 Área de Serviço / Lavanderia

| Equipamento | Enum `EquipmentType` | Qtd | Peso NBR (UHC) | Sistemas |
|-------------|---------------------|-----|-----------------|----------|
| Tanque | `LaundryTub` | 1 | 0.70 | AF, ES |
| Máquina de lavar roupa | `WashingMachine` | 1 | 0.30 | AF, ES |
| Ralo seco | `FloorDrain` | 1 | — | ES |

### 4.6 Varanda Gourmet

| Equipamento | Enum `EquipmentType` | Qtd | Peso NBR (UHC) | Sistemas |
|-------------|---------------------|-----|-----------------|----------|
| Pia | `KitchenSink` | 0-1 | 0.70 | AF, ES |
| Ralo seco | `FloorDrain` | 0-1 | — | ES |

### 4.7 Ambientes Secos

| Equipamento | Qtd |
|-------------|-----|
| Nenhum | 0 |

> **Legenda**: AF = Água Fria, AQ = Água Quente, ES = Esgoto, UHC = Unidade Hunter de Contribuição.

---

## 5. Regras de Validação

### 5.1 Erros críticos (bloqueantes)

| Regra | Condição | Ação |
|-------|----------|------|
| **V01** | Nome do Room vazio ou null | ❌ Bloquear pipeline |
| **V02** | Room sem área (= 0) | ❌ Bloquear pipeline |
| **V03** | Room "Not Enclosed" | ❌ Bloquear pipeline |
| **V04** | Room duplicado (mesmo nome + número) | ❌ Bloquear pipeline |

### 5.2 Avisos (não bloqueantes)

| Regra | Condição | Ação |
|-------|----------|------|
| **V05** | Nome não reconhecido → `Unknown` | ⚠️ Avisar, solicitar classificação manual |
| **V06** | Confiança < 60% | ⚠️ Avisar, solicitar confirmação |
| **V07** | Ambiente molhado sem equipamentos | ⚠️ Avisar (pode ser intencional) |
| **V08** | Área fora do esperado para o tipo | ⚠️ Avisar |

### 5.3 Áreas esperadas por tipo

| RoomType | Área mínima (m²) | Área máxima (m²) | Área típica (m²) |
|----------|-------------------|-------------------|-------------------|
| `Bathroom` | 2.0 | 12.0 | 4.0 - 6.0 |
| `MasterBathroom` | 3.0 | 20.0 | 5.0 - 10.0 |
| `Lavatory` | 1.2 | 5.0 | 1.5 - 3.0 |
| `Kitchen` | 4.0 | 25.0 | 8.0 - 15.0 |
| `Laundry` | 2.0 | 12.0 | 3.0 - 6.0 |
| `DryArea` | 4.0 | 50.0 | 10.0 - 20.0 |
| `Balcony` | 2.0 | 30.0 | 4.0 - 10.0 |
| `Garage` | 12.0 | 60.0 | 15.0 - 30.0 |

---

## 6. Regras de Automação

### 6.1 Ambientes que disparam automação

| RoomType | Gera equipamentos | Cria rede AF | Cria rede ES | Cria ventilação |
|----------|-------------------|-------------|-------------|-----------------|
| `Bathroom` | ✅ | ✅ | ✅ | ✅ |
| `MasterBathroom` | ✅ | ✅ | ✅ | ✅ |
| `Lavatory` | ✅ | ✅ | ✅ | ⚠️ Depende |
| `Kitchen` | ✅ | ✅ | ✅ | ✅ |
| `Laundry` | ✅ | ✅ | ✅ | ⚠️ Depende |
| `Balcony` | ⚠️ Se gourmet | ⚠️ Se pia | ⚠️ Se ralo | ❌ |

### 6.2 Ambientes ignorados pela automação hidráulica

| RoomType | Razão |
|----------|-------|
| `DryArea` | Sem pontos hidráulicos |
| `Garage` | Sem pontos (exceto ralo em casos especiais) |
| `Unknown` | Não classificado — requer intervenção manual |

### 6.3 Propriedade `EhRelevante`

```csharp
public bool EhRelevante => Classificacao.Tipo switch
{
    RoomType.Bathroom       => true,
    RoomType.MasterBathroom => true,
    RoomType.Lavatory       => true,
    RoomType.Kitchen        => true,
    RoomType.Laundry        => true,
    RoomType.Balcony        => true,  // verificar se tem pia
    _                       => false
};
```

---

## 7. Mapeamento Strategy

Cada `RoomType` mapeia para uma Strategy do padrão implementado:

| RoomType | Strategy | Responsabilidades |
|----------|----------|-------------------|
| `Bathroom` | `BathroomStrategy` | Vaso + Lavatório + Chuveiro + Ralo |
| `MasterBathroom` | `MasterBathroomStrategy` | Banheiro completo + opcionais (banheira, bidê) |
| `Lavatory` | `LavatoryStrategy` | Vaso + Lavatório (sem chuveiro) |
| `Kitchen` | `KitchenStrategy` | Pia + lava-louça opcional |
| `Laundry` | `LaundryStrategy` | Tanque + Máquina de lavar + Ralo |
| `Balcony` | `BalconyStrategy` | Pia opcional + Ralo opcional |
| `Garage` | `DryAreaStrategy` | Nenhum equipamento |
| `DryArea` | `DryAreaStrategy` | Nenhum equipamento |
| `ServiceArea` | `ServiceAreaStrategy` | Ralo + torneira externa (opcional) |
| `Unknown` | — | ❌ Erro: exige classificação manual |

### 7.1 Registro no Factory

```csharp
public class RoomStrategyFactory
{
    private readonly Dictionary<RoomType, IRoomStrategy> _strategies = new()
    {
        [RoomType.Bathroom]       = new BathroomStrategy(),
        [RoomType.MasterBathroom] = new MasterBathroomStrategy(),
        [RoomType.Lavatory]       = new LavatoryStrategy(),
        [RoomType.Kitchen]        = new KitchenStrategy(),
        [RoomType.Laundry]        = new LaundryStrategy(),
        [RoomType.Balcony]        = new BalconyStrategy(),
        [RoomType.Garage]         = new DryAreaStrategy(),
        [RoomType.ServiceArea]    = new ServiceAreaStrategy(),
        [RoomType.DryArea]        = new DryAreaStrategy(),
    };

    public IRoomStrategy GetStrategy(RoomType type)
    {
        if (_strategies.TryGetValue(type, out var strategy))
            return strategy;

        throw new InvalidOperationException(
            $"Nenhuma Strategy registrada para RoomType: {type}. " +
            "Classifique o ambiente manualmente antes de prosseguir.");
    }
}
```

---

## 8. Aplicação no Modelo de Teste

### 8.1 Rooms do modelo de teste

| # | Room Name | Room Number | RoomType | Strategy | Equipamentos |
|---|-----------|-------------|----------|----------|-------------|
| 1 | Banheiro - Social | 01 | `Bathroom` | `BathroomStrategy` | Vaso, Lav, Chuv, Ralo |
| 2 | Área de Serviço | 02 | `Laundry` | `LaundryStrategy` | Tanque, MLav, Ralo |
| 3 | Sala de Estar | 03 | `DryArea` | `DryAreaStrategy` | — |
| 4 | Cozinha | 04 | `Kitchen` | `KitchenStrategy` | Pia |
| 5 | Quarto - 01 | 05 | `DryArea` | `DryAreaStrategy` | — |
| 6 | Quarto - 02 | 06 | `DryArea` | `DryAreaStrategy` | — |

### 8.2 Resultado esperado da classificação

```
[INFO] Room "Banheiro - Social" → Bathroom (Confiança: 95%)  ✅
[INFO] Room "Área de Serviço"   → Laundry  (Confiança: 92%)  ✅
[INFO] Room "Sala de Estar"     → DryArea  (Confiança: 90%)  ✅
[INFO] Room "Cozinha"           → Kitchen  (Confiança: 98%)  ✅
[INFO] Room "Quarto - 01"       → DryArea  (Confiança: 88%)  ✅
[INFO] Room "Quarto - 02"       → DryArea  (Confiança: 88%)  ✅
─────────────────────────────────────────────────────
Classificados: 6/6 | Relevantes: 3 | Secos: 3 | Erros: 0
```

---

## 9. Extensibilidade

### 9.1 Adicionar novo tipo de ambiente

Para suportar um novo ambiente (ex: **Piscina**):

**Passo 1** — Adicionar ao enum `RoomType`:
```csharp
public enum RoomType
{
    // ... existentes ...
    Pool = 12
}
```

**Passo 2** — Adicionar palavras-chave ao classificador:
```csharp
// No ClassificadorAmbientes
{ RoomType.Pool, new[] { "piscina", "pool" } }
```

**Passo 3** — Criar Strategy:
```csharp
public class PoolStrategy : RoomStrategyBase
{
    public override RoomType TipoAmbiente => RoomType.Pool;

    public override List<EquipmentType> GetEquipmentList()
    {
        return new() { EquipmentType.FloorDrain };
    }
}
```

**Passo 4** — Registrar no Factory:
```csharp
_strategies[RoomType.Pool] = new PoolStrategy();
```

**Passo 5** — Adicionar à tabela de nomenclatura (este documento):

| Nome Padrão | Enum | Categoria | Hidráulica |
|------------|------|-----------|------------|
| Piscina | `Pool` | Semi | ⚠️ Opcional |

### 9.2 Checklist de extensão

```
[ ] Enum atualizado
[ ] Palavras-chave adicionadas
[ ] Strategy criada
[ ] Factory registrada
[ ] Documento de nomenclatura atualizado
[ ] Modelo de teste atualizado (se necessário)
```

---

> **Este padrão garante que 100% dos ambientes residenciais sejam identificados, classificados e processados automaticamente pelo plugin.**
