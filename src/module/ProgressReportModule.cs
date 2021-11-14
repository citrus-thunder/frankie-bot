using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;
using Discord.Commands;

using SQLite;
using Cronos;

using FrankieBot.DB;
using FrankieBot.DB.ViewModel;
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

		#endregion // Options

		#region Jobs

		/// <summary>
		/// Title of job responsible for opening scheduled progress report windows
		/// </summary>
		public const string JobAnnounceWindowOpened = "progress_report_announce_window_opened";

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
		/// Checks and ensures that correct jobs are running
		/// </summary>
		/// <param name="context"></param>
		/// <param name="scheduler"></param>
		/// <returns></returns>
		protected static async Task RebuildJobs(SocketCommandContext context, SchedulerService scheduler)
		{
			// todo
			// if module is disabled, find and stop jobs (if found)
			// if enabled, start jobs w/ current options

			// todo: check if a window is currently open, set close job
			await Task.CompletedTask; // temp
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

			// Find or Create option and set to true
			Option option = null;
			await db.RunDBAction(Context, context =>
			{
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
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
				}
			});

			await RebuildJobs(Context, SchedulerService);

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
			await db.RunDBAction(Context, context =>
			{
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
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
				}
			});

			await RebuildJobs(Context, SchedulerService);

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
		=> await OpenWindow(duration);

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
		[Command ("list")]
		public async Task ListReports(IUser user, int page)
		{
			ViewModelContainer<ProgressReport> reports = null;

			// convenience alias
			var db = DataBaseService;
			await db.RunDBAction(Context, context =>
			{
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
				{
					var userID = user.Id.ToString();
					reports = ProgressReport.Find(connection, pr => pr.UserID == userID).ContentAs<ProgressReport>();
				}
			});
			
			if (reports.IsEmpty)
			{
				await Context.Channel.SendMessageAsync($"<@{user.Id}> does not have any reports to list!");
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

		private async Task OpenWindow(int duration = -1)
		{
			// convenience alias
			var db = DataBaseService;

			await db.RunDBAction(Context, async context =>
			{
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
				{
					try
					{
						var announcementChannelOption = Option.FindOne(connection, o => o.Name == OptionAnnouncementChannel).As<Option>();
						var announce = !announcementChannelOption.IsEmpty;
						ISocketMessageChannel announceChannel = null;
						if (announce)
						{
							announceChannel = Context.Guild.GetChannel(ulong.Parse(announcementChannelOption.Value)) as ISocketMessageChannel;
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

						if (announce)
						{
							await announceChannel.SendMessageAsync($"Progress report submissions are now open! Submissions will be accepted from <t:{new DateTimeOffset(window.StartTime).ToUnixTimeSeconds()}:F> until <t:{new DateTimeOffset(window.EndTime).ToUnixTimeSeconds()}:F>.");
						}

						// Create Job
						// - Update/Add Job in DB
						// - Start Window Close Job

						// convenience alias
						var scheduler = SchedulerService;

						var closeJob = scheduler.GetJob(context.Guild, JobAnnounceWindowClosed);
						if (closeJob != null)
						{
							closeJob.Stop();
							scheduler.RemoveJob(context, closeJob.Name);
						}

						var guildId = context.Guild.Id.ToString();
						var closeJobRecord = CronJob.FindOne(connection, j => j.Name == JobAnnounceWindowClosed && j.GuildID == guildId).As<CronJob>();
						if (closeJobRecord.IsEmpty)
						{
							closeJobRecord = new CronJob(connection)
							{
								Name = JobAnnounceWindowClosed,
								Guild = context.Guild
							};
						}

						closeJobRecord.Run += async (object sender, EventArgs e) =>
						{
							closeJobRecord.Stop();
							if (announce)
							{
								// should this be part of the CloseWindow command?
								await announceChannel.SendMessageAsync("The progress report submission window is now closed!");
							}
							await CloseWindow(context.Guild, window);
						};

						await scheduler.AddJob(closeJobRecord, false);
						closeJobRecord.StartAt(window.EndTime, Timeout.InfiniteTimeSpan);
					}
					catch (DBException ex)
					{
						// log exception
						await Context.Channel.SendMessageAsync($"Unable to force open window: {ex.Message}");
					}
				}
			});
		}

		private async Task CloseWindow(IGuild guild, ProgressReportWindow window)
		{
			// convenience alias
			var scheduler = SchedulerService;

			scheduler.RemoveJob(guild, JobAnnounceWindowClosed);
			await Task.CompletedTask; //temp
		}

		/// <summary>
		/// Command module containing command used to set and alter Progress Report module options
		/// </summary>
		[Group("option")]
		[Alias("set", "o")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public class Options : ModuleBase<SocketCommandContext>
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
				// todo: validate cron string
				// todo: validate window open/close sanity (no open before close, etc.) (?)
				var db = DataBaseService;

				// Find window open option and set
				Option option = null;
				await db.RunDBAction(Context, context =>
				{
					using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
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
					}
				});

				await RebuildJobs(Context, SchedulerService);

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
				await db.RunDBAction(Context, context =>
				{
					using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
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
					}
				});

				// Set up/update jobs
				await RebuildJobs(Context, SchedulerService);

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
					await db.RunDBAction(Context, context =>
					{
						using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
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
						}
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
					await db.RunDBAction(Context, context =>
					{
						using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
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
						}
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
				await db.RunDBAction(Context, context =>
				{
					using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
					{
						var reportChannelOption = Option.FindOne(connection, o => o.Name == OptionReportChannel).As<Option>();
						if (!reportChannelOption.IsEmpty)
						{
							reportChannelOption.Delete();
						}

						var restrictChannelOption = Option.FindOne(connection, o => o.Name == OptionRestrictReportChannel).As<Option>();
						if (restrictChannelOption.IsEmpty)
						{
							restrictChannelOption = new Option()
							{
								Name = OptionRestrictReportChannel
							};
							restrictChannelOption.Initialize();
						}
						restrictChannelOption.Value = "false";
						restrictChannelOption.Save();
					}
				});
				await Context.Channel.SendMessageAsync("Report submission channel cleared. Progress report submissions can now be submitted in any channel");
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
			await db.RunDBAction(Context, async context =>
			{
				using (var connection = new DBConnection(context, db.GetServerDBFilePath(context.Guild)))
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
						return time > w.StartTime && time < w.StartTime.AddHours(w.Duration);
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
				}
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