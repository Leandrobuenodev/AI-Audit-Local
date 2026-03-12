using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using PipelineDocAuditor.Interfaces;
using PipelineDocAuditor.Infrastructure;
using PipelineDocAuditor.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        IConfiguration config = context.Configuration;

        services.AddSingleton<DocumentIntelligenceProcessor>(sp =>
        {
            return new DocumentIntelligenceProcessor(config["AzureDocIntelligenceEndpoint"] ?? "", config["AzureDocIntelligenceKey"] ?? "");
        });

        services.AddSingleton<IDocumentProcessor>(sp =>
        {
            return new AzureDocumentProcessor(config["AzureDocIntelligenceEndpoint"] ?? "", config["AzureDocIntelligenceKey"] ?? "");
        });

        services.AddScoped<IAuditService>(sp =>
        {
            return new OpenAiAuditService(config["AzureOpenAIEndpoint"] ?? "", config["AzureOpenAIKey"] ?? "", config["AzureOpenAIDeployment"] ?? "");
        });

        services.AddSingleton<IResultExporter, WordResultExporter>();
    })
    .Build();

host.Run();