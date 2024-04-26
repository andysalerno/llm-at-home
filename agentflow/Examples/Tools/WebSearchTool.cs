using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using AgentFlow.LlmClient;
using AgentFlow.Tools;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Examples.Tools;

public class WebSearchTool : ITool
{
    private const string Uri = "https://www.googleapis.com/customsearch/v1";
    private const string SearchKeyEnvVarName = "SEARCH_KEY";
    private const string SearchKeyCxEnvVarName = "SEARCH_KEY_CX";
    private readonly IEmbeddingsClient embeddingsClient;
    private readonly IScraperClient scraperClient;
    private readonly IHttpClientFactory httpClientFactory;

    public WebSearchTool(IEmbeddingsClient embeddingsClient, IScraperClient scraperClient, IHttpClientFactory httpClientFactory)
    {
        this.embeddingsClient = embeddingsClient;
        this.scraperClient = scraperClient;
        this.httpClientFactory = httpClientFactory;
    }

    public string Name { get; } = "search_web";

    public string Definition { get; } =
@"def search_web(query: str) -> str:
    """"""
    Searches the web using Google for the given query and returns the top 3 excerpts from the top 3 websites.
    Examples:
        search_web('Eiffel Tower height')
        search_web('best pizza in Seattle')
        search_web('Seattle Kraken game results')
    """"""
    pass
".TrimEnd();

    public async Task<string> GetOutput(string input)
    {
        ILogger logger = this.GetLogger();

        SearchResults searchResults = await this.GetSearchResultsAsync(input);

        ImmutableArray<Chunk> topNPagesContents = await this.GetTopNPagesAsync(searchResults, topN: 3);

        logger.LogInformation("Got page contents: {Contents}", topNPagesContents);
        logger.LogInformation("Got page contents count: {Contents}", topNPagesContents.Length);

        EmbeddingResponse embeddings = await this.embeddingsClient.GetEmbeddingsAsync(input, topNPagesContents);

        logger.LogInformation("Got embeddings results with count: {Embeddings}", embeddings.Data.Length);

        EmbeddingData queryEmbedding = embeddings.QueryData
            ?? throw new InvalidOperationException("Expected query data to be returned on the embedding response");

        IEnumerable<(float, string)> scoresByIndex = embeddings
            .Data
            .Select((e, i) => Tuple.Create(i, CosineSimilarity(queryEmbedding.Embedding, e.Embedding)))
            .OrderByDescending(t => t.Item2) // order by cosine similarity, descending
            .Select(t => (t.Item2, topNPagesContents[t.Item1].Content)) // map score to the original text
            .Take(3)
            .ToArray();

        logger.LogInformation("got scored chunks: {Scored}", scoresByIndex);

        return string.Join("\n\n", scoresByIndex.Select((s, i) => $"[SOURCE {i}] {s.Item2.Trim()}"));
    }

    private static float CosineSimilarity(ImmutableArray<float> a, ImmutableArray<float> b)
    {
        float dotProduct = a.Zip(b).Select(tuple => tuple.First * tuple.Second).Sum();
        float magnitudeA = (float)Math.Sqrt(a.Select(n => (float)Math.Pow(n, 2)).Sum());
        float magnitudeB = (float)Math.Sqrt(b.Select(n => (float)Math.Pow(n, 2)).Sum());

        return dotProduct / (magnitudeA * magnitudeB);
    }

    private async Task<ImmutableArray<Chunk>> GetTopNPagesAsync(SearchResults searchResults, int topN)
    {
        var logger = this.GetLogger();
        logger.LogInformation("Requesting chunks...");

        using var client = this.httpClientFactory.CreateClient();

        var links = searchResults
            .Items
            .Take(topN)
            .Select(r => r.Link)
            .Select(r => new Uri(r));

        ScrapeResponse response = await this.scraperClient.GetScrapedSiteContentAsync(links);

        logger.LogInformation("Saw chunks: {Chunks}", JsonSerializer.Serialize(response));

        return response.Chunks;
    }

    private async Task<SearchResults> GetSearchResultsAsync(string searchQuery)
    {
        string googleKey = Environment.GetEnvironmentVariable(SearchKeyEnvVarName)
            ?? throw new InvalidOperationException($"{SearchKeyEnvVarName} environment variable was expected but not found.");

        string googleCx = Environment.GetEnvironmentVariable(SearchKeyCxEnvVarName)
            ?? throw new InvalidOperationException($"{SearchKeyCxEnvVarName} environment variable was expected but not found.");

        using var client = this.httpClientFactory.CreateClient();

        var searchUri = new UriBuilder(Uri);
        {
            var query = HttpUtility.ParseQueryString(searchUri.Query);

            query["q"] = searchQuery.Trim();
            query["key"] = googleKey;
            query["cx"] = googleCx;
            searchUri.Query = query.ToString();
        }

        var result = await client.GetAsync(searchUri.Uri);

        var parsed = await result.Content.ReadFromJsonAsync<SearchResults>()
            ?? throw new InvalidOperationException("Could not parse response as SearchResults");

        return parsed;
    }

    private record SearchResults(ImmutableArray<SearchItem> Items);

    private record SearchItem(string Title, string Link, string DisplayLink, string Snippet);
}
