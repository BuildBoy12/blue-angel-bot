using System;
using System.Collections.Generic;
using System.Text;

namespace BlueAngel
{
    public class Config
    {
        public bool Debug { get; set; }
        public string BotToken { get; set; }
        public string APIKey { get; set; }
        public string Prefix { get; set; }

        public static readonly Config Default = new Config
        {
            Debug = false,
            BotToken = "",
            APIKey = "",
            Prefix = "+",
        };
    }
}
