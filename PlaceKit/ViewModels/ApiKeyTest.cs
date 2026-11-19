using System;
using System.Diagnostics;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.ViewModels
{
    // Tests an API key for Settings and the AI setup. Never throws: exceptions must not reach Office from a dialog's command.
    internal sealed class ApiKeyTest
    {
        ApiKeyTest(bool isConnected, string error)
        {
            IsConnected = isConnected;
            Error = error;
        }

        public bool IsConnected { get; }

        // null when connected or canceled
        public string Error { get; }

        public static ApiKeyTest Run(IAiService ai, AiProvider provider, string apiKey)
        {
            try
            {
                ai.TestKey(provider, apiKey);
                return new ApiKeyTest(true, null);
            }
            catch (AiServiceException ex)
            {
                return new ApiKeyTest(false, ex.Message);
            }
            catch (OperationCanceledException)
            {
                return new ApiKeyTest(false, null);
            }
            catch (Exception ex)
            {
                Trace.TraceError("PlaceKit: {0}", ex);
                return new ApiKeyTest(false, $"The test failed: {ex.Message}");
            }
        }
    }
}
