using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Oaf.Abstractions;
using Oaf.AspNet;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace Oaf
{
    public static class OafExtensions
    {
        public static IHostBuilder UseOaf<TStartup>(this IHostBuilder builder)
        {
            var configBuilder = new ConfigurationBuilder().AddInMemoryCollection();

            configBuilder.AddEnvironmentVariables(prefix: "ASPNETCORE_");

            var _config = configBuilder.Build();

            builder.ConfigureHostConfiguration(config =>
            {
                config.AddConfiguration(_config);
            });

            builder.ConfigureServices(services =>
            {
                var context = new AspFunctionContext<TStartup>()
                {
                    HostingEnvironment = new OafHostEnvironment(_config)
                };

                services.AddSingleton<IAspNetOafContext>(context);
                services.AddSingleton((IWebHostEnvironment)context.HostingEnvironment);
                services.AddSingleton((IHostEnvironment)context.HostingEnvironment);

                services.TryAddSingleton(sp => new DiagnosticListener("Microsoft.AspNetCore"));
                services.TryAddSingleton<DiagnosticSource>(sp => sp.GetRequiredService<DiagnosticListener>());
                services.TryAddSingleton(sp => new ActivitySource("Microsoft.AspNetCore"));

                services.TryAddSingleton<IHttpContextFactory, DefaultHttpContextFactory>();
                services.TryAddScoped<IMiddlewareFactory, MiddlewareFactory>();
                services.TryAddSingleton<IApplicationBuilderFactory, ApplicationBuilderFactory>();

                services.AddAuthorization();
                services.AddRouting();

                UseStartup(context, services);

                services.AddSingleton<OafService>();
                services.AddSingleton<IOafHandler>(s => s.GetRequiredService<OafService>());
                services.AddHostedService<OafService>(s => s.GetRequiredService<OafService>());
            });

            return builder;
        }

        private static void UseStartup<[DynamicallyAccessedMembers(StartupLinkerOptions.Accessibility)]TStartup>(
            AspFunctionContext<TStartup> context,
            IServiceCollection services,
            TStartup? instance = default)
        {
            var startupType = typeof(TStartup);

            ExceptionDispatchInfo? startupError = null;
            ConfigureBuilder? configureBuilder = null;

            try
            {
                // We cannot support methods that return IServiceProvider as that is terminal and we need ConfigureServices to compose
                if (typeof(IStartup).IsAssignableFrom(startupType))
                {
                    throw new NotSupportedException($"{typeof(IStartup)} isn't supported");
                }
                if (StartupLoader.HasConfigureServicesIServiceProviderDelegate(startupType, context.HostingEnvironment.EnvironmentName))
                {
                    throw new NotSupportedException($"ConfigureServices returning an {typeof(IServiceProvider)} isn't supported.");
                }

                var provider = services.BuildServiceProvider();
                instance ??= (TStartup)ActivatorUtilities.CreateInstance(provider, startupType);
                context.StartupInstance = instance;

                // Startup.ConfigureServices
                var configureServicesBuilder = StartupLoader.FindConfigureServicesDelegate(startupType, context.HostingEnvironment.EnvironmentName);
                var configureServices = configureServicesBuilder.Build(instance);

                configureServices(services);

                // Resolve Configure after calling ConfigureServices and ConfigureContainer
                configureBuilder = StartupLoader.FindConfigureDelegate(startupType, context.HostingEnvironment.EnvironmentName);
            }
            catch (Exception ex)
            {
                startupError = ExceptionDispatchInfo.Capture(ex);
            }

            // Startup.Configure
            context.AppConfigure = app =>
            {
                // Throw if there was any errors initializing startup
                startupError?.Throw();

                // Execute Startup.Configure
                if (instance != null && configureBuilder != null)
                {
                    configureBuilder.Build(instance)(app);
                }
            };
        }
    }
}
