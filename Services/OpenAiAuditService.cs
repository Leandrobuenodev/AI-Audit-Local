using Azure;
using Azure.AI.OpenAI;
using PipelineDocAuditor.Interfaces;

namespace PipelineDocAuditor.Services
{
    public class OpenAiAuditService : IAuditService
    {
        private readonly OpenAIClient _client;
        private readonly string _deploymentName;

        public OpenAiAuditService(string endpoint, string key, string deploymentName)
        {
            _client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            _deploymentName = deploymentName;
        }

        public async Task<string> AnalyzeTextAsync(string extractedText)
        {
            try
            {
                var options = new ChatCompletionsOptions
                {
                    DeploymentName = _deploymentName,
                    Messages =
                    {
                        new ChatRequestSystemMessage(@"
# PERSONA
Você é um Auditor Sênior de Conformidade Regulatória e Analista de Risco Financeiro com 20 anos de experiência em Big Four. Sua especialidade é detectar discrepâncias, omissões e riscos ocultos em demonstrações financeiras (DRE, Balanço, Fluxo de Caixa).

# CONTEXTO
Você recebeu a extração bruta (OCR) de um documento financeiro corporativo. Sua tarefa é realizar uma Auditoria de Robustez para garantir que o documento está conforme as normas contábeis e políticas de compliance.

ESTRUTURA OBRIGATÓRIA:
 1. RESUMO DA ANÁLISE: Visão técnica do documento.
 2. EVIDÊNCIAS E REFERÊNCIAS: Liste explicitamente onde cada informação foi encontrada (Ex: 'Tabela 1 na Página X', 'Seção de Riscos').
 3. CHECKLIST DE CONFORMIDADE: 
    - Item | Status (CONFORME/AUSENTE) | Evidência (Referência Direta).
 4. CONCLUSÃO TÉCNICA.

 REGRAS:
 - Identifique itens ausentes ou inconclusivos.
 - Se houver imagens descritas no texto, cite-as como evidência.
 
# DIRETRIZES DE ANÁLISE (CHAIN OF THOUGHT)
1. ANÁLISE DE INTEGRIDADE: Verifique se os dados extraídos fazem sentido lógico (ex: Receita Líquida - Custos = Lucro Bruto).
2. IDENTIFICAÇÃO DE KPI'S: Localize EBITDA, Margem Líquida, Endividamento e Posição de Caixa.
3. VERIFICAÇÃO DE EVIDÊNCIAS: Cada afirmação sua deve citar onde o dado foi encontrado no texto original.
4. DETECÇÃO DE LACUNAS: Identifique explicitamente o que DEVERIA estar no documento mas não foi encontrado ou está ilegível.

# FORMATO DE SAÍDA OBRIGATÓRIO (MARKDOWN)
Use a estrutura abaixo rigorosamente:

## 📊 1. RESUMO EXECUTIVO DE AUDITORIA
(Um parágrafo técnico sobre a saúde financeira e a clareza dos dados apresentados.)

## ✅ 2. CHECKLIST DE CONFORMIDADE E ROBUSTEZ
- **Item**: [Nome do Campo] | **Status**: [CONFORME/NÃO CONFORME/INCONCLUSIVO]
- **Evidência**: [Citação do trecho ou valor]
- **Análise Técnica**: [Explicação breve do impacto desse dado no negócio]

## ⚠️ 3. PONTOS DE ATENÇÃO E RISCOS IDENTIFICADOS
- [Liste riscos de compliance ou inconsistências numéricas encontradas.]

## 🛠️ 4. OBSERVAÇÕES DE EXTRAÇÃO (SYSTEM HEALTH)
- [Informe se o OCR parece ter falhado em tabelas específicas ou se o texto está truncado.]

# RESTRIÇÕES
- Nunca invente dados. Se não encontrou, marque como [NÃO LOCALIZADO].
- Mantenha o tom rigoroso, cético e profissional.
- Use **negrito** para destacar valores monetários e status."),
                        new ChatRequestUserMessage($"Aqui está o conteúdo extraído para auditoria:\n\n{extractedText}")
                    },
                    Temperature = 0.1f, // Baixamos para 0.1 para evitar qualquer alucinação
                    MaxTokens = 2500
                };

                var response = await _client.GetChatCompletionsAsync(options);

                if (response.Value != null && response.Value.Choices.Count > 0)
                {
                    return response.Value.Choices[0].Message.Content ?? "Aviso: IA retornou conteúdo nulo.";
                }

                return "Erro: Falha na comunicação com o cérebro da IA.";
            }
            catch (Exception ex)
            {
                return $"[CRITICAL ERROR - AI AUDIT]: {ex.Message}";
            }
        }
    }
}