using ACCloud.DocumentStudio.Application.Documents;
using ACCloud.DocumentStudio.Application.Templates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ACCloud.DocumentStudio.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDocumentStudioApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<ITemplateService, TemplateService>();
        services.AddScoped<IDocumentGenerator, DocumentGenerator>();
        services.AddScoped<IDocumentService, DocumentService>();

        return services;
    }
}
