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
	[Group("progress")]
	[Alias("p", "pr")]
	public class ProgressReportModule : ModuleBase<SocketCommandContext>
	{
		public const string OptionEnabled = "progress_report_module_enabled";
		public const string OptionWindowOpen = "progress_report_window_open";
		public const string OptionWindowDuration = "progress_report_window_duration";
		public const string OptionAnnouncementChannel = "progress_report_announcement_channel";
		public const string OptionWindowReminderRole = "progress_report_reminder_role";

		public const string JobAnnounceWindowOpened = "progress_report_announce_window_opened";
		public const string JobAnnounceWindowClosed = "progress_report_window_closed";

		public DataBaseService DataBaseService { get; set; }
		public SchedulerService SchedulerService { get; set; }

		protected static async Task RebuildJobs(SocketCommandContext context, SchedulerService scheduler)
		{
			// if module is disabled, find and stop jobs (if found)
			// if enabled, start jobs w/ current options
		}

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

		}

		[Group("option")]
		[Alias("set", "o")]
		[RequireUserPermission(GuildPermission.Administrator)]
		public class Options : ModuleBase<SocketCommandContext>
		{
			public DataBaseService DataBaseService { get; set; }
			public SchedulerService SchedulerService { get; set; }

			[Command("window")]
			public async Task SetWindow(string openCron, int duration)
			{
				await SetWindowOpen(openCron);
				await SetWindowDuration(duration);
			}

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

	[Group("report")]
	[Alias("r", "rp")]
	public class ReportModule : ModuleBase<SocketCommandContext>
	{
		[Command]
		public async Task SubmitReport(int wordCount, string description)
		{

		}

		[Command]
		[RequireUserPermission(GuildPermission.Administrator)]
		public async Task SubmitReport(IUser user, int wordcount, string description)
		{

		}
	}
}