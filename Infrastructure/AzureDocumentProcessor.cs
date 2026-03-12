using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PipelineDocAuditor.Interfaces;

namespace PipelineDocAuditor.Infrastructure
{
    public class AzureDocumentProcessor : IDocumentProcessor
    {
        public AzureDocumentProcessor(string endpoint, string key) { } // Aceita as strings agora [cite: 2026-03-09]
        public Task<string> ProcessDocumentAsync(Stream stream) => Task.FromResult("Legado");
        public Task<(string text, List<List<List<string>>> tables, int imageCount)> ProcessLayoutAsync(Stream stream) => Task.FromResult(("", new List<List<List<string>>>(), 0));
    }
}