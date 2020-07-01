using Discord;
using Discord.Commands;
using Discord.WebSocket;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ClashOfClans;
using ClashOfClans.Models;
using ClashOfClans.Search;
using System.Threading.Channels;

namespace BlueAngel
{
    public class Bot
    {
		public static DiscordSocketClient Client
		{
			get
			{
				DiscordSocketClient result;
				if ((result = client) == null)
				{
					result = (client = new DiscordSocketClient());
				}
				return result;
			}
		}

		public Bot(Program program)
		{
			this.program = program;
			InitBot().GetAwaiter().GetResult();
		}

		private async Task InitBot()
		{
			Client.Ready += RefreshStats;
			Client.MessageReceived += OnMessageReceived;
			await Client.LoginAsync(TokenType.Bot, Program.Config.BotToken);
			await Client.StartAsync();
			await Task.Delay(-1);
		}

		public static ClashOfClansClient coc = new ClashOfClansClient(Program.Config.APIKey);
		public bool announcedWarDay = false;
		public bool announcedWar1hLeft = false;

		private async Task RefreshStats()
        {
			
			Clan clan = await coc.Clans.GetClanAsync("#2VPJQP0J");
			for (; ; )
            {
				if ((bool)clan.IsWarLogPublic)
				{
					var warLog = (ClanWarLog)await coc.Clans.GetClanWarLogAsync("#2VPJQP0J");
					var e = client.GetGuild(529804945646682171);
					var d = e.GetTextChannel(550737505901740032);
					foreach (var war in warLog.Take(1))
                    {
						if (!announcedWarDay && war.EndTime.AddDays(-1) < DateTime.Now && war.EndTime > DateTime.Now)
						{												
							d.SendMessageAsync("<@> War day has now begun!");
							announcedWarDay = true;
						}							
						else if(!announcedWar1hLeft && war.EndTime.AddHours(-1) < DateTime.Now && war.EndTime > DateTime.Now)
                        {
							d.SendMessageAsync("<@> War day has one hour remaining!");
							announcedWar1hLeft = true;
						}
						else if (announcedWarDay && war.EndTime < DateTime.Now)
						{
							announcedWarDay = false;
							announcedWar1hLeft = false;
						}
					}
				} 			
				await Task.Delay(10000);
            }
        }

		public string message;
		public async Task OnMessageReceived(SocketMessage msg)
		{
			if (!msg.Content.StartsWith("+"))
				return;

			message = msg.Content.Substring(1).ToLower();
			CommandContext context = new CommandContext(Client, (IUserMessage)msg);
			await HandleCommand(context);
		}

		public async Task HandleCommand(ICommandContext ctx)
		{
			try
            {
				Clan clan = await coc.Clans.GetClanAsync("#2VPJQP0J");
				string[] args = ctx.Message.Content.Split(' ');
				EmbedBuilder embed = new EmbedBuilder();
				embed.WithColor(0, 139, 139);
				embed.WithAuthor("CoC Stats", null, "https://github.com/BuildBoy12");
				if (message.StartsWith("ping"))
				{
					await ctx.Channel.SendMessageAsync($"Pong! {client.Latency}ms");
				}
				if (message.StartsWith("help"))
                {
					Dictionary<string, string> helpDict = new Dictionary<string, string>
					{
						{ "help", "It's the help about the help to help you." },
						{ "ping", "Replies with the bots current latency." },
						{ "view", "Subcommands: [view.clan, view.player, view.top]"},
						{ "view.clan", "A lookup tool for this and other clans statistics.\nUsage: view clan [Clan ID]\nExample: view clan #2VPJQP0J" },
						{ "view.player", "A lookup tool for player statistics.\nUsage: view player {Player ID}\nExample: view player #J8R2QQ98" },
						{ "view.top", "Shows top players in the clan for the specified option.\nUsage: view top {donations/trophies/bhtrophies}\nExample: view top donations" }
					};
					embed.WithTitle("Help");	
					try 
					{ 
						if (args[1] != null) 
						{ 
							helpDict.TryGetValue(args[1].ToLower(), out string descript);
							if (descript == null)
								descript = "Command not found.";
							embed.WithDescription(descript); 
						} 
					}
					catch (IndexOutOfRangeException) 
					{
						string s = "Available Commands\n------------------------";
						foreach (var k in helpDict.Keys)
						{
							s += Environment.NewLine + k;
						}
						embed.WithDescription(s);
					}
					await ctx.Channel.SendMessageAsync(null, false, embed.Build());
                }
				if (message.StartsWith("view"))
				{
					try
                    {
						switch (args[1])
						{
							case "clan":
								try
								{
									try { if (args[2] != null) clan = await coc.Clans.GetClanAsync(args[2]); }
									catch (ClashOfClans.Core.ClashOfClansException e) { await ctx.Channel.SendMessageAsync("Please enter a valid clan ID."); return; }
									catch (IndexOutOfRangeException) { }

									embed.WithTitle(clan.Name);
									embed.WithThumbnailUrl(clan.BadgeUrls.Medium.ToString());
									embed.AddField("Clan Tag", clan.Tag);
									embed.AddField("War Win Streak", clan.WarWinStreak);
									embed.AddField("War Wins", clan.WarWins);
									embed.AddField("War Losses", clan.WarLosses);
									embed.AddField("War Ties", clan.WarTies);
									await ctx.Channel.SendMessageAsync(null, false, embed.Build());
								}
								catch (Exception e)
								{
									Console.WriteLine("2: " + e);
									await ctx.Channel.SendMessageAsync("<@250429949410934786> An internal error has occured.");
								}
								break;
							case "player":
								await ctx.Channel.SendMessageAsync("Command is currently under construction!");
								break;
							case "top":
								await ctx.Channel.SendMessageAsync("Command is currently under construction!");
								break;
							default:
								await ctx.Channel.SendMessageAsync("Please select a valid subcommand! Use help for more info.");
								break;
						}
					}
					catch (IndexOutOfRangeException)
                    {
						await ctx.Channel.SendMessageAsync("Please select a valid subcommand! Use help for more info.");
					}				
				}
			}
			catch (Exception e)
            {
				Console.WriteLine("Command Handling Error:" + e);
            }			
		}

		private static DiscordSocketClient client;
		private readonly Program program;
	}
}
