using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TinfoilWebServer.ConsoleLogging;
using TinfoilWebServer.Services;
using TinfoilWebServer.Settings;

namespace TinfoilWebServer
{
    public class Program
    {
        public static string CurrentDirectory { get; }
        public static string ConfigFileName { get; }

        static Program()
        {
            CurrentDirectory = Directory.GetCurrentDirectory();
            ConfigFileName = $"{Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location)}.config.json";
        }

        public static void Main(string[] args)
        {
            var configRoot = new ConfigurationBuilder()
                .SetBasePath(CurrentDirectory)
                .AddJsonFile(ConfigFileName, optional: true, reloadOnChange: true)
                .Build();

            var appSettings = AppSettingsLoader.Load(configRoot);

            var webHostBuilder = new WebHostBuilder()
                .SuppressStatusMessages(true)
                .ConfigureLogging((hostingContext, loggingBuilder) =>
                {
                    loggingBuilder
                        .AddConsoleFormatter<CustomConsoleFormatter, CustomConsoleFormatterOptions>(options => { })
                        .AddConfiguration(appSettings.LoggingConfig)
                        .AddConsole(options => options.FormatterName = nameof(CustomConsoleFormatter));
                })
                .ConfigureServices(services =>
                {
                    services
                        .AddSingleton(appSettings)
                        .AddSingleton<IRequestManager, RequestManager>()
                        .AddSingleton<ITinfoilIndexBuilder, TinfoilIndexBuilder>()
                        .AddSingleton<IServedDirAliasMap, ServedDirAliasMap>()
                        .AddSingleton<IPhysicalPathConverter, PhysicalPathConverter>()
                        .AddSingleton<IFileFilter, FileFilter>();
                })
                .UseConfiguration(configRoot)
                .UseKestrel((ctx, options) =>
                {
                    options.Configure(appSettings.KestrelConfig);
                })
                .UseStartup<Startup>();

            webHostBuilder.Build().Run();
        }


    }
}