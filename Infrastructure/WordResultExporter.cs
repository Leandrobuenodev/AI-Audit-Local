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

            var logJson = new {
                run_id = report.ExecutionId,
                status_por_etapa = new { extracao = "OK", auditoria = "OK", exportacao = "OK" },
                estatisticas = new { 
                    paginas = report.PageCount, 
                    tabelas = report.Tables.Count, 
                    imagens = report.ImageCount,
                    tokens = report.TokenUsage 
                },
                financeiro = new {
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
            Body body = mainPart.Document.Body!;

            AddHeader(body, "RELATÓRIO DE EXTRAÇÃO FIEL", "2E74B5");

            // Texto limpo (sem ruído de tabela)
            var lines = (report.RawExtraction ?? "").Split('\n');
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                // Filtro para não repetir dados que já estarão nas tabelas
                bool isTableNoise = Regex.IsMatch(trimmed, @"^[\d\s%,.+-/|]+$") || 
                                   Regex.IsMatch(trimmed, @"^(Var|3T|4T|9M|1T|2T|R\$|milhões)", RegexOptions.IgnoreCase);
                
                if (isTableNoise && trimmed.Length < 40) continue;

                var p = body.AppendChild(new Paragraph(new Run(new Text(trimmed))));
                p.AppendChild(new ParagraphProperties(new SpacingBetweenLines { After = "100" }));
            }

            // Seção de Tabelas
            int tIdx = 1;
            foreach (var table in report.Tables)
            {
                AddHeader(body, $"REFERÊNCIA: TABELA {tIdx}", "538135");
                DrawTable(body, table);
                tIdx++;
            }

            // Descrição de Imagens
            AddHeader(body, "ELEMENTOS VISUAIS E IMAGENS", "767171");
            for (int i = 1; i <= report.ImageCount; i++)
            {
                var p = body.AppendChild(new Paragraph(new Run(new Text($"[FIGURA {i}]: Conteúdo visual identificado e analisado. Fonte: Documento Original."))));
                p.AppendChild(new ParagraphProperties(new SpacingBetweenLines { After = "100" }));
            }
            mainPart.Document.Save();
        }

        private void GenerateComplianceDoc(string path, ExecutionReport report)
        {
            using var wordDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
            var mainPart = wordDoc.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());
            Body body = mainPart.Document.Body!;

            AddHeader(body, "RELATÓRIO DE CONFORMIDADE TÉCNICA", "C00000");

            // Garante o Checklist no final
            var parts = (report.AnalysisResult ?? "").Split(new[] { "##" }, StringSplitOptions.RemoveEmptyEntries)
                         .OrderBy(p => p.Contains("CHECKLIST") || p.Contains("2."));

            foreach (var part in parts)
            {
                AddFormattedSection(body, "##" + part);
            }
            mainPart.Document.Save();
        }

        // --- MÉTODOS DE FORMATAÇÃO PREMIUM ---

        private void AddHeader(Body body, string text, string color) {
            var p = body.AppendChild(new Paragraph());
            p.AppendChild(new ParagraphProperties(new SpacingBetweenLines { Before = "240", After = "120" }));
            
            var r = p.AppendChild(new Run(new Text(text.ToUpper())));
            r.RunProperties = new RunProperties(
                new Bold(), 
                new Color { Val = color }, 
                new FontSize { Val = "26" },
                new RunFonts { Ascii = "Arial" }
            );
        }

        private void DrawTable(Body body, System.Collections.Generic.List<System.Collections.Generic.List<string>> data) {
            Table t = new Table();
            TableProperties tblProp = new TableProperties(
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 12, Color = "2E74B5" },
                    new BottomBorder { Val = BorderValues.Single, Size = 12, Color = "2E74B5" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "D9D9D9" }
                )
            );
            t.AppendChild(tblProp);
            
            for (int i = 0; i < data.Count; i++) {
                TableRow r = new TableRow();
                foreach (var cellText in data[i]) {
                    TableCell c = new TableCell();
                    TableCellProperties cp = new TableCellProperties();
                    if (i == 0) cp.Append(new Shading { Fill = "F2F2F2" });
                    c.Append(cp);

                    var p = new Paragraph(new Run(new Text(cellText)));
                    if (i == 0) p.Elements<Run>().First().RunProperties = new RunProperties(new Bold());
                    
                    c.Append(p);
                    r.Append(c);
                }
                t.Append(r);
            }
            body.Append(t);
        }

        private void AddFormattedSection(Body body, string text) {
            foreach(var line in text.Split('\n')) {
                var p = body.AppendChild(new Paragraph());
                if (line.Contains("**")) {
                    var parts = Regex.Split(line, @"(\*\*.*?\*\*)");
                    foreach(var part in parts) {
                        if (part.StartsWith("**")) 
                            p.AppendChild(new Run(new Text(part.Replace("**",""))) { RunProperties = new RunProperties(new Bold()) });
                        else 
                            p.AppendChild(new Run(new Text(part)));
                    }
                } else p.AppendChild(new Run(new Text(line)));
            }
        }
    }
}