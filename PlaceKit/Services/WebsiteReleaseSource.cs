using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using PlaceKit.Interfaces;
using PlaceKit.Models;

namespace PlaceKit.Services
{
    // The update feed on the website, a JSON file edited by hand for each release:
    // { "version": "1.2.0", "page": "https://...", "download": "https://.../PlaceKit.msi" }
    public sealed class WebsiteReleaseSource : IReleaseSource, IDisposable
    {
        static readonly TimeSpan Timeout = TimeSpan.FromSeconds(20);

        static readonly JsonDocumentOptions Lenient = new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

        readonly HttpClient _http;
        readonly Uri _feed;

        public WebsiteReleaseSource(ProductInfo product)
            : this(product, new HttpClientHandler())
        {
        }

        internal WebsiteReleaseSource(ProductInfo product, HttpMessageHandler handler)
        {
            _feed = product.UpdateFeed;
            _http = new HttpClient(handler) { Timeout = Timeout };
            _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue(product.Name, product.Version));
        }

        public async Task<ReleaseInfo> GetLatestAsync(CancellationToken token)
        {
            string site = _feed.Host;
            string json;
            try
            {
                using (HttpResponseMessage response = await _http.GetAsync(_feed, token).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new UpdateCheckException($"No update information on {site} yet.");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new UpdateCheckException($"{site} answered with error {(int)response.StatusCode}. Try again later.");
                    }

                    json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
            }
            catch (HttpRequestException ex)
            {
                throw new UpdateCheckException($"{site} can't be reached. Check the internet connection and try again.", ex);
            }
            catch (TaskCanceledException ex) when (!token.IsCancellationRequested)
            {
                throw new UpdateCheckException($"{site} didn't answer in time. Try again later.", ex);
            }

            return Parse(json);
        }

        public void Dispose() => _http.Dispose();

        // Lenient, since the file is written by hand. Links must be https: the add-in opens them.
        internal static ReleaseInfo Parse(string json)
        {
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json.TrimStart('﻿'), Lenient))
                {
                    JsonElement release = document.RootElement;
                    string text = release.GetProperty("version").GetString();
                    if (!ReleaseInfo.TryParseVersion(text, out Version version))
                    {
                        throw new UpdateCheckException($"The update information names \"{text}\", which is not a version number.");
                    }

                    Uri installer = release.TryGetProperty("download", out JsonElement download) && download.ValueKind != JsonValueKind.Null
                        ? SecureLink(download.GetString())
                        : null;
                    return new ReleaseInfo(version, SecureLink(release.GetProperty("page").GetString()), installer);
                }
            }
            catch (Exception ex) when (ex is JsonException || ex is KeyNotFoundException || ex is InvalidOperationException || ex is FormatException || ex is ArgumentException)
            {
                throw new UpdateCheckException("The update information could not be read. Try again later.", ex);
            }
        }

        static Uri SecureLink(string text)
        {
            Uri link = new Uri(text, UriKind.Absolute);
            return link.Scheme == Uri.UriSchemeHttps ? link : throw new UriFormatException($"{text} is not an https link.");
        }
    }
}
