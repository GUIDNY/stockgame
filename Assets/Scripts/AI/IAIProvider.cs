using System.Threading;
using System.Threading.Tasks;

namespace Echobound.AI
{
    /// <summary>
    /// Abstraction over any LLM backend. The game never talks to a provider directly; AIRequestManager does.
    /// Implementations: AnthropicProvider, OpenAIProvider, MockAIProvider (offline, deterministic).
    /// </summary>
    public interface IAIProvider
    {
        string Name { get; }
        bool IsOnline { get; }
        Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken);
    }
}
