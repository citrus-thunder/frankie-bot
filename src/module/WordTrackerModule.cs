using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;

using Microsoft.Extensions.DependencyInjection;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using SQLite;
using Cronos;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;
using FrankieBot.DB.Container;
using FrankieBot.Discord.Services;
using FrankieBot.Discord;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Module responsible for handling word tracking and daily word goals
	/// </summary>
	[Group("wordtracker")]
	[Alias("wt")]
	public class WordTrackerModule : ModuleBase<SocketCommandContext>
	{
		#region Options

		/// <summary>
		/// Option title for option which enables the Word Tracker module.
		/// </summary>
		public const string OptionEnabled = "word_tracker_module_enabled";

		/// <summary>
		/// Option title for option which sets the announcement channel
		/// </summary>
		public const string OptionAnnounceChannel = "word_tracker_announce_channel";

		/// <summary>
		/// Option title for option which sets the channel reports must be submitted in
		/// </summary>
		public const string OptionReportChannel = "word_tracker_report_channel";

		/// <summary>
		/// Option title for option which sets the random word count goal minimum
		/// </summary>
		public const string OptionGoalMinimum = "word_tracker_goal_minimum";

		/// <summary>
		/// Option title for option which sets the random word count goal maximum
		/// </summary>
		public const string OptionGoalMaximum = "word_tracker_goal_maximum";

		/// <summary>
		/// Option title for the role to notify for word tracker events
		/// </summary>
		public const string OptionNotifyRole = "word_tracker_notify_role";

		#endregion // Options

		#region Jobs

		/// <summary>
		/// Job title for the daily wordtracker refresh
		/// </summary>
		public const string JobRefresh = "word_tracker_refresh";

		#endregion // Jobs

		/// <summary>
		/// This module's DataBaseService reference
		/// </summary>
		/// <value></value>
		public DataBaseService DataBaseService { get; set; }

		/// <summary>
		/// This module's SchedulerService reference
		/// </summary>
		/// <value></value>
		public SchedulerService SchedulerService { get; set; }

		/// <summary>
		/// Initializes the module for all guilds
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static async Task Initialize(IServiceProvider services)
		{
			var database = services.GetRequiredService<DataBaseService>();
			var scheduler = services.GetRequiredService<SchedulerService>();
			await database.RunForAllGuilds(async guild =>
			{
				await RebuildJobs(guild, database, scheduler);
			});
		}

		/// <summary>
		/// Checks and ensures that correct jobs are running
		/// </summary>
		/// <param name="guild"></param>
		/// <param name="dataBaseService"></param>
		/// <param name="schedulerService"></param>
		/// <returns></returns>
		protected static async Task RebuildJobs(IGuild guild, DataBaseService dataBaseService, SchedulerService schedulerService)
		{
			Dictionary<string, string> options = null;
			await dataBaseService.RunGuildDBAction(guild, connection =>
			{
				options = Option.FindAll(connection).As<Options, Option>().Get();
			});

			bool enabled = false;
			if (options.TryGetValue(OptionEnabled, out string value))
			{
				enabled = bool.Parse(value);
			}

			if (enabled)
			{
				// find and stop refresh job if exists
				var refreshJob = schedulerService.GetJob(guild, JobRefresh);
				if (refreshJob != null)
				{
					schedulerService.RemoveJob(refreshJob);
				}

				// build/run refresh job
				await dataBaseService.RunGuildDBAction(guild, connection =>
				{
					refreshJob = new CronJob(connection)
					{
						Name = JobRefresh,
						Guild = guild,
						CronString = "0 0 * * *"
					};
				});

				await schedulerService.AddJob(refreshJob);
				refreshJob.Run += async (object sender, EventArgs e) =>
				{
					await RefreshTracker(guild, dataBaseService, schedulerService);
				};
			}
			else
			{
				// Module is disabled. Find any related jobs and remove them
				var refreshJob = schedulerService.GetJob(guild, JobRefresh);
				if (refreshJob != null)
				{
					schedulerService.RemoveJob(refreshJob);
				}
			}
		}

		private static async Task RefreshTracker(IGuild guild, DataBaseService dataBaseService, SchedulerService schedulerService)
		{
			List<WTSubscriber> subscribers = null;
			Option announceChannelOption = null;
			Option notifyRoleOption = null;
			List<(IUser User, int Goal, int Progress)> todaysGoals = new List<(IUser User, int Goal, int Progress)>();
			List<(IUser User, int Goal, int Progress)> tomorrowsGoals = new List<(IUser User, int Goal, int Progress)>();

			var rand = new Random();

			await dataBaseService.RunGuildDBAction(guild, connection =>
			{
				subscribers = WTSubscriber.FindAll(connection).ContentAs<WTSubscriber>().Content;
				announceChannelOption = Option.FindOne(connection, o => o.Name == OptionAnnounceChannel).As<Option>();
				notifyRoleOption = Option.FindOne(connection, o => o.Name == OptionNotifyRole).As<Option>();
				var minOption = Option.FindOne(connection, o => o.Name == OptionGoalMinimum).As<Option>();
				var maxOption = Option.FindOne(connection, o => o.Name == OptionGoalMaximum).As<Option>();

				if (minOption.IsEmpty)
				{
					minOption = new Option(connection)
					{
						Name = OptionGoalMinimum
					};
					minOption.Initialize();
					minOption.Save();
				}

				if (maxOption.IsEmpty)
				{
					maxOption = new Option(connection)
					{
						Name = OptionGoalMaximum
					};
					maxOption.Initialize();
					maxOption.Save();
				}

				var min = int.Parse(minOption.Value);
				var max = int.Parse(maxOption.Value);

				foreach (var sub in subscribers)
				{
					sub.Initialize(guild);
					if (sub.User == null)
					{
						// If user is null, the user is not present in the guild.
						// Delete their subscription.
						sub.Delete();
						continue;
					}
					// todo: we can check here if the user met their goal for the day and
					// reward them with the currency module, if enabled.
					todaysGoals.Add((sub.User, sub.WordCountGoal, sub.WordCountProgress));

					sub.WordCountProgress = 0;
					sub.WordCountGoal = sub.HasCustomGoal ?
						sub.CustomGoal :
						rand.Next(min, max + 1);

					sub.Save();

					tomorrowsGoals.Add((sub.User, sub.WordCountGoal, sub.WordCountProgress));
				}
			});

			if (announceChannelOption != null && !announceChannelOption.IsEmpty)
			{
				var guildChannel = await guild.GetChannelAsync(ulong.Parse(announceChannelOption.Value)) as SocketGuildChannel;
				if (guildChannel == null)
				{
					// todo: log "channel not found" error
					return;
				}

				if (guildChannel is ISocketMessageChannel channel)
				{
					var ping = "";
					if (!notifyRoleOption.IsEmpty)
					{
						var pingRole = guild.GetRole(ulong.Parse(notifyRoleOption.Value));
						if (pingRole != null)
						{
							ping += $" <@&{pingRole?.Id}>";
						}
					}
					await channel.SendMessageAsync($"Word tracker is being refreshed{ping}");

					// build embed for previous day's results & post
					if (todaysGoals.Count > 0)
					{
						var eb = new EmbedBuilder()
							.WithTitle("Yesterday's Word Tracker Results");

						var fields = new List<EmbedFieldBuilder>();

						foreach (var res in todaysGoals)
						{
							fields.Add(new EmbedFieldBuilder()
								.WithName(res.User.Username)
								.WithValue($"{res.Progress} / {res.Goal}"));
						}

						eb.WithFields(fields);
						await channel.SendMessageAsync(embed: eb.Build());
					}

					// build embed for new day's goals & post
					if (tomorrowsGoals.Count > 0)
					{
						var eb = new EmbedBuilder()
							.WithTitle("Today's Word Tracker Goals");

						var fields = new List<EmbedFieldBuilder>();

						foreach (var res in tomorrowsGoals)
						{
							fields.Add(new EmbedFieldBuilder()
								.WithName(res.User.Username)
								.WithValue($"{res.Progress} / {res.Goal}"));
						}

						eb.WithFields(fields);
						await channel.SendMessageAsync(embed: eb.Build());
					}
				}
			}
		}

		/// <summary>
		/// Enables the Word Tracker module
		/// </summary>
		/// <returns></returns>
		[Command("enable")]
		[Alias("on", "true")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task EnableModule()
		{
			Option option = null;
			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				option = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();

				if (option.IsEmpty)
				{
					option = new Option(connection)
					{
						Name = OptionEnabled
					};
					option.Initialize();
				}

				option.Value = "true";
				option.Save();
			});

			await RebuildJobs(Context.Guild, DataBaseService, SchedulerService);

			await Context.Channel.SendMessageAsync("Word Tracker module enabled");
		}

		/// <summary>
		/// Disables the Word Tracker module
		/// </summary>
		/// <returns></returns>
		[Command("disable")]
		[Alias("off", "false")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task DisableModule()
		{
			Option option = null;
			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				option = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();

				if (option.IsEmpty)
				{
					option = new Option(connection)
					{
						Name = OptionEnabled
					};
					option.Initialize();
				}

				option.Value = "false";
				option.Save();
			});

			await RebuildJobs(Context.Guild, DataBaseService, SchedulerService);

			await Context.Channel.SendMessageAsync("Word Tracker module disabled");
		}

		/// <summary>
		/// Forces the word tracker to refresh
		/// </summary>
		/// <returns></returns>
		[Command("forcerefresh")]
		[Alias("refresh")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task ForceRefresh()
		{
			await Context.Channel.SendMessageAsync("Forcing word tracker refresh...");
			await RefreshTracker(Context.Guild, DataBaseService, SchedulerService);
		}

		/// <summary>
		/// Subscribes the user to daily word goals
		/// </summary>
		/// <param name="wordCount"></param>
		/// <returns></returns>
		[Command("subscribe")]
		[Alias("sub")]
		public async Task Subscribe(int wordCount = -1)
		{
			await DataBaseService.RunGuildDBAction(Context.Guild, async connection =>
			{
				var userID = Context.Message.Author.Id.ToString();
				var subscriber = WTSubscriber.FindOne(connection, s => s.UserID == userID).As<WTSubscriber>();

				if (!subscriber.IsEmpty)
				{
					await Context.Channel.SendMessageAsync("You are already subscribed to the word tracker!");
					return;
				}
				else
				{
					var rand = new Random();
					
					var wcMinOption = Option.FindOne(connection, o => o.Name == OptionGoalMinimum).As<Option>();
					if (wcMinOption.IsEmpty)
					{
						wcMinOption = new Option(connection)
						{
							Name = OptionGoalMinimum
						};
						wcMinOption.Initialize();
						wcMinOption.Save();
					}

					var wcMaxOption = Option.FindOne(connection, o => o.Name == OptionGoalMaximum).As<Option>();
					if (wcMaxOption.IsEmpty)
					{
						wcMaxOption = new Option(connection)
						{
							Name = OptionGoalMaximum
						};
						wcMaxOption.Initialize();
						wcMaxOption.Save();
					}

					int min = int.Parse(wcMinOption.Value);
					int max = int.Parse(wcMaxOption.Value);

					subscriber = new WTSubscriber(connection)
					{
						User = Context.Message.Author,
						WordCountGoal = wordCount > 0 ? wordCount : rand.Next(min, max + 1),
						CustomGoal = wordCount > 0 ? wordCount : 0
					};

					subscriber.Save();
					await Context.Channel.SendMessageAsync($"{Context.Message.Author.Username} has subscribed to the word tracker! Their goal is {subscriber.WordCountGoal} words.");
				}
			});
		}

		/// <summary>
		/// Unsubscribes the user from daily word goals
		/// </summary>
		/// <returns></returns>
		[Command("unsubscribe")]
		[Alias("unsub")]
		public async Task Unsubscribe()
		{
			await DataBaseService.RunGuildDBAction(Context.Guild, async connection =>
			{
				var userID = Context.Message.Author.Id.ToString();
				var subscriber = WTSubscriber.FindOne(connection, s => s.UserID == userID).As<WTSubscriber>();
				subscriber.Initialize(Context.Guild);

				if (subscriber.IsEmpty)
				{
					await Context.Channel.SendMessageAsync("You are not subscribed to the word tracker!");
					return;
				}
				else
				{
					subscriber.Delete();
					await Context.Channel.SendMessageAsync($"{Context.Message.Author.Username} has unsubscribed from the word tracker.");
				}
			});
		}

		/// <summary>
		/// Sets a custom word count goal for the subscriber
		/// </summary>
		/// <param name="wordCount"></param>
		/// <returns></returns>
		[Command("setgoal")]
		[Alias("goal")]
		public async Task SetCustomGoal(int wordCount)
		{
			await DataBaseService.RunGuildDBAction(Context.Guild, async connection =>
			{
				var channelOption = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
				if (!channelOption.IsEmpty)
				{
					var channel = Context.Guild.GetChannel(ulong.Parse(channelOption.Value));
					if (channel != null && Context.Channel != channel)
					{
						await Context.Channel.SendMessageAsync($"Word tracker updates must be posted in <#{channel.Id}>.");
						return;
					}
				}

				var userID = Context.Message.Author.Id.ToString();
				var subscriber = WTSubscriber.FindOne(connection, s => s.UserID == userID).As<WTSubscriber>();
				subscriber.Initialize(Context.Guild);
			
				if (subscriber.IsEmpty)
				{
					await Context.Channel.SendMessageAsync("You are not subscribed to the word tracker! Please subscribe before setting a custom goal.");
				}
				else
				{
					subscriber.CustomGoal = wordCount;
					subscriber.WordCountGoal = wordCount;
					subscriber.Save();

					await Context.Channel.SendMessageAsync($"Custom word count goal for {subscriber.User.Username} has been set to {subscriber.WordCountGoal}");
				}
			});
		}

		/// <summary>
		/// Clears a subscriber's custom word count goal
		/// </summary>
		/// <returns></returns>
		[Command("unsetgoal")]
		[Alias("cleargoal")]
		public async Task ClearCustomGoal()
		{
			await DataBaseService.RunGuildDBAction(Context.Guild, async connection =>
			{
				var channelOption = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
				if (!channelOption.IsEmpty)
				{
					var channel = Context.Guild.GetChannel(ulong.Parse(channelOption.Value));
					if (channel != null && Context.Channel != channel)
					{
						await Context.Channel.SendMessageAsync($"Word tracker updates must be posted in <#{channel.Id}>.");
						return;
					}
				}

				var userID = Context.Message.Author.Id.ToString();
				var subscriber = WTSubscriber.FindOne(connection, s => s.UserID == userID).As<WTSubscriber>();
				subscriber.Initialize(Context.Guild);

				if (subscriber.IsEmpty)
				{
					await Context.Channel.SendMessageAsync("You are not subscribed to the word tracker! Please subscribe before setting a custom goal.");
				}
				else
				{
					var rand = new Random();
					var wcMinOption = Option.FindOne(connection, o => o.Name == OptionGoalMinimum).As<Option>();
					var wcMaxOption = Option.FindOne(connection, o => o.Name == OptionGoalMaximum).As<Option>();

					int min = int.Parse(wcMinOption.Value);
					int max = int.Parse(wcMaxOption.Value);

					subscriber.CustomGoal = -1;
					subscriber.WordCountGoal = rand.Next(min, max + 1);
					subscriber.Save();

					await Context.Channel.SendMessageAsync($"Custom word count goal for {subscriber.User.Username} has been cleared. Their word count has been set to {subscriber.WordCountGoal}");
				}
			});
		}

		/// <summary>
		/// Adds to the user's daily word tracker goal progress
		/// </summary>
		/// <param name="words"></param>
		/// <returns></returns>
		[Command("add")]
		[Alias("a")]
		public async Task AddToWordCount(int words)
		{
			// todo: check if a reporting channel is set. If so, ensure this
			// command was invoked from the appropriate channel
			await DataBaseService.RunGuildDBAction(Context.Guild, async connection =>
			{
				var channelOption = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
				if (!channelOption.IsEmpty)
				{
					var channel = Context.Guild.GetChannel(ulong.Parse(channelOption.Value));
					if (channel != null && Context.Channel != channel)
					{
						await Context.Channel.SendMessageAsync($"Word tracker updates must be posted in <#{channel.Id}>.");
						return;
					}
				}

				var userID = Context.Message.Author.Id.ToString();
				var subscriber = WTSubscriber.FindOne(connection, s => s.UserID == userID).As<WTSubscriber>();
				subscriber.Initialize(Context.Guild);
			
				if (subscriber.IsEmpty)
				{
					await Context.Channel.SendMessageAsync("You are not subscribed to the word tracker!");
				}
				else
				{
					subscriber.WordCountProgress += words;
					bool metGoal = subscriber.WordCountProgress >= subscriber.WordCountGoal;
					subscriber.Save();
					await Context.Channel.SendMessageAsync($"Progress updated for {subscriber.User.Username}: {subscriber.WordCountProgress}/{subscriber.WordCountGoal} words");

					if (metGoal)
					{
						await Context.Channel.SendMessageAsync($"Congratulations, {subscriber.User.Username}! You've met today's goal :tada:");
					}
				}
			});
		}

		/// <summary>
		/// Edits the user's daily word tracker goal progress
		/// </summary>
		/// <param name="words"></param>
		/// <returns></returns>
		[Command("edit")]
		[Alias("e")]
		public async Task EditWordCount(int words)
		{
			// todo: check if a reporting channel is set. If so, ensure this
			// command was invoked from the appropriate channel
			await DataBaseService.RunGuildDBAction(Context.Guild, async connection =>
			{
				var channelOption = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
				if (!channelOption.IsEmpty)
				{
					var channel = Context.Guild.GetChannel(ulong.Parse(channelOption.Value));
					if (channel != null && Context.Channel != channel)
					{
						await Context.Channel.SendMessageAsync($"Word tracker updates must be posted in <#{channel.Id}>.");
						return;
					}
				}

				var userID = Context.Message.Author.Id.ToString();
				var subscriber = WTSubscriber.FindOne(connection, s => s.UserID == userID).As<WTSubscriber>();
				subscriber.Initialize(Context.Guild);
			
				if (subscriber.IsEmpty)
				{
					await Context.Channel.SendMessageAsync("You are not subscribed to the word tracker!");
				}
				else
				{
					subscriber.WordCountProgress = words;
					bool metGoal = subscriber.WordCountProgress >= subscriber.WordCountGoal;
					subscriber.Save();
					await Context.Channel.SendMessageAsync($"Progress updated for {subscriber.User.Username}: {subscriber.WordCountProgress}/{subscriber.WordCountGoal} words");
				}
			});
		}

		/// <summary>
		/// Lists current subscribers as well as their goals and progress
		/// </summary>
		/// <returns></returns>
		[Command("list")]
		[Alias("ls")]
		public async Task ListWordCounts()
		{
			List<WTSubscriber> subscribers = new List<WTSubscriber>();
			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				var subs = WTSubscriber.FindAll(connection).ContentAs<WTSubscriber>();

				// Comb through found subs and ensure the users aren't null (i.e. still in the guild)
				// If so, add them to the outer list.
				foreach (var sub in subs.Content)
				{
					sub.Initialize(Context.Guild);
					if (!sub.IsEmpty)
					{
						subscribers.Add(sub);
					}
				}
			});

			var eb = new EmbedBuilder()
				.WithTitle("Today's Word Tracker Progress");

			if (subscribers.Count < 1)
			{
				eb.WithDescription("The word tracker currently has no subscribers!");
			}
			else
			{
				var fields = new List<EmbedFieldBuilder>();
				foreach (var sub in subscribers)
				{
					fields.Add(new EmbedFieldBuilder()
						.WithName(sub.User.Username)
						.WithValue($"`{sub.WordCountProgress}/{sub.WordCountGoal} words`")
						);
				}
				eb.WithFields(fields);
			}

			await Context.Channel.SendMessageAsync(embed: eb.Build());
		}

		/// <summary>
		/// Command module containing commands for setting WordTrackerModule options
		/// </summary>
		[Group("option")]
		[Alias("set", "o")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public class ModuleOptions : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// This module's DataBaseService reference
			/// </summary>
			public DataBaseService DataBaseService { get; set; }

			/// <summary>
			/// This module's SchedulerService reference
			/// </summary>
			public SchedulerService SchedulerService { get; set; }

			/// <summary>
			/// Sets the channel where word tracker refresh announcements are posted
			/// </summary>
			/// <param name="channel"></param>
			/// <returns></returns>
			[Command("announcechannel")]
			[Alias("announce")]
			public async Task SetAnnounceChannel(IChannel channel)
			{
				if (channel is SocketChannel socketChannel)
				{
					await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
					{
						var option = Option.FindOne(connection, o => o.Name == OptionAnnounceChannel).As<Option>();
						if (option.IsEmpty)
						{
							option = new Option(connection)
							{
								Name = OptionAnnounceChannel
							};
							option.Initialize();
						}
						option.Value = socketChannel.Id.ToString();
						option.Save();
					});

					await Context.Channel.SendMessageAsync($"Word tracker announcement channel set to <#{channel.Id}>");
				}
				else
				{
					await Context.Channel.SendMessageAsync("Invalid channel specified");
				}
			}

			/// <summary>
			/// Clears the word tracker announcement channel, if set
			/// </summary>
			/// <returns></returns>
			[Command("clearannouncechannel")]
			[Alias("clearannounce")]
			public async Task ClearAnnounceChannel()
			{
				await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
				{
					var option = Option.FindOne(connection, o => o.Name == OptionAnnounceChannel).As<Option>();
					if (!option.IsEmpty)
					{
						option.Delete();
					}
				});

				await Context.Channel.SendMessageAsync("Word tracker announcement channel cleared");
			}

			/// <summary>
			/// Sets the channel word tracker updates must be submitted to
			/// </summary>
			/// <param name="channel"></param>
			/// <returns></returns>
			[Command("reportchannel")]
			[Alias("report")]
			public async Task SetReportChannel(IChannel channel)
			{
				if (channel is SocketChannel socketChannel)
				{
					await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
					{
						var option = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
						if (option.IsEmpty)
						{
							option = new Option(connection)
							{
								Name = OptionReportChannel
							};
							option.Initialize();
						}
						option.Value = socketChannel.Id.ToString();
						option.Save();
					});

					await Context.Channel.SendMessageAsync($"Word tracker reporting channel set to <#{channel.Id}>");
				}
				else
				{
					await Context.Channel.SendMessageAsync("Invalid channel specified");
				}
			}

			/// <summary>
			/// Clears the word tracker report channel, if set
			/// </summary>
			/// <returns></returns>
			[Command("clearreportchannel")]
			[Alias("clearreport")]
			public async Task ClearReportChannel()
			{
				await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
				{
					var option = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
					if (!option.IsEmpty)
					{
						option.Delete();
					}
				});

				await Context.Channel.SendMessageAsync("Word tracker report channel cleared");
			}

			/// <summary>
			/// Sets the role to be pinged for word tracker notifications
			/// </summary>
			/// <param name="role"></param>
			/// <returns></returns>
			[Command("notifyrole")]
			[Alias("notify", "pingrole", "ping")]
			public async Task SetNotifyRole(IRole role)
			{
				await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
				{
					var option = Option.FindOne(connection, o => o.Name == OptionNotifyRole).As<Option>();
					if (option.IsEmpty)
					{
						option = new Option(connection)
						{
							Name = OptionNotifyRole
						};
						option.Initialize();
					}

					option.Value = role.Id.ToString();
					option.Save();
				});
				await Context.Channel.SendMessageAsync($"Word tracker notification role set to <@&{role.Id}>");
			}

			/// <summary>
			/// Clears the role to be pinged for word tracker notifications, if set
			/// </summary>
			/// <returns></returns>
			[Command("clearnotifyrole")]
			[Alias("clearnotify", "clearpingrole", "clearping")]
			public async Task ClearNotifyRole()
			{
				await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
				{
					var option = Option.FindOne(connection, o => o.Name == OptionNotifyRole).As<Option>();
					if (!option.IsEmpty)
					{
						option.Delete();
					}
				});

				await Context.Channel.SendMessageAsync("Word tracker notification role cleared");
			}
		}
	}
}