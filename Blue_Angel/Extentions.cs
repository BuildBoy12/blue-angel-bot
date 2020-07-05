using Discord;
using Discord.WebSocket;
using System.Threading.Tasks;
using System;

namespace BlueAngel
{
    public static class Extentions
    {
        public static async Task DIMessage(string message = "[Message not set]", bool isTTS = false, Embed embed = null, ulong server = 529804945646682171, ulong channel = 550737505901740032)
        {
            try
            {
                SocketGuild curServer = Bot.Client.GetGuild(server);
                SocketTextChannel curChannel = curServer.GetTextChannel(channel);
                await curChannel.SendMessageAsync(message, isTTS, embed);
                Program.Log($"Message sent to {curServer.Name}, {curChannel.Name}: {message}.", true);
            }
            catch(NullReferenceException)
            {
                Program.Error("Unable to reach server.");
            }          
        }
    }
}
