using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace PipelineDocAuditor.Services
{
    public class DocumentIntelligenceProcessor
    {
        private readonly DocumentAnalysisClient _client;

        public DocumentIntelligenceProcessor(string endpoint, string key)
        {
            _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(key));
        }

        public async Task<(string Content, List<List<List<string>>> Tables)> ProcessDocumentAsync(Stream stream)
        {
            // Usamos o modelo 'prebuilt-layout' que é o melhor para tabelas
            var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, "prebuilt-layout", stream);
            var result = operation.Value;

            var tables = new List<List<List<string>>>();

            // Extração de Tabelas Reais
            foreach (var table in result.Tables)
            {
                var tableRows = new List<List<string>>();
                for (int i = 0; i < table.RowCount; i++)
                {
                    var rowCells = new List<string>();
                    for (int j = 0; j < table.ColumnCount; j++)
                    {
                        // Busca a célula correspondente
                        var cell = table.Cells.FirstOrDefault(c => c.RowIndex == i && c.ColumnIndex == j);
                        rowCells.Add(cell?.Content ?? "");
                    }
                    tableRows.Add(rowCells);
                }
                tables.Add(tableRows);
            }

            return (result.Content, tables);
        }
    }
}