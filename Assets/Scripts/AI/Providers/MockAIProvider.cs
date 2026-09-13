using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Echobound.Core;

namespace Echobound.AI.Providers
{
    /// <summary>
    /// Offline provider. Produces JSON text exactly like a real model would, so the same validators run on it.
    /// World generation is deterministic per seed; dialogue and events are rule-based.
    /// </summary>
    public class MockAIProvider : IAIProvider
    {
        private readonly SeededRandom _rng;
        public string Name => "mock";
        public bool IsOnline => true;
        public int SimulatedLatencyMs = 120;

        public MockAIProvider(int seed) { _rng = new SeededRandom(seed); }

        public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken)
        {
            if (SimulatedLatencyMs > 0) await Task.Delay(SimulatedLatencyMs, cancellationToken);
            object result;
            switch (request.Kind)
            {
                case AIRequestKind.WORLD_GENERATION:
                    result = NarrativeGenerator.Generate((request.Payload as WorldGenPayload)?.SeedNumber ?? _rng.Next(1, int.MaxValue));
                    break;
                case AIRequestKind.DIALOGUE:
                    if (!(request.Payload is DialoguePayload dp)) return AIResponse.Fail("mock dialogue needs a DialoguePayload", Name, false);
                    result = MockDialogue.Build(dp, dp.Rng?.Value ?? _rng);
                    break;
                case AIRequestKind.DIRECTOR_EVENT:
                    if (!(request.Payload is DirectorEventPayload ep)) return AIResponse.Fail("mock event needs a DirectorEventPayload", Name, false);
                    result = FallbackEventFactory.Create(ep, _rng);
                    break;
                case AIRequestKind.QUEST_MUTATION:
                    if (!(request.Payload is QuestMutationPayload mp)) return AIResponse.Fail("mock mutation needs a QuestMutationPayload", Name, false);
                    var t = MockQuestText.Build(mp);
                    result = new { title = t.Title, narrative_reason = t.NarrativeReason, objective_descriptions = t.ObjectiveDescriptions, player_notification = t.PlayerNotification };
                    break;
                default:
                    return AIResponse.Fail("mock provider does not handle " + request.Kind, Name, false);
            }
            string text = JsonConvert.SerializeObject(result, Formatting.None);
            int approxIn = (request.SystemPrompt?.Length ?? 0) / 4 + (request.UserPrompt?.Length ?? 0) / 4;
            return new AIResponse { Success = true, Text = text, Provider = Name, Model = "mock", InputTokens = approxIn, OutputTokens = text.Length / 4 };
        }
    }
}
