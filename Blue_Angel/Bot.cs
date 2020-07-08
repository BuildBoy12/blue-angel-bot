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
using ClashOfClans.Core;
using System.IO;

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
			await Task.Run(() => RefreshStats());
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
				#region CWL start so that the new round msg isnt announced on bot start
				try
				{ 
					ClanWarLeagueGroup leagueGroup = await coc.Clans.GetClanWarLeagueGroupAsync(BAClanTag);
					Dictionary<float, List<string>> perRoundTags = new Dictionary<float, List<string>>();
					List<string> mainTag = new List<string>();
					float curRound = 1;
					foreach (ClanWarLeagueRound round in leagueGroup.Rounds)
					{
						List<string> validTags = new List<string>();
						foreach (string tag in round.WarTags)
						{
							validTags.Add(tag);
						}
						perRoundTags.Add(curRound, validTags);
						curRound++;
					}
					foreach (float key in perRoundTags.Keys)
					{
						perRoundTags.TryGetValue(key, out List<string> temp);
						if (temp.Last() != "#0") mainTag.Add(temp.Last());
					}
					prevRound = mainTag.Count();
				} 
				catch (ClashOfClansException)
				{
					
				}				
                #endregion
                for (; ; )
				{
					try
                    {
						string[] allFiles = Directory.GetFiles($"{Directory.GetCurrentDirectory()}/reminders/", "*.txt");
						foreach (string txt in allFiles)
						{
							string[] lines = File.ReadAllLines(txt);
							if (DateTime.Parse(lines[1]) < DateTime.UtcNow)
							{
								await Extentions.DIMessage(lines[0] + " " + lines[2], false, null, ulong.Parse(lines[3]), ulong.Parse(lines[4]));								
								File.Delete(txt);
							}
						}
					}
					catch(Exception e)
                    {
						Program.Error("Reminder check error: " + e);
                    }				
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
							float curRound = 1;
							foreach (ClanWarLeagueRound round in leagueGroup.Rounds)
							{
								List<string> validTags = new List<string>();
								foreach (string tag in round.WarTags)
								{
									validTags.Add(tag);
								}
								perRoundTags.Add(curRound, validTags);
								curRound++;
							}
							foreach(float key in perRoundTags.Keys)
                            {
								perRoundTags.TryGetValue(key, out List<string> temp);
								if(temp.Last() != "#0") mainTag.Add(temp.Last());
                            }
							if (mainTag.Count() == 0) prevRound = 0;
							else
                            {
								if (prevRound < mainTag.Count())
								{
									await Extentions.DIMessage("<@> A new CWL Round has been unlocked!");
									prevRound = mainTag.Count();
									announcedCWL1hLeft = false;
								}
								ClanWarLeagueWar warLeague = await coc.Clans.GetClanWarLeagueWarAsync(mainTag[(int)mainTag.Count() - 2]);
								if (warLeague.EndTime > DateTime.UtcNow)
								{
									if (!announcedCWL1hLeft && warLeague.EndTime.AddHours(-1) < DateTime.UtcNow)
									{
										await Extentions.DIMessage("<@> War day has one hour remaining!", false, null, 727369782638149754, 727370227230441524);
										announcedCWL1hLeft = true;
										Program.Log("Announcing CWL1H reminder.", true);
									}
								}
							}							
						}
						catch (ClashOfClansException)
                        {

                        }						
						catch (Exception exce)
						{
							Program.Error("CWL Check Exception: " + exce);
						}
						#endregion
					}
					await Task.Delay(2000);
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
			await Task.Run(() => HandleCommand(context, message));
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
						{ "clan", "A lookup tool for this and other clans statistics.\nUsage: clan [Clan ID]\nExample: clan #2VPJQP0J" },
						{ "player", "A lookup tool for player statistics.\nUsage: player {Player ID}\nExample: player #J8R2QQ98" },
						{ "top", "Shows top players in the clan for the specified option.\nUsage: top {donations/trophies/bhtrophies}\nExample: top donations" },
						{ "remind", "Sends the set message after the specified amount of time.\nUsage: remind {time|s/m/h/d} {message}\nExample: remind 10m Don't fail my attack" }
						};
						try
						{
							embed.WithTitle("Help");
							if (args[1] != null)
							{
								helpDict.TryGetValue(args[1].ToLower(), out string descript);
								if (descript == null)
									descript = "Command not found.";
								embed.WithDescription(descript);
								embed.WithTitle("Help - " + args[1].FirstCharToUpper());
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
							catch (ArgumentException) { try { clan = await coc.Clans.GetClanAsync("#" + args[1]); } catch (ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid clan ID."); return; } }
							catch (ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid clan ID."); return; }
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
							await ctx.Channel.SendMessageAsync("An internal error has occured.");
						}
						break;
					case "player":
						try
						{
							Player player = await coc.Players.GetPlayerAsync("#9QCVUR0QQ");
							try { if (args[1] != null) player = await coc.Players.GetPlayerAsync(args[1].ToUpper()); }
							catch (ArgumentException) { try { player = await coc.Players.GetPlayerAsync("#" + args[1].ToUpper()); } catch (ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid player ID."); return; } }
							catch (ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid player ID."); return; }
							catch (IndexOutOfRangeException) { }

							embed.WithTitle(player.Name);
							embed.WithThumbnailUrl(player.League.IconUrls.Medium.ToString());
							embed.AddField("Player Tag", player.Tag);
							embed.AddField("Clan Name", player.Clan.Name);
							embed.AddField("Town Hall", player.TownHallLevel);
							await ctx.Channel.SendMessageAsync(null, false, embed.Build());
						}
						catch (Exception e)
						{
							Program.Error("Player Error: " + e);
							await ctx.Channel.SendMessageAsync("An internal error has occured.");
						}
						break;
					case "top":
						try
						{
							//await ctx.Channel.SendMessageAsync("Command is currently under construction!");
							//return;
							try { if (args[2] != null) clan = await coc.Clans.GetClanAsync(args[2]); }
							catch (ArgumentException) { try { clan = await coc.Clans.GetClanAsync("#" + args[2]); } catch (ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid clan ID."); return; } }
							catch (ClashOfClansException) { await ctx.Channel.SendMessageAsync("Please enter a valid clan ID."); return; }
							catch (IndexOutOfRangeException) { }
							List<TopTracker> scores = new List<TopTracker>();							
							try
                            {
								switch (args[1])
								{
                                    #region Donations
                                    case "d":
									case "donate":
									case "donation":
									case "donations":
										foreach (ClanMember v in clan.MemberList)
										{
											scores.Add(new TopTracker() { playerName = v.Name, score = (int)v.Donations });
										}
										TopTracker[] topDonators = GetHighScores(scores, 10);
										string sortedD = "Top Donators\n------------------------";
										for (int i = 0; i < topDonators.Length; i++)
										{
											sortedD += Environment.NewLine + topDonators[i].playerName + " - " + topDonators[i].score;
										}
										embed.WithDescription(sortedD);
										await ctx.Channel.SendMessageAsync(null, false, embed.Build());
										break;
                                    #endregion
                                    #region Trophies
                                    case "t":
									case "trophy":
									case "trophies":
										foreach (ClanMember v in clan.MemberList)
										{
											scores.Add(new TopTracker() { playerName = v.Name, score = (int)v.Trophies });
										}
										TopTracker[] topTrophies = GetHighScores(scores, 10);
										string sortedT = "Top Trophies\n------------------------";
										for (int i = 0; i < topTrophies.Length; i++)
										{
											sortedT += Environment.NewLine + topTrophies[i].playerName + " - " + topTrophies[i].score;
										}
										embed.WithDescription(sortedT);
										await ctx.Channel.SendMessageAsync(null, false, embed.Build());
										break;
                                    #endregion
                                    #region Builder Hall Trophies
                                    case "bh":
									case "bht":
									case "bhtrophies":
										foreach (ClanMember v in clan.MemberList)
										{
											scores.Add(new TopTracker() { playerName = v.Name, score = (int)v.VersusTrophies });
										}
										TopTracker[] topBHTrophies = GetHighScores(scores, 10);
										string sortedBHT = "Top BH Trophies\n------------------------";
										for (int i = 0; i < topBHTrophies.Length; i++)
										{
											sortedBHT += Environment.NewLine + topBHTrophies[i].playerName + " - " + topBHTrophies[i].score;
										}
										embed.WithDescription(sortedBHT);
										await ctx.Channel.SendMessageAsync(null, false, embed.Build());
										break;
                                    #endregion
								}
							}
							catch (IndexOutOfRangeException)
							{
								foreach (var v in clan.MemberList)
								{
									scores.Add(new TopTracker() { playerName = v.Name, score = (int)v.Trophies });
								}
								TopTracker[] topTrophies2 = GetHighScores(scores, 10);
								string sortedT2 = "Top Trophies\n------------------------";
								for (int i = 0; i < topTrophies2.Length; i++)
								{
									sortedT2 += Environment.NewLine + topTrophies2[i].playerName + " - " + topTrophies2[i].score;
								}
								embed.WithDescription(sortedT2);
								await ctx.Channel.SendMessageAsync(null, false, embed.Build());
							}
							catch (Exception e)
                            {
								Program.Error("Top Error [Switch]: " + e);
								await ctx.Channel.SendMessageAsync("An internal error has occured.");
							}								
						}
						catch (Exception e)
						{
							Program.Error("Top Error: " + e);
							await ctx.Channel.SendMessageAsync("An internal error has occured.");
						}
						break;
					case "remind":
					case "remindme":
					case "reminder":
						try
						{
							//await ctx.Channel.SendMessageAsync("Command under construction!");
							//return;
							args = ctx.Message.Content.Substring(1).Split(' ');
							char[] split = args[1].ToLower().ToCharArray();
							char lastChar = split.Last();						
							int timeInMilliseconds = new int();
							if(!char.IsNumber(lastChar))
                            {
								string time = args[1].Substring(0, args[1].Length - 1);
								if (!int.TryParse(time, out int parsedTime) || parsedTime <= 0)
								{
									await ctx.Channel.SendMessageAsync("Please enter a valid time.");
									return;
								}
								switch (lastChar)
                                {
									case 's':
										timeInMilliseconds = parsedTime * 1000;
										break;
									case 'm':
										timeInMilliseconds = parsedTime * 60000;
										break;
									case 'h':
										timeInMilliseconds = parsedTime * 3600000;
										break;
									case 'd':
										timeInMilliseconds = parsedTime * 86400000;
										break;
									default:
										timeInMilliseconds = parsedTime * 1000;
										break;
                                }
							}
							else
                            {
								if (!int.TryParse(args[1], out int parsedTime) || parsedTime <= 0)
								{
									await ctx.Channel.SendMessageAsync("Please enter a valid time.");
									return;
								}
								timeInMilliseconds = parsedTime * 1000;
							}
							string origMsg = string.Empty;
							foreach (var v in args)
                            {
								if (v != args[0] && v != args[1])
									origMsg += v + " ";
                            }
							await ctx.Channel.SendMessageAsync("I will remind you in " + timeInMilliseconds / 1000 + " seconds: " + origMsg);
							string path = $"{Directory.GetCurrentDirectory()}/reminders/{DateTime.UtcNow.Ticks}.txt";
							File.Create(path).Close();
							string[] write = new[]
							{
								ctx.Message.Author.Mention,
								$"{DateTime.UtcNow.AddMilliseconds(timeInMilliseconds)}",
								origMsg,
								ctx.Guild.Id.ToString(),
								ctx.Channel.Id.ToString(),							
							};
							File.WriteAllLines(path, write);
						}
						catch (Exception e)
						{
							Program.Error("Reminder Error: " + e);
							await ctx.Channel.SendMessageAsync("An internal error has occured.");
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

		TopTracker[] GetHighScores(List<TopTracker> list, int count)
        {
			return list.OrderByDescending(x => x.score).Take(count).ToArray();
		}
	}

	public class TopTracker
	{
		public string playerName { get; set; }
		public int score { get; set; }
	}
}	
