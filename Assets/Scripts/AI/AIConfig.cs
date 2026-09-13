using System;
using System.IO;
using Newtonsoft.Json;
using Echobound.Core;

namespace Echobound.AI
{
    /// <summary>
    /// AI configuration. Keys are NEVER hardcoded: they come from a git-ignored local file or environment variables.
    /// In production, set ProxyUrl to a backend that holds the key and leave ApiKey empty.
    /// </summary>
    [Serializable]
    public class AIConfig
    {
        [JsonProperty("provider")] public string Provider = "mock";      // mock | anthropic | openai
        [JsonProperty("model")] public string Model = "claude-opus-5";
        [JsonProperty("api_key")] public string ApiKey = "";
        [JsonProperty("proxy_url")] public string ProxyUrl = "";
        [JsonProperty("timeout_seconds")] public float TimeoutSeconds = 30f;
        [JsonProperty("max_retries")] public int MaxRetries = 2;
        [JsonProperty("min_seconds_between_requests")] public float MinSecondsBetweenRequests = 0.5f;
        [JsonProperty("max_tokens_dialogue")] public int MaxTokensDialogue = 400;
        [JsonProperty("max_tokens_event")] public int MaxTokensEvent = 800;
        [JsonProperty("max_tokens_world")] public int MaxTokensWorld = 6000;
        [JsonProperty("input_price_per_million")] public float InputPricePerMillion = 5f;
        [JsonProperty("output_price_per_million")] public float OutputPricePerMillion = 25f;
        [JsonProperty("log_prompts")] public bool LogPrompts = true;

        public const string LocalFileName = "ai_config.local.json";

        /// <summary>Loads config from the streaming-assets folder, then overlays environment variables.</summary>
        public static AIConfig Load(string streamingAssetsPath)
        {
            var cfg = new AIConfig();
            try
            {
                string path = Path.Combine(streamingAssetsPath ?? "", LocalFileName);
                if (File.Exists(path))
                {
                    cfg = JsonConvert.DeserializeObject<AIConfig>(File.ReadAllText(path)) ?? cfg;
                    GameLog.Info("AIConfig: loaded " + path);
                }
            }
            catch (Exception e) { GameLog.Warn("AIConfig: could not read local config: " + e.Message); }

            string envProvider = Environment.GetEnvironmentVariable("ECHOBOUND_AI_PROVIDER");
            if (!string.IsNullOrEmpty(envProvider)) cfg.Provider = envProvider;
            string envModel = Environment.GetEnvironmentVariable("ECHOBOUND_AI_MODEL");
            if (!string.IsNullOrEmpty(envModel)) cfg.Model = envModel;
            string envProxy = Environment.GetEnvironmentVariable("ECHOBOUND_AI_PROXY_URL");
            if (!string.IsNullOrEmpty(envProxy)) cfg.ProxyUrl = envProxy;
            if (string.IsNullOrEmpty(cfg.ApiKey))
            {
                string key = cfg.Provider == "openai"
                    ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                    : Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
                if (!string.IsNullOrEmpty(key)) cfg.ApiKey = key;
            }
            cfg.Provider = (cfg.Provider ?? "mock").Trim().ToLowerInvariant();
            if (cfg.Provider != "mock" && string.IsNullOrEmpty(cfg.ApiKey) && string.IsNullOrEmpty(cfg.ProxyUrl))
            {
                GameLog.Warn("AIConfig: provider '" + cfg.Provider + "' has no api key or proxy url; falling back to mock provider.");
                cfg.Provider = "mock";
            }
            return cfg;
        }

        public bool HasCredentials => !string.IsNullOrEmpty(ApiKey) || !string.IsNullOrEmpty(ProxyUrl);
    }
}
