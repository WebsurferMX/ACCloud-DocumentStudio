using System.Text.Json;
using System.Text.RegularExpressions;
using ACCloud.DocumentStudio.TemplateEngine.Interfaces;

namespace ACCloud.DocumentStudio.TemplateEngine.Services;

public sealed class TemplateResolver
    : ITemplateResolver
{
    public string Resolve(
        string template,
        object data)
    {
        var json = JsonSerializer.Serialize(data);

        using var document =
            JsonDocument.Parse(json);

        var pattern = @"\{\{(.+?)\}\}";

        return Regex.Replace(
            template,
            pattern,
            match =>
            {
                var path =
                    match.Groups[1].Value;

                var parts =
                    path.Split('.');

                JsonElement current =
                    document.RootElement;

                foreach (var part in parts)
                {
                    if (!current.TryGetProperty(
                        part,
                        out current))
                    {
                        return string.Empty;
                    }
                }

                return current.ToString();
            });
    }
}