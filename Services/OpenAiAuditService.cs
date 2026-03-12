using Azure;
using Azure.AI.OpenAI;
using PipelineDocAuditor.Interfaces;
using System;
using System.Threading.Tasks;

namespace PipelineDocAuditor.Services
{
    public class OpenAiAuditService : IAuditService
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;

        public OpenAiAuditService(string endpoint, string key, string deployment)
        {
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            _deploymentName = deployment;
        }

        public async Task<string> AnalyzeTextAsync(string text)
        {
            var options = new ChatCompletionsOptions()
            {
                DeploymentName = _deploymentName,
                Messages =
                {
                    new ChatRequestSystemMessage(@"Você é um Auditor Executivo Sênior.
Sua missão é criar um Relatório de Conformidade LEVÍVEL e ESTRUTURADO.

REGRAS DE FORMATAÇÃO:
1. Use [BOLD]Título[BOLD] para títulos.
2. Use listas (•) para itens identificados.
3. NÃO use Markdown (* ou #).

CONTEÚDO OBRIGATÓRIO:
- [BOLD]RESUMO EXECUTIVO[BOLD]: Texto fluido sobre a saúde financeira.
- [BOLD]ANÁLISE DE DADOS (TABELAS)[BOLD]: Resuma os números mais importantes encontrados nas tabelas (ex: Lucro, Ebitda, PDD).
- [BOLD]CHECKLIST DE CONFORMIDADE[BOLD]: Liste o que foi encontrado e o que falta.
- [BOLD]IMAGENS E EVIDÊNCIAS[BOLD]: Descreva o contexto das figuras detectadas."),
                    new ChatRequestUserMessage(text)
                },
                Temperature = 0.3f
            };

            var response = await _client.GetChatCompletionsAsync(options);
            return response.Value.Choices[0].Message.Content;
        }
    }
}