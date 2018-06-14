# OpenIdDict Credentials Flow for WebAPI

AspNet Core 2.0 - August 2017

## References

- <https://github.com/openiddict>
- <https://github.com/openiddict/openiddict-samples/tree/dev/samples/PasswordFlow>: official sample

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
install-package AspNet.Security.OAuth.Validation -pre
install-package OpenIddict -pre
install-package OpenIddict.EntityFrameworkCore -pre
install-package OpenIddict.Mvc -pre
install-package MailKit
install-package NLog -pre
install-package Swashbuckle.AspNetCore
```

MailKit can be used for mailing, Swashbuckle.AspNetCore for Swagger, NLog for file-based logging.

4.should you want to configure logging or other services, do it in `Program.cs`. Usually, the default configuration already does all what is typically required. See https://joonasw.net/view/aspnet-core-2-configuration-changes .

5.under `Models`, add identity models (`ApplicationUser`, `ApplicationDbContext`).

6.under `Services`, add `DatabaseInitializer`.

7.add your database connection string to `appsettings.json`. You will then override it using an environment variable source (or a production-targeted version of appsettings) for production. E.g.:

```json
  "Data": {
    "DefaultConnection": {
      "ConnectionString": "Server=(local)\\SqlExpress;Database=oid;Trusted_Connection=True;MultipleActiveResultSets=true;"
    }
  }
```

Alternatively, just use an in-memory database.

8. `Startup/ConfigureServices`: see code. Note: if deploying to Azure, ensure to CORS-enable your web app in the portal too.

9. in `Startup/Configure`, add OpenIddict and the OAuth2 token validation middleware in your ASP.NET Core pipeline by calling `app.UseOAuthValidation()` and `app.UseOpenIddict()` after `app.UseIdentity()` and before `app.UseMvc()`: see code. Also note that here we seed the database using the injected service (see nr.6 above).

10.under `Controllers`, add `AuthorizationController.cs`.

To secure your API, add an `[Authorize]` or `[Authorize(Roles = "some roles here")]` attribute to your controller or controller's method. Note: you should define the authentication scheme for this attribute, to avoid redirection to a login page: i.e. use `[Authorize(AuthenticationSchemes = OAuthValidationDefaults.AuthenticationScheme)]`. See <https://github.com/openiddict/openiddict-core/blob/dev/samples/Mvc.Server/Controllers/ResourceController.cs#L9>.

