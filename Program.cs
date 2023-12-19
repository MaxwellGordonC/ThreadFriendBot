using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using ThreadFriendBot.config;
using ThreadFriendBot.External_Classes.Slash_Commands;

namespace ThreadFriendBot
{
    internal class Program
    {
        const double DAY_THRESHOLD = 7.0;
        const double CHECK_HOURS = 12.0;
        const int THREAD_MESSAGE_DELAY = 1000;

        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        private static System.Timers.Timer ThreadTimer;

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

            // MaxG: Subscribe the ClientReady function to Ready.
            //       Ready is triggered on connecting to servers, when the bot is ready to process events.
            Client.Ready += ClientReady;

            // MaxG: On resume, start the timer again.
            Client.Resumed += ClientResumed;

            Client.ThreadCreated += JoinThread;

            // MaxG: Register the slash commands.
            var SlashCommandsConfig = Client.UseSlashCommands();
            SlashCommandsConfig.RegisterCommands<Frequency>();


            ThreadTimer = new System.Timers.Timer(TimeSpan.FromHours(CHECK_HOURS).TotalMilliseconds);
            ThreadTimer.Elapsed += OnTimedEvent;
            ThreadTimer.AutoReset = true;
            ThreadTimer.Enabled = true;

            await Client.ConnectAsync();

            // MaxG: Loop on startup.
            await Task.Delay(THREAD_MESSAGE_DELAY);
            await LoopAllThreads();

            // MaxG: Keep the bot running infinitely (-1).
            await Task.Delay(-1);
        }

        private static Task ClientResumed(DiscordClient sender, ReadyEventArgs args)
        {
            // MaxG: Resume the timer after the connection resumes.
            ThreadTimer.Enabled = true;

            return Task.CompletedTask;
        }

        private static void OnTimedEvent(Object source, System.Timers.ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                await LoopAllThreads();
            });
        }

        private static async Task<Task> JoinThread(DiscordClient sender, ThreadCreateEventArgs args)
        {
            await args.Thread.JoinThreadAsync();
            return Task.CompletedTask;
        }

        private static Task ClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        private static async Task<Task> LoopAllThreads()
        {
            Console.WriteLine("Looping all threads");
            var AllThreads = Client.Guilds.SelectMany(guild => guild.Value.Threads);


            foreach (var Thread in AllThreads)
            {
                Console.WriteLine("Checking Thread " + Thread.Value.Name);

                await CheckLastThreadMessage(Thread.Value);
                
                // MaxG: Delay to reduce spam.
                await Task.Delay(THREAD_MESSAGE_DELAY);
            }
            
            return Task.CompletedTask;
        }

        private static async Task<Task> CheckLastThreadMessage(DiscordThreadChannel Thread)
        {
            // MaxG: Retrieve the most recent message.
            var messages = await Thread.GetMessagesAsync(1);

            if ( messages.Any() )
            {
                var message = messages.First();

                // MaxG: Get the timestamp.
                DateTimeOffset message_time = message.Timestamp;

                // MaxG: Standardize.
                message_time = message_time.ToUniversalTime();

                TimeSpan difference = DateTimeOffset.UtcNow - message_time;

                Console.WriteLine("The day difference is " + difference.Days);

                // MaxG: Check if it has been too many days since the last message.
                if ( difference.Days > DAY_THRESHOLD )
                {
                    // MaxG: Send a message.
                    await Thread.SendMessageAsync("Hi friend, just keeping the thread alive :slight_smile:");
                }
            }

            return Task.CompletedTask;
        }
    }
}