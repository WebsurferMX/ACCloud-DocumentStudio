using QuestPDF.Infrastructure;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Services;
using ACCloud.DocumentStudio.Renderer.Interfaces;
using ACCloud.DocumentStudio.Renderer.Services;

QuestPDF.Settings.License =
    LicenseType.Community;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen();

builder.Services.AddScoped<
    ITemplateResolver,
    TemplateResolver>();

builder.Services.AddScoped<
    ITemplateCompiler,
    TemplateCompiler>();

builder.Services.AddScoped<
    IDocumentRenderer,
    QuestPdfRenderer>();

var app = builder.Build();

app.UseSwagger();

app.UseSwaggerUI();

app.MapControllers();

app.Run();