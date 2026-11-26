using ACCloud.DocumentStudio.TemplateEngine.Expressions;

namespace ACCloud.DocumentStudio.UnitTests;

public class NumberToWordsEsTests
{
    [Theory]
    [InlineData("0.50", "CERO PESOS 50/100 M.N.")]
    [InlineData("1", "UN PESO 00/100 M.N.")]
    [InlineData("21", "VEINTIÚN PESOS 00/100 M.N.")]
    [InlineData("100", "CIEN PESOS 00/100 M.N.")]
    [InlineData("101", "CIENTO UN PESOS 00/100 M.N.")]
    [InlineData("1000", "MIL PESOS 00/100 M.N.")]
    [InlineData("1234.56", "MIL DOSCIENTOS TREINTA Y CUATRO PESOS 56/100 M.N.")]
    [InlineData("21000", "VEINTIÚN MIL PESOS 00/100 M.N.")]
    [InlineData("146635.83", "CIENTO CUARENTA Y SEIS MIL SEISCIENTOS TREINTA Y CINCO PESOS 83/100 M.N.")]
    [InlineData("1000000", "UN MILLÓN DE PESOS 00/100 M.N.")]
    [InlineData("2500100", "DOS MILLONES QUINIENTOS MIL CIEN PESOS 00/100 M.N.")]
    public void Mexican_Pesos(string amount, string expected)
    {
        Assert.Equal(expected, NumberToWordsEs.Amount(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void Dollars()
    {
        Assert.Equal("DIEZ DÓLARES 05/100 USD", NumberToWordsEs.Amount(10.05m, "USD"));
    }
}
