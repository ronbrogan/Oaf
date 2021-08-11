using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Oaf.Abstractions;
using System.Threading.Tasks;

namespace Oaf
{
    public class AspnetPipelineFunctions
    {
        private readonly IOafHandler proxy;

        public AspnetPipelineFunctions(IOafHandler proxy)
        {
            this.proxy = proxy;
        }

        [Function("AspNetCore")]
        public async Task<HttpResponseData> AspnetCore(
            [HttpTrigger(
                AuthorizationLevel.Anonymous, 
                "get", "post", "put", "delete", "options", "head", "patch",
                Route = "{*route}")]
            HttpRequestData req,
            FunctionContext context)
        {
            return await this.proxy.HandleAsync(req, context);
        }
    }
}
