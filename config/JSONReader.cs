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

        public async Task ReadJSON()
        {
            using (StreamReader sr = new StreamReader("config.json"))
            {

            }
        }
    }

    internal sealed class JSONStructure
    {
        public string token { get; set; }
    }
}
