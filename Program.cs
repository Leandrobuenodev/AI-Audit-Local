using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using PipelineDocAuditor.Services;
using PipelineDocAuditor.Interfaces;
using PipelineDocAuditor.Infrastructure;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Garante que o Worker não morra imediatamente se o Azurite oscilar
        services.AddHttpClient();

        services.AddSingleton(sp =>
            new DocumentIntelligenceProcessor(
                context.Configuration["AzureDocIntelligenceEndpoint"] ?? "",
                context.Configuration["AzureDocIntelligenceKey"] ?? ""
            ));

        services.AddSingleton<IAuditService>(sp =>
            new OpenAiAuditService(
                context.Configuration["AzureOpenAIEndpoint"] ?? "",
                context.Configuration["AzureOpenAIKey"] ?? "",
                context.Configuration["AzureOpenAIDeployment"] ?? ""
            ));

        services.AddSingleton<IResultExporter, WordResultExporter>();
    })
    .Build();

host.Run();