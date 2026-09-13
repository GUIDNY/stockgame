using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Echobound.Core;

namespace Echobound.AI.Providers
{
    /// <summary>
    /// Anthropic Messages API over raw HTTP (Unity cannot consume the NuGet SDK directly).
    /// Requests JSON via output_config.format when a schema is available.
    /// Direct mode: api key in the x-api-key header (local development only).
    /// Proxy mode: POST to ProxyUrl with no key; the backend injects credentials.
    /// </summary>
    public class AnthropicProvider : IAIProvider
    {
        private const string Endpoint = "https://api.anthropic.com/v1/messages";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        private readonly AIConfig _config;
        private int _consecutiveFailures;

        public string Name => "anthropic";
        public bool IsOnline => _config.HasCredentials && _consecutiveFailures < 5;

        public AnthropicProvider(AIConfig config) { _config = config; }

        public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var body = new JObject
            {
                ["model"] = _config.Model,
                ["max_tokens"] = Math.Max(64, request.MaxTokens),
                ["system"] = request.SystemPrompt ?? "",
                ["messages"] = new JArray { new JObject { ["role"] = "user", ["content"] = request.UserPrompt ?? "" } },
                ["output_config"] = new JObject { ["effort"] = request.Kind == AIRequestKind.WORLD_GENERATION ? "high" : "low" }
            };
            if (!string.IsNullOrEmpty(request.JsonSchema))
            {
                try
                {
                    body["output_config"]["format"] = new JObject { ["type"] = "json_schema", ["schema"] = JObject.Parse(request.JsonSchema) };
                }
                catch (Exception e) { GameLog.Warn("AnthropicProvider: schema not attached: " + e.Message); }
            }

            bool useProxy = !string.IsNullOrEmpty(_config.ProxyUrl);
            var msg = new HttpRequestMessage(HttpMethod.Post, useProxy ? _config.ProxyUrl : Endpoint)
            {
                Content = new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json")
            };
            if (!useProxy)
            {
                msg.Headers.Add("x-api-key", _config.ApiKey);
                msg.Headers.Add("anthropic-version", "2023-06-01");
            }

            try
            {
                using (var http = await Http.SendAsync(msg, cancellationToken))
                {
                    string text = await http.Content.ReadAsStringAsync();
                    if (!http.IsSuccessStatusCode)
                    {
                        _consecutiveFailures++;
                        int code = (int)http.StatusCode;
                        bool retryable = code == 429 || code >= 500 || code == 408 || code == 529;
                        return AIResponse.Fail($"HTTP {code}: {Truncate(text)}", Name, retryable);
                    }
                    var json = JObject.Parse(text);
                    string stopReason = json["stop_reason"]?.ToString();
                    if (stopReason == "refusal")
                    {
                        _consecutiveFailures = 0;
                        return AIResponse.Fail("model refused the request", Name, false);
                    }
                    var sb = new StringBuilder();
                    foreach (var block in json["content"] as JArray ?? new JArray())
                        if (block["type"]?.ToString() == "text") sb.Append(block["text"]?.ToString());
                    _consecutiveFailures = 0;
                    return new AIResponse
                    {
                        Success = true, Text = sb.ToString(), Provider = Name, Model = json["model"]?.ToString() ?? _config.Model,
                        InputTokens = json["usage"]?["input_tokens"]?.Value<int>() ?? 0,
                        OutputTokens = json["usage"]?["output_tokens"]?.Value<int>() ?? 0,
                        LatencyMs = sw.ElapsedMilliseconds
                    };
                }
            }
            catch (OperationCanceledException) { _consecutiveFailures++; throw; }
            catch (Exception e)
            {
                _consecutiveFailures++;
                return AIResponse.Fail("network error: " + e.Message, Name);
            }
        }

        private static string Truncate(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length <= 300 ? s : s.Substring(0, 300));
    }
}
