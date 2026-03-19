# Fluxo de Versionamento e Controle de Código — Plugin Hidráulico Revit

> Governança completa de código, branches, commits, releases e integração com IA para desenvolvimento incremental do plugin.

---

## 1. Estratégia de Versionamento

### 1.1 Modelo adotado: Semantic Versioning (SemVer)

```
MAJOR.MINOR.PATCH[-prerelease]

Exemplo: 1.3.2-beta.1
```

| Componente | Incrementa quando | Exemplo |
|-----------|-------------------|---------|
| **MAJOR** | Mudança incompatível na API, reestruturação de módulos, mudança de norma que altera cálculos | 1.0.0 → 2.0.0 |
| **MINOR** | Novo módulo, nova funcionalidade, nova etapa do pipeline | 1.0.0 → 1.1.0 |
| **PATCH** | Correção de bug, ajuste de parâmetro, fix de validação | 1.1.0 → 1.1.1 |
| **Pre-release** | Build de teste, não é produção | 1.2.0-alpha.1 |

### 1.2 Regras de incremento

```
REGRA 1: PATCH reseta para 0 quando MINOR incrementa
  1.3.5 → 1.4.0 (novo módulo)

REGRA 2: MINOR reseta para 0 quando MAJOR incrementa
  1.7.3 → 2.0.0 (reestruturação)

REGRA 3: Pre-release incremental
  1.2.0-alpha.1 → 1.2.0-alpha.2 → 1.2.0-beta.1 → 1.2.0

REGRA 4: Versão NUNCA retroage
  1.5.0 publicada → próxima é ≥ 1.5.1
```

### 1.3 Mapeamento versão → módulos

| Versão planejada | Módulos incluídos | Marco |
|-----------------|-------------------|-------|
| 0.1.0-alpha | M14 (Logs) + M15 (UI base) | Infraestrutura funcional |
| 0.2.0-alpha | M01 (Detecção) + M02 (Classificação) | Leitura do modelo |
| 0.3.0-alpha | M03 (Pontos) + M05 (Validação) | Análise hidráulica |
| 0.4.0-alpha | M04 (Inserção) | Primeira modificação do modelo |
| 0.5.0-alpha | M06 (Prumadas) + M10 (Sistemas MEP) | Infraestrutura de rede |
| 0.6.0-beta | M07 (Rede AF) + M08 (Rede ES) | Geração de redes |
| 0.7.0-beta | M09 (Inclinações) + M11 (Dimensionamento) | Motor de cálculo |
| 0.8.0-beta | M12 (Tabelas) + M13 (Pranchas) | Documentação |
| 0.9.0-rc | Todos os módulos | Release Candidate |
| **1.0.0** | Todos os módulos validados | **Primeira versão estável** |

---

## 2. Estrutura de Branches

### 2.1 Modelo: Git Flow simplificado para equipe pequena + IA

```
main ──────────────────────────────────────────→ produção estável
  │
  └── develop ─────────────────────────────────→ integração contínua
        │
        ├── feature/M01-deteccao-ambientes ───→ módulo em desenvolvimento
        ├── feature/M02-classificacao ────────→ módulo em desenvolvimento
        ├── feature/M14-logs ────────────────→ módulo em desenvolvimento
        ├── fix/corrigir-conversao-unidades ──→ correção pontual
        │
        └── release/0.2.0-alpha ─────────────→ preparação de release
              │
              └── (merge para main + tag)
```

### 2.2 Definição de cada branch

| Branch | Função | Origem | Destino | Proteção |
|--------|--------|--------|---------|----------|
| `main` | Código estável e testado. Cada commit é uma versão publicável. | release/* ou hotfix/* | — | 🔒 Protegida. Merge apenas via PR aprovado. |
| `develop` | Integração de features. Sempre compilável, mas pode ter bugs. | main (inicial) | release/* | ⚠️ Semi-protegida. Merge via PR. |
| `feature/{id}-{nome}` | Desenvolvimento de um módulo ou funcionalidade. | develop | develop | 🔓 Livre. Desenvolvedor/IA altera diretamente. |
| `fix/{descricao}` | Correção de bug identificado em develop. | develop | develop | 🔓 Livre. |
| `release/{versao}` | Preparação de release. Apenas fixes e docs. | develop | main + develop | ⚠️ Semi-protegida. Apenas fixes permitidos. |
| `hotfix/{descricao}` | Correção urgente em produção. | main | main + develop | ⚠️ Requer agilidade. PR simplificado. |

### 2.3 Nomenclatura obrigatória

```
feature/M{NN}-{descricao-curta}
  Exemplos:
    feature/M01-deteccao-ambientes
    feature/M07-rede-agua-fria
    feature/M14-sistema-logs

fix/{descricao-curta}
  Exemplos:
    fix/conversao-unidades-area
    fix/classificacao-bwc

release/{versao}
  Exemplos:
    release/0.2.0-alpha
    release/1.0.0

hotfix/{descricao-curta}
  Exemplos:
    hotfix/crash-ao-abrir-sem-rooms
```

---

## 3. Fluxo de Desenvolvimento

### 3.1 Ciclo completo (por módulo)

```
┌─────────────────────────────────────────────────────────────────────┐
│ 1. PLANEJAMENTO                                                     │
│    → Definir módulo, consultar docs, identificar dependências        │
│    → Criar issue/tarefa com escopo claro                            │
│                                                                     │
│ 2. BRANCH                                                           │
│    → git checkout develop                                           │
│    → git pull origin develop                                        │
│    → git checkout -b feature/M{NN}-{nome}                           │
│                                                                     │
│ 3. DESENVOLVIMENTO                                                  │
│    → Implementar em C# (classes, services, models)                  │
│    → Se necessário: criar script Dynamo (.dyn)                      │
│    → Commits frequentes (a cada unidade funcional)                  │
│    → IA (Claude) gera código → desenvolvedor revisa e commita       │
│                                                                     │
│ 4. TESTES LOCAIS                                                    │
│    → Compilar solution                                              │
│    → Testar no Revit com modelo de teste                            │
│    → Verificar critérios de validação da etapa                      │
│    → Corrigir bugs encontrados                                      │
│                                                                     │
│ 5. PULL REQUEST                                                     │
│    → git push origin feature/M{NN}-{nome}                           │
│    → Abrir PR: feature/M{NN} → develop                             │
│    → Preencher template de PR                                       │
│                                                                     │
│ 6. REVISÃO                                                          │
│    → Code review (humano + IA)                                      │
│    → Verificar padrões de código                                    │
│    → Verificar cobertura de testes                                  │
│    → Verificar compatibilidade com módulos existentes               │
│                                                                     │
│ 7. MERGE                                                            │
│    → Squash merge para develop                                      │
│    → Deletar branch feature/*                                       │
│    → Verificar CI (compilação pós-merge)                            │
└─────────────────────────────────────────────────────────────────────┘
```

### 3.2 Diagrama visual

```
develop:    ──●───────●───────────●─────────●──────→
               \     / \         / \       /
feature/M14:    ●──●    \       /   \     /
                         \     /     \   /
feature/M15:              ●──●       \ /
                                      │
feature/M01:                          ●──●──●
```

---

## 4. Padrão de Commits

### 4.1 Formato obrigatório: Conventional Commits

```
<tipo>(<escopo>): <descrição curta>

[corpo opcional — detalhe técnico]

[rodapé opcional — referências]
```

### 4.2 Tipos de commit

| Tipo | Quando usar | Exemplo |
|------|-----------|---------|
| `feat` | Nova funcionalidade | `feat(M01): implementar leitura de Rooms` |
| `fix` | Correção de bug | `fix(M01): corrigir conversão ft² para m²` |
| `refactor` | Melhoria sem alterar comportamento | `refactor(M02): extrair lógica de matching para classe` |
| `test` | Testes | `test(M01): adicionar teste para Room sem Location` |
| `docs` | Documentação | `docs: atualizar referencia_normativa.json` |
| `style` | Formatação, espaços, ponto-e-vírgula | `style(M14): formatação code style` |
| `chore` | Configuração, build, CI | `chore: atualizar .csproj para Revit 2024` |
| `perf` | Performance | `perf(M07): otimizar FilteredElementCollector` |
| `ci` | Integração contínua | `ci: adicionar build check no PR` |

### 4.3 Regras de commit

```
REGRA 1: Descrição em português, técnica e clara
  ✅ feat(M02): implementar matching fuzzy para nomes de ambientes
  ❌ feat: mudanças no classificador

REGRA 2: Escopo = ID do módulo ou área
  ✅ fix(M11): corrigir cálculo de perda de carga FWH
  ❌ fix: bug no cálculo

REGRA 3: Um commit = uma unidade lógica
  ✅ Um commit por classe criada ou funcionalidade implementada
  ❌ Um commit com 15 arquivos de 3 módulos diferentes

REGRA 4: Commits da IA devem ser identificados
  ✅ feat(M01): implementar RoomReader [AI-assisted]
  ❌ feat(M01): implementar RoomReader (sem indicação)

REGRA 5: Corpo para mudanças complexas
  feat(M11): implementar cálculo de pressão disponível

  Calcula P_din = P_est - ΣΔH para cada ponto de consumo.
  Usa fórmula FWH com fator K=1.20 para perdas localizadas.
  
  Ref: docs/DefinicaoNormativa/Dimensionamento_AguaFria_NBR5626.md
```

### 4.4 Commits proibidos

```
❌ "WIP"
❌ "fix"
❌ "mudanças"
❌ "update"
❌ "teste"
❌ Qualquer commit sem tipo e escopo
```

---

## 5. Controle de Versão de Scripts Dynamo

### 5.1 Desafio

Arquivos `.dyn` são JSONs grandes, difíceis de diff e merge. Conflitos de merge em .dyn são virtualmente irresolvíveis.

### 5.2 Estratégia adotada

| Regra | Descrição |
|-------|-----------|
| **Versionamento paralelo** | Scripts .dyn vivem no repositório mas com regras especiais |
| **Um script = uma branch** | Nunca editar 2 scripts na mesma branch |
| **Sem merge automático** | Sempre forçar merge manual para .dyn |
| **Nome versionado** | Exemplo: `04_InserirEquipamentos_v1.2.dyn` |
| **Changelog interno** | Manter bloco de notas no próprio script (nó Note) |

### 5.3 Estrutura de armazenamento

```
PluginRevit/
├── src/                       ← C# (merge normal)
├── dynamo/
│   ├── scripts/
│   │   ├── 04_InserirEquipamentos.dyn
│   │   ├── 07_GerarRedeAF.dyn
│   │   ├── 08_GerarRedeES.dyn
│   │   ├── 09_AplicarInclinacoes.dyn
│   │   └── 13_GerarPranchas.dyn
│   ├── packages/              ← Dependências Dynamo (fixadas)
│   │   └── packages.json      ← Lista de pacotes com versão
│   └── CHANGELOG_DYNAMO.md    ← Histórico de mudanças por script
└── ...
```

### 5.4 Regras para .dyn no Git

```gitattributes
# Arquivo .gitattributes na raiz do repositório
*.dyn binary
*.dyn merge=binary
```

Isso força Git a tratar .dyn como binário, evitando merge automático corrompido.

### 5.5 Compatibilidade

| Parâmetro | Valor |
|-----------|-------|
| Dynamo mínimo | 2.x (compatível com Revit 2022+) |
| Pacotes externos permitidos | Apenas os listados em `packages.json` |
| Quando atualizar script | Apenas na branch `feature/` correspondente |

---

## 6. Versionamento do Plugin (Builds)

### 6.1 Onde a versão é definida

| Local | Formato | Exemplo |
|-------|---------|---------|
| `AssemblyInfo.cs` | `[assembly: AssemblyVersion("M.m.p.0")]` | `1.3.2.0` |
| `AssemblyInfo.cs` | `[assembly: AssemblyFileVersion("M.m.p.build")]` | `1.3.2.47` |
| `referencia_normativa.json` | `metadata.versao` | `"1.0.0"` |
| `PluginInfo.cs` (constante) | `public const string Version = "M.m.p"` | `"1.3.2"` |
| `.addin` manifest | `<VendorDescription>` | Inclui versão |

### 6.2 Build number

```
AssemblyFileVersion: MAJOR.MINOR.PATCH.BUILD

BUILD = incremento automático a cada compilação
  Exemplo: 0.2.0.1, 0.2.0.2, 0.2.0.3, ...

BUILD reseta a cada PATCH increment:
  0.2.0.47 → 0.2.1.1
```

### 6.3 Compatibilidade com Revit

| Versão Revit | .NET Framework | API Version | Suportado |
|-------------|---------------|-------------|-----------|
| 2022 | .NET 4.8 | 2022.0 | ✅ |
| 2023 | .NET 4.8 | 2023.0 | ✅ |
| 2024 | .NET 4.8 / .NET Core | 2024.0 | ✅ |
| 2025 | .NET 8 | 2025.0 | ⚠️ Requer adaptação |

**Estratégia multi-versão:**
```
PluginRevit.sln
├── PluginRevit.Core/          ← Lógica sem referência ao Revit API
├── PluginRevit.Revit2022/     ← Adapter para Revit 2022
├── PluginRevit.Revit2024/     ← Adapter para Revit 2024
└── PluginRevit.Shared/        ← Interfaces e DTOs compartilhados
```

---

## 7. Estratégia de Releases

### 7.1 Tipos de release

| Tipo | Sufixo | Estabilidade | Público | Uso |
|------|--------|-------------|---------|-----|
| **Alpha** | `-alpha.N` | Instável. Pode ter bugs. Features incompletas. | Apenas dev | Teste interno durante desenvolvimento |
| **Beta** | `-beta.N` | Funcional. Pode ter bugs menores. | Dev + testers | Validação com modelos reais |
| **Release Candidate** | `-rc.N` | Praticamente pronta. Apenas fixes. | Dev + testers | Teste final antes de produção |
| **Produção** | (sem sufixo) | Estável. Testada. Validada. | Todos | Uso real em projetos |

### 7.2 Critérios para cada tipo

| De → Para | Critérios obrigatórios |
|-----------|----------------------|
| **Dev → Alpha** | Compilação sem erros. Módulos da fase funcionam isoladamente. |
| **Alpha → Beta** | Todos os módulos até a fase integram sem erro Crítico. Testes em 2+ modelos. |
| **Beta → RC** | Todos os 13 módulos funcionais. 0 erros Críticos. Pipeline completo em modelo de teste. |
| **RC → Produção** | 0 erros Críticos e 0 Médios não resolvidos. Testado em ≥ 3 modelos reais. Documentação completa. |

### 7.3 Processo de release

```
1. Criar branch release/{versao} a partir de develop
   git checkout develop
   git checkout -b release/0.3.0-alpha

2. Na branch release:
   - Atualizar AssemblyInfo.cs com nova versão
   - Atualizar CHANGELOG.md
   - Atualizar metadata.versao no JSON
   - Apenas fixes permitidos (sem features novas)

3. Testar na branch release
   - Compilar
   - Testar no Revit
   - Validar critérios da fase

4. Merge para main
   git checkout main
   git merge release/0.3.0-alpha --no-ff
   git tag -a v0.3.0-alpha -m "Alpha: Análise hidráulica (M03+M05)"

5. Merge de volta para develop
   git checkout develop
   git merge release/0.3.0-alpha

6. Deletar branch
   git branch -d release/0.3.0-alpha

7. Push tudo
   git push origin main develop --tags
```

---

## 8. Controle de Qualidade

### 8.1 Checklist obrigatório antes de merge para develop

| # | Item | Verificação |
|---|------|------------|
| 01 | Solution compila sem erros | Build succeeded |
| 02 | 0 warnings críticos | Verificar output |
| 03 | Código segue padrões do projeto | Naming, namespaces, organização |
| 04 | Commits seguem Conventional Commits | Verificar histórico |
| 05 | Módulo funciona isoladamente no Revit | Teste manual |
| 06 | Critérios de validação da etapa atendidos | Conferir doc Criterios_Validacao |
| 07 | JSON normativo não foi corrompido | Parsear referencia_normativa.json |
| 08 | Log funciona para o módulo | Eventos aparecem no DataGrid |

### 8.2 Checklist antes de merge para main (release)

| # | Item | Verificação |
|---|------|------------|
| 01 | Todos os itens de develop ✅ | — |
| 02 | Pipeline completo até a fase funciona | Testar E01→EN sequencialmente |
| 03 | 0 erros Críticos | Log limpo |
| 04 | Testado em ≥ 2 modelos | Teste_01 + Teste_02 |
| 05 | CHANGELOG atualizado | Descreve mudanças da versão |
| 06 | Versão atualizada em 3 locais | AssemblyInfo, JSON, PluginInfo |
| 07 | Scripts Dynamo compatíveis | Testar scripts usados na versão |

### 8.3 Testes obrigatórios por módulo

| Módulo | Teste obrigatório | Método |
|--------|-------------------|--------|
| M01 | Ler Rooms de modelo com 15+ Rooms | Manual no Revit |
| M02 | Classificar 50+ nomes com ≥ 90% acerto | Dataset de nomes |
| M04 | Inserir em ambiente retangular 2.5×3.0m | Manual no Revit |
| M07 | Conectar 10 pontos AF | Manual no Revit |
| M08 | Conectar 10 pontos ES com CX sif e gordura | Manual no Revit |
| M09 | Aplicar decliv em 20 trechos, 0 contra gravidade | Manual no Revit |
| M11 | Comparar dimensionamento com planilha Excel | Dados vs. planilha |

---

## 9. Integração com IA (Claude)

### 9.1 Como a IA deve gerar código

| Regra | Descrição |
|-------|-----------|
| **Escopo claro** | IA recebe prompt com escopo exato: classe, métodos, dependências |
| **Contexto normativo** | IA recebe trechos relevantes do `referencia_normativa.json` |
| **Padrão do projeto** | IA recebe exemplos de código existente para seguir padrões |
| **Uma classe por vez** | IA gera no máximo 1 classe por interação (evitar desalinhamento) |
| **Sem dependências novas** | IA não pode adicionar pacotes NuGet sem aprovação |

### 9.2 Template de prompt para IA

```
CONTEXTO:
- Módulo: M{NN} — {nome}
- Fase: F{N}
- Dependências: {lista de classes/services existentes}
- Arquivo normativo: {trecho relevante do JSON}

TAREFA:
Criar a classe {NomeClasse} no namespace PluginRevit.{Modulo}.
Deve implementar: {interface, se aplicável}
Deve consumir: {services existentes}

PADRÃO:
- C# 10+
- Revit API 2024
- Logging via LogService.Instance.Log(nivel, mensagem, elementId?)
- Transaction via TransactionHelper.Execute(doc, nome, action)

SAÍDA:
- Código completo da classe
- XML docs em todos os métodos públicos
- Sem TODO ou placeholders
```

### 9.3 Como validar código gerado pela IA

```
PROCEDIMENTO:

1. COMPILAÇÃO
   - Código compila sem erros? → Se não: devolver para IA com erro

2. REVISÃO HUMANA
   - Lógica está correta?
   - Segue padrões do projeto?
   - Usa LogService corretamente?
   - Usa Transaction corretamente?
   - Não introduz dependências novas?

3. TESTE FUNCIONAL
   - Executar no Revit com modelo de teste
   - Verificar resultado vs. esperado

4. INTEGRAÇÃO
   - Módulo integra com módulos existentes sem breaking changes?
   - Interfaces mantidas?
```

### 9.4 Rastreabilidade de contribuições da IA

```
COMMIT: feat(M01): implementar RoomReader [AI-assisted]

Corpo do commit:
  Gerado com assistência de IA (Claude Opus 4.6).
  Revisado e validado manualmente.
  Testado com modelo Teste_01_Basico.rvt.
  
  Prompt ID: session-{data}-{modulo}
```

Tag `[AI-assisted]` obrigatória em commits com código gerado por IA.

---

## 10. Gestão de Conflitos

### 10.1 Prevenção (mais importante)

| Regra | Ação |
|-------|------|
| 1 módulo = 1 branch | Evita sobreposição de arquivos |
| Pull de develop antes de começar feature | `git pull origin develop` |
| Rebase frequente | `git rebase develop` a cada 2–3 dias |
| Sem edição paralela do mesmo arquivo | Se necessário, comunicar |
| .dyn como binário | Evita merge automático corrompido |

### 10.2 Resolução

| Tipo de conflito | Ação |
|-----------------|------|
| **C# — conflito simples** | Resolver manualmente, compilar, testar |
| **C# — conflito complexo** | Criar branch `fix/merge-{desc}`, resolver com calma |
| **.dyn — qualquer conflito** | Aceitar UMA versão (theirs ou ours), nunca merge manual |
| **JSON normativo** | Aceitar versão mais recente, verificar integridade |
| **AssemblyInfo** | Sempre aceitar versão de develop (ou release) |

### 10.3 Boas práticas

```
1. NUNCA forçar push em main ou develop
   git push --force → PROIBIDO em branches protegidas

2. SEMPRE rebase antes de PR
   git checkout feature/M01-deteccao
   git rebase develop
   (resolver conflitos se houver)
   git push origin feature/M01-deteccao --force-with-lease

3. Squash merge para develop
   Mantém histórico limpo: 1 commit = 1 feature

4. No-ff merge para main
   Mantém rastreabilidade: merge commit com referência à release
```

---

## 11. Segurança do Código

### 11.1 Controle de acesso

| Branch | Quem pode push direto | Quem pode merge via PR |
|--------|----------------------|----------------------|
| `main` | Ninguém | Somente após aprovação |
| `develop` | Ninguém | Após PR revisado |
| `feature/*` | Desenvolvedor + IA | — |
| `fix/*` | Desenvolvedor | — |
| `release/*` | Desenvolvedor | Merge para main via PR |

### 11.2 Estratégia de backup

| Camada | Mecanismo | Frequência |
|--------|-----------|-----------|
| **Git remoto** | Push para GitHub/GitLab/Azure DevOps | A cada commit (mínimo diário) |
| **Backup local** | Cópia da pasta `.git` para drive externo | Semanal |
| **Backup de config** | Cópia de `config/*.json` e `referencia_normativa.json` | A cada alteração |
| **Backup de modelos Revit** | Cópia dos .rvt de teste | Antes de cada teste significativo |

### 11.3 Proteção de branches

```
REGRAS (configurar no repositório remoto):

main:
  - Require pull request reviews: 1
  - Require status checks: build
  - No force push
  - No deletion

develop:
  - Require pull request reviews: 1
  - No force push
  - No deletion
```

### 11.4 .gitignore obrigatório

```gitignore
# Build
bin/
obj/
*.user
*.suo

# Revit
*.rvt
*.rfa
*.rte
*.rft
!tests/models/*.rvt

# Dynamo
*.dyn.bak
backup/

# IDE
.vs/
*.DotSettings.user

# OS
Thumbs.db
Desktop.ini
.DS_Store

# Logs (não versionar logs gerados)
logs/*.json
!logs/.gitkeep

# Secrets
*.pfx
*.snk
appsettings.local.json
```

### 11.5 Arquivos que DEVEM ser versionados

```
✅ Todo código C# (src/)
✅ Scripts Dynamo (dynamo/scripts/*.dyn)
✅ JSON normativo (docs/DefinicaoNormativa/referencia_normativa.json)
✅ JSON de configuração padrão (config/*.json)
✅ Documentação (docs/)
✅ Arquivo .addin
✅ .gitignore e .gitattributes
✅ CHANGELOG.md
✅ README.md
```

---

## 12. Estrutura do Repositório

```
PluginRevit/
│
├── .gitignore
├── .gitattributes
├── README.md
├── CHANGELOG.md
├── LICENSE
│
├── docs/
│   ├── DefinicaoNormativa/
│   │   ├── referencia_normativa.json
│   │   └── *.md
│   ├── Escopo&Requisitos/
│   │   └── *.md
│   └── EstrategiaDeDesenvolvimento/
│       └── *.md
│
├── src/
│   ├── PluginRevit.sln
│   ├── PluginRevit.Core/                 ← Lógica de negócio (sem Revit API)
│   │   ├── Models/
│   │   ├── Services/
│   │   ├── Rules/
│   │   └── Config/
│   ├── PluginRevit.Revit/                ← Camada Revit API
│   │   ├── Commands/
│   │   ├── UI/
│   │   ├── Adapters/
│   │   └── ExternalEvents/
│   └── PluginRevit.Tests/                ← Testes unitários
│       └── *.Tests.cs
│
├── dynamo/
│   ├── scripts/
│   │   └── *.dyn
│   ├── packages/
│   │   └── packages.json
│   └── CHANGELOG_DYNAMO.md
│
├── config/
│   ├── config_geral.json
│   ├── config_agua_fria.json
│   ├── config_esgoto.json
│   └── ...
│
├── tests/
│   ├── models/                            ← Modelos .rvt de teste
│   │   ├── Teste_01_Basico.rvt
│   │   └── Teste_02_Sobrado.rvt
│   └── data/
│       └── nomes_ambientes_corpus.json    ← Dataset para testes de classificação
│
└── build/
    ├── PluginRevit.addin
    └── install.bat                        ← Script de instalação local
```

---

## 13. CHANGELOG.md — Formato

```markdown
# Changelog

Todas as mudanças notáveis deste projeto serão documentadas aqui.
Formato baseado em [Keep a Changelog](https://keepachangelog.com/).

## [0.2.0-alpha] - 2026-04-15

### Adicionado
- M01: Detecção de ambientes (Rooms + Spaces)
- M02: Classificação de ambientes com NLP

### Corrigido
- Conversão de área ft² para m² com fator correto

### Alterado
- LogService: adicionar campo ElementId opcional

## [0.1.0-alpha] - 2026-04-01

### Adicionado
- M14: Sistema de logs (4 níveis, export JSON)
- M15: Interface WPF com 3 abas
- Estrutura do projeto e configuração
```
