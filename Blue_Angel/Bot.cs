using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClashOfClans;
using ClashOfClans.Models;
using System.Linq;
using System.Threading;

namespace BlueAngel
{
    public class Bot
    {
		private static DiscordSocketClient client;
		private readonly Program program;

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
            Client.Log += Program.Log;
			Client.MessageReceived += OnMessageReceived;
			await Client.LoginAsync(TokenType.Bot, Program.Config.BotToken);
			await Client.StartAsync();
			new Thread(() => RefreshStats()).Start();
			await Task.Delay(-1);
		}

        public static ClashOfClansClient coc = new ClashOfClansClient(Program.Config.APIKey);
        public string BAClanTag = "#2VPJQP0J";
		public bool announcedPrepDay = false;
		public bool announcedWarDay = false;
		public bool announcedWar1hLeft = false;
		public bool announcedCWL1hLeft = false;
		public float prevRound = 0;

		private async Task RefreshStats()
        {
			try
            {
				await Task.Delay(1000);
				Clan clan = await coc.Clans.GetClanAsync(BAClanTag);
				Program.Log("Starting loop.");
				for (; ; )
				{
					if ((bool)clan.IsWarLogPublic)
					{
						#region ClanWar
						try
                        {
							var war = await coc.Clans.GetCurrentWarAsync(BAClanTag);
							if (war.EndTime != new DateTime())
							{
								if (!announcedPrepDay && war.PreparationStartTime < DateTime.UtcNow)
								{
									await Extentions.DIMessage("<@> Preperation day has now begun!");
									announcedPrepDay = true;
								}
								if (!announcedWarDay && war.StartTime < DateTime.UtcNow)
								{
									await Extentions.DIMessage("<@> War day has now begun!");
									announcedWarDay = true;
								}
								else if (!announcedWar1hLeft && war.EndTime.AddHours(-1) < DateTime.UtcNow)
								{
									await Extentions.DIMessage("<@> War day has one hour remaining!");
									announcedWar1hLeft = true;
								}
							}
							if (war.EndTime < DateTime.UtcNow)
							{
								announcedPrepDay = false;
								announcedWarDay = false;
								announcedWar1hLeft = false;
							}
						}
						catch(Exception e)
                        {
							Program.Error("CW Check Exception: " + e);
                        }												
						#endregion
						#region ClanWarLeague
						try
						{
							ClanWarLeagueGroup leagueGroup = await coc.Clans.GetClanWarLeagueGroupAsync(BAClanTag);
							Dictionary<float, List<string>> perRoundTags = new Dictionary<float, List<string>>();
							List<string> mainTag = new List<string>();
							float i = 1;
							float rounds = 0;
							foreach (ClanWarLeagueRound round in leagueGroup.Rounds)
							{
								List<string> validTags = new List<string>();
								foreach (string tag in round.WarTags)
								{
									validTags.Add(tag);
								}
								perRoundTags.Add(i, validTags);
								i++;
							}
							foreach(float key in perRoundTags.Keys)
                            {
								perRoundTags.TryGetValue(key, out List<string> temp);
								if(temp.Last() != "#0") mainTag.Add(temp.Last());
                            }
							rounds = mainTag.Count();
							if(prevRound < rounds)
                            {
								await Extentions.DIMessage("<@> A new CWL Round has been unlocked!");
								prevRound = rounds;
							}
							ClanWarLeagueWar warLeague = await coc.Clans.GetClanWarLeagueWarAsync(mainTag[(int)rounds - 1]);
							if (warLeague.EndTime > DateTime.UtcNow)
							{
								if (!announcedCWL1hLeft && warLeague.EndTime.AddHours(-1) < DateTime.UtcNow)
								{
									await Extentions.DIMessage("<@> War day has one hour remaining!");
									announcedCWL1hLeft = true;
								}
							}
							if (warLeague.EndTime < DateTime.UtcNow)
							{
								announcedCWL1hLeft = false;
							}
						}
						catch (Exception exce)
						{
							Program.Error("CWL Check Exception: " + exce);
						}
						#endregion
					}
					await Task.Delay(30000);
				}
			}
			catch (Exception e)
            {
				Program.Error("Refresh Stats Error: " + e);
            }		
        }

		public async Task OnMessageReceived(SocketMessage msg)
		{
			if (!msg.Content.StartsWith("+"))
				return;

			string message = msg.Content.Substring(1).ToLower();
			CommandContext context = new CommandContext(Client, (IUserMessage)msg);
			await HandleCommand(context, message);
		}

		public async Task HandleCommand(ICommandContext ctx, string message)
		{
			try
			{
				Clan clan = await coc.Clans.GetClanAsync(BAClanTag);
				string[] args = message.Split(' ');
				EmbedBuilder embed = new EmbedBuilder();
				embed.WithColor(0, 139, 139);
				embed.WithAuthor("CoC Stats", null, "https://github.com/BuildBoy12/blue-angel-bot");
                switch (args[0])
                {
					case "ping":
						await ctx.Channel.SendMessageAsync($"Pong! {client.Latency}ms");
						break;
					case "help":
						Dictionary<string, string> helpDict = new Dictionary<string, string>
						{
						{ "help", "It's the help about the help to help you." },
						{ "ping", "Replies with the bots current latency." },
						{ "clan", "A lookup tool for this and other clans statistics.\nUsage: view clan [Clan ID]\nExample: view clan #2VPJQP0J" },
						{ "player", "A lookup tool for player statistics.\nUsage: view player {Player ID}\nExample: view player #J8R2QQ98" },
						{ "top", "Shows top players in the clan for the specified option.\nUsage: view top {donations/trophies/bhtrophies}\nExample: view top donations" },
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
						break;
					case "clan":
						try
						{
							try { if (args[1] != null) clan = await coc.Clans.GetClanAsync(args[1]); }
							catch (ArgumentException) { try { clan = await coc.Clans.GetClanAsync("#" + args[1]); } catch (ClashOfClans.Core.ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid clan ID."); return; } }
							catch (ClashOfClans.Core.ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid clan ID."); return; }
							catch (IndexOutOfRangeException) { }

							embed.WithTitle(clan.Name);
							embed.WithThumbnailUrl(clan.BadgeUrls.Medium.ToString());
							embed.AddField("Clan Tag", clan.Tag);
							embed.AddField("War Win Streak", clan.WarWinStreak);
							embed.AddField("War Wins", clan.WarWins);
							embed.AddField("War Losses", (bool)clan.IsWarLogPublic ? clan.WarLosses.ToString() : "War log hidden.");
							embed.AddField("War Ties", (bool)clan.IsWarLogPublic ? clan.WarTies.ToString() : "War log hidden.");
							await ctx.Channel.SendMessageAsync(null, false, embed.Build());
						}
						catch (Exception e)
						{
							Program.Error("Clan Error: " + e);
							await ctx.Channel.SendMessageAsync("<@250429949410934786> An internal error has occured.");
						}
						break;
					case "player":
						try
						{
							await ctx.Channel.SendMessageAsync("Command is currently under construction!");
						}
						catch (Exception e)
						{
							Program.Error("Player Error " + e);
							await ctx.Channel.SendMessageAsync("<@250429949410934786> An internal error has occured.");
						}
						break;
					case "top":
						try
						{
							await ctx.Channel.SendMessageAsync("Command is currently under construction!");
						}
						catch (Exception e)
						{
							Program.Error("Top Error " + e);
							await ctx.Channel.SendMessageAsync("<@250429949410934786> An internal error has occured.");
						}
						break;
					case "remind":
					case "reminder":
						try
						{
							var split = args[1].ToCharArray();


						}
						catch (IndexOutOfRangeException)
						{
							await ctx.Channel.SendMessageAsync("No.");
						}
						break;
				}
			}
			catch (Exception e)
			{
				Program.Error("Command Handling Error: " + e);
				await ctx.Channel.SendMessageAsync("There was an error handling the command.");
			}
		}
	}
}
