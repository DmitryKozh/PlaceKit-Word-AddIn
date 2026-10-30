using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Core;
using Anthropic.Exceptions;
using Anthropic.Models.Beta;
using Anthropic.Models.Beta.Messages;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    // Sends the ValueRequest to Claude by Anthropic with the user's API key.
    public sealed class ClaudeValueFinder : IValueFinder
    {
        internal const string Model = "claude-opus-5-5";

        static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

        readonly IApiKeyStore _keys;
        readonly IProgressService _progress;

        public ClaudeValueFinder(IApiKeyStore keys, IProgressService progress)
        {
            _keys = keys;
            _progress = progress;
        }

        public AiProvider Provider => AiProvider.Claude;

        public IReadOnlyDictionary<string, string> FindValues(string text, IReadOnlyList<string> names)
        {
            string apiKey = _keys.Load(Provider);
            if (apiKey == null)
            {
                throw new ValueFinderException("Add your Claude API key first.");
            }

            // Word's thread waits, as for the other long operations. The request runs on the thread pool,
            // so it never needs Word's thread to continue.
            return _progress.Run("Finding the values", (progress, token) =>
                Task.Run(() => RequestAsync(apiKey, text, names, token), token).GetAwaiter().GetResult());
        }

        static async Task<IReadOnlyDictionary<string, string>> RequestAsync(string apiKey, string text, IReadOnlyList<string> names, CancellationToken token)
        {
            AnthropicClient client = new AnthropicClient { ApiKey = apiKey, Timeout = Timeout };
            BetaMessage message;
            try
            {
                message = await client.Beta.Messages.Create(CreateRequest(text, names), token).ConfigureAwait(false);
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
            catch (AnthropicUnauthorizedException ex)
            {
                throw new ValueFinderException("Claude did not accept the API key. Check that it is complete and still active.", ex);
            }
            catch (AnthropicForbiddenException ex)
            {
                throw new ValueFinderException("This API key may not use Claude. Check its permissions in the Claude Console.", ex);
            }
            catch (AnthropicRateLimitException ex)
            {
                throw new ValueFinderException("There are too many requests with this API key right now. Try again in a minute.", ex);
            }
            catch (Anthropic5xxException ex)
            {
                throw new ValueFinderException("Claude is not available right now. Try again later.", ex);
            }
            catch (AnthropicIOException ex)
            {
                throw new ValueFinderException("Could not reach Claude. Check the internet connection.", ex);
            }
            catch (AnthropicApiException ex)
            {
                throw new ValueFinderException("Claude returned an error: " + ex.Message, ex);
            }
            catch (OperationCanceledException ex)
            {
                throw new ValueFinderException("Claude took too long to answer. Try again.", ex);
            }

            return ReadAnswer(message, names);
        }

        internal static MessageCreateParams CreateRequest(string text, IReadOnlyList<string> names) => new MessageCreateParams
        {
            Model = Model,
            MaxTokens = 16000,
            // A refusal by Claude's safety checks is answered by another model in the same request.
            Betas = new List<ApiEnum<string, AnthropicBeta>> { AnthropicBeta.ServerSideFallback2026_07_01 },
            Fallbacks = new Default(),
            // Finding values takes little reasoning; low effort keeps the wait short.
            OutputConfig = new BetaOutputConfig
            {
                Effort = Effort.Low,
                Format = new BetaJsonOutputFormat { Schema = ValueRequest.CreateSchema(names) },
            },
            System = ValueRequest.Instructions,
            Messages = new List<BetaMessageParam>
            {
                new BetaUserMessageParam { Content = ValueRequest.CreatePrompt(text, names) },
            },
        };

        static IReadOnlyDictionary<string, string> ReadAnswer(BetaMessage message, IReadOnlyList<string> names)
        {
            if (message.StopReason == BetaStopReason.Refusal)
            {
                throw new ValueFinderException("Claude declined to read this text.");
            }

            if (message.StopReason == BetaStopReason.MaxTokens)
            {
                throw new ValueFinderException("Claude's answer was cut off. Try a shorter text.");
            }

            string json = string.Concat(message.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(block => block.Text));
            return ValueRequest.ParseValues(json, names);
        }
    }
}
