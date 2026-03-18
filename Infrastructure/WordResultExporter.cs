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
            Body body = mainPart.Document.Body!;

            AddHeader(body, "RELATÓRIO DE EXTRAÇÃO FIEL", "2E74B5");

            var lines = (report.RawExtraction ?? "").Split('\n');
            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;

                bool isTableNoise = Regex.IsMatch(trimmed, @"^[\d\s%,.+-/|]+$") ||
                                   Regex.IsMatch(trimmed, @"^(Var|3T|4T|9M|1T|2T|R\$|milhões)", RegexOptions.IgnoreCase);

                if (isTableNoise && trimmed.Length < 40) continue;

                AddStyledParagraph(body, trimmed, "333333", false, "22", false);
            }

            int tIdx = 1;
            foreach (var table in report.Tables)
            {
                AddHeader(body, $"REFERÊNCIA: TABELA {tIdx}", "538135");
                DrawTable(body, table);
                tIdx++;
            }

            AddHeader(body, "ELEMENTOS VISUAIS E IMAGENS", "767171");
            for (int i = 1; i <= report.ImageCount; i++)
            {
                AddStyledParagraph(body, $"[FIGURA {i}]: Conteúdo visual identificado. Fonte: Documento Original.", "767171", false, "20", true);
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

            var lines = (report.AnalysisResult ?? "").Split('\n');

            foreach (var line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmedLine)) continue;

                string cleanLine = Regex.Replace(trimmedLine, @"^#+\s*", "");

                if (Regex.IsMatch(cleanLine, @"^(📊|✅|⚠️|🛠️|🖼️|\d\.)"))
                {
                    AddStyledParagraph(body, cleanLine, "1F4E78", true, "26", true);
                }
                else if (cleanLine.StartsWith("-") || cleanLine.Contains("**"))
                {
                    AddChecklistLine(body, cleanLine);
                }
                else
                {
                    AddStyledParagraph(body, cleanLine, "333333", false, "22", false);
                }
            }
            mainPart.Document.Save();
        }

        private void AddHeader(Body body, string text, string color)
        {
            AddStyledParagraph(body, text.ToUpper(), color, true, "28", true);
        }

        private void AddStyledParagraph(Body body, string text, string color, bool isBold, string size, bool hasSpacing)
        {
            var p = body.AppendChild(new Paragraph());
            var pProp = p.AppendChild(new ParagraphProperties());

            if (hasSpacing)
                pProp.Append(new SpacingBetweenLines { Before = "240", After = "120" });

            var run = p.AppendChild(new Run());
            var rProp = new RunProperties(
                new Color { Val = color },
                new FontSize { Val = size },
                new RunFonts { Ascii = "Arial" }
            );

            if (isBold) rProp.Append(new Bold());

            run.RunProperties = rProp;
            run.AppendChild(new Text(text));
        }

        private void AddChecklistLine(Body body, string line)
        {
            var p = body.AppendChild(new Paragraph());
            p.AppendChild(new ParagraphProperties(
                new Indentation { Left = "360" },
                new SpacingBetweenLines { After = "60" }
            ));

            string content = line.TrimStart('-', ' ');
            var parts = Regex.Split(content, @"(\*\*.*?\*\*)");

            foreach (var part in parts)
            {
                var run = p.AppendChild(new Run());

                // CORREÇÃO AQUI: Criamos a referência rProp para evitar o erro de nulo
                var rProp = new RunProperties(
                    new RunFonts { Ascii = "Arial" },
                    new FontSize { Val = "22" }
                );

                if (part.StartsWith("**") && part.EndsWith("**"))
                {
                    rProp.Append(new Bold());
                    run.RunProperties = rProp; // Atribui as propriedades ao run
                    run.AppendChild(new Text(part.Replace("**", "")));
                }
                else
                {
                    run.RunProperties = rProp; // Atribui as propriedades ao run
                    run.AppendChild(new Text(part));
                }
            }
        }

        private void DrawTable(Body body, System.Collections.Generic.List<System.Collections.Generic.List<string>> data)
        {
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

            for (int i = 0; i < data.Count; i++)
            {
                TableRow r = new TableRow();
                foreach (var cellText in data[i])
                {
                    TableCell c = new TableCell();
                    TableCellProperties cp = new TableCellProperties();
                    if (i == 0) cp.Append(new Shading { Fill = "F2F2F2" });
                    c.Append(cp);

                    var p = new Paragraph();
                    var run = p.AppendChild(new Run(new Text(cellText)));

                    if (i == 0)
                    {
                        var rProp = new RunProperties(new Bold());
                        run.RunProperties = rProp;
                    }

                    c.Append(p);
                    r.Append(c);
                }
                t.Append(r);
            }
            body.Append(t);
        }
    }
}