using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using System;
using System.Threading.Tasks;
using VegasVibes.commands;
using VegasVibes.config;

namespace VegasVibes
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        private static Balances BalancesManager { get; set; }

        static async Task Main(string[] args)
        {
            // Initialize JSONReader to grab hidden token
            var jsonReader = new JSONReader();
            await jsonReader.ReadJSON();

            // Initialize BalancesManager
            BalancesManager = new Balances();
            await BalancesManager.LoadBalancesAsync();

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = jsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            Client = new DiscordClient(discordConfig);

            // Enable interactivity
            Client.UseInteractivity(new InteractivityConfiguration
            {
                Timeout = TimeSpan.FromSeconds(30) // Default timeout for interactive commands
            });

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] { "!" }
            };

            Commands = Client.UseCommandsNext(commandsConfig);

            // Assign BalancesManager to the Gambling class
            Gambling.BalancesManager = BalancesManager;

            // Register commands
            Commands.RegisterCommands<Gambling>();

            await Client.ConnectAsync();

            // Save balances on shutdown
            AppDomain.CurrentDomain.ProcessExit += async (s, e) => await BalancesManager.SaveBalancesAsync();

            await Task.Delay(-1);
        }
    }
}
