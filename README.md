# AspNetCore On Azure Functions (AspNetCore OAF)

This library provides mechanisms to proxy all HTTP requests handled by the standalone (out of process) C# function worker into the AspNet Core pipeline.

For now, it's a toy that's being used for non-critical deployments (staging/pre-prod sites). It's not 'supported' and you shouldn't use it


## Usage

Create a `IHostBuilder` as usual for your function worker. At the end, call `UseOaf<TSTartup>()`. The `TStartup` type is the same startup type that is used when configuring the web host for a normal AspNetCore project. No further configuration is required (or supported). 

```csharp
public static async Task Main(string[] args)
{
    var host = CreateApiHostBuilder(args)   // Your centralized IHostBuilder logic
        .ConfigureFunctionsWorkerDefaults() // Wire up isolated function worker
        .UseOaf<Startup>()                  // Wire up Oaf with your AspNetCore startup class
        .Build();

    await host.RunAsync();
}

```

*Optional*
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

Upon starting the function app, you should see a wildcard HTTP function to handle the requests. All requests will route to that function and be processed through the AspNetCore pipeline, allowing you to take advantage of standard middleware, authentication, and other things (such as Swagger/OpenAPI).

```
Azure Functions Core Tools
Core Tools Version:       3.0.3442 Commit hash: 6bfab24b2743f8421475d996402c398d2fe4a9e0  (64-bit)
Function Runtime Version: 3.0.15417.0


Functions:

        AspNetCore: [GET,POST,PUT,DELETE,OPTIONS,HEAD,PATCH] http://localhost:7071/{*route}
        
For detailed output, run func with --verbose flag.
[2021-08-12T03:36:33.170Z] Azure Functions .NET Worker (PID: 12345) initialized in debug mode. Waiting for debugger to attach...
[2021-08-12T03:36:38.229Z] Host lock lease acquired by instance ID '0000000000000000000000000007224C'.
[2021-08-12T03:36:46.252Z] Worker process started and initialized.
[2021-08-12T03:36:46.870Z] info: Microsoft.Hosting.Lifetime[0]
[2021-08-12T03:36:46.871Z]       Application started. Press Ctrl+C to shut down.
[2021-08-12T03:36:46.872Z] info: Microsoft.Hosting.Lifetime[0]
[2021-08-12T03:36:46.873Z]       Hosting environment: Production
[2021-08-12T03:36:46.874Z] info: Microsoft.Hosting.Lifetime[0]
[2021-08-12T03:36:46.875Z]       Content root path: (null)
```

## Publishing for Azure Functions
When publishing, specify a Runtime Identifier and set the `OafPublish` property to any non-empty value. Example:

```
dotnet publish -r linux-x64 -p:OafPublish=true --output api_publish .\src\FunctionAPI\FunctionAPI.csproj
```

The above settings and the MSBuild targets/props shipped with the nuget package will create a runtime-specific
output that is R2R, while not being single file. 

Not being a 'single file' output is important as the function runtime inspects the accompanying dll files.
