# Serviço de Domínio — IExportService (IExportacaoService)

> Especificação completa da interface de serviço responsável pela geração de tabelas hidráulicas, quantitativos de materiais e definições de pranchas do projeto, totalmente agnóstica ao Revit, para uso no PluginCore.

---

## 1. Definição da Interface

### 1.1 O que é IExportService

`IExportService` é o **serviço de entregáveis** do sistema. Ele recebe os resultados das etapas anteriores (equipamentos posicionados, redes dimensionadas, sizing results) e gera **estruturas de dados** que representam tabelas, quantitativos e pranchas. Ele NÃO cria Schedules ou Sheets no Revit — ele produz os dados estruturados que a camada Infrastructure materializa.

### 1.2 Papel no sistema

```
ISizingService (E11)
    │
    └── NetworkSizingResult (redes dimensionadas)
            │
            ╔══════════════════╗
            ║  IExportService  ║  ← ESTE SERVIÇO (Core)
            ╠══════════════════╣
            ║ GenerateTable()  ║  → TableDefinition (dados puros)
            ║ GenerateBOM()    ║  → BillOfMaterials (quantitativos)
            ║ GenerateSheet()  ║  → SheetDefinition (layout)
            ║ ExportToCSV()    ║  → string CSV
            ║ ExportToJSON()   ║  → string JSON
            ╚═══════╤══════════╝
                    │
          ┌─────────┼──────────────┐
          ▼         ▼              ▼
    RevitSchedule  CSV/Excel    RevitSheet
     Writer        (direto)      Writer
   (Infrastr.)                 (Infrastr.)
```

| Etapa | Método usado |
|-------|-------------|
| E12 — Tabelas | `GenerateEquipmentTable()`, `GeneratePipingTable()` |
| E12 — Quantitativos | `GenerateBillOfMaterials()` |
| E12 — Pranchas | `GenerateSheetDefinitions()` |
| E12 — Exportação | `ExportToCSV()`, `ExportToJSON()` |
| E12 — Validação | `ValidateExportData()` |

### 1.3 Por que é independente do Revit

```
ESTE SERVIÇO (Core):
  - Gera TableDefinition: linhas, colunas, agrupamentos, totais
  - Gera SheetDefinition: viewports, titleblocks, posições
  - Gera BillOfMaterials: itens + quantidades + custos
  - Tudo como DTOs puros (List, Dictionary, string, double)

QUEM USA O REVIT (Infrastructure):
  - RevitScheduleWriter → cria ViewSchedule a partir de TableDefinition
  - RevitSheetWriter → cria ViewSheet a partir de SheetDefinition
  - Nenhum tipo Revit aparece na interface

EXPORTAÇÃO DIRETA (sem Revit):
  - ExportToCSV() → arquivo .csv (abre no Excel)
  - ExportToJSON() → arquivo .json (persistência / debug)
  - Funciona mesmo sem Revit instalado
```

---

## 2. Responsabilidades

### 2.1 O que o serviço DEVE fazer

| Ação | Detalhe |
|------|---------|
| Gerar tabela de equipamentos | Lista de aparelhos com peso AF, UHC ES, DN, posição |
| Gerar tabela de tubulações | Lista de trechos com DN, comprimento, material, V, J, ΔP |
| Gerar tabela de dimensionamento AF | Caminho crítico com cálculo de pressão encadeado |
| Gerar tabela de dimensionamento ES | Trechos com UHC, DN, declividade |
| Gerar quantitativos | BOM (Bill of Materials): tubos por DN, fittings, equipamentos |
| Gerar definição de pranchas | Layout de views em folhas (posição, escala, titleblock) |
| Agrupar e totalizar | Subtotais por sistema, por pavimento, por ambiente |
| Exportar CSV | Formato tabular para Excel |
| Exportar JSON | Formato estruturado para persistência |
| Validar dados | Verificar completude antes de exportar |

### 2.2 O que NÃO DEVE fazer

| Proibição | Quem faz |
|-----------|---------|
| ❌ Criar ViewSchedule no Revit | RevitScheduleWriter (Infrastructure) |
| ❌ Criar ViewSheet no Revit | RevitSheetWriter (Infrastructure) |
| ❌ Formatar células (fonte, cor) | Camada de apresentação |
| ❌ Dimensionar tubulações | ISizingService |
| ❌ Montar redes | INetworkService |
| ❌ Gerar PDF | Ferramenta externa |

---

## 3. Interface Completa — Código C#

```csharp
namespace HidraulicoPlugin.Core.Interfaces
{
    /// <summary>
    /// Serviço de geração de entregáveis: tabelas, quantitativos e pranchas.
    /// Corresponde à etapa E12.
    /// Independente do Revit.
    /// </summary>
    public interface IExportService
    {
        // ══════════════════════════════════════════════════════════
        //  TABELAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera tabela de equipamentos por ambiente.
        /// Colunas: Ambiente, Equipamento, Peso AF, UHC ES, DN Sub-ramal, DN Descarga.
        /// </summary>
        TableDefinition GenerateEquipmentTable(
            List<RoomInfo> rooms,
            List<EquipmentInfo> equipment);

        /// <summary>
        /// Gera tabela de tubulações (inventário de trechos).
        /// Colunas: ID, Sistema, Tipo, DN, Comprimento, Material.
        /// </summary>
        TableDefinition GeneratePipingTable(PipeNetwork network);

        /// <summary>
        /// Gera tabela de dimensionamento de água fria.
        /// Formato: tabela encadeada do caminho crítico.
        /// Colunas: Trecho, ΣP, Q, DN, DI, V, J, L, Leq, ΔP, P_disp.
        /// </summary>
        TableDefinition GenerateColdWaterSizingTable(
            NetworkSizingResult sizingResult,
            PressureTraversalResult pressureTraversal);

        /// <summary>
        /// Gera tabela de dimensionamento de esgoto.
        /// Colunas: Trecho, ΣUHC, DN, Decliv, V, Tipo.
        /// </summary>
        TableDefinition GenerateSewerSizingTable(
            NetworkSizingResult sizingResult);

        /// <summary>
        /// Gera tabela de ventilação.
        /// Colunas: Trecho, DN_esgoto, DN_vent, Comprimento.
        /// </summary>
        TableDefinition GenerateVentilationTable(
            NetworkSizingResult sizingResult);

        /// <summary>
        /// Gera tabela de prumadas.
        /// Colunas: Prumada, Sistema, DN, Pavimentos, ΣP/ΣUHC.
        /// </summary>
        TableDefinition GenerateRiserTable(List<Riser> risers,
            List<RiserSizingResult> sizingResults);

        /// <summary>
        /// Gera tabela de resumo por sistema.
        /// Colunas: Sistema, Total Trechos, Compr. Total, DN mín, DN máx.
        /// </summary>
        TableDefinition GenerateSystemSummaryTable(
            List<NetworkSizingResult> allResults);

        /// <summary>
        /// Gera tabela customizada a partir de dados genéricos.
        /// </summary>
        TableDefinition GenerateCustomTable(
            string tableName,
            List<ColumnDefinition> columns,
            List<Dictionary<string, object>> rows);

        // ══════════════════════════════════════════════════════════
        //  QUANTITATIVOS (BOM)
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera quantitativo completo de tubulações.
        /// Agrupa por: material + DN + sistema.
        /// </summary>
        BillOfMaterials GeneratePipingBOM(List<PipeNetwork> networks);

        /// <summary>
        /// Gera quantitativo de fittings (conexões).
        /// Agrupa por: tipo + DN.
        /// </summary>
        BillOfMaterials GenerateFittingBOM(List<PipeNetwork> networks);

        /// <summary>
        /// Gera quantitativo de equipamentos.
        /// Agrupa por: tipo.
        /// </summary>
        BillOfMaterials GenerateEquipmentBOM(List<EquipmentInfo> equipment);

        /// <summary>
        /// Gera quantitativo consolidado (tubos + fittings + equipamentos).
        /// </summary>
        ConsolidatedBOM GenerateConsolidatedBOM(
            List<PipeNetwork> networks,
            List<EquipmentInfo> equipment);

        // ══════════════════════════════════════════════════════════
        //  PRANCHAS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Gera definições de pranchas para o projeto.
        /// Determina quais views vão em quais folhas.
        /// </summary>
        List<SheetDefinition> GenerateSheetDefinitions(
            SheetGenerationInput input);

        /// <summary>
        /// Gera o layout de uma prancha específica.
        /// Posiciona viewports dentro da folha.
        /// </summary>
        SheetLayout GenerateSheetLayout(
            SheetDefinition sheet,
            SheetFormatType format);

        // ══════════════════════════════════════════════════════════
        //  EXPORTAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Exporta uma tabela para formato CSV.
        /// </summary>
        string ExportTableToCSV(TableDefinition table,
            CSVOptions options = null);

        /// <summary>
        /// Exporta uma tabela para JSON.
        /// </summary>
        string ExportTableToJSON(TableDefinition table);

        /// <summary>
        /// Exporta quantitativo para CSV.
        /// </summary>
        string ExportBOMToCSV(BillOfMaterials bom,
            CSVOptions options = null);

        /// <summary>
        /// Exporta um pacote completo de entregáveis.
        /// Gera todas as tabelas + BOMs + pranchas.
        /// </summary>
        ExportPackage GenerateFullExportPackage(FullExportInput input);

        // ══════════════════════════════════════════════════════════
        //  VALIDAÇÃO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Valida se os dados estão completos para exportação.
        /// </summary>
        ExportValidationResult ValidateExportData(FullExportInput input);

        // ══════════════════════════════════════════════════════════
        //  PIPELINE
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Pipeline completo E12: validar → gerar tabelas → gerar BOMs →
        /// gerar pranchas → exportar pacote.
        /// </summary>
        ExportProcessingResult ProcessExport(FullExportInput input,
            ExportOptions options = null);
    }
}
```

---

## 4. DTOs — Tabelas

### 4.1 TableDefinition

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Definição de uma tabela estruturada.
    /// Agnóstica ao formato de saída (CSV, JSON, Revit Schedule).
    /// </summary>
    public class TableDefinition
    {
        // ── Identidade ──────────────────────────────────────────

        /// <summary>Nome da tabela (ex: "Dimensionamento AF").</summary>
        public string Name { get; set; }

        /// <summary>Descrição (ex: "Tabela de dimensionamento de água fria").</summary>
        public string Description { get; set; }

        /// <summary>Sistema associado (AF, ES, VE, All).</summary>
        public HydraulicSystem? System { get; set; }

        /// <summary>Tipo de tabela.</summary>
        public TableType Type { get; set; }

        // ── Estrutura ───────────────────────────────────────────

        /// <summary>Definições das colunas.</summary>
        public List<ColumnDefinition> Columns { get; set; } = new();

        /// <summary>Linhas de dados.</summary>
        public List<TableRow> Rows { get; set; } = new();

        /// <summary>Agrupamentos aplicados.</summary>
        public List<GroupDefinition> Groups { get; set; } = new();

        /// <summary>Linha de totais (se aplicável).</summary>
        public TableRow TotalsRow { get; set; }

        // ── Metadados ───────────────────────────────────────────

        /// <summary>Data de geração.</summary>
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Norma de referência.</summary>
        public string NormReference { get; set; }

        /// <summary>Notas de rodapé.</summary>
        public List<string> FooterNotes { get; set; } = new();

        // ── Métricas ────────────────────────────────────────────

        public int RowCount => Rows.Count;
        public int ColumnCount => Columns.Count;
    }

    /// <summary>
    /// Definição de uma coluna.
    /// </summary>
    public class ColumnDefinition
    {
        /// <summary>ID interno (ex: "dn", "flow_rate").</summary>
        public string Id { get; set; }

        /// <summary>Cabeçalho exibido (ex: "DN (mm)").</summary>
        public string Header { get; set; }

        /// <summary>Tipo de dado.</summary>
        public ColumnDataType DataType { get; set; }

        /// <summary>Unidade (ex: "mm", "L/s", "mca", "m").</summary>
        public string Unit { get; set; }

        /// <summary>Formato numérico (ex: "F2" = 2 decimais).</summary>
        public string Format { get; set; }

        /// <summary>Largura relativa (1-10).</summary>
        public int Width { get; set; } = 1;

        /// <summary>Se deve ter subtotal.</summary>
        public bool HasSubtotal { get; set; }

        /// <summary>Tipo de subtotal (Sum, Average, Count, Max, Min).</summary>
        public AggregationType SubtotalType { get; set; } = AggregationType.Sum;
    }

    /// <summary>
    /// Linha de dados.
    /// </summary>
    public class TableRow
    {
        /// <summary>Valores por coluna (chave = ColumnDefinition.Id).</summary>
        public Dictionary<string, object> Values { get; set; } = new();

        /// <summary>Nível de indentação (para hierarquia).</summary>
        public int IndentLevel { get; set; }

        /// <summary>Se é linha de subtotal.</summary>
        public bool IsSubtotal { get; set; }

        /// <summary>Nome do grupo (se houver agrupamento).</summary>
        public string GroupName { get; set; }

        /// <summary>Acessa valor por coluna ID.</summary>
        public object this[string columnId]
        {
            get => Values.TryGetValue(columnId, out var v) ? v : null;
            set => Values[columnId] = value;
        }
    }

    /// <summary>
    /// Definição de agrupamento.
    /// </summary>
    public class GroupDefinition
    {
        /// <summary>Coluna usada para agrupar.</summary>
        public string GroupByColumnId { get; set; }

        /// <summary>Se deve mostrar subtotal por grupo.</summary>
        public bool ShowSubtotal { get; set; } = true;

        /// <summary>Se deve mostrar cabeçalho de grupo.</summary>
        public bool ShowGroupHeader { get; set; } = true;
    }

    /// <summary>Tipo de tabela.</summary>
    public enum TableType
    {
        EquipmentList = 1,
        PipingInventory = 2,
        ColdWaterSizing = 3,
        SewerSizing = 4,
        VentilationSizing = 5,
        RiserSummary = 6,
        SystemSummary = 7,
        Custom = 99
    }

    /// <summary>Tipo de dado da coluna.</summary>
    public enum ColumnDataType
    {
        Text, Integer, Decimal, Boolean, Enum
    }

    /// <summary>Tipo de agregação.</summary>
    public enum AggregationType
    {
        None, Sum, Average, Count, Max, Min
    }
}
```

---

## 5. DTOs — Quantitativos (BOM)

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Bill of Materials — quantitativo de materiais.
    /// </summary>
    public class BillOfMaterials
    {
        public string Name { get; set; }
        public BOMCategory Category { get; set; }
        public List<BOMItem> Items { get; set; } = new();
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public int TotalItems => Items.Count;
        public double TotalCost => Items.Sum(i => i.TotalCost);

        public string GetSummary() =>
            $"{Name}: {TotalItems} itens, custo total estimado: R$ {TotalCost:N2}";
    }

    /// <summary>
    /// Item do quantitativo.
    /// </summary>
    public class BOMItem
    {
        /// <summary>Descrição do item (ex: "Tubo PVC soldável DN 25").</summary>
        public string Description { get; set; }

        /// <summary>Código do material (se disponível).</summary>
        public string MaterialCode { get; set; }

        /// <summary>Categoria (Tubo, Fitting, Equipamento).</summary>
        public BOMCategory Category { get; set; }

        /// <summary>Sistema (AF, ES, VE).</summary>
        public HydraulicSystem System { get; set; }

        /// <summary>Material (PVC, CPVC, PPR).</summary>
        public PipeMaterial? Material { get; set; }

        /// <summary>DN em mm (para tubos e fittings).</summary>
        public int? DiameterMm { get; set; }

        /// <summary>Quantidade.</summary>
        public double Quantity { get; set; }

        /// <summary>Unidade (m, pç, un).</summary>
        public string Unit { get; set; }

        /// <summary>Preço unitário estimado (R$).</summary>
        public double UnitPrice { get; set; }

        /// <summary>Custo total (Quantity × UnitPrice).</summary>
        public double TotalCost => Quantity * UnitPrice;

        /// <summary>Tipo de fitting (se for fitting).</summary>
        public FittingType? FittingType { get; set; }

        /// <summary>Tipo de equipamento (se for equipamento).</summary>
        public EquipmentType? EquipmentType { get; set; }
    }

    /// <summary>Categoria do BOM.</summary>
    public enum BOMCategory
    {
        Pipe = 1,
        Fitting = 2,
        Equipment = 3,
        Accessory = 4,
        Consolidated = 99
    }

    /// <summary>
    /// BOM consolidado (todos os tipos).
    /// </summary>
    public class ConsolidatedBOM
    {
        public BillOfMaterials Piping { get; set; }
        public BillOfMaterials Fittings { get; set; }
        public BillOfMaterials Equipment { get; set; }

        public double TotalCost =>
            (Piping?.TotalCost ?? 0) +
            (Fittings?.TotalCost ?? 0) +
            (Equipment?.TotalCost ?? 0);

        public int TotalLineItems =>
            (Piping?.TotalItems ?? 0) +
            (Fittings?.TotalItems ?? 0) +
            (Equipment?.TotalItems ?? 0);

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Exporta tudo como uma única tabela.</summary>
        public TableDefinition ToTable()
        {
            var table = new TableDefinition
            {
                Name = "Quantitativo Consolidado",
                Type = TableType.Custom,
                Columns = new List<ColumnDefinition>
                {
                    new() { Id = "category", Header = "Categoria", DataType = ColumnDataType.Text },
                    new() { Id = "description", Header = "Descrição", DataType = ColumnDataType.Text, Width = 4 },
                    new() { Id = "system", Header = "Sistema", DataType = ColumnDataType.Text },
                    new() { Id = "dn", Header = "DN (mm)", DataType = ColumnDataType.Integer, Unit = "mm" },
                    new() { Id = "quantity", Header = "Qtd", DataType = ColumnDataType.Decimal, Format = "F1", HasSubtotal = true },
                    new() { Id = "unit", Header = "Un", DataType = ColumnDataType.Text },
                    new() { Id = "unit_price", Header = "P.Unit (R$)", DataType = ColumnDataType.Decimal, Format = "N2" },
                    new() { Id = "total_price", Header = "Total (R$)", DataType = ColumnDataType.Decimal, Format = "N2", HasSubtotal = true, SubtotalType = AggregationType.Sum }
                },
                Groups = new() { new() { GroupByColumnId = "category", ShowSubtotal = true } }
            };
            return table;
        }
    }
}
```

---

## 6. DTOs — Pranchas

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Definição de uma prancha (sheet) do projeto.
    /// </summary>
    public class SheetDefinition
    {
        /// <summary>Número da prancha (ex: "HID-01").</summary>
        public string SheetNumber { get; set; }

        /// <summary>Nome da prancha (ex: "Planta Hidráulica — 1º Pavimento").</summary>
        public string SheetName { get; set; }

        /// <summary>Formato da folha.</summary>
        public SheetFormatType Format { get; set; } = SheetFormatType.A1;

        /// <summary>Nome do titleblock (família Revit).</summary>
        public string TitleBlockName { get; set; }

        /// <summary>Viewports a posicionar na prancha.</summary>
        public List<ViewportDefinition> Viewports { get; set; } = new();

        /// <summary>Tabelas (schedules) a incluir na prancha.</summary>
        public List<SchedulePlacement> Schedules { get; set; } = new();

        /// <summary>Categoria da prancha.</summary>
        public SheetCategory Category { get; set; }

        /// <summary>Disciplina.</summary>
        public string Discipline { get; set; } = "Hidráulica";

        /// <summary>Revisão.</summary>
        public string Revision { get; set; } = "R00";
    }

    /// <summary>
    /// Definição de um viewport na prancha.
    /// </summary>
    public class ViewportDefinition
    {
        /// <summary>Nome da view no Revit (ex: "Planta AF — 1º Pav").</summary>
        public string ViewName { get; set; }

        /// <summary>Tipo de view.</summary>
        public ViewType ViewType { get; set; }

        /// <summary>Escala da view (ex: 50 = 1:50).</summary>
        public int Scale { get; set; } = 50;

        /// <summary>Posição X no papel (mm da borda esquerda).</summary>
        public double PositionXmm { get; set; }

        /// <summary>Posição Y no papel (mm da borda inferior).</summary>
        public double PositionYmm { get; set; }

        /// <summary>Sistema filtrado (AF, ES, VE, All).</summary>
        public HydraulicSystem? SystemFilter { get; set; }

        /// <summary>Pavimento filtrado.</summary>
        public string LevelFilter { get; set; }
    }

    /// <summary>
    /// Posicionamento de schedule na prancha.
    /// </summary>
    public class SchedulePlacement
    {
        /// <summary>Nome do schedule.</summary>
        public string ScheduleName { get; set; }

        /// <summary>Tabela de dados associada.</summary>
        public TableType TableType { get; set; }

        /// <summary>Posição X no papel (mm).</summary>
        public double PositionXmm { get; set; }

        /// <summary>Posição Y no papel (mm).</summary>
        public double PositionYmm { get; set; }
    }

    /// <summary>
    /// Layout calculado de uma prancha.
    /// </summary>
    public class SheetLayout
    {
        public string SheetNumber { get; set; }
        public SheetFormatType Format { get; set; }

        /// <summary>Largura útil do papel em mm.</summary>
        public double UsableWidthMm { get; set; }

        /// <summary>Altura útil do papel em mm.</summary>
        public double UsableHeightMm { get; set; }

        /// <summary>Viewports com posições calculadas.</summary>
        public List<ViewportDefinition> PlacedViewports { get; set; } = new();

        /// <summary>Schedules com posições calculadas.</summary>
        public List<SchedulePlacement> PlacedSchedules { get; set; } = new();

        /// <summary>Porcentagem de área ocupada.</summary>
        public double OccupancyPercent { get; set; }
    }

    /// <summary>Formato da folha.</summary>
    public enum SheetFormatType
    {
        A0 = 0,
        A1 = 1,
        A2 = 2,
        A3 = 3,
        A4 = 4,
        Custom = 99
    }

    /// <summary>Tipo de view.</summary>
    public enum ViewType
    {
        FloorPlan = 1,
        Section = 2,
        Detail = 3,
        ThreeD = 4,
        Legend = 5,
        Schedule = 6
    }

    /// <summary>Categoria da prancha.</summary>
    public enum SheetCategory
    {
        General = 1,
        ColdWaterPlan = 2,
        SewerPlan = 3,
        VentilationPlan = 4,
        Isometric = 5,
        Details = 6,
        Schedules = 7
    }
}
```

---

## 7. DTOs — Inputs e Outputs

### 7.1 FullExportInput

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Dados de entrada para geração completa de entregáveis.
    /// Consolidação de todas as etapas anteriores.
    /// </summary>
    public class FullExportInput
    {
        // ── E01/E02 ─────────────────────────────────────────────
        public List<RoomInfo> Rooms { get; set; } = new();

        // ── E03/E04 ─────────────────────────────────────────────
        public List<EquipmentInfo> Equipment { get; set; } = new();

        // ── E06-E09 ─────────────────────────────────────────────
        public List<PipeNetwork> Networks { get; set; } = new();
        public List<Riser> Risers { get; set; } = new();

        // ── E11 ─────────────────────────────────────────────────
        public List<NetworkSizingResult> SizingResults { get; set; } = new();
        public List<RiserSizingResult> RiserSizingResults { get; set; } = new();
        public PressureTraversalResult PressureTraversal { get; set; }
        public CriticalPointResult CriticalPoint { get; set; }

        // ── Projeto ─────────────────────────────────────────────
        public ProjectInfo ProjectInfo { get; set; }
    }

    /// <summary>
    /// Informações do projeto para título e carimbo.
    /// </summary>
    public class ProjectInfo
    {
        public string ProjectName { get; set; }
        public string ClientName { get; set; }
        public string EngineerName { get; set; }
        public string CreaNumber { get; set; }
        public string Address { get; set; }
        public string Date { get; set; }
        public string Revision { get; set; } = "R00";
    }
}
```

### 7.2 SheetGenerationInput

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Input para geração de pranchas.
    /// </summary>
    public class SheetGenerationInput
    {
        /// <summary>Pavimentos do projeto.</summary>
        public List<string> LevelNames { get; set; } = new();

        /// <summary>Sistemas a incluir.</summary>
        public List<HydraulicSystem> Systems { get; set; } = new();

        /// <summary>Formato padrão das folhas.</summary>
        public SheetFormatType DefaultFormat { get; set; } = SheetFormatType.A1;

        /// <summary>Nome do titleblock.</summary>
        public string TitleBlockName { get; set; }

        /// <summary>Escala padrão das plantas.</summary>
        public int DefaultScale { get; set; } = 50;

        /// <summary>Esquema de numeração (ex: "HID-{NN}").</summary>
        public string NumberingPattern { get; set; } = "HID-{NN}";

        /// <summary>Se deve separar por sistema ou combinar.</summary>
        public bool SeparateBySystem { get; set; } = true;

        /// <summary>Se deve incluir pranchas de detalhes.</summary>
        public bool IncludeDetails { get; set; } = true;

        /// <summary>Se deve incluir pranchas de tabelas.</summary>
        public bool IncludeScheduleSheets { get; set; } = true;

        /// <summary>Informações do projeto (para titleblock).</summary>
        public ProjectInfo ProjectInfo { get; set; }

        /// <summary>Tabelas já geradas para incluir nas pranchas.</summary>
        public List<TableDefinition> Tables { get; set; } = new();
    }
}
```

### 7.3 ExportOptions e CSVOptions

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class ExportOptions
    {
        /// <summary>Quais tabelas gerar.</summary>
        public List<TableType> TablesToGenerate { get; set; } = new()
        {
            TableType.EquipmentList,
            TableType.ColdWaterSizing,
            TableType.SewerSizing,
            TableType.RiserSummary,
            TableType.SystemSummary
        };

        /// <summary>Gerar quantitativos?</summary>
        public bool GenerateBOM { get; set; } = true;

        /// <summary>Gerar pranchas?</summary>
        public bool GenerateSheets { get; set; } = true;

        /// <summary>Exportar CSV?</summary>
        public bool ExportCSV { get; set; } = true;

        /// <summary>Exportar JSON?</summary>
        public bool ExportJSON { get; set; } = true;

        /// <summary>Diretório de saída para arquivos.</summary>
        public string OutputDirectory { get; set; }
    }

    public class CSVOptions
    {
        public string Separator { get; set; } = ";";
        public string DecimalSeparator { get; set; } = ",";
        public bool IncludeHeaders { get; set; } = true;
        public bool IncludeUnits { get; set; } = true;
        public string Encoding { get; set; } = "UTF-8";
        public string LineEnding { get; set; } = "\r\n";
    }
}
```

### 7.4 ExportPackage

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    /// <summary>
    /// Pacote completo de entregáveis.
    /// </summary>
    public class ExportPackage
    {
        /// <summary>Todas as tabelas geradas.</summary>
        public List<TableDefinition> Tables { get; set; } = new();

        /// <summary>Quantitativo consolidado.</summary>
        public ConsolidatedBOM BOM { get; set; }

        /// <summary>Definições de pranchas.</summary>
        public List<SheetDefinition> Sheets { get; set; } = new();

        /// <summary>Arquivos CSV gerados (nome → conteúdo).</summary>
        public Dictionary<string, string> CSVFiles { get; set; } = new();

        /// <summary>Arquivos JSON gerados (nome → conteúdo).</summary>
        public Dictionary<string, string> JSONFiles { get; set; } = new();

        /// <summary>Resumo.</summary>
        public ExportSummary Summary { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class ExportSummary
    {
        public int TotalTables { get; set; }
        public int TotalBOMItems { get; set; }
        public int TotalSheets { get; set; }
        public int TotalCSVFiles { get; set; }
        public int TotalJSONFiles { get; set; }
        public double EstimatedCostBRL { get; set; }
        public TimeSpan GenerationTime { get; set; }

        public string GetSummary() =>
            $"Exportação: {TotalTables} tabelas, {TotalBOMItems} itens BOM, " +
            $"{TotalSheets} pranchas, {TotalCSVFiles} CSVs. " +
            $"Custo estimado: R$ {EstimatedCostBRL:N2}. " +
            $"Tempo: {GenerationTime.TotalSeconds:F1}s";
    }
}
```

### 7.5 Validação e Pipeline

```csharp
namespace HidraulicoPlugin.Core.DTOs
{
    public class ExportValidationResult
    {
        public bool IsValid { get; set; }
        public List<ExportValidationIssue> Issues { get; set; } = new();
        public int CriticalCount => Issues.Count(i => i.IsCritical);
    }

    public class ExportValidationIssue
    {
        public bool IsCritical { get; set; }
        public string Code { get; set; }
        public string Message { get; set; }
        public string MissingData { get; set; }
    }

    public class ExportProcessingResult
    {
        public ExportValidationResult Validation { get; set; }
        public ExportPackage Package { get; set; }
        public bool IsSuccessful { get; set; }
        public List<string> Errors { get; set; } = new();
        public TimeSpan TotalDuration { get; set; }

        public string GetSummary() =>
            IsSuccessful
                ? $"✅ {Package?.Summary?.GetSummary()}"
                : $"❌ Exportação falhou: {string.Join(", ", Errors)}";
    }
}
```

---

## 8. Regras de Geração de Tabelas

### 8.1 Tabela de Dimensionamento AF (formato padrão)

```
┌───────┬────────────────┬──────┬────────┬──────┬──────┬───────┬──────┬──────┬──────┬───────┬───────┐
│Trecho │ Aparelhos      │  ΣP  │ Q(L/s) │  DN  │  DI  │ V(m/s)│ J    │ L(m) │Leq(m)│ΔP(mca)│ P_disp│
├───────┼────────────────┼──────┼────────┼──────┼──────┼───────┼──────┼──────┼──────┼───────┼───────┤
│ A-B   │ Barrilete      │ 3.60 │ 0.569  │  32  │ 27.8 │ 0.94  │0.032 │ 4.00 │ 2.30 │ 0.202 │ 9.798 │
│ B-C   │ Col. AF-01     │ 1.80 │ 0.402  │  25  │ 21.6 │ 1.10  │0.064 │ 3.00 │ 1.50 │ 0.288 │ 9.510 │
│ C-D   │ Ram. Banheiro  │ 0.90 │ 0.285  │  20  │ 17.0 │ 1.25  │0.101 │ 2.50 │ 3.40 │ 0.596 │ 8.914 │
│ D-E   │ Sub-ram. Chuv. │ 0.50 │ 0.212  │  20  │ 17.0 │ 0.93  │0.060 │ 1.00 │ 1.10 │ 0.126 │ 8.788 │
└───────┴────────────────┴──────┴────────┴──────┴──────┴───────┴──────┴──────┴──────┴───────┴───────┘
                                                                              P_disponível final: 8.788 mca
                                                                              P_mínima requerida: 1.000 mca
                                                                              Margem: 7.788 mca ✅
```

### 8.2 Tabela de Dimensionamento ES (formato padrão)

```
┌───────┬────────────────┬──────┬──────┬──────────┬───────┬─────────┐
│Trecho │ Tipo           │ ΣUHC │  DN  │ Decliv(%)│ V(m/s)│ Material│
├───────┼────────────────┼──────┼──────┼──────────┼───────┼─────────┤
│ a-b   │ Ram. descarga  │   2  │  40  │   2.0    │ 0.65  │ PVC     │
│ b-c   │ Ram. descarga  │   2  │  40  │   2.0    │ 0.65  │ PVC     │
│ c-d   │ CX sifonada    │   4  │  50  │   2.0    │ 0.72  │ PVC     │
│ d-e   │ Subcoletor     │  10  │  75  │   2.0    │ 0.85  │ PVC     │
│ e-f   │ Ram. descarga  │   6  │ 100  │   —      │  —    │ PVC     │
│ f-g   │ TQ             │  16  │ 100  │   —      │  —    │ PVC     │
│ g-h   │ Coletor predial│  16  │ 100  │   1.0    │ 0.92  │ PVC     │
└───────┴────────────────┴──────┴──────┴──────────┴───────┴─────────┘
```

---

## 9. Exemplo de Uso (no Orchestrator / E12)

```csharp
public class PipelineOrchestrator
{
    private readonly IExportService _exportService;
    private readonly ILogService _log;

    public void ExecuteE12(
        List<RoomInfo> rooms,
        List<EquipmentInfo> equipment,
        List<PipeNetwork> networks,
        List<NetworkSizingResult> sizingResults,
        PressureTraversalResult pressureTraversal)
    {
        _log.LogStepStart("E12_GenerateTables", "Geração de entregáveis");

        // Pipeline completo
        var result = _exportService.ProcessExport(
            new FullExportInput
            {
                Rooms = rooms,
                Equipment = equipment,
                Networks = networks,
                SizingResults = sizingResults,
                PressureTraversal = pressureTraversal,
                ProjectInfo = new ProjectInfo
                {
                    ProjectName = "Residencial Jardim Europa",
                    ClientName = "Construtora Alpha",
                    EngineerName = "Eng. João Silva",
                    CreaNumber = "CREA-SP 123456",
                    Date = DateTime.Now.ToString("dd/MM/yyyy")
                }
            },
            new ExportOptions
            {
                GenerateBOM = true,
                GenerateSheets = true,
                ExportCSV = true,
                OutputDirectory = @"C:\Projetos\JardimEuropa\Export"
            });

        _log.LogStepEnd("E12_GenerateTables",
            result.IsSuccessful,
            new
            {
                tables = result.Package?.Tables?.Count,
                bomItems = result.Package?.BOM?.TotalLineItems,
                sheets = result.Package?.Sheets?.Count,
                cost = result.Package?.BOM?.TotalCost
            },
            result.TotalDuration);

        Console.WriteLine(result.GetSummary());
        // → "✅ Exportação: 7 tabelas, 23 itens BOM, 5 pranchas, 7 CSVs.
        //    Custo estimado: R$ 4.532,80. Tempo: 1.2s"
    }
}
```

---

## 10. Resumo Visual

```
IExportService
│
├── Tabelas (7 tipos)
│   ├── GenerateEquipmentTable(rooms, equip) → TableDefinition
│   ├── GeneratePipingTable(network) → TableDefinition
│   ├── GenerateColdWaterSizingTable(sizing, pressure) → TableDefinition
│   ├── GenerateSewerSizingTable(sizing) → TableDefinition
│   ├── GenerateVentilationTable(sizing) → TableDefinition
│   ├── GenerateRiserTable(risers, sizing) → TableDefinition
│   ├── GenerateSystemSummaryTable(allResults) → TableDefinition
│   └── GenerateCustomTable(name, columns, rows) → TableDefinition
│
├── Quantitativos (BOM)
│   ├── GeneratePipingBOM(networks) → BillOfMaterials
│   ├── GenerateFittingBOM(networks) → BillOfMaterials
│   ├── GenerateEquipmentBOM(equipment) → BillOfMaterials
│   └── GenerateConsolidatedBOM(nets, equip) → ConsolidatedBOM
│
├── Pranchas
│   ├── GenerateSheetDefinitions(input) → List<SheetDefinition>
│   └── GenerateSheetLayout(sheet, format) → SheetLayout
│
├── Exportação
│   ├── ExportTableToCSV(table, options) → string
│   ├── ExportTableToJSON(table) → string
│   ├── ExportBOMToCSV(bom, options) → string
│   └── GenerateFullExportPackage(input) → ExportPackage
│
├── Validação
│   └── ValidateExportData(input) → ExportValidationResult
│
├── Pipeline
│   └── ProcessExport(input, options) → ExportProcessingResult
│
├── DTOs de Tabela
│   ├── TableDefinition (name, columns, rows, groups, totals)
│   ├── ColumnDefinition (id, header, type, unit, format, subtotal)
│   ├── TableRow (values dict, indent, group)
│   └── GroupDefinition (groupBy, showSubtotal)
│
├── DTOs de BOM
│   ├── BillOfMaterials (name, items, totalCost)
│   ├── BOMItem (desc, qty, unit, price, DN, material)
│   └── ConsolidatedBOM (piping + fittings + equipment)
│
├── DTOs de Prancha
│   ├── SheetDefinition (number, name, format, viewports, schedules)
│   ├── ViewportDefinition (viewName, scale, position, filters)
│   ├── SchedulePlacement (name, position)
│   └── SheetLayout (usableArea, placed items, occupancy%)
│
└── Dependências
    ├── NetworkSizingResult, PressureTraversalResult (E11)
    ├── PipeNetwork, Riser (E06-E09)
    ├── EquipmentInfo, RoomInfo (E03-E04)
    └── RevitScheduleWriter, RevitSheetWriter (Infrastructure)
```
