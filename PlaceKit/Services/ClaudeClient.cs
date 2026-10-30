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
    public sealed class ClaudeClient : IAiClient
    {
        internal const string Model = "claude-opus-5-5";

        static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

        public AiProvider Provider => AiProvider.Claude;

        public async Task<string> SendAsync(string apiKey, AiRequest request, CancellationToken token)
        {
            BetaMessage message;
            try
            {
                message = await CreateClient(apiKey).Beta.Messages.Create(CreateParams(request), token).ConfigureAwait(false);
            }
            catch (Exception ex) when (Describe(ex, token) is Exception described)
            {
                throw described;
            }

            if (message.StopReason == BetaStopReason.Refusal)
            {
                throw new AiServiceException("Claude declined to read this text.");
            }

            if (message.StopReason == BetaStopReason.MaxTokens)
            {
                throw new AiServiceException("Claude's answer was cut off. Try a shorter text.");
            }

            return string.Concat(message.Content.Select(block => block.Value).OfType<BetaTextBlock>().Select(block => block.Text));
        }

        public async Task TestKeyAsync(string apiKey, CancellationToken token)
        {
            try
            {
                await CreateClient(apiKey).Models.List(cancellationToken: token).ConfigureAwait(false);
            }
            catch (Exception ex) when (Describe(ex, token) is Exception described)
            {
                throw described;
            }
        }

        internal static MessageCreateParams CreateParams(AiRequest request) => new MessageCreateParams
        {
            Model = Model,
            MaxTokens = 16000,
            // If Claude refuses, the server retries with another model.
            Betas = new List<ApiEnum<string, AnthropicBeta>> { AnthropicBeta.ServerSideFallback2026_07_01 },
            Fallbacks = new Default(),
            OutputConfig = new BetaOutputConfig
            {
                Effort = request.Effort == AiEffort.Low ? Effort.Low : Effort.Medium,
                Format = new BetaJsonOutputFormat { Schema = request.Schema },
            },
            System = request.Instructions,
            Messages = new List<BetaMessageParam>
            {
                new BetaUserMessageParam { Content = request.Prompt },
            },
        };

        static AnthropicClient CreateClient(string apiKey) => new AnthropicClient { ApiKey = apiKey, Timeout = Timeout };

        // null: not an API error, let it through
        internal static Exception Describe(Exception error, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return new OperationCanceledException(token);
            }

            switch (error)
            {
                case AnthropicUnauthorizedException _:
                    return new AiServiceException("Claude did not accept the API key. Check that it is complete and still active.", error);
                case AnthropicForbiddenException _:
                    return new AiServiceException("This API key may not use Claude. Check its permissions in the Claude Console.", error);
                case AnthropicRateLimitException _:
                    return new AiServiceException("There are too many requests with this API key right now. Try again in a minute.", error);
                case Anthropic5xxException _:
                    return new AiServiceException("Claude is not available right now. Try again later.", error);
                case AnthropicIOException _:
                    return new AiServiceException("Could not reach Claude. Check the internet connection.", error);
                case AnthropicApiException _:
                    return new AiServiceException("Claude returned an error: " + error.Message, error);
                case OperationCanceledException _:
                    return new AiServiceException("Claude took too long to answer. Try again.", error);
                default:
                    return null;
            }
        }
    }
}
