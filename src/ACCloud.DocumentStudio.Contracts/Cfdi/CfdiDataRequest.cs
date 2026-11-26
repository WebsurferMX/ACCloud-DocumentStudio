namespace ACCloud.DocumentStudio.Contracts.Cfdi;

public sealed class CfdiDataRequest
{
    /// <summary>XML completo del CFDI (3.3 o 4.0).</summary>
    public string Xml { get; set; } = string.Empty;
}
