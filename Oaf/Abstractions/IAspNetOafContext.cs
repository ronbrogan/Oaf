using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using System;

namespace Oaf.Abstractions
{
    public interface IAspNetOafContext
    {
        IWebHostEnvironment HostingEnvironment { get; }

        object StartupInstance { get; }

        Action<IApplicationBuilder> AppConfigure { get; }
    }
}
