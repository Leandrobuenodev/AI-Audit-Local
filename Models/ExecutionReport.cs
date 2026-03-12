using System;
using System.Collections.Generic;

namespace PipelineDocAuditor.Models
{
    public class ExecutionReport
    {
        public string ExecutionId { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string AnalysisResult { get; set; } = string.Empty;
        public string RawExtraction { get; set; } = string.Empty;
        public List<List<List<string>>> Tables { get; set; } = new();
        public int ImageCount { get; set; }
        public decimal TotalCosts { get; set; }
        public decimal DocIntelligenceCost { get; set; }
        public decimal OpenAiCost { get; set; }
    }
}