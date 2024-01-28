using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;
using ThreadFriendBot.External_Classes.Slash_Commands;
using Microsoft.Extensions.Configuration;

namespace ThreadFriendBot
{
    internal class Program
    {
        //const double DAY_THRESHOLD = 7.0;
        //const double CHECK_HOURS = 12.0;
        //const int THREAD_MESSAGE_DELAY = 1000;
        
        const string CONF_TOKEN = "Token";
        const string CONF_DAY_THRESHOLD = "DayThreshold";
        const string CONF_CHECK_HOURS = "CheckHours";
        const string CONF_MESSAGE_DELAY = "MessageDelay";
        const string CONF_MESSAGES = "Messages";

        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        private static System.Timers.Timer ThreadTimer;

        private static IConfigurationRoot Config = new ConfigurationBuilder().AddJsonFile("config.json", optional: false, reloadOnChange: true).Build();

        private static string GetToken() { return Config[CONF_TOKEN]; }
        private static double GetDayThreshold() { return double.Parse( Config[CONF_DAY_THRESHOLD] ); }
        private static double GetCheckHours() { return double.Parse( Config[CONF_CHECK_HOURS] ); }
        private static int GetMessageDelay() { return Int32.Parse( Config[CONF_MESSAGE_DELAY] ); }
        private static string[] GetMessages()
        {
            // MaxG: Get all messages as an array of strings.
            return Config.GetSection(CONF_MESSAGES).GetChildren().Select(child => child.Value).ToArray();
        }
        private static Random Rand = new Random();

        // TODO: Append message number at the end of the message.
        private static string GetRandomMessage()
        {
            string[] msgs = GetMessages();
            int rand_idx = Rand.Next(0, msgs.Length);

            return msgs[rand_idx];
        }

        static async Task Main(string[] args)
        {
            // MaxG: Read the config JSON and grab the bot token.
            Config.Reload();
                       
            var DiscordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = GetToken(),
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

            // MaxG: Register the slash commands.
            var SlashCommandsConfig = Client.UseSlashCommands();
            SlashCommandsConfig.RegisterCommands<DayThreshold>();

            ThreadTimer = new System.Timers.Timer(TimeSpan.FromHours( GetCheckHours() ).TotalMilliseconds);
            ThreadTimer.Elapsed += OnTimedEvent;
            ThreadTimer.AutoReset = true;
            ThreadTimer.Enabled = true;

            await Client.ConnectAsync();

            // MaxG: Loop on startup.
            await Task.Delay(GetMessageDelay());
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


        private static Task ClientReady(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        private static async Task<Task> LoopAllThreads()
        {
            Console.WriteLine("Looping all threads");
            var AllThreads = Client.Guilds.SelectMany(guild => guild.Value.Threads);

            Console.WriteLine("Beginning thread checks at " + DateTime.Now);
            foreach (var Thread in AllThreads)
            {
                Console.WriteLine("Checking Thread " + Thread.Value.Name);
               
                // MaxG: Skip the thread entirely if it's locked.
                bool? IsLocked = Thread.Value.ThreadMetadata.IsLocked;
                if ( IsLocked.HasValue && IsLocked.Value )
                {
                    Console.WriteLine("Thread " + Thread.Value.Name + " is locked. Skipping.");
                    continue;
                }

                await CheckLastThreadMessage(Thread.Value);
                
                // MaxG: Delay to reduce spam.
                await Task.Delay( GetMessageDelay() );
            }
            Console.WriteLine("Ending thread checks at " + DateTime.Now);

            return Task.CompletedTask;
        }

        private static async Task<Task> CheckLastThreadMessage(DiscordThreadChannel Thread)
        {
            // MaxG: Retrieve the most recent message.
            var messages = await Thread.GetMessagesAsync(1);

            if ( messages.Any() )
            {
                var message = messages[0];

                // MaxG: Get the timestamp.
                DateTimeOffset message_time = message.Timestamp;

                // MaxG: Standardize.
                message_time = message_time.ToUniversalTime();

                TimeSpan difference = DateTimeOffset.UtcNow - message_time;

                Console.WriteLine("The day difference is " + difference.Days);

                // MaxG: Check if it has been too many days since the last message.
                if ( difference.Days > GetDayThreshold() )
                {
                    // MaxG: Send a message.
                    await Thread.SendMessageAsync( GetRandomMessage() );
                }
            }

            return Task.CompletedTask;
        }
    }
}