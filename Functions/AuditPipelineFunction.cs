using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using PipelineDocAuditor.Interfaces;
using PipelineDocAuditor.Models;
using PipelineDocAuditor.Services;

namespace PipelineDocAuditor.Functions
{
    public class AuditPipelineFunction
    {
        private readonly ILogger _logger;
        private readonly DocumentIntelligenceProcessor _docProcessor;
        private readonly IAuditService _auditService;
        private readonly IResultExporter _exporter;

        public AuditPipelineFunction(ILoggerFactory loggerFactory, DocumentIntelligenceProcessor doc, IAuditService audit, IResultExporter exp)
        {
            _logger = loggerFactory.CreateLogger<AuditPipelineFunction>();
            _docProcessor = doc;
            _auditService = audit;
            _exporter = exp;
        }

        [Function("AnalyzeDocument")]
        public async Task Run([BlobTrigger("uploads/{fileName}")] Stream blobStream, string fileName)
        {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var outputDir = Path.Combine(userProfile, "Downloads", "AuditoriaSaida");
            string lockFilePath = Path.Combine(outputDir, $"{fileName}.lock");

            try
            {
                if (!Directory.Exists(outputDir)) Directory.CreateDirectory(outputDir);
                if (File.Exists(lockFilePath)) return;

                _logger.LogInformation($"[START] {fileName}");

                // 1. Extração estruturada
                var result = await _docProcessor.ProcessLayoutAsync(blobStream);

                // 2. Análise de IA
                string auditorPrompt = "Aja como um Auditor Financeiro Sênior Cético. Compare o texto com as tabelas.\n" +
                    "Aponte inconsistências e falta de conformidade rigorosamente.\n\n" + result.text;

                string analysis = await _auditService.AnalyzeTextAsync(auditorPrompt);

                // 3. Montagem do Report com Custos
                var report = new ExecutionReport
                {
                    ExecutionId = Guid.NewGuid().ToString(),
                    FileName = fileName,
                    AnalysisResult = analysis,
                    RawExtraction = result.text,
                    Tables = result.tables,
                    ImageCount = result.imageCount,
                    DocIntelligenceCost = 0.06m,
                    OpenAiCost = 0.02m,
                    TotalCosts = 0.08m
                };

                // 4. Exportação DOCX
                await _exporter.ExportResultsAsync(report, fileName, outputDir);

                // 5. GERAÇÃO DO JSON (A peça que faltava) [cite: 2026-03-10]
                string jsonPath = Path.Combine(outputDir, $"{Path.GetFileNameWithoutExtension(fileName)}_log.json");
                var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
                await File.WriteAllTextAsync(jsonPath, JsonSerializer.Serialize(report, jsonOptions));

                // 6. Trava de Idempotência
                await File.WriteAllTextAsync(lockFilePath, DateTime.Now.ToString());

                _logger.LogInformation($"[SUCCESS] Entrega completa: DOCX e JSON gerados em {outputDir}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[ERROR] {ex.Message}");
            }
        }
    }
}