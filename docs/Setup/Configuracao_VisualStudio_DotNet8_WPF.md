# Guia de Configuração — Visual Studio 2022 + .NET 8.0 + WPF

> Guia passo a passo para instalar e configurar o Visual Studio 2022, o SDK .NET 8.0 e preparar projetos WPF para o desenvolvimento de interfaces do plugin de automação hidráulica.

---

## 📋 Índice

- [Requisitos](#1-requisitos)
- [Instalar o Visual Studio 2022](#2-instalar-o-visual-studio-2022)
- [Instalar o .NET 8.0 SDK](#3-instalar-o-net-80-sdk)
- [Configurar o Visual Studio](#4-configurar-o-visual-studio)
- [Criar Projeto WPF](#5-criar-projeto-wpf)
- [Estrutura do Projeto WPF](#6-estrutura-do-projeto-wpf)
- [Configurar o .csproj](#7-configurar-o-csproj)
- [Integração com PluginCore](#8-integração-com-plugincore)
- [Design System e Estilos](#9-design-system-e-estilos)
- [Padrão MVVM](#10-padrão-mvvm)
- [Testes da Interface](#11-testes-da-interface)
- [Integração com Revit](#12-integração-com-revit)
- [Troubleshooting](#13-troubleshooting)

---

## 1. Requisitos

### Software necessário

| Software | Versão | Download |
|----------|--------|----------|
| **Windows** | 10 (21H2+) ou 11 — 64-bit | — |
| **Visual Studio** | 2022 v17.8+ | [visualstudio.microsoft.com](https://visualstudio.microsoft.com/) |
| **.NET SDK** | 8.0+ | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Git** | 2.40+ | [git-scm.com](https://git-scm.com/) |

### Hardware recomendado

| Recurso | Mínimo | Recomendado |
|---------|--------|-------------|
| **RAM** | 8 GB | 16 GB |
| **Disco** | 10 GB livres | 20 GB livres (SSD) |
| **Processador** | 4 cores, 1.8 GHz | 8 cores, 3.0 GHz+ |

### Licenciamento do Visual Studio

| Edição | Custo | Adequado para |
|--------|-------|--------------|
| **Community** | Gratuito | Desenvolvedores solo, open-source, estudantes |
| **Professional** | Pago | Equipes pequenas |
| **Enterprise** | Pago | Equipes grandes com DevOps integrado |

> 💡 Para este projeto, o **Visual Studio Community** é suficiente.

---

## 2. Instalar o Visual Studio 2022

### 2.1 Download

1. Acesse o site oficial:
   ```
   https://visualstudio.microsoft.com/vs/
   ```

2. Clique em **Download** da edição desejada (Community é gratuito).

3. O download do **Visual Studio Installer** inicia automaticamente (~1 MB).

### 2.2 Seleção de Workloads

Ao executar o instalador, a tela de workloads será exibida. Selecione:

#### Workloads obrigatórios

- [x] **.NET desktop development**
  - Inclui: WPF, Windows Forms, .NET SDK, NuGet
  - **Essencial** para desenvolvimento de interfaces WPF

#### Workloads recomendados

- [x] **Desktop development with C++**
  - Útil para: componentes nativos, build tools do Visual Studio
  - Opcional, mas recomendado para compatibilidade completa

#### Componentes individuais (aba "Individual components")

Verifique se estão marcados:

- [x] .NET 8.0 Runtime (Long Term Support)
- [x] .NET SDK (latest)
- [x] NuGet package manager
- [x] XAML Designer
- [x] .NET Profiling tools
- [x] IntelliCode
- [x] Live Share (opcional — para pair programming)

### 2.3 Configuração de instalação

| Opção | Valor recomendado |
|-------|------------------|
| **Diretório** | `C:\Program Files\Microsoft Visual Studio\2022\Community` (padrão) |
| **Download cache** | Manter padrão |
| **Shared components** | Manter padrão |

### 2.4 Instalação

1. Clique em **Install** (ou **Modify** se já instalado).
2. Aguarde o download e instalação (10-30 minutos, dependendo da conexão).
3. Após concluir, clique em **Launch**.

### 2.5 Primeira execução

1. Faça login com sua **conta Microsoft** (opcional, mas recomendado para sincronizar configurações).
2. Escolha o tema: **Dark** (recomendado para longas sessões de desenvolvimento).
3. Escolha o perfil: **Visual C#**.
4. Aguarde a indexação inicial.

> ✅ **Checkpoint**: Visual Studio 2022 instalado e aberto.

---

## 3. Instalar o .NET 8.0 SDK

### 3.1 Verificar se já está instalado

O .NET 8.0 SDK pode já ter sido instalado junto com o Visual Studio. Verifique:

```bash
dotnet --list-sdks
```

Saída esperada (exemplo):
```
8.0.400 [C:\Program Files\dotnet\sdk]
```

Se a versão 8.0.x aparecer, pule para o [passo 4](#4-configurar-o-visual-studio).

### 3.2 Download manual

Se não estiver instalado:

1. Acesse:
   ```
   https://dotnet.microsoft.com/en-us/download/dotnet/8.0
   ```

2. Na seção **SDK**, baixe o instalador para **Windows x64**.

3. Execute o instalador e siga os passos padrão.

### 3.3 Verificar instalação

```bash
# Verificar versão do SDK
dotnet --version
# → 8.0.400 (ou superior)

# Verificar todos os SDKs instalados
dotnet --list-sdks
# → 8.0.400 [C:\Program Files\dotnet\sdk]

# Verificar runtimes instalados
dotnet --list-runtimes
# → Microsoft.NETCore.App 8.0.x
# → Microsoft.WindowsDesktop.App 8.0.x  ← necessário para WPF
```

> ⚠️ **O runtime `Microsoft.WindowsDesktop.App` é obrigatório** para aplicações WPF. Ele é instalado automaticamente com o SDK, mas verifique na lista acima.

### 3.4 Verificar variáveis de ambiente

```bash
# O dotnet deve estar no PATH
where dotnet
# → C:\Program Files\dotnet\dotnet.exe
```

Se `where dotnet` não retornar resultado, adicione manualmente ao PATH:

```
C:\Program Files\dotnet\
```

> ✅ **Checkpoint**: .NET 8.0 SDK instalado e verificado.

---

## 4. Configurar o Visual Studio

### 4.1 Confirmar .NET 8.0 no VS

1. Abra o Visual Studio.
2. **Tools** → **Options** → **Environment** → **Preview Features**.
3. Certifique-se de que **"Use previews of the .NET SDK"** está **desmarcado** (usamos a versão estável).

### 4.2 Configurações recomendadas

#### Editor

**Tools** → **Options** → **Text Editor** → **C#**:

| Configuração | Valor |
|-------------|-------|
| **Tab size** | 4 |
| **Indent size** | 4 |
| **Insert spaces** | ✅ (usar espaços, não tabs) |
| **Word wrap** | ✅ |
| **Line numbers** | ✅ |

#### XAML Designer

**Tools** → **Options** → **XAML Designer**:

| Configuração | Valor |
|-------------|-------|
| **Enable XAML Designer** | ✅ |
| **Default view** | Split (design + code) |
| **Artboard Background** | Dark (combina com Dark theme) |

#### Build e Debug

**Tools** → **Options** → **Projects and Solutions** → **Build and Run**:

| Configuração | Valor |
|-------------|-------|
| **On Run, when projects are out of date** | Always build |
| **On Run, when build or deployment errors occur** | Do not launch |
| **MSBuild project build output verbosity** | Minimal |

#### NuGet

**Tools** → **Options** → **NuGet Package Manager**:

| Configuração | Valor |
|-------------|-------|
| **Default package management format** | PackageReference |
| **Automatically check for missing packages during build** | ✅ |

### 4.3 Extensões recomendadas

Instale via **Extensions** → **Manage Extensions**:

| Extensão | Finalidade |
|----------|-----------|
| **EditorConfig Language Service** | Padronização de formatação entre devs |
| **XAML Styler** | Formatação automática de XAML |
| **Output Enhancer** | Cores nos logs de output |
| **Markdown Editor v2** | Edição de documentação `.md` |
| **GitHub Extension** | Integração com GitHub |

### 4.4 Criar .editorconfig

Na raiz do projeto, crie um `.editorconfig` para padronização:

```ini
# EditorConfig — Hydraulic Plugin
root = true

[*]
charset = utf-8
end_of_line = crlf
indent_style = space
indent_size = 4
insert_final_newline = true
trim_trailing_whitespace = true

[*.cs]
dotnet_sort_system_directives_first = true
dotnet_style_qualification_for_field = false:suggestion
dotnet_style_qualification_for_property = false:suggestion
dotnet_style_qualification_for_method = false:suggestion
csharp_new_line_before_open_brace = all
csharp_style_var_for_built_in_types = false:suggestion
csharp_style_expression_bodied_methods = when_on_single_line:suggestion

[*.xaml]
indent_size = 4

[*.json]
indent_size = 2

[*.md]
trim_trailing_whitespace = false
```

> ✅ **Checkpoint**: Visual Studio configurado para o projeto.

---

## 5. Criar Projeto WPF

### 5.1 Via Visual Studio

1. Abra o Visual Studio.
2. **File** → **New** → **Project**.
3. Busque por **"WPF App"**.
4. Selecione **WPF Application** (certifique-se de que é C# e .NET, não .NET Framework).
5. Configure:

   | Campo | Valor |
   |-------|-------|
   | **Project name** | `HydraulicUI` |
   | **Location** | `C:\Users\<user>\Desktop\PluginRevit\` |
   | **Solution** | Add to existing solution (`PluginRevit.sln`) |
   | **Framework** | .NET 8.0 (Long Term Support) |

6. Clique em **Create**.

### 5.2 Via CLI (alternativa)

```bash
cd C:\Users\User\Desktop\PluginRevit

# Criar projeto WPF
dotnet new wpf -n HydraulicUI --framework net8.0-windows

# Adicionar à solution existente
dotnet sln PluginRevit.sln add HydraulicUI/HydraulicUI.csproj
```

### 5.3 Verificar build

```bash
dotnet build HydraulicUI/HydraulicUI.csproj
```

Deve compilar sem erros. Execute para confirmar:

```bash
dotnet run --project HydraulicUI
```

Uma janela WPF vazia deve abrir.

> ✅ **Checkpoint**: Projeto WPF criado e compilando.

---

## 6. Estrutura do Projeto WPF

### 6.1 Organização de pastas recomendada

```
HydraulicUI/
│
├── HydraulicUI.csproj         # Arquivo de projeto
├── App.xaml                   # Configuração global (estilos, recursos)
├── App.xaml.cs                # Startup da aplicação
│
├── Views/                     # Interfaces XAML
│   ├── MainWindow.xaml        # Janela principal
│   ├── PipelineView.xaml      # Monitoramento do pipeline
│   ├── StepApprovalView.xaml  # Aprovação de etapas
│   ├── ErrorListView.xaml     # Lista de erros
│   └── SettingsView.xaml      # Configurações
│
├── ViewModels/                # View Models (MVVM)
│   ├── MainViewModel.cs
│   ├── PipelineViewModel.cs
│   ├── StepApprovalViewModel.cs
│   └── ErrorListViewModel.cs
│
├── Models/                    # DTOs específicos da UI
│   ├── StepDisplayModel.cs
│   └── ErrorDisplayModel.cs
│
├── Controls/                  # Controles customizados
│   ├── ProgressStepControl.xaml
│   └── ErrorBadgeControl.xaml
│
├── Resources/                 # Recursos visuais
│   ├── Styles/
│   │   ├── Colors.xaml        # Paleta de cores
│   │   ├── Buttons.xaml       # Estilos de botões
│   │   └── Typography.xaml    # Fontes e tamanhos
│   ├── Icons/                 # Ícones SVG/PNG
│   └── Images/                # Logos e imagens
│
├── Converters/                # Value Converters WPF
│   ├── StatusToColorConverter.cs
│   ├── BoolToVisibilityConverter.cs
│   └── PercentToWidthConverter.cs
│
└── Services/                  # Serviços da UI
    ├── NavigationService.cs
    └── DialogService.cs
```

### 6.2 Criar pastas

```bash
cd HydraulicUI
mkdir Views ViewModels Models Controls Resources Resources\Styles Resources\Icons Converters Services
```

> ✅ **Checkpoint**: Estrutura de pastas organizada.

---

## 7. Configurar o .csproj

### 7.1 Arquivo `HydraulicUI.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>
    <PlatformTarget>x64</PlatformTarget>
    <RootNamespace>HidraulicoPlugin.UI</RootNamespace>
    <AssemblyName>HydraulicUI</AssemblyName>

    <!-- Informações do assembly -->
    <Authors>Murillo Santtos</Authors>
    <Product>Hydraulic Automation Plugin</Product>
    <Description>Interface WPF do plugin de automação hidráulica</Description>
    <Version>0.1.0</Version>
  </PropertyGroup>

  <!-- Referência ao PluginCore -->
  <ItemGroup>
    <ProjectReference Include="..\PluginCore\PluginCore.csproj" />
  </ItemGroup>

  <!-- Pacotes NuGet recomendados -->
  <ItemGroup>
    <!-- MVVM toolkit da Microsoft -->
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.2.2" />

    <!-- Dependency Injection -->
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />

    <!-- JSON para comunicação -->
    <PackageReference Include="System.Text.Json" Version="8.0.4" />
  </ItemGroup>

</Project>
```

### 7.2 Pacotes NuGet explicados

| Pacote | Versão | Finalidade |
|--------|--------|-----------|
| **CommunityToolkit.Mvvm** | 8.2.2 | Implementação leve de MVVM (ObservableObject, RelayCommand, etc.) |
| **Microsoft.Extensions.DependencyInjection** | 8.0.0 | IoC container para injeção de dependências |
| **System.Text.Json** | 8.0.4 | Serialização JSON para comunicação com PluginCore |

### 7.3 Instalar pacotes

```bash
cd HydraulicUI
dotnet add package CommunityToolkit.Mvvm --version 8.2.2
dotnet add package Microsoft.Extensions.DependencyInjection --version 8.0.0
dotnet add package System.Text.Json --version 8.0.4
```

> ✅ **Checkpoint**: `.csproj` configurado com dependências.

---

## 8. Integração com PluginCore

### 8.1 Referência de projeto

A UI se comunica com o PluginCore via referência de projeto direta:

```
PluginRevit.sln
├── PluginCore       ← domínio (sem Revit, sem UI)
├── HydraulicUI      ← UI WPF (referencia PluginCore)
└── Revit2026        ← integração Revit (referencia PluginCore)
```

### 8.2 Fluxo de comunicação

```
┌──────────────────┐      ┌──────────────┐      ┌──────────────┐
│   HydraulicUI    │      │  PluginCore   │      │   Revit2026  │
│                  │      │              │      │              │
│  PipelineView ───┼──→   │ PipelineRunner│  ←───┼── Revit API  │
│  ApprovalView ───┼──→   │ EventBus     │  ←───┼── Dynamo     │
│  ErrorListView ──┼──→   │ ErrorCollector│      │              │
│                  │      │              │      │              │
│  ← ProgressObs ──┼──←   │ Observers    │      │              │
│  ← ErrorEvents ──┼──←   │ Events       │      │              │
└──────────────────┘      └──────────────┘      └──────────────┘
```

### 8.3 Exemplo: ViewModel consumindo PluginCore

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HidraulicoPlugin.Core.Pipeline;
using HidraulicoPlugin.Core.Events;
using HidraulicoPlugin.Core.Events.Observers;

namespace HidraulicoPlugin.UI.ViewModels
{
    public partial class PipelineViewModel : ObservableObject
    {
        private readonly PipelineRunner _runner;
        private readonly ProgressObserver _progressObserver;

        [ObservableProperty]
        private string _currentStepName = "Não iniciado";

        [ObservableProperty]
        private double _overallProgress;

        [ObservableProperty]
        private string _statusLine = "⚪ Aguardando início";

        [ObservableProperty]
        private bool _isWaitingApproval;

        public PipelineViewModel(PipelineRunner runner,
            ProgressObserver progressObserver)
        {
            _runner = runner;
            _progressObserver = progressObserver;

            // Bind observer → ViewModel
            _progressObserver.OnProgressChanged = (obs) =>
            {
                // Atualizar na UI thread
                App.Current.Dispatcher.Invoke(() =>
                {
                    CurrentStepName = obs.CurrentStepName ?? "—";
                    OverallProgress = obs.OverallProgress * 100;
                    StatusLine = obs.GetStatusLine();
                    IsWaitingApproval = obs.IsWaitingApproval;
                });
            };
        }

        [RelayCommand]
        private void ExecuteNext()
        {
            Task.Run(() => _runner.ExecuteUntilApproval());
        }

        [RelayCommand]
        private void Approve()
        {
            var stepId = _progressObserver.CurrentStepId;
            if (stepId != null)
                _runner.Approve(stepId, "Aprovado via UI");
        }

        [RelayCommand]
        private void Reject()
        {
            var stepId = _progressObserver.CurrentStepId;
            if (stepId != null)
                _runner.Reject(stepId, "Rejeitado via UI");
        }
    }
}
```

> ✅ **Checkpoint**: UI integrada com PluginCore.

---

## 9. Design System e Estilos

### 9.1 Paleta de cores (`Resources/Styles/Colors.xaml`)

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <!-- Background -->
    <Color x:Key="BgPrimary">#1A1B2E</Color>
    <Color x:Key="BgSecondary">#232440</Color>
    <Color x:Key="BgCard">#2A2B4A</Color>
    <Color x:Key="BgHover">#33345A</Color>

    <!-- Text -->
    <Color x:Key="TextPrimary">#E8E8F0</Color>
    <Color x:Key="TextSecondary">#9999B8</Color>
    <Color x:Key="TextMuted">#6666880</Color>

    <!-- Accent -->
    <Color x:Key="AccentBlue">#4A7CFF</Color>
    <Color x:Key="AccentGreen">#34D399</Color>
    <Color x:Key="AccentYellow">#FBBF24</Color>
    <Color x:Key="AccentRed">#F87171</Color>
    <Color x:Key="AccentPurple">#A78BFA</Color>

    <!-- Status -->
    <Color x:Key="StatusPending">#6B7280</Color>
    <Color x:Key="StatusRunning">#4A7CFF</Color>
    <Color x:Key="StatusCompleted">#34D399</Color>
    <Color x:Key="StatusFailed">#F87171</Color>
    <Color x:Key="StatusWaiting">#FBBF24</Color>

    <!-- Brushes -->
    <SolidColorBrush x:Key="BgPrimaryBrush" Color="{StaticResource BgPrimary}" />
    <SolidColorBrush x:Key="BgSecondaryBrush" Color="{StaticResource BgSecondary}" />
    <SolidColorBrush x:Key="BgCardBrush" Color="{StaticResource BgCard}" />
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource TextPrimary}" />
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource TextSecondary}" />
    <SolidColorBrush x:Key="AccentBlueBrush" Color="{StaticResource AccentBlue}" />
    <SolidColorBrush x:Key="AccentGreenBrush" Color="{StaticResource AccentGreen}" />
    <SolidColorBrush x:Key="AccentRedBrush" Color="{StaticResource AccentRed}" />

</ResourceDictionary>
```

### 9.2 Registrar estilos no `App.xaml`

```xml
<Application x:Class="HidraulicoPlugin.UI.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             StartupUri="Views/MainWindow.xaml">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Buttons.xaml" />
                <ResourceDictionary Source="Resources/Styles/Typography.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

> ✅ **Checkpoint**: Design system configurado.

---

## 10. Padrão MVVM

### 10.1 Por que MVVM

| Benefício | Detalhe |
|-----------|---------|
| **Separação** | View (XAML) não conhece lógica de domínio |
| **Testabilidade** | ViewModels podem ser testados sem UI |
| **Reatividade** | Binding bidirecional atualiza UI automaticamente |
| **Compatibilidade** | Padrão nativo do WPF |

### 10.2 Estrutura com CommunityToolkit.Mvvm

```
View (XAML) ←─ DataBinding ─→ ViewModel ←─ Serviços ─→ PluginCore

MainWindow.xaml
  DataContext = MainViewModel
    ↕ Binding
  PipelineView.xaml
    DataContext = PipelineViewModel
      ↕ Binding
    StepApprovalView.xaml
      DataContext = StepApprovalViewModel
```

### 10.3 Base ViewModel (com CommunityToolkit.Mvvm)

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace HidraulicoPlugin.UI.ViewModels
{
    /// <summary>
    /// ViewModel base com suporte a propriedades observáveis.
    /// O CommunityToolkit gera automaticamente:
    /// - INotifyPropertyChanged
    /// - Propriedades públicas a partir de [ObservableProperty]
    /// - Commands a partir de [RelayCommand]
    /// </summary>
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);
    }
}
```

### 10.4 Configurar DI no App.xaml.cs

```csharp
using Microsoft.Extensions.DependencyInjection;
using HidraulicoPlugin.Core.Pipeline;
using HidraulicoPlugin.Core.Events;
using HidraulicoPlugin.Core.Events.Observers;

namespace HidraulicoPlugin.UI
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var services = new ServiceCollection();

            // Core
            services.AddSingleton<EventBus>();
            services.AddSingleton<ProgressObserver>();
            services.AddSingleton<LogObserver>();
            services.AddSingleton<DebugObserver>();

            // ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<PipelineViewModel>();
            services.AddTransient<StepApprovalViewModel>();
            services.AddTransient<ErrorListViewModel>();

            Services = services.BuildServiceProvider();

            // Registrar observers no EventBus
            var eventBus = Services.GetRequiredService<EventBus>();
            eventBus.Subscribe(Services.GetRequiredService<ProgressObserver>());
            eventBus.Subscribe(Services.GetRequiredService<LogObserver>());
            eventBus.Subscribe(Services.GetRequiredService<DebugObserver>());
        }
    }
}
```

> ✅ **Checkpoint**: Padrão MVVM configurado com DI.

---

## 11. Testes da Interface

### 11.1 Teste de execução isolada

A UI pode ser executada independentemente do Revit para desenvolvimento e testes:

```bash
# Executar a UI standalone
dotnet run --project HydraulicUI
```

### 11.2 Teste com dados mock

Crie um serviço mock para simular o pipeline sem Revit:

```csharp
public class MockPipelineService
{
    public PipelineContext CreateMockContext()
    {
        return new PipelineContext
        {
            RawRooms = new List<RoomRawData>
            {
                new() { Id = "room_01", Name = "Banheiro Social", Area = 4.5 },
                new() { Id = "room_02", Name = "Cozinha", Area = 8.0 },
                new() { Id = "room_03", Name = "Área de Serviço", Area = 3.5 },
            }
        };
    }
}
```

### 11.3 Verificação visual

| Item | Verificar |
|------|----------|
| **Janela principal** | Abre sem erros |
| **Tema dark** | Cores consistentes com o design system |
| **Pipeline view** | Mostra etapas com status |
| **Bindings** | Dados atualizam quando ViewModel muda |
| **Botões** | Approve/Reject funcionam |
| **Erros** | Lista de erros renderiza corretamente |
| **Responsividade** | Redimensionar janela funciona |

> ✅ **Checkpoint**: UI funcional e testável isoladamente.

---

## 12. Integração com Revit

### 12.1 Chamada da UI pelo plugin Revit

O plugin Revit abre a janela WPF como um diálogo modal:

```csharp
// No ExternalCommand do Revit
public Result Execute(ExternalCommandData commandData,
    ref string message, ElementSet elements)
{
    // Criar janela WPF
    var mainWindow = new MainWindow();

    // Configurar como filha do Revit (foco correto)
    var revitHandle = commandData.Application.MainWindowHandle;
    var hwndHelper = new System.Windows.Interop.WindowInteropHelper(mainWindow)
    {
        Owner = revitHandle
    };

    // Exibir como modal
    mainWindow.ShowDialog();

    return Result.Succeeded;
}
```

### 12.2 Comunicação bidirecional

```
Revit2026 (ExternalCommand)
    │
    ├── Cria PipelineContext com dados do Revit
    ├── Cria PipelineRunner
    ├── Passa runner para MainWindow
    │
    ▼
HydraulicUI (MainWindow)
    │
    ├── Exibe pipeline para o usuário
    ├── Usuário clica "Executar" → ViewModel.ExecuteNext()
    ├── ProgressObserver atualiza UI em tempo real
    ├── Usuário aprova/rejeita etapas
    │
    ▼
PluginCore (PipelineRunner)
    │
    ├── Executa etapas
    ├── Emite eventos via EventBus
    └── Retorna resultados
```

### 12.3 Solution final

```
PluginRevit.sln
│
├── PluginCore/           ← Domínio (zero dependências externas)
│   ├── Models/
│   ├── Interfaces/
│   ├── Pipeline/
│   ├── Strategies/
│   ├── Events/
│   └── ErrorHandling/
│
├── HydraulicUI/          ← WPF (.NET 8.0, referencia PluginCore)
│   ├── Views/
│   ├── ViewModels/
│   ├── Resources/
│   └── Converters/
│
├── Revit2026/            ← Integração (.NET 8.0, referencia PluginCore + Revit API)
│   ├── Commands/
│   └── Services/
│
└── Tests/                ← Testes unitários
    ├── PluginCore.Tests/
    └── HydraulicUI.Tests/
```

> ✅ **Checkpoint**: Ambiente completo para desenvolvimento do plugin.

---

## 13. Troubleshooting

### Erro: "WPF is not supported on this platform"

**Causa**: Target framework não inclui `-windows`.

**Solução**: Altere o `.csproj`:
```xml
<TargetFramework>net8.0-windows</TargetFramework>
```

---

### Erro: "The XAML Designer encountered an error"

**Causa**: Referência a tipos ou recursos que só existem em runtime.

**Solução**:
1. Build o projeto (`Ctrl+Shift+B`)
2. Feche e reabra o arquivo XAML
3. Se persistir: **Tools** → **Options** → **XAML Designer** → desmarque e remarque **Enable XAML Designer**

---

### Erro: "Could not find type 'MainViewModel'"

**Causa**: O ViewModel está em namespace diferente ou o XAML não referencia o namespace correto.

**Solução**: Verifique o `xmlns:vm` no XAML:
```xml
<Window xmlns:vm="clr-namespace:HidraulicoPlugin.UI.ViewModels">
    <Window.DataContext>
        <vm:MainViewModel />
    </Window.DataContext>
</Window>
```

---

### Erro: erro ao acessar UI thread a partir do Observer

**Causa**: Evento do pipeline é disparado em thread de background.

**Solução**: Use `Dispatcher.Invoke()`:
```csharp
App.Current.Dispatcher.Invoke(() =>
{
    CurrentStepName = obs.CurrentStepName;
});
```

---

### Build lento após adicionar muitos XAML

**Causa**: O XAML Designer recompila a cada mudança.

**Solução**: Para builds mais rápidos durante desenvolvimento:
```xml
<PropertyGroup>
  <!-- Desabilitar designer temporariamente -->
  <DisableXbfGeneration>true</DisableXbfGeneration>
</PropertyGroup>
```

---

> **Ambiente configurado com sucesso!** O Visual Studio está pronto para desenvolvimento de interfaces WPF integradas ao plugin de automação hidráulica.
