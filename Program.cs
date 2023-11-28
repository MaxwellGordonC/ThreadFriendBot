using DSharpPlus;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using ThreadFriendBot.config;

namespace ThreadFriendBot
{
    internal class Program
    {
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }

        static async Task Main(string[] args)
        {
            // MaxG: Read the config JSON and grab the bot token.
            var JsonReader = new JSONReader();
            await JsonReader.ReadJSON();

            var DiscordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = JsonReader.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true
            };

            // MaxG: Create a new instance of the client with this configuration.
            Client = new DiscordClient(DiscordConfig);

            Client.Ready += ClientReady;

            await Client.ConnectAsync();
            // MaxG: Keep the bot running infinitely (-1).
            await Task.Delay(-1);
        }

        private static Task ClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
