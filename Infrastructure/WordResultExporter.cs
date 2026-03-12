using Xceed.Words.NET;
using Xceed.Document.NET;
using PipelineDocAuditor.Interfaces;
using PipelineDocAuditor.Models;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PipelineDocAuditor.Infrastructure
{
    public class WordResultExporter : IResultExporter
    {
        public async Task ExportResultsAsync(ExecutionReport report, string fileName, string outputPath)
        {
            if (!Directory.Exists(outputPath)) Directory.CreateDirectory(outputPath);
            string safeName = Regex.Replace(Path.GetFileNameWithoutExtension(fileName), @"[:<>|?*\\/\r\n]", "_");

            // EXTRAÇÃO FIEL
            using (var docRaw = DocX.Create(Path.Combine(outputPath, $"{safeName}_ExtracaoFiel.docx")))
            {
                var title = docRaw.InsertParagraph("EXTRAÇÃO FIEL DE DADOS ESTRUTURADOS").FontSize(16).Bold();
                title.Alignment = Alignment.center;

                var lines = report.RawExtraction.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var p = docRaw.InsertParagraph();
                    if (line.StartsWith("[H1]"))
                    {
                        p.Append(line.Replace("[H1]", "")).FontSize(14).Bold();
                        p.SpacingBefore(20);
                    }
                    else if (line.StartsWith("[H2]"))
                    {
                        p.Append(line.Replace("[H2]", "")).FontSize(12).Bold();
                        p.SpacingBefore(12);
                    }
                    else
                    {
                        p.Append(line).FontSize(10);
                        p.Alignment = Alignment.both;
                        p.SpacingAfter(6);
                    }
                }

                if (report.Tables?.Any() == true)
                {
                    docRaw.InsertParagraph("\nTABELAS DETECTADAS").Bold().FontSize(14).SpacingBefore(30);
                    for (int i = 0; i < report.Tables.Count; i++)
                    {
                        var tableData = report.Tables[i];
                        if (tableData.Count == 0 || tableData[0].All(string.IsNullOrWhiteSpace)) continue;
                        docRaw.InsertParagraph($"Tabela {i + 1}").Italic().SpacingBefore(10);
                        var t = docRaw.AddTable(tableData.Count, tableData[0].Count);
                        t.Design = TableDesign.TableGrid;
                        for (int r = 0; r < tableData.Count; r++)
                            for (int c = 0; c < tableData[r].Count; c++)
                                t.Rows[r].Cells[c].Paragraphs[0].Append(tableData[r][c]?.Trim() ?? "");
                        docRaw.InsertTable(t);
                    }
                }
                docRaw.Save();
            }

            // CONFORMIDADE
            using (var doc = DocX.Create(Path.Combine(outputPath, $"{safeName}_Conformidade.docx")))
            {
                doc.InsertParagraph("RELATÓRIO DE CONFORMIDADE").FontSize(16).Bold().Alignment = Alignment.center;
                string formatted = PipelineDocAuditor.Services.DocumentFormatter.PrepareBoldForWord(report.AnalysisResult);
                string[] parts = formatted.Split(new[] { "[BOLD]" }, System.StringSplitOptions.None);
                var p = doc.InsertParagraph();
                for (int i = 0; i < parts.Length; i++)
                {
                    if (i % 2 != 0) p.Append(parts[i]).Bold();
                    else p.Append(parts[i]);
                }
                doc.Save();
            }
            await Task.CompletedTask;
        }
    }
}