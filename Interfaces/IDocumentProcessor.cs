using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PipelineDocAuditor.Interfaces
{
    public interface IDocumentProcessor
    {
        Task<string> ProcessDocumentAsync(Stream stream);
        Task<(string text, List<List<List<string>>> tables, int imageCount)> ProcessLayoutAsync(Stream stream);
    }
}