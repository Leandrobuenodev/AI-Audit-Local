using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;
using PipelineDocAuditor.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PipelineDocAuditor.Services
{
    public class DocumentIntelligenceProcessor : IDocumentProcessor
    {
        private readonly DocumentAnalysisClient _client;

        public DocumentIntelligenceProcessor(string endpoint, string key)
        {
            _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
        }

        public async Task<(string text, List<List<List<string>>> tables, int imageCount)> ProcessLayoutAsync(Stream stream)
        {
            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", stream);
            var result = operation.Value;

            var contentBuilder = new StringBuilder();
            foreach (var para in result.Paragraphs)
            {
                string content = para.Content.Trim();
                if (IsNoise(content)) continue;

                string prefix = para.Role.ToString() switch
                {
                    "title" => "[H1]",
                    "sectionHeading" => "[H2]",
                    _ => ""
                };
                contentBuilder.AppendLine($"{prefix}{content}");
            }

            var tablesData = new List<List<List<string>>>();
            foreach (var table in result.Tables)
            {
                var rows = new List<List<string>>();
                for (int i = 0; i < table.RowCount; i++) rows.Add(new List<string>(new string[table.ColumnCount]));
                foreach (var cell in table.Cells) rows[cell.RowIndex][cell.ColumnIndex] = cell.Content;
                tablesData.Add(rows);
            }

            return (contentBuilder.ToString(), tablesData, result.Styles?.Count ?? 0);
        }

        private bool IsNoise(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            return Regex.IsMatch(text, @"^[\d\s.,%T\-]+$") && text.Length < 15;
        }

        public async Task<string> ProcessDocumentAsync(Stream stream) => (await ProcessLayoutAsync(stream)).text;
    }
}