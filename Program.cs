using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;
//using ThreadFriendBot.External_Classes.Slash_Commands;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;
using System.Text;


namespace ThreadFriendBot
{
    public sealed class BotConfiguration
    {
        public string Token { get; set; }
        public double DayThreshold { get; set; }
        public double CheckHours { get; set; }
        public int MessageDelay { get; set; }
        public string[] Messages { get; set; }
        public ulong[]? UserMentions { get; set; }
        public int RepeatMentionThreshold { get; set; }
        public ulong? TicketBotID { get; set; }
        public string NotAThreadMessage { get; set; }
    }

    internal class Program
    {      
        private static DiscordClient Client { get; set; }
        private static CommandsNextExtension Commands { get; set; }
        private static System.Timers.Timer ThreadTimer;

        private static IConfigurationRoot Config;
        private static BotConfiguration BotConfig;

        private static Random Rand = new Random();

        private static string GetRandomMessage()
        {
            string[] msgs = BotConfig.Messages;
            int rand_idx = Rand.Next(0, msgs.Length);

            return msgs[rand_idx];
        }

        // MaxG: Return a string in the format of "message [number of repeats]".
        private static async Task<string> GetRandThreadMsg(DiscordMessage PreviousMsg)
        {
            // MaxG: Edge case; starting message;
            if (PreviousMsg == null || PreviousMsg.Author.Id != Client.CurrentUser.Id)
            {
                return $"{GetRandomMessage()} `[1]`";
            }

            // MaxG: Parse using regex to extract the number of repeats.
            string pattern = @"\`\[(\d+)\]\`";
            Match match = Regex.Match(PreviousMsg.Content, pattern);

            if (match.Success)
            {
                // MaxG: Extract int.
                int repeats = int.Parse(match.Groups[1].Value) + 1;

                string result = $"{GetRandomMessage()} `[{repeats}]`";

                int repeat_threshold = BotConfig.RepeatMentionThreshold;

                // MaxG: See if there have been too many repeats.
                //                                      Check if NOT null and not empty.
                if ( repeats >= repeat_threshold && BotConfig.UserMentions?.Length > 0 )
                {
                    Console.WriteLine($"Number of repeats, {repeats}, is greater than {repeat_threshold}. Mentioning users.");
                    ulong[] mentions = BotConfig.UserMentions;

                    StringBuilder sb = new StringBuilder(result);

                    for (int i = 0; i < mentions.Length; i++)
                    {
                        DiscordUser user = await Client.GetUserAsync(mentions[i]);
                        sb.Append($" {user.Mention} ");
                        Console.WriteLine($"Mentioning {user.Username}");
                    }

                    result = sb.ToString();
                }

                return result;
            }

            // MaxG: Fallback. This should not happen. If it does, a previous version may be conflicting.
            Console.WriteLine($"GetRandThreadMsg({PreviousMsg}) ==> parsing failed.");
            return $"{GetRandomMessage()} `[1]`";
        }

        private static void OnConfigChanged(object state)
        {
            Console.WriteLine("config.json has been updated.");
        }

        static async Task Main(string[] args)
        {
            Config = new ConfigurationBuilder().AddJsonFile("config.json", optional: false, reloadOnChange: true).Build();
            BotConfig = new();

            Config.Bind(BotConfig);
            
            // MaxG: Read the config JSON and grab the bot token.
            Config.Reload();

            // MaxG: Subscribe to Changed.
            Config.GetReloadToken().RegisterChangeCallback(OnConfigChanged, null);
                       
            var DiscordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = BotConfig.Token,
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

            // MaxG: Join the thread and check to see if it is a ticket.
            Client.ThreadCreated += JoinThread;

            // MaxG: Register the slash commands.
            //var SlashCommandsConfig = Client.UseSlashCommands();
            //SlashCommandsConfig.RegisterCommands<DayThreshold>();

            ThreadTimer = new System.Timers.Timer(TimeSpan.FromHours( BotConfig.CheckHours ).TotalMilliseconds);
            ThreadTimer.Elapsed += OnTimedEvent;
            ThreadTimer.AutoReset = true;
            ThreadTimer.Enabled = true;

            await Client.ConnectAsync();

            // MaxG: Loop on startup.
            await Task.Delay( BotConfig.MessageDelay );
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
            // MaxG: Wait just to ensure all users have joined.
            await Task.Delay(BotConfig.MessageDelay);

            // MaxG: Loop all users.
            foreach (DiscordUser user in args.Thread.Users)
            {
                if (user.Id == BotConfig.TicketBotID)
                {
                    StringBuilder sb = new StringBuilder(BotConfig.NotAThreadMessage);

                    // MaxG: Optionally mention users.
                    if (BotConfig.UserMentions?.Length > 0)
                    {
                        ulong[] mentions = BotConfig.UserMentions;

                        for (int i = 0; i < mentions.Length; i++)
                        {
                            DiscordUser u = await Client.GetUserAsync(mentions[i]);
                            sb.Append($" {u.Mention} ");
                        }
                    }

                    string result = sb.ToString();
                    await args.Thread.SendMessageAsync(result);

                    return Task.CompletedTask;
                }
            }

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
                await Task.Delay(BotConfig.MessageDelay);
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

                Console.WriteLine($"The day difference is {difference.TotalDays}");

                // MaxG: Check if it has been too many days since the last message.
                if (difference.TotalDays >= BotConfig.DayThreshold )
                {
                    Console.WriteLine("Sending a message!");

                    // MaxG: Send a message.
                    await Thread.SendMessageAsync( await GetRandThreadMsg(message) );

                    // MaxG: Reduce spam.
                    if (message.Author.Id == Client.CurrentUser.Id)
                    { 
                        await Thread.DeleteMessageAsync(message);
                    }
                }
            }

            return Task.CompletedTask;
        }
    }
}