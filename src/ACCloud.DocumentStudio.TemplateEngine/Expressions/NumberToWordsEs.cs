using System.Text;

namespace ACCloud.DocumentStudio.TemplateEngine.Expressions;

/// <summary>
/// Importe con letra en español, formato usual de CFDI:
/// <c>MIL DOSCIENTOS TREINTA Y CUATRO PESOS 56/100 M.N.</c>
/// </summary>
public static class NumberToWordsEs
{
    private static readonly string[] UpToTwentyNine =
    [
        "CERO", "UN", "DOS", "TRES", "CUATRO", "CINCO", "SEIS", "SIETE", "OCHO", "NUEVE",
        "DIEZ", "ONCE", "DOCE", "TRECE", "CATORCE", "QUINCE", "DIECISÉIS", "DIECISIETE",
        "DIECIOCHO", "DIECINUEVE", "VEINTE", "VEINTIÚN", "VEINTIDÓS", "VEINTITRÉS",
        "VEINTICUATRO", "VEINTICINCO", "VEINTISÉIS", "VEINTISIETE", "VEINTIOCHO", "VEINTINUEVE"
    ];

    private static readonly string[] Tens =
    [
        "", "", "", "TREINTA", "CUARENTA", "CINCUENTA", "SESENTA", "SETENTA", "OCHENTA", "NOVENTA"
    ];

    private static readonly string[] Hundreds =
    [
        "", "CIENTO", "DOSCIENTOS", "TRESCIENTOS", "CUATROCIENTOS", "QUINIENTOS",
        "SEISCIENTOS", "SETECIENTOS", "OCHOCIENTOS", "NOVECIENTOS"
    ];

    public static string Amount(decimal amount, string? currency = "MXN")
    {
        amount = Math.Round(Math.Abs(amount), 2, MidpointRounding.AwayFromZero);

        var integer = (long)Math.Truncate(amount);
        var cents = (int)((amount - integer) * 100);

        var (singular, plural, suffix) = (currency ?? "MXN").Trim().ToUpperInvariant() switch
        {
            "MXN" or "" => ("PESO", "PESOS", "M.N."),
            "USD" => ("DÓLAR", "DÓLARES", "USD"),
            "EUR" => ("EURO", "EUROS", "EUR"),
            var other => (other, other, other)
        };

        var words = Words(integer);
        var exactMillions = integer >= 1_000_000 && integer % 1_000_000 == 0;
        var unit = integer == 1 ? singular : plural;

        return $"{words}{(exactMillions ? " DE" : string.Empty)} {unit} {cents:00}/100 {suffix}";
    }

    public static string Words(long number)
    {
        if (number == 0)
        {
            return "CERO";
        }

        var builder = new StringBuilder();

        var millions = number / 1_000_000;
        var thousands = number % 1_000_000 / 1000;
        var rest = number % 1000;

        if (millions > 0)
        {
            builder.Append(millions == 1 ? "UN MILLÓN" : $"{Words(millions)} MILLONES");
        }

        if (thousands > 0)
        {
            Append(builder, thousands == 1 ? "MIL" : $"{BelowThousand((int)thousands)} MIL");
        }

        if (rest > 0)
        {
            Append(builder, BelowThousand((int)rest));
        }

        return builder.ToString();
    }

    private static string BelowThousand(int number)
    {
        if (number == 100)
        {
            return "CIEN";
        }

        var hundreds = number / 100;
        var rest = number % 100;
        var builder = new StringBuilder(Hundreds[hundreds]);

        if (rest > 0)
        {
            Append(builder, BelowHundred(rest));
        }

        return builder.ToString();
    }

    private static string BelowHundred(int number)
    {
        if (number < 30)
        {
            return UpToTwentyNine[number];
        }

        var units = number % 10;

        return units == 0
            ? Tens[number / 10]
            : $"{Tens[number / 10]} Y {UpToTwentyNine[units]}";
    }

    private static void Append(StringBuilder builder, string words)
    {
        if (builder.Length > 0)
        {
            builder.Append(' ');
        }

        builder.Append(words);
    }
}
