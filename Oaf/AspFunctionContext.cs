using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Oaf.Abstractions;
using System;

namespace Oaf
{
    internal class AspFunctionContext<TStartup> : IAspNetOafContext
    {
        public IWebHostEnvironment HostingEnvironment { get; set; }

        public TStartup StartupInstance { get; set; }

        public Action<IApplicationBuilder> AppConfigure { get; internal set; }

        object IAspNetOafContext.StartupInstance => this.StartupInstance;
    }
}
