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
    private const int TopNChunks = 5;
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
""""
def search_web(query: str) -> str:
    """
    Searches the web using Google for the given query and returns the top 3 excerpts from the top 3 websites.
    Examples:
        search_web('Eiffel Tower height')
        search_web('best pizza in Seattle')
        search_web('Seattle Kraken game results')
    """
    pass

"""".TrimEnd();

    public async Task<string> GetOutputAsync(string input)
    {
        ILogger logger = this.GetLogger();

        SearchResults searchResults = await this.GetSearchResultsAsync(input);

        ImmutableArray<Chunk> topNPagesContents = await this.GetTopNPagesAsync(searchResults, topN: 3);

        logger.LogInformation("Got page contents: {Contents}", topNPagesContents);
        logger.LogInformation("Got page contents count: {Contents}", topNPagesContents.Length);

        ScoresResponse scores = await this.embeddingsClient.GetScoresAsync(input, topNPagesContents);

        IEnumerable<(float, Chunk)> scoresByIndex = scores
            .Scores
            .Zip(topNPagesContents)
            .OrderByDescending(i => i.First)
            .Take(TopNChunks)
            .ToImmutableArray();

        logger.LogInformation("got scores: {Scores}", scores.Scores);

        return string.Join("\n\n", scoresByIndex.Select((s, _) => $"[SOURCE {s.Item2.Uri}] [SCORE {s.Item1}] {s.Item2.Content.Trim()}"));
    }

    private async Task<ImmutableArray<Chunk>> GetTopNPagesAsync(SearchResults searchResults, int topN)
    {
        var logger = this.GetLogger();

        using var client = this.httpClientFactory.CreateClient();

        var links = searchResults
            .Items
            .Take(topN)
            .Select(r => r.Link)
            .Select(r => new Uri(r));

        logger.LogInformation("Requesting chunks for these sites: {Sites}", links.ToList());

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

        return await result.Content.ReadFromJsonAsync<SearchResults>()
            ?? throw new InvalidOperationException("Could not parse response as SearchResults");
    }

    private sealed record SearchResults(ImmutableArray<SearchItem> Items);

    private sealed record SearchItem(string Title, string Link, string DisplayLink, string Snippet);
}