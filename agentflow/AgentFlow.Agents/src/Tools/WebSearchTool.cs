using System.Collections.Immutable;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using AgentFlow.Agents;
using AgentFlow.Agents.Extensions;
using AgentFlow.Generic;
using AgentFlow.LlmClient;
using AgentFlow.Prompts;
using AgentFlow.Tools;
using AgentFlow.WorkSpace;
using Microsoft.Extensions.Logging;

namespace AgentFlow.Agents.Tools;

public sealed class WebSearchTool : ITool
{
    private const int NumChunksToRAG = 5;
    private const int NumPagesToRead = 5;
    private const string Uri = "https://www.googleapis.com/customsearch/v1";
    private const string SearchKeyEnvVarName = "SEARCH_KEY";
    private const string SearchKeyCxEnvVarName = "SEARCH_KEY_CX";
    private readonly CustomAgentBuilderFactory agentFactory;
    private readonly ICellRunner<ConversationThread> runner;
    private readonly IEnvironmentVariableProvider environmentVariableProvider;
    private readonly IEmbeddingsClient embeddingsClient;
    private readonly IScraperClient scraperClient;
    private readonly IPromptRenderer promptRenderer;
    private readonly IFactory<Prompt> promptFactory;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly ImmutableArray<string> searchSiteUris;

    public WebSearchTool(
        CustomAgentBuilderFactory agentFactory,
        ICellRunner<ConversationThread> runner,
        IEnvironmentVariableProvider environmentVariableProvider,
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        IPromptRenderer promptRenderer,
        IFactory<Prompt> promptFactory,
        IHttpClientFactory httpClientFactory)
        : this(
            agentFactory,
            runner,
            environmentVariableProvider,
            embeddingsClient,
            scraperClient,
            promptRenderer,
            promptFactory,
            httpClientFactory,
            ImmutableArray<string>.Empty,
            "search_web",
            ("Eiffel Tower height", "best pizza in Seattle", "Abraham Lincoln early life"))
    {
    }

    public WebSearchTool(
        CustomAgentBuilderFactory agentFactory,
        ICellRunner<ConversationThread> runner,
        IEnvironmentVariableProvider environmentVariableProvider,
        IEmbeddingsClient embeddingsClient,
        IScraperClient scraperClient,
        IPromptRenderer promptRenderer,
        IFactory<Prompt> promptFactory,
        IHttpClientFactory httpClientFactory,
        ImmutableArray<string> searchSiteUris,
        string toolName,
        (string Example1, string Example2, string Example3) exampleQueries)
    {
        this.agentFactory = agentFactory;
        this.runner = runner;
        this.environmentVariableProvider = environmentVariableProvider;
        this.embeddingsClient = embeddingsClient;
        this.scraperClient = scraperClient;
        this.promptRenderer = promptRenderer;
        this.promptFactory = promptFactory;
        this.httpClientFactory = httpClientFactory;
        this.searchSiteUris = searchSiteUris;
        this.Name = toolName;
        this.ExampleQueries = exampleQueries;
    }

    public string Name { get; }

    public (string Example1, string Example2, string Example3) ExampleQueries { get; }

    public string Definition =>
        $""""
        def {this.Name}(query: str) -> List[str]:
            """
            Searches the web using Google for the given query and returns the top 3 excerpts from the top 3 websites.
            Examples:
                {this.Name}('{this.ExampleQueries.Example1}')
                {this.Name}('{this.ExampleQueries.Example2}')
                {this.Name}('{this.ExampleQueries.Example3}')
            """
            pass # impl omitted

        """".TrimEnd();

    public async Task<string> GetOutputAsync(ConversationThread conversation, string input)
    {
        ILogger logger = this.GetLogger();

        SearchResults searchResults = await this.GetSearchResultsAsync(input);

        ImmutableArray<Chunk> topNPagesContents = await this.GetTopNPagesAsync(searchResults, topN: NumPagesToRead);

        logger.LogDebug("Got page contents: {Contents}", topNPagesContents);
        logger.LogInformation("Got page contents count: {Contents}", topNPagesContents.Length);

        string rewrittenQuery = await this.RewriteQueryAsync(input, conversation);

        ScoresResponse scores = await this.embeddingsClient.GetScoresAsync(rewrittenQuery, topNPagesContents);

        IEnumerable<(float, Chunk)> scoresByIndex = scores
            .Scores
            .Zip(topNPagesContents)
            .OrderByDescending(i => i.First)
            .Take(NumChunksToRAG)
            .ToImmutableArray();

        logger.LogInformation("got scores: {Scores}", scores.Scores);

        return string.Join(
            "\n\n",
            scoresByIndex.Select((s, _) => $"[SOURCE {s.Item2.Uri}] {s.Item2.Content.Trim()}"));
    }

    private async Task<string> RewriteQueryAsync(string originalQuery, ConversationThread history)
    {
        var agent = this
            .agentFactory
            .CreateBuilder()
            .WithName(new AgentName("QueryRewriter"))
            .WithRole(Role.Assistant)
            .WithInstructionsStrategy(InstructionStrategy.AppendedToUserMessage)
            .WithInstructionsFromPrompt(this.promptFactory.Create())
            .SetVariableValue("ORIGINAL_QUERY", originalQuery)
            .Build();

        // Filter out anything except assistant and user messages:
        var historyWithRewriteInstructions = history
            .WithMatchingMessages(message => new[] { Role.Assistant, Role.User }.Contains(message.Role));

        var logger = this.GetLogger();
        logger.LogDebug(
            "Current message history for request is: {Messages}",
            JsonSerializer.Serialize(historyWithRewriteInstructions.Messages));

        var program = await agent.GetNextConversationStateAsync();

        ConversationThread nextState = await this.runner.RunAsync(program, historyWithRewriteInstructions);

        string rewrittenQuery = nextState.Messages.Last().Content.Trim();

        logger.LogInformation("Query rewritten from: '{Original}' to: '{Rewritten}'", originalQuery, rewrittenQuery);

        return rewrittenQuery;
    }

    private async Task<ImmutableArray<Chunk>> GetTopNPagesAsync(SearchResults searchResults, int topN)
    {
        var logger = this.GetLogger();

        using var client = this.httpClientFactory.CreateClient(nameof(WebSearchTool));

        var links = searchResults
            .Items
            .Take(topN)
            .Select(r => r.Link)
            .Select(r => new Uri(r));

        logger.LogInformation("Requesting chunks for these sites: {Sites}", links.ToList());

        ScrapeResponse response = await this.scraperClient.GetScrapedSiteContentAsync(links);

        logger.LogDebug("Saw chunks: {Chunks}", JsonSerializer.Serialize(response));

        return response.Chunks;
    }

    private async Task<SearchResults> GetSearchResultsAsync(string searchQuery)
    {
        string googleKey = this.environmentVariableProvider.GetVariableValue(SearchKeyEnvVarName)
            ?? throw new InvalidOperationException(
                $"{SearchKeyEnvVarName} environment variable was expected but not found.");

        string googleCx = this.environmentVariableProvider.GetVariableValue(SearchKeyCxEnvVarName)
            ?? throw new InvalidOperationException(
                $"{SearchKeyCxEnvVarName} environment variable was expected but not found.");

        using var client = this.httpClientFactory.CreateClient<WebSearchTool>();

        var searchUri = new UriBuilder(Uri);
        {
            var query = HttpUtility.ParseQueryString(searchUri.Query);

            string siteFilter = string.Join(" OR ", this.searchSiteUris.Select(s => $"site:{s}"));

            query["q"] = (searchQuery.Trim() + " " + siteFilter).Trim();
            query["key"] = googleKey;
            query["cx"] = googleCx;
            searchUri.Query = query.ToString();
        }

        var result = await client.GetAsync(searchUri.Uri);

        return await result.Content.ReadFromJsonAsync<SearchResults>()
            ?? throw new InvalidOperationException("Could not parse response as SearchResults");
    }

    public sealed record SearchResults(ImmutableArray<SearchItem> Items);

    public sealed record SearchItem(string Title, string Link, string DisplayLink, string Snippet);
}
