using System.Text.RegularExpressions;

namespace PipelineDocAuditor.Services
{
    public static class DocumentFormatter
    {
        public static string CleanRawText(string text)
        {
            // Remove quebras de linha que deixam o texto pulando
            return Regex.Replace(text, @"\r\n?|\n", " ").Trim();
        }

        public static string PrepareBoldForWord(string aiResult)
        {
            // Troca asteriscos pelo marcador que o seu Word entende
            return aiResult.Replace("**", "[BOLD]");
        }
    }
}