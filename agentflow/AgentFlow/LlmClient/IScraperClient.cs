using System.Collections.Immutable;

namespace AgentFlow.LlmClient;

public interface IScraperClient
{
    Task<ScrapeResponse> GetScrapedSiteContentAsync(IEnumerable<Uri> uris);
}

public record ScrapeResponse(ImmutableArray<string> Chunks);
