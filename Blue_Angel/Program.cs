using Newtonsoft.Json;
using System;
using System.IO;

namespace BlueAngel
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Wassup homie!");
            new Program();
        }

        public Program()
        {
            Console.WriteLine("Initializing..");
            _bot = new Bot(this);
        }

        public static Config GetConfig()
        {
            bool flag = File.Exists("Config.json");
            Config result;
            if (flag)
            {
                result = JsonConvert.DeserializeObject<Config>(File.ReadAllText("Config.json"));
            }
            else
            {
                File.WriteAllText("Config.json", JsonConvert.SerializeObject(Config.Default));
                result = Config.Default;
            }
            return result;
        }

        public static Bot _bot;

        public static Config Config = GetConfig();

        public static bool fileLocked = false;
    }
}
