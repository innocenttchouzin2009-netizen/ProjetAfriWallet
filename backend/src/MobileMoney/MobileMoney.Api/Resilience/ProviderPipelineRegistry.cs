namespace MobileMoney.Production.Resilience;

public sealed class ProviderPipelineRegistry
{
    private readonly Dictionary<string, ResiliencePipeline> _pipelines = new(StringComparer.OrdinalIgnoreCase);

    public void Register(string providerName, ResiliencePipeline pipeline)
    {
        _pipelines[providerName] = pipeline;
    }

    public ResiliencePipeline GetOrDefault(string providerName)
    {
        return _pipelines.TryGetValue(providerName, out var pipeline) ? pipeline : throw new InvalidOperationException($"No pipeline registered for {providerName}");
    }
}
