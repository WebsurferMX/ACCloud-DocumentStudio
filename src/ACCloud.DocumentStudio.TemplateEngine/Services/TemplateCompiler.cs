using ACCloud.DocumentStudio.Renderer.Models;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;
using ACCloud.DocumentStudio.TemplateEngine.Models;

namespace ACCloud.DocumentStudio.TemplateEngine.Services;

public sealed class TemplateCompiler
    : ITemplateCompiler
{
    private readonly ITemplateResolver _resolver;

    public TemplateCompiler(
        ITemplateResolver resolver)
    {
        _resolver = resolver;
    }

    public List<RenderTextElement> Compile(
        TemplateDefinition template,
        object data)
    {
        var result =
            new List<RenderTextElement>();

        foreach (var element in template.Elements)
        {
            if (element is TextElementDefinition textElement)
            {
                var resolvedText =
                    _resolver.Resolve(
                        textElement.Value,
                        data);

                result.Add(
                    new RenderTextElement
                    {
                        Text = resolvedText,
                        X = textElement.X,
                        Y = textElement.Y,
                        Width = textElement.Width,
                        Height = textElement.Height,
                        FontSize = textElement.FontSize
                    });
            }
        }

        return result;
    }
}