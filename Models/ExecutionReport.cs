using System;
using System.Collections.Generic;

namespace PipelineDocAuditor.Models
{
    public class ExecutionReport
    {
        public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; } = string.Empty;
        public string RawExtraction { get; set; } = string.Empty;
        public string AnalysisResult { get; set; } = string.Empty;
        public string Status { get; set; } = "Success";
        public List<List<List<string>>> Tables { get; set; } = new();

        // Propriedades que estavam faltando e causando o erro:
        public int PageCount { get; set; }
        public int ImageCount { get; set; }
        public int TokenUsage { get; set; }
        public decimal DocIntelCost { get; set; }
        public decimal OpenAiCost { get; set; }
        public decimal TotalCosts { get; set; }
    }
}