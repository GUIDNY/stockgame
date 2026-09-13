using System;
using System.Collections.Generic;

namespace Echobound.AI
{
    public enum AIRequestKind { WORLD_GENERATION, DIALOGUE, DIRECTOR_EVENT, QUEST_GENERATION, QUEST_MUTATION, RUMOR }

    /// <summary>A provider-agnostic request. The prompt is text; Payload carries typed context for the mock provider.</summary>
    public class AIRequest
    {
        public string Id = Guid.NewGuid().ToString("N").Substring(0, 8);
        public AIRequestKind Kind;
        public string SystemPrompt = "";
        public string UserPrompt = "";
        public int MaxTokens = 600;
        public float Temperature = 0.8f;
        /// <summary>Optional JSON schema (as a JSON string) for providers that support constrained output.</summary>
        public string JsonSchema;
        /// <summary>Typed context for the MockAIProvider and for logging. Never sent over the network.</summary>
        public object Payload;
        public int Priority = 5;
        public DateTime CreatedUtc = DateTime.UtcNow;
    }

    public class AIResponse
    {
        public bool Success;
        public string Text = "";
        public string Error = "";
        public int InputTokens;
        public int OutputTokens;
        public long LatencyMs;
        public string Provider = "";
        public string Model = "";
        public bool Retryable = true;

        public static AIResponse Fail(string error, string provider, bool retryable = true) =>
            new AIResponse { Success = false, Error = error, Provider = provider, Retryable = retryable };
    }

    /// <summary>Result of a validated request: the typed object plus where it came from.</summary>
    public class AIResult<T>
    {
        public T Value;
        public bool FromAI;
        public bool FromFallback;
        public List<string> ValidationNotes = new List<string>();
        public string RawText = "";
    }
}
