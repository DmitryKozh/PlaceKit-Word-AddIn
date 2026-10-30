using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Models;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    public sealed class OpenAIClient : IAiClient
    {
        internal const string Model = "gpt-6.1-sol";

        static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

        public AiProvider Provider => AiProvider.OpenAI;

        public async Task<string> SendAsync(string apiKey, AiRequest request, CancellationToken token)
        {
            ChatClient client = new ChatClient(Model, new ApiKeyCredential(apiKey), CreateClientOptions());
            List<ChatMessage> messages = new List<ChatMessage>
            {
                new SystemChatMessage(request.Instructions),
                new UserChatMessage(request.Prompt),
            };

            ChatCompletion completion;
            try
            {
                completion = await client.CompleteChatAsync(messages, CreateOptions(request), token).ConfigureAwait(false);
            }
            catch (Exception ex) when (Describe(ex, token) is Exception described)
            {
                throw described;
            }

            if (!string.IsNullOrEmpty(completion.Refusal) || completion.FinishReason == ChatFinishReason.ContentFilter)
            {
                throw new AiServiceException("ChatGPT declined to read this text.");
            }

            if (completion.FinishReason == ChatFinishReason.Length)
            {
                throw new AiServiceException("ChatGPT's answer was cut off. Try a shorter text.");
            }

            return string.Concat(completion.Content.Select(part => part.Text));
        }

        public async Task TestKeyAsync(string apiKey, CancellationToken token)
        {
            try
            {
                await new OpenAIModelClient(new ApiKeyCredential(apiKey), CreateClientOptions()).GetModelsAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex) when (Describe(ex, token) is Exception described)
            {
                throw described;
            }
        }

        internal static ChatCompletionOptions CreateOptions(AiRequest request) => new ChatCompletionOptions
        {
            // still experimental in the SDK (OPENAI001)
#pragma warning disable OPENAI001
            ReasoningEffortLevel = request.Effort == AiEffort.Low ? ChatReasoningEffortLevel.Low : ChatReasoningEffortLevel.Medium,
#pragma warning restore OPENAI001
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "answer",
                jsonSchema: BinaryData.FromObjectAsJson(request.Schema),
                jsonSchemaIsStrict: true),
        };

        static OpenAIClientOptions CreateClientOptions() => new OpenAIClientOptions { NetworkTimeout = Timeout };

        // After its retries the SDK throws an AggregateException; describe the last error.
        // Status 0 means no response at all.
        internal static Exception Describe(Exception error, CancellationToken token)
        {
            if (token.IsCancellationRequested)
            {
                return new OperationCanceledException(token);
            }

            if (error is AggregateException retries && retries.InnerExceptions.Count > 0)
            {
                return Describe(retries.InnerExceptions[retries.InnerExceptions.Count - 1], token);
            }

            if (error is OperationCanceledException)
            {
                return new AiServiceException("ChatGPT took too long to answer. Try again.", error);
            }

            if (error is HttpRequestException)
            {
                return new AiServiceException("Could not reach OpenAI. Check the internet connection.", error);
            }

            if (!(error is ClientResultException ex))
            {
                return null;
            }

            switch (ex.Status)
            {
                case 0:
                    return new AiServiceException("Could not reach OpenAI. Check the internet connection.", ex);
                case 401:
                    return new AiServiceException("OpenAI did not accept the API key. Check that it is complete and still active.", ex);
                case 403:
                    return new AiServiceException("This API key may not use the model. Check its permissions on the OpenAI platform.", ex);
                case 429 when ex.Message.IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0:
                    // A ChatGPT subscription doesn't include API credit.
                    return new AiServiceException("The OpenAI account has no API credit. A ChatGPT subscription does not include it; add credit on the OpenAI platform.", ex);
                case 429:
                    return new AiServiceException("There are too many requests with this API key right now. Try again in a minute.", ex);
                default:
                    return ex.Status >= 500
                        ? new AiServiceException("OpenAI is not available right now. Try again later.", ex)
                        : new AiServiceException("OpenAI returned an error: " + ex.Message, ex);
            }
        }
    }
}
