using Discord;
using Discord.Interactions;
using Discord.WebSocket;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using System;
using System.Threading;
using System.Threading.Tasks;

using DoorBot;

namespace DoorBot
{
    class Program
    {
        // Entry point of the program.
        static async Task Main(string[] args)
        {
            // One of the more flexable ways to access the configuration data is to use the Microsoft's Configuration model,
            // this way we can avoid hard coding the environment secrets. I opted to use the Json and environment variable providers here.
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables(prefix: "DC_")
                .AddJsonFile("Config/config.json", optional: true)
                .Build();

            DoorUserDB db = DoorUserDB.GetInstance(config);

            //NFCReader dc = new();



            Task bott = BotRunAsync(config);

            GpioOutput.StartupBeeps();

            _ = Task.Run(NfcLoop);

            //await Task.WhenAll(bott, doort);

            await bott;

            //door.Dispose();
            Console.WriteLine("EXITING");
            //Console.ReadLine();

        }

        static void NfcLoop()
        {
            try
            {
                using var dc = new NFCReader();
                while (true)
                {

                    dc.ReadMiFare();

                    Console.WriteLine("New Loop");
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Environment.Exit(1);
            }
        }

        static async Task BotRunAsync(IConfiguration configuration)
        {
            // Dependency injection is a key part of the Interactions framework but it needs to be disposed at the end of the app's lifetime.
            await using var services = ConfigureServices(configuration);

            var client = services.GetRequiredService<DiscordSocketClient>();
            var commands = services.GetRequiredService<InteractionService>();

            client.Log += LogAsync;
            commands.Log += LogAsync;

            // Slash Commands and Context Commands are can be automatically registered, but this process needs to happen after the client enters the READY state.
            // Since Global Commands take around 1 hour to register, we should use a test guild to instantly update and test our commands. To determine the method we should
            // register the commands with, we can check whether we are in a DEBUG environment and if we are, we can register the commands to a predetermined test guild.
            client.Ready += async () =>
            {
                if (IsDebug())
                {
                    // Id of the test guild can be provided from the Configuration object
                    await commands.RegisterCommandsToGuildAsync(configuration.GetValue<ulong>("testGuild"), true);
                    await commands.RegisterCommandsToGuildAsync(configuration.GetValue<ulong>("roommatesGuild"), true);
                }
                else
                    await commands.RegisterCommandsGloballyAsync(true);
            };

            // Here we can initialize the service that will register and execute our commands
            await services.GetRequiredService<CommandHandler>().InitializeAsync();

            // Bot token can be provided from the Configuration object we set up earlier
            await client.LoginAsync(TokenType.Bot, configuration["token"]);
            await client.StartAsync();

            await Task.Delay(Timeout.Infinite);
        }

        static Task LogAsync(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }

        static ServiceProvider ConfigureServices(IConfiguration configuration)
            => new ServiceCollection()
                .AddSingleton(configuration)
                .AddSingleton<DiscordSocketClient>()
                .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
                .AddSingleton<CommandHandler>()
                .BuildServiceProvider();

        static bool IsDebug()
        {
#if DEBUG
            return true;
#else
                return false;
#endif
        }
    }
}