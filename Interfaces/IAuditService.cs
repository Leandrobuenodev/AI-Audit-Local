using System.Threading.Tasks;
namespace PipelineDocAuditor.Interfaces
{
    public interface IAuditService { Task<string> AnalyzeTextAsync(string text); }
}