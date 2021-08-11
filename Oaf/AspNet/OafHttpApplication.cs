using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Threading.Tasks;

namespace Oaf.AspNet
{
    internal class OafHttpApplication : IHttpApplication<OafHttpApplication.Context>
    {
        public class Context
        {
            public HttpContext HttpContext { get; set; }
        }

        private readonly RequestDelegate requestDelegate;
        private readonly DefaultHttpContextFactory contextFactory;

        public OafHttpApplication(RequestDelegate requestDelegate, DefaultHttpContextFactory contextFactory)
        {
            this.requestDelegate = requestDelegate;
            this.contextFactory = contextFactory;
        }

        public Context CreateContext(IFeatureCollection contextFeatures) => new() { HttpContext = contextFactory.Create(contextFeatures) };

        public async Task ProcessRequestAsync(Context context) => await requestDelegate.Invoke(context.HttpContext);

        public void DisposeContext(Context context, Exception exception) { }
    }
}
