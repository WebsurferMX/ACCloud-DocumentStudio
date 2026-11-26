namespace ACCloud.DocumentStudio.Domain.Enums;

public enum TemplateStatus
{
    /// <summary>Tiene versiones, ninguna publicada.</summary>
    Draft = 1,

    /// <summary>Tiene una versión publicada (la que se usa por defecto al generar).</summary>
    Published = 2,

    /// <summary>Eliminada lógicamente; no se lista ni se puede generar.</summary>
    Archived = 3
}
