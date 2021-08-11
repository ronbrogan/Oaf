using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Threading.Tasks;

namespace Oaf.Abstractions
{
    public interface IOafHandler
    {
        Task<HttpResponseData> HandleAsync(HttpRequestData request, FunctionContext functionContext);
    }
}
