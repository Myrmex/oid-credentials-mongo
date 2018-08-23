# OpenIdDict Credentials Flow for WebAPI

AspNet Core 2.1.3 - OpenIdDict 2.0.0-rtm-1090 - MongoDB (see <https://github.com/Myrmex/oid-credentials> for SQL-based database)

To use a Docker container for MongoDB, rather than installing a local copy:

	docker run --name mongo -d -p 27017:27017 mongo --noauth

Once this container has been created, start it again with:

	docker container start mongo

## References

- <https://github.com/openiddict>
- <https://github.com/openiddict/openiddict-samples/tree/dev/samples/PasswordFlow>: official sample
- <https://github.com/openiddict/openiddict-core/blob/dev/samples/>: up-to-date samples.
- <https://github.com/openiddict/openiddict-core/issues/593>: latest changes.
- <https://github.com/alexandre-spieser/AspNetCore.Identity.MongoDbCore>: MongoDB user and role store adapter.
- <https://github.com/serilog/serilog-sinks-mongodb>: Serilog MongoDB sink.

## Quick Test

Sample token request:

```
POST http://localhost:53736/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=password&scope=offline_access profile email roles&resource=http://localhost:4200&username=zeus&password=P4ssw0rd!
```

After getting the token in the response, make requests like:

```
GET http://localhost:53736/api/values
Content-Type: application/json
Authorization: Bearer ...
```

## Instructions

1.create a new WebAPI app without any authentication.

2.add the appropriate MyGet repositories to your NuGet sources. This can be done by adding a new `NuGet.Config` file at the root of your solution:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="NuGet" value="https://api.nuget.org/v3/index.json" />
    <add key="aspnet-contrib" value="https://www.myget.org/F/aspnet-contrib/api/v3/index.json" />
  </packageSources>
</configuration>
```

3.ensure that you have these packages in the project (you can list them using a NuGet command like `get-package | Format-Table -AutoSize` in the NuGet console):

```
install-package AspNetCore.Identity.MongoDbCore -pre
install-package AspNet.Security.OAuth.Validation -pre
install-package OpenIddict -pre
install-package OpenIddict.MongoDB -pre
install-package OpenIddict.Mvc -pre
install-package MailKit
install-package Serilog
install-package Serilog.Sinks.MongoDB
install-package Swashbuckle.AspNetCore 
```

MailKit can be used for mailing, Swashbuckle.AspNetCore for Swagger, Serilog for logging. Here we are logging to MongoDB, too.

4.should you want to configure logging or other services, do it in `Program.cs`. Usually, the default configuration already does all what is typically required. See <https://joonasw.net/view/aspnet-core-2-configuration-changes>. E.g. to configure logging with Serilog:

```cs
public static void Main(string[] args)
{
    // see http://www.carlrippon.com/?p=1118
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddJsonFile(
            $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json",
            optional: true)
        .Build();

    // https://github.com/serilog/serilog-aspnetcore
    string maxSize = configuration["Serilog:MaxMbSize"];
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.MongoDBCapped(configuration["Serilog:ConnectionString"],
            ParseLogLevel(configuration["Serilog:MinLevel"], LogEventLevel.Information),
            !String.IsNullOrEmpty(maxSize) && Int32.TryParse(maxSize, out int n) && n > 0 ? n : 10)
        .CreateLogger();

    BuildWebHost(args).Run();
}

public static IWebHost BuildWebHost(string[] args)
{
    // https://joonasw.net/view/aspnet-core-2-configuration-changes

    return WebHost.CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        .Build();
}

private static LogEventLevel ParseLogLevel(string text, LogEventLevel @default)
{
    if (String.IsNullOrEmpty(text) ||
        Array.IndexOf(new[] { "verbose", "debug", "information", "warning", "error", "fatal" },
        text.ToLowerInvariant()) == -1) return @default;
    return Enum.Parse<LogEventLevel>(text, true);
}
```

5.under `Models`, add identity models (`ApplicationUser`, `ApplicationRole`).

6.under `Services`, add `DatabaseInitializer` and its MongoDB-based implementation.

7.add the connection string to `appsettings.json`. You will then override it using an environment variable source (or a production-targeted version of appsettings) for production. E.g.:

```json
  "Auth": {
    "ConnectionString": "mongodb://localhost:27017/oid-auth",
    "DatabaseName": "oid-auth"
  },
  "Data": {
    "DefaultConnection": {
      "ConnectionString": "Server=(local)\\SqlExpress;Database=oid;Trusted_Connection=True;MultipleActiveResultSets=true;"
    }
  },
  "Serilog": {
    "ConnectionString": "mongodb://localhost:27017/oid-logs",
    "MinLevel": "Information",
    "MaxMbSize":  10
  }
```

8. `Startup/ConfigureServices`: see code, which registers the identity services to use MongoDB stores with our models; configures identity to use the same JWT claims as OpenIddict; adds OpenIdDict, letting it use MongoDB (configuration data are read from `appsettings.json`), and configuring server options. It also adds the database seed service, and Swagger. Note: if deploying to Azure, ensure to CORS-enable your web app in the portal, too.

9. in `Startup/Configure`, add OpenIddict and the OAuth2 token validation middleware in your ASP.NET Core pipeline by calling `app.UseOAuthValidation()` and `app.UseOpenIddict()` after `app.UseIdentity()` and before `app.UseMvc()`: see code. Also note that here we seed the database using the injected service (see nr.6 above).

10.under `Controllers`, add `AuthorizationController.cs`.

**Note**: to secure your API, add an `[Authorize]` or `[Authorize(Roles = "some roles here")]` attribute to your controller or controller's method. Note: *you should define the authentication scheme for this attribute, to avoid redirection to a login page* (and thus a 404 from your client): i.e. use `[Authorize(AuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]`. See <https://github.com/openiddict/openiddict-core/blob/dev/samples/Mvc.Server/Controllers/ResourceController.cs#L9>.
