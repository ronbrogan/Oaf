# AspNetCore On Azure Functions (AspNetCore OAF)

This library provides mechanisms to proxy all HTTP requests handled by the standalone (out of process) C# function worker into the AspNet Core pipeline.

For now, it's a toy that's being used for non-critical deployments (staging/pre-prod sites). It's not 'supported' and you shouldn't use it


## Usage

In your function project's `host.json` configure it so that we can match unprefixed routes:

```json
{
    "extensions": {
        "http": {
            "routePrefix": ""
        }
    }
}
```

Create a `IHostBuilder` as usual for your function worker. At the end, call `UseOaf<TSTartup>()`. The `TStartup` type is the same startup type that is used when configuring the web host for a normal AspNetCore project. No further configuration is required (or supported). 

```csharp
public static async Task Main(string[] args)
{
    var host = API.Program.CreateApiHostBuilder(args)
        .ConfigureFunctionsWorkerDefaults()
        .UseOaf<Startup>()
        .Build();

    await host.RunAsync();
}

```

Upon starting the function app, you should see a wildcard HTTP function to handle the requests. All requests will route to that function and be processed through the AspNetCore pipeline, allowing you to take advantage of standard middleware, authentication, and other things (such as Swagger/OpenAPI).

```
Azure Functions Core Tools
Core Tools Version:       3.0.3442 Commit hash: 6bfab24b2743f8421475d996402c398d2fe4a9e0  (64-bit)
Function Runtime Version: 3.0.15417.0


Functions:

        AspNetCore: [GET,POST,PUT,DELETE,OPTIONS,HEAD,PATCH] http://localhost:7071/{*route}
        
For detailed output, run func with --verbose flag.
[2021-08-12T03:36:33.170Z] Azure Functions .NET Worker (PID: 34264) initialized in debug mode. Waiting for debugger to attach...
[2021-08-12T03:36:38.229Z] Host lock lease acquired by instance ID '0000000000000000000000003127224C'.
[2021-08-12T03:36:46.252Z] Worker process started and initialized.
[2021-08-12T03:36:46.870Z] info: Microsoft.Hosting.Lifetime[0]
[2021-08-12T03:36:46.871Z]       Application started. Press Ctrl+C to shut down.
[2021-08-12T03:36:46.872Z] info: Microsoft.Hosting.Lifetime[0]
[2021-08-12T03:36:46.873Z]       Hosting environment: Production
[2021-08-12T03:36:46.874Z] info: Microsoft.Hosting.Lifetime[0]
[2021-08-12T03:36:46.875Z]       Content root path: (null)
```