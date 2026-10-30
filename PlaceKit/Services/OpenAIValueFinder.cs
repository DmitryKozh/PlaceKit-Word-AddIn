using System;
using System.ClientModel;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    // Sends the ValueRequest to ChatGPT through the OpenAI API with the user's API key.
    public sealed class OpenAIValueFinder : IValueFinder
    {
        // OpenAI's model to balance intelligence and cost; it supports Chat Completions and strict structured output.
        internal const string Model = "gpt-6.1-sol";

        static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

        readonly IApiKeyStore _keys;
        readonly IProgressService _progress;

        public OpenAIValueFinder(IApiKeyStore keys, IProgressService progress)
        {
            _keys = keys;
            _progress = progress;
        }

        public AiProvider Provider => AiProvider.OpenAI;

        public IReadOnlyDictionary<string, string> FindValues(string text, IReadOnlyList<string> names)
        {
            string apiKey = _keys.Load(Provider);
            if (apiKey == null)
            {
                throw new ValueFinderException("Add your OpenAI API key first.");
            }

            // As in ClaudeValueFinder: Word's thread waits, the request runs on the thread pool.
            return _progress.Run("Finding the values", (progress, token) =>
                Task.Run(() => RequestAsync(apiKey, text, names, token), token).GetAwaiter().GetResult());
        }

        static async Task<IReadOnlyDictionary<string, string>> RequestAsync(string apiKey, string text, IReadOnlyList<string> names, CancellationToken token)
        {
            ChatClient client = new ChatClient(Model, new ApiKeyCredential(apiKey), new OpenAIClientOptions { NetworkTimeout = Timeout });
            List<ChatMessage> messages = new List<ChatMessage>
            {
                new SystemChatMessage(ValueRequest.Instructions),
                new UserChatMessage(ValueRequest.CreatePrompt(text, names)),
            };

            ChatCompletion completion;
            try
            {
                completion = await client.CompleteChatAsync(messages, CreateOptions(names), token).ConfigureAwait(false);
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                throw new OperationCanceledException(token);
            }
            catch (Exception ex) when (ex is ClientResultException || ex is AggregateException || ex is HttpRequestException || ex is OperationCanceledException)
            {
                throw Describe(ex);
            }

            return ReadAnswer(completion, names);
        }

        internal static ChatCompletionOptions CreateOptions(IReadOnlyList<string> names) => new ChatCompletionOptions
        {
            // Finding values takes little reasoning; low effort keeps the wait short. The SDK still marks the option
            // as experimental (OPENAI001); if it changes, the build fails here rather than the request.
#pragma warning disable OPENAI001
            ReasoningEffortLevel = ChatReasoningEffortLevel.Low,
#pragma warning restore OPENAI001
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                jsonSchemaFormatName: "placeholder_values",
                jsonSchema: BinaryData.FromObjectAsJson(ValueRequest.CreateSchema(names)),
                jsonSchemaIsStrict: true),
        };

        // The SDK retries a failed request and then throws an AggregateException with the error of each try; the last
        // one is described. Status 0: no answer from the server at all.
        internal static ValueFinderException Describe(Exception error)
        {
            if (error is AggregateException retries && retries.InnerExceptions.Count > 0)
            {
                return Describe(retries.InnerExceptions[retries.InnerExceptions.Count - 1]);
            }

            if (error is OperationCanceledException)
            {
                return new ValueFinderException("ChatGPT took too long to answer. Try again.", error);
            }

            if (!(error is ClientResultException ex))
            {
                return new ValueFinderException("Could not reach OpenAI. Check the internet connection.", error);
            }

            switch (ex.Status)
            {
                case 0:
                    return new ValueFinderException("Could not reach OpenAI. Check the internet connection.", ex);
                case 401:
                    return new ValueFinderException("OpenAI did not accept the API key. Check that it is complete and still active.", ex);
                case 403:
                    return new ValueFinderException("This API key may not use the model. Check its permissions on the OpenAI platform.", ex);
                case 429 when ex.Message.IndexOf("quota", StringComparison.OrdinalIgnoreCase) >= 0:
                    // Often a ChatGPT subscriber's first try: the subscription doesn't pay for the API.
                    return new ValueFinderException("The OpenAI account has no API credit. A ChatGPT subscription does not include it; add credit on the OpenAI platform.", ex);
                case 429:
                    return new ValueFinderException("There are too many requests with this API key right now. Try again in a minute.", ex);
                default:
                    return ex.Status >= 500
                        ? new ValueFinderException("OpenAI is not available right now. Try again later.", ex)
                        : new ValueFinderException("OpenAI returned an error: " + ex.Message, ex);
            }
        }

        static IReadOnlyDictionary<string, string> ReadAnswer(ChatCompletion completion, IReadOnlyList<string> names)
        {
            if (!string.IsNullOrEmpty(completion.Refusal) || completion.FinishReason == ChatFinishReason.ContentFilter)
            {
                throw new ValueFinderException("ChatGPT declined to read this text.");
            }

            if (completion.FinishReason == ChatFinishReason.Length)
            {
                throw new ValueFinderException("ChatGPT's answer was cut off. Try a shorter text.");
            }

            string json = string.Concat(completion.Content.Select(part => part.Text));
            return ValueRequest.ParseValues(json, names);
        }
    }
}
