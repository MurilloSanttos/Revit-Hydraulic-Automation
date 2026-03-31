using System.Text;
using Autodesk.Revit.DB;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Revit2026.Modules.Schedules.Export
{
    /// <summary>
    /// Interface para exportação de Schedules.
    /// </summary>
    public interface IScheduleExportService
    {
        void ExportarCsv(ViewSchedule schedule, string caminhoArquivo);
        void ExportarExcel(ViewSchedule schedule, string caminhoArquivo);
    }

    /// <summary>
    /// Dados extraídos de um ViewSchedule.
    /// </summary>
    public class ScheduleData
    {
        public string Nome { get; set; } = "";
        public List<string> Cabecalhos { get; set; } = new();
        public List<List<string>> Linhas { get; set; } = new();
        public int TotalColunas => Cabecalhos.Count;
        public int TotalLinhas => Linhas.Count;

        public override string ToString() =>
            $"{Nome}: {TotalColunas} colunas × {TotalLinhas} linhas";
    }

    /// <summary>
    /// Serviço de exportação de ViewSchedule para CSV e Excel (.xlsx).
    ///
    /// Extrai dados via TableData/GetCellText e preserva:
    /// - Cabeçalhos
    /// - Linhas formatadas
    /// - Valores vazios como string vazia
    ///
    /// CSV: separador ;, UTF-8 com BOM
    /// Excel: OpenXML SDK, uma aba por schedule
    /// </summary>
    public class ScheduleExportService : IScheduleExportService
    {
        private const string CSV_SEPARATOR = ";";

        // ══════════════════════════════════════════════════════════
        //  EXPORTAR CSV
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Exporta ViewSchedule para arquivo CSV.
        /// Separador: ;
        /// Encoding: UTF-8 com BOM
        /// </summary>
        public void ExportarCsv(ViewSchedule schedule, string caminhoArquivo)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (string.IsNullOrEmpty(caminhoArquivo))
                throw new ArgumentNullException(nameof(caminhoArquivo));

            var dados = ExtrairDados(schedule);
            GarantirDiretorio(caminhoArquivo);

            var sb = new StringBuilder();

            // Cabeçalhos
            if (dados.Cabecalhos.Count > 0)
            {
                sb.AppendLine(string.Join(CSV_SEPARATOR,
                    dados.Cabecalhos.Select(EscaparCsv)));
            }

            // Linhas
            foreach (var linha in dados.Linhas)
            {
                sb.AppendLine(string.Join(CSV_SEPARATOR,
                    linha.Select(EscaparCsv)));
            }

            File.WriteAllText(caminhoArquivo, sb.ToString(),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        }

        // ══════════════════════════════════════════════════════════
        //  EXPORTAR EXCEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Exporta ViewSchedule para arquivo Excel (.xlsx).
        /// Uma aba por schedule com cabeçalho na primeira linha.
        /// </summary>
        public void ExportarExcel(ViewSchedule schedule, string caminhoArquivo)
        {
            if (schedule == null)
                throw new ArgumentNullException(nameof(schedule));
            if (string.IsNullOrEmpty(caminhoArquivo))
                throw new ArgumentNullException(nameof(caminhoArquivo));

            var dados = ExtrairDados(schedule);
            GarantirDiretorio(caminhoArquivo);

            using var spreadsheet = SpreadsheetDocument.Create(
                caminhoArquivo, SpreadsheetDocumentType.Workbook);

            // Workbook
            var workbookPart = spreadsheet.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            // Stylesheet para formatação
            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = CriarStylesheet();
            stylesPart.Stylesheet.Save();

            // Worksheet
            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var sheetData = new SheetData();
            worksheetPart.Worksheet = new Worksheet(sheetData);

            // Sheet
            var sheets = workbookPart.Workbook.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Sheets());
            var nomeAba = SanitizarNomeAba(dados.Nome);

            sheets.AppendChild(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = nomeAba
            });

            // ── Cabeçalho (linha 1) ───────────────────────────
            if (dados.Cabecalhos.Count > 0)
            {
                var headerRow = new Row { RowIndex = 1 };

                for (int col = 0; col < dados.Cabecalhos.Count; col++)
                {
                    var cell = CriarCelula(
                        GetColumnReference(col) + "1",
                        dados.Cabecalhos[col],
                        styleIndex: 1); // Negrito
                    headerRow.AppendChild(cell);
                }

                sheetData.AppendChild(headerRow);
            }

            // ── Dados (linha 2+) ──────────────────────────────
            for (int rowIdx = 0; rowIdx < dados.Linhas.Count; rowIdx++)
            {
                var linha = dados.Linhas[rowIdx];
                uint rowNum = (uint)(rowIdx + 2); // +2 porque header = 1

                var dataRow = new Row { RowIndex = rowNum };

                for (int col = 0; col < linha.Count; col++)
                {
                    var cellRef = GetColumnReference(col) + rowNum;
                    var valor = linha[col];

                    // Tentar como número
                    if (double.TryParse(valor.Replace(",", "."),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var numero))
                    {
                        var cell = CriarCelulaNumero(cellRef, numero);
                        dataRow.AppendChild(cell);
                    }
                    else
                    {
                        var cell = CriarCelula(cellRef, valor);
                        dataRow.AppendChild(cell);
                    }
                }

                sheetData.AppendChild(dataRow);
            }

            // ── Ajustar largura das colunas ───────────────────
            AjustarLarguraColunas(worksheetPart, dados);

            worksheetPart.Worksheet.Save();
            workbookPart.Workbook.Save();
        }

        // ══════════════════════════════════════════════════════════
        //  EXPORTAR MÚLTIPLOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Exporta múltiplos schedules para um único arquivo Excel.
        /// Cada schedule vira uma aba.
        /// </summary>
        public void ExportarMultiplosExcel(
            IList<ViewSchedule> schedules,
            string caminhoArquivo)
        {
            if (schedules == null || schedules.Count == 0)
                throw new ArgumentException("Lista de schedules vazia.");

            GarantirDiretorio(caminhoArquivo);

            using var spreadsheet = SpreadsheetDocument.Create(
                caminhoArquivo, SpreadsheetDocumentType.Workbook);

            var workbookPart = spreadsheet.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = CriarStylesheet();
            stylesPart.Stylesheet.Save();

            var sheets = workbookPart.Workbook.AppendChild(new DocumentFormat.OpenXml.Spreadsheet.Sheets());
            uint sheetId = 0;

            foreach (var schedule in schedules)
            {
                sheetId++;
                var dados = ExtrairDados(schedule);

                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var sheetData = new SheetData();
                worksheetPart.Worksheet = new Worksheet(sheetData);

                var nomeAba = SanitizarNomeAba(dados.Nome);

                sheets.AppendChild(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = sheetId,
                    Name = nomeAba
                });

                // Cabeçalho
                if (dados.Cabecalhos.Count > 0)
                {
                    var headerRow = new Row { RowIndex = 1 };
                    for (int col = 0; col < dados.Cabecalhos.Count; col++)
                    {
                        headerRow.AppendChild(CriarCelula(
                            GetColumnReference(col) + "1",
                            dados.Cabecalhos[col],
                            styleIndex: 1));
                    }
                    sheetData.AppendChild(headerRow);
                }

                // Dados
                for (int rowIdx = 0; rowIdx < dados.Linhas.Count; rowIdx++)
                {
                    var linha = dados.Linhas[rowIdx];
                    uint rowNum = (uint)(rowIdx + 2);
                    var dataRow = new Row { RowIndex = rowNum };

                    for (int col = 0; col < linha.Count; col++)
                    {
                        var cellRef = GetColumnReference(col) + rowNum;
                        var valor = linha[col];

                        if (double.TryParse(valor.Replace(",", "."),
                            System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out var numero))
                        {
                            dataRow.AppendChild(CriarCelulaNumero(cellRef, numero));
                        }
                        else
                        {
                            dataRow.AppendChild(CriarCelula(cellRef, valor));
                        }
                    }

                    sheetData.AppendChild(dataRow);
                }

                AjustarLarguraColunas(worksheetPart, dados);
                worksheetPart.Worksheet.Save();
            }

            workbookPart.Workbook.Save();
        }

        // ══════════════════════════════════════════════════════════
        //  EXTRAÇÃO DE DADOS
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Extrai todos os dados de um ViewSchedule.
        /// Usa TableData + GetCellText para valores formatados.
        /// </summary>
        public static ScheduleData ExtrairDados(ViewSchedule schedule)
        {
            var dados = new ScheduleData
            {
                Nome = schedule.Name ?? "Schedule"
            };

            var tableData = schedule.GetTableData();
            if (tableData == null)
                return dados;

            // ── Extrair cabeçalhos ────────────────────────────
            var headerSection = tableData.GetSectionData(SectionType.Header);
            if (headerSection != null && headerSection.NumberOfRows > 0)
            {
                var lastHeaderRow = headerSection.NumberOfRows - 1;
                var numCols = headerSection.NumberOfColumns;

                for (int col = 0; col < numCols; col++)
                {
                    try
                    {
                        var texto = schedule.GetCellText(
                            SectionType.Header, lastHeaderRow, col);
                        dados.Cabecalhos.Add(texto ?? "");
                    }
                    catch
                    {
                        dados.Cabecalhos.Add("");
                    }
                }
            }

            // ── Extrair corpo ─────────────────────────────────
            var bodySection = tableData.GetSectionData(SectionType.Body);
            if (bodySection == null)
                return dados;

            var bodyRows = bodySection.NumberOfRows;
            var bodyCols = bodySection.NumberOfColumns;

            // Se não temos cabeçalhos do header, usar primeira linha do body
            if (dados.Cabecalhos.Count == 0 && bodyRows > 0)
            {
                for (int col = 0; col < bodyCols; col++)
                {
                    try
                    {
                        var texto = schedule.GetCellText(
                            SectionType.Body, 0, col);
                        dados.Cabecalhos.Add(texto ?? "");
                    }
                    catch
                    {
                        dados.Cabecalhos.Add("");
                    }
                }
            }

            // Linhas de dados
            int startRow = dados.Cabecalhos.Count > 0 &&
                           headerSection?.NumberOfRows == 0 ? 1 : 0;

            for (int row = startRow; row < bodyRows; row++)
            {
                var linha = new List<string>();
                bool linhaVazia = true;

                for (int col = 0; col < bodyCols; col++)
                {
                    try
                    {
                        var texto = schedule.GetCellText(
                            SectionType.Body, row, col);
                        var valor = texto ?? "";
                        linha.Add(valor);

                        if (!string.IsNullOrWhiteSpace(valor))
                            linhaVazia = false;
                    }
                    catch
                    {
                        linha.Add("");
                    }
                }

                // Não exportar linhas completamente vazias
                if (!linhaVazia)
                    dados.Linhas.Add(linha);
            }

            return dados;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS — CSV
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Escapa valor para CSV (RFC 4180).
        /// </summary>
        private static string EscaparCsv(string valor)
        {
            if (string.IsNullOrEmpty(valor))
                return "";

            // Se contém separador, aspas ou quebra de linha → encapsular
            if (valor.Contains(CSV_SEPARATOR) ||
                valor.Contains('"') ||
                valor.Contains('\n') ||
                valor.Contains('\r'))
            {
                return "\"" + valor.Replace("\"", "\"\"") + "\"";
            }

            return valor;
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS — EXCEL
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Cria célula de texto (InlineString).
        /// </summary>
        private static Cell CriarCelula(
            string referencia,
            string valor,
            uint styleIndex = 0)
        {
            var cell = new Cell
            {
                CellReference = referencia,
                DataType = CellValues.InlineString,
                StyleIndex = styleIndex
            };

            cell.AppendChild(new InlineString(
                new Text(valor ?? "") { Space = SpaceProcessingModeValues.Preserve }));

            return cell;
        }

        /// <summary>
        /// Cria célula numérica.
        /// </summary>
        private static Cell CriarCelulaNumero(string referencia, double valor)
        {
            return new Cell
            {
                CellReference = referencia,
                DataType = CellValues.Number,
                CellValue = new CellValue(valor.ToString(
                    System.Globalization.CultureInfo.InvariantCulture))
            };
        }

        /// <summary>
        /// Converte índice de coluna (0-based) para referência Excel (A, B, ..., AA, AB...).
        /// </summary>
        private static string GetColumnReference(int colIndex)
        {
            var result = new StringBuilder();
            var index = colIndex;

            do
            {
                result.Insert(0, (char)('A' + index % 26));
                index = index / 26 - 1;
            }
            while (index >= 0);

            return result.ToString();
        }

        /// <summary>
        /// Sanitiza nome para aba Excel (max 31 chars, sem caracteres proibidos).
        /// </summary>
        private static string SanitizarNomeAba(string nome)
        {
            if (string.IsNullOrEmpty(nome))
                return "Sheet1";

            var sanitizado = nome
                .Replace("\\", "_")
                .Replace("/", "_")
                .Replace("*", "_")
                .Replace("[", "(")
                .Replace("]", ")")
                .Replace(":", "-")
                .Replace("?", "")
                .Replace("'", "");

            if (sanitizado.Length > 31)
                sanitizado = sanitizado.Substring(0, 31);

            return string.IsNullOrWhiteSpace(sanitizado) ? "Sheet1" : sanitizado;
        }

        /// <summary>
        /// Cria stylesheet mínima com estilo normal + negrito.
        /// </summary>
        private static Stylesheet CriarStylesheet()
        {
            return new Stylesheet(
                new Fonts(
                    // 0 — Normal
                    new Font(
                        new FontSize { Val = 11 },
                        new FontName { Val = "Calibri" }),
                    // 1 — Negrito
                    new Font(
                        new Bold(),
                        new FontSize { Val = 11 },
                        new FontName { Val = "Calibri" })
                ),
                new Fills(
                    // 0 — Sem preenchimento
                    new Fill(new PatternFill { PatternType = PatternValues.None }),
                    // 1 — Gray125 (obrigatório pelo OpenXml)
                    new Fill(new PatternFill { PatternType = PatternValues.Gray125 })
                ),
                new Borders(
                    // 0 — Sem borda
                    new Border(
                        new LeftBorder(), new RightBorder(),
                        new TopBorder(), new BottomBorder(),
                        new DiagonalBorder())
                ),
                new CellFormats(
                    // 0 — Normal
                    new CellFormat
                    {
                        FontId = 0, FillId = 0, BorderId = 0
                    },
                    // 1 — Negrito (cabeçalho)
                    new CellFormat
                    {
                        FontId = 1, FillId = 0, BorderId = 0,
                        ApplyFont = true
                    }
                )
            );
        }

        /// <summary>
        /// Ajusta largura das colunas baseado no conteúdo.
        /// </summary>
        private static void AjustarLarguraColunas(
            WorksheetPart worksheetPart,
            ScheduleData dados)
        {
            if (dados.TotalColunas == 0)
                return;

            var columns = new Columns();

            for (int col = 0; col < dados.TotalColunas; col++)
            {
                // Largura baseada no maior conteúdo
                double maxLen = dados.Cabecalhos[col].Length;

                foreach (var linha in dados.Linhas)
                {
                    if (col < linha.Count && linha[col].Length > maxLen)
                        maxLen = linha[col].Length;
                }

                // Mínimo 10, máximo 50, +2 padding
                var width = Math.Min(50, Math.Max(10, maxLen + 2));

                columns.AppendChild(new Column
                {
                    Min = (uint)(col + 1),
                    Max = (uint)(col + 1),
                    Width = width,
                    CustomWidth = true
                });
            }

            worksheetPart.Worksheet.InsertBefore(
                columns,
                worksheetPart.Worksheet.GetFirstChild<SheetData>());
        }

        // ══════════════════════════════════════════════════════════
        //  HELPERS — IO
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// Garante que o diretório do arquivo exista.
        /// </summary>
        private static void GarantirDiretorio(string caminhoArquivo)
        {
            var dir = Path.GetDirectoryName(caminhoArquivo);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }
    }
}
