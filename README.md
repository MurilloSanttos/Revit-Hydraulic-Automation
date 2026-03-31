<p align="center">
  <img src="https://img.shields.io/badge/Revit-2026-blue?style=for-the-badge&logo=autodesk" alt="Revit 2026"/>
  <img src="https://img.shields.io/badge/C%23-.NET%208-purple?style=for-the-badge&logo=dotnet" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD-green?style=for-the-badge" alt="Clean Architecture"/>
  <img src="https://img.shields.io/badge/OpenXML-3.2.0-teal?style=for-the-badge" alt="OpenXML"/>
  <img src="https://img.shields.io/badge/Status-Em%20Desenvolvimento-orange?style=for-the-badge" alt="Status"/>
</p>

# 🔧 Revit Hydraulic Automation

**Plugin de automação hidráulica para o Autodesk Revit** — automatiza de **70% a 80%** do fluxo de projeto hidráulico residencial, com execução semi-automática e validação humana em cada etapa.

> Transforma um processo manual de horas em um fluxo orquestrado de minutos, mantendo o engenheiro no controle de cada decisão.

---

## 📋 Índice

- [Visão Geral](#-visão-geral)
- [Funcionalidades](#-funcionalidades)
- [Arquitetura](#-arquitetura)
- [Estrutura do Projeto](#-estrutura-do-projeto)
- [Componentes Implementados](#-componentes-implementados)
- [Como Funciona](#-como-funciona)
- [Tecnologias](#-tecnologias)
- [Normas Técnicas](#-normas-técnicas)
- [Roadmap](#-roadmap)
- [Pré-requisitos](#-pré-requisitos)
- [Instalação](#-instalação)
- [Contribuição](#-contribuição)
- [Licença](#-licença)

---

## 🎯 Visão Geral

O **Revit Hydraulic Automation** é um plugin integrado ao Autodesk Revit que automatiza o projeto hidráulico residencial. O sistema opera como um **orquestrador inteligente**: ele analisa o modelo BIM, toma decisões baseadas em regras normativas brasileiras, e coordena a criação de elementos MEP via **External Events thread-safe**.

### O Problema

O projeto hidráulico no Revit é um processo predominantemente manual:

- Classificação de ambientes feita visualmente
- Equipamentos inseridos um a um
- Redes traçadas manualmente
- Dimensionamento calculado em planilhas externas
- Tabelas e pranchas montadas individualmente

### A Solução

Um pipeline automatizado por etapas que:

- **Detecta e classifica** ambientes automaticamente
- **Insere equipamentos** com base em regras normativas
- **Gera redes hidráulicas** (água fria, esgoto, ventilação)
- **Dimensiona** toda a instalação seguindo NBR 5626 e NBR 8160
- **Produz tabelas, views e pranchas** prontas para entrega
- **Exporta quantitativos** para CSV e Excel (.xlsx)

Cada etapa é validada pelo engenheiro antes de avançar, garantindo **controle total** sobre o resultado.

---

## ⚡ Funcionalidades

| # | Etapa | Descrição | Status |
|---|-------|-----------|--------|
| E01 | **Detecção de Ambientes** | Leitura de Rooms/Spaces do modelo Revit | ✅ Implementado |
| E02 | **Classificação Inteligente** | Classificação automática por tipo (banheiro, cozinha, etc.) | ✅ Implementado |
| E03 | **Gestão de Spaces MEP** | Criação, matching e validação de Spaces | ✅ Implementado |
| E04 | **Inserção Automática** | Posicionamento de equipamentos MEP via External Events | ✅ Implementado |
| E05 | **Validação do Modelo** | Verificação de conectividade e consistência (BFS) | ✅ Implementado |
| E06 | **Criação de Sistemas** | PipingSystem para AF, AQ, ES, VT (9 tipos) | ✅ Implementado |
| E07 | **Atribuição de Elementos** | Vinculação de elementos a sistemas MEP | ✅ Implementado |
| E08 | **Nomenclatura Padronizada** | MEP-[SIGLA]-[NÍVEL]-[XX] automático | ✅ Implementado |
| E09 | **Tabelas de Quantitativos** | Schedules de tubulação, conexões e equipamentos | ✅ Implementado |
| E10 | **Views Hidráulicas** | Floor Plans com filtros por sistema (5 cores) | ✅ Implementado |
| E11 | **Pranchas Automáticas** | ViewSheets com TitleBlock, numeração e posicionamento | ✅ Implementado |
| E12 | **Exportação** | CSV (UTF-8) e Excel (.xlsx) via OpenXML SDK | ✅ Implementado |
| E13 | **Dimensionamento** | Cálculo hidráulico completo (vazão, DN, pressão) | 🔄 Em desenvolvimento |
| E14 | **Orquestrador** | HydraulicOrchestrator de alto nível | 📋 Planejado |

---

## 🏗 Arquitetura

O sistema segue **Clean Architecture** combinada com **Domain-Driven Design (DDD)**, garantindo separação total entre regras de domínio e infraestrutura.

```
┌──────────────────────────────────────────────────────────────────┐
│                        Revit UI / Commands                       │
├──────────────────────────────────────────────────────────────────┤
│                    Modules (Entregáveis)                          │
│      Schedules · Sheets · Views · Export                         │
├──────────────────────────────────────────────────────────────────┤
│                Infrastructure (Thread-Safety)                     │
│   BaseExternalEventHandler · EventActionQueue · Handlers         │
├──────────────────────────────────────────────────────────────────┤
│                    Services (Revit API)                           │
│    RoomReader · SpaceManager · Sistemas · Inserção               │
├──────────────────────────────────────────────────────────────────┤
│                   PluginCore (Domínio)                            │
│              Models · Interfaces · Pipeline · Strategies         │
├──────────────────────────────────────────────────────────────────┤
│                      Data / Config                                │
│             Normas · Parâmetros · Mapeamentos                    │
└──────────────────────────────────────────────────────────────────┘
```

### Princípios

- **Independência do Revit**: O PluginCore não referencia a API do Revit — toda comunicação é feita via interfaces e DTOs
- **Thread-Safety**: Todas as operações Revit passam por `BaseExternalEventHandler` com `ConcurrentQueue`
- **Pipeline orquestrado**: Execução por etapas com aprovação humana, retry e rollback
- **Padrões GoF**: Strategy (decisões por ambiente), Observer (eventos/logging), Pipeline (fluxo de execução)
- **Classificação de erros**: Sistema estruturado com 3 níveis (Critical → Stop, Warning → Pause, Info → Continue)

---

## 📁 Estrutura do Projeto

```
Revit-Hydraulic-Automation/
│
├── PluginCore/                          # Domínio e regras de negócio
│   ├── Interfaces/                     # Contratos (IAmbienteService, ILogService, ...)
│   ├── Models/                         # Modelos (AmbienteInfo, EquipamentoHidraulico, ...)
│   ├── Services/                       # Implementações de domínio
│   ├── Pipeline/                       # PipelineRunner, PipelineContext
│   ├── Logging/                        # Sistema de logging estruturado
│   └── Domain/                         # Entidades e value objects
│
├── Revit2026/                           # Integração com Revit 2026
│   ├── Commands/                       # ExternalCommands do Revit
│   ├── Infrastructure/                 # Thread-safety e eventos
│   │   └── ExternalEvents/
│   │       ├── BaseExternalEventHandler.cs
│   │       ├── EventQueue/
│   │       │   └── EventActionQueue.cs     # Fila com callbacks
│   │       └── Handlers/
│   │           ├── CreateSpacesEventHandler.cs
│   │           ├── InsertEquipmentEventHandler.cs
│   │           └── CreateNetworkEventHandler.cs
│   │
│   ├── Services/                       # Adapters para Revit API
│   │   ├── RoomReaderService.cs        # Leitura de Rooms
│   │   ├── SpaceManagerService.cs      # Gestão de Spaces MEP
│   │   ├── SpaceCreatorService.cs      # Criação de Spaces
│   │   ├── RoomSpaceMatcherService.cs  # Correspondência Room↔Space
│   │   ├── SpaceClassificationTransferService.cs
│   │   ├── SpaceOrphanDetectorService.cs
│   │   ├── LevelReaderService.cs       # Leitura de Levels
│   │   ├── MEPFixtureReaderService.cs  # Leitura de equipamentos MEP
│   │   ├── MepFamilyDetectorService.cs # Detecção de famílias
│   │   ├── RevitElementMapper.cs       # Mapeamento de elementos
│   │   ├── Insercao/
│   │   │   └── EquipmentInsertionService.cs
│   │   ├── Sistemas/
│   │   │   ├── PipingSystemCreator.cs       # Água Fria / Quente
│   │   │   ├── WasteSystemCreator.cs        # Esgoto Sanitário
│   │   │   ├── VentSystemCreator.cs         # Ventilação
│   │   │   ├── SystemAssignmentService.cs   # Atribuição a sistemas
│   │   │   ├── SystemConnectivityValidator.cs  # Validação BFS
│   │   │   └── SystemNamingService.cs       # Nomenclatura padrão
│   │   ├── Posicionamento/             # Lógica de posicionamento
│   │   └── Tubulacao/                  # Serviços de tubulação
│   │
│   ├── Modules/                        # Módulos de entregáveis
│   │   ├── Schedules/
│   │   │   ├── Common/
│   │   │   │   └── ScheduleConfiguratorService.cs   # Configurador centralizado
│   │   │   ├── Piping/
│   │   │   │   ├── PipeQuantityScheduleCreator.cs   # Quantitativo tubulação
│   │   │   │   └── PipeFittingQuantityScheduleCreator.cs  # Quantitativo conexões
│   │   │   ├── Equipment/
│   │   │   │   └── EquipmentByRoomScheduleCreator.cs  # Equipamentos por ambiente
│   │   │   └── Export/
│   │   │       └── ScheduleExportService.cs   # CSV + Excel (.xlsx)
│   │   │
│   │   ├── Sheets/
│   │   │   ├── ViewSheetCreator.cs          # Criação de pranchas
│   │   │   ├── ViewPlacementService.cs      # Posicionamento de Views
│   │   │   ├── SchedulePlacementService.cs  # Posicionamento de Schedules
│   │   │   ├── LegendsAndNotesService.cs    # Legendas e notas técnicas
│   │   │   └── SheetNumberingService.cs     # Numeração sequencial
│   │   │
│   │   └── Views/
│   │       └── FloorPlanHydraulicViewCreator.cs  # Floor Plans hidráulicas
│   │
│   ├── HidraulicaRevit.addin          # Manifesto do plugin
│   └── Revit2026.csproj               # Projeto .NET 8
│
├── DynamoScripts/                       # Scripts Dynamo por etapa
│   └── 01_Ambientes/
│
├── Data/                                # Dados e configuração
│   ├── Config/                         # Parâmetros hidráulicos (JSON)
│   ├── Mappings/                       # Mapeamentos de classificação
│   └── Logs/                           # Logs de execução
│
├── docs/                                # Documentação técnica
│   ├── ArquiteturaGeral/
│   ├── DefinicaoNormativa/             # Tabelas NBR 5626 e NBR 8160
│   ├── EstrategiaDeDesenvolvimento/
│   ├── Escopo&Requisitos/
│   ├── ModeloDeDominio/
│   ├── ServicosCore/
│   └── Patterns/
│
├── HydraulicUI/                         # Interface gráfica (WPF)
│
└── PluginRevit.sln                      # Solution principal
```

---

## 🧩 Componentes Implementados

### 🔒 Infrastructure — Thread-Safety

| Componente | Descrição |
|------------|-----------|
| **BaseExternalEventHandler** | Handler base com `ConcurrentQueue`, guard contra reentrada, Transaction automática |
| **EventActionQueue** | Fila com callbacks tipados, métricas (Interlocked), evento `ItemProcessado` |
| **CreateSpacesEventHandler** | Criação de MEP Spaces via `SpaceManagerService` |
| **InsertEquipmentEventHandler** | Inserção individual, lote e por ambiente |
| **CreateNetworkEventHandler** | Criação de redes (9 tipos) — despacha para 3 creators |

### 🔧 Services — Revit API

| Serviço | Descrição |
|---------|-----------|
| **RoomReaderService** | Leitura e classificação de Rooms |
| **SpaceManagerService** | CRUD completo de MEP Spaces |
| **RoomSpaceMatcherService** | Correspondência geométrica Room↔Space |
| **EquipmentInsertionService** | Inserção de FamilyInstance com Level e parâmetros |
| **PipingSystemCreator** | PipingSystem para Água Fria / Quente |
| **WasteSystemCreator** | PipingSystem para Esgoto (3 subtipos) |
| **VentSystemCreator** | PipingSystem para Ventilação (4 subtipos) |
| **SystemAssignmentService** | Atribuição de elementos + auto-conexão (≤5mm) |
| **SystemConnectivityValidator** | Grafo BFS para detectar ilhas e conectores abertos |
| **SystemNamingService** | Nomenclatura `MEP-[AF/ES/VT]-[NÍVEL]-[XX]` |

### 📊 Modules — Entregáveis

| Módulo | Descrição |
|--------|-----------|
| **PipeQuantityScheduleCreator** | Schedule de tubulação (6 campos, filtros, sorting, formatação mm/m) |
| **PipeFittingQuantityScheduleCreator** | Schedule de conexões (7 campos + COUNT) |
| **EquipmentByRoomScheduleCreator** | Equipamentos agrupados por Room (7 campos) |
| **ScheduleConfiguratorService** | Configuração centralizada (campos, filtros, sorting, formatação) |
| **ScheduleExportService** | Exportação CSV (`;` UTF-8) e Excel (.xlsx via OpenXML) |
| **FloorPlanHydraulicViewCreator** | Floor Plans com 5 filtros por sistema (cores), 25 categorias ocultas |
| **ViewSheetCreator** | Criação de pranchas com TitleBlock automático |
| **ViewPlacementService** | Posicionamento grid com dimensões variáveis |
| **SchedulePlacementService** | Posicionamento em coluna/duas colunas balanceadas |
| **LegendsAndNotesService** | Legendas + TextNotes com notas NBR padrão |
| **SheetNumberingService** | Numeração sequencial com reordenação em 2 fases |

---

## ⚙ Como Funciona

O plugin opera como uma **pipeline semi-automática**:

```
 Usuário inicia       E01          E02          E03-E04
 o plugin         ┌─────────┐ ┌─────────┐ ┌──────────────┐
 ──────────────▶  │ Detectar │→│Classificar│→│Criar Spaces  │
                  │Ambientes │ │Ambientes │ │Inserir Equip.│
                  └────┬─────┘ └────┬─────┘ └──────┬───────┘
                       │            │              │
                    Aprovar ✅   Aprovar ✅      Aprovar ✅
                       │            │              │
                       ▼            ▼              ▼
                  ┌──────────────────────────────────────────┐
                  │       PipelineContext (dados)             │
                  │  Rooms → Spaces → Equipment → Networks   │
                  └──────────────────────────────────────────┘
                       │            │              │
                    E06-E08      E09-E10        E11-E12
                  ┌─────────┐ ┌─────────┐ ┌──────────────┐
                  │ Criar   │→│ Tabelas │→│  Pranchas +  │
                  │ Sistemas│ │  Views  │ │  Exportação  │
                  └────┬────┘ └────┬────┘ └──────┬───────┘
                       │           │             │
                    ✅ BFS      ✅ CSV/XLSX    ✅ Numeração
                   Validação    Exportação     Automática
```

### Execução Thread-Safe

```
Qualquer Thread (UI, Dynamo, Webhook)
    └── handler.EnfileirarAcao(action)
        └── ConcurrentQueue<Action<UIApplication>>
            └── ExternalEvent.Raise()
                └── Thread Revit: Execute(UIApplication)
                    ├── Transaction
                    │   └── Operação na API do Revit
                    ├── Commit / RollBack
                    └── Callback(resultado)
```

### Fluxo de cada etapa

1. **Execução automática** — O sistema processa os dados
2. **Validação** — Erros são classificados (🛑 Critical / ⚠️ Warning / ℹ️ Info)
3. **Aprovação humana** — O engenheiro revisa e aprova ou rejeita
4. **Próxima etapa** — O pipeline avança com os dados aprovados

---

## 🛠 Tecnologias

| Tecnologia | Uso |
|-----------|-----|
| **C# / .NET 8** | Linguagem principal do PluginCore e integração Revit |
| **Autodesk Revit API** | Leitura/escrita de elementos no modelo BIM |
| **DocumentFormat.OpenXml 3.2.0** | Exportação de schedules para Excel (.xlsx) |
| **Newtonsoft.Json 13.0.3** | Serialização de configurações e comunicação entre camadas |
| **Dynamo** | Execução de automações visuais (inserção de equipamentos, roteamento) |
| **WPF** | Interface gráfica do plugin (HydraulicUI) |

---

## 📐 Normas Técnicas

O sistema implementa computacionalmente as seguintes normas brasileiras:

| Norma | Aplicação |
|-------|-----------| 
| **NBR 5626:2020** | Instalações prediais de água fria e água quente — dimensionamento, vazões, pressões |
| **NBR 5648** | Tubulações de PVC soldável para água fria |
| **NBR 5688** | Tubulações de PVC série normal para esgoto |
| **NBR 8160:1999** | Instalações prediais de esgoto sanitário — dimensionamento, ventilação, declividade |

### Dados normativos implementados

- Tabela de pesos e unidades Hunter de contribuição por aparelho
- Diâmetros mínimos por tipo de equipamento
- Regras de declividade por diâmetro
- Critérios de ventilação
- Limites de velocidade e pressão
- Notas técnicas padrão para pranchas hidráulicas

---

## 🗺 Roadmap

### Fase 1 — Fundação ✅
- [x] Estrutura do projeto (Clean Architecture)
- [x] Modelos de domínio (7 entidades)
- [x] Interfaces de serviço (8 contratos)
- [x] Padrões de implementação (Pipeline, Strategy, Observer, Error Classification)
- [x] Definições normativas (NBR 5626 e NBR 8160)
- [x] Documentação técnica completa

### Fase 2 — Core Services ✅
- [x] RoomReaderService (leitura e classificação de ambientes)
- [x] SpaceManagerService (criação e gestão de Spaces MEP)
- [x] RoomSpaceMatcherService (correspondência Room↔Space)
- [x] SpaceOrphanDetectorService (detecção de Spaces órfãos)
- [x] LevelReaderService (leitura de Levels)
- [x] Sistema de logging estruturado (ILogService)

### Fase 3 — Infrastructure Thread-Safe ✅
- [x] BaseExternalEventHandler (ConcurrentQueue + guard contra reentrada)
- [x] EventActionQueue (fila com callbacks tipados e métricas)
- [x] CreateSpacesEventHandler
- [x] InsertEquipmentEventHandler (individual, lote, por ambiente)
- [x] CreateNetworkEventHandler (9 tipos de rede)

### Fase 4 — Sistemas MEP ✅
- [x] PipingSystemCreator (Água Fria / Quente)
- [x] WasteSystemCreator (Esgoto — 3 subtipos)
- [x] VentSystemCreator (Ventilação — 4 subtipos)
- [x] SystemAssignmentService (atribuição + auto-conexão ≤5mm)
- [x] SystemConnectivityValidator (BFS para ilhas desconectadas)
- [x] SystemNamingService (MEP-[SIGLA]-[NÍVEL]-[XX])

### Fase 5 — Entregáveis ✅
- [x] PipeQuantityScheduleCreator (quantitativo de tubulação)
- [x] PipeFittingQuantityScheduleCreator (quantitativo de conexões)
- [x] EquipmentByRoomScheduleCreator (equipamentos por ambiente)
- [x] ScheduleConfiguratorService (configuração centralizada)
- [x] ScheduleExportService (CSV + Excel .xlsx)
- [x] FloorPlanHydraulicViewCreator (5 filtros por sistema com cor)
- [x] ViewSheetCreator (pranchas com TitleBlock automático)
- [x] ViewPlacementService (posicionamento grid)
- [x] SchedulePlacementService (coluna/duas colunas)
- [x] LegendsAndNotesService (legendas + notas NBR)
- [x] SheetNumberingService (numeração com reordenação 2-fases)

### Fase 6 — Dimensionamento e Orquestração 🔄
- [ ] Motor de dimensionamento hidráulico completo
- [ ] Verificação de pressão no ponto crítico
- [ ] HydraulicOrchestrator (coordenação do pipeline completo)
- [ ] Interface WPF (HydraulicUI) funcional
- [ ] Testes de integração em modelo real

---

## 📦 Pré-requisitos

| Requisito | Versão |
|-----------|--------|
| Autodesk Revit | 2026 |
| .NET SDK | 8.0+ |
| Visual Studio | 2022+ |
| Dynamo | 3.x (incluído no Revit) |

---

## 🚀 Instalação

### Build

```bash
# Clonar o repositório
git clone https://github.com/MurilloSanttos/Revit-Hydraulic-Automation.git

# Abrir a solution
start PluginRevit.sln

# Build via Visual Studio ou CLI
dotnet build
```

### Deploy no Revit

O deploy é automático durante o build (modo Debug):

1. Compile o projeto `Revit2026`
2. O `DeployToRevit` target copia automaticamente:
   - `HidraulicaRevit.addin` → `%APPDATA%\Autodesk\Revit\Addins\2026\`
   - DLLs → `%APPDATA%\Autodesk\Revit\Addins\2026\HidraulicaRevit\`
3. Reinicie o Revit

---

## 🤝 Contribuição

### Padrão de Commits

```
tipo(escopo): descrição curta

feat(sistemas): implementar VentSystemCreator com 4 subtipos
feat(schedules): exportação Excel via OpenXML SDK
fix(pipeline): corrigir rollback em cascata
docs(readme): atualizar roadmap
refactor(models): renomear AmbienteInfo → RoomInfo
test(sizing): adicionar testes de pressão
```

### Organização de Código

- **Interfaces** antes de implementações
- **1 classe por arquivo** (interfaces no mesmo arquivo quando acopladas)
- **Nomes em inglês** no código, **documentação em português**
- **Sem dependência do Revit** no PluginCore
- **Thread-safety**: toda operação Revit via `BaseExternalEventHandler`
- **Logs obrigatórios**: todo serviço registra via `ILogService`

---

## 📄 Licença

Este projeto é de **uso privado e interno**. Todos os direitos reservados.

---

<p align="center">
  <sub>Desenvolvido por <strong>Murillo Santtos</strong> — Engenharia Hidráulica + Automação BIM</sub>
</p>
