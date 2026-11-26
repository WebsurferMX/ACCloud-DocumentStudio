using ACCloud.DocumentStudio.Renderer.Interfaces;
using ACCloud.DocumentStudio.Renderer.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace ACCloud.DocumentStudio.Renderer.Services;

public sealed class QuestPdfRenderer
    : IDocumentRenderer
{
    public byte[] Render(
        List<RenderTextElement> elements)
    {
        return Document
            .Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);

                    page.Content()
                        .Column(column =>
                        {
                            foreach (var element in elements)
                            {
                                column.Item()
                                    .Text(element.Text)
                                    .FontSize(element.FontSize);
                            }
                        });
                });
            })
            .GeneratePdf();
    }
}
