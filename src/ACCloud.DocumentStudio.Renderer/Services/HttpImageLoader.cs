using ACCloud.DocumentStudio.Renderer.Interfaces;

namespace ACCloud.DocumentStudio.Renderer.Services;

/// <summary>
/// Carga imágenes desde data URI, base64 o URL http(s), con límite de tamaño.
/// El timeout se configura en el HttpClient registrado.
/// </summary>
public sealed class HttpImageLoader
    : IImageLoader
{
    public const int MaxBytes = 5 * 1024 * 1024;

    private readonly HttpClient _httpClient;

    public HttpImageLoader(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<byte[]?> LoadAsync(
        string source,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return null;
        }

        source = source.Trim();

        if (source.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = source.IndexOf(',');

            return comma < 0
                ? null
                : TryDecodeBase64(source[(comma + 1)..]);
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            return await DownloadAsync(uri, cancellationToken);
        }

        return TryDecodeBase64(source);
    }

    private async Task<byte[]?> DownloadAsync(
        Uri uri,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(
                uri,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (!response.IsSuccessStatusCode
                || response.Content.Headers.ContentLength > MaxBytes)
            {
                return null;
            }

            await using var stream =
                await response.Content.ReadAsStreamAsync(cancellationToken);

            using var buffer = new MemoryStream();
            var chunk = new byte[81920];
            int read;

            while ((read = await stream.ReadAsync(chunk, cancellationToken)) > 0)
            {
                if (buffer.Length + read > MaxBytes)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
            }

            return buffer.ToArray();
        }
        catch (HttpRequestException)
        {
            return null;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout del HttpClient.
            return null;
        }
    }

    private static byte[]? TryDecodeBase64(string value)
    {
        var buffer = new byte[value.Length];

        return Convert.TryFromBase64String(value, buffer, out var written)
            ? buffer[..written]
            : null;
    }
}
