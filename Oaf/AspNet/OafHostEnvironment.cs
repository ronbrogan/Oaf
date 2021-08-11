using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace Oaf.AspNet
{
    public class OafHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = default!;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = default!;
        public string EnvironmentName { get; set; } = Environments.Production;
        public string WebRootPath { get; set; } = default!;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public OafHostEnvironment(IConfiguration configuration)
        {
            var fallbackAppName = Assembly.GetEntryAssembly()?.GetName().Name ?? string.Empty;
            ApplicationName = configuration[WebHostDefaults.ApplicationKey] ?? fallbackAppName;
            EnvironmentName = configuration.GetValue<string>(WebHostDefaults.EnvironmentKey) ?? Environments.Production;
            WebRootPath = configuration[WebHostDefaults.WebRootKey];
            ContentRootPath = configuration[WebHostDefaults.ContentRootKey];
        }
    }
}
