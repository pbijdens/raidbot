using Botje.Core;
using Botje.Core.Commands;
using Botje.Core.Loggers;
using Botje.Core.Services;
using Botje.Core.Utils;
using Botje.DB;
using Botje.Messaging;
using Botje.Messaging.PrivateConversation;
using Botje.Messaging.Services;
using Botje.Messaging.Telegram;
using Ninject;
using System;
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
            kernel.Bind<IConsoleCommand>().ToConstant(new ListCommand { }).InSingletonScope();
            kernel.Bind<IConsoleCommand>().ToConstant(new UpdateCommand { }).InSingletonScope();
            kernel.Bind<IConsoleCommand>().ToConstant(new ExitCommand { TokenSource = source }).InSingletonScope();
            kernel.Bind<IConsoleCommand>().To<PingCommand>().InSingletonScope();
            kernel.Bind<IConsoleCommand>().To<HelpCommand>().InSingletonScope();
            kernel.Bind<IConsoleCommand>().To<LogLevelCommand>().InSingletonScope();
            kernel.Bind<IConsoleCommand>().To<TgCommands.MeCommand>().InSingletonScope();

            // Set up the components
            kernel.Bind<IBotModule>().To<RaidBot.RaidCreationWizard>().InSingletonScope();
            kernel.Bind<IBotModule>().To<RaidBot.RaidEventHandler>().InSingletonScope();
            kernel.Bind<IBotModule>().To<RaidBot.WhereAmI>().InSingletonScope();
            kernel.Bind<IBotModule>().To<RaidBot.RaidStatistics>().InSingletonScope();
            kernel.Bind<IBotModule>().To<RaidBot.Alias>().InSingletonScope();
            kernel.Bind<IBotModule>().To<RaidBot.Level>().InSingletonScope();
            kernel.Bind<IBotModule>().To<RaidBot.CleanupChannel>().InSingletonScope();

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
