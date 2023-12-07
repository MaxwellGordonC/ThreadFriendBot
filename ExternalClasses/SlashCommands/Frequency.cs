using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThreadFriendBot.config;

namespace ThreadFriendBot.External_Classes.Slash_Commands
{
    // MaxG: Commands related to frequency of messaging.
    public class Frequency : ApplicationCommandModule
    {
        public async Task SetFrequency(long NewFrequency)
        {
            StreamReader sr = new StreamReader("config.json");

            // MaxG: Read the JSON file.
            string json = await sr.ReadToEndAsync();

            // MaxG: Close the file after reading it!
            sr.Close();

            // MaxG: Parse the file and fix the data into the JSONStructure class structure.
            JSONStructure config = JsonConvert.DeserializeObject<JSONStructure>(json);
            // MaxG: Set the new frequency
            config.Frequency = NewFrequency;

            // MaxG: Write the frequency back into the file.
            StreamWriter sw = new StreamWriter("config.json");
            string updated_config = JsonConvert.SerializeObject(config, Formatting.Indented);
            
            await sw.WriteAsync(updated_config);
            // MaxG: Flushing ensures all the data was actually written. Not doing this will cause it to exit
            //       before anything actually gets written.
            await sw.FlushAsync();
            sw.Close();
        }

        [SlashCommand("frequency", "Set the frequency in days of when to send a friendly message.")]
        public async Task SCFrequency(InteractionContext ctx, [Option("frequency", "message frequency (in whole days)")] long frequency)
        {
            await SetFrequency(frequency);

            // MaxG: Send a message to the user that the frequency has been set.
            await ctx.CreateResponseAsync(DSharpPlus.InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent($"Set frequency to {frequency} days."));
        }
    }
}
