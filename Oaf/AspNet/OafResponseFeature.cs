using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Oaf.AspNet
{
    internal class OafResponseFeature : IHttpResponseFeature, IHttpResponseBodyFeature, IResponseCookiesFeature
    {
        private Func<Task> _responseStartingAsync = () => Task.CompletedTask;
        private Func<Task> _responseCompletedAsync = () => Task.CompletedTask;

        private readonly HttpResponseData responseData;
        private StreamResponseBodyFeature bodyFeature;

        public int StatusCode
        {
            get => (int)responseData.StatusCode;
            set => responseData.StatusCode = (HttpStatusCode)value;
        }

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; }

        public Stream Body
        {
            get => bodyFeature.Stream;
            set => bodyFeature = new StreamResponseBodyFeature(value);
        }

        public bool HasStarted { get; set; }

        public Stream Stream => bodyFeature.Stream;
        public PipeWriter Writer => bodyFeature.Writer;

        public IResponseCookies Cookies { get; }

        public OafResponseFeature(HttpResponseData responseData)
        {
            this.responseData = responseData;
            bodyFeature = new StreamResponseBodyFeature(responseData.Body);
            Cookies = new CookieProxy(responseData);
            Headers = new HeaderProxy(responseData);
        }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            if (HasStarted)
            {
                throw new InvalidOperationException();
            }

            var prior = _responseStartingAsync;
            _responseStartingAsync = async () =>
            {
                await callback(state);
                await prior();
            };
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
            var prior = _responseCompletedAsync;
            _responseCompletedAsync = async () =>
            {
                try
                {
                    await callback(state);
                }
                finally
                {
                    await prior();
                }
            };
        }

        public void DisableBuffering() => bodyFeature.DisableBuffering();

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (HasStarted)
            {
                return;
            }

            HasStarted = true;

            await _responseStartingAsync();
            await bodyFeature.StartAsync(cancellationToken);
        }

        public Task SendFileAsync(string path, long offset, long? count, CancellationToken cancellationToken = default)
            => bodyFeature.SendFileAsync(path, offset, count, cancellationToken);

        public async Task CompleteAsync()
        {
            await _responseCompletedAsync();
            await bodyFeature.CompleteAsync();
        }
        

        private class HeaderProxy : IHeaderDictionary
        {
            private HttpHeadersCollection headers;
            private IEnumerable<KeyValuePair<string, IEnumerable<string>>> headerEnumerable => headers;

            public HeaderProxy(HttpResponseData responseData)
            {
                headers = responseData.Headers;
            }

            public StringValues this[string key]
            {
                get => headers.TryGetValues(key, out var vals) ? vals.ToArray() : new StringValues();
                set => headers.Add(key, (IEnumerable<string>)value);
            }

            public long? ContentLength
            {
                get => headers.TryGetValues(HeaderNames.ContentLength, out var vals) ? long.Parse(vals.First()) : null;
                set => headers.Add(HeaderNames.ContentLength, value.ToString());
            }

            public ICollection<string> Keys => headerEnumerable.Select(k => k.Key).ToArray();

            public ICollection<StringValues> Values => headerEnumerable.Select(k => (StringValues)k.Value.ToArray()).ToArray();

            public int Count => headers.Count();

            public bool IsReadOnly => false;

            public void Add(string key, StringValues value) => headers.Add(key, value.ToArray());

            public void Add(KeyValuePair<string, StringValues> item)
                => headers.Add(item.Key, item.Value.ToArray());

            public void Clear() => headers.Clear();

            public bool Contains(KeyValuePair<string, StringValues> item)
                => headers.Contains(new KeyValuePair<string, IEnumerable<string>>(item.Key, item.Value.ToArray()));

            public bool ContainsKey(string key) => headers.Contains(key);

            public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex)
            {
                foreach (var (h, vals) in headerEnumerable)
                {
                    array[arrayIndex++] = new KeyValuePair<string, StringValues>(h, vals.ToArray());
                }
            }

            public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator()
            {
                return headerEnumerable.Select(h => new KeyValuePair<string, StringValues>(h.Key, h.Value.ToArray())).GetEnumerator();
            }

            public bool Remove(string key) => headers.Remove(key);

            public bool Remove(KeyValuePair<string, StringValues> item) => headers.Remove(item.Value);

            public bool TryGetValue(string key, [MaybeNullWhen(false)] out StringValues value)
            {
                if (headers.TryGetValues(key, out var vals))
                {
                    value = vals.ToArray();
                    return true;
                }

                value = default;
                return false;
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        private class CookieProxy : IResponseCookies
        {
            private readonly HttpResponseData responseData;

            public CookieProxy(HttpResponseData responseData)
            {
                this.responseData = responseData;
            }

            public void Append(string key, string value)
            {
                responseData.Cookies.Append(key, value);
            }

            public void Append(string key, string value, CookieOptions options)
            {
                responseData.Cookies.Append(new HttpCookie(key, value)
                {
                    Domain = options.Domain,
                    Expires = options.Expires,
                    Secure = options.Secure,
                    HttpOnly = options.HttpOnly,
                    MaxAge = options.MaxAge?.TotalSeconds,
                    Path = options.Path,
                    SameSite = options.SameSite switch
                    {
                        SameSiteMode.None => SameSite.ExplicitNone,
                        SameSiteMode.Unspecified => SameSite.None,
                        SameSiteMode.Lax => SameSite.Lax,
                        SameSiteMode.Strict => SameSite.Strict,
                    },
                });
            }

            public void Delete(string key)
            {
            }

            public void Delete(string key, CookieOptions options)
            {
            }
        }
    }
}
