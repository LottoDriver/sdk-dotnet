using System;
using LottoDriver.CustomersApi.Sdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

using LottoDriver.Examples.CustomersApi.Common.DataAccess;

namespace LottoDriver.Examples.CustomersApi.WorkerService
{
    /// <summary>
    /// Entry point of the .NET 8 example worker. Wires Serilog, registers the
    /// LottoDriver SDK client and the SQLite database in DI, and starts the
    /// background <see cref="Worker"/>.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Exit codes:
        /// <c>0</c> normal shutdown, <c>1</c> logger could not be initialised,
        /// <c>2</c> host terminated unexpectedly.
        /// </summary>
        public static int Main(string[] args)
        {
            if (!CreateLogger()) return 1;

            try
            {
                CreateHostBuilder(args).Build().Run();
                return 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Host terminated unexpectedly!");
                return 2;
            }
        }

        /// <summary>
        /// Builds the host. Registers <see cref="ICustomersApiClient"/> and
        /// <see cref="IDatabase"/> as transient services, attaches logging
        /// handlers to the SDK error events, and adds the <see cref="Worker"/>
        /// as a hosted service.
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureServices((hostContext, services) =>
                {
                    var configuration = hostContext.Configuration;

                    services.AddTransient<ICustomersApiClient>(serviceProvider =>
                    {
                        var logger = serviceProvider.GetRequiredService<ILogger<CustomersApiClient>>();

                        var client = new CustomersApiClient(
                            configuration.GetValue<string>("AppSettings:LottoDriverApiUrl"),
                            configuration.GetValue<string>("AppSettings:LottoDriverClientId"),
                            configuration.GetValue<string>("AppSettings:LottoDriverSecret")
                        );

                        // Error: transient client-internal errors (network, HTTP, JSON).
                        // CallbackError: exceptions thrown by handlers attached to the SDK.
                        // Both are informational; the SDK keeps running either way.
                        client.Error += (source, exception) =>
                            logger.LogError(exception, "Error in lotto driver api client");

                        client.CallbackError += (source, exception) =>
                            logger.LogError(exception, "Error in lotto driver api client event handlers");

                        return client;
                    });

                    services.AddTransient<IDatabase>(_ => new SQLiteDatabase(configuration.GetValue<string>("AppSettings:DatabasePath")));

                    services.AddHostedService<Worker>();
                });
        }

        /// <summary>
        /// Initialises Serilog from <c>appsettings.json</c> (and an optional
        /// environment-specific override) before the host is built. Returns
        /// <c>false</c> when configuration is missing or invalid; the caller
        /// uses that to exit early with a non-zero code.
        /// </summary>
        private static bool CreateLogger()
        {
            try
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .Build();

                Log.Logger = new LoggerConfiguration()
                    .ReadFrom.Configuration(configuration)
                    .CreateLogger();

                return true;
            }
            catch (Exception ex)
            {
                if (Environment.UserInteractive)
                {
                    Console.WriteLine("Logger creation failed!");
                    Console.WriteLine(ex.Message);
                }

                return false;
            }
        }
    }


}
