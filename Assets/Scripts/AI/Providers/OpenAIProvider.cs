using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Echobound.AI.Providers
{
    /// <summary>OpenAI Chat Completions over raw HTTP. Same proxy/direct split as the Anthropic provider.</summary>
    public class OpenAIProvider : IAIProvider
    {
        private const string Endpoint = "https://api.openai.com/v1/chat/completions";
        private static readonly HttpClient Http = new HttpClient { Timeout = TimeSpan.FromSeconds(120) };
        private readonly AIConfig _config;
        private int _consecutiveFailures;

        public string Name => "openai";
        public bool IsOnline => _config.HasCredentials && _consecutiveFailures < 5;

        public OpenAIProvider(AIConfig config) { _config = config; }

        public async Task<AIResponse> CompleteAsync(AIRequest request, CancellationToken cancellationToken)
        {
            var sw = Stopwatch.StartNew();
            var body = new JObject
            {
                ["model"] = _config.Model,
                ["max_tokens"] = Math.Max(64, request.MaxTokens),
                ["temperature"] = request.Temperature,
                ["messages"] = new JArray
                {
                    new JObject { ["role"] = "system", ["content"] = request.SystemPrompt ?? "" },
                    new JObject { ["role"] = "user", ["content"] = request.UserPrompt ?? "" }
                },
                ["response_format"] = new JObject { ["type"] = "json_object" }
            };
            bool useProxy = !string.IsNullOrEmpty(_config.ProxyUrl);
            var msg = new HttpRequestMessage(HttpMethod.Post, useProxy ? _config.ProxyUrl : Endpoint)
            {
                Content = new StringContent(body.ToString(Newtonsoft.Json.Formatting.None), Encoding.UTF8, "application/json")
            };
            if (!useProxy) msg.Headers.Add("Authorization", "Bearer " + _config.ApiKey);
            try
            {
                using (var http = await Http.SendAsync(msg, cancellationToken))
                {
                    string text = await http.Content.ReadAsStringAsync();
                    if (!http.IsSuccessStatusCode)
                    {
                        _consecutiveFailures++;
                        int code = (int)http.StatusCode;
                        return AIResponse.Fail($"HTTP {code}: {(text.Length > 300 ? text.Substring(0, 300) : text)}", Name, code == 429 || code >= 500);
                    }
                    var json = JObject.Parse(text);
                    string content = json["choices"]?[0]?["message"]?["content"]?.ToString() ?? "";
                    _consecutiveFailures = 0;
                    return new AIResponse
                    {
                        Success = true, Text = content, Provider = Name, Model = json["model"]?.ToString() ?? _config.Model,
                        InputTokens = json["usage"]?["prompt_tokens"]?.Value<int>() ?? 0,
                        OutputTokens = json["usage"]?["completion_tokens"]?.Value<int>() ?? 0,
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
    }
}
