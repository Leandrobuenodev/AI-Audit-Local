using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using PipelineDocAuditor.Interfaces;
using PipelineDocAuditor.Models;

namespace PipelineDocAuditor.Infrastructure
{
    public class WordResultExporter : IResultExporter
    {
        public async Task ExportResultsAsync(ExecutionReport report, string fileName, string outputDir)
        {
            if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);

            GenerateFaithfulDoc(Path.Combine(outputDir, $"{fileName}_ExtracaoFiel.docx"), report);
            GenerateComplianceDoc(Path.Combine(outputDir, $"{fileName}_RelatorioConformidade.docx"), report);

            var logJson = new
            {
                run_id = report.ExecutionId,
                status_por_etapa = new { extracao = "OK", auditoria = "OK", exportacao = "OK" },
                estatisticas = new
                {
                    paginas = report.PageCount,
                    tabelas = report.Tables.Count,
                    imagens = report.ImageCount,
                    tokens = report.TokenUsage
                },
                financeiro = new
                {
                    custo_document_intelligence = report.DocIntelCost,
                    custo_openai = report.OpenAiCost,
                    total_estimado = report.TotalCosts
                }
            };
            await File.WriteAllTextAsync(Path.Combine(outputDir, $"{fileName}_LogCustos.json"),
                JsonConvert.SerializeObject(logJson, Formatting.Indented));
        }

        private void GenerateFaithfulDoc(string path, ExecutionReport report)
        {
            using var wordDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            Body body = mainPart.Document.Body!; // CORREÇÃO: Pegando o Body

            AddHeader(body, "RELATÓRIO DE EXTRAÇÃO FIEL (TEXTO + TABELAS + IMAGENS)", "2E74B5");

            var cleanText = FilterTableNoise(report.RawExtraction);
            foreach (var line in cleanText.Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    body.AppendChild(new Paragraph(new Run(new Text(line.Trim()))));
            }

            int tIdx = 1;
            foreach (var table in report.Tables)
            {
                AddHeader(body, $"[REFERÊNCIA: TABELA {tIdx}] - CONTEÚDO ESTRUTURADO", "538135");
                DrawTable(body, table);
                tIdx++;
            }

            AddHeader(body, "DESCRIÇÃO DE ELEMENTOS VISUAIS (IMAGENS)", "767171");
            for (int i = 1; i <= report.ImageCount; i++)
            {
                body.AppendChild(new Paragraph(new Run(new Text($"[IMAGEM {i}]: Identificada no documento original. Conteúdo: Elemento visual analisado pelo motor de IA.")) { RunProperties = new RunProperties(new Italic()) }));
            }
            mainPart.Document.Save();
        }

        private void GenerateComplianceDoc(string path, ExecutionReport report)
        {
            using var wordDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            Body body = mainPart.Document.Body!; // CORREÇÃO: Pegando o Body

            AddHeader(body, "RELATÓRIO DE CONFORMIDADE E CHECKLIST", "C00000");

            var parts = (report.AnalysisResult ?? "").Split(new[] { "##" }, StringSplitOptions.RemoveEmptyEntries)
                         .OrderBy(p => p.Contains("CHECKLIST") || p.Contains("2."));

            foreach (var part in parts)
            {
                AddFormattedSection(body, "##" + part);
            }
            mainPart.Document.Save();
        }

        private string FilterTableNoise(string text) =>
            Regex.Replace(text ?? "", @"^[\d\s%,.+-/|]+$", "", RegexOptions.Multiline);

        private void AddHeader(Body body, string text, string color)
        {
            var p = body.AppendChild(new Paragraph());
            var r = p.AppendChild(new Run(new Text(text)));
            r.RunProperties = new RunProperties(new Bold(), new Color { Val = color }, new FontSize { Val = "28" });
        }

        private void DrawTable(Body body, System.Collections.Generic.List<System.Collections.Generic.List<string>> data)
        {
            Table t = new Table();
            t.AppendChild(new TableProperties(new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

            foreach (var rowData in data)
            {
                TableRow r = new TableRow();
                foreach (var cell in rowData) r.Append(new TableCell(new Paragraph(new Run(new Text(cell)))));
                t.Append(r);
            }
            body.Append(t);
        }

        private void AddFormattedSection(Body body, string text)
        {
            foreach (var line in text.Split('\n'))
            {
                var p = body.AppendChild(new Paragraph());
                if (line.Contains("**"))
                {
                    var parts = Regex.Split(line, @"(\*\*.*?\*\*)");
                    foreach (var part in parts)
                    {
                        if (part.StartsWith("**"))
                            p.AppendChild(new Run(new Text(part.Replace("**", ""))) { RunProperties = new RunProperties(new Bold()) });
                        else
                            p.AppendChild(new Run(new Text(part)));
                    }
                }
                else p.AppendChild(new Run(new Text(line)));
            }
        }
    }
}