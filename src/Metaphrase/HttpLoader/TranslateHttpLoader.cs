using Metaphrase.Abstract;
using Metaphrase.Primitives;
using System.Net.Http.Json;
using System.Text.Json;

namespace Metaphrase.HttpLoader;

/// <summary>
/// A loader that retrieves translations from an HTTP server.
/// </summary>
public sealed class TranslateHttpLoader : TranslateLoader
{
    private readonly HttpClient httpClient;
    private readonly TranslateHttpLoaderOptions options;

    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateHttpLoader"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client used to make requests.</param>
    /// <param name="options">The options for configuring the loader.</param>
    public TranslateHttpLoader(HttpClient httpClient, TranslateHttpLoaderOptions? options = null)
    {
        this.httpClient = httpClient;
        this.options = options ?? new();
    }

    /// <inheritdoc/>
    public override Observable<Translations> GetTranslation(string lang)
    {
        return Observable
            .FromAsync(token => new ValueTask<JsonElement>(httpClient.GetFromJsonAsync<JsonElement>($"{options.Prefix}{lang}{options.Suffix}", token)))
            .Select(static json => Translations.FromJson(json));
    }
}
