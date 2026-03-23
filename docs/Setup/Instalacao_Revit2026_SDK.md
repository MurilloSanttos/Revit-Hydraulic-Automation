# Guia de Instalação — Revit 2026 + SDK para Desenvolvimento de Plugins

> Guia passo a passo para configurar o ambiente de desenvolvimento completo: Revit 2026, SDK, Visual Studio e primeiro plugin de teste.

---

## 📋 Índice

- [Requisitos do Sistema](#1-requisitos-do-sistema)
- [Instalar o Revit 2026](#2-instalar-o-revit-2026)
- [Instalar o Revit 2026 SDK](#3-instalar-o-revit-2026-sdk)
- [Configurar o Visual Studio](#4-configurar-o-visual-studio)
- [Configurar Referências do Projeto](#5-configurar-referências-do-projeto)
- [Criar o Arquivo .addin](#6-criar-o-arquivo-addin)
- [Testar o Ambiente](#7-testar-o-ambiente-hello-world)
- [Configurar Debug no Revit](#8-configurar-debug-no-revit)
- [Instalar o Dynamo SDK](#9-instalar-o-dynamo-sdk-opcional)
- [Estrutura Final](#10-estrutura-final-do-ambiente)
- [Troubleshooting](#11-troubleshooting)

---

## 1. Requisitos do Sistema

### Hardware mínimo

| Recurso | Mínimo | Recomendado |
|---------|--------|-------------|
| **Processador** | Intel i5 / AMD Ryzen 5 (4+ cores) | Intel i7 / AMD Ryzen 7 (8+ cores) |
| **Memória RAM** | 8 GB | 16 GB ou superior |
| **Disco** | 30 GB livres (SSD recomendado) | 50 GB livres em SSD NVMe |
| **GPU** | DirectX 11, 2 GB VRAM | DirectX 12, 4+ GB VRAM |
| **Resolução** | 1920 × 1080 | 2560 × 1440 ou superior |

### Software

| Software | Versão |
|----------|--------|
| **Sistema Operacional** | Windows 10 (64-bit) versão 21H2+ ou Windows 11 |
| **Visual Studio** | 2022 (v17.8+) com workload ".NET Desktop Development" |
| **.NET SDK** | 8.0+ |
| **Git** | 2.40+ |

### Conta Autodesk

- Conta Autodesk ativa com licença do Revit 2026 (comercial, educacional ou trial)
- Acesso ao [Autodesk Developer Network](https://aps.autodesk.com/) (gratuito)

---

## 2. Instalar o Revit 2026

### 2.1 Download

1. Acesse o site oficial da Autodesk:
   ```
   https://www.autodesk.com/products/revit
   ```

2. Faça login com sua conta Autodesk.

3. Em **Products** → **Revit**, selecione a versão **2026**.

4. Clique em **Download** (Trial de 30 dias ou licença completa).

5. O instalador será baixado como um executável (aproximadamente 15 GB).

> **Nota**: Se você possui licença educacional, acesse via [Autodesk Education](https://www.autodesk.com/education/free-software/revit).

### 2.2 Instalação

1. **Execute o instalador** como Administrador:
   - Clique com o botão direito → **Executar como administrador**

2. **Aceite os termos** de licença.

3. **Configure o diretório** de instalação:
   ```
   C:\Program Files\Autodesk\Revit 2026
   ```
   > ⚠️ **Não altere o diretório padrão** — outros componentes (Dynamo, SDK) dependem deste caminho.

4. **Componentes**: Mantenha todos os componentes marcados, em especial:
   - [x] Revit 2026
   - [x] Dynamo for Revit
   - [x] Material Library
   - [x] Content Libraries (templates PT-BR)

5. Clique em **Install** e aguarde (15-30 minutos).

### 2.3 Primeira execução

1. Abra o **Revit 2026** pelo menu Iniciar.
2. Faça login com sua conta Autodesk quando solicitado.
3. Na tela inicial, crie um novo projeto:
   - **Template**: Architectural (ou qualquer template disponível)
4. Verifique se o Revit abre normalmente e o Dynamo está acessível:
   - Aba **Manage** → **Visual Programming** → **Dynamo**
5. Feche o Revit.

> ✅ **Checkpoint**: Revit 2026 instalado e funcional.

---

## 3. Instalar o Revit 2026 SDK

### 3.1 Download

1. Acesse o Autodesk Platform Services (antigo Forge):
   ```
   https://aps.autodesk.com/developer/overview/revit
   ```

2. Localize a seção **Revit SDK** para a versão **2026**.

3. Alternativa — acesso direto via Autodesk Developer Network:
   ```
   https://www.autodesk.com/developer-network/platform-technologies/revit
   ```

4. Baixe o arquivo `REVIT_2026_SDK.msi` (ou `.exe`).

> **Nota**: O SDK também pode estar disponível dentro do instalador do Revit em "Additional Tools" ou no disco de instalação.

### 3.2 Instalação do SDK

1. **Execute o instalador** do SDK como Administrador.

2. **Configure o diretório** de instalação:
   ```
   C:\RevitSDK\2026
   ```
   > 💡 Use um caminho curto e sem espaços para facilitar referências em scripts.

3. Conclua a instalação.

### 3.3 Conteúdo do SDK

Após a instalação, a pasta terá esta estrutura:

```
C:\RevitSDK\2026\
├── Revit 2026 SDK Readme.htm     # Documentação inicial
├── REX SDK Readme.htm            # Extensões REX
│
├── Samples/                      # Exemplos de plugins
│   ├── HelloWorld/               # Plugin mínimo
│   ├── ExternalCommand/          # Comando externo
│   ├── Events/                   # Manipulação de eventos
│   └── ... (200+ exemplos)
│
├── Add-In Manager/               # Gerenciador de add-ins
│
├── Macros/                       # Exemplos de macros
│
├── RevitLookup/                  # Ferramenta de inspeção do modelo
│
└── Documentation/
    └── RevitAPI.chm              # Documentação completa da API
```

### 3.4 Instalar o RevitLookup (Recomendado)

O **RevitLookup** é uma ferramenta essencial para inspecionar elementos do modelo Revit durante o desenvolvimento:

1. Abra a pasta `C:\RevitSDK\2026\RevitLookup`
2. Abra a solution `RevitLookup.sln` no Visual Studio
3. Configure o **Target Framework** para corresponder ao Revit 2026
4. Build o projeto
5. Copie o `.dll` resultante para a pasta de addins (passo detalhado na seção 6)
6. Reinicie o Revit — o RevitLookup aparecerá na aba **Add-Ins**

> ✅ **Checkpoint**: SDK instalado com exemplos e documentação acessíveis.

---

## 4. Configurar o Visual Studio

### 4.1 Instalar workloads necessários

1. Abra o **Visual Studio Installer**.

2. Clique em **Modify** na instalação do Visual Studio 2022.

3. Marque os seguintes workloads:

   - [x] **.NET desktop development**
   - [x] **Desktop development with C++** (opcional, para componentes nativos)

4. Na aba **Individual components**, verifique:

   - [x] .NET 8.0 Runtime
   - [x] .NET SDK
   - [x] NuGet package manager

5. Clique em **Modify** e aguarde.

### 4.2 Extensões recomendadas

| Extensão | Finalidade |
|----------|-----------|
| **GitHub Extension** | Integração com repositório |
| **Markdown Editor** | Edição de documentação |
| **EditorConfig** | Padronização de código |

### 4.3 Configuração do Git

```bash
# Configurar identidade
git config --global user.name "Seu Nome"
git config --global user.email "seu@email.com"

# Configurar line endings para Windows
git config --global core.autocrlf true
```

> ✅ **Checkpoint**: Visual Studio configurado para desenvolvimento .NET.

---

## 5. Configurar Referências do Projeto

### 5.1 Criar o projeto

Se for contribuir com o plugin existente, clone o repositório:

```bash
git clone https://github.com/MurilloSanttos/Revit-Hydraulic-Automation.git
cd Revit-Hydraulic-Automation
```

Se for criar um projeto do zero:

1. Visual Studio → **Create a new project**
2. Template: **Class Library** (C#)
3. Framework: **.NET 8.0**
4. Nome: `Revit2026` (ou o nome do seu plugin)

### 5.2 Adicionar referências da API do Revit

As DLLs da API do Revit estão no diretório de instalação:

```
C:\Program Files\Autodesk\Revit 2026\
```

**DLLs obrigatórias**:

| DLL | Função |
|-----|--------|
| `RevitAPI.dll` | API principal (elementos, parâmetros, transações) |
| `RevitAPIUI.dll` | API de interface (ribbon, diálogos, seleção) |

**DLLs opcionais**:

| DLL | Função |
|-----|--------|
| `RevitAPIIFC.dll` | Exportação/importação IFC |
| `RevitAPIMacros.dll` | Suporte a macros |

### 5.3 Adicionar via `.csproj`

Edite o arquivo `.csproj` do projeto de integração:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <PlatformTarget>x64</PlatformTarget>
  </PropertyGroup>

  <!-- Referências da API do Revit -->
  <ItemGroup>
    <Reference Include="RevitAPI">
      <HintPath>C:\Program Files\Autodesk\Revit 2026\RevitAPI.dll</HintPath>
      <Private>false</Private>
    </Reference>
    <Reference Include="RevitAPIUI">
      <HintPath>C:\Program Files\Autodesk\Revit 2026\RevitAPIUI.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>

</Project>
```

> ⚠️ **`Private` deve ser `false`** — o Revit já carrega essas DLLs. Copiar para o output causaria conflito.

### 5.4 Verificar build

```bash
dotnet build
```

Deve compilar sem erros. Se houver erros de referência, verifique os caminhos das DLLs.

> ✅ **Checkpoint**: Projeto compila com referências da API do Revit.

---

## 6. Criar o Arquivo .addin

O Revit descobre plugins via arquivos `.addin` no diretório de addins.

### 6.1 Diretório de addins

```
# Para todos os usuários:
C:\ProgramData\Autodesk\Revit\Addins\2026\

# Para o usuário atual:
%APPDATA%\Autodesk\Revit\Addins\2026\
```

### 6.2 Criar o arquivo `.addin`

Crie um arquivo XML com extensão `.addin`:

**`HidraulicaRevit.addin`**:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RevitAddIns>
  <AddIn Type="Command">
    <Name>Hydraulic Automation</Name>
    <FullClassName>HidraulicoPlugin.Revit2026.Commands.DetectarAmbientesCommand</FullClassName>
    <Assembly>C:\caminho\para\output\Revit2026.dll</Assembly>
    <AddInId>A1B2C3D4-E5F6-7890-ABCD-EF1234567890</AddInId>
    <VendorId>MurilloSanttos</VendorId>
    <VendorDescription>Hydraulic Automation Plugin</VendorDescription>
  </AddIn>
</RevitAddIns>
```

> 💡 **Gere um GUID único** para o `AddInId`. No PowerShell: `[guid]::NewGuid()`

### 6.3 Configurar output automático

Adicione ao `.csproj` um post-build event para copiar automaticamente:

```xml
<Target Name="CopyAddinToRevit" AfterTargets="Build">
  <Copy
    SourceFiles="$(ProjectDir)HidraulicaRevit.addin"
    DestinationFolder="$(AppData)\Autodesk\Revit\Addins\2026\"
    SkipUnchangedFiles="true" />
  <Copy
    SourceFiles="$(TargetPath)"
    DestinationFolder="$(AppData)\Autodesk\Revit\Addins\2026\"
    SkipUnchangedFiles="true" />
</Target>
```

> ✅ **Checkpoint**: Arquivo .addin configurado.

---

## 7. Testar o Ambiente (Hello World)

### 7.1 Código do plugin de teste

```csharp
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HidraulicoPlugin.Revit2026.Commands
{
    [Transaction(TransactionMode.Manual)]
    public class HelloWorldCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            TaskDialog dialog = new TaskDialog("Hydraulic Automation");
            dialog.MainInstruction = "Plugin carregado com sucesso!";
            dialog.MainContent =
                "O ambiente de desenvolvimento está configurado.\n\n" +
                $"Revit Version: {commandData.Application.Application.VersionNumber}\n" +
                $"Username: {commandData.Application.Application.Username}";
            dialog.Show();

            return Result.Succeeded;
        }
    }
}
```

### 7.2 Compilar e testar

1. Build o projeto (`Ctrl+Shift+B` ou `dotnet build`)
2. Abra o Revit 2026
3. Se aparecer uma mensagem de segurança sobre o add-in, clique em **Always Load**
4. Vá em **Add-Ins** → **External Tools** → **Hello World**
5. Deve aparecer um diálogo com a mensagem de sucesso

### 7.3 Verificação

Se o diálogo apareceu:

```
✅ Revit 2026 instalado
✅ SDK configurado
✅ Visual Studio configurado
✅ Referências da API corretas
✅ Arquivo .addin funcional
✅ Plugin carregando no Revit
```

> ✅ **Checkpoint**: Ambiente de desenvolvimento 100% funcional.

---

## 8. Configurar Debug no Revit

### 8.1 Configurar o Visual Studio para attach no Revit

1. No Visual Studio, vá em **Debug** → **Properties** do projeto.

2. Na aba **Debug** → **Launch Profiles**:
   ```
   Start external program: C:\Program Files\Autodesk\Revit 2026\Revit.exe
   ```

3. Agora, ao pressionar `F5`, o Visual Studio:
   - Compila o projeto
   - Inicia o Revit
   - Conecta o debugger automaticamente

### 8.2 Breakpoints

1. Coloque breakpoints no código do seu `IExternalCommand.Execute()`
2. Inicie o debug (`F5`)
3. No Revit, execute o comando do plugin
4. O Visual Studio vai pausar no breakpoint

### 8.3 Hot Reload (Limitado)

O Revit carrega DLLs em memória e não permite hot reload completo. Para testar alterações:

1. Feche o Revit
2. Recompile
3. Abra o Revit novamente

> 💡 **Dica**: Use o **Add-In Manager** (do SDK) para carregar/descarregar plugins sem reiniciar o Revit durante o desenvolvimento.

---

## 9. Instalar o Dynamo SDK (Opcional)

Se o plugin vai integrar com Dynamo (como é o caso do Hydraulic Automation):

### 9.1 Localizar o Dynamo

O Dynamo é instalado junto com o Revit em:

```
C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\
```

### 9.2 DLLs do Dynamo

Para desenvolvimento de nós customizados ou integração, adicione referências a:

| DLL | Função |
|-----|--------|
| `DynamoCore.dll` | Core do Dynamo |
| `DynamoServices.dll` | Serviços e atributos |
| `ProtoGeometry.dll` | Geometria do Dynamo |

### 9.3 Referências no `.csproj`

```xml
<ItemGroup>
  <Reference Include="DynamoCore">
    <HintPath>C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoCore.dll</HintPath>
    <Private>false</Private>
  </Reference>
  <Reference Include="DynamoServices">
    <HintPath>C:\Program Files\Autodesk\Revit 2026\AddIns\DynamoForRevit\DynamoServices.dll</HintPath>
    <Private>false</Private>
  </Reference>
</ItemGroup>
```

---

## 10. Estrutura Final do Ambiente

Após completar todos os passos, seu ambiente deve ter:

```
Softwares instalados:
├── Autodesk Revit 2026        → C:\Program Files\Autodesk\Revit 2026\
├── Dynamo for Revit           → (incluso no Revit)
├── Revit SDK 2026             → C:\RevitSDK\2026\
├── Visual Studio 2022         → com .NET workload
├── .NET 8.0 SDK               → via Visual Studio
└── Git                        → configurado

Repositório clonado:
├── Revit-Hydraulic-Automation → C:\Users\<user>\Desktop\PluginRevit\
│   ├── PluginCore/            → domínio (sem referências Revit)
│   ├── Revit2026/             → integração (com referências Revit)
│   ├── DynamoScripts/         → scripts .dyn
│   └── docs/                  → documentação

Addin registrado:
└── %APPDATA%\Autodesk\Revit\Addins\2026\
    ├── HidraulicaRevit.addin  → manifesto
    └── Revit2026.dll          → plugin compilado
```

---

## 11. Troubleshooting

### Erro: "Could not load file or assembly 'RevitAPI'"

**Causa**: Target framework incompatível ou referência com caminho errado.

**Solução**:
1. Verifique se o caminho da DLL no `.csproj` está correto
2. Confirme que o `TargetFramework` é `net8.0-windows`
3. Confirme `PlatformTarget` é `x64`

---

### Erro: Plugin não aparece no Revit

**Causa**: Arquivo `.addin` ausente ou com erro.

**Solução**:
1. Verifique se o `.addin` está em `%APPDATA%\Autodesk\Revit\Addins\2026\`
2. Valide o XML do `.addin` (abra em um editor)
3. Confirme que o `FullClassName` corresponde exatamente à classe no código
4. Confirme que o caminho do `Assembly` aponta para o `.dll` correto

---

### Erro: "This add-in could not be loaded" na inicialização

**Causa**: DLL compilada para framework ou plataforma incompatível.

**Solução**:
1. Certifique-se de que está compilando para `x64`
2. Verifique se `Private` é `false` para as referências do Revit
3. Limpe e recompile: `dotnet clean && dotnet build`

---

### Erro: Revit trava ao carregar plugin

**Causa**: Exceção não tratada no código de inicialização.

**Solução**:
1. Adicione try/catch ao `Execute()` do comando
2. Use `TaskDialog.Show()` para exibir a exceção
3. Verifique os logs do Revit em:
   ```
   %LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit 2026\Journals\
   ```

---

### Dynamo não abre / crasha

**Causa**: Conflito de versão ou pacote incompatível.

**Solução**:
1. Feche o Revit
2. Limpe a pasta de pacotes do Dynamo:
   ```
   %APPDATA%\Dynamo\Dynamo Revit\2.x\packages\
   ```
3. Reabra o Revit e tente novamente

---

> **Ambiente configurado com sucesso!** Agora você pode iniciar o desenvolvimento do plugin de automação hidráulica.
