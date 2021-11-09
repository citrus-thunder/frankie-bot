#pragma warning disable 1591 // Hide XMLdoc warnings.

using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

using FrankieBot.Discord.Services;

namespace FrankieBot.Discord.Modules
{
	/// <summary>
	/// Command Module containing test commands
	/// </summary>
	public class TestModule : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Reference to SchedulerService
		/// </summary>
		/// <remarks>
		/// Set automatically via Dependency Injection
		/// </remarks>
		public SchedulerService SchedulerService { get; set; }

		/// <summary>
		/// Responds to user message containing the key word. Used for testing.
		/// </summary>
		/// <returns></returns>
		[Command("ping")]
		public Task PingAsync()
			=> ReplyAsync("pong!");

		[Command("cron")]
		[RequireOwner]
		public async Task CronTest([Remainder] string cronExpression)
		{
			await Context.Channel.SendMessageAsync("Cron test submitted");
			var cron = Cronos.CronExpression.Parse(cronExpression);
			var next = cron.GetNextOccurrence(DateTime.UtcNow);
			var span = next - DateTime.UtcNow;
			await Context.Channel.SendMessageAsync($"Next test in {span}");

			var job = await SchedulerService.AddJob(Context, "test job", cronExpression);
			job.Run += async (object sender, EventArgs e) =>
			{
				await Context.Channel.SendMessageAsync("Test!");
			};
		}

		[Command("stopcron")]
		[RequireOwner]
		public void StopCron()
		{
			SchedulerService.GetJob(Context.Guild, "test job")?.Stop();
		}
	}
}
#pragma warning restore 1591