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

//using ViewModel = FrankieBot.DB.ViewModel;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Module containing progress report functionality
	/// </summary>
	[Group("progress")]
	[Alias("p", "pr")]
	public class ProgressReportModule : ModuleBase<SocketCommandContext>
	{
		#region Options

		/// <summary>
		/// Option title for option which enables the Progress Report module
		/// </summary>
		public const string OptionEnabled = "progress_report_module_enabled";

		/// <summary>
		/// Option title for option which sets the progress report window open schedule
		/// </summary>
		public const string OptionWindowOpen = "progress_report_window_open";

		/// <summary>
		/// Option title for option which sets the progress report window open duration
		/// </summary>
		public const string OptionWindowDuration = "progress_report_window_duration";

		/// <summary>
		/// Option title for option which sets the channel window open/close announcements are sent to
		/// </summary>
		public const string OptionAnnouncementChannel = "progress_report_announcement_channel";

		/// <summary>
		/// Option title for option which sets whether report submissions must be in a particular channel
		/// </summary>
		public const string OptionRestrictReportChannel = "progress_report_restrict_report_channel";

		/// <summary>
		/// Option title for option which sets the channel reports must be submitted in
		/// </summary>
		public const string OptionReportChannel = "progress_report_report_channel";

		/// <summary>
		/// Option title for option which sets the role Frankie pings to remind users of progress report
		/// window openings/closings
		/// </summary>
		public const string OptionWindowReminderRole = "progress_report_reminder_role";

		/// <summary>
		/// Option title for option which sets whether the Progress Report module will automatically apply
		/// rank roles to users
		/// </summary>
		public const string OptionRanksEnabled = "progress_report_ranks_enabled";

		#endregion // Options

		#region Jobs

		/// <summary>
		/// Title of job responsible for opening scheduled progress report windows
		/// </summary>
		public const string JobOpenWindow = "progress_report_announce_window_opened";

		/// <summary>
		/// Title of job responsible for announcing the closure of scheduled progress report windows
		/// </summary>
		public const string JobAnnounceWindowClosed = "progress_report_announce_window_closed";

		#endregion // Jobs

		/// <summary>
		/// DataBaseService reference
		/// </summary>
		/// <remarks>
		/// Set via Dependency Injection
		/// </remarks>
		public DataBaseService DataBaseService { get; set; }

		/// <summary>
		/// SchedulerServiceReference
		/// </summary>
		/// <remarks>
		/// Set via Dependency Injection
		/// </remarks>
		public SchedulerService SchedulerService { get; set; }

		/// <summary>
		/// Initializes this module for all guilds
		/// </summary>
		/// <param name="services"></param>
		/// <returns></returns>
		public static async Task Initialize(IServiceProvider services)
		{
			var database = services.GetRequiredService<DataBaseService>();
			var scheduler = services.GetRequiredService<SchedulerService>();
			await database.RunForAllGuilds(async (guild) =>
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
				var openOption = Option.FindOne(connection, o => o.Name == OptionWindowOpen);
				if (openOption.IsEmpty)
				{
					openOption = new Option(connection)
					{
						Name = OptionWindowOpen
					};
					openOption.Initialize();
					openOption.Save();
				}

				options = Option.FindAll(connection).As<Options, Option>().Get();
			});

			bool enabled = false;
			if (options.TryGetValue(OptionEnabled, out string value))
			{
				enabled = bool.Parse(value);
			}

			if (enabled)
			{
				// build/run next open job
				options.TryGetValue(OptionWindowOpen, out string openCronString);

				var openJob = schedulerService.GetJob(guild, JobOpenWindow);
				if (openJob != null)
				{
					schedulerService.RemoveJob(openJob);
				}

				await dataBaseService.RunGuildDBAction(guild, connection =>
				{
					openJob = new CronJob(connection)
					{
						Name = JobOpenWindow,
						Guild = guild,
						CronString = openCronString
					};
				});

				await schedulerService.AddJob(openJob);
				openJob.Run += async (object sender, EventArgs e) =>
				{
					await OpenWindow(guild, dataBaseService, schedulerService);
				};

				// check if window open. If so, recreate close job
				ProgressReportWindow currentWindow = null;
				await dataBaseService.RunGuildDBAction(guild, connection =>
				{
					var time = DateTime.UtcNow;
					currentWindow = ProgressReportWindow.FindOne(connection, w =>
						w.StartTime <= time && w.StartTime.AddHours(w.Duration) >= time
						).As<ProgressReportWindow>();
				});

				if (!currentWindow.IsEmpty)
				{
					// a window should be active. Set up announce window close job
					var closeJob = schedulerService.GetJob(guild, JobAnnounceWindowClosed);
					if (closeJob != null)
					{
						closeJob.Stop();
						schedulerService.RemoveJob(closeJob);
					}

					await dataBaseService.RunGuildDBAction(guild, connection =>
					{
						closeJob = new CronJob(connection)
						{
							Name = JobAnnounceWindowClosed,
							Guild = guild
						};
					});

					closeJob.Run += async (object sender, EventArgs e) =>
					{
						closeJob.Stop();
						await CloseWindow(guild, currentWindow, schedulerService, dataBaseService);
					};

					await schedulerService.AddJob(closeJob, false);
					closeJob.StartAt(currentWindow.EndTime, Timeout.InfiniteTimeSpan);
				}
			}
			else
			{
				// Module is disabled. Find any related open jobs and remove them
				var openJob = schedulerService.GetJob(guild, JobOpenWindow);
				if (openJob != null)
				{
					schedulerService.RemoveJob(openJob);
				}

				var closeJob = schedulerService.GetJob(guild, JobAnnounceWindowClosed);
				if (closeJob != null)
				{
					schedulerService.RemoveJob(closeJob);
				}
			}
		}

		private static async Task OpenWindow(IGuild guild, DataBaseService dataBaseService, SchedulerService schedulerService, int duration = -1)
		{
			var g = guild as SocketGuild;
			// convenience alias
			var db = dataBaseService;

			await db.RunGuildDBAction(guild, async connection =>
			{
				var announceReminderOption = Option.FindOne(connection, o => o.Name == OptionWindowReminderRole).As<Option>();
				var announcementChannelOption = Option.FindOne(connection, o => o.Name == OptionAnnouncementChannel).As<Option>();
				var announce = !announcementChannelOption.IsEmpty;
				ISocketMessageChannel announceChannel = null;
				if (announce)
				{
					announceChannel = g.GetChannel(ulong.Parse(announcementChannelOption.Value)) as ISocketMessageChannel;
					if (announceChannel == null)
					{
						// todo: log that the specified channel is not found
						announce = false;
					}
				}

				// Create Window

				if (duration <= -1)
				{
					var durationOption = Option.FindOne(connection, o => o.Name == OptionWindowDuration).As<Option>();
					if (durationOption.IsEmpty)
					{
						durationOption = new Option(connection)
						{
							Name = OptionWindowDuration
						};
						durationOption.Initialize();
						durationOption.Save();
					}
					duration = int.Parse(durationOption.Value);
				}

				var window = new ProgressReportWindow(connection)
				{
					StartTime = DateTime.UtcNow,
					Duration = duration
				};

				window.Save();

				// Create Job

				var closeJob = schedulerService.GetJob(g, JobAnnounceWindowClosed);
				if (closeJob != null)
				{
					closeJob.Stop();
					schedulerService.RemoveJob(g, closeJob.Name);
				}

				var guildId = g.Id.ToString();
				var closeJobRecord = CronJob.FindOne(connection, j => j.Name == JobAnnounceWindowClosed && j.GuildID == guildId).As<CronJob>();
				if (closeJobRecord.IsEmpty)
				{
					closeJobRecord = new CronJob(connection)
					{
						Name = JobAnnounceWindowClosed,
						Guild = g
					};
				}
				// We don't need to save this job to the DB as it is not a recurring job

				closeJobRecord.Run += async (object sender, EventArgs e) =>
				{
					closeJobRecord.Stop();
					await CloseWindow(g, window, schedulerService, dataBaseService);
				};

				// We keep the awaitables at the bottom of this process as they tend to
				// break the SQLite connection handle.
				await schedulerService.AddJob(closeJobRecord, false);
				closeJobRecord.StartAt(window.EndTime, Timeout.InfiniteTimeSpan);
				if (announce)
				{
					var ping = "";
					if (!announceReminderOption.IsEmpty)
					{
						var role = guild.GetRole(ulong.Parse(announceReminderOption.Value));
						if (role != null)
						{
							ping = $"<@&{role.Id}> ";
						}
					}
					await announceChannel.SendMessageAsync($"{ping}Progress report submissions are now open! Submissions will be accepted from <t:{new DateTimeOffset(window.StartTime).ToUnixTimeSeconds()}:F> until <t:{new DateTimeOffset(window.EndTime).ToUnixTimeSeconds()}:F>.");
				}
			});
		}

		private static async Task CloseWindow(IGuild guild, ProgressReportWindow window, SchedulerService schedulerService, DataBaseService dataBaseService)
		{
			schedulerService.RemoveJob(guild, JobAnnounceWindowClosed);

			ISocketMessageChannel channel = null;
			Option announceReminderRole = null;
			bool updateRanks = false;
			await dataBaseService.RunGuildDBAction(guild, connection =>
			{
				announceReminderRole = Option.FindOne(connection, o => o.Name == OptionWindowReminderRole).As<Option>();
				var announcementChannelOption = Option.FindOne(connection, o => o.Name == OptionAnnouncementChannel).As<Option>();
				if (!announcementChannelOption.IsEmpty)
				{
					var g = guild as SocketGuild;
					channel = g.GetChannel(ulong.Parse(announcementChannelOption.Value)) as ISocketMessageChannel;
				}

				var rankOption = Option.FindOne(connection, o => o.Name == OptionRanksEnabled).As<Option>();
				if (rankOption.IsEmpty)
				{
					rankOption = new Option(connection)
					{
						Name = OptionRanksEnabled
					};
					rankOption.Initialize();
					rankOption.Save();
				}

				updateRanks = bool.Parse(rankOption.Value);
			});

			var ping = "";
			if (!announceReminderRole.IsEmpty)
			{
				var role = guild.GetRole(ulong.Parse(announceReminderRole.Value));
				if (role != null)
				{
					ping = $"<@&{role.Id}> ";
				}
			}
			await channel?.SendMessageAsync($"{ping}The progress report submission window is now closed!");

			if (updateRanks)
			{
				await UpdateRanks(guild, window, dataBaseService);
			}
		}

		/// <summary>
		/// Updates ranks based on submissions during the given progress report window
		/// </summary>
		/// <param name="windowID"></param>
		/// <returns></returns>
		[Command("updateranks")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task ForceUpdateRanks(int windowID)
		{
			var db = DataBaseService;

			ProgressReportWindow window = null;
			await db.RunGuildDBAction(Context.Guild, connection =>
			{
				window = ProgressReportWindow.Find(connection, windowID).As<ProgressReportWindow>();
			});

			if (window != null && !window.IsEmpty)
			{
				await UpdateRanks(Context.Guild, window, db);
			}
		}

		/// <summary>
		/// Updates ranks based on submissions during the most recent progress report window
		/// </summary>
		/// <returns></returns>
		[Command("updateranks")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task ForceUpdateRanks()
		{
			var db = DataBaseService;
			ProgressReportWindow window = null;

			await db.RunGuildDBAction(Context.Guild, connection =>
			{
				var windows = ProgressReportWindow.FindAll(connection).ContentAs<ProgressReportWindow>();
				if (windows.Content.Count > 0)
				{
					windows.Content.Sort((a, b) => b.StartTime.CompareTo(a.StartTime));
					window = windows.Content[0];
				}
			});

			if (window != null && !window.IsEmpty)
			{
				await UpdateRanks(Context.Guild, window, db);
			}
			else
			{
				await Context.Channel.SendMessageAsync("No report windows found!");
			}
		}

		private static async Task UpdateRanks(IGuild guild, ProgressReportWindow window, DataBaseService dataBaseService)
		{
			List<ProgressReport> submissions = null;
			List<Rank> ranks = null;
			ISocketMessageChannel announceChannel = null;
			var changeList = new List<(IGuildUser user, IRole oldRole, IRole newRole)>();

			var db = dataBaseService;
			await db.RunGuildDBAction(guild, connection =>
			{
				var allRanks = Rank.FindAll(connection).ContentAs<Rank>();
				foreach (var rank in allRanks.Content)
				{
					rank.Initialize(guild);
				}
				ranks = allRanks.Content.ToList();

				var allSubmissions = ProgressReport.Find(connection, pr => pr.WindowID == window.ID).ContentAs<ProgressReport>();
				foreach (var submission in allSubmissions.Content)
				{
					submission.Initialize(guild);
				}
				submissions = allSubmissions.Content.ToList();

				var announcementChannelOption = Option.FindOne(connection, o => o.Name == OptionAnnouncementChannel).As<Option>();
				if (!announcementChannelOption.IsEmpty)
				{
					var g = guild as SocketGuild;
					announceChannel = g.GetChannel(ulong.Parse(announcementChannelOption.Value)) as ISocketMessageChannel;
				}
			});

			ranks.Sort((x, y) => x.Threshold.CompareTo(y.Threshold));
			var rankRoleIDs = ranks.Select(r => r.Role.Id).ToList();

			foreach (var submission in submissions)
			{
				var user = await guild.GetUserAsync(submission.User.Id);
				var count = submission.WordCount;

				IRole newRole = null;
				IRole currentRole = null;
				var currentRankId = user.RoleIds.Where(rid => rankRoleIDs.Contains(rid));
				if (currentRankId.Any())
				{
					currentRole = guild.GetRole(currentRankId.First());
				}

				foreach (var rank in ranks)
				{
					if (count >= rank.Threshold)
					{
						newRole = rank.Role;
					}
					else if (count < rank.Threshold)
					{
						break;
					}
				}

				if (newRole != null)
				{
					if (newRole != currentRole)
					{
						if (currentRole != null)
						{
							await user.RemoveRoleAsync(currentRole.Id);
						}
						await user.AddRoleAsync(newRole.Id);
						changeList.Add((user, currentRole, newRole));
					}
				}
			}

			if (announceChannel != null)
			{
				var embed = new EmbedBuilder()
					.WithTitle("Progress Report Rank Updates");
				if (changeList.Count < 1)
				{
					embed.WithDescription("No rank changes occurred for this window");
					await announceChannel.SendMessageAsync(embed: embed.Build());
				}
				else
				{
					var fields = new List<EmbedFieldBuilder>();
					foreach (var change in changeList)
					{
						var oldRank = change.oldRole?.Name ?? "No Role";
						var newField = new EmbedFieldBuilder()
							.WithName(change.user.Username)
							.WithValue($"{oldRank} => {change.newRole.Name}");
						fields.Add(newField);
					}
					embed.WithFields(fields);
					await announceChannel.SendMessageAsync(embed: embed.Build());
				}
			}
		}

		/// <summary>
		/// Enables the Progress Report module
		/// </summary>
		/// <returns></returns>
		[Command("enable")]
		[Alias("on", "true")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task EnableModule()
		{
			var db = DataBaseService;

			// Find or Create options and set
			Option option = null;
			await db.RunGuildDBAction(Context.Guild, connection =>
			{
				option = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();

				if (option.IsEmpty)
				{
					option = new Option(connection);
					option.Name = OptionEnabled;
					option.Initialize();
				}

				option.Value = "true";
				option.Save();
			});

			await RebuildJobs(Context.Guild, DataBaseService, SchedulerService);

			await Context.Channel.SendMessageAsync("Progress report module enabled");
		}

		/// <summary>
		/// Disables the Progress Report module
		/// </summary>
		/// <returns></returns>
		[Command("disable")]
		[Alias("off", "false")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task DisableModule()
		{
			var db = DataBaseService;

			// Find or Create option and set to false
			Option option = null;
			await db.RunGuildDBAction(Context.Guild, connection =>
			{
				option = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();

				if (option.IsEmpty)
				{
					option = new Option(connection);
					option.Name = OptionEnabled;
					option.Initialize();
				}

				option.Value = "false";
				option.Save();
			});

			await RebuildJobs(Context.Guild, DataBaseService, SchedulerService);

			await Context.Channel.SendMessageAsync("Progress report module disabled");
		}

		/// <summary>
		/// Forces open a submission window starting at the current time and
		/// lasting for the given duration
		/// </summary>
		/// <param name="duration"></param>
		/// <returns></returns>
		[Command("forceopen")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task ForceOpenWindow(int duration)
		{
			try
			{
				await OpenWindow(duration);
			}
			catch (DBException ex)
			{
				await Context.Channel.SendMessageAsync($"Unable to force open window: {ex}");
			}
		}

		/// <summary>
		/// Lists the current user's Progress Reports
		/// </summary>
		/// <returns></returns>
		[Command("list")]
		public async Task ListReports()
		=> await ListReports(Context.Message.Author, 1);

		/// <summary>
		/// Lists a given user's Progress Reports
		/// </summary>
		/// <param name="user"></param>
		/// <returns></returns>
		[Command("list")]
		public async Task ListReports(IUser user)
		=> await ListReports(user, 1);

		/// <summary>
		/// Lists a given user's progress reports at the given offset
		/// </summary>
		/// <param name="user"></param>
		/// <param name="page"></param>
		/// <returns></returns>
		[Command("list")]
		public async Task ListReports(IUser user, int page)
		{
			ViewModelContainer<ProgressReport> reports = null;

			// convenience alias
			var db = DataBaseService;
			await db.RunGuildDBAction(Context.Guild, connection =>
			{
				var userID = user.Id.ToString();
				reports = ProgressReport.Find(connection, pr => pr.UserID == userID).ContentAs<ProgressReport>();
			});

			if (reports.IsEmpty)
			{
				await Context.Channel.SendMessageAsync($"<@{user.Id}> does not have any reports to list!");
				return;
			}

			// build embed and post
			var fields = new List<EmbedFieldBuilder>();
			foreach (var report in reports.Content)
			{
				var note = report.Note != "" ?
					$"\n {report.Note}" :
					"";
				var newField = new EmbedFieldBuilder()
					.WithName($"[#{report.ID}]: {new DateTimeOffset(report.TimeStamp.ToLocalTime()).ToString("d/M/yyyy hh:mm tt K")}")
					.WithValue($"`{report.WordCount} words`{note}");
				fields.Add(newField);
			}
			var embed = new EmbedBuilder()
				.WithAuthor(user)
				.WithFields(fields);

			await Context.Channel.SendMessageAsync(text: $"Reports for <@{user.Id}>", embed: embed.Build());
		}

		/// <summary>
		/// List reports for the current Progress Report window
		/// </summary>
		/// <returns></returns>
		[Command("query")]
		[Alias("q")]
		public async Task QueryWindow()
		{
			await QueryWindow(DateTime.Now.ToString("yyyyMMdd"));
		}

		/// <summary>
		/// List reports for the Progress Report window with the given ID
		/// </summary>
		/// <param name="windowID"></param>
		/// <returns></returns>
		[Command("query")]
		[Alias("q")]
		public async Task QueryWindow(int windowID)
		{
			ProgressReportWindow window = null;
			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				window = ProgressReportWindow.Find(connection, windowID).As<ProgressReportWindow>();
			});

			if (window != null && !window.IsEmpty)
			{
				await ListWindowReports(Context, window);
			}
			else
			{
				await Context.Channel.SendMessageAsync($"Could not find window with ID {windowID}");
			}
		}

		/// <summary>
		/// List reports for the Progress Report window which occurred on the given date
		/// </summary>
		/// <param name="date"></param>
		/// <returns></returns>
		[Command("query")]
		[Alias("q")]
		public async Task QueryWindow([Remainder] string date)
		{
			var pattern = @"(?<year>[0-9]{4})[.\-/\\_]?(?<month>[0-9]{2})[.\-/\\_]?(?<day>[0-9]{2})";

			var match = Regex.Match(date, pattern);
			if (match.Success)
			{
				var year = int.Parse(match.Groups["year"].Value);
				var month = int.Parse(match.Groups["month"].Value);
				var day = int.Parse(match.Groups["day"].Value);

				var dateStamp = new DateTime(year, month, day);

				ProgressReportWindow window = null;
				await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
				{
					window = ProgressReportWindow.FindOne(connection, w =>
					{
						return dateStamp >= w.StartTime.Date && dateStamp <= w.StartTime.AddHours(w.Duration).Date;
					}).As<ProgressReportWindow>();
				});

				if (window != null && !window.IsEmpty)
				{
					await ListWindowReports(Context, window);
				}
				else
				{
					await Context.Channel.SendMessageAsync("No window found for the given date");
				}
			}
			else
			{
				await Context.Channel.SendMessageAsync("Invalid date format: please use YYYYMMDD");
			}
		}

		private async Task ListWindowReports(SocketCommandContext context, ProgressReportWindow window)
		{
			// todo: build report list embed
			var submissions = new List<ProgressReport>();
			await DataBaseService.RunGuildDBAction(Context.Guild, connection => 
			{
				var subList = ProgressReport.Find(connection, pr => pr.WindowID == window.ID).ContentAs<ProgressReport>();
				subList.Content.ForEach(s => s.Initialize(Context.Guild));
				submissions =  subList.Content;
			});

			if (submissions.Count < 1)
			{
				await Context.Channel.SendMessageAsync("No submissions found for this window");
				return;
			}

			var fields = new List<EmbedFieldBuilder>();
			foreach (var submission in submissions)
			{
				var note = submission.Note != "" ?
					$"\n{submission.Note}" :
					"";
				var newField = new EmbedFieldBuilder()
					.WithName(submission.User.Username)
					.WithValue($"`{submission.WordCount}` words{note}");
				fields.Add(newField);
			}

			var embed = new EmbedBuilder()
				.WithTitle($"Reports for Window #{window.ID}")
				.WithFields(fields);

			await Context.Channel.SendMessageAsync(embed: embed.Build());
		}

		/// <summary>
		/// Edits the word count and note for the Progress Report with the given ID
		/// </summary>
		/// <param name="submissionID"></param>
		/// <param name="wordCount"></param>
		/// <param name="note"></param>
		/// <returns></returns>
		[Command("edit")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task EditSubmission(int submissionID, int wordCount, [Remainder] string note)
		{
			await EditUserSubmission(submissionID, wordCount, note);
		}

		/// <summary>
		/// Edits the word count for the Progress Report with the given ID
		/// </summary>
		/// <param name="submissionID"></param>
		/// <param name="wordCount"></param>
		/// <returns></returns>
		[Command("edit")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task EditSubmission(int submissionID, int wordCount)
		{
			await EditUserSubmission(submissionID, wordCount: wordCount);
		}

		/// <summary>
		/// Edits the note for the Progress Report with the given ID
		/// </summary>
		/// <param name="submissionID"></param>
		/// <param name="note"></param>
		/// <returns></returns>
		[Command("edit")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task EditSubmission(int submissionID, [Remainder] string note)
		{
			await EditUserSubmission(submissionID, note: note);
		}

		private async Task EditUserSubmission(int submissionID, int wordCount = -1, string note = null)
		{
			string message = null;

			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				var submission = ProgressReport.Find(connection, submissionID).As<ProgressReport>();
				if (submission.IsEmpty)
				{
					message = $"Could not find submission with ID {submissionID}";
				}
				else
				{
					var saveRequired = false;
					if (wordCount >= 0)
					{
						submission.WordCount = wordCount;
						message = $"Report word count updated: {wordCount}";
						saveRequired = true;
					}

					if (note != null)
					{
						submission.Note = note;
						message += $"\nReport note updated: {note}";
						saveRequired = true;
					}

					if (saveRequired)
					{
						submission.Save();
					}
				}
			});

			if (message != null && message != String.Empty)
			{
				await Context.Channel.SendMessageAsync(message);
			}
		}

		/// <summary>
		/// Deletes the Progress Report submission with the given ID
		/// </summary>
		/// <param name="submissionID"></param>
		/// <returns></returns>
		[Command("delete")]
		[Alias("remove", "rm")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task DeleteSubmission(int submissionID)
		{
			string message = null;
			await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
			{
				var submission = ProgressReport.Find(connection, submissionID).As<ProgressReport>();

				if (!submission.IsEmpty)
				{
					submission.Delete();
					message = "Progress report submission deleted!";
				}
				else
				{
					message = $"Progress report submission with ID {submissionID} not found";
				}
			});

			if (message != null && message != String.Empty)
			{
				await Context.Channel.SendMessageAsync(message);
			}
		}

		/// <summary>
		/// Displays info on the current Progress Report Window (if present) and the
		/// next window, if scheduled
		/// </summary>
		/// <returns></returns>
		[Command("info")]
		public async Task DisplayInfo()
		{
			var db = DataBaseService;
			bool enabled = false;
			Option windowScheduleOption = null;
			Option windowDurationOption = null;
			ProgressReportWindow currentWindow = null;

			await db.RunGuildDBAction(Context.Guild, connection =>
			{
				var enableOption = Option.FindOne(connection, o => o.Name == OptionEnabled).As<Option>();
				if (!enableOption.IsEmpty)
				{
					enabled = bool.Parse(enableOption.Value);
				}

				windowScheduleOption = Option.FindOne(connection, o => o.Name == OptionWindowOpen).As<Option>();
				windowDurationOption = Option.FindOne(connection, o => o.Name == OptionWindowDuration).As<Option>();

				var time = DateTime.UtcNow;
				currentWindow = ProgressReportWindow.FindOne(connection, w =>
				{
					return time >= w.StartTime && time <= w.StartTime.AddHours(w.Duration);
				}).As<ProgressReportWindow>();
			});

			if (!enabled)
			{
				await Context.Channel.SendMessageAsync("Progress Report Module is not currently enabled!");
				return;
			}

			if (currentWindow != null && !currentWindow.IsEmpty)
			{
				var open = new DateTimeOffset(currentWindow.StartTime.ToLocalTime()).ToUnixTimeSeconds();
				var close = new DateTimeOffset(currentWindow.EndTime.ToLocalTime()).ToUnixTimeSeconds();

				await Context.Channel.SendMessageAsync($"The current submission window opened at <t:{open}:F> and will remain open until <t:{close}:F>");
			}

			if (
					(windowScheduleOption != null && !windowScheduleOption.IsEmpty) &&
					(windowDurationOption != null && !windowDurationOption.IsEmpty)
				)
			{
				var cron = CronExpression.Parse(windowScheduleOption.Value);
				var next = cron.GetNextOccurrence(new DateTimeOffset(DateTime.Now), TimeZoneInfo.Local);

				var open = next.Value.ToUnixTimeSeconds();
				var close = next.Value.AddHours(int.Parse(windowDurationOption.Value)).ToUnixTimeSeconds();

				await Context.Channel.SendMessageAsync($"The next submission window will open at <t:{open}:f> and will remain open until <t:{close}:f>");
			}
		}

		/// <summary>
		/// Displays rank info
		/// </summary>
		/// <returns></returns>
		[Command("ranks")]
		public async Task DisplayRanks()
		{
			//todo: display ranks if ranks are enabled and present.
			var db = DataBaseService;

			await db.RunGuildDBAction(Context.Guild, async connection =>
			{
				var enabledOption = Option.FindOne(connection, o => o.Name == OptionRanksEnabled).As<Option>();

				if (enabledOption.IsEmpty)
				{
					enabledOption = new Option(connection);
					enabledOption.Name = OptionRanksEnabled;
					enabledOption.Initialize();
					enabledOption.Save();
				}

				if (!bool.Parse(enabledOption.Value))
				{
					await Context.Channel.SendMessageAsync("Ranks are not enabled on this server");
					return;
				}

				var ranks = Rank.FindAll(connection).ContentAs<Rank>();

				if (ranks.Content.Count < 1)
				{
					await Context.Channel.SendMessageAsync("No ranks have been defined");
					return;
				}

				ranks.Content.Sort((r1, r2) => r2.Threshold.CompareTo(r1.Threshold));

				var message = "";
				foreach (var rank in ranks.Content)
				{
					rank.Initialize(Context.Guild);
					message += $"{rank.Role.Name}: {rank.Threshold}\n";
				}
				await Context.Channel.SendMessageAsync("Ranks: \n" + message);
			});
		}

		private async Task OpenWindow(int duration = -1)
		=> await OpenWindow(Context.Guild, DataBaseService, SchedulerService, duration);

		private async Task CloseWindow(IGuild guild, ProgressReportWindow window)
		=> await CloseWindow(guild, window, SchedulerService, DataBaseService);

		/// <summary>
		/// Command module containing command used to set and alter Progress Report module options
		/// </summary>
		[Group("option")]
		[Alias("set", "o")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public class ModuleOptions : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// DataBaseService reference
			/// </summary>
			/// <remarks>
			/// Set via Dependency Injection
			/// </remarks>
			public DataBaseService DataBaseService { get; set; }

			/// <summary>
			/// SchedulerService reference
			/// </summary>
			/// <remarks>
			/// Set via Dependency Injection
			/// </remarks>
			public SchedulerService SchedulerService { get; set; }

			/// <summary>
			/// Sets weekly window open time and duration
			/// </summary>
			/// <param name="openCron"></param>
			/// <param name="duration"></param>
			/// <returns></returns>
			[Command("window")]
			public async Task SetWindow(string openCron, int duration)
			{
				if (!CronJob.TryValidate(openCron, out string message))
				{
					await Context.Channel.SendMessageAsync($"Error setting open cron: {message}");
					return;
				}
				await SetWindowOpen(openCron);
				await SetWindowDuration(duration);
			}

			/// <summary>
			/// Sets weekly window open time
			/// </summary>
			/// <param name="cron"></param>
			/// <returns></returns>
			[Command("windowopen")]
			[Alias("open")]
			public async Task SetWindowOpen(string cron)
			{
				var db = DataBaseService;
				if (!CronJob.TryValidate(cron, out string message))
				{
					await Context.Channel.SendMessageAsync($"Error setting open cron: {message}");
					return;
				}

				// Find window open option and set
				Option option = null;
				await db.RunGuildDBAction(Context.Guild, connection =>
				{
					option = Option.FindOne(connection, o => o.Name == OptionWindowOpen).As<Option>();

					if (option.IsEmpty)
					{
						option = new Option(connection);
						option.Name = OptionWindowOpen;
						option.Initialize();
					}

					option.Value = cron;
					option.Save();
				});

				await RebuildJobs(Context.Guild, DataBaseService, SchedulerService);

				await Context.Channel.SendMessageAsync("Progress report window open updated");
			}

			/// <summary>
			/// Sets duration of progress report windows
			/// </summary>
			/// <param name="duration"></param>
			/// <returns></returns>
			[Command("windowduration")]
			[Alias("windowlength", "length", "duration")]
			public async Task SetWindowDuration(int duration)
			{
				if (duration < 1)
				{
					await Context.Channel.SendMessageAsync("Error setting submission window duration. Duration must be at least 1");
					return;
				}
				// todo: validate cron string
				var db = DataBaseService;

				// Find window duration option and set
				Option option = null;
				await db.RunGuildDBAction(Context.Guild, connection =>
				{
					option = Option.FindOne(connection, o => o.Name == OptionWindowDuration).As<Option>();

					if (option.IsEmpty)
					{
						option = new Option(connection);
						option.Name = OptionWindowDuration;
						option.Initialize();
					}

					option.Value = duration.ToString();
					option.Save();
				});

				// Set up/update jobs
				await RebuildJobs(Context.Guild, DataBaseService, SchedulerService);

				await Context.Channel.SendMessageAsync($"Progress report window duration updated. Windows will be open for {duration} hour(s).");
			}

			/// <summary>
			/// Sets the channel where submission window open/close announcements are posted
			/// </summary>
			/// <param name="channel"></param>
			/// <returns></returns>
			[Command("announcechannel")]
			[Alias("announceat", "announce")]
			public async Task SetAnnouncementChannel(IMessageChannel channel)
			{
				if (channel is SocketChannel socketChannel)
				{
					// convenience alias
					var db = DataBaseService;
					await db.RunGuildDBAction(Context.Guild, connection =>
					{
						var announcementChannelOption = Option.FindOne(connection, o => o.Name == OptionAnnouncementChannel).As<Option>();
						if (announcementChannelOption.IsEmpty)
						{
							announcementChannelOption = new Option(connection)
							{
								Name = OptionAnnouncementChannel,
							};
							announcementChannelOption.Initialize();
						}
						announcementChannelOption.Value = socketChannel.Id.ToString();
						announcementChannelOption.Save();
					});
					await Context.Channel.SendMessageAsync($"Progress report announcement channel set to <#{channel.Id}>");
				}
				else
				{
					await Context.Channel.SendMessageAsync("Invalid channel specified");
				}
			}

			/// <summary>
			/// Sets the channel reports must be submitted to
			/// </summary>
			/// <param name="channel"></param>
			/// <returns></returns>
			[Command("reportchannel")]
			[Alias("reportat", "submitchannel", "submitat")]
			public async Task SetReportChannel(IMessageChannel channel)
			{
				if (channel is SocketChannel socketChannel)
				{
					// convenience alias
					var db = DataBaseService;
					await db.RunGuildDBAction(Context.Guild, connection =>
					{
						var reportChannelOption = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
						if (reportChannelOption.IsEmpty)
						{
							reportChannelOption = new Option(connection)
							{
								Name = OptionReportChannel,
							};
							reportChannelOption.Initialize();
						}
						reportChannelOption.Value = socketChannel.Id.ToString();
						reportChannelOption.Save();

						var restrictChannelOption = Option.FindOne(connection, o => o.Name == OptionRestrictReportChannel).As<Option>();
						if (restrictChannelOption.IsEmpty)
						{
							restrictChannelOption = new Option(connection)
							{
								Name = OptionRestrictReportChannel
							};
							restrictChannelOption.Initialize();
						}
						restrictChannelOption.Value = "true";
						restrictChannelOption.Save();
					});
					await Context.Channel.SendMessageAsync($"Progress report submission channel set to <#{channel.Id}>");
				}
				else
				{
					await Context.Channel.SendMessageAsync("Invalid channel specified");
				}
			}

			/// <summary>
			/// Unsets the report channel and the restricted report channel option
			/// </summary>
			/// <returns></returns>
			[Command("clearreportchannel")]
			public async Task ClearReportChannel()
			{
				// convenience alias
				var db = DataBaseService;
				await db.RunGuildDBAction(Context.Guild, connection =>
				{
					var reportChannelOption = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
					if (!reportChannelOption.IsEmpty)
					{
						reportChannelOption.Delete();
					}

					var restrictChannelOption = Option.FindOne(connection, o => o.Name == OptionRestrictReportChannel).As<Option>();
					if (restrictChannelOption.IsEmpty)
					{
						restrictChannelOption = new Option(connection)
						{
							Name = OptionRestrictReportChannel
						};
						restrictChannelOption.Initialize();
					}
					restrictChannelOption.Value = "false";
					restrictChannelOption.Save();
				});
				await Context.Channel.SendMessageAsync("Report submission channel cleared. Progress report submissions can now be submitted in any channel");
			}

			/// <summary>
			/// Sets the role to be pinged when Progress Report windows open and close
			/// </summary>
			/// <param name="role"></param>
			/// <returns></returns>
			[Command("reminder")]
			[Alias("remind")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task SetRemindRole(IRole role)
			{
				await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
				{
					var option = Option.FindOne(connection, o => o.Name == OptionWindowReminderRole).As<Option>();
					if (option.IsEmpty)
					{
						option = new Option(connection)
						{
							Name = OptionWindowReminderRole,
						};
						option.Initialize();
					}
					option.Value = role.Id.ToString();
					option.Save();
				});
				await Context.Channel.SendMessageAsync("Progress Report reminder role set!");
			}

			/// <summary>
			/// Clears the Progress Report Window open/close reminder role
			/// </summary>
			/// <returns></returns>
			[Command("unsetreminder")]
			[Alias("clearreminder", "noreminder")]
			[RequireUserPermission(GuildPermission.Administrator)]
			public async Task ClearRemindRole()
			{
				await DataBaseService.RunGuildDBAction(Context.Guild, connection =>
				{
					var option = Option.FindOne(connection, o => o.Name == OptionWindowReminderRole).As<Option>();
					if (!option.IsEmpty)
					{
						option.Delete();
					}
				});
				await Context.Channel.SendMessageAsync("Progress Report reminder role cleared!");
			}
		}

		/// <summary>
		/// Contains commands pertaining to the progress report module's optional rank capabilities
		/// </summary>
		[Group("rank")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public class RankOptions : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// DataBaseService reference
			/// </summary>
			/// <remarks>
			/// Set via Dependency Injection
			/// </remarks>
			public DataBaseService DataBaseService { get; set; }

			/// <summary>
			/// Enables automatic rankings
			/// </summary>
			/// <returns></returns>
			[Command("enable")]
			public async Task EnableRanks()
			{
				var db = DataBaseService;

				// Find or Create option and set to true
				Option option = null;
				await db.RunGuildDBAction(Context.Guild, connection =>
				{
					option = Option.FindOne(connection, o => o.Name == OptionRanksEnabled).As<Option>();

					if (option.IsEmpty)
					{
						option = new Option(connection);
						option.Name = OptionEnabled;
						option.Initialize();
					}

					option.Value = "true";
					option.Save();
				});

				await Context.Channel.SendMessageAsync("Progress report ranking enabled");
			}

			/// <summary>
			/// Disables automatic rankings
			/// </summary>
			/// <returns></returns>
			[Command("disable")]
			public async Task DisableRanks()
			{
				var db = DataBaseService;

				// Find or Create option and set to false
				Option option = null;
				await db.RunGuildDBAction(Context.Guild, connection =>
				{
					option = Option.FindOne(connection, o => o.Name == OptionRanksEnabled).As<Option>();

					if (option.IsEmpty)
					{
						option = new Option(connection);
						option.Name = OptionEnabled;
						option.Initialize();
					}

					option.Value = "false";
					option.Save();
				});

				await Context.Channel.SendMessageAsync("Progress report ranking enabled");
			}

			/// <summary>
			/// Adds a role as a rank
			/// </summary>
			/// <param name="role">Role to be added as a rank</param>
			/// <param name="threshold">Word cound threshold to qualify for the rank</param>
			/// <returns></returns>
			[Command("add")]
			public async Task AddRank(IRole role, int threshold)
			{
				var db = DataBaseService;

				await db.RunGuildDBAction(Context.Guild, async connection =>
				{
					try
					{
						var rank = new Rank(connection)
						{
							Role = role,
							Threshold = threshold
						};
						rank.Initialize(Context.Guild);
						rank.Save();
						await Context.Channel.SendMessageAsync($"Rank added: \"{role.Name}\" ({threshold})");
					}
					catch (ConstraintViolationException ex)
					{
						await Context.Channel.SendMessageAsync($"Error adding new rank: {ex}");
					}
				});
			}

			/// <summary>
			/// Removes a role as a rank
			/// </summary>
			/// <param name="role"></param>
			/// <returns></returns>
			[Command("remove")]
			[Alias("delete")]
			public async Task RemoveRank(IRole role)
			{
				var db = DataBaseService;

				await db.RunGuildDBAction(Context.Guild, async connection =>
				{
					var roleId = role.Id.ToString();
					var rank = Rank.FindOne(connection, r => r.RoleID == roleId).As<Rank>();
					rank.Delete();
					await Context.Channel.SendMessageAsync("Rank removed");
				});
			}

			/// <summary>
			/// Modifies a rank's threshold
			/// </summary>
			/// <param name="role"></param>
			/// <param name="threshold"></param>
			/// <returns></returns>
			[Command("edit")]
			public async Task EditRank(IRole role, int threshold)
			{
				var db = DataBaseService;

				await db.RunGuildDBAction(Context.Guild, async connection =>
				{
					try
					{
						var roleId = role.Id.ToString();
						var rank = Rank.FindOne(connection, r => r.RoleID == roleId).As<Rank>();

						if (rank.IsEmpty)
						{
							await Context.Channel.SendMessageAsync("Rank not found!");
							return;
						}

						if (rank.Threshold == threshold)
						{
							await Context.Channel.SendMessageAsync($"Rank already has threshold of {threshold}. No change required.");
							return;
						}

						rank.Threshold = threshold;
						rank.Save();
						await Context.Channel.SendMessageAsync("Rank threshold updated!");
					}
					catch (ConstraintViolationException)
					{
						await Context.Channel.SendMessageAsync($"Error editing rank. Thresholds must be unique; ensure this threshold isn't already in use by another rank!");
					}
				});
			}

			/// <summary>
			/// Clears all ranks
			/// </summary>
			/// <returns></returns>
			[Command("clear")]
			public async Task ClearRanks()
			{
				var db = DataBaseService;

				await db.RunGuildDBAction(Context.Guild, async connection =>
				{
					try
					{
						var ranks = Rank.FindAll(connection).ContentAs<Rank>();
						ranks.Content.ForEach(r => r.Delete());
						await Context.Channel.SendMessageAsync("Ranks removed");
					}
					catch (Exception ex)
					{
						await Context.Channel.SendMessageAsync($"Error removing ranks: {ex}");
					}
				});
			}
		}
	}

	/// <summary>
	/// Contains commands for users to submit progress reports
	/// </summary>
	[Group("report")]
	[Alias("r", "rp")]
	public class ReportModule : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// DataBaseService reference
		/// </summary>
		/// <remarks>
		/// Set via Dependency Injection
		/// </remarks>
		public DataBaseService DataBaseService { get; set; }

		/// <summary>
		/// Submits a progress report
		/// </summary>
		/// <param name="wordCount"></param>
		/// <param name="note"></param>
		/// <returns></returns>
		[Command]
		public async Task SubmitReport(int wordCount, [Remainder] string note)
		{
			await SubmitReport(Context.User, wordCount, note);
		}

		/// <summary>
		/// Submits a progress report
		/// </summary>
		/// <param name="wordcount"></param>
		/// <returns></returns>
		[Command]
		public async Task SubmitReport(int wordcount)
		=> await SubmitReport(wordcount, "");

		/// <summary>
		/// Submits a progress report on behalf of a user
		/// </summary>
		/// <param name="user"></param>
		/// <param name="wordCount"></param>
		/// <param name="note"></param>
		/// <returns></returns>
		[Command]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task SubmitReport(IUser user, int wordCount, [Remainder] string note)
		{
			// convenience alias
			var db = DataBaseService;

			await db.RunGuildDBAction(Context.Guild, async connection =>
			{
				// Check to make sure the report was submitted in the correct channel (if restricted report channel is set)
				var restrictedOption = Option.FindOne(connection, o => o.Name == ProgressReportModule.OptionRestrictReportChannel).As<Option>();
				if (restrictedOption.IsEmpty)
				{
					restrictedOption = new Option(connection)
					{
						Name = ProgressReportModule.OptionRestrictReportChannel
					};
					restrictedOption.Initialize();
				}
				var restricted = bool.Parse(restrictedOption.Value);

				if (restricted)
				{
					var reportChannelOption = Option.FindOne(connection, o => o.Name == ProgressReportModule.OptionReportChannel).As<Option>();
					if (reportChannelOption.IsEmpty)
					{
						// todo: log that restricted is true, but no restricted channel is set
						restricted = false;
					}
					else
					{
						var reportChannelID = ulong.Parse(reportChannelOption.Value);
						if (Context.Channel.Id != reportChannelID)
						{
							await Context.Channel.SendMessageAsync($"Error recording submission: Reports must be submitted in <#{reportChannelID}>");
							return;
						}
					}
				}

				// Check to make sure a submission window is active
				var time = Context.Message.Timestamp.UtcDateTime;
				var window = ProgressReportWindow.FindOne(connection, w =>
				{
					return time >= w.StartTime && time <= w.StartTime.AddHours(w.Duration);
				}).As<ProgressReportWindow>();
				if (window.IsEmpty)
				{
					await Context.Channel.SendMessageAsync("Error recording submission: Progress report submissions are not currently open.");
					return;
				}

				// Update report for current user and window if one exists; otherwise make a new one
				var userID = user.Id.ToString();
				var windowID = window.ID.ToString();
				var report = ProgressReport.FindOne(connection, pr => pr.UserID == userID && pr.WindowID == window.ID).As<ProgressReport>();
				var statusText = "updated";
				if (report.IsEmpty)
				{
					statusText = "submitted";
					report = new ProgressReport(connection)
					{
						User = user,
						Window = window
					};
					report.Initialize();
				}
				report.WordCount = wordCount;
				report.Note = note;
				report.TimeStamp = Context.Message.Timestamp.UtcDateTime;

				report.Save();
				await Context.Channel.SendMessageAsync($"Progress report {statusText}. Great work, <@{user.Id}>!");
			});
		}

		/// <summary>
		/// Submits a progress report on behalf of a user
		/// </summary>
		/// <param name="user"></param>
		/// <param name="wordCount"></param>
		/// <returns></returns>
		[Command]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task SubmitReport(IUser user, int wordCount)
		=> await SubmitReport(user, wordCount, "");
	}
}