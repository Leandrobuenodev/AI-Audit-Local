using System.Threading.Tasks;
using PipelineDocAuditor.Models;

namespace PipelineDocAuditor.Interfaces
{
    public interface IResultExporter
    {
        Task ExportResultsAsync(ExecutionReport report, string fileName, string outputDir);
    }
}