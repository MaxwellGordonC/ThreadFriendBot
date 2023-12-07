using DSharpPlus.Net.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThreadFriendBot.config
{
    internal class JSONReader
    {
        public string token { get; set; }
        public long Frequency { get; set; }

        public async Task ReadJSON()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {
                // MaxG: Read the JSON file.
                string json = await sr.ReadToEndAsync();

                JSONStructure data = JsonConvert.DeserializeObject<JSONStructure>(json);

                this.token = data.token;
                this.Frequency = data.Frequency;
            }
        }
    }

    internal sealed class JSONStructure
    {
        public string token { get; set; }
        public long Frequency { get; set; }
    }
}
