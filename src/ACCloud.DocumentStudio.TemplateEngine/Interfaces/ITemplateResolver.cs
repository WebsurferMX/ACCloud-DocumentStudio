namespace ACCloud.DocumentStudio.TemplateEngine.Interfaces;

public interface ITemplateResolver
{
    string Resolve(
        string template,
        object data);
}
