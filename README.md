<p align="center">
  <img src="https://img.shields.io/badge/Revit-2026-blue?style=for-the-badge&logo=autodesk" alt="Revit 2026"/>
  <img src="https://img.shields.io/badge/C%23-.NET%208-purple?style=for-the-badge&logo=dotnet" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/Architecture-Clean%20%2B%20DDD-green?style=for-the-badge" alt="Clean Architecture"/>
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

O **Revit Hydraulic Automation** é um plugin integrado ao Autodesk Revit que automatiza o projeto hidráulico residencial. O sistema opera como um **orquestrador inteligente**: ele analisa o modelo BIM, toma decisões baseadas em regras normativas brasileiras, e delega a criação de elementos MEP para ferramentas especializadas como **Dynamo** e **unMEP**.

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
- **Produz tabelas e pranchas** prontas para entrega

Cada etapa é validada pelo engenheiro antes de avançar, garantindo **controle total** sobre o resultado.

---

## ⚡ Funcionalidades

| # | Etapa | Descrição | Status |
|---|-------|-----------|--------|
| E01 | **Detecção de Ambientes** | Leitura de Rooms/Spaces do modelo Revit | 🔄 Em desenvolvimento |
| E02 | **Classificação Inteligente** | Classificação automática por tipo (banheiro, cozinha, etc.) | 🔄 Em desenvolvimento |
| E03 | **Identificação de Equipamentos** | Definição de equipamentos por ambiente (Strategy Pattern) | 📋 Planejado |
| E04 | **Inserção Automática** | Posicionamento de aparelhos sanitários via Dynamo | 📋 Planejado |
| E05 | **Validação do Modelo** | Verificação de consistência e completude | 📋 Planejado |
| E06 | **Criação de Prumadas** | Geração de prumadas verticais por sistema | 📋 Planejado |
| E07 | **Rede de Água Fria** | Geração automática da rede AF | 📋 Planejado |
| E08 | **Rede de Esgoto** | Geração da rede ES com declividade | 📋 Planejado |
| E09 | **Rede de Ventilação** | Geração da rede de ventilação | 📋 Planejado |
| E10 | **Exportação para Revit** | Materialização das redes no modelo via Dynamo/unMEP | 📋 Planejado |
| E11 | **Dimensionamento** | Cálculo hidráulico completo (vazão, DN, pressão, velocidade) | 📋 Planejado |
| E12 | **Tabelas e Pranchas** | Geração de tabelas de dimensionamento e pranchas finais | 📋 Planejado |

---

## 🏗 Arquitetura

O sistema segue **Clean Architecture** combinada com **Domain-Driven Design (DDD)**, garantindo separação total entre regras de domínio e infraestrutura.

```
┌─────────────────────────────────────────────────────────┐
│                    Revit UI / Commands                   │
├─────────────────────────────────────────────────────────┤
│              Revit Integration (Adapters)                │
│         RoomReader · ScheduleWriter · SheetWriter        │
├──────────────┬──────────────────────┬───────────────────┤
│   Dynamo     │     PluginCore       │      unMEP        │
│  Integration │  ┌────────────────┐  │   Integration     │
│              │  │ Domain Models  │  │                   │
│  .dyn scripts│  │ Services (I*)  │  │  Pipe routing     │
│  JSON I/O    │  │ Pipeline       │  │  MEP elements     │
│              │  │ Strategies     │  │                   │
│              │  │ Error Handling │  │                   │
│              │  └────────────────┘  │                   │
├──────────────┴──────────────────────┴───────────────────┤
│                    Data / Config                         │
│           Normas · Parâmetros · Mapeamentos              │
└─────────────────────────────────────────────────────────┘
```

### Princípios

- **Independência do Revit**: O PluginCore não referencia a API do Revit — toda comunicação é feita via interfaces e DTOs
- **Pipeline orquestrado**: Execução por etapas com aprovação humana, retry e rollback
- **Padrões GoF aplicados**: Strategy (decisões por ambiente), Observer (eventos/logging), Pipeline (fluxo de execução)
- **Classificação de erros**: Sistema estruturado com 3 níveis (Critical → Stop, Warning → Pause, Info → Continue)

---

## 📁 Estrutura do Projeto

```
Revit-Hydraulic-Automation/
│
├── PluginCore/                    # Domínio e regras de negócio
│   ├── Interfaces/                # Contratos de serviço (IAmbienteService, ILogService)
│   ├── Models/                    # Modelos de domínio (AmbienteInfo, etc.)
│   ├── Services/                  # Implementações de domínio
│   └── Logging/                   # Sistema de logging estruturado
│
├── Revit2026/                     # Integração com Revit 2026
│   ├── Commands/                  # ExternalCommands do Revit
│   ├── Services/                  # Adapters (RoomReader, SpaceManager)
│   └── HidraulicaRevit.addin     # Manifesto do plugin
│
├── DynamoScripts/                 # Scripts Dynamo por etapa
│   └── 01_Ambientes/             # Scripts de detecção de ambientes
│
├── Data/                          # Dados e configuração
│   ├── Config/                    # Parâmetros hidráulicos (JSON)
│   ├── Mappings/                  # Mapeamentos de classificação
│   └── Logs/                      # Logs de execução
│
├── docs/                          # Documentação técnica
│   ├── ArquiteturaGeral/          # Diagramas e decisões arquiteturais
│   ├── DefinicaoNormativa/        # Tabelas NBR 5626 e NBR 8160
│   ├── EstrategiaDeDesenvolvimento/ # Estratégias de teste e desenvolvimento
│   ├── Escopo&Requisitos/         # Escopo funcional e critérios de aceitação
│   ├── ModeloDeDominio/           # Especificação dos 7 modelos de domínio
│   ├── ServicosCore/              # Especificação das 8 interfaces de serviço
│   └── Patterns/                  # Padrões de implementação (Pipeline, Strategy, etc.)
│
└── tests/                         # Testes unitários e de integração
```

---

## ⚙ Como Funciona

O plugin opera como uma **pipeline semi-automática de 12 etapas**:

```
 Usuário inicia       E01          E02          E03          E04
 o plugin         ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
 ──────────────▶  │ Detectar │→│Classificar│→│Identificar│→│ Inserir │
                  │Ambientes │ │Ambientes │ │ Equip.  │ │ Equip.  │
                  └────┬─────┘ └────┬─────┘ └────┬─────┘ └────┬────┘
                       │            │            │            │
                    Aprovar ✅   Aprovar ✅   Aprovar ✅   Aprovar ✅
                       │            │            │            │
                       ▼            ▼            ▼            ▼
                  ┌──────────────────────────────────────────────────┐
                  │            PipelineContext (dados)               │
                  │  Rooms → Equipment → Points → Networks → Sizing │
                  └──────────────────────────────────────────────────┘
                       │            │            │            │
                    E06-E09      E10          E11         E12
                  ┌─────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐
                  │  Gerar  │→│Exportar │→│Dimensio-│→│ Tabelas │
                  │  Redes  │ │ p/Revit │ │  nar    │ │ Pranchas│
                  └─────────┘ └─────────┘ └─────────┘ └─────────┘
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
| **Dynamo** | Execução de automações visuais (inserção de equipamentos, roteamento) |
| **unMEP** | Roteamento avançado de tubulações e conexões MEP |
| **JSON** | Contrato de comunicação entre camadas e persistência de configuração |

---

## 📐 Normas Técnicas

O sistema implementa computacionalmente as seguintes normas brasileiras:

| Norma | Aplicação |
|-------|-----------|
| **NBR 5626:2020** | Instalações prediais de água fria e água quente — dimensionamento, vazões, pressões |
| **NBR 8160:1999** | Instalações prediais de esgoto sanitário — dimensionamento, ventilação, declividade |

### Dados normativos implementados

- Tabela de pesos e unidades Hunter de contribuição por aparelho
- Diâmetros mínimos por tipo de equipamento
- Regras de declividade por diâmetro
- Critérios de ventilação
- Limites de velocidade e pressão

---

## 🗺 Roadmap

### Fase 1 — Fundação ✅
- [x] Estrutura do projeto (Clean Architecture)
- [x] Modelos de domínio (7 entidades)
- [x] Interfaces de serviço (8 contratos)
- [x] Padrões de implementação (Pipeline, Strategy, Observer, Error Classification)
- [x] Definições normativas (NBR 5626 e NBR 8160)
- [x] Documentação técnica completa

### Fase 2 — Core Services 🔄
- [ ] Implementação do IRoomService
- [ ] Implementação do IEquipmentService
- [ ] Pipeline Runner funcional
- [ ] Sistema de logging
- [ ] Error handling

### Fase 3 — Integração Revit
- [ ] RoomReader (leitura de ambientes do Revit)
- [ ] Integração com Dynamo (execução de scripts)
- [ ] Integração com unMEP (roteamento)

### Fase 4 — Redes e Dimensionamento
- [ ] Geração de redes (AF, ES, VE)
- [ ] Motor de dimensionamento hidráulico
- [ ] Verificação de pressão no ponto crítico

### Fase 5 — Entregáveis
- [ ] Geração de tabelas de dimensionamento
- [ ] Geração de quantitativos (BOM)
- [ ] Montagem de pranchas

---

## 📦 Pré-requisitos

| Requisito | Versão |
|-----------|--------|
| Autodesk Revit | 2026 |
| .NET SDK | 8.0+ |
| Visual Studio | 2022+ |
| Dynamo | 3.x (incluído no Revit) |
| unMEP | Compatível com Revit 2026 |

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

1. Compile o projeto `Revit2026`
2. Copie os arquivos de saída para o diretório de addins do Revit:
   ```
   %APPDATA%\Autodesk\Revit\Addins\2026\
   ```
3. Copie o arquivo `HidraulicaRevit.addin` para o mesmo diretório
4. Reinicie o Revit

---

## 🤝 Contribuição

### Padrão de Commits

```
tipo(escopo): descrição curta

feat(core): adicionar ISizingService
fix(pipeline): corrigir rollback em cascata
docs(readme): atualizar roadmap
refactor(models): renomear AmbienteInfo → RoomInfo
test(sizing): adicionar testes de pressão
```

### Organização de Código

- **Interfaces** antes de implementações
- **1 classe por arquivo**
- **Nomes em inglês** no código, **documentação em português**
- **Sem dependência do Revit** no PluginCore
- **Testes para toda lógica** de domínio

---

## 📄 Licença

Este projeto é de **uso privado e interno**. Todos os direitos reservados.

---

<p align="center">
  <sub>Desenvolvido por <strong>Murillo Santtos</strong> — Engenharia Hidráulica + Automação BIM</sub>
</p>
