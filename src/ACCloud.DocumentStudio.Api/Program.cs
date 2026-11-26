using ACCloud.DocumentStudio.Api.Services;
using ACCloud.DocumentStudio.Renderer.Interfaces;
using ACCloud.DocumentStudio.Renderer.Services;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Services;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License =
    LicenseType.Community;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // Permite que "type" no sea la primera propiedad de cada elemento.
        options.JsonSerializerOptions.AllowOutOfOrderMetadataProperties = true;
    });

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<
    ITemplateResolver,
    TemplateResolver>();

builder.Services.AddSingleton<
    ITemplateCompiler,
    TemplateCompiler>();

builder.Services.AddHttpClient<
    IImageLoader,
    HttpImageLoader>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
    });

builder.Services.AddScoped<
    IDocumentRenderer,
    QuestPdfRenderer>();

builder.Services.AddSingleton<BuiltInTemplateProvider>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();

app.UseSwaggerUI();

app.MapControllers();

app.Run();
