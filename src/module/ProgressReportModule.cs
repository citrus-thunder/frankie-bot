using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;

using SQLite;

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
		/// Option title for option which sets the role Frankie pings to remind users of progress report
		/// window openings/closings
		/// </summary>
		public const string OptionWindowReminderRole = "progress_report_reminder_role";

		/// <summary>
		/// Title of job responsible for opening scheduled progress report windows
		/// </summary>
		public const string JobAnnounceWindowOpened = "progress_report_announce_window_opened";

		/// <summary>
		/// Title of job responsible for announcing the closure of scheduled progress report windows
		/// </summary>
		public const string JobAnnounceWindowClosed = "progress_report_announce_window_closed";

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
			// if module is disabled, find and stop jobs (if found)
			// if enabled, start jobs w/ current options
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

		private async Task OpenWindow(int duration)
		{
			await Task.CompletedTask; // temp
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

				await Context.Channel.SendMessageAsync("Progress report window open updated");
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
		/// Submits a progress report
		/// </summary>
		/// <param name="wordCount"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		[Command]
		public async Task SubmitReport(int wordCount, string description)
		{
			await Task.CompletedTask; // temp
		}

		/// <summary>
		/// Submits a progress report on behalf of a user
		/// </summary>
		/// <param name="user"></param>
		/// <param name="wordcount"></param>
		/// <param name="description"></param>
		/// <returns></returns>
		[Command]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task SubmitReport(IUser user, int wordcount, string description)
		{
			await Task.CompletedTask; // temp
		}
	}
}