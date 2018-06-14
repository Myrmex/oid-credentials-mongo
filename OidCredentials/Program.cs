using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace OidCredentials
{
    public static class Program
    {
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
    }
}
