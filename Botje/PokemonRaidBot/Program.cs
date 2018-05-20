using Botje.Core;
using Botje.Core.Commands;
using Botje.Core.Loggers;
using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.PrivateConversation;
using Botje.Messaging.Telegram;
using NGettext;
using Ninject;
using PokemonRaidBot.LocationAPI;
using PokemonRaidBot.Utils;
using System;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace PokemonRaidBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var settings = new JsonSettingsReader();

            // These two settings files are excluded for the GIT solution
#if DEBUG
            settings.Read("settings.debug.json", "default-settings.json");
#else
            settings.Read("settings.release.json", "default-settings.json");
#endif
            TimeUtils.Initialize(settings.Timezones);

            var kernel = new StandardKernel();
            kernel.Bind<ILoggerFactory>().To<ConsoleLoggerFactory>();
            kernel.Bind<ISettingsManager>().ToConstant(settings);

            // I18N
            ICatalog catalog;
            if (string.IsNullOrEmpty(settings.Language))
            {
                catalog = new Catalog("raidbot", "i18n", new CultureInfo("en-US"));
            }
            else
            {
                catalog = new Catalog("raidbot", "i18n", new CultureInfo(settings.Language));
            }
            kernel.Bind<ICatalog>().ToConstant(catalog);
            kernel.Bind<ITimeService>().To<TimeService>();

            // Core services
            var database = kernel.Get<Database>();
            database.Setup(settings.DataFolder);
            kernel.Bind<IDatabase>().ToConstant(database);
            kernel.Bind<IPrivateConversationManager>().To<PrivateConversationManager>().InSingletonScope();

            // Google location API
            var googleLocationAPIService = kernel.Get<GoogleAddressService>();
            googleLocationAPIService.SetApiKey(settings.GoogleLocationAPIKey);
            kernel.Bind<ILocationToAddressService>().ToConstant(googleLocationAPIService);

            // Set up the messaging client
            CancellationTokenSource source = new CancellationTokenSource();
            TelegramClient client = kernel.Get<ThrottlingTelegramClient>();
            client.Setup(settings.BotKey, source.Token);
            kernel.Bind<IMessagingClient>().ToConstant(client);

            // Set up the console commands
            var helpCommand = new HelpCommand();
            kernel.Bind<IConsoleCommand>().To<PingCommand>().InSingletonScope();
            kernel.Bind<IConsoleCommand>().To<HelpCommand>().InSingletonScope();
            kernel.Bind<IConsoleCommand>().To<LogLevelCommand>().InSingletonScope();
            kernel.Bind<IConsoleCommand>().ToConstant(new ConsoleCommands.ListCommand { }).InSingletonScope();
            kernel.Bind<IConsoleCommand>().ToConstant(new ConsoleCommands.UpdateCommand { }).InSingletonScope();
            kernel.Bind<IConsoleCommand>().ToConstant(new ConsoleCommands.ExitCommand { TokenSource = source }).InSingletonScope();
            kernel.Bind<IConsoleCommand>().To<ConsoleCommands.MeCommand>().InSingletonScope();

            // Set up the components
            kernel.Bind<IBotModule>().To<Modules.RaidCreationWizard>().InSingletonScope();
            kernel.Bind<IBotModule>().To<Modules.RaidEditor>().InSingletonScope();
            kernel.Bind<IBotModule>().To<Modules.RaidEventHandler>().InSingletonScope();
            kernel.Bind<IBotModule>().To<Modules.CleanupChannel>().InSingletonScope();

            kernel.Bind<IBotModule>().To<ChatCommands.WhoAmI>().InSingletonScope();
            kernel.Bind<IBotModule>().To<ChatCommands.WhereAmI>().InSingletonScope();
            kernel.Bind<IBotModule>().To<ChatCommands.RaidStatistics>().InSingletonScope();
            kernel.Bind<IBotModule>().To<ChatCommands.Alias>().InSingletonScope();
            kernel.Bind<IBotModule>().To<ChatCommands.Level>().InSingletonScope();

            var modules = kernel.GetAll<IBotModule>().ToList();

            // Start the system
            modules.ForEach(m => m.Startup());
            client.Start();

            // Runt the console loop in the background
            var consoleLoop = kernel.Get<ConsoleLoop>();
            consoleLoop.Run(source.Token);

            // Shut down the modules
            modules.ForEach(m => m.Shutdown());

            Console.WriteLine("Program terminated. Have a nice day.");
        }
    }
}
