using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Echobound.AI.Schemas;
using Echobound.Core;

namespace Echobound.AI
{
    /// <summary>One line of the AI request/response log shown in the debug panel.</summary>
    public class AIRequestLogEntry
    {
        public DateTime TimeUtc;
        public string RequestId = "";
        public AIRequestKind Kind;
        public string Provider = "";
        public bool Success;
        public bool UsedFallback;
        public int Attempts;
        public int InputTokens;
        public int OutputTokens;
        public long LatencyMs;
        public string PromptPreview = "";
        public string ResponsePreview = "";
        public string Notes = "";

        public override string ToString() =>
            $"{TimeUtc:HH:mm:ss} {Kind} [{Provider}] {(Success ? "OK" : "FAIL")}{(UsedFallback ? " (fallback)" : "")} x{Attempts} in={InputTokens} out={OutputTokens} {LatencyMs}ms {Notes}";
    }

    /// <summary>
    /// Single entry point for every LLM call. Guarantees: never blocks the game, never throws to callers,
    /// never applies invalid JSON, always returns something (validated AI result or deterministic fallback).
    /// Queue + rate limit + retry + timeout + validation + logging + token accounting.
    /// </summary>
    public class AIRequestManager
    {
        private readonly AIConfig _config;
        private IAIProvider _provider;
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);
        private DateTime _lastRequestUtc = DateTime.MinValue;
        private readonly List<AIRequestLogEntry> _log = new List<AIRequestLogEntry>();
        private readonly List<string> _promptLog = new List<string>();
        private readonly List<string> _responseLog = new List<string>();

        public int TotalInputTokens { get; private set; }
        public int TotalOutputTokens { get; private set; }
        public int TotalRequests { get; private set; }
        public int FailedRequests { get; private set; }
        public int FallbacksUsed { get; private set; }
        public int PendingRequests { get; private set; }
        public float EstimatedCostUsd =>
            TotalInputTokens / 1_000_000f * _config.InputPricePerMillion + TotalOutputTokens / 1_000_000f * _config.OutputPricePerMillion;

        public IReadOnlyList<AIRequestLogEntry> Log => _log;
        public IReadOnlyList<string> PromptLog => _promptLog;
        public IReadOnlyList<string> ResponseLog => _responseLog;
        public IAIProvider Provider => _provider;
        public AIConfig Config => _config;

        public event Action<AIRequestLogEntry> RequestLogged;

        public AIRequestManager(AIConfig config, IAIProvider provider)
        {
            _config = config;
            _provider = provider;
        }

        /// <summary>Hot-swap the backend (used by the debug panel and when a network provider goes offline).</summary>
        public void SetProvider(IAIProvider provider) { _provider = provider; }

        /// <summary>
        /// Sends a request, validates the answer and returns a typed result. On any failure the fallback is used.
        /// </summary>
        public async Task<AIResult<T>> RequestAsync<T>(AIRequest request, IResponseValidator<T> validator, Func<T> fallback)
        {
            var result = new AIResult<T>();
            var entry = new AIRequestLogEntry { TimeUtc = DateTime.UtcNow, RequestId = request.Id, Kind = request.Kind, Provider = _provider?.Name ?? "none" };
            var sw = Stopwatch.StartNew();
            PendingRequests++;
            TotalRequests++;
            if (validator != null && request.JsonSchema == null) request.JsonSchema = validator.JsonSchema;
            if (_config.LogPrompts) AddLog(_promptLog, $"[{request.Id} {request.Kind}] SYSTEM:\n{request.SystemPrompt}\n\nUSER:\n{request.UserPrompt}");

            bool succeeded = false;
            try
            {
                await _gate.WaitAsync();
                try
                {
                    succeeded = await TryAttemptsAsync(request, validator, result, entry);
                }
                finally { _gate.Release(); }
            }
            catch (Exception e)
            {
                entry.Notes = "manager exception: " + e.Message;
                GameLog.Error("AIRequestManager: " + e);
            }

            if (!succeeded)
            {
                // Deterministic fallback: the game must never stall on the AI.
                FailedRequests++;
                FallbacksUsed++;
                entry.UsedFallback = true;
                try { result.Value = fallback != null ? fallback() : default; }
                catch (Exception e) { GameLog.Error("AI fallback threw: " + e); }
                result.FromFallback = true;
            }

            PendingRequests--;
            sw.Stop();
            entry.LatencyMs = sw.ElapsedMilliseconds;
            entry.PromptPreview = Preview(request.UserPrompt);
            entry.Success = succeeded;
            Record(entry);
            return result;
        }

        /// <summary>Runs provider attempts with retry, timeout and validation. Returns true when a valid result was produced.</summary>
        private async Task<bool> TryAttemptsAsync<T>(AIRequest request, IResponseValidator<T> validator, AIResult<T> result, AIRequestLogEntry entry)
        {
            string lastError = null;
            string feedback = null;
            int maxAttempts = Math.Max(1, _config.MaxRetries + 1);
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                entry.Attempts = attempt;
                await RespectRateLimit();
                var attemptRequest = feedback == null ? request : WithFeedback(request, feedback);
                AIResponse response;
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(3f, _config.TimeoutSeconds))))
                {
                    try { response = await _provider.CompleteAsync(attemptRequest, cts.Token); }
                    catch (OperationCanceledException) { response = AIResponse.Fail("timeout after " + _config.TimeoutSeconds + "s", _provider.Name); }
                    catch (Exception e) { response = AIResponse.Fail("provider exception: " + e.Message, _provider.Name); }
                }
                _lastRequestUtc = DateTime.UtcNow;
                entry.InputTokens += response.InputTokens;
                entry.OutputTokens += response.OutputTokens;
                TotalInputTokens += response.InputTokens;
                TotalOutputTokens += response.OutputTokens;

                if (!response.Success)
                {
                    lastError = response.Error;
                    GameLog.Warn($"AI {request.Kind} attempt {attempt} failed: {response.Error}");
                    if (!response.Retryable) break;
                    await Task.Delay(TimeSpan.FromMilliseconds(400 * attempt));
                    continue;
                }
                AddLog(_responseLog, $"[{request.Id} {request.Kind}] {response.Text}");
                result.RawText = response.Text;
                entry.ResponsePreview = Preview(response.Text);
                if (validator == null)
                {
                    result.FromAI = true;
                    return true;
                }
                var validation = validator.Validate(response.Text);
                if (validation.Ok)
                {
                    result.Value = validation.Value;
                    result.FromAI = true;
                    result.ValidationNotes.AddRange(validation.Corrections);
                    entry.Notes = validation.Corrections.Count > 0 ? "corrected: " + string.Join(", ", validation.Corrections) : "";
                    return true;
                }
                lastError = "validation failed: " + validation.ErrorText;
                feedback = validation.ErrorText;
                GameLog.Warn($"AI {request.Kind} attempt {attempt} rejected: {validation.ErrorText}");
            }
            entry.Notes = lastError ?? "unknown failure";
            return false;
        }

        private void Record(AIRequestLogEntry entry)
        {
            _log.Add(entry);
            if (_log.Count > 100) _log.RemoveAt(0);
            GameLog.Info("AI: " + entry);
            RequestLogged?.Invoke(entry);
        }

        private async Task RespectRateLimit()
        {
            var since = DateTime.UtcNow - _lastRequestUtc;
            var min = TimeSpan.FromSeconds(_config.MinSecondsBetweenRequests);
            if (since < min) await Task.Delay(min - since);
        }

        private static AIRequest WithFeedback(AIRequest original, string feedback)
        {
            return new AIRequest
            {
                Id = original.Id, Kind = original.Kind, SystemPrompt = original.SystemPrompt,
                UserPrompt = original.UserPrompt + "\n\nYOUR PREVIOUS ANSWER WAS REJECTED: " + feedback +
                             "\nReturn corrected JSON only, using only identifiers from the World Bible.",
                MaxTokens = original.MaxTokens, Temperature = Math.Max(0.2f, original.Temperature - 0.2f),
                JsonSchema = original.JsonSchema, Payload = original.Payload, Priority = original.Priority
            };
        }

        private void AddLog(List<string> list, string text)
        {
            list.Add(text);
            if (list.Count > 40) list.RemoveAt(0);
        }

        private static string Preview(string s) => string.IsNullOrEmpty(s) ? "" : (s.Length <= 160 ? s : s.Substring(0, 160) + "…").Replace('\n', ' ');
    }
}
