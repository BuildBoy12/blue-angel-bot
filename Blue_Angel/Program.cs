using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Newtonsoft.Json;

namespace BlueAngel
{
    public class Program
    {
        public static Bot _bot;
        public static Config Config = GetConfig();
        public static bool fileLocked = false;
        private static string LogFile;

        static void Main()
        {
            new Program();
        }

        public Program()
        {
            string path = $"{Directory.GetCurrentDirectory()}/logs/{DateTime.UtcNow.Ticks}.txt";
            Log($"Creating log file: {path}", true);
            if (!Directory.Exists($"{Directory.GetCurrentDirectory()}/logs"))
                Directory.CreateDirectory($"{Directory.GetCurrentDirectory()}/logs");
            if (!File.Exists(path))
                File.Create(path).Close();
            LogFile = path;
            _bot = new Bot(this);
        }

        public static Config GetConfig()
        {
            Config result;
            if (File.Exists("Config.json"))
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

        public static Task Log(LogMessage msg)
        {
            Console.Write(msg.ToString() + Environment.NewLine);
            while (fileLocked)
                Thread.Sleep(1000);

            if (LogFile != null)
            {
                fileLocked = true;
                File.AppendAllText(LogFile, msg.ToString() + Environment.NewLine);
            }

            fileLocked = false;
            return Task.CompletedTask;
        }

        public static void Log(string message, bool debug = false)
        {
            if (!debug)
                Log(new LogMessage(LogSeverity.Info, "LOG", message));
            else if (Config.Debug)
                Log(new LogMessage(LogSeverity.Debug, "DEBUG", message));
        }

        public static void Error(string message) => Log(new LogMessage(LogSeverity.Debug, "ERROR", message));
    }
}
