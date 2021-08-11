using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using Oaf.Abstractions;
using Oaf.AspNet;
using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Oaf
{
    public class OafService : BackgroundService, IOafHandler
    {
        private readonly IAspNetOafContext appContext;
        private readonly IServiceProvider provider;
        private OafHttpApplication? app;

        public OafService(IAspNetOafContext appContext, IServiceProvider provider)
        {
            this.appContext = appContext;
            this.provider = provider;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var builderFactory = new ApplicationBuilderFactory(this.provider);
            var serverFeatures = new FeatureCollection();

            var builder = builderFactory.CreateBuilder(serverFeatures);

            this.appContext.AppConfigure(builder);

            var application = builder.Build();

            this.app = new OafHttpApplication(application, new DefaultHttpContextFactory(this.provider));
            return Task.CompletedTask;
        }

        public async Task<HttpResponseData> HandleAsync(HttpRequestData request, FunctionContext functionContext)
        {
            if(this.app == null)
            {
                return request.CreateResponse(HttpStatusCode.InternalServerError);
            }

            var dict = new HeaderDictionary();
            foreach(var (h,k) in request.Headers)
            {
                dict.Add(h, new StringValues(k.ToArray()));
            }

            var features = new FeatureCollection();
            var requestFeature = new HttpRequestFeature
            {
                Scheme = request.Url.Scheme,
                Path = WebUtility.UrlDecode(request.Url.AbsolutePath),
                QueryString = request.Url.Query,
                Method = request.Method,
                Body = request.Body,
                Headers = dict,
                RawTarget = request.Url.OriginalString
            };

            features.Set<IHttpRequestFeature>(requestFeature);

            var response = request.CreateResponse();

            var responseFeatures = new OafResponseFeature(response);
            features.Set<IHttpResponseFeature>(responseFeatures);
            features.Set<IHttpResponseBodyFeature>(responseFeatures);
            features.Set<IResponseCookiesFeature>(responseFeatures);

            var ctx = this.app.CreateContext(features);
            await this.app.ProcessRequestAsync(ctx);
            await ctx.HttpContext.Response.StartAsync();

            return response;
        }
    }
}
